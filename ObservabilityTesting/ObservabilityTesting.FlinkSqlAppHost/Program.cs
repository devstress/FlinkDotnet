// ObservabilityTesting - Focused on testing Prometheus and Grafana integration with Flink
using ObservabilityTesting.FlinkSqlAppHost;

const string LatestTag = "latest";

// Setup environment for Aspire - required by Aspire SDK
SetupEnvironment();

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

Console.WriteLine("[INFO] Memory resources validated\n");

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);
string repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));

Console.WriteLine("🔍 Running in OBSERVABILITY TESTING mode");
Console.WriteLine("   ✅ Full stack enabled: Kafka + Flink + Prometheus + Grafana + JobGateway");

// 1. Kafka - Message broker for test data
Console.WriteLine("[INFO] Configuring Kafka...");
IResourceBuilder<KafkaServerResource> kafka = builder.AddKafka("kafka")
    .WithLifetime(ContainerLifetime.Persistent);

// Configure Kafka advertised listeners to use container name for inter-container communication
// This fixes the issue where Flink jobs can't connect to Kafka because it advertises localhost
// PLAINTEXT is for internal (container-to-container), PLAINTEXT_HOST is for external (host-to-container)
kafka.WithEnvironment("KAFKA_ADVERTISED_LISTENERS", "PLAINTEXT://kafka:9092,PLAINTEXT_HOST://localhost:9093,CONTROLLER://kafka:29093");
kafka.WithEnvironment("KAFKA_LISTENER_SECURITY_PROTOCOL_MAP", "PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT,CONTROLLER:PLAINTEXT");
kafka.WithEnvironment("KAFKA_LISTENERS", "PLAINTEXT://0.0.0.0:9092,PLAINTEXT_HOST://0.0.0.0:9093,CONTROLLER://0.0.0.0:29093");
kafka.WithEnvironment("KAFKA_INTER_BROKER_LISTENER_NAME", "PLAINTEXT");
kafka.WithEnvironment("KAFKA_CONTROLLER_LISTENER_NAMES", "CONTROLLER");

kafka.WithKafkaUI();

// Flink configuration file with correct jobmanager.rpc.address
string flinkConfigPath = Path.Combine(repoRoot, "ObservabilityTesting", "flink-config.yaml");

// Constants for Flink container configuration
const string FlinkImage = "flink";
const string FlinkVersion = "2.1.0-java17";

// 2. Flink JobManager with Prometheus metrics enabled
Console.WriteLine("[INFO] Configuring Flink JobManager with Prometheus metrics...");

string metricsJarPath = Path.Combine(repoRoot, "LocalTesting", "connectors", "flink", "metrics", "flink-metrics-prometheus-2.1.0.jar");
IResourceBuilder<ContainerResource> jobManager = builder.AddContainer("flink-jobmanager", FlinkImage, FlinkVersion)
    .WithHttpEndpoint(targetPort: 8081, name: "jobmanager-http")
    .WithHttpEndpoint(targetPort: 9250, name: "jm-metrics")
    .WithBindMount(flinkConfigPath, "/opt/flink/conf/config.yaml", isReadOnly: true)  // Mount proper config
    .WithEntrypoint("/bin/bash")
    .WithArgs("-c", "bin/jobmanager.sh start && tail -f /dev/null")
    .WithLifetime(ContainerLifetime.Persistent);

if (File.Exists(metricsJarPath))
{
    jobManager = jobManager.WithBindMount(metricsJarPath, "/opt/flink/lib/flink-metrics-prometheus-2.1.0.jar", isReadOnly: true);
    Console.WriteLine("   [INFO] Prometheus metrics JAR mounted for JobManager");
}

// 3. Flink TaskManager with Prometheus metrics enabled  
Console.WriteLine("[INFO] Configuring Flink TaskManager with Prometheus metrics...");

