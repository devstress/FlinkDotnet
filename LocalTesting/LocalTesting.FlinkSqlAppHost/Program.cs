// Configure container runtime - prefer Podman if available, fallback to Docker Desktop
using System.Diagnostics;
using LocalTesting.FlinkSqlAppHost;

if (!ConfigureContainerRuntime())
{
    return;
}

LogConfiguredPorts();
SetupEnvironment();

// Validate system memory and calculate dynamic allocations
Console.WriteLine("\n🔍 Analyzing system resources...");
if (!MemoryCalculator.ValidateMinimumMemory())
{
    Console.WriteLine("❌ System does not meet minimum memory requirements for Flink");
    Console.WriteLine("   Please ensure at least 4GB RAM is available");
    return;
}

Console.WriteLine($"✅ Memory resources validated\n");

// Check if LearningCourse mode is enabled - enables additional infrastructure for learning exercises
var isLearningCourse = Environment.GetEnvironmentVariable("LEARNINGCOURSE")?.ToLower() == "true";
if (isLearningCourse)
{
    Console.WriteLine("📚 LearningCourse mode enabled - Redis and Observability stack will be deployed");
}

var diagnosticsVerbose = Environment.GetEnvironmentVariable("DIAGNOSTICS_VERBOSE") == "1";
if (diagnosticsVerbose)
{
    Console.WriteLine("[diag] DIAGNOSTICS_VERBOSE=1 enabled for LocalTesting.FlinkSqlAppHost startup diagnostics");
}

const string JavaOpenOptions = "--add-opens=java.base/java.lang=ALL-UNNAMED --add-opens=java.base/java.net=ALL-UNNAMED --add-opens=java.base/java.io=ALL-UNNAMED --add-opens=java.base/java.nio=ALL-UNNAMED --add-opens=java.base/sun.nio.ch=ALL-UNNAMED --add-opens=java.base/java.lang.reflect=ALL-UNNAMED --add-opens=java.base/java.text=ALL-UNNAMED --add-opens=java.base/java.time=ALL-UNNAMED --add-opens=java.base/java.util=ALL-UNNAMED --add-opens=java.base/java.util.concurrent=ALL-UNNAMED --add-opens=java.base/java.util.concurrent.atomic=ALL-UNNAMED --add-opens=java.base/java.util.concurrent.locks=ALL-UNNAMED";

var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
var connectorsDir = Path.Combine(repoRoot, "LocalTesting", "connectors", "flink", "lib");
var testLogsDir = Path.GetFullPath(Path.Combine(repoRoot, "LocalTesting", "test-logs"));

// Ensure test-logs directory exists
Directory.CreateDirectory(testLogsDir);

Environment.SetEnvironmentVariable("LOG_FILE_PATH", testLogsDir);
Console.WriteLine($"📁 Log files will be written to: {testLogsDir}");

var gatewayJarPath = FindGatewayJarPath(repoRoot);
if (diagnosticsVerbose && File.Exists(gatewayJarPath))
{
    Console.WriteLine($"[diag] Gateway JAR configured: {gatewayJarPath}");
}

PrepareConnectorDirectory(connectorsDir, diagnosticsVerbose);

var builder = DistributedApplication.CreateBuilder(args);

// Detect LEARNINGCOURSE mode for conditional metrics configuration
var isLearningCourseMode = Environment.GetEnvironmentVariable("LEARNINGCOURSE")?.ToLower() == "true";
Console.WriteLine($"🔍 Running in {(isLearningCourseMode ? "LEARNINGCOURSE" : "PRODUCTION")} mode");
Console.WriteLine($"   Metrics export: {(isLearningCourseMode ? "ENABLED (Flink + Kafka)" : "DISABLED")}");

// Configure Kafka - Aspire's AddKafka() uses KRaft mode by default (no Zookeeper)
var kafka = builder.AddKafka("kafka");

