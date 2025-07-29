using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Flink.JobBuilder.Backpressure;

/// <summary>
/// Buffer Pool implementation with size and time thresholds following Flink patterns.
/// Manages buffering of items with configurable size and time-based flush triggers.
/// 
/// Features:
/// - Size-based threshold: Flush when buffer reaches maximum size
/// - Time-based threshold: Flush when items have been buffered for too long
/// - Backpressure integration: Apply backpressure when buffer is full
/// - Thread-safe operations for concurrent producer/consumer scenarios
/// 
/// Based on Flink's buffer pool design for AsyncSink optimization.
/// </summary>
/// <typeparam name="T">Type of items being buffered</typeparam>
public class BufferPool<T> : IDisposable
{
    private readonly ConcurrentQueue<BufferedItem<T>> _buffer = new();
    private readonly int _maxSize;
    private readonly TimeSpan _maxAge;
    private readonly IRateLimitingStrategy _rateLimiter;
    private readonly Timer _flushTimer;
    private readonly SemaphoreSlim _flushSemaphore = new(1, 1);
    
    private volatile int _currentSize;
    private volatile bool _disposed;
    private DateTime _lastFlush = DateTime.UtcNow;

    /// <summary>
    /// Event triggered when buffer should be flushed.
    /// </summary>
    public event Func<BufferedItem<T>[], Task> OnFlush = delegate { return Task.CompletedTask; };

    /// <summary>
    /// Event triggered when backpressure should be applied.
    /// </summary>
    public event Action<BackpressureEvent> OnBackpressure = delegate { };

    /// <summary>
    /// Creates a new BufferPool with size and time thresholds.
    /// </summary>
    /// <param name="maxSize">Maximum number of items in buffer before forced flush</param>
    /// <param name="maxAge">Maximum time items can remain in buffer before forced flush</param>
    /// <param name="rateLimiter">Optional rate limiter for flush operations</param>
    public BufferPool(int maxSize, TimeSpan maxAge, IRateLimitingStrategy? rateLimiter = null)
    {
        if (maxSize <= 0) throw new ArgumentException("Max size must be positive", nameof(maxSize));
        if (maxAge <= TimeSpan.Zero) throw new ArgumentException("Max age must be positive", nameof(maxAge));

        _maxSize = maxSize;
        _maxAge = maxAge;
        _rateLimiter = rateLimiter ?? new TokenBucketRateLimiter(double.MaxValue, double.MaxValue);

        // Timer for time-based flushing (check every 1/10 of max age)
        var timerInterval = TimeSpan.FromMilliseconds(Math.Max(100, _maxAge.TotalMilliseconds / 10));
        _flushTimer = new Timer(OnTimerFlush, null, timerInterval, timerInterval);
    }

