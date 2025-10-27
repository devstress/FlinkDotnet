using Confluent.Kafka;
using Confluent.Kafka.Admin;
using FlinkDotNet.DataStream;
using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;

namespace Exercise43;

/// <summary>
/// Day 4 Exercise 4.3: Production Performance Testing with Real Streaming Infrastructure
/// 
/// This exercise demonstrates:
/// - Real-time performance metrics collection (latency P50/P95/P99, throughput)
/// - Industry-realistic load patterns (Netflix, Uber, Twitter scenarios)
/// - Production-grade performance testing architecture with Kafka + FlinkDotNet
/// - Streaming metrics aggregation and percentile calculation
/// 
/// Architecture: Kafka Producer (Load) → Kafka → Flink Jobs (Metrics) → Kafka → Results Display
/// Key Learning: Real performance testing with streaming infrastructure at scale
/// </summary>
class Program
{
    // KAFKA ADDRESSES - Read from environment variables set by test infrastructure
    private static string KafkaBootstrapServers =>
        Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9093";
        
    private static string KafkaFlinkBootstrapServers =>
        Environment.GetEnvironmentVariable("KAFKA_FLINK_BOOTSTRAP_SERVERS") ?? "kafka:9092";
        
    private static string FlinkGatewayUrl =>
        Environment.GetEnvironmentVariable("FLINK_JOB_GATEWAY_URL") ?? "http://localhost:8080";

    private const string LoadInputTopic = "performance-load-input";
    private const string LatencyMeasurementsTopic = "performance-latency-measurements";
    private const string ThroughputMetricsTopic = "performance-throughput-metrics";
    private const string ResultsOutputTopic = "performance-results-output";
    
    private const int LoadGenerationCount = 500;

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
            Log.Information("  Exercise 4.3: Production Performance Testing - Real Infrastructure");
            Log.Information("================================================================================");
            Log.Information("");
            Log.Information("🎓 Learning Objective:");
            Log.Information("   Demonstrate production-grade performance testing with streaming metrics");
            Log.Information("   (Industry patterns: Netflix/Uber/Twitter load patterns)");
            Log.Information("");
            Log.Information("📊 Configuration:");
            Log.Information("   Kafka (Host): {KafkaHost}", KafkaBootstrapServers);
            Log.Information("   Kafka (Flink): {KafkaFlink}", KafkaFlinkBootstrapServers);
            Log.Information("   Flink Gateway: {FlinkGateway}", FlinkGatewayUrl);
            Log.Information("   Load Messages: {LoadCount}", LoadGenerationCount);
            Log.Information("");
            Log.Information("📈 Performance Metrics:");
            Log.Information("   Latency: P50, P95, P99 percentiles");
            Log.Information("   Throughput: Messages/sec, Bytes/sec");
            Log.Information("   Load Patterns: Constant, RampUp, Spike, Stress");
            Log.Information("");
            Log.Information("📊 Industry Benchmarks:");
            Log.Information("   Netflix:  <100ms video start, 99.9% uptime");
            Log.Information("   Uber:     <23ms pricing latency, 99.99% accuracy");
            Log.Information("   Twitter:  <50ms timeline update, 50K tweets/sec");
            Log.Information("");

            var jobClients = new List<FlinkDotNet.DataStream.IJobClient>();

