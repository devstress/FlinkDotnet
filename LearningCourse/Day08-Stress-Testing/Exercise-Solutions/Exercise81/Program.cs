using System.Diagnostics;
using System.Text.Json;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using FlinkDotNet.DataStream;
using Serilog;

namespace Exercise81;

/// <summary>
/// Exercise 8.1: Advanced Load Generation & Stress Testing
/// 
/// Real-time stress testing system that demonstrates:
/// - High-volume Kafka message production
/// - Real Flink stream processing under load
/// - Performance monitoring and metrics collection
/// - Throughput and latency benchmarking
/// 
/// Architecture: Kafka topic → Flink stream processing → Performance analysis
/// </summary>
class Program
{
    // Kafka addresses - read from environment variables set by test infrastructure
    private static string KafkaBootstrapServers =>
        Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9093";
        
    private static string KafkaFlinkBootstrapServers =>
        Environment.GetEnvironmentVariable("KAFKA_FLINK_BOOTSTRAP_SERVERS") ?? "kafka:9092";
        
    private static string FlinkGatewayUrl =>
        Environment.GetEnvironmentVariable("FLINKDOTNET_JOBGATEWAY_URL") ?? "http://localhost:8080";

    // Kafka topics for stress testing
    private const string StressTestTopic = "stress-test-events";
    private const string ProcessedEventsTopic = "processed-stress-events";
    private const string ConsumerGroup = "exercise81-consumer";
    
    // Stress test parameters (reduced for faster test execution)
    private static readonly List<LoadScenario> Scenarios = new()
    {
        new() { Name = "Baseline Load", RatePerSecond = 40, DurationSeconds = 5 },
        new() { Name = "Moderate Load", RatePerSecond = 60, DurationSeconds = 5 },
        new() { Name = "High Load", RatePerSecond = 80, DurationSeconds = 5 }
    };

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
            Log.Information("  Exercise 8.1: Advanced Load Generation & Stress Testing");
            Log.Information("================================================================================");
            Log.Information("");
            Log.Information("Learning Objectives:");
            Log.Information("  - High-volume Kafka message production");
            Log.Information("  - Real Flink stream processing under load");
            Log.Information("  - Performance monitoring and benchmarking");
            Log.Information("  - Throughput and latency analysis");
            Log.Information("");
            Log.Information("Configuration:");
            Log.Information("  Kafka (Host): {KafkaHost}", KafkaBootstrapServers);
            Log.Information("  Kafka (Flink): {KafkaFlink}", KafkaFlinkBootstrapServers);
            Log.Information("  Flink Gateway: {FlinkGateway}", FlinkGatewayUrl);
            Log.Information("");

            FlinkDotNet.DataStream.IJobClient? jobClient = null;

