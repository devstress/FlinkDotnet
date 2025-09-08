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
                        ClientId = "LocalTesting.WebApi.Producer.ThousandMsgPerSec",
                        Acks = Acks.Leader,  // Use leader ack for maximum speed (instead of All)
                        MessageTimeoutMs = 60000,   // Reduced timeout for faster failure detection
                        RequestTimeoutMs = 30000,   // Reduced request timeout for speed
                        EnableIdempotence = false,  // Disable for maximum speed
                        CompressionType = CompressionType.Lz4,  // Faster compression than Snappy
                        BatchSize = 1048576,  // 1MB batch size for maximum throughput (increased from 512KB)
                        LingerMs = 5,        // Slightly longer linger for better batching efficiency
                        QueueBufferingMaxMessages = 10000000,  // 10M message buffer for ultra-high throughput
                        QueueBufferingMaxKbytes = 8192 * 1024,  // 8GB buffer for ultra-high-speed production
                        MessageSendMaxRetries = 0,  // No retries for maximum speed
                        RetryBackoffMs = 10,        // Minimal retry backoff
                        // Ultra-high performance settings optimized for thousands msg/sec
                        MaxInFlight = 50,           // Increased in-flight requests for maximum parallelism
                        DeliveryReportFields = "none", // No delivery reports for maximum speed
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

            // OPTIMIZED: Large batch processing for thousands msg/sec throughput
            // Process messages in larger batches to reduce async overhead significantly
            const int optimizedBatchSize = 2000; // Increased from 100 to 2000 for better performance
            var messageBatches = partitionMessages
                .Select((message, index) => new { message, index })
                .GroupBy(x => x.index / optimizedBatchSize)
                .Select(g => g.Select(x => x.message).ToList())
                .ToList();
            
            _logger.LogDebug("Partition {PartitionNumber}: Processing {MessageCount} messages in {BatchCount} optimized batches", 
                partitionNumber, partitionMessages.Count, messageBatches.Count);
            
            foreach (var messageBatch in messageBatches)
            {
                var batchStartTime = DateTime.UtcNow;
                var batchTasks = new List<Task>();
                var batchStateUpdates = new List<(string trackingId, Dictionary<string, object?> metadata)>();
                
                // FIRE-AND-FORGET: Prepare all messages quickly without waiting
                foreach (var message in messageBatch)
                {
                    var jsonMessage = JsonSerializer.Serialize(message);
                    var messageBytes = System.Text.Encoding.UTF8.GetByteCount(jsonMessage);
                    
                    // Simplified tracking ID generation for speed
                    var trackingId = $"kafka-{partitionNumber}-{message.MessageId}";
                    
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
                    
                    // PERFORMANCE OPTIMIZATION: Simplified producer call without delivery report processing
                    var produceTask = producer.ProduceAsync(topicPartition, kafkaMessage)
                        .ContinueWith(deliveryReportTask =>
                        {
                            var latencySeconds = (DateTime.UtcNow - messageStartTime).TotalSeconds;
                            
                            if (deliveryReportTask.IsCompletedSuccessfully && 
                                deliveryReportTask.Result.Status == PersistenceStatus.Persisted)
                            {
                                Interlocked.Increment(ref partitionSuccessCount);
                                
                                // BATCHED: Collect metrics for batch processing instead of individual recording
                                lock (batchStateUpdates)
                                {
                                    batchStateUpdates.Add((trackingId, new Dictionary<string, object?>
                                    {
                                        ["kafka.persistenceStatus"] = deliveryReportTask.Result.Status.ToString(),
                                        ["kafka.offset"] = deliveryReportTask.Result.Offset.Value,
                                        ["kafka.latencySeconds"] = latencySeconds,
                                        ["kafka.actualPartition"] = deliveryReportTask.Result.Partition.Value,
                                        ["messageSize"] = messageBytes
                                    }));
                                }
                            }
                            else
                            {
                                Interlocked.Increment(ref partitionFailureCount);
                                _logger.LogWarning("Failed to produce message {MessageId} to partition {PartitionNumber}", 
                                    message.MessageId, partitionNumber);
                            }
                        }, TaskContinuationOptions.ExecuteSynchronously);

                    batchTasks.Add(produceTask);
                    
                    // ASYNC START: Start tracking asynchronously without waiting
                    _ = Task.Run(async () => await _messageStateService.StartTrackingAsync(trackingId, MessageState.Produced, new Dictionary<string, object?>
                    {
                        ["topic"] = topic,
                        ["partition"] = partitionNumber.ToString(),
                        ["correlationId"] = message.CorrelationId,
                        ["originalMessageId"] = message.MessageId.ToString(),
                        ["messageSize"] = messageBytes
                    }));
                }
                
                // Wait for all messages in this batch to complete
                await Task.WhenAll(batchTasks);
                
                // BATCH PROCESSING: Record metrics and update states in batch
                if (batchStateUpdates.Count > 0)
                {
                    var batchLatency = (DateTime.UtcNow - batchStartTime).TotalSeconds;
                    
                    // Record metrics for all successful messages in this batch
                    _metricsService.RecordKafkaProducerMessage(topic, partitionName, batchStateUpdates.Count, 
                        batchStateUpdates.Sum(x => (int)x.metadata["messageSize"]!));
                    _metricsService.RecordKafkaProducerLatency(topic, batchLatency);
                    _metricsService.RecordFlowKafkaToFlink(batchStateUpdates.Count);
                    
                    // Update all message states in batch (fire-and-forget for performance)
                    _ = Task.Run(async () =>
                    {
                        foreach (var (trackingId, metadata) in batchStateUpdates)
                        {
                            await _messageStateService.UpdateMetadataAsync(trackingId, metadata);
                        }
                    });
                }
                
                // Log batch completion for monitoring
                _logger.LogDebug("Partition {PartitionNumber} batch completed: {SuccessCount} messages in {LatencyMs}ms", 
                    partitionNumber, batchStateUpdates.Count, (DateTime.UtcNow - batchStartTime).TotalMilliseconds);
            }

            // Add partition results to total
            Interlocked.Add(ref totalSuccessCount, partitionSuccessCount);
            Interlocked.Add(ref totalFailureCount, partitionFailureCount);

            _logger.LogInformation("Partition {PartitionNumber} completed: {SuccessCount} successful, {FailureCount} failed", 
                partitionNumber, partitionSuccessCount, partitionFailureCount);
        });

        // Wait for all partitions to complete
        await Task.WhenAll(partitionTasks);
        
        // Ensure all messages are flushed with optimized timeout for thousands msg/sec
        producer.Flush(TimeSpan.FromMinutes(1)); // 1 minute flush timeout optimized for performance

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