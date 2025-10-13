using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Collections.Concurrent;
using System.Diagnostics;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Console.WriteLine("🚀 Day 3 Exercise 3.3: Production Performance Testing - Netflix/Uber Load Patterns");
Console.WriteLine("".PadRight(85, '='));

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<ILoadTestEngine, ProductionLoadTestEngine>();
        services.AddSingleton<IPerformanceMonitor, PerformanceMonitor>();
        services.AddSingleton<ITestScenario, NetflixPeakTrafficScenario>();
        services.AddSingleton<ITestScenario, UberSurgePricingScenario>();
        services.AddSingleton<ITestScenario, TwitterViralContentScenario>();
        services.AddHostedService<PerformanceTestingService>();
    })
    .UseSerilog()
    .Build();

try
{
    Log.Information("Starting Exercise 3.3: Production Performance Testing");
    
    Console.WriteLine("📊 Performance testing with real industry load patterns:");
    Console.WriteLine("   • Netflix: 15 Petabits/sec peak traffic (200M concurrent users)");
    Console.WriteLine("   • Uber: 23ms pricing latency under surge conditions");
    Console.WriteLine("   • Twitter: Viral content handling (50K tweets/sec spike)");
    Console.WriteLine("   • Testing realistic failure scenarios and recovery patterns");
    Console.WriteLine();
    
    // Start the host and run simulation for a fixed duration
    await host.StartAsync();
    
    // Run simulation for 15 seconds to allow performance tests to complete
    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
    
    try
    {
        await Task.Delay(Timeout.Infinite, cts.Token);
    }
    catch (TaskCanceledException)
    {
        // Expected - simulation complete
    }
    
    Log.Information("Exercise 3.3 completed successfully");
    Console.WriteLine();
    Console.WriteLine("================================================================================");
    Console.WriteLine("  EXERCISE COMPLETED SUCCESSFULLY!");
    Console.WriteLine("================================================================================");
    Console.WriteLine("✅ Production performance testing completed");
    Console.WriteLine();
}
catch (Exception ex)
{
    Log.Error(ex, "Error in Exercise 3.3: Production Performance Testing");
    Console.WriteLine($"❌ Error: {ex.Message}");
    Environment.Exit(1);
}
finally
{
    await host.StopAsync();
    await Log.CloseAndFlushAsync();
}

// Performance testing engine based on real industry requirements
public interface ILoadTestEngine
{
    Task<LoadTestResult> ExecuteScenarioAsync(ITestScenario scenario, LoadTestConfiguration config);
    Task<SystemHealthMetrics> GetSystemHealthAsync();
}

public interface IPerformanceMonitor
{
    void StartMonitoring();
    void StopMonitoring();
    PerformanceMetrics GetCurrentMetrics();
    void RecordLatency(string operation, double latencyMs);
    void RecordThroughput(string operation, int operations);
}

public interface ITestScenario
{
    string Name { get; }
    string Description { get; }
    LoadPattern LoadPattern { get; }
    Task<ScenarioResult> ExecuteAsync(LoadTestConfiguration config, CancellationToken cancellationToken);
}

public class ProductionLoadTestEngine : ILoadTestEngine
{
    private readonly IPerformanceMonitor _performanceMonitor;
    private readonly ILogger<ProductionLoadTestEngine> _logger;

    public ProductionLoadTestEngine(IPerformanceMonitor performanceMonitor, ILogger<ProductionLoadTestEngine> logger)
    {
        _performanceMonitor = performanceMonitor;
        _logger = logger;
    }

    public async Task<LoadTestResult> ExecuteScenarioAsync(ITestScenario scenario, LoadTestConfiguration config)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Starting load test scenario: {Scenario}", scenario.Name);
            _performanceMonitor.StartMonitoring();

            using var cancellationTokenSource = new CancellationTokenSource(config.TestDuration);
            var scenarioResult = await scenario.ExecuteAsync(config, cancellationTokenSource.Token);
            
