using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Serilog;

/// <summary>
/// Enterprise-grade metrics collection service implementing Netflix-style Four Golden Signals monitoring.
/// Based on Google SRE practices and Netflix's production observability patterns.
/// 
/// References:
/// - Netflix Technology Blog: Observability at Scale
/// - Google SRE Book: The Four Golden Signals
/// - Prometheus Best Practices for Enterprise Monitoring
/// </summary>
public class NetflixStyleMetricsService
{
    private static readonly ActivitySource ActivitySource = new("FlinkDotNet.Exercise41.Metrics");
    private static readonly Meter NetflixMeter = new("FlinkDotNet.Exercise41.Netflix");
    
    // === THE FOUR GOLDEN SIGNALS IMPLEMENTATION ===
    // Following Netflix's production monitoring philosophy
    
    // 1. LATENCY - Request processing time distributions (Netflix target: P99 < 100ms)
    private static readonly Histogram<double> RequestLatency = NetflixMeter.CreateHistogram<double>(
        "http_request_duration_seconds",
        "seconds",
        "Duration of HTTP requests - Netflix production target P99 < 100ms");
        
    private static readonly Histogram<double> ContentDeliveryLatency = NetflixMeter.CreateHistogram<double>(
        "content_delivery_latency_ms",
        "milliseconds", 
        "Content delivery latency - Netflix CDN performance metric");

    // 2. TRAFFIC - Request rates and throughput (Netflix peak: 200M concurrent users)
    private static readonly Counter<long> RequestsTotal = NetflixMeter.CreateCounter<long>(
        "http_requests_total",
        description: "Total HTTP requests - Netflix handles 200M+ concurrent users");
        
    private static readonly Counter<long> ContentStreamsStarted = NetflixMeter.CreateCounter<long>(
        "content_streams_started_total",
        description: "Total content streams initiated - Netflix core business metric");

    // 3. ERRORS - Error rates and failure classifications (Netflix SLO: 99.97% availability)
    private static readonly Counter<long> ErrorsTotal = NetflixMeter.CreateCounter<long>(
        "errors_total",
        description: "Total errors by type - Netflix 99.97% availability SLO");
        
    private static readonly Counter<long> ContentBufferingEvents = NetflixMeter.CreateCounter<long>(
        "content_buffering_events_total",
        description: "Content buffering events - Netflix quality of experience metric");

    // === BUSINESS METRICS (Netflix-specific KPIs) ===
    
    private static readonly Counter<long> ContentMinutesWatched = NetflixMeter.CreateCounter<long>(
        "content_minutes_watched_total",
        description: "Total content minutes watched - Netflix engagement metric");
        
    private static readonly Histogram<double> SessionDuration = NetflixMeter.CreateHistogram<double>(
        "user_session_duration_minutes",
        "minutes",
        "User session duration - Netflix average 23 minutes per session");
        
    private static readonly Counter<long> SubscriptionEvents = NetflixMeter.CreateCounter<long>(
        "subscription_events_total", 
        description: "Subscription lifecycle events - Netflix business growth metric");

    // === NETFLIX ADAPTIVE BITRATE METRICS ===
    
    private static readonly Counter<long> BitrateAdaptations = NetflixMeter.CreateCounter<long>(
        "bitrate_adaptations_total",
        description: "Bitrate adaptation events - Netflix adaptive streaming quality");
        
    private static readonly Histogram<double> VideoQualityScore = NetflixMeter.CreateHistogram<double>(
        "video_quality_score",
        description: "Video quality score 1-5 - Netflix QoE measurement");

    // Observable gauges for current state metrics
    private double _currentConcurrentUsers = 50_000_000;
    private double _currentServiceAvailability = 99.97;
    private double _currentCpuUtilization = 35.0;
    private double _currentMemoryUtilization = 60.0;
    private long _currentActiveConnections = 25000L;
    private double _currentCdnCacheHitRate = 95.0;
    private double _currentContentCatalogSize = 15000;

    private readonly ILogger<NetflixStyleMetricsService> _logger;
    private readonly Random _deterministicRandom;

    public NetflixStyleMetricsService(ILogger<NetflixStyleMetricsService> logger)
    {
        _logger = logger;
        // Use deterministic seed for consistent educational behavior
        _deterministicRandom = new Random(42);
        
        // Set up observable gauges with callbacks
        SetupObservableGauges();
    }

