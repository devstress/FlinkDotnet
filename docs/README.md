# FlinkDotNet

FlinkDotNet provides a .NET-first developer experience for Apache Flink by generating an intermediate representation (IR) of jobs in C# and executing them with a prebuilt IR Runner jar on a Flink cluster. It also includes a Flink Job Gateway (ASP.NET Core) and LocalTesting environment for quick iteration.

Key pieces:
- .NET SDK/DSL (imported via single NuGet `FlinkDotNet`) produces a JSON IR and validates it pre-submit.
- IR Runner Jar builds a Flink DataStream topology from the IR and runs it.
- Flink Job Gateway mediates submission, status, metrics, and cancel APIs.
- LocalTesting (Aspire) composes Kafka + Flink + Gateway for end-to-end tests.

## 5‑Minute Quick Start

Prereqs: .NET 9 SDK, Docker Desktop (for LocalTesting), Java 17 (for Flink locally if needed).

1) Generate IR in C#
```
// Install-Package FlinkDotNet
var job = new JobDefinition {
  Metadata = new JobMetadata { JobId = Guid.NewGuid().ToString("n"), Version = "1.0", Parallelism = 1 },
  Source = new KafkaSourceDefinition { Topic = "input" },
  Operations = [ new MapOperationDefinition { Expression = "x => x" } ],
  Sink = new KafkaSinkDefinition { Topic = "output" }
};
```

2) Submit to the Gateway
```
var gateway = new FlinkJobGatewayService();
var result = await gateway.SubmitJobAsync(job);
if (!result.Success) Console.WriteLine($"Failed: {result.ErrorMessage}");
```

3) Run the LocalTesting integration to verify environment
- See `LocalTesting/LocalTesting.IntegrationTests` and docs/quickstart.md.

4) Explore the IR Schema
- The frozen v1 schema is at `docs/ir-schema-v1.json`.

## Architecture Overview

- IR: Lightweight JSON contract describing sources, operations and sinks with a `type` discriminator per union.
- Runner: A shaded Flink jar consumes the IR, wires connectors and ops, emits consolidated metrics.
- Gateway: Uploads/ensures Runner jar, runs with IR argument, exposes REST endpoints for submit/status/metrics/cancel.
- SDK: Fluent DSL + pre-submit validation producing helpful messages, plus simple helpers for common pipelines.

## Next Steps

- See `docs/dsl-guide.md` for DSL/IR mapping and examples.
- See `docs/gateway-api.md` for REST endpoints.
- See `docs/observability.md` for metrics mapping.
