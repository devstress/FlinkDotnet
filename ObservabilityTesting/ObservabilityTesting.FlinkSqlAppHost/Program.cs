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
// Using simple configuration like LocalTesting/ReleasePackagesTesting (NO KafkaUI)
// KafkaUI adds extra listener configuration that causes advertised listener issues
Console.WriteLine("[INFO] Configuring Kafka...");
IResourceBuilder<KafkaServerResource> kafka = builder.AddKafka("kafka")
    .WithEnvironment("KAFKA_JMX_ENABLED", "true")  // CRITICAL: Required for JMX exporter to work
    .WithEnvironment("KAFKA_JMX_PORT", "9101")
    .WithEnvironment("KAFKA_JMX_HOSTNAME", "kafka")
    .WithEnvironment("KAFKA_JMX_OPTS",
        "-Dcom.sun.management.jmxremote " +
        "-Dcom.sun.management.jmxremote.authenticate=false " +
        "-Dcom.sun.management.jmxremote.ssl=false " +
        "-Djava.rmi.server.hostname=kafka " +
        "-Dcom.sun.management.jmxremote.rmi.port=9101 " +
        "-Dcom.sun.management.jmxremote.host=0.0.0.0 " +  // CRITICAL: Bind to all interfaces
        "-Dcom.sun.management.jmxremote.local.only=false")
    // CRITICAL: Confluent images also need KAFKA_OPTS for JMX
    .WithEnvironment("KAFKA_OPTS",
        "-Dcom.sun.management.jmxremote " +
        "-Dcom.sun.management.jmxremote.authenticate=false " +
        "-Dcom.sun.management.jmxremote.ssl=false " +
        "-Djava.rmi.server.hostname=kafka " +
        "-Dcom.sun.management.jmxremote.port=9101 " +
        "-Dcom.sun.management.jmxremote.rmi.port=9101 " +
        "-Dcom.sun.management.jmxremote.host=0.0.0.0 " +  // CRITICAL: Bind to all interfaces
        "-Dcom.sun.management.jmxremote.local.only=false")
    .WithLifetime(ContainerLifetime.Persistent);

Console.WriteLine("[INFO] Kafka configured with Aspire default settings");
Console.WriteLine("  - Port 9092: PLAINTEXT_HOST for host access");
Console.WriteLine("  - Port 9093: PLAINTEXT_INTERNAL for container access");
Console.WriteLine("  - JMX Port 9101: For metrics export (JMX exporter)");
Console.WriteLine("  [INFO] Using both KAFKA_JMX_OPTS and KAFKA_OPTS for Confluent compatibility");


// 1.5. Kafka JMX Exporter - Exports Kafka JMX metrics to Prometheus format
// CRITICAL: This container enables Prometheus to scrape Kafka metrics
Console.WriteLine("[INFO] Configuring Kafka JMX Exporter...");
string jmxConfigPath = Path.Combine(repoRoot, "ObservabilityTesting", "jmx-exporter-kafka-config.yml");

IResourceBuilder<ContainerResource>? kafkaExporter = null;
if (File.Exists(jmxConfigPath))
{
    kafkaExporter = builder.AddContainer("kafka-exporter", "bitnami/jmx-exporter", LatestTag)
        .WithBindMount(jmxConfigPath, "/opt/bitnami/jmx-exporter/exporter.yml", isReadOnly: true)
        .WithHttpEndpoint(targetPort: 5556, name: "kafka-metrics")
        .WithReference(kafka)  // Keep reference for network connectivity
        .WaitFor(kafka)  // CRITICAL: Wait for Kafka container to be started
        .WithEntrypoint("/bin/sh")
        .WithArgs("-c",
            // CRITICAL: Add 10-second delay to allow Kafka JMX port to be fully initialized
            // Kafka container starts quickly but JMX port takes time to become available
            "sleep 10 && java -jar /opt/bitnami/jmx-exporter/jmx_prometheus_standalone.jar 5556 /opt/bitnami/jmx-exporter/exporter.yml")
        .WithLifetime(ContainerLifetime.Persistent);

    Console.WriteLine("   [INFO] Kafka JMX Exporter configured: kafka:9101 → :5556/metrics");
    Console.WriteLine("   [INFO] JMX Exporter will wait 10s after Kafka starts for JMX port initialization");
}
else
{
    Console.WriteLine("   [WARNING] Kafka JMX Exporter config not found, skipping deployment");
}

// Constants for Flink container configuration
const string FlinkImage = "flink";
const string FlinkVersion = "2.1.0-java17";

// 2. Flink JobManager with Prometheus metrics enabled
Console.WriteLine("[INFO] Configuring Flink JobManager with Prometheus metrics...");

// Configure JobManager FLINK_PROPERTIES (metrics configuration)
string jobManagerFlinkProperties =
    "metrics.reporters: prom\n" +
    "metrics.reporter.prom.factory.class: org.apache.flink.metrics.prometheus.PrometheusReporterFactory\n" +
    "metrics.reporter.prom.port: 9250\n" +
    "metrics.reporter.prom.filterLabelValueCharacters: false\n";

