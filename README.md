# FlinkDotNet

**FlinkDotNet** is a comprehensive .NET framework that enables developers to build and submit streaming jobs to Apache Flink 2.1 clusters using a fluent C# API. It provides extensive compatibility with Apache Flink 2.1 and focuses on three core technologies - **Apache Flink** (real-time stream processing), **Kafka** (message streaming broker), and **Temporal** (workflow orchestration) - making it easier for .NET developers to handle large-scale data processing challenges in multi-tiered, distributed real-time stream processing.

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

**Prerequisites:** .NET 9.0 SDK, Docker Desktop

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
var orders = env.FromKafka("orders", "kafka:9093", "my-group");

// Transform with Flink operators
var processed = orders
    .Filter(o => o.Amount > 1000)
    .Map(o => o.ToUpperInvariant())
    .KeyBy(o => o.CustomerId);

// Write back to Kafka
processed.SinkToKafka("high-value-orders", "kafka:9093");

await env.ExecuteAsync("fraud-detection");
```

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

## Key Features

| Feature | Description |
|---------|-------------|
| **DataStream API** | Complete Apache Flink 2.1 API: map, filter, flatMap, window, aggregate, join |
| **Kafka Integration** | First-class support for Kafka sources and sinks |
| **Event-Time Processing** | Watermarks, late data handling, time windows (tumbling/sliding/session) |
| **Exactly-Once** | Checkpointing and savepoints for fault tolerance |
| **Dynamic Scaling** | Flink 2.1 adaptive scheduler, reactive mode, savepoint-based scaling |
| **Multi-Cluster Orchestration** | Temporal-powered workflows for thousands of clusters |
| **Local Development** | .NET Aspire integration - start full stack with one command |
| **Enterprise Observability** | Full PGL stack (Prometheus, Grafana, Loki) + OpenTelemetry |

## Proven at Scale

✅ **10 Integration Tests Passing** - Complete pipeline validated on every commit

🔗 [**View Live Test Results**](https://github.com/devstress/FlinkDotnet/actions/workflows/localtesting-integration-tests.yml)

**What's validated:**
- ✅ Kafka producer/consumer with Flink processing
- ✅ Basic transformations (map, filter, flatMap)
- ✅ Stateful processing (timers, event-time windows)
- ✅ Flink SQL via TableEnvironment and SQL Gateway
- ✅ Complex multi-step pipelines
- ✅ Aspire orchestration and service discovery
- ✅ Temporal workflow integration

## Real-World Use Cases

**Financial Services** - Real-time fraud detection, risk calculation, regulatory reporting  
**E-commerce** - Order processing, inventory management, personalization  
**IoT/Manufacturing** - Sensor data processing, predictive maintenance, quality control  
**Healthcare** - Patient monitoring, care coordination, compliance tracking  

See [**Architecture & Use Cases**](docs/architecture-and-usecases.md) for detailed implementations.

## Project Structure

```
FlinkDotNet/
├── FlinkDotNet.DataStream/      # Apache Flink 2.1 compatible streaming API
├── Flink.JobBuilder/            # Fluent DSL for rapid development
├── FlinkDotNet.JobGateway/      # Job submission service
├── FlinkDotNet.Orchestration/   # Multi-cluster management
├── FlinkDotNet.Temporal/        # Durable workflow definitions
└── FlinkDotNet.ClusterManager/  # Actor-based cluster lifecycle

LocalTesting/                     # Complete local dev environment
├── LocalTesting.FlinkSqlAppHost/    # .NET Aspire orchestration
└── LocalTesting.IntegrationTests/   # End-to-end validation tests

