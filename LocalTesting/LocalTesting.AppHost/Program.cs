using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LocalTesting.Shared.Constants;

var builder = DistributedApplication.CreateBuilder(args);

// Simple Aspire configuration - clean and maintainable
Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");
Environment.SetEnvironmentVariable("DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS", "true");

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

// Redis - simple configuration
var redis = builder.AddRedis("redis")
    .WithEndpoint(PortConstants.RedisExternal, PortConstants.RedisInternal, name: "redis");

// Convert ports to strings for Aspire compatibility
var kafkaInternalStr = PortConstants.KafkaInternal.ToString();
var kafkaControllerInternalStr = PortConstants.KafkaControllerInternal.ToString();
var prometheusInternalStr = PortConstants.PrometheusInternal.ToString();

// Kafka - clean, standard configuration  
var kafka = builder.AddContainer("kafka", "apache/kafka:3.8.0")
    .WithEndpoint(PortConstants.KafkaExternal, PortConstants.KafkaInternal, name: "kafka")
    .WithEnvironment("KAFKA_NODE_ID", "1")
    .WithEnvironment("KAFKA_PROCESS_ROLES", "broker,controller")
    .WithEnvironment("KAFKA_LISTENERS", $"PLAINTEXT://0.0.0.0:{kafkaInternalStr},CONTROLLER://0.0.0.0:{kafkaControllerInternalStr}")
    .WithEnvironment("KAFKA_ADVERTISED_LISTENERS", $"PLAINTEXT://kafka:{kafkaInternalStr}")
    .WithEnvironment("KAFKA_CONTROLLER_LISTENER_NAMES", "CONTROLLER")
    .WithEnvironment("KAFKA_LISTENER_SECURITY_PROTOCOL_MAP", "PLAINTEXT:PLAINTEXT,CONTROLLER:PLAINTEXT")
    .WithEnvironment("KAFKA_CONTROLLER_QUORUM_VOTERS", $"1@kafka:{kafkaControllerInternalStr}")
    .WithEnvironment("CLUSTER_ID", "LOCAL_TESTING_KRAFT_CLUSTER")
    .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "1")
    .WithEnvironment("KAFKA_AUTO_CREATE_TOPICS_ENABLE", "true")
    .WithEnvironment("KAFKA_COMPRESSION_TYPE", "lz4");

// Flink JobManager - simplified configuration
var flinkJobManager = builder.AddContainer("flink-jobmanager", "flink:2.1.0")
    .WithHttpEndpoint(PortConstants.FlinkJobManagerWebExternal, PortConstants.FlinkJobManagerWebInternal, name: "jobmanager-ui")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithArgs("jobmanager");

// Flink TaskManager - simplified configuration
var flinkTaskManager = builder.AddContainer("flink-taskmanager", "flink:2.1.0")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithArgs("taskmanager")
    .WaitFor(flinkJobManager);

// Prometheus - essential observability
var prometheus = builder.AddContainer("prometheus", "prom/prometheus:latest")
    .WithHttpEndpoint(PortConstants.PrometheusExternal, PortConstants.PrometheusInternal, name: "prometheus")
    .WithBindMount("./prometheus-minimal.yml", "/etc/prometheus/prometheus.yml")
    .WithArgs("--config.file=/etc/prometheus/prometheus.yml", 
             $"--web.listen-address=0.0.0.0:{prometheusInternalStr}");

// LocalTesting Web API - core service
var localTestingApi = builder.AddProject<Projects.LocalTesting_WebApi>("localtesting-webapi")
    .WithReference(redis)
    .WithEnvironment("KAFKA_BOOTSTRAP_SERVERS", PortConstants.KafkaBootstrapServers())
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