// Enable JMX for metrics export only in LEARNINGCOURSE mode
if (isLearningCourseMode)
{
    kafka = kafka
        .WithEnvironment("KAFKA_JMX_PORT", "9101")
        .WithEnvironment("KAFKA_JMX_HOSTNAME", "0.0.0.0")
        .WithEnvironment("KAFKA_JMX_OPTS",
            "-Dcom.sun.management.jmxremote " +
            "-Dcom.sun.management.jmxremote.authenticate=false " +
            "-Dcom.sun.management.jmxremote.ssl=false " +
            "-Djava.rmi.server.hostname=kafka " +
            "-Dcom.sun.management.jmxremote.rmi.port=9101");
    Console.WriteLine("   📊 Kafka JMX metrics enabled on port 9101");
}

// Kafka JMX Exporter - only in LEARNINGCOURSE mode
// Uses the Bitnami JMX Exporter (latest version 1.5.0) as a standalone HTTP server
// Connects to Kafka's JMX endpoint (kafka:9101) and exposes metrics on port 5556
if (isLearningCourseMode)
{
    Console.WriteLine("   📊 Deploying Kafka JMX Exporter for metrics collection");
    
    var jmxConfigPath = Path.Combine(repoRoot, "LocalTesting", "jmx-exporter-kafka-config.yml");
    
    if (File.Exists(jmxConfigPath))
    {
#pragma warning disable S1481 // Kafka exporter is created but not directly referenced - accessed via Prometheus
        var kafkaExporter = builder.AddContainer("kafka-exporter", "bitnami/jmx-exporter", "latest")
            .WithBindMount(jmxConfigPath, "/opt/bitnami/jmx-exporter/exporter.yml", isReadOnly: true)
            .WithHttpEndpoint(targetPort: 5556, name: "metrics")
            .WithArgs("5556", "/opt/bitnami/jmx-exporter/exporter.yml")
            .WithReference(kafka);
        #pragma warning restore S1481
        
        Console.WriteLine("   📊 Kafka JMX Exporter configured: kafka:9101 → :5556/metrics");
    }
    else
    {
        Console.WriteLine("   ⚠️  Kafka JMX Exporter config not found, skipping deployment");
    }
}

// Flink JobManager with named HTTP endpoint for service references
// All ports are hardcoded - no WaitFor dependencies needed for parallel startup
var jobManagerBuilder = builder.AddContainer("flink-jobmanager", "flink:2.1.0-java17")
    .WithHttpEndpoint(port: Ports.JobManagerHostPort, targetPort: 8081, name: "jm-http");

// Only add Podman-specific container runtime args if Podman is detected
if (Environment.GetEnvironmentVariable("ASPIRE_CONTAINER_RUNTIME") == "podman")
{
    jobManagerBuilder = jobManagerBuilder
        .WithContainerRuntimeArgs("--publish", $"{Ports.JobManagerHostPort}:8081");
}

var jobManager = jobManagerBuilder
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithEnvironment("LOG_FILE_PATH", "/opt/flink/test-logs");  // Set log path inside container
    // REMOVED: .WithEnvironment("KAFKA_BOOTSTRAP", "kafka:9092")
    // REASON: FlinkJobRunner.java prioritizes environment variable over job definition
    // This caused jobs to use wrong Kafka address (localhost:17901 instead of kafka:9092)
    // Job definitions explicitly provide bootstrapServers, so environment variable is not needed

// Configure Prometheus metrics for JobManager only in LEARNINGCOURSE mode
if (isLearningCourseMode)
{
    var jobManagerFlinkProperties =
        "metrics.reporters: prom\n" +
        "metrics.reporter.prom.factory.class: org.apache.flink.metrics.prometheus.PrometheusReporterFactory\n" +
        "metrics.reporter.prom.port: 9250\n" +
        "metrics.reporter.prom.filterLabelValueCharacters: false\n";
    jobManager = jobManager.WithEnvironment("FLINK_PROPERTIES", jobManagerFlinkProperties);
}
    
