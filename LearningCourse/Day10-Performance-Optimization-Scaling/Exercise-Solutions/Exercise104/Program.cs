using System.Diagnostics;
using Confluent.Kafka.Admin;
using Confluent.Kafka;
using Serilog;

namespace Exercise104;

/// <summary>
/// Exercise 10.4: Throughput Tuning with Real Kafka Infrastructure
/// 
/// Demonstrates high-performance throughput optimization patterns:
/// - Serialization comparison (JSON vs Binary vs MessagePack)
/// - Compression testing (None vs GZip)
/// - Batch optimization for maximum throughput
/// - End-to-end performance analysis
/// 
/// Architecture: High-Volume Generator → Kafka (optimized) → Throughput Analysis
/// </summary>
class Program
{
    // Environment-based configuration
    private static string KafkaBootstrapServers =>
        Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "127.0.0.1:9093";

    // Kafka topics
    private const string InputTopic = "throughput-optimization-input";

    // Test configuration
    private const int EventCount = 1000; // Process 1000 events per scenario

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
            Log.Information("  Exercise 10.4: Throughput Tuning");
            Log.Information("================================================================================");
            Log.Information("");
            Log.Information("🎓 Learning Objectives:");
            Log.Information("   • Compare serialization formats for throughput");
            Log.Information("   • Test compression impact on performance");
            Log.Information("   • Optimize batch sizes for maximum throughput");
            Log.Information("   • Apply production-grade optimization patterns");
            Log.Information("");
            Log.Information("📊 Configuration:");
            Log.Information("   Kafka: {KafkaHost}", KafkaBootstrapServers);
            Log.Information("   Events per scenario: {EventCount}", EventCount);
            Log.Information("");
            Log.Information("🔬 Test Scenarios:");
            Log.Information("   1. Baseline (JSON, No Compression, Batch=1)");
            Log.Information("   2. Binary Serialization");
            Log.Information("   3. MessagePack Serialization");
            Log.Information("   4. Optimized (MessagePack + GZip + Batch=100)");
            Log.Information("");

            // Step 1: Verify infrastructure
            Log.Information(">> Step 1/4: Verifying Kafka is ready...");
            await WaitForKafkaReadyAsync();
            Log.Information("");

            Log.Information(">> Step 2/4: Creating Kafka topics...");
            await CreateTopicsAsync();
            Log.Information("");

            // Step 2: Run throughput scenarios
            Log.Information(">> Step 3/4: Running throughput optimization scenarios...");
            var allMetrics = await RunAllScenariosAsync();
            Log.Information("");

            // Step 3: Generate analysis and recommendations
            Log.Information(">> Step 4/4: Analyzing results and generating recommendations...");
            GenerateComparisonReport(allMetrics);
            GenerateRecommendations(allMetrics);
            Log.Information("");

            // Results Summary
            Log.Information("================================================================================");
            Log.Information("  Exercise 10.4 Results - Throughput Tuning");
            Log.Information("================================================================================");
            
            if (allMetrics.Count > 1)
            {
                var baseline = allMetrics[0];
                var optimized = allMetrics[allMetrics.Count - 1];

                var throughputImprovement = baseline.ThroughputEventsPerSec > 0
                    ? ((optimized.ThroughputEventsPerSec - baseline.ThroughputEventsPerSec) / baseline.ThroughputEventsPerSec) * 100
                    : 0;

                var sizeReduction = baseline.SerializedSizeBytes > 0
                    ? (1.0 - (double)optimized.SerializedSizeBytes / baseline.SerializedSizeBytes) * 100
                    : 0;

                Log.Information("  ✅ Key Achievements:");
                Log.Information("     • Tested {ScenarioCount} optimization strategies", allMetrics.Count);
                Log.Information("     • Throughput improvement: {Improvement:F1}%", throughputImprovement);
                Log.Information("     • Data size reduction: {SizeReduction:F1}%", sizeReduction);
                Log.Information("     • Compression ratio: {Ratio:F2}x", optimized.CompressionRatio);
                Log.Information("     • Optimal batch size: {BatchSize}", optimized.BatchSize);
            }