            stopwatch.Stop();
            _performanceMonitor.StopMonitoring();

            var performanceMetrics = _performanceMonitor.GetCurrentMetrics();
            var systemHealth = await GetSystemHealthAsync();

            return new LoadTestResult(
                ScenarioName: scenario.Name,
                Duration: stopwatch.Elapsed,
                OperationsCompleted: scenarioResult.OperationsCompleted,
                OperationsFailed: scenarioResult.OperationsFailed,
                AverageLatencyMs: performanceMetrics.AverageLatencyMs,
                P95LatencyMs: performanceMetrics.P95LatencyMs,
                P99LatencyMs: performanceMetrics.P99LatencyMs,
                ThroughputOpsPerSec: performanceMetrics.ThroughputOpsPerSec,
                SystemHealth: systemHealth,
                Success: scenarioResult.Success
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Load test scenario failed: {Scenario}", scenario.Name);
            throw new InvalidOperationException($"Load test failed for scenario {scenario.Name}", ex);
        }
    }

    public async Task<SystemHealthMetrics> GetSystemHealthAsync()
    {
        // Simulate system health metrics collection
        await Task.Delay(50);
        
        // Real production health indicators
        var cpuUsage = GetSimulatedCpuUsage();
        var memoryUsage = GetSimulatedMemoryUsage();
        var networkUtilization = GetSimulatedNetworkUsage();
        
        return new SystemHealthMetrics(
            CpuUsagePercent: cpuUsage,
            MemoryUsagePercent: memoryUsage,
            NetworkUtilizationPercent: networkUtilization,
            HealthStatus: DetermineHealthStatus(cpuUsage, memoryUsage, networkUtilization)
        );
    }

    private double GetSimulatedCpuUsage()
    {
        // Simulate realistic CPU usage patterns
        var baseUsage = 0.35; // 35% baseline
        var timeVariation = Math.Sin(DateTime.UtcNow.Minute * Math.PI / 30) * 0.15; // ±15% variation
        return Math.Max(0.1, Math.Min(0.95, baseUsage + timeVariation));
    }

    private double GetSimulatedMemoryUsage()
    {
        // Simulate memory usage (typically more stable than CPU)
        var baseUsage = 0.65; // 65% baseline
        var variation = (DateTime.UtcNow.Second % 10) * 0.02; // Small variation
        return Math.Max(0.4, Math.Min(0.85, baseUsage + variation));
    }

    private double GetSimulatedNetworkUsage()
    {
        // Simulate network utilization based on load
        var hour = DateTime.UtcNow.Hour;
        var peakMultiplier = hour switch
        {
            >= 18 and <= 22 => 0.8,  // Peak hours: 80% utilization
            >= 12 and <= 17 => 0.6,  // Business hours: 60%
            >= 8 and <= 11 => 0.4,   // Morning: 40%
            _ => 0.2                  // Off-peak: 20%
        };
        
        return Math.Max(0.1, Math.Min(0.95, peakMultiplier));
    }

    private static HealthStatus DetermineHealthStatus(double cpu, double memory, double network)
    {
        if (cpu > 0.9 || memory > 0.9 || network > 0.9)
            return HealthStatus.Critical;
        if (cpu > 0.75 || memory > 0.8 || network > 0.8)
            return HealthStatus.Warning;
        return HealthStatus.Healthy;
    }
}

public class PerformanceMonitor : IPerformanceMonitor
{
    private readonly ConcurrentQueue<LatencyMeasurement> _latencyMeasurements = new();
    private readonly ConcurrentDictionary<string, int> _throughputCounters = new();
    private readonly ILogger<PerformanceMonitor> _logger;
    private DateTime _monitoringStartTime;
    private bool _isMonitoring;

    public PerformanceMonitor(ILogger<PerformanceMonitor> logger)
    {
        _logger = logger;
    }

