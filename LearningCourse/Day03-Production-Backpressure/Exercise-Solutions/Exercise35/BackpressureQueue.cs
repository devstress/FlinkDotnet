using System.Collections.Concurrent;

namespace Exercise35.Core;

/// <summary>
/// Simple semaphore-based backpressure queue implementation.
/// Limits concurrent processing to a fixed number of messages.
/// This approach is simpler than distributed rate limiting but effective for many scenarios.
/// </summary>
public class BackpressureQueue : IDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private readonly string _serviceName;
    private readonly ConcurrentDictionary<string, DateTime> _activeMessages;
    private volatile bool _disposed;

    public BackpressureQueue(int maxConcurrency, string serviceName)
    {
        if (maxConcurrency <= 0)
            throw new ArgumentException("Max concurrency must be positive", nameof(maxConcurrency));
        
        _semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        _serviceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
        _activeMessages = new ConcurrentDictionary<string, DateTime>();
        
        MaxConcurrency = maxConcurrency;
    }

    public int MaxConcurrency { get; }
    public int CurrentConcurrency => MaxConcurrency - _semaphore.CurrentCount;
    public int AvailableSlots => _semaphore.CurrentCount;
    public string ServiceName => _serviceName;

    /// <summary>
    /// Attempts to acquire a slot for message processing.
    /// Returns null if no slot is available (backpressure applied).
    /// Returns a MessageSlot that must be disposed when processing is complete.
    /// </summary>
    public async Task<MessageSlot?> TryAcquireAsync(string messageId, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(BackpressureQueue));

        // Non-blocking attempt to acquire semaphore
        if (await _semaphore.WaitAsync(0, cancellationToken))
        {
            _activeMessages[messageId] = DateTime.UtcNow;
            return new MessageSlot(this, messageId);
        }

        return null; // Backpressure - no slots available
    }

    /// <summary>
    /// Acquires a slot for message processing, waiting if necessary.
    /// This creates backpressure by blocking until a slot becomes available.
    /// </summary>
    public async Task<MessageSlot> AcquireAsync(string messageId, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(BackpressureQueue));

        await _semaphore.WaitAsync(cancellationToken);
        _activeMessages[messageId] = DateTime.UtcNow;
        return new MessageSlot(this, messageId);
    }

    internal void Release(string messageId)
    {
        _activeMessages.TryRemove(messageId, out _);
        _semaphore.Release();
    }

    /// <summary>
    /// Gets statistics about current queue state for monitoring.
    /// </summary>
    public BackpressureStats GetStats()
    {
        return new BackpressureStats
        {
            ServiceName = _serviceName,
            MaxConcurrency = MaxConcurrency,
            CurrentConcurrency = CurrentConcurrency,
            AvailableSlots = AvailableSlots,
            UtilizationPercentage = (double)CurrentConcurrency / MaxConcurrency * 100,
            ActiveMessages = _activeMessages.Keys.ToList(),
            OldestMessageAge = _activeMessages.Values.Any() 
                ? DateTime.UtcNow - _activeMessages.Values.Min() 
                : TimeSpan.Zero
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _semaphore.Dispose();
            _activeMessages.Clear();
        }
    }
}

/// <summary>
/// Represents a slot in the backpressure queue.
/// Must be disposed when message processing is complete to release the slot.
/// </summary>
public class MessageSlot : IDisposable
{
    private readonly BackpressureQueue _queue;
    private readonly string _messageId;
    private bool _disposed;

    internal MessageSlot(BackpressureQueue queue, string messageId)
    {
        _queue = queue;
        _messageId = messageId;
    }

    public string MessageId => _messageId;
    public DateTime AcquiredAt { get; } = DateTime.UtcNow;

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _queue.Release(_messageId);
        }
    }
}

/// <summary>
/// Statistics about backpressure queue state for monitoring and debugging.
/// </summary>
public class BackpressureStats
{
    public string ServiceName { get; init; } = string.Empty;
    public int MaxConcurrency { get; init; }
    public int CurrentConcurrency { get; init; }
    public int AvailableSlots { get; init; }
    public double UtilizationPercentage { get; init; }
    public List<string> ActiveMessages { get; init; } = new();
    public TimeSpan OldestMessageAge { get; init; }
}