    private void SetupObservableGauges()
    {
        // 4. SATURATION and current state metrics using observable gauges
        NetflixMeter.CreateObservableGauge("concurrent_users_current",
            () => _currentConcurrentUsers,
            description: "Current concurrent users - Netflix peak 200M during prime time");

        NetflixMeter.CreateObservableGauge("service_availability_percent",
            () => _currentServiceAvailability,
            "%",
            "Current service availability - Netflix SLO target 99.97%");

        NetflixMeter.CreateObservableGauge("cpu_utilization_percent",
            () => _currentCpuUtilization,
            "%",
            "CPU utilization - Netflix auto-scaling triggers at 70%");

        NetflixMeter.CreateObservableGauge("memory_utilization_percent",
            () => _currentMemoryUtilization,
            "%",
            "Memory utilization - Netflix capacity management");

        NetflixMeter.CreateObservableGauge("active_connections_current",
            () => _currentActiveConnections,
            description: "Current active connections - Netflix connection pool saturation");

        NetflixMeter.CreateObservableGauge("cdn_cache_hit_rate_percent",
            () => _currentCdnCacheHitRate,
            "%",
            "CDN cache hit rate - Netflix content delivery efficiency");

        NetflixMeter.CreateObservableGauge("content_catalog_size_hours",
            () => _currentContentCatalogSize,
            "hours",
            "Total content catalog size in hours - Netflix content library metric");
    }

    public async Task RunNetflixMetricsDemo()
    {
        _logger.LogInformation("Starting Netflix-style metrics collection demonstration");
        
        using var activity = ActivitySource.StartActivity("NetflixMetricsDemo");
        activity?.SetTag("demo.type", "four_golden_signals");
        activity?.SetTag("company.pattern", "netflix");
        
        // Simulate Netflix-scale operations with realistic patterns
        await SimulateNetflixPrimeTimeLoad();
        await SimulateNetflixContentDelivery();
        await SimulateNetflixUserBehavior();
        await SimulateNetflixInfrastructure();
        
        _logger.LogInformation("Netflix-style metrics demonstration completed");
    }

    /// <summary>
    /// Simulate Netflix prime time load patterns (6-10 PM peak hours)
    /// Netflix handles 3x normal traffic during prime time
    /// </summary>
    private async Task SimulateNetflixPrimeTimeLoad()
    {
        _logger.LogInformation("Simulating Netflix prime time load patterns...");
        
        using var activity = ActivitySource.StartActivity("NetflixPrimeTimeLoad");
        
        var baselineUsers = 50_000_000; // 50M baseline concurrent users
        var totalRequests = 0L;
        var successfulRequests = 0L;
        
        for (int hour = 0; hour < 24; hour++)
        {
            // Calculate realistic user load based on time of day
            var loadMultiplier = GetNetflixLoadMultiplier(hour);
            var currentUsers = (long)(baselineUsers * loadMultiplier);
            var requestsThisHour = currentUsers / 10; // ~10% of users make requests per hour
            
            _currentConcurrentUsers = currentUsers;
            
            _logger.LogInformation("Hour {Hour:D2}:00 - {CurrentUsers:N0} concurrent users ({LoadMultiplier:P0} of peak)", 
                hour, currentUsers, loadMultiplier);
            
            // Process requests for this hour
            for (int request = 0; request < Math.Min(requestsThisHour, 10000); request += 100)
            {
                var isSuccess = SimulateNetflixRequestSuccess();
                var latency = SimulateNetflixRequestLatency(isSuccess, hour >= 18 && hour <= 22);
                
                totalRequests += 100;
                
                // 1. LATENCY - Record Netflix-style request latency
                RequestLatency.Record(latency / 1000.0, 
                    new KeyValuePair<string, object?>("endpoint", "/api/content"),
                    new KeyValuePair<string, object?>("region", "us-west-2"),
                    new KeyValuePair<string, object?>("time_period", hour >= 18 && hour <= 22 ? "prime_time" : "off_peak"));
                
                // 2. TRAFFIC - Record request volume
                RequestsTotal.Add(100,
                    new KeyValuePair<string, object?>("method", "GET"),
                    new KeyValuePair<string, object?>("service", "content-api"),
                    new KeyValuePair<string, object?>("load_pattern", GetLoadPatternName(hour)));
                
                if (isSuccess)
                {
                    successfulRequests += 100;
                    
                    // Start content streams
                    var streamsStarted = _deterministicRandom.Next(80, 95); // 80-95% of successful requests start streams
                    ContentStreamsStarted.Add(streamsStarted,
                        new KeyValuePair<string, object?>("content_type", GetRandomContentType()),
                        new KeyValuePair<string, object?>("quality", GetRandomVideoQuality()));
                }
                else
                {
                    // 3. ERRORS - Record error patterns
                    ErrorsTotal.Add(100 - (isSuccess ? 0 : 100),
                        new KeyValuePair<string, object?>("error_type", GetRandomErrorType()),
                        new KeyValuePair<string, object?>("severity", GetErrorSeverity()),
                        new KeyValuePair<string, object?>("recovery_action", "auto_retry"));
                }
                
                // Update availability calculation (Netflix SLO: 99.97%)
                var currentAvailability = totalRequests > 0 ? (double)successfulRequests / totalRequests * 100 : 100;
                _currentServiceAvailability = currentAvailability;
                
                if (request % 1000 == 0)
                {
                    await Task.Delay(10); // Brief pause for realistic timing
                }
            }
            
            // Log hourly summary
            var hourlyAvailability = totalRequests > 0 ? (double)successfulRequests / totalRequests * 100 : 100;
            var sloCompliant = hourlyAvailability >= 99.97;
            
            _logger.LogInformation("Hour {Hour:D2} Summary: {HourlyAvailability:F3}% availability {SloStatus} | {CurrentUsers:N0} users", 
                hour, hourlyAvailability, sloCompliant ? "✅" : "🚨", currentUsers);
            
            if (!sloCompliant)
            {
                _logger.LogWarning("SLO VIOLATION: Availability {HourlyAvailability:F3}% below Netflix target 99.97%", hourlyAvailability);
            }
        }
        
        var finalAvailability = (double)successfulRequests / totalRequests * 100;
        _logger.LogInformation("24-Hour Netflix Load Complete: {FinalAvailability:F3}% availability | {TotalRequests:N0} total requests", 
            finalAvailability, totalRequests);
    }

