using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities.Audio;
using Microsoft.Extensions.Logging;
using JellySentia.Services;
using JellySentia.Configuration;

namespace JellySentia.Tests
{
    public class AnalysisServiceTests
    {
        private readonly Mock<ILibraryManager> _mockLibraryManager;
        private readonly Mock<ILogger<AnalysisService>> _mockLogger;
        private readonly AnalysisService _analysisService;

        public AnalysisServiceTests()
        {
            _mockLibraryManager = new Mock<ILibraryManager>();
            _mockLogger = new Mock<ILogger<AnalysisService>>();
            
            // Mock plugin instance
            var mockConfig = new PluginConfiguration
            {
                AnalysisServiceUrl = "localhost:50051",
                DatabasePath = ":memory:",
                MaxConcurrentAnalysis = 2
            };
            
            var mockPlugin = new Mock<Plugin>();
            mockPlugin.Setup(p => p.Configuration).Returns(mockConfig);
            Plugin.Instance = mockPlugin.Object;
            
            _analysisService = new AnalysisService(_mockLibraryManager.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Execute_ShouldAnalyzeAudioItems()
        {
            // Arrange
            var audioItems = new[]
            {
                CreateMockAudioItem("1", "/path/to/track1.mp3"),
                CreateMockAudioItem("2", "/path/to/track2.mp3")
            };

            _mockLibraryManager
                .Setup(lm => lm.GetItemList(It.IsAny<InternalItemsQuery>()))
                .Returns(audioItems);

            var progress = new Progress<double>();
            var cancellationToken = new CancellationToken();

            // Act
            await _analysisService.Execute(cancellationToken, progress);

            // Assert
            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Found 2 audio items")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()
                ),
                Times.Once
            );
        }

        [Fact]
        public void GetDefaultTriggers_ShouldReturnDailyTrigger()
        {
            // Act
            var triggers = _analysisService.GetDefaultTriggers();

            // Assert
            Assert.Single(triggers);
            var trigger = triggers.First();
            Assert.Equal(TaskTriggerInfo.TriggerType.Daily, trigger.Type);
            Assert.Equal(TimeSpan.FromHours(2).Ticks, trigger.TimeOfDayTicks);
        }

        private Audio CreateMockAudioItem(string id, string path)
        {
            var mockAudio = new Mock<Audio>();
            mockAudio.Setup(a => a.Id).Returns(Guid.Parse($"00000000-0000-0000-0000-00000000000{id}"));
            mockAudio.Setup(a => a.Path).Returns(path);
            return mockAudio.Object;
        }
    }

    public class SimilarityServiceTests
    {
        private readonly Mock<ILibraryManager> _mockLibraryManager;
        private readonly Mock<ILogger<SimilarityService>> _mockLogger;
        private readonly SimilarityService _similarityService;

        public SimilarityServiceTests()
        {
            _mockLibraryManager = new Mock<ILibraryManager>();
            _mockLogger = new Mock<ILogger<SimilarityService>>();
            
            // Mock plugin instance
            var mockConfig = new PluginConfiguration
            {
                AnalysisServiceUrl = "localhost:50051",
                DatabasePath = ":memory:"
            };
            
            var mockPlugin = new Mock<Plugin>();
            mockPlugin.Setup(p => p.Configuration).Returns(mockConfig);
            Plugin.Instance = mockPlugin.Object;
            
            _similarityService = new SimilarityService(_mockLibraryManager.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetSimilarTracks_WithNoAnalysis_ShouldReturnEmpty()
        {
            // Arrange
            var itemId = Guid.NewGuid();

            // Act
            var results = await _similarityService.GetSimilarTracks(itemId, 10);

            // Assert
            Assert.Empty(results);
        }
    }

    public class PlaylistServiceTests
    {
        private readonly Mock<ILibraryManager> _mockLibraryManager;
        private readonly Mock<IPlaylistManager> _mockPlaylistManager;
        private readonly Mock<ILogger<PlaylistService>> _mockLogger;
        private readonly PlaylistService _playlistService;

        public PlaylistServiceTests()
        {
            _mockLibraryManager = new Mock<ILibraryManager>();
            _mockPlaylistManager = new Mock<IPlaylistManager>();
            _mockLogger = new Mock<ILogger<PlaylistService>>();
            
            // Mock plugin instance
            var mockConfig = new PluginConfiguration
            {
                DatabasePath = ":memory:"
            };
            
            var mockPlugin = new Mock<Plugin>();
            mockPlugin.Setup(p => p.Configuration).Returns(mockConfig);
            Plugin.Instance = mockPlugin.Object;
            
            _playlistService = new PlaylistService(
                _mockLibraryManager.Object,
                _mockPlaylistManager.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task CreateSmartPlaylist_WithNoMatchingTracks_ShouldThrow()
        {
            // Arrange
            var request = new SmartPlaylistRequest
            {
                Name = "Test Playlist",
                MinTempo = 200,
                MaxTempo = 250  // Unrealistic tempo range
            };
            var userId = Guid.NewGuid();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _playlistService.CreateSmartPlaylist(request, userId)
            );
        }
    }
}