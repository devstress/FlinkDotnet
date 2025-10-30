# FlinkDotNet

**FlinkDotNet** is a .NET framework that enables developers to write Apache Flink 2.1 streaming jobs in C# and submit them to production Flink clusters.
This repo also provides a comprehensive distributed message-oriented architecture that enables developers to build production-grade stream processing applications using Apache Flink 2.1, Apache Kafka, Temporal workflows, and Microsoft Aspire orchestration - all accessible through a native .NET SDK.

<!-- Build & Test Status -->
[![Build](https://github.com/devstress/FlinkDotnet/actions/workflows/unit-tests.yml/badge.svg)](https://github.com/devstress/FlinkDotnet/actions/workflows/unit-tests.yml)
[![LocalTesting Integration Tests](https://github.com/devstress/FlinkDotnet/actions/workflows/localtesting-integration-tests.yml/badge.svg)](https://github.com/devstress/FlinkDotnet/actions/workflows/localtesting-integration-tests.yml)
[![Day01 Integration Tests](https://github.com/devstress/FlinkDotnet/actions/workflows/day01-integration-tests.yml/badge.svg)](https://github.com/devstress/FlinkDotnet/actions/workflows/day01-integration-tests.yml)

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
[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-blue)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![Apache Flink 2.1](https://img.shields.io/badge/Flink-2.1-orange)](https://flink.apache.org/)
[![Apache Kafka](https://img.shields.io/badge/Kafka-3.x-black)](https://kafka.apache.org/)
[![Temporal](https://img.shields.io/badge/Temporal-latest-purple)](https://temporal.io/)
[![Microsoft Aspire](https://img.shields.io/badge/Aspire-latest-512BD4)](https://learn.microsoft.com/en-us/dotnet/aspire/)

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
var stream = env.FromKafka("orders")
    .Filter(order => order.Amount > 100)
    .Map(order => order.ToUpperCase())
    .SinkToKafka("processed-orders");

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
# Prerequisites: .NET 9.0 SDK, Docker Desktop (or Podman)

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

| Flink Version | Release Date | Coverage | Key Features Implemented | Integration Tests |
|---------------|--------------|----------|-------------------------|-------------------|
| **1.0-1.9** | 2016-2019 | ✅ **100%** | DataStream API, Windows, State Management, CEP, Kafka Integration | [LocalTesting.IntegrationTests](LocalTesting/LocalTesting.IntegrationTests/) |
| **1.10** | Feb 2020 | ✅ **100%** | Table API, SQL Gateway, **Catalog API (WI14)** | [CatalogTests.cs](LocalTesting/LocalTesting.IntegrationTests/) (54 tests) |
| **1.11** | Jul 2020 | ✅ **100%** | DDL Support, Change Data Capture | [GatewayAllPatternsTests.cs](LocalTesting/LocalTesting.IntegrationTests/) |
| **1.12** | Dec 2020 | ✅ **100%** | **Unified Source API/FLIP-27 (WI15)**, SQL Connectors | [UnifiedSource.cs](FlinkDotNet/FlinkDotNet.DataStream/) |
| **1.13** | May 2021 | ✅ **100%** | SQL Functions, Window TVF | [GatewayAllPatternsTests.cs](LocalTesting/LocalTesting.IntegrationTests/) |
| **1.14** | Nov 2021 | ✅ **100%** | SQL Client, Batch SQL | [GatewayAllPatternsTests.cs](LocalTesting/LocalTesting.IntegrationTests/) |
| **1.15-1.18** | 2022-2023 | ✅ **100%** | **Table Store/Apache Paimon (WI13)**, Advanced Table Features | [PaimonIntegrationTests.cs](LocalTesting/LocalTesting.IntegrationTests/) |
| **1.19** | Mar 2024 | ✅ **100%** | Performance Improvements, Checkpoint Optimizations | [GatewayAllPatternsTests.cs](LocalTesting/LocalTesting.IntegrationTests/) |
| **1.20** | Oct 2024 | ✅ **100%** | **Unified Sink v2 (WI6), Materialized Tables (WI7)** | [UnifiedSinkV2ConsolidatedTests.cs](LocalTesting/LocalTesting.IntegrationTests/) (5 tests), [MaterializedTableTests.cs](LocalTesting/LocalTesting.IntegrationTests/) (5 tests) |
| **2.1** | Jul 2025 | ✅ **100%** | **AI/ML Integration (WI8-WI9), VARIANT Type (WI10), PTFs (WI10), Performance & Format (WI12, WI16)** | [ModelTests.cs](LocalTesting/LocalTesting.IntegrationTests/), [PerformanceFormatTests.cs](LocalTesting/LocalTesting.IntegrationTests/), [PerformanceConfiguration.cs](FlinkDotNet/FlinkDotNet.DataStream/) |

### Feature Categories (21/21 Complete - 100% 🎉)

**P0 Features (Critical - ALL COMPLETE)**:
- ✅ **Unified Sink API v2** (WI6, Flink 1.20) - Modern sink pattern replacing SinkFunction
- ✅ **Materialized Tables** (WI7, Flink 1.20) - Declarative ETL with auto-refresh
- ✅ **AI/ML Integration** (WI8-WI9, Flink 2.1) - CREATE MODEL, ML_PREDICT, AI providers (OpenAI, Azure OpenAI, Amazon Bedrock, Google Vertex AI, Hugging Face)

**P1 Features (High Priority - ALL COMPLETE)**:
- ✅ **VARIANT Data Type** (WI10, Flink 2.1) - Semi-structured JSON data handling
- ✅ **Table API & Advanced SQL** (WI10, Flink 2.1) - All 7 sub-features complete
- ✅ **Process Table Functions (PTFs)** (WI10, Flink 2.1) - Advanced table processing
- ✅ **Apache Paimon** (WI13, Flink 1.15-1.18) - Lakehouse storage integration
- ✅ **Observability Testing** (WI11) - Comprehensive test coverage and monitoring
- ✅ **Catalog API** (WI14, Flink 1.10) - Hive/JDBC/GenericInMemory catalog management
- ✅ **Unified Source API** (WI15, Flink 1.12) - FLIP-27 modern source connector framework

**P2 Features (Medium Priority - ALL COMPLETE)**:
- ✅ **Performance & Format** (WI12, WI16, Flink 2.1) - All 4 sub-features complete:
  - ✅ Custom Async Sink Batching (WI12)
  - ✅ State Backend Configuration (WI16)
  - ✅ Smile Format for Compiled Plans (WI16)
  - ✅ MultiJoin Optimization (WI16)

### Test Coverage

**Total**: 420+ integration tests across all features
- **Core Features**: 310+ tests (WI6-WI13)
- **Catalog API**: 54 tests (WI14)
- **Unified Source API**: 21 tests (WI15, pending NUnit conversion)
- **Performance & Format**: 35 tests (WI16, pending NUnit conversion)

All integration tests are located in: [LocalTesting/LocalTesting.IntegrationTests/](LocalTesting/LocalTesting.IntegrationTests/)

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

- **.NET 9.0 SDK** - For development
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
