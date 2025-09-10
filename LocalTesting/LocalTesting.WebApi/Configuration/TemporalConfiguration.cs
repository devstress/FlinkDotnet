using System.ComponentModel.DataAnnotations;
using LocalTesting.Shared.Constants;

namespace LocalTesting.WebApi.Configuration;

/// <summary>
/// Configuration settings for Temporal server and agent optimization
/// </summary>
public class TemporalConfiguration
{
    public const string SectionName = "Temporal";

    /// <summary>
    /// Temporal server URL for client connections
    /// </summary>
    [Required]
    public string ServerUrl { get; set; } = PortConstants.TemporalServerUrl();

    /// <summary>
    /// Temporal namespace for workflow execution
    /// </summary>
    public string Namespace { get; set; } = "default";

    /// <summary>
    /// Agent optimization settings for dynamic performance tuning
    /// </summary>
    public TemporalAgentOptimizationConfiguration AgentOptimization { get; set; } = new();
}

/// <summary>
/// Configuration for Temporal agent optimization and dynamic scaling
/// </summary>
public class TemporalAgentOptimizationConfiguration
{
    /// <summary>
    /// Maximum concurrent activities per worker
    /// </summary>
    [Range(1, 10000)]
    public int MaxConcurrentActivities { get; set; } = 100;

    /// <summary>
    /// Maximum concurrent workflow tasks per worker
    /// </summary>
    [Range(1, 10000)]
    public int MaxConcurrentWorkflowTasks { get; set; } = 100;

    /// <summary>
    /// Maximum concurrent local activities per worker
    /// </summary>
    [Range(1, 10000)]
    public int MaxConcurrentLocalActivities { get; set; } = 100;

    /// <summary>
    /// Number of worker instances to create
    /// </summary>
    [Range(1, 100)]
    public int WorkerCount { get; set; } = 10;

    /// <summary>
    /// Timeout for activity tasks
    /// </summary>
    public TimeSpan ActivityTaskTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Timeout for workflow tasks
    /// </summary>
    public TimeSpan WorkflowTaskTimeout { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Heartbeat timeout for activities
    /// </summary>
    public TimeSpan HeartbeatTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Schedule to close timeout for workflows
    /// </summary>
    public TimeSpan ScheduleToCloseTimeout { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Schedule to start timeout for activities
    /// </summary>
    public TimeSpan ScheduleToStartTimeout { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Start to close timeout for activities
    /// </summary>
    public TimeSpan StartToCloseTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Enable dynamic optimization based on performance metrics
    /// </summary>
    public bool EnableDynamicOptimization { get; set; } = true;

    /// <summary>
    /// Interval between optimization adjustments
    /// </summary>
    public TimeSpan OptimizationInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Performance thresholds for optimization decisions
    /// </summary>
    public TemporalPerformanceThresholds PerformanceThresholds { get; set; } = new();
}

/// <summary>
/// Performance thresholds that trigger Temporal agent optimization
/// </summary>
public class TemporalPerformanceThresholds
{
    /// <summary>
    /// Maximum acceptable latency in milliseconds before scaling up
    /// </summary>
    [Range(100, 60000)]
    public int MaxLatencyMs { get; set; } = 5000;

    /// <summary>
    /// Minimum throughput per second before scaling up
    /// </summary>
    [Range(1, 100000)]
    public int MinThroughputPerSecond { get; set; } = 100;

    /// <summary>
    /// Maximum error rate (0.0 to 1.0) before optimization
    /// </summary>
    [Range(0.0, 1.0)]
    public double MaxErrorRate { get; set; } = 0.05;

    /// <summary>
    /// Maximum queue depth before scaling up workers
    /// </summary>
    [Range(1, 100000)]
    public int MaxQueueDepth { get; set; } = 1000;
}