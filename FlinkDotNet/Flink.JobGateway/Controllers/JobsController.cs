using Microsoft.AspNetCore.Mvc;
using Flink.JobBuilder.Models;
using Flink.JobGateway.Services;

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
    public async Task<ActionResult<JobSubmissionResult>> SubmitJob([FromBody] JobDefinition jobDefinition)
    {
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
                $"Internal server error: {ex.Message}"
            );
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