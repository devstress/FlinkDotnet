using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Flink.JobBuilder.Models;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Flink.JobBuilder.Services
{
    /// <summary>
    /// HTTP-based implementation for communicating with Flink Job Gateway
    /// </summary>
    public class FlinkJobGatewayService : IFlinkJobGatewayService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly FlinkJobGatewayConfiguration _configuration;
        private readonly ILogger? _logger;
        private static readonly Serilog.Core.Logger _log = CreateLogger();

        /// <summary>
        /// Gets or sets the delay between retry attempts.
        /// Static field for testability (can be set to 1ms in tests).
        /// </summary>
        public static TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

        private static Serilog.Core.Logger CreateLogger()
        {
            System.IO.Abstractions.FileSystem fileSystem = new();
            return global::FlinkDotNet.Common.Logging.LoggerFactory.CreateLogger(
                fileSystem,
                "FlinkDotNet.JobGateway.log");
        }

        private readonly JsonSerializerOptions _jsonOptions;

        public FlinkJobGatewayService(FlinkJobGatewayConfiguration? configuration = null, HttpClient? httpClient = null, ILogger? logger = null)
        {
            this._configuration = configuration ?? new FlinkJobGatewayConfiguration();
            this._httpClient = httpClient ?? this.CreateDefaultHttpClient();
            this._logger = logger;
            this._jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        private HttpClient CreateDefaultHttpClient()
        {
            _log.Information("[FlinkJobGatewayService.CreateDefaultHttpClient] Creating HttpClient with BaseUrl={BaseUrl}", this._configuration.BaseUrl);

            HttpClient client = new()
            {
                BaseAddress = new Uri(this._configuration.BaseUrl),
                Timeout = this._configuration.HttpTimeout
            };

            client.DefaultRequestHeaders.Add("User-Agent", "Flink.JobBuilder/1.0.0");

            if (!string.IsNullOrEmpty(this._configuration.ApiKey))
            {
                client.DefaultRequestHeaders.Add("X-API-Key", this._configuration.ApiKey);
            }

            _log.Information("[FlinkJobGatewayService.CreateDefaultHttpClient] HttpClient created with BaseAddress={BaseAddress}", client.BaseAddress);
            return client;
        }

        public async Task<JobSubmissionResult> SubmitJobAsync(JobDefinition jobDefinition, CancellationToken cancellationToken = default)
        {
            string targetUrl = new Uri(this._httpClient.BaseAddress!, "/api/v1/jobs/submit").ToString();
            this._logger?.LogInformation("Submitting job {JobId} to Flink Job Gateway at {Url}", jobDefinition.Metadata.JobId, targetUrl);
            _log.Information("[FlinkJobGatewayService.SubmitJobAsync] Submitting job {JobId}, Source.BootstrapServers={BootstrapServers}, TargetUrl={TargetUrl}",
                jobDefinition.Metadata.JobId, (jobDefinition.Source as KafkaSourceDefinition)?.BootstrapServers, targetUrl);

            JobSubmissionResult? validation = this.ValidateJobDefinition(jobDefinition);
            if (validation != null)
            {
                return validation;
            }

            string json = this.SerializeAndLogJobDefinition(jobDefinition);
            StringContent content = new(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await this.ExecuteWithRetryAsync(async () =>
                await this._httpClient.PostAsync("/api/v1/jobs/submit", content, cancellationToken));

            return await this.ProcessSubmissionResponseAsync(jobDefinition, response, targetUrl, cancellationToken);
        }

        private JobSubmissionResult? ValidateJobDefinition(JobDefinition jobDefinition)
        {
            IrValidationResult validation = JobDefinitionValidator.Validate(jobDefinition);
            if (validation.IsValid)
            {
                return null;
            }

            string msg = $"Job validation failed: {string.Join(", ", validation.Errors)}";
            this._logger?.LogWarning("Job validation failed: {ValidationErrors}", msg);
            return JobSubmissionResult.CreateFailure(jobDefinition.Metadata.JobId, msg);
        }

        private string SerializeAndLogJobDefinition(JobDefinition jobDefinition)
        {
            string json = JsonSerializer.Serialize(jobDefinition, this._jsonOptions);
            this.LogSerializedJob(jobDefinition, json);
            LogBootstrapServersInJson(json);
            return json;
        }

        private void LogSerializedJob(JobDefinition jobDefinition, string json)
        {
            bool hasDiscriminatorToken = json.Contains("\"type\"", StringComparison.Ordinal);
            string firstSnippet = json.Length > 500 ? json[..500] + "...(truncated)" : json;
            this._logger?.LogInformation(
                "Job {JobId} JSON serialized (length={Length}, hasDiscriminatorToken={HasType}). Snippet: {Snippet}",
                jobDefinition.Metadata.JobId, json.Length, hasDiscriminatorToken, firstSnippet);

            this.CountDiscriminatorOccurrences(jobDefinition.Metadata.JobId, json);
        }

        private static void LogBootstrapServersInJson(string json)
        {
            _log.Information("[FlinkJobGatewayService.SubmitJobAsync] After JSON serialization, checking bootstrap servers in JSON");
            bool bootstrapServersInJson = json.Contains("bootstrapServers", StringComparison.OrdinalIgnoreCase) ||
                                         json.Contains("\"bootstrap", StringComparison.OrdinalIgnoreCase);
            _log.Information("[FlinkJobGatewayService.SubmitJobAsync] JSON contains bootstrap servers reference: {HasBootstrapServers}", bootstrapServersInJson);

            ExtractBootstrapServersFromJson(json);
        }

        private static void ExtractBootstrapServersFromJson(string json)
        {
            try
            {
                JsonDocument jsonDoc = JsonDocument.Parse(json);
                if (jsonDoc.RootElement.TryGetProperty("source", out JsonElement sourceElement) &&
                    sourceElement.TryGetProperty("bootstrapServers", out JsonElement bootstrapElement))
                {
                    _log.Information("[FlinkJobGatewayService.SubmitJobAsync] Bootstrap servers in JSON: {BootstrapServers}",
                        bootstrapElement.GetString());
                }
            }
            catch (Exception ex)
            {
                _log.Warning(ex, "[FlinkJobGatewayService.SubmitJobAsync] Failed to parse bootstrap servers from JSON");
            }
        }

        private void CountDiscriminatorOccurrences(string jobId, string json)
        {
            if (this._logger == null)
            {
                return;
            }

            int typeCount = 0;
            int idx = 0;
            while ((idx = json.IndexOf("\"type\"", idx, StringComparison.Ordinal)) >= 0)
            {
                typeCount++;
                idx += 6;
            }
            this._logger.LogDebug("Job {JobId} discriminator occurrences: {TypeCount}", jobId, typeCount);
        }

        private async Task<JobSubmissionResult> ProcessSubmissionResponseAsync(
            JobDefinition jobDefinition,
            HttpResponseMessage response,
            string targetUrl,
            CancellationToken cancellationToken)
        {
            string rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode && string.IsNullOrWhiteSpace(rawResponse))
            {
                string errorMsg = $"Gateway returned empty response body from {targetUrl} - this indicates a serialization problem in the Gateway";
                this._logger?.LogError("Gateway returned empty response body from {TargetUrl} - this indicates a serialization problem in the Gateway", targetUrl);
                return JobSubmissionResult.CreateFailure(jobDefinition.Metadata.JobId, errorMsg);
            }

            string responseSnippet = rawResponse.Length > 600 ? rawResponse[..600] + "...(truncated)" : rawResponse;

            return response.IsSuccessStatusCode
                ? await this.HandleSuccessResponseAsync(jobDefinition, rawResponse, responseSnippet)
                : this.HandleFailureResponse(jobDefinition, response, responseSnippet, targetUrl);
        }

        private async Task<JobSubmissionResult> HandleSuccessResponseAsync(
            JobDefinition jobDefinition,
            string rawResponse,
            string responseSnippet)
        {
            JobSubmissionResult? result = null;
            try
            {
                result = JsonSerializer.Deserialize<JobSubmissionResult>(rawResponse, this._jsonOptions);
            }
            catch (Exception ex)
            {
                this._logger?.LogError(ex, "Deserialization of JobSubmissionResult failed for Job {JobId}. Raw response snippet: {Snippet}",
                    jobDefinition.Metadata.JobId, responseSnippet);
            }

            if (result != null)
            {
                result.SubmittedAt = DateTime.UtcNow;
                this._logger?.LogInformation("Job {JobId} submitted successfully. Flink Job ID: {FlinkJobId}. Raw response snippet: {Snippet}",
                    jobDefinition.Metadata.JobId, result.FlinkJobId, responseSnippet);
                return result;
            }

            this._logger?.LogWarning("Job {JobId} submission success status but null result. Raw response snippet: {Snippet}",
                jobDefinition.Metadata.JobId, responseSnippet);

            return await Task.FromResult(new JobSubmissionResult
            {
                JobId = jobDefinition.Metadata.JobId,
                Success = false,
                ErrorMessage = "Deserialization failed",
                SubmittedAt = DateTime.UtcNow
            });
        }

        private JobSubmissionResult HandleFailureResponse(
            JobDefinition jobDefinition,
            HttpResponseMessage response,
            string responseSnippet,
            string targetUrl)
        {
            this._logger?.LogWarning("Job {JobId} submission failed HTTP {Status} to {Url}. Raw response snippet: {Snippet}",
                jobDefinition.Metadata.JobId, response.StatusCode, targetUrl, responseSnippet);
            this._logger?.LogError("Failed to submit job {JobId}. Status: {StatusCode}, URL: {Url}",
                jobDefinition.Metadata.JobId, response.StatusCode, targetUrl);

            return new JobSubmissionResult
            {
                JobId = jobDefinition.Metadata.JobId,
                Success = false,
                ErrorMessage = $"HTTP {response.StatusCode} from {targetUrl}: {responseSnippet}",
                SubmittedAt = DateTime.UtcNow
            };
        }

        public async Task<JobStatus> GetJobStatusAsync(string flinkJobId, CancellationToken cancellationToken = default)
        {
            this._logger?.LogDebug("Getting status for job {FlinkJobId}", flinkJobId);

            HttpResponseMessage response = await this.ExecuteWithRetryAsync(async () => await this._httpClient.GetAsync($"/api/v1/jobs/{flinkJobId}/status", cancellationToken));

            if (!response.IsSuccessStatusCode)
            {
                this._logger?.LogWarning("Failed to get status for job {FlinkJobId}. Status: {StatusCode}",
                    flinkJobId, response.StatusCode);

                return new JobStatus
                {
                    FlinkJobId = flinkJobId,
                    State = "UNKNOWN",
                    ErrorMessage = $"Failed to retrieve status: HTTP {response.StatusCode}"
                };
            }

            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            JobStatus? status = JsonSerializer.Deserialize<JobStatus>(responseContent, this._jsonOptions);

            if (status != null)
            {
                return status;
            }

            this._logger?.LogWarning("Failed to deserialize status for job {FlinkJobId}", flinkJobId);
            return new JobStatus
            {
                FlinkJobId = flinkJobId,
                State = "UNKNOWN",
                ErrorMessage = "Failed to deserialize status"
            };
        }

        public async Task<JobMetrics> GetJobMetricsAsync(string flinkJobId, CancellationToken cancellationToken = default)
        {
            this._logger?.LogDebug("Getting metrics for job {FlinkJobId}", flinkJobId);

            HttpResponseMessage response = await this.ExecuteWithRetryAsync(async () => await this._httpClient.GetAsync($"/api/v1/jobs/{flinkJobId}/metrics", cancellationToken));

            if (!response.IsSuccessStatusCode)
            {
                this._logger?.LogWarning("Failed to get metrics for job {FlinkJobId}. Status: {StatusCode}",
                    flinkJobId, response.StatusCode);

                return new JobMetrics();
            }

            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            JobMetrics? metrics = JsonSerializer.Deserialize<JobMetrics>(responseContent, this._jsonOptions);

            if (metrics != null)
            {
                return metrics;
            }

            this._logger?.LogWarning("Failed to deserialize metrics for job {FlinkJobId}", flinkJobId);
            return new JobMetrics();
        }

        public async Task<bool> CancelJobAsync(string flinkJobId, CancellationToken cancellationToken = default)
        {
            this._logger?.LogInformation("Canceling job {FlinkJobId}", flinkJobId);

            HttpResponseMessage response = await this.ExecuteWithRetryAsync(async () => await this._httpClient.PostAsync($"/api/v1/jobs/{flinkJobId}/cancel", null, cancellationToken));

            bool success = response.IsSuccessStatusCode;

            if (success)
            {
                this._logger?.LogInformation("Job {FlinkJobId} canceled successfully", flinkJobId);
            }
            else
            {
                this._logger?.LogError("Failed to cancel job {FlinkJobId}. Status: {StatusCode}",
                    flinkJobId, response.StatusCode);
            }

            return success;
        }

        public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                this._logger?.LogDebug("Performing health check on Flink Job Gateway");

                HttpResponseMessage response = await this._httpClient.GetAsync("/api/v1/health", cancellationToken);
                bool isHealthy = response.IsSuccessStatusCode;

                this._logger?.LogDebug("Health check result: {IsHealthy}", isHealthy);
                return isHealthy;
            }
            catch (Exception ex)
            {
                this._logger?.LogError(ex, "Health check failed");
                return false;
            }
        }

        private async Task<HttpResponseMessage> ExecuteWithRetryAsync(Func<Task<HttpResponseMessage>> operation)
        {
            int retryCount = 0;
            while (retryCount <= this._configuration.MaxRetries)
            {
                try
                {
                    HttpResponseMessage response = await operation();

                    if (response.IsSuccessStatusCode)
                    {
                        return response;
                    }

                    bool shouldRetry = await this.ShouldRetryResponseAsync(response, retryCount);
                    if (!shouldRetry || retryCount == this._configuration.MaxRetries)
                    {
                        return response;
                    }
                }
                catch (Exception ex) when (retryCount < this._configuration.MaxRetries)
                {
                    this._logger?.LogWarning(ex, "Request failed, retrying ({RetryCount}/{MaxRetries})",
                        retryCount + 1, this._configuration.MaxRetries);
                }

                retryCount++;
                await Task.Delay(RetryDelay * (retryCount + 1)); // Exponential backoff: uses static RetryDelay (default 1s, configurable for tests)
            }

            throw new HttpRequestException($"Request failed after {this._configuration.MaxRetries} retries");
        }

        private async Task<bool> ShouldRetryResponseAsync(HttpResponseMessage response, int retryCount)
        {
            // Retry on server errors (5xx)
            if ((int) response.StatusCode >= 500)
            {
                return true;
            }

            // For client errors (4xx), only retry on specific conditions
            return !((int)response.StatusCode < 400 || (int)response.StatusCode >= 500)
                && await this.ShouldRetryClientErrorAsync(response, retryCount);
        }

        private async Task<bool> ShouldRetryClientErrorAsync(HttpResponseMessage response, int retryCount)
        {
            // Always retry on 429 (Too Many Requests)
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return true;
            }

            // Retry on 400 (Bad Request) if Flink cluster is not ready
            bool shouldRetryFlinkNotReady = await ShouldRetryFlinkClusterNotReadyAsync(response);
            if (!shouldRetryFlinkNotReady)
            {
                return false;
            }

            this.LogFlinkClusterNotReady(retryCount);
            return true;
        }

        private void LogFlinkClusterNotReady(int retryCount)
        {
            if (retryCount >= this._configuration.MaxRetries)
            {
                return;
            }

            this._logger?.LogWarning(
                "Flink cluster not ready, retrying ({RetryAttempt}/{MaxRetries}) after {DelayMs}ms",
                retryCount + 1,
                this._configuration.MaxRetries,
                this._configuration.RetryDelay * (retryCount + 1));
        }

        private static async Task<bool> ShouldRetryFlinkClusterNotReadyAsync(HttpResponseMessage response)
        {
            if (response.StatusCode != HttpStatusCode.BadRequest)
            {
                return false;
            }

            try
            {
                string content = await response.Content.ReadAsStringAsync();
                // Check if the error message contains "Flink cluster is not healthy or unreachable"
                return content.Contains("Flink cluster is not healthy", StringComparison.OrdinalIgnoreCase) ||
                       content.Contains("Flink cluster is not healthy or unreachable", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private bool _disposed;

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this._disposed)
            {
                return;
            }

            if (disposing)
            {
                this._httpClient?.Dispose();
            }

            this._disposed = true;
        }
    }
}
