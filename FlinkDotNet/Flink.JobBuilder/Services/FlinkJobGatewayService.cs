using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Flink.JobBuilder.Models;
using Microsoft.Extensions.Logging;
using Serilog;
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
        private static readonly Serilog.ILogger _log = CreateLogger();

        private static Serilog.ILogger CreateLogger()
        {
            var logFilePath = System.Environment.GetEnvironmentVariable("LOG_FILE_PATH") ?? "test-logs";
            var today = System.DateTime.UtcNow.ToString("yyyyMMdd");
            var logFile = System.IO.Path.Combine(logFilePath, $"FlinkDotNet.JobGateway.log.{today}");

            // Clean up old log files (older than 1 day)
            try
            {
                if (System.IO.Directory.Exists(logFilePath))
                {
                    var logFiles = System.IO.Directory.GetFiles(logFilePath, "FlinkDotNet.JobGateway.log.*");
                    foreach (var file in logFiles)
                    {
                        var fileInfo = new System.IO.FileInfo(file);
                        if (fileInfo.LastWriteTimeUtc < System.DateTime.UtcNow.AddDays(-1))
                        {
                            System.IO.File.Delete(file);
                        }
                    }
                }
            }
            catch
            {
                // Ignore cleanup errors
            }

            return new LoggerConfiguration()
                .WriteTo.File(
                    path: logFile,
                    rollingInterval: RollingInterval.Infinite,
                    rollOnFileSizeLimit: false,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    fileSizeLimitBytes: 100_000_000,
                    shared: true)
                .WriteTo.Console()
                .MinimumLevel.Debug()
                .CreateLogger();
        }

        private readonly JsonSerializerOptions _jsonOptions;

        public FlinkJobGatewayService(FlinkJobGatewayConfiguration? configuration = null, HttpClient? httpClient = null, ILogger? logger = null)
        {
            _configuration = configuration ?? new FlinkJobGatewayConfiguration();
            _httpClient = httpClient ?? CreateDefaultHttpClient();
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        private HttpClient CreateDefaultHttpClient()
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(_configuration.BaseUrl),
                Timeout = _configuration.HttpTimeout
            };

            client.DefaultRequestHeaders.Add("User-Agent", "Flink.JobBuilder/1.0.0");

            if (!string.IsNullOrEmpty(_configuration.ApiKey))
            {
                client.DefaultRequestHeaders.Add("X-API-Key", _configuration.ApiKey);
            }

            return client;
        }

        public async Task<JobSubmissionResult> SubmitJobAsync(JobDefinition jobDefinition, CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation("Submitting job {JobId} to Flink Job Gateway at {BaseUrl}", jobDefinition.Metadata.JobId, _configuration.BaseUrl);
            _log.Information("[FlinkJobGatewayService.SubmitJobAsync] Submitting job {JobId} to {BaseUrl}, Source.BootstrapServers={BootstrapServers}",
                jobDefinition.Metadata.JobId, _configuration.BaseUrl, (jobDefinition.Source as KafkaSourceDefinition)?.BootstrapServers);

            var validation = ValidateJobDefinition(jobDefinition);
            if (validation != null)
                return validation;

            var json = SerializeAndLogJobDefinition(jobDefinition);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await ExecuteWithRetryAsync(async () =>
                await _httpClient.PostAsync("/api/v1/jobs/submit", content, cancellationToken));

            return await ProcessSubmissionResponseAsync(jobDefinition, response, cancellationToken);
        }

        private JobSubmissionResult? ValidateJobDefinition(JobDefinition jobDefinition)
        {
            var validation = JobDefinitionValidator.Validate(jobDefinition);
            if (!validation.IsValid)
            {
                var msg = $"Job validation failed: {string.Join(", ", validation.Errors)}";
                _logger?.LogWarning(msg);
                return JobSubmissionResult.CreateFailure(jobDefinition.Metadata.JobId, msg);
            }
            return null;
        }

        private string SerializeAndLogJobDefinition(JobDefinition jobDefinition)
        {
            var json = JsonSerializer.Serialize(jobDefinition, _jsonOptions);
            LogSerializedJob(jobDefinition, json);
            LogBootstrapServersInJson(json);
            return json;
        }

        private void LogSerializedJob(JobDefinition jobDefinition, string json)
        {
            var hasDiscriminatorToken = json.Contains("\"type\"", StringComparison.Ordinal);
            var firstSnippet = json.Length > 500 ? json[..500] + "...(truncated)" : json;
            _logger?.LogInformation(
                "Job {JobId} JSON serialized (length={Length}, hasDiscriminatorToken={HasType}). Snippet: {Snippet}",
                jobDefinition.Metadata.JobId, json.Length, hasDiscriminatorToken, firstSnippet);

            CountDiscriminatorOccurrences(jobDefinition.Metadata.JobId, json);
        }

        private static void LogBootstrapServersInJson(string json)
        {
            _log.Information("[FlinkJobGatewayService.SubmitJobAsync] After JSON serialization, checking bootstrap servers in JSON");
            var bootstrapServersInJson = json.Contains("bootstrapServers", StringComparison.OrdinalIgnoreCase) ||
                                         json.Contains("\"bootstrap", StringComparison.OrdinalIgnoreCase);
            _log.Information("[FlinkJobGatewayService.SubmitJobAsync] JSON contains bootstrap servers reference: {HasBootstrapServers}", bootstrapServersInJson);

            ExtractBootstrapServersFromJson(json);
        }

        private static void ExtractBootstrapServersFromJson(string json)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(json);
                if (jsonDoc.RootElement.TryGetProperty("source", out var sourceElement) &&
                    sourceElement.TryGetProperty("bootstrapServers", out var bootstrapElement))
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
            if (_logger == null)
                return;

            var typeCount = 0;
            var idx = 0;
            while ((idx = json.IndexOf("\"type\"", idx, StringComparison.Ordinal)) >= 0)
            {
                typeCount++;
                idx += 6;
            }
            _logger.LogDebug("Job {JobId} discriminator occurrences: {TypeCount}", jobId, typeCount);
        }

        private async Task<JobSubmissionResult> ProcessSubmissionResponseAsync(
            JobDefinition jobDefinition,
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            var rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode && string.IsNullOrWhiteSpace(rawResponse))
            {
                var errorMsg = "Gateway returned empty response body - this indicates a serialization problem in the Gateway";
                _logger?.LogError(errorMsg);
                return JobSubmissionResult.CreateFailure(jobDefinition.Metadata.JobId, errorMsg);
            }

            var responseSnippet = rawResponse.Length > 600 ? rawResponse[..600] + "...(truncated)" : rawResponse;

            if (response.IsSuccessStatusCode)
                return await HandleSuccessResponseAsync(jobDefinition, rawResponse, responseSnippet);

            return HandleFailureResponse(jobDefinition, response, responseSnippet);
        }

        private async Task<JobSubmissionResult> HandleSuccessResponseAsync(
            JobDefinition jobDefinition,
            string rawResponse,
            string responseSnippet)
        {
            JobSubmissionResult? result = null;
            try
            {
                result = JsonSerializer.Deserialize<JobSubmissionResult>(rawResponse, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Deserialization of JobSubmissionResult failed for Job {JobId}. Raw response snippet: {Snippet}",
                    jobDefinition.Metadata.JobId, responseSnippet);
            }

            if (result != null)
            {
                result.SubmittedAt = DateTime.UtcNow;
                _logger?.LogInformation("Job {JobId} submitted successfully. Flink Job ID: {FlinkJobId}. Raw response snippet: {Snippet}",
                    jobDefinition.Metadata.JobId, result.FlinkJobId, responseSnippet);
                return result;
            }

            _logger?.LogWarning("Job {JobId} submission success status but null result. Raw response snippet: {Snippet}",
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
            string responseSnippet)
        {
            _logger?.LogWarning("Job {JobId} submission failed HTTP {Status}. Raw response snippet: {Snippet}",
                jobDefinition.Metadata.JobId, response.StatusCode, responseSnippet);
            _logger?.LogError("Failed to submit job {JobId}. Status: {StatusCode}",
                jobDefinition.Metadata.JobId, response.StatusCode);

            return new JobSubmissionResult
            {
                JobId = jobDefinition.Metadata.JobId,
                Success = false,
                ErrorMessage = $"HTTP {response.StatusCode}: {responseSnippet}",
                SubmittedAt = DateTime.UtcNow
            };
        }

        public async Task<JobStatus> GetJobStatusAsync(string flinkJobId, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Getting status for job {FlinkJobId}", flinkJobId);

            var response = await ExecuteWithRetryAsync(async () =>
            {
                return await _httpClient.GetAsync($"/api/v1/jobs/{flinkJobId}/status", cancellationToken);
            });

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var status = JsonSerializer.Deserialize<JobStatus>(responseContent, _jsonOptions);

                if (status != null)
                {
                    return status;
                }
            }

            _logger?.LogWarning("Failed to get status for job {FlinkJobId}. Status: {StatusCode}",
                flinkJobId, response.StatusCode);

            return new JobStatus
            {
                FlinkJobId = flinkJobId,
                State = "UNKNOWN",
                ErrorMessage = $"Failed to retrieve status: HTTP {response.StatusCode}"
            };
        }

        public async Task<JobMetrics> GetJobMetricsAsync(string flinkJobId, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Getting metrics for job {FlinkJobId}", flinkJobId);

            var response = await ExecuteWithRetryAsync(async () =>
            {
                return await _httpClient.GetAsync($"/api/v1/jobs/{flinkJobId}/metrics", cancellationToken);
            });

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var metrics = JsonSerializer.Deserialize<JobMetrics>(responseContent, _jsonOptions);

                if (metrics != null)
                {
                    return metrics;
                }
            }

            _logger?.LogWarning("Failed to get metrics for job {FlinkJobId}. Status: {StatusCode}",
                flinkJobId, response.StatusCode);

            return new JobMetrics();
        }

        public async Task<bool> CancelJobAsync(string flinkJobId, CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation("Canceling job {FlinkJobId}", flinkJobId);

            var response = await ExecuteWithRetryAsync(async () =>
            {
                return await _httpClient.PostAsync($"/api/v1/jobs/{flinkJobId}/cancel", null, cancellationToken);
            });

            var success = response.IsSuccessStatusCode;

            if (success)
            {
                _logger?.LogInformation("Job {FlinkJobId} canceled successfully", flinkJobId);
            }
            else
            {
                _logger?.LogError("Failed to cancel job {FlinkJobId}. Status: {StatusCode}",
                    flinkJobId, response.StatusCode);
            }

            return success;
        }

        public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger?.LogDebug("Performing health check on Flink Job Gateway");

                var response = await _httpClient.GetAsync("/api/v1/health", cancellationToken);
                var isHealthy = response.IsSuccessStatusCode;

                _logger?.LogDebug("Health check result: {IsHealthy}", isHealthy);
                return isHealthy;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Health check failed");
                return false;
            }
        }

        private async Task<HttpResponseMessage> ExecuteWithRetryAsync(Func<Task<HttpResponseMessage>> operation)
        {
            var retryCount = 0;
            while (retryCount <= _configuration.MaxRetries)
            {
                try
                {
                    var response = await operation();

                    if (response.IsSuccessStatusCode)
                    {
                        return response;
                    }

                    var shouldRetry = await ShouldRetryResponseAsync(response, retryCount);
                    if (!shouldRetry || retryCount == _configuration.MaxRetries)
                    {
                        return response;
                    }
                }
                catch (Exception ex) when (retryCount < _configuration.MaxRetries)
                {
                    _logger?.LogWarning(ex, "Request failed, retrying ({RetryCount}/{MaxRetries})",
                        retryCount + 1, _configuration.MaxRetries);
                }

                retryCount++;
                await Task.Delay(_configuration.RetryDelay * (retryCount + 1)); // Exponential backoff: 2s, 4s, 6s
            }

            throw new HttpRequestException($"Request failed after {_configuration.MaxRetries} retries");
        }

        private async Task<bool> ShouldRetryResponseAsync(HttpResponseMessage response, int retryCount)
        {
            // Retry on server errors (5xx)
            if ((int) response.StatusCode >= 500)
            {
                return true;
            }

            // For client errors (4xx), only retry on specific conditions
            if ((int) response.StatusCode >= 400 && (int) response.StatusCode < 500)
            {
                return await ShouldRetryClientErrorAsync(response, retryCount);
            }

            return false;
        }

        private async Task<bool> ShouldRetryClientErrorAsync(HttpResponseMessage response, int retryCount)
        {
            // Always retry on 429 (Too Many Requests)
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return true;
            }

            // Retry on 400 (Bad Request) if Flink cluster is not ready
            var shouldRetryFlinkNotReady = await ShouldRetryFlinkClusterNotReadyAsync(response);
            if (shouldRetryFlinkNotReady)
            {
                LogFlinkClusterNotReady(retryCount);
                return true;
            }

            return false;
        }

        private void LogFlinkClusterNotReady(int retryCount)
        {
            if (retryCount < _configuration.MaxRetries)
            {
                var message = $"Flink cluster not ready, retrying ({retryCount + 1}/{_configuration.MaxRetries}) after {_configuration.RetryDelay * (retryCount + 1)}ms";
                _logger?.LogWarning(message);
                _log.Warning("[FlinkJobGatewayService.ExecuteWithRetryAsync] {Message}", message);
            }
        }

        private static async Task<bool> ShouldRetryFlinkClusterNotReadyAsync(HttpResponseMessage response)
        {
            if (response.StatusCode != HttpStatusCode.BadRequest)
                return false;

            try
            {
                var content = await response.Content.ReadAsStringAsync();
                // Check if the error message contains "Flink cluster is not healthy or unreachable"
                return content.Contains("Flink cluster is not healthy", StringComparison.OrdinalIgnoreCase) ||
                       content.Contains("Flink cluster is not healthy or unreachable", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _httpClient?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
