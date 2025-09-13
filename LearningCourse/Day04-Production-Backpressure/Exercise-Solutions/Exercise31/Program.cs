using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Collections.Concurrent;
using System.Diagnostics;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Console.WriteLine("🚀 Day 3 Exercise 3.1: Netflix-Style Adaptive Backpressure Implementation");
Console.WriteLine("".PadRight(70, '='));

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<IStreamProcessor, AdaptiveStreamProcessor>();
        services.AddSingleton<IBackpressureManager, BackpressureManager>();
        services.AddSingleton<ICapacityMonitor, CapacityMonitor>();
        services.AddHostedService<StreamingWorkloadSimulator>();
    })
    .UseSerilog()
    .Build();

try
{
    Log.Information("Starting Exercise 3.1: Netflix-Style Adaptive Backpressure");
    
    Console.WriteLine("📊 Simulating Netflix-scale streaming workload:");
    Console.WriteLine("   • Peak capacity: 15 Petabits/second (200M concurrent users)");
    Console.WriteLine("   • Backpressure threshold: 80% capacity (12 Petabits/second)");
    Console.WriteLine("   • Adaptive quality degradation to maintain service availability");
    Console.WriteLine();
    
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Error(ex, "Error in Exercise 3.1: Netflix-Style Adaptive Backpressure");
    Console.WriteLine($"❌ Error: {ex.Message}");
}
finally
{
    await host.StopAsync();
    await Log.CloseAndFlushAsync();
}

// Netflix-inspired adaptive streaming with production backpressure management
public interface IStreamProcessor
{
    Task ProcessStreamAsync(StreamingRequest request);
    StreamingMetrics GetCurrentMetrics();
}

public interface IBackpressureManager
{
    bool ShouldApplyBackpressure();
    QualityLevel GetOptimalQuality(double currentLoad);
    void RecordMetrics(double throughput, double latency);
}

public interface ICapacityMonitor
{
    double GetCurrentCapacityUtilization();
    double GetPredictedLoad();
    bool IsSystemHealthy();
}

public class AdaptiveStreamProcessor : IStreamProcessor
{
    private readonly IBackpressureManager _backpressureManager;
    private readonly ICapacityMonitor _capacityMonitor;
    private readonly ILogger<AdaptiveStreamProcessor> _logger;
    private readonly ConcurrentDictionary<string, StreamingSession> _activeSessions = new();

    public AdaptiveStreamProcessor(
        IBackpressureManager backpressureManager,
        ICapacityMonitor capacityMonitor,
        ILogger<AdaptiveStreamProcessor> logger)
    {
        _backpressureManager = backpressureManager;
        _capacityMonitor = capacityMonitor;
        _logger = logger;
    }

    public async Task ProcessStreamAsync(StreamingRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Netflix pattern: Check backpressure before processing
            if (_backpressureManager.ShouldApplyBackpressure())
            {
                var optimalQuality = _backpressureManager.GetOptimalQuality(_capacityMonitor.GetCurrentCapacityUtilization());
                request = request with { Quality = optimalQuality };
                _logger.LogInformation("Backpressure applied: Quality adjusted to {Quality}", optimalQuality);
            }

            // Process streaming request with quality adaptation
            await ProcessWithQualityAdaptation(request);
            
            // Record performance metrics for backpressure decisions
            stopwatch.Stop();
            _backpressureManager.RecordMetrics(
                GetThroughputForQuality(request.Quality),
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process streaming request for user {UserId}", request.UserId);
            
            // Re-throw with contextual information for upstream handling
            throw new InvalidOperationException($"Stream processing failed for user {request.UserId}", ex);
        }
    }

    private async Task ProcessWithQualityAdaptation(StreamingRequest request)
    {
        // Simulate Netflix-style processing based on quality level
        var processingTime = request.Quality switch
        {
            QualityLevel.Ultra4K => 250,     // 4K: Higher CPU/bandwidth requirements
            QualityLevel.HD1080p => 150,     // 1080p: Balanced performance
            QualityLevel.HD720p => 100,      // 720p: Reduced load
            QualityLevel.SD480p => 50,       // 480p: Minimal load for emergency capacity
            _ => 150
        };

        // Update active session tracking
        var session = new StreamingSession(request.UserId, request.Quality, DateTime.UtcNow);
        _activeSessions.AddOrUpdate(request.UserId, session, (key, oldValue) => session);

        await Task.Delay(processingTime); // Simulate processing
    }

