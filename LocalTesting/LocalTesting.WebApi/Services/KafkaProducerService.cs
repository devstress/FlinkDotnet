using Confluent.Kafka;
using LocalTesting.WebApi.Models;
using LocalTesting.Shared.Constants;
using System.Text.Json;

namespace LocalTesting.WebApi.Services;

public class KafkaProducerService : IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;
    private readonly IConfiguration _configuration; // Still needed for configuration settings
    private readonly AsyncBufferedObservabilityService _asyncMetricsService;
    private readonly IMessageStateService _messageStateService;

    public KafkaProducerService(
        IProducer<string, string> producer, // Injected Aspire-managed producer
        ILogger<KafkaProducerService> logger, 
        IConfiguration configuration, // Still needed for application settings
        AsyncBufferedObservabilityService asyncMetricsService, 
        IMessageStateService messageStateService)
    {
        _producer = producer;
        _logger = logger;
        _configuration = configuration;
        _asyncMetricsService = asyncMetricsService;
        _messageStateService = messageStateService;
        _logger.LogInformation("KafkaProducerService created with Aspire-managed Kafka producer and AsyncBufferedObservabilityService for high-performance metrics");
    }

    public async Task ProduceMessagesAsync(string topic, List<ComplexLogicMessage> messages)
    {
        var startTime = DateTime.UtcNow;
        
        // Check for high-performance mode configuration
        var highPerformanceMode = _configuration.GetValue<bool>("Kafka:HighPerformanceMode", false);

        _logger.LogInformation("Producing {MessageCount} messages to topic '{Topic}' with {Mode} mode", 
            messages.Count, topic, highPerformanceMode ? "HIGH PERFORMANCE" : "FULL OBSERVABILITY");

        // Use the Aspire-managed producer directly

        if (highPerformanceMode)
        {
            // OPTIMIZED HIGH-PERFORMANCE MODE: Minimal overhead for thousands msg/sec
            await ProduceMessagesHighPerformanceAsync(topic, messages, _producer, startTime);
        }
        else
        {
            // FULL OBSERVABILITY MODE: Complete tracking and state management
            await ProduceMessagesFullObservabilityAsync(topic, messages, _producer, startTime);
        }
    }

    /// <summary>
    /// High-performance message production with batch submission and direct flush
    /// Optimized for thousands of messages per second throughput using synchronous Produce calls
    /// </summary>
    private async Task ProduceMessagesHighPerformanceAsync(string topic, List<ComplexLogicMessage> messages, 
        IProducer<string, string> producer, DateTime startTime)
    {
        var totalSuccessCount = 0;
        var totalFailureCount = 0;

        // Group messages by partition for parallel processing
        var messagesByPartition = messages
            .GroupBy(m => m.PartitionNumber)
            .ToDictionary(g => g.Key, g => g.OrderBy(m => m.MessageId).ToList());

        _logger.LogInformation("HIGH PERFORMANCE BATCH: Messages distributed across {PartitionCount} partitions: {PartitionDistribution}", 
            messagesByPartition.Count, 
            string.Join(", ", messagesByPartition.Select(kv => $"P{kv.Key}={kv.Value.Count}")));

        // Process partitions in parallel with BATCH SUBMISSION and DIRECT FLUSH
        var partitionTasks = messagesByPartition.Select(async partitionGroup =>
        {
            var partitionNumber = partitionGroup.Key;
            var partitionMessages = partitionGroup.Value;
            var partitionSuccessCount = 0;
            var partitionFailureCount = 0;

            // BATCH SUBMISSION: Use synchronous Produce calls for maximum speed
            const int batchSize = 1000; // Optimized batch size for thousands msg/sec
            var messageBatches = partitionMessages
                .Select((message, index) => new { message, index })
                .GroupBy(x => x.index / batchSize)
                .Select(g => g.Select(x => x.message).ToList())
                .ToList();
            
            _logger.LogDebug("HIGH PERFORMANCE Partition {PartitionNumber}: Processing {MessageCount} messages in {BatchCount} batches", 
                partitionNumber, partitionMessages.Count, messageBatches.Count);
            
            foreach (var messageBatch in messageBatches)
            {
                var batchStartTime = DateTime.UtcNow;
                var topicPartition = new TopicPartition(topic, new Partition(partitionNumber));
                
                // SYNCHRONOUS BATCH SUBMISSION: No async overhead per message
                await Task.Run(() =>
                {
                    foreach (var message in messageBatch)
                    {
                        try
                        {
                            var jsonMessage = JsonSerializer.Serialize(message);
                            
                            var kafkaMessage = new Message<string, string>
                            {
                                Key = message.CorrelationId,
                                Value = jsonMessage
                                // NO HEADERS: Maximum performance with minimal overhead
                            };
                            
                            // SYNCHRONOUS PRODUCE: Eliminates async task overhead
                            producer.Produce(topicPartition, kafkaMessage, (deliveryReport) =>
                            {
                                if (deliveryReport.Status == PersistenceStatus.Persisted)
                                {
                                    Interlocked.Increment(ref partitionSuccessCount);
                                }
                                else
                                {
                                    Interlocked.Increment(ref partitionFailureCount);
                                    _logger.LogWarning("Failed to produce message to partition {PartitionNumber}: {Status}", 
                                        partitionNumber, deliveryReport.Status);
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            Interlocked.Increment(ref partitionFailureCount);
                            _logger.LogError(ex, "Error producing message {MessageId} to partition {PartitionNumber}", 
                                message.MessageId, partitionNumber);
                        }
                    }
                    
                    // DIRECT FLUSH: Force immediate batch submission
                    producer.Flush(TimeSpan.FromSeconds(10));
                });
                
                var batchLatency = (DateTime.UtcNow - batchStartTime).TotalMilliseconds;
                var batchThroughput = messageBatch.Count / Math.Max(batchLatency / 1000.0, 0.001);
                
                _logger.LogDebug("HIGH PERFORMANCE Partition {PartitionNumber} batch: {BatchSize} messages in {LatencyMs}ms = {Throughput:F0} msg/sec", 
                    partitionNumber, messageBatch.Count, batchLatency, batchThroughput);
                
                // ASYNC BUFFERED METRICS: Fire-and-forget, no blocking
                _asyncMetricsService.RecordKafkaProducerMessage(topic, $"partition-{partitionNumber}", 
                    messageBatch.Count, messageBatch.Count * 1024); // Estimated size
            }

            _logger.LogInformation("HIGH PERFORMANCE Partition {PartitionNumber}: {SuccessCount} successful, {FailureCount} failed", 
                partitionNumber, partitionSuccessCount, partitionFailureCount);
            
            // Add partition results to total
            Interlocked.Add(ref totalSuccessCount, partitionSuccessCount);
            Interlocked.Add(ref totalFailureCount, partitionFailureCount);
        });

        // Wait for all partitions to complete
        await Task.WhenAll(partitionTasks);
        
        // FINAL FLUSH: Ensure all messages are committed
        producer.Flush(TimeSpan.FromSeconds(30));

        var totalTime = (DateTime.UtcNow - startTime).TotalSeconds;
        var messagesPerSecond = totalSuccessCount / Math.Max(totalTime, 1.0);
        
        _logger.LogInformation("HIGH PERFORMANCE BATCH production completed: {SuccessCount} successful, {FailureCount} failed, {MessagesPerSecond:F2} msg/sec", 
            totalSuccessCount, totalFailureCount, messagesPerSecond);
            
        // Log per-partition throughput for performance analysis
        foreach (var partitionGroup in messagesByPartition)
        {
            var partitionThroughput = partitionGroup.Value.Count / Math.Max(totalTime, 1.0);
            _logger.LogInformation("HIGH PERFORMANCE Partition {PartitionNumber}: {MessagesPerSecond:F2} msg/sec", 
                partitionGroup.Key, partitionThroughput);
        }
    }

    /// <summary>
    /// Full observability message production with complete tracking and state management
    /// Used when detailed monitoring and debugging is needed
    /// </summary>
    private async Task ProduceMessagesFullObservabilityAsync(string topic, List<ComplexLogicMessage> messages, 
        IProducer<string, string> producer, DateTime startTime)
    {
        var totalSuccessCount = 0;
        var totalFailureCount = 0;

        // Group messages by partition for parallel processing while maintaining FIFO within each partition
        var messagesByPartition = messages
            .GroupBy(m => m.PartitionNumber)
            .ToDictionary(g => g.Key, g => g.OrderBy(m => m.MessageId).ToList());

        _logger.LogInformation("FULL OBSERVABILITY: Messages distributed across {PartitionCount} partitions: {PartitionDistribution}", 
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
                    
                    // ASYNC BUFFERED METRICS: Fire-and-forget high-performance recording
                    _asyncMetricsService.RecordKafkaProducerMessage(topic, partitionName, batchStateUpdates.Count, 
                        batchStateUpdates.Sum(x => (int)x.metadata["messageSize"]!));
                    _asyncMetricsService.RecordKafkaProducerLatency(topic, batchLatency);
                    
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
        
        _logger.LogInformation("FULL OBSERVABILITY production completed: {SuccessCount} successful, {FailureCount} failed, {MessagesPerSecond:F2} msg/sec", 
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
            BootstrapServers = _configuration["KAFKA_BOOTSTRAP_SERVERS"] ?? PortConstants.KafkaBootstrapServers("localhost"),
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
                            
                            // ASYNC BUFFERED METRICS: Fire-and-forget high-performance recording
                            var partition = result.Partition.Value.ToString();
                            _asyncMetricsService.RecordKafkaConsumerMessage(topic, partition, consumerGroup, 1);
                            
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

    /// <summary>
    /// Test Kafka connectivity without producing messages - for infrastructure readiness validation
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            _logger.LogDebug("🔍 Testing Kafka connectivity...");
            
            // Create a temporary producer just for connection testing
            var config = new ProducerConfig
            {
                BootstrapServers = _configuration["KAFKA_BOOTSTRAP_SERVERS"] ?? PortConstants.KafkaBootstrapServers("localhost"),
                ClientId = "LocalTesting.HealthCheck.Producer",
                MessageTimeoutMs = 15000,  // Increased timeout for container startup (5s -> 15s)
                RequestTimeoutMs = 10000,  // Increased request timeout (3s -> 10s)  
                SocketTimeoutMs = 8000,    // Increased socket timeout (2s -> 8s)
                // Minimal settings for quick connection test
                Acks = Acks.None,          // No acknowledgment needed for health check
                EnableIdempotence = false, // Disable for speed
                MessageSendMaxRetries = 2, // Allow some retries for container startup (0 -> 2)
                QueueBufferingMaxMessages = 1000 // Minimal buffer for health check
            };

            using var testProducer = new ProducerBuilder<string, string>(config).Build();
            
            // Test connectivity by attempting a simple produce operation with test message
            var testMessage = new Message<string, string> 
            { 
                Key = $"health-check-{Guid.NewGuid():N}",
                Value = $"health-check-{DateTime.UtcNow:O}" 
            };
            
            // Use a test topic (will be created automatically or fail gracefully)
            var deliveryReport = await testProducer.ProduceAsync("health-check-topic", testMessage);
            
            if (deliveryReport.Status == PersistenceStatus.Persisted || deliveryReport.Status == PersistenceStatus.PossiblyPersisted)
            {
                _logger.LogDebug("✅ Kafka connectivity test successful - message produced to partition {Partition}", deliveryReport.Partition.Value);
                return true;
            }
            else
            {
                _logger.LogDebug("⚠️ Kafka connectivity test failed - message not persisted: {Status}", deliveryReport.Status);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("⚠️ Kafka connectivity test failed: {Error}", ex.Message);
            return false;
        }
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
}