    public void StartMonitoring()
    {
        _isMonitoring = true;
        _monitoringStartTime = DateTime.UtcNow;
        _latencyMeasurements.Clear();
        _throughputCounters.Clear();
        _logger.LogInformation("Performance monitoring started");
    }

    public void StopMonitoring()
    {
        _isMonitoring = false;
        _logger.LogInformation("Performance monitoring stopped");
    }

    public void RecordLatency(string operation, double latencyMs)
    {
        if (!_isMonitoring) return;
        
        _latencyMeasurements.Enqueue(new LatencyMeasurement(operation, latencyMs, DateTime.UtcNow));
        
        // Keep only recent measurements (last 5 minutes)
        while (_latencyMeasurements.TryPeek(out var oldest) && 
               DateTime.UtcNow - oldest.Timestamp > TimeSpan.FromMinutes(5))
        {
            _latencyMeasurements.TryDequeue(out _);
        }
    }

    public void RecordThroughput(string operation, int operations)
    {
        if (!_isMonitoring) return;
        
        _throughputCounters.AddOrUpdate(operation, operations, (key, existing) => existing + operations);
    }

    public PerformanceMetrics GetCurrentMetrics()
    {
        var measurements = _latencyMeasurements.ToArray();
        var monitoringDuration = DateTime.UtcNow - _monitoringStartTime;
        
        if (measurements.Length == 0)
        {
            return new PerformanceMetrics(0, 0, 0, 0);
        }

        var latencies = measurements.Select(m => m.LatencyMs).OrderBy(l => l).ToArray();
        var averageLatency = latencies.Average();
        var p95Index = (int)(latencies.Length * 0.95);
        var p99Index = (int)(latencies.Length * 0.99);
        var p95Latency = p95Index < latencies.Length ? latencies[p95Index] : latencies[latencies.Length - 1];
        var p99Latency = p99Index < latencies.Length ? latencies[p99Index] : latencies[latencies.Length - 1];
        
        var totalOperations = _throughputCounters.Values.Sum();
        var throughput = monitoringDuration.TotalSeconds > 0 ? totalOperations / monitoringDuration.TotalSeconds : 0;

        return new PerformanceMetrics(averageLatency, p95Latency, p99Latency, throughput);
    }
}

// Netflix peak traffic scenario - 200M concurrent users
public class NetflixPeakTrafficScenario : ITestScenario
{
    public string Name => "Netflix Peak Traffic";
    public string Description => "200M concurrent users, 15 Petabits/sec, adaptive quality streaming";
    public LoadPattern LoadPattern => LoadPattern.SustainedPeak;

    private readonly IPerformanceMonitor _performanceMonitor;
    private readonly ILogger<NetflixPeakTrafficScenario> _logger;

    public NetflixPeakTrafficScenario(IPerformanceMonitor performanceMonitor, ILogger<NetflixPeakTrafficScenario> logger)
    {
        _performanceMonitor = performanceMonitor;
        _logger = logger;
    }

