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
                        ClientId = "LocalTesting.WebApi.Producer.MillionMsgPerSec",
                        Acks = Acks.Leader,  // Use leader ack for maximum speed (instead of All)
                        MessageTimeoutMs = 120000,  // Increased timeout for high-volume processing
                        RequestTimeoutMs = 90000,   // Increased request timeout
                        EnableIdempotence = false,  // Disable for maximum speed
                        CompressionType = CompressionType.Lz4,  // Faster compression than Snappy
                        BatchSize = 524288,  // 512KB batch size for maximum throughput (increased from 128KB)
                        LingerMs = 1,        // Ultra-short linger for million msg/sec speed
                        QueueBufferingMaxMessages = 5000000,  // 5M message buffer (increased from 2M)
                        QueueBufferingMaxKbytes = 4096 * 1024,  // 4GB buffer for ultra-high-speed production
                        MessageSendMaxRetries = 1,  // Fewer retries for speed
                        RetryBackoffMs = 25,        // Faster retry (reduced from 50ms)
                        // Ultra-high performance settings
                        MaxInFlight = 20,           // More in-flight requests for million msg/sec
                        DeliveryReportFields = "key,timestamp", // Minimal delivery report for maximum speed
                        ApiVersionRequest = true,   // Enable for better performance
                        BrokerVersionFallback = "2.8.0",
                        // Additional high-throughput optimizations
                        SocketKeepaliveEnable = true,
                        SocketNagleDisable = true   // Disable Nagle algorithm for low latency
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
        var startTime = DateTime.UtcNow;
        var totalSuccessCount = 0;
        var totalFailureCount = 0;

        _logger.LogInformation("Producing {MessageCount} messages to topic '{Topic}' with parallel partition strategy", 
            messages.Count, topic);

        var producer = GetOrCreateProducer(); // Lazy initialization

        // Group messages by partition for parallel processing while maintaining FIFO within each partition
        var messagesByPartition = messages
            .GroupBy(m => m.PartitionNumber)
            .ToDictionary(g => g.Key, g => g.OrderBy(m => m.MessageId).ToList());

        _logger.LogInformation("Messages distributed across {PartitionCount} partitions: {PartitionDistribution}", 
            messagesByPartition.Count, 
            string.Join(", ", messagesByPartition.Select(kv => $"P{kv.Key}={kv.Value.Count}")));

        // Process partitions in parallel to maximize throughput
        var partitionTasks = messagesByPartition.Select(async partitionGroup =>
        {
            var partitionNumber = partitionGroup.Key;
            var partitionMessages = partitionGroup.Value;
            var partitionSuccessCount = 0;
            var partitionFailureCount = 0;
            var partitionName = $"partition-{partitionNumber}";

            _logger.LogDebug("Starting partition {PartitionNumber} with {MessageCount} messages", 
                partitionNumber, partitionMessages.Count);

            // Ultra-high-speed batch processing for million msg/sec throughput
            // Process messages in micro-batches within partition to reduce overhead
            const int microBatchSize = 100; // Process 100 messages at a time for optimal speed
            var messageBatches = partitionMessages
                .Select((message, index) => new { message, index })
                .GroupBy(x => x.index / microBatchSize)
                .Select(g => g.Select(x => x.message).ToList())
                .ToList();
            
            _logger.LogDebug("Partition {PartitionNumber}: Processing {MessageCount} messages in {BatchCount} micro-batches", 
                partitionNumber, partitionMessages.Count, messageBatches.Count);
            
            var batchTasks = messageBatches.Select(async messageBatch =>
            {
                var batchTasks = new List<Task>();
                
                foreach (var message in messageBatch)
                {
                    var jsonMessage = JsonSerializer.Serialize(message);
                    var messageBytes = System.Text.Encoding.UTF8.GetByteCount(jsonMessage);
                    
                    // Simplified tracking ID generation for speed
                    var trackingId = $"kafka-{partitionNumber}-{message.MessageId}";
                    
                    // Batch message state tracking (reduce async overhead)
                    var trackingTask = _messageStateService.StartTrackingAsync(trackingId, MessageState.Produced, new Dictionary<string, object?>
                    {
                        ["topic"] = topic,
                        ["partition"] = partitionNumber.ToString(),
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
                            { "timestamp", System.Text.Encoding.UTF8.GetBytes(message.Timestamp.ToString("O")) },
                            { "partition.number", System.Text.Encoding.UTF8.GetBytes(partitionNumber.ToString()) }
                        }
                    };

                    var messageStartTime = DateTime.UtcNow;
                    
                    // Use TopicPartition to explicitly specify the partition
                    var topicPartition = new TopicPartition(topic, new Partition(partitionNumber));
                    
                    var produceTask = producer.ProduceAsync(topicPartition, kafkaMessage)
                        .ContinueWith(async deliveryReport =>
                        {
                            var latencySeconds = (DateTime.UtcNow - messageStartTime).TotalSeconds;
                            
                            if (deliveryReport.Result.Status == PersistenceStatus.Persisted)
                            {
                                Interlocked.Increment(ref partitionSuccessCount);
                                
                                // Record observability metrics for successful message production
                                _metricsService.RecordKafkaProducerMessage(topic, partitionName, 1, messageBytes);
                                _metricsService.RecordKafkaProducerLatency(topic, latencySeconds);
                                _metricsService.RecordFlowKafkaToFlink(1); // Track flow progression
                                
                                // Update message state to show successful production (batch optimized)
                                await _messageStateService.UpdateMetadataAsync(trackingId, new Dictionary<string, object?>
                                {
                                    ["kafka.persistenceStatus"] = deliveryReport.Result.Status.ToString(),
                                    ["kafka.offset"] = deliveryReport.Result.Offset.Value,
                                    ["kafka.latencySeconds"] = latencySeconds,
                                    ["kafka.actualPartition"] = deliveryReport.Result.Partition.Value
                                });
                            }
                            else
                            {
                                Interlocked.Increment(ref partitionFailureCount);
                                _logger.LogWarning("Failed to produce message {MessageId} to partition {PartitionNumber}: {Status}", 
                                    message.MessageId, partitionNumber, deliveryReport.Result.Status);
                                
                                // Mark message as failed in state tracking
                                await _messageStateService.MarkAsFailedAsync(trackingId, 
                                    $"Kafka production failed: {deliveryReport.Result.Status}", "KafkaProducer");
                            }
                        });

                    batchTasks.Add(produceTask);
                    batchTasks.Add(trackingTask); // Include tracking task in batch
                }
                
                // Wait for all messages in this micro-batch to complete
                await Task.WhenAll(batchTasks);
            });
            
            // Wait for all micro-batches in this partition to complete
            await Task.WhenAll(batchTasks);
            
            // Add partition results to total
            Interlocked.Add(ref totalSuccessCount, partitionSuccessCount);
            Interlocked.Add(ref totalFailureCount, partitionFailureCount);

            _logger.LogInformation("Partition {PartitionNumber} completed: {SuccessCount} successful, {FailureCount} failed", 
                partitionNumber, partitionSuccessCount, partitionFailureCount);
        });

        // Wait for all partitions to complete
        await Task.WhenAll(partitionTasks);
        
        // Ensure all messages are flushed with extended timeout for high volume
        producer.Flush(TimeSpan.FromMinutes(2)); // 2 minutes flush timeout for 100k messages

        var totalTime = (DateTime.UtcNow - startTime).TotalSeconds;
        var messagesPerSecond = totalSuccessCount / Math.Max(totalTime, 1.0);
        
        _logger.LogInformation("Message production completed: {SuccessCount} successful, {FailureCount} failed, {MessagesPerSecond:F2} msg/sec total", 
            totalSuccessCount, totalFailureCount, messagesPerSecond);
            
        // Log per-partition throughput for performance analysis
        foreach (var partitionGroup in messagesByPartition)
        {
            var partitionThroughput = partitionGroup.Value.Count / Math.Max(totalTime, 1.0);
            _logger.LogInformation("Partition {PartitionNumber} throughput: {MessagesPerSecond:F2} msg/sec", 
                partitionGroup.Key, partitionThroughput);
        }
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