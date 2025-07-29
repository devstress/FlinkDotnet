using System;
using System.Threading;
using System.Threading.Tasks;

namespace Flink.JobBuilder.Backpressure;

/// <summary>
/// JobManager coordinator interface for distributed rate limiting.
/// Enables coordination of rate limits across Flink JobManager instances.
/// 
/// Based on Flink's AsyncSink coordination patterns for distributed rate limiting.
/// </summary>
public interface IJobManagerRateLimiterCoordinator : IDisposable
{
    /// <summary>
    /// Coordinates rate limit updates across JobManager instances.
    /// </summary>
    /// <param name="rateLimiterId">Unique identifier for the rate limiter</param>
    /// <param name="newRateLimit">New rate limit to coordinate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CoordinateRateLimitAsync(string rateLimiterId, double newRateLimit, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Registers this rate limiter instance with the JobManager.
    /// </summary>
    /// <param name="rateLimiterId">Unique identifier for the rate limiter</param>
    /// <param name="onRateLimitUpdated">Callback when rate limit is updated by JobManager</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RegisterRateLimiterAsync(string rateLimiterId, Action<double> onRateLimitUpdated, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Unregisters this rate limiter instance from the JobManager.
    /// </summary>
    /// <param name="rateLimiterId">Unique identifier for the rate limiter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UnregisterRateLimiterAsync(string rateLimiterId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the current coordinated rate limit from JobManager.
    /// </summary>
    /// <param name="rateLimiterId">Unique identifier for the rate limiter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<double> GetCoordinatedRateLimitAsync(string rateLimiterId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Reports current utilization to JobManager for adaptive rate limiting.
    /// </summary>
    /// <param name="rateLimiterId">Unique identifier for the rate limiter</param>
    /// <param name="utilization">Current utilization (0.0 to 1.0)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ReportUtilizationAsync(string rateLimiterId, double utilization, CancellationToken cancellationToken = default);
}

/// <summary>
/// Default JobManager coordinator implementation that provides local coordination only.
/// For full distributed coordination, use KafkaJobManagerRateLimiterCoordinator.
/// </summary>
public class LocalJobManagerRateLimiterCoordinator : IJobManagerRateLimiterCoordinator
{
    private volatile bool _disposed;
    
    public Task CoordinateRateLimitAsync(string rateLimiterId, double newRateLimit, CancellationToken cancellationToken = default)
    {
        // Local implementation - no coordination needed
        return Task.CompletedTask;
    }
    
    public Task RegisterRateLimiterAsync(string rateLimiterId, Action<double> onRateLimitUpdated, CancellationToken cancellationToken = default)
    {
        // Local implementation - no registration needed
        return Task.CompletedTask;
    }
    
    public Task UnregisterRateLimiterAsync(string rateLimiterId, CancellationToken cancellationToken = default)
    {
        // Local implementation - no unregistration needed
        return Task.CompletedTask;
    }
    
    public Task<double> GetCoordinatedRateLimitAsync(string rateLimiterId, CancellationToken cancellationToken = default)
    {
        // Local implementation - return default rate limit
        return Task.FromResult(double.MaxValue);
    }
    
    public Task ReportUtilizationAsync(string rateLimiterId, double utilization, CancellationToken cancellationToken = default)
    {
        // Local implementation - no reporting needed
        return Task.CompletedTask;
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;
    }
}