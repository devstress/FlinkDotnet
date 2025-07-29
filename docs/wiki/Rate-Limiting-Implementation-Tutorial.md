# ⚠️ DEPRECATED - This Document Has Been Consolidated

> **🚨 Important Update**: This document has been consolidated into the new comprehensive reference. Please use the single reference link below instead of this scattered document.
>
> **📋 NEW SINGLE REFERENCE**: [Flink.NET Backpressure: Complete Reference Guide](Backpressure-Complete-Reference.md)
>
> **Why the change?**: Users reported the backpressure wiki was "messy" with too many scattered documents. We've consolidated everything into one comprehensive guide that covers:
> - Performance guidance (when to enable/disable rate limiting)
> - Scalability architecture (multiple consumers, logical queues)
> - Unique identifier strategy and partition relationships
> - Rebalancing integration and best practices
> - Industry patterns with scholarly references
> - Complete implementation guide with examples
>
> **🎯 This gives you everything in one place instead of hunting through multiple documents.**

---

# Rate Limiting Implementation Tutorial

**Step-by-step guide for implementing comprehensive backpressure with rate limiting in Flink.NET**

> **🚨 Flink JobManager Compatibility Note**: This tutorial has been updated to use synchronous rate limiting patterns (`rateLimiter.TryAcquire()`) that work reliably when jobs are submitted to Flink's JobManager. Async patterns (`await rateLimiter.TryAcquireAsync()`) should only be used in local development and testing. See [Flink JobManager Async Compatibility Guide](../flink-jobmanager-async-compatibility.md) for details.

