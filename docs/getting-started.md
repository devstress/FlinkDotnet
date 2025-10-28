# Getting Started with FlinkDotNet

This guide will help you get FlinkDotNet up and running, from installation through your first streaming job.

## Prerequisites

### Required Software
- **.NET 9.0 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Docker Desktop** or **Podman** - For local testing environment
- **Java 17** - Required for Flink cluster (auto-installed in containers)

### Verify Installation

```bash
# Check .NET version (must be 9.0.x)
dotnet --version

# Check Docker
docker --version

# Install Aspire workload (required on Linux, optional on Windows/macOS)
dotnet workload install aspire
```

## Quick Start (5 Minutes)

### Option 1: Install from NuGet (Recommended)

1. **Create a new .NET console application:**

```bash
dotnet new console -n MyFlinkApp
cd MyFlinkApp
```

2. **Add FlinkDotNet package:**

```bash
dotnet add package FlinkDotNet
```

3. **Write your first streaming job:**

```csharp
using FlinkDotNet.DataStream;

// Get Flink execution environment
var env = Flink.GetExecutionEnvironment();

// Read from Kafka
var orders = env.FromKafka("orders", "kafka:9092", "my-group");

// Transform with Flink operators
var highValueOrders = orders
    .Filter(o => o.Amount > 1000)
    .Map(o => o.ToUpperInvariant())
    .KeyBy(o => o.CustomerId);

// Write back to Kafka
highValueOrders.SinkToKafka("high-value-orders", "kafka:9092");

// Execute the job
await env.ExecuteAsync("HighValueOrderProcessor");
Console.WriteLine("Job submitted successfully!");
```

4. **Run your application:**

```bash
dotnet run
```

Your job will be submitted to the configured Flink cluster and start processing data.

### Option 2: Use Docker Image

Run FlinkJobGateway as a container:

```bash
docker pull devstress/flinkdotnet:latest
docker run -p 8080:8080 \
  -e FLINK_CLUSTER_HOST=your-flink-host \
  -e FLINK_CLUSTER_PORT=8081 \
  devstress/flinkdotnet:latest
```

Access the API at `http://localhost:8080`.

### Option 3: Clone and Build from Source

```bash
# Clone the repository
git clone https://github.com/devstress/FlinkDotnet.git
cd FlinkDotnet

# Build the solution
dotnet build FlinkDotNet/FlinkDotNet.sln --configuration Release

# Run integration tests to validate
cd LocalTesting
dotnet test LocalTesting.IntegrationTests --configuration Release
```

Expected result: ✅ 10 integration tests pass - validates complete pipeline.

## Local Development Environment

For local development and testing, use the LocalTesting environment with .NET Aspire:

### Start the Full Stack

```bash
cd LocalTesting
dotnet run --project LocalTesting.FlinkSqlAppHost
```

This starts:
- Apache Flink cluster (JobManager + TaskManagers)
- Apache Kafka broker
- Temporal.io server
- FlinkJobGateway service
- Monitoring stack (Prometheus, Grafana, Loki)

Access the services:
- **Aspire Dashboard**: http://localhost:15000
- **Flink Web UI**: http://localhost:18002
- **Grafana**: http://localhost:3000
- **Temporal Web UI**: http://localhost:8233

### Run Integration Tests

```bash
cd LocalTesting
dotnet test LocalTesting.IntegrationTests --configuration Release
```

This validates:
- Kafka producer/consumer with Flink processing
- Basic transformations (map, filter, flatMap)
- Stateful processing (timers, event-time windows)
- Flink SQL via TableEnvironment
- Multi-step pipelines
- Temporal workflow integration

## Configuration

### Application Settings

Create `appsettings.json` in your project:

```json
{
  "Flink": {
    "JobManagerRestAddress": "http://localhost:18002",
    "KafkaConfig": {
      "BootstrapServers": "localhost:9092",
      "GroupId": "my-consumer-group"
    }
  }
}
```

### Environment Variables

Set these for production deployments:

```bash
# Flink cluster connection
export FLINK_CLUSTER_HOST=flink-jobmanager
export FLINK_CLUSTER_PORT=8081

# Kafka configuration
export KAFKA_BOOTSTRAP_SERVERS=kafka:9092
export KAFKA_CONSUMER_GROUP=flinkdotnet-group

# Gateway settings
export GATEWAY_PORT=8080
```

## Your First Real Job

Let's build a more complete streaming job:

### 1. Define Your Data Model

```csharp
public record Order(
    string OrderId,
    string CustomerId,
    decimal Amount,
    string Region,
    DateTime Timestamp
);
```

### 2. Create the Streaming Job

```csharp
using FlinkDotNet.DataStream;

public class OrderProcessingJob
{
    public static async Task Main(string[] args)
    {
        // Configure Flink environment
        var env = Flink.GetExecutionEnvironment();
        env.SetParallelism(4);
        env.EnableCheckpointing(TimeSpan.FromSeconds(30));

        // Read orders from Kafka
        var orders = env.FromKafka<Order>(
            topic: "orders",
            bootstrapServers: "kafka:9092",
            groupId: "order-processor"
        );

        // Filter high-value orders
        var highValueOrders = orders
            .Filter(order => order.Amount > 1000);

        // Group by region and calculate totals
        var regionalTotals = highValueOrders
            .KeyBy(order => order.Region)
            .Window(TumblingTimeWindow.Of(TimeSpan.FromMinutes(5)))
            .Reduce((a, b) => a with { Amount = a.Amount + b.Amount });

        // Write results to Kafka
        regionalTotals.SinkToKafka("regional-totals", "kafka:9092");

        // Execute the job
        var jobClient = await env.ExecuteAsync("OrderProcessingJob");
        Console.WriteLine($"Job submitted: {jobClient.JobId}");
    }
}
```