jobManager = jobManager
    .WithEnvironment("JAVA_TOOL_OPTIONS", JavaOpenOptions)
    .WithEnvironment("JAVA_TOOL_OPTIONS", JavaOpenOptions)
    .WithBindMount(Path.Combine(connectorsDir, "flink-sql-connector-kafka-4.0.1-2.0.jar"), "/opt/flink/lib/flink-sql-connector-kafka-4.0.1-2.0.jar", isReadOnly: true)
    .WithBindMount(Path.Combine(connectorsDir, "flink-json-2.1.0.jar"), "/opt/flink/lib/flink-json-2.1.0.jar", isReadOnly: true)
    .WithBindMount(testLogsDir, "/opt/flink/test-logs");  // Mount host test-logs to container

// Expose Prometheus metrics port only in LEARNINGCOURSE mode
if (isLearningCourseMode)
{
    jobManager = jobManager.WithHttpEndpoint(port: 9250, targetPort: 9250, name: "jm-metrics");
    Console.WriteLine("   📊 Flink JobManager Prometheus metrics exposed on port 9250");
}

// Mount Prometheus metrics JAR only in LEARNINGCOURSE mode
// NOTE: Config file is NOT mounted because FLINK_PROPERTIES provides full Prometheus config
if (isLearningCourseMode)
{
    var metricsJarPath = Path.Combine(repoRoot, "LocalTesting", "connectors", "flink", "metrics", "flink-metrics-prometheus-2.1.0.jar");
    if (File.Exists(metricsJarPath))
    {
        jobManager = jobManager.WithBindMount(metricsJarPath, "/opt/flink/lib/flink-metrics-prometheus-2.1.0.jar", isReadOnly: true);
        Console.WriteLine("   📊 Flink Prometheus metrics JAR mounted for JobManager");
        Console.WriteLine("   📊 JobManager Prometheus port: 9250 (via FLINK_PROPERTIES)");
    }
}

jobManager = jobManager.WithArgs("jobmanager");

// Flink TaskManager with increased slots for parallel test execution (10 tests)
// CRITICAL: TaskManager must wait for both JobManager and Kafka to be ready
// - WaitFor(jobManager): Ensures TaskManager can register with JobManager
// - WaitFor(kafka): Ensures Kafka is ready before TaskManager starts processing jobs
var taskManagerBuilder = builder.AddContainer("flink-taskmanager", "flink:2.1.0-java17")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithEnvironment("TASK_MANAGER_NUMBER_OF_TASK_SLOTS", "10")
    .WithEnvironment("LOG_FILE_PATH", "/opt/flink/test-logs");  // Set log path inside container

// Configure Prometheus metrics for TaskManager only in LEARNINGCOURSE mode
if (isLearningCourseMode)
{
    var taskManagerFlinkProperties =
        "metrics.reporters: prom\n" +
        "metrics.reporter.prom.factory.class: org.apache.flink.metrics.prometheus.PrometheusReporterFactory\n" +
        "metrics.reporter.prom.port: 9251\n" +
        "metrics.reporter.prom.filterLabelValueCharacters: false\n";
    taskManagerBuilder = taskManagerBuilder.WithEnvironment("FLINK_PROPERTIES", taskManagerFlinkProperties);
}

taskManagerBuilder = taskManagerBuilder
    .WithEnvironment("JAVA_TOOL_OPTIONS", JavaOpenOptions)
    .WithEnvironment("JAVA_TOOL_OPTIONS", JavaOpenOptions)
    .WithBindMount(Path.Combine(connectorsDir, "flink-sql-connector-kafka-4.0.1-2.0.jar"), "/opt/flink/lib/flink-sql-connector-kafka-4.0.1-2.0.jar", isReadOnly: true)
    .WithBindMount(Path.Combine(connectorsDir, "flink-json-2.1.0.jar"), "/opt/flink/lib/flink-json-2.1.0.jar", isReadOnly: true)
    .WithBindMount(testLogsDir, "/opt/flink/test-logs");  // Mount host test-logs to container

// Expose Prometheus metrics port only in LEARNINGCOURSE mode
if (isLearningCourseMode)
{
    taskManagerBuilder = taskManagerBuilder.WithHttpEndpoint(port: 9251, targetPort: 9251, name: "tm-metrics");
    Console.WriteLine("   📊 Flink TaskManager Prometheus metrics exposed on port 9251");
}

