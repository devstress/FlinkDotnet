using FlinkDotNet.Orchestra.Models;
using FlinkDotNet.Temporal.Models;

namespace FlinkDotNet.Temporal.Workflows;

/// <summary>
/// Main Temporal workflow for orchestrating multiple Flink clusters.
/// Implements enterprise actor workflow patterns for massive scale cluster management.
/// Note: Temporal attributes will be added when implementing actual workflows
/// </summary>
public interface IClusterOrchestratorWorkflow
{
    /// <summary>
    /// Orchestrates the lifecycle of multiple Flink clusters with fault tolerance and resilience.
    /// </summary>
    /// <param name="request">Orchestration parameters and configuration.</param>
    /// <returns>Task representing the ongoing orchestration workflow.</returns>
    Task OrchestrateClustersAsync(OrchestrationRequest request);
}

/// <summary>
/// Workflow for distributing jobs across multiple Flink clusters with intelligent placement.
/// Note: Temporal attributes will be added when implementing actual workflows
/// </summary>
public interface IJobDistributionWorkflow
{
    /// <summary>
    /// Distributes a batch of jobs across available clusters using specified strategies.
    /// </summary>
    /// <param name="jobs">List of job definitions to distribute.</param>
    /// <param name="strategy">Distribution strategy to use.</param>
    /// <returns>Task with distribution results.</returns>
    Task<JobDistributionResult> DistributeJobsAsync(List<FlinkJobDefinition> jobs, SubmissionStrategy strategy);
}

/// <summary>
/// Workflow for managing the lifecycle of a single Flink cluster actor.
/// Note: Temporal attributes will be added when implementing actual workflows
/// </summary>
public interface IClusterLifecycleWorkflow
{
    /// <summary>
    /// Manages the complete lifecycle of a Flink cluster from provisioning to decommissioning.
    /// </summary>
    /// <param name="config">Cluster configuration.</param>
    /// <returns>Task representing the cluster lifecycle management.</returns>
    Task ManageClusterLifecycleAsync(ClusterConfiguration config);
}

/// <summary>
/// Workflow for auto-scaling clusters based on demand and performance metrics.
/// Note: Temporal attributes will be added when implementing actual workflows
/// </summary>
public interface IAutoScalingWorkflow
{
    /// <summary>
    /// Continuously monitors cluster utilization and scales clusters automatically.
    /// </summary>
    /// <param name="config">Auto-scaling configuration.</param>
    /// <returns>Task representing the ongoing auto-scaling workflow.</returns>
    Task AutoScaleClustersAsync(AutoScalingConfig config);
}

/// <summary>
/// Workflow for handling cluster failures and implementing recovery strategies.
/// Note: Temporal attributes will be added when implementing actual workflows
/// </summary>
public interface IFailureRecoveryWorkflow
{
    /// <summary>
    /// Handles cluster failures with automatic detection, recovery, and failover.
    /// </summary>
    /// <param name="clusterId">ID of the failed cluster.</param>
    /// <param name="failureInfo">Information about the failure.</param>
    /// <returns>Task representing the recovery workflow.</returns>
    Task HandleClusterFailureAsync(string clusterId, ClusterFailureInfo failureInfo);
}