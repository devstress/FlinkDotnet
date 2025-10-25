using System.Text;
using System.Text.Json;
using Flink.JobBuilder.Models;
using FlinkDotNet.JobGateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlinkDotNet.JobGateway.Controllers;

/// <summary>
/// REST Controller for Flink Job Gateway
/// Handles job submissions from .NET SDK and communicates with Apache Flink cluster
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[ApiVersion("1.0")]
public class JobsController : ControllerBase
{
    private const string LogBorderTop = "╔══════════════════════════════════════════════════════════════";
    private const string LogBorderBottom = "╚══════════════════════════════════════════════════════════════";
    
    private readonly ILogger<JobsController> _logger;
    private readonly IFlinkJobManager _flinkJobManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobsController"/> class.
    /// </summary>
    /// <param name="logger">Logger for tracking controller operations.</param>
    /// <param name="flinkJobManager">The Flink job manager service for job operations.</param>
    public JobsController(ILogger<JobsController> logger, IFlinkJobManager flinkJobManager)
    {
        _logger = logger;
        _flinkJobManager = flinkJobManager;
    }

    /// <summary>
    /// Submit a job to the Flink cluster. The request body must contain a JobDefinition JSON payload.
    /// </summary>
    /// <returns>Job submission result</returns>
    [HttpPost("submit")]
    public async Task<ActionResult<JobSubmissionResult>> SubmitJob()
    {
        LogRequestReceived();

        var requestBodyResult = await ReadRequestBodyAsync();
        if (requestBodyResult.Error != null)
            return requestBodyResult.Error;

        var jobDefResult = DeserializeJobDefinition(requestBodyResult.Body!);
        if (jobDefResult.Error != null)
            return jobDefResult.Error;

        var jobDefinition = jobDefResult.JobDefinition!;
        EnsureJobMetadata(jobDefinition);

        _logger.LogInformation("📋 Job metadata: JobId={JobId}, JobName={JobName}",
            jobDefinition.Metadata.JobId,
            jobDefinition.Metadata.JobName ?? "Unnamed");

        return await SubmitJobToFlinkAsync(jobDefinition);
    }

    private void LogRequestReceived()
    {
        _logger.LogInformation(LogBorderTop);
        _logger.LogInformation("║ 🔵 [Gateway] Received job submission request");
        _logger.LogInformation("║ 📡 Client: {ClientIP}", HttpContext.Connection.RemoteIpAddress);
        _logger.LogInformation("║ 🌐 Endpoint: POST /api/v1/jobs/submit");
        _logger.LogInformation(LogBorderBottom);
    }

    private async Task<(string? Body, ActionResult? Error)> ReadRequestBodyAsync()
    {
        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var raw = await reader.ReadToEndAsync();
            _logger.LogDebug("📝 Request body length: {Length} bytes", raw.Length);

            if (string.IsNullOrWhiteSpace(raw))
            {
                _logger.LogWarning("⚠️ Empty request body received");
                return (null, BadRequest(new
                {
                    error = "Empty request body"
                }));
            }

            return (raw, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed reading request body");
            return (null, BadRequest(new
            {
                error = "Unable to read request body",
                ex.Message
            }));
        }
    }

    private (JobDefinition? JobDefinition, ActionResult? Error) DeserializeJobDefinition(string raw)
    {
        try
        {
            var opts = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
            };
            var jobDefinition = JsonSerializer.Deserialize<JobDefinition>(raw, opts);

            if (jobDefinition == null)
            {
                _logger.LogError("❌ Unable to deserialize job definition");
                return (null, BadRequest(new
                {
                    error = "Unable to deserialize job definition"
                }));
            }

            _logger.LogInformation("✅ Job definition deserialized successfully");
            return (jobDefinition, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Deserialization failure for job submission. Raw snippet: {Snippet}",
                raw.Length > 400 ? raw[..400] : raw);
            return (null, BadRequest(new
            {
                error = "Invalid job definition JSON",
                ex.Message
            }));
        }
    }

    private void EnsureJobMetadata(JobDefinition jobDefinition)
    {
        // Allow sink-less SQL jobs
        if (jobDefinition.Source is SqlSourceDefinition && jobDefinition.Sink == null)
        {
            _logger.LogDebug("SQL job without sink accepted (statements define sinks). JobId placeholder will be set if missing.");
        }

        // Ensure metadata basics
        jobDefinition.Metadata ??= new JobMetadata();
        if (string.IsNullOrWhiteSpace(jobDefinition.Metadata.JobId))
            jobDefinition.Metadata.JobId = Guid.NewGuid().ToString();
    }

    private async Task<ActionResult<JobSubmissionResult>> SubmitJobToFlinkAsync(JobDefinition jobDefinition)
    {
        try
        {
            _logger.LogInformation("🚀 Submitting job to Flink cluster...");
            var result = await _flinkJobManager.SubmitJobAsync(jobDefinition);

            if (result.IsSuccess)
            {
                _logger.LogInformation(LogBorderTop);
                _logger.LogInformation("║ ✅ [Gateway] Job submitted successfully");
                _logger.LogInformation("║ 📋 JobId: {JobId}", result.JobId);
                _logger.LogInformation("║ 🆔 FlinkJobId: {FlinkJobId}", result.FlinkJobId);
                _logger.LogInformation("║ 📤 Response: 200 OK");
                _logger.LogInformation(LogBorderBottom);
                return Ok(result);
            }
            else
            {
                _logger.LogError(LogBorderTop);
                _logger.LogError("║ ❌ [Gateway] Job submission failed");
                _logger.LogError("║ 📋 JobId: {JobId}", result.JobId);
                _logger.LogError("║ ⚠️ Error: {ErrorMessage}", result.ErrorMessage);
                _logger.LogError("║ 📤 Response: 400 Bad Request");
                _logger.LogError(LogBorderBottom);
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LogBorderTop);
            _logger.LogError("║ ❌ [Gateway] Exception during job submission");
            _logger.LogError("║ 📋 JobId: {JobId}", jobDefinition.Metadata.JobId);
            _logger.LogError("║ 💥 Exception: {Message}", ex.Message);
            _logger.LogError("║ 📤 Response: 500 Internal Server Error");
            _logger.LogError(LogBorderBottom);
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
