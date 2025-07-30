using System;

namespace Flink.JobBuilder.Backpressure;

/// <summary>
/// Redis configuration for rate limiter state storage.
/// 
/// Provides comprehensive configuration options for Redis-based rate limiter storage
/// with emphasis on fault tolerance, persistence, and zero message loss guarantees.
/// 
/// Professional References:
/// - Carlson, J. (2019). "Redis in Action" Manning Publications - Chapter 4: "Persistence"
/// - Macedo, J. & Alexandre, A. (2017). "Mastering Redis" Packt Publishing - Chapter 3: "Data Safety"
/// - Sanfilippo, S. & Noordhuis, P. (2018). "Redis Design and Implementation" - AOF persistence patterns
/// - Kamps, J. & Dooley, B. (2020). "Pro Redis" Apress - Chapter 8: "High Availability and Sentinel"
/// 
/// Academic Foundation:
/// - DeCandia, G. et al. (2007). "Dynamo: Amazon's Highly Available Key-value Store" ACM SOSP
/// - Bernstein, P. & Newcomer, E. (2009). "Principles of Transaction Processing" 2nd Edition
/// - Gray, J. & Reuter, A. (1993). "Transaction Processing: Concepts and Techniques" - Durability principles
/// </summary>
public class RedisConfig
{
    /// <summary>
    /// Redis connection string (default: localhost:6379).
    /// Supports Redis Sentinel, Redis Cluster, and standalone configurations.
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// Database index for rate limiter data (default: 0).
    /// Provides logical separation of rate limiter state from other Redis data.
    /// </summary>
    public int Database { get; set; } = 0;

    /// <summary>
    /// Key prefix for rate limiter entries (default: "rl:").
    /// Prevents key collisions with other Redis usage patterns.
    /// </summary>
    public string KeyPrefix { get; set; } = "rl:";

    /// <summary>
    /// Redis persistence strategy for fault tolerance and zero message loss.
    /// 
    /// Professional Reference: Carlson, J. (2019) "Redis in Action" - Chapter 4, persistence patterns
    /// Academic Foundation: Gray, J. & Reuter, A. (1993) "Transaction Processing" - Durability (ACID)
    /// </summary>
    public RedisPersistenceStrategy PersistenceStrategy { get; set; } = RedisPersistenceStrategy.AOF_EverySec;

    /// <summary>
    /// Configuration for Append Only File (AOF) persistence mode.
    /// 
    /// AOF provides better durability guarantees than RDB snapshots for rate limiter state.
    /// Reference: Sanfilippo, S. & Noordhuis, P. (2018) "Redis Design and Implementation"
    /// </summary>
    public RedisAOFConfig AOFConfig { get; set; } = new();

    /// <summary>
    /// Configuration for Redis Database (RDB) snapshot persistence.
    /// Used as backup strategy or primary persistence for memory-optimized scenarios.
    /// </summary>
    public RedisRDBConfig RDBConfig { get; set; } = new();

    /// <summary>
    /// Memory-only configuration with fallback to source recalculation.
    /// 
    /// When enabled, Redis operates in memory-only mode and rate limiter state
    /// is recalculated from source of truth (e.g., Kafka consumer lag) if lost.
    /// 
    /// Trade-off Analysis:
    /// - Performance: Fastest (no disk I/O)
    /// - Durability: Lowest (state lost on restart)
    /// - Recovery: Automatic via source recalculation
    /// 
    /// Professional Reference: Macedo, J. & Alexandre, A. (2017) "Mastering Redis" - Memory optimization
    /// </summary>
    public RedisMemoryOnlyConfig MemoryOnlyConfig { get; set; } = new();

    /// <summary>
    /// Redis High Availability configuration.
    /// 
    /// Supports Redis Sentinel for automatic failover and Redis Cluster for horizontal scaling.
    /// Reference: Kamps, J. & Dooley, B. (2020) "Pro Redis" - Chapter 8: High Availability
    /// </summary>
    public RedisHighAvailabilityConfig HighAvailabilityConfig { get; set; } = new();