var taskManager = taskManagerBuilder;

// Mount Prometheus metrics JAR only in LEARNINGCOURSE mode
// NOTE: Config file is NOT mounted because FLINK_PROPERTIES provides full Prometheus config
if (isLearningCourseMode)
{
    var metricsJarPath = Path.Combine(repoRoot, "LocalTesting", "connectors", "flink", "metrics", "flink-metrics-prometheus-2.1.0.jar");
    if (File.Exists(metricsJarPath))
    {
        taskManager = taskManager.WithBindMount(metricsJarPath, "/opt/flink/lib/flink-metrics-prometheus-2.1.0.jar", isReadOnly: true);
        Console.WriteLine("   📊 Flink Prometheus metrics JAR mounted for TaskManager");
        Console.WriteLine("   📊 TaskManager Prometheus port: 9251 (via FLINK_PROPERTIES)");
    }
}

taskManager = taskManager
    .WithReference(kafka)
    .WithArgs("taskmanager");

// Flink SQL Gateway - Enables SQL Gateway REST API for direct SQL submission
// SQL Gateway provides /v1/statements endpoint for executing SQL without JAR submission
// Required for Pattern5 (SqlPassthrough) which uses "gateway" execution mode
// Runs on port 8083 (separate from JobManager REST API on port 8081)
// CRITICAL: SQL Gateway must wait for JobManager to be ready before starting
var sqlGatewayBuilder = builder.AddContainer("flink-sql-gateway", "flink:2.1.0-java17")
    .WithHttpEndpoint(port: Ports.SqlGatewayHostPort, targetPort: 8083, name: "sg-http")
    .WaitFor(jobManager);  // Wait for JobManager to be ready before starting SQL Gateway

if (Environment.GetEnvironmentVariable("ASPIRE_CONTAINER_RUNTIME") == "podman")
{
    sqlGatewayBuilder = sqlGatewayBuilder
        .WithContainerRuntimeArgs("--publish", $"{Ports.SqlGatewayHostPort}:8083");
}

// Build base Flink properties for SQL Gateway
// CRITICAL: sql-gateway.endpoint.rest.address is REQUIRED by Flink 2.1.0
// Without it, SQL Gateway fails with "Missing required options are: address"
var baseSqlGatewayFlinkProperties =
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
    "env.java.opts.all: --add-opens=java.base/java.lang=ALL-UNNAMED --add-opens=java.base/java.net=ALL-UNNAMED --add-opens=java.base/java.io=ALL-UNNAMED --add-opens=java.base/java.nio=ALL-UNNAMED --add-opens=java.base/sun.nio.ch=ALL-UNNAMED --add-opens=java.base/java.lang.reflect=ALL-UNNAMED --add-opens=java.base/java.text=ALL-UNNAMED --add-opens=java.base/java.time=ALL-UNNAMED --add-opens=java.base/java.util=ALL-UNNAMED --add-opens=java.base/java.util.concurrent=ALL-UNNAMED --add-opens=java.base/java.util.concurrent.atomic=ALL-UNNAMED --add-opens=java.base/java.util.concurrent.locks=ALL-UNNAMED\n";

// Add Prometheus configuration for SQL Gateway in LEARNINGCOURSE mode
if (isLearningCourseMode)
{
    baseSqlGatewayFlinkProperties +=
        "metrics.reporters: prom\n" +
        "metrics.reporter.prom.factory.class: org.apache.flink.metrics.prometheus.PrometheusReporterFactory\n" +
        "metrics.reporter.prom.port: 9252\n" +
        "metrics.reporter.prom.filterLabelValueCharacters: false\n";
}

sqlGatewayBuilder = sqlGatewayBuilder
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithEnvironment("LOG_FILE_PATH", "/opt/flink/test-logs")  // Set log path inside container
    .WithEnvironment("FLINK_PROPERTIES", baseSqlGatewayFlinkProperties);  // SQL Gateway needs FLINK_PROPERTIES for sql-gateway.endpoint.rest.address

