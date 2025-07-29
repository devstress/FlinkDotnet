using System;
using System.Collections.Generic;

namespace Flink.JobBuilder.Models
{
    /// <summary>
    /// Result of job submission to Flink Job Gateway
    /// </summary>
    public class JobSubmissionResult
    {
        public string JobId { get; set; } = string.Empty;
        public string FlinkJobId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime SubmittedAt { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
        
        /// <summary>
        /// Gets whether the submission was successful
        /// </summary>
        public bool IsSuccess => Success;
        
        /// <summary>
        /// Creates a successful job submission result
        /// </summary>
        public static JobSubmissionResult CreateSuccess(string jobId, string flinkJobId)
        {
            return new JobSubmissionResult
            {
                JobId = jobId,
                FlinkJobId = flinkJobId,
                Success = true,
                SubmittedAt = DateTime.UtcNow
            };
        }
        
        /// <summary>
        /// Creates a failed job submission result
        /// </summary>
        public static JobSubmissionResult CreateFailure(string jobId, string errorMessage)
        {
            return new JobSubmissionResult
            {
                JobId = jobId,
                Success = false,
                ErrorMessage = errorMessage,
                SubmittedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Result of job execution (for bounded jobs)
    /// </summary>
    public class JobExecutionResult
    {
        public string JobId { get; set; } = string.Empty;
        public string FlinkJobId { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Error { get; set; }
        public DateTime? CompletedAt { get; set; }
        public JobMetrics? Metrics { get; set; }
    }

    /// <summary>
    /// Job status information
    /// </summary>
    public class JobStatus
    {
        public string JobId { get; set; } = string.Empty;
        public string FlinkJobId { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty; // CREATED, RUNNING, FINISHED, FAILED, CANCELED
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration => EndTime.HasValue && StartTime.HasValue ? EndTime.Value - StartTime.Value : null;
        public string? ErrorMessage { get; set; }
        public JobMetrics? Metrics { get; set; }
    }

    /// <summary>
    /// Job execution metrics
    /// </summary>
    public class JobMetrics
    {
        public string FlinkJobId { get; set; } = string.Empty;
        public TimeSpan? Runtime { get; set; }
        public long RecordsIn { get; set; }
        public long RecordsOut { get; set; }
        public int Parallelism { get; set; }
        public int Checkpoints { get; set; }
        public DateTime? LastCheckpoint { get; set; }
        public long RecordsRead { get; set; }
        public long RecordsWritten { get; set; }
        public long BytesRead { get; set; }
        public long BytesWritten { get; set; }
        public TimeSpan? Duration { get; set; }
        public Dictionary<string, object> CustomMetrics { get; set; } = new();
    }

    /// <summary>
    /// Gateway service configuration
    /// </summary>
    public class FlinkJobGatewayConfiguration
    {
        public string BaseUrl { get; set; } = "http://localhost:8080";
        public string? ApiKey { get; set; }
        public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromMinutes(5);
        public bool UseHttps { get; set; } = false;
        public int MaxRetries { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    }
}