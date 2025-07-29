# Kafka vs Redis Rate Limiter Storage Demonstration

This document provides practical examples demonstrating why Kafka partitions are superior to Redis for rate limiter state storage in Flink.NET.

## Quick Comparison

| Aspect | Kafka Partitions | Redis |
|--------|------------------|-------|
| **Scale** | 10M+ ops/sec (distributed) | 1M ops/sec (per node) |
| **Persistence** | Built-in durable log | AOF configuration required |
| **Setup** | Single service + ZooKeeper | Multiple services (masters + sentinels) |
| **Failover** | Automatic (5-10 seconds) | Manual (15-30 seconds) |
| **Data Loss** | None (with proper config) | Possible during failover |

## Code Examples

### Kafka-Based Rate Limiter (Production)

```csharp
// Enterprise-grade configuration
var kafkaConfig = new KafkaConfig
{
    BootstrapServers = "kafka1:9092,kafka2:9092,kafka3:9092",
    Performance = new KafkaPerformanceConfig
    {
        ReplicationFactor = 3,      // Survive 2 broker failures
        PartitionCount = 12,        // Horizontal scaling
        EnableCompaction = true     // Keep only latest state
    }
};

// Create distributed rate limiter
var rateLimiter = RateLimiterFactory.CreateWithKafkaStorage(
    rateLimit: 1000.0,              // 1000 ops/sec sustained
    burstCapacity: 2000.0,          // Allow bursts up to 2000
    kafkaConfig: kafkaConfig
);

// Multi-tier enforcement across Kafka partitions
var multiTierLimiter = RateLimiterFactory.CreateMultiTierWithKafkaStorage(kafkaConfig);

Console.WriteLine($"Storage: {rateLimiter.StorageBackend.BackendType}");     // Apache Kafka
Console.WriteLine($"Distributed: {rateLimiter.IsDistributed}");             // True
Console.WriteLine($"Persistent: {rateLimiter.IsPersistent}");               // True
Console.WriteLine($"Latency: {rateLimiter.StorageBackend.TypicalLatency}"); // 5ms
```

### Redis Alternative (Problems)

```csharp
// What you would need with Redis (much more complex)
var redisCluster = new RedisCluster
{
    Masters = new[] { "redis1:6379", "redis2:6379", "redis3:6379" },
    Sentinels = new[] { "sentinel1:26379", "sentinel2:26379", "sentinel3:26379" },
    AOFConfig = new AOFConfig 
    { 
        SyncPolicy = "everysec",    // Performance vs durability tradeoff
        RewriteThreshold = 100      // When to rewrite AOF file
    },
    ClusterConfig = new ClusterConfig
    {
        HashSlots = 16384,          // Manual slot management
        FailoverTimeout = 15000     // 15 second failover time
    }
};

// Much more complex setup, single points of failure, manual intervention required
```

## Practical Benefits in Action

### 1. **Automatic Scaling**

```csharp
// Kafka: Add partitions dynamically
kafka-topics --alter --topic rate-limiter-state --partitions 24 --bootstrap-server kafka:9092

// Rate limiters automatically distribute across new partitions
// No application code changes required
```

```bash
# Redis: Manual resharding required
redis-cli --cluster reshard cluster-node:6379
# Manual slot redistribution, potential data migration, application downtime
```

### 2. **Failure Recovery**

```csharp
// Kafka: Automatic failover
// If Broker 1 fails → Broker 2 becomes leader automatically
// Rate limiter state preserved, no data loss
// Application continues without interruption

// Redis: Manual intervention
// If Master 1 fails → Sentinel quorum required
// Potential data loss during failover
// Application may need reconnection logic
```

### 3. **State Distribution**

```csharp
// Kafka: Partition-based distribution
var context = new RateLimitingContext 
{ 
    TopicName = "orders",
    ConsumerId = "consumer-1" 
};

// Rate limiter state automatically partitioned by consistent hash
// Each rate limiter gets its own partition for parallel processing
multiTierLimiter.TryAcquire(context); // Flink JobManager compatible
```

### 4. **Performance Characteristics**

```csharp
// Kafka Performance
- Throughput: 10M+ operations/sec (cluster)
- Storage: Unlimited (disk-based with compression)
- Latency: 5ms typical (with proper batching)
- Scaling: Linear with partition count

// Redis Performance  
- Throughput: 1M operations/sec (per node)
- Storage: Limited by available RAM
- Latency: 1-100ms (depends on AOF sync policy)
- Scaling: Manual, requires resharding
```

## Integration with Flink AsyncSink

```csharp
public class FlinkAsyncSinkWithKafkaRateLimiting
{
    private readonly MultiTierRateLimiter _rateLimiter;
    
    public FlinkAsyncSinkWithKafkaRateLimiting(KafkaConfig kafkaConfig)
    {
        _rateLimiter = RateLimiterFactory.CreateMultiTierWithKafkaStorage(kafkaConfig);
    }
    
    public async Task WriteAsync(SinkRecord record)
    {
        var context = new RateLimitingContext 
        {
            TopicName = record.Topic,
            ConsumerId = Environment.MachineName
        };
        
        if (_rateLimiter.TryAcquire(context)) // Flink JobManager compatible
        {
            try
            {
                await ProcessRecord(record).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Record processing failed");
            }
        }
        else
        {
            // Backpressure: Rate limit exceeded
            throw new RateLimitExceededException("Multi-tier rate limit exceeded");
        }
    }
}
```

## Conclusion

**Kafka partitions provide enterprise-grade rate limiter state storage** that scales horizontally, provides automatic failover, and integrates seamlessly with Flink's distributed streaming architecture. Redis requires significantly more infrastructure complexity and manual intervention for equivalent functionality.

**Recommendation**: Use Kafka-based storage for production deployments, in-memory storage only for development/testing.