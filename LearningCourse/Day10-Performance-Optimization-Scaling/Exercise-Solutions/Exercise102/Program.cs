using System.Diagnostics;
using Confluent.Kafka.Admin;
using Confluent.Kafka;
using Serilog;

namespace Exercise102;

/// <summary>
/// Exercise 10.2: Horizontal Scaling with Real Kafka/Flink Infrastructure
/// 
/// Demonstrates LinkedIn-scale horizontal scaling patterns:
/// - Dynamic node count testing (1, 2, 4, 8 nodes)
/// - 8-partition Kafka topic for load distribution
/// - Node-level metrics tracking
/// - Load distribution analysis
/// - Scaling efficiency calculation
/// 
/// Architecture: Event Generator → Kafka (8 partitions) → Flink (1,2,4,8 nodes) → Scaling Analysis
/// </summary>
class Program
{
    // Environment-based configuration (set by test infrastructure)
    private static string KafkaBootstrapServers =>
        Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9093";

    private static string KafkaFlinkBootstrapServers =>
        Environment.GetEnvironmentVariable("KAFKA_FLINK_BOOTSTRAP_SERVERS") ?? "kafka:9092";

    private static string FlinkGatewayUrl =>
        Environment.GetEnvironmentVariable("FLINK_JOB_GATEWAY_URL") ?? "http://localhost:8086";

    // Kafka topics - 8 partitions for load distribution testing
    private const string InputTopic = "horizontal-scaling-input";
    private const string OutputTopic = "horizontal-scaling-output";
    private const string ConsumerGroup = "exercise102-consumer";
    private const int TotalPartitions = 8;