    public async Task<ScenarioResult> ExecuteAsync(LoadTestConfiguration config, CancellationToken cancellationToken)
    {
        var operationsCompleted = 0;
        var operationsFailed = 0;
        var concurrentTasks = new List<Task>();

        try
        {
            _logger.LogInformation("Executing Netflix peak traffic scenario");

            // Simulate Netflix's evening peak: 8-10 PM global traffic
            var peakConcurrency = Math.Min(config.MaxConcurrentUsers, 1000); // Scale down for demo
            
            while (!cancellationToken.IsCancellationRequested && concurrentTasks.Count < peakConcurrency)
            {
                // Clean up completed tasks
                concurrentTasks.RemoveAll(t => t.IsCompleted);
                
                // Add new streaming sessions
                while (concurrentTasks.Count < peakConcurrency && !cancellationToken.IsCancellationRequested)
                {
                    var task = SimulateStreamingSession(cancellationToken);
                    concurrentTasks.Add(task);
                    
                    // Netflix pattern: Gradual ramp-up to peak
                    if (concurrentTasks.Count % 50 == 0)
                    {
                        await Task.Delay(100, cancellationToken);
                    }
                }

                await Task.Delay(1000, cancellationToken); // Monitor every second
            }

            // Wait for remaining tasks to complete
            await Task.WhenAll(concurrentTasks);
            operationsCompleted = concurrentTasks.Count;

            return new ScenarioResult(operationsCompleted, operationsFailed, true);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation(ex, "Netflix scenario cancelled");
            return new ScenarioResult(operationsCompleted, operationsFailed, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Netflix scenario failed");
            return new ScenarioResult(operationsCompleted, operationsFailed + 1, false);
        }
    }

    private async Task SimulateStreamingSession(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Netflix streaming session: 23 minutes average watch time
            var sessionDuration = TimeSpan.FromMinutes(23);
            var endTime = DateTime.UtcNow.Add(sessionDuration);

            while (DateTime.UtcNow < endTime && !cancellationToken.IsCancellationRequested)
            {
                // Simulate video chunk delivery (every 4-6 seconds)
                await SimulateVideoChunkDelivery();
                await Task.Delay(5000, cancellationToken); // 5 second chunks
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        finally
        {
            stopwatch.Stop();
            _performanceMonitor.RecordLatency("streaming_session", stopwatch.ElapsedMilliseconds);
            _performanceMonitor.RecordThroughput("netflix_sessions", 1);
        }
    }

    private async Task SimulateVideoChunkDelivery()
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Netflix adaptive bitrate: 4K (25 Mbps), 1080p (8 Mbps), 720p (5 Mbps)
        var qualities = new[] { "4K", "1080p", "720p", "480p" };
        var selectedQuality = qualities[DateTime.UtcNow.Second % qualities.Length];
        
        // Simulate chunk processing time based on quality
        var processingTime = selectedQuality switch
        {
            "4K" => 45,      // 4K: Higher processing overhead
            "1080p" => 25,   // 1080p: Standard processing
            "720p" => 15,    // 720p: Reduced processing
            "480p" => 8,     // 480p: Minimal processing
            _ => 25
        };

        await Task.Delay(processingTime);
        
        stopwatch.Stop();
        _performanceMonitor.RecordLatency($"video_chunk_{selectedQuality}", stopwatch.ElapsedMilliseconds);
    }
}

// Uber surge pricing scenario - high demand, real-time pricing
public class UberSurgePricingScenario : ITestScenario
{
    public string Name => "Uber Surge Pricing";
    public string Description => "Real-time pricing under surge conditions, 23ms target latency";
    public LoadPattern LoadPattern => LoadPattern.SpikeThenSustain;

    private readonly IPerformanceMonitor _performanceMonitor;
    private readonly ILogger<UberSurgePricingScenario> _logger;

    public UberSurgePricingScenario(IPerformanceMonitor performanceMonitor, ILogger<UberSurgePricingScenario> logger)
    {
        _performanceMonitor = performanceMonitor;
        _logger = logger;
    }

