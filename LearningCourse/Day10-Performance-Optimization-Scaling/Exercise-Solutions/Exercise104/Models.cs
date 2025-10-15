using System.Text.Json.Serialization;
using MessagePack;

namespace Exercise104;

/// <summary>
/// Event for throughput testing with various serialization formats
/// </summary>
[MessagePackObject]
public class ThroughputEvent
{
    [Key(0)]
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [Key(1)]
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [Key(2)]
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [Key(3)]
    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = string.Empty;

    [Key(4)]
    [JsonPropertyName("value")]
    public double Value { get; set; }

    [Key(5)]
    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Create a sample event for testing
    /// </summary>
    public static ThroughputEvent CreateSample()
    {
        return new ThroughputEvent
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            UserId = $"user_{Random.Shared.Next(1, 10000)}",
            EventType = $"type_{Random.Shared.Next(1, 10)}",
            Value = Random.Shared.NextDouble() * 1000,
            Metadata = new Dictionary<string, string>
            {
                ["source"] = "throughput-test",
                ["version"] = "1.0",
                ["region"] = $"region_{Random.Shared.Next(1, 5)}"
            }
        };
    }
}

/// <summary>
/// Metrics for throughput testing scenarios
/// </summary>
public class ThroughputMetrics
{
    public string Scenario { get; set; } = string.Empty;
    public int EventsProcessed { get; set; }
    public double ProcessingTimeMs { get; set; }
    public double ThroughputEventsPerSec { get; set; }
    public double AvgLatencyMs { get; set; }
    public long SerializedSizeBytes { get; set; }
    public double SerializationTimeMs { get; set; }
    public double DeserializationTimeMs { get; set; }
    public double CompressionRatio { get; set; }
    public int BatchSize { get; set; }

    /// <summary>
    /// Calculate events per second
    /// </summary>
    public void CalculateThroughput()
    {
        if (ProcessingTimeMs > 0)
        {
            ThroughputEventsPerSec = (EventsProcessed / ProcessingTimeMs) * 1000;
        }
    }
}

/// <summary>
/// Serialization format enum
/// </summary>
public enum SerializationFormat
{
    Json,
    Binary,
    MessagePack
}

/// <summary>
/// Compression algorithm enum
/// </summary>
public enum CompressionType
{
    None,
    GZip
}