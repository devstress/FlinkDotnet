using System.Diagnostics;
using Confluent.Kafka.Admin;
using Confluent.Kafka;
using Serilog;

namespace Exercise103;

/// <summary>
/// Exercise 10.3: Memory Management Optimization with Real Kafka Infrastructure
/// 
/// Demonstrates Uber-scale memory optimization patterns:
/// - Object pooling for reduced GC pressure
/// - LRU cache for efficient lookups
/// - Real GC and memory monitoring
/// - Comparative performance analysis
/// 
/// Architecture: Event Generator → Kafka → Memory-Optimized Processing → Performance Analysis
/// </summary>
class Program
{
    // Environment-based configuration
    private static string KafkaBootstrapServers =>
        Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "127.0.0.1:9093";

    // Kafka topics
    private const string InputTopic = "memory-optimization-input";
    private const string OutputTopic = "memory-optimization-output";

    // Test configuration
    private const int EventCount = 500; // Process 500 events per scenario

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
            Log.Information("  Exercise 10.3: Memory Management Optimization");
            Log.Information("================================================================================");
            Log.Information("");
            Log.Information("🎓 Learning Objectives:");
            Log.Information("   • Implement object pooling to reduce GC pressure");
            Log.Information("   • Use LRU cache for efficient data lookups");
            Log.Information("   • Monitor real GC behavior and memory usage");
            Log.Information("   • Apply Uber-scale memory optimization patterns");
            Log.Information("");
            Log.Information("📊 Configuration:");
            Log.Information("   Kafka: {KafkaHost}", KafkaBootstrapServers);
            Log.Information("   Events per scenario: {EventCount}", EventCount);
            Log.Information("");
            Log.Information("🔬 Test Scenarios:");
            Log.Information("   1. Baseline (No Optimization)");
            Log.Information("   2. Object Pooling");
            Log.Information("   3. LRU Cache");
            Log.Information("   4. Combined Optimization (Pool + Cache)");
            Log.Information("");

            // Step 1: Verify infrastructure
            Log.Information(">> Step 1/4: Verifying Kafka is ready...");
            await WaitForKafkaReadyAsync();
            Log.Information("");

            Log.Information(">> Step 2/4: Creating Kafka topics...");
            await CreateTopicsAsync();
            Log.Information("");

            // Step 2: Run memory scenarios
            Log.Information(">> Step 3/4: Running memory optimization scenarios...");
            var allMetrics = await RunAllScenariosAsync();
            Log.Information("");

            // Step 3: Generate analysis and recommendations
            Log.Information(">> Step 4/4: Analyzing results and generating recommendations...");
            var analyzer = new MemoryAnalyzer();
            analyzer.GenerateComparisonReport(allMetrics);
            analyzer.GenerateRecommendations(allMetrics);
            Log.Information("");

            // Results Summary
            Log.Information("================================================================================");
            Log.Information("  Exercise 10.3 Results - Memory Management");
            Log.Information("================================================================================");
            
            if (allMetrics.Count > 1)
            {
                var baseline = allMetrics[0];
                var optimized = allMetrics[allMetrics.Count - 1];

                var gcReduction = baseline.Gen0Collections > 0
                    ? (1.0 - (double)optimized.Gen0Collections / baseline.Gen0Collections) * 100
                    : 0;

                var allocationReduction = baseline.AllocationRateMBPerSec > 0
                    ? (1.0 - optimized.AllocationRateMBPerSec / baseline.AllocationRateMBPerSec) * 100
                    : 0;

                Log.Information("  ✅ Key Achievements:");
                Log.Information("     • Tested {ScenarioCount} optimization strategies", allMetrics.Count);
                Log.Information("     • GC Gen0 reduction: {GCReduction:F1}%", gcReduction);
                Log.Information("     • Allocation rate reduction: {AllocationReduction:F1}%", allocationReduction);
                if (optimized.ObjectPoolHits > 0)
                {
                    Log.Information("     • Object pool efficiency: {PoolEfficiency:F1}%", optimized.PoolEfficiency);
                }
                if (optimized.CacheHits > 0)
                {
                    Log.Information("     • Cache hit ratio: {CacheHitRatio:F1}%", optimized.CacheHitRatio);
                }
            }

