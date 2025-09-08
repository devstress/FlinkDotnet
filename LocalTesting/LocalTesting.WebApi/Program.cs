using LocalTesting.WebApi.Configuration;
using LocalTesting.WebApi.Services;
using LocalTesting.WebApi.Services.Temporal;
using FlinkDotNet.Orchestration.Interfaces;
using FlinkDotNet.Orchestration.Services;
using FlinkDotNet.Orchestration.Models;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using System.Diagnostics.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Configure IPv4-only binding compatible with Aspire orchestration
// Use port 13001 (13000+ range as required)
builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(System.Net.IPAddress.Parse("127.0.0.1"), 13001); // Internal port for Aspire
});

// Configure Flink job management defaults
builder.Configuration["Flink:UseFlinkDotNet"] = "true"; // Default to FlinkDotNet

// Configure OpenTelemetry with local collector pattern for high-performance
// Pattern: WebAPI → local OTel Collector (service discovery) → backend observability stack
var otlpTracesEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT") ?? "http://otel-collector:4317";
var otlpMetricsEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://otel-collector:4318";

Console.WriteLine($"📊 OpenTelemetry Configuration:");
Console.WriteLine($"   • Traces Endpoint: {otlpTracesEndpoint}");
Console.WriteLine($"   • Metrics Endpoint: {otlpMetricsEndpoint}");

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("LocalTesting.WebApi")
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = "local-testing",
            ["service.version"] = "1.0.0",
            ["observability.pattern"] = "local-collector"
        }))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options =>
        {
            // Use environment-configured collector endpoint for traces (gRPC)
            options.Endpoint = new Uri(otlpTracesEndpoint);
            options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMeter("FlinkDotNet.Kafka")
        .AddMeter("FlinkDotNet.Flink") 
        .AddMeter("FlinkDotNet.Temporal")
        .AddMeter("FlinkDotNet.Flow")
        .AddOtlpExporter(options =>
        {
            // Use environment-configured collector endpoint for metrics (HTTP)
            options.Endpoint = new Uri(otlpMetricsEndpoint);
            options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
        }))
    .WithLogging(logging => logging
        .AddOtlpExporter(options =>
        {
            // Use environment-configured collector endpoint for logs (HTTP)  
            options.Endpoint = new Uri(otlpMetricsEndpoint);
            options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
        }));

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Use PascalCase property names to match test expectations
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "LocalTesting API - Complex Logic Stress Test Interactive Interface", 
        Version = "v1",
        Description = "Interactive API for debugging and executing Complex Logic Stress Test scenarios step by step. " +
                     "This API transforms BDD test scenarios into executable endpoints for local testing and debugging."
    });
    c.EnableAnnotations();
});

// Add Redis connection service - individual services will connect when needed
builder.Services.AddSingleton<IRedisConnectionService, RedisConnectionService>();

// Add Redis connection as a singleton that doesn't connect during startup
builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    // Don't establish connection during startup - let individual services handle this
    var connectionString = builder.Configuration.GetConnectionString("redis") ?? "localhost:6379";
    var configOptions = ConfigurationOptions.Parse(connectionString);
    configOptions.ConnectTimeout = 5000;
    configOptions.AbortOnConnectFail = false; // Critical: don't fail startup if Redis unavailable
    
    // This will only connect when first accessed, not during registration
    return ConnectionMultiplexer.Connect(configOptions);
});

// Add custom services
// Replace synchronous observability with high-performance async buffered service
builder.Services.AddSingleton<AsyncBufferedObservabilityService>();
// Keep existing service for backward compatibility during transition
builder.Services.AddSingleton<ObservabilityMetricsService>();

// Configure HTTP client for Prometheus with enhanced timeouts for infrastructure delays
builder.Services.AddHttpClient<PrometheusMetricsService>(client =>
{
    var prometheusUrl = Environment.GetEnvironmentVariable("PROMETHEUS_URL") ?? "http://prometheus:9090";
    client.BaseAddress = new Uri(prometheusUrl);
    client.Timeout = TimeSpan.FromSeconds(30); // Increased timeout for infrastructure delays
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
    // Configure for container networking reliability
    MaxConnectionsPerServer = 10,
    UseCookies = false
});
builder.Services.AddSingleton<IMessageStateService, MessageStateService>();
builder.Services.AddSingleton<AspireHealthCheckService>();
builder.Services.AddSingleton<ComplexLogicStressTestService>();
builder.Services.AddSingleton<SecurityTokenManagerService>();
builder.Services.AddSingleton<TemporalSecurityTokenService>();
builder.Services.AddSingleton<KafkaProducerService>();
builder.Services.AddSingleton<FlinkJobManagementService>();
builder.Services.AddSingleton<BackpressureMonitoringService>();

// Add Infrastructure Readiness and Prometheus Warmup services (Phase 1 & Phase 2 implementation)
builder.Services.AddSingleton<IInfrastructureReadinessService, InfrastructureReadinessService>();
builder.Services.AddSingleton<PrometheusWarmupService>();
builder.Services.AddHttpClient<InfrastructureReadinessService>(); // HTTP client for infrastructure connectivity checks

// Configure strongly-typed configuration sections
builder.Services.Configure<TemporalConfiguration>(
    builder.Configuration.GetSection(TemporalConfiguration.SectionName));

// Add Adaptive Parameters and Temporal Optimization services (Phase 3 & Phase 4 implementation)
builder.Services.AddSingleton<ISystemCapacityDetector, SystemCapacityDetector>();
builder.Services.AddSingleton<ITemporalAgentOptimizer, TemporalAgentOptimizer>();
builder.Services.AddHttpClient<SystemCapacityDetector>(); // HTTP client for infrastructure API calls
builder.Services.AddHttpClient<TemporalAgentOptimizer>(); // HTTP client for Temporal API calls

// Add orchestration services for latest architecture
builder.Services.AddSingleton<IFlinkOrchestra, FlinkOrchestra>();

// Add Orchestra background service for non-blocking initialization
builder.Services.AddHostedService<OrchestraInitializationService>();

// Add HTTP client for external calls with extended timeout for complex operations
builder.Services.AddHttpClient().ConfigureHttpClientDefaults(clientBuilder =>
{
    clientBuilder.ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromMinutes(3); // 3 minutes for complex operations
    });
});

try
{
    var app = builder.Build();

    // Orchestra initialization is now handled by OrchestraInitializationService background service
    // This allows the application to start immediately without waiting for Orchestra setup

    // Configure the HTTP request pipeline
    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LocalTesting API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at app's root
        c.DocumentTitle = "LocalTesting - Complex Logic Stress Test Interface";
        c.DefaultModelsExpandDepth(-1);
        c.DefaultModelExpandDepth(2);
    });

    app.UseAuthorization();

    // Add simple health check endpoint for LocalTesting
    app.MapGet("/health", () => Results.Ok(new { 
        Status = "Healthy", 
        Timestamp = DateTime.UtcNow, 
        Service = "LocalTesting WebAPI",
        Version = "1.0.0" 
    }));

    app.MapControllers();

    Console.WriteLine("Starting LocalTesting WebAPI application...");
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"CRITICAL ERROR: Application startup failed: {ex}");
    Environment.Exit(1);
}

// Orchestra initialization is now handled by OrchestraInitializationService background service
// All Orchestra-related initialization logic has been moved to that service to avoid blocking startup