LearningCourse/                   # 15-day learning path
└── Day01-Kafka-Flink-Data-Pipeline/ # Baeldung tutorial adaptation
```

## Documentation

| Guide | Description |
|-------|-------------|
| [**Getting Started**](docs/wiki/Getting-Started.md) | Complete setup and first job |
| [**Architecture & Use Cases**](docs/architecture-and-usecases.md) | System design, scaling strategies, real-world examples |
| [**API Reference**](docs/api-reference.md) | Complete DataStream API documentation |
| [**Flink vs Kafka Streams vs Temporal**](docs/flink-vs-temporal-decision-guide.md) | When to use each technology |
| [**Learning Course**](LearningCourse/README.md) | 15-day hands-on exercises |
| [**Contributing**](CONTRIBUTING.md) | Development guidelines |

### Quick Links

- 📖 [Quickstart Guide](docs/quickstart.md)
- 🔧 [Local Development Setup](docs/local-testing-setup.md)
- 📊 [Observability & Monitoring](docs/observability.md)
- 🚨 [Troubleshooting](docs/troubleshooting.md)
- 🔄 [CI/CD Integration](docs/ci-cd-integration.md)

## Learning Path

New to FlinkDotNet? Follow our [**15-Day Learning Course**](LearningCourse/README.md):

- **Days 1-2:** Kafka + Flink fundamentals, stream processing basics
- **Days 3-4:** Event-time windowing, backpressure handling
- **Days 5-6:** Temporal workflows, enterprise observability
- **Days 7-8:** Stress testing, exactly-once semantics
- **Days 9-10:** Performance tuning, security patterns
- **Days 11-14:** Disaster recovery, chaos engineering
- **Day 15:** Capstone project

Each day includes working code examples and integration tests.

## Apache Flink 2.1 Support

FlinkDotNet implements extensive Apache Flink 2.1 features:

- **Adaptive Scheduler** - Automatic parallelism optimization
- **Reactive Mode** - Elastic scaling based on cluster resources
- **Dynamic Scaling** - Change parallelism without job restart
- **Advanced Partitioning** - Rebalance, rescale, forward, shuffle, broadcast, custom
- **Savepoint Operations** - Create, restore, scale from savepoints
- **Fine-grained Resource Management** - Slot sharing groups, resource profiles

See [**Apache Flink 2.1 Features**](docs/flink-21-features.md) for complete API mapping.

## Performance

**Validated throughput** (LocalTesting environment):
- 📈 **800K+ messages/sec** through complete Kafka → Flink → Output pipeline
- 📈 **80K+ msg/sec per Kafka partition** (20 partitions tested)
- 📈 **10% Temporal workflow processing** (80K workflows/sec) with full orchestration
- 📈 **3 TaskManagers, 8 slots each** = 24 parallel task capacity

See [**Performance Benchmarks**](docs/performance-benchmarks.md) for detailed metrics.

## Community & Support

- 💬 **GitHub Issues** - Bug reports and feature requests
- 📧 **Discussions** - Architecture questions and best practices
- 🌟 **Star the repo** - Stay updated on releases
- 🤝 **Contribute** - See [CONTRIBUTING.md](CONTRIBUTING.md)

## Comparison

| Feature | FlinkDotNet | Kafka Streams | AWS Kinesis | Azure Stream Analytics |
|---------|-------------|---------------|-------------|------------------------|
| Language | **C# native** | Java/Scala | Multiple | SQL/JavaScript |
| Scale | Millions/sec | < 100K/sec | Thousands/sec | Cloud-dependent |
| Exactly-Once | ✅ External systems | ✅ Kafka only | ❌ | ❌ |
| Complex CEP | ✅ | ❌ | ❌ | Limited |
| Multi-Cloud | ✅ | ✅ | AWS only | Azure only |
| Local Dev | ✅ Aspire | ✅ | ❌ | ❌ |
| Cost | Infrastructure | Infrastructure | Per shard | Per job |

See [**Technology Decision Guide**](docs/flink-vs-temporal-decision-guide.md) for detailed comparison.

## Requirements

- **.NET 9.0 SDK** - Required for all development
- **Docker Desktop** - For local testing with Aspire
- **Apache Flink 2.1 cluster** - Production deployments
- **Apache Kafka** - For stream sources/sinks (optional)

## License

MIT License - see [LICENSE](LICENSE) for details.

## Acknowledgments

Built on top of:
- [Apache Flink](https://flink.apache.org/) - Stream processing framework
- [Apache Kafka](https://kafka.apache.org/) - Distributed streaming platform
- [Temporal.io](https://temporal.io/) - Durable workflow orchestration
- [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/) - Local development orchestration

---

**Ready to process billions of events?** Start with the [Quick Start](#quick-start) or explore the [Learning Course](LearningCourse/README.md).

🌟 **Star this repo** to stay updated on new features and releases.
