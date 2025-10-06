// Configure container runtime - prefer Podman if available, fallback to Docker Desktop
using System.Diagnostics;
using LocalTesting.FlinkSqlAppHost;

if (!ConfigureContainerRuntime())
{
    return;
}

// Ensure custom Docker network exists for DNS resolution between containers
// This is CRITICAL for Flink containers to resolve 'kafka' hostname
EnsureDockerNetworkExists("aspire-flink-network");

LogConfiguredPorts();
SetupEnvironment();

var diagnosticsVerbose = Environment.GetEnvironmentVariable("DIAGNOSTICS_VERBOSE") == "1";
if (diagnosticsVerbose)
{
    Console.WriteLine("[diag] DIAGNOSTICS_VERBOSE=1 enabled for LocalTesting.FlinkSqlAppHost startup diagnostics");
}

const string JavaOpenOptions = "--add-opens=java.base/java.lang=ALL-UNNAMED --add-opens=java.base/java.net=ALL-UNNAMED --add-opens=java.base/java.io=ALL-UNNAMED --add-opens=java.base/java.nio=ALL-UNNAMED --add-opens=java.base/sun.nio.ch=ALL-UNNAMED --add-opens=java.base/java.lang.reflect=ALL-UNNAMED --add-opens=java.base/java.text=ALL-UNNAMED --add-opens=java.base/java.time=ALL-UNNAMED --add-opens=java.base/java.util=ALL-UNNAMED --add-opens=java.base/java.util.concurrent=ALL-UNNAMED --add-opens=java.base/java.util.concurrent.atomic=ALL-UNNAMED --add-opens=java.base/java.util.concurrent.locks=ALL-UNNAMED";

var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
var connectorsDir = Path.Combine(repoRoot, "LocalTesting", "connectors", "flink", "lib");

var gatewayJarPath = FindGatewayJarPath(repoRoot);
if (diagnosticsVerbose && File.Exists(gatewayJarPath))
{
    Console.WriteLine($"[diag] Gateway JAR configured: {gatewayJarPath}");
}

PrepareConnectorDirectory(connectorsDir, diagnosticsVerbose);

var builder = DistributedApplication.CreateBuilder(args);

// Configure Kafka with FIXED external port 9093
// Both tests and Flink jobs connect to localhost:9093 (mapped to container port 9092)
#pragma warning disable S1481 // Kafka resource is created but not directly referenced - used via connection string
var kafka = builder.AddKafka("kafka");
#pragma warning restore S1481

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
    // REMOVED: .WithEnvironment("KAFKA_BOOTSTRAP", "kafka:9092")
    // REASON: FlinkJobRunner.java prioritizes environment variable over job definition
    // This caused jobs to use wrong Kafka address (localhost:17901 instead of kafka:9092)
    // Job definitions explicitly provide bootstrapServers, so environment variable is not needed
    .WithEnvironment("FLINK_PROPERTIES",
        "jobmanager.rpc.address: flink-jobmanager\n" +
        "rest.address: 0.0.0.0\n" +
        "rest.bind-address: 0.0.0.0\n" +
        "parallelism.default: 1\n" +
        "rest.port: 8081\n" +
        "rest.bind-port: 8081\n" +
        "jobmanager.memory.process.size: 2048m\n" +
        "heartbeat.interval: 5000\n" +
        "heartbeat.timeout: 30000\n" +
        "pekko.ask.timeout: 30s\n" +
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
        "taskmanager.memory.process.size: 3072m\n" +
        "taskmanager.numberOfTaskSlots: 10\n" +
        "heartbeat.interval: 5000\n" +
        "heartbeat.timeout: 30000\n" +
        "pekko.ask.timeout: 30s\n" +
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
        "sql-gateway.endpoint.type: rest\n" +
        "sql-gateway.session.check-interval: 60000\n" +
        "sql-gateway.session.idle-timeout: 600000\n" +
        "sql-gateway.worker.threads.max: 10\n" +
        "env.java.opts.all: --add-opens=java.base/java.lang=ALL-UNNAMED --add-opens=java.base/java.net=ALL-UNNAMED --add-opens=java.base/java.io=ALL-UNNAMED --add-opens=java.base/java.nio=ALL-UNNAMED --add-opens=java.base/sun.nio.ch=ALL-UNNAMED --add-opens=java.base/java.lang.reflect=ALL-UNNAMED --add-opens=java.base/java.text=ALL-UNNAMED --add-opens=java.base/java.time=ALL-UNNAMED --add-opens=java.base/java.util=ALL-UNNAMED --add-opens=java.base/java.util.concurrent=ALL-UNNAMED --add-opens=java.base/java.util.concurrent.atomic=ALL-UNNAMED --add-opens=java.base/java.util.concurrent.locks=ALL-UNNAMED\n")
    .WithEnvironment("JAVA_TOOL_OPTIONS", JavaOpenOptions)
    .WithBindMount(Path.Combine(connectorsDir, "flink-sql-connector-kafka-4.0.1-2.0.jar"), "/opt/flink/lib/flink-sql-connector-kafka-4.0.1-2.0.jar", isReadOnly: true)
    .WithBindMount(Path.Combine(connectorsDir, "flink-json-2.1.0.jar"), "/opt/flink/lib/flink-json-2.1.0.jar", isReadOnly: true)
    .WithArgs("/opt/flink/bin/sql-gateway.sh", "start-foreground");

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
    .WithHttpEndpoint(name: "gateway-http")
    .WithEnvironment("ASPNETCORE_URLS", $"http://localhost:{Ports.GatewayHostPort.ToString()}")  // Override launchSettings.json
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Production")  // Use Production environment
    .WithEnvironment("FLINK_CONNECTOR_PATH", connectorsDir)
    .WithEnvironment("FLINK_RUNNER_JAR_PATH", gatewayJarPath)  // Point to Release build JAR
    .WithReference(jobManager.GetEndpoint("jm-http"))  // Reference JobManager for standard job submission
    .WithReference(sqlGateway.GetEndpoint("sg-http"));  // Reference SQL Gateway for direct SQL execution

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

static void EnsureDockerNetworkExists(string networkName)
{
    try
    {
        // Check if network already exists
        var checkPsi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"network inspect {networkName}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var checkProcess = Process.Start(checkPsi);
        if (checkProcess != null)
        {
            checkProcess.StandardOutput.ReadToEnd();
            checkProcess.WaitForExit(5000);
            
            if (checkProcess.ExitCode == 0)
            {
                Console.WriteLine($"✅ Docker network '{networkName}' already exists");
                return;
            }
        }

        // Network doesn't exist, create it
        var createPsi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"network create {networkName}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var createProcess = Process.Start(createPsi);
        if (createProcess != null)
        {
            createProcess.StandardOutput.ReadToEnd(); // Consume output
            var error = createProcess.StandardError.ReadToEnd();
            createProcess.WaitForExit(5000);
            
            if (createProcess.ExitCode == 0)
            {
                Console.WriteLine($"✅ Created Docker network '{networkName}' for DNS resolution");
            }
            else
            {
                Console.WriteLine($"⚠️ Failed to create network '{networkName}': {error}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Error managing Docker network: {ex.Message}");
    }
}