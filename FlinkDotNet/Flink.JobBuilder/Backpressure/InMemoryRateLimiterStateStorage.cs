using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Flink.JobBuilder.Backpressure;

/// <summary>
/// In-memory rate limiter state storage implementation.
/// 
/// This implementation is provided for comparison with Kafka-based storage.
/// While suitable for single-instance deployments, it has significant limitations:
/// 
/// LIMITATIONS vs Kafka:
/// 
/// 1. SCALE:
///    - Limited to single process memory
///    - Cannot scale horizontally across multiple instances
///    - Memory constraints limit the number of rate limiters
/// 
/// 2. PERSISTENCE:
///    - All state is lost on process restart
///    - No durability guarantees
///    - No recovery mechanism for failures
/// 
/// 3. INFRASTRUCTURE:
///    - Simple setup but no high availability
///    - Single point of failure
///    - No built-in replication or backup
/// 
/// 4. AVAILABILITY AND RESILIENCE:
///    - Process restart loses all rate limiter state
///    - No failover capabilities
///    - Memory leaks can affect entire application
/// 
/// Use this implementation only for:
/// - Development and testing environments
/// - Single-instance deployments with acceptable data loss
/// - Scenarios where external dependencies must be minimized
/// 
/// For production environments, use KafkaRateLimiterStateStorage instead.
/// </summary>
public class InMemoryRateLimiterStateStorage : IRateLimiterStateStorage
{
    private readonly ConcurrentDictionary<string, RateLimiterState> _stateStore = new();
    private readonly ILogger<InMemoryRateLimiterStateStorage> _logger;
    private volatile bool _disposed;

    /// <summary>
    /// Initializes in-memory rate limiter state storage.
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public InMemoryRateLimiterStateStorage(ILogger<InMemoryRateLimiterStateStorage>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<InMemoryRateLimiterStateStorage>.Instance;
        _logger.LogWarning("Using in-memory rate limiter state storage - not suitable for production environments");
    }

    /// <inheritdoc />
    public StorageBackendInfo BackendInfo => new()
    {
        BackendType = "In-Memory",
        Description = "Local process memory storage - not persistent, not distributed",
        SupportsDistribution = false,
        SupportsPersistence = false,
        SupportsReplication = false,
        TypicalLatency = TimeSpan.FromMicroseconds(10), // Very fast but not durable
        ScalabilityCharacteristics = "Limited to single process memory, not horizontally scalable"
    };

    /// <inheritdoc />
    public Task SaveStateAsync(string rateLimiterId, RateLimiterState state, CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(InMemoryRateLimiterStateStorage));

        _stateStore.AddOrUpdate(rateLimiterId, state, (_, _) => state);
        
        _logger.LogDebug("Rate limiter state saved to memory: {RateLimiterId}", rateLimiterId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<RateLimiterState?> LoadStateAsync(string rateLimiterId, CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(InMemoryRateLimiterStateStorage));

        var found = _stateStore.TryGetValue(rateLimiterId, out var state);
        
        if (found)
        {
            _logger.LogDebug("Rate limiter state loaded from memory: {RateLimiterId}", rateLimiterId);
        }
        else
        {
            _logger.LogDebug("Rate limiter state not found in memory: {RateLimiterId}", rateLimiterId);
        }

        return Task.FromResult(state);
    }

    /// <inheritdoc />
    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        // In-memory storage is always "healthy" if not disposed
        return Task.FromResult(!_disposed);
    }

    /// <summary>
    /// Gets the current number of stored rate limiter states.
    /// </summary>
    public int StateCount => _stateStore.Count;

    /// <summary>
    /// Clears all stored rate limiter states.
    /// </summary>
    public void ClearAllStates()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(InMemoryRateLimiterStateStorage));

        var count = _stateStore.Count;
        _stateStore.Clear();
        
        _logger.LogInformation("Cleared {Count} rate limiter states from memory", count);
    }

    /// <summary>
    /// Disposes resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected dispose method following standard pattern.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        
        if (disposing)
        {
            var count = _stateStore.Count;
            _stateStore.Clear();
            
            _logger.LogInformation("In-memory rate limiter state storage disposed, lost {Count} states", count);
        }
        
        _disposed = true;
    }
}