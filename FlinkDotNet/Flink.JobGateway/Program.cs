using Flink.JobGateway.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
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

// Add API versioning
builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Register services
builder.Services.AddHttpClient<IFlinkJobManager, FlinkJobManager>((serviceProvider, httpClient) =>
{
    // Get Flink cluster configuration from environment or use defaults
    var flinkClusterHost = Environment.GetEnvironmentVariable("FLINK_CLUSTER_HOST") ?? "flink-jobmanager";
    var flinkClusterPort = int.Parse(Environment.GetEnvironmentVariable("FLINK_CLUSTER_PORT") ?? "8081");
    
    // Configure HTTP client for Flink REST API
    var flinkBaseUrl = $"http://{flinkClusterHost}:{flinkClusterPort}";
    httpClient.BaseAddress = new Uri(flinkBaseUrl);
    httpClient.Timeout = TimeSpan.FromMinutes(5);
    
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Configuring HttpClient for Flink cluster at: {FlinkBaseUrl}", flinkBaseUrl);
});

// Configure logging
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConsole();
    loggingBuilder.AddDebug();
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Flink Job Gateway API v1");
        c.RoutePrefix = string.Empty; // Make Swagger UI the default page
    });
}

app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok("OK"));
app.MapGet("/api/v1/health", () => Results.Ok(new { status = "OK", timestamp = DateTime.UtcNow }));

await app.RunAsync();
