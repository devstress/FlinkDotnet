using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using Serilog;
using Serilog.Context;

/// <summary>
/// Enterprise-grade distributed tracing implementation following Uber's microservice architecture patterns.
/// Demonstrates request flow tracing across multiple services with realistic latencies and error handling.
/// 
/// References:
/// - Uber Engineering Blog: Distributed Tracing at Scale
/// - OpenTelemetry Semantic Conventions
/// - Jaeger Best Practices for Enterprise Microservices
/// - Twitter's Real-time Pipeline Tracing Architecture
/// </summary>
public class UberStyleDistributedTracingService
{
    private static readonly ActivitySource ActivitySource = new("FlinkDotNet.Exercise42.DistributedTracing");
    private readonly ILogger<UberStyleDistributedTracingService> _logger;
    private readonly Random _deterministicRandom;

    // === UBER MICROSERVICE ARCHITECTURE SIMULATION ===
    // Uber's production architecture: 2000+ microservices handling 15M+ trips per day
    
    private readonly string[] _serviceNames = 
    {
        "api-gateway",        // Entry point - handles all external requests
        "user-service",       // User authentication and profile management
        "location-service",   // Real-time location tracking and geospatial operations
        "pricing-service",    // Dynamic pricing calculation (surge pricing)
        "driver-service",     // Driver matching and availability
        "trip-service",       // Trip lifecycle management
        "payment-service",    // Payment processing and billing
        "notification-service", // Push notifications and communications
        "analytics-service",  // Real-time analytics and business intelligence
        "fraud-service"       // Fraud detection and prevention
    };

    private readonly Dictionary<string, int> _serviceLatencies = new()
    {
        { "api-gateway", 15 },        // 15ms - Fast reverse proxy
        { "user-service", 45 },       // 45ms - Database lookup + validation
        { "location-service", 80 },   // 80ms - Geospatial calculations + GPS processing
        { "pricing-service", 120 },   // 120ms - Complex surge pricing algorithms
        { "driver-service", 200 },    // 200ms - Driver matching optimization
        { "trip-service", 85 },       // 85ms - Trip state management
        { "payment-service", 300 },   // 300ms - External payment provider integration
        { "notification-service", 25 }, // 25ms - Push notification queuing
        { "analytics-service", 150 }, // 150ms - Real-time data aggregation
        { "fraud-service", 95 }       // 95ms - ML-based fraud detection
    };

    // Uber's typical service dependencies for a trip request
    private readonly Dictionary<string, string[]> _serviceDependencies = new()
    {
        { "api-gateway", new string[] { "user-service", "location-service" } },
        { "user-service", new string[] { "fraud-service" } },
        { "location-service", new string[] { "driver-service", "pricing-service" } },
        { "pricing-service", new string[] { "analytics-service" } },
        { "driver-service", new string[] { "location-service", "analytics-service" } },
        { "trip-service", new string[] { "user-service", "driver-service", "payment-service" } },
        { "payment-service", new string[] { "fraud-service" } },
        { "notification-service", new string[] { } }, // No dependencies - async processing
        { "analytics-service", new string[] { } },    // No dependencies - data aggregation
        { "fraud-service", new string[] { } }         // No dependencies - ML inference
    };

    public UberStyleDistributedTracingService(ILogger<UberStyleDistributedTracingService> logger)
    {
        _logger = logger;
        // Deterministic random for consistent educational outcomes
        _deterministicRandom = new Random(DateTime.Now.Hour * 60 + DateTime.Now.Minute);
    }

