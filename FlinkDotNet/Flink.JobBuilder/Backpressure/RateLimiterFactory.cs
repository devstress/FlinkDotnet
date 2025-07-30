using Microsoft.Extensions.Logging;
using System;

namespace Flink.JobBuilder.Backpressure;

/// <summary>
/// Factory for creating rate limiters with simple, operationally efficient approaches.
/// Prioritizes simplicity and natural backpressure over complex distributed storage.
/// </summary>
public static class RateLimiterFactory
{
    /// <summary>
    /// Creates a lag-based rate limiter that monitors consumer lag for natural backpressure (RECOMMENDED FOR PRODUCTION).
    /// 
    /// SIMPLE APPROACH - Natural Backpressure:
    /// - Basic token bucket with consumer lag monitoring
    /// - When consumer lag > threshold → pause token refilling (automatic backpressure)
    /// - When consumer lag decreases → resume token refilling
    /// - Much simpler operationally than complex distributed state storage
    /// - Reactive approach: responds to actual system load rather than trying to predict it
    /// 
    /// Benefits over complex approaches:
    /// - No distributed state storage complexity (Kafka partitions, Redis clusters)
    /// - Natural backpressure based on actual consumer performance
    /// - Simple to operate, debug, and reason about
    /// - Automatic adjustment to system load without manual configuration
    /// - Follows reactive streams principles
    /// 
    /// Professional References:
    /// - Hohpe, G. & Woolf, B. (2003) "Enterprise Integration Patterns" - Backpressure patterns
    /// - Reactive Streams Specification (2015) - Reactive backpressure handling
    /// </summary>
    /// <param name="rateLimit">Maximum sustained rate in operations per second</param>
    /// <param name="burstCapacity">Maximum burst capacity (tokens that can accumulate)</param>
    /// <param name="consumerGroup">Kafka consumer group to monitor for lag</param>
    /// <param name="lagThreshold">Lag threshold to trigger backpressure (default: 5 seconds)</param>
    /// <param name="lagMonitor">Consumer lag monitor (auto-created if not provided)</param>
    /// <param name="logger">Logger instance</param>
    /// <returns>Lag-based rate limiter with consumer lag monitoring</returns>
    public static LagBasedRateLimiter CreateLagBasedBucket(
        double rateLimit,
        double burstCapacity,
        string consumerGroup,
        TimeSpan? lagThreshold = null,
        IKafkaConsumerLagMonitor? lagMonitor = null,
        ILogger<LagBasedRateLimiter>? logger = null)
    {
        return new LagBasedRateLimiter(rateLimit, burstCapacity, consumerGroup, lagThreshold, lagMonitor, logger);
    }

    /// <summary>
    /// Creates a multi-tier rate limiter with Kafka-based storage (LEGACY).
    /// 
    /// NOTE: Consider using CreateLagBasedBucket() for simpler operational model.
    /// See wiki for full documentation on multi-tier rate limiting patterns.
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
    /// Creates a multi-tier rate limiter with in-memory storage (LEGACY).
    /// 
    /// NOTE: Consider using CreateLagBasedBucket() for simpler operational model.
    /// See wiki for full documentation on multi-tier rate limiting patterns.
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
    /// Creates a production-ready lag-based rate limiter configuration with recommended settings.
    /// 
    /// This configuration provides simple, operationally efficient rate limiting:
    /// - Natural backpressure based on actual consumer performance
    /// - No complex distributed storage requirements
    /// - Automatic adjustment to system load
    /// - Simple to operate and debug
    /// 
    /// Professional References:
    /// - Hohpe, G. & Woolf, B. (2003) "Enterprise Integration Patterns" - Backpressure patterns
    /// - Reactive Streams Specification (2015) - Reactive backpressure handling
    /// </summary>
    /// <param name="rateLimit">Rate limit per second (default: 1000)</param>
    /// <param name="burstCapacity">Maximum burst capacity (default: 2000)</param>
    /// <param name="consumerGroup">Kafka consumer group to monitor</param>
    /// <param name="lagThreshold">Lag threshold for backpressure (default: 5 seconds)</param>
    /// <param name="logger">Logger instance</param>
    /// <returns>Production-ready lag-based rate limiter</returns>
    public static (LagBasedRateLimiter rateLimiter, string configuration) CreateProductionConfiguration(
        double rateLimit = 1000.0,
        double burstCapacity = 2000.0,
        string consumerGroup = "default-consumer-group",
        TimeSpan? lagThreshold = null,
        ILogger<LagBasedRateLimiter>? logger = null)
    {
        var threshold = lagThreshold ?? TimeSpan.FromSeconds(5);
        var rateLimiter = CreateLagBasedBucket(rateLimit, burstCapacity, consumerGroup, threshold, logger: logger);
        
        var config = $"""
            Production Rate Limiter Configuration (Lag-Based):
            - Approach: Simple lag-based backpressure (RECOMMENDED)
            - Rate Limit: {rateLimit} ops/sec
            - Burst Capacity: {burstCapacity} tokens
            - Consumer Group: {consumerGroup}
            - Lag Threshold: {threshold.TotalSeconds}s
            - Backpressure: Automatic based on consumer lag
            - Operational Complexity: LOW - no distributed storage required
            - Recommended for: Production systems needing simple, effective rate limiting
            
            When consumer lag > {threshold.TotalSeconds}s → Rate limiter pauses token refilling
            When consumer lag <= {threshold.TotalSeconds}s → Rate limiter resumes normal operation
            
            For alternative approaches (Kafka state storage, Redis), see wiki documentation.
            """;

        return (rateLimiter, config);
    }

