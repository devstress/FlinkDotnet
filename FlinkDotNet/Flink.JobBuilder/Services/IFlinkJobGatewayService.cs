using System.Threading;
using System.Threading.Tasks;
using Flink.JobBuilder.Models;

namespace Flink.JobBuilder.Services
{
    /// <summary>
    /// Interface for communicating with Flink Job Gateway
    /// </summary>
    public interface IFlinkJobGatewayService
    {
        /// <summary>
        /// Submit a job to the Flink cluster via the gateway
        /// </summary>
        /// <param name="jobDefinition">Job definition with IR</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Job submission result</returns>
        Task<JobSubmissionResult> SubmitJobAsync(JobDefinition jobDefinition, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the status of a running job
        /// </summary>
        /// <param name="flinkJobId">Flink job ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Job status</returns>
        Task<JobStatus> GetJobStatusAsync(string flinkJobId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get metrics for a running job
        /// </summary>
        /// <param name="flinkJobId">Flink job ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Job metrics</returns>
        Task<JobMetrics> GetJobMetricsAsync(string flinkJobId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancel a running job
        /// </summary>
        /// <param name="flinkJobId">Flink job ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if canceled successfully</returns>
        Task<bool> CancelJobAsync(string flinkJobId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Test connectivity to the gateway
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if gateway is reachable</returns>
        Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
    }
}