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

// Minimal Kafka setup on 9092 using resource name 'messaging'
var kafka = builder.AddKafka("messaging", Ports.KafkaPort);

if (diagnosticsVerbose)
{
    Console.WriteLine("[diag] Kafka configured for external access on localhost:" + Ports.KafkaPort);
}

var includeGatewaySetting = Environment.GetEnvironmentVariable("INCLUDE_FLINK_GATEWAY");
var includeGateway = includeGatewaySetting switch
{
    null => true,
    "" => true,
    var s when string.Equals(s, "0", StringComparison.OrdinalIgnoreCase) => false,
    var s when string.Equals(s, "false", StringComparison.OrdinalIgnoreCase) => false,
    var s when string.Equals(s, "no", StringComparison.OrdinalIgnoreCase) => false,
    _ => true
};

// Flink JobManager
var jobManager = builder.AddContainer("flink-jobmanager", "flink:2.1.0-java17")
    .WithHttpEndpoint(name: "jobmanager-ui", targetPort: Ports.JobManagerHostPort)
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithEnvironment("KAFKA_BOOTSTRAP", "kafka:9092")
    .WithEnvironment("FLINK_PROPERTIES",
        "jobmanager.rpc.address: flink-jobmanager\n" +
        "rest.address: 0.0.0.0\n" +
        "rest.bind-address: 0.0.0.0\n" +
        "parallelism.default: 1\n" +
        "rest.port: 8081\n" +
        "rest.bind-port: 8081\n" +
        "jobmanager.memory.process.size: 1600m\n" +
        "env.java.opts.all: --add-opens=java.base/java.lang=ALL-UNNAMED --add-opens=java.base/java.net=ALL-UNNAMED --add-opens=java.base/java.io=ALL-UNNAMED --add-opens=java.base/java.nio=ALL-UNNAMED --add-opens=java.base/sun.nio.ch=ALL-UNNAMED --add-opens=java.base/java.lang.reflect=ALL-UNNAMED --add-opens=java.base/java.text=ALL-UNNAMED --add-opens=java.base/java.time=ALL-UNNAMED --add-opens=java.base/java.util=ALL-UNNAMED --add-opens=java.base/java.util.concurrent=ALL-UNNAMED --add-opens=java.base/java.util.concurrent.atomic=ALL-UNNAMED --add-opens=java.base/java.util.concurrent.locks=ALL-UNNAMED\n")
    .WithEnvironment("JAVA_TOOL_OPTIONS", JavaOpenOptions)
    .WithBindMount(connectorsDir, "/opt/flink/usrlib", isReadOnly: true)
    .WithArgs("jobmanager");

// Flink TaskManager
builder.AddContainer("flink-taskmanager", "flink:2.1.0-java17")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithEnvironment("KAFKA_BOOTSTRAP", "kafka:9092")
    .WithEnvironment("TASK_MANAGER_NUMBER_OF_TASK_SLOTS", "2")
    .WithEnvironment("FLINK_PROPERTIES",
        "jobmanager.rpc.address: flink-jobmanager\n" +
        "rest.address: 0.0.0.0\n" +
        "rest.bind-address: 0.0.0.0\n" +
        "parallelism.default: 1\n" +
        "taskmanager.memory.process.size: 1728m\n" +
        "taskmanager.numberOfTaskSlots: 2\n" +
        "env.java.opts.all: --add-opens=java.base/java.lang=ALL-UNNAMED --add-opens=java.base/java.net=ALL-UNNAMED --add-opens=java.base/java.io=ALL-UNNAMED --add-opens=java.base/java.nio=ALL-UNNAMED --add-opens=java.base/sun.nio.ch=ALL-UNNAMED --add-opens=java.base/java.lang.reflect=ALL-UNNAMED --add-opens=java.base/java.text=ALL-UNNAMED --add-opens=java.base/java.time=ALL-UNNAMED --add-opens=java.base/java.util=ALL-UNNAMED --add-opens=java.base/java.util.concurrent=ALL-UNNAMED --add-opens=java.base/java.util.concurrent.atomic=ALL-UNNAMED --add-opens=java.base/java.util.concurrent.locks=ALL-UNNAMED\n")
    .WithEnvironment("JAVA_TOOL_OPTIONS", JavaOpenOptions)
    .WithBindMount(connectorsDir, "/opt/flink/usrlib", isReadOnly: true)
    .WithArgs("taskmanager")
    .WaitFor(jobManager);

if (includeGateway)
{
    builder.AddProject("flink-job-gateway", "../../FlinkDotNet/Flink.JobGateway/Flink.JobGateway.csproj")
        .WithHttpEndpoint(name: "flink-job-gateway", targetPort: Ports.GatewayHostPort)
        .WithEnvironment("ASPNETCORE_URLS", "http://0.0.0.0:" + Ports.GatewayHostPort)
        .WithEnvironment("FLINK_CLUSTER_HOST", "localhost")
        .WithEnvironment("FLINK_CLUSTER_PORT", Ports.JobManagerHostPort.ToString())
        .WithEnvironment("FLINK_CONNECTOR_PATH", connectorsDir)
        .WithEnvironment("KAFKA_BOOTSTRAP", "kafka:9092")
        .WaitFor(jobManager)
        .WaitFor(kafka);
}
else if (diagnosticsVerbose)
{
    Console.WriteLine("[diag] INCLUDE_FLINK_GATEWAY toggled off; skipping Flink.JobGateway start");
}

await builder.Build().RunAsync();