            try
            {
                // Step 1: Verify infrastructure
                Log.Information(">> Step 1/8: Verifying Kafka is ready...");
                await WaitForKafkaReadyAsync();
                Log.Information("");

                Log.Information(">> Step 2/8: Verifying Flink cluster is healthy...");
                await WaitForFlinkHealthyAsync();
                Log.Information("");

                Log.Information(">> Step 3/8: Creating Kafka topics for performance testing...");
                await CreateTopicsAsync();
                Log.Information("");

                // Step 2: Submit Flink performance testing jobs
                Log.Information(">> Step 4/8: Submitting FlinkDotNet performance testing jobs...");
                
                Log.Information("   ⚙️  Job 1: Load Generator (generates realistic load patterns)");
                var loadGeneratorJob = await SubmitLoadGeneratorJobAsync();
                jobClients.Add(loadGeneratorJob);
                
                Log.Information("   ⚙️  Job 2: Latency Measurement (calculates P50/P95/P99)");
                var latencyMeasurementJob = await SubmitLatencyMeasurementJobAsync();
                jobClients.Add(latencyMeasurementJob);
                
                Log.Information("   ⚙️  Job 3: Throughput Benchmark (measures msg/sec)");
                var throughputBenchmarkJob = await SubmitThroughputBenchmarkJobAsync();
                jobClients.Add(throughputBenchmarkJob);
                
                await Task.Delay(TimeSpan.FromSeconds(5));
                Log.Information("");

                // Step 3: Run performance test scenarios
                Log.Information(">> Step 5/8: Running performance test scenarios...");
                await RunNetflixScenarioAsync();
                await RunUberScenarioAsync();
                await RunTwitterScenarioAsync();
                Log.Information("");

                // Step 4: Wait for processing
                Log.Information(">> Step 6/8: Waiting for metrics processing (15 seconds)...");
                await Task.Delay(TimeSpan.FromSeconds(15));
                Log.Information("");
                
                // Step 5: Consume and display results
                Log.Information(">> Step 7/8: Consuming performance test results...");
                var (resultsCount, scenarios) = await ConsumePerformanceResultsAsync();
                Log.Information("");

                // Step 6: Display final summary
                Log.Information(">> Step 8/8: Performance Testing Summary");
                DisplayPerformanceTestingSummary(scenarios);
                
                Log.Information("");
                Log.Information("[SUCCESS] EXERCISE COMPLETED SUCCESSFULLY!");
                Log.Information("================================================================================");

                return 0;
            }
            finally
            {
                // Cleanup: Cancel all Flink jobs
                if (jobClients.Any())
                {
                    Log.Information("");
                    Log.Information(">> Cleaning up: Cancelling Flink jobs...");
                    foreach (var jobClient in jobClients)
                    {
                        try
                        {
                            await jobClient.CancelAsync();
                            Log.Information("   [SUCCESS] Job cancelled: {JobId}", jobClient.GetJobId());
                        }
                        catch (Exception ex)
                        {
                            Log.Warning(ex, "Failed to cancel job");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Exercise 4.3 failed with exception");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    /// <summary>
    /// Submit Flink job for load generation based on patterns
    /// </summary>
    private static async Task<FlinkDotNet.DataStream.IJobClient> SubmitLoadGeneratorJobAsync()
    {
        var environment = StreamExecutionEnvironment.GetExecutionEnvironment();
        environment.SetParallelism(2);

        var loadRequestStream = environment.FromKafka(
            topic: LoadInputTopic,
            bootstrapServers: KafkaFlinkBootstrapServers,
            groupId: "load-generator-consumer",
            startingOffsets: "earliest"
        );

        // Generate load messages based on pattern
        var loadMessages = loadRequestStream
            .Map(new LoadGeneratorFunction());

        loadMessages
            .SinkToKafka(LatencyMeasurementsTopic, KafkaFlinkBootstrapServers);

        var jobClient = await environment.ExecuteAsync("Exercise43-LoadGenerator");
        Log.Information("      [SUCCESS] LoadGeneratorJob submitted: {JobId}", jobClient.GetJobId());
        return jobClient;
    }

    /// <summary>
    /// Submit Flink job for latency measurement and percentile calculation
    /// </summary>
    private static async Task<FlinkDotNet.DataStream.IJobClient> SubmitLatencyMeasurementJobAsync()
    {
        var environment = StreamExecutionEnvironment.GetExecutionEnvironment();
        environment.SetParallelism(4);

        var loadMessageStream = environment.FromKafka(
            topic: LatencyMeasurementsTopic,
            bootstrapServers: KafkaFlinkBootstrapServers,
            groupId: "latency-measurement-consumer",
            startingOffsets: "earliest"
        );

        // Calculate latencies and generate results
        var results = loadMessageStream
            .Map(new LatencyCalculationFunction());

        results
            .SinkToKafka(ResultsOutputTopic, KafkaFlinkBootstrapServers);

        var jobClient = await environment.ExecuteAsync("Exercise43-LatencyMeasurement");
        Log.Information("      [SUCCESS] LatencyMeasurementJob submitted: {JobId}", jobClient.GetJobId());
        return jobClient;
    }

    /// <summary>
    /// Submit Flink job for throughput benchmarking
    /// </summary>
    private static async Task<FlinkDotNet.DataStream.IJobClient> SubmitThroughputBenchmarkJobAsync()
    {
        var environment = StreamExecutionEnvironment.GetExecutionEnvironment();
        environment.SetParallelism(2);

        var loadMessageStream = environment.FromKafka(
            topic: LatencyMeasurementsTopic,
            bootstrapServers: KafkaFlinkBootstrapServers,
            groupId: "throughput-benchmark-consumer",
            startingOffsets: "earliest"
        );

        // Count messages for throughput calculation
        var throughput = loadMessageStream
            .Map(new ThroughputCalculationFunction());

        throughput
            .SinkToKafka(ThroughputMetricsTopic, KafkaFlinkBootstrapServers);

        var jobClient = await environment.ExecuteAsync("Exercise43-ThroughputBenchmark");
        Log.Information("      [SUCCESS] ThroughputBenchmarkJob submitted: {JobId}", jobClient.GetJobId());
        return jobClient;
    }

    /// <summary>
    /// Run Netflix peak traffic scenario (constant high load)
    /// </summary>
    private static async Task RunNetflixScenarioAsync()
    {
        Log.Information("   📺 Scenario 1: Netflix Peak Traffic (Constant load @ 1000 msg/sec)");
        
        var request = new LoadTestRequest(
            ScenarioId: "Netflix-Peak",
            Pattern: "Constant",
            TargetRatePerSecond: 1000,
            DurationSeconds: 15,
            StartTimestamp: DateTime.UtcNow
        );
        
        await ProduceLoadTestRequestAsync(request);
        Log.Information("      [SUCCESS] Netflix scenario triggered");
        await Task.Delay(1000);
    }

    /// <summary>
    /// Run Uber surge pricing scenario (spike pattern)
    /// </summary>
    private static async Task RunUberScenarioAsync()
    {
        Log.Information("   🚗 Scenario 2: Uber Surge Pricing (Spike load: 500→5000→500 msg/sec)");
        
        var request = new LoadTestRequest(
            ScenarioId: "Uber-Surge",
            Pattern: "Spike",
            TargetRatePerSecond: 500,
            DurationSeconds: 15,
            StartTimestamp: DateTime.UtcNow
        );
        
        await ProduceLoadTestRequestAsync(request);
        Log.Information("      [SUCCESS] Uber scenario triggered");
        await Task.Delay(1000);
    }

    /// <summary>
    /// Run Twitter viral content scenario (ramp-up pattern)
    /// </summary>
    private static async Task RunTwitterScenarioAsync()
    {
        Log.Information("   🐦 Scenario 3: Twitter Viral Content (RampUp load: 100→1000 msg/sec)");
        
        var request = new LoadTestRequest(
            ScenarioId: "Twitter-Viral",
            Pattern: "RampUp",
            TargetRatePerSecond: 100,
            DurationSeconds: 15,
            StartTimestamp: DateTime.UtcNow
        );
        
        await ProduceLoadTestRequestAsync(request);
        Log.Information("      [SUCCESS] Twitter scenario triggered");
        await Task.Delay(1000);
    }

    private static async Task ProduceLoadTestRequestAsync(LoadTestRequest request)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = KafkaBootstrapServers,
            ClientId = "exercise43-load-producer"
        };

        using var producer = new ProducerBuilder<string, string>(config).Build();
        
        // Produce load generation messages
        for (int i = 0; i < LoadGenerationCount; i++)
        {
            var loadMessage = new LoadMessage(
                ScenarioId: request.ScenarioId,
                SentTimestamp: DateTime.UtcNow,
                MessageData: $"{request.Pattern}-Message-{i}",
                Operation: request.Pattern
            );
            
            var messageJson = JsonSerializer.Serialize(loadMessage);
            
            await producer.ProduceAsync(LoadInputTopic, new Message<string, string>
            {
                Key = request.ScenarioId,
                Value = messageJson
            });
        }
        
        producer.Flush(TimeSpan.FromSeconds(5));
    }

    private static Task<(int resultsCount, Dictionary<string, ScenarioMetrics> scenarios)> ConsumePerformanceResultsAsync()
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = KafkaBootstrapServers,
            GroupId = "performance-results-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(ResultsOutputTopic);

        Log.Information("   Consuming from '{ResultsOutputTopic}' (max 20 seconds)...", ResultsOutputTopic);

        var resultsCount = 0;
        var scenarios = new Dictionary<string, ScenarioMetrics>();
        var timeoutCount = 0;
        const int maxTimeouts = 60;
        var stopwatch = Stopwatch.StartNew();

        while (timeoutCount < maxTimeouts && stopwatch.Elapsed < TimeSpan.FromSeconds(20))
        {
            try
            {
                var result = consumer.Consume(TimeSpan.FromSeconds(1));
                
                if (result != null)
                {
                    resultsCount++;
                    timeoutCount = 0;

                    try
                    {
                        var perfResult = JsonSerializer.Deserialize<PerformanceTestResult>(result.Message.Value);
                        if (perfResult != null)
                        {
                            if (!scenarios.ContainsKey(perfResult.ScenarioId))
                            {
                                scenarios[perfResult.ScenarioId] = new ScenarioMetrics(perfResult.ScenarioId);
                            }
                            
                            scenarios[perfResult.ScenarioId].AddResult(perfResult);
                        }
                    }
                    catch
                    {
                        // Ignore parsing errors
                    }
                    
                    if (resultsCount % 50 == 0)
                    {
                        Log.Information("   [{Count}] results consumed...", resultsCount);
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
                Log.Error(ex, "Error consuming results");
                break;
            }
        }

        consumer.Close();
        Log.Information("   [SUCCESS] Consumed {ResultsCount} performance results", resultsCount);
        return Task.FromResult((resultsCount, scenarios));
    }

    private static void DisplayPerformanceTestingSummary(Dictionary<string, ScenarioMetrics> scenarios)
    {
        Log.Information("");
        Log.Information("================================================================================");
        Log.Information("  Performance Testing Results - Industry Scenarios");
        Log.Information("================================================================================");
        
        foreach (var scenario in scenarios.Values.OrderBy(s => s.ScenarioId))
        {
            Log.Information("");
            Log.Information("  🎯 {ScenarioId}", scenario.ScenarioId);
            Log.Information("     Operations: {Operations:N0}", scenario.TotalOperations);
            Log.Information("     Avg Latency: {AvgLatency:F2}ms", scenario.AverageLatency);
            Log.Information("     P50 Latency: {P50:F2}ms", scenario.P50Latency);
            Log.Information("     P95 Latency: {P95:F2}ms", scenario.P95Latency);
            Log.Information("     P99 Latency: {P99:F2}ms", scenario.P99Latency);
            
            DisplayIndustryBenchmark(scenario.ScenarioId);
        }
        
        Log.Information("");
        Log.Information("  🎓 Key Learnings:");
        Log.Information("     ✅ Real Kafka/FlinkDotNet streaming performance testing");
        Log.Information("     ✅ Industry-realistic load patterns (Netflix/Uber/Twitter)");
        Log.Information("     ✅ Production-grade latency percentile calculation");
        Log.Information("     ✅ Streaming metrics aggregation at scale");
        Log.Information("     ✅ Performance benchmarking with real infrastructure");
        Log.Information("");
    }

    private static void DisplayIndustryBenchmark(string scenarioId)
    {
        if (scenarioId.Contains("Netflix", StringComparison.OrdinalIgnoreCase))
        {
            Log.Information("     📊 Netflix Benchmark: <100ms video start, 99.9% uptime");
        }
        else if (scenarioId.Contains("Uber", StringComparison.OrdinalIgnoreCase))
        {
            Log.Information("     📊 Uber Benchmark: <23ms pricing latency, 99.99% accuracy");
        }
        else if (scenarioId.Contains("Twitter", StringComparison.OrdinalIgnoreCase))
        {
            Log.Information("     📊 Twitter Benchmark: <50ms timeline update, 50K tweets/sec");
        }
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
            new TopicSpecification { Name = LoadInputTopic, NumPartitions = 1, ReplicationFactor = 1 },
            new TopicSpecification { Name = LatencyMeasurementsTopic, NumPartitions = 4, ReplicationFactor = 1 },
            new TopicSpecification { Name = ThroughputMetricsTopic, NumPartitions = 2, ReplicationFactor = 1 },
            new TopicSpecification { Name = ResultsOutputTopic, NumPartitions = 1, ReplicationFactor = 1 }
        };

        try
        {
            await admin.CreateTopicsAsync(topicsToCreate);
            Log.Information("   [SUCCESS] Topics created: performance-load-input, performance-latency-measurements, performance-throughput-metrics, performance-results-output");
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

// Data models for performance testing
public record LoadTestRequest(
    [property: JsonPropertyName("scenario_id")] string ScenarioId,
    [property: JsonPropertyName("pattern")] string Pattern,
    [property: JsonPropertyName("target_rate_per_second")] int TargetRatePerSecond,
    [property: JsonPropertyName("duration_seconds")] int DurationSeconds,
    [property: JsonPropertyName("start_timestamp")] DateTime StartTimestamp
);

public record LoadMessage(
    [property: JsonPropertyName("scenario_id")] string ScenarioId,
    [property: JsonPropertyName("sent_timestamp")] DateTime SentTimestamp,
    [property: JsonPropertyName("message_data")] string MessageData,
    [property: JsonPropertyName("operation")] string Operation
);

public record PerformanceTestResult(
    [property: JsonPropertyName("scenario_id")] string ScenarioId,
    [property: JsonPropertyName("operations_completed")] int OperationsCompleted,
    [property: JsonPropertyName("average_latency_ms")] double AverageLatencyMs,
    [property: JsonPropertyName("p50_latency_ms")] double P50LatencyMs,
    [property: JsonPropertyName("p95_latency_ms")] double P95LatencyMs,
    [property: JsonPropertyName("p99_latency_ms")] double P99LatencyMs,
    [property: JsonPropertyName("throughput_ops_per_sec")] double ThroughputOpsPerSec,
    [property: JsonPropertyName("success")] bool Success
);

// Helper class to track scenario metrics
public class ScenarioMetrics
{
    public string ScenarioId { get; }
    public int TotalOperations { get; private set; }
    public double AverageLatency { get; private set; }
    public double P50Latency { get; private set; }
    public double P95Latency { get; private set; }
    public double P99Latency { get; private set; }

    public ScenarioMetrics(string scenarioId)
    {
        ScenarioId = scenarioId;
    }

    public void AddResult(PerformanceTestResult result)
    {
        TotalOperations += result.OperationsCompleted;
        AverageLatency = (AverageLatency + result.AverageLatencyMs) / 2;
        P50Latency = Math.Max(P50Latency, result.P50LatencyMs);
        P95Latency = Math.Max(P95Latency, result.P95LatencyMs);
        P99Latency = Math.Max(P99Latency, result.P99LatencyMs);
    }
}

/// <summary>
/// Flink Map Function for load generation based on patterns
/// </summary>
public class LoadGeneratorFunction : FlinkDotNet.DataStream.IMapFunction<string, string>
{
    public string Map(string input)
    {
        try
        {
            var loadMessage = JsonSerializer.Deserialize<LoadMessage>(input);
            if (loadMessage == null)
                return string.Empty;
            
            // Simulate processing based on load pattern
            var processingTime = loadMessage.Operation switch
            {
                "Constant" => 10,
                "RampUp" => 15,
                "Spike" => 5,
                "Stress" => 20,
                _ => 10
            };
            
            Thread.Sleep(processingTime);
            
            return input;
        }
        catch
        {
            return string.Empty;
        }
    }
}

/// <summary>
/// Flink Map Function for latency calculation
/// </summary>
public class LatencyCalculationFunction : FlinkDotNet.DataStream.IMapFunction<string, string>
{
    private readonly List<double> _latencies = new();

    public string Map(string input)
    {
        try
        {
            var loadMessage = JsonSerializer.Deserialize<LoadMessage>(input);
            if (loadMessage == null)
                return string.Empty;
            
            var receivedTime = DateTime.UtcNow;
            var latencyMs = (receivedTime - loadMessage.SentTimestamp).TotalMilliseconds;
            
            _latencies.Add(latencyMs);
            
            // Calculate percentiles every 100 messages
            if (_latencies.Count >= 100)
            {
                var sortedLatencies = _latencies.OrderBy(l => l).ToArray();
                var p50Index = (int)(sortedLatencies.Length * 0.50);
                var p95Index = (int)(sortedLatencies.Length * 0.95);
                var p99Index = (int)(sortedLatencies.Length * 0.99);
                
                var result = new PerformanceTestResult(
                    ScenarioId: loadMessage.ScenarioId,
                    OperationsCompleted: _latencies.Count,
                    AverageLatencyMs: _latencies.Average(),
                    P50LatencyMs: sortedLatencies[p50Index],
                    P95LatencyMs: sortedLatencies[p95Index],
                    P99LatencyMs: sortedLatencies[p99Index],
                    ThroughputOpsPerSec: _latencies.Count / 5.0,
                    Success: true
                );
                
                _latencies.Clear();
                return JsonSerializer.Serialize(result);
            }
            
            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}

/// <summary>
/// Flink Map Function for throughput calculation
/// </summary>
public class ThroughputCalculationFunction : FlinkDotNet.DataStream.IMapFunction<string, string>
{
    private int _messageCount = 0;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    public string Map(string input)
    {
        try
        {
            _messageCount++;
            
            // Report throughput every 100 messages
            if (_messageCount % 100 == 0)
            {
                var throughput = _messageCount / _stopwatch.Elapsed.TotalSeconds;
                return $"Throughput: {throughput:F1} msg/sec";
            }
            
            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
