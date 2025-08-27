using Confluent.Kafka;
using Exercise35.Core;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Exercise35.Services;

/// <summary>
/// Gateway service that produces messages to Kafka with per-customer BackpressureQueue=2 limiting.
/// Simulates the producer side of the Gateway → Kafka → Flink → Temporal flow.
/// Each customer can have up to 2 concurrent messages being processed by this gateway.
/// </summary>
public class GatewayService : IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly BackpressureQueue _backpressureQueue;
    private readonly ILogger<GatewayService> _logger;
    private readonly string _topicName;
    private readonly int _gatewayId;
    private volatile bool _disposed;

    public GatewayService(
        string bootstrapServers,
        string topicName,
        int gatewayId,
        ILogger<GatewayService> logger)
    {
        _topicName = topicName;
        _gatewayId = gatewayId;
        _logger = logger;
        _backpressureQueue = new BackpressureQueue(2, $"Gateway-{gatewayId}"); // BackpressureQueue=2 per customer

        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            ClientId = $"gateway-{gatewayId}",
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageTimeoutMs = 30000,
            RequestTimeoutMs = 30000,
            // Optimize for backpressure scenarios
            LingerMs = 5, // Small linger to batch messages efficiently
            BatchSize = 16384 // Reasonable batch size
        };

        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka producer error: {Error}", e))
            .Build();

        _logger.LogInformation("Gateway {GatewayId} initialized with BackpressureQueue=2 per customer", gatewayId);
    }

    public string ServiceName => $"Gateway-{_gatewayId}";
    public BackpressureStats GetStats() => _backpressureQueue.GetStats();

    /// <summary>
    /// Sends messages to Kafka with per-customer backpressure control.
    /// Each customer can have up to 2 concurrent messages in processing.
    /// Returns the number of messages successfully sent.
    /// </summary>
    public async Task<SendResult> SendMessagesAsync(
        List<CustomerMessage> messages,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(GatewayService));

        var result = new SendResult();
        var semTasks = new List<Task>();

        foreach (var message in messages)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            // Apply per-customer backpressure - group by customer for processing
            var task = ProcessSingleMessageAsync(message, result, cancellationToken);
            semTasks.Add(task);

            // Limit concurrent processing to prevent overwhelming the system
            if (semTasks.Count >= 10) // Allow some queuing but not unlimited
            {
                await Task.WhenAny(semTasks);
                semTasks.RemoveAll(t => t.IsCompleted);
            }
        }

        // Wait for remaining tasks
        await Task.WhenAll(semTasks);

        _logger.LogInformation("Gateway {GatewayId} completed batch: {Sent} sent, {Dropped} dropped due to per-customer backpressure",
            _gatewayId, result.MessagesSent, result.MessagesDropped);

        return result;
    }

    private async Task ProcessSingleMessageAsync(
        CustomerMessage message, 
        SendResult result,
        CancellationToken cancellationToken)
    {
        var messageId = $"gw{_gatewayId}-{message.CustomerId}-{message.Timestamp:yyyyMMddHHmmssfff}";

        // Try to acquire backpressure slot for this customer (non-blocking)
        using var slot = await _backpressureQueue.TryAcquireAsync(message.CustomerId, messageId, cancellationToken);
        
        if (slot == null)
        {
            // Backpressure applied for this customer - drop message
            Interlocked.Increment(ref result.MessagesDropped);
            _logger.LogDebug("Gateway {GatewayId} dropped message {MessageId} for customer {CustomerId} due to per-customer backpressure", 
                _gatewayId, messageId, message.CustomerId);
            return;
        }

        try
        {
            // Simulate message processing time
            await Task.Delay(Random.Shared.Next(10, 50), cancellationToken);

            var messageJson = JsonSerializer.Serialize(message);
            var kafkaMessage = new Message<string, string>
            {
                Key = message.CustomerId.ToString(),
                Value = messageJson,
                Headers = new Headers
                {
                    { "MessageId", System.Text.Encoding.UTF8.GetBytes(messageId) },
                    { "GatewayId", System.Text.Encoding.UTF8.GetBytes(_gatewayId.ToString()) },
                    { "Timestamp", System.Text.Encoding.UTF8.GetBytes(message.Timestamp.ToString("O")) }
                }
            };

            var deliveryResult = await _producer.ProduceAsync(_topicName, kafkaMessage, cancellationToken);
            
            Interlocked.Increment(ref result.MessagesSent);
            _logger.LogDebug("Gateway {GatewayId} sent message {MessageId} for customer {CustomerId} to partition {Partition}",
                _gatewayId, messageId, message.CustomerId, deliveryResult.Partition.Value);
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref result.MessagesFailed);
            _logger.LogError(ex, "Gateway {GatewayId} failed to send message {MessageId} for customer {CustomerId}", 
                _gatewayId, messageId, message.CustomerId);
        }
        // slot is automatically disposed here, releasing the backpressure slot
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _producer?.Dispose();
            _backpressureQueue?.Dispose();
        }
    }
}

/// <summary>
/// Result of a batch send operation.
/// </summary>
public class SendResult
{
    public int MessagesSent;
    public int MessagesDropped;
    public int MessagesFailed;
    
    public int TotalProcessed => MessagesSent + MessagesDropped + MessagesFailed;
    public double SuccessRate => TotalProcessed > 0 ? (double)MessagesSent / TotalProcessed * 100 : 0;
}

/// <summary>
/// Message structure for customer data.
/// </summary>
public class CustomerMessage
{
    public int CustomerId { get; set; }
    public string Data { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string MessageType { get; set; } = "CustomerEvent";
}