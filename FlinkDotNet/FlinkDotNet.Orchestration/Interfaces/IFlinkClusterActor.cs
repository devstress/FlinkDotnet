using FlinkDotNet.Orchestration.Models;

namespace FlinkDotNet.Orchestration.Interfaces;

/// <summary>
/// Represents a Flink cluster actor that manages the lifecycle and operations of a single Flink cluster.
/// Based on enterprise actor model for cluster orchestration.
/// </summary>
public interface IFlinkClusterActor
{
    /// <summary>
    /// Gets the unique identifier for this cluster actor.
    /// </summary>
    string ClusterId { get; }

    /// <summary>
    /// Gets the current status and health information of the cluster.
    /// </summary>
    /// <returns>Current cluster status including health, capacity, and performance metrics.</returns>
    Task<ClusterStatus> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits a Flink job to this cluster for execution.
    /// </summary>
    /// <param name="job">The Flink job definition to submit.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Result of the job submission including job ID and placement information.</returns>
    Task<JobSubmissionResult> SubmitJobAsync(FlinkJobDefinition job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scales the cluster to the specified parallelism level.
    /// </summary>
    /// <param name="parallelism">Target parallelism level for the cluster.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if scaling was successful, false otherwise.</returns>
    Task<bool> ScaleAsync(int parallelism, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a graceful restart of the cluster.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task that completes when restart is finished.</returns>
    Task RestartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gracefully shuts down the cluster and releases all resources.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task that completes when shutdown is finished.</returns>
    Task ShutdownAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts health monitoring for this cluster.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the monitoring operation.</param>
    /// <returns>Task that runs continuously monitoring cluster health.</returns>
    Task StartHealthMonitoringAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets performance metrics for the cluster.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Current performance metrics.</returns>
    Task<ClusterMetrics> GetMetricsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Main orchestration service that manages multiple Flink clusters and distributes jobs.
/// Implements enterprise multi-cluster orchestration patterns.
/// </summary>
public interface IFlinkOrchestra
{
    /// <summary>
    /// Submits a job to the best available cluster based on the specified strategy.
    /// </summary>
    /// <param name="job">The Flink job definition to submit.</param>
    /// <param name="strategy">Strategy for cluster selection and job placement.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Result of the job submission including placement information.</returns>
    Task<JobSubmissionResult> SubmitJobAsync(FlinkJobDefinition job, SubmissionStrategy strategy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about all available clusters in the orchestra.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Array of cluster information including health and capacity.</returns>
    Task<ClusterInfo[]> GetAvailableClustersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Provisions a new Flink cluster with the specified configuration.
    /// </summary>
    /// <param name="config">Configuration for the new cluster.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The created cluster actor.</returns>
    Task<IFlinkClusterActor> ProvisionClusterAsync(ClusterConfiguration config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets comprehensive health report for all clusters in the orchestra.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Health report aggregating status across all clusters.</returns>
    Task<HealthReport> GetClusterHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Scales the orchestra by adding or removing clusters based on demand.
    /// </summary>
    /// <param name="targetCapacity">Target total capacity for the orchestra.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Scaling operation result.</returns>
    Task<ScalingResult> ScaleOrchestraAsync(int targetCapacity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a Temporal workflow to manage cluster lifecycle and job distribution.
    /// </summary>
    /// <param name="request">Orchestration parameters and configuration.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Workflow execution ID.</returns>
    Task<string> StartOrchestrationWorkflowAsync(OrchestrationRequest request, CancellationToken cancellationToken = default);
}