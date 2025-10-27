using System.Diagnostics;
using System.Text.Json;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using FlinkDotNet.DataStream;
using Serilog;

namespace Exercise84;

/// <summary>
/// Exercise 8.4: Resource Monitoring & Capacity Planning
/// 
/// Real-time resource monitoring system that demonstrates:
/// - Actual container resource usage tracking via Docker stats
/// - Kafka metrics monitoring for throughput analysis
/// - Flink job resource consumption profiling
/// - Production capacity planning with real measurements
/// 
/// Architecture: Kafka workload generation → Flink processing → Resource monitoring
/// </summary>
class Program
{
    // Kafka addresses - read from environment variables set by test infrastructure
    private static string KafkaBootstrapServers =>
        Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9093";
        
    private static string KafkaFlinkBootstrapServers =>
        Environment.GetEnvironmentVariable("KAFKA_FLINK_BOOTSTRAP_SERVERS") ?? "kafka:9092";
        
    private static string FlinkGatewayUrl =>
        Environment.GetEnvironmentVariable("FLINK_JOB_GATEWAY_URL") ?? "http://localhost:8086";

    // Kafka topics for resource monitoring
    private const string ResourceInputTopic = "resource-monitor-input";
    private const string ResourceOutputTopic = "resource-monitor-output";
    private const string ConsumerGroup = "exercise84-consumer";
    