sqlGatewayBuilder = sqlGatewayBuilder
    .WithEnvironment("JAVA_TOOL_OPTIONS", JavaOpenOptions)
    .WithEnvironment("JAVA_TOOL_OPTIONS", JavaOpenOptions)
    .WithBindMount(Path.Combine(connectorsDir, "flink-sql-connector-kafka-4.0.1-2.0.jar"), "/opt/flink/lib/flink-sql-connector-kafka-4.0.1-2.0.jar", isReadOnly: true)
    .WithBindMount(Path.Combine(connectorsDir, "flink-json-2.1.0.jar"), "/opt/flink/lib/flink-json-2.1.0.jar", isReadOnly: true)
    .WithBindMount(testLogsDir, "/opt/flink/test-logs");  // Mount host test-logs to container

// Expose Prometheus metrics port only in LEARNINGCOURSE mode
if (isLearningCourseMode)
{
    sqlGatewayBuilder = sqlGatewayBuilder.WithHttpEndpoint(port: 9252, targetPort: 9252, name: "sg-metrics");
    Console.WriteLine("   📊 Flink SQL Gateway Prometheus metrics exposed on port 9252");
}

var sqlGateway = sqlGatewayBuilder;

// Mount Prometheus metrics JAR and config file only in LEARNINGCOURSE mode
if (isLearningCourseMode)
{
    var metricsJarPath = Path.Combine(repoRoot, "LocalTesting", "connectors", "flink", "metrics", "flink-metrics-prometheus-2.1.0.jar");
    if (File.Exists(metricsJarPath))
    {
        sqlGateway = sqlGateway.WithBindMount(metricsJarPath, "/opt/flink/lib/flink-metrics-prometheus-2.1.0.jar", isReadOnly: true);
        Console.WriteLine("   📊 Flink Prometheus metrics JAR mounted for SQL Gateway");
    }
    
    // Mount Flink config file with Prometheus metrics configuration
    var flinkConfigPath = Path.Combine(repoRoot, "LocalTesting", "flink-conf-learningcourse.yaml");
    if (File.Exists(flinkConfigPath))
    {
        sqlGateway = sqlGateway.WithBindMount(flinkConfigPath, "/opt/flink/conf/config.yaml", isReadOnly: true);
        Console.WriteLine("   📊 Flink config file mounted for SQL Gateway (Prometheus metrics enabled)");
    }
}

sqlGateway = sqlGateway.WithArgs("/opt/flink/bin/sql-gateway.sh", "start-foreground");

// Flink.JobGateway - Add Flink Job Gateway
#pragma warning disable S1481 // Gateway resource is created but not directly referenced - used via Aspire orchestration
var gateway = builder.AddProject<Projects.FlinkDotNet_JobGateway>("flink-job-gateway")
    .WithHttpEndpoint(port: 8080, name: "gateway-http")
    .WithEnvironment("FLINK_CONNECTOR_PATH", connectorsDir)  // Host path to connectors
    .WithEnvironment("FLINK_RUNNER_JAR_PATH", gatewayJarPath)  // Host path to JAR
    .WithEnvironment("LOG_FILE_PATH", testLogsDir)  // Host path to logs
    .WithReference(jobManager.GetEndpoint("jm-http"))  // Reference JobManager endpoint for service discovery
    .WithReference(sqlGateway.GetEndpoint("sg-http"))  // Reference SQL Gateway endpoint for service discovery
    .WithReference(kafka);  // Reference Kafka for service discovery and connection string injection
#pragma warning restore S1481

// Temporal PostgreSQL - Database for Temporal server
// CRITICAL: Must configure PostgreSQL WITHOUT password for Temporal auto-setup compatibility
// Temporal's auto-setup expects simple authentication (trust or no password)
var temporalDbServer = builder.AddPostgres("temporal-postgres")
    .WithEnvironment("POSTGRES_HOST_AUTH_METHOD", "trust")  // Allow trust authentication (no password)
    .WithEnvironment("POSTGRES_DB", "temporal");  // Create temporal database on startup
                                                  // PostgreSQL will use default "postgres" user with trust authentication

// Note: Temporal auto-setup will also create "temporal_visibility" database

