# FlinkDotNet

**FlinkDotNet** is a .NET framework that enables developers to write Apache Flink 2.1 streaming jobs in C# and submit them to production Flink clusters.

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

```csharp
var env = Flink.GetExecutionEnvironment();
var stream = env.FromKafka("orders")
    .Filter(order => order.Amount > 100)
    .Map(order => order.ToUpperCase())
    .SinkToKafka("processed-orders");

await env.ExecuteAsync("order-processor");
```

## Local Development with Aspire

**[.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/)** is a container orchestrator that simplifies running distributed applications locally. It manages the lifecycle of containers and provides a unified dashboard for monitoring.

FlinkDotNet uses Aspire to orchestrate a complete streaming stack:
- **Apache Flink** - Real-time stream processing engine
- **Apache Kafka** - Message streaming broker
- **Temporal** - Workflow orchestration platform
- **FlinkDotNet Gateway** - Job submission service

With one command, Aspire starts all containers and connects them together, making it easy to develop and test distributed streaming applications locally.

### Try It Out

```bash
# Prerequisites: .NET 9.0 SDK, Docker Desktop (or Podman)

# 1. Clone and run LocalTesting
git clone https://github.com/devstress/FlinkDotnet.git
cd FlinkDotnet/LocalTesting
dotnet run --project LocalTesting.FlinkSqlAppHost

# 2. Aspire Dashboard opens at http://localhost:15888
# 3. Run integration tests to validate everything works
dotnet test LocalTesting.IntegrationTests
```

**LocalTesting** includes 9 integration tests that validate the complete pipeline: Kafka → Flink → Processing → Output. See [LocalTesting/README.md](LocalTesting/README.md) for details.

## Learn FlinkDotNet

**[LearningCourse](LearningCourse/README.md)** provides a 15-day hands-on course covering:
- Day 01: Kafka-Flink Data Pipeline
- Day 02: Flink 2.1 Fundamentals
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
docker run -p 8080:8080 \
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
