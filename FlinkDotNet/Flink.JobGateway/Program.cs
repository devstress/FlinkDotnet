using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Flink.JobGateway.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.OpenApi.Models;

namespace Flink.JobGateway;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        ConfigureServices(builder);
        var app = builder.Build();
        ConfigurePipeline(app);
        await app.RunAsync();
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

        builder.Services.AddHttpClient<IFlinkJobManager, FlinkJobManager>();
        builder.Services.AddLogging(lb => { lb.AddConsole(); lb.AddDebug(); });
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
        if (isSubmit && ctx.Response.StatusCode == 400)
        {
            mem.Position = 0;
            var bodyText = await new StreamReader(mem).ReadToEndAsync();
            ctx.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("JobSubmitModelState")
                .LogWarning("Job submission returned 400. Response body: {Body}", bodyText);
            mem.Position = 0;
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
    public void OnActionExecuted(ActionExecutedContext context) { }
}
