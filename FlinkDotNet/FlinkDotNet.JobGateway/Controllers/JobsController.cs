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
public class JobsController(ILogger<JobsController> logger, IFlinkJobManager flinkJobManager) : ControllerBase
{
    private const string LogBorderTop = "╔══════════════════════════════════════════════════════════════";
    private const string LogBorderBottom = "╚══════════════════════════════════════════════════════════════";

    private readonly ILogger<JobsController> _logger = logger;
    private readonly IFlinkJobManager _flinkJobManager = flinkJobManager;

    /// <summary>
    /// Submit a job to the Flink cluster. The request body must contain a JobDefinition JSON payload.
    /// </summary>
    /// <returns>Job submission result</returns>
    [HttpPost("submit")]
    public async Task<ActionResult<JobSubmissionResult>> SubmitJob()
    {
        this.LogRequestReceived();

        var requestBodyResult = await this.ReadRequestBodyAsync();
        if (requestBodyResult.Error != null)
        {
            return requestBodyResult.Error;
        }

        var jobDefResult = this.DeserializeJobDefinition(requestBodyResult.Body!);
        if (jobDefResult.Error != null)
        {
            return jobDefResult.Error;
        }

        var jobDefinition = jobDefResult.JobDefinition!;
        this.EnsureJobMetadata(jobDefinition);

        this._logger.LogInformation("📋 Job metadata: JobId={JobId}, JobName={JobName}",
            jobDefinition.Metadata.JobId,
            jobDefinition.Metadata.JobName ?? "Unnamed");

        return await this.SubmitJobToFlinkAsync(jobDefinition);
    }

    private void LogRequestReceived()
    {
        this._logger.LogInformation(LogBorderTop);
        this._logger.LogInformation("║ 🔵 [Gateway] Received job submission request");
        this._logger.LogInformation("║ 📡 Client: {ClientIP}", this.HttpContext.Connection.RemoteIpAddress);
        this._logger.LogInformation("║ 🌐 Endpoint: POST /api/v1/jobs/submit");
        this._logger.LogInformation(LogBorderBottom);
    }

    private async Task<(string? Body, ActionResult? Error)> ReadRequestBodyAsync()
    {
        try
        {
            using var reader = new StreamReader(this.Request.Body, Encoding.UTF8);
            var raw = await reader.ReadToEndAsync();
            this._logger.LogDebug("📝 Request body length: {Length} bytes", raw.Length);

            if (string.IsNullOrWhiteSpace(raw))
            {
                this._logger.LogWarning("⚠️ Empty request body received");
                return (null, this.BadRequest(new
                {
                    error = "Empty request body"
                }));
            }

            return (raw, null);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "❌ Failed reading request body");
            return (null, this.BadRequest(new
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
                this._logger.LogError("❌ Unable to deserialize job definition");
                return (null, this.BadRequest(new
                {
                    error = "Unable to deserialize job definition"
                }));
            }

            this._logger.LogInformation("✅ Job definition deserialized successfully");
            return (jobDefinition, null);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "❌ Deserialization failure for job submission. Raw snippet: {Snippet}",
                raw.Length > 400 ? raw[..400] : raw);
            return (null, this.BadRequest(new
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
            this._logger.LogDebug("SQL job without sink accepted (statements define sinks). JobId placeholder will be set if missing.");
        }

        // Ensure metadata basics
        jobDefinition.Metadata ??= new JobMetadata();
        if (string.IsNullOrWhiteSpace(jobDefinition.Metadata.JobId))
        {
            jobDefinition.Metadata.JobId = Guid.NewGuid().ToString();
        }
    }

    private async Task<ActionResult<JobSubmissionResult>> SubmitJobToFlinkAsync(JobDefinition jobDefinition)
    {
        try
        {
            this._logger.LogInformation("🚀 Submitting job to Flink cluster...");
            var result = await this._flinkJobManager.SubmitJobAsync(jobDefinition);

            if (result.IsSuccess)
            {
                this._logger.LogInformation(LogBorderTop);
                this._logger.LogInformation("║ ✅ [Gateway] Job submitted successfully");
                this._logger.LogInformation("║ 📋 JobId: {JobId}", result.JobId);
                this._logger.LogInformation("║ 🆔 FlinkJobId: {FlinkJobId}", result.FlinkJobId);
                this._logger.LogInformation("║ 📤 Response: 200 OK");
                this._logger.LogInformation(LogBorderBottom);
                return this.Ok(result);
            }
            else
            {
                this._logger.LogError(LogBorderTop);
                this._logger.LogError("║ ❌ [Gateway] Job submission failed");
                this._logger.LogError("║ 📋 JobId: {JobId}", result.JobId);
                this._logger.LogError("║ ⚠️ Error: {ErrorMessage}", result.ErrorMessage);
                this._logger.LogError("║ 📤 Response: 400 Bad Request");
                this._logger.LogError(LogBorderBottom);
                return this.BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, LogBorderTop);
            this._logger.LogError("║ ❌ [Gateway] Exception during job submission");
            this._logger.LogError("║ 📋 JobId: {JobId}", jobDefinition.Metadata.JobId);
            this._logger.LogError("║ 💥 Exception: {Message}", ex.Message);
            this._logger.LogError("║ 📤 Response: 500 Internal Server Error");
            this._logger.LogError(LogBorderBottom);
            var result = JobSubmissionResult.CreateFailure(
                jobDefinition.Metadata.JobId,
                $"Internal server error: {ex.Message}");
            return this.StatusCode(500, result);
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
        this._logger.LogInformation("Retrieving status for job: {FlinkJobId}", flinkJobId);

        try
        {
            var status = await this._flinkJobManager.GetJobStatusAsync(flinkJobId);
            return status != null ? this.Ok(status) : this.NotFound();
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error retrieving job status: {Message}", ex.Message);
            return this.StatusCode(500);
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
        this._logger.LogInformation("Retrieving metrics for job: {FlinkJobId}", flinkJobId);

        try
        {
            var metrics = await this._flinkJobManager.GetJobMetricsAsync(flinkJobId);
            return metrics != null ? this.Ok(metrics) : this.NotFound();
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error retrieving job metrics: {Message}", ex.Message);
            return this.StatusCode(500);
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
        this._logger.LogInformation("Canceling job: {FlinkJobId}", flinkJobId);

        try
        {
            var canceled = await this._flinkJobManager.CancelJobAsync(flinkJobId);
            return canceled ? this.Ok() : this.NotFound();
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error canceling job: {Message}", ex.Message);
            return this.StatusCode(500);
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet("health")]
    public ActionResult<string> HealthCheck() => this.Ok("OK");
}
