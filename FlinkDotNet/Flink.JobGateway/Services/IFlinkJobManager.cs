using Flink.JobBuilder.Models;

namespace Flink.JobGateway.Services;

/// <summary>
/// Interface for managing Apache Flink jobs
/// </summary>
public interface IFlinkJobManager
{
    /// <summary>
    /// Submit a job to the Flink cluster
    /// </summary>
    /// <param name="jobDefinition">Job definition from .NET SDK</param>
    /// <returns>Job submission result</returns>
    Task<JobSubmissionResult> SubmitJobAsync(JobDefinition jobDefinition);

    /// <summary>
    /// Get the status of a running job
    /// </summary>
    /// <param name="flinkJobId">Flink job ID</param>
    /// <returns>Job status</returns>
    Task<JobStatus?> GetJobStatusAsync(string flinkJobId);

    /// <summary>
    /// Get metrics for a job
    /// </summary>
    /// <param name="flinkJobId">Flink job ID</param>
    /// <returns>Job metrics</returns>
    Task<JobMetrics?> GetJobMetricsAsync(string flinkJobId);

    /// <summary>
    /// Cancel a running job
    /// </summary>
    /// <param name="flinkJobId">Flink job ID</param>
    /// <returns>True if canceled successfully</returns>
    Task<bool> CancelJobAsync(string flinkJobId);
}