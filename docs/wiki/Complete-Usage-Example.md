# Complete Usage Example: Rate Limiting with Kafka Backend

This example demonstrates how to use Flink.NET backpressure in a real .NET application with Kafka backend connectivity.

## 📋 Context

**This code runs in your .NET producer/consumer applications, NOT in Flink jobs.**

The rate limiting happens in your application before messages are sent to Kafka or after they're consumed from Kafka. Flink processes the rate-limited messages from Kafka topics.

## 🔧 Complete Example

### 1. Configuration (appsettings.json)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Flink.JobBuilder.Backpressure": "Debug"
    }
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "TopicName": "rate-limiter-state",
    "Security": {
      "SecurityProtocol": "PLAINTEXT"
    }
  },
  "RateLimiting": {
    "DefaultRateLimit": 1000,
    "DefaultBurstCapacity": 2000,
    "BufferMaxSize": 1000,
    "BufferMaxAge": "00:00:05"
  }
}
```

### 2. Service Implementation

```csharp
using Flink.JobBuilder.Backpressure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Confluent.Kafka;

public class MessageProcessingService
{
    private readonly ILogger<MessageProcessingService> _logger;
    private readonly TokenBucketRateLimiter _rateLimiter;
    private readonly BufferPool<ProcessedMessage> _bufferPool;
    private readonly IProducer<string, string> _kafkaProducer;

    public MessageProcessingService(
        IConfiguration configuration, 
        ILogger<MessageProcessingService> logger)
    {
        _logger = logger;
        
        // 1. Configure Kafka connection for messaging
        var kafkaBootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true
        };
        _kafkaProducer = new ProducerBuilder<string, string>(producerConfig).Build();

        // 2. Configure rate limiter with same Kafka backend for distributed state
        var kafkaConfig = new KafkaConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            Security = new KafkaSecurityConfig
            {
                SecurityProtocol = configuration["Kafka:Security:SecurityProtocol"] ?? "PLAINTEXT"
            }
        };

        _rateLimiter = RateLimiterFactory.CreateWithKafkaStorage(
            rateLimit: configuration.GetValue<double>("RateLimiting:DefaultRateLimit", 1000.0),
            burstCapacity: configuration.GetValue<double>("RateLimiting:DefaultBurstCapacity", 2000.0),
            kafkaConfig: kafkaConfig
        );

        // 3. Configure buffer pool with size and time thresholds
        _bufferPool = new BufferPool<ProcessedMessage>(
            maxSize: configuration.GetValue<int>("RateLimiting:BufferMaxSize", 1000),
            maxAge: configuration.GetValue<TimeSpan>("RateLimiting:BufferMaxAge", TimeSpan.FromSeconds(5)),
            rateLimiter: _rateLimiter
        );

        // 4. Handle buffer flush events
        _bufferPool.OnFlush += HandleBufferFlush;
        _bufferPool.OnBackpressure += HandleBackpressure;
    }

    public async Task ProcessMessageAsync(string inputMessage)
    {
        _logger.LogDebug("Processing message: {Message}", inputMessage);

        // Apply rate limiting before processing
        if (_rateLimiter.TryAcquire())
        {
            var processedMessage = new ProcessedMessage
            {
                OriginalMessage = inputMessage,
                ProcessedAt = DateTime.UtcNow,
                ProcessingId = Guid.NewGuid()
            };

            // Add to buffer with automatic size/time flushing
            var added = await _bufferPool.TryAddAsync(processedMessage);
            if (!added)
            {
                _logger.LogWarning("Buffer full - applying backpressure for message: {Message}", inputMessage);
                // Handle backpressure - could retry later, use DLQ, etc.
            }
        }
        else
        {
            _logger.LogWarning("Rate limited - message dropped or queued: {Message}", inputMessage);
            // Handle rate limiting - could queue for retry, send to DLQ, etc.
        }
    }

    private async Task HandleBufferFlush(BufferedItem<ProcessedMessage>[] items)
    {
        _logger.LogInformation("Flushing {Count} messages to Kafka", items.Length);

        try
        {
            var flushTasks = items.Select(async item => 
            {
                var message = new Message<string, string>
                {
                    Key = item.Item.ProcessingId.ToString(),
                    Value = JsonSerializer.Serialize(item.Item),
                    Timestamp = new Timestamp(item.Timestamp)
                };

                await _kafkaProducer.ProduceAsync("processed-messages", message);
            });

            await Task.WhenAll(flushTasks);
            _logger.LogInformation("Successfully flushed {Count} messages to Kafka", items.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to flush messages to Kafka");
            throw; // Will trigger retry logic in buffer pool
        }
    }

    private void HandleBackpressure(BackpressureEvent evt)
    {
        _logger.LogWarning("Backpressure triggered: {Reason}, Utilization: {Utilization:P1}", 
            evt.Reason, evt.Utilization);

        // Could implement dynamic rate adjustment here
        if (evt.Utilization > 0.95)
        {
            var newRate = _rateLimiter.CurrentRateLimit * 0.8;
            _rateLimiter.UpdateRateLimit(newRate);
            _logger.LogInformation("Reduced rate limit to {NewRate}", newRate);
        }
    }

    public void Dispose()
    {
        _rateLimiter?.Dispose();
        _bufferPool?.Dispose();
        _kafkaProducer?.Dispose();
    }
}

