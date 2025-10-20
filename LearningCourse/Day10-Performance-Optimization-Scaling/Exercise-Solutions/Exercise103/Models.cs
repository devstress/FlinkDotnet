using System.Text.Json.Serialization;

namespace Exercise103;

/// <summary>
/// Event representing a memory-intensive operation
/// </summary>
public class MemoryEvent
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;

    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Create a sample event with payload
    /// </summary>
    public static MemoryEvent CreateSample(int size = 1024)
    {
        return new MemoryEvent
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            UserId = $"user_{Random.Shared.Next(1, 1000)}",
            Data = new string('X', size), // Payload to create memory pressure
            Metadata = new Dictionary<string, string>
            {
                ["source"] = "memory-test",
                ["version"] = "1.0"
            }
        };
    }
}

/// <summary>
/// Memory metrics collected during testing
/// </summary>
public class MemoryMetrics
{
    public string Scenario { get; set; } = string.Empty;
    public int EventsProcessed { get; set; }
    public long TotalAllocatedBytes { get; set; }
    public long Gen0Collections { get; set; }
    public long Gen1Collections { get; set; }
    public long Gen2Collections { get; set; }
    public long PeakWorkingSet { get; set; }
    public double AverageHeapSize { get; set; }
    public double AllocationRateMBPerSec { get; set; }
    public double ProcessingTimeMs { get; set; }
    public int ObjectPoolHits { get; set; }
    public int ObjectPoolMisses { get; set; }
    public int CacheHits { get; set; }
    public int CacheMisses { get; set; }

    /// <summary>
    /// Calculate object pool efficiency
    /// </summary>
    public double PoolEfficiency =>
        ObjectPoolHits + ObjectPoolMisses > 0
            ? (double)ObjectPoolHits / (ObjectPoolHits + ObjectPoolMisses) * 100
            : 0;

    /// <summary>
    /// Calculate cache hit ratio
    /// </summary>
    public double CacheHitRatio =>
        CacheHits + CacheMisses > 0
            ? (double)CacheHits / (CacheHits + CacheMisses) * 100
            : 0;
}

/// <summary>
/// GC profile for a test scenario
/// </summary>
public class GCProfile
{
    public long InitialGen0Collections { get; set; }
    public long InitialGen1Collections { get; set; }
    public long InitialGen2Collections { get; set; }
    public long InitialAllocatedBytes { get; set; }
    public long InitialWorkingSet { get; set; }

    public long FinalGen0Collections { get; set; }
    public long FinalGen1Collections { get; set; }
    public long FinalGen2Collections { get; set; }
    public long FinalAllocatedBytes { get; set; }
    public long FinalWorkingSet { get; set; }

    /// <summary>
    /// Calculate GC statistics
    /// </summary>
    public (long Gen0, long Gen1, long Gen2) GetCollectionCounts() =>
        (
            FinalGen0Collections - InitialGen0Collections,
            FinalGen1Collections - InitialGen1Collections,
            FinalGen2Collections - InitialGen2Collections
        );

    /// <summary>
    /// Calculate total bytes allocated during test
    /// </summary>
    public long GetTotalAllocated() => FinalAllocatedBytes - InitialAllocatedBytes;

    /// <summary>
    /// Calculate peak working set increase
    /// </summary>
    public long GetPeakWorkingSetIncrease() => FinalWorkingSet - InitialWorkingSet;
}