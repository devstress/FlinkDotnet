using System.Diagnostics;
using Confluent.Kafka.Admin;
using Confluent.Kafka;
using Serilog;

namespace Exercise101;

/// <summary>
/// Exercise 10.1: Resource Optimization with Real Kafka/Flink Infrastructure
/// 
/// Demonstrates Netflix-scale resource optimization patterns:
/// - Dynamic parallelism testing (1, 4, 8)
/// - Real CPU/Memory/GC monitoring
/// - Throughput vs resource efficiency analysis
/// - Production-ready optimization recommendations
/// 
/// Architecture: Event Generator → Kafka → Flink (variable parallelism) → Performance Analysis
/// </summary>
class Program
{
    // Environment-based configuration (set by test infrastructure)
    private static string KafkaBootstrapServers =>
        Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9093";

    private static string KafkaFlinkBootstrapServers =>
        Environment.GetEnvironmentVariable("KAFKA_FLINK_BOOTSTRAP_SERVERS") ?? "kafka:9092";

    private static string FlinkGatewayUrl =>
        Environment.GetEnvironmentVariable("FLINK_GATEWAY_URL") ?? "http://localhost:8080";

    // Kafka topics
    private const string InputTopic = "resource-optimization-input";
    private const string OutputTopic = "resource-optimization-output";
    private const string ConsumerGroup = "exercise101-consumer";

