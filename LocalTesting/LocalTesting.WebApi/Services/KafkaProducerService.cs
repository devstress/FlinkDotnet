using Confluent.Kafka;
using LocalTesting.WebApi.Models;
using System.Text.Json;

namespace LocalTesting.WebApi.Services;

public class KafkaProducerService : IDisposable
{
    private IProducer<string, string>? _producer;
    private readonly ILogger<KafkaProducerService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ObservabilityMetricsService _metricsService;
    private readonly IMessageStateService _messageStateService;
    private readonly object _lock = new object();

    public KafkaProducerService(ILogger<KafkaProducerService> logger, IConfiguration configuration, ObservabilityMetricsService metricsService, IMessageStateService messageStateService)
    {
        _logger = logger;
        _configuration = configuration;
        _metricsService = metricsService;
        _messageStateService = messageStateService;
        _logger.LogInformation("KafkaProducerService created (producer will be initialized on first use)");
    }

    private IProducer<string, string> GetOrCreateProducer()
    {
        if (_producer == null)
        {
            lock (_lock)
            {
                if (_producer == null)
                {
                    var config = new ProducerConfig
                    {
                        BootstrapServers = _configuration["KAFKA_BOOTSTRAP_SERVERS"] ?? "localhost:9092",
                        ClientId = "LocalTesting.WebApi.Producer",
                        Acks = Acks.All,
                        MessageTimeoutMs = 30000,
                        RequestTimeoutMs = 30000,
                        EnableIdempotence = true,
                        CompressionType = CompressionType.Snappy,
                        BatchSize = 32768,
                        LingerMs = 5
                    };

                    _producer = new ProducerBuilder<string, string>(config)
                        .SetErrorHandler((_, e) => _logger.LogError("Kafka producer error: {Error}", e.Reason))
                        .SetLogHandler((_, log) => _logger.LogDebug("Kafka producer log: {Message}", log.Message))
                        .Build();

                    _logger.LogInformation("Kafka producer initialized with bootstrap servers: {BootstrapServers}", config.BootstrapServers);
                }
            }
        }
        return _producer;
    }

    public async Task ProduceMessagesAsync(string topic, List<ComplexLogicMessage> messages)
    {
        var tasks = new List<Task>();
        var successCount = 0;
        var failureCount = 0;
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("Producing {MessageCount} messages to topic '{Topic}' with state tracking", messages.Count, topic);

        var producer = GetOrCreateProducer(); // Lazy initialization

        foreach (var message in messages)
        {
            var jsonMessage = JsonSerializer.Serialize(message);
            var messageBytes = System.Text.Encoding.UTF8.GetByteCount(jsonMessage);
            
            // Generate unique message ID for tracking
            var trackingId = _messageStateService.GenerateMessageId("kafka");
            
            // Start message state tracking
            await _messageStateService.StartTrackingAsync(trackingId, MessageState.Produced, new Dictionary<string, object?>
            {
                ["topic"] = topic,
                ["partition"] = message.PartitionNumber.ToString(),
                ["correlationId"] = message.CorrelationId,
                ["originalMessageId"] = message.MessageId.ToString(),
                ["messageSize"] = messageBytes
            });
            
            var kafkaMessage = new Message<string, string>
            {
                Key = message.CorrelationId,
                Value = jsonMessage,
                Headers = new Headers
                {
                    { "correlation.id", System.Text.Encoding.UTF8.GetBytes(message.CorrelationId) },
                    { "message.id", System.Text.Encoding.UTF8.GetBytes(message.MessageId.ToString()) },
                    { "tracking.id", System.Text.Encoding.UTF8.GetBytes(trackingId) },
                    { "batch.number", System.Text.Encoding.UTF8.GetBytes(message.BatchNumber.ToString()) },
                    { "timestamp", System.Text.Encoding.UTF8.GetBytes(message.Timestamp.ToString("O")) }
                }
            };

            var messageStartTime = DateTime.UtcNow;
            var partition = message.PartitionNumber.ToString();
            
            var task = producer.ProduceAsync(topic, kafkaMessage)
                .ContinueWith(async deliveryReport =>
                {
                    var latencySeconds = (DateTime.UtcNow - messageStartTime).TotalSeconds;
                    
                    if (deliveryReport.Result.Status == PersistenceStatus.Persisted)
                    {
                        Interlocked.Increment(ref successCount);
                        
                        // Record observability metrics for successful message production
                        _metricsService.RecordKafkaProducerMessage(topic, partition, 1, messageBytes);
                        _metricsService.RecordKafkaProducerLatency(topic, latencySeconds);
                        _metricsService.RecordFlowKafkaToFlink(1); // Track flow progression
                        
                        // Update message state to show successful production
                        await _messageStateService.UpdateMetadataAsync(trackingId, new Dictionary<string, object?>
                        {
                            ["kafka.persistenceStatus"] = deliveryReport.Result.Status.ToString(),
                            ["kafka.offset"] = deliveryReport.Result.Offset.Value,
                            ["kafka.latencySeconds"] = latencySeconds
                        });
                    }
                    else
                    {
                        Interlocked.Increment(ref failureCount);
                        _logger.LogWarning("Failed to produce message {MessageId}: {Status}", 
                            message.MessageId, deliveryReport.Result.Status);
                        
                        // Mark message as failed in state tracking
                        await _messageStateService.MarkAsFailedAsync(trackingId, 
                            $"Kafka production failed: {deliveryReport.Result.Status}", "KafkaProducer");
                    }
                });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        producer.Flush(TimeSpan.FromSeconds(30));

        var totalTime = (DateTime.UtcNow - startTime).TotalSeconds;
        var messagesPerSecond = successCount / Math.Max(totalTime, 1.0);
        
        _logger.LogInformation("Message production completed: {SuccessCount} successful, {FailureCount} failed, {MessagesPerSecond:F2} msg/sec", 
            successCount, failureCount, messagesPerSecond);
    }

    public async Task<List<ComplexLogicMessage>> ConsumeMessagesAsync(string topic, string consumerGroup, int maxMessages = 1000, TimeSpan? timeout = null)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["KAFKA_BOOTSTRAP_SERVERS"] ?? "localhost:9092",
            GroupId = consumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            SessionTimeoutMs = 30000,
            HeartbeatIntervalMs = 10000,
            MaxPollIntervalMs = 300000
        };

        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka consumer error: {Error}", e.Reason))
            .SetLogHandler((_, log) => _logger.LogDebug("Kafka consumer log: {Message}", log.Message))
            .Build();

