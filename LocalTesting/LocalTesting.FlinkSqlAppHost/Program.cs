using LocalTesting.FlinkSqlAppHost;

// Basic environment setup
Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");

// Set up Aspire dashboard configuration for testing
Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://localhost:15888");
Environment.SetEnvironmentVariable("ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL", "http://localhost:16686");
Environment.SetEnvironmentVariable("ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL", "http://localhost:16687");

var diagnosticsVerbose = Environment.GetEnvironmentVariable("DIAGNOSTICS_VERBOSE") == "1";
if (diagnosticsVerbose)
{
    Console.WriteLine("[diag] DIAGNOSTICS_VERBOSE=1 enabled for LocalTesting.FlinkSqlAppHost startup diagnostics");
}

// Ports to match LearningCourse


const string JavaOpenOptions = "--add-opens=java.base/java.lang=ALL-UNNAMED --add-opens=java.base/java.net=ALL-UNNAMED --add-opens=java.base/java.io=ALL-UNNAMED --add-opens=java.base/java.nio=ALL-UNNAMED --add-opens=java.base/sun.nio.ch=ALL-UNNAMED --add-opens=java.base/java.lang.reflect=ALL-UNNAMED --add-opens=java.base/java.text=ALL-UNNAMED --add-opens=java.base/java.time=ALL-UNNAMED --add-opens=java.base/java.util=ALL-UNNAMED --add-opens=java.base/java.util.concurrent=ALL-UNNAMED --add-opens=java.base/java.util.concurrent.atomic=ALL-UNNAMED --add-opens=java.base/java.util.concurrent.locks=ALL-UNNAMED";

var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
var connectorsDir = Path.Combine(repoRoot, "LocalTesting", "connectors", "flink", "lib");

// Configure Gateway JAR path to use Release build
var gatewayJarPath = Path.Combine(repoRoot, "FlinkDotNet", "Flink.JobGateway", "bin", "Release", "net9.0", "flink-ir-runner.jar");
if (!File.Exists(gatewayJarPath))
{
    // Fallback to Debug if Release not found
    gatewayJarPath = Path.Combine(repoRoot, "FlinkDotNet", "Flink.JobGateway", "bin", "Debug", "net9.0", "flink-ir-runner.jar");
}

if (diagnosticsVerbose && File.Exists(gatewayJarPath))
{
    Console.WriteLine($"[diag] Gateway JAR configured: {gatewayJarPath}");
}

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

var builder = DistributedApplication.CreateBuilder(args);

// Use Aspire's default Kafka configuration - works for BackPressureExample
// Aspire automatically handles port allocation and container networking
// Note: Kafka resource is created but not referenced by Gateway to prevent
// Aspire from injecting connection strings that would override job definitions
var kafka = builder.AddKafka("kafka");  // AddKafka already creates 'internal' endpoint automatically

if (diagnosticsVerbose)
{
    Console.WriteLine("[diag] Kafka configured - Aspire will inject connection strings automatically");
}

// Flink JobManager with named HTTP endpoint for service references
var jobManager = builder.AddContainer("flink-jobmanager", "flink:2.1.0-java17")
    .WithHttpEndpoint(port: Ports.JobManagerHostPort, targetPort: 8081, name: "http")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithEnvironment("FLINK_PROPERTIES",
        "jobmanager.rpc.address: flink-jobmanager\n" +
        "rest.address: 0.0.0.0\n" +
        "rest.bind-address: 0.0.0.0\n" +
        "parallelism.default: 1\n" +
        "rest.port: 8081\n" +
        "rest.bind-port: 8081\n" +
        "jobmanager.memory.process.size: 1600m\n" +
        "env.java.opts.all: --add-opens=java.base/java.lang=ALL-UNNAMED --add-opens=java.base/java.net=ALL-UNNAMED --add-opens=java.base/java.io=ALL-UNNAMED --add-opens=java.base/java.nio=ALL-UNNAMED --add-opens=java.base/sun.nio.ch=ALL-UNNAMED --add-opens=java.base/java.lang.reflect=ALL-UNNAMED --add-opens=java.base/java.text=ALL-UNNAMED --add-opens=java.base/java.time=ALL-UNNAMED --add-opens=java.base/java.util=ALL-UNNAMED --add-opens=java.base/java.util.concurrent=ALL-UNNAMED --add-opens=java.base/java.util.concurrent.atomic=ALL-UNNAMED --add-opens=java.base/java.util.concurrent.locks=ALL-UNNAMED\n" +
        "classloader.resolve-order: parent-first\n" +
        "classloader.parent-first-patterns.default: org.apache.flink.;org.apache.kafka.;com.fasterxml.jackson.\n")
    .WithEnvironment("JAVA_TOOL_OPTIONS", JavaOpenOptions)
    .WithEnvironment("FLINK_CLASSPATH", "/opt/flink/usrlib/*")
    .WithBindMount(connectorsDir, "/opt/flink/usrlib", isReadOnly: true)
    .WithArgs("jobmanager")
    .WaitFor(kafka);  // Wait for Kafka to ensure network connectivity

