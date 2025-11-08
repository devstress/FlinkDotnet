// Copyright 2025 FlinkDotNet
// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;
using FlinkDotNet.JobManager.Interfaces;
using FlinkDotNet.JobManager.Models;
using FlinkDotNet.JobManager.Models.Requests;
using FlinkDotNet.JobManager.Models.Responses;

namespace FlinkDotNet.JobManager.Controllers;

/// <summary>
/// API controller for job management operations.
/// </summary>
[ApiController]
[Route("api/jobs")]
[Produces("application/json")]
public class JobsController : ControllerBase
{
    private readonly IDispatcher _dispatcher;
    private readonly ILogger<JobsController> _logger;

    public JobsController(IDispatcher dispatcher, ILogger<JobsController> logger)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Submit a new job for execution.
    /// </summary>
    /// <param name="request">Job submission request containing the job graph.</param>
    /// <returns>Job ID and initial status.</returns>
    [HttpPost("submit")]
    [ProducesResponseType(typeof(SubmitJobResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SubmitJob([FromBody] SubmitJobRequest request)
    {
        try
        {
            _logger.LogInformation("Submitting job: {JobName}", request.JobName);

            // Convert request to JobGraph
            JobGraph jobGraph = ConvertToJobGraph(request);

            // Submit job
            JobSubmissionResult result = await _dispatcher.SubmitJobAsync(jobGraph);

            if (!result.Success)
            {
                _logger.LogWarning("Job submission failed: {Error}", result.ErrorMessage);
                return BadRequest(new { error = result.ErrorMessage });
            }

            _logger.LogInformation("Job submitted successfully: {JobId}", result.JobId);

            SubmitJobResponse response = new()
            {
                JobId = result.JobId,
                State = JobExecutionState.Created,
                SubmittedAt = DateTime.UtcNow,
                Message = "Job submitted successfully"
            };

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid job submission request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit job");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get the status of a job.
    /// </summary>
    /// <param name="jobId">The unique identifier of the job.</param>
    /// <returns>Current job status and metrics.</returns>
    [HttpGet("{jobId}/status")]
    [ProducesResponseType(typeof(JobStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJobStatus(string jobId)
    {
        _logger.LogDebug("Getting status for job: {JobId}", jobId);

        JobStatus? jobStatus = await _dispatcher.GetJobStatusAsync(jobId);

        if (jobStatus == null)
        {
            _logger.LogWarning("Job not found: {JobId}", jobId);
            return NotFound(new { error = $"Job {jobId} not found" });
        }

        JobStatusResponse response = new()
        {
            JobId = jobStatus.JobId,
            JobName = jobStatus.JobName,
            State = jobStatus.State,
            SubmittedAt = jobStatus.StartTime ?? DateTime.UtcNow,
            StartedAt = jobStatus.StartTime,
            FinishedAt = jobStatus.EndTime,
            Duration = jobStatus.Duration,
            ErrorMessage = null, // Error message will be added to JobStatus model in future iteration
            TotalTasks = 0, // Task count retrieval from ExecutionGraph deferred to future iteration
            RunningTasks = 0,
            CompletedTasks = 0,
            FailedTasks = 0
        };

        return Ok(response);
    }

    /// <summary>
    /// Cancel a running job.
    /// </summary>
    /// <param name="jobId">The unique identifier of the job to cancel.</param>
    /// <returns>Success or failure result.</returns>
    [HttpPost("{jobId}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelJob(string jobId)
    {
        _logger.LogInformation("Canceling job: {JobId}", jobId);

        try
        {
            await _dispatcher.CancelJobAsync(jobId);
            _logger.LogInformation("Job canceled successfully: {JobId}", jobId);
            return Ok(new { message = $"Job {jobId} canceled successfully" });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Job not found: {JobId}", jobId);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel job: {JobId}", jobId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// List all jobs.
    /// </summary>
    /// <param name="state">Optional filter by job state.</param>
    /// <returns>List of jobs matching the criteria.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(JobListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListJobs([FromQuery] string? state = null)
    {
        _logger.LogDebug("Listing jobs (state filter: {State})", state ?? "none");

        List<JobStatus> jobs = await _dispatcher.ListJobsAsync();

        // Apply state filter if provided
        if (!string.IsNullOrEmpty(state) && Enum.TryParse<JobExecutionState>(state, true, out JobExecutionState stateFilter))
        {
            jobs = jobs.Where(j => j.State == stateFilter).ToList();
        }

        JobListResponse response = new()
        {
            TotalJobs = jobs.Count,
            Jobs = jobs.Select(j => new JobSummary
            {
                JobId = j.JobId,
                JobName = j.JobName,
                State = j.State,
                SubmittedAt = j.StartTime ?? DateTime.UtcNow,
                Duration = j.Duration
            }).ToList()
        };

        return Ok(response);
    }

    private static JobGraph ConvertToJobGraph(SubmitJobRequest request)
    {
        JobGraph jobGraph = new()
        {
            JobName = request.JobName,
            MaxParallelism = request.MaxParallelism
        };

        // Convert vertices
        foreach (JobVertexRequest vertexRequest in request.Vertices)
        {
            if (!Enum.TryParse<OperatorType>(vertexRequest.OperatorType, true, out OperatorType operatorType))
            {
                throw new ArgumentException($"Invalid operator type: {vertexRequest.OperatorType}");
            }

            JobVertex vertex = new()
            {
                OperatorName = vertexRequest.OperatorName,
                Type = operatorType,
                Parallelism = vertexRequest.Parallelism,
                OperatorLogic = vertexRequest.OperatorLogic ?? string.Empty
            };

            jobGraph.Vertices.Add(vertex);
        }

        // Convert edges
        foreach (JobEdgeRequest edgeRequest in request.Edges)
        {
            if (edgeRequest.SourceVertexIndex < 0 || edgeRequest.SourceVertexIndex >= jobGraph.Vertices.Count)
            {
                throw new ArgumentException($"Invalid source vertex index: {edgeRequest.SourceVertexIndex}");
            }

            if (edgeRequest.TargetVertexIndex < 0 || edgeRequest.TargetVertexIndex >= jobGraph.Vertices.Count)
            {
                throw new ArgumentException($"Invalid target vertex index: {edgeRequest.TargetVertexIndex}");
            }

            if (!Enum.TryParse<PartitioningStrategy>(edgeRequest.Strategy, true, out PartitioningStrategy strategy))
            {
                throw new ArgumentException($"Invalid partitioning strategy: {edgeRequest.Strategy}");
            }

            JobEdge edge = new()
            {
                SourceVertexId = jobGraph.Vertices[edgeRequest.SourceVertexIndex].VertexId,
                TargetVertexId = jobGraph.Vertices[edgeRequest.TargetVertexIndex].VertexId,
                Strategy = strategy
            };

            jobGraph.Edges.Add(edge);
        }

        return jobGraph;
    }
}