// Mount Kafka connector JARs - CRITICAL for Flink to read/write Kafka topics
string connectorsDir = Path.Combine(repoRoot, "LocalTesting", "connectors", "flink", "lib");
string kafkaConnectorJar = Path.Combine(connectorsDir, "flink-sql-connector-kafka-4.0.1-2.0.jar");
string jsonConnectorJar = Path.Combine(connectorsDir, "flink-json-2.1.0.jar");

IResourceBuilder<ContainerResource> jobManager = builder.AddContainer("flink-jobmanager", FlinkImage, FlinkVersion)
    .WithHttpEndpoint(targetPort: 8081, name: "jobmanager-http")
    .WithHttpEndpoint(targetPort: 9250, name: "jm-metrics")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")  // CRITICAL: Set hostname for RPC binding
    .WithEnvironment("FLINK_PROPERTIES", jobManagerFlinkProperties)  // Metrics configuration
    .WithBindMount(kafkaConnectorJar, "/opt/flink/lib/flink-sql-connector-kafka-4.0.1-2.0.jar", isReadOnly: true)
    .WithBindMount(jsonConnectorJar, "/opt/flink/lib/flink-json-2.1.0.jar", isReadOnly: true)
    .WithArgs("jobmanager")  // Use standard Flink Docker entrypoint
    .WithLifetime(ContainerLifetime.Persistent);

Console.WriteLine("[INFO] Mounted Kafka connector JARs to JobManager");

// 3. Flink TaskManager with Prometheus metrics enabled  
Console.WriteLine("[INFO] Configuring Flink TaskManager with Prometheus metrics...");

// Configure TaskManager FLINK_PROPERTIES (memory and metrics)
string taskManagerFlinkProperties =
    "metrics.reporters: prom\n" +
    "metrics.reporter.prom.factory.class: org.apache.flink.metrics.prometheus.PrometheusReporterFactory\n" +
    "metrics.reporter.prom.port: 9251\n" +
    "metrics.reporter.prom.filterLabelValueCharacters: false\n";

builder.AddContainer("flink-taskmanager", FlinkImage, FlinkVersion)
    .WithHttpEndpoint(targetPort: 9251, name: "tm-metrics")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")  // Standard Flink environment variable  
    .WithEnvironment("FLINK_PROPERTIES", taskManagerFlinkProperties)  // Metrics configuration only
    .WithBindMount(kafkaConnectorJar, "/opt/flink/lib/flink-sql-connector-kafka-4.0.1-2.0.jar", isReadOnly: true)
    .WithBindMount(jsonConnectorJar, "/opt/flink/lib/flink-json-2.1.0.jar", isReadOnly: true)
    .WithArgs("taskmanager")  // Use standard Flink Docker entrypoint
    .WithReference(kafka)  // Matches LocalTesting pattern - ensures same network for Kafka connectivity
    .WaitFor(jobManager)
    .WithLifetime(ContainerLifetime.Persistent);

Console.WriteLine("[INFO] Mounted Kafka connector JARs to TaskManager");

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
    .WithBindMount(kafkaConnectorJar, "/opt/flink/lib/flink-sql-connector-kafka-4.0.1-2.0.jar", isReadOnly: true)
    .WithBindMount(jsonConnectorJar, "/opt/flink/lib/flink-json-2.1.0.jar", isReadOnly: true)
    .WithArgs("/opt/flink/bin/sql-gateway.sh", "start-foreground")
    .WaitFor(jobManager)
    .WithLifetime(ContainerLifetime.Persistent);

Console.WriteLine("   [INFO] SQL Gateway configured on port 8083");
Console.WriteLine("[INFO] Mounted Kafka connector JARs to SQL Gateway");

// 5. Prometheus - Metrics collection
// CRITICAL: Use ObservabilityTesting-specific prometheus.yml with Aspire internal networks
Console.WriteLine("[INFO] Configuring Prometheus...");
string prometheusConfig = Path.Combine(repoRoot, "ObservabilityTesting", "prometheus.yml");
IResourceBuilder<ContainerResource> prometheusBuilder = builder.AddContainer("prometheus", "prom/prometheus", LatestTag)
    .WithHttpEndpoint(targetPort: Ports.PrometheusHostPort, name: "prometheus-http")
    .WithBindMount(prometheusConfig, "/etc/prometheus/prometheus.yml", isReadOnly: true)
    .WithLifetime(ContainerLifetime.Persistent);

// Ensure Prometheus can reach all metrics endpoints via Aspire network
// Using WaitFor to establish network connectivity for DNS resolution
prometheusBuilder = prometheusBuilder
    .WaitFor(jobManager);

// Add kafka-exporter dependency if it was deployed
if (kafkaExporter is not null)
{
    prometheusBuilder = prometheusBuilder.WaitFor(kafkaExporter);
    Console.WriteLine("   [INFO] Prometheus configured with kafka-exporter network dependency");
}

IResourceBuilder<ContainerResource> prometheus = prometheusBuilder;

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
    .WaitFor(prometheus)  // Ensure Prometheus is ready to scrape Gateway metrics
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
