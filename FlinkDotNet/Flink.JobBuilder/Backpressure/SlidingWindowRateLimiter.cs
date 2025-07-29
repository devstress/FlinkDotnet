using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Flink.JobBuilder.Backpressure;

/// <summary>
/// Sliding Window rate limiter implementation for time-based rate limiting.
/// Tracks actual request rate over a sliding time window for precise control.
/// 
/// Algorithm:
/// - Maintains timestamps of recent requests in sliding window
/// - Calculates current rate based on request count in window
/// - Allows bursts within window constraints
/// - More precise than token bucket for time-sensitive scenarios
/// 
/// Use cases:
/// - API rate limiting with precise time windows
/// - Network bandwidth limiting
/// - Database connection rate limiting
/// </summary>
public class SlidingWindowRateLimiter : IRateLimitingStrategy
{
    private readonly object _lock = new();
    private readonly Queue<DateTime> _requestTimestamps = new();
    private readonly TimeSpan _windowSize;
    private readonly int _maxRequestsPerWindow;
    private double _currentRateLimit;

    /// <summary>
    /// Creates a new SlidingWindow rate limiter.
    /// </summary>
    /// <param name="maxRequestsPerSecond">Maximum requests per second</param>
    /// <param name="windowSizeSeconds">Size of sliding window in seconds</param>
    public SlidingWindowRateLimiter(double maxRequestsPerSecond, double windowSizeSeconds = 1.0)
    {
        if (maxRequestsPerSecond <= 0) 
            throw new ArgumentException("Rate limit must be positive", nameof(maxRequestsPerSecond));
        if (windowSizeSeconds <= 0) 
            throw new ArgumentException("Window size must be positive", nameof(windowSizeSeconds));

        _currentRateLimit = maxRequestsPerSecond;
        _windowSize = TimeSpan.FromSeconds(windowSizeSeconds);
        _maxRequestsPerWindow = (int)Math.Ceiling(maxRequestsPerSecond * windowSizeSeconds);
    }

    /// <inheritdoc />
    public Task<bool> TryAcquireAsync(int permits = 1, CancellationToken cancellationToken = default)
    {
        if (permits <= 0) throw new ArgumentException("Permits must be positive", nameof(permits));

        lock (_lock)
        {
            var now = DateTime.UtcNow;
            CleanupOldRequests(now);

            // Check if adding permits would exceed window limit
            if (_requestTimestamps.Count + permits <= _maxRequestsPerWindow)
            {
                // Add request timestamps
                for (int i = 0; i < permits; i++)
                {
                    _requestTimestamps.Enqueue(now);
                }
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
            var now = DateTime.UtcNow;
            CleanupOldRequests(now);

            // Check if adding permits would exceed window limit
            if (_requestTimestamps.Count + permits <= _maxRequestsPerWindow)
            {
                // Add request timestamps
                for (int i = 0; i < permits; i++)
                {
                    _requestTimestamps.Enqueue(now);
                }
                return true;
            }

            return false;
        }
    }

    /// <inheritdoc />
    public async Task AcquireAsync(int permits = 1, CancellationToken cancellationToken = default)
    {
        if (permits <= 0) throw new ArgumentException("Permits must be positive", nameof(permits));

        while (!cancellationToken.IsCancellationRequested)
        {
            if (await TryAcquireAsync(permits, cancellationToken))
            {
                return;
            }

            // Calculate wait time until oldest request expires
            var waitTime = CalculateWaitTime(permits);
            
            await Task.Delay(waitTime, cancellationToken);
        }
        
        cancellationToken.ThrowIfCancellationRequested();
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
                CleanupOldRequests(DateTime.UtcNow);
                return (double)_requestTimestamps.Count / _maxRequestsPerWindow;
            }
        }
    }

    /// <inheritdoc />
    public void UpdateRateLimit(double newRateLimit)
    {
        if (newRateLimit <= 0) throw new ArgumentException("Rate limit must be positive", nameof(newRateLimit));

        lock (_lock)
        {
            _currentRateLimit = newRateLimit;
            // Note: _maxRequestsPerWindow is calculated at construction and doesn't change
            // This is by design to maintain window consistency
        }
    }

    /// <inheritdoc />
    public void Reset()
    {
        lock (_lock)
        {
            _requestTimestamps.Clear();
        }
    }

    /// <summary>
    /// Gets current request count in the sliding window.
    /// </summary>
    public int CurrentRequestCount
    {
        get
        {
            lock (_lock)
            {
                CleanupOldRequests(DateTime.UtcNow);
                return _requestTimestamps.Count;
            }
        }
    }

    /// <summary>
    /// Gets the actual rate based on current window contents.
    /// </summary>
    public double ActualRate
    {
        get
        {
            lock (_lock)
            {
                CleanupOldRequests(DateTime.UtcNow);
                return _requestTimestamps.Count / _windowSize.TotalSeconds;
            }
        }
    }

    /// <summary>
    /// Gets the window size.
    /// </summary>
    public TimeSpan WindowSize => _windowSize;

    private void CleanupOldRequests(DateTime now)
    {
        var cutoffTime = now - _windowSize;
        
        while (_requestTimestamps.Count > 0 && _requestTimestamps.Peek() < cutoffTime)
        {
            _requestTimestamps.Dequeue();
        }
    }

    private TimeSpan CalculateWaitTime(int permits)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            CleanupOldRequests(now);

            if (_requestTimestamps.Count + permits <= _maxRequestsPerWindow)
            {
                return TimeSpan.Zero;
            }

            // Find when enough requests will expire to allow new permits
            var requestsToExpire = (_requestTimestamps.Count + permits) - _maxRequestsPerWindow;
            var timestamps = _requestTimestamps.ToArray();
            
            if (requestsToExpire > 0 && requestsToExpire <= timestamps.Length)
            {
                var expirationTime = timestamps[requestsToExpire - 1] + _windowSize;
                var waitTime = expirationTime - now;
                return waitTime > TimeSpan.Zero ? waitTime : TimeSpan.FromMilliseconds(1);
            }

            // Fallback: wait for average request interval
            var avgInterval = _windowSize.TotalMilliseconds / _maxRequestsPerWindow;
            return TimeSpan.FromMilliseconds(Math.Max(1, avgInterval));
        }
    }
}