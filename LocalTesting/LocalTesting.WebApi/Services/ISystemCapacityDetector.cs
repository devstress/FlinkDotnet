using LocalTesting.WebApi.Models;

namespace LocalTesting.WebApi.Services;

/// <summary>
/// Service for detecting actual system capacity to replace hardcoded test parameters
/// with adaptive scaling based on real infrastructure metrics.
/// 
/// This service implements Phase 3 from WI15 architecture design:
/// - Dynamic capacity detection using real infrastructure metrics
/// - Adaptive parameter calculation based on system performance
/// - Real-time capacity monitoring for optimization decisions
/// </summary>
public interface ISystemCapacityDetector
{
    /// <summary>
    /// Detect Kafka cluster capacity including broker count, partition distribution, and throughput limits
    /// </summary>
    Task<KafkaCapacity> DetectKafkaCapacityAsync();
    
    /// <summary>
    /// Detect Flink cluster capacity including available task slots, JobManager status, and parallelism limits
    /// </summary>
    Task<FlinkCapacity> DetectFlinkCapacityAsync();
    
    /// <summary>
    /// Detect Temporal server capacity including worker capacity, workflow limits, and agent configuration
    /// </summary>
    Task<TemporalCapacity> DetectTemporalCapacityAsync();
    
    /// <summary>
    /// Calculate optimal test parameters based on detected system capacity and performance targets
    /// </summary>
    /// <param name="target">Performance target to optimize for</param>
    /// <returns>Adaptive parameters optimized for actual system capacity</returns>
    Task<AdaptiveParameters> CalculateOptimalParametersAsync(PerformanceTarget target);
    
    /// <summary>
    /// Get comprehensive system capacity summary including all infrastructure components
    /// </summary>
    Task<SystemCapacitySummary> GetSystemCapacitySummaryAsync();
    
    /// <summary>
    /// Validate that detected capacity meets minimum requirements for test execution
    /// </summary>
    /// <param name="minimumRequirements">Minimum capacity requirements</param>
    /// <returns>Validation result with details about capacity adequacy</returns>
    Task<CapacityValidationResult> ValidateCapacityRequirementsAsync(MinimumCapacityRequirements minimumRequirements);
}