// Temporal Server - Official temporalio/auto-setup image from temporal.io
// Auto-setup handles schema creation and namespace setup automatically
// CRITICAL: Temporal provides durable workflow execution with:
// - Workflow state persistence and recovery
// - Activity retry and compensation patterns
// - Signal and query support for interactive workflows
// - Timer services for delayed/scheduled operations
// IMPORTANT: Using .WithReference() to get Aspire-injected connection details
// Aspire will inject: ConnectionStrings__temporal-postgres = "Host=...;Port=...;Username=postgres;Password=..."
// Temporal will parse this connection string and extract credentials automatically
builder.AddContainer("temporal-server", "temporalio/auto-setup", "1.22.4")
    .WithHttpEndpoint(port: Ports.TemporalGrpcPort, targetPort: 7233, name: "temporal-grpc")
    .WithHttpEndpoint(port: Ports.TemporalUIPort, targetPort: 8233, name: "temporal-ui")
    .WithEnvironment("DB", "postgres12")
    .WithEnvironment("POSTGRES_SEEDS", temporalDbServer.Resource.Name)  // Use Aspire resource name for hostname
    .WithEnvironment("DB_PORT", "5432")  // Explicit port
    .WithEnvironment("POSTGRES_USER", "postgres")  // Default PostgreSQL user
    .WithEnvironment("POSTGRES_PWD", "")  // No password with trust authentication
    .WithEnvironment("DBNAME", "temporal")  // Specify database name for Temporal
    .WithEnvironment("VISIBILITY_DBNAME", "temporal_visibility")  // Specify visibility database name
    .WithEnvironment("SKIP_DB_CREATE", "false")  // Let Temporal create databases
    .WithEnvironment("SKIP_DEFAULT_NAMESPACE_CREATION", "false")  // Create default namespace
    .WaitFor(temporalDbServer);  // Wait for PostgreSQL to be ready

// LearningCourse Infrastructure - Conditionally add Redis and Observability stack
if (isLearningCourse)
{
    // Redis - Required for Day15 Capstone Project exercises (Exercise151-154)
    // Provides state management, caching, and distributed coordination capabilities
    // CRITICAL: Use Bitnami Redis image with ALLOW_EMPTY_PASSWORD for learning exercises
    // This allows exercises to connect with simple "localhost:port" format without authentication
    #pragma warning disable S1481 // Redis resource is created but not directly referenced - used via connection string
    var redis = builder.AddContainer("redis", "bitnami/redis", "latest")
        .WithHttpEndpoint(port: Ports.RedisHostPort, targetPort: 6379, name: "redis-port")
        .WithEnvironment("ALLOW_EMPTY_PASSWORD", "yes");  // Disable password requirement for learning
    #pragma warning restore S1481
    
    Console.WriteLine($"✅ Redis deployed on port {Ports.RedisHostPort} for LearningCourse exercises");
    
    // Observability Stack - Prometheus for metrics collection
    // Required for monitoring and performance analysis exercises
    // CRITICAL: Gateway runs as .NET project on host, accessible via host.docker.internal
    // Gateway port must be manually updated in prometheus.yml after checking Aspire dashboard
    var prometheusConfig = Path.Combine(repoRoot, "LocalTesting", "prometheus.yml");
    
    Console.WriteLine($"   📊 Prometheus config: prometheus.yml");
    Console.WriteLine($"   ⚠️  Gateway port (17105) may need updating - check Aspire dashboard for actual port");
    
    var prometheusBuilder = builder.AddContainer("prometheus", "prom/prometheus", "latest")
        .WithHttpEndpoint(port: Ports.PrometheusHostPort, targetPort: 9090, name: "prometheus-http")
        .WithBindMount(prometheusConfig, "/etc/prometheus/prometheus.yml", isReadOnly: true);
    // Note: Gateway is accessed via host.docker.internal, not service name, so no WithReference needed
    
    // Add explicit port mapping for Podman/Docker compatibility
    if (Environment.GetEnvironmentVariable("ASPIRE_CONTAINER_RUNTIME") == "podman")
    {
        prometheusBuilder = prometheusBuilder
            .WithContainerRuntimeArgs("--publish", $"{Ports.PrometheusHostPort}:9090");
    }
    
    var prometheus = prometheusBuilder;
    
    // Note: Prometheus uses static_configs in prometheus.yml for scraping
    // Container DNS resolution works automatically within Aspire's shared Docker network
    
    // Note: Prometheus will scrape Flink metrics using container DNS names (flink-jobmanager, flink-taskmanager, etc.)
    // Aspire automatically creates a shared Docker network where containers can resolve each other by name
    // Gateway is scraped via host.docker.internal since it runs as a .NET project on the host
    
    Console.WriteLine($"✅ Prometheus deployed on port {Ports.PrometheusHostPort} for metrics collection");
    
    // Observability Stack - Grafana for metrics visualization
    // Provides dashboards and alerting for performance monitoring
    // CRITICAL: Anonymous authentication enabled for learning environment (no login required)
    // Complete anonymous access configuration to bypass login page entirely
    var grafanaBuilder = builder.AddContainer("grafana", "grafana/grafana", "latest")
        .WithHttpEndpoint(port: Ports.GrafanaHostPort, targetPort: 3000, name: "grafana-http")
        .WithEnvironment("GF_AUTH_ANONYMOUS_ENABLED", "true")  // Enable anonymous access
        .WithEnvironment("GF_AUTH_ANONYMOUS_ORG_ROLE", "Admin")  // Grant admin role to anonymous users
        .WithEnvironment("GF_AUTH_DISABLE_LOGIN_FORM", "true")  // Completely hide login form
        .WithEnvironment("GF_SECURITY_ADMIN_PASSWORD", "admin")  // Keep admin account for advanced config
        .WithEnvironment("GF_SECURITY_ADMIN_USER", "admin")
        .WaitFor(prometheus);  // Wait for Prometheus to be ready
    
    #pragma warning disable S1481 // Grafana resource is created but not directly referenced - accessed via browser
    var grafana = grafanaBuilder;
    #pragma warning restore S1481
    
    Console.WriteLine($"✅ Grafana deployed on port {Ports.GrafanaHostPort} for visualization");
}

