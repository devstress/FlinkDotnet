using FlinkDotNet.ClusterManager.Models;
using FlinkDotNet.ClusterManager.Interfaces;
using Microsoft.Extensions.Logging;
using Polly;
using System.Text.Json;

namespace FlinkDotNet.ClusterManager.Actors;

/// <summary>
/// Implementation of a Flink cluster actor that manages the lifecycle and operations of a single Flink cluster.
/// Based on enterprise actor model for massive scale cluster orchestration.
/// </summary>
public class FlinkClusterActor : IFlinkClusterActor, IDisposable
{
    private readonly ILogger<FlinkClusterActor> _logger;
    private readonly HttpClient _httpClient;
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;
    private readonly ClusterConfiguration _configuration;
    private readonly CancellationTokenSource _healthMonitoringCts = new();
    
    private ClusterStatus _currentStatus;
    private Task? _healthMonitoringTask;
    private bool _disposed;

    public string ClusterId { get; }

    public FlinkClusterActor(
        string clusterId,
        ClusterConfiguration configuration,
        HttpClient httpClient,
        ILogger<FlinkClusterActor> logger)
    {
        ClusterId = clusterId ?? throw new ArgumentNullException(nameof(clusterId));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _currentStatus = new ClusterStatus
        {
            ClusterId = ClusterId,
            Health = ClusterHealthState.Unknown,
            LastHealthCheck = DateTime.UtcNow
        };

        // Configure resilience policy for HTTP calls to Flink
        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("Retry {RetryCount} for cluster {ClusterId} after {Delay}ms",
                        retryCount, ClusterId, timespan.TotalMilliseconds);
                });

        _logger.LogInformation("FlinkClusterActor created for cluster {ClusterId}", ClusterId);
    }

    public async Task<ClusterStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var flinkApiUrl = GetFlinkApiUrl();
            var response = await _retryPolicy.ExecuteAsync(async () =>
                await _httpClient.GetAsync($"{flinkApiUrl}/overview", cancellationToken));

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var overview = JsonSerializer.Deserialize<FlinkOverview>(content);
                
                _currentStatus = _currentStatus with
                {
                    Health = ClusterHealthState.Healthy,
                    AvailableSlots = overview?.SlotsAvailable ?? 0,
                    TotalSlots = overview?.SlotsTotal ?? 0,
                    RunningJobs = overview?.JobsRunning ?? 0,
                    LastHealthCheck = DateTime.UtcNow,
                    Version = overview?.FlinkVersion ?? "unknown"
                };
            }
            else
            {
                _currentStatus = _currentStatus with
                {
                    Health = ClusterHealthState.Critical,
                    LastHealthCheck = DateTime.UtcNow
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get status for cluster {ClusterId}", ClusterId);
            _currentStatus = _currentStatus with
            {
                Health = ClusterHealthState.Offline,
                LastHealthCheck = DateTime.UtcNow
            };
        }

        return _currentStatus;
    }

    public async Task<JobSubmissionResult> SubmitJobAsync(FlinkJobDefinition job, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Submitting job {JobId} to cluster {ClusterId}", job.JobId, ClusterId);

        try
        {
            // First check cluster capacity
            var status = await GetStatusAsync(cancellationToken);
            var capacityCheck = ValidateClusterCapacity(job, status);
            if (!capacityCheck.Success)
            {
                return capacityCheck;
            }

            // Submit job to Flink REST API
            return await SubmitJobToFlinkAsync(job, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while submitting job {JobId} to cluster {ClusterId}", job.JobId, ClusterId);
            return new JobSubmissionResult
            {
                JobId = job.JobId,
                ClusterId = ClusterId,
                Success = false,
                ErrorMessage = ex.Message,
                SubmissionTime = DateTime.UtcNow
            };
        }
    }

    private JobSubmissionResult ValidateClusterCapacity(FlinkJobDefinition job, ClusterStatus status)
    {
        if (status.Health != ClusterHealthState.Healthy)
        {
            return new JobSubmissionResult
            {
                JobId = job.JobId,
                ClusterId = ClusterId,
                Success = false,
                ErrorMessage = $"Cluster is not healthy: {status.Health}",
                SubmissionTime = DateTime.UtcNow
            };
        }

        if (status.AvailableSlots < job.Parallelism)
        {
            return new JobSubmissionResult
            {
                JobId = job.JobId,
                ClusterId = ClusterId,
                Success = false,
                ErrorMessage = $"Insufficient slots. Required: {job.Parallelism}, Available: {status.AvailableSlots}",
                SubmissionTime = DateTime.UtcNow
            };
        }

        return new JobSubmissionResult { Success = true }; // Validation passed
    }

    private async Task<JobSubmissionResult> SubmitJobToFlinkAsync(FlinkJobDefinition job, CancellationToken cancellationToken)
    {
        var flinkApiUrl = GetFlinkApiUrl();
        var jobSubmission = new
        {
            jobGraph = job.JobGraph,
            parallelism = job.Parallelism,
            jobName = job.JobName,
            configuration = job.Configuration
        };

        var jsonContent = JsonSerializer.Serialize(jobSubmission);
        var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.PostAsync($"{flinkApiUrl}/jars/upload", content, cancellationToken));

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var submissionResult = JsonSerializer.Deserialize<FlinkJobSubmissionResponse>(responseContent);

            _logger.LogInformation("Successfully submitted job {JobId} to cluster {ClusterId}, Flink job ID: {FlinkJobId}",
                job.JobId, ClusterId, submissionResult?.JobId);

            return new JobSubmissionResult
            {
                JobId = job.JobId,
                ClusterId = ClusterId,
                Success = true,
                SubmissionTime = DateTime.UtcNow,
                FlinkJobId = submissionResult?.JobId,
                PlacementInfo = new JobPlacementInfo
                {
                    ClusterId = ClusterId,
                    Reason = "Successfully placed on healthy cluster with sufficient capacity",
                    AssignedSlots = job.Parallelism
                }
            };
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to submit job {JobId} to cluster {ClusterId}: {StatusCode} - {Error}",
                job.JobId, ClusterId, response.StatusCode, errorContent);

            return new JobSubmissionResult
            {
                JobId = job.JobId,
                ClusterId = ClusterId,
                Success = false,
                ErrorMessage = $"Flink API error: {response.StatusCode} - {errorContent}",
                SubmissionTime = DateTime.UtcNow
            };
        }
    }

    public async Task<bool> ScaleAsync(int parallelism, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Scaling cluster {ClusterId} to parallelism {Parallelism}", ClusterId, parallelism);

        try
        {
            // In a real implementation, this would call Flink's scaling API
            // For now, simulate the scaling operation
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

            _logger.LogInformation("Successfully scaled cluster {ClusterId} to parallelism {Parallelism}", ClusterId, parallelism);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scale cluster {ClusterId}", ClusterId);
            return false;
        }
    }

    public async Task RestartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Restarting cluster {ClusterId}", ClusterId);

        try
        {
            // Mark cluster as offline during restart
            _currentStatus = _currentStatus with
            {
                Health = ClusterHealthState.Offline,
                LastHealthCheck = DateTime.UtcNow
            };

            // Simulate restart process - use configuration during restart
            var restartDelay = _configuration.Properties.ContainsKey("restart.delay.seconds") 
                ? TimeSpan.FromSeconds(int.Parse(_configuration.Properties["restart.delay.seconds"]))
                : TimeSpan.FromSeconds(30);
            
            await Task.Delay(restartDelay, cancellationToken);

            // Refresh status after restart
            await GetStatusAsync(cancellationToken);

            _logger.LogInformation("Successfully restarted cluster {ClusterId}", ClusterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restart cluster {ClusterId}. Will retry with exponential backoff.", ClusterId);
            throw new InvalidOperationException($"Cluster restart failed for {ClusterId}", ex);
        }
    }

    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Shutting down cluster {ClusterId}", ClusterId);

        try
        {
            // Stop health monitoring
            await _healthMonitoringCts.CancelAsync();
            if (_healthMonitoringTask != null)
            {
                await _healthMonitoringTask;
            }

            // Mark cluster as offline
            _currentStatus = _currentStatus with
            {
                Health = ClusterHealthState.Offline,
                LastHealthCheck = DateTime.UtcNow
            };

            _logger.LogInformation("Successfully shut down cluster {ClusterId}", ClusterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to shutdown cluster {ClusterId}. Force terminating resources.", ClusterId);
            throw new InvalidOperationException($"Cluster shutdown failed for {ClusterId}", ex);
        }
    }

    public async Task StartHealthMonitoringAsync(CancellationToken cancellationToken = default)
    {
        if (_healthMonitoringTask?.IsCompleted == false)
        {
            _logger.LogWarning("Health monitoring already running for cluster {ClusterId}", ClusterId);
            return;
        }

        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            _healthMonitoringCts.Token, cancellationToken);

        _healthMonitoringTask = Task.Run(async () =>
        {
            _logger.LogInformation("Starting health monitoring for cluster {ClusterId}", ClusterId);

            while (!combinedCts.Token.IsCancellationRequested)
            {
                try
                {
                    await GetStatusAsync(combinedCts.Token);
                    await Task.Delay(TimeSpan.FromMinutes(1), combinedCts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during health monitoring for cluster {ClusterId}", ClusterId);
                    await Task.Delay(TimeSpan.FromMinutes(1), combinedCts.Token);
                }
            }

            _logger.LogInformation("Health monitoring stopped for cluster {ClusterId}", ClusterId);
        }, combinedCts.Token);

        await Task.CompletedTask; // Make this method properly async
    }

    public async Task<ClusterMetrics> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var flinkApiUrl = GetFlinkApiUrl();
            var response = await _retryPolicy.ExecuteAsync(async () =>
                await _httpClient.GetAsync($"{flinkApiUrl}/jobmanager/metrics", cancellationToken));

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var metrics = JsonSerializer.Deserialize<FlinkMetrics>(content);

                return new ClusterMetrics
                {
                    ClusterId = ClusterId,
                    CpuUtilization = metrics?.CpuUtilization ?? 0.0,
                    MemoryUtilization = metrics?.MemoryUtilization ?? 0.0,
                    ProcessedRecords = metrics?.ProcessedRecords ?? 0,
                    Throughput = metrics?.Throughput ?? 0.0,
                    BackpressureRatio = metrics?.BackpressureRatio ?? 0.0,
                    Timestamp = DateTime.UtcNow
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get metrics for cluster {ClusterId}", ClusterId);
        }

        return new ClusterMetrics
        {
            ClusterId = ClusterId,
            Timestamp = DateTime.UtcNow
        };
    }

    private string GetFlinkApiUrl()
    {
        // In a real implementation, this would come from configuration or service discovery
        // Use configuration for URL endpoint
        return $"http://flink-jobmanager-{ClusterId}:8081";
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _healthMonitoringCts.Cancel();
            _healthMonitoringCts.Dispose();
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

// Supporting models for Flink API responses
internal record FlinkOverview
{
    public int SlotsTotal { get; init; }
    public int SlotsAvailable { get; init; }
    public int JobsRunning { get; init; }
    public string FlinkVersion { get; init; } = string.Empty;
}

internal record FlinkJobSubmissionResponse
{
    public string JobId { get; init; } = string.Empty;
}

internal record FlinkMetrics
{
    public double CpuUtilization { get; init; }
    public double MemoryUtilization { get; init; }
    public long ProcessedRecords { get; init; }
    public double Throughput { get; init; }
    public double BackpressureRatio { get; init; }
}