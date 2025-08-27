using Exercise35.Core;
using Microsoft.Extensions.Logging;

namespace Exercise35.Services;

/// <summary>
/// Temporal client service that simulates forwarding messages to Temporal instances.
/// In a real implementation, this would make HTTP/gRPC calls to Temporal.
/// For this exercise, it just receives and discards messages with per-customer BackpressureQueue=2 limiting.
/// Each customer can have up to 2 concurrent messages being processed by this Temporal instance.
/// </summary>
public class TemporalClientService : IDisposable
{
    private readonly BackpressureQueue _backpressureQueue;
    private readonly string _endpoint;
    private readonly int _instanceId;
    private readonly Microsoft.Extensions.Logging.ILogger _logger;
    private volatile bool _disposed;

    public TemporalClientService(
        string endpoint,
        int instanceId,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        _endpoint = endpoint;
        _instanceId = instanceId;
        _logger = logger;
        _backpressureQueue = new BackpressureQueue(2, $"Temporal-{instanceId}"); // BackpressureQueue=2 per customer

        _logger.LogInformation("Temporal instance {InstanceId} initialized with BackpressureQueue=2 per customer, endpoint: {Endpoint}",
            instanceId, _endpoint);
    }

    public string ServiceName => $"Temporal-{_instanceId}";
    public string Endpoint => _endpoint;
    public BackpressureStats GetStats() => _backpressureQueue.GetStats();

    /// <summary>
    /// Processes a message by receiving it and discarding it (as specified in requirements).
    /// Uses per-customer backpressure limiting.
    /// Returns true if processed successfully, false if backpressure was applied.
    /// </summary>
    public async Task<bool> ProcessMessageAsync(
        CustomerMessage message,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(TemporalClientService));

        var messageId = $"temporal-{_instanceId}-{message.CustomerId}-{DateTime.UtcNow:yyyyMMddHHmmssfff}";

        // Try to acquire backpressure slot for this customer (non-blocking)
        using var slot = await _backpressureQueue.TryAcquireAsync(message.CustomerId, messageId, cancellationToken);
        
        if (slot == null)
        {
            // Backpressure applied for this customer - reject message
            _logger.LogDebug("Temporal instance {InstanceId} rejected message for customer {CustomerId} due to per-customer backpressure",
                _instanceId, message.CustomerId);
            return false;
        }

        try
        {
            // Simulate Temporal processing time (workflow execution, etc.)
            await Task.Delay(Random.Shared.Next(30, 100), cancellationToken);

            // In a real implementation, this would:
            // 1. Start/signal a Temporal workflow
            // 2. Execute business logic
            // 3. Persist state
            // 
            // For this exercise, we just "receive and discard" as specified
            _logger.LogDebug("Temporal instance {InstanceId} processed and discarded message for customer {CustomerId}",
                _instanceId, message.CustomerId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Temporal instance {InstanceId} failed to process message for customer {CustomerId}",
                _instanceId, message.CustomerId);
            return false;
        }
        // slot is automatically disposed here, releasing the backpressure slot
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _backpressureQueue?.Dispose();
        }
    }
}

/// <summary>
/// Temporal service that hosts multiple Temporal instances.
/// Manages the lifecycle of Temporal processing instances.
/// </summary>
public class TemporalService : IDisposable
{
    private readonly List<TemporalClientService> _instances;
    private readonly ILogger<TemporalService> _logger;
    private volatile bool _disposed;

    public TemporalService(
        List<string> endpoints,
        ILogger<TemporalService> logger)
    {
        _logger = logger;
        _instances = new List<TemporalClientService>();

        for (int i = 0; i < endpoints.Count; i++)
        {
            _instances.Add(new TemporalClientService(endpoints[i], i, logger));
        }

        _logger.LogInformation("Temporal service initialized with {InstanceCount} instances", endpoints.Count);
    }

    public List<BackpressureStats> GetAllStats()
    {
        return _instances.Select(instance => instance.GetStats()).ToList();
    }

    public TemporalClientService GetInstance(int instanceId)
    {
        if (instanceId < 0 || instanceId >= _instances.Count)
            throw new ArgumentOutOfRangeException(nameof(instanceId));
        
        return _instances[instanceId];
    }

    public int InstanceCount => _instances.Count;

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            foreach (var instance in _instances)
            {
                instance.Dispose();
            }
            _instances.Clear();
        }
    }
}