IResourceBuilder<ContainerResource> taskManager = builder.AddContainer("flink-taskmanager", FlinkImage, FlinkVersion)
    .WithHttpEndpoint(targetPort: 9251, name: "tm-metrics")
    .WithBindMount(flinkConfigPath, "/opt/flink/conf/config.yaml", isReadOnly: true)  // Mount proper config
    .WithEnvironment("FLINK_PROPERTIES", "metrics.reporter.prom.port: 9251\n")  // Override metrics port for TaskManager
    .WithEntrypoint("/bin/bash")
    .WithArgs("-c", "bin/taskmanager.sh start && tail -f /dev/null")
    .WaitFor(jobManager)
    .WithLifetime(ContainerLifetime.Persistent);

if (File.Exists(metricsJarPath))
{
    _ = taskManager.WithBindMount(metricsJarPath, "/opt/flink/lib/flink-metrics-prometheus-2.1.0.jar", isReadOnly: true);
    Console.WriteLine("   [INFO] Prometheus metrics JAR mounted for TaskManager");
}

// 4. Flink SQL Gateway - Required for FlinkDotNet JobGateway to communicate with Flink
Console.WriteLine("[INFO] Configuring Flink SQL Gateway...");

// Java options for SQL Gateway (split for readability)
const string JavaOptsBase = "--add-opens=java.base/java.lang=ALL-UNNAMED --add-opens=java.base/java.net=ALL-UNNAMED " +
    "--add-opens=java.base/java.io=ALL-UNNAMED --add-opens=java.base/java.nio=ALL-UNNAMED " +
    "--add-opens=java.base/sun.nio.ch=ALL-UNNAMED --add-opens=java.base/java.lang.reflect=ALL-UNNAMED " +
    "--add-opens=java.base/java.text=ALL-UNNAMED --add-opens=java.base/java.time=ALL-UNNAMED " +
    "--add-opens=java.base/java.util=ALL-UNNAMED --add-opens=java.base/java.util.concurrent=ALL-UNNAMED " +
    "--add-opens=java.base/java.util.concurrent.atomic=ALL-UNNAMED " +
    "--add-opens=java.base/java.util.concurrent.locks=ALL-UNNAMED";

string baseSqlGatewayFlinkProperties =
    "jobmanager.rpc.address: flink-jobmanager\n" +
    "rest.address: flink-jobmanager\n" +
    "rest.port: 8081\n" +
    "sql-gateway.endpoint.rest.address: flink-sql-gateway\n" +
    "sql-gateway.endpoint.rest.bind-address: 0.0.0.0\n" +
    "sql-gateway.endpoint.rest.port: 8083\n" +
    "sql-gateway.endpoint.rest.bind-port: 8083\n" +
    "sql-gateway.endpoint.type: rest\n" +
    "sql-gateway.session.check-interval: 60000\n" +
    "sql-gateway.session.idle-timeout: 600000\n" +
    "sql-gateway.worker.threads.max: 10\n" +
    $"env.java.opts.all: {JavaOptsBase}\n";

IResourceBuilder<ContainerResource> sqlGateway = builder.AddContainer("flink-sql-gateway", FlinkImage, FlinkVersion)
    .WithHttpEndpoint(targetPort: 8083, name: "sg-http")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithEnvironment("FLINK_PROPERTIES", baseSqlGatewayFlinkProperties)
    .WithArgs("/opt/flink/bin/sql-gateway.sh", "start-foreground")
    .WaitFor(jobManager)
    .WithLifetime(ContainerLifetime.Persistent);

Console.WriteLine("   [INFO] SQL Gateway configured on port 8083");

// 5. Prometheus - Metrics collection
Console.WriteLine("[INFO] Configuring Prometheus...");
string prometheusConfig = Path.Combine(repoRoot, "LocalTesting", "prometheus.yml");
IResourceBuilder<ContainerResource> prometheus = builder.AddContainer("prometheus", "prom/prometheus", LatestTag)
    .WithHttpEndpoint(targetPort: Ports.PrometheusHostPort, name: "prometheus-http")
    .WithBindMount(prometheusConfig, "/etc/prometheus/prometheus.yml", isReadOnly: true)
    .WithLifetime(ContainerLifetime.Persistent);

