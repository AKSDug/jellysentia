using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Net.Client;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;
using JellySentia.Configuration;
using JellySentia.Models;
using Dapper;
using Microsoft.Data.Sqlite;

namespace JellySentia.Services
{
    public class SimilarityService
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger<SimilarityService> _logger;
        private readonly PluginConfiguration _config;
        private readonly GrpcChannel _grpcChannel;
        private readonly SimilarityClient _similarityClient;
        private readonly string _dbPath;

        public SimilarityService(ILibraryManager libraryManager, ILogger<SimilarityService> logger)
        {
            _libraryManager = libraryManager;
            _logger = logger;
            _config = Plugin.Instance!.Configuration;
            _dbPath = _config.DatabasePath;
            
            _grpcChannel = GrpcChannel.ForAddress($"http://{_config.AnalysisServiceUrl}");
            _similarityClient = new SimilarityClient(_grpcChannel);
        }

        public async Task<List<SimilarTrack>> GetSimilarTracks(Guid itemId, int count = 20)
        {
            try
            {
                // Get feature vector from database
                using var connection = new SqliteConnection($"Data Source={_dbPath}");
                var featureVector = await connection.QuerySingleOrDefaultAsync<byte[]>(
                    "SELECT feature_vector FROM analysis_results WHERE item_id = @ItemId",
                    new { ItemId = itemId.ToString() }
                );

                if (featureVector == null)
                {
                    _logger.LogWarning($"No analysis results found for item {itemId}");
                    return new List<SimilarTrack>();
                }

                // Query similarity service
                var request = new SimilarityRequest
                {
                    FeatureVector = Google.Protobuf.ByteString.CopyFrom(featureVector),
                    TopK = count
                };

                var response = await _similarityClient.FindSimilarAsync(request);
                
                // Map results to library items
                var similarTracks = new List<SimilarTrack>();
                foreach (var match in response.Matches)
                {
                    var item = _libraryManager.GetItemById(Guid.Parse(match.ItemId));
                    if (item != null)
                    {
                        similarTracks.Add(new SimilarTrack
                        {
                            Item = item,
                            Similarity = match.Similarity,
                            Distance = match.Distance
                        });
                    }
                }

                return similarTracks.OrderByDescending(t => t.Similarity).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error finding similar tracks for {itemId}");
                return new List<SimilarTrack>();
            }
        }

        public async Task BuildSimilarityIndex()
        {
            _logger.LogInformation("Building similarity index");
            
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            var results = await connection.QueryAsync<(string ItemId, byte[] FeatureVector)>(
                "SELECT item_id, feature_vector FROM analysis_results WHERE feature_vector IS NOT NULL"
            );

            var request = new BuildIndexRequest();
            foreach (var (itemId, featureVector) in results)
            {
                request.Items.Add(new IndexItem
                {
                    ItemId = itemId,
                    FeatureVector = Google.Protobuf.ByteString.CopyFrom(featureVector)
                });
            }

            await _similarityClient.BuildIndexAsync(request);
            _logger.LogInformation($"Similarity index built with {results.Count()} items");
        }
    }

    public class SimilarTrack
    {
        public BaseItem Item { get; set; } = null!;
        public float Similarity { get; set; }
        public float Distance { get; set; }
    }
}