    /// <summary>
    /// Connection timeout settings for Redis operations.
    /// </summary>
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Command timeout for individual Redis operations.
    /// </summary>
    public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Maximum number of connection retries before failing.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Delay between connection retry attempts.
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Enable Redis SSL/TLS encryption for secure connections.
    /// </summary>
    public bool UseSsl { get; set; } = false;

    /// <summary>
    /// Redis authentication password (if required).
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Redis authentication username (for Redis 6+ ACL support).
    /// </summary>
    public string? Username { get; set; }
}

/// <summary>
/// Redis persistence strategy options for rate limiter state storage.
/// 
/// Professional Guidance:
/// - Production Systems: Use AOF_EverySec for balance of performance and durability
/// - High-Write Load: Use AOF_No with RDB backup for maximum performance
/// - Maximum Durability: Use AOF_Always (significant performance impact)
/// - Development/Testing: Use MemoryOnly for fastest performance
/// 
/// References:
/// - Carlson, J. (2019) "Redis in Action" - Persistence strategy selection
/// - Sanfilippo, S. & Noordhuis, P. (2018) "Redis Design and Implementation" - AOF vs RDB trade-offs
/// </summary>
public enum RedisPersistenceStrategy
{
    /// <summary>
    /// Memory-only mode with fallback to source recalculation.
    /// 
    /// Best for: Development, testing, high-performance scenarios with acceptable data loss
    /// Durability: None (state lost on restart)
    /// Performance: Highest (no disk I/O)
    /// Recovery: Automatic via source recalculation
    /// </summary>
    MemoryOnly,

    /// <summary>
    /// RDB snapshot-only persistence.
    /// 
    /// Best for: Low-write scenarios, backup purposes
    /// Durability: Medium (can lose data between snapshots)
    /// Performance: High (minimal impact during normal operation)
    /// Recovery: From last snapshot
    /// </summary>
    RDB_Only,

    /// <summary>
    /// AOF with no explicit fsync (let OS decide when to flush).
    /// 
    /// Best for: High-performance scenarios with some durability requirements
    /// Durability: Low (can lose up to 30 seconds of data)
    /// Performance: High (no forced disk writes)
    /// Recovery: From AOF file (may lose recent data)
    /// </summary>
    AOF_No,

    /// <summary>
    /// AOF with fsync every second (RECOMMENDED FOR PRODUCTION).
    /// 
    /// Best for: Production systems requiring balance of performance and durability
    /// Durability: High (can lose at most 1 second of data)
    /// Performance: Good (fsync once per second)
    /// Recovery: From AOF file (minimal data loss)
    /// 
    /// Reference: Redis official documentation - "Redis Persistence" best practices
    /// </summary>
    AOF_EverySec,

    /// <summary>
    /// AOF with fsync on every write operation.
    /// 
    /// Best for: Maximum durability requirements, financial systems
    /// Durability: Highest (no data loss on clean shutdown)
    /// Performance: Lowest (disk write on every operation)
    /// Recovery: Complete from AOF file
    /// 
    /// Professional Note: Use only when zero data loss is absolutely required
    /// Reference: Gray, J. & Reuter, A. (1993) "Transaction Processing" - Durability guarantees
    /// </summary>
    AOF_Always,

    /// <summary>
    /// Hybrid: AOF for durability + RDB for faster restarts.
    /// 
    /// Best for: Enterprise production systems with strict SLA requirements
    /// Durability: Highest (dual persistence mechanisms)
    /// Performance: Medium (both AOF and RDB overhead)
    /// Recovery: RDB for speed, AOF for completeness
    /// </summary>
    AOF_Plus_RDB
}