    /// <summary>
    /// Simulate Netflix content delivery performance and CDN metrics
    /// Netflix operates one of the world's largest CDNs
    /// </summary>
    private async Task SimulateNetflixContentDelivery()
    {
        _logger.LogInformation("Simulating Netflix content delivery and CDN performance...");
        
        using var activity = ActivitySource.StartActivity("NetflixContentDelivery");
        
        var contentTypes = new[] { "movie", "series", "documentary", "standup", "kids" };
        var regions = new[] { "us-east", "us-west", "eu-west", "ap-southeast", "sa-east" };
        
        for (int delivery = 0; delivery < 2000; delivery++)
        {
            var contentType = contentTypes[GetDeterministicIndex(delivery, contentTypes.Length)];
            var region = regions[GetDeterministicIndex(delivery * 3, regions.Length)];
            var isHighDemandContent = GetDeterministicBoolean(delivery, 0.3); // 30% high-demand content
            
            // Simulate content delivery latency (Netflix CDN optimized for <50ms)
            var deliveryLatency = SimulateContentDeliveryLatency(region, isHighDemandContent);
            ContentDeliveryLatency.Record(deliveryLatency,
                new KeyValuePair<string, object?>("content_type", contentType),
                new KeyValuePair<string, object?>("region", region),
                new KeyValuePair<string, object?>("demand_level", isHighDemandContent ? "high" : "normal"));
            
            // Simulate CDN cache performance
            var cacheHitRate = SimulateCacheHitRate(region, isHighDemandContent);
            _currentCdnCacheHitRate = cacheHitRate;
            
            // Simulate bitrate adaptation for Netflix adaptive streaming
            if (GetDeterministicBoolean(delivery, 0.15)) // 15% of streams need bitrate adaptation
            {
                BitrateAdaptations.Add(1,
                    new KeyValuePair<string, object?>("from_quality", GetRandomVideoQuality()),
                    new KeyValuePair<string, object?>("to_quality", GetAdaptiveVideoQuality()),
                    new KeyValuePair<string, object?>("reason", GetAdaptationReason()));
            }
            
            // Simulate video quality scoring
            var qualityScore = SimulateVideoQualityScore(deliveryLatency, cacheHitRate);
            VideoQualityScore.Record(qualityScore,
                new KeyValuePair<string, object?>("content_type", contentType),
                new KeyValuePair<string, object?>("delivery_method", cacheHitRate > 95 ? "edge_cache" : "origin"));
            
            if (delivery % 200 == 0)
            {
                _logger.LogInformation("Content Delivery Progress: {Delivery:N0}/2000 | Quality: {QualityScore:F1}/5.0 | Cache Hit: {CacheHitRate:F1}%", 
                    delivery, qualityScore, cacheHitRate);
                await Task.Delay(10);
            }
        }
        
        _logger.LogInformation("Netflix content delivery simulation completed");
    }