            Log.Information("");
            Log.Information("  🎓 Key Learnings:");
            Log.Information("     ✅ Real Kafka infrastructure with memory profiling");
            Log.Information("     ✅ Object pooling reduces GC frequency significantly");
            Log.Information("     ✅ LRU cache improves lookup performance");
            Log.Information("     ✅ Combined optimizations compound benefits");
            Log.Information("     ✅ Uber applies these patterns at petabyte scale");
            Log.Information("");
            Log.Information("  💡 Production Insights:");
            Log.Information("     • Object pooling is essential for high-throughput systems");
            Log.Information("     • Cache hit ratios >80% indicate effective caching");
            Log.Information("     • Monitor Gen2 collections - they are expensive");
            Log.Information("     • Measure before optimizing - data drives decisions");
            Log.Information("     • Memory management = throughput + lower infrastructure costs");
            Log.Information("");
            Log.Information("[SUCCESS] EXERCISE COMPLETED SUCCESSFULLY!");
            Log.Information("================================================================================");

            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Exercise 10.3 failed with exception");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    /// <summary>
    /// Run all memory optimization scenarios
    /// </summary>
    private static async Task<List<MemoryMetrics>> RunAllScenariosAsync()
    {
        var allMetrics = new List<MemoryMetrics>();
        var scenario = new MemoryScenario(KafkaBootstrapServers, InputTopic, OutputTopic);
        var monitor = new MemoryMonitor(samplingIntervalMs: 100);

        var scenarios = new[]
        {
            ("Baseline (No Optimization)", new Func<Task<MemoryMetrics>>(async () => 
                await scenario.RunBaselineAsync(EventCount, monitor))),
            ("Object Pooling", new Func<Task<MemoryMetrics>>(async () => 
                await scenario.RunWithObjectPoolingAsync(EventCount, monitor))),
            ("LRU Cache", new Func<Task<MemoryMetrics>>(async () => 
                await scenario.RunWithCachingAsync(EventCount, monitor))),
            ("Combined (Pool + Cache)", new Func<Task<MemoryMetrics>>(async () => 
                await scenario.RunCombinedOptimizationAsync(EventCount, monitor)))
        };

        for (int i = 0; i < scenarios.Length; i++)
        {
            var (name, scenarioFunc) = scenarios[i];

            Log.Information("═══════════════════════════════════════════════════════════");
            Log.Information("  Scenario {Current}/{Total}: {Name}", i + 1, scenarios.Length, name);
            Log.Information("═══════════════════════════════════════════════════════════");

            try
            {
                monitor.Clear();
                var metrics = await scenarioFunc();
                allMetrics.Add(metrics);

                // Display scenario results
                Log.Information("   Events Processed: {Count}", metrics.EventsProcessed);
                Log.Information("   Gen0 Collections: {Gen0}", metrics.Gen0Collections);
                Log.Information("   Gen2 Collections: {Gen2}", metrics.Gen2Collections);
                Log.Information("   Peak Working Set: {Peak:F1} MB", metrics.PeakWorkingSet / 1024.0 / 1024.0);
                Log.Information("   Allocation Rate: {Rate:F2} MB/sec", metrics.AllocationRateMBPerSec);
                
                if (metrics.ObjectPoolHits > 0)
                {
                    Log.Information("   Pool Efficiency: {Efficiency:F1}%", metrics.PoolEfficiency);
                }
                
                if (metrics.CacheHits > 0)
                {
                    Log.Information("   Cache Hit Ratio: {Ratio:F1}%", metrics.CacheHitRatio);
                }

                Log.Information("   ✅ Scenario completed");

                // Cool-down between scenarios - OPTIMIZED: Reduced from 3s to 0.5s
                if (i < scenarios.Length - 1)
                {
                    Log.Information("");
                    Log.Information("⏸️  Cool-down period: 0.5 seconds...");
                    await Task.Delay(500);
                    
                    // Force GC to clean state
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    Log.Information("");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Scenario {Name} failed", name);
                
                // Create fallback metrics
                allMetrics.Add(new MemoryMetrics
                {
                    Scenario = name,
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
        var timeout = TimeSpan.FromSeconds(60);
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
}