// Flink TaskManager with increased slots for parallel test execution
builder.AddContainer("flink-taskmanager", "flink:2.1.0-java17")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithEnvironment("TASK_MANAGER_NUMBER_OF_TASK_SLOTS", "8")
    .WithEnvironment("FLINK_PROPERTIES",
        "jobmanager.rpc.address: flink-jobmanager\n" +
        "rest.address: 0.0.0.0\n" +
        "rest.bind-address: 0.0.0.0\n" +
        "parallelism.default: 1\n" +
        "taskmanager.memory.process.size: 2048m\n" +
        "taskmanager.numberOfTaskSlots: 8\n" +
        "env.java.opts.all: --add-opens=java.base/java.lang=ALL-UNNAMED --add-opens=java.base/java.net=ALL-UNNAMED --add-opens=java.base/java.io=ALL-UNNAMED --add-opens=java.base/java.nio=ALL-UNNAMED --add-opens=java.base/sun.nio.ch=ALL-UNNAMED --add-opens=java.base/java.lang.reflect=ALL-UNNAMED --add-opens=java.base/java.text=ALL-UNNAMED --add-opens=java.base/java.time=ALL-UNNAMED --add-opens=java.base/java.util=ALL-UNNAMED --add-opens=java.base/java.util.concurrent=ALL-UNNAMED --add-opens=java.base/java.util.concurrent.atomic=ALL-UNNAMED --add-opens=java.base/java.util.concurrent.locks=ALL-UNNAMED\n" +
        "classloader.resolve-order: parent-first\n" +
        "classloader.parent-first-patterns.default: org.apache.flink.;org.apache.kafka.;com.fasterxml.jackson.\n")
    .WithEnvironment("JAVA_TOOL_OPTIONS", JavaOpenOptions)
    .WithEnvironment("FLINK_CLASSPATH", "/opt/flink/usrlib/*")
    .WithBindMount(connectorsDir, "/opt/flink/usrlib", isReadOnly: true)
    .WithArgs("taskmanager")
    .WaitFor(kafka)  // Wait for Kafka first to ensure network connectivity
    .WaitFor(jobManager);  // Then wait for JobManager

// Flink.JobGateway - Add Flink Job Gateway
// IMPORTANT: Gateway needs container network address since it submits jobs to Flink containers
// Flink jobs run inside Docker containers and must use "kafka:9092" (container network name)
// NOT "localhost:port" (which only works from the host machine)

// CRITICAL: Use .WithReference() for Aspire service discovery
// Aspire automatically injects endpoint URLs as environment variables:
// services__flink-jobmanager__http__0 = "http://localhost:{dynamicPort}"
// The Gateway's DiscoverFlinkEndpoint() method (FlinkJobManager.cs line 45)
// checks for this variable first, enabling automatic Flink cluster discovery
//
// NOTE: Gateway does NOT have KAFKA_BOOTSTRAP environment variable set because:
// 1. Job definitions explicitly provide bootstrapServers (e.g., "kafka:9092")
// 2. Flink containers (JobManager/TaskManager) have KAFKA_BOOTSTRAP=kafka:9092 set
// 3. Java FlinkJobRunner jobs inherit environment from Flink containers, not Gateway
// 4. This prevents any confusion about which Kafka address to use
builder.AddProject<Projects.Flink_JobGateway>("flink-job-gateway")
    .WithHttpEndpoint(port: Ports.GatewayHostPort, name: "flink-job-gateway")
    .WithEnvironment("ASPNETCORE_URLS", $"http://localhost:{Ports.GatewayHostPort.ToString()}")  // Override launchSettings.json
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Production")  // Use Production environment
    .WithEnvironment("FLINK_CONNECTOR_PATH", connectorsDir)
    .WithEnvironment("FLINK_RUNNER_JAR_PATH", gatewayJarPath)  // Point to Release build JAR
    .WithReference(jobManager.GetEndpoint("http"))  // Reference the HTTP endpoint for service discovery
    .WaitFor(jobManager);  // Gateway only depends on Flink, not Kafka directly

#pragma warning disable S6966 // Await RunAsync instead - Required for Aspire testing framework compatibility
builder.Build().Run();
#pragma warning restore S6966