    public async Task RunDistributedTracingDemo()
    {
        _logger.LogInformation("Starting Uber-style distributed tracing demonstration");
        _logger.LogInformation("Monitoring {ServiceCount} microservices: {Services}", 
            _serviceNames.Length, string.Join(", ", _serviceNames));
        
        using var mainActivity = ActivitySource.StartActivity("UberDistributedTracingDemo");
        mainActivity?.SetTag("demo.type", "distributed_tracing");
        mainActivity?.SetTag("company.pattern", "uber");
        mainActivity?.SetTag("architecture.type", "microservices");
        mainActivity?.SetTag("service.count", _serviceNames.Length);
        
        // Simulate various Uber scenarios with distributed tracing
        await SimulateUberTripRequest();
        await SimulateUberSurgePricingFlow();
        await SimulateUberPaymentProcessing();
        await SimulateUberRealTimeAnalytics();
        
        _logger.LogInformation("Uber-style distributed tracing demonstration completed");
    }

    /// <summary>
    /// Simulate a complete Uber trip request flow with distributed tracing
    /// Demonstrates how a single user request flows through multiple microservices
    /// </summary>
    private async Task SimulateUberTripRequest()
    {
        _logger.LogInformation("Simulating Uber trip request with distributed tracing...");
        
        using var tripActivity = ActivitySource.StartActivity("UberTripRequest");
        tripActivity?.SetTag("trip.type", "standard");
        tripActivity?.SetTag("user.city", "san_francisco");
        tripActivity?.SetTag("trace.scenario", "full_trip_lifecycle");
        
        var traceId = Guid.NewGuid().ToString("N")[..16]; // 16-character trace ID
        var userId = _deterministicRandom.Next(1000000, 9999999);
        var requestStartTime = DateTime.UtcNow;
        
        using (LogContext.PushProperty("TraceId", traceId))
        using (LogContext.PushProperty("UserId", userId))
        {
            _logger.LogInformation("Trip request initiated - TraceId: {TraceId}, UserId: {UserId}", traceId, userId);
            
            // === PHASE 1: Request Reception and Authentication ===
            await TraceServiceCall("api-gateway", traceId, "POST", "/api/v1/trips/request", 
                new { operation = "request_validation", user_id = userId });
            
            await TraceServiceCall("user-service", traceId, "GET", $"/api/users/{userId}/profile",
                new { operation = "user_authentication", validation_type = "jwt_token" });
            
            await TraceServiceCall("fraud-service", traceId, "POST", "/api/fraud/check",
                new { operation = "fraud_detection", user_risk_score = _deterministicRandom.NextDouble() * 100 });

            // === PHASE 2: Location Processing and Driver Matching ===
            var pickupLat = 37.7749 + (_deterministicRandom.NextDouble() - 0.5) * 0.1; // San Francisco area
            var pickupLng = -122.4194 + (_deterministicRandom.NextDouble() - 0.5) * 0.1;
            
            await TraceServiceCall("location-service", traceId, "POST", "/api/location/geocode",
                new { operation = "geocoding", pickup_lat = pickupLat, pickup_lng = pickupLng });
                
            await TraceServiceCall("driver-service", traceId, "POST", "/api/drivers/find",
                new { operation = "driver_matching", radius_km = 5, max_drivers = 10 });

            // === PHASE 3: Pricing and Trip Creation ===
            await TraceServiceCall("pricing-service", traceId, "POST", "/api/pricing/calculate",
                new { operation = "surge_pricing", base_price = 12.50, surge_multiplier = GetSurgeMultiplier() });
                
            await TraceServiceCall("analytics-service", traceId, "POST", "/api/analytics/demand",
                new { operation = "demand_analysis", area_id = "sf_downtown" });
            
            var tripId = Guid.NewGuid().ToString("N")[..12];
            await TraceServiceCall("trip-service", traceId, "POST", "/api/trips",
                new { operation = "trip_creation", trip_id = tripId, estimated_duration_minutes = 18 });

            // === PHASE 4: Payment Processing and Notifications ===
            await TraceServiceCall("payment-service", traceId, "POST", "/api/payments/authorize",
                new { operation = "payment_authorization", amount = 15.75, payment_method = "credit_card" });
                
            await TraceServiceCall("notification-service", traceId, "POST", "/api/notifications/send",
                new { operation = "trip_confirmation", notification_type = "push", channel = "mobile_app" });
            
            var totalLatency = (DateTime.UtcNow - requestStartTime).TotalMilliseconds;
            
            _logger.LogInformation("Trip request completed - TraceId: {TraceId}, TotalLatency: {Latency}ms, TripId: {TripId}", 
                traceId, totalLatency, tripId);
                
            tripActivity?.SetTag("trip.total_latency_ms", totalLatency);
            tripActivity?.SetTag("trip.id", tripId);
            tripActivity?.SetTag("trip.status", "confirmed");
        }
    }

