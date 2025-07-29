using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Flink.JobBuilder.Models;

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
            _logger?.LogInformation("Submitting job {JobId} to Flink Job Gateway", jobDefinition.Metadata.JobId);

            var json = JsonSerializer.Serialize(jobDefinition, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await ExecuteWithRetryAsync(async () =>
            {
                return await _httpClient.PostAsync("/api/v1/jobs/submit", content, cancellationToken);
            });

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<JobSubmissionResult>(responseContent, _jsonOptions);
                
                if (result != null)
                {
                    result.SubmittedAt = DateTime.UtcNow;
                    _logger?.LogInformation("Job {JobId} submitted successfully. Flink Job ID: {FlinkJobId}", 
                        jobDefinition.Metadata.JobId, result.FlinkJobId);
                    return result;
                }
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger?.LogError("Failed to submit job {JobId}. Status: {StatusCode}, Error: {Error}", 
                jobDefinition.Metadata.JobId, response.StatusCode, errorContent);

            return new JobSubmissionResult
            {
                JobId = jobDefinition.Metadata.JobId,
                Success = false,
                ErrorMessage = $"HTTP {response.StatusCode}: {errorContent}",
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
                    
                    // Don't retry on client errors (4xx) except 429 (Too Many Requests)
                    if (response.IsSuccessStatusCode || 
                        ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500 && response.StatusCode != HttpStatusCode.TooManyRequests))
                    {
                        return response;
                    }

                    if (retryCount == _configuration.MaxRetries)
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
                await Task.Delay(_configuration.RetryDelay * retryCount); // Exponential backoff
            }

            throw new HttpRequestException($"Request failed after {_configuration.MaxRetries} retries");
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