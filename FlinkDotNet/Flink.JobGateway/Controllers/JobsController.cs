using Microsoft.AspNetCore.Mvc;
using Flink.JobBuilder.Models;
using Flink.JobGateway.Services;
using System.Text.Json;
using System.Text;

namespace Flink.JobGateway.Controllers;

/// <summary>
/// REST Controller for Flink Job Gateway
/// Handles job submissions from .NET SDK and communicates with Apache Flink cluster
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[ApiVersion("1.0")]
public class JobsController : ControllerBase
{
    private readonly ILogger<JobsController> _logger;
    private readonly IFlinkJobManager _flinkJobManager;

    public JobsController(ILogger<JobsController> logger, IFlinkJobManager flinkJobManager)
    {
        _logger = logger;
        _flinkJobManager = flinkJobManager;
    }

    /// <summary>
    /// Submit a job to the Flink cluster
    /// </summary>
    /// <param name="jobDefinition">Job definition from .NET SDK</param>
    /// <returns>Job submission result</returns>
    [HttpPost("submit")]
    public async Task<ActionResult<JobSubmissionResult>> SubmitJob()
    {
        string raw;
        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            raw = await reader.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed reading request body");
            return BadRequest(new { error = "Unable to read request body", ex.Message });
        }

        if (string.IsNullOrWhiteSpace(raw))
        {
            return BadRequest(new { error = "Empty request body" });
        }

        JobDefinition? jobDefinition = null;
        try
        {
            var opts = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
            };
            jobDefinition = JsonSerializer.Deserialize<JobDefinition>(raw, opts);
            if (jobDefinition == null)
            {
                return BadRequest(new { error = "Unable to deserialize job definition" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Deserialization failure for job submission. Raw snippet: {Snippet}", raw.Length > 400 ? raw[..400] : raw);
            return BadRequest(new { error = "Invalid job definition JSON", ex.Message });
        }

        // Allow sink-less SQL jobs
        if (jobDefinition.Source is SqlSourceDefinition && jobDefinition.Sink == null)
        {
            _logger.LogDebug("SQL job without sink accepted (statements define sinks). JobId placeholder will be set if missing.");
        }

        // Ensure metadata basics
        jobDefinition.Metadata ??= new JobMetadata();
        if (string.IsNullOrWhiteSpace(jobDefinition.Metadata.JobId))
        {
            jobDefinition.Metadata.JobId = Guid.NewGuid().ToString();
        }

        _logger.LogInformation("Received job submission request for job: {JobId}", jobDefinition.Metadata.JobId);

        try
        {
            var result = await _flinkJobManager.SubmitJobAsync(jobDefinition);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Job submitted successfully: {JobId} -> {FlinkJobId}",
                    result.JobId, result.FlinkJobId);
                return Ok(result);
            }
            else
            {
                _logger.LogError("Job submission failed: {ErrorMessage}", result.ErrorMessage);
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting job: {Message}", ex.Message);
            var result = JobSubmissionResult.CreateFailure(
                jobDefinition.Metadata.JobId,
                $"Internal server error: {ex.Message}");
            return StatusCode(500, result);
        }
    }

    /// <summary>
    /// Get the status of a running job
    /// </summary>
    /// <param name="flinkJobId">Flink job ID</param>
    /// <returns>Job status</returns>
    [HttpGet("{flinkJobId}/status")]
    public async Task<ActionResult<JobStatus>> GetJobStatus(string flinkJobId)
    {
        _logger.LogInformation("Retrieving status for job: {FlinkJobId}", flinkJobId);

        try
        {
            var status = await _flinkJobManager.GetJobStatusAsync(flinkJobId);
            if (status != null)
            {
                return Ok(status);
            }
            else
            {
                return NotFound();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job status: {Message}", ex.Message);
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Get metrics for a job
    /// </summary>
    /// <param name="flinkJobId">Flink job ID</param>
    /// <returns>Job metrics</returns>
    [HttpGet("{flinkJobId}/metrics")]
    public async Task<ActionResult<JobMetrics>> GetJobMetrics(string flinkJobId)
    {
        _logger.LogInformation("Retrieving metrics for job: {FlinkJobId}", flinkJobId);

        try
        {
            var metrics = await _flinkJobManager.GetJobMetricsAsync(flinkJobId);
            if (metrics != null)
            {
                return Ok(metrics);
            }
            else
            {
                return NotFound();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job metrics: {Message}", ex.Message);
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Cancel a running job
    /// </summary>
    /// <param name="flinkJobId">Flink job ID</param>
    /// <returns>Success status</returns>
    [HttpPost("{flinkJobId}/cancel")]
    public async Task<ActionResult> CancelJob(string flinkJobId)
    {
        _logger.LogInformation("Canceling job: {FlinkJobId}", flinkJobId);

        try
        {
            var canceled = await _flinkJobManager.CancelJobAsync(flinkJobId);
            if (canceled)
            {
                return Ok();
            }
            else
            {
                return NotFound();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling job: {Message}", ex.Message);
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet("health")]
    public ActionResult<string> HealthCheck()
    {
        return Ok("OK");
    }
}
