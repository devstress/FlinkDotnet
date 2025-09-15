// Basic environment setup
Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");

var builder = DistributedApplication.CreateBuilder(args);

// Kafka (Aspire-provided resource, exposes connection string)
builder.AddKafka("kafka");

// Flink (JobManager + TaskManager)
var flinkJobManager = builder.AddContainer("flink-jobmanager", "flink:2.1.0")
    .WithHttpEndpoint(8081, targetPort: 8081, name: "jobmanager-ui")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithArgs("jobmanager");

builder.AddContainer("flink-taskmanager", "flink:2.1.0")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithArgs("taskmanager")
    .WaitFor(flinkJobManager);

// Optional: mount connector jars if present at LocalTesting/connectors/flink/lib
try
{
    var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../.."));
    var connectorsDir = Path.Combine(repoRoot, "LocalTesting", "connectors", "flink", "lib");
    if (Directory.Exists(connectorsDir))
    {
        flinkJobManager.WithBindMount(connectorsDir, "/opt/flink/lib");
        builder.AddContainer("flink-taskmanager", "flink:2.1.0")
            .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
            .WithArgs("taskmanager")
            .WithBindMount(connectorsDir, "/opt/flink/lib")
            .WaitFor(flinkJobManager);
    }
}
catch
{
    // Swallow exceptions during Flink connector setup as it's optional
}

// Flink Job Gateway (from FlinkDotNet)
builder.AddProject("flink-job-gateway", "../../FlinkDotNet/Flink.JobGateway/Flink.JobGateway.csproj")
    .WithEnvironment("ASPNETCORE_URLS", "http://0.0.0.0:8080")
    .WithEnvironment("FLINK_CLUSTER_HOST", "localhost")
    .WithEnvironment("FLINK_CLUSTER_PORT", "8081");

await builder.Build().RunAsync();