#pragma warning disable S6966 // Await RunAsync instead - Required for Aspire testing framework compatibility
builder.Build().Run();
#pragma warning restore S6966

static bool ConfigureContainerRuntime()
{
    // Try Docker Desktop first (preferred)
    if (IsDockerAvailable())
    {
        Console.WriteLine("✅ Using Docker Desktop as container runtime");
        // No need to set ASPIRE_CONTAINER_RUNTIME - Docker is the default
        return true;
    }
    
    // Fallback to Podman if Docker is not available
    if (IsPodmanAvailable())
    {
        Console.WriteLine("✅ Using Podman as container runtime (Docker not available)");
        Environment.SetEnvironmentVariable("ASPIRE_CONTAINER_RUNTIME", "podman");
        SetPodmanDockerHost();
        return true;
    }
    
    Console.WriteLine("❌ No container runtime found. Please install Docker Desktop or Podman.");
    return false;
}

static void LogConfiguredPorts()
{
    Console.WriteLine($"📍 Configured ports:");
    Console.WriteLine($"   - Flink JobManager: {Ports.JobManagerHostPort}");
    Console.WriteLine($"   - Gateway: {Ports.GatewayHostPort}");
    Console.WriteLine($"   - Kafka: <dynamic port allocated by Aspire>");
}

static void SetupEnvironment()
{
    Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");
    Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://localhost:15888");
    Environment.SetEnvironmentVariable("ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL", "http://localhost:16686");
    Environment.SetEnvironmentVariable("ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL", "http://localhost:16687");
}

static string FindGatewayJarPath(string repoRoot)
{
    var candidates = new[]
    {
        Path.Combine(repoRoot, "FlinkDotNet", "Flink.JobGateway", "bin", "Release", "net9.0", "flink-ir-runner-java17.jar"),
        Path.Combine(repoRoot, "FlinkDotNet", "Flink.JobGateway", "bin", "Debug", "net9.0", "flink-ir-runner-java17.jar")
    };

    return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
}

