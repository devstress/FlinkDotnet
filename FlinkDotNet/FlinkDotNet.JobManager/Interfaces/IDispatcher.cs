//  Licensed to the Apache Software Foundation (ASF) under one
//  or more contributor license agreements.  See the NOTICE file
//  distributed with this work for additional information
//  regarding copyright ownership.  The ASF licenses this file
//  to you under the Apache License, Version 2.0 (the
//  "License"); you may not use this file except in compliance
//  with the License.  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
// limitations under the License.

using FlinkDotNet.JobManager.Models;

namespace FlinkDotNet.JobManager.Interfaces;

/// <summary>
/// Dispatcher handles job submission and creates JobMaster instances.
/// Equivalent to Apache Flink's Dispatcher component.
/// </summary>
public interface IDispatcher
{
    /// <summary>
    /// Submit a new job for execution
    /// </summary>
    /// <param name="jobGraph">Job graph definition</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Job submission result with job ID</returns>
    public Task<JobSubmissionResult> SubmitJobAsync(JobGraph jobGraph, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel a running job
    /// </summary>
    /// <param name="jobId">Job identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task CancelJobAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get status of a job
    /// </summary>
    /// <param name="jobId">Job identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current job status</returns>
    public Task<JobStatus> GetJobStatusAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// List all jobs in the cluster
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of job statuses</returns>
    public Task<List<JobStatus>> ListJobsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of job submission
/// </summary>
public class JobSubmissionResult
{
    /// <summary>
    /// Assigned job identifier
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Whether submission was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if submission failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Job status information
/// </summary>
public class JobStatus
{
    /// <summary>
    /// Job identifier
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Job name
    /// </summary>
    public string JobName { get; set; } = string.Empty;

    /// <summary>
    /// Current execution state
    /// </summary>
    public JobExecutionState State { get; set; }

    /// <summary>
    /// Job start time
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Job end time
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Duration of job execution
    /// </summary>
    public TimeSpan? Duration => EndTime.HasValue && StartTime.HasValue
        ? EndTime.Value - StartTime.Value
        : null;
}