/// <summary>
/// Kafka cluster capacity information
/// </summary>
public class KafkaCapacity
{
    public int BrokerCount { get; set; }
    public int TotalPartitions { get; set; }
    public int ReplicationFactor { get; set; }
    public double EstimatedMaxThroughputMessagesPerSecond { get; set; }
    public double EstimatedMaxThroughputBytesPerSecond { get; set; }
    public int RecommendedBatchSize { get; set; }
    public TimeSpan RecommendedTestDuration { get; set; }
    public Dictionary<string, object> BrokerMetrics { get; set; } = new();
    public string CapacitySource { get; set; } = "Kafka Admin API + Prometheus";
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Flink cluster capacity information
/// </summary>
public class FlinkCapacity
{
    public int JobManagerCount { get; set; }
    public int TaskManagerCount { get; set; }
    public int TotalTaskSlots { get; set; }
    public int AvailableTaskSlots { get; set; }
    public int MaxParallelism { get; set; }
    public int RecommendedJobCount { get; set; }
    public double EstimatedProcessingRateMessagesPerSecond { get; set; }
    public Dictionary<string, object> JobManagerMetrics { get; set; } = new();
    public Dictionary<string, object> TaskManagerMetrics { get; set; } = new();
    public string CapacitySource { get; set; } = "Flink REST API + Prometheus";
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Temporal server capacity information
/// </summary>
public class TemporalCapacity
{
    public int ServerCount { get; set; }
    public int CurrentWorkerCount { get; set; }
    public int MaxRecommendedWorkerCount { get; set; }
    public int CurrentConcurrentActivities { get; set; }
    public int MaxConcurrentActivities { get; set; }
    public int CurrentConcurrentWorkflows { get; set; }
    public int MaxConcurrentWorkflows { get; set; }
    public double EstimatedWorkflowExecutionRatePerSecond { get; set; }
    public AgentConfiguration CurrentAgentConfiguration { get; set; } = new();
    public AgentConfiguration RecommendedAgentConfiguration { get; set; } = new();
    public Dictionary<string, object> ServerMetrics { get; set; } = new();
    public string CapacitySource { get; set; } = "Temporal gRPC API + Prometheus";
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Temporal agent configuration for optimization
/// </summary>
public partial class AgentConfiguration
{
    public int MaxConcurrentActivities { get; set; } = 20;
    public int MaxConcurrentWorkflowTasks { get; set; } = 10;
    public int MaxConcurrentActivityTasks { get; set; } = 20;
    public int WorkerAgentCount { get; set; } = 1;
    public int ActivityAgentCount { get; set; } = 1;
    public int WorkflowAgentCount { get; set; } = 1;
    public TimeSpan TaskQueuePollInterval { get; set; } = TimeSpan.FromMilliseconds(100);
    public TimeSpan TaskExecutionTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(5);
}

/// <summary>
/// Adaptive test parameters calculated from real system capacity
/// </summary>
public class AdaptiveParameters
{
    public int OptimalKafkaMessages { get; set; }
    public int OptimalFlinkJobs { get; set; }
    public int OptimalTemporalWorkflows { get; set; }
    public int OptimalBatchSize { get; set; }
    public TimeSpan ExpectedExecutionTime { get; set; }
    public double EstimatedThroughputMessagesPerSecond { get; set; }
    public string CapacitySource { get; set; } = "Dynamic Capacity Detection";
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> CalculationDetails { get; set; } = new();
    public AdaptiveParameterJustification Justification { get; set; } = new();
}

/// <summary>
/// Justification for adaptive parameter calculations
/// </summary>
public class AdaptiveParameterJustification
{
    public string KafkaMessageCountReason { get; set; } = string.Empty;
    public string FlinkJobCountReason { get; set; } = string.Empty;
    public string TemporalWorkflowCountReason { get; set; } = string.Empty;
    public string ExecutionTimeReason { get; set; } = string.Empty;
    public List<string> CapacityLimitations { get; set; } = new();
    public List<string> OptimizationOpportunities { get; set; } = new();
}

/// <summary>
/// Performance target for parameter optimization
/// </summary>
public class PerformanceTarget
{
    public PerformanceGoal PrimaryGoal { get; set; } = PerformanceGoal.Balanced;
    public int? TargetMessageCount { get; set; }
    public TimeSpan? MaxExecutionTime { get; set; }
    public double? TargetThroughputMessagesPerSecond { get; set; }
    public double ResourceUtilizationLimit { get; set; } = 0.8; // 80% max utilization
    public bool PreferStability { get; set; } = true;
    public bool AllowAggressiveOptimization { get; set; } = false;
}

/// <summary>
/// Performance optimization goals
/// </summary>
public enum PerformanceGoal
{
    MaxThroughput,    // Optimize for highest message throughput
    MinLatency,       // Optimize for lowest processing latency
    Balanced,         // Balance throughput and stability
    StabilityFirst,   // Prioritize system stability over performance
    CustomTarget      // Use custom target parameters
}

/// <summary>
/// Comprehensive system capacity summary
/// </summary>
public class SystemCapacitySummary
{
    public KafkaCapacity KafkaCapacity { get; set; } = new();
    public FlinkCapacity FlinkCapacity { get; set; } = new();
    public TemporalCapacity TemporalCapacity { get; set; } = new();
    public AdaptiveParameters RecommendedParameters { get; set; } = new();
    public SystemBottleneck PrimaryBottleneck { get; set; }
    public List<CapacityRecommendation> Recommendations { get; set; } = new();
    public bool IsCapacityAdequate { get; set; }
    public string OverallAssessment { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// System bottleneck identification
/// </summary>
public enum SystemBottleneck
{
    None,
    KafkaBrokers,
    KafkaPartitions,
    FlinkTaskSlots,
    FlinkJobManagers,
    TemporalWorkers,
    TemporalDatabase,
    NetworkBandwidth,
    Unknown
}

/// <summary>
/// Capacity optimization recommendations
/// </summary>
public class CapacityRecommendation
{
    public string Component { get; set; } = string.Empty;
    public string Issue { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public CapacityRecommendationPriority Priority { get; set; }
    public bool CanBeOptimizedInCurrentImplementation { get; set; }
}

/// <summary>
/// Priority levels for capacity recommendations
/// </summary>
public enum CapacityRecommendationPriority
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Minimum capacity requirements for test execution
/// </summary>
public class MinimumCapacityRequirements
{
    public int MinKafkaBrokers { get; set; } = 1;
    public int MinFlinkTaskSlots { get; set; } = 1;
    public int MinTemporalWorkers { get; set; } = 1;
    public double MinThroughputMessagesPerSecond { get; set; } = 100;
    public TimeSpan MaxAcceptableExecutionTime { get; set; } = TimeSpan.FromMinutes(10);
}

/// <summary>
/// Capacity validation result
/// </summary>
public class CapacityValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> ValidationErrors { get; set; } = new();
    public List<string> ValidationWarnings { get; set; } = new();
    public MinimumCapacityRequirements RequiredCapacity { get; set; } = new();
    public SystemCapacitySummary DetectedCapacity { get; set; } = new();
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
}