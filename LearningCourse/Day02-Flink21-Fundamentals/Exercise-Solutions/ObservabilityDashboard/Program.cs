using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/observability-dashboard-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

Log.Information("📊 Starting Day 1 Observability Dashboard Service");

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Host.UseSerilog();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add health checks for observability
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("Observability dashboard is running"))
    .AddCheck("prometheus", () => 
    {
        // Simulate Prometheus connectivity check
        return HealthCheckResult.Healthy("Prometheus is scraping metrics");
    })
    .AddCheck("grafana", () => 
    {
        // Simulate Grafana dashboard check
        return HealthCheckResult.Healthy("Grafana dashboards are accessible");
    })
    .AddCheck("loki", () =>
    {
        // Simulate Loki log aggregation check
        return HealthCheckResult.Healthy("Loki is collecting logs");
    });

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable Prometheus metrics
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

// Observability dashboard endpoints
app.MapGet("/dashboard/metrics", () => 
{
    Log.Information("Serving observability metrics dashboard");
    var metrics = new
    {
        timestamp = DateTime.UtcNow,
        system = new
        {
            cpu_usage = Random.Shared.NextDouble() * 100,
            memory_usage = Random.Shared.NextDouble() * 100,
            disk_usage = Random.Shared.NextDouble() * 100
        },
        kafka = new
        {
            messages_per_second = Random.Shared.Next(1000, 5000),
            consumer_lag = Random.Shared.Next(0, 100),
            broker_count = 3
        },
        flink = new
        {
            jobs_running = Random.Shared.Next(1, 5),
            task_managers = 3,
            backpressure = Random.Shared.NextDouble() * 10
        }
    };
    return Results.Ok(metrics);
});

app.MapGet("/dashboard/logs", () => 
{
    Log.Information("Serving observability logs dashboard");
    var logs = new
    {
        timestamp = DateTime.UtcNow,
        recent_events = new[]
        {
            new { level = "INFO", message = "Kafka producer connected", timestamp = DateTime.UtcNow.AddMinutes(-1) },
            new { level = "INFO", message = "Flink job started successfully", timestamp = DateTime.UtcNow.AddMinutes(-2) },
            new { level = "WARN", message = "High memory usage detected", timestamp = DateTime.UtcNow.AddMinutes(-3) }
        }
    };
    return Results.Ok(logs);
});

Log.Information("📈 Observability Dashboard Service started on port {Port}", 5001);
await app.RunAsync();