using System.IO.Abstractions;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using FlinkDotNet.JobGateway.Filters;
using FlinkDotNet.JobGateway.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Prometheus;
using Serilog;

namespace FlinkDotNet.JobGateway;

/// <summary>
/// Main program entry point for the Flink Job Gateway API.
/// </summary>
public class Program
{
    /// <summary>
    /// Protected constructor for WebApplicationFactory testing.
    /// </summary>
    protected Program()
    {
    }
    /// <summary>
    /// Main entry point for the application. Configures logging, services, and HTTP pipeline.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    public static async Task Main(string[] args)
    {
        // Configure Serilog early for startup logging using shared LoggerFactory
        Log.Logger = FlinkDotNet.Common.Logging.LoggerFactory.CreateLogger(new FileSystem(), "FlinkDotNet.JobGateway.log");

        try
        {
            string logFilePath = Environment.GetEnvironmentVariable("LOG_FILE_PATH") ?? "test-logs";
            string today = DateTime.UtcNow.ToString("yyyyMMdd");
            string logFile = Path.Combine(logFilePath, $"FlinkDotNet.JobGateway.log.{today}");

            Log.Information("=== Gateway Starting === LOG_FILE_PATH: {LogPath}, Log file: {LogFile}, FLINK_CLUSTER_HOST={Host}, FLINK_CLUSTER_PORT={Port}, KAFKA_BOOTSTRAP={Kafka}",
                logFilePath, logFile,
                Environment.GetEnvironmentVariable("FLINK_CLUSTER_HOST"),
                Environment.GetEnvironmentVariable("FLINK_CLUSTER_PORT"),
                Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP"));

            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            // Use Serilog for ASP.NET Core logging
            _ = builder.Host.UseSerilog();

            ConfigureServices(builder);
            WebApplication app = builder.Build();
            ConfigurePipeline(app);

            Log.Information("Gateway configured, starting web server...");
            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Gateway failed to start");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        _ = builder.Services
            .AddControllers(options => options.Filters.Add<ModelStateLoggingFilter>())
            .AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                o.JsonSerializerOptions.WriteIndented = false;
                o.JsonSerializerOptions.TypeInfoResolverChain.Insert(0, new DefaultJsonTypeInfoResolver());
            });

        _ = builder.Services.AddEndpointsApiExplorer();
        _ = builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Flink Job Gateway API",
            Version = "v1",
            Description = "REST API for submitting and managing Apache Flink jobs from .NET applications"
        }));

        _ = builder.Services.AddApiVersioning(options =>
        {
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.DefaultApiVersion = new ApiVersion(1, 0);
        });
        _ = builder.Services.AddVersionedApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        // Register FlinkJobManager as singleton to preserve job tracking across requests
        // The in-memory _jobMapping dictionary must persist for LOCAL mode jobs
        _ = builder.Services.AddHttpClient(nameof(FlinkJobManager));
        _ = builder.Services.AddSingleton<IFlinkJobManager>(sp =>
        {
            IHttpClientFactory httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            HttpClient httpClient = httpClientFactory.CreateClient(nameof(FlinkJobManager));
            ILogger<FlinkJobManager> logger = sp.GetRequiredService<ILogger<FlinkJobManager>>();
            IConfiguration configuration = sp.GetRequiredService<IConfiguration>();
            return new FlinkJobManager(logger, configuration, httpClient);
        });

        // Register MetricsService as singleton for persistent metrics across requests
        // Prometheus metrics are configured via appsettings.json (similar to Flink's approach)
        bool metricsEnabled = builder.Configuration.GetValue<bool>("Metrics:Prometheus:Enabled");

        if (metricsEnabled)
        {
            Log.Information("Prometheus metrics ENABLED (configured in appsettings)");
            _ = builder.Services.AddSingleton<MetricsService>();
        }
        else
        {
            Log.Information("Prometheus metrics DISABLED (configured in appsettings)");
        }

        // Logging is now configured via Serilog in Main()
    }

    private static void ConfigurePipeline(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            _ = app.UseSwagger();
            _ = app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Flink Job Gateway API v1");
                c.RoutePrefix = string.Empty;
            });
        }

        // Enable Prometheus metrics endpoint based on configuration (similar to Flink's metrics.reporters)
        bool metricsEnabled = app.Configuration.GetValue<bool>("Metrics:Prometheus:Enabled");

        if (metricsEnabled)
        {
            _ = app.UseMetricServer();
            _ = app.UseHttpMetrics();
            string metricsPath = app.Configuration.GetValue<string>("Metrics:Prometheus:Path") ?? "/metrics";
            Log.Information("Prometheus metrics endpoint enabled at {Path} (configured via appsettings)", metricsPath);
        }

        _ = app.UseRouting();
        _ = app.Use(BodyLoggingMiddleware);  // Moved AFTER UseRouting so routing can match endpoints
        _ = app.UseAuthorization();
        _ = app.MapControllers();
        _ = app.MapGet("/health", () => Results.Ok("OK"));
        _ = app.MapGet("/api/v1/health", () => Results.Ok(new { status = "OK", timestamp = DateTime.UtcNow }));
    }

    private static async Task BodyLoggingMiddleware(HttpContext ctx, Func<Task> next)
    {
        bool isSubmit = ctx.Request.Path.Equals("/api/v1/jobs/submit", StringComparison.OrdinalIgnoreCase);
        if (isSubmit)
        {
            try
            {
                ctx.Request.EnableBuffering();
                using StreamReader reader = new(ctx.Request.Body, Encoding.UTF8, leaveOpen: true);
                string raw = await reader.ReadToEndAsync();
                ctx.Request.Body.Position = 0;
                ctx.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JobSubmitRawBody")
                    .LogInformation("Raw job submission body: {Body}", raw);
            }
            catch (Exception ex)
            {
                ctx.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JobSubmitRawBody")
                    .LogWarning(ex, "Failed to read raw submission body.");
            }
        }

        Stream originalBody = ctx.Response.Body;
        using MemoryStream mem = new();
        ctx.Response.Body = mem;
        await next();

        // CRITICAL FIX: Reset memory stream position BEFORE copying back
        // The controller wrote to the stream, so position is at the end
        // We must reset to 0 to copy the full response
        mem.Position = 0;

        if (isSubmit && ctx.Response.StatusCode == 400)
        {
            string bodyText = await new StreamReader(mem).ReadToEndAsync();
            ctx.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("JobSubmitModelState")
                .LogWarning("Job submission returned 400. Response body: {Body}", bodyText);
            mem.Position = 0; // Reset again after reading for logging
        }

        await mem.CopyToAsync(originalBody);
        ctx.Response.Body = originalBody;
    }
}
