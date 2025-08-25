using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/day1-production-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Host.UseSerilog();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("Application is running"))
    .AddCheck("flink", () => 
    {
        try
        {
            // Simulate Flink health check
            return HealthCheckResult.Healthy("Flink cluster is accessible");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Flink cluster error: {ex.Message}");
        }
    })
    .AddCheck("streaming", () =>
    {
        // Simulate streaming health check
        return HealthCheckResult.Healthy("Stream processing is active");
    });

// Add Prometheus metrics
builder.Services.AddSingleton(Metrics.DefaultRegistry);

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add Prometheus metrics middleware
app.UseMetricServer();
app.UseHttpMetrics();

// Configure health checks with detailed JSON response
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        var response = new
        {
            Status = report.Status.ToString(),
            Timestamp = DateTime.UtcNow,
            Duration = report.TotalDuration,
            Checks = report.Entries.Select(e => new
            {
                Name = e.Key,
                Status = e.Value.Status.ToString(),
                Description = e.Value.Description,
                Duration = e.Value.Duration,
                Exception = e.Value.Exception?.Message,
                Data = e.Value.Data
            })
        };

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }
});

// Comprehensive health endpoint
app.MapHealthChecks("/health/comprehensive", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = async (context, report) =>
    {
        var response = new
        {
            Status = report.Status.ToString(),
            Timestamp = DateTime.UtcNow,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            Version = "1.0.0",
            Uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime,
            Machine = Environment.MachineName,
            Checks = report.Entries.Select(e => new
            {
                Name = e.Key,
                Status = e.Value.Status.ToString(),
                Description = e.Value.Description,
                Duration = e.Value.Duration,
                Exception = e.Value.Exception?.Message,
                Data = e.Value.Data
            }),
            SystemInfo = new
            {
                ProcessorCount = Environment.ProcessorCount,
                WorkingSet = Environment.WorkingSet,
                GCTotalMemory = GC.GetTotalMemory(false)
            }
        };

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }
});

// Metrics endpoint
app.MapGet("/metrics", async context =>
{
    var metrics = new
    {
        Timestamp = DateTime.UtcNow,
        Application = new
        {
            Name = "Day1ProductionApp",
            Version = "1.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        },
        System = new
        {
            ProcessorCount = Environment.ProcessorCount,
            WorkingSet = Environment.WorkingSet,
            TotalMemory = GC.GetTotalMemory(false),
            Uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime
        },
        StreamProcessing = new
        {
            Status = "Active",
            EventsProcessed = Random.Shared.Next(1000, 10000),
            Throughput = $"{Random.Shared.Next(100, 1000)} events/sec",
            LastProcessed = DateTime.UtcNow.AddSeconds(-Random.Shared.Next(1, 30))
        }
    };

    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(JsonSerializer.Serialize(metrics, new JsonSerializerOptions
    {
        WriteIndented = true
    }));
});

// Streaming simulation endpoint
app.MapPost("/stream/start", async (HttpContext context) =>
{
    Log.Information("Starting stream processing simulation");
    
    // Simulate stream processing
    _ = Task.Run(async () =>
    {
        var eventCounter = Metrics.CreateCounter("events_processed_total", "Total number of events processed");
        var processingDuration = Metrics.CreateHistogram("event_processing_duration_seconds", "Event processing duration");
        
        while (true)
        {
            using (processingDuration.NewTimer())
            {
                // Simulate event processing
                await Task.Delay(Random.Shared.Next(10, 100));
                eventCounter.Inc();
                
                if (Random.Shared.Next(1, 100) <= 5) // 5% chance of logging
                {
                    Log.Information("Processed event batch: {EventCount} events", Random.Shared.Next(10, 100));
                }
            }
            
            await Task.Delay(Random.Shared.Next(100, 1000));
        }
    });
    
    await context.Response.WriteAsync(JsonSerializer.Serialize(new
    {
        Status = "Started",
        Message = "Stream processing simulation started",
        Timestamp = DateTime.UtcNow
    }));
});

// Welcome endpoint
app.MapGet("/", () => new
{
    Application = "Day 1 Production Streaming Application",
    Version = "1.0.0",
    Status = "Running",
    Timestamp = DateTime.UtcNow,
    Endpoints = new
    {
        Health = "/health",
        ComprehensiveHealth = "/health/comprehensive",
        Metrics = "/metrics",
        StartStream = "POST /stream/start",
        PrometheusMetrics = "/metrics",
        Swagger = "/swagger"
    },
    Documentation = "This is the Day 1 production streaming application demonstrating Flink 2.0 integration patterns."
});

Log.Information("🚀 Day 1 Production Streaming Application starting...");
Log.Information("📊 Health checks available at: /health and /health/comprehensive");
Log.Information("📈 Metrics available at: /metrics");
Log.Information("📚 API documentation at: /swagger");

await app.RunAsync();

await Log.CloseAndFlushAsync();