    /// <summary>
    /// Simulate Uber's surge pricing calculation with distributed tracing
    /// Demonstrates complex business logic flow across analytics and pricing services
    /// </summary>
    private async Task SimulateUberSurgePricingFlow()
    {
        _logger.LogInformation("Simulating Uber surge pricing calculation with distributed tracing...");
        
        using var surgePricingActivity = ActivitySource.StartActivity("UberSurgePricingFlow");
        surgePricingActivity?.SetTag("pricing.type", "surge_calculation");
        surgePricingActivity?.SetTag("city", "new_york");
        
        var traceId = Guid.NewGuid().ToString("N")[..16];
        var area = "manhattan_midtown";
        
        using (LogContext.PushProperty("TraceId", traceId))
        using (LogContext.PushProperty("PricingArea", area))
        {
            _logger.LogInformation("Surge pricing calculation initiated - TraceId: {TraceId}, Area: {Area}", traceId, area);
            
            // Real-time demand analysis
            await TraceServiceCall("analytics-service", traceId, "GET", "/api/analytics/real-time-demand",
                new { operation = "demand_analysis", area_id = area, time_window_minutes = 15 });
            
            // Driver availability check
            await TraceServiceCall("driver-service", traceId, "GET", "/api/drivers/availability",
                new { operation = "supply_analysis", area_id = area, active_drivers_count = _deterministicRandom.Next(50, 200) });
            
            // Historical pricing data
            await TraceServiceCall("analytics-service", traceId, "GET", "/api/analytics/pricing-history",
                new { operation = "historical_analysis", area_id = area, lookback_hours = 24 });
            
            // Surge multiplier calculation
            var surgeMultiplier = GetSurgeMultiplier();
            await TraceServiceCall("pricing-service", traceId, "POST", "/api/pricing/surge/calculate",
                new { operation = "surge_calculation", base_multiplier = 1.0, calculated_multiplier = surgeMultiplier });
            
            // Price update propagation
            await TraceServiceCall("api-gateway", traceId, "POST", "/api/pricing/update",
                new { operation = "price_propagation", area_id = area, new_multiplier = surgeMultiplier });
            
            _logger.LogInformation("Surge pricing calculation completed - TraceId: {TraceId}, SurgeMultiplier: {Multiplier}x", 
                traceId, surgeMultiplier);
                
            surgePricingActivity?.SetTag("pricing.surge_multiplier", surgeMultiplier);
            surgePricingActivity?.SetTag("pricing.area", area);
        }
    }

