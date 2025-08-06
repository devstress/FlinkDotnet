using FlinkDotNet.ClusterManager.Models;

namespace FlinkDotNet.Temporal.Models;

/// <summary>
/// Result of distributing jobs across multiple clusters.
/// </summary>
public record JobDistributionResult
{
    public int TotalJobs { get; init; }
    public int SuccessfulPlacements { get; init; }
    public int FailedPlacements { get; init; }
    public List<JobPlacementResult> Placements { get; init; } = new();
    public TimeSpan TotalDistributionTime { get; init; }
    public Dictionary<string, int> ClusterDistribution { get; init; } = new();
}

/// <summary>
/// Result of placing a single job on a cluster.
/// </summary>
public record JobPlacementResult
{
    public string JobId { get; init; } = string.Empty;
    public string ClusterId { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime PlacementTime { get; init; }
    public TimeSpan PlacementDuration { get; init; }
}

/// <summary>
/// Configuration for auto-scaling behavior.
/// </summary>
public record AutoScalingConfig
{
    public int MinClusters { get; init; } = 1;
    public int MaxClusters { get; init; } = 100;
    public double ScaleUpThreshold { get; init; } = 80.0;  // CPU/Memory utilization %
    public double ScaleDownThreshold { get; init; } = 30.0;
    public TimeSpan EvaluationInterval { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan CooldownPeriod { get; init; } = TimeSpan.FromMinutes(10);
    public int ScaleUpIncrement { get; init; } = 1;
    public int ScaleDownIncrement { get; init; } = 1;
    public List<AutoScalingMetric> Metrics { get; init; } = new();
}

/// <summary>
/// Metrics used for auto-scaling decisions.
/// </summary>
public record AutoScalingMetric
{
    public string Name { get; init; } = string.Empty;
    public double Weight { get; init; } = 1.0;
    public double Threshold { get; init; }
    public AutoScalingMetricType Type { get; init; }
}

/// <summary>
/// Types of metrics for auto-scaling.
/// </summary>
public enum AutoScalingMetricType
{
    CpuUtilization,
    MemoryUtilization,
    JobQueueLength,
    Throughput,
    BackpressureRatio,
    Custom
}

/// <summary>
/// Information about a cluster failure.
/// </summary>
public record ClusterFailureInfo
{
    public string ClusterId { get; init; } = string.Empty;
    public ClusterFailureType FailureType { get; init; }
    public DateTime FailureTime { get; init; }
    public string Description { get; init; } = string.Empty;
    public Dictionary<string, object> FailureContext { get; init; } = new();
    public List<string> AffectedJobs { get; init; } = new();
    public FailureSeverity Severity { get; init; }
}

/// <summary>
/// Types of cluster failures.
/// </summary>
public enum ClusterFailureType
{
    Unknown,
    OutOfMemory,
    DiskFull,
    NetworkPartition,
    JobManagerFailure,
    TaskManagerFailure,
    CheckpointFailure,
    ConfigurationError,
    ResourceExhaustion
}

/// <summary>
/// Severity levels for failures.
/// </summary>
public enum FailureSeverity
{
    Low,        // Non-critical, can self-recover
    Medium,     // Requires intervention but not urgent
    High,       // Affects availability, needs immediate attention
    Critical    // Complete failure, requires emergency response
}

/// <summary>
/// Configuration for cluster health monitoring.
/// </summary>
public record HealthMonitoringConfig
{
    public TimeSpan CheckInterval { get; init; } = TimeSpan.FromMinutes(1);
    public TimeSpan HealthTimeout { get; init; } = TimeSpan.FromSeconds(30);
    public int MaxConsecutiveFailures { get; init; } = 3;
    public List<HealthCheckType> EnabledChecks { get; init; } = new();
    public Dictionary<string, object> CheckParameters { get; init; } = new();
}

/// <summary>
/// Types of health checks to perform on clusters.
/// </summary>
public enum HealthCheckType
{
    RestApiHealthCheck,
    JobManagerConnectivity,
    TaskManagerStatus,
    CheckpointStatus,
    BackpressureMonitoring,
    ResourceUtilization,
    JobStatus
}

/// <summary>
/// Request for cluster provisioning through Temporal workflow.
/// </summary>
public record ClusterProvisioningRequest
{
    public string RequestId { get; init; } = string.Empty;
    public ClusterConfiguration Configuration { get; init; } = new();
    public string Region { get; init; } = "default";
    public string Zone { get; init; } = "default";
    public Priority Priority { get; init; } = Priority.Normal;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(15);
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Result of cluster provisioning operation.
/// </summary>
public record ClusterProvisioningResult
{
    public string ClusterId { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime ProvisioningStartTime { get; init; }
    public DateTime? ProvisioningEndTime { get; init; }
    public TimeSpan? ProvisioningDuration { get; init; }
    public Dictionary<string, object> ProvisioningMetadata { get; init; } = new();
}

/// <summary>
/// Priority levels for operations.
/// </summary>
public enum Priority
{
    Low,
    Normal,
    High,
    Critical
}