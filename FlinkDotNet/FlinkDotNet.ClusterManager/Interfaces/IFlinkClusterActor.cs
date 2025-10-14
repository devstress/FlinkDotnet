using FlinkDotNet.ClusterManager.Models;

namespace FlinkDotNet.ClusterManager.Interfaces;

/// <summary>
/// Represents a Flink cluster actor that manages the lifecycle and operations of a single Flink cluster.
/// Based on enterprise actor model for cluster orchestration.
/// </summary>
public interface IFlinkClusterActor
{
    /// <summary>
    /// Gets the unique identifier for this cluster actor.
    /// </summary>
    string ClusterId
    {
        get;
    }

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