    /// <summary>
    /// Simulate payment processing with distributed tracing
    /// Demonstrates error handling and retry logic in distributed systems
    /// </summary>
    private async Task SimulateUberPaymentProcessing()
    {
        _logger.LogInformation("Simulating Uber payment processing with distributed tracing...");
        
        using var paymentActivity = ActivitySource.StartActivity("UberPaymentProcessing");
        paymentActivity?.SetTag("payment.type", "trip_completion");
        paymentActivity?.SetTag("payment.provider", "stripe");
        
        var traceId = Guid.NewGuid().ToString("N")[..16];
        var tripId = Guid.NewGuid().ToString("N")[..12];
        var amount = 23.45m;
        
        using (LogContext.PushProperty("TraceId", traceId))
        using (LogContext.PushProperty("TripId", tripId))
        {
            _logger.LogInformation("Payment processing initiated - TraceId: {TraceId}, Amount: ${Amount}", traceId, amount);
            
            // Trip completion validation
            await TraceServiceCall("trip-service", traceId, "PUT", $"/api/trips/{tripId}/complete",
                new { operation = "trip_completion", final_amount = amount, duration_minutes = 22 });
            
            // Fraud check before payment
            await TraceServiceCall("fraud-service", traceId, "POST", "/api/fraud/payment-check",
                new { operation = "payment_fraud_check", amount = amount, risk_score = _deterministicRandom.NextDouble() * 100 });
            
            // Payment processing (simulate potential retry)
            var paymentSuccess = _deterministicRandom.NextDouble() > 0.1; // 90% success rate
            
            if (!paymentSuccess)
            {
                _logger.LogWarning("Payment failed, initiating retry - TraceId: {TraceId}", traceId);
                await TraceServiceCall("payment-service", traceId, "POST", "/api/payments/retry",
                    new { operation = "payment_retry", attempt = 1, retry_reason = "gateway_timeout" });
                paymentSuccess = true; // Assume retry succeeds
            }
            
            await TraceServiceCall("payment-service", traceId, "POST", "/api/payments/charge",
                new { operation = "payment_charge", amount = amount, status = paymentSuccess ? "success" : "failed" });
            
            // Analytics and notifications
            await TraceServiceCall("analytics-service", traceId, "POST", "/api/analytics/trip-completed",
                new { operation = "trip_analytics", trip_revenue = amount, payment_method = "credit_card" });
                
            await TraceServiceCall("notification-service", traceId, "POST", "/api/notifications/receipt",
                new { operation = "receipt_notification", notification_type = "email", template = "trip_receipt" });
            
            _logger.LogInformation("Payment processing completed - TraceId: {TraceId}, Status: {Status}", 
                traceId, paymentSuccess ? "Success" : "Failed");
                
            paymentActivity?.SetTag("payment.amount", amount);
            paymentActivity?.SetTag("payment.status", paymentSuccess ? "success" : "failed");
            paymentActivity?.SetTag("trip.id", tripId);
        }
    }

    /// <summary>
    /// Simulate real-time analytics processing with distributed tracing
    /// Demonstrates high-volume data processing patterns
    /// </summary>
    private async Task SimulateUberRealTimeAnalytics()
    {
        _logger.LogInformation("Simulating Uber real-time analytics with distributed tracing...");
        
        using var analyticsActivity = ActivitySource.StartActivity("UberRealTimeAnalytics");
        analyticsActivity?.SetTag("analytics.type", "real_time_dashboard");
        analyticsActivity?.SetTag("data.volume", "high");
        
        var traceId = Guid.NewGuid().ToString("N")[..16];
        
        using (LogContext.PushProperty("TraceId", traceId))
        {
            _logger.LogInformation("Real-time analytics processing initiated - TraceId: {TraceId}", traceId);
            
            // City-wide metrics aggregation
            await TraceServiceCall("analytics-service", traceId, "GET", "/api/analytics/city-metrics",
                new { operation = "city_aggregation", active_trips = _deterministicRandom.Next(5000, 15000) });
            
            // Driver performance analytics
            await TraceServiceCall("analytics-service", traceId, "GET", "/api/analytics/driver-performance",
                new { operation = "driver_analytics", active_drivers = _deterministicRandom.Next(2000, 8000) });
            
            // Revenue analytics
            await TraceServiceCall("analytics-service", traceId, "GET", "/api/analytics/revenue",
                new { operation = "revenue_analytics", hourly_revenue = _deterministicRandom.Next(50000, 200000) });
            
            // Demand prediction
            await TraceServiceCall("analytics-service", traceId, "POST", "/api/analytics/predict-demand",
                new { operation = "demand_prediction", prediction_horizon_minutes = 30 });
            
            _logger.LogInformation("Real-time analytics processing completed - TraceId: {TraceId}", traceId);
            
            analyticsActivity?.SetTag("analytics.processing_time_ms", _deterministicRandom.Next(100, 300));
            analyticsActivity?.SetTag("analytics.records_processed", _deterministicRandom.Next(10000, 50000));
        }
    }