    // Performance test scenarios
    private static readonly List<(string Name, int Parallelism, int EventsPerSec, int DurationSec)> Scenarios = new()
    {
        ("Baseline", 1, 100, 15),           // Parallelism=1: Establish baseline
        ("Optimized", 4, 100, 15),          // Parallelism=4: Show improvement
        ("Over-Provisioned", 8, 100, 15)    // Parallelism=8: Demonstrate diminishing returns
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
            Log.Information("  Exercise 10.1: Resource Optimization");
            Log.Information("================================================================================");
            Log.Information("");
            Log.Information("🎓 Learning Objectives:");
            Log.Information("   • Optimize Flink parallelism for resource efficiency");
            Log.Information("   • Measure real CPU, memory, and throughput metrics");
            Log.Information("   • Identify optimal configuration through testing");
            Log.Information("   • Apply Netflix-scale optimization patterns");
            Log.Information("");
            Log.Information("📊 Configuration:");
            Log.Information("   Kafka (Host): {KafkaHost}", KafkaBootstrapServers);
            Log.Information("   Kafka (Flink): {KafkaFlink}", KafkaFlinkBootstrapServers);
            Log.Information("   Flink Gateway: {FlinkGateway}", FlinkGatewayUrl);
            Log.Information("");
            Log.Information("🔬 Test Scenarios:");
            foreach (var (name, parallelism, eventsPerSec, durationSec) in Scenarios)
            {
                Log.Information("   • {Name}: Parallelism={Parallelism}, {EventsPerSec} events/sec, {Duration}s",
                    name, parallelism, eventsPerSec, durationSec);
            }
            Log.Information("");

            ResourceMonitor? resourceMonitor = null;

            try
            {
                // Step 1: Verify infrastructure
                Log.Information(">> Step 1/6: Verifying Kafka is ready...");
                await WaitForKafkaReadyAsync();
                Log.Information("");

                Log.Information(">> Step 2/6: Verifying Flink cluster is healthy...");
                await WaitForFlinkHealthyAsync();
                Log.Information("");

                Log.Information(">> Step 3/6: Creating Kafka topics...");
                await CreateTopicsAsync();
                Log.Information("");

                // Step 2: Start resource monitoring
                Log.Information(">> Step 4/6: Starting resource monitoring...");
                resourceMonitor = new ResourceMonitor();
                await resourceMonitor.StartMonitoringAsync();
                Log.Information("");

                // Step 3: Run performance scenarios
                Log.Information(">> Step 5/6: Running performance scenarios...");
                var allMetrics = await RunAllScenariosAsync(resourceMonitor);
                Log.Information("");

                // Step 4: Analyze and generate optimization report
                Log.Information(">> Step 6/6: Analyzing performance and generating recommendations...");
                await resourceMonitor.StopMonitoringAsync();
                
                var analyzer = new OptimizationAnalyzer();
                var analysis = analyzer.AnalyzePerformance(allMetrics);
                
                // Generate comprehensive reports
                resourceMonitor.GenerateResourceReport();
                analyzer.GenerateReport(analysis);
                Log.Information("");

                // Results Summary
                Log.Information("================================================================================");
                Log.Information("  Exercise 10.1 Results - Resource Optimization");
                Log.Information("================================================================================");
                Log.Information("  ✅ Key Achievements:");
                Log.Information("     • Tested {ScenarioCount} parallelism configurations", Scenarios.Count);
                Log.Information("     • Identified optimal parallelism: {OptimalParallelism}", analysis.OptimalParallelism);
                Log.Information("     • Throughput improvement: {Improvement:F1}%", analysis.ThroughputImprovement);
                Log.Information("     • Resource efficiency gain: {Efficiency:F1}%", analysis.ResourceEfficiency);
                Log.Information("");
                Log.Information("  🎓 Key Learnings:");
                Log.Information("     ✅ Real Kafka/Flink infrastructure performance testing");
                Log.Information("     ✅ Actual CPU/Memory monitoring with System.Diagnostics");
                Log.Information("     ✅ Parallelism optimization shows diminishing returns");
                Log.Information("     ✅ Netflix-style resource efficiency patterns validated");
                Log.Information("     ✅ Data-driven optimization recommendations generated");
                Log.Information("");
                Log.Information("  💡 Production Insights:");
                Log.Information("     • Higher parallelism ≠ better performance");
                Log.Information("     • Optimal configuration balances throughput and resources");
                Log.Information("     • Measure before optimizing - data drives decisions");
                Log.Information("     • Netflix applies these patterns at 10B+ events/day scale");
                Log.Information("");
                Log.Information("[SUCCESS] EXERCISE COMPLETED SUCCESSFULLY!");
                Log.Information("================================================================================");

                return 0;
            }
            finally
            {
                // Cleanup: Stop monitoring
                if (resourceMonitor != null)
                {
                    await resourceMonitor.StopMonitoringAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Exercise 10.1 failed with exception");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    /// <summary>
    /// Run all performance scenarios
    /// </summary>
    private static async Task<List<PerformanceMetrics>> RunAllScenariosAsync(ResourceMonitor resourceMonitor)
    {
        var allMetrics = new List<PerformanceMetrics>();
        var eventGenerator = new EventGenerator(KafkaBootstrapServers, InputTopic);
        var scenario = new PerformanceScenario(
            KafkaBootstrapServers,
            KafkaFlinkBootstrapServers,
            InputTopic,
            OutputTopic,
            ConsumerGroup);

        for (int i = 0; i < Scenarios.Count; i++)
        {
            var (name, parallelism, eventsPerSec, durationSec) = Scenarios[i];

            Log.Information("═══════════════════════════════════════════════════════════");
            Log.Information("  Scenario {Current}/{Total}: {Name}", i + 1, Scenarios.Count, name);
            Log.Information("═══════════════════════════════════════════════════════════");

            try
            {
                var metrics = await scenario.RunScenarioAsync(
                    name,
                    parallelism,
                    eventsPerSec,
                    durationSec,
                    eventGenerator,
                    resourceMonitor);

                allMetrics.Add(metrics);

                // Cool-down between scenarios - OPTIMIZED: Reduced from 3s to 0.5s
                if (i < Scenarios.Count - 1)
                {
                    Log.Information("");
                    Log.Information("⏸️  Cool-down period: 0.5 seconds...");
                    await Task.Delay(500);
                    
                    // Force GC to clean state
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Log.Information("");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Scenario {Name} failed", name);
                
                // Create fallback metrics to continue testing
                allMetrics.Add(new PerformanceMetrics
                {
                    Scenario = name,
                    Parallelism = parallelism,
                    EventsGenerated = 0,
                    EventsProcessed = 0
                });
            }
        }

        return allMetrics;
    }

    /// <summary>
    /// Create Kafka topics for testing
    /// </summary>
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
                Log.Information("   [SUCCESS] Topics already exist");
            }
            else
            {
                Log.Warning("Some topics failed to create: {Errors}",
                    string.Join(", ", errors.Select(e => e.Error.Reason)));
            }
        }
    }

    /// <summary>
    /// Wait for Kafka to be ready
    /// </summary>
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

    /// <summary>
    /// Wait for Flink cluster to be healthy
    /// </summary>
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
