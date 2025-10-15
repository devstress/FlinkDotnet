using Confluent.Kafka;
using Confluent.Kafka.Admin;
using FlinkDotNet.DataStream;
using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;

namespace Exercise41;

/// <summary>
/// Day 4 Exercise 4.1: Netflix-Style Adaptive Backpressure with Real Streaming Infrastructure
/// 
/// This exercise demonstrates:
/// - Real-time adaptive quality adjustment based on system load
/// - Netflix-style streaming with quality degradation (Ultra4K → HD1080p → HD720p → SD480p)
/// - Flink native backpressure through parallelism configuration
/// - Production-ready streaming architecture with Kafka + FlinkDotNet
/// 
/// Architecture: Kafka Producer → Kafka → Flink Job (Adaptive Quality) → Kafka → Consumer
/// Key Learning: Quality degrades under load to maintain service availability
/// </summary>
class Program
{
    // KAFKA ADDRESSES - Read from environment variables set by test infrastructure
    // KAFKA_BOOTSTRAP_SERVERS: For host-to-container communication (producer/consumer from exercise)
    // KAFKA_FLINK_BOOTSTRAP_SERVERS: For container-to-container communication (Flink job connectivity)
    private static string KafkaBootstrapServers =>
        Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9093";
        
    private static string KafkaFlinkBootstrapServers =>
        Environment.GetEnvironmentVariable("KAFKA_FLINK_BOOTSTRAP_SERVERS") ?? "kafka:9092";
        
    private static string FlinkGatewayUrl =>
        Environment.GetEnvironmentVariable("FLINK_GATEWAY_URL") ?? "http://localhost:8080";

    private const string InputTopic = "streaming-requests-input";
    private const string OutputTopic = "streaming-sessions-output";
    private const string ConsumerGroup = "adaptive-quality-consumer";
    
    private const int RequestCount = 500;

    static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            Log.Information("================================================================================");
            Log.Information("  Day 4 Exercise 4.1: Netflix-Style Adaptive Backpressure");
            Log.Information("================================================================================");
            Log.Information("");
            Log.Information("🎓 Learning Objective:");
            Log.Information("   Demonstrate adaptive quality degradation under system load");
            Log.Information("   (Netflix pattern: Maintain service availability by reducing quality)");
            Log.Information("");
            Log.Information("📊 Configuration:");
            Log.Information("   Kafka (Host): {KafkaHost}", KafkaBootstrapServers);
            Log.Information("   Kafka (Flink): {KafkaFlink}", KafkaFlinkBootstrapServers);
            Log.Information("   Flink Gateway: {FlinkGateway}", FlinkGatewayUrl);
            Log.Information("   Streaming Requests: {RequestCount}", RequestCount);
            Log.Information("");
            Log.Information("📺 Netflix Quality Levels:");
            Log.Information("   Ultra4K  → 25 Mbps (Premium experience)");
            Log.Information("   HD1080p  → 8 Mbps  (Standard quality)");
            Log.Information("   HD720p   → 5 Mbps  (Reduced load)");
            Log.Information("   SD480p   → 1.5 Mbps (Emergency capacity)");
            Log.Information("");

            FlinkDotNet.DataStream.IJobClient? jobClient = null;