    /// <summary>
    /// Simulate Netflix user behavior patterns and engagement metrics
    /// Netflix tracks detailed user engagement for content recommendations
    /// </summary>
    private async Task SimulateNetflixUserBehavior()
    {
        _logger.LogInformation("Simulating Netflix user behavior and engagement patterns...");
        
        using var activity = ActivitySource.StartActivity("NetflixUserBehavior");
        
        var subscriptionTiers = new[] { "basic", "standard", "premium" };
        var deviceTypes = new[] { "tv", "mobile", "desktop", "tablet" };
        
        for (int session = 0; session < 1500; session++)
        {
            var subscriptionTier = subscriptionTiers[GetDeterministicIndex(session, subscriptionTiers.Length)];
            var deviceType = deviceTypes[GetDeterministicIndex(session * 2, deviceTypes.Length)];
            
            // Simulate session duration (Netflix average: 23 minutes per session)
            var sessionMinutes = SimulateSessionDuration(subscriptionTier, deviceType);
            SessionDuration.Record(sessionMinutes,
                new KeyValuePair<string, object?>("subscription_tier", subscriptionTier),
                new KeyValuePair<string, object?>("device_type", deviceType),
                new KeyValuePair<string, object?>("time_of_day", GetTimeOfDayCategory()));
            
            // Track content minutes watched
            var contentMinutes = sessionMinutes * 0.85; // 85% of session time is actual content
            ContentMinutesWatched.Add((long)contentMinutes,
                new KeyValuePair<string, object?>("content_type", GetRandomContentType()),
                new KeyValuePair<string, object?>("subscription_tier", subscriptionTier));
            
            // Simulate subscription events (signups, cancellations, upgrades)
            if (GetDeterministicBoolean(session, 0.02)) // 2% of sessions trigger subscription events
            {
                SubscriptionEvents.Add(1,
                    new KeyValuePair<string, object?>("event_type", GetSubscriptionEventType()),
                    new KeyValuePair<string, object?>("from_tier", subscriptionTier),
                    new KeyValuePair<string, object?>("region", GetRandomRegion()));
            }
            
            // Simulate buffering events (Netflix quality metric)
            if (GetDeterministicBoolean(session, 0.05)) // 5% of sessions experience buffering
            {
                var bufferingCount = GetDeterministicValue(session, 1, 3);
                ContentBufferingEvents.Add(bufferingCount,
                    new KeyValuePair<string, object?>("device_type", deviceType),
                    new KeyValuePair<string, object?>("connection_type", GetConnectionType()),
                    new KeyValuePair<string, object?>("resolution", GetRandomVideoQuality()));
            }
            
            if (session % 150 == 0)
            {
                _logger.LogInformation("User Session Progress: {Session:N0}/1500 | Avg Session: {SessionMinutes:F1} min | Content: {ContentMinutes:F1} min", 
                    session, sessionMinutes, contentMinutes);
                await Task.Delay(10);
            }
        }
        
        // Update content catalog size (Netflix has 15,000+ hours of content)
        _currentContentCatalogSize = 15000 + GetDeterministicValue(42, 0, 500); // 15,000-15,500 hours
        
        _logger.LogInformation("Netflix user behavior simulation completed");
    }