    /// <summary>
    /// Trace a service call with realistic latency simulation and comprehensive logging
    /// </summary>
    private async Task TraceServiceCall(string serviceName, string traceId, string method, string endpoint, object payload)
    {
        using var serviceActivity = ActivitySource.StartActivity($"{serviceName}.{method}");
        serviceActivity?.SetTag("service.name", serviceName);
        serviceActivity?.SetTag("http.method", method);
        serviceActivity?.SetTag("http.url", endpoint);
        serviceActivity?.SetTag("trace.id", traceId);
        
        var startTime = DateTime.UtcNow;
        var baseLatency = _serviceLatencies.ContainsKey(serviceName) ? _serviceLatencies[serviceName] : 100;
        
        // Add realistic latency variation (±30%)
        var actualLatency = (int)(baseLatency * (0.7 + _deterministicRandom.NextDouble() * 0.6));
        
        using (LogContext.PushProperty("ServiceName", serviceName))
        using (LogContext.PushProperty("Method", method))
        using (LogContext.PushProperty("Endpoint", endpoint))
        {
            _logger.LogInformation("Service call started - {ServiceName} {Method} {Endpoint}", serviceName, method, endpoint);
            
            // Simulate service processing time
            await Task.Delay(actualLatency);
            
            var endTime = DateTime.UtcNow;
            var actualDuration = (endTime - startTime).TotalMilliseconds;
            
            // Simulate occasional service errors (5% error rate)
            var isError = _deterministicRandom.NextDouble() < 0.05;
            
            if (isError)
            {
                var errorType = GetRandomErrorType();
                _logger.LogError("Service call failed - {ServiceName} {Method} {Endpoint}, Error: {ErrorType}, Duration: {Duration}ms", 
                    serviceName, method, endpoint, errorType, actualDuration);
                    
                serviceActivity?.SetTag("error", true);
                serviceActivity?.SetTag("error.type", errorType);
                serviceActivity?.SetStatus(ActivityStatusCode.Error, errorType);
            }
            else
            {
                _logger.LogInformation("Service call completed - {ServiceName} {Method} {Endpoint}, Duration: {Duration}ms", 
                    serviceName, method, endpoint, actualDuration);
                    
                serviceActivity?.SetStatus(ActivityStatusCode.Ok);
            }
            
            serviceActivity?.SetTag("http.status_code", isError ? GetErrorStatusCode() : 200);
            serviceActivity?.SetTag("duration_ms", actualDuration);
            serviceActivity?.SetTag("payload.type", payload.GetType().Name);
        }
        
        // Simulate downstream service calls
        if (_serviceDependencies.ContainsKey(serviceName))
        {
            var dependencies = _serviceDependencies[serviceName];
            var dependenciesToCall = dependencies.Where(d => _deterministicRandom.NextDouble() < 0.3).Take(1);
            
            foreach (var dependency in dependenciesToCall) // 30% chance of downstream call, limit to 1
            {
                await TraceServiceCall(dependency, traceId, "GET", "/api/internal/data", 
                    new { operation = "dependency_call", parent_service = serviceName });
            }
        }
    }

    private double GetSurgeMultiplier()
    {
        var hour = DateTime.UtcNow.Hour;
        var baseMultiplier = 1.0;
        
        // Peak hours surge pricing
        if (hour >= 7 && hour <= 9) baseMultiplier = 1.8; // Morning rush
        else if (hour >= 17 && hour <= 19) baseMultiplier = 2.2; // Evening rush
        else if (hour >= 22 || hour <= 2) baseMultiplier = 1.5; // Late night
        
        // Add random variation
        return Math.Round(baseMultiplier * (0.8 + _deterministicRandom.NextDouble() * 0.4), 1);
    }