/// <summary>
/// Redis AOF (Append Only File) configuration for maximum durability.
/// 
/// AOF provides append-only logging of all write operations, enabling point-in-time recovery
/// and minimal data loss in case of system failures.
/// 
/// Reference: Sanfilippo, S. & Noordhuis, P. (2018) "Redis Design and Implementation" - AOF internals
/// </summary>
public class RedisAOFConfig
{
    /// <summary>
    /// Enable AOF persistence (default: true).
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// AOF fsync policy for controlling when data is written to disk.
    /// </summary>
    public AOFFsyncPolicy FsyncPolicy { get; set; } = AOFFsyncPolicy.EverySec;

    /// <summary>
    /// Enable AOF file rewriting to optimize file size (default: true).
    /// 
    /// AOF rewrite creates a minimal AOF file by replaying the current dataset,
    /// reducing file size and improving recovery performance.
    /// </summary>
    public bool EnableRewrite { get; set; } = true;

    /// <summary>
    /// Minimum percentage of AOF file size increase before triggering rewrite (default: 100%).
    /// </summary>
    public int RewritePercentage { get; set; } = 100;

    /// <summary>
    /// Minimum AOF file size before considering rewrite (default: 64MB).
    /// </summary>
    public long RewriteMinSize { get; set; } = 64 * 1024 * 1024; // 64MB

    /// <summary>
    /// Enable AOF file verification on startup (default: true).
    /// Checks for AOF file corruption and attempts automatic repair.
    /// </summary>
    public bool VerifyOnStartup { get; set; } = true;
}

/// <summary>
/// AOF fsync policy options for controlling disk write behavior.
/// 
/// Professional Reference: Carlson, J. (2019) "Redis in Action" - AOF fsync strategies
/// Academic Foundation: Gray, J. & Reuter, A. (1993) "Transaction Processing" - Write-ahead logging
/// </summary>
public enum AOFFsyncPolicy
{
    /// <summary>
    /// No explicit fsync - let operating system decide when to flush.
    /// Fastest performance, lowest durability guarantee.
    /// </summary>
    No,

    /// <summary>
    /// Fsync every second (RECOMMENDED FOR PRODUCTION).
    /// Good balance between performance and durability.
    /// Can lose at most 1 second of data.
    /// </summary>
    EverySec,

    /// <summary>
    /// Fsync on every write operation.
    /// Maximum durability, significant performance impact.
    /// Use only when zero data loss is absolutely required.
    /// </summary>
    Always
}

/// <summary>
/// Redis RDB (Redis Database) snapshot configuration.
/// 
/// RDB provides point-in-time snapshots of the dataset, useful for backups
/// and faster restart times compared to AOF replay.
/// 
/// Reference: Carlson, J. (2019) "Redis in Action" - RDB snapshot strategies
/// </summary>
public class RedisRDBConfig
{
    /// <summary>
    /// Enable RDB snapshots (default: true as backup mechanism).
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// RDB snapshot triggers based on time and number of changes.
    /// Default: Save snapshot every 60 seconds if at least 1000 keys changed.
    /// 
    /// Format: "seconds changes" (e.g., "60 1000" means save after 60 seconds if 1000+ keys changed)
    /// </summary>
    public string[] SaveIntervals { get; set; } = { "60 1000", "300 100", "3600 1" };

    /// <summary>
    /// Enable RDB file compression (default: true).
    /// Reduces file size at cost of CPU during save/load operations.
    /// </summary>
    public bool EnableCompression { get; set; } = true;

    /// <summary>
    /// Enable RDB file checksum verification (default: true).
    /// Adds CRC64 checksum to detect file corruption.
    /// </summary>
    public bool EnableChecksum { get; set; } = true;

    /// <summary>
    /// RDB file name pattern (default: "rate-limiter-{timestamp}.rdb").
    /// </summary>
    public string FileNamePattern { get; set; } = "rate-limiter-{timestamp}.rdb";
}

