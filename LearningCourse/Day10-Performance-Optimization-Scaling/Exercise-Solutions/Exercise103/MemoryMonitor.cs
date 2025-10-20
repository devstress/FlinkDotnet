using System.Diagnostics;

namespace Exercise103;

/// <summary>
/// Monitor memory usage and GC behavior during testing
/// </summary>
public class MemoryMonitor
{
    private readonly List<MemorySample> _samples = new();
    private readonly object _lock = new();
    private Timer? _samplingTimer;
    private readonly int _samplingIntervalMs;

    private class MemorySample
    {
        public DateTime Timestamp { get; set; }
        public long WorkingSetBytes { get; set; }
        public long GCTotalMemory { get; set; }
        public long Gen0Collections { get; set; }
        public long Gen1Collections { get; set; }
        public long Gen2Collections { get; set; }
        public long TotalAllocatedBytes { get; set; }
    }

    public MemoryMonitor(int samplingIntervalMs = 100)
    {
        _samplingIntervalMs = samplingIntervalMs;
    }

    /// <summary>
    /// Start monitoring memory usage
    /// </summary>
    public void StartMonitoring()
    {
        lock (_lock)
        {
            _samples.Clear();
            _samplingTimer = new Timer(CollectSample, null, 0, _samplingIntervalMs);
        }
    }

    /// <summary>
    /// Stop monitoring memory usage
    /// </summary>
    public void StopMonitoring()
    {
        _samplingTimer?.Dispose();
        _samplingTimer = null;
    }

    /// <summary>
    /// Collect a memory sample
    /// </summary>
    private void CollectSample(object? state)
    {
        var process = Process.GetCurrentProcess();
        var sample = new MemorySample
        {
            Timestamp = DateTime.UtcNow,
            WorkingSetBytes = process.WorkingSet64,
            GCTotalMemory = GC.GetTotalMemory(false),
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2),
            TotalAllocatedBytes = GC.GetTotalAllocatedBytes()
        };

        lock (_lock)
        {
            _samples.Add(sample);
        }
    }

    /// <summary>
    /// Capture GC profile snapshot
    /// </summary>
    public GCProfile CaptureProfile()
    {
        var process = Process.GetCurrentProcess();
        return new GCProfile
        {
            InitialGen0Collections = GC.CollectionCount(0),
            InitialGen1Collections = GC.CollectionCount(1),
            InitialGen2Collections = GC.CollectionCount(2),
            InitialAllocatedBytes = GC.GetTotalAllocatedBytes(),
            InitialWorkingSet = process.WorkingSet64
        };
    }

    /// <summary>
    /// Complete GC profile with final measurements
    /// </summary>
    public void CompleteProfile(GCProfile profile)
    {
        var process = Process.GetCurrentProcess();
        profile.FinalGen0Collections = GC.CollectionCount(0);
        profile.FinalGen1Collections = GC.CollectionCount(1);
        profile.FinalGen2Collections = GC.CollectionCount(2);
        profile.FinalAllocatedBytes = GC.GetTotalAllocatedBytes();
        profile.FinalWorkingSet = process.WorkingSet64;
    }

    /// <summary>
    /// Get memory statistics from collected samples
    /// </summary>
    public (double AvgHeapMB, long PeakWorkingSetMB, double AllocationRateMBPerSec) GetStatistics()
    {
        lock (_lock)
        {
            if (_samples.Count == 0)
                return (0, 0, 0);

            var avgHeapBytes = _samples.Average(s => s.GCTotalMemory);
            var peakWorkingSetBytes = _samples.Max(s => s.WorkingSetBytes);

            // Calculate allocation rate
            var allocationRate = 0.0;
            if (_samples.Count >= 2)
            {
                var firstSample = _samples.First();
                var lastSample = _samples.Last();
                var duration = (lastSample.Timestamp - firstSample.Timestamp).TotalSeconds;
                if (duration > 0)
                {
                    var allocatedBytes = lastSample.TotalAllocatedBytes - firstSample.TotalAllocatedBytes;
                    allocationRate = (allocatedBytes / 1024.0 / 1024.0) / duration; // MB/sec
                }
            }

            return (
                avgHeapBytes / 1024.0 / 1024.0,
                peakWorkingSetBytes / 1024 / 1024,
                allocationRate
            );
        }
    }

    /// <summary>
    /// Clear all collected samples
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _samples.Clear();
        }
    }

    /// <summary>
    /// Generate a detailed memory report
    /// </summary>
    public void GenerateReport(string scenario)
    {
        var (avgHeapMB, peakWorkingSetMB, allocationRateMBPerSec) = GetStatistics();

        Console.WriteLine($"   Memory Report - {scenario}:");
        Console.WriteLine($"      Average Heap Size: {avgHeapMB:F2} MB");
        Console.WriteLine($"      Peak Working Set: {peakWorkingSetMB} MB");
        Console.WriteLine($"      Allocation Rate: {allocationRateMBPerSec:F2} MB/sec");
    }
}

