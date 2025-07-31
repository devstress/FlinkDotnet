using LocalTesting.WebApi.Services;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("LocalTesting.WebApi")
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = "local-testing",
            ["service.version"] = "1.0.0"
        }))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithLogging(logging => logging
        .AddOtlpExporter());

// Add services to the container
builder.Services.AddControllers();
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

// Add Redis connection
builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    var connectionString = builder.Configuration.GetConnectionString("redis") ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(connectionString);
});

// Add custom services
builder.Services.AddSingleton<AspireHealthCheckService>();
builder.Services.AddSingleton<ComplexLogicStressTestService>();
builder.Services.AddSingleton<SecurityTokenManagerService>();
builder.Services.AddSingleton<KafkaProducerService>();
builder.Services.AddSingleton<FlinkJobManagementService>();
builder.Services.AddSingleton<BackpressureMonitoringService>();

// Add HTTP client for external calls
builder.Services.AddHttpClient();

var app = builder.Build();

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
app.MapControllers();

app.Run();