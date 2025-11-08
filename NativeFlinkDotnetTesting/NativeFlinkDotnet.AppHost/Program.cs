// NativeFlinkDotnet AppHost - Pure .NET distributed processing with Temporal
// No Java Flink dependencies - uses native JobManager and TaskManager

const string NativeFlinkDotnetName = "NativeFlinkDotnetTesting";
const string LogFilePathEnv = "LOG_FILE_PATH";

LogConfiguredPorts();
SetupEnvironment();

string repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
string testLogsDir = Path.GetFullPath(Path.Combine(repoRoot, NativeFlinkDotnetName, "test-logs"));

// Ensure test-logs directory exists
Directory.CreateDirectory(testLogsDir);

Environment.SetEnvironmentVariable(LogFilePathEnv, testLogsDir);
Console.WriteLine($"[INFO] Log files will be written to: {testLogsDir}");

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);
Console.WriteLine("[INFO] Building NativeFlinkDotnet AppHost");
Console.WriteLine("[INFO] Architecture: Pure .NET with Temporal (No Java Flink)");

// Configure Kafka for message streaming
var kafka = builder.AddKafka("kafka")
    .WithKafkaUI()
    .WithLifetime(ContainerLifetime.Persistent);

Console.WriteLine("[INFO] ✓ Kafka configured");

// Configure Temporal server for workflow orchestration and state management
_ = builder.AddContainer("temporal", "temporalio/auto-setup", "latest")
    .WithHttpEndpoint(port: 7233, targetPort: 7233, name: "frontend")
    .WithHttpEndpoint(port: 8233, targetPort: 8233, name: "web-ui")
    .WithEnvironment("TEMPORAL_ADDRESS", "0.0.0.0:7233")
    .WithLifetime(ContainerLifetime.Persistent);

Console.WriteLine("[INFO] ✓ Temporal server configured");

// Add JobManager - coordinates job execution and resource allocation
var jobManager = builder.AddProject<Projects.FlinkDotNet_JobManager>("jobmanager")
    .WithReference(kafka)
    .WithEnvironment("TEMPORAL_HOST", "temporal")
    .WithEnvironment("TEMPORAL_PORT", "7233")
    .WithEnvironment("KAFKA_BOOTSTRAP_SERVERS", kafka.Resource.ConnectionStringExpression)
    .WithHttpEndpoint(port: 8081, targetPort: 8080, name: "rest-api");

Console.WriteLine("[INFO] ✓ JobManager configured");

// Add TaskManager instances - execute data processing tasks
// Start with 2 TaskManager instances, each with 4 slots = 8 total slots
for (int i = 1; i <= 2; i++)
{
    var taskManagerId = "tm-" + i.ToString();
    _ = builder.AddProject<Projects.FlinkDotNet_TaskManager>($"taskmanager-{i}")
        .WithReference(kafka)
        .WithReference(jobManager)
        .WithEnvironment("TEMPORAL_HOST", "temporal")
        .WithEnvironment("TEMPORAL_PORT", "7233")
        .WithEnvironment("TASKMANAGER_ID", taskManagerId)
        .WithEnvironment("TASKMANAGER_SLOTS", "4")
        .WithEnvironment("JOBMANAGER_HOST", "jobmanager")
        .WithEnvironment("JOBMANAGER_PORT", "8081")
        .WithEnvironment("KAFKA_BOOTSTRAP_SERVERS", kafka.Resource.ConnectionStringExpression);

    Console.WriteLine($"[INFO] ✓ TaskManager-{i} configured (4 slots)");
}

Console.WriteLine("[INFO] Total execution capacity: 8 parallel slots");
Console.WriteLine("[INFO] ✓ All components configured successfully");
Console.WriteLine("[INFO] Starting NativeFlinkDotnet cluster...");

builder.Build().Run();

static void LogConfiguredPorts()
{
    Console.WriteLine("[INFO] NativeFlinkDotnet Port Configuration:");
    Console.WriteLine("  - Kafka: 9092 (broker), 9093 (UI)");
    Console.WriteLine("  - Temporal: 7233 (frontend), 8233 (Web UI)");
    Console.WriteLine("  - JobManager: 8081 (REST API)");
}

static void SetupEnvironment()
{
    // Set up any required environment variables
    var aspireEnv = Environment.GetEnvironmentVariable("ASPIRE_ENVIRONMENT");
    Console.WriteLine($"[INFO] ASPIRE_ENVIRONMENT = {aspireEnv ?? "Development"}");
}
