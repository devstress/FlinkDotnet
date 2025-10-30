// ObservabilityTesting - Focused on testing Prometheus and Grafana integration with Flink
using ObservabilityTesting.FlinkSqlAppHost;

const string LatestTag = "latest";

Console.WriteLine("=== ObservabilityTesting AppHost Starting ===");
Console.WriteLine("[INFO] This AppHost deploys Flink + Kafka + Prometheus + Grafana for observability testing");

// Validate system memory
Console.WriteLine("\n[INFO] Analyzing system resources...");
if (!MemoryCalculator.ValidateMinimumMemory())
{
    Console.WriteLine("[ERROR] System does not meet minimum memory requirements for Flink");
    Console.WriteLine("   Please ensure at least 4GB RAM is available");
    return;
}

var taskManagerMemoryMb = MemoryCalculator.CalculateTaskManagerProcessMemoryMb();
var jobManagerMemoryMb = MemoryCalculator.CalculateJobManagerProcessMemoryMb();
Console.WriteLine("[INFO] Memory resources validated\n");

var builder = DistributedApplication.CreateBuilder(args);
var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));

// 1. Kafka - Message broker for test data
Console.WriteLine("[INFO] Configuring Kafka...");
#pragma warning disable S1481 // kafka resource is created and used by Aspire infrastructure
var kafka = builder.AddKafka("kafka")
    .WithKafkaUI()
    .WithLifetime(ContainerLifetime.Persistent);
#pragma warning restore S1481

// 2. Flink JobManager with Prometheus metrics enabled
Console.WriteLine("[INFO] Configuring Flink JobManager with Prometheus metrics...");
var jobManagerFlinkProperties = $"jobmanager.memory.process.size: {jobManagerMemoryMb}m\n" +
    "metrics.reporters: prom\n" +
    "metrics.reporter.prom.factory.class: org.apache.flink.metrics.prometheus.PrometheusReporterFactory\n" +
    "metrics.reporter.prom.port: 9250\n" +
    "metrics.reporter.prom.filterLabelValueCharacters: false\n";

var metricsJarPath = Path.Combine(repoRoot, "LocalTesting", "connectors", "flink", "metrics", "flink-metrics-prometheus-2.1.0.jar");
var jobManager = builder.AddContainer("flink-jobmanager", "flink", "2.1.0-java17")
    .WithHttpEndpoint(targetPort: 8081, name: "jobmanager-http")
    .WithHttpEndpoint(targetPort: 9250, name: "jm-metrics")
    .WithEnvironment("FLINK_PROPERTIES", jobManagerFlinkProperties)
    .WithEntrypoint("/bin/bash")
    .WithArgs("-c", "bin/jobmanager.sh start && tail -f /dev/null");

if (File.Exists(metricsJarPath))
{
    jobManager = jobManager.WithBindMount(metricsJarPath, "/opt/flink/lib/flink-metrics-prometheus-2.1.0.jar", isReadOnly: true);
    Console.WriteLine("   [INFO] Prometheus metrics JAR mounted for JobManager");
}

// 3. Flink TaskManager with Prometheus metrics enabled  
Console.WriteLine("[INFO] Configuring Flink TaskManager with Prometheus metrics...");
var taskManagerFlinkProperties = $"taskmanager.memory.process.size: {taskManagerMemoryMb}m\n" +
    "metrics.reporters: prom\n" +
    "metrics.reporter.prom.factory.class: org.apache.flink.metrics.prometheus.PrometheusReporterFactory\n" +
    "metrics.reporter.prom.port: 9251\n" +
    "metrics.reporter.prom.filterLabelValueCharacters: false\n" +
    "taskmanager.numberOfTaskSlots: 8\n";

var taskManager = builder.AddContainer("flink-taskmanager", "flink", "2.1.0-java17")
    .WithHttpEndpoint(targetPort: 9251, name: "tm-metrics")
    .WithEnvironment("FLINK_PROPERTIES", taskManagerFlinkProperties)
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithEntrypoint("/bin/bash")
    .WithArgs("-c", "bin/taskmanager.sh start && tail -f /dev/null")
    .WaitFor(jobManager);

if (File.Exists(metricsJarPath))
{
    taskManager = taskManager.WithBindMount(metricsJarPath, "/opt/flink/lib/flink-metrics-prometheus-2.1.0.jar", isReadOnly: true);
    Console.WriteLine("   [INFO] Prometheus metrics JAR mounted for TaskManager");
}

// 4. Prometheus - Metrics collection
Console.WriteLine("[INFO] Configuring Prometheus...");
var prometheusConfig = Path.Combine(repoRoot, "LocalTesting", "prometheus.yml");
var prometheus = builder.AddContainer("prometheus", "prom/prometheus", LatestTag)
    .WithHttpEndpoint(targetPort: Ports.PrometheusHostPort, name: "prometheus-http")
    .WithBindMount(prometheusConfig, "/etc/prometheus/prometheus.yml", isReadOnly: true);

// 5. Grafana - Metrics visualization
Console.WriteLine("[INFO] Configuring Grafana...");
#pragma warning disable S1481 // grafana resource is created and used by Aspire infrastructure
var grafana = builder.AddContainer("grafana", "grafana/grafana", LatestTag)
    .WithHttpEndpoint(targetPort: Ports.GrafanaHostPort, name: "grafana-http")
    .WithEnvironment("GF_AUTH_ANONYMOUS_ENABLED", "true")
    .WithEnvironment("GF_AUTH_ANONYMOUS_ORG_ROLE", "Admin")
    .WithEnvironment("GF_AUTH_DISABLE_LOGIN_FORM", "true")
    .WithEnvironment("GF_SECURITY_ADMIN_PASSWORD", "admin")
    .WaitFor(prometheus);
#pragma warning restore S1481

// 6. Gateway - FlinkDotNet job submission endpoint (built from Dockerfile)
Console.WriteLine("[INFO] Configuring Gateway to build from Dockerfile...");

var gatewayDockerfilePath = Path.Combine(repoRoot, "FlinkDotNet", "FlinkDotNet.JobGateway", "Dockerfile");
if (!File.Exists(gatewayDockerfilePath))
{
    throw new FileNotFoundException($"Gateway Dockerfile not found at: {gatewayDockerfilePath}");
}

#pragma warning disable S1481 // gateway resource is created and used by Aspire infrastructure
// Use PublishAsDockerFile to build the Gateway image from Dockerfile as part of the Aspire build
var gateway = builder.AddProject<Projects.FlinkDotNet_JobGateway>("gateway")
    .WithHttpEndpoint(targetPort: 8086, port: Ports.GatewayHostPort, name: "gateway-http")
    .WithEnvironment("FLINK_JOBMANAGER_URL", "http://flink-jobmanager:8081")
    .PublishAsDockerFile()
    .WaitFor(jobManager);
#pragma warning restore S1481

Console.WriteLine($"   [INFO] Gateway will be built from Dockerfile: {gatewayDockerfilePath}");

Console.WriteLine("[INFO] All services configured successfully");
Console.WriteLine($"   - Kafka: Port {Ports.KafkaExternalPort}");
Console.WriteLine("   - Flink JobManager: Port 8081, Metrics: 9250");
Console.WriteLine("   - Flink TaskManager: Metrics: 9251");
Console.WriteLine($"   - Prometheus: Port {Ports.PrometheusHostPort}");
Console.WriteLine($"   - Grafana: Port {Ports.GrafanaHostPort}");
Console.WriteLine($"   - Gateway: Port {Ports.GatewayHostPort}");

builder.Build().Run();
