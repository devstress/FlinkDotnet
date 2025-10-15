using System.Text.Json.Serialization;

namespace Exercise101;

/// <summary>
/// Performance event for resource optimization testing
/// </summary>
public class PerformanceEvent
{
    [JsonPropertyName("event_id")]
    public long EventId { get; set; }
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
    
    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;
    
    [JsonPropertyName("payload_size")]
    public int PayloadSize { get; set; }
    
    [JsonPropertyName("scenario")]
    public string Scenario { get; set; } = string.Empty;
}

/// <summary>
/// Processed event with performance metrics
/// </summary>
public class ProcessedEvent
{
    [JsonPropertyName("event_id")]
    public long EventId { get; set; }
    
    [JsonPropertyName("original_timestamp")]
    public DateTime OriginalTimestamp { get; set; }
    
    [JsonPropertyName("processed_timestamp")]
    public DateTime ProcessedTimestamp { get; set; }
    
    [JsonPropertyName("processing_time_ms")]
    public long ProcessingTimeMs { get; set; }
    
    [JsonPropertyName("processed_data")]
    public string ProcessedData { get; set; } = string.Empty;
    
    [JsonPropertyName("parallelism")]
    public int Parallelism { get; set; }
    
    [JsonPropertyName("scenario")]
    public string Scenario { get; set; } = string.Empty;
}

/// <summary>
/// Performance metrics for a scenario
/// </summary>
public class PerformanceMetrics
{
    public string Scenario { get; set; } = string.Empty;
    public int Parallelism { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    
    // Throughput metrics
    public long EventsGenerated { get; set; }
    public long EventsProcessed { get; set; }
    public double ThroughputEventsPerSec { get; set; }
    public double SuccessRate { get; set; }
    
    // Latency metrics
    public TimeSpan AverageLatency { get; set; }
    public TimeSpan MinLatency { get; set; }
    public TimeSpan MaxLatency { get; set; }
    public TimeSpan P95Latency { get; set; }
    public TimeSpan P99Latency { get; set; }
    
    // Resource usage metrics
    public long PeakMemoryMB { get; set; }
    public long AverageMemoryMB { get; set; }
    public double PeakCPUPercent { get; set; }
    public double AverageCPUPercent { get; set; }
    public int ThreadCount { get; set; }
    public int TotalGCCollections { get; set; }
    public int Gen0Collections { get; set; }
    public int Gen1Collections { get; set; }
    public int Gen2Collections { get; set; }
}

/// <summary>
/// Resource snapshot for monitoring
/// </summary>
public class ResourceSnapshot
{
    public DateTime Timestamp { get; set; }
    public long MemoryUsedMB { get; set; }
    public double CPUPercent { get; set; }
    public int ThreadCount { get; set; }
    public int Gen0Collections { get; set; }
    public int Gen1Collections { get; set; }
    public int Gen2Collections { get; set; }
    public string Scenario { get; set; } = string.Empty;
}

/// <summary>
/// Optimization recommendation
/// </summary>
public class OptimizationRecommendation
{
    public string Category { get; set; } = string.Empty;
    public string Issue { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public int Priority { get; set; } // 1=Critical, 2=High, 3=Medium, 4=Low
}

/// <summary>
/// Optimization analysis results
/// </summary>
public class OptimizationAnalysis
{
    public int OptimalParallelism { get; set; }
    public double ThroughputImprovement { get; set; }
    public double ResourceEfficiency { get; set; }
    public List<OptimizationRecommendation> Recommendations { get; set; } = new();
    public PerformanceComparison BestScenario { get; set; } = new();
    public List<PerformanceComparison> AllComparisons { get; set; } = new();
}

/// <summary>
/// Performance comparison between scenarios
/// </summary>
public class PerformanceComparison
{
    public string ScenarioName { get; set; } = string.Empty;
    public int Parallelism { get; set; }
    public double ThroughputEventsPerSec { get; set; }
    public double ThroughputGain { get; set; } // Compared to baseline
    public double LatencyMs { get; set; }
    public double LatencyChange { get; set; } // Compared to baseline
    public long MemoryUsedMB { get; set; }
    public double MemoryChange { get; set; } // Compared to baseline
    public double CPUPercent { get; set; }
    public double CPUChange { get; set; } // Compared to baseline
    public double EfficiencyScore { get; set; } // Throughput per resource unit
}