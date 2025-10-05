// Configure container runtime - prefer Podman if available, fallback to Docker Desktop
using System.Diagnostics;
using LocalTesting.FlinkSqlAppHost;

if (IsPodmanAvailable())
{
    Console.WriteLine("✅ Using Podman as container runtime");
    Environment.SetEnvironmentVariable("ASPIRE_CONTAINER_RUNTIME", "podman");
    
    // Set DOCKER_HOST to Podman socket for better compatibility
    SetPodmanDockerHost();
}
else if (IsDockerAvailable())
{
    Console.WriteLine("✅ Using Docker Desktop as container runtime");
    // Docker Desktop is the default, no need to set ASPIRE_CONTAINER_RUNTIME
}
else
{
    Console.WriteLine("❌ No container runtime found. Please install Docker Desktop or Podman.");
    return;
}

// Log configured ports for debugging
Console.WriteLine($"📍 Configured ports:");
Console.WriteLine($"   - Flink JobManager: {Ports.JobManagerHostPort}");
Console.WriteLine($"   - Gateway: {Ports.GatewayHostPort}");
Console.WriteLine($"   - Kafka: {Ports.KafkaPort}");

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

// Configure Gateway JAR path to use Release build (Java 17 version)
var gatewayJarPath = Path.Combine(repoRoot, "FlinkDotNet", "Flink.JobGateway", "bin", "Release", "net9.0", "flink-ir-runner-java17.jar");
if (!File.Exists(gatewayJarPath))
{
    // Fallback to Debug if Release not found
    gatewayJarPath = Path.Combine(repoRoot, "FlinkDotNet", "Flink.JobGateway", "bin", "Debug", "net9.0", "flink-ir-runner-java17.jar");
    
    // Final fallback to legacy naming convention
    if (!File.Exists(gatewayJarPath))
    {
        gatewayJarPath = Path.Combine(repoRoot, "FlinkDotNet", "Flink.JobGateway", "bin", "Release", "net9.0", "flink-ir-runner.jar");
        if (!File.Exists(gatewayJarPath))
        {
            gatewayJarPath = Path.Combine(repoRoot, "FlinkDotNet", "Flink.JobGateway", "bin", "Debug", "net9.0", "flink-ir-runner.jar");
        }
    }
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
builder.AddKafka("kafka");  // AddKafka already creates 'internal' endpoint automatically

if (diagnosticsVerbose)
{
    Console.WriteLine("[diag] Kafka configured - Aspire will inject connection strings automatically");
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
    .WithBindMount(Path.Combine(connectorsDir, "flink-sql-connector-kafka-4.0.1-2.0.jar"), "/opt/flink/lib/flink-sql-connector-kafka-4.0.1-2.0.jar", isReadOnly: true)
    .WithBindMount(Path.Combine(connectorsDir, "flink-json-2.1.0.jar"), "/opt/flink/lib/flink-json-2.1.0.jar", isReadOnly: true)
    .WithArgs("jobmanager");

// Flink TaskManager with increased slots for parallel test execution (10 tests)
// All ports are hardcoded - no WaitFor dependencies needed for parallel startup
builder.AddContainer("flink-taskmanager", "flink:2.1.0-java17")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithEnvironment("TASK_MANAGER_NUMBER_OF_TASK_SLOTS", "10")
    .WithEnvironment("FLINK_PROPERTIES",
        "jobmanager.rpc.address: flink-jobmanager\n" +
        "rest.address: 0.0.0.0\n" +
        "rest.bind-address: 0.0.0.0\n" +
        "parallelism.default: 1\n" +
        "taskmanager.memory.process.size: 2048m\n" +
        "taskmanager.numberOfTaskSlots: 10\n" +
        "env.java.opts.all: --add-opens=java.base/java.lang=ALL-UNNAMED --add-opens=java.base/java.net=ALL-UNNAMED --add-opens=java.base/java.io=ALL-UNNAMED --add-opens=java.base/java.nio=ALL-UNNAMED --add-opens=java.base/sun.nio.ch=ALL-UNNAMED --add-opens=java.base/java.lang.reflect=ALL-UNNAMED --add-opens=java.base/java.text=ALL-UNNAMED --add-opens=java.base/java.time=ALL-UNNAMED --add-opens=java.base/java.util=ALL-UNNAMED --add-opens=java.base/java.util.concurrent=ALL-UNNAMED --add-opens=java.base/java.util.concurrent.atomic=ALL-UNNAMED --add-opens=java.base/java.util.concurrent.locks=ALL-UNNAMED\n" +
        "classloader.resolve-order: parent-first\n" +
        "classloader.parent-first-patterns.default: org.apache.flink.;org.apache.kafka.;com.fasterxml.jackson.\n")
    .WithEnvironment("JAVA_TOOL_OPTIONS", JavaOpenOptions)
    .WithBindMount(Path.Combine(connectorsDir, "flink-sql-connector-kafka-4.0.1-2.0.jar"), "/opt/flink/lib/flink-sql-connector-kafka-4.0.1-2.0.jar", isReadOnly: true)
    .WithBindMount(Path.Combine(connectorsDir, "flink-json-2.1.0.jar"), "/opt/flink/lib/flink-json-2.1.0.jar", isReadOnly: true)
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

var sqlGateway = sqlGatewayBuilder
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithEnvironment("FLINK_PROPERTIES",
        "jobmanager.rpc.address: flink-jobmanager\n" +
        "rest.address: flink-jobmanager\n" +
        "rest.port: 8081\n" +
        "sql-gateway.endpoint.rest.address: 0.0.0.0\n" +
        "sql-gateway.endpoint.rest.bind-address: 0.0.0.0\n" +
        "sql-gateway.endpoint.rest.port: 8083\n" +
        "sql-gateway.endpoint.rest.bind-port: 8083\n" +
        "sql-gateway.endpoint.type: remote\n" +
        "sql-gateway.session.check-interval: 60000\n" +
        "sql-gateway.session.idle-timeout: 600000\n" +
        "sql-gateway.worker.threads.max: 10\n" +
        "env.java.opts.all: --add-opens=java.base/java.lang=ALL-UNNAMED --add-opens=java.base/java.net=ALL-UNNAMED --add-opens=java.base/java.io=ALL-UNNAMED --add-opens=java.base/java.nio=ALL-UNNAMED --add-opens=java.base/sun.nio.ch=ALL-UNNAMED --add-opens=java.base/java.lang.reflect=ALL-UNNAMED --add-opens=java.base/java.text=ALL-UNNAMED --add-opens=java.base/java.time=ALL-UNNAMED --add-opens=java.base/java.util=ALL-UNNAMED --add-opens=java.base/java.util.concurrent=ALL-UNNAMED --add-opens=java.base/java.util.concurrent.atomic=ALL-UNNAMED --add-opens=java.base/java.util.concurrent.locks=ALL-UNNAMED\n")
    .WithEnvironment("JAVA_TOOL_OPTIONS", JavaOpenOptions)
    .WithBindMount(Path.Combine(connectorsDir, "flink-sql-connector-kafka-4.0.1-2.0.jar"), "/opt/flink/lib/flink-sql-connector-kafka-4.0.1-2.0.jar", isReadOnly: true)
    .WithBindMount(Path.Combine(connectorsDir, "flink-json-2.1.0.jar"), "/opt/flink/lib/flink-json-2.1.0.jar", isReadOnly: true)
    .WithArgs("sql-gateway");

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
// Gateway with service reference for Flink discovery
// All ports are hardcoded - no WaitFor dependencies needed for parallel startup
builder.AddProject<Projects.Flink_JobGateway>("flink-job-gateway")
    .WithHttpEndpoint(port: Ports.GatewayHostPort, name: "flink-job-gateway")
    .WithEnvironment("ASPNETCORE_URLS", $"http://localhost:{Ports.GatewayHostPort.ToString()}")  // Override launchSettings.json
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Production")  // Use Production environment
    .WithEnvironment("FLINK_CONNECTOR_PATH", connectorsDir)
    .WithEnvironment("FLINK_RUNNER_JAR_PATH", gatewayJarPath)  // Point to Release build JAR
    .WithReference(jobManager.GetEndpoint("jm-http"))  // Reference JobManager for standard job submission
    .WithReference(sqlGateway.GetEndpoint("sg-http"));  // Reference SQL Gateway for direct SQL execution

#pragma warning disable S6966 // Await RunAsync instead - Required for Aspire testing framework compatibility
builder.Build().Run();
#pragma warning restore S6966

static bool IsPodmanAvailable()
{
    try
    {
        // First check if podman command exists
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
        
        if (versionProcess?.ExitCode != 0)
        {
            return false;
        }

        // Check if Podman machine is running (required on Windows/macOS)
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
        if (machineProcess != null)
        {
            var output = machineProcess.StandardOutput.ReadToEnd();
            machineProcess.WaitForExit(5000);
            
            // If machine list shows "true", Podman machine is running
            if (output.Contains("true", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("   ℹ️ Podman machine is running");
                return true;
            }
            else if (!string.IsNullOrWhiteSpace(output))
            {
                Console.WriteLine("   ⚠️ Podman machine is not running. Start with: podman machine start");
                return false;
            }
        }

        // On Linux, Podman runs natively without a machine
        // If we got here and machine list had no output, assume Linux
        Console.WriteLine("   ℹ️ Podman detected (native mode)");
        return true;
    }
    catch
    {
        return false;
    }
}

static bool IsDockerAvailable()
{
    try
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = "version",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        process?.WaitForExit(5000);
        return process?.ExitCode == 0;
    }
    catch
    {
        return false;
    }
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