    private double GetThroughputForQuality(QualityLevel quality)
    {
        // Real Netflix bitrate requirements (Mbps)
        return quality switch
        {
            QualityLevel.Ultra4K => 25.0,   // 4K Ultra HD
            QualityLevel.HD1080p => 8.0,    // 1080p HD
            QualityLevel.HD720p => 5.0,     // 720p HD
            QualityLevel.SD480p => 1.5,     // 480p SD
            _ => 8.0
        };
    }

    public StreamingMetrics GetCurrentMetrics()
    {
        var sessions = _activeSessions.Values.ToList();
        var qualityDistribution = sessions
            .GroupBy(s => s.Quality)
            .ToDictionary(g => g.Key, g => g.Count());

        return new StreamingMetrics(
            ActiveSessions: sessions.Count,
            CapacityUtilization: _capacityMonitor.GetCurrentCapacityUtilization(),
            QualityDistribution: qualityDistribution,
            BackpressureActive: _backpressureManager.ShouldApplyBackpressure()
        );
    }
}

public class BackpressureManager : IBackpressureManager
{
    private readonly Queue<MetricReading> _recentMetrics = new();
    private readonly object _metricsLock = new object();
    
    // Netflix-inspired thresholds based on published performance data
    private const double BackpressureThreshold = 0.80;      // 80% capacity
    private const double CriticalThreshold = 0.95;          // 95% capacity
    private const double TargetLatencyMs = 100;             // 100ms target latency
    private const int MetricsWindowSize = 100;              // Rolling window

    public bool ShouldApplyBackpressure()
    {
        lock (_metricsLock)
        {
            if (_recentMetrics.Count < 10) return false; // Need baseline

            var avgLatency = _recentMetrics.Average(m => m.LatencyMs);
            var avgThroughput = _recentMetrics.Average(m => m.ThroughputMbps);
            
            // Netflix pattern: Backpressure based on latency and capacity
            return avgLatency > TargetLatencyMs * 1.5 || avgThroughput > BackpressureThreshold * 15000; // 15 Petabits = 15,000,000 Mbps
        }
    }

    public QualityLevel GetOptimalQuality(double currentLoad)
    {
        // Netflix adaptive quality based on system load
        return currentLoad switch
        {
            >= CriticalThreshold => QualityLevel.SD480p,    // Emergency: Drop to SD
            >= 0.90 => QualityLevel.HD720p,                 // High load: 720p
            >= BackpressureThreshold => QualityLevel.HD1080p, // Medium load: 1080p
            _ => QualityLevel.Ultra4K                       // Normal: Full quality
        };
    }

    public void RecordMetrics(double throughput, double latency)
    {
        lock (_metricsLock)
        {
            _recentMetrics.Enqueue(new MetricReading(throughput, latency, DateTime.UtcNow));
            
            // Maintain rolling window
            while (_recentMetrics.Count > MetricsWindowSize)
            {
                _recentMetrics.Dequeue();
            }
        }
    }
}

public class CapacityMonitor : ICapacityMonitor
{
    public double GetCurrentCapacityUtilization()
    {
        // Simulate Netflix-style capacity calculation based on time of day
        var hour = DateTime.UtcNow.Hour;
        
        // Netflix peak hours: 20:00-23:00 UTC (8-11 PM)
        var peakMultiplier = hour switch
        {
            >= 20 and <= 23 => 3.5,  // Peak evening hours
            >= 18 and < 20 => 2.2,   // Pre-peak buildup
            >= 12 and < 18 => 1.5,   // Afternoon moderate usage
            >= 6 and < 12 => 1.0,    // Morning baseline
            _ => 0.6                  // Late night/early morning
        };

        // Add deterministic variation based on minute of hour
        var minute = DateTime.UtcNow.Minute;
        var variation = Math.Sin(minute * Math.PI / 30) * 0.15; // ±15% variation
        
        var baseUtilization = 0.4 * peakMultiplier + variation;
        return Math.Max(0.1, Math.Min(0.98, baseUtilization)); // Keep within realistic bounds
    }

    public double GetPredictedLoad()
    {
        // Netflix predictive analytics: forecast load 5 minutes ahead
        var currentLoad = GetCurrentCapacityUtilization();
        var hour = DateTime.UtcNow.Hour;
        var minute = DateTime.UtcNow.Minute;
        
        // Predict trend based on time progression toward/away from peak
        var trendMultiplier = hour switch
        {
            >= 19 and < 20 => 1.2,   // Building toward peak
            >= 20 and < 23 => 1.0,   // Stable at peak
            >= 23 and <= 24 => 0.8,  // Declining from peak
            _ => 1.0
        };

        return currentLoad * trendMultiplier;
    }

    public bool IsSystemHealthy()
    {
        var utilization = GetCurrentCapacityUtilization();
        var predictedLoad = GetPredictedLoad();
        
        // Netflix health criteria: current load manageable and trend sustainable
        return utilization < CriticalThreshold && predictedLoad < CriticalThreshold;
    }

