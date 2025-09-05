# LocalTesting - Aspire Environment for LearningCourse

This Aspire setup provides the infrastructure environment for the [LearningCourse](../LearningCourse/README.md). Please refer to the LearningCourse documentation for complete usage instructions and examples.

## Message Flow Architecture

The LocalTesting environment implements a comprehensive message processing pipeline with real-time observability:

```
📥 Ingress (Single Topic)
    ↓
🔀 Kafka Producers (10 partitions)
  • Single ingress topic: "ingress-topic"
  • Partitions: partition0, partition1, ..., partition9
  • Rate: ~80,000+ msg/sec per partition
    ↓
📨 Kafka → Flink Processing
  • Flink consumes from all partitions
  • Input rate = Kafka consuming rate  
  • Processing: Real-time stream processing
    ↓
⚡ Flink Jobs (2 parallel jobs)
  • Job: real-job-1, real-job-2
  • Input operators: kafka-source
  • Output operators: kafka-sink
  • Processing latency: ~2ms per message
    ↓
🔄 Temporal Workflows (Subset Processing)
  • Triggered by: ~0.2% of messages (workflow patterns)
  • Purpose: Stateful workflow orchestration
  • Types: RealWorkflow1, RealWorkflow2, RealWorkflow3
  • Activities: RealActivity1, RealActivity2
    ↓
📤 Final Output Topic
  • All messages processed through pipeline
  • Expected: Same count as ingress (no loss in healthy system)
  • Rate: ~99,000+ msg/sec end-to-end
```

### Component Performance Characteristics

- **Kafka Producing**: ~80,000 msg/sec per partition
- **Kafka Consuming**: Flink input rate (same as producing)
- **Flink Processing**: ~99,000 msg/sec (parallel jobs)
- **Temporal Processing**: ~0.5 workflows/sec (subset of messages)
- **End-to-End**: ~99,000 msg/sec (total pipeline throughput)

### Observability Metrics

Real-time metrics are available via:
- **Prometheus**: http://localhost:18006 (metrics collection)
- **Grafana**: http://localhost:18007 (dashboards)
- **WebAPI**: http://localhost:44273/api/observability/metrics/messages-per-second

## Prerequisites

### .NET SDK Requirements
- **.NET 9.0 SDK or later** is required for proper Aspire testing framework functionality
- Check your version: `dotnet --version` (should show 9.0.x)
- Install from: https://dotnet.microsoft.com/download/dotnet/9.0

### Why .NET 9.0 is Required
- Aspire testing framework (`Aspire.Hosting.Testing`) is designed for .NET 9.0
- Integration tests will fail to build or run properly with .NET 8.0
- The observability test uses `DistributedApplicationTestingBuilder` which requires .NET 9.0

### Environment Verification
```bash
# Verify .NET version
dotnet --version  # Should show 9.0.x

# Build LocalTesting solution
dotnet build LocalTesting.sln

# Run LocalTesting Aspire orchestrator
cd LocalTesting.AppHost && dotnet run

# Run Observability tests
dotnet test LocalTesting/LocalTesting.IntegrationTests/LocalTesting.IntegrationTests.csproj --filter "Category=observability"
```