    /// <summary>
    /// Simulate Netflix infrastructure metrics and resource utilization
    /// Netflix auto-scales based on CPU utilization and other saturation metrics
    /// </summary>
    private async Task SimulateNetflixInfrastructure()
    {
        _logger.LogInformation("Simulating Netflix infrastructure metrics and auto-scaling...");
        
        using var activity = ActivitySource.StartActivity("NetflixInfrastructure");
        
        var baselineCpu = 35.0; // 35% baseline CPU utilization
        var baselineMemory = 60.0; // 60% baseline memory utilization
        var baselineConnections = 25000L; // 25k baseline connections
        
        for (int minute = 0; minute < 60; minute++) // Simulate 1 hour of infrastructure metrics
        {
            // 4. SATURATION - Calculate realistic resource utilization
            var loadFactor = GetInfrastructureLoadFactor(minute);
            
            var currentCpu = Math.Min(95, baselineCpu * loadFactor + GetDeterministicValue(minute, -5, 15));
            var currentMemory = Math.Min(90, baselineMemory * loadFactor + GetDeterministicValue(minute * 2, -10, 20));
            var currentConnections = (long)(baselineConnections * loadFactor + GetDeterministicValue(minute * 3, -2000, 5000));
            
            _currentCpuUtilization = currentCpu;
            _currentMemoryUtilization = currentMemory;
            _currentActiveConnections = Math.Max(0, currentConnections);
            
            // Netflix auto-scaling triggers
            if (currentCpu > 70) // Netflix scales at 70% CPU
            {
                _logger.LogWarning("AUTO-SCALING TRIGGER: CPU {CurrentCpu:F1}% exceeds Netflix scaling threshold (70%)", currentCpu);
                
                // Simulate scaling effect (gradual CPU reduction)
                currentCpu = Math.Max(50, currentCpu - 15);
                _currentCpuUtilization = currentCpu;
                
                _logger.LogInformation("AUTO-SCALING APPLIED: CPU reduced to {CurrentCpu:F1}% after scaling", currentCpu);
            }
            
            if (currentMemory > 85) // Memory pressure threshold
            {
                _logger.LogWarning("MEMORY PRESSURE: Memory {CurrentMemory:F1}% approaching critical threshold (90%)", currentMemory);
            }
            
            if (currentConnections > 45000) // Connection pool saturation
            {
                _logger.LogWarning("CONNECTION SATURATION: {CurrentConnections:N0} connections approaching limit (50k)", currentConnections);
            }
            
            // Log infrastructure summary every 10 minutes
            if (minute % 10 == 0)
            {
                _logger.LogInformation("Infrastructure Minute {Minute:D2}: CPU {CurrentCpu:F1}%, Memory {CurrentMemory:F1}%, Connections {CurrentConnections:N0}", 
                    minute, currentCpu, currentMemory, currentConnections);
            }
            
            await Task.Delay(50); // Simulate real-time collection interval
        }
        
        _logger.LogInformation("Netflix infrastructure simulation completed");
    }

    // === HELPER METHODS FOR REALISTIC DATA GENERATION ===
    
    private double GetNetflixLoadMultiplier(int hour)
    {
        // Netflix load patterns: peak 6-10 PM (18-22), low 2-6 AM (2-6)
        return hour switch
        {
            >= 2 and <= 6 => 0.3,   // Low traffic: 2-6 AM
            >= 7 and <= 11 => 0.6,  // Morning: 7-11 AM  
            >= 12 and <= 17 => 0.8, // Afternoon: 12-5 PM
            >= 18 and <= 22 => 1.0, // Prime time: 6-10 PM
            _ => 0.5                 // Late night: 11 PM-1 AM
        };
    }

    private bool SimulateNetflixRequestSuccess()
    {
        // Netflix SLO: 99.97% availability = 0.03% error rate
        return GetDeterministicBoolean(GetDeterministicTimestamp(), 0.9997);
    }

    private double SimulateNetflixRequestLatency(bool isSuccess, bool isPrimeTime)
    {
        if (!isSuccess)
        {
            return 500 + _deterministicRandom.NextDouble() * 1000; // 500-1500ms for errors
        }

        // Netflix production latency targets
        var baseLatency = isPrimeTime ? 40 : 25; // Higher latency during prime time
        var variation = _deterministicRandom.NextDouble() * 30; // ±30ms variation
        
        return Math.Max(5, baseLatency + variation); // Minimum 5ms, Netflix target P99 < 100ms
    }

    private double SimulateContentDeliveryLatency(string region, bool isHighDemand)
    {
        var regionLatency = region switch
        {
            "us-east" => 15,
            "us-west" => 20,
            "eu-west" => 35,
            "ap-southeast" => 45,
            "sa-east" => 55,
            _ => 30
        };

        var demandMultiplier = isHighDemand ? 1.3 : 1.0;
        var variation = _deterministicRandom.NextDouble() * 10;
        
        return regionLatency * demandMultiplier + variation;
    }