### 3. Run and Monitor

```bash
# Run the job
dotnet run

# Check Flink Web UI
open http://localhost:18002

# View Grafana dashboards
open http://localhost:3000
```

## Common Patterns

### Pattern 1: Simple Transformation Pipeline

```csharp
var env = Flink.GetExecutionEnvironment();

var input = env.FromKafka("input", "kafka:9092", "group1");
var output = input
    .Filter(msg => msg.IsValid)
    .Map(msg => msg.Transform())
    .KeyBy(msg => msg.Key);

output.SinkToKafka("output", "kafka:9092");
await env.ExecuteAsync("SimpleTransform");
```

### Pattern 2: Windowed Aggregation

```csharp
var env = Flink.GetExecutionEnvironment();

var events = env.FromKafka("events", "kafka:9092", "group2");
var aggregated = events
    .KeyBy(e => e.UserId)
    .Window(TumblingTimeWindow.Of(TimeSpan.FromMinutes(1)))
    .Reduce((a, b) => new Event { Count = a.Count + b.Count });

aggregated.SinkToKafka("aggregated", "kafka:9092");
await env.ExecuteAsync("WindowedAgg");
```

### Pattern 3: Multi-Stream Join

```csharp
var env = Flink.GetExecutionEnvironment();

var orders = env.FromKafka("orders", "kafka:9092", "group3");
var customers = env.FromKafka("customers", "kafka:9092", "group4");

var enriched = orders
    .Join(customers)
    .Where(order => order.CustomerId)
    .EqualTo(customer => customer.Id)
    .Window(TumblingTimeWindow.Of(TimeSpan.FromMinutes(1)))
    .Apply((order, customer) => EnrichOrder(order, customer));

enriched.SinkToKafka("enriched-orders", "kafka:9092");
await env.ExecuteAsync("OrderEnrichment");
```

## Troubleshooting

### Connection Issues

**Problem**: Cannot connect to Flink cluster

**Solution**:
```bash
# Check Flink is running
curl http://localhost:18002/overview

# Verify network connectivity
docker network ls
docker network inspect <network-name>

# Check service logs
docker logs flink-jobmanager
```

### Job Submission Failures

**Problem**: Job submission fails with validation errors

**Solution**:
```csharp
// Enable detailed logging
var env = Flink.GetExecutionEnvironment();
env.SetLogLevel(LogLevel.Debug);

// Validate job before submission
var jobDefinition = env.GetJobDefinition();
var validation = JobDefinitionValidator.Validate(jobDefinition);
if (!validation.IsValid)
{
    Console.WriteLine($"Validation errors: {string.Join(", ", validation.Errors)}");
}
```

### Performance Issues

**Problem**: Low throughput or high latency

**Solution**:
```csharp
// Increase parallelism
env.SetParallelism(8);

// Tune buffer timeout
env.SetBufferTimeout(TimeSpan.FromMilliseconds(100));

// Enable operator chaining
env.EnableOperatorChaining();
```

See [Troubleshooting Guide](troubleshooting.md) for more solutions.

## Next Steps

### Learning Path

1. **Basics** (Day 1-2)
   - [Quickstart Guide](quickstart.md) - Hello World examples
   - [API Reference](api-reference.md) - Complete API documentation

2. **Intermediate** (Day 3-7)
   - [Architecture Guide](architecture-and-usecases.md) - System design patterns
   - [Observability](observability.md) - Monitoring and metrics
   - [Flink 2.1 Features](flink-21-features.md) - Advanced capabilities

3. **Advanced** (Day 8-14)
   - [Performance Tuning](performance-benchmarks.md) - Optimization strategies
   - [Deployment Guide](deployment.md) - Production deployment
   - [Learning Course](../LearningCourse/README.md) - 15-day comprehensive training

### Example Projects

- **LocalTesting** - Complete local development environment
  - Location: `LocalTesting/`
  - Run: `dotnet run --project LocalTesting.FlinkSqlAppHost`

- **Sample Applications** - Reference implementations
  - Location: `Sample/`
  - Run: Follow README in each sample

- **Learning Course** - Hands-on exercises
  - Location: `LearningCourse/`
  - 15 days of progressive examples

### Join the Community

- 💬 **GitHub Issues** - Bug reports and feature requests
- 📧 **Discussions** - Ask questions and share experiences
- 🌟 **Star the repo** - Stay updated on releases
- 🤝 **Contribute** - See [CONTRIBUTING.md](../CONTRIBUTING.md)

## Validation Checklist

Before deploying to production, ensure:

- [ ] All integration tests pass locally
- [ ] Job successfully submits to Flink cluster
- [ ] Monitoring dashboards show expected metrics
- [ ] Checkpointing is enabled and working
- [ ] Error handling is implemented
- [ ] Resource limits are configured
- [ ] Security settings are applied
- [ ] Documentation is updated

## Support

If you encounter issues:

1. Check [Troubleshooting Guide](troubleshooting.md)
2. Search [GitHub Issues](https://github.com/devstress/FlinkDotnet/issues)
3. Ask in [Discussions](https://github.com/devstress/FlinkDotnet/discussions)
4. Review [Learning Course](../LearningCourse/README.md) examples

---

**Ready to build streaming applications?** Continue to [API Reference](api-reference.md) for detailed documentation.