    /// <summary>
    /// Adds an item to the buffer, potentially triggering flush or backpressure.
    /// </summary>
    /// <param name="item">Item to buffer</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if item was buffered, false if backpressure was applied</returns>
    public async Task<bool> TryAddAsync(T item, CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BufferPool<T>));

        // Check if we need to apply backpressure
        if (_currentSize >= _maxSize)
        {
            OnBackpressure(new BackpressureEvent
            {
                Reason = BackpressureReason.BufferFull,
                CurrentSize = _currentSize,
                MaxSize = _maxSize,
                Utilization = (double)_currentSize / _maxSize
            });

            // Try to flush to make space
            await TryFlushAsync(cancellationToken);

            // If still full, apply backpressure
            if (_currentSize >= _maxSize)
            {
                return false;
            }
        }

        // Add item to buffer
        var bufferedItem = new BufferedItem<T>
        {
            Item = item,
            Timestamp = DateTime.UtcNow
        };

        _buffer.Enqueue(bufferedItem);
        Interlocked.Increment(ref _currentSize);

        // Check if size threshold reached
        if (_currentSize >= _maxSize)
        {
            _ = Task.Run(() => TryFlushAsync(cancellationToken), cancellationToken);
        }

        return true;
    }

    /// <summary>
    /// Adds an item to buffer, waiting if backpressure is applied.
    /// </summary>
    /// <param name="item">Item to buffer</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task AddAsync(T item, CancellationToken cancellationToken = default)
    {
        while (!await TryAddAsync(item, cancellationToken))
        {
            // Wait before retrying
            await Task.Delay(10, cancellationToken);
        }
    }

    /// <summary>
    /// Forces immediate flush of all buffered items.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) return;

        await _flushSemaphore.WaitAsync(cancellationToken);
        try
        {
            await PerformFlushAsync();
        }
        finally
        {
            _flushSemaphore.Release();
        }
    }

    /// <summary>
    /// Gets current buffer statistics.
    /// </summary>
    public BufferPoolStats GetStats()
    {
        return new BufferPoolStats
        {
            CurrentSize = _currentSize,
            MaxSize = _maxSize,
            Utilization = (double)_currentSize / _maxSize,
            MaxAge = _maxAge,
            LastFlush = _lastFlush,
            RateLimiterUtilization = _rateLimiter.CurrentUtilization
        };
    }

    private async Task TryFlushAsync(CancellationToken cancellationToken)
    {
        if (!await _flushSemaphore.WaitAsync(100, cancellationToken))
        {
            return; // Another flush is already in progress
        }

        try
        {
            // Apply rate limiting to flush operations
            if (!await _rateLimiter.TryAcquireAsync(1, cancellationToken))
            {
                return; // Rate limited, skip this flush
            }

            await PerformFlushAsync();
        }
        finally
        {
            _flushSemaphore.Release();
        }
    }

    private async Task PerformFlushAsync()
    {
        if (_currentSize == 0) return;

        var itemsToFlush = new List<BufferedItem<T>>();
        
        // Drain the buffer
        while (_buffer.TryDequeue(out var item))
        {
            itemsToFlush.Add(item);
            Interlocked.Decrement(ref _currentSize);
        }

        if (itemsToFlush.Count > 0)
        {
            _lastFlush = DateTime.UtcNow;
            await OnFlush.Invoke(itemsToFlush.ToArray());
        }
    }

    private async void OnTimerFlush(object? state)
    {
        if (_disposed) return;

        try
        {
            var now = DateTime.UtcNow;
            var shouldFlush = false;

            // Check if oldest items exceed max age
            if (_currentSize > 0)
            {
                var oldestAllowedTime = now - _maxAge;
                if (_lastFlush < oldestAllowedTime)
                {
                    shouldFlush = true;
                }
            }

            if (shouldFlush)
            {
                await TryFlushAsync(CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            // Log error in real implementation
            Console.WriteLine($"Timer flush error: {ex.Message}");
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
            _flushTimer?.Dispose();
            
            // Final flush
            try
            {
                FlushAsync().Wait(TimeSpan.FromSeconds(5));
            }
            catch
            {
                // Best effort cleanup
            }
            
            _flushSemaphore?.Dispose();
        }
        
        _disposed = true;
    }
}

/// <summary>
/// Represents an item in the buffer with timestamp.
/// </summary>
/// <typeparam name="T">Type of the buffered item</typeparam>
public class BufferedItem<T>
{
    public T Item { get; init; } = default!;
    public DateTime Timestamp { get; init; }
    public TimeSpan Age => DateTime.UtcNow - Timestamp;
}

/// <summary>
/// Statistics about buffer pool state.
/// </summary>
public class BufferPoolStats
{
    public int CurrentSize { get; init; }
    public int MaxSize { get; init; }
    public double Utilization { get; init; }
    public TimeSpan MaxAge { get; init; }
    public DateTime LastFlush { get; init; }
    public double RateLimiterUtilization { get; init; }
}

/// <summary>
/// Backpressure event information.
/// </summary>
public class BackpressureEvent
{
    public BackpressureReason Reason { get; init; }
    public int CurrentSize { get; init; }
    public int MaxSize { get; init; }
    public double Utilization { get; init; }
}

/// <summary>
/// Reasons for backpressure activation.
/// </summary>
public enum BackpressureReason
{
    BufferFull,
    RateLimited,
    TimeThreshold,
    SystemPressure
}