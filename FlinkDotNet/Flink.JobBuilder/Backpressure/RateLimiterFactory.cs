using Microsoft.Extensions.Logging;
using System;

namespace Flink.JobBuilder.Backpressure;

/// <summary>
/// Factory for creating rate limiters with appropriate storage backends.
/// Provides easy configuration and best practice defaults.
/// </summary>
public static class RateLimiterFactory
{
    /// <summary>
    /// Creates a TokenBucket rate limiter with Kafka-based storage (RECOMMENDED FOR PRODUCTION).
    /// 
    /// Kafka provides superior characteristics for distributed rate limiting via partitions:
    /// - Built-in horizontal scaling through partitions distributed across brokers
    /// - Durable, replicated storage with automatic leader election and fault tolerance
    /// - Each rate limiter can have dedicated partitions for parallel processing
    /// - Excellent integration with existing Flink streaming infrastructure
    /// - Handles millions of operations per second with linear scalability
    /// 
    /// Professional References:
    /// - Kreps, J. et al. (2011) "Kafka: a Distributed Messaging System for Log Processing" - Partition design
    /// - Hunt, P. et al. (2010) "ZooKeeper: Wait-free coordination for Internet-scale systems" - Coordination patterns
    /// </summary>
    /// <param name="rateLimit">Maximum sustained rate in operations per second</param>
    /// <param name="burstCapacity">Maximum burst capacity (tokens that can accumulate)</param>
    /// <param name="kafkaConfig">Kafka configuration for state storage with partition distribution</param>
    /// <param name="rateLimiterId">Unique identifier (auto-generated if not provided)</param>
    /// <param name="logger">Logger instance</param>
    /// <returns>TokenBucket rate limiter with Kafka storage</returns>
    public static TokenBucketRateLimiter CreateWithKafkaStorage(
        double rateLimit,
        double burstCapacity,
        KafkaConfig kafkaConfig,
        string? rateLimiterId = null,
        ILogger<KafkaRateLimiterStateStorage>? logger = null)
    {
        var stateStorage = new KafkaRateLimiterStateStorage(kafkaConfig, logger: logger);
        return new TokenBucketRateLimiter(rateLimit, burstCapacity, rateLimiterId, stateStorage);
    }

    /// <summary>
    /// Creates a TokenBucket rate limiter with in-memory storage (development/testing only).
    /// </summary>
    /// <param name="rateLimit">Maximum sustained rate in operations per second</param>
    /// <param name="burstCapacity">Maximum burst capacity (tokens that can accumulate)</param>
    /// <param name="rateLimiterId">Unique identifier (auto-generated if not provided)</param>
    /// <param name="logger">Logger instance</param>
    /// <returns>TokenBucket rate limiter with in-memory storage</returns>
    public static TokenBucketRateLimiter CreateWithInMemoryStorage(
        double rateLimit,
        double burstCapacity,
        string? rateLimiterId = null,
        ILogger<InMemoryRateLimiterStateStorage>? logger = null)
    {
        var stateStorage = new InMemoryRateLimiterStateStorage(logger);
        return new TokenBucketRateLimiter(rateLimit, burstCapacity, rateLimiterId, stateStorage);
    }

    /// <summary>
    /// Creates a multi-tier rate limiter with Kafka-based storage (RECOMMENDED FOR PRODUCTION).
    /// 
    /// Provides enterprise-grade distributed rate limiting with:
    /// - Kafka partition-based horizontal scaling and fault tolerance
    /// - Built-in replication and leader election for high availability
    /// - Linear scalability through partition distribution across brokers
    /// - Excellent integration with Flink streaming infrastructure
    /// </summary>
    /// <param name="kafkaConfig">Kafka configuration for state storage</param>
    /// <param name="logger">Logger instance</param>
    /// <returns>Multi-tier rate limiter with Kafka storage</returns>
    public static MultiTierRateLimiter CreateMultiTierWithKafkaStorage(
        KafkaConfig kafkaConfig,
        ILogger<KafkaRateLimiterStateStorage>? logger = null)
    {
        var stateStorage = new KafkaRateLimiterStateStorage(kafkaConfig, logger: logger);
        return new MultiTierRateLimiter(stateStorage);
    }

    /// <summary>
    /// Creates a multi-tier rate limiter with in-memory storage (development/testing only).
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <returns>Multi-tier rate limiter with in-memory storage</returns>
    public static MultiTierRateLimiter CreateMultiTierWithInMemoryStorage(
        ILogger<InMemoryRateLimiterStateStorage>? logger = null)
    {
        var stateStorage = new InMemoryRateLimiterStateStorage(logger);
        return new MultiTierRateLimiter(stateStorage);
    }

