using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using FlinkDotNet.JobGateway.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
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
        // Configure Serilog early for startup logging
        var logFilePath = Environment.GetEnvironmentVariable("LOG_FILE_PATH") ?? "test-logs";
        // To achieve FlinkDotNet.JobGateway.log.YYYYMMDD pattern:
        // Use the filename with explicit date, and let Serilog handle it with Infinite rolling
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var logFile = Path.Combine(logFilePath, $"FlinkDotNet.JobGateway.log.{today}");

        // Clean up old log files (older than 1 day)
        try
        {
            if (Directory.Exists(logFilePath))
            {
                var logFiles = Directory.GetFiles(logFilePath, "FlinkDotNet.JobGateway.log.*");
                foreach (var file in logFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTimeUtc < DateTime.UtcNow.AddDays(-1))
                    {
                        File.Delete(file);
                    }
                }
            }
        }
        catch
        {
            // Ignore cleanup errors
        }

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(
                logFile,
                rollingInterval: RollingInterval.Infinite,
                rollOnFileSizeLimit: false,
                shared: true,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            Log.Information("=== Gateway Starting ===");
            Log.Information("LOG_FILE_PATH: {LogPath}", logFilePath);
            Log.Information("Log file: {LogFile}", logFile);
            Log.Information("FLINK_CLUSTER_HOST: {Host}", Environment.GetEnvironmentVariable("FLINK_CLUSTER_HOST"));
            Log.Information("FLINK_CLUSTER_PORT: {Port}", Environment.GetEnvironmentVariable("FLINK_CLUSTER_PORT"));
            Log.Information("KAFKA_BOOTSTRAP: {Kafka}", Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP"));

            // Check for Aspire service discovery variables
            var aspireFlinkEndpoint = Environment.GetEnvironmentVariable("services__flink-jobmanager__http__0");
            Log.Information("Aspire Flink endpoint: {Endpoint}", aspireFlinkEndpoint ?? "NOT SET");

            var builder = WebApplication.CreateBuilder(args);

            // Use Serilog for ASP.NET Core logging
            builder.Host.UseSerilog();

            ConfigureServices(builder);
            var app = builder.Build();
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
        builder.Services
            .AddControllers(options => options.Filters.Add<ModelStateLoggingFilter>())
            .AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                o.JsonSerializerOptions.WriteIndented = false;
                o.JsonSerializerOptions.TypeInfoResolverChain.Insert(0, new DefaultJsonTypeInfoResolver());
            });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Flink Job Gateway API",
                Version = "v1",
                Description = "REST API for submitting and managing Apache Flink jobs from .NET applications"
            });
        });

        builder.Services.AddApiVersioning(options =>
        {
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.DefaultApiVersion = new ApiVersion(1, 0);
        });
        builder.Services.AddVersionedApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        // Register FlinkJobManager as singleton to preserve job tracking across requests
        // The in-memory _jobMapping dictionary must persist for LOCAL mode jobs
        builder.Services.AddHttpClient(nameof(FlinkJobManager));
        builder.Services.AddSingleton<IFlinkJobManager>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(nameof(FlinkJobManager));
            var logger = sp.GetRequiredService<ILogger<FlinkJobManager>>();
            var configuration = sp.GetRequiredService<IConfiguration>();
            return new FlinkJobManager(logger, configuration, httpClient);
        });
        
        // Register MetricsService as singleton for persistent metrics across requests
        // Prometheus metrics are configured via appsettings.json (similar to Flink's approach)
        var metricsEnabled = builder.Configuration.GetValue<bool>("Metrics:Prometheus:Enabled");
        
        if (metricsEnabled)
        {
            Log.Information("Prometheus metrics ENABLED (configured in appsettings)");
            builder.Services.AddSingleton<MetricsService>();
        }
        else
        {
            Log.Information("Prometheus metrics DISABLED (configured in appsettings)");
        }
        
        // Logging is now configured via Serilog in Main()
    }

    private static void ConfigurePipeline(WebApplication app)
    {
        app.Use(BodyLoggingMiddleware);

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Flink Job Gateway API v1");
                c.RoutePrefix = string.Empty;
            });
        }

        // Enable Prometheus metrics endpoint based on configuration (similar to Flink's metrics.reporters)
        var metricsEnabled = app.Configuration.GetValue<bool>("Metrics:Prometheus:Enabled");
        
        if (metricsEnabled)
        {
            app.UseMetricServer();
            app.UseHttpMetrics();
            var metricsPath = app.Configuration.GetValue<string>("Metrics:Prometheus:Path") ?? "/metrics";
            Log.Information("Prometheus metrics endpoint enabled at {Path} (configured via appsettings)", metricsPath);
        }

        app.UseAuthorization();
        app.MapControllers();
        app.MapGet("/health", () => Results.Ok("OK"));
        app.MapGet("/api/v1/health", () => Results.Ok(new { status = "OK", timestamp = DateTime.UtcNow }));
    }

    private static async Task BodyLoggingMiddleware(HttpContext ctx, Func<Task> next)
    {
        var isSubmit = ctx.Request.Path.Equals("/api/v1/jobs/submit", StringComparison.OrdinalIgnoreCase);
        if (isSubmit)
        {
            try
            {
                ctx.Request.EnableBuffering();
                using var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8, leaveOpen: true);
                var raw = await reader.ReadToEndAsync();
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

        var originalBody = ctx.Response.Body;
        using var mem = new MemoryStream();
        ctx.Response.Body = mem;
        await next();

        // CRITICAL FIX: Reset memory stream position BEFORE copying back
        // The controller wrote to the stream, so position is at the end
        // We must reset to 0 to copy the full response
        mem.Position = 0;

        if (isSubmit && ctx.Response.StatusCode == 400)
        {
            var bodyText = await new StreamReader(mem).ReadToEndAsync();
            ctx.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("JobSubmitModelState")
                .LogWarning("Job submission returned 400. Response body: {Body}", bodyText);
            mem.Position = 0; // Reset again after reading for logging
        }

        await mem.CopyToAsync(originalBody);
        ctx.Response.Body = originalBody;
    }
}

internal sealed class ModelStateLoggingFilter : IActionFilter
{
    private readonly ILogger<ModelStateLoggingFilter> _logger;
    public ModelStateLoggingFilter(ILogger<ModelStateLoggingFilter> logger) => _logger = logger;
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(kv => kv.Value?.Errors.Count > 0)
                .Select(kv => $"{kv.Key}:{string.Join("|", kv.Value!.Errors.Select(e => e.ErrorMessage))}");
            _logger.LogWarning("ModelState invalid for {Path}. Errors: {Errors}",
                context.HttpContext.Request.Path,
                string.Join("; ", errors));
        }
    }
    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
