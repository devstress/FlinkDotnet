using LocalTesting.WebApi.Models;

namespace LocalTesting.WebApi.Services;

/// <summary>
/// Service for optimizing Temporal agent configuration for improved performance.
/// 
/// This service implements Phase 4 from WI15 architecture design:
/// - Agent-only scaling strategy without modifying infrastructure topology
/// - Dynamic agent scaling based on observability data
/// - Real-time performance monitoring and optimization
/// 
/// CONSTRAINT: Can only increase number of Temporal agents and optimize configuration.
/// Cannot modify topics/partitions/JobManagers or infrastructure components.
/// </summary>
public interface ITemporalAgentOptimizer
{
    /// <summary>
    /// Calculate optimal agent configuration based on current workload metrics
    /// </summary>
    /// <param name="workload">Current workload metrics for optimization</param>
    /// <returns>Optimized agent configuration for the workload</returns>
    Task<AgentConfiguration> CalculateOptimalAgentConfigAsync(WorkloadMetrics workload);
    
    /// <summary>
    /// Scale Temporal agents to target count with specified configuration
    /// </summary>
    /// <param name="targetAgentCount">Target number of agents to scale to</param>
    /// <param name="config">Agent configuration to apply</param>
    /// <returns>Result of the scaling operation</returns>
    Task<ScalingResult> ScaleAgentsAsync(int targetAgentCount, AgentConfiguration config);
    
    /// <summary>
    /// Monitor current agent performance metrics for optimization decisions
    /// </summary>
    /// <returns>Current agent performance metrics</returns>
    Task<PerformanceMetrics> MonitorAgentPerformanceAsync();
    
    /// <summary>
    /// Optimize agent configuration for specific workload pattern
    /// </summary>
    /// <param name="pattern">Workload pattern to optimize for</param>
    /// <returns>Complete optimization result with before/after metrics</returns>
    Task<OptimizationResult> OptimizeForWorkloadAsync(WorkloadPattern pattern);
    
    /// <summary>
    /// Get current agent configuration and status
    /// </summary>
    /// <returns>Current agent configuration details</returns>
    Task<AgentStatus> GetCurrentAgentStatusAsync();
    
    /// <summary>
    /// Apply incremental optimization based on real-time metrics
    /// </summary>
    /// <param name="targetImprovement">Target performance improvement percentage</param>
    /// <returns>Incremental optimization result</returns>
    Task<IncrementalOptimizationResult> ApplyIncrementalOptimizationAsync(double targetImprovement = 0.2);
    
    /// <summary>
    /// Validate agent optimization is working within infrastructure constraints
    /// </summary>
    /// <returns>Validation result with constraint compliance details</returns>
    Task<AgentOptimizationValidationResult> ValidateOptimizationConstraintsAsync();
}

/// <summary>
/// Workload metrics for agent optimization decisions
/// </summary>
public class WorkloadMetrics
{
    public double CurrentWorkflowExecutionRate { get; set; }
    public double CurrentActivityExecutionRate { get; set; }
    public int ActiveWorkflowCount { get; set; }
    public int QueuedWorkflowCount { get; set; }
    public int ActiveActivityCount { get; set; }
    public int QueuedActivityCount { get; set; }
    public double AverageWorkflowExecutionTimeSeconds { get; set; }
    public double AverageActivityExecutionTimeSeconds { get; set; }
    public double AgentUtilizationPercentage { get; set; }
    public double TaskQueueDepth { get; set; }
    public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
}

/// <summary>
/// Result of agent scaling operation
/// </summary>
public class ScalingResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int PreviousAgentCount { get; set; }
    public int NewAgentCount { get; set; }
    public AgentConfiguration PreviousConfiguration { get; set; } = new();
    public AgentConfiguration NewConfiguration { get; set; } = new();
    public TimeSpan ScalingDuration { get; set; }
    public List<string> ScalingSteps { get; set; } = new();
    public Dictionary<string, object> ScalingMetrics { get; set; } = new();
    public DateTime ScaledAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Performance metrics for agent optimization
/// </summary>
public class PerformanceMetrics
{
    public double WorkflowThroughputPerSecond { get; set; }
    public double ActivityThroughputPerSecond { get; set; }
    public double AverageLatencyMilliseconds { get; set; }
    public double P95LatencyMilliseconds { get; set; }
    public double P99LatencyMilliseconds { get; set; }
    public double AgentCpuUtilizationPercentage { get; set; }
    public double AgentMemoryUtilizationPercentage { get; set; }
    public double TaskQueueUtilizationPercentage { get; set; }
    public int ErrorCount { get; set; }
    public double ErrorRatePercentage { get; set; }
    public int RetryCount { get; set; }
    public double RetryRatePercentage { get; set; }
    public Dictionary<string, double> DetailedMetrics { get; set; } = new();
    public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Workload patterns for optimization strategies
/// </summary>
public enum WorkloadPattern
{
    /// <summary>High volume, steady state processing</summary>
    HighVolumeStreaming,
    
