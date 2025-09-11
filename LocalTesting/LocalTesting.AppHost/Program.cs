using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LocalTesting.Shared.Constants;

// Configure Aspire environment variables BEFORE creating builder
Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");
Environment.SetEnvironmentVariable("DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS", "true");

// Force IPv4 preference for CI environments to avoid IPv6 connection issues
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")))
{
    Environment.SetEnvironmentVariable("ASPNETCORE_PREFERIPV4", "true");
    Environment.SetEnvironmentVariable("DOTNET_SYSTEM_NET_SOCKETS_PREFERIPV4", "true");
}

// Configure Aspire Dashboard required environment variables before builder creation
Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://localhost:18888");
Environment.SetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_ENDPOINT_URL", "http://localhost:13323");
Environment.SetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_HTTP_ENDPOINT_URL", "http://localhost:13324");

var builder = DistributedApplication.CreateBuilder(args);

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

// Kafka - Complete custom container replacement for external access
// WI27 FIX: Replace AddKafka() with properly configured custom Kafka container
var kafka = builder.AddContainer("kafka", "apache/kafka:3.8.0")
    .WithEndpoint(9092, targetPort: 9092, name: "kafka") // WI27: Add targetPort to fix endpoint configuration
    .WithEnvironment("KAFKA_NODE_ID", "1")
    .WithEnvironment("KAFKA_PROCESS_ROLES", "broker")
    .WithEnvironment("KAFKA_LISTENERS", "PLAINTEXT://0.0.0.0:9092") 
    .WithEnvironment("KAFKA_ADVERTISED_LISTENERS", "PLAINTEXT://localhost:9092")
    .WithEnvironment("CLUSTER_ID", "LocalTestingCluster2024")
    .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "1")
    .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR", "1")
    .WithEnvironment("KAFKA_AUTO_CREATE_TOPICS_ENABLE", "true")
    .WithEnvironment("KAFKA_NUM_PARTITIONS", "3")
    .WithEnvironment("KAFKA_DEFAULT_REPLICATION_FACTOR", "1");

// Prometheus - essential observability with enhanced health checks
var prometheus = builder.AddContainer("prometheus", "prom/prometheus:latest")
    .WithHttpEndpoint(PortConstants.PrometheusExternal, PortConstants.PrometheusInternal, name: "prometheus")
    .WithBindMount("./prometheus-minimal.yml", "/etc/prometheus/prometheus.yml")
    .WithArgs("--config.file=/etc/prometheus/prometheus.yml", 
             $"--web.listen-address=0.0.0.0:{PortConstants.PrometheusInternal}");

// Flink JobManager - custom container with health check validation
var flinkJobManager = builder.AddContainer("flink-jobmanager", "flink:2.1.0")
    .WithHttpEndpoint(PortConstants.FlinkJobManagerWebExternal, PortConstants.FlinkJobManagerWebInternal, name: "jobmanager-ui")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithArgs("jobmanager");

// Flink TaskManager - custom container with dependency coordination
var flinkTaskManager = builder.AddContainer("flink-taskmanager", "flink:2.1.0")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithArgs("taskmanager")
    .WaitFor(flinkJobManager);

// LocalTesting Web API - core service with explicit Kafka endpoint reference  
var localTestingApi = builder.AddProject<Projects.LocalTesting_WebApi>("localtesting-webapi")
    .WithReference(redis)
    .WithReference(kafka.GetEndpoint("kafka")) // WI27: Reference the specific kafka endpoint for service discovery
    .WithEnvironment("FLINK_JOBMANAGER_URL", PortConstants.FlinkJobManagerUrl())
    .WithEnvironment("PROMETHEUS_URL", PortConstants.PrometheusUrl())
    .WithHttpEndpoint(PortConstants.WebApiExternal, PortConstants.WebApiInternal, name: "webapi")
    .WaitFor(redis)
    .WaitFor(kafka);

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
