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
    /// Creates a TokenBucket rate limiter with Kafka-based storage (recommended for production).
    /// </summary>
    /// <param name="rateLimit">Maximum sustained rate in operations per second</param>
    /// <param name="burstCapacity">Maximum burst capacity (tokens that can accumulate)</param>
    /// <param name="kafkaConfig">Kafka configuration for state storage</param>
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
    /// Creates a multi-tier rate limiter with Kafka-based storage (recommended for production).
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
    /// This configuration provides:
    /// - High availability through Kafka replication (3 replicas)
    /// - Horizontal scaling through partitions (12 partitions)
    /// - Log compaction to retain only latest state
    /// - Appropriate timeouts and error handling
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
    /// Creates a development configuration with in-memory storage and logging.
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
            Development Rate Limiter Configuration:
            - Storage: In-Memory (not persistent)
            - Rate Limit: {rateLimit} ops/sec
            - Burst Capacity: {burstCapacity} tokens
            - Distribution: Single instance only
            - Recommended for: Development, testing, single-instance deployments
            
            For production, use CreateWithKafkaStorage() instead.
            """;

        return (rateLimiter, config);
    }
}