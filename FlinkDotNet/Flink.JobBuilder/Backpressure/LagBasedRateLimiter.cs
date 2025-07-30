using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Flink.JobBuilder.Backpressure;

/// <summary>
/// Simple lag-based rate limiter that monitors Kafka consumer lag for natural backpressure.
/// 
/// SIMPLE APPROACH (Recommended):
/// - Basic token bucket implementation 
/// - Monitors consumer lag from actual Kafka consumers
/// - When lag > threshold → pause token refilling (natural backpressure)
/// - When lag decreases → resume token refilling
/// - Much simpler operationally than distributed state storage
/// 
/// Benefits:
/// - Reactive backpressure based on actual system load
/// - No complex Kafka state storage or Redis clusters
/// - Simple to operate and debug
/// - Automatic adjustment to consumer performance
/// - Follows reactive streams principles
/// 
/// Algorithm:
/// 1. Token bucket refills at configured rate
/// 2. Consumer lag monitor checks lag every interval
/// 3. If lag > threshold: pause refilling until lag decreases
/// 4. If lag <= threshold: resume normal refilling
/// 5. Operations consume tokens as normal
/// 
/// This creates natural backpressure - if consumers can't keep up,
/// the rate limiter automatically slows down the producers.
/// </summary>
public class LagBasedRateLimiter : IRateLimitingStrategy, IDisposable
{
    private readonly object _lock = new();
    private readonly double _maxTokens;
    private readonly string _consumerGroup;
    private readonly TimeSpan _lagThreshold;
    private readonly IKafkaConsumerLagMonitor _lagMonitor;
    private readonly ILogger<LagBasedRateLimiter>? _logger;
    private readonly Timer _lagCheckTimer;
    
    // Async wait queue for non-blocking operations
    private readonly ConcurrentQueue<LagBasedWaitingRequest> _waitingRequests = new();
    private readonly Timer _processWaitingRequestsTimer;
    
    private double _currentTokens;
    private DateTime _lastRefill;
    private double _currentRateLimit;
    private bool _isRefillPaused;
    private volatile bool _disposed;