    /// <summary>
    /// Creates a production-ready configuration with Kafka storage and recommended settings.
    /// 
    /// This configuration provides enterprise-grade distributed rate limiting:
    /// - Kafka partition-based horizontal scaling (12 partitions for parallelism)
    /// - High availability through replication (3 replicas across brokers)
    /// - Log compaction to retain only latest state per rate limiter
    /// - Built-in leader election and automatic failover
    /// - Linear scalability for millions of operations per second
    /// 
    /// Professional References:
    /// - Kreps, J. et al. (2011) "Kafka: a Distributed Messaging System for Log Processing"
    /// - Hunt, P. et al. (2010) "ZooKeeper: Wait-free coordination for Internet-scale systems"
    /// </summary>
    /// <param name="bootstrapServers">Kafka bootstrap servers</param>
    /// <param name="topicName">Topic name for rate limiter state (default: rate-limiter-state)</param>
    /// <returns>Production-ready Kafka configuration</returns>
    public static KafkaConfig CreateProductionKafkaConfig(
        string bootstrapServers,
        string topicName = "rate-limiter-state")
    {
        return new KafkaConfig
        {
            BootstrapServers = bootstrapServers,
            Performance = new KafkaPerformanceConfig
            {
                ReplicationFactor = 3,      // High availability
                PartitionCount = 12,        // Horizontal scaling
                RetentionTime = TimeSpan.FromDays(7),
                EnableCompaction = true     // Keep only latest state
            }
        };
    }

    /// <summary>
    /// Creates a development configuration with Kafka storage and logging.
    /// 
    /// Provides developer-friendly Kafka configuration with:
    /// - Simplified partition setup for fast iteration (single partition)
    /// - Local Kafka instance connectivity (localhost:9092)
    /// - Detailed logging for debugging partition behavior
    /// - Production-like behavior in development environment
    /// </summary>
    /// <param name="rateLimit">Rate limit per second</param>
    /// <param name="burstCapacity">Maximum burst capacity</param>
    /// <param name="bootstrapServers">Kafka bootstrap servers (default: localhost:9092)</param>
    /// <param name="logger">Logger instance</param>
    /// <returns>Development-friendly Kafka configuration</returns>
    public static (TokenBucketRateLimiter rateLimiter, string configuration) CreateDevelopmentKafkaConfiguration(
        double rateLimit = 1000.0,
        double burstCapacity = 2000.0,
        string bootstrapServers = "localhost:9092",
        ILogger<KafkaRateLimiterStateStorage>? logger = null)
    {
        var kafkaConfig = new KafkaConfig
        {
            BootstrapServers = bootstrapServers,
            Performance = new KafkaPerformanceConfig
            {
                ReplicationFactor = 1,      // Single replica for development
                PartitionCount = 1,         // Single partition for simplicity
                RetentionTime = TimeSpan.FromHours(1), // Short retention for development
                EnableCompaction = true
            }
        };

        var rateLimiter = CreateWithKafkaStorage(rateLimit, burstCapacity, kafkaConfig, logger: logger);
        
        var config = $"""
            Development Rate Limiter Configuration (Kafka):
            - Storage: Kafka with single partition (development setup)
            - Bootstrap Servers: {bootstrapServers}
            - Rate Limit: {rateLimit} ops/sec
            - Burst Capacity: {burstCapacity} tokens
            - Persistence: Kafka log with 1-hour retention
            - Fault Tolerance: Single replica (development mode)
            - Distribution: Single partition for development simplicity
            - Recommended for: Development, testing, Kafka integration testing
            
            For production, use CreateProductionKafkaConfig() with multiple partitions and replicas.
            For ultra-low latency scenarios, consider Redis storage (see wiki for examples).
            """;

        return (rateLimiter, config);
    }

    /// <summary>
    /// Creates a development configuration with in-memory storage and logging (LEGACY).
    /// 
    /// NOTE: Consider using CreateDevelopmentKafkaConfiguration() for better development experience
    /// with Kafka-based storage that matches production patterns.
    /// </summary>
    /// <param name="rateLimit">Rate limit per second</param>
    /// <param name="burstCapacity">Maximum burst capacity</param>
    /// <param name="logger">Logger instance</param>
    /// <returns>Development-friendly configuration</returns>
    public static (TokenBucketRateLimiter rateLimiter, string configuration) CreateDevelopmentConfiguration(
        double rateLimit = 1000.0,
        double burstCapacity = 2000.0,
        ILogger<InMemoryRateLimiterStateStorage>? logger = null)
    {
        var rateLimiter = CreateWithInMemoryStorage(rateLimit, burstCapacity, logger: logger);
        
        var config = $"""
            Development Rate Limiter Configuration (In-Memory):
            - Storage: In-Memory (not persistent)
            - Rate Limit: {rateLimit} ops/sec
            - Burst Capacity: {burstCapacity} tokens
            - Distribution: Single instance only
            - Recommended for: Development, testing, single-instance deployments
            
            For production distributed systems, use CreateWithKafkaStorage() instead.
            For development with distributed patterns, use CreateDevelopmentKafkaConfiguration().
            For ultra-low latency development, see Redis examples in the wiki.
            """;

        return (rateLimiter, config);
    }
}