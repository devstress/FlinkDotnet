# FlinkDotnet Usage Examples

This page demonstrates common patterns for using FlinkDotnet to build streaming applications.

## Basic Streaming Job

### 1. Simple Data Pipeline

```csharp
using Flink.JobBuilder;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddFlinkJobBuilder(config =>
{
    config.BaseUrl = "http://localhost:18000";
});

var serviceProvider = services.BuildServiceProvider();

// Create a basic stream processing job
var job = serviceProvider.CreateJobBuilder()
    .FromKafka("input-topic")
    .Map("message => message.toUpperCase()")
    .ToKafka("output-topic");

await job.Submit("SimpleDataPipeline");
```

## Windowing and Aggregation

### 2. Tumbling Window Example

```csharp
var windowedJob = serviceProvider.CreateJobBuilder()
    .FromKafka("orders")
    .KeyBy("customerId")
    .Window(TimeWindow.Tumbling(TimeSpan.FromMinutes(5)))
    .Aggregate("SUM", "amount")
    .ToKafka("order-totals");

await windowedJob.Submit("OrderAggregation");
```

### 3. Sliding Window with Complex Processing

```csharp
var complexJob = serviceProvider.CreateJobBuilder()
    .FromKafka("sensor-data")
    .Filter("temperature > 25.0")
    .KeyBy("sensorId")
    .Window(TimeWindow.Sliding(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(2)))
    .Process(new TemperatureAggregator())
    .ToKafka("temperature-alerts");

await complexJob.Submit("TemperatureMonitoring");
```

## Configuration Examples

### 4. Production Configuration

```json
{
  "Flink": {
    "JobManagerRestAddress": "http://flink-jobmanager:8081",
    "KafkaConfig": {
      "BootstrapServers": "kafka-cluster:9092",
      "GroupId": "flinkdotnet-consumer",
      "SecurityProtocol": "SASL_SSL",
      "SaslMechanism": "PLAIN",
      "SaslUsername": "${KAFKA_USERNAME}",
      "SaslPassword": "${KAFKA_PASSWORD}"
    },
    "CheckpointConfig": {
      "CheckpointInterval": "00:00:30",
      "CheckpointTimeout": "00:01:00",
      "StateBackend": "RocksDB"
    }
  }
}
```

### 5. Local Development Configuration

```json
{
  "Flink": {
    "JobManagerRestAddress": "http://localhost:18002",
    "KafkaConfig": {
      "BootstrapServers": "localhost:9092",
      "GroupId": "flinkdotnet-dev"
    }
  }
}
```

## Error Handling and Monitoring

### 6. Job with Error Handling

```csharp
var robustJob = serviceProvider.CreateJobBuilder()
    .FromKafka("transactions")
    .Map("transaction => ValidateTransaction(transaction)")
    .Filter("isValid == true")
    .KeyBy("accountId")
    .Process(new TransactionProcessor())
    .ToKafka("validated-transactions")
    .OnError(ErrorStrategy.DeadLetter("failed-transactions"));

await robustJob.Submit("TransactionProcessor");
```

### 7. Job Monitoring

```csharp
// Submit job and get handle for monitoring
var jobResult = await job.Submit("MonitoredJob");

// Check job status
var status = await jobResult.GetStatus();
Console.WriteLine($"Job Status: {status.State}");

// Get job metrics
var metrics = await jobResult.GetMetrics();
Console.WriteLine($"Records processed: {metrics.RecordsIn}");
```

## Advanced Patterns

### 8. Join Operations

```csharp
var joinJob = serviceProvider.CreateJobBuilder()
    .FromKafka("orders")
    .Join(
        other: serviceProvider.CreateStream().FromKafka("customers"),
        condition: "orders.customerId == customers.id",
        window: TimeWindow.Tumbling(TimeSpan.FromMinutes(1))
    )
    .Map("joined => EnrichOrder(joined)")
    .ToKafka("enriched-orders");

await joinJob.Submit("OrderEnrichment");
```

### 9. Multi-Output Job

```csharp
var multiOutputJob = serviceProvider.CreateJobBuilder()
    .FromKafka("events")
    .Branch(
        condition: "eventType == 'order'",
        thenTo: "order-events",
        elseTo: "other-events"
    )
    .Process("order-events", new OrderProcessor())
    .Process("other-events", new GeneralProcessor());

await multiOutputJob.Submit("EventRouter");
```

## Testing Integration

### 10. Integration Test Example

```csharp
[Test]
public async Task JobSubmission_WithValidConfiguration_ShouldSucceed()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddFlinkJobBuilder(config =>
    {
        config.BaseUrl = TestConfig.FlinkGatewayUrl;
    });
    
    var serviceProvider = services.BuildServiceProvider();
    
    // Act
    var job = serviceProvider.CreateJobBuilder()
        .FromKafka("test-input")
        .Map("message => message.toUpperCase()")
        .ToKafka("test-output");
        
    var result = await job.Submit("TestJob");
    
    // Assert
    Assert.IsTrue(result.IsSuccess);
    Assert.IsNotEmpty(result.FlinkJobId);
}
```

For more comprehensive examples and training materials, see the [LearningCourse](../../LearningCourse/README.md).