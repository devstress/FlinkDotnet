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

var builder = DistributedApplication.CreateBuilder(args);

// Pre-build FlinkIRRunner JAR to avoid startup delays
try
{
    var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../.."));
    var runnerDir = Path.Combine(repoRoot, "FlinkIRRunner");
    var jarPath = Path.Combine(runnerDir, "target", "flink-ir-runner.jar");
    
    if (!File.Exists(jarPath))
    {
        if (diagnosticsVerbose) Console.WriteLine($"[diag] Pre-building FlinkIRRunner JAR at {jarPath}");
        
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "mvn",
            Arguments = "clean package -DskipTests",
            WorkingDirectory = runnerDir,
            RedirectStandardOutput = !diagnosticsVerbose,
            RedirectStandardError = !diagnosticsVerbose,
            UseShellExecute = false
        };
        
        using var process = System.Diagnostics.Process.Start(psi);
        if (process != null)
        {
            process.WaitForExit(TimeSpan.FromMinutes(2)); // 2 minute timeout
            if (process.ExitCode == 0)
            {
                if (diagnosticsVerbose) Console.WriteLine($"[diag] Successfully built FlinkIRRunner JAR");
            }
            else
            {
                if (diagnosticsVerbose) Console.WriteLine($"[diag][warn] FlinkIRRunner JAR build failed with exit code {process.ExitCode}");
            }
        }
    }
    else
    {
        if (diagnosticsVerbose) Console.WriteLine($"[diag] FlinkIRRunner JAR already exists at {jarPath}");
    }
}
catch (Exception ex) 
{ 
    if (diagnosticsVerbose) Console.WriteLine($"[diag][warn] JAR pre-build failed: {ex.Message}"); 
}

// Ensure connector directory exists (used when real Flink runs)
try
{
    var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../.."));
    var connectorsDir = Path.Combine(repoRoot, "LocalTesting", "connectors", "flink", "lib");
    Directory.CreateDirectory(connectorsDir);
}
catch (Exception ex) { if (diagnosticsVerbose) Console.WriteLine($"[diag][warn] Connector dir prep failed: {ex.Message}"); }

// Set up Kafka with optimized configuration for LocalTesting
builder.AddKafka("kafka")
    .WithEnvironment("KAFKA_REST_SCHEMA_REGISTRY_URL", "")
    .WithEnvironment("SCHEMA_REGISTRY_URL", "")
    .WithEnvironment("KAFKA_UNUSED_SUPPRESS", "1")
    .WithEnvironment("KAFKA_HEAP_OPTS", "-Xmx1G -Xms1G");

// Set up Flink JobManager (single instance) with compatible JVM options
var jobManager = builder.AddContainer("flink-jobmanager", "flink:2.1.0")
    .WithHttpEndpoint(8081, targetPort: 8081, name: "jobmanager-ui")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithEnvironment("KAFKA_BOOTSTRAP", "kafka:9092")
    .WithEnvironment("FLINK_PROPERTIES", 
        "jobmanager.rpc.address: flink-jobmanager\n" +
        "parallelism.default: 1\n" +
        "rest.port: 8081\n" +
        "rest.bind-port: 8081\n" +
        "jobmanager.memory.process.size: 1600m\n" +
        "env.java.opts.all: --add-opens=java.base/java.lang=ALL-UNNAMED --add-opens=java.base/java.net=ALL-UNNAMED --add-opens=java.base/java.io=ALL-UNNAMED --add-opens=java.base/java.nio=ALL-UNNAMED --add-opens=java.base/sun.nio.ch=ALL-UNNAMED --add-opens=java.base/java.lang.reflect=ALL-UNNAMED --add-opens=java.base/java.text=ALL-UNNAMED --add-opens=java.base/java.time=ALL-UNNAMED --add-opens=java.base/java.util=ALL-UNNAMED --add-opens=java.base/java.util.concurrent=ALL-UNNAMED --add-opens=java.base/java.util.concurrent.atomic=ALL-UNNAMED --add-opens=java.base/java.util.concurrent.locks=ALL-UNNAMED\n")
    .WithArgs("jobmanager");

// Set up Flink TaskManager (single instance) with compatible JVM options  
builder.AddContainer("flink-taskmanager", "flink:2.1.0")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithEnvironment("KAFKA_BOOTSTRAP", "kafka:9092")
    .WithEnvironment("TASK_MANAGER_NUMBER_OF_TASK_SLOTS", "2") // Allow parallel processing
    .WithEnvironment("FLINK_PROPERTIES", 
        "jobmanager.rpc.address: flink-jobmanager\n" +
        "parallelism.default: 1\n" +
        "taskmanager.memory.process.size: 1728m\n" +
        "taskmanager.numberOfTaskSlots: 2\n" +
        "env.java.opts.all: --add-opens=java.base/java.lang=ALL-UNNAMED --add-opens=java.base/java.net=ALL-UNNAMED --add-opens=java.base/java.io=ALL-UNNAMED --add-opens=java.base/java.nio=ALL-UNNAMED --add-opens=java.base/sun.nio.ch=ALL-UNNAMED --add-opens=java.base/java.lang.reflect=ALL-UNNAMED --add-opens=java.base/java.text=ALL-UNNAMED --add-opens=java.base/java.time=ALL-UNNAMED --add-opens=java.base/java.util=ALL-UNNAMED --add-opens=java.base/java.util.concurrent=ALL-UNNAMED --add-opens=java.base/java.util.concurrent.atomic=ALL-UNNAMED --add-opens=java.base/java.util.concurrent.locks=ALL-UNNAMED\n")
    .WithArgs("taskmanager")
    .WaitFor(jobManager);

// Set up FlinkDotnet Gateway
// Gateway now determines jar paths internally and builds on demand
var gatewayRepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../.."));
var connectorsPath = Path.Combine(gatewayRepoRoot, "LocalTesting", "connectors", "flink", "lib");

builder.AddProject("flink-job-gateway", "../../FlinkDotNet/Flink.JobGateway/Flink.JobGateway.csproj")
    .WithEnvironment("ASPNETCORE_URLS", "http://0.0.0.0:8080")
    .WithEnvironment("FLINK_CLUSTER_HOST", "flink-jobmanager")
    .WithEnvironment("FLINK_CLUSTER_PORT", "8081")
    .WithEnvironment("FLINK_CONNECTOR_PATH", connectorsPath)
    .WithEnvironment("KAFKA_BOOTSTRAP", "kafka:9092")
    .WaitFor(jobManager);

await builder.Build().RunAsync();