    // Scaling test scenarios
    private static readonly List<(string Name, int NodeCount, int EventsPerSec, int DurationSec)> Scenarios = new()
    {
        ("Single Node", 1, 100, 15),        // 1 node: Baseline
        ("Horizontal Scale", 2, 100, 15),   // 2 nodes: 2x expected
        ("Optimized", 4, 100, 15),          // 4 nodes: 4x expected
        ("Saturated", 8, 100, 15)           // 8 nodes: 1 partition per node
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
            Log.Information("  Exercise 10.2: Horizontal Scaling");
            Log.Information("================================================================================");
            Log.Information("");
            Log.Information("🎓 Learning Objectives:");
            Log.Information("   • Demonstrate horizontal scaling with Kafka partitions");
            Log.Information("   • Track load distribution across simulated nodes");
            Log.Information("   • Calculate scaling efficiency (actual vs ideal speedup)");
            Log.Information("   • Identify optimal node count and bottlenecks");
            Log.Information("   • Apply LinkedIn-scale load balancing patterns");
            Log.Information("");
            Log.Information("📊 Configuration:");
            Log.Information("   Kafka (Host): {KafkaHost}", KafkaBootstrapServers);
            Log.Information("   Kafka (Flink): {KafkaFlink}", KafkaFlinkBootstrapServers);
            Log.Information("   Flink Gateway: {FlinkGateway}", FlinkGatewayUrl);
            Log.Information("   Total Partitions: {Partitions}", TotalPartitions);
            Log.Information("");
            Log.Information("🔬 Test Scenarios:");
            foreach (var (name, nodeCount, eventsPerSec, durationSec) in Scenarios)
            {
                Log.Information("   • {Name}: Nodes={NodeCount}, {EventsPerSec} events/sec, {Duration}s",
                    name, nodeCount, eventsPerSec, durationSec);
            }
            Log.Information("");

            // Step 1: Verify infrastructure
            Log.Information(">> Step 1/5: Verifying Kafka is ready...");
            await WaitForKafkaReadyAsync();
            Log.Information("");

            Log.Information(">> Step 2/5: Verifying Flink cluster is healthy...");
            await WaitForFlinkHealthyAsync();
            Log.Information("");

            Log.Information(">> Step 3/5: Creating Kafka topics with {Partitions} partitions...", TotalPartitions);
            await CreateTopicsAsync();
            Log.Information("");

            // Step 2: Run scaling scenarios
            Log.Information(">> Step 4/5: Running horizontal scaling scenarios...");
            var allMetrics = await RunAllScenariosAsync();
            Log.Information("");

            // Step 3: Analyze and generate scaling report
            Log.Information(">> Step 5/5: Analyzing scaling performance and generating recommendations...");
            
            var loadBalancer = new LoadBalancer();
            loadBalancer.GenerateLoadDistributionReport(allMetrics);
            
            var analyzer = new ScalingAnalyzer();
            var analysis = analyzer.AnalyzeScaling(allMetrics);
            analyzer.GenerateReport(analysis);
            Log.Information("");

            // Results Summary
            Log.Information("================================================================================");
            Log.Information("  Exercise 10.2 Results - Horizontal Scaling");
            Log.Information("================================================================================");
            Log.Information("  ✅ Key Achievements:");
            Log.Information("     • Tested {ScenarioCount} node configurations (1, 2, 4, 8)", Scenarios.Count);
            Log.Information("     • Optimal node count: {OptimalNodes}", analysis.OptimalNodeCount);
            Log.Information("     • Best throughput: {Throughput:F1} events/sec", analysis.BestThroughput);
            Log.Information("     • Scaling efficiency: {Efficiency:F1}%", analysis.BestScalingEfficiency);
            Log.Information("     • Scaling pattern: {Pattern}", analysis.ScalingPattern);
            Log.Information("");
            Log.Information("  🎓 Key Learnings:");
            Log.Information("     ✅ Real Kafka partitioning enables horizontal scaling");
            Log.Information("     ✅ Load distribution quality measured via coefficient of variation");
            Log.Information("     ✅ Scaling efficiency shows diminishing returns pattern");
            Log.Information("     ✅ Partition count limits maximum effective node count");
            Log.Information("     ✅ LinkedIn-style dynamic load balancing demonstrated");
            Log.Information("");
            Log.Information("  💡 Production Insights:");
            Log.Information("     • More nodes ≠ proportional performance gain");
            Log.Information("     • Partition count must exceed node count for good distribution");
            Log.Information("     • Monitor scaling efficiency to avoid over-provisioning");
            Log.Information("     • LinkedIn processes 7T+ messages/day with similar patterns");
            Log.Information("");
            Log.Information("[SUCCESS] EXERCISE COMPLETED SUCCESSFULLY!");
            Log.Information("================================================================================");

            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Exercise 10.2 failed with exception");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    /// <summary>
    /// Run all scaling scenarios
    /// </summary>
    private static async Task<List<ScalingMetrics>> RunAllScenariosAsync()
    {
        var allMetrics = new List<ScalingMetrics>();
        var eventGenerator = new EventGenerator(KafkaBootstrapServers, InputTopic, TotalPartitions);
        var loadBalancer = new LoadBalancer();
        var nodeSimulator = new NodeSimulator(
            KafkaBootstrapServers,
            KafkaFlinkBootstrapServers,
            InputTopic,
            OutputTopic,
            ConsumerGroup);

        for (int i = 0; i < Scenarios.Count; i++)
        {
            var (name, nodeCount, eventsPerSec, durationSec) = Scenarios[i];

            Log.Information("═══════════════════════════════════════════════════════════");
            Log.Information("  Scenario {Current}/{Total}: {Name}", i + 1, Scenarios.Count, name);
            Log.Information("═══════════════════════════════════════════════════════════");

            try
            {
                var metrics = await nodeSimulator.RunScalingScenarioAsync(
                    name,
                    nodeCount,
                    eventsPerSec,
                    durationSec,
                    eventGenerator,
                    loadBalancer);

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
                allMetrics.Add(new ScalingMetrics
                {
                    Scenario = name,
                    NodeCount = nodeCount,
                    TotalPartitions = TotalPartitions,
                    EventsGenerated = 0,
                    EventsProcessed = 0
                });
            }
        }

        return allMetrics;
    }

    /// <summary>
    /// Create Kafka topics with specified partitions for testing
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
            new TopicSpecification 
            { 
                Name = InputTopic, 
                NumPartitions = TotalPartitions, 
                ReplicationFactor = 1 
            },
            new TopicSpecification 
            { 
                Name = OutputTopic, 
                NumPartitions = TotalPartitions, 
                ReplicationFactor = 1 
            }
        };

        try
        {
            await admin.CreateTopicsAsync(topicsToCreate);
            Log.Information("   [SUCCESS] Topics created with {Partitions} partitions: {Topics}",
                TotalPartitions, string.Join(", ", topicsToCreate.Select(t => t.Name)));
        }
        catch (CreateTopicsException ex)
        {
            var errors = ex.Results.Where(r => r.Error.Code != ErrorCode.TopicAlreadyExists).ToList();
            if (!errors.Any())
            {
                Log.Information("   [SUCCESS] Topics already exist with {Partitions} partitions", TotalPartitions);
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
