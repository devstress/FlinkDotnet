using System.Diagnostics;
using System.Text.Json;
using Confluent.Kafka;
using Serilog;

namespace Exercise102;

/// <summary>
/// Generates high-volume events with explicit partition keys for load distribution testing
/// </summary>
public class EventGenerator
{
    private readonly string _bootstrapServers;
    private readonly string _topic;
    private readonly int _totalPartitions;

    public EventGenerator(string bootstrapServers, string topic, int totalPartitions = 8)
    {
        _bootstrapServers = bootstrapServers;
        _topic = topic;
        _totalPartitions = totalPartitions;
    }

    /// <summary>
    /// Generate events distributed across partitions for scaling testing
    /// </summary>
    public async Task<long> GenerateEventsAsync(
        string scenarioName,
        int eventsPerSecond,
        int durationSeconds,
        int nodeCount)
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _bootstrapServers,
            ClientId = $"exercise102-producer-{scenarioName.Replace(" ", "-").ToLower()}",
            Acks = Acks.All,
            LingerMs = 5,
            BatchSize = 16384,
            CompressionType = CompressionType.Snappy,
            // Explicit partition assignment for testing
            Partitioner = Partitioner.Murmur2
        };

        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

        Log.Information("   Generating {EventsPerSec} events/sec for {Duration}s (Nodes={NodeCount}, Partitions={Partitions})",
            eventsPerSecond, durationSeconds, nodeCount, _totalPartitions);

        var stopwatch = Stopwatch.StartNew();
        var targetEvents = eventsPerSecond * durationSeconds;
        var eventCounter = 0L;
        var eventsPerInterval = eventsPerSecond / 10; // 100ms intervals
        var intervalMs = 100;

        // Track partition distribution
        var partitionCounts = new Dictionary<int, long>();
        for (int i = 0; i < _totalPartitions; i++)
            partitionCounts[i] = 0;

        try
        {
            for (int second = 0; second < durationSeconds * 10; second++)
            {
                var batchTasks = new List<Task>();

                // Generate events for this interval
                for (int i = 0; i < eventsPerInterval && eventCounter < targetEvents; i++)
                {
                    var eventId = Interlocked.Increment(ref eventCounter);
                    
                    // Round-robin partition assignment to ensure even distribution
                    var partitionKey = (int)(eventId % _totalPartitions);
                    
                    var scalingEvent = CreateScalingEvent(eventId, scenarioName, nodeCount, partitionKey);
                    var eventJson = JsonSerializer.Serialize(scalingEvent);

                    // Use partition key as Kafka key for explicit partitioning
                    var task = producer.ProduceAsync(_topic, new Message<string, string>
                    {
                        Key = partitionKey.ToString(),
                        Value = eventJson
                    });

                    batchTasks.Add(task);

                    // Batch produce in groups of 100 to avoid overwhelming
                    if (batchTasks.Count >= 100)
                    {
                        await Task.WhenAll(batchTasks);
                        batchTasks.Clear();
                    }
                }

                // Wait for remaining batch
                if (batchTasks.Any())
                {
                    await Task.WhenAll(batchTasks);
                }

                // Wait for next interval
                await Task.Delay(intervalMs);

                // Progress reporting every 2 seconds
                if ((second + 1) % 20 == 0)
                {
                    var elapsed = stopwatch.Elapsed.TotalSeconds;
                    var currentRate = eventCounter / elapsed;
                    Log.Information("   [{Count}/{Target}] events produced ({Rate:F1} events/sec)",
                        eventCounter, targetEvents, currentRate);
                }
            }

            // Flush remaining messages
            producer.Flush(TimeSpan.FromSeconds(10));
            stopwatch.Stop();

            var actualRate = eventCounter / stopwatch.Elapsed.TotalSeconds;
            Log.Information("   [SUCCESS] Generated {EventCount} events in {Duration:F1}s ({Rate:F1} events/sec)",
                eventCounter, stopwatch.Elapsed.TotalSeconds, actualRate);

            // Log partition distribution
            Log.Information("   Partition distribution:");
            foreach (var (partition, count) in partitionCounts.OrderBy(kvp => kvp.Key))
            {
                var percentage = (count / (double)eventCounter) * 100;
                Log.Information("     - Partition {Partition}: {Count} events ({Percentage:F1}%)",
                    partition, count, percentage);
            }

            return eventCounter;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error generating events");
            throw;
        }
    }

    /// <summary>
    /// Create a scaling event with partition key
    /// </summary>
    private static ScalingEvent CreateScalingEvent(long eventId, string scenario, int nodeCount, int partitionKey)
    {
        // Variable payload sizes based on scenario
        var payloadSize = scenario switch
        {
            "Single Node" => 100,
            "Horizontal Scale" => 200,
            "Optimized" => 250,
            "Saturated" => 300,
            _ => 200
        };

        var data = GenerateRealisticData(eventId, payloadSize);

        return new ScalingEvent
        {
            EventId = eventId,
            Timestamp = DateTime.UtcNow,
            Data = data,
            PayloadSize = payloadSize,
            PartitionKey = partitionKey,
            Scenario = scenario
        };
    }

    /// <summary>
    /// Generate realistic event data
    /// </summary>
    private static string GenerateRealisticData(long eventId, int payloadSize)
    {
        var random = new Random((int)(eventId % int.MaxValue));
        var dataBuilder = new System.Text.StringBuilder();

        // Simulate realistic JSON-like data
        dataBuilder.Append($"{{\"event_id\":{eventId},");
        dataBuilder.Append($"\"user_id\":\"user_{random.Next(1, 1000):D4}\",");
        dataBuilder.Append($"\"session_id\":\"session_{random.Next(1, 100):D4}\",");
        dataBuilder.Append($"\"action\":\"action_{random.Next(1, 20)}\",");
        dataBuilder.Append($"\"timestamp\":\"{DateTime.UtcNow:O}\",");

        // Pad with additional data to reach payload size
        var paddingNeeded = payloadSize - dataBuilder.Length - 20;
        if (paddingNeeded > 0)
        {
            dataBuilder.Append("\"metadata\":\"");
            dataBuilder.Append(new string('x', paddingNeeded));
            dataBuilder.Append("\"");
        }

        dataBuilder.Append("}");
        return dataBuilder.ToString();
    }
}