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

// Temporal (auto-setup for local dev)
builder.AddContainer("temporal", "temporalio/auto-setup:1.22")
    .WithEndpoint(7233, targetPort: 7233, name: "temporal-grpc")
    .WithEnvironment("DB", "sqlite");

// Flink Job Gateway (from FlinkDotNet)
builder.AddProject("flink-job-gateway", "../../FlinkDotNet/Flink.JobGateway/Flink.JobGateway.csproj")
    .WithEnvironment("ASPNETCORE_URLS", "http://0.0.0.0:8080");

await builder.Build().RunAsync();
