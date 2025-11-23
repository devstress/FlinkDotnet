# FlinkDotNet

**FlinkDotNet** is a .NET framework that enables developers to write Apache Flink 2.1 streaming jobs in C# and submit them to production Flink clusters.
This repo also provides a comprehensive distributed messaging architecture that enables developers to build stream processing using Apache Flink 2.1, Apache Kafka, Temporal workflows, and Microsoft Aspire orchestration - all accessible through a native .NET SDK.

<!-- Build & Test Status -->
[![Build](https://github.com/devstress/FlinkDotnet/actions/workflows/unit-tests.yml/badge.svg)](https://github.com/devstress/FlinkDotnet/actions/workflows/unit-tests.yml)
[![LocalTesting Integration Tests](https://github.com/devstress/FlinkDotnet/actions/workflows/localtesting-integration-tests.yml/badge.svg)](https://github.com/devstress/FlinkDotnet/actions/workflows/localtesting-integration-tests.yml)
[![Day01 Integration Tests](https://github.com/devstress/FlinkDotnet/actions/workflows/day01-integration-tests.yml/badge.svg)](https://github.com/devstress/FlinkDotnet/actions/workflows/day01-integration-tests.yml)
[![Observability Integration Tests](https://github.com/devstress/FlinkDotnet/actions/workflows/observability-integration-tests.yml/badge.svg)](https://github.com/devstress/FlinkDotnet/actions/workflows/observability-integration-tests.yml)

<!-- Code Quality - SonarQube -->
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=devstress_flinkdotnet&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=devstress_flinkdotnet)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=devstress_flinkdotnet&metric=coverage)](https://sonarcloud.io/summary/new_code?id=devstress_flinkdotnet)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=devstress_flinkdotnet&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=devstress_flinkdotnet)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=devstress_flinkdotnet&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=devstress_flinkdotnet)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=devstress_flinkdotnet&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=devstress_flinkdotnet)
[![Duplicated Lines (%)](https://sonarcloud.io/api/project_badges/measure?project=devstress_flinkdotnet&metric=duplicated_lines_density)](https://sonarcloud.io/summary/new_code?id=devstress_flinkdotnet)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=devstress_flinkdotnet&metric=vulnerabilities)](https://sonarcloud.io/summary/new_code?id=devstress_flinkdotnet)
[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=devstress_flinkdotnet&metric=bugs)](https://sonarcloud.io/summary/new_code?id=devstress_flinkdotnet)
[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=devstress_flinkdotnet&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=devstress_flinkdotnet)
[![Technical Debt](https://sonarcloud.io/api/project_badges/measure?project=devstress_flinkdotnet&metric=sqale_index)](https://sonarcloud.io/summary/new_code?id=devstress_flinkdotnet)
[![Lines of Code](https://sonarcloud.io/api/project_badges/measure?project=devstress_flinkdotnet&metric=ncloc)](https://sonarcloud.io/summary/new_code?id=devstress_flinkdotnet)

<!-- Technology Stack -->
[![.NET 10.0](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Apache Flink 2.1](https://img.shields.io/badge/Flink-2.1-orange)](https://flink.apache.org/)
[![Apache Kafka](https://img.shields.io/badge/Kafka-3.x-black)](https://kafka.apache.org/)
[![Temporal](https://img.shields.io/badge/Temporal-latest-purple)](https://temporal.io/)
[![Microsoft Aspire](https://img.shields.io/badge/Aspire-latest-512BD4)](https://learn.microsoft.com/en-us/dotnet/aspire/)

## What is Kafka and Flink? Why Do We Need Them?

### The Problems You'll Face as Your Application Grows

| **Stage** | **What Works** | **Problems You'll Hit** | **Why Simple Solutions Break** |
|-----------|----------------|-------------------------|--------------------------------|
| **Starting Out** | Single server + database | Everything runs on one machine | Works great for small apps! |
| **Growing Fast** | Need to handle more users | Server crashes under heavy load | One machine can't handle thousands of users at once |
| | | Data gets lost when server restarts | No backup - if server dies, everything is gone |
| | | Slow response times | Processing requests one by one is too slow |
| **Going Big** | Need multiple servers | How do servers talk to each other? | Direct connections become a tangled mess |
| | | Messages get lost between servers | Network failures mean data disappears |
| | | Can't track long-running processes | If a process takes hours, how do you monitor it? |
| | | Need to process data in real-time | Batch processing is too slow for live data |
| **Enterprise Scale** | Millions of users globally | Coordinating across data centers | Hundreds of servers need to work together seamlessly |
| | | Handling 1 million+ connections per second | Need smart routing and load balancing |
| | | Data must survive server failures | Redundancy and durability become critical |
| | | Complex retry logic needed | Failures happen - need automatic recovery |

### The Solutions: Kafka, Flink, and Temporal

**FlinkDotNet brings billion-scale architecture to .NET developers** - combining these three technologies to handle routing billions of messages per second across distributed systems, processing them in real-time with global context awareness, and coordinating millions of complex workflows simultaneously.

## What is FlinkDotNet?
FlinkDotNet lets you write **Apache Flink 2.1** streaming jobs in C# and submit them to Flink clusters. No Java required.
A complete distributed message-oriented architecture for building enterprise stream processing applications in .NET, it combines:

- **Apache Flink 2.1** - distributed stream processing engine with state management.
- **Apache Kafka** - Distributed message streaming/queue
- **Temporal** - Durable workflow orchestration/Durable execution solution.
- **Microsoft Aspire** - Local development containerised orchestration.
- **FlinkDotNet SDK** - Native .NET API for writing Flink jobs in C#

```csharp
var env = Flink.GetExecutionEnvironment();
var stream = env.FromKafka(
        topic: "orders",
        bootstrapServers: "localhost:9092",
        groupId: "order-consumer-group")
    .Filter(order => order.Amount > 100)
    .Map(order => order.ToUpperCase())
    .SinkToKafka("processed-orders", "localhost:9092");

await env.ExecuteAsync("order-processor");
```

## Distributed Architecture with Aspire Orchestration

**[.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/)** orchestrates the complete distributed message-oriented architecture locally, providing production-parity development environments.

### Full Architecture Stack

**Stream Processing Layer**:
- **Apache Flink 2.1** - JobManager, TaskManager, and SQL Gateway for real-time stream processing
- **FlinkDotNet SDK** - Native .NET DataStream API with fluent C# DSL
- **Job Gateway** - ASP.NET Core service for job submission and management

**Messaging & Orchestration Layer**:
- **Apache Kafka** - KRaft-mode message broker with JMX metrics export
- **Temporal** - Durable workflow orchestration with PostgreSQL backend

**Observability Stack** (LearningCourse mode):
- **Prometheus** - Metrics collection from Flink, Kafka, and custom applications
- **Grafana** - Visualization dashboards for performance monitoring
- **JMX Exporters** - Metrics bridge for Java components

With one command, Aspire starts all containers, configures service discovery, and provides a unified dashboard - enabling you to develop and test complete distributed streaming applications locally.

### Component Roles in High-Throughput Processing

When processing millions of messages per second, each component plays a critical role:

**Apache Kafka** - Message ingestion and buffering
- Handles message ingestion at scale (millions of messages/second)
- Provides durable message storage with partitioning for parallel consumption
- Acts as buffer between producers and stream processors
- Enables replay and reprocessing of historical data

**Apache Flink 2.1** - Distributed stream processing engine
- Processes messages in parallel across multiple TaskManager instances
- Provides stateful computations with exactly-once processing guarantees
- Scales horizontally by adding more TaskManager slots
- Handles backpressure to prevent system overload

**FlinkDotNet SDK** - .NET development interface
- Enables writing stream processing logic in C# with type safety
- Compiles to Flink's native execution model
- Provides fluent API for common streaming patterns (map, filter, window, join)
- Eliminates need for Java expertise while maintaining full Flink performance

**Temporal** - Durable workflow orchestration
- Manages long-running workflows across distributed job submissions
- Provides guaranteed execution with automatic retries and compensation
- Maintains workflow state even during infrastructure failures
- Coordinates complex multi-step processing pipelines

**Microsoft Aspire** - Local development orchestration
- Simulates production environment locally with container orchestration
- Manages service discovery between Kafka, Flink, Temporal, and custom services
- Provides unified dashboard for monitoring all components
- Enables rapid iteration and testing before production deployment

Together, these components form a production-grade streaming architecture capable of processing high-volume event streams with reliability and fault tolerance.

### Try It Out

```bash
# Prerequisites: .NET 10.0 SDK, Docker Desktop (or Podman)

# 1. Clone and run LocalTesting
git clone https://github.com/devstress/FlinkDotnet.git
cd FlinkDotnet/LocalTesting
dotnet run --project LocalTesting.FlinkSqlAppHost

# 2. Aspire Dashboard opens at http://localhost:15888
# 3. Navigate to LearningCourse folder and follow the instructions there
```
```bash
# Or run integration tests to validate everything works
dotnet test LocalTesting.IntegrationTests
```

**LocalTesting** includes integration tests that validate the complete pipeline: Kafka → Flink → Processing → Output.

## Apache Flink Versions Coverage

FlinkDotNet provides **100% feature parity** with Apache Flink 1.0-2.1, implementing all major features across every version release.

### Version-by-Version Coverage

| Flink Version | Release Date | Coverage | Key Features Implemented |
|---------------|--------------|----------|-------------------------|
| **1.0-1.9** | 2016-2019 | ✅ **100%** | DataStream API, Windows, State Management, CEP, Kafka Integration |
| **1.10** | Feb 2020 | ✅ **100%** | Table API, SQL Gateway, Catalog API |
| **1.11** | Jul 2020 | ✅ **100%** | DDL Support, Change Data Capture |
| **1.12** | Dec 2020 | ✅ **100%** | Unified Source API/FLIP-27, SQL Connectors |
| **1.13** | May 2021 | ✅ **100%** | SQL Functions, Window TVF |
| **1.14** | Nov 2021 | ✅ **100%** | SQL Client, Batch SQL |
| **1.15-1.18** | 2022-2023 | ✅ **100%** | Table Store/Apache Paimon, Advanced Table Features |
| **1.19** | Mar 2024 | ✅ **100%** | Performance Improvements, Checkpoint Optimizations |
| **1.20** | Oct 2024 | ✅ **100%** | Unified Sink v2 (WI6), Materialized Tables |
| **2.0** | Mar 2025 | ✅ **100%** | Disaggregated State Management, Unified Batch/Stream, API Overhaul |
| **2.1** | Jul 2025 | ✅ **100%** | AI/ML Integration, VARIANT Type, PTFs, Performance & Format |

## Learn FlinkDotNet

**[LearningCourse](LearningCourse/README.md)** provides a 15-day hands-on course covering:
- Day 01: Kafka-Flink Data Pipeline
- Day 02: Flink 2.1 Fundamentals - **Complete Apache Flink 1.0-2.1 Version Coverage**
- Day 03-15: Advanced topics (AI integration, backpressure, observability, workflows, stress testing, and more)

Each day includes working examples and integration tests you can run locally.

## Documentation

- **[Getting Started Guide](docs/getting-started.md)** - Setup and first job
- **[API Reference](docs/api-reference.md)** - Complete DataStream API
- **[Features](docs/features.md)** - Apache Flink 2.1 capabilities
- **[Architecture & Use Cases](docs/architecture-and-usecases.md)** - System design

## Installation

### NuGet Package
```bash
dotnet add package FlinkDotNet
```

### Docker Image
```bash
docker pull devstress/flinkdotnet:latest
docker run -p 8086:8086 \
  -e FLINK_CLUSTER_HOST=your-flink-host \
  -e FLINK_CLUSTER_PORT=8081 \
  devstress/flinkdotnet:latest
```

### Standalone Executables
Download from [GitHub Releases](https://github.com/devstress/FlinkDotnet/releases) - includes Windows and Linux packages.

## Requirements

- **.NET 10.0 SDK** - For development
- **Docker Desktop or Podman** - For local testing with Aspire
- **Apache Flink 2.1 cluster** - For production deployments

## Community & Support

- 💬 **[GitHub Issues](https://github.com/devstress/FlinkDotnet/issues)** - Bug reports and feature requests
- 📧 **[Discussions](https://github.com/devstress/FlinkDotnet/discussions)** - Questions and best practices
- 🤝 **[Contributing](CONTRIBUTING.md)** - Development guidelines

## License

MIT License - see [LICENSE](LICENSE) for details.

---

**Get Started:** Try the [LocalTesting](LocalTesting/README.md) environment or explore the [15-Day Learning Course](LearningCourse/README.md).