    /// <summary>
    /// Creates a lag-based rate limiter that monitors consumer lag for backpressure.
    /// </summary>
    /// <param name="rateLimit">Maximum sustained rate in operations per second</param>
    /// <param name="burstCapacity">Maximum burst capacity (tokens that can accumulate)</param>
    /// <param name="consumerGroup">Kafka consumer group to monitor for lag</param>
    /// <param name="lagThreshold">Lag threshold to trigger backpressure (default: 5 seconds)</param>
    /// <param name="lagMonitor">Consumer lag monitor (auto-created if not provided)</param>
    /// <param name="logger">Logger instance</param>
    public LagBasedRateLimiter(
        double rateLimit,
        double burstCapacity,
        string consumerGroup,
        TimeSpan? lagThreshold = null,
        IKafkaConsumerLagMonitor? lagMonitor = null,
        ILogger<LagBasedRateLimiter>? logger = null)
    {
        if (rateLimit <= 0) throw new ArgumentException("Rate limit must be positive", nameof(rateLimit));
        if (burstCapacity <= 0) throw new ArgumentException("Burst capacity must be positive", nameof(burstCapacity));
        if (string.IsNullOrWhiteSpace(consumerGroup)) throw new ArgumentException("Consumer group is required", nameof(consumerGroup));

        _currentRateLimit = rateLimit;
        _maxTokens = burstCapacity;
        _currentTokens = burstCapacity; // Start with full bucket
        _consumerGroup = consumerGroup;
        _lagThreshold = lagThreshold ?? TimeSpan.FromSeconds(5);
        _lagMonitor = lagMonitor ?? new DefaultKafkaConsumerLagMonitor(logger);
        _logger = logger;
        _lastRefill = DateTime.UtcNow;
        _isRefillPaused = false;

        _logger?.LogInformation("LagBasedRateLimiter created: rate={RateLimit}, burst={BurstCapacity}, group={ConsumerGroup}, threshold={LagThreshold}", 
            rateLimit, burstCapacity, consumerGroup, _lagThreshold);

        // Timer to check consumer lag and adjust refilling
        _lagCheckTimer = new Timer(CheckConsumerLagAsync, null, 
            TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            
        // Timer to process waiting requests efficiently
        _processWaitingRequestsTimer = new Timer(ProcessWaitingRequestsAsync, null,
            TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(10));
    }

    /// <inheritdoc />
    public Task<bool> TryAcquireAsync(int permits = 1, CancellationToken cancellationToken = default)
    {
        if (permits <= 0) throw new ArgumentException("Permits must be positive", nameof(permits));

        lock (_lock)
        {
            RefillTokens();
            
            if (_currentTokens >= permits)
            {
                _currentTokens -= permits;
                return Task.FromResult(true);
            }
            
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc />
    public bool TryAcquire(int permits = 1)
    {
        if (permits <= 0) throw new ArgumentException("Permits must be positive", nameof(permits));

        lock (_lock)
        {
            RefillTokens();
            
            if (_currentTokens >= permits)
            {
                _currentTokens -= permits;
                return true;
            }
            
            return false;
        }
    }

    /// <inheritdoc />
    public async Task AcquireAsync(int permits = 1, CancellationToken cancellationToken = default)
    {
        if (permits <= 0) throw new ArgumentException("Permits must be positive", nameof(permits));

        // Try immediate acquisition first
        if (await TryAcquireAsync(permits, cancellationToken))
        {
            return;
        }

        // Use non-blocking async wait queue
        var waitingRequest = new LagBasedWaitingRequest
        {
            Permits = permits,
            TaskCompletionSource = new TaskCompletionSource<bool>(),
            CancellationToken = cancellationToken,
            RequestTime = DateTime.UtcNow
        };

        // Register cancellation callback
        using var cancellationRegistration = cancellationToken.Register(() =>
        {
            waitingRequest.TaskCompletionSource.TrySetCanceled(cancellationToken);
        });

        _waitingRequests.Enqueue(waitingRequest);

        // Wait for tokens to become available
        await waitingRequest.TaskCompletionSource.Task;
    }

    /// <inheritdoc />
    public double CurrentRateLimit => _currentRateLimit;

    /// <inheritdoc />
    public double CurrentUtilization
    {
        get
        {
            lock (_lock)
            {
                RefillTokens();
                return 1.0 - (_currentTokens / _maxTokens);
            }
        }
    }

    /// <inheritdoc />
    public void UpdateRateLimit(double newRateLimit)
    {
        if (newRateLimit <= 0) throw new ArgumentException("Rate limit must be positive", nameof(newRateLimit));

        lock (_lock)
        {
            RefillTokens(); // Update tokens with old rate first
            _currentRateLimit = newRateLimit;
        }
        
        _logger?.LogInformation("Rate limit updated to {NewRateLimit} ops/sec", newRateLimit);
    }

    /// <inheritdoc />
    public void Reset()
    {
        lock (_lock)
        {
            _currentTokens = _maxTokens;
            _lastRefill = DateTime.UtcNow;
            _isRefillPaused = false;
        }
        
        _logger?.LogInformation("Rate limiter reset to full capacity");
    }

    /// <summary>
    /// Gets current token count for monitoring/debugging.
    /// </summary>
    public double CurrentTokens
    {
        get
        {
            lock (_lock)
            {
                RefillTokens();
                return _currentTokens;
            }
        }
    }

    /// <summary>
    /// Gets maximum token capacity.
    /// </summary>
    public double MaxTokens => _maxTokens;

    /// <summary>
    /// Gets the consumer group being monitored.
    /// </summary>
    public string ConsumerGroup => _consumerGroup;

    /// <summary>
    /// Gets the lag threshold for triggering backpressure.
    /// </summary>
    public TimeSpan LagThreshold => _lagThreshold;

    /// <summary>
    /// Indicates if token refilling is currently paused due to high lag.
    /// </summary>
    public bool IsRefillPaused => _isRefillPaused;

    /// <summary>
    /// Gets the current consumer lag.
    /// </summary>
    public TimeSpan CurrentLag => _lagMonitor.GetCurrentLag(_consumerGroup);

    private void RefillTokens()
    {
        // Don't refill if paused due to high consumer lag
        if (_isRefillPaused)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var elapsed = (now - _lastRefill).TotalSeconds;
        
        if (elapsed > 0)
        {
            var tokensToAdd = elapsed * _currentRateLimit;
            _currentTokens = Math.Min(_maxTokens, _currentTokens + tokensToAdd);
            _lastRefill = now;
        }
    }

    /// <summary>
    /// Checks consumer lag and adjusts token refilling accordingly.
    /// </summary>
    private async void CheckConsumerLagAsync(object? state)
    {
        if (_disposed) return;

        try
        {
            var currentLag = await _lagMonitor.GetCurrentLagAsync(_consumerGroup);
            var wasRefillPaused = _isRefillPaused;

            lock (_lock)
            {
                if (currentLag > _lagThreshold)
                {
                    // High lag - pause refilling to create backpressure
                    _isRefillPaused = true;
                    if (!wasRefillPaused)
                    {
                        _logger?.LogWarning("High consumer lag detected ({CurrentLag}ms > {Threshold}ms). Pausing token refilling for backpressure.", 
                            currentLag.TotalMilliseconds, _lagThreshold.TotalMilliseconds);
                    }
                }
                else
                {
                    // Normal lag - resume refilling
                    _isRefillPaused = false;
                    if (wasRefillPaused)
                    {
                        _logger?.LogInformation("Consumer lag reduced ({CurrentLag}ms <= {Threshold}ms). Resuming token refilling.", 
                            currentLag.TotalMilliseconds, _lagThreshold.TotalMilliseconds);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error checking consumer lag for group {ConsumerGroup}", _consumerGroup);
        }
    }

    /// <summary>
    /// Processes waiting requests efficiently using async patterns.
    /// </summary>
    private async void ProcessWaitingRequestsAsync(object? state)
    {
        if (_disposed) return;

        try
        {
            var processedRequests = 0;
            var maxRequestsPerCycle = 100; // Prevent blocking the timer thread

            while (processedRequests < maxRequestsPerCycle && _waitingRequests.TryDequeue(out var waitingRequest))
            {
                // Skip cancelled requests
                if (waitingRequest.CancellationToken.IsCancellationRequested)
                {
                    waitingRequest.TaskCompletionSource.TrySetCanceled(waitingRequest.CancellationToken);
                    processedRequests++;
                    continue;
                }

                // Try to fulfill the request
                if (await TryAcquireAsync(waitingRequest.Permits, waitingRequest.CancellationToken))
                {
                    waitingRequest.TaskCompletionSource.TrySetResult(true);
                    processedRequests++;
                }
                else
                {
                    // Check if request has timed out
                    var requestAge = DateTime.UtcNow - waitingRequest.RequestTime;
                    if (requestAge > TimeSpan.FromMinutes(5)) // 5-minute timeout
                    {
                        waitingRequest.TaskCompletionSource.TrySetException(
                            new TimeoutException("Rate limiter request timed out after 5 minutes"));
                        processedRequests++;
                    }
                    else
                    {
                        // Re-queue the request for next cycle
                        _waitingRequests.Enqueue(waitingRequest);
                        break; // Stop processing to avoid busy waiting
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing waiting requests");
        }
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
            // Complete any waiting requests
            while (_waitingRequests.TryDequeue(out var waitingRequest))
            {
                waitingRequest.TaskCompletionSource.TrySetCanceled();
            }
            
            _lagCheckTimer?.Dispose();
            _processWaitingRequestsTimer?.Dispose();
            _lagMonitor?.Dispose();
        }
        
        _disposed = true;
    }
}

/// <summary>
/// Interface for monitoring Kafka consumer lag.
/// </summary>
public interface IKafkaConsumerLagMonitor : IDisposable
{
    /// <summary>
    /// Gets the current lag for a consumer group synchronously.
    /// </summary>
    /// <param name="consumerGroup">Consumer group name</param>
    /// <returns>Current lag as TimeSpan</returns>
    TimeSpan GetCurrentLag(string consumerGroup);

    /// <summary>
    /// Gets the current lag for a consumer group asynchronously.
    /// </summary>
    /// <param name="consumerGroup">Consumer group name</param>
    /// <returns>Current lag as TimeSpan</returns>
    Task<TimeSpan> GetCurrentLagAsync(string consumerGroup);
}

/// <summary>
/// Default implementation of Kafka consumer lag monitoring.
/// In production, this would use actual Kafka Admin API or monitoring tools.
/// </summary>
public class DefaultKafkaConsumerLagMonitor : IKafkaConsumerLagMonitor
{
    private readonly ILogger? _logger;
    private readonly ConcurrentDictionary<string, TimeSpan> _lagCache = new();
    private readonly Timer _refreshTimer;
    private volatile bool _disposed;

    public DefaultKafkaConsumerLagMonitor(ILogger? logger = null)
    {
        _logger = logger;
        
        // Refresh lag data every 5 seconds
        _refreshTimer = new Timer(RefreshLagData, null, 
            TimeSpan.Zero, TimeSpan.FromSeconds(5));
    }

    /// <inheritdoc />
    public TimeSpan GetCurrentLag(string consumerGroup)
    {
        return _lagCache.TryGetValue(consumerGroup, out var lag) ? lag : TimeSpan.Zero;
    }

    /// <inheritdoc />
    public Task<TimeSpan> GetCurrentLagAsync(string consumerGroup)
    {
        return Task.FromResult(GetCurrentLag(consumerGroup));
    }

    private void RefreshLagData(object? state)
    {
        if (_disposed) return;

        try
        {
            // In production, this would:
            // 1. Use Kafka Admin Client to get consumer group metadata
            // 2. Calculate lag based on latest offsets vs committed offsets
            // 3. Convert offset lag to time lag using message timestamps
            
            // For now, simulate varying lag
            var random = new Random();
            var simulatedLagMs = random.Next(0, 10000); // 0-10 seconds
            var lag = TimeSpan.FromMilliseconds(simulatedLagMs);
            
            // Update cache for all groups (in reality, would iterate actual groups)
            foreach (var group in new[] { "default-group", "test-group", "my-consumer-group" })
            {
                _lagCache.AddOrUpdate(group, lag, (_, _) => lag);
            }
            
            _logger?.LogDebug("Refreshed consumer lag data: {LagMs}ms", simulatedLagMs);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error refreshing consumer lag data");
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        
        if (disposing)
        {
            _refreshTimer?.Dispose();
        }
        
        _disposed = true;
    }
}

/// <summary>
/// Represents a waiting request in the async queue.
/// Used for efficient non-blocking async operations.
/// </summary>
internal class LagBasedWaitingRequest
{
    public int Permits { get; init; }
    public TaskCompletionSource<bool> TaskCompletionSource { get; init; } = new();
    public CancellationToken CancellationToken { get; init; }
    public DateTime RequestTime { get; init; }
}