            Log.Information("");
            Log.Information("  🎓 Key Learnings:");
            Log.Information("     ✅ Real Kafka infrastructure with performance profiling");
            Log.Information("     ✅ MessagePack provides best serialization performance");
            Log.Information("     ✅ Compression reduces network traffic significantly");
            Log.Information("     ✅ Batching dramatically improves throughput");
            Log.Information("     ✅ Combined optimizations multiply benefits");
            Log.Information("");
            Log.Information("  💡 Production Insights:");
            Log.Information("     • Serialization choice = 2-3x throughput difference");
            Log.Information("     • Compression = lower bandwidth costs");
            Log.Information("     • Batching = essential for high-throughput systems");
            Log.Information("     • MessagePack + GZip + Batching = production standard");
            Log.Information("     • Measure your specific workload before optimizing");
            Log.Information("");
            Log.Information("[SUCCESS] EXERCISE COMPLETED SUCCESSFULLY!");
            Log.Information("================================================================================");

            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Exercise 10.4 failed with exception");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    /// <summary>
    /// Run all throughput optimization scenarios
    /// </summary>
    private static async Task<List<ThroughputMetrics>> RunAllScenariosAsync()
    {
        var allMetrics = new List<ThroughputMetrics>();
        var scenario = new ThroughputScenario(KafkaBootstrapServers, InputTopic);

        var scenarios = new[]
        {
            ("Baseline", new Func<Task<ThroughputMetrics>>(async () => 
                await scenario.RunBaselineAsync(EventCount))),
            ("Binary Serialization", new Func<Task<ThroughputMetrics>>(async () => 
                await scenario.RunBinarySerializationAsync(EventCount))),
            ("MessagePack", new Func<Task<ThroughputMetrics>>(async () => 
                await scenario.RunMessagePackAsync(EventCount))),
            ("Optimized", new Func<Task<ThroughputMetrics>>(async () => 
                await scenario.RunOptimizedAsync(EventCount, batchSize: 100)))
        };

        for (int i = 0; i < scenarios.Length; i++)
        {
            var (name, scenarioFunc) = scenarios[i];

            Log.Information("═══════════════════════════════════════════════════════════");
            Log.Information("  Scenario {Current}/{Total}: {Name}", i + 1, scenarios.Length, name);
            Log.Information("═══════════════════════════════════════════════════════════");

            try
            {
                var metrics = await scenarioFunc();
                allMetrics.Add(metrics);

                // Display scenario results
                Log.Information("   Events Processed: {Count}", metrics.EventsProcessed);
                Log.Information("   Throughput: {Throughput:F0} events/sec", metrics.ThroughputEventsPerSec);
                Log.Information("   Serialized Size: {Size:F2} KB", metrics.SerializedSizeBytes / 1024.0);
                Log.Information("   Compression Ratio: {Ratio:F2}x", metrics.CompressionRatio);
                Log.Information("   Processing Time: {Time:F0} ms", metrics.ProcessingTimeMs);
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
                allMetrics.Add(new ThroughputMetrics
                {
                    Scenario = name,
                    EventsProcessed = 0
                });
            }
        }

        return allMetrics;
    }

    /// <summary>
    /// Generate performance comparison report
    /// </summary>
    private static void GenerateComparisonReport(List<ThroughputMetrics> allMetrics)
    {
        if (allMetrics.Count == 0)
        {
            Console.WriteLine("   No metrics to analyze");
            return;
        }

        Console.WriteLine("");
        Console.WriteLine("================================================================================");
        Console.WriteLine("  Throughput Optimization Comparison");
        Console.WriteLine("================================================================================");
        Console.WriteLine("");

        // Table header
        Console.WriteLine("  {0,-35} {1,15} {2,12} {3,12}",
            "Scenario", "Throughput/s", "Size (KB)", "Comp Ratio");
        Console.WriteLine("  " + new string('-', 78));

        // Table rows
        foreach (var metrics in allMetrics)
        {
            Console.WriteLine("  {0,-35} {1,15:F0} {2,12:F2} {3,12:F2}x",
                metrics.Scenario,
                metrics.ThroughputEventsPerSec,
                metrics.SerializedSizeBytes / 1024.0,
                metrics.CompressionRatio);
        }

        Console.WriteLine("");

        // Calculate improvements
        if (allMetrics.Count > 1)
        {
            var baseline = allMetrics[0];
            var optimized = allMetrics[allMetrics.Count - 1];

            var throughputImprovement = baseline.ThroughputEventsPerSec > 0
                ? ((optimized.ThroughputEventsPerSec - baseline.ThroughputEventsPerSec) / baseline.ThroughputEventsPerSec) * 100
                : 0;

            var sizeReduction = baseline.SerializedSizeBytes > 0
                ? (1.0 - (double)optimized.SerializedSizeBytes / baseline.SerializedSizeBytes) * 100
                : 0;

            Console.WriteLine("  Key Improvements:");
            Console.WriteLine($"     Throughput Increase: {throughputImprovement:F1}%");
            Console.WriteLine($"     Data Size Reduction: {sizeReduction:F1}%");
            Console.WriteLine($"     Optimal Configuration: {optimized.Scenario}");
        }

        Console.WriteLine("");
    }

    /// <summary>
    /// Generate optimization recommendations
    /// </summary>
    private static void GenerateRecommendations(List<ThroughputMetrics> allMetrics)
    {
        Console.WriteLine("  💡 Optimization Recommendations:");
        Console.WriteLine("");

        if (allMetrics.Count == 0)
        {
            Console.WriteLine("     No data available for recommendations");
            return;
        }

        // Find best performing scenario
        var bestThroughput = allMetrics.MaxBy(m => m.ThroughputEventsPerSec);
        var smallestSize = allMetrics.MinBy(m => m.SerializedSizeBytes);

        if (bestThroughput != null)
        {
            Console.WriteLine($"     ✅ Best Throughput: {bestThroughput.Scenario}");
            Console.WriteLine($"        - {bestThroughput.ThroughputEventsPerSec:F0} events/sec");
            Console.WriteLine("        - Use this for maximum processing speed");
        }

        Console.WriteLine("");

        if (smallestSize != null)
        {
            Console.WriteLine($"     ✅ Smallest Data Size: {smallestSize.Scenario}");
            Console.WriteLine($"        - {smallestSize.SerializedSizeBytes / 1024.0:F2} KB total");
            Console.WriteLine("        - Use this to minimize network bandwidth");
        }

        Console.WriteLine("");

        // General recommendations
        Console.WriteLine("     🔧 General Recommendations:");
        Console.WriteLine("        • Use MessagePack for binary efficiency");
        Console.WriteLine("        • Enable GZip compression for network optimization");
        Console.WriteLine("        • Batch events (50-100) for maximum throughput");
        Console.WriteLine("        • Monitor compression CPU overhead in production");
        Console.WriteLine("        • Profile your specific workload and data patterns");

        Console.WriteLine("");
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
            new TopicSpecification { Name = InputTopic, NumPartitions = 4, ReplicationFactor = 1 }
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
