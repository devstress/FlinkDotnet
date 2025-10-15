# FlinkDotNet

FlinkDotNet provides a .NET-first developer experience for Apache Flink by generating an intermediate representation (IR) of jobs in C# and executing them with an IR Runner jar on a Flink cluster. The Flink Job Gateway (ASP.NET Core) prebuilds and bundles this jar during project build, and a LocalTesting environment accelerates iteration.

Key pieces:
- .NET SDK/DSL (imported via single NuGet `FlinkDotNet`) produces a JSON IR and validates it pre-submit.
- IR Runner Jar builds a Flink DataStream topology from the IR and runs it.
- Flink Job Gateway mediates submission, status, metrics, and cancel APIs.
- LocalTesting (Aspire) composes Kafka + Flink + Gateway for end-to-end tests.

## 5‑Minute Quick Start

Prereqs: .NET 9 SDK, Docker Desktop or Podman (for LocalTesting), Java 17 and Maven (auto-installed if not found).

1) Generate and validate IR in C#
```csharp
// Install-Package FlinkDotNet
var job = new JobDefinition {
  Metadata = new JobMetadata { JobId = Guid.NewGuid().ToString("n"), Version = "1.0", Parallelism = 1 },
  Source = new KafkaSourceDefinition { Topic = "input" },
  Operations = [ new MapOperationDefinition { Expression = "x => x" } ],
  Sink = new KafkaSinkDefinition { Topic = "output" }
};

// Validate job definition with enhanced validator
var validationResult = JobDefinitionValidator.Validate(job);
if (!validationResult.IsValid)
{
    Console.WriteLine($"Validation errors: {string.Join(", ", validationResult.Errors)}");
    return;
}
```

2) Submit to the Gateway with improved error handling
```csharp
var gateway = new FlinkJobGatewayService();
var result = await gateway.SubmitJobAsync(job);
if (!result.Success) 
{
    Console.WriteLine($"Submission failed: {result.ErrorMessage}");
    // Enhanced error messages provide specific guidance
}
```

3) Monitor job with enhanced metrics collection
```csharp
// JobMetricsBuilder pattern provides structured metrics
var metrics = await gateway.GetJobMetricsAsync(result.JobId);
if (metrics != null)
{
    Console.WriteLine($"Records In: {metrics.RecordsIn}, Records Out: {metrics.RecordsOut}");
    Console.WriteLine($"Parallelism: {metrics.Parallelism}/{metrics.MaxParallelism}");
}
```

3) Run the LocalTesting integration to verify environment
- See `LocalTesting/LocalTesting.IntegrationTests` and docs/quickstart.md.

4) Explore the IR Schema
- The frozen v1 schema is at `docs/ir-schema-v1.json`.

## Architecture Overview

- **Enhanced IR**: Lightweight JSON contract with comprehensive validation
  - *JobDefinitionValidator*: Modular validation with focused, maintainable methods
  - *Improved error messages*: Specific validation guidance for developers
  - *Code quality optimized*: All validation logic under complexity thresholds
- **Optimized Runner**: A shaded Flink jar with improved error handling and metrics
- **Enhanced Gateway**: Robust job management with structured error handling
  - *FlinkJobManager*: Restructured with builder patterns for metrics collection
  - *Fault tolerance*: Comprehensive error handling and recovery patterns
  - *Maintainable architecture*: Complex operations split into focused methods
- **Refined SDK**: Fluent DSL with enhanced validation and helpful error messages

## Next Steps

- See `docs/dsl-guide.md` for DSL/IR mapping and examples.
- See `docs/gateway-api.md` for REST endpoints.
- See `docs/observability.md` for metrics mapping.