    private double SimulateCacheHitRate(string region, bool isHighDemand)
    {
        var baseCacheRate = isHighDemand ? 98.5 : 94.0; // High-demand content cached better
        var regionAdjustment = region == "us-east" ? 2.0 : 0.0; // Primary region advantage
        var variation = _deterministicRandom.NextDouble() * 2 - 1; // ±1% variation
        
        return Math.Min(99.9, Math.Max(85, baseCacheRate + regionAdjustment + variation));
    }

    private double SimulateVideoQualityScore(double latency, double cacheHitRate)
    {
        // Quality score based on delivery performance
        var baseScore = 4.2; // Netflix average quality score
        var latencyPenalty = latency > 50 ? (latency - 50) * 0.01 : 0;
        var cacheBenefit = cacheHitRate > 95 ? 0.3 : 0;
        var variation = _deterministicRandom.NextDouble() * 0.4 - 0.2; // ±0.2 variation
        
        return Math.Min(5.0, Math.Max(1.0, baseScore - latencyPenalty + cacheBenefit + variation));
    }

    private double SimulateSessionDuration(string subscriptionTier, string deviceType)
    {
        // Netflix session duration patterns by tier and device
        var baseDuration = subscriptionTier switch
        {
            "premium" => 28,   // Premium users watch longer
            "standard" => 23,  // Average Netflix session duration
            "basic" => 18,     // Basic users shorter sessions
            _ => 23
        };

        var deviceMultiplier = deviceType switch
        {
            "tv" => 1.4,       // TV sessions longer
            "desktop" => 1.1,  // Desktop moderate
            "tablet" => 0.9,   // Tablet shorter
            "mobile" => 0.7,   // Mobile shortest
            _ => 1.0
        };

        var variation = _deterministicRandom.NextDouble() * 10 - 5; // ±5 minutes
        return Math.Max(2, baseDuration * deviceMultiplier + variation);
    }

    private double GetInfrastructureLoadFactor(int minute)
    {
        // Simulate realistic infrastructure load cycles
        var cycleFactor = Math.Sin(minute * Math.PI / 30) * 0.3 + 1.0; // 30-minute cycle
        var randomVariation = _deterministicRandom.NextDouble() * 0.2 + 0.9; // ±10% variation
        return cycleFactor * randomVariation;
    }

    // === DETERMINISTIC DATA GENERATION HELPERS ===
    
    private long GetDeterministicTimestamp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000;
    }

    private int GetDeterministicIndex(int seed, int length)
    {
        return Math.Abs((seed * 31 + 17) % length);
    }

    private bool GetDeterministicBoolean(long seed, double probability)
    {
        var hash = seed * 2654435761L % int.MaxValue;
        return (hash / (double)int.MaxValue) < probability;
    }

    private int GetDeterministicValue(long seed, int min, int max)
    {
        var hash = Math.Abs(seed * 1103515245L + 12345L) % (max - min + 1);
        return min + (int)hash;
    }

    private string GetRandomContentType()
    {
        var types = new[] { "movie", "series", "documentary", "standup_comedy", "kids_content" };
        return types[_deterministicRandom.Next(types.Length)];
    }

    private string GetRandomVideoQuality()
    {
        var qualities = new[] { "1080p", "720p", "480p", "4k_uhd" };
        return qualities[_deterministicRandom.Next(qualities.Length)];
    }

    private string GetAdaptiveVideoQuality()
    {
        // Adaptive streaming typically reduces quality
        var qualities = new[] { "720p", "480p", "360p" };
        return qualities[_deterministicRandom.Next(qualities.Length)];
    }

    private string GetRandomErrorType()
    {
        var errors = new[] { "timeout", "service_unavailable", "rate_limited", "auth_failure" };
        return errors[_deterministicRandom.Next(errors.Length)];
    }

    private string GetErrorSeverity()
    {
        var severities = new[] { "low", "medium", "high", "critical" };
        return severities[_deterministicRandom.Next(severities.Length)];
    }

    private string GetLoadPatternName(int hour)
    {
        return hour switch
        {
            >= 18 and <= 22 => "prime_time",
            >= 2 and <= 6 => "low_traffic",
            _ => "normal"
        };
    }

    private string GetTimeOfDayCategory()
    {
        var hour = DateTime.UtcNow.Hour;
        return hour switch
        {
            >= 6 and <= 12 => "morning",
            >= 13 and <= 17 => "afternoon", 
            >= 18 and <= 22 => "evening",
            _ => "night"
        };
    }

    private string GetSubscriptionEventType()
    {
        var events = new[] { "signup", "cancellation", "upgrade", "downgrade", "reactivation" };
        return events[_deterministicRandom.Next(events.Length)];
    }

    private string GetRandomRegion()
    {
        var regions = new[] { "north_america", "europe", "asia_pacific", "latin_america" };
        return regions[_deterministicRandom.Next(regions.Length)];
    }

    private string GetConnectionType()
    {
        var connections = new[] { "fiber", "cable", "dsl", "mobile_5g", "mobile_4g" };
        return connections[_deterministicRandom.Next(connections.Length)];
    }

    private string GetAdaptationReason()
    {
        var reasons = new[] { "bandwidth_drop", "cpu_load", "buffer_underrun", "user_preference" };
        return reasons[_deterministicRandom.Next(reasons.Length)];
    }
}

