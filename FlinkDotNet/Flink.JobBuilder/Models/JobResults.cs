using System;
using System.Collections.Generic;

namespace Flink.JobBuilder.Models
{
    /// <summary>
    /// Result of job submission to Flink Job Gateway
    /// </summary>
    public class JobSubmissionResult
    {
        public string FlinkJobId { get; set; } = string.Empty;
        public bool Success
        {
            get; set;
        }
        public string? ErrorMessage
        {
            get; set;
        }
        public DateTime SubmittedAt
        {
            get; set;
        }
        public Dictionary<string, string> Metadata { get; init; } = [];

        /// <summary>
        /// Gets whether the submission was successful
        /// </summary>
        public bool IsSuccess => this.Success;

        /// <summary>
        /// Creates a successful job submission result
        /// </summary>
        public static JobSubmissionResult CreateSuccess(string flinkJobId)
        {
            return new JobSubmissionResult
            {
                FlinkJobId = flinkJobId,
                Success = true,
                SubmittedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a failed job submission result
        /// </summary>
        public static JobSubmissionResult CreateFailure(string errorMessage)
        {
            return new JobSubmissionResult
            {
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
        public string FlinkJobId { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public bool Success
        {
            get; set;
        }
        public string? Error
        {
            get; set;
        }
        public DateTime? CompletedAt
        {
            get; set;
        }
        public JobMetrics? Metrics
        {
            get; set;
        }
    }

    /// <summary>
    /// Job status information
    /// </summary>
    public class JobStatus
    {
        public string FlinkJobId { get; set; } = string.Empty;
        /// <summary>
        /// State of the job: CREATED, RUNNING, FINISHED, FAILED, CANCELED
        /// </summary>
        public string State { get; set; } = string.Empty;
        public DateTime? StartTime
        {
            get; set;
        }
        public DateTime? EndTime
        {
            get; set;
        }
        public TimeSpan? Duration => this.EndTime.HasValue && this.StartTime.HasValue ? this.EndTime.Value - this.StartTime.Value : null;
        public string? ErrorMessage
        {
            get; set;
        }
        public JobMetrics? Metrics
        {
            get; set;
        }
    }

    /// <summary>
    /// Job execution metrics
    /// </summary>
    public class JobMetrics
    {
        public string FlinkJobId { get; set; } = string.Empty;
        public TimeSpan? Runtime
        {
            get; set;
        }
        public long RecordsIn
        {
            get; set;
        }
        public long RecordsOut
        {
            get; set;
        }
        public int Parallelism
        {
            get; set;
        }
        public int Checkpoints
        {
            get; set;
        }
        public DateTime? LastCheckpoint
        {
            get; set;
        }
        public long RecordsRead
        {
            get; set;
        }
        public long RecordsWritten
        {
            get; set;
        }
        public long BytesRead
        {
            get; set;
        }
        public long BytesWritten
        {
            get; set;
        }
        public TimeSpan? Duration
        {
            get; set;
        }
        public Dictionary<string, object> CustomMetrics { get; init; } = [];

        public string? BackpressureLevel
        {
            get; set;
        }
    }

    /// <summary>
    /// Gateway service configuration
    /// Priority: Explicit configuration > Environment variables > Default values
    /// For ASP.NET Core apps with DI, use ConfigureFlinkJobGateway() extension method to bind from appsettings
    /// </summary>
    public class FlinkJobGatewayConfiguration
    {
        private string? _baseUrl;

        /// <summary>
        /// Base URL for the Flink Job Gateway
        /// Priority: Explicitly set value > FLINK_JOB_GATEWAY_URL environment variable > FlinkJobGateway:BaseUrl appsettings (when using DI)
        /// </summary>
        public string BaseUrl
        {
            get => this._baseUrl ?? Environment.GetEnvironmentVariable("FLINK_JOB_GATEWAY_URL")
                   ?? throw new InvalidOperationException("BaseUrl must be configured via property, FLINK_JOB_GATEWAY_URL environment variable, or FlinkJobGateway:BaseUrl in appsettings");
            set => this._baseUrl = value;
        }

        public string? ApiKey
        {
            get; set;
        }
        public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromMinutes(5);
        public bool UseHttps
        {
            get; set;
        }
        public int MaxRetries { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    }
}