// 6. Grafana - Metrics visualization
Console.WriteLine("[INFO] Configuring Grafana...");

builder.AddContainer("grafana", "grafana/grafana", LatestTag)
    .WithHttpEndpoint(targetPort: Ports.GrafanaHostPort, name: "grafana-http")
    .WithEnvironment("GF_AUTH_ANONYMOUS_ENABLED", "true")
    .WithEnvironment("GF_AUTH_ANONYMOUS_ORG_ROLE", "Admin")
    .WithEnvironment("GF_AUTH_DISABLE_LOGIN_FORM", "true")
    .WithEnvironment("GF_SECURITY_ADMIN_PASSWORD", "admin")
    .WaitFor(prometheus)
    .WithLifetime(ContainerLifetime.Persistent);

// 7. FlinkDotNet JobGateway - FlinkDotNet job submission endpoint (using pre-built Docker image)
Console.WriteLine("[INFO] Configuring FlinkDotNet JobGateway from pre-built Docker image...");

const string gatewayImageTag = "flinkdotnet-gateway:local";

// Use AddContainer with pre-built image instead of PublishAsDockerFile
// The Docker image is built as part of the AppHost build process (see .csproj BuildGatewayDockerImage target)
builder.AddContainer("flinkdotnet-jobgateway", gatewayImageTag)
    .WithHttpEndpoint(targetPort: 8086, name: "gateway-http")
    .WithHttpEndpoint(targetPort: 9253, name: "gateway-metrics")  // Prometheus metrics endpoint
    .WithEnvironment("FLINK_JOBMANAGER_URL", "http://flink-jobmanager:8081")
    .WithEnvironment("Flink__JobManager__BaseUrl", "http://flink-jobmanager:8081")
    .WithEnvironment("Flink__SqlGateway__BaseUrl", "http://flink-sql-gateway:8083")
    .WithEnvironment("FLINK_CONNECTOR_PATH", "/app")  // Path to connector JARs (matches Gateway search logic)
    .WithEnvironment("Metrics__Prometheus__Enabled", "true")  // Enable Prometheus metrics
    .WithEnvironment("Metrics__Prometheus__Port", "9253")     // Metrics on port 9253
    .WithEnvironment("Metrics__Prometheus__Path", "/metrics") // Metrics path
    .WaitFor(jobManager)
    .WaitFor(sqlGateway)
    .WithLifetime(ContainerLifetime.Persistent);

Console.WriteLine($"   [INFO] FlinkDotNet JobGateway will use pre-built Docker image: {gatewayImageTag}");

Console.WriteLine("[INFO] All services configured successfully");
Console.WriteLine($"   - Kafka: Port {Ports.KafkaExternalPort}");
Console.WriteLine("   - Flink JobManager: Port 8081, Metrics: 9250");
Console.WriteLine("   - Flink TaskManager: 8 task slots, Metrics: 9251");
Console.WriteLine("   - Flink SQL Gateway: Port 8083");
Console.WriteLine($"   - Prometheus: Port {Ports.PrometheusHostPort}");
Console.WriteLine($"   - Grafana: Port {Ports.GrafanaHostPort}");
Console.WriteLine("   - FlinkDotNet JobGateway: Dynamic port (container port 8086)");

builder.Build().Run();

// Setup environment for Aspire Dashboard
static void SetupEnvironment()
{
    Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");
    // CRITICAL: Set ASPNETCORE_URLS for Aspire Dashboard (required by Aspire SDK)
    // This will be inherited by child processes, but we override it per-project using WithEnvironment()
    // FlinkDotNet JobGateway explicitly sets ASPNETCORE_URLS via WithEnvironment()
    Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://localhost:18888");
    Environment.SetEnvironmentVariable("ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL", "http://localhost:18889");
    Environment.SetEnvironmentVariable("ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL", "http://localhost:18890");
}
