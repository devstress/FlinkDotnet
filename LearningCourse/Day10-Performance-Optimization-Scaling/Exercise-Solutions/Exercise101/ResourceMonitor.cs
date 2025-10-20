using System.Diagnostics;
using Serilog;

namespace Exercise101;

/// <summary>
/// Monitors system resource usage during performance testing
/// Uses System.Diagnostics.Process for real CPU/Memory metrics
/// </summary>
public class ResourceMonitor
{
    private readonly List<ResourceSnapshot> _snapshots = new();
    private readonly Stopwatch _overallStopwatch = new();
    private CancellationTokenSource? _monitoringCts;
    private Process? _currentProcess;
    private DateTime _lastCPUTime = DateTime.UtcNow;
    private TimeSpan _lastTotalProcessorTime;
    private string _currentScenario = string.Empty;

    public async Task StartMonitoringAsync()
    {
        Log.Information("   Starting resource monitoring...");
        _overallStopwatch.Start();
        _monitoringCts = new CancellationTokenSource();
        _currentProcess = Process.GetCurrentProcess();
        _lastTotalProcessorTime = _currentProcess.TotalProcessorTime;

        // Background task to collect resource snapshots every 500ms
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

    public void SetCurrentScenario(string scenario)
    {
        _currentScenario = scenario;
    }

    /// <summary>
    /// Collect resource snapshots in background
    /// </summary>
    private async Task CollectResourceSnapshotsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var snapshot = new ResourceSnapshot
                {
                    Timestamp = DateTime.UtcNow,
                    MemoryUsedMB = GC.GetTotalMemory(false) / 1024 / 1024,
                    CPUPercent = CalculateRealCPUUsage(),
                    ThreadCount = ThreadPool.ThreadCount,
                    Gen0Collections = GC.CollectionCount(0),
                    Gen1Collections = GC.CollectionCount(1),
                    Gen2Collections = GC.CollectionCount(2),
                    Scenario = _currentScenario
                };

                _snapshots.Add(snapshot);
                await Task.Delay(500, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error collecting resource snapshot");
            }
        }
    }

    /// <summary>
    /// Calculate real CPU usage using Process.TotalProcessorTime
    /// </summary>
    private double CalculateRealCPUUsage()
    {
        if (_currentProcess == null)
            return 0;

        try
        {
            var currentTime = DateTime.UtcNow;
            var currentTotalProcessorTime = _currentProcess.TotalProcessorTime;

            var cpuTimeDelta = (currentTotalProcessorTime - _lastTotalProcessorTime).TotalMilliseconds;
            var timeDelta = (currentTime - _lastCPUTime).TotalMilliseconds;

            var cpuPercent = (cpuTimeDelta / (timeDelta * Environment.ProcessorCount)) * 100.0;

            _lastCPUTime = currentTime;
            _lastTotalProcessorTime = currentTotalProcessorTime;

            return Math.Max(0, Math.Min(100, cpuPercent));
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Get resource snapshots for a specific scenario
    /// </summary>
    public List<ResourceSnapshot> GetScenarioSnapshots(string scenarioName)
    {
        return _snapshots.Where(s => s.Scenario == scenarioName).ToList();
    }

    /// <summary>
    /// Get all resource snapshots
    /// </summary>
    public List<ResourceSnapshot> GetAllSnapshots() => _snapshots;

    /// <summary>
    /// Calculate performance metrics for a scenario
    /// </summary>
    public PerformanceMetrics CalculateMetrics(
        string scenarioName,
        int parallelism,
        DateTime startTime,
        DateTime endTime,
        long eventsGenerated,
        List<ProcessedEvent> processedEvents)
    {
        var scenarioSnapshots = _snapshots
            .Where(s => s.Timestamp >= startTime && s.Timestamp <= endTime && s.Scenario == scenarioName)
            .ToList();

        var metrics = new PerformanceMetrics
        {
            Scenario = scenarioName,
            Parallelism = parallelism,
            StartTime = startTime,
            EndTime = endTime,
            Duration = endTime - startTime,
            EventsGenerated = eventsGenerated,
            EventsProcessed = processedEvents.Count
        };

        // Calculate throughput
        if (metrics.Duration.TotalSeconds > 0)
        {
            metrics.ThroughputEventsPerSec = metrics.EventsProcessed / metrics.Duration.TotalSeconds;
            metrics.SuccessRate = eventsGenerated > 0 ? (double)metrics.EventsProcessed / eventsGenerated : 0;
        }

        // Calculate latency metrics
        if (processedEvents.Any())
        {
            var latencies = processedEvents
                .Select(e => e.ProcessedTimestamp - e.OriginalTimestamp)
                .OrderBy(l => l)
                .ToList();

            metrics.AverageLatency = TimeSpan.FromMilliseconds(latencies.Average(l => l.TotalMilliseconds));
            metrics.MinLatency = latencies.First();
            metrics.MaxLatency = latencies.Last();
            metrics.P95Latency = latencies[(int)(latencies.Count * 0.95)];
            metrics.P99Latency = latencies[(int)(latencies.Count * 0.99)];
        }

        // Calculate resource metrics
        if (scenarioSnapshots.Any())
        {
            metrics.PeakMemoryMB = scenarioSnapshots.Max(s => s.MemoryUsedMB);
            metrics.AverageMemoryMB = (long)scenarioSnapshots.Average(s => s.MemoryUsedMB);
            metrics.PeakCPUPercent = scenarioSnapshots.Max(s => s.CPUPercent);
            metrics.AverageCPUPercent = scenarioSnapshots.Average(s => s.CPUPercent);
            metrics.ThreadCount = scenarioSnapshots.Last().ThreadCount;

            var firstSnapshot = scenarioSnapshots.First();
            var lastSnapshot = scenarioSnapshots.Last();
            metrics.Gen0Collections = lastSnapshot.Gen0Collections - firstSnapshot.Gen0Collections;
            metrics.Gen1Collections = lastSnapshot.Gen1Collections - firstSnapshot.Gen1Collections;
            metrics.Gen2Collections = lastSnapshot.Gen2Collections - firstSnapshot.Gen2Collections;
            metrics.TotalGCCollections = metrics.Gen0Collections + metrics.Gen1Collections + metrics.Gen2Collections;
        }

        return metrics;
    }

    /// <summary>
    /// Generate resource usage report
    /// </summary>
    public void GenerateResourceReport()
    {
        Console.WriteLine("\n📊 RESOURCE USAGE SUMMARY");
        Console.WriteLine("==========================");
        Console.WriteLine($"Monitoring Duration: {_overallStopwatch.Elapsed.TotalSeconds:F1} seconds");
        Console.WriteLine($"Snapshots Collected: {_snapshots.Count:N0}");

        if (_snapshots.Any())
        {
            var peakMemory = _snapshots.Max(s => s.MemoryUsedMB);
            var avgMemory = _snapshots.Average(s => s.MemoryUsedMB);
            var peakCPU = _snapshots.Max(s => s.CPUPercent);
            var avgCPU = _snapshots.Average(s => s.CPUPercent);

            Console.WriteLine($"\nOverall Resource Usage:");
            Console.WriteLine($"  Memory:");
            Console.WriteLine($"    - Peak: {peakMemory} MB");
            Console.WriteLine($"    - Average: {avgMemory:F1} MB");
            Console.WriteLine($"  CPU:");
            Console.WriteLine($"    - Peak: {peakCPU:F1}%");
            Console.WriteLine($"    - Average: {avgCPU:F1}%");
            Console.WriteLine($"  Threading:");
            Console.WriteLine($"    - Max Threads: {_snapshots.Max(s => s.ThreadCount)}");
        }
    }
}