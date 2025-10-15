namespace FlinkDotNet.ClusterManager.Models;

/// <summary>
/// Represents the current status and health of a Flink cluster.
/// </summary>
public record ClusterStatus
{
    public string ClusterId { get; init; } = string.Empty;
    public ClusterHealthState Health
    {
        get; init;
    }
    public int AvailableSlots
    {
        get; init;
    }
    public int TotalSlots
    {
        get; init;
    }
    public int RunningJobs
    {
        get; init;
    }
    public DateTime LastHealthCheck
    {
        get; init;
    }
    public string Version { get; init; } = string.Empty;
    public Dictionary<string, object> AdditionalMetrics { get; init; } = new();
}

/// <summary>
/// Represents performance metrics for a Flink cluster.
/// </summary>
public record ClusterMetrics
{
    public string ClusterId { get; init; } = string.Empty;
    public double CpuUtilization
    {
        get; init;
    }
    public double MemoryUtilization
    {
        get; init;
    }
    public long ProcessedRecords
    {
        get; init;
    }
    public double Throughput
    {
        get; init;
    }
    public double BackpressureRatio
    {
        get; init;
    }
    public DateTime Timestamp
    {
        get; init;
    }
    public Dictionary<string, double> CustomMetrics { get; init; } = new();
}

/// <summary>
/// Defines the health states of a Flink cluster.
/// </summary>
public enum ClusterHealthState
{
    Unknown,
    Healthy,
    Warning,
    Critical,
    Offline
}

/// <summary>
/// Represents a Flink job definition for submission.
/// </summary>
public record FlinkJobDefinition
{
    public string JobId { get; init; } = string.Empty;
    public string JobName { get; init; } = string.Empty;
    public string JobGraph { get; init; } = string.Empty;
    public int Parallelism { get; init; } = 1;
    public Dictionary<string, string> Configuration { get; init; } = new();
    public JobPriority Priority { get; init; } = JobPriority.Normal;
    public TimeSpan? Timeout
    {
        get; init;
    }
    public List<string> RequiredResources { get; init; } = new();
    public JobResourceRequirements ResourceRequirements { get; init; } = new();
}

/// <summary>
/// Represents the result of submitting a job to a cluster.
/// </summary>
public record JobSubmissionResult
{
    public string JobId { get; init; } = string.Empty;
    public string ClusterId { get; init; } = string.Empty;
    public bool Success
    {
        get; init;
    }
    public string? ErrorMessage
    {
        get; init;
    }
    public DateTime SubmissionTime
    {
        get; init;
    }
    public string? FlinkJobId
    {
        get; init;
    }
    public JobPlacementInfo PlacementInfo { get; init; } = new();
}

/// <summary>
/// Defines job priority levels for scheduling.
/// </summary>
public enum JobPriority
{
    Low,
    Normal,
    High,
    Critical
}

/// <summary>
/// Resource requirements for a job.
/// </summary>
public record JobResourceRequirements
{
    public int MinSlots { get; init; } = 1;
    public int MaxSlots { get; init; } = int.MaxValue;
    public long MemoryMB { get; init; } = 1024;
    public double CpuCores { get; init; } = 1.0;
    public Dictionary<string, object> AdditionalRequirements { get; init; } = new();
}

/// <summary>
/// Information about where a job was placed.
/// </summary>
public record JobPlacementInfo
{
    public string ClusterId { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public int AssignedSlots
    {
        get; init;
    }
    public SubmissionStrategy Strategy { get; init; } = SubmissionStrategy.BestFit;
    public Dictionary<string, object> PlacementMetadata { get; init; } = new();
}

/// <summary>
/// Strategies for job submission and cluster selection.
/// </summary>
public enum SubmissionStrategy
{
    BestFit,        // Place on cluster with best resource match
    LeastLoaded,    // Place on cluster with lowest utilization
    RoundRobin,     // Distribute jobs evenly across clusters
    LocalityFirst,  // Prefer clusters in same region/zone
    HighAvailability // Prefer clusters with HA configuration
}

/// <summary>
/// Configuration for creating a new Flink cluster.
/// </summary>
public record ClusterConfiguration
{
    public string Name { get; init; } = string.Empty;
    public int TaskSlots { get; init; } = 4;
    public int TaskManagers { get; init; } = 2;
    public string FlinkVersion { get; init; } = "1.18.0";
    public Dictionary<string, string> Properties { get; init; } = new();
    public ResourceLimits ResourceLimits { get; init; } = new();
    public string Region { get; init; } = "default";
    public string Zone { get; init; } = "default";
    public bool HighAvailability { get; init; } = true;
    
    /// <summary>
    /// Base retry delay in milliseconds for HTTP operations (default: 1000ms for production, can be set to 0 for tests)
    /// </summary>
    public int RetryBaseDelayMs { get; init; } = 1000;
}

/// <summary>
/// Resource limits for a cluster.
/// </summary>
public record ResourceLimits
{
    public long MaxMemoryMB { get; init; } = 8192;
    public double MaxCpuCores { get; init; } = 4.0;
    public long MaxDiskGB { get; init; } = 100;
    public int MaxJobs { get; init; } = 50;
}
