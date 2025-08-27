using System.Collections.Concurrent;

namespace Exercise35.Core;

/// <summary>
/// Customer-aware backpressure queue implementation.
/// Limits concurrent processing per customer to a fixed number of messages.
/// This approach provides granular per-customer backpressure control, preventing 
/// one customer from blocking others.
/// </summary>
public class BackpressureQueue : IDisposable
{
    private readonly int _maxConcurrencyPerCustomer;
    private readonly string _serviceName;
    private readonly ConcurrentDictionary<int, SemaphoreSlim> _customerSemaphores;
    private readonly ConcurrentDictionary<string, (int CustomerId, DateTime StartTime)> _activeMessages;
    private volatile bool _disposed;

    public BackpressureQueue(int maxConcurrencyPerCustomer, string serviceName)
    {
        if (maxConcurrencyPerCustomer <= 0)
            throw new ArgumentException("Max concurrency per customer must be positive", nameof(maxConcurrencyPerCustomer));
        
        _maxConcurrencyPerCustomer = maxConcurrencyPerCustomer;
        _serviceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
        _customerSemaphores = new ConcurrentDictionary<int, SemaphoreSlim>();
        _activeMessages = new ConcurrentDictionary<string, (int, DateTime)>();
    }

    public int MaxConcurrencyPerCustomer => _maxConcurrencyPerCustomer;
    public string ServiceName => _serviceName;

    /// <summary>
    /// Gets the current number of active messages for a specific customer.
    /// </summary>
    public int GetCustomerConcurrency(int customerId)
    {
        if (_customerSemaphores.TryGetValue(customerId, out var semaphore))
        {
            return _maxConcurrencyPerCustomer - semaphore.CurrentCount;
        }
        return 0;
    }

    /// <summary>
    /// Gets the available slots for a specific customer.
    /// </summary>
    public int GetCustomerAvailableSlots(int customerId)
    {
        if (_customerSemaphores.TryGetValue(customerId, out var semaphore))
        {
            return semaphore.CurrentCount;
        }
        return _maxConcurrencyPerCustomer; // No semaphore yet means all slots available
    }

    /// <summary>
    /// Attempts to acquire a slot for message processing for a specific customer.
    /// Returns null if no slot is available for that customer (backpressure applied).
    /// Returns a MessageSlot that must be disposed when processing is complete.
    /// </summary>
    public async Task<MessageSlot?> TryAcquireAsync(int customerId, string messageId, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(BackpressureQueue));

        // Get or create semaphore for this customer
        var semaphore = _customerSemaphores.GetOrAdd(customerId, 
            _ => new SemaphoreSlim(_maxConcurrencyPerCustomer, _maxConcurrencyPerCustomer));

        // Non-blocking attempt to acquire semaphore for this customer
        if (await semaphore.WaitAsync(0, cancellationToken))
        {
            _activeMessages[messageId] = (customerId, DateTime.UtcNow);
            return new MessageSlot(this, messageId, customerId);
        }