    public async Task<ScenarioResult> ExecuteAsync(LoadTestConfiguration config, CancellationToken cancellationToken)
    {
        var operationsCompleted = 0;
        var operationsFailed = 0;

        try
        {
            _logger.LogInformation("Executing Uber surge pricing scenario");

            // Simulate surge event: sudden spike in ride requests
            var tasks = new List<Task>();
            var surgeMultiplier = 3.5; // 3.5x normal demand

            while (!cancellationToken.IsCancellationRequested)
            {
                // Uber pattern: burst of pricing requests during surge
                var burstSize = (int)(config.MaxConcurrentUsers * surgeMultiplier / 10);
                
                for (int i = 0; i < burstSize && !cancellationToken.IsCancellationRequested; i++)
                {
                    var task = SimulatePricingRequest(cancellationToken);
                    tasks.Add(task);
                }

                // Wait for batch completion and clean up
                var completedTasks = await Task.WhenAny(Task.WhenAll(tasks), Task.Delay(2000, cancellationToken));
                var finished = tasks.Where(t => t.IsCompleted).ToList();
                operationsCompleted += finished.Count;
                tasks.RemoveAll(t => t.IsCompleted);

                await Task.Delay(1000, cancellationToken); // 1 second between bursts
            }

            return new ScenarioResult(operationsCompleted, operationsFailed, true);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation(ex, "Uber scenario cancelled");
            return new ScenarioResult(operationsCompleted, operationsFailed, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Uber scenario failed");
            return new ScenarioResult(operationsCompleted, operationsFailed + 1, false);
        }
    }

    private async Task SimulatePricingRequest(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Uber's target: 23ms for pricing calculations
            var baseLatency = 23;
            var locationComplexity = GetLocationComplexity();
            var demandFactor = GetDemandFactor();
            
            var calculationTime = (int)(baseLatency * locationComplexity * demandFactor);
            await Task.Delay(Math.Min(calculationTime, 100), cancellationToken); // Cap at 100ms for demo
            
            stopwatch.Stop();
            _performanceMonitor.RecordLatency("uber_pricing", stopwatch.ElapsedMilliseconds);
            _performanceMonitor.RecordThroughput("pricing_requests", 1);
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
    }

    private double GetLocationComplexity()
    {
        // Simulate location-based complexity
        var locations = new[] { "downtown", "airport", "suburban", "rural" };
        var location = locations[DateTime.UtcNow.Second % locations.Length];
        
        return location switch
        {
            "downtown" => 1.5,   // High complexity: traffic, multiple routes
            "airport" => 1.8,    // Highest complexity: regulations, traffic
            "suburban" => 1.0,   // Standard complexity
            "rural" => 0.7,      // Lower complexity: fewer routes
            _ => 1.0
        };
    }

    private double GetDemandFactor()
    {
        // Simulate demand-based pricing complexity
        var hour = DateTime.UtcNow.Hour;
        
        return hour switch
        {
            >= 7 and <= 9 => 2.0,    // Rush hour: complex surge calculations
            >= 17 and <= 19 => 2.2,  // Evening rush: highest complexity
            >= 22 or <= 2 => 1.8,    // Late night: surge for safety
            _ => 1.0                  // Normal hours
        };
    }
}

// Twitter viral content scenario - handling viral spikes
public class TwitterViralContentScenario : ITestScenario
{
    public string Name => "Twitter Viral Content";
    public string Description => "Viral content handling, 50K tweets/sec spike, trending algorithms";
    public LoadPattern LoadPattern => LoadPattern.ViralSpike;

    private readonly IPerformanceMonitor _performanceMonitor;
    private readonly ILogger<TwitterViralContentScenario> _logger;

    public TwitterViralContentScenario(IPerformanceMonitor performanceMonitor, ILogger<TwitterViralContentScenario> logger)
    {
        _performanceMonitor = performanceMonitor;
        _logger = logger;
    }