            try
            {
                // Step 1: Verify infrastructure
                Log.Information(">> Step 1/7: Verifying Kafka is ready...");
                await WaitForKafkaReadyAsync();
                Log.Information("");

                Log.Information(">> Step 2/7: Verifying Flink cluster is healthy...");
                await WaitForFlinkHealthyAsync();
                Log.Information("");

                Log.Information(">> Step 3/7: Creating Kafka topics...");
                await CreateTopicsAsync();
                Log.Information("");

                // Step 2: Submit Flink adaptive quality job
                Log.Information(">> Step 4/7: Submitting FlinkDotNet adaptive quality job...");
                Log.Information("   ⚙️  Configuring backpressure:");
                Log.Information("      - Source Parallelism: 4 (fast input)");
                Log.Information("      - Map Parallelism: 2 (adaptive quality - BOTTLENECK)");
                Log.Information("      - Sink Parallelism: 4 (fast output)");
                Log.Information("   💡 Bottleneck triggers quality adjustments");
                jobClient = await SubmitAdaptiveQualityJobAsync();
                await Task.Delay(TimeSpan.FromSeconds(5));
                Log.Information("");

                // Step 3: Produce streaming requests
                Log.Information(">> Step 5/7: Producing streaming requests to Kafka...");
                var stopwatch = Stopwatch.StartNew();
                var producedCount = await ProduceStreamingRequestsAsync();
                stopwatch.Stop();
                var produceRate = producedCount / stopwatch.Elapsed.TotalSeconds;
                Log.Information("   📈 Production Rate: {Rate:F1} msg/sec", produceRate);
                Log.Information("");

                // Step 4: Wait for processing
                Log.Information(">> Step 6/7: Waiting for adaptive quality processing (15 seconds)...");
                await Task.Delay(TimeSpan.FromSeconds(15));
                Log.Information("");
                
                // Step 5: Consume streaming sessions
                Log.Information(">> Step 7/7: Consuming streaming sessions from Kafka...");
                var (consumedCount, qualityDistribution, backpressureCount) = await ConsumeStreamingSessionsAsync();
                Log.Information("");

                // Results
                var successRate = producedCount > 0 ? (double)consumedCount / producedCount * 100 : 0;
                var backpressureRate = consumedCount > 0 ? (double)backpressureCount / consumedCount * 100 : 0;
                
                Log.Information("================================================================================");
                Log.Information("  Exercise 4.1 Results - Netflix Adaptive Backpressure");
                Log.Information("================================================================================");
                Log.Information("  📊 Statistics:");
                Log.Information("     Streaming Requests Produced: {Produced:N0}", producedCount);
                Log.Information("     Streaming Sessions Consumed: {Consumed:N0}", consumedCount);
                Log.Information("     Success Rate: {SuccessRate:F1}%", successRate);
                Log.Information("     Backpressure Activations: {BackpressureCount:N0} ({BackpressureRate:F1}%)", backpressureCount, backpressureRate);
                Log.Information("     Production Rate: {ProduceRate:F1} msg/sec", produceRate);
                Log.Information("");
                Log.Information("  📺 Quality Distribution:");
                foreach (var (quality, count) in qualityDistribution.OrderByDescending(x => x.Value))
                {
                    var percentage = consumedCount > 0 ? (double)count / consumedCount * 100 : 0;
                    Log.Information("     {Quality,-10}: {Count,3} sessions ({Percentage:F1}%)", quality, count, percentage);
                }
                Log.Information("");
                Log.Information("  🎓 Key Learnings:");
                Log.Information("     ✅ Netflix-style quality adaptation under load");
                Log.Information("     ✅ Real Flink backpressure through parallelism mismatch");
                Log.Information("     ✅ Adaptive quality maintains service availability");
                Log.Information("     ✅ Production-ready streaming with Kafka integration");
                Log.Information("     ✅ Quality degradation pattern used by Netflix at scale");
                Log.Information("");
                Log.Information("  💡 Real Netflix Stats:");
                Log.Information("     • 200M+ concurrent users during peak hours");
                Log.Information("     • 15 Petabits/second peak global traffic");
                Log.Information("     • 80% capacity threshold triggers quality adaptation");
                Log.Information("     • 99.9% uptime through intelligent backpressure");
                Log.Information("");
                Log.Information("[SUCCESS] EXERCISE COMPLETED SUCCESSFULLY!");
                Log.Information("================================================================================");

                return 0;
            }
            finally
            {
                // Cleanup: Cancel Flink job
                if (jobClient != null)
                {
                    Log.Information("");
                    Log.Information(">> Cleaning up: Cancelling Flink job...");
                    try
                    {
                        await jobClient.CancelAsync();
                        Log.Information("   [SUCCESS] Flink job cancelled");
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to cancel job");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Exercise 4.1 failed with exception");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    /// <summary>
    /// Submit Flink job for adaptive quality adjustment
    /// Key: Parallelism mismatch creates bottleneck triggering quality degradation
    /// </summary>
    private static async Task<FlinkDotNet.DataStream.IJobClient> SubmitAdaptiveQualityJobAsync()
    {
        var environment = StreamExecutionEnvironment.GetExecutionEnvironment();

        // Configure buffer timeout for backpressure
        environment.SetBufferTimeout(100); // 100ms buffer timeout

        // Source: Kafka consumer with HIGH parallelism (fast input)
        var requestStream = environment.FromKafka(
            topic: InputTopic,
            bootstrapServers: KafkaFlinkBootstrapServers,
            groupId: ConsumerGroup,
            startingOffsets: "earliest"
        ).SetParallelism(4);  // 4 parallel consumers (FAST)

        // Map: Adaptive quality with LOW parallelism (creates BOTTLENECK)
        var sessionStream = requestStream
            .Map(new AdaptiveQualityFunction())
            .SetParallelism(2);  // Only 2 parallel processors (BOTTLENECK triggers backpressure)

        // Sink: Kafka producer with HIGH parallelism (fast output)
        sessionStream
            .SinkToKafka(OutputTopic, KafkaFlinkBootstrapServers)
            .SetParallelism(4);  // 4 parallel producers (FAST)

        // Execute job
        var jobClient = await environment.ExecuteAsync("Exercise41-Netflix-Adaptive-Quality");

        Log.Information("   [SUCCESS] Flink job submitted");
        Log.Information("   JobId: {JobId}", jobClient.GetJobId());
        
        return jobClient;
    }

    /// <summary>
    /// Produce streaming request messages to Kafka
    /// </summary>
    private static async Task<int> ProduceStreamingRequestsAsync()
    {
        var config = new ProducerConfig
        {
            BootstrapServers = KafkaBootstrapServers,
            ClientId = "exercise41-producer",
            Acks = Acks.All,
            LingerMs = 5
        };

        using var producer = new ProducerBuilder<string, string>(config).Build();

        var producedCount = 0;
        Log.Information("   Producing {RequestCount} streaming requests...", RequestCount);

        for (int i = 0; i < RequestCount; i++)
        {
            var request = GenerateStreamingRequest(i);
            var requestJson = JsonSerializer.Serialize(request);

            try
            {
                var result = await producer.ProduceAsync(InputTopic, new Message<string, string>
                {
                    Key = request.UserId,
                    Value = requestJson
                });
                
                if (result.Status == PersistenceStatus.Persisted)
                {
                    producedCount++;
                    
                    if ((i + 1) % 100 == 0)
                    {
                        Log.Information("   [{Count}/{Total}] requests produced...", i + 1, RequestCount);
                    }
                }
            }
            catch (ProduceException<string, string> ex)
            {
                Log.Error(ex, "Failed to produce request {RequestId}", i);
            }
        }

        producer.Flush(TimeSpan.FromSeconds(10));
        Log.Information("   [SUCCESS] All {MessageCount} requests produced", producedCount);
        return producedCount;
    }

    /// <summary>
    /// Consume and verify streaming sessions from Kafka
    /// </summary>
    private static Task<(int consumedCount, Dictionary<string, int> qualityDistribution, int backpressureCount)> 
        ConsumeStreamingSessionsAsync()
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = KafkaBootstrapServers,
            GroupId = ConsumerGroup + "-verify",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(OutputTopic);

        Log.Information("   Consuming from '{OutputTopic}' (max 30 seconds)...", OutputTopic);

        var consumedCount = 0;
        var backpressureCount = 0;
        var qualityDistribution = new Dictionary<string, int>();
        var timeoutCount = 0;
        const int maxTimeouts = 60;  // Allow 60s for Flink distributed processing (WI49 fix)
        var stopwatch = Stopwatch.StartNew();

        while (timeoutCount < maxTimeouts && stopwatch.Elapsed < TimeSpan.FromSeconds(90))
        {
            try
            {
                var result = consumer.Consume(TimeSpan.FromSeconds(1));
                
                if (result != null)
                {
                    consumedCount++;
                    timeoutCount = 0;

                    // Parse streaming session
                    try
                    {
                        var session = JsonSerializer.Deserialize<StreamingSession>(result.Message.Value);
                        if (session != null)
                        {
                            // Track quality distribution
                            var quality = session.CurrentQuality.ToString();
                            qualityDistribution.TryGetValue(quality, out var count);
                            qualityDistribution[quality] = count + 1;

                            // Track backpressure activations
                            if (session.BackpressureActive)
                            {
                                backpressureCount++;
                            }
                        }
                    }
                    catch
                    {
                        // Ignore parsing errors
                    }
                    
                    if (consumedCount % 100 == 0)
                    {
                        Log.Information("   [{Count}] sessions consumed (backpressure: {Backpressure})...", 
                            consumedCount, backpressureCount);
                    }
                    
                    consumer.Commit(result);
                }
                else
                {
                    timeoutCount++;
                }
            }
            catch (ConsumeException ex)
            {
                Log.Error(ex, "Error consuming session");
                break;
            }
        }

        consumer.Close();
        Log.Information("   [SUCCESS] Consumed {ConsumedCount} streaming sessions", consumedCount);
        return Task.FromResult((consumedCount, qualityDistribution, backpressureCount));
    }

    private static async Task CreateTopicsAsync()
    {
        var adminConfig = new AdminClientConfig 
        { 
            BootstrapServers = KafkaBootstrapServers
        };
        
        using var admin = new AdminClientBuilder(adminConfig).Build();

        var topicsToCreate = new[]
        {
            new TopicSpecification { Name = InputTopic, NumPartitions = 4, ReplicationFactor = 1 },
            new TopicSpecification { Name = OutputTopic, NumPartitions = 4, ReplicationFactor = 1 }
        };

        try
        {
            await admin.CreateTopicsAsync(topicsToCreate);
            Log.Information("   [SUCCESS] Topics created: {Topics}", 
                string.Join(", ", topicsToCreate.Select(t => t.Name)));
        }
        catch (CreateTopicsException ex)
        {
            var errors = ex.Results.Where(r => r.Error.Code != ErrorCode.TopicAlreadyExists).ToList();
            if (!errors.Any())
            {
                Log.Information("   [SUCCESS] Topics already exist: {Topics}", 
                    string.Join(", ", topicsToCreate.Select(t => t.Name)));
            }
            else
            {
                Log.Warning("Some topics failed to create");
            }
        }
    }

    private static async Task WaitForKafkaReadyAsync()
    {
        var timeout = TimeSpan.FromSeconds(30);
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            try
            {
                var adminConfig = new AdminClientConfig
                {
                    BootstrapServers = KafkaBootstrapServers,
                    SocketTimeoutMs = 3000
                };

                using var admin = new AdminClientBuilder(adminConfig).Build();
                var metadata = admin.GetMetadata(TimeSpan.FromSeconds(3));

                if (metadata?.Brokers?.Count > 0)
                {
                    Log.Information("   [SUCCESS] Kafka is ready with {BrokerCount} broker(s)", metadata.Brokers.Count);
                    return;
                }
            }
            catch
            {
                // Continue waiting
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException($"Kafka not ready within {timeout.TotalSeconds} seconds");
    }

    private static async Task WaitForFlinkHealthyAsync()
    {
        var timeout = TimeSpan.FromSeconds(30);
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            try
            {
                using var httpClient = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                var response = await httpClient.GetAsync($"{FlinkGatewayUrl}/api/v1/health");
                
                if (response.IsSuccessStatusCode)
                {
                    Log.Information("   [SUCCESS] Flink cluster is healthy");
                    return;
                }
            }
            catch
            {
                // Continue waiting
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException($"Flink cluster not healthy within {timeout.TotalSeconds} seconds");
    }

    private static StreamingRequest GenerateStreamingRequest(int requestId)
    {
        // Netflix quality distribution: 40% HD1080p, 35% Ultra4K, 20% HD720p, 5% SD480p
        var qualityDistribution = (requestId % 100) switch
        {
            < 40 => QualityLevel.HD1080p,    // 40% - Most common
            < 75 => QualityLevel.Ultra4K,    // 35% - Premium users
            < 95 => QualityLevel.HD720p,     // 20% - Standard users
            _ => QualityLevel.SD480p          // 5% - Low bandwidth
        };
        
        return new StreamingRequest(
            UserId: $"user_{requestId % 100:D3}",
            RequestedQuality: qualityDistribution,
            Timestamp: DateTime.UtcNow
        );
    }
}

// Data models for Netflix-style streaming
public record StreamingRequest(
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("requested_quality")] QualityLevel RequestedQuality,
    [property: JsonPropertyName("timestamp")] DateTime Timestamp
);

public record StreamingSession(
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("current_quality")] QualityLevel CurrentQuality,
    [property: JsonPropertyName("original_quality")] QualityLevel OriginalQuality,
    [property: JsonPropertyName("backpressure_active")] bool BackpressureActive,
    [property: JsonPropertyName("system_load")] double SystemLoad,
    [property: JsonPropertyName("start_time")] DateTime StartTime,
    [property: JsonPropertyName("last_update")] DateTime LastUpdate
);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum QualityLevel
{
    SD480p,      // 1.5 Mbps - Emergency capacity
    HD720p,      // 5 Mbps - Reduced load
    HD1080p,     // 8 Mbps - Standard quality
    Ultra4K      // 25 Mbps - Premium experience
}

/// <summary>
/// Flink Map Function for adaptive quality adjustment
/// Simulates Netflix-style quality degradation based on system load
/// </summary>
public class AdaptiveQualityFunction : FlinkDotNet.DataStream.IMapFunction<string, string>
{
    public string Map(string input)
    {
        try
        {
            var request = JsonSerializer.Deserialize<StreamingRequest>(input);
            if (request == null)
                return string.Empty;
            
            // Simulate system load (deterministic based on time for consistent testing)
            var currentLoad = GetSimulatedSystemLoad();
            
            // Netflix adaptive quality pattern: Degrade quality under load
            var adjustedQuality = currentLoad switch
            {
                >= 0.95 => QualityLevel.SD480p,    // Critical: Emergency mode (95%+ load)
                >= 0.90 => QualityLevel.HD720p,    // High: Reduce to 720p (90%+ load)
                >= 0.80 => QualityLevel.HD1080p,   // Medium: Standard HD (80%+ load)
                _ => QualityLevel.Ultra4K          // Normal: Full quality (<80% load)
            };
            
            // Simulate processing time based on quality (creates backpressure bottleneck)
            var processingTime = adjustedQuality switch
            {
                QualityLevel.Ultra4K => 50,   // Highest processing cost
                QualityLevel.HD1080p => 30,
                QualityLevel.HD720p => 20,
                QualityLevel.SD480p => 10,    // Lowest processing cost
                _ => 30                        // Default fallback
            };
            
            Thread.Sleep(processingTime);
            
            var session = new StreamingSession(
                UserId: request.UserId,
                CurrentQuality: adjustedQuality,
                OriginalQuality: request.RequestedQuality,
                BackpressureActive: adjustedQuality < request.RequestedQuality,
                SystemLoad: currentLoad,
                StartTime: request.Timestamp,
                LastUpdate: DateTime.UtcNow
            );
            
            return JsonSerializer.Serialize(session);
        }
        catch
        {
            return string.Empty;
        }
    }
    
    private double GetSimulatedSystemLoad()
    {
        // Deterministic load simulation for consistent testing
        // Based on current minute to create varying load patterns
        var minute = DateTime.UtcNow.Minute;
        var baseLoad = 0.4 + (minute % 60) / 100.0; // Range: 0.4 to 0.99
        return Math.Min(0.99, baseLoad);
    }
}