public class Program
{
    public static async Task Main(string[] args)
    {
        // Configure Serilog for structured logging
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File("logs/exercise41-metrics-.log", rollingInterval: RollingInterval.Day)
            .Enrich.WithProperty("Exercise", "41")
            .Enrich.WithProperty("Component", "MetricsCollection")
            .CreateLogger();

        Console.WriteLine("🚀 Day 4 Exercise 4.1: Netflix-Style Enterprise Metrics Collection");
        Console.WriteLine("=================================================================");
        Console.WriteLine("📊 Implementing The Four Golden Signals of Monitoring");
        Console.WriteLine("🎯 Netflix Production Patterns: 200M users, 99.97% availability SLO");
        Console.WriteLine("📈 Grafana Dashboard: http://localhost:18010");
        Console.WriteLine("🔍 Prometheus Metrics: http://localhost:18006");
        Console.WriteLine("");

        // Main application setup and execution
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                // Add Netflix-style metrics service
                services.AddSingleton<NetflixStyleMetricsService>();
                
                // Configure OpenTelemetry with enterprise resource attributes
                services.AddOpenTelemetry()
                    .ConfigureResource(resource => resource
                        .AddService("FlinkDotNet.Exercise41.MetricsCollection", "1.0.0")
                        .AddAttributes(new Dictionary<string, object>
                        {
                            ["deployment.environment"] = "local-testing",
                            ["service.namespace"] = "flinkdotnet.day04",
                            ["service.instance.id"] = Environment.MachineName,
                            ["company.pattern"] = "netflix",
                            ["monitoring.signals"] = "four_golden_signals"
                        }))
                    .WithMetrics(metrics => metrics
                        .AddMeter("FlinkDotNet.Exercise41.Netflix")
                        .AddConsoleExporter()
                        .AddOtlpExporter(options =>
                        {
                            options.Endpoint = new Uri("http://localhost:18009");
                            options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                        }));
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddSerilog();
            })
            .UseSerilog()
            .Build();

        try
        {
            Log.Information("Starting Netflix-style enterprise metrics collection");
            
            var metricsService = host.Services.GetRequiredService<NetflixStyleMetricsService>();
            await metricsService.RunNetflixMetricsDemo();
            
            Log.Information("Netflix-style enterprise metrics collection completed successfully");
            Console.WriteLine("");
            Console.WriteLine("📊 Metrics Collection Summary:");
            Console.WriteLine("   🎯 Four Golden Signals: Latency, Traffic, Errors, Saturation");
            Console.WriteLine("   📈 Netflix Production Patterns: 200M users, 99.97% availability");
            Console.WriteLine("   🔍 Real Industry Metrics: Content delivery, user engagement, infrastructure");
            Console.WriteLine("   📊 View metrics at: http://localhost:18006 (Prometheus)");
            Console.WriteLine("   📈 Dashboard at: http://localhost:18010 (Grafana)");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Netflix-style metrics collection failed");
            Console.WriteLine($"Error: {ex.Message}");
            Environment.ExitCode = 1;
        }
        finally
        {
            await host.StopAsync();
            await Log.CloseAndFlushAsync();
        }
    }
}
