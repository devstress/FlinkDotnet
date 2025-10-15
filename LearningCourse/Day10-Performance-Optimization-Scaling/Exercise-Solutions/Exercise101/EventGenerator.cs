using System.Diagnostics;
using System.Text.Json;
using Confluent.Kafka;
using Serilog;

namespace Exercise101;

/// <summary>
/// Generates high-volume events for performance testing
/// </summary>
public class EventGenerator
{
    private readonly string _bootstrapServers;
    private readonly string _topic;

    public EventGenerator(string bootstrapServers, string topic)
    {
        _bootstrapServers = bootstrapServers;
        _topic = topic;
    }

    /// <summary>
    /// Generate events at specified rate for performance testing
    /// </summary>
    public async Task<long> GenerateEventsAsync(
        string scenarioName,
        int eventsPerSecond,
        int durationSeconds,
        int parallelism)
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _bootstrapServers,
            ClientId = $"exercise101-producer-{scenarioName.Replace(" ", "-").ToLower()}",
            Acks = Acks.All,
            LingerMs = 5,
            BatchSize = 16384,
            CompressionType = CompressionType.Snappy
        };

        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

        Log.Information("   Generating {EventsPerSec} events/sec for {Duration}s (Parallelism={Parallelism})",
            eventsPerSecond, durationSeconds, parallelism);

        var stopwatch = Stopwatch.StartNew();
        var targetEvents = eventsPerSecond * durationSeconds;
        var eventCounter = 0L;
        var eventsPerInterval = eventsPerSecond / 10; // 100ms intervals
        var intervalMs = 100;

        try
        {
            for (int second = 0; second < durationSeconds * 10; second++)
            {
                var batchTasks = new List<Task>();

                // Generate events for this interval
                for (int i = 0; i < eventsPerInterval && eventCounter < targetEvents; i++)
                {
                    var eventId = Interlocked.Increment(ref eventCounter);
                    var perfEvent = CreatePerformanceEvent(eventId, scenarioName, parallelism);
                    var eventJson = JsonSerializer.Serialize(perfEvent);

                    var task = producer.ProduceAsync(_topic, new Message<string, string>
                    {
                        Key = eventId.ToString(),
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

            return eventCounter;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error generating events");
            throw;
        }
    }

    /// <summary>
    /// Create a performance event with realistic data
    /// </summary>
    private static PerformanceEvent CreatePerformanceEvent(long eventId, string scenario, int parallelism)
    {
        // Variable payload sizes to simulate realistic workloads
        var payloadSize = scenario switch
        {
            "Baseline" => 100,      // Small payload
            "Optimized" => 250,     // Medium payload
            "Over-Provisioned" => 500, // Larger payload
            _ => 200
        };

        // Generate realistic data based on event ID
        var data = GenerateRealisticData(eventId, payloadSize);

        return new PerformanceEvent
        {
            EventId = eventId,
            Timestamp = DateTime.UtcNow,
            Data = data,
            PayloadSize = payloadSize,
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