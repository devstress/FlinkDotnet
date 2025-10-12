# Quick Start (LocalTesting)

This guide shows how to run the end-to-end LocalTesting setup and submit a simple job.

## Prerequisites
- .NET 9 SDK
- Docker Desktop (Linux containers)
- Java 17 and Maven (auto-installed if not found, required to build `FlinkDotNet.JobGateway`, which prebuilds the IR Runner jar)
- Optional connectors copied to `LocalTesting/connectors/flink/lib/` if your job needs extra Flink SQL libraries (the Flink Job Gateway bundles them automatically)

## Run LocalTesting Integration

1) Build the solution (this builds `FlinkDotNet.JobGateway` and prebuilds/bundles `flink-ir-runner.jar`)
```
dotnet build FlinkDotNet/FlinkDotNet.sln -c Debug
```

2) Start the Aspire host that brings up Kafka, Flink, and the Flink Job Gateway (leave it running)
```
dotnet run --project LocalTesting/LocalTesting.FlinkSqlAppHost/LocalTesting.FlinkSqlAppHost.csproj
```

3) In a separate shell, run the gateway bundling integration test which submits a Flink job end-to-end
```
dotnet test LocalTesting/LocalTesting.IntegrationTests/LocalTesting.IntegrationTests.csproj -c Debug --filter TestCategory=gateway-bundling
```

The test ensures:
- Kafka is reachable and topics are created
- Flink JobManager REST endpoint is healthy
- Gateway builds the job bundle and the submission succeeds (messages flow input ➜ output)

## Submitting a Job Programmatically

```
var job = new JobDefinition {
  Metadata = new JobMetadata { JobId = Guid.NewGuid().ToString("n"), Version = "1.0", Parallelism = 1 },
  Source = new KafkaSourceDefinition { Topic = "input" },
  Operations = [ new MapOperationDefinition { Expression = "x => x" } ],
  Sink = new KafkaSinkDefinition { Topic = "output" }
};

var gateway = new FlinkJobGatewayService(new FlinkJobGatewayConfiguration
{
  BaseUrl = "http://localhost:8080"
});

var result = await gateway.SubmitJobAsync(job);
Console.WriteLine(result.Success ? $"FlinkJobId: {result.FlinkJobId}" : result.ErrorMessage);
```

Note: The Gateway now prebuilds and bundles the IR Runner jar at build time. Ensure Java and Maven are available on PATH. You can disable the prebuild with `/p:BuildFlinkRunner=false` and provide a prebuilt jar at `FlinkDotNet/FlinkDotNet.JobGateway/flink-ir-runner.jar` (or set `FLINK_RUNNER_JAR_PATH`).

