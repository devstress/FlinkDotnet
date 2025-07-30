using Flink.JobBuilder.Backpressure;
using LocalTesting.WebApi.Models;
using System.Collections.Concurrent;

namespace LocalTesting.WebApi.Services;

public class BackpressureMonitoringService
{
    private readonly ILogger<BackpressureMonitoringService> _logger;
    private LagBasedRateLimiter? _rateLimiter;
    private string _consumerGroup = string.Empty;
    private TimeSpan _lagThreshold;
    private bool _isInitialized = false;

    public BackpressureMonitoringService(ILogger<BackpressureMonitoringService> logger)
    {
        _logger = logger;
    }

    public async Task InitializeAsync(string consumerGroup, TimeSpan lagThreshold, double rateLimit, double burstCapacity)
    {
        _consumerGroup = consumerGroup;
        _lagThreshold = lagThreshold;

        // Create lag monitor implementation
        var lagMonitor = new DefaultKafkaConsumerLagMonitor(_logger);

        // Initialize the lag-based rate limiter with backpressure
        _rateLimiter = new LagBasedRateLimiter(
            rateLimit: rateLimit,
            burstCapacity: burstCapacity,
            consumerGroup: consumerGroup,
            lagThreshold: lagThreshold,
            lagMonitor: lagMonitor);

        _isInitialized = true;
        
        _logger.LogInformation(
            "BackpressureMonitoringService initialized: Group={ConsumerGroup}, Threshold={LagThreshold}s, Rate={RateLimit}, Burst={BurstCapacity}",
            consumerGroup, lagThreshold.TotalSeconds, rateLimit, burstCapacity);

        await Task.CompletedTask;
    }

    public BackpressureStatus GetBackpressureStatus()
    {
        if (!_isInitialized || _rateLimiter == null)
        {
            return new BackpressureStatus
            {
                IsBackpressureActive = false,
                CurrentLag = TimeSpan.Zero,
                LagThreshold = _lagThreshold,
                CurrentTokens = 0,
                MaxTokens = 0,
                RateLimit = 0,
                IsRefillPaused = false,
                LastCheck = DateTime.UtcNow,
                RateLimiterType = "Not Initialized"
            };
        }

        // Simulate consumer lag monitoring
        var simulatedLag = TimeSpan.FromSeconds(Random.Shared.NextDouble() * 10); // 0-10 seconds
        var isBackpressureActive = simulatedLag > _lagThreshold;

        return new BackpressureStatus
        {
            IsBackpressureActive = isBackpressureActive,
            CurrentLag = simulatedLag,
            LagThreshold = _lagThreshold,
            CurrentTokens = _rateLimiter.CurrentTokens,
            MaxTokens = _rateLimiter.MaxTokens,
            RateLimit = _rateLimiter.CurrentRateLimit,
            IsRefillPaused = isBackpressureActive, // Would be controlled by the actual lag monitor
            LastCheck = DateTime.UtcNow,
            RateLimiterType = nameof(LagBasedRateLimiter)
        };
    }

    public async Task<bool> TryAcquireAsync(int permits = 1)
    {
        if (!_isInitialized || _rateLimiter == null)
        {
            throw new InvalidOperationException("BackpressureMonitoringService not initialized");
        }

        return await _rateLimiter.TryAcquireAsync(permits);
    }

    public async Task AcquireAsync(int permits = 1)
    {
        if (!_isInitialized || _rateLimiter == null)
        {
            throw new InvalidOperationException("BackpressureMonitoringService not initialized");
        }

        await _rateLimiter.AcquireAsync(permits);
    }

    public void UpdateRateLimit(double newRateLimit)
    {
        if (!_isInitialized || _rateLimiter == null)
        {
            throw new InvalidOperationException("BackpressureMonitoringService not initialized");
        }

        _rateLimiter.UpdateRateLimit(newRateLimit);
        _logger.LogInformation("Rate limit updated to {NewRateLimit} ops/sec", newRateLimit);
    }

    public void Reset()
    {
        if (!_isInitialized || _rateLimiter == null)
        {
            throw new InvalidOperationException("BackpressureMonitoringService not initialized");
        }

        _rateLimiter.Reset();
        _logger.LogInformation("Rate limiter reset");
    }

    public Dictionary<string, object> GetMetrics()
    {
        if (!_isInitialized || _rateLimiter == null)
        {
            return new Dictionary<string, object> { ["status"] = "not_initialized" };
        }

        var status = GetBackpressureStatus();
        
        return new Dictionary<string, object>
        {
            ["consumerGroup"] = _consumerGroup,
            ["lagThreshold"] = _lagThreshold.TotalSeconds,
            ["currentLag"] = status.CurrentLag.TotalSeconds,
            ["isBackpressureActive"] = status.IsBackpressureActive,
            ["currentTokens"] = status.CurrentTokens,
            ["maxTokens"] = status.MaxTokens,
            ["rateLimit"] = status.RateLimit,
            ["utilization"] = _rateLimiter.CurrentUtilization,
            ["isRefillPaused"] = status.IsRefillPaused,
            ["rateLimiterType"] = status.RateLimiterType,
            ["lastCheck"] = status.LastCheck
        };
    }
}

// Simple implementation of the lag monitor for demonstration
public class DefaultKafkaConsumerLagMonitor : IKafkaConsumerLagMonitor
{
    private readonly ILogger? _logger;
    private readonly Random _random = new();
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

    public TimeSpan GetCurrentLag(string consumerGroup)
    {
        return _lagCache.TryGetValue(consumerGroup, out var lag) ? lag : TimeSpan.Zero;
    }

    public async Task<TimeSpan> GetCurrentLagAsync(string consumerGroup)
    {
        // Simulate checking consumer lag
        await Task.Delay(50);
        
        // Return cached lag or simulate new lag
        if (_lagCache.TryGetValue(consumerGroup, out var cachedLag))
        {
            return cachedLag;
        }
        
        // Simulate varying lag between 0-10 seconds
        var lagSeconds = _random.NextDouble() * 10;
        var lag = TimeSpan.FromSeconds(lagSeconds);
        
        _lagCache.TryAdd(consumerGroup, lag);
        
        _logger?.LogDebug("Consumer lag for group {ConsumerGroup}: {Lag}ms", consumerGroup, lag.TotalMilliseconds);
        
        return lag;
    }

    private void RefreshLagData(object? state)
    {
        if (_disposed) return;

        try
        {
            // Simulate varying lag
            var simulatedLagMs = _random.Next(0, 10000); // 0-10 seconds
            var lag = TimeSpan.FromMilliseconds(simulatedLagMs);
            
            // Update cache for all groups
            foreach (var group in new[] { "stress-test-group", "default-group", "test-group" })
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