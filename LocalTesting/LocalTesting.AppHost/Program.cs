using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LocalTesting.Shared.Constants;

var builder = DistributedApplication.CreateBuilder(args);

// Simple Aspire configuration - clean and maintainable
Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");
Environment.SetEnvironmentVariable("DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS", "true");

// Configure Aspire Dashboard required environment variables
Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://localhost:18888");
Environment.SetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_ENDPOINT_URL", "http://localhost:13323");
Environment.SetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_HTTP_ENDPOINT_URL", "http://localhost:13324");

// Optimize DCP for faster startup in test environments
Environment.SetEnvironmentVariable("ASPIRE_DCP_RESOURCE_TIMEOUT", "30");
Environment.SetEnvironmentVariable("ASPIRE_DCP_STARTUP_TIMEOUT", "120");

// Configure reasonable timeouts for infrastructure startup
builder.Services.Configure<HostOptions>(options =>
{
    options.StartupTimeout = TimeSpan.FromSeconds(120);
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});

Console.WriteLine("🚀 Starting LocalTesting infrastructure with clean, simple configuration...");

// Redis - simplified Aspire configuration with automatic port allocation
var redis = builder.AddRedis("redis");

// Kafka - Using Aspire.Hosting.Kafka for simplicity and reliability
var kafka = builder.AddKafka("kafka");

// Prometheus - essential observability (custom container - no official Aspire hosting available)
var prometheus = builder.AddContainer("prometheus", "prom/prometheus:latest")
    .WithHttpEndpoint(PortConstants.PrometheusExternal, PortConstants.PrometheusInternal, name: "prometheus")
    .WithBindMount("./prometheus-minimal.yml", "/etc/prometheus/prometheus.yml")
    .WithArgs("--config.file=/etc/prometheus/prometheus.yml", 
             $"--web.listen-address=0.0.0.0:{PortConstants.PrometheusInternal}");

// Flink JobManager - custom container (no official Aspire hosting available)
var flinkJobManager = builder.AddContainer("flink-jobmanager", "flink:2.1.0")
    .WithHttpEndpoint(PortConstants.FlinkJobManagerWebExternal, PortConstants.FlinkJobManagerWebInternal, name: "jobmanager-ui")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithArgs("jobmanager");

// Flink TaskManager - custom container (no official Aspire hosting available)
var flinkTaskManager = builder.AddContainer("flink-taskmanager", "flink:2.1.0")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithArgs("taskmanager")
    .WaitFor(flinkJobManager);

// LocalTesting Web API - core service
var localTestingApi = builder.AddProject<Projects.LocalTesting_WebApi>("localtesting-webapi")
    .WithReference(redis)
    .WithReference(kafka) // Aspire automatically provides connection configuration
    .WithEnvironment("FLINK_JOBMANAGER_URL", PortConstants.FlinkJobManagerUrl())
    .WithEnvironment("PROMETHEUS_URL", PortConstants.PrometheusUrl())
    .WithHttpEndpoint(PortConstants.WebApiExternal, PortConstants.WebApiInternal, name: "webapi")
    .WaitFor(redis)
    .WaitFor(kafka)
    .WaitFor(flinkTaskManager)
    .WaitFor(prometheus);

// Simple startup
try
{
    Console.WriteLine("🚀 Starting simplified LocalTesting infrastructure...");
    var app = builder.Build();
    await app.RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Startup failed: {ex.Message}");
    throw;
}