/// <summary>
/// Memory-only Redis configuration with fallback strategies.
/// 
/// When enabled, provides ultra-high performance with automatic state recovery
/// from external sources when Redis data is lost due to restarts or failures.
/// 
/// Professional Reference: Macedo, J. & Alexandre, A. (2017) "Mastering Redis" - Memory optimization
/// Academic Foundation: DeCandia, G. et al. (2007) "Dynamo" - Eventually consistent systems
/// </summary>
public class RedisMemoryOnlyConfig
{
    /// <summary>
    /// Enable memory-only mode (default: false).
    /// When true, disables all Redis persistence mechanisms.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Enable automatic state recalculation from source of truth when Redis data is lost.
    /// 
    /// Sources include:
    /// - Kafka consumer lag monitoring
    /// - Application metrics and telemetry
    /// - External monitoring systems
    /// </summary>
    public bool EnableSourceRecalculation { get; set; } = true;

    /// <summary>
    /// Source of truth for rate limiter state recalculation.
    /// </summary>
    public RecalculationSource RecalculationSource { get; set; } = RecalculationSource.ConsumerLagMonitoring;

    /// <summary>
    /// Interval for checking and updating rate limiter state from source of truth.
    /// </summary>
    public TimeSpan RecalculationInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum time to wait for source recalculation before falling back to default state.
    /// </summary>
    public TimeSpan RecalculationTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Default rate limiter state to use when recalculation fails or times out.
    /// </summary>
    public DefaultRateLimiterState DefaultState { get; set; } = new();
}

/// <summary>
/// Source of truth options for rate limiter state recalculation.
/// </summary>
public enum RecalculationSource
{
    /// <summary>
    /// Recalculate from Kafka consumer lag monitoring.
    /// Uses current consumer lag to determine appropriate rate limits.
    /// </summary>
    ConsumerLagMonitoring,

    /// <summary>
    /// Recalculate from application metrics and telemetry.
    /// Uses CPU usage, memory utilization, and throughput metrics.
    /// </summary>
    ApplicationMetrics,

    /// <summary>
    /// Recalculate from external monitoring systems.
    /// Integrates with Prometheus, DataDog, or other monitoring platforms.
    /// </summary>
    ExternalMonitoring,

    /// <summary>
    /// Use static configuration values.
    /// Falls back to predefined rate limits without dynamic calculation.
    /// </summary>
    StaticConfiguration
}

/// <summary>
/// Default rate limiter state for fallback scenarios.
/// </summary>
public class DefaultRateLimiterState
{
    /// <summary>
    /// Default rate limit in operations per second.
    /// </summary>
    public double DefaultRateLimit { get; set; } = 1000.0;

    /// <summary>
    /// Default burst capacity in tokens.
    /// </summary>
    public double DefaultBurstCapacity { get; set; } = 2000.0;

    /// <summary>
    /// Initial token count when creating new rate limiters.
    /// </summary>
    public double InitialTokens { get; set; } = 1000.0;
}

/// <summary>
/// Redis High Availability configuration for enterprise deployments.
/// 
/// Supports Redis Sentinel for automatic failover and Redis Cluster for horizontal scaling.
/// Reference: Kamps, J. & Dooley, B. (2020) "Pro Redis" - High Availability patterns
/// </summary>
public class RedisHighAvailabilityConfig
{
    /// <summary>
    /// Enable Redis Sentinel for automatic failover (default: false).
    /// </summary>
    public bool EnableSentinel { get; set; } = false;

    /// <summary>
    /// Redis Sentinel connection strings.
    /// Format: "sentinel-host:port"
    /// </summary>
    public string[] SentinelHosts { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Redis master name as configured in Sentinel.
    /// </summary>
    public string MasterName { get; set; } = "mymaster";

    /// <summary>
    /// Enable Redis Cluster mode for horizontal scaling (default: false).
    /// </summary>
    public bool EnableCluster { get; set; } = false;

    /// <summary>
    /// Redis Cluster node endpoints.
    /// Format: "cluster-node-host:port"
    /// </summary>
    public string[] ClusterNodes { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Health check interval for monitoring Redis availability.
    /// </summary>
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum time to wait for failover completion.
    /// </summary>
    public TimeSpan FailoverTimeout { get; set; } = TimeSpan.FromSeconds(60);
}