static void PrepareConnectorDirectory(string connectorsDir, bool diagnosticsVerbose)
{
    try
    {
        Directory.CreateDirectory(connectorsDir);
        if (diagnosticsVerbose)
        {
            Console.WriteLine($"[diag] Connector directory ready at {connectorsDir}");
        }
    }
    catch (Exception ex)
    {
        if (diagnosticsVerbose)
        {
            Console.WriteLine($"[diag][warn] Connector dir prep failed: {ex.Message}");
        }
    }
}

static bool IsPodmanAvailable()
{
    try
    {
        if (!IsPodmanCommandAvailable())
        {
            return false;
        }

        return IsPodmanMachineRunning();
    }
    catch
    {
        return false;
    }
}

static bool IsPodmanCommandAvailable()
{
    var versionPsi = new ProcessStartInfo
    {
        FileName = "podman",
        Arguments = "version",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    using var versionProcess = Process.Start(versionPsi);
    versionProcess?.WaitForExit(5000);
    return versionProcess?.ExitCode == 0;
}

static bool IsPodmanMachineRunning()
{
    var machinePsi = new ProcessStartInfo
    {
        FileName = "podman",
        Arguments = "machine list --format \"{{.Running}}\"",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    using var machineProcess = Process.Start(machinePsi);
    if (machineProcess == null)
    {
        return false;
    }

    var output = machineProcess.StandardOutput.ReadToEnd();
    machineProcess.WaitForExit(5000);
    
    if (output.Contains("true", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("   ℹ️ Podman machine is running");
        return true;
    }
    
    if (!string.IsNullOrWhiteSpace(output))
    {
        Console.WriteLine("   ⚠️ Podman machine is not running. Start with: podman machine start");
        return false;
    }

    // On Linux, Podman runs natively without a machine
    Console.WriteLine("   ℹ️ Podman detected (native mode)");
    return true;
}

static bool IsDockerAvailable()
{
    try
    {
        // First check if Docker command is available
        if (!IsDockerCommandAvailable())
        {
            return false;
        }

        // Then check if Docker daemon is running
        return IsDockerDaemonRunning();
    }
    catch
    {
        return false;
    }
}

static bool IsDockerCommandAvailable()
{
    var versionPsi = new ProcessStartInfo
    {
        FileName = "docker",
        Arguments = "version",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    using var versionProcess = Process.Start(versionPsi);
    versionProcess?.WaitForExit(5000);
    return versionProcess?.ExitCode == 0;
}

static bool IsDockerDaemonRunning()
{
    var psi = new ProcessStartInfo
    {
        FileName = "docker",
        Arguments = "info",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    using var process = Process.Start(psi);
    if (process == null)
    {
        return false;
    }

    process.StandardOutput.ReadToEnd(); // Consume output to prevent blocking
    var error = process.StandardError.ReadToEnd();
    process.WaitForExit(5000);

    if (process.ExitCode == 0)
    {
        Console.WriteLine("   ℹ️ Docker daemon is running");
        return true;
    }

    // Docker command exists but daemon is not running
    if (error.Contains("Cannot connect to the Docker daemon", StringComparison.OrdinalIgnoreCase) ||
        error.Contains("Is the docker daemon running", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("   ⚠️ Docker is installed but daemon is not running. Start Docker Desktop.");
        return false;
    }

    Console.WriteLine($"   ⚠️ Docker daemon check failed: {error}");
    return false;
}

static void SetPodmanDockerHost()
{
    try
    {
        // Get Podman connection URI
        var psi = new ProcessStartInfo
        {
            FileName = "podman",
            Arguments = "system connection ls --format \"{{.URI}}\" --filter default=true",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process != null)
        {
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(5000);
            
            if (!string.IsNullOrWhiteSpace(output) && process.ExitCode == 0)
            {
                Environment.SetEnvironmentVariable("DOCKER_HOST", output);
                Console.WriteLine($"   ℹ️ DOCKER_HOST set to: {output}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"   ⚠️ Could not set DOCKER_HOST: {ex.Message}");
    }
}

