using System;
using System.Threading;
using System.Threading.Tasks;

namespace Flink.JobBuilder.Backpressure;

/// <summary>
/// Interface for rate limiter state storage backend.
/// Supports both in-memory and distributed storage implementations.
/// </summary>
public interface IRateLimiterStateStorage : IDisposable
{
    /// <summary>
    /// Saves rate limiter state to storage.
    /// </summary>
    /// <param name="rateLimiterId">Unique identifier for the rate limiter</param>
    /// <param name="state">State to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SaveStateAsync(string rateLimiterId, RateLimiterState state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads rate limiter state from storage.
    /// </summary>
    /// <param name="rateLimiterId">Unique identifier for the rate limiter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Rate limiter state or null if not found</returns>
    Task<RateLimiterState?> LoadStateAsync(string rateLimiterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the storage backend is available and operational.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if storage is healthy</returns>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets storage backend information.
    /// </summary>
    StorageBackendInfo BackendInfo { get; }
}

/// <summary>
/// Rate limiter state for persistence.
/// </summary>
public class RateLimiterState
{
    public string RateLimiterId { get; init; } = string.Empty;
    public double CurrentTokens { get; init; }
    public double MaxTokens { get; init; }
    public double CurrentRateLimit { get; init; }
    public DateTime LastRefill { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string RateLimiterType { get; init; } = string.Empty;
    public System.Collections.Generic.Dictionary<string, object> AdditionalProperties { get; init; } = new();
}

/// <summary>
/// Information about the storage backend.
/// </summary>
public class StorageBackendInfo
{
    public string BackendType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool SupportsDistribution { get; init; }
    public bool SupportsPersistence { get; init; }
    public bool SupportsReplication { get; init; }
    public TimeSpan TypicalLatency { get; init; }
    public string ScalabilityCharacteristics { get; init; } = string.Empty;
}