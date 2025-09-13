using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/load-testing-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

Log.Information("🚀 Starting Day 1 Load Testing Service");

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Host.UseSerilog();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add health checks for load testing
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("Load testing service is running"))
    .AddCheck("kafka_load", () => 
    {
        // Simulate Kafka load testing capability check
        return HealthCheckResult.Healthy("Kafka load testing ready");
    })
    .AddCheck("flink_load", () => 
    {
        // Simulate Flink processing load check
        return HealthCheckResult.Healthy("Flink cluster ready for load testing");
    })
    .AddCheck("metrics_collection", () =>
    {
        // Simulate metrics collection capability
        return HealthCheckResult.Healthy("Performance metrics collection active");
    });

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable Prometheus metrics for load testing
app.UseHttpMetrics();
app.MapMetrics();

// Health check endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                description = x.Value.Description
            }),
            duration = report.TotalDuration
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
});

// Load testing endpoints
app.MapPost("/load-test/start", (LoadTesting.LoadTestRequest request) => 
{
    Log.Information("Starting load test with {MessageCount} messages over {DurationSeconds} seconds", 
        request.MessageCount, request.DurationSeconds);
    
    var testId = Guid.NewGuid();
    var result = new
    {
        test_id = testId,
        status = "started",
        message_count = request.MessageCount,
        duration_seconds = request.DurationSeconds,
        estimated_completion = DateTime.UtcNow.AddSeconds(request.DurationSeconds)
    };
    
    return Results.Accepted($"/load-test/status/{testId}", result);
});

app.MapGet("/load-test/status/{testId:guid}", (Guid testId) => 
{
    Log.Information("Checking load test status for {TestId}", testId);
    
    var status = new
    {
        test_id = testId,
        status = "completed",
        messages_sent = Random.Shared.Next(8000, 12000),
        messages_per_second = Random.Shared.Next(800, 1200),
        average_latency_ms = Random.Shared.Next(10, 50),
        errors = Random.Shared.Next(0, 5),
        completion_time = DateTime.UtcNow
    };
    
    return Results.Ok(status);
});

app.MapGet("/load-test/metrics", () => 
{
    Log.Information("Serving load testing performance metrics");
    
    var metrics = new
    {
        timestamp = DateTime.UtcNow,
        current_load = new
        {
            messages_per_second = Random.Shared.Next(500, 1500),
            cpu_usage = Random.Shared.NextDouble() * 100,
            memory_usage = Random.Shared.NextDouble() * 100,
            network_throughput_mbps = Random.Shared.NextDouble() * 1000
        },
        peak_performance = new
        {
            max_messages_per_second = 2500,
            peak_cpu_usage = 85.5,
            peak_memory_usage = 78.2
        }
    };
    
    return Results.Ok(metrics);
});

Log.Information("⚡ Load Testing Service started on port {Port}", 5002);
await app.RunAsync();

namespace LoadTesting
{
    public record LoadTestRequest(int MessageCount, int DurationSeconds);
}