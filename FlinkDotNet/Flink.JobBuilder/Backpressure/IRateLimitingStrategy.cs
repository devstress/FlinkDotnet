using System;
using System.Threading;
using System.Threading.Tasks;

namespace Flink.JobBuilder.Backpressure;

/// <summary>
/// Rate limiting strategy interface following Apache Flink 2.0 AsyncSink patterns.
/// Implements custom RateLimitingStrategy for optimizing throughput of async sinks.
/// 
/// Based on: https://flink.apache.org/2022/11/25/optimising-the-throughput-of-async-sinks-using-a-custom-ratelimitingstrategy/
/// </summary>
public interface IRateLimitingStrategy
{
    /// <summary>
    /// Attempts to acquire permission to proceed with the operation.
    /// Returns true if permission is granted, false if rate limited.
    /// </summary>
    /// <param name="permits">Number of permits to acquire (default: 1)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if operation can proceed, false if rate limited</returns>
    Task<bool> TryAcquireAsync(int permits = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronous version of TryAcquireAsync for Flink JobManager compatibility.
    /// When jobs are submitted to Flink JobManager, async patterns may not work correctly
    /// due to serialization and execution context changes. Use this method instead.
    /// </summary>
    /// <param name="permits">Number of permits to acquire (default: 1)</param>
    /// <returns>True if operation can proceed, false if rate limited</returns>
    bool TryAcquire(int permits = 1);

    /// <summary>
    /// Acquires permission to proceed, waiting if necessary.
    /// Implements backpressure by blocking until permits are available.
    /// </summary>
    /// <param name="permits">Number of permits to acquire (default: 1)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AcquireAsync(int permits = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current rate limit in operations per second.
    /// </summary>
    double CurrentRateLimit { get; }

    /// <summary>
    /// Gets the current utilization percentage (0.0 to 1.0).
    /// </summary>
    double CurrentUtilization { get; }

    /// <summary>
    /// Updates the rate limit dynamically based on system conditions.
    /// Supports adaptive rate limiting for optimal throughput.
    /// </summary>
    /// <param name="newRateLimit">New rate limit in operations per second</param>
    void UpdateRateLimit(double newRateLimit);

    /// <summary>
    /// Resets the rate limiter state.
    /// Useful for testing and recovery scenarios.
    /// </summary>
    void Reset();
}

/// <summary>
/// Rate limiting configuration for different enforcement tiers.
/// Supports hierarchical rate limiting from global to endpoint level.
/// </summary>
public class RateLimitingTier
{
    public string Name { get; init; } = string.Empty;
    public string Scope { get; init; } = string.Empty;
    public double RateLimit { get; init; }
    public double BurstCapacity { get; init; }
    public TimeSpan BurstDuration { get; init; }
    public RateLimitingEnforcement Enforcement { get; init; }
}

/// <summary>
/// Types of rate limiting enforcement strategies.
/// </summary>
public enum RateLimitingEnforcement
{
    /// <summary>Hard limit - immediately reject requests exceeding limit</summary>
    HardLimit,
    
    /// <summary>Throttling - slow down requests but don't reject</summary>
    Throttling,
    
    /// <summary>Backpressure - signal upstream to slow down</summary>
    Backpressure,
    
    /// <summary>Circuit breaker - stop requests after threshold breached</summary>
    CircuitBreaker
}