public class ProcessedMessage
{
    public string OriginalMessage { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
    public Guid ProcessingId { get; set; }
}
```

### 3. Dependency Injection Setup (Program.cs)

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging
builder.Logging.AddConsole();

// Register services
builder.Services.AddSingleton<MessageProcessingService>();

// For ASP.NET Core applications
// builder.Services.AddSingleton<MessageProcessingService>();
// builder.Services.AddHostedService<MessageProcessingBackgroundService>();

var host = builder.Build();

// Example usage
var messageService = host.Services.GetRequiredService<MessageProcessingService>();

// Process some messages
var messages = new[] { "Hello", "World", "Rate", "Limited", "Messages" };
foreach (var message in messages)
{
    await messageService.ProcessMessageAsync(message);
    await Task.Delay(100); // Simulate processing time
}

await host.RunAsync();
```

### 4. Background Service (Optional)

```csharp
public class MessageProcessingBackgroundService : BackgroundService
{
    private readonly MessageProcessingService _messageService;
    private readonly ILogger<MessageProcessingBackgroundService> _logger;

    public MessageProcessingBackgroundService(
        MessageProcessingService messageService,
        ILogger<MessageProcessingBackgroundService> logger)
    {
        _messageService = messageService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Message processing background service started");

        // Example: Process messages from a queue, file, API, etc.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Get messages from your source (Kafka consumer, database, API, etc.)
                var messages = await GetMessagesFromSource();
                
                foreach (var message in messages)
                {
                    await _messageService.ProcessMessageAsync(message);
                }

                await Task.Delay(1000, stoppingToken); // Process every second
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in message processing background service");
                await Task.Delay(5000, stoppingToken); // Wait before retrying
            }
        }
    }

    private async Task<string[]> GetMessagesFromSource()
    {
        // Implement your message source logic here
        // Could be Kafka consumer, database polling, HTTP API, etc.
        await Task.Delay(100);
        return new[] { $"Message {DateTime.Now:HH:mm:ss}" };
    }
}
```

## 🚀 Running the Example

### With Aspire (Recommended)

```bash
# Start Aspire containers
cd Sample/FlinkDotNet.Aspire.AppHost
dotnet run

# In another terminal, run your application
cd YourApplication
dotnet run
```

### Without Aspire (Manual Setup)

```bash
# Start Kafka
docker run -d --name kafka \
  -p 9092:9092 \
  -e KAFKA_ENABLE_KRAFT=yes \
  -e KAFKA_CFG_PROCESS_ROLES=broker,controller \
  -e KAFKA_CFG_LISTENERS=PLAINTEXT://0.0.0.0:9092,CONTROLLER://0.0.0.0:9093 \
  bitnami/kafka:latest

# Run your application
dotnet run
```

## 📊 Expected Output

```
info: MessageProcessingService[0]
      Processing message: Hello
info: MessageProcessingService[0]
      Processing message: World
info: MessageProcessingService[0]
      Flushing 2 messages to Kafka
info: MessageProcessingService[0]
      Successfully flushed 2 messages to Kafka
warn: MessageProcessingService[0]
      Rate limited - message dropped or queued: Rate
```

## 🔍 Key Points

✅ **Rate limiting happens in your .NET application** before messages reach Kafka  
✅ **Kafka is used for both messaging AND rate limiter state storage**  
✅ **Configuration is external** via appsettings.json or environment variables  
✅ **Flink processes the rate-limited messages** from Kafka topics  
✅ **Backpressure is applied automatically** when limits are exceeded  

This complete example shows how to integrate Flink.NET backpressure into a real application with proper configuration management and error handling.