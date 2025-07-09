using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Playlists;
using Microsoft.Extensions.Logging;
using JellySentia.Configuration;
using Dapper;
using Microsoft.Data.Sqlite;

namespace JellySentia.Services
{
    public class PlaylistService
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IPlaylistManager _playlistManager;
        private readonly ILogger<PlaylistService> _logger;
        private readonly PluginConfiguration _config;
        private readonly string _dbPath;

        public PlaylistService(
            ILibraryManager libraryManager,
            IPlaylistManager playlistManager,
            ILogger<PlaylistService> logger)
        {
            _libraryManager = libraryManager;
            _playlistManager = playlistManager;
            _logger = logger;
            _config = Plugin.Instance!.Configuration;
            _dbPath = _config.DatabasePath;
        }

        public async Task<Playlist> CreateSmartPlaylist(SmartPlaylistRequest request, Guid userId)
        {
            var tracks = await QueryTracks(request);
            
            if (!tracks.Any())
            {
                throw new InvalidOperationException("No tracks match the specified criteria");
            }

            var trackIds = tracks.Select(t => t.ItemId).ToArray();
            
            var playlistCreationRequest = new PlaylistCreationRequest
            {
                Name = request.Name,
                ItemIdList = trackIds,
                UserId = userId,
                MediaType = "Audio"
            };

            var result = await _playlistManager.CreatePlaylist(playlistCreationRequest);
            
            _logger.LogInformation($"Created smart playlist '{request.Name}' with {tracks.Count} tracks");
            
            return result;
        }

        private async Task<List<SmartPlaylistTrack>> QueryTracks(SmartPlaylistRequest request)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            
            var query = "SELECT item_id, tempo, energy, valence, danceability, acousticness FROM analysis_results WHERE 1=1";
            var parameters = new DynamicParameters();

            // Build dynamic query based on criteria
            if (request.MinTempo.HasValue)
            {
                query += " AND tempo >= @MinTempo";
                parameters.Add("MinTempo", request.MinTempo.Value);
            }
            
            if (request.MaxTempo.HasValue)
            {
                query += " AND tempo <= @MaxTempo";
                parameters.Add("MaxTempo", request.MaxTempo.Value);
            }
            
            if (request.MinEnergy.HasValue)
            {
                query += " AND energy >= @MinEnergy";
                parameters.Add("MinEnergy", request.MinEnergy.Value);
            }
            
            if (request.MaxEnergy.HasValue)
            {
                query += " AND energy <= @MaxEnergy";
                parameters.Add("MaxEnergy", request.MaxEnergy.Value);
            }
            
            if (request.MinValence.HasValue)
            {
                query += " AND valence >= @MinValence";
                parameters.Add("MinValence", request.MinValence.Value);
            }
            
            if (request.MaxValence.HasValue)
            {
                query += " AND valence <= @MaxValence";
                parameters.Add("MaxValence", request.MaxValence.Value);
            }
            
            if (!string.IsNullOrEmpty(request.Key))
            {
                query += " AND key = @Key";
                parameters.Add("Key", request.Key);
            }
            
            if (!string.IsNullOrEmpty(request.Scale))
            {
                query += " AND scale = @Scale";
                parameters.Add("Scale", request.Scale);
            }

            // Add sorting
            query += request.SortBy switch
            {
                "tempo" => " ORDER BY tempo",
                "energy" => " ORDER BY energy DESC",
                "valence" => " ORDER BY valence DESC",
                "danceability" => " ORDER BY danceability DESC",
                _ => " ORDER BY RANDOM()"
            };

            if (request.Limit > 0)
            {
                query += " LIMIT @Limit";
                parameters.Add("Limit", request.Limit);
            }

            var results = await connection.QueryAsync<SmartPlaylistTrack>(query, parameters);
            return results.ToList();
        }
    }

    public class SmartPlaylistRequest
    {
        public string Name { get; set; } = "Smart Playlist";
        public float? MinTempo { get; set; }
        public float? MaxTempo { get; set; }
        public float? MinEnergy { get; set; }
        public float? MaxEnergy { get; set; }
        public float? MinValence { get; set; }
        public float? MaxValence { get; set; }
        public float? MinDanceability { get; set; }
        public float? MaxDanceability { get; set; }
        public string? Key { get; set; }
        public string? Scale { get; set; }
        public string SortBy { get; set; } = "random";
        public int Limit { get; set; } = 100;
    }

    public class SmartPlaylistTrack
    {
        public string ItemId { get; set; } = "";
        public float Tempo { get; set; }
        public float Energy { get; set; }
        public float Valence { get; set; }
        public float Danceability { get; set; }
        public float Acousticness { get; set; }
    }
}