        return null; // Backpressure - no slots available for this customer
    }

    /// <summary>
    /// Acquires a slot for message processing for a specific customer, waiting if necessary.
    /// This creates backpressure by blocking until a slot becomes available for that customer.
    /// </summary>
    public async Task<MessageSlot> AcquireAsync(int customerId, string messageId, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(BackpressureQueue));

        // Get or create semaphore for this customer
        var semaphore = _customerSemaphores.GetOrAdd(customerId, 
            _ => new SemaphoreSlim(_maxConcurrencyPerCustomer, _maxConcurrencyPerCustomer));

        await semaphore.WaitAsync(cancellationToken);
        _activeMessages[messageId] = (customerId, DateTime.UtcNow);
        return new MessageSlot(this, messageId, customerId);
    }

    internal void Release(string messageId, int customerId)
    {
        _activeMessages.TryRemove(messageId, out _);
        
        if (_customerSemaphores.TryGetValue(customerId, out var semaphore))
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Gets aggregated statistics about current queue state for monitoring.
    /// </summary>
    public BackpressureStats GetStats()
    {
        var totalConcurrency = _activeMessages.Count;
        var totalCustomers = _customerSemaphores.Count;
        var customerStats = new List<CustomerBackpressureStats>();

        foreach (var kvp in _customerSemaphores)
        {
            var customerId = kvp.Key;
            var semaphore = kvp.Value;
            var currentConcurrency = _maxConcurrencyPerCustomer - semaphore.CurrentCount;
            var activeMessagesForCustomer = _activeMessages.Values
                .Where(v => v.CustomerId == customerId)
                .ToList();

            customerStats.Add(new CustomerBackpressureStats
            {
                CustomerId = customerId,
                MaxConcurrency = _maxConcurrencyPerCustomer,
                CurrentConcurrency = currentConcurrency,
                AvailableSlots = semaphore.CurrentCount,
                UtilizationPercentage = (double)currentConcurrency / _maxConcurrencyPerCustomer * 100,
                ActiveMessageCount = activeMessagesForCustomer.Count,
                OldestMessageAge = activeMessagesForCustomer.Any() 
                    ? DateTime.UtcNow - activeMessagesForCustomer.Min(msg => msg.StartTime)
                    : TimeSpan.Zero
            });
        }

        return new BackpressureStats
        {
            ServiceName = _serviceName,
            MaxConcurrencyPerCustomer = _maxConcurrencyPerCustomer,
            TotalConcurrency = totalConcurrency,
            ActiveCustomers = totalCustomers,
            TotalActiveMessages = _activeMessages.Keys.ToList(),
            CustomerStats = customerStats,
            OldestMessageAge = _activeMessages.Values.Any() 
                ? DateTime.UtcNow - _activeMessages.Values.Min(v => v.StartTime) 
                : TimeSpan.Zero
        };
    }

    /// <summary>
    /// Gets statistics for a specific customer.
    /// </summary>
    public CustomerBackpressureStats? GetCustomerStats(int customerId)
    {
        if (!_customerSemaphores.TryGetValue(customerId, out var semaphore))
            return null;

        var currentConcurrency = _maxConcurrencyPerCustomer - semaphore.CurrentCount;
        var activeMessagesForCustomer = _activeMessages.Values
            .Where(v => v.CustomerId == customerId)
            .ToList();

        return new CustomerBackpressureStats
        {
            CustomerId = customerId,
            MaxConcurrency = _maxConcurrencyPerCustomer,
            CurrentConcurrency = currentConcurrency,
            AvailableSlots = semaphore.CurrentCount,
            UtilizationPercentage = (double)currentConcurrency / _maxConcurrencyPerCustomer * 100,
            ActiveMessageCount = activeMessagesForCustomer.Count,
            OldestMessageAge = activeMessagesForCustomer.Any() 
                ? DateTime.UtcNow - activeMessagesForCustomer.Min(msg => msg.StartTime)
                : TimeSpan.Zero
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            
            foreach (var semaphore in _customerSemaphores.Values)
            {
                semaphore.Dispose();
            }
            _customerSemaphores.Clear();
            _activeMessages.Clear();
        }
    }
}

/// <summary>
/// Represents a slot in the backpressure queue for a specific customer.
/// Must be disposed when message processing is complete to release the slot.
/// </summary>
public class MessageSlot : IDisposable
{
    private readonly BackpressureQueue _queue;
    private readonly string _messageId;
    private readonly int _customerId;
    private bool _disposed;

    internal MessageSlot(BackpressureQueue queue, string messageId, int customerId)
    {
        _queue = queue;
        _messageId = messageId;
        _customerId = customerId;
    }

    public string MessageId => _messageId;
    public int CustomerId => _customerId;
    public DateTime AcquiredAt { get; } = DateTime.UtcNow;

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _queue.Release(_messageId, _customerId);
        }
    }
}

/// <summary>
/// Statistics about backpressure queue state for monitoring and debugging.
/// </summary>
public class BackpressureStats
{
    public string ServiceName { get; init; } = string.Empty;
    public int MaxConcurrencyPerCustomer { get; init; }
    public int TotalConcurrency { get; init; }
    public int ActiveCustomers { get; init; }
    public List<string> TotalActiveMessages { get; init; } = new();
    public List<CustomerBackpressureStats> CustomerStats { get; init; } = new();
    public TimeSpan OldestMessageAge { get; init; }
    
    public double OverallUtilizationPercentage => ActiveCustomers > 0 
        ? CustomerStats.Average(c => c.UtilizationPercentage) 
        : 0;
}

/// <summary>
/// Statistics about backpressure queue state for a specific customer.
/// </summary>
public class CustomerBackpressureStats
{
    public int CustomerId { get; init; }
    public int MaxConcurrency { get; init; }
    public int CurrentConcurrency { get; init; }
    public int AvailableSlots { get; init; }
    public double UtilizationPercentage { get; init; }
    public int ActiveMessageCount { get; init; }
    public TimeSpan OldestMessageAge { get; init; }
}