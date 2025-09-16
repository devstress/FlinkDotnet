// Basic environment setup
Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");

// Verbose diagnostics gate
var diagnosticsVerbose = Environment.GetEnvironmentVariable("DIAGNOSTICS_VERBOSE") == "1";
if (diagnosticsVerbose)
{
    Console.WriteLine("[diag] DIAGNOSTICS_VERBOSE=1 enabled for BackPressure.Runner startup diagnostics");
}

var builder = DistributedApplication.CreateBuilder(args);

// Kafka (Aspire-provided resource, exposes connection string)
builder.AddKafka("kafka");
if (diagnosticsVerbose)
{
    Console.WriteLine("[diag] Added Kafka resource 'kafka'");
}

// Flink (JobManager + TaskManager)
var flinkJobManager = builder.AddContainer("flink-jobmanager", "flink:2.1.0")
    .WithHttpEndpoint(8081, targetPort: 8081, name: "jobmanager-ui")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithArgs("jobmanager");

builder.AddContainer("flink-taskmanager", "flink:2.1.0")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithArgs("taskmanager")
    .WaitFor(flinkJobManager);

if (diagnosticsVerbose)
{
    Console.WriteLine("[diag] Defined Flink JobManager + initial TaskManager containers");
}

// Optional: mount connector jars if present at LocalTesting/connectors/flink/lib
try
{
    var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../.."));
    var connectorsDir = Path.Combine(repoRoot, "LocalTesting", "connectors", "flink", "lib");
    if (diagnosticsVerbose)
    {
        Console.WriteLine($"[diag] Checking connectors directory: {connectorsDir} exists={Directory.Exists(connectorsDir)}");
    }
    if (Directory.Exists(connectorsDir))
    {
        flinkJobManager.WithBindMount(connectorsDir, "/opt/flink/lib");
        builder.AddContainer("flink-taskmanager", "flink:2.1.0")
            .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
            .WithArgs("taskmanager")
            .WithBindMount(connectorsDir, "/opt/flink/lib")
            .WaitFor(flinkJobManager);
        if (diagnosticsVerbose)
        {
            Console.WriteLine("[diag] Mounted connector JARs into Flink JobManager & TaskManager");
        }
    }
}
catch (Exception ex)
{
    // Swallow exceptions during Flink connector setup as it's optional
    if (diagnosticsVerbose)
    {
        Console.WriteLine($"[diag][warn] Exception during connector mount attempt (ignored): {ex.GetType().Name}: {ex.Message}");
    }
}

// Resolve and validate runner JAR path early
var runnerJarPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../FlinkIRRunner/target/flink-ir-runner.jar"));
var runnerJarExists = File.Exists(runnerJarPath);
if (diagnosticsVerbose)
{
    long size = runnerJarExists ? new FileInfo(runnerJarPath).Length : 0;
    Console.WriteLine($"[diag] FLINK_RUNNER_JAR_PATH resolved -> {runnerJarPath}; exists={runnerJarExists}; size={size} bytes");
    if (!runnerJarExists)
    {
        Console.WriteLine("[diag][warn] Flink IR Runner JAR not found. Flink job submissions will likely fail (expected in dev if not built).");
    }
}

// Flink Job Gateway (from FlinkDotNet)
builder.AddProject("flink-job-gateway", "../../FlinkDotNet/Flink.JobGateway/Flink.JobGateway.csproj")
    .WithEnvironment("ASPNETCORE_URLS", "http://0.0.0.0:8080")
    .WithEnvironment("FLINK_CLUSTER_HOST", "localhost")
    .WithEnvironment("FLINK_CLUSTER_PORT", "8081")
    .WithEnvironment("FLINK_RUNNER_JAR_PATH", runnerJarPath);

if (diagnosticsVerbose)
{
    Console.WriteLine("[diag] Added Flink Job Gateway project with environment variables set");
}

if (diagnosticsVerbose)
{
    Console.WriteLine("[diag] Building distributed application...");
}
await builder.Build().RunAsync();