    private const double CriticalThreshold = 0.95;
}

// Simulate realistic Netflix-scale workload patterns
public class StreamingWorkloadSimulator : BackgroundService
{
    private readonly IStreamProcessor _streamProcessor;
    private readonly ILogger<StreamingWorkloadSimulator> _logger;
    private int _userIdCounter = 1;

    public StreamingWorkloadSimulator(IStreamProcessor streamProcessor, ILogger<StreamingWorkloadSimulator> logger)
    {
        _streamProcessor = streamProcessor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(1000, stoppingToken); // Initial startup delay
        
        var concurrentRequests = new List<Task>();
        var maxConcurrency = 50; // Simulate moderate load for demo
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Clean up completed tasks
                concurrentRequests.RemoveAll(t => t.IsCompleted);
                
                // Simulate Netflix user request patterns
                if (concurrentRequests.Count < maxConcurrency)
                {
                    var request = GenerateRealisticRequest();
                    var task = _streamProcessor.ProcessStreamAsync(request);
                    concurrentRequests.Add(task);
                }

                // Display metrics every 5 seconds
                await DisplayMetrics();
                await Task.Delay(2000, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break; // Normal shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in workload simulation");
                await Task.Delay(5000, stoppingToken); // Recover from errors
            }
        }
    }

    private StreamingRequest GenerateRealisticRequest()
    {
        var userId = $"user_{_userIdCounter++}";
        
        // Netflix quality distribution: 40% HD, 35% 4K, 20% 720p, 5% SD
        var qualityDistribution = GenerateQualityDistribution();
        
        return new StreamingRequest(userId, qualityDistribution);
    }

    private QualityLevel GenerateQualityDistribution()
    {
        var hour = DateTime.UtcNow.Hour;
        var second = DateTime.UtcNow.Second;
        
        // Time-based deterministic quality selection (consistent for education)
        var selector = (hour * 60 + second) % 100;
        
        return selector switch
        {
            < 40 => QualityLevel.HD1080p,    // 40% - Most common
            < 75 => QualityLevel.Ultra4K,    // 35% - Premium users
            < 95 => QualityLevel.HD720p,     // 20% - Standard users
            _ => QualityLevel.SD480p          // 5% - Low bandwidth
        };
    }

    private async Task DisplayMetrics()
    {
        var metrics = _streamProcessor.GetCurrentMetrics();
        
        Console.Clear();
        Console.WriteLine("🚀 Netflix-Style Adaptive Backpressure - Live Metrics");
        Console.WriteLine("".PadRight(70, '='));
        Console.WriteLine($"📊 Active Streaming Sessions: {metrics.ActiveSessions:N0}");
        Console.WriteLine($"⚡ System Capacity Utilization: {metrics.CapacityUtilization:P1}");
        Console.WriteLine($"🎯 Backpressure Status: {(metrics.BackpressureActive ? "🔴 ACTIVE" : "🟢 Normal")}");
        Console.WriteLine();
        
        Console.WriteLine("📺 Quality Distribution:");
        foreach (var quality in metrics.QualityDistribution)
        {
            var percentage = (double)quality.Value / metrics.ActiveSessions;
            var bar = new string('█', Math.Min(30, (int)(percentage * 30)));
            Console.WriteLine($"   {quality.Key,-12}: {percentage:P1} {bar}");
        }
        
        Console.WriteLine();
        Console.WriteLine("💡 Real Netflix Stats:");
        Console.WriteLine("   • 200M+ concurrent users during peak hours");
        Console.WriteLine("   • 15 Petabits/second peak global traffic");
        Console.WriteLine("   • 80% capacity threshold triggers quality adaptation");
        Console.WriteLine("   • 99.9% uptime through intelligent backpressure management");
        
        await Task.Delay(100); // Brief pause for display
    }
}

// Data models representing Netflix-style streaming architecture
public record StreamingRequest(string UserId, QualityLevel Quality);

public record StreamingSession(string UserId, QualityLevel Quality, DateTime StartTime);

public record StreamingMetrics(
    int ActiveSessions,
    double CapacityUtilization,
    Dictionary<QualityLevel, int> QualityDistribution,
    bool BackpressureActive
);

public record MetricReading(double ThroughputMbps, double LatencyMs, DateTime Timestamp);

public enum QualityLevel
{
    SD480p,      // Standard Definition - Emergency capacity
    HD720p,      // HD Ready - Reduced load
    HD1080p,     // Full HD - Standard quality
    Ultra4K      // Ultra HD - Premium experience
}
