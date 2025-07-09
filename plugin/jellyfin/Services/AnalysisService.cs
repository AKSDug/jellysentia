using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Net.Client;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using JellySentia.Configuration;
using JellySentia.Models;
using Dapper;
using Microsoft.Data.Sqlite;
using TagLib;

namespace JellySentia.Services
{
    public class AnalysisService : IScheduledTask
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger<AnalysisService> _logger;
        private readonly PluginConfiguration _config;
        private readonly GrpcChannel _grpcChannel;
        private readonly AnalysisClient _analysisClient;
        private readonly string _dbPath;

        public string Name => "Music Analysis";
        public string Key => "JellySentiaAnalysis";
        public string Description => "Analyze music library with Essentia";
        public string Category => "JellySentia";

        public AnalysisService(ILibraryManager libraryManager, ILogger<AnalysisService> logger)
        {
            _libraryManager = libraryManager;
            _logger = logger;
            _config = Plugin.Instance!.Configuration;
            _dbPath = _config.DatabasePath;
            
            _grpcChannel = GrpcChannel.ForAddress($"http://{_config.AnalysisServiceUrl}");
            _analysisClient = new AnalysisClient(_grpcChannel);
            
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS analysis_results (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    item_id TEXT NOT NULL UNIQUE,
                    file_path TEXT NOT NULL,
                    analyzed_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                    tempo REAL,
                    key TEXT,
                    scale TEXT,
                    energy REAL,
                    danceability REAL,
                    valence REAL,
                    acousticness REAL,
                    instrumentalness REAL,
                    speechiness REAL,
                    loudness REAL,
                    spectral_centroid REAL,
                    spectral_rolloff REAL,
                    zero_crossing_rate REAL,
                    mfcc_mean TEXT,
                    chroma_mean TEXT,
                    feature_vector BLOB
                );
                
                CREATE INDEX IF NOT EXISTS idx_item_id ON analysis_results(item_id);
                CREATE INDEX IF NOT EXISTS idx_analyzed_at ON analysis_results(analyzed_at);
            ");
        }

        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            _logger.LogInformation("Starting music analysis task");
            
            var audioItems = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { nameof(Audio) },
                IsVirtualItem = false,
                Recursive = true
            }).OfType<Audio>().ToList();

            _logger.LogInformation($"Found {audioItems.Count} audio items to analyze");

            var analyzed = 0;
            var errors = 0;
            var semaphore = new SemaphoreSlim(_config.MaxConcurrentAnalysis);

            var tasks = audioItems.Select(async (item, index) =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    if (await IsAlreadyAnalyzed(item.Id.ToString()))
                    {
                        Interlocked.Increment(ref analyzed);
                        return;
                    }

                    await AnalyzeTrack(item, cancellationToken);
                    Interlocked.Increment(ref analyzed);
                    
                    var progressValue = (double)analyzed / audioItems.Count * 100;
                    progress.Report(progressValue);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error analyzing {item.Path}");
                    Interlocked.Increment(ref errors);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
            
            _logger.LogInformation($"Analysis complete. Analyzed: {analyzed}, Errors: {errors}");
        }

        private async Task<bool> IsAlreadyAnalyzed(string itemId)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            var count = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM analysis_results WHERE item_id = @ItemId",
                new { ItemId = itemId }
            );
            return count > 0;
        }

        private async Task AnalyzeTrack(Audio item, CancellationToken cancellationToken)
        {
            var request = new AnalysisRequest
            {
                FilePath = item.Path,
                Depth = _config.AnalysisDepth.ToString(),
                Descriptors = { _config.EnabledDescriptors }
            };

            var response = await _analysisClient.AnalyzeTrackAsync(request, cancellationToken: cancellationToken);
            
            await SaveAnalysisResults(item, response);
            
            if (_config.EnableTagWriteback)
            {
                await WriteTagsToFile(item.Path, response);
            }
        }

        private async Task SaveAnalysisResults(Audio item, AnalysisResponse response)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.ExecuteAsync(@"
                INSERT OR REPLACE INTO analysis_results (
                    item_id, file_path, tempo, key, scale, energy, danceability,
                    valence, acousticness, instrumentalness, speechiness, loudness,
                    spectral_centroid, spectral_rolloff, zero_crossing_rate,
                    mfcc_mean, chroma_mean, feature_vector
                ) VALUES (
                    @ItemId, @FilePath, @Tempo, @Key, @Scale, @Energy, @Danceability,
                    @Valence, @Acousticness, @Instrumentalness, @Speechiness, @Loudness,
                    @SpectralCentroid, @SpectralRolloff, @ZeroCrossingRate,
                    @MfccMean, @ChromaMean, @FeatureVector
                )",
                new
                {
                    ItemId = item.Id.ToString(),
                    FilePath = item.Path,
                    Tempo = response.Features.GetValueOrDefault("tempo"),
                    Key = response.Features.GetValueOrDefault("key"),
                    Scale = response.Features.GetValueOrDefault("scale"),
                    Energy = response.Features.GetValueOrDefault("energy"),
                    Danceability = response.Features.GetValueOrDefault("danceability"),
                    Valence = response.Features.GetValueOrDefault("valence"),
                    Acousticness = response.Features.GetValueOrDefault("acousticness"),
                    Instrumentalness = response.Features.GetValueOrDefault("instrumentalness"),
                    Speechiness = response.Features.GetValueOrDefault("speechiness"),
                    Loudness = response.Features.GetValueOrDefault("loudness"),
                    SpectralCentroid = response.Features.GetValueOrDefault("spectral_centroid"),
                    SpectralRolloff = response.Features.GetValueOrDefault("spectral_rolloff"),
                    ZeroCrossingRate = response.Features.GetValueOrDefault("zero_crossing_rate"),
                    MfccMean = response.Features.GetValueOrDefault("mfcc_mean"),
                    ChromaMean = response.Features.GetValueOrDefault("chroma_mean"),
                    FeatureVector = response.FeatureVector.ToByteArray()
                });
        }

        private async Task WriteTagsToFile(string filePath, AnalysisResponse response)
        {
            try
            {
                var file = TagLib.File.Create(filePath);
                
                // Write custom tags
                file.Tag.SetCustomTag("TEMPO", response.Features.GetValueOrDefault("tempo", ""));
                file.Tag.SetCustomTag("KEY", response.Features.GetValueOrDefault("key", ""));
                file.Tag.SetCustomTag("ENERGY", response.Features.GetValueOrDefault("energy", ""));
                file.Tag.SetCustomTag("DANCEABILITY", response.Features.GetValueOrDefault("danceability", ""));
                file.Tag.SetCustomTag("VALENCE", response.Features.GetValueOrDefault("valence", ""));
                file.Tag.SetCustomTag("ACOUSTICNESS", response.Features.GetValueOrDefault("acousticness", ""));
                
                await Task.Run(() => file.Save());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to write tags to {filePath}");
            }
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
            {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfo.TriggerType.Daily,
                    TimeOfDayTicks = TimeSpan.FromHours(2).Ticks
                }
            };
        }
    }
}