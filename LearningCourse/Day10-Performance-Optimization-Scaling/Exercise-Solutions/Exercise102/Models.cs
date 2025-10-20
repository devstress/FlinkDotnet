using System.Text.Json.Serialization;

namespace Exercise102;

/// <summary>
/// Scaling event for horizontal scaling testing
/// </summary>
public class ScalingEvent
{
    [JsonPropertyName("event_id")]
    public long EventId { get; set; }
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
    
    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;
    
    [JsonPropertyName("payload_size")]
    public int PayloadSize { get; set; }
    
    [JsonPropertyName("partition_key")]
    public int PartitionKey { get; set; }
    
    [JsonPropertyName("scenario")]
    public string Scenario { get; set; } = string.Empty;
}

/// <summary>
/// Processed event with node information
/// </summary>
public class ProcessedScalingEvent
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
    
    [JsonPropertyName("node_id")]
    public int NodeId { get; set; }
    
    [JsonPropertyName("partition")]
    public int Partition { get; set; }
    
    [JsonPropertyName("scenario")]
    public string Scenario { get; set; } = string.Empty;
}

/// <summary>
/// Node-level performance metrics
/// </summary>
public class NodeMetrics
{
    public int NodeId { get; set; }
    public int NodeCount { get; set; }
    public long EventsProcessed { get; set; }
    public double ThroughputEventsPerSec { get; set; }
    public TimeSpan AverageLatency { get; set; }
    public List<int> AssignedPartitions { get; set; } = new();
    public double LoadPercentage { get; set; } // % of total load handled
}

/// <summary>
/// Scaling scenario metrics
/// </summary>
public class ScalingMetrics
{
    public string Scenario { get; set; } = string.Empty;
    public int NodeCount { get; set; }
    public int TotalPartitions { get; set; }
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
    
    // Load distribution metrics
    public List<NodeMetrics> NodeMetrics { get; set; } = new();
    public double LoadDistributionCoefficient { get; set; } // CV of node loads (lower = better distribution)
    public double PartitionsPerNode { get; set; }
    
    // Scaling efficiency
    public double SpeedupFactor { get; set; } // Actual speedup vs baseline
    public double ScalingEfficiency { get; set; } // Actual speedup / ideal speedup (%)
    public double LinearScalingDeviation { get; set; } // % deviation from linear scaling
}

/// <summary>
/// Load distribution analysis
/// </summary>
public class LoadDistributionAnalysis
{
    public int NodeCount { get; set; }
    public double AverageLoad { get; set; }
    public double MinLoad { get; set; }
    public double MaxLoad { get; set; }
    public double LoadVariance { get; set; }
    public double CoefficientOfVariation { get; set; } // Lower is better (0 = perfect distribution)
    public string DistributionQuality { get; set; } = string.Empty;
    public List<NodeMetrics> NodeMetrics { get; set; } = new();
}

/// <summary>
/// Scaling efficiency comparison
/// </summary>
public class ScalingComparison
{
    public string ScenarioName { get; set; } = string.Empty;
    public int NodeCount { get; set; }
    public double ThroughputEventsPerSec { get; set; }
    public double SpeedupVsBaseline { get; set; } // Actual speedup factor
    public double IdealSpeedup { get; set; } // Ideal linear speedup
    public double ScalingEfficiency { get; set; } // % of ideal speedup achieved
    public double LatencyMs { get; set; }
    public double LoadDistributionCV { get; set; } // Coefficient of variation
    public double PartitionsPerNode { get; set; }
    public string BottleneckIndicator { get; set; } = string.Empty;
}

/// <summary>
/// Horizontal scaling analysis results
/// </summary>
public class ScalingAnalysis
{
    public int OptimalNodeCount { get; set; }
    public double BestThroughput { get; set; }
    public double BestScalingEfficiency { get; set; }
    public List<ScalingComparison> AllComparisons { get; set; } = new();
    public List<ScalingRecommendation> Recommendations { get; set; } = new();
    public string ScalingPattern { get; set; } = string.Empty; // "Linear", "Sub-linear", "Diminishing Returns"
}

/// <summary>
/// Scaling optimization recommendation
/// </summary>
public class ScalingRecommendation
{
    public string Category { get; set; } = string.Empty;
    public string Issue { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public int Priority { get; set; } // 1=Critical, 2=High, 3=Medium, 4=Low
}