    /// <summary>Burst processing with intermittent high load</summary>
    BurstProcessing,
    
    /// <summary>Low latency, real-time processing</summary>
    LowLatencyRealTime,
    
    /// <summary>Batch processing with predictable patterns</summary>
    BatchProcessing,
    
    /// <summary>Mixed workload with varying patterns</summary>
    MixedWorkload,
    
    /// <summary>Testing workload for observability validation</summary>
    TestingWorkload
}

/// <summary>
/// Complete optimization result with before/after comparison
/// </summary>
public class OptimizationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public WorkloadPattern OptimizedPattern { get; set; }
    public PerformanceMetrics BeforeOptimization { get; set; } = new();
    public PerformanceMetrics AfterOptimization { get; set; } = new();
    public AgentConfiguration OptimizedConfiguration { get; set; } = new();
    public OptimizationSummary Summary { get; set; } = new();
    public List<OptimizationStep> OptimizationSteps { get; set; } = new();
    public TimeSpan TotalOptimizationTime { get; set; }
    public DateTime OptimizedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Summary of optimization improvements
/// </summary>
public class OptimizationSummary
{
    public double ThroughputImprovementPercentage { get; set; }
    public double LatencyImprovementPercentage { get; set; }
    public double ResourceUtilizationImprovement { get; set; }
    public double ErrorRateImprovement { get; set; }
    public int AgentCountIncrease { get; set; }
    public List<string> KeyImprovements { get; set; } = new();
    public List<string> RemainingBottlenecks { get; set; } = new();
}

/// <summary>
/// Individual optimization step details
/// </summary>
public class OptimizationStep
{
    public string StepName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Success { get; set; }
    public TimeSpan Duration { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();
    public string Result { get; set; } = string.Empty;
}

/// <summary>
/// Current agent status and configuration
/// </summary>
public class AgentStatus
{
    public int CurrentAgentCount { get; set; }
    public AgentConfiguration CurrentConfiguration { get; set; } = new();
    public AgentHealthStatus HealthStatus { get; set; }
    public PerformanceMetrics CurrentPerformance { get; set; } = new();
    public List<AgentInstance> AgentInstances { get; set; } = new();
    public DateTime LastOptimizedAt { get; set; }
    public string OptimizationStatus { get; set; } = string.Empty;
}

/// <summary>
/// Health status of agent pool
/// </summary>
public enum AgentHealthStatus
{
    Healthy,
    Warning,
    Critical,
    Unknown
}

/// <summary>
/// Individual agent instance details
/// </summary>
public class AgentInstance
{
    public string AgentId { get; set; } = string.Empty;
    public AgentType Type { get; set; }
    public AgentInstanceStatus Status { get; set; }
    public double CpuUtilization { get; set; }
    public double MemoryUtilization { get; set; }
    public int ActiveTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int FailedTasks { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
}

/// <summary>
/// Agent type specialization
/// </summary>
public enum AgentType
{
    Worker,
    Activity,
    Workflow
}

/// <summary>
/// Individual agent instance status
/// </summary>
public enum AgentInstanceStatus
{
    Running,
    Starting,
    Stopping,
    Stopped,
    Error
}

/// <summary>
/// Result of incremental optimization
/// </summary>
public class IncrementalOptimizationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public double ActualImprovementPercentage { get; set; }
    public double TargetImprovementPercentage { get; set; }
    public List<string> OptimizationActions { get; set; } = new();
    public PerformanceMetrics BeforeMetrics { get; set; } = new();
    public PerformanceMetrics AfterMetrics { get; set; } = new();
    public AgentConfiguration AppliedConfiguration { get; set; } = new();
    public DateTime OptimizedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Validation result for agent optimization constraints
/// </summary>
public class AgentOptimizationValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> ConstraintViolations { get; set; } = new();
    public List<string> ConstraintWarnings { get; set; } = new();
    public InfrastructureConstraintStatus InfrastructureStatus { get; set; } = new();
    public AgentConstraintStatus AgentStatus { get; set; } = new();
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Infrastructure constraint compliance status
/// </summary>
public class InfrastructureConstraintStatus
{
    public bool TopicsUnmodified { get; set; } = true;
    public bool PartitionsUnmodified { get; set; } = true;
    public bool JobManagersUnmodified { get; set; } = true;
    public bool ServerCountUnmodified { get; set; } = true;
    public string ComplianceMessage { get; set; } = string.Empty;
}

/// <summary>
/// Agent-specific constraint compliance status
/// </summary>
public class AgentConstraintStatus
{
    public bool WithinAgentLimits { get; set; } = true;
    public bool ConfigurationValid { get; set; } = true;
    public bool ResourceLimitsRespected { get; set; } = true;
    public int MaxAllowedAgents { get; set; } = 50;
    public int CurrentAgentCount { get; set; }
    public string ComplianceMessage { get; set; } = string.Empty;
}