    /// <summary>
    /// Creates a development configuration with lag-based rate limiting and logging.
    /// </summary>
    /// <param name="rateLimit">Rate limit per second (default: 100)</param>
    /// <param name="burstCapacity">Maximum burst capacity (default: 200)</param>
    /// <param name="consumerGroup">Kafka consumer group to monitor (default: test-group)</param>
    /// <param name="logger">Logger instance</param>
    /// <returns>Development-friendly configuration</returns>
    public static (LagBasedRateLimiter rateLimiter, string configuration) CreateDevelopmentConfiguration(
        double rateLimit = 100.0,
        double burstCapacity = 200.0,
        string consumerGroup = "test-group",
        ILogger<LagBasedRateLimiter>? logger = null)
    {
        var lagThreshold = TimeSpan.FromSeconds(2); // Shorter threshold for development
        var rateLimiter = CreateLagBasedBucket(rateLimit, burstCapacity, consumerGroup, lagThreshold, logger: logger);
        
        var config = $"""
            Development Rate Limiter Configuration (Lag-Based):
            - Approach: Simple lag-based backpressure
            - Rate Limit: {rateLimit} ops/sec
            - Burst Capacity: {burstCapacity} tokens
            - Consumer Group: {consumerGroup}
            - Lag Threshold: {lagThreshold.TotalSeconds}s (shorter for development)
            - Recommended for: Development, testing, local environments
            
            For production systems, use CreateProductionConfiguration() with higher thresholds.
            For alternative approaches, see wiki documentation.
            """;

        return (rateLimiter, config);
    }

    // ========================================================================================
    // LEGACY METHODS - Alternative Approaches (See Wiki for Full Documentation)
    // ========================================================================================
    // 
    // The following methods provide alternative rate limiting approaches for specific use cases.
    // For most scenarios, use CreateLagBasedBucket() instead.
    // 
    // See Wiki Documentation for:
    // - Kafka state storage approach (complex distributed storage)
    // - Redis storage approach (ultra-low latency)
    // - In-memory approach (single-instance only)
    // - Multi-tier approach (hierarchical rate limiting)
    //
    // These approaches require additional operational complexity and are only recommended
    // for specific advanced use cases documented in the wiki.
    // ========================================================================================

    /// <summary>
    /// [LEGACY] Creates a TokenBucket rate limiter with in-memory storage (development/testing only).
    /// 
    /// NOTE: Consider using CreateLagBasedBucket() for better production-ready patterns.
    /// This method is provided for compatibility and specific testing scenarios.
    /// See wiki for full documentation and alternative approaches.
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
    /// [LEGACY] Creates a TokenBucket rate limiter with Kafka-based storage.
    /// 
    /// NOTE: Consider using CreateLagBasedBucket() for simpler operational model.
    /// This method provides complex distributed state storage for advanced use cases.
    /// See wiki for full documentation on when to use this approach.
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
    /// [LEGACY] Creates a production-ready Kafka configuration for distributed state storage.
    /// 
    /// NOTE: Consider using CreateProductionConfiguration() with lag-based approach instead.
    /// This method provides complex Kafka partition configuration for advanced use cases.
    /// See wiki for full documentation and operational considerations.
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

}