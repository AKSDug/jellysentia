using MediaBrowser.Model.Plugins;

namespace JellySentia.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string AnalysisServiceUrl { get; set; } = "localhost:50051";
        public string DatabasePath { get; set; } = "/config/plugins/JellySentia/jellysentia.db";
        public AnalysisDepth AnalysisDepth { get; set; } = AnalysisDepth.Standard;
        public bool EnableTagWriteback { get; set; } = true;
        public bool EnableSimilarityIndex { get; set; } = true;
        public int MaxConcurrentAnalysis { get; set; } = 4;
        public int SimilarityIndexSize { get; set; } = 100;
        public bool EnableWebUI { get; set; } = true;
        public int WebUIPort { get; set; } = 8097;
        public string[] EnabledDescriptors { get; set; } = new[]
        {
            "tempo", "key", "scale", "energy", "danceability", "valence",
            "acousticness", "instrumentalness", "speechiness", "loudness"
        };
    }

    public enum AnalysisDepth
    {
        Lightweight,
        Standard,
        Comprehensive
    }
}