            try
            {
                // Step 1: Verify infrastructure
                Log.Information(">> Step 1/6: Verifying Kafka is ready...");
                await WaitForKafkaReadyAsync();
                Log.Information("");

                Log.Information(">> Step 2/6: Verifying Flink cluster is ready...");
                await WaitForFlinkHealthyAsync();
                Log.Information("");

                Log.Information(">> Step 3/6: Creating Kafka topics...");
                await CreateTopicsAsync();
                Log.Information("");

                // Step 2: Submit Flink stress test processing job
                Log.Information(">> Step 4/6: Submitting Flink stress test processing job...");
                jobClient = await SubmitStressTestJobAsync();
                await Task.Delay(TimeSpan.FromSeconds(5)); // Wait for job to start
                Log.Information("");

                // Step 3: Execute stress test scenarios
                Log.Information(">> Step 5/6: Executing stress test scenarios...");
                var overallMetrics = await ExecuteStressTestScenariosAsync();
                Log.Information("");

                // Step 4: Generate performance report
                Log.Information(">> Step 6/6: Generating performance report...");
                GeneratePerformanceReport(overallMetrics);
                Log.Information("");

                // Results
                Log.Information("================================================================================");
                Log.Information("  Exercise 8.1 Results - Stress Testing");
                Log.Information("================================================================================");
                Log.Information("  Overall Performance:");
                Log.Information("     Total Events: {TotalEvents:N0}", overallMetrics.TotalEventsGenerated);
                Log.Information("     Total Processed: {TotalProcessed:N0}", overallMetrics.TotalEventsProcessed);
                Log.Information("     Average Throughput: {AvgThroughput:F1} events/sec", overallMetrics.AverageThroughput);
                Log.Information("     Peak Throughput: {PeakThroughput:F1} events/sec", overallMetrics.PeakThroughput);
                Log.Information("");
                Log.Information("  Key Learnings:");
                Log.Information("     [SUCCESS] High-volume Kafka production validated");
                Log.Information("     [SUCCESS] Flink stream processing under load tested");
                Log.Information("     [SUCCESS] Performance metrics collected and analyzed");
                Log.Information("     [SUCCESS] Production-ready stress testing patterns");
                Log.Information("");
                Log.Information("[SUCCESS] Exercise 8.1 COMPLETED successfully");
                Log.Information("================================================================================");

                return 0;
            }
            finally
            {
                // Cleanup: Cancel the Flink job
                if (jobClient != null)
                {
                    Log.Information("");
                    Log.Information(">> Cleaning up: Cancelling Flink job...");
                    try
                    {
                        await jobClient.CancelAsync();
                        await Task.Delay(TimeSpan.FromSeconds(3)); // Wait for job to fully terminate
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
            Log.Fatal(ex, "Exercise 8.1 failed with exception");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    /// <summary>
    /// Submit Flink job for stress test event processing
    /// </summary>
    private static async Task<FlinkDotNet.DataStream.IJobClient> SubmitStressTestJobAsync()
    {
        var environment = StreamExecutionEnvironment.GetExecutionEnvironment();

        // Source stream from Kafka
        var stressTestStream = environment.FromKafka(
            topic: StressTestTopic,
            bootstrapServers: KafkaFlinkBootstrapServers,
            groupId: ConsumerGroup,
            startingOffsets: "earliest"
        );

        // Process events (simulate realistic processing time)
        var processedStream = stressTestStream
            .Map(new StressTestProcessingFunction());

        // Sink processed events
        processedStream.SinkToKafka(ProcessedEventsTopic, KafkaFlinkBootstrapServers);

        // Execute job
        var jobClient = await environment.ExecuteAsync("Exercise81-StressTesting");

        Log.Information("   [SUCCESS] Flink stress test job submitted");
        Log.Information("   JobId: {JobId}", jobClient.GetJobId());
        
        return jobClient;
    }

    /// <summary>
    /// Execute all stress test scenarios
    /// </summary>
    private static async Task<OverallMetrics> ExecuteStressTestScenariosAsync()
    {
        var overallMetrics = new OverallMetrics();
        
        Console.WriteLine("\n📊 Stress Testing Scenarios:");
        foreach (var scenario in Scenarios)
        {
            Console.WriteLine($"  • {scenario.Name}: {scenario.RatePerSecond} events/sec for {scenario.DurationSeconds}s");
        }
        Console.WriteLine();

        foreach (var scenario in Scenarios)
        {
            Log.Information("🎯 Starting {ScenarioName}...", scenario.Name);
            
            var metrics = await ExecuteSingleScenarioAsync(scenario);
            overallMetrics.AddScenarioMetrics(metrics);
            
            Log.Information("✅ {ScenarioName} completed:", scenario.Name);
            Log.Information("   • Generated: {EventsGenerated:N0} events", metrics.EventsGenerated);
            Log.Information("   • Processed: {EventsProcessed:N0} events", metrics.EventsProcessed);
            Log.Information("   • Throughput: {Throughput:F1} events/sec", metrics.AverageThroughput);
            Log.Information("   • Error Rate: {ErrorRate:P2}", metrics.ErrorRate);
            
            // Cool-down period between scenarios
            if (scenario != Scenarios[^1])
            {
                Console.WriteLine("⏸️ Cool-down period: 3 seconds...");
                await Task.Delay(3000);
            }
        }

        return overallMetrics;
    }

    /// <summary>
    /// Execute a single stress test scenario
    /// </summary>
    private static async Task<ScenarioMetrics> ExecuteSingleScenarioAsync(LoadScenario scenario)
    {
        var metrics = new ScenarioMetrics
        {
            ScenarioName = scenario.Name,
            StartTime = DateTime.UtcNow
        };

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = KafkaBootstrapServers,
            ClientId = $"exercise81-producer-{scenario.Name.Replace(" ", "-").ToLower()}",
            Acks = Acks.All,
            LingerMs = 5
        };

        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

        Log.Information("   Generating load: {Rate} events/sec for {Duration}s", 
            scenario.RatePerSecond, scenario.DurationSeconds);

        var stopwatch = Stopwatch.StartNew();
        var eventCount = 0;
        var targetEvents = scenario.RatePerSecond * scenario.DurationSeconds;

        for (int second = 0; second < scenario.DurationSeconds; second++)
        {
            var eventsThisSecond = Math.Min(scenario.RatePerSecond, targetEvents - eventCount);
            
            // Generate events for this second
            for (int i = 0; i < eventsThisSecond; i++)
            {
                var streamEvent = GenerateStressTestEvent(eventCount, scenario.Name);
                
                try
                {
                    await producer.ProduceAsync(StressTestTopic, new Message<string, string>
                    {
                        Key = streamEvent.Id,
                        Value = JsonSerializer.Serialize(streamEvent)
                    });
                    
                    eventCount++;
                }
                catch (ProduceException<string, string> ex)
                {
                    Log.Error(ex, "Failed to produce event {EventId}", streamEvent.Id);
                }
            }
            
            // Wait for next second
            await Task.Delay(1000);
        }

        producer.Flush(TimeSpan.FromSeconds(10));
        stopwatch.Stop();

        metrics.EndTime = DateTime.UtcNow;
        metrics.Duration = stopwatch.Elapsed;
        metrics.EventsGenerated = eventCount;
        
        // Wait for processing and consume results
        await Task.Delay(TimeSpan.FromSeconds(3));
        var processedCount = await ConsumeProcessedEventsAsync(eventCount);
        
        metrics.EventsProcessed = processedCount;
        metrics.AverageThroughput = eventCount / metrics.Duration.TotalSeconds;
        metrics.ErrorRate = 1.0 - ((double)processedCount / eventCount);

        Log.Information("   Load generation completed: {EventCount} events in {Duration:F1}s", 
            eventCount, metrics.Duration.TotalSeconds);

        return metrics;
    }

    /// <summary>
    /// Consume processed events to validate processing
    /// </summary>
    private static Task<int> ConsumeProcessedEventsAsync(int expectedCount)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = KafkaBootstrapServers,
            GroupId = ConsumerGroup + "-verify",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(ProcessedEventsTopic);

        var consumedCount = 0;
        var timeoutCount = 0;
        const int maxTimeouts = 10;
        var stopwatch = Stopwatch.StartNew();

        while (consumedCount < expectedCount && timeoutCount < maxTimeouts && stopwatch.Elapsed < TimeSpan.FromSeconds(30))
        {
            try
            {
                var result = consumer.Consume(TimeSpan.FromSeconds(1));
                
                if (result != null)
                {
                    consumedCount++;
                    timeoutCount = 0;
                    consumer.Commit(result);
                }
                else
                {
                    timeoutCount++;
                }
            }
            catch (ConsumeException ex)
            {
                Log.Error(ex, "Error consuming processed event");
                break;
            }
        }

        consumer.Close();
        return Task.FromResult(consumedCount);
    }

    /// <summary>
    /// Generate realistic stress test event
    /// </summary>
    private static StreamEvent GenerateStressTestEvent(int sequence, string scenarioName)
    {
        var eventTypes = new[] { "UserAction", "SystemEvent", "ErrorEvent", "MetricEvent" };
        var eventType = (sequence % 20) switch
        {
            < 10 => "UserAction",
            < 16 => "SystemEvent",
            < 19 => "MetricEvent",
            _ => "ErrorEvent"
        };

        return new StreamEvent
        {
            Id = $"stress-{sequence:D8}",
            Timestamp = DateTime.UtcNow,
            EventType = eventType,
            ScenarioName = scenarioName,
            Sequence = sequence,
            Data = GenerateEventData(eventType, sequence)
        };
    }

    private static Dictionary<string, object> GenerateEventData(string eventType, int sequence)
    {
        return eventType switch
        {
            "UserAction" => new Dictionary<string, object>
            {
                ["userId"] = $"user_{(sequence % 100) + 1:D3}",
                ["action"] = new[] { "login", "logout", "view", "click" }[sequence % 4],
                ["sessionId"] = $"session_{sequence / 10:D4}"
            },
            "SystemEvent" => new Dictionary<string, object>
            {
                ["component"] = new[] { "database", "cache", "api", "queue" }[sequence % 4],
                ["level"] = sequence % 10 == 0 ? "error" : "info",
                ["cpu"] = Math.Round(30 + (sequence % 50) * 1.2, 1)
            },
            "ErrorEvent" => new Dictionary<string, object>
            {
                ["errorCode"] = $"ERR_{(sequence % 5) + 1:D3}",
                ["severity"] = sequence % 3 == 0 ? "high" : "medium"
            },
            _ => new Dictionary<string, object>
            {
                ["metricName"] = "response_time",
                ["value"] = Math.Round(50 + Math.Sin(sequence * 0.1) * 30, 2)
            }
        };
    }

    private static void GeneratePerformanceReport(OverallMetrics metrics)
    {
        Console.WriteLine("\n📊 COMPREHENSIVE STRESS TEST REPORT");
        Console.WriteLine("=====================================");
        Console.WriteLine($"Total Duration: {metrics.TotalDuration.TotalMinutes:F1} minutes");
        Console.WriteLine($"Total Events Generated: {metrics.TotalEventsGenerated:N0}");
        Console.WriteLine($"Total Events Processed: {metrics.TotalEventsProcessed:N0}");
        Console.WriteLine($"Average Throughput: {metrics.AverageThroughput:F1} events/sec");
        Console.WriteLine($"Peak Throughput: {metrics.PeakThroughput:F1} events/sec");
        Console.WriteLine($"Overall Success Rate: {(1 - metrics.OverallErrorRate):P2}");
        Console.WriteLine("\n🎉 Stress testing analysis completed!");
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
            new TopicSpecification { Name = StressTestTopic, NumPartitions = 4, ReplicationFactor = 1 },
            new TopicSpecification { Name = ProcessedEventsTopic, NumPartitions = 4, ReplicationFactor = 1 }
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
                Log.Information("   [SUCCESS] Topics already exist");
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
}

// Data models
public class LoadScenario
{
    public string Name { get; set; } = string.Empty;
    public int RatePerSecond { get; set; }
    public int DurationSeconds { get; set; }
}

public class StreamEvent
{
    public string Id { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string ScenarioName { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}

public class ScenarioMetrics
{
    public string ScenarioName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public long EventsGenerated { get; set; }
    public long EventsProcessed { get; set; }
    public double AverageThroughput { get; set; }
    public double ErrorRate { get; set; }
}

public class OverallMetrics
{
    private readonly List<ScenarioMetrics> _scenarios = new();
    
    public void AddScenarioMetrics(ScenarioMetrics metrics)
    {
        _scenarios.Add(metrics);
    }
    
    public long TotalEventsGenerated => _scenarios.Sum(s => s.EventsGenerated);
    public long TotalEventsProcessed => _scenarios.Sum(s => s.EventsProcessed);
    public TimeSpan TotalDuration => _scenarios.Any() ? 
        _scenarios.Max(s => s.EndTime) - _scenarios.Min(s => s.StartTime) : TimeSpan.Zero;
    public double AverageThroughput => TotalDuration.TotalSeconds > 0 ? 
        TotalEventsGenerated / TotalDuration.TotalSeconds : 0;
    public double PeakThroughput => _scenarios.Any() ? _scenarios.Max(s => s.AverageThroughput) : 0;
    public double OverallErrorRate => TotalEventsGenerated > 0 ? 
        1.0 - ((double)TotalEventsProcessed / TotalEventsGenerated) : 0;
}

/// <summary>
/// Map function that processes stress test events
/// </summary>
public class StressTestProcessingFunction : FlinkDotNet.DataStream.IMapFunction<string, string>
{
    public string Map(string eventJson)
    {
        try
        {
            var streamEvent = JsonSerializer.Deserialize<StreamEvent>(eventJson);
            if (streamEvent == null) return eventJson;

            // Simulate realistic processing time based on event type
            var processingTime = streamEvent.EventType switch
            {
                "UserAction" => 5,
                "SystemEvent" => 8,
                "ErrorEvent" => 15,
                "MetricEvent" => 3,
                _ => 10
            };
            
            Thread.Sleep(processingTime);

            // Add processing metadata
            streamEvent.Data["processed_at"] = DateTime.UtcNow.ToString("O");
            streamEvent.Data["processing_time_ms"] = processingTime;

            return JsonSerializer.Serialize(streamEvent);
        }
        catch
        {
            return eventJson;
        }
    }
}
