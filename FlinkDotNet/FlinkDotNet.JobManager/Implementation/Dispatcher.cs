// Copyright 2025 FlinkDotNet
// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using FlinkDotNet.JobManager.Interfaces;
using FlinkDotNet.JobManager.Models;
using Temporalio.Client;

namespace FlinkDotNet.JobManager.Implementation;

/// <summary>
/// Dispatcher manages job submission and lifecycle.
/// Thread-safe implementation for concurrent job submission.
/// </summary>
public class Dispatcher(IResourceManager resourceManager, ITemporalClient temporalClient, ILoggerFactory loggerFactory) : IDispatcher
{
    private readonly ConcurrentDictionary<string, JobInfo> _jobs = new();
    private readonly IResourceManager _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
    private readonly ITemporalClient _temporalClient = temporalClient ?? throw new ArgumentNullException(nameof(temporalClient));
    private readonly ILoggerFactory _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

    /// <summary>
    /// Submit a new job for execution.
    /// </summary>
#pragma warning disable CS1998 // Async method lacks 'await' operators
    public async Task<JobSubmissionResult> SubmitJobAsync(JobGraph jobGraph, CancellationToken cancellationToken = default)
#pragma warning restore CS1998
    {
        if (jobGraph == null)
            throw new ArgumentNullException(nameof(jobGraph));

        try
        {
            // Validate job graph
            ValidateJobGraph(jobGraph);

            // Generate unique job ID
            string jobId = Guid.NewGuid().ToString();
            jobGraph.JobId = jobId;

            // Create job info
            JobInfo jobInfo = new()
            {
                JobId = jobId,
                JobName = jobGraph.JobName,
                JobGraph = jobGraph,
                State = JobExecutionState.Created,
                SubmittedAt = DateTime.UtcNow,
                TotalTasks = CalculateTotalTasks(jobGraph)
            };

            // Store job
            if (!_jobs.TryAdd(jobId, jobInfo))
            {
                return new JobSubmissionResult
                {
                    JobId = jobId,
                    Success = false,
                    ErrorMessage = $"Job with ID {jobId} already exists"
                };
            }

            // Start job execution asynchronously
            _ = Task.Run(() => ExecuteJobAsync(jobInfo), cancellationToken);

            return new JobSubmissionResult
            {
                JobId = jobId,
                Success = true
            };
        }
        catch (Exception ex)
        {
            return new JobSubmissionResult
            {
                JobId = string.Empty,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Get status of a job.
    /// </summary>
    public Task<JobStatus> GetJobStatusAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (_jobs.TryGetValue(jobId, out JobInfo? jobInfo))
        {
            return Task.FromResult(new JobStatus
            {
                JobId = jobInfo.JobId,
                JobName = jobInfo.JobName,
                State = jobInfo.State,
                StartTime = jobInfo.StartedAt,
                EndTime = jobInfo.FinishedAt
            });
        }

        return Task.FromResult<JobStatus>(null!);
    }

    /// <summary>
    /// Cancel a running job.
    /// </summary>
    public async Task CancelJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (!_jobs.TryGetValue(jobId, out JobInfo? jobInfo))
        {
            throw new ArgumentException($"Job {jobId} not found", nameof(jobId));
        }

        if (jobInfo.State == JobExecutionState.Running || jobInfo.State == JobExecutionState.Created || jobInfo.State == JobExecutionState.Deploying)
        {
            jobInfo.State = JobExecutionState.Canceling;

            // Cancel via JobMaster if available
            if (jobInfo.JobMaster != null)
            {
                await jobInfo.JobMaster.CancelJobAsync(cancellationToken);
            }
            else
            {
                // Fallback to cancellation token if JobMaster not yet created
                jobInfo.CancellationToken?.Cancel();

                // Wait a bit for cancellation to complete
                await Task.Delay(100, cancellationToken);
            }

            jobInfo.State = JobExecutionState.Canceled;
            jobInfo.FinishedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// List all jobs.
    /// </summary>
    public Task<List<JobStatus>> ListJobsAsync(CancellationToken cancellationToken = default)
    {
        List<JobStatus> jobs = [.. _jobs.Values
            .OrderByDescending(j => j.SubmittedAt)
            .Select(j => new JobStatus
            {
                JobId = j.JobId,
                JobName = j.JobName,
                State = j.State,
                StartTime = j.StartedAt,
                EndTime = j.FinishedAt
            })];

        return Task.FromResult(jobs);
    }

    /// <summary>
    /// Get jobs by state.
    /// </summary>
    public Task<IEnumerable<JobInfo>> GetJobsByStateAsync(JobExecutionState state)
    {
        IEnumerable<JobInfo> jobs = _jobs.Values
            .Where(j => j.State == state)
            .OrderByDescending(j => j.SubmittedAt);
        return Task.FromResult(jobs);
    }

    private static void ValidateJobGraph(JobGraph jobGraph)
    {
        if (string.IsNullOrWhiteSpace(jobGraph.JobName))
        {
            throw new ArgumentException("Job name cannot be empty", nameof(jobGraph));
        }

        if (jobGraph.Vertices.Count == 0)
        {
            throw new ArgumentException("Job must have at least one vertex", nameof(jobGraph));
        }

        if (jobGraph.MaxParallelism <= 0)
        {
            throw new ArgumentException("Max parallelism must be positive", nameof(jobGraph));
        }

        // Validate vertices
        foreach (JobVertex vertex in jobGraph.Vertices)
        {
            if (string.IsNullOrWhiteSpace(vertex.OperatorName))
            {
                throw new ArgumentException("Vertex must have an operator name", nameof(jobGraph));
            }

            if (vertex.Parallelism <= 0 || vertex.Parallelism > jobGraph.MaxParallelism)
            {
                throw new ArgumentException(
                    $"Vertex parallelism must be between 1 and {jobGraph.MaxParallelism}",
                    nameof(jobGraph));
            }
        }

        // Validate edges
        foreach (JobEdge edge in jobGraph.Edges)
        {
            if (!jobGraph.Vertices.Any(v => v.VertexId == edge.SourceVertexId))
            {
                throw new ArgumentException($"Edge references non-existent source vertex {edge.SourceVertexId}", nameof(jobGraph));
            }

            if (!jobGraph.Vertices.Any(v => v.VertexId == edge.TargetVertexId))
            {
                throw new ArgumentException($"Edge references non-existent target vertex {edge.TargetVertexId}", nameof(jobGraph));
            }
        }
    }

    private static int CalculateTotalTasks(JobGraph jobGraph)
    {
        return jobGraph.Vertices.Sum(v => v.Parallelism);
    }

    private async Task ExecuteJobAsync(JobInfo jobInfo)
    {
        ILogger<JobMaster> jobMasterLogger = this._loggerFactory.CreateLogger<JobMaster>();

        try
        {
            // Create JobMaster for this job
            JobMaster jobMaster = new(
                jobInfo.JobId,
                jobInfo.JobGraph,
                this._resourceManager,
                this._temporalClient,
                jobMasterLogger);

            // Store JobMaster reference for later access
            jobInfo.JobMaster = jobMaster;

            // Start job execution via JobMaster
            await jobMaster.StartJobAsync(jobInfo.CancellationToken?.Token ?? CancellationToken.None);

            // Get final execution graph
            ExecutionGraph executionGraph = await jobMaster.GetExecutionGraphAsync();

            // Update job info based on execution graph state
            jobInfo.State = executionGraph.State;
            jobInfo.FinishedAt = executionGraph.FinishedAt;
            jobInfo.ErrorMessage = executionGraph.FailureMessage;

            // Update task counts
            jobInfo.CompletedTasks = executionGraph.ExecutionVertices.Count(v => v.State == ExecutionState.Finished);
            jobInfo.FailedTasks = executionGraph.ExecutionVertices.Count(v => v.State == ExecutionState.Failed);
            jobInfo.RunningTasks = executionGraph.ExecutionVertices.Count(v => v.State == ExecutionState.Running);
        }
        catch (OperationCanceledException)
        {
            jobInfo.State = JobExecutionState.Canceled;
            jobInfo.FinishedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            jobInfo.State = JobExecutionState.Failed;
            jobInfo.FinishedAt = DateTime.UtcNow;
            jobInfo.ErrorMessage = ex.Message;
        }
    }
}

/// <summary>
/// Runtime information about a job.
/// </summary>
public class JobInfo
{
    public required string JobId
    {
        get; set;
    }
    public required string JobName
    {
        get; set;
    }
    public required JobGraph JobGraph
    {
        get; set;
    }
    public JobExecutionState State
    {
        get; set;
    }
    public DateTime SubmittedAt
    {
        get; set;
    }
    public DateTime? StartedAt
    {
        get; set;
    }
    public DateTime? FinishedAt
    {
        get; set;
    }
    public int TotalTasks
    {
        get; set;
    }
    public int RunningTasks
    {
        get; set;
    }
    public int CompletedTasks
    {
        get; set;
    }
    public int FailedTasks
    {
        get; set;
    }
    public string? ErrorMessage
    {
        get; set;
    }
    public CancellationTokenSource? CancellationToken
    {
        get; set;
    }
    public JobMaster? JobMaster
    {
        get; set;
    }
}
