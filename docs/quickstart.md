# Quick Start (LocalTesting)

This guide shows how to run the end-to-end LocalTesting setup and submit a simple job.

## Prerequisites
- .NET 9 SDK
- Docker Desktop (Linux containers)

## Run LocalTesting Integration

1) Build the solution
```
dotnet build FlinkDotNet/FlinkDotNet.sln -c Debug
```

2) Run the integration tests (NUnit)
```
dotnet test LocalTesting/LocalTesting.IntegrationTests/LocalTesting.IntegrationTests.csproj -c Debug --filter TestCategory=observability
```

The test ensures:
- Kafka is reachable and topics are created
- Gateway health check returns OK
- IR generation and submission attempt works

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

Note: Once the IR Runner jar is integrated in the Gateway, the job will be submitted to Flink and a real `FlinkJobId` returned. Until then, submissions validate IR and exercise the gateway.

