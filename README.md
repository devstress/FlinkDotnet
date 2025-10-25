# FlinkDotNet

**FlinkDotNet** is a comprehensive .NET framework that enables developers to build and submit streaming jobs to Apache Flink 2.1 clusters using a fluent C# API. It provides extensive compatibility with Apache Flink 2.1 and integrates with three core technologies - **Apache Flink** (real-time stream processing), **Kafka** (message streaming broker), and **Temporal.io** (workflow orchestration platform) - making it easier for .NET developers to handle large-scale data processing challenges in multi-tiered, distributed real-time stream processing.

<!-- Build & Test Status -->
[![Build](https://github.com/devstress/FlinkDotnet/actions/workflows/unit-tests.yml/badge.svg)](https://github.com/devstress/FlinkDotnet/actions/workflows/unit-tests.yml)
[![Integration Tests](https://github.com/devstress/FlinkDotnet/actions/workflows/localtesting-integration-tests.yml/badge.svg)](https://github.com/devstress/FlinkDotnet/actions/workflows/localtesting-integration-tests.yml)

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
[![Docker](https://img.shields.io/badge/Docker-latest-2496ED)](https://www.docker.com/)
[![Kubernetes](https://img.shields.io/badge/Kubernetes-1.28+-326CE5)](https://kubernetes.io/)
[![Java 17](https://img.shields.io/badge/Java-17-red)](https://openjdk.org/)

<!-- Project Stats -->
[![GitHub Stars](https://img.shields.io/github/stars/devstress/FlinkDotnet?style=social)](https://github.com/devstress/FlinkDotnet/stargazers)
[![GitHub Forks](https://img.shields.io/github/forks/devstress/FlinkDotnet?style=social)](https://github.com/devstress/FlinkDotnet/network/members)
[![GitHub Issues](https://img.shields.io/github/issues/devstress/FlinkDotnet)](https://github.com/devstress/FlinkDotnet/issues)
[![GitHub Pull Requests](https://img.shields.io/github/issues-pr/devstress/FlinkDotnet)](https://github.com/devstress/FlinkDotnet/pulls)

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

## What FlinkDotNet Does

FlinkDotNet lets you write **Apache Flink 2.1** streaming jobs in C# and submit them to production Flink clusters. No Java required for job development - just write fluent C# code.

```csharp
// Write this in C#...
var env = Flink.GetExecutionEnvironment();
var stream = env.FromKafka("orders")
    .Filter(order => order.Amount > 100)
    .Map(order => order.ToUpperCase())
    .SinkToKafka("processed-orders");

await env.ExecuteAsync("order-processor");

// ...and it runs on Apache Flink clusters processing millions of events/sec
```

**What makes it different?**
- ✅ **Native .NET API** - Full Apache Flink 2.1 DataStream API in C#
- ✅ **Production-Ready** - 10 passing integration tests, validated end-to-end pipeline
- ✅ **Enterprise Scale** - Supports Kafka, event-time windowing, exactly-once semantics
- ✅ **Zero Java Code** - Write everything in C#, runs on Flink clusters via IR translation
- ✅ **Local Development** - .NET Aspire integration for one-command cluster startup

## Quick Start

**Prerequisites:** .NET 9.0 SDK, Docker Desktop (or Podman)

```bash
# 1. Clone and build
git clone https://github.com/devstress/FlinkDotnet.git
cd FlinkDotnet
dotnet build FlinkDotNet/FlinkDotNet.sln --configuration Release

# 2. Run integration tests (validates complete pipeline)
cd LocalTesting
dotnet test LocalTesting.IntegrationTests --configuration Release

# Expected: ✅ 10 tests pass - Kafka → Flink → Processing → Output validated
```

**Your first Flink job:**

```csharp
using FlinkDotNet.DataStream;

var env = Flink.GetExecutionEnvironment();

// Read from Kafka
var orders = env.FromKafka("orders", "kafka:9092", "my-group");

// Transform with Flink operators
var processed = orders
    .Filter(o => o.Amount > 1000)
    .Map(o => o.ToUpperInvariant())
    .KeyBy(o => o.CustomerId);

// Write back to Kafka
processed.SinkToKafka("high-value-orders", "kafka:9092");

await env.ExecuteAsync("fraud-detection");
```

## Installation Options

### 1. Install FlinkDotNet Client from NuGet

Add the FlinkDotNet package to your .NET project:

```bash
dotnet add package FlinkDotNet
```

Use the fluent API to build and submit Flink jobs from your .NET application.

### 2. Use FlinkJobGateway Docker Image

Run FlinkJobGateway as a container:

```bash
docker pull flinkdotnet/jobgateway:latest
docker run -p 8080:8080 \
  -e FLINK_CLUSTER_HOST=your-flink-host \
  -e FLINK_CLUSTER_PORT=8081 \
  flinkdotnet/jobgateway:latest
```

Access the API at `http://localhost:8080`.

### 3. Validate Release Packages

For complete setup and validation instructions, see [ReleasePackagesTesting](ReleasePackagesTesting/README.md) - includes post-release validation examples and integration tests.

### 4. Other FlinkJobGateway Installation Options

Download standalone executables from [GitHub Releases](https://github.com/devstress/FlinkDotnet/releases):

- **Windows**: `jobgateway-win-x64-VERSION.zip` - Extract, edit `start-gateway.bat`, run
- **Linux**: `jobgateway-linux-x64-VERSION.tar.gz` - Extract, edit `start-gateway.sh`, run

See the included `README.md` in each package for detailed setup instructions.

### 5. Contributing & Development

For local development and contributions:

- **LocalTesting**: Complete local dev environment with .NET Aspire orchestration
- **LearningCourse**: 15-day hands-on exercises and integration tests

See [LocalTesting](LocalTesting/README.md) and [LearningCourse](LearningCourse/README.md) for details.

## Why FlinkDotNet?

### The Problem You're Solving

As your .NET application scales, you need:
- **Real-time processing** of millions of events/second
- **Exactly-once guarantees** across distributed systems
- **Event-time windowing** for out-of-order data
- **Multi-cluster orchestration** for enterprise deployments

Traditional solutions require Java expertise or vendor lock-in. FlinkDotNet brings Apache Flink's proven stream processing to .NET developers.

### Architecture in 30 Seconds

```
┌─────────────────┐     C# Job      ┌──────────────────┐     JSON IR    ┌─────────────────┐
│   Your .NET     │────────────────>│  FlinkDotNet     │───────────────>│  Apache Flink   │
│   Application   │                 │  SDK             │                │  Cluster (2.1)  │
│  (C# Fluent API)│<────────────────│  (Translates)    │<───────────────│  (Executes)     │
└─────────────────┘     Results     └──────────────────┘    Job Status  └─────────────────┘
```

**How it works:**
1. Write stream processing jobs using C# fluent API (just like Java Flink)
2. FlinkDotNet translates to portable JSON IR (Intermediate Representation)
3. Submit to Flink cluster via Gateway - prebuilt Java runner interprets IR
4. Jobs run at full Flink performance on production clusters

## Documentation

📚 **Complete guides and references for all aspects of FlinkDotNet:**

### Getting Started
- **[Getting Started Guide](docs/getting-started.md)** - Complete setup, first job, and local development
- **[Quickstart](docs/quickstart.md)** - 5-minute minimal example
- **[Installation Options](docs/getting-started.md#installation-options)** - NuGet, Docker, and source installation

### Core Documentation
- **[Features](docs/features.md)** - Complete feature list with Apache Flink 2.1 capabilities
- **[API Reference](docs/api-reference.md)** - Complete DataStream API documentation
- **[Architecture & Use Cases](docs/architecture-and-usecases.md)** - System design and real-world examples

### Advanced Topics
- **[Performance Benchmarks](docs/performance-benchmarks.md)** - Throughput metrics and optimization
- **[Observability & Monitoring](docs/observability.md)** - Metrics, logging, and tracing
- **[Deployment](docs/deployment.md)** - Production deployment strategies
- **[Troubleshooting](docs/troubleshooting.md)** - Common issues and solutions

### Technology Decisions
- **[Flink vs Temporal Decision Guide](docs/flink-vs-temporal-decision-guide.md)** - When to use each technology
- **[Apache Flink 2.1 Features](docs/flink-21-features.md)** - Complete API mapping

### Learning Resources
- **[15-Day Learning Course](LearningCourse/README.md)** - Comprehensive hands-on training
- **[Local Testing Setup](docs/local-testing-setup.md)** - Development environment details
- **[Contributing Guide](CONTRIBUTING.md)** - Development guidelines

## Proven at Scale

✅ **10 Integration Tests Passing** - Complete pipeline validated on every commit

🔗 [**View Live Test Results**](https://github.com/devstress/FlinkDotnet/actions/workflows/localtesting-integration-tests.yml)

**What's validated:**
- ✅ Kafka → Flink → Output pipeline (800K+ msg/sec)
- ✅ Basic transformations (map, filter, flatMap)
- ✅ Stateful processing (timers, event-time windows)
- ✅ Flink SQL via TableEnvironment
- ✅ Multi-step pipelines
- ✅ Temporal workflow integration

## Requirements

- **.NET 9.0 SDK** - Required for all development
- **Docker Desktop** or **Podman** - For local testing with Aspire
- **Apache Flink 2.1 cluster** - Production deployments
- **Apache Kafka** - For stream sources/sinks (optional)

## Community & Support

- 💬 **[GitHub Issues](https://github.com/devstress/FlinkDotnet/issues)** - Bug reports and feature requests
- 📧 **[Discussions](https://github.com/devstress/FlinkDotnet/discussions)** - Architecture questions and best practices
- 🌟 **Star the repo** - Stay updated on releases
- 🤝 **[Contribute](CONTRIBUTING.md)** - Development guidelines

## License

MIT License - see [LICENSE](LICENSE) for details.

## Acknowledgments

Built on top of:
- [Apache Flink](https://flink.apache.org/) - Stream processing framework
- [Apache Kafka](https://kafka.apache.org/) - Distributed streaming platform
- [Temporal.io](https://temporal.io/) - Durable workflow orchestration
- [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/) - Local development orchestration

---

**Ready to process billions of events?** Start with the [Getting Started Guide](docs/getting-started.md) or explore the [15-Day Learning Course](LearningCourse/README.md).

🌟 **Star this repo** to stay updated on new features and releases.
