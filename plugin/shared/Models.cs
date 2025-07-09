using System;
using System.Collections.Generic;

namespace JellySentia.Models
{
    public class AnalysisResult
    {
        public Guid ItemId { get; set; }
        public string FilePath { get; set; } = "";
        public DateTime AnalyzedAt { get; set; }
        public Dictionary<string, float> Features { get; set; } = new();
        public byte[] FeatureVector { get; set; } = Array.Empty<byte>();
    }

    public class AnalysisProgress
    {
        public bool IsRunning { get; set; }
        public double ProgressPercent { get; set; }
        public int TotalItems { get; set; }
        public int AnalyzedItems { get; set; }
        public int ErrorCount { get; set; }
        public DateTime? StartedAt { get; set; }
        public TimeSpan? EstimatedTimeRemaining { get; set; }
    }

    public class SimilarityMatch
    {
        public Guid ItemId { get; set; }
        public float Similarity { get; set; }
        public float Distance { get; set; }
        public Dictionary<string, float> Features { get; set; } = new();
    }
}