This tutorial shows you how to implement the enhanced backpressure system following Apache Flink 2.0 AsyncSink patterns. Based on the official article: [Optimising the throughput of async sinks using a custom RateLimitingStrategy](https://flink.apache.org/2022/11/25/optimising-the-throughput-of-async-sinks-using-a-custom-ratelimitingstrategy/).

## Table of Contents

1. [Quick Start](#quick-start) 🚀 **START HERE**
2. [Understanding Rate Limiting](#understanding-rate-limiting)
3. [Implementation Patterns](#implementation-patterns)
4. [Buffer Pool with Thresholds](#buffer-pool-with-thresholds)
5. [Multi-Tier Enforcement](#multi-tier-enforcement)
6. [Real-World Examples](#real-world-examples)
7. [Testing and Validation](#testing-and-validation)
8. [Troubleshooting](#troubleshooting)

## Quick Start

> **📋 Usage Context**: This is **normal .NET application code** that runs in your producer/consumer applications, **NOT** code that gets serialized and submitted to Flink JobManager. The rate limiting happens in your .NET application before sending data to Kafka or processing from Kafka.

### Step 1: Add the Rate Limiting Components

```csharp
using Flink.JobBuilder.Backpressure;

// OPTION A: In-Memory Storage (Development/Testing)
var rateLimiter = new TokenBucketRateLimiter(
    rateLimit: 1000.0,      // 1000 operations per second
    burstCapacity: 2000.0   // Allow bursts up to 2000
);

// OPTION B: Kafka-Based Distributed Storage (Production)
var kafkaConfig = new KafkaConfig 
{ 
    BootstrapServers = "localhost:9092" // Your Kafka connection string
};
var rateLimiter = RateLimiterFactory.CreateWithKafkaStorage(
    rateLimit: 1000.0,
    burstCapacity: 2000.0,
    kafkaConfig: kafkaConfig
);

// Check if operation is allowed (Flink JobManager compatible)
if (rateLimiter.TryAcquire())
{
    // Process your message
    ProcessMessage(message);
}
```

### Backend API Configuration

**Where to define connection strings:**

```csharp
// In your appsettings.json
{
  "Kafka": {
    "BootstrapServers": "kafka1:9092,kafka2:9092,kafka3:9092",
    "SecurityProtocol": "PLAINTEXT"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}

// In your .NET application startup
var kafkaConfig = new KafkaConfig
{
    BootstrapServers = configuration["Kafka:BootstrapServers"]
};
```

### Step 2: Add Buffer Pool with Thresholds

> **📋 Backend Connection**: The buffer pool uses the same rate limiter with its configured backend (Kafka or in-memory).

```csharp
// Buffer pool with size AND time thresholds (as requested in issue)
var bufferPool = new BufferPool<YourMessageType>(
    maxSize: 1000,                    // Size threshold: flush at 1000 items
    maxAge: TimeSpan.FromSeconds(5),  // Time threshold: flush after 5 seconds
    rateLimiter: rateLimiter          // Uses same backend as rateLimiter above
);

// Handle flush events (use ConfigureAwait for Flink compatibility)
bufferPool.OnFlush += async items =>
{
    Console.WriteLine($"Flushing {items.Length} items");
    await BatchProcess(items.Select(i => i.Item)).ConfigureAwait(false);
};

// Add items to buffer (use ConfigureAwait for Flink compatibility)
await bufferPool.AddAsync(yourMessage).ConfigureAwait(false);
```

### Complete Producer/Consumer Example

**This code runs in your .NET application (not in Flink jobs):**

```csharp
public class KafkaProducerWithBackpressure
{
    private readonly IProducer<string, string> _kafkaProducer;
    private readonly TokenBucketRateLimiter _rateLimiter;
    private readonly BufferPool<Message> _bufferPool;

    public KafkaProducerWithBackpressure(string kafkaBootstrapServers)
    {
        // Configure Kafka connection
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = kafkaBootstrapServers
        };
        _kafkaProducer = new ProducerBuilder<string, string>(producerConfig).Build();

        // Configure rate limiter with same Kafka backend
        var kafkaConfig = new KafkaConfig { BootstrapServers = kafkaBootstrapServers };
        _rateLimiter = RateLimiterFactory.CreateWithKafkaStorage(1000.0, 2000.0, kafkaConfig);
        
        // Configure buffer pool
        _bufferPool = new BufferPool<Message>(1000, TimeSpan.FromSeconds(5), _rateLimiter);
        _bufferPool.OnFlush += FlushToKafka;
    }

    public async Task SendAsync(Message message)
    {
        // Rate limiting happens here in your application
        if (_rateLimiter.TryAcquire())
        {
            await _bufferPool.AddAsync(message);
        }
        else
        {
            // Handle backpressure - maybe retry later or drop message
            Console.WriteLine("Rate limited - message dropped or queued for retry");
        }
    }

    private async Task FlushToKafka(BufferedItem<Message>[] items)
    {
        foreach (var item in items)
        {
            await _kafkaProducer.ProduceAsync("your-topic", 
                new Message<string, string> { Value = item.Item.ToString() });
        }
    }
}

// Usage in your application
var producer = new KafkaProducerWithBackpressure("localhost:9092");
await producer.SendAsync(new Message { Data = "Hello World" });
```

### Step 3: Test Your Implementation

```bash
# Run the demo to see rate limiting in action
cd FlinkDotNet/Flink.JobBuilder
dotnet run --project Demo/RateLimitingDemo.cs

# Run integration tests
cd Sample/FlinkDotNet.Aspire.IntegrationTests  
dotnet test --filter "Category=backpressure_test"
```

## Understanding Rate Limiting

### ❓ Usage Context: Normal Code vs Flink Jobs

**The code examples in this wiki are for normal .NET applications, NOT for Flink job serialization.**

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                    HOW FLINK.NET RATE LIMITING WORKS                           │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────────────────┐ │
│  │ YOUR .NET APP   │    │ KAFKA CLUSTER   │    │     FLINK CLUSTER           │ │
│  │                 │    │                 │    │                             │ │
│  │ • Rate Limiter  │───▶│ • Topics        │───▶│ • JobManager                │ │
│  │ • Buffer Pool   │    │ • Partitions    │    │ • TaskManagers              │ │
│  │ • Backpressure  │    │ • State Storage │    │ • Stream Processing         │ │
│  │   Logic         │    │                 │    │                             │ │
│  │                 │    │                 │    │                             │ │
│  └─────────────────┘    └─────────────────┘    └─────────────────────────────┘ │
│           │                        │                           │                │
│           │              ┌─────────────────┐                  │                │
│           │              │ REDIS (Optional)│                  │                │
│           │              │                 │                  │                │
│           └──────────────│ • Additional    │──────────────────┘                │
│                          │   State Storage │                                   │
│                          │ • Caching       │                                   │
│                          └─────────────────┘                                   │
│                                                                                 │
│ ✅ Rate limiting happens in YOUR .NET APPLICATION                              │
│ ✅ Kafka/Redis are backends for distributed state storage                      │
│ ✅ Flink processes the rate-limited messages from Kafka                        │
│                                                                                 │
└─────────────────────────────────────────────────────────────────────────────────┘
```

**Key Points:**
- ✅ **Rate Limiter Code**: Runs in your .NET producer/consumer applications
- ✅ **Kafka**: Used as both message broker AND rate limiter state storage
- ✅ **Redis**: Optional additional state storage for caching
- ✅ **Flink**: Processes messages that passed through rate limiting

### 🔌 Backend API and Connection Strings

The rate limiter needs backend storage for distributed coordination. Here's where to configure connections:

#### Option 1: Kafka Backend (Recommended for Production)

**Why Kafka for state storage?**
- Same infrastructure as your message broker
- Built-in replication and scaling
- Consistent with Flink's data flow

```csharp
// Connection configuration
var kafkaConfig = new KafkaConfig
{
    BootstrapServers = "kafka1:9092,kafka2:9092,kafka3:9092",
    Security = new KafkaSecurityConfig
    {
        SecurityProtocol = "SASL_SSL",
        SaslMechanism = "PLAIN",
        SaslUsername = "your-username",
        SaslPassword = "your-password"
    }
};

// Create rate limiter with distributed state
var rateLimiter = RateLimiterFactory.CreateWithKafkaStorage(
    rateLimit: 1000.0,
    burstCapacity: 2000.0,
    kafkaConfig: kafkaConfig
);
```

#### Option 2: In-Memory Backend (Development Only)

**When to use:**
- Local development
- Testing
- Single-instance deployments where state loss is acceptable

```csharp
// No external connections required
var rateLimiter = RateLimiterFactory.CreateWithInMemoryStorage(
    rateLimit: 1000.0,
    burstCapacity: 2000.0
);
```

#### Option 3: Configuration from appsettings.json

```json
{
  "RateLimiting": {
    "Backend": "Kafka",
    "Kafka": {
      "BootstrapServers": "kafka1:9092,kafka2:9092,kafka3:9092",
      "TopicName": "rate-limiter-state",
      "ReplicationFactor": 3,
      "PartitionCount": 12
    },
    "Redis": {
      "ConnectionString": "redis-cluster:6379",
      "Database": 0
    }
  }
}
```

```csharp
// In your DI container setup
services.Configure<RateLimitingOptions>(configuration.GetSection("RateLimiting"));

// In your service
public class MessageService
{
    private readonly TokenBucketRateLimiter _rateLimiter;
    
    public MessageService(IOptions<RateLimitingOptions> options)
    {
        var config = options.Value;
        var kafkaConfig = new KafkaConfig { BootstrapServers = config.Kafka.BootstrapServers };
        
        _rateLimiter = RateLimiterFactory.CreateWithKafkaStorage(
            1000.0, 2000.0, kafkaConfig);
    }
}
```

### What Problem Does This Solve?

**Before Rate Limiting:**
```
Producer: 📤📤📤📤📤📤📤📤 (10,000 msg/sec)
Consumer: 📥📥💥 CRASH!      (Can only handle 2,000 msg/sec)
```

**After Rate Limiting:**
```
Producer: 📤📤📤📤📤📤📤📤 (10,000 msg/sec)
Rate Limiter: 🚦          (Allows 2,000 msg/sec)
Consumer: 📥📥✅ STABLE!    (Processes 2,000 msg/sec smoothly)
```

### Core Components

#### 1. IRateLimitingStrategy Interface
```csharp
public interface IRateLimitingStrategy
{
    // Async methods (for local development/testing)
    Task<bool> TryAcquireAsync(int permits = 1, CancellationToken cancellationToken = default);
    Task AcquireAsync(int permits = 1, CancellationToken cancellationToken = default);
    
    // Synchronous methods (recommended for Flink JobManager execution)
    bool TryAcquire(int permits = 1);
    
    // Dynamic rate adjustment
    void UpdateRateLimit(double newRateLimit);
    
    // Monitoring
    double CurrentRateLimit { get; }
    double CurrentUtilization { get; }
}
```

**🚨 Important:** Use synchronous methods (`TryAcquire`) in code submitted to Flink JobManager. Use async methods (`TryAcquireAsync`) only in local development and testing.

**Code Reference**: `FlinkDotNet/Flink.JobBuilder/Backpressure/IRateLimitingStrategy.cs`

## Implementation Patterns

### Pattern 1: Token Bucket (Best for Burst Traffic)

**When to use**: Your system can handle occasional bursts above the sustained rate.

```csharp
// Example: API that can handle 1000 req/sec sustained, 2000 req/sec for short bursts
var tokenBucket = new TokenBucketRateLimiter(
    rateLimit: 1000.0,      // Sustained rate
    burstCapacity: 2000.0   // Burst tolerance
);

// Burst scenario (Flink JobManager compatible pattern)
for (int i = 0; i < 1500; i++)  // Send 1500 requests quickly
{
    if (tokenBucket.TryAcquire())
    {
        ProcessRequest(requests[i]);  // First 2000 will succeed immediately
    }
    else
    {
        // Rate limited - handle gracefully (no blocking waits in Flink jobs)
        _logger.LogWarning($"Rate limited - request {i} dropped or queued");
        // Alternative: Add to retry queue or use exponential backoff
    }
}
```

**Real-world example**: Database writes that can handle 5000 writes/sec normally, but can burst to 10000 for short periods.

### Pattern 2: Sliding Window (Best for Precise Time Limits)

**When to use**: You need precise control over time windows (e.g., API rate limits).

```csharp
// Example: External API with strict "1000 requests per 10 seconds" limit
var slidingWindow = new SlidingWindowRateLimiter(
    maxRequestsPerSecond: 100.0,  // 1000 requests / 10 seconds = 100/sec average
    windowSizeSeconds: 10.0
);

// Process API requests (Flink JobManager compatible)
foreach (var apiCall in apiCalls)
{
    if (slidingWindow.TryAcquire())
    {
        try
        {
            var response = await ExternalApiClient.CallAsync(apiCall).ConfigureAwait(false);
            await ProcessResponse(response).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API call failed");
        }
    }
    else
    {
        // Rate limited - handle gracefully
        await EnqueueForLater(apiCall).ConfigureAwait(false);  // Will retry when window allows
    }
}
```

**Real-world example**: Third-party API integration with strict rate limits.

## Buffer Pool with Thresholds

The buffer pool implements the "size and time threshold" requirement from the issue.

### Basic Usage

```csharp
var bufferPool = new BufferPool<OrderEvent>(
    maxSize: 500,                      // Size threshold
    maxAge: TimeSpan.FromSeconds(10),  // Time threshold  
    rateLimiter: new TokenBucketRateLimiter(100, 200)
);

// Size threshold demo (use ConfigureAwait for Flink compatibility)
Console.WriteLine("Adding items to trigger size threshold...");
for (int i = 1; i <= 600; i++)  // Exceed maxSize of 500
{
    await bufferPool.AddAsync(new OrderEvent { Id = i }).ConfigureAwait(false);
    
    if (i == 500)
    {
        Console.WriteLine("Size threshold reached - buffer will flush automatically");
    }
}
```

### Advanced Configuration

```csharp
// Production configuration
var productionBuffer = new BufferPool<TransactionRecord>(
    maxSize: 10000,                         // Batch 10K records
    maxAge: TimeSpan.FromSeconds(30),       // Max 30 second delay
    rateLimiter: new TokenBucketRateLimiter(1000, 2000)
);

// Handle different flush triggers (use ConfigureAwait for Flink compatibility)
productionBuffer.OnFlush += async items =>
{
    var ageTriggered = items.Any(i => i.Age > TimeSpan.FromSeconds(25));
    var sizeTriggered = items.Length >= 10000;
    
    Console.WriteLine($"Flush triggered by: {(sizeTriggered ? "SIZE" : "TIME")}");
    
    try
    {
        // Batch write to database
        await DatabaseWriter.BatchInsertAsync(items.Select(i => i.Item)).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Batch write failed");
    }
};

// Monitor backpressure
productionBuffer.OnBackpressure += evt =>
{
    if (evt.Reason == BackpressureReason.BufferFull)
    {
        Console.WriteLine($"Buffer full: {evt.CurrentSize}/{evt.MaxSize}");
        
        // Apply upstream backpressure
        KafkaConsumer.Pause();
    }
};
```

## Multi-Tier Enforcement

Implements hierarchical rate limiting as referenced in the Flink article.

### Configuration

```csharp
var multiTier = new MultiTierRateLimiter();

// Configure tiers following LinkedIn best practices
var tiers = new[]
{
    // Global cluster limit
    new RateLimitingTier 
    { 
        Name = "Global", 
        Scope = "Entire cluster", 
        RateLimit = 1_000_000,     // 1M ops/sec for entire cluster
        BurstCapacity = 1_500_000,
        Enforcement = RateLimitingEnforcement.HardLimit
    },
    
    // Per-topic limit  
    new RateLimitingTier 
    { 
        Name = "Topic", 
        Scope = "Per topic", 
        RateLimit = 100_000,       // 100K ops/sec per topic
        BurstCapacity = 150_000,
        Enforcement = RateLimitingEnforcement.Throttling
    },
    
    // Per-consumer limit
    new RateLimitingTier 
    { 
        Name = "Consumer", 
        Scope = "Per instance", 
        RateLimit = 10_000,        // 10K ops/sec per consumer
        BurstCapacity = 15_000,
        Enforcement = RateLimitingEnforcement.Backpressure
    },
    
    // Per-client IP limit (prevents noisy neighbors)
    new RateLimitingTier 
    { 
        Name = "Client", 
        Scope = "Per-IP", 
        RateLimit = 5_000,         // 5K ops/sec per IP
        BurstCapacity = 7_500,
        Enforcement = RateLimitingEnforcement.CircuitBreaker
    }
};

multiTier.ConfigureTiers(tiers);
```

### Usage with Context

```csharp
// Create request context
var context = new RateLimitingContext
{
    TopicName = "user_events",
    ConsumerGroup = "analytics_group", 
    ConsumerId = "consumer_1",
    ClientIp = "192.168.1.100",
    RequestType = "batch_process"
};

// Multi-tier validation (Flink JobManager compatible - must pass ALL tiers)
if (multiTier.TryAcquire(context, permits: 100))
{
    // All tiers approved - process batch
    try
    {
        await ProcessBatch(messages).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Batch processing failed");
    }
}
else
{
    // Failed at one or more tiers
    var utilization = multiTier.GetUtilizationMetrics();
    
    // Find which tier is limiting
    var limitingTier = utilization.OrderByDescending(u => u.Value).First();
    Console.WriteLine($"Limited by {limitingTier.Key} tier at {limitingTier.Value:P1} utilization");
    
    // Apply appropriate backpressure (avoid blocking in Flink jobs)
    await ApplyBackpressure(limitingTier.Key).ConfigureAwait(false);
}
```

## Real-World Examples

### Example 1: High-Volume Order Processing

```csharp
public class OrderProcessingService
{
    private readonly MultiTierRateLimiter _rateLimiter;
    private readonly BufferPool<Order> _orderBuffer;
    
    public OrderProcessingService()
    {
        // Configure for high-volume order processing
        _rateLimiter = new MultiTierRateLimiter();
        _rateLimiter.ConfigureTiers(GetOrderProcessingTiers());
        
        _orderBuffer = new BufferPool<Order>(
            maxSize: 5000,                     // Batch 5K orders
            maxAge: TimeSpan.FromSeconds(15),  // Max 15 second delay
            rateLimiter: _rateLimiter
        );
        
        _orderBuffer.OnFlush += ProcessOrderBatch;
    }
    
    public async Task<bool> SubmitOrder(Order order)
    {
        var context = new RateLimitingContext
        {
            TopicName = "orders",
            ClientIp = order.CustomerIp,
            RequestType = order.Priority.ToString()
        };
        
        // Multi-tier rate limiting (Flink JobManager compatible)
        if (_rateLimiter.TryAcquire(context))
        {
            // Add to buffer (triggers size/time thresholds)
            return await _orderBuffer.TryAddAsync(order).ConfigureAwait(false);
        }
        
        return false; // Rate limited
    }
    
    private async Task ProcessOrderBatch(BufferedItem<Order>[] orders)
    {
        Console.WriteLine($"Processing batch of {orders.Length} orders");
        
        // Group by priority for processing
        var orderGroups = orders.GroupBy(o => o.Item.Priority);
        
        foreach (var group in orderGroups.OrderByDescending(g => g.Key))
        {
            await ProcessOrderGroup(group.Select(o => o.Item));
        }
    }
}
```

### Example 2: Database Connection Rate Limiting

```csharp
public class DatabaseService
{
    private readonly SlidingWindowRateLimiter _connectionLimiter;
    private readonly TokenBucketRateLimiter _queryLimiter;
    
    public DatabaseService()
    {
        // Limit new connections: 10 per minute
        _connectionLimiter = new SlidingWindowRateLimiter(
            maxRequestsPerSecond: 10.0 / 60.0,  // 10 per 60 seconds
            windowSizeSeconds: 60.0
        );
        
        // Limit queries: 1000/sec sustained, 2000/sec burst
        _queryLimiter = new TokenBucketRateLimiter(1000.0, 2000.0);
    }
    
    public async Task<IDbConnection> GetConnectionAsync()
    {
        // Rate limit connection creation (non-blocking for Flink JobManager)
        if (_connectionLimiter.TryAcquire())
        {
            return await CreateDatabaseConnection().ConfigureAwait(false);
        }
        else
        {
            throw new RateLimitExceededException("Connection rate limit exceeded");
        }
    }
    
    public async Task<T> ExecuteQueryAsync<T>(string sql, object parameters)
    {
        // Rate limit queries (Flink JobManager compatible)
        if (_queryLimiter.TryAcquire())
        {
            return await ExecuteQuery<T>(sql, parameters).ConfigureAwait(false);
        }
        
        throw new RateLimitExceededException("Query rate limit exceeded");
    }
}
```

### Example 3: External API Integration

```csharp
public class ExternalApiService
{
    private readonly MultiTierRateLimiter _rateLimiter;
    private readonly BufferPool<ApiRequest> _requestBuffer;
    
    public ExternalApiService()
    {
        // Configure for external API limits
        var tiers = new[]
        {
            new RateLimitingTier 
            { 
                Name = "API_Global", 
                RateLimit = 1000,      // 1000 req/sec API limit
                BurstCapacity = 1200,
                Enforcement = RateLimitingEnforcement.HardLimit
            },
            new RateLimitingTier 
            { 
                Name = "API_PerEndpoint", 
                RateLimit = 100,       // 100 req/sec per endpoint
                BurstCapacity = 150,
                Enforcement = RateLimitingEnforcement.Throttling
            }
        };
        
        _rateLimiter = new MultiTierRateLimiter();
        _rateLimiter.ConfigureTiers(tiers);
        
        // Buffer requests for efficient batching
        _requestBuffer = new BufferPool<ApiRequest>(
            maxSize: 50,                      // Batch 50 requests
            maxAge: TimeSpan.FromSeconds(5),  // Max 5 second delay
            rateLimiter: _rateLimiter
        );
        
        _requestBuffer.OnFlush += ProcessApiRequestBatch;
    }
    
    public async Task<ApiResponse> CallApiAsync(ApiRequest request)
    {
        var context = new RateLimitingContext
        {
            RequestType = request.Endpoint,
            ClientId = request.ClientId
        };
        
        if (_rateLimiter.TryAcquire(context))
        {
            await _requestBuffer.AddAsync(request).ConfigureAwait(false);
            return await WaitForResponse(request.Id).ConfigureAwait(false);
        }
        
        throw new RateLimitExceededException("API rate limit exceeded");
    }
}
```

## Testing and Validation

### Running the Tests

```bash
# Run comprehensive backpressure tests
cd Sample/FlinkDotNet.Aspire.IntegrationTests
dotnet test --filter "Category=backpressure_test" --verbosity normal

# Run specific rate limiting demo
cd FlinkDotNet/Flink.JobBuilder
dotnet run --project Demo/RateLimitingDemo.cs
```

### Expected Test Output

```
🚀 Flink.NET Enhanced Backpressure Rate Limiting Demo
=====================================================

1. TOKEN BUCKET RATE LIMITER DEMO
----------------------------------
📊 Configuration: 5 ops/sec, 10 burst capacity
📊 Initial tokens: 10.0

🚀 Testing burst capacity (should succeed quickly):
   Request 1: ✅ ALLOWED (0ms) - Tokens: 9.0
   Request 2: ✅ ALLOWED (0ms) - Tokens: 8.0
   ...
   Request 8: ✅ ALLOWED (1ms) - Tokens: 2.0

⏳ Testing rate limiting (should be throttled):
   Request 9: ✅ ALLOWED (1ms) - Tokens: 1.0
   Request 10: ✅ ALLOWED (1ms) - Tokens: 0.0
   Request 11: ❌ DENIED (0ms) - Tokens: 0.0

📊 Final utilization: 100%
```

### Validation Methods

```csharp
// Built-in validation methods
var multiTier = new MultiTierRateLimiter();

Console.WriteLine($"Hierarchical enforcement: {multiTier.ValidateHierarchicalEnforcement()}");
Console.WriteLine($"Burst accommodation: {multiTier.ValidateBurstAccommodation()}"); 
Console.WriteLine($"Multi-tier enforcement: {multiTier.ValidateMultiTierEnforcement()}");
Console.WriteLine($"Fair allocation: {multiTier.ValidateFairAllocation()}");
```

## Troubleshooting

### Common Issues

#### Issue 1: Rate Limiter Too Restrictive

**Symptoms**: 
- Most requests denied
- High utilization (>90%) constantly

**Solution**:
```csharp
// Increase rate limits or burst capacity
var rateLimiter = new TokenBucketRateLimiter(
    rateLimit: 2000.0,      // Increased from 1000
    burstCapacity: 4000.0   // Increased from 2000
);

// Or use dynamic adjustment
if (rateLimiter.CurrentUtilization > 0.9)
{
    rateLimiter.UpdateRateLimit(rateLimiter.CurrentRateLimit * 1.2);
}
```

#### Issue 2: Buffer Pool Not Flushing

**Symptoms**:
- Items accumulating in buffer
- No flush events triggered

**Debug**:
```csharp
var stats = bufferPool.GetStats();
Console.WriteLine($"Buffer: {stats.CurrentSize}/{stats.MaxSize}");
Console.WriteLine($"Last flush: {stats.LastFlush}");
Console.WriteLine($"Max age: {stats.MaxAge}");

// Check if size/time thresholds are too high
if (stats.CurrentSize > 0 && DateTime.UtcNow - stats.LastFlush > stats.MaxAge)
{
    Console.WriteLine("Time threshold should have triggered - check configuration");
}
```

#### Issue 3: Multi-Tier Always Rejecting

**Symptoms**:
- All multi-tier requests denied
- Single-tier requests work fine

**Debug**:
```csharp
var utilization = multiTier.GetUtilizationMetrics();
foreach (var tier in utilization)
{
    Console.WriteLine($"{tier.Key}: {tier.Value:P1} utilized");
    
    if (tier.Value > 0.95)
    {
        Console.WriteLine($"  ^ This tier is blocking requests");
    }
}
```

### Performance Tuning

#### For High Throughput

```csharp
// Optimize for high throughput
var highThroughputLimiter = new TokenBucketRateLimiter(
    rateLimit: 100_000.0,    // Very high sustained rate
    burstCapacity: 200_000.0 // High burst capacity
);

var highThroughputBuffer = new BufferPool<Message>(
    maxSize: 50_000,                        // Large batches
    maxAge: TimeSpan.FromMilliseconds(100), // Quick flushes
    rateLimiter: highThroughputLimiter
);
```

#### For Low Latency

```csharp
// Optimize for low latency
var lowLatencyBuffer = new BufferPool<Message>(
    maxSize: 100,                          // Small batches
    maxAge: TimeSpan.FromMilliseconds(10), // Very quick flushes
    rateLimiter: new TokenBucketRateLimiter(10_000, 15_000)
);
```

### Monitoring and Alerting

```csharp
// Set up monitoring
Timer.Periodic(TimeSpan.FromMinutes(1), () =>
{
    var utilization = rateLimiter.CurrentUtilization;
    
    if (utilization > 0.8)
    {
        Console.WriteLine($"⚠️ High utilization: {utilization:P1}");
        
        // Send alert
        AlertingService.SendAlert($"Rate limiter utilization: {utilization:P1}");
    }
    
    if (utilization > 0.95)
    {
        Console.WriteLine($"🚨 Critical utilization: {utilization:P1}");
        
        // Auto-scale
        rateLimiter.UpdateRateLimit(rateLimiter.CurrentRateLimit * 1.5);
    }
});
```

## World Best Practices Summary

✅ **Token Bucket**: Best for bursty traffic with sustained rate limits  
✅ **Sliding Window**: Best for precise time-based API rate limiting  
✅ **Buffer Pool**: Implements size AND time thresholds as requested  
✅ **Multi-Tier**: Hierarchical enforcement prevents noisy neighbors  
✅ **Backpressure**: Automatic upstream flow control  
✅ **Monitoring**: Built-in utilization and performance metrics  
✅ **Thread Safety**: All components are thread-safe and production ready  
✅ **Flink 2.0 Patterns**: Based on official AsyncSink documentation  

**Next Steps**: Deploy to production with monitoring and alerting configured.