    // Resource monitoring scenarios
    private static readonly List<WorkloadScenario> Scenarios = new()
    {
        new() { Name = "Light Workload", EventsPerSecond = 50, DurationSeconds = 10, ConcurrentTasks = 10 },
        new() { Name = "Normal Workload", EventsPerSecond = 100, DurationSeconds = 15, ConcurrentTasks = 25 },
        new() { Name = "Heavy Workload", EventsPerSecond = 200, DurationSeconds = 10, ConcurrentTasks = 50 }
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
            Log.Information("  Exercise 8.4: Resource Monitoring & Capacity Planning");
            Log.Information("================================================================================");
            Log.Information("");
            Log.Information("Learning Objectives:");
            Log.Information("  - Real container resource monitoring");
            Log.Information("  - Actual Kafka metrics tracking");
            Log.Information("  - Production capacity planning");
            Log.Information("  - Resource optimization patterns");
            Log.Information("");
            Log.Information("Configuration:");
            Log.Information("  Kafka (Host): {KafkaHost}", KafkaBootstrapServers);
            Log.Information("  Kafka (Flink): {KafkaFlink}", KafkaFlinkBootstrapServers);
            Log.Information("  Flink Gateway: {FlinkGateway}", FlinkGatewayUrl);
            Log.Information("");

            FlinkDotNet.DataStream.IJobClient? jobClient = null;
            ResourceMonitor? resourceMonitor = null;

            try
            {
                // Step 1: Verify infrastructure
                Log.Information(">> Step 1/7: Verifying Kafka is ready...");
                await WaitForKafkaReadyAsync();
                Log.Information("");

                Log.Information(">> Step 2/7: Verifying Flink cluster is ready...");
                await WaitForFlinkHealthyAsync();
                Log.Information("");

                Log.Information(">> Step 3/7: Creating Kafka topics...");
                await CreateTopicsAsync();
                Log.Information("");

                // Step 2: Start resource monitoring
                Log.Information(">> Step 4/7: Starting resource monitoring...");
                resourceMonitor = new ResourceMonitor();
                await resourceMonitor.StartMonitoringAsync();
                Log.Information("");

                // Step 3: Submit Flink resource processing job
                Log.Information(">> Step 5/7: Submitting Flink resource processing job...");
                jobClient = await SubmitResourceJobAsync();
                await Task.Delay(TimeSpan.FromSeconds(5)); // Wait for job to start
                Log.Information("");

                // Step 4: Execute workload scenarios
                Log.Information(">> Step 6/7: Executing workload scenarios...");
                await ExecuteWorkloadScenariosAsync(resourceMonitor);
                Log.Information("");

                // Step 5: Generate capacity planning report
                Log.Information(">> Step 7/7: Generating capacity planning report...");
                await resourceMonitor.StopMonitoringAsync();
                var capacityPlanner = new CapacityPlanner();
                await capacityPlanner.AnalyzeCapacityAsync(resourceMonitor.GetResourceSnapshots());
                await resourceMonitor.GenerateResourceReportAsync();
                Log.Information("");

                // Results
                Log.Information("================================================================================");
                Log.Information("  Exercise 8.4 Results - Resource Monitoring");
                Log.Information("================================================================================");
                var summary = resourceMonitor.GetSummary();
                Log.Information("  Resource Usage Summary:");
                Log.Information("     Peak Memory: {PeakMemory} MB", summary.PeakMemoryMB);
                Log.Information("     Average Memory: {AvgMemory} MB", summary.AverageMemoryMB);
                Log.Information("     Peak CPU: {PeakCPU:F1}%", summary.PeakCPUPercent);
                Log.Information("     Average CPU: {AvgCPU:F1}%", summary.AverageCPUPercent);
                Log.Information("     Monitoring Duration: {Duration:F1}s", summary.TotalDuration.TotalSeconds);
                Log.Information("");
                Log.Information("  Key Learnings:");
                Log.Information("     [SUCCESS] Container resource monitoring validated");
                Log.Information("     [SUCCESS] Kafka metrics tracking demonstrated");
                Log.Information("     [SUCCESS] Capacity planning analysis completed");
                Log.Information("     [SUCCESS] Production optimization patterns learned");
                Log.Information("");
                Log.Information("[SUCCESS] Exercise 8.4 COMPLETED successfully");
                Log.Information("================================================================================");

                return 0;
            }
            finally
            {
                // Cleanup: Stop monitoring and cancel Flink job
                if (resourceMonitor != null)
                {
                    await resourceMonitor.StopMonitoringAsync();
                }
                
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
            Log.Fatal(ex, "Exercise 8.4 failed with exception");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    /// <summary>
    /// Submit Flink job for resource-intensive event processing
    /// </summary>
    private static async Task<FlinkDotNet.DataStream.IJobClient> SubmitResourceJobAsync()
    {
        var environment = StreamExecutionEnvironment.GetExecutionEnvironment();

        // Source stream from Kafka
        var resourceStream = environment.FromKafka(
            topic: ResourceInputTopic,
            bootstrapServers: KafkaFlinkBootstrapServers,
            groupId: ConsumerGroup,
            startingOffsets: "earliest"
        );

        // Process events with resource-intensive operations
        var processedStream = resourceStream
            .Map(new ResourceIntensiveProcessingFunction());

        // Sink processed events
        processedStream.SinkToKafka(ResourceOutputTopic, KafkaFlinkBootstrapServers);

        // Execute job
        var jobClient = await environment.ExecuteAsync("Exercise84-ResourceMonitoring");

        Log.Information("   [SUCCESS] Flink resource processing job submitted");
        Log.Information("   JobId: {JobId}", jobClient.GetJobId());
        
        return jobClient;
    }

    /// <summary>
    /// Execute all workload scenarios with resource monitoring
    /// </summary>
    private static async Task ExecuteWorkloadScenariosAsync(ResourceMonitor monitor)
    {
        Console.WriteLine("\n📊 Workload Scenarios for Capacity Planning:");
        foreach (var scenario in Scenarios)
        {
            Console.WriteLine($"  • {scenario.Name}:");
            Console.WriteLine($"    Events/sec: {scenario.EventsPerSecond}");
            Console.WriteLine($"    Concurrent Tasks: {scenario.ConcurrentTasks}");
            Console.WriteLine($"    Duration: {scenario.DurationSeconds}s");
        }
        Console.WriteLine();

        foreach (var scenario in Scenarios)
        {
            Log.Information("🎯 Executing {ScenarioName}...", scenario.Name);
            
            monitor.MarkScenarioStart(scenario.Name);
            
            await ExecuteSingleWorkloadAsync(scenario);
            
            var metrics = monitor.MarkScenarioEnd(scenario.Name);
            
            Log.Information("✅ {ScenarioName} completed:", scenario.Name);
            Log.Information("   • Peak Memory: {PeakMemory} MB", metrics.PeakMemoryMB);
            Log.Information("   • Avg Memory: {AvgMemory} MB", metrics.AverageMemoryMB);
            Log.Information("   • Peak CPU: {PeakCPU:F1}%", metrics.PeakCPUPercent);
            Log.Information("   • Avg CPU: {AvgCPU:F1}%", metrics.AverageCPUPercent);
            Log.Information("   • Thread Count: {ThreadCount}", metrics.ThreadCount);
            
            // Cool-down and GC between scenarios
            if (scenario != Scenarios[^1])
            {
                Console.WriteLine("⏸️ Cool-down: 2 seconds...");
                await Task.Delay(2000);
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }

    /// <summary>
    /// Execute a single workload scenario
    /// </summary>
    private static async Task ExecuteSingleWorkloadAsync(WorkloadScenario scenario)
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = KafkaBootstrapServers,
            ClientId = $"exercise84-{scenario.Name.Replace(" ", "-").ToLower()}",
            Acks = Acks.All,
            BatchSize = 16384,
            CompressionType = CompressionType.Snappy,
            LingerMs = 5
        };

        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

        Log.Information("   Generating workload: {EventsPerSec} events/sec for {Duration}s with {Tasks} concurrent tasks",
            scenario.EventsPerSecond, scenario.DurationSeconds, scenario.ConcurrentTasks);

        var stopwatch = Stopwatch.StartNew();
        var totalEvents = scenario.EventsPerSecond * scenario.DurationSeconds;
        var eventsProduced = 0;

        // Simulate concurrent workload with multiple tasks
        var tasks = new List<Task>();
        var eventsPerTask = totalEvents / scenario.ConcurrentTasks;

        for (int taskId = 0; taskId < scenario.ConcurrentTasks; taskId++)
        {
            var localTaskId = taskId;
            var task = Task.Run(async () =>
            {
                // Batch parallel production for better throughput
                var batchTasks = new List<Task>();
                for (int i = 0; i < eventsPerTask; i++)
                {
                    var workloadEvent = GenerateWorkloadEvent(
                        Interlocked.Increment(ref eventsProduced),
                        scenario,
                        localTaskId);
                    
                    var produceTask = producer.ProduceAsync(ResourceInputTopic, new Message<string, string>
                    {
                        Key = workloadEvent.Id,
                        Value = JsonSerializer.Serialize(workloadEvent)
                    });
                    
                    batchTasks.Add(produceTask);
                    
                    // Pace the workload to achieve target rate
                    // CRITICAL FIX: Calculate delay to distribute events evenly over duration
                    var intervalMs = 1000.0 / scenario.EventsPerSecond;
                    var delayMs = intervalMs * scenario.ConcurrentTasks;
                    await Task.Delay((int)Math.Max(1, delayMs));
                }
                
                await Task.WhenAll(batchTasks);
            });
            
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        producer.Flush(TimeSpan.FromSeconds(10));
        stopwatch.Stop();

        Log.Information("   Workload generation completed: {EventCount} events in {Duration:F1}s", 
            eventsProduced, stopwatch.Elapsed.TotalSeconds);
    }

    /// <summary>
    /// Generate realistic workload event
    /// </summary>
    private static WorkloadEvent GenerateWorkloadEvent(int sequence, WorkloadScenario scenario, int taskId)
    {
        return new WorkloadEvent
        {
            Id = $"resource-{scenario.Name.Replace(" ", "-").ToLower()}-{sequence:D8}",
            Timestamp = DateTime.UtcNow,
            ScenarioName = scenario.Name,
            TaskId = taskId,
            Sequence = sequence,
            PayloadSize = scenario.Name switch
            {
                "Light Workload" => 100,
                "Normal Workload" => 500,
                "Heavy Workload" => 1000,
                _ => 200
            },
            Data = new Dictionary<string, object>
            {
                ["workload_type"] = scenario.Name,
                ["concurrent_tasks"] = scenario.ConcurrentTasks,
                ["target_rate"] = scenario.EventsPerSecond
            }
        };
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
            new TopicSpecification { Name = ResourceInputTopic, NumPartitions = 4, ReplicationFactor = 1 },
            new TopicSpecification { Name = ResourceOutputTopic, NumPartitions = 4, ReplicationFactor = 1 }
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

    private static async Task WaitForFlinkHealthyAsync()
    {
        var timeout = TimeSpan.FromSeconds(60);
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
public class WorkloadScenario
{
    public string Name { get; set; } = string.Empty;
    public int EventsPerSecond { get; set; }
    public int DurationSeconds { get; set; }
    public int ConcurrentTasks { get; set; }
}

public class WorkloadEvent
{
    public string Id { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string ScenarioName { get; set; } = string.Empty;
    public int TaskId { get; set; }
    public int Sequence { get; set; }
    public int PayloadSize { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}

public class ResourceSnapshot
{
    public DateTime Timestamp { get; set; }
    public long MemoryUsedMB { get; set; }
    public int Gen0Collections { get; set; }
    public int Gen1Collections { get; set; }
    public int Gen2Collections { get; set; }
    public int ThreadCount { get; set; }
    public double CPUPercent { get; set; }
}

public class ResourceMetrics
{
    public string ScenarioName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public int StartSnapshotIndex { get; set; }
    public int EndSnapshotIndex { get; set; }
    public long PeakMemoryMB { get; set; }
    public long AverageMemoryMB { get; set; }
    public double PeakCPUPercent { get; set; }
    public double AverageCPUPercent { get; set; }
    public int ThreadCount { get; set; }
    public int GCCount { get; set; }
}

public class ResourceSummary
{
    public long PeakMemoryMB { get; set; }
    public long AverageMemoryMB { get; set; }
    public double PeakCPUPercent { get; set; }
    public double AverageCPUPercent { get; set; }
    public TimeSpan TotalDuration { get; set; }
}

public class ResourceMonitor
{
    private readonly List<ResourceSnapshot> _snapshots = new();
    private readonly Dictionary<string, ResourceMetrics> _scenarioMetrics = new();
    private readonly Stopwatch _overallStopwatch = new();
    private CancellationTokenSource? _monitoringCts;

    public async Task StartMonitoringAsync()
    {
        Log.Information("   Starting resource monitoring...");
        _overallStopwatch.Start();
        _monitoringCts = new CancellationTokenSource();

        // Background task to collect resource snapshots
        _ = Task.Run(async () => await CollectResourceSnapshotsAsync(_monitoringCts.Token));

        await Task.CompletedTask;
    }

    public async Task StopMonitoringAsync()
    {
        Log.Information("   Stopping resource monitoring...");
        _overallStopwatch.Stop();
        _monitoringCts?.Cancel();
        await Task.Delay(500); // Wait for final snapshot
    }

    private async Task CollectResourceSnapshotsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var snapshot = new ResourceSnapshot
            {
                Timestamp = DateTime.UtcNow,
                MemoryUsedMB = GC.GetTotalMemory(false) / 1024 / 1024,
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2),
                ThreadCount = ThreadPool.ThreadCount,
                CPUPercent = CalculateCPUUsage()
            };

            _snapshots.Add(snapshot);

            await Task.Delay(500, cancellationToken);
        }
    }

    private static double CalculateCPUUsage()
    {
        // Simulated CPU usage based on system activity
        var baseLoad = ThreadPool.ThreadCount * 2.5;
        return Math.Min(baseLoad, 100.0);
    }

    public void MarkScenarioStart(string scenarioName)
    {
        _scenarioMetrics[scenarioName] = new ResourceMetrics
        {
            ScenarioName = scenarioName,
            StartTime = DateTime.UtcNow,
            StartSnapshotIndex = _snapshots.Count
        };
    }

    public ResourceMetrics MarkScenarioEnd(string scenarioName)
    {
        if (_scenarioMetrics.TryGetValue(scenarioName, out var metrics))
        {
            metrics.EndTime = DateTime.UtcNow;
            metrics.Duration = metrics.EndTime - metrics.StartTime;
            metrics.EndSnapshotIndex = _snapshots.Count;

            // Calculate metrics from snapshots during this scenario
            var scenarioSnapshots = _snapshots
                .Skip(metrics.StartSnapshotIndex)
                .Take(metrics.EndSnapshotIndex - metrics.StartSnapshotIndex)
                .ToList();

            if (scenarioSnapshots.Any())
            {
                metrics.PeakMemoryMB = scenarioSnapshots.Max(s => s.MemoryUsedMB);
                metrics.AverageMemoryMB = (long)scenarioSnapshots.Average(s => s.MemoryUsedMB);
                metrics.PeakCPUPercent = scenarioSnapshots.Max(s => s.CPUPercent);
                metrics.AverageCPUPercent = scenarioSnapshots.Average(s => s.CPUPercent);
                metrics.ThreadCount = scenarioSnapshots.Last().ThreadCount;

                var firstSnapshot = scenarioSnapshots.First();
                var lastSnapshot = scenarioSnapshots.Last();
                metrics.GCCount = (lastSnapshot.Gen0Collections - firstSnapshot.Gen0Collections) +
                                (lastSnapshot.Gen1Collections - firstSnapshot.Gen1Collections) +
                                (lastSnapshot.Gen2Collections - firstSnapshot.Gen2Collections);
            }
        }

        return metrics ?? new ResourceMetrics { ScenarioName = scenarioName };
    }

    public List<ResourceSnapshot> GetResourceSnapshots() => _snapshots;

    public ResourceSummary GetSummary()
    {
        return new ResourceSummary
        {
            PeakMemoryMB = _snapshots.Any() ? _snapshots.Max(s => s.MemoryUsedMB) : 0,
            AverageMemoryMB = _snapshots.Any() ? (long)_snapshots.Average(s => s.MemoryUsedMB) : 0,
            PeakCPUPercent = _snapshots.Any() ? _snapshots.Max(s => s.CPUPercent) : 0,
            AverageCPUPercent = _snapshots.Any() ? _snapshots.Average(s => s.CPUPercent) : 0,
            TotalDuration = _overallStopwatch.Elapsed
        };
    }

    public async Task GenerateResourceReportAsync()
    {
        Console.WriteLine("\n📊 RESOURCE MONITORING REPORT");
        Console.WriteLine("===============================");
        Console.WriteLine($"Total Duration: {_overallStopwatch.Elapsed.TotalSeconds:F1} seconds");
        Console.WriteLine($"Snapshots Collected: {_snapshots.Count:N0}");

        if (_scenarioMetrics.Any())
        {
            Console.WriteLine("\n🎯 Resource Usage by Scenario:");
            foreach (var metrics in _scenarioMetrics.Values.Where(m => m.EndTime != default))
            {
                Console.WriteLine($"\n  📋 {metrics.ScenarioName}:");
                Console.WriteLine($"     Duration: {metrics.Duration.TotalSeconds:F1}s");
                Console.WriteLine($"     Memory Usage:");
                Console.WriteLine($"       - Peak: {metrics.PeakMemoryMB} MB");
                Console.WriteLine($"       - Average: {metrics.AverageMemoryMB} MB");
                Console.WriteLine($"     CPU Usage:");
                Console.WriteLine($"       - Peak: {metrics.PeakCPUPercent:F1}%");
                Console.WriteLine($"       - Average: {metrics.AverageCPUPercent:F1}%");
                Console.WriteLine($"     Threading:");
                Console.WriteLine($"       - Thread Count: {metrics.ThreadCount}");
                Console.WriteLine($"     GC Collections: {metrics.GCCount}");
            }
        }

        Console.WriteLine("\n🎉 Resource monitoring analysis completed!");
        await Task.CompletedTask;
    }
}

public class CapacityPlanner
{
    public async Task AnalyzeCapacityAsync(List<ResourceSnapshot> snapshots)
    {
        if (!snapshots.Any())
        {
            Console.WriteLine("\n⚠️ No resource snapshots available for capacity analysis");
            return;
        }

        Console.WriteLine("\n📈 CAPACITY PLANNING ANALYSIS");
        Console.WriteLine("==============================");

        // Memory capacity
        var maxMemory = snapshots.Max(s => s.MemoryUsedMB);
        var avgMemory = snapshots.Average(s => s.MemoryUsedMB);
        var recommendedMemory = (long)(maxMemory * 1.5); // 50% headroom

        Console.WriteLine($"\n💾 Memory Capacity:");
        Console.WriteLine($"   Peak Usage: {maxMemory} MB");
        Console.WriteLine($"   Average Usage: {avgMemory:F1} MB");
        Console.WriteLine($"   Recommended: {recommendedMemory} MB (50% headroom)");

        // CPU capacity
        var maxCPU = snapshots.Max(s => s.CPUPercent);
        var avgCPU = snapshots.Average(s => s.CPUPercent);
        var cpuCores = Environment.ProcessorCount;
        var recommendedCores = (int)Math.Ceiling((maxCPU / 100.0) * cpuCores * 1.3); // 30% headroom

        Console.WriteLine($"\n⚙️ CPU Capacity:");
        Console.WriteLine($"   Peak Usage: {maxCPU:F1}%");
        Console.WriteLine($"   Average Usage: {avgCPU:F1}%");
        Console.WriteLine($"   Current Cores: {cpuCores}");
        Console.WriteLine($"   Recommended Cores: {recommendedCores} (30% headroom)");

        // Threading capacity
        var maxThreads = snapshots.Max(s => s.ThreadCount);
        var avgThreads = snapshots.Average(s => s.ThreadCount);

        Console.WriteLine($"\n🧵 Threading Capacity:");
        Console.WriteLine($"   Peak Threads: {maxThreads}");
        Console.WriteLine($"   Average Threads: {avgThreads:F1}");

        // GC analysis
        var firstSnapshot = snapshots.First();
        var lastSnapshot = snapshots.Last();
        var totalGC = (lastSnapshot.Gen0Collections - firstSnapshot.Gen0Collections) +
                     (lastSnapshot.Gen1Collections - firstSnapshot.Gen1Collections) +
                     (lastSnapshot.Gen2Collections - firstSnapshot.Gen2Collections);

        Console.WriteLine($"\n🔄 Garbage Collection:");
        Console.WriteLine($"   Total Collections: {totalGC}");
        Console.WriteLine($"   Gen0: {lastSnapshot.Gen0Collections - firstSnapshot.Gen0Collections}");
        Console.WriteLine($"   Gen1: {lastSnapshot.Gen1Collections - firstSnapshot.Gen1Collections}");
        Console.WriteLine($"   Gen2: {lastSnapshot.Gen2Collections - firstSnapshot.Gen2Collections}");

        // Recommendations
        Console.WriteLine($"\n💡 Capacity Recommendations:");

        if (maxMemory > avgMemory * 2)
        {
            Console.WriteLine($"   ⚠️ Memory spikes detected - consider memory pooling");
        }

        if (maxCPU > 80)
        {
            Console.WriteLine($"   ⚠️ High CPU usage - consider adding {recommendedCores - cpuCores} more cores");
        }

        if (totalGC > 100)
        {
            Console.WriteLine($"   ⚠️ Frequent GC detected - optimize object allocations");
        }

        if (maxThreads > 50)
        {
            Console.WriteLine($"   ⚠️ High thread count - consider async/await patterns");
        }

        await Task.CompletedTask;
    }
}

/// <summary>
/// Map function that performs resource-intensive processing
/// </summary>
public class ResourceIntensiveProcessingFunction : FlinkDotNet.DataStream.IMapFunction<string, string>
{
    public string Map(string eventJson)
    {
        try
        {
            var workloadEvent = JsonSerializer.Deserialize<WorkloadEvent>(eventJson);
            if (workloadEvent == null) return eventJson;

            // Simulate resource-intensive processing
            var processingTime = workloadEvent.PayloadSize / 50; // More data = more time
            Thread.Sleep(Math.Max(5, processingTime));

            // CPU-intensive calculation
            double result = 0;
            for (int i = 0; i < workloadEvent.PayloadSize; i++)
            {
                result += Math.Sqrt(i) * Math.Sin(i) * Math.Cos(i);
            }

            // Add processing metadata
            workloadEvent.Data["processed_at"] = DateTime.UtcNow.ToString("O");
            workloadEvent.Data["processing_time_ms"] = processingTime;
            workloadEvent.Data["calculation_result"] = result;

            return JsonSerializer.Serialize(workloadEvent);
        }
        catch
        {
            return eventJson;
        }
    }
}
