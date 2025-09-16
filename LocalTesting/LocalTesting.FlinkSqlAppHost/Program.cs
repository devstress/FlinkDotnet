// Basic environment setup
Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");

var diagnosticsVerbose = Environment.GetEnvironmentVariable("DIAGNOSTICS_VERBOSE") == "1";
if (diagnosticsVerbose)
{
    Console.WriteLine("[diag] DIAGNOSTICS_VERBOSE=1 enabled for LocalTesting.FlinkSqlAppHost startup diagnostics");
}

var builder = DistributedApplication.CreateBuilder(args);

// Ensure connector directory exists (used when real Flink runs)
try
{
    var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../.."));
    var connectorsDir = Path.Combine(repoRoot, "LocalTesting", "connectors", "flink", "lib");
    Directory.CreateDirectory(connectorsDir);
}
catch (Exception ex) { if (diagnosticsVerbose) Console.WriteLine($"[diag][warn] Connector dir prep failed: {ex.Message}"); }

// Set up Kafka (single instance)
builder.AddKafka("kafka")
    .WithEnvironment("KAFKA_REST_SCHEMA_REGISTRY_URL", "")
    .WithEnvironment("SCHEMA_REGISTRY_URL", "")
    .WithEnvironment("KAFKA_UNUSED_SUPPRESS", "1");

// Set up Flink JobManager (single instance)
var jobManager = builder.AddContainer("flink-jobmanager", "flink:2.1.0")
    .WithHttpEndpoint(8081, targetPort: 8081, name: "jobmanager-ui")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithEnvironment("KAFKA_BOOTSTRAP", "kafka:9092")
    .WithEnvironment("FLINK_PROPERTIES", "jobmanager.rpc.address: flink-jobmanager\nparallelism.default: 1\nrest.port: 8081\n")
    .WithArgs("jobmanager");

// Set up Flink TaskManager (single instance)
builder.AddContainer("flink-taskmanager", "flink:2.1.0")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithEnvironment("KAFKA_BOOTSTRAP", "kafka:9092")
    .WithEnvironment("TASK_MANAGER_NUMBER_OF_TASK_SLOTS", "1") // Single task slot
    .WithEnvironment("FLINK_PROPERTIES", "jobmanager.rpc.address: flink-jobmanager\nparallelism.default: 1\n")
    .WithArgs("taskmanager")
    .WaitFor(jobManager);

// Set up FlinkDotnet Gateway
var runnerJarPath = "/app/flink-ir-runner.jar";
var gateway = builder.AddProject("flink-job-gateway", "../../FlinkDotNet/Flink.JobGateway/Flink.JobGateway.csproj")
    .WithEnvironment("ASPNETCORE_URLS", "http://0.0.0.0:8080")
    .WithEnvironment("FLINK_CLUSTER_HOST", "flink-jobmanager")
    .WithEnvironment("FLINK_CLUSTER_PORT", "8081")
    .WithEnvironment("FLINK_RUNNER_JAR_PATH", runnerJarPath)
    .WithEnvironment("KAFKA_BOOTSTRAP", "kafka:9092")
    .WaitFor(jobManager);

await builder.Build().RunAsync();