    private string GetRandomErrorType()
    {
        var errorTypes = new[] 
        { 
            "timeout", "service_unavailable", "rate_limit_exceeded", 
            "database_connection_failed", "external_api_error", "validation_error" 
        };
        return errorTypes[_deterministicRandom.Next(errorTypes.Length)];
    }

    private int GetErrorStatusCode()
    {
        var errorCodes = new[] { 500, 502, 503, 504, 429, 400 };
        return errorCodes[_deterministicRandom.Next(errorCodes.Length)];
    }
}

/// <summary>
/// Console application entry point demonstrating enterprise distributed tracing patterns
/// </summary>
class Program
{
    // === OBSERVABILITY ENDPOINTS - Environment Variable Pattern ===
    private static string OtelCollectorUrl =>
        Environment.GetEnvironmentVariable("OTEL_COLLECTOR_URL") ?? "http://localhost:18009";

    static async Task Main(string[] args)
    {
        // Configure comprehensive logging with Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: 
                "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File("logs/exercise42-distributed-tracing-.log", 
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .CreateLogger();

        Console.WriteLine("🚀 Day 4 Exercise 4.2: Uber-Style Distributed Tracing");
        Console.WriteLine("================================================================");
        Console.WriteLine("📊 Enterprise-grade distributed tracing with OpenTelemetry");
        Console.WriteLine("🏢 Uber-style microservice architecture patterns");
        Console.WriteLine("📈 Real-world service dependencies and latencies");
        Console.WriteLine("🔍 Aspire Dashboard: http://localhost:18888");
        Console.WriteLine("📊 OpenTelemetry Traces: http://localhost:18009");
        Console.WriteLine("");

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddSingleton<UberStyleDistributedTracingService>();
                
                // Configure OpenTelemetry tracing
                services.AddOpenTelemetry()
                    .WithTracing(builder =>
                    {
                        builder
                            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                                .AddService("flinkdotnet-exercise42", "1.0.0")
                                .AddAttributes(new Dictionary<string, object>
                                {
                                    { "service.type", "distributed-tracing-demo" },
                                    { "service.pattern", "uber-microservices" },
                                    { "environment", "learning" }
                                }))
                            .AddSource("FlinkDotNet.Exercise42.DistributedTracing")
                            .AddConsoleExporter()
                            .AddOtlpExporter(options =>
                            {
                                options.Endpoint = new Uri(OtelCollectorUrl);
                                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                            }); // For integration with observability stack
                    });
            })
            .UseSerilog()
            .Build();

        try
        {
            Log.Information("Starting Exercise 4.2: Distributed Tracing with Uber-style patterns");
            
            var tracingService = host.Services.GetRequiredService<UberStyleDistributedTracingService>();
            await tracingService.RunDistributedTracingDemo();
            
            Console.WriteLine("\n✅ Distributed tracing demonstration completed successfully!");
            Console.WriteLine("📋 Key learning outcomes:");
            Console.WriteLine("   • OpenTelemetry activity and span creation");
            Console.WriteLine("   • Service dependency tracing across microservices");
            Console.WriteLine("   • Realistic latency simulation and error handling");
            Console.WriteLine("   • Correlation ID propagation through request flows");
            Console.WriteLine("   • Production-grade observability patterns");
            Console.WriteLine("");
            Console.WriteLine("🔍 View traces at:");
            Console.WriteLine("   📊 Aspire Dashboard: http://localhost:18888");
            Console.WriteLine("   📈 OpenTelemetry Endpoint: http://localhost:18009");
            
            Log.Information("Exercise 4.2: Distributed Tracing completed successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in Exercise 4.2: Distributed Tracing");
            Console.WriteLine($"❌ Error: {ex.Message}");
            Environment.ExitCode = 1;
        }
        finally
        {
            await host.StopAsync();
            await Log.CloseAndFlushAsync();
        }
    }
}
