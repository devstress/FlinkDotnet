# FlinkDotNet - .NET SDK for Apache Flink

FlinkDotNet enables you to build and run Apache Flink streaming jobs using idiomatic C#/.NET code. Jobs are defined using a fluent DSL and executed on real Flink clusters through an Intermediate Representation (IR) system.

## Architecture Overview

FlinkDotNet follows a three-tier architecture:

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────────┐
│   .NET DSL      │───▶│   Job Gateway    │───▶│   Flink Cluster     │
│ (C# JobBuilder) │    │ (ASP.NET Core)   │    │ (Java/Scala)        │
│                 │    │                  │    │                     │
│ • FlinkJobBuilder    │ • IR Validation  │    │ • IR Runner JAR     │
│ • Fluent API    │    │ • JAR Upload     │    │ • DataStream API    │
│ • Type Safety   │    │ • Job Lifecycle  │    │ • Kafka Connectors  │
└─────────────────┘    └──────────────────┘    └─────────────────────┘
```

### Key Components

1. **FlinkDotNet SDK** - C# fluent DSL for defining streaming jobs
2. **IR (Intermediate Representation)** - JSON-based job definition with v1.0 schema 
3. **Job Gateway** - ASP.NET Core API for job submission and management
4. **IR Runner JAR** - Java component that converts IR to Flink DataStream jobs

## Quick Start (5 minutes)

### Prerequisites

- .NET 9.0 SDK
- Docker Desktop (for local testing)
- Java 17+ (for IR Runner JAR)

### 1. Install FlinkDotNet

```bash
dotnet add package FlinkDotNet
```

### 2. Create Your First Streaming Job

```csharp
using FlinkDotNet;

// Create a simple Kafka-to-Kafka pipeline
var job = Flink.JobBuilder
    .FromKafka("input-events", "localhost:9092")
    .Filter("event.type == 'important'")
    .Map("event.toUpperCase()")
    .WithTimer(10000) // 10 second processing time window
    .ToKafka("output-events", "localhost:9092");

// Submit to Flink cluster
var result = await job.Submit("my-streaming-job");
Console.WriteLine($"Job submitted: {result.FlinkJobId}");
```

### 3. Run Local Testing Environment

```bash
# Clone the repository
git clone https://github.com/devstress/FlinkDotnet.git
cd FlinkDotnet

# Start LocalTesting environment (Kafka + Flink + Gateway)
cd LocalTesting
dotnet run --project BackPressure.AppHost

# Run integration tests
dotnet test LocalTesting.IntegrationTests --filter Category=observability
```

### 4. Monitor Your Jobs

```csharp
// Get job status
var status = await gateway.GetJobStatusAsync(flinkJobId);
Console.WriteLine($"Job state: {status.State}");

// Get job metrics  
var metrics = await gateway.GetJobMetricsAsync(flinkJobId);
Console.WriteLine($"Records processed: {metrics.RecordsIn} → {metrics.RecordsOut}");

// Cancel job if needed
await gateway.CancelJobAsync(flinkJobId);
```

## Core Features

### Rich DSL Operations

```csharp
var job = Flink.JobBuilder
    .FromKafka("events")
    
    // Transformations
    .Filter("payload.isValid")
    .Map("payload.normalize()")
    
    // Async operations
    .AsyncHttp("https://api.service.com/enrich", timeoutMs: 5000)
    .AsyncDatabase(connectionString, "SELECT * FROM lookup WHERE id = ?")
    
    // State management
    .WithState("user-session", "map", ttlMs: 3600000)
    
    // Windowing and timers
    .GroupBy("userId")
    .Window("TUMBLING", 60, "SECONDS")
    .Aggregate("COUNT", "events")
    
    // Error handling
    .WithRetry(maxRetries: 3, deadLetterTopic: "failed-events")
    .WithSideOutput("errors", errorCondition: "payload.invalid")
    
    // Output
    .ToKafka("processed-events");
```

### Multiple Source Types

- **Kafka**: High-throughput event streaming
- **HTTP**: REST API polling with configurable intervals  
- **Database**: SQL query polling with change detection
- **File**: Batch file processing

### Multiple Sink Types

- **Kafka**: Event publishing with exactly-once semantics
- **Console**: Debug output for development
- **File**: Batch file output (JSON, CSV, Parquet)
- **Database**: SQL insert/update operations
- **HTTP**: REST API publishing
- **Redis**: Atomic operations and caching

### Production-Ready Features

- **Fault Tolerance**: Automatic checkpointing and recovery
- **Scalability**: Horizontal scaling with Flink parallelism
- **Monitoring**: Built-in metrics and health checks
- **Security**: TLS/SSL support and authentication
- **Deployment**: Docker, Kubernetes, and cloud-ready

## Project Structure

```
FlinkDotNet/
├── FlinkDotNet/              # Main SDK package
├── Flink.JobBuilder/         # Core DSL and IR generation
├── Flink.JobGateway/         # ASP.NET Core job management API
├── Flink.IRRunner/           # Java IR-to-Flink converter
├── LocalTesting/             # Development environment
│   ├── BackPressure.AppHost/ # Aspire orchestration
│   └── *.IntegrationTests/   # End-to-end tests
├── docs/                     # Documentation
└── scripts/                  # Build and validation tools
```

## Documentation

- **[Quick Start Guide](quickstart.md)** - Get running in 5 minutes
- **[DSL Reference](dsl-guide.md)** - Complete API documentation
- **[Gateway API](gateway-api.md)** - REST endpoint reference  
- **[IR Runner](runner.md)** - Java component internals
- **[Deployment](deployment.md)** - Production deployment guides
- **[Troubleshooting](troubleshooting.md)** - Common issues and solutions

## Status

FlinkDotNet is actively developed and production-ready:

- ✅ **Core Architecture**: Stable IR v1.0 schema and runtime
- ✅ **SDK**: Complete fluent DSL with validation
- ✅ **Gateway**: Full job lifecycle management API
- ✅ **IR Runner**: Tested Java component with Flink 1.18.1
- ✅ **Testing**: Comprehensive integration test suite  
- ✅ **CI/CD**: Automated builds and artifact publishing

## Contributing

See [CONTRIBUTING.md](../CONTRIBUTING.md) for development setup and guidelines.

## License

Licensed under the Apache License 2.0. See [LICENSE](../LICENSE) for details.