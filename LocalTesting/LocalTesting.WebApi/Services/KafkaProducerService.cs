using Confluent.Kafka;
using LocalTesting.WebApi.Models;
using System.Text.Json;

namespace LocalTesting.WebApi.Services;

public class KafkaProducerService : IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;
    private readonly IConfiguration _configuration;

    public KafkaProducerService(ILogger<KafkaProducerService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        var config = new ProducerConfig
        {
            BootstrapServers = _configuration["KAFKA_BOOTSTRAP_SERVERS"] ?? "localhost:9092,localhost:9093,localhost:9094",
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

        _logger.LogInformation("KafkaProducerService initialized with bootstrap servers: {BootstrapServers}", config.BootstrapServers);
    }

    public async Task ProduceMessagesAsync(string topic, List<ComplexLogicMessage> messages)
    {
        var tasks = new List<Task>();
        var successCount = 0;
        var failureCount = 0;

        _logger.LogInformation("Producing {MessageCount} messages to topic '{Topic}'", messages.Count, topic);

        foreach (var message in messages)
        {
            var jsonMessage = JsonSerializer.Serialize(message);
            var kafkaMessage = new Message<string, string>
            {
                Key = message.CorrelationId,
                Value = jsonMessage,
                Headers = new Headers
                {
                    { "correlation.id", System.Text.Encoding.UTF8.GetBytes(message.CorrelationId) },
                    { "message.id", System.Text.Encoding.UTF8.GetBytes(message.MessageId.ToString()) },
                    { "batch.number", System.Text.Encoding.UTF8.GetBytes(message.BatchNumber.ToString()) },
                    { "timestamp", System.Text.Encoding.UTF8.GetBytes(message.Timestamp.ToString("O")) }
                }
            };

            var task = _producer.ProduceAsync(topic, kafkaMessage)
                .ContinueWith(deliveryReport =>
                {
                    if (deliveryReport.Result.Status == PersistenceStatus.Persisted)
                    {
                        Interlocked.Increment(ref successCount);
                    }
                    else
                    {
                        Interlocked.Increment(ref failureCount);
                        _logger.LogWarning("Failed to produce message {MessageId}: {Status}", 
                            message.MessageId, deliveryReport.Result.Status);
                    }
                });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        _producer.Flush(TimeSpan.FromSeconds(30));

        _logger.LogInformation("Message production completed: {SuccessCount} successful, {FailureCount} failed", 
            successCount, failureCount);
    }

    public async Task<List<ComplexLogicMessage>> ConsumeMessagesAsync(string topic, string consumerGroup, int maxMessages = 1000, TimeSpan? timeout = null)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["KAFKA_BOOTSTRAP_SERVERS"] ?? "localhost:9092,localhost:9093,localhost:9094",
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

        _logger.LogInformation("Consumed {MessageCount} messages from topic '{Topic}'", messages.Count, topic);
        return messages;
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
}