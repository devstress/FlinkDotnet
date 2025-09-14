using Confluent.Kafka;
using Exercise35.Core;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Exercise35.Services;

/// <summary>
/// Flink processor service that consumes from Kafka, routes messages by customer,
/// and forwards to appropriate Temporal instances with per-customer BackpressureQueue=2 limiting.
/// Each customer can have up to 2 concurrent messages being processed by this Flink TaskManager.
/// </summary>
public class FlinkProcessorService : IDisposable
{
    private readonly IConsumer<string, string> _consumer;
    private readonly BackpressureQueue _backpressureQueue;
    private readonly Dictionary<int, TemporalClientService> _temporalClients;
    private readonly ILogger<FlinkProcessorService> _logger;
    private readonly int _taskManagerId;
    private volatile bool _disposed;
    private readonly CancellationTokenSource _processingCts;

    public FlinkProcessorService(
        string bootstrapServers,
        string topicName,
        string consumerGroup,
        int taskManagerId,
        List<string> temporalEndpoints,
        BackpressureConfiguration backpressureConfig,
        ILogger<FlinkProcessorService> logger)
    {
        _taskManagerId = taskManagerId;
        _logger = logger;
        _processingCts = new CancellationTokenSource();
        
        // Get configured BackpressureQueue limit for Flink service type
        var maxConcurrencyPerCustomer = backpressureConfig.GetMaxConcurrencyPerCustomer("Flink");
        _backpressureQueue = new BackpressureQueue(maxConcurrencyPerCustomer, $"Flink-TM-{taskManagerId}");

        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = consumerGroup,
            ClientId = $"flink-tm-{taskManagerId}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false, // Manual commit for backpressure control
            SessionTimeoutMs = 30000,
            MaxPollIntervalMs = 300000,
            // Optimize for backpressure scenarios
            FetchMinBytes = 1,
            FetchWaitMaxMs = 100,
            MaxPartitionFetchBytes = 1048576 // 1MB
        };

        _consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka consumer error: {Error}", e))
            .Build();

        _consumer.Subscribe(topicName);

        // Initialize Temporal clients - round-robin assignment
        _temporalClients = new Dictionary<int, TemporalClientService>();
        for (int i = 0; i < temporalEndpoints.Count; i++)
        {
            _temporalClients[i] = new TemporalClientService(
                temporalEndpoints[i], 
                i, 
                backpressureConfig,
                logger);
        }

        _logger.LogInformation("Flink TaskManager {TaskManagerId} initialized with BackpressureQueue={MaxConcurrency} per customer, {TemporalCount} Temporal endpoints",
            taskManagerId, maxConcurrencyPerCustomer, temporalEndpoints.Count);
    }

    public string ServiceName => $"Flink-TM-{_taskManagerId}";
    public BackpressureStats GetStats() => _backpressureQueue.GetStats();

    /// <summary>
    /// Starts processing messages from Kafka with per-customer backpressure control.
    /// </summary>
    public async Task StartProcessingAsync(CancellationToken cancellationToken = default)
    {
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _processingCts.Token);
        var stats = new ProcessingStats();

        _logger.LogInformation("Flink TaskManager {TaskManagerId} starting message processing", _taskManagerId);

        try
        {
            while (!combinedCts.Token.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(TimeSpan.FromMilliseconds(1000));
                    if (consumeResult?.Message == null)
                        continue;

                    await ProcessMessageAsync(consumeResult, stats, combinedCts.Token);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Flink TaskManager {TaskManagerId} consume error", _taskManagerId);
                    await Task.Delay(1000, combinedCts.Token);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Flink TaskManager {TaskManagerId} processing cancelled", _taskManagerId);
        }
        finally
        {
            _logger.LogInformation("Flink TaskManager {TaskManagerId} completed: {Processed} processed, {Dropped} dropped due to per-customer backpressure",
                _taskManagerId, stats.MessagesProcessed, stats.MessagesDropped);
        }
    }

    private async Task ProcessMessageAsync(
        ConsumeResult<string, string> consumeResult,
        ProcessingStats stats,
        CancellationToken cancellationToken)
    {
        var messageId = $"flink-{_taskManagerId}-{consumeResult.Partition.Value}-{consumeResult.Offset.Value}";

        // Parse message first to get customer ID
        CustomerMessage? customerMessage = null;
        try
        {
            customerMessage = JsonSerializer.Deserialize<CustomerMessage>(consumeResult.Message.Value);
            if (customerMessage == null)
            {
                _logger.LogWarning("Flink TaskManager {TaskManagerId} received invalid message: {MessageId}",
                    _taskManagerId, messageId);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Flink TaskManager {TaskManagerId} failed to parse message: {MessageId}",
                _taskManagerId, messageId);
            return;
        }

        // Try to acquire backpressure slot for this customer (non-blocking)
        using var slot = await _backpressureQueue.TryAcquireAsync(customerMessage.CustomerId, messageId, cancellationToken);
        
        if (slot == null)
        {
            // Backpressure applied for this customer - don't commit offset to create natural backpressure
            Interlocked.Increment(ref stats.MessagesDropped);
            _logger.LogDebug("Flink TaskManager {TaskManagerId} dropped message for customer {CustomerId} due to per-customer backpressure: {MessageId}",
                _taskManagerId, customerMessage.CustomerId, messageId);
            return;
        }

        try
        {
            // Route to appropriate Temporal instance based on customer ID
            var temporalInstanceId = customerMessage.CustomerId % _temporalClients.Count;
            var temporalClient = _temporalClients[temporalInstanceId];

            // Simulate Flink processing time
            await Task.Delay(Random.Shared.Next(20, 80), cancellationToken);

            // Forward to Temporal
            var forwarded = await temporalClient.ProcessMessageAsync(customerMessage, cancellationToken);
            
            if (forwarded)
            {
                // Commit offset only on successful processing
                _consumer.Commit(consumeResult);
                Interlocked.Increment(ref stats.MessagesProcessed);
                
                _logger.LogDebug("Flink TaskManager {TaskManagerId} processed and forwarded message {MessageId} for customer {CustomerId} to Temporal-{TemporalId}",
                    _taskManagerId, messageId, customerMessage.CustomerId, temporalInstanceId);
            }
            else
            {
                // Temporal was backpressured - don't commit offset
                Interlocked.Increment(ref stats.MessagesDropped);
                _logger.LogDebug("Flink TaskManager {TaskManagerId} couldn't forward message {MessageId} for customer {CustomerId} - Temporal backpressured",
                    _taskManagerId, messageId, customerMessage.CustomerId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Flink TaskManager {TaskManagerId} failed to process message {MessageId} for customer {CustomerId}",
                _taskManagerId, messageId, customerMessage.CustomerId);
            // Don't commit offset on error to allow retry
        }
        // slot is automatically disposed here, releasing the backpressure slot
    }

    public void StopProcessing()
    {
        _processingCts.Cancel();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _processingCts.Cancel();
            _consumer?.Dispose();
            _backpressureQueue?.Dispose();
            
            foreach (var client in _temporalClients.Values)
            {
                client.Dispose();
            }
            _temporalClients.Clear();
            
            _processingCts?.Dispose();
        }
    }
}

/// <summary>
/// Statistics for Flink processing.
/// </summary>
public class ProcessingStats
{
    public int MessagesProcessed;
    public int MessagesDropped;
}