/// <summary>
/// Analyzer for comparing memory scenarios
/// </summary>
public class MemoryAnalyzer
{
    /// <summary>
    /// Analyze and compare memory metrics
    /// </summary>
    public void GenerateComparisonReport(List<MemoryMetrics> allMetrics)
    {
        if (allMetrics.Count == 0)
        {
            Console.WriteLine("   No metrics to analyze");
            return;
        }

        Console.WriteLine("");
        Console.WriteLine("================================================================================");
        Console.WriteLine("  Memory Optimization Comparison");
        Console.WriteLine("================================================================================");
        Console.WriteLine("");

        // Table header
        Console.WriteLine("  {0,-25} {1,12} {2,12} {3,12} {4,12}",
            "Scenario", "GC Gen0", "GC Gen2", "Peak WS MB", "Alloc MB/s");
        Console.WriteLine("  " + new string('-', 78));

        // Table rows
        foreach (var metrics in allMetrics)
        {
            Console.WriteLine("  {0,-25} {1,12} {2,12} {3,12:F1} {4,12:F2}",
                metrics.Scenario,
                metrics.Gen0Collections,
                metrics.Gen2Collections,
                metrics.PeakWorkingSet / 1024.0 / 1024.0,
                metrics.AllocationRateMBPerSec);
        }

        Console.WriteLine("");

        // Calculate improvements
        if (allMetrics.Count > 1)
        {
            var baseline = allMetrics[0];
            var optimized = allMetrics[allMetrics.Count - 1];

            var gen0Improvement = baseline.Gen0Collections > 0
                ? (1.0 - (double)optimized.Gen0Collections / baseline.Gen0Collections) * 100
                : 0;

            var allocationImprovement = baseline.AllocationRateMBPerSec > 0
                ? (1.0 - optimized.AllocationRateMBPerSec / baseline.AllocationRateMBPerSec) * 100
                : 0;

            Console.WriteLine("  Key Improvements:");
            Console.WriteLine($"     Gen0 GC Reduction: {gen0Improvement:F1}%");
            Console.WriteLine($"     Allocation Rate Reduction: {allocationImprovement:F1}%");

            if (optimized.ObjectPoolHits > 0)
            {
                Console.WriteLine($"     Object Pool Efficiency: {optimized.PoolEfficiency:F1}%");
            }

            if (optimized.CacheHits > 0)
            {
                Console.WriteLine($"     Cache Hit Ratio: {optimized.CacheHitRatio:F1}%");
            }
        }

        Console.WriteLine("");
    }

    /// <summary>
    /// Generate optimization recommendations
    /// </summary>
    public void GenerateRecommendations(List<MemoryMetrics> allMetrics)
    {
        Console.WriteLine("  💡 Optimization Recommendations:");
        Console.WriteLine("");

        if (allMetrics.Count == 0)
        {
            Console.WriteLine("     No data available for recommendations");
            return;
        }

        var baseline = allMetrics[0];
        var hasOptimization = allMetrics.Count > 1;

        // Object pooling recommendation
        if (hasOptimization && allMetrics.Any(m => m.ObjectPoolHits > 0))
        {
            var poolMetrics = allMetrics.First(m => m.ObjectPoolHits > 0);
            Console.WriteLine($"     ✅ Object Pooling: {poolMetrics.PoolEfficiency:F1}% efficiency achieved");
            Console.WriteLine("        - Reduces GC pressure by reusing objects");
            Console.WriteLine("        - Best for frequently allocated short-lived objects");
        }
        else
        {
            Console.WriteLine("     🔧 Consider Object Pooling for high-frequency allocations");
        }

        Console.WriteLine("");

        // Caching recommendation
        if (hasOptimization && allMetrics.Any(m => m.CacheHits > 0))
        {
            var cacheMetrics = allMetrics.First(m => m.CacheHits > 0);
            Console.WriteLine($"     ✅ LRU Cache: {cacheMetrics.CacheHitRatio:F1}% hit ratio achieved");
            Console.WriteLine("        - Reduces computational overhead");
            Console.WriteLine("        - Best for repeated lookups with limited key space");
        }
        else
        {
            Console.WriteLine("     🔧 Consider LRU Cache for repeated computations");
        }

        Console.WriteLine("");

        // GC tuning recommendations
        if (baseline.Gen2Collections > 5)
        {
            Console.WriteLine("     ⚠️  High Gen2 collections detected");
            Console.WriteLine("        - Consider reducing object lifetime");
            Console.WriteLine("        - Implement object pooling for large objects");
            Console.WriteLine("        - Review memory allocation patterns");
        }

        Console.WriteLine("");
    }
}