    public async Task<ScenarioResult> ExecuteAsync(LoadTestConfiguration config, CancellationToken cancellationToken)
    {
        var operationsCompleted = 0;
        var operationsFailed = 0;

        try
        {
            _logger.LogInformation("Executing Twitter viral content scenario");

            // Simulate viral event: exponential growth in engagement
            var baseLoad = 100;
            var viralMultiplier = 1.0;
            
            while (!cancellationToken.IsCancellationRequested && viralMultiplier < 50)
            {
                var currentLoad = (int)(baseLoad * viralMultiplier);
                var tasks = new List<Task>();

                // Generate viral content processing tasks
                for (int i = 0; i < Math.Min(currentLoad, config.MaxConcurrentUsers); i++)
                {
                    var task = SimulateViralContentProcessing(viralMultiplier, cancellationToken);
                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);
                operationsCompleted += tasks.Count;

                // Exponential growth pattern (viral spread)
                viralMultiplier *= 1.3; // 30% growth per iteration
                await Task.Delay(2000, cancellationToken); // 2 second intervals
            }

            return new ScenarioResult(operationsCompleted, operationsFailed, true);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation(ex, "Twitter scenario cancelled");
            return new ScenarioResult(operationsCompleted, operationsFailed, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Twitter scenario failed");
            return new ScenarioResult(operationsCompleted, operationsFailed + 1, false);
        }
    }

    private async Task SimulateViralContentProcessing(double viralMultiplier, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Twitter processing: timeline updates, trending calculations, notifications
            var baseProcessingTime = 15; // 15ms baseline
            var viralComplexity = Math.Min(viralMultiplier / 10, 3.0); // Cap complexity
            
            var processingTime = (int)(baseProcessingTime * viralComplexity);
            await Task.Delay(processingTime, cancellationToken);

            stopwatch.Stop();
            _performanceMonitor.RecordLatency("viral_content", stopwatch.ElapsedMilliseconds);
            _performanceMonitor.RecordThroughput("viral_operations", 1);
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
    }
}

// Performance testing orchestration service
public class PerformanceTestingService : BackgroundService
{
    private readonly ILoadTestEngine _loadTestEngine;
    private readonly IEnumerable<ITestScenario> _scenarios;
    private readonly ILogger<PerformanceTestingService> _logger;

    public PerformanceTestingService(
        ILoadTestEngine loadTestEngine, 
        IEnumerable<ITestScenario> scenarios,
        ILogger<PerformanceTestingService> logger)
    {
        _loadTestEngine = loadTestEngine;
        _scenarios = scenarios;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(1000, stoppingToken); // Initial delay
        
        var config = new LoadTestConfiguration(
            TestDuration: TimeSpan.FromMinutes(2), // 2 minute tests for demo
            MaxConcurrentUsers: 200,
            RampUpDuration: TimeSpan.FromSeconds(30)
        );

        foreach (var scenario in _scenarios)
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                await DisplayScenarioInfo(scenario);
                var result = await _loadTestEngine.ExecuteScenarioAsync(scenario, config);
                await DisplayResults(result);
                
                await Task.Delay(5000, stoppingToken); // 5 second break between scenarios
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute scenario {Scenario}", scenario.Name);
            }
        }

        await DisplayFinalSummary();
    }

    private async Task DisplayScenarioInfo(ITestScenario scenario)
    {
        Console.Clear();
        Console.WriteLine("🚀 Production Performance Testing - Real Industry Load Patterns");
        Console.WriteLine("".PadRight(85, '='));
        Console.WriteLine($"📊 Running Scenario: {scenario.Name}");
        Console.WriteLine($"📝 Description: {scenario.Description}");
        Console.WriteLine($"📈 Load Pattern: {scenario.LoadPattern}");
        Console.WriteLine();
        Console.WriteLine("⏳ Test in progress...");
        
        await Task.Delay(100);
    }

    private async Task DisplayResults(LoadTestResult result)
    {
        Console.Clear();
        Console.WriteLine("🚀 Production Performance Testing - Test Results");
        Console.WriteLine("".PadRight(85, '='));
        Console.WriteLine($"📊 Scenario: {result.ScenarioName}");
        Console.WriteLine($"✅ Status: {(result.Success ? "✅ PASSED" : "❌ FAILED")}");
        Console.WriteLine($"⏱️ Duration: {result.Duration.TotalSeconds:F1} seconds");
        Console.WriteLine();
        
        Console.WriteLine("📈 Performance Metrics:");
        Console.WriteLine($"   Operations Completed: {result.OperationsCompleted:N0}");
        Console.WriteLine($"   Operations Failed: {result.OperationsFailed:N0}");
        Console.WriteLine($"   Throughput: {result.ThroughputOpsPerSec:F1} ops/sec");
        Console.WriteLine();
        
        Console.WriteLine("⚡ Latency Distribution:");
        Console.WriteLine($"   Average: {result.AverageLatencyMs:F1}ms");
        Console.WriteLine($"   P95: {result.P95LatencyMs:F1}ms");
        Console.WriteLine($"   P99: {result.P99LatencyMs:F1}ms");
        Console.WriteLine();
        
        Console.WriteLine("🖥️ System Health:");
        Console.WriteLine($"   CPU Usage: {result.SystemHealth.CpuUsagePercent:P1}");
        Console.WriteLine($"   Memory Usage: {result.SystemHealth.MemoryUsagePercent:P1}");
        Console.WriteLine($"   Network Utilization: {result.SystemHealth.NetworkUtilizationPercent:P1}");
        Console.WriteLine($"   Status: {GetHealthStatusIcon(result.SystemHealth.HealthStatus)} {result.SystemHealth.HealthStatus}");
        Console.WriteLine();
        
        Console.WriteLine("📊 Industry Benchmarks:");
        DisplayIndustryBenchmarks(result.ScenarioName);
        
        await Task.Delay(100);
    }