        consumer.Subscribe(topic);
        
        var messages = new List<ComplexLogicMessage>();
        var endTime = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromMinutes(5));
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("Consuming up to {MaxMessages} messages from topic '{Topic}' with consumer group '{ConsumerGroup}'", 
            maxMessages, topic, consumerGroup);

        try
        {
            while (messages.Count < maxMessages && DateTime.UtcNow < endTime)
            {
                var result = consumer.Consume(TimeSpan.FromSeconds(1));
                if (result?.Message != null)
                {
                    try
                    {
                        var message = JsonSerializer.Deserialize<ComplexLogicMessage>(result.Message.Value);
                        if (message != null)
                        {
                            messages.Add(message);
                            
                            // Extract tracking ID from message headers if available
                            string? trackingId = null;
                            var trackingHeader = result.Message.Headers?.FirstOrDefault(h => h.Key == "tracking.id");
                            if (trackingHeader != null)
                            {
                                trackingId = System.Text.Encoding.UTF8.GetString(trackingHeader.GetValueBytes());
                            }
                            
                            // Record observability metrics for message consumption
                            var partition = result.Partition.Value.ToString();
                            _metricsService.RecordKafkaConsumerMessage(topic, partition, consumerGroup, 1);
                            _metricsService.RecordFlowFlinkToTemporal(1); // Track flow progression
                            
                            // Update message state if tracking ID is available
                            if (!string.IsNullOrEmpty(trackingId))
                            {
                                await _messageStateService.UpdateStateAsync(trackingId, MessageState.Consumed, "KafkaConsumer", 
                                    $"Consumed from topic {topic}, partition {partition}, offset {result.Offset.Value}");
                                
                                await _messageStateService.UpdateMetadataAsync(trackingId, new Dictionary<string, object?>
                                {
                                    ["kafka.consumedAt"] = DateTime.UtcNow,
                                    ["kafka.consumerGroup"] = consumerGroup,
                                    ["kafka.partition"] = partition,
                                    ["kafka.offset"] = result.Offset.Value
                                });
                            }
                        }
                        consumer.Commit(result);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning("Failed to deserialize message: {Error}", ex.Message);
                    }
                }
            }
        }
        catch (ConsumeException ex)
        {
            _logger.LogError("Error consuming messages: {Error}", ex.Error.Reason);
        }
        finally
        {
            consumer.Close();
        }

        var totalTime = (DateTime.UtcNow - startTime).TotalSeconds;
        var messagesPerSecond = messages.Count / Math.Max(totalTime, 1.0);

        _logger.LogInformation("Consumed {MessageCount} messages from topic '{Topic}', {MessagesPerSecond:F2} msg/sec", 
            messages.Count, topic, messagesPerSecond);
        return messages;
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
}