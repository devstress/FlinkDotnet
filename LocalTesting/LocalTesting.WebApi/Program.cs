using LocalTesting.WebApi.Configuration;
using LocalTesting.WebApi.Services;
using LocalTesting.WebApi.Services.Temporal;
using LocalTesting.Shared.Constants;
using FlinkDotNet.Orchestration.Interfaces;
using FlinkDotNet.Orchestration.Services;
using FlinkDotNet.Orchestration.Models;
using Prometheus;
using Confluent.Kafka; // WI27: Added for manual Kafka producer configuration

var builder = WebApplication.CreateBuilder(args);

// Force IPv4 preference in CI environments to avoid connection issues
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")))
{
    Environment.SetEnvironmentVariable("ASPNETCORE_PREFERIPV4", "true");
    Environment.SetEnvironmentVariable("DOTNET_SYSTEM_NET_SOCKETS_PREFERIPV4", "true");
}

// Configure IPv4-only binding compatible with Aspire orchestration
// Use port 8080 internally (standard ASP.NET Core default), exposed externally on 13001
builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(System.Net.IPAddress.Parse("127.0.0.1"), PortConstants.WebApiInternal); // Internal port for Aspire
});

// Configure Flink job management defaults
builder.Configuration["Flink:UseFlinkDotNet"] = "true"; // Default to FlinkDotNet

// Configure native Prometheus metrics endpoint
// Replaced OpenTelemetry with direct Prometheus metrics for simplified architecture
Console.WriteLine($"📊 Native Prometheus Configuration:");
Console.WriteLine($"   • Metrics Endpoint: /metrics");
Console.WriteLine($"   • Architecture: Direct scraping (no OpenTelemetry collector)");
Console.WriteLine($"   • User Requirement: Complete OTel removal with native metrics");

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
        Title = "LocalTesting API - Observability Testing Interface", 
        Version = "v1",
        Description = "Focused API for observability testing and infrastructure monitoring. " +
                     "Provides endpoints for metrics collection, infrastructure health checks, and workload execution validation."
    });
    c.EnableAnnotations();
});

// Add Redis connection service - individual services will connect when needed
builder.Services.AddSingleton<IRedisConnectionService, RedisConnectionService>();

// Add Kafka producer configuration manually for custom container
// WI27 FIX: Replace Aspire AddKafkaProducer with manual configuration since using custom container
builder.Services.AddSingleton<IProducer<string, string>>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    // WI27: Use kafka endpoint connection string from Aspire service discovery
    var bootstrapServers = configuration.GetConnectionString("kafka") ?? "localhost:9092";
    var producerConfig = new ProducerConfig
    {
        BootstrapServers = bootstrapServers,
        ClientId = "LocalTesting.WebApi.Producer",
        Acks = Acks.Leader,
        RetryBackoffMs = 1000,
        MessageTimeoutMs = 30000,
        EnableIdempotence = false // Simplified for LocalTesting
    };
    return new ProducerBuilder<string, string>(producerConfig).Build();
});

// Add Aspire Redis client integration - replaces manual StackExchange.Redis setup
builder.AddRedisClient("redis");

// Add custom services - REFINED: Only observability-focused services
// Replace synchronous observability with high-performance async buffered service
builder.Services.AddSingleton<AsyncBufferedObservabilityService>();
// Keep existing service for backward compatibility during transition
builder.Services.AddSingleton<ObservabilityMetricsService>();

// OPTIMIZED: Configure HTTP client for Prometheus with shorter timeouts for faster startup
builder.Services.AddHttpClient<PrometheusMetricsService>(client =>
{
    var prometheusUrl = Environment.GetEnvironmentVariable("PROMETHEUS_URL") ?? PortConstants.PrometheusUrl();
    client.BaseAddress = new Uri(prometheusUrl);
    client.Timeout = TimeSpan.FromSeconds(10); // Reduced from 30 for faster startup
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
    // Configure for container networking reliability with faster timeouts
    MaxConnectionsPerServer = 5, // Reduced from 10
    UseCookies = false
});
builder.Services.AddSingleton<IMessageStateService, MessageStateService>();
builder.Services.AddSingleton<AspireHealthCheckService>();
// REMOVED: ComplexLogicStressTestService - not needed for observability-only focus
// REMOVED: SecurityTokenManagerService - not needed for observability-only focus  
// REMOVED: TemporalSecurityTokenService - not needed for observability-only focus
builder.Services.AddSingleton<KafkaProducerService>();
// REMOVED: FlinkJobManagementService - not needed for observability-only focus
// REMOVED: BackpressureMonitoringService - not needed for observability-only focus

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

// REMOVED: Orchestra services - not needed for observability-only focus
// REMOVED: OrchestraInitializationService - not needed for observability-only focus

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

    // REMOVED: Orchestra initialization - not needed for observability-only focus

    // Configure the HTTP request pipeline
    
    // Add Prometheus metrics endpoint before other middleware
    app.UseRouting();
    app.UseHttpMetrics(); // Enable HTTP metrics collection
    app.MapMetrics(); // Expose /metrics endpoint
    
    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LocalTesting API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at app's root
        c.DocumentTitle = "LocalTesting - Observability Testing Interface";
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

    Console.WriteLine("Starting LocalTesting WebAPI application (Observability Focus)...");
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"CRITICAL ERROR: Application startup failed: {ex}");
    Environment.Exit(1);
}

// REMOVED: Orchestra-related initialization comments - focusing on observability endpoints only