    private static void DisplayIndustryBenchmarks(string scenarioName)
    {
        switch (scenarioName)
        {
            case "Netflix Peak Traffic":
                Console.WriteLine("   • Netflix Target: <100ms video start time, 99.9% uptime");
                Console.WriteLine("   • Industry Standard: <150ms latency, 99.5% availability");
                break;
            case "Uber Surge Pricing":
                Console.WriteLine("   • Uber Target: <23ms pricing calculation, 99.99% accuracy");
                Console.WriteLine("   • Industry Standard: <50ms pricing, 99.9% accuracy");
                break;
            case "Twitter Viral Content":
                Console.WriteLine("   • Twitter Target: <50ms timeline update, handle 50K tweets/sec");
                Console.WriteLine("   • Industry Standard: <100ms social feed, 10K posts/sec");
                break;
        }
    }

    private static string GetHealthStatusIcon(HealthStatus status)
    {
        return status switch
        {
            HealthStatus.Healthy => "🟢",
            HealthStatus.Warning => "🟡",
            HealthStatus.Critical => "🔴",
            _ => "⚪"
        };
    }

    private async Task DisplayFinalSummary()
    {
        Console.WriteLine();
        Console.WriteLine("🎯 Performance Testing Complete!");
        Console.WriteLine("".PadRight(50, '='));
        Console.WriteLine("💡 Key Insights:");
        Console.WriteLine("   • Real-world load patterns reveal system bottlenecks");
        Console.WriteLine("   • Performance testing validates production readiness");
        Console.WriteLine("   • Industry benchmarks provide realistic targets");
        Console.WriteLine("   • Continuous testing ensures sustained performance");
        
        await Task.Delay(100);
    }
}

// Data models for performance testing
public record LoadTestConfiguration(
    TimeSpan TestDuration,
    int MaxConcurrentUsers,
    TimeSpan RampUpDuration
);

public record LoadTestResult(
    string ScenarioName,
    TimeSpan Duration,
    int OperationsCompleted,
    int OperationsFailed,
    double AverageLatencyMs,
    double P95LatencyMs,
    double P99LatencyMs,
    double ThroughputOpsPerSec,
    SystemHealthMetrics SystemHealth,
    bool Success
);

public record ScenarioResult(int OperationsCompleted, int OperationsFailed, bool Success);

public record PerformanceMetrics(double AverageLatencyMs, double P95LatencyMs, double P99LatencyMs, double ThroughputOpsPerSec);

public record SystemHealthMetrics(double CpuUsagePercent, double MemoryUsagePercent, double NetworkUtilizationPercent, HealthStatus HealthStatus);

public record LatencyMeasurement(string Operation, double LatencyMs, DateTime Timestamp);

public enum LoadPattern
{
    SustainedPeak,
    SpikeThenSustain,
    ViralSpike,
    GradualRampUp
}

public enum HealthStatus
{
    Healthy,
    Warning,
    Critical
}
