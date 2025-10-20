# FlinkDotnet Usage Examples

This page demonstrates common patterns for using FlinkDotnet to build streaming applications.

## Basic Streaming Job

### 1. Simple Data Pipeline

```csharp
using FlinkDotNet.DataStream;

// Get Flink execution environment
var env = Flink.GetExecutionEnvironment();

// Create a basic stream processing job
var input = env.FromKafka("input-topic", "kafka:9093", "consumer-group");
var processed = input.Map(message => message.ToUpperInvariant());
processed.SinkToKafka("output-topic", "kafka:9093");

await env.ExecuteAsync("SimpleDataPipeline");
```

## Windowing and Aggregation

### 2. Tumbling Window Example

```csharp
var env = Flink.GetExecutionEnvironment();

var orders = env.FromKafka("orders", "kafka:9093", "order-group");
var aggregated = orders
    .KeyBy(order => order.CustomerId)
    .Window(TumblingTimeWindow.Of(TimeSpan.FromMinutes(5)))
    .Reduce((a, b) => new Order { Amount = a.Amount + b.Amount });

aggregated.SinkToKafka("order-totals", "kafka:9093");
await env.ExecuteAsync("OrderAggregation");
```

### 3. Sliding Window with Complex Processing

```csharp
var env = Flink.GetExecutionEnvironment();

var sensorData = env.FromKafka("sensor-data", "kafka:9093", "sensor-group");
var alerts = sensorData
    .Filter(data => data.Temperature > 25.0)
    .KeyBy(data => data.SensorId)
    .Window(SlidingTimeWindow.Of(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(2)))
    .Process(new TemperatureAggregator());

alerts.SinkToKafka("temperature-alerts", "kafka:9093");
await env.ExecuteAsync("TemperatureMonitoring");
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
var env = Flink.GetExecutionEnvironment();

var transactions = env.FromKafka("transactions", "kafka:9093", "tx-group");
var validated = transactions
    .Map(tx => ValidateTransaction(tx))
    .Filter(tx => tx.IsValid)
    .KeyBy(tx => tx.AccountId)
    .Process(new TransactionProcessor());

validated.SinkToKafka("validated-transactions", "kafka:9093");
await env.ExecuteAsync("TransactionProcessor");
```

### 7. Job Monitoring

```csharp
var env = Flink.GetExecutionEnvironment();

// Create and execute job
var stream = env.FromKafka("input", "kafka:9093", "group");
stream.SinkToKafka("output", "kafka:9093");

var jobClient = await env.ExecuteAsync("MonitoredJob");

// Monitor via Flink REST API (http://flink-jobmanager:8081)
Console.WriteLine($"Job submitted: {jobClient.JobId}");
```

## Advanced Patterns

### 8. Join Operations

```csharp
var env = Flink.GetExecutionEnvironment();

var orders = env.FromKafka("orders", "kafka:9093", "order-group");
var customers = env.FromKafka("customers", "kafka:9093", "customer-group");

var enriched = orders
    .Join(customers)
    .Where(order => order.CustomerId)
    .EqualTo(customer => customer.Id)
    .Window(TumblingTimeWindow.Of(TimeSpan.FromMinutes(1)))
    .Apply((order, customer) => EnrichOrder(order, customer));

enriched.SinkToKafka("enriched-orders", "kafka:9093");
await env.ExecuteAsync("OrderEnrichment");
```

### 9. Multi-Output Job

```csharp
var env = Flink.GetExecutionEnvironment();

var events = env.FromKafka("events", "kafka:9093", "event-group");

// Split stream based on condition
var splitStream = events.Split(
    new OutputSelector<Event>() {
        event => event.EventType == "order" ? new[] { "orders" } : new[] { "others" }
    }
);

var orderEvents = splitStream.Select("orders").Process(new OrderProcessor());
var otherEvents = splitStream.Select("others").Process(new GeneralProcessor());

orderEvents.SinkToKafka("order-events", "kafka:9093");
otherEvents.SinkToKafka("other-events", "kafka:9093");

await env.ExecuteAsync("EventRouter");
```

## Testing Integration

### 10. Integration Test Example

```csharp
[Test]
public async Task JobSubmission_WithValidConfiguration_ShouldSucceed()
{
    // Arrange
    var env = Flink.GetExecutionEnvironment();
    
    // Act
    var input = env.FromKafka("test-input", "kafka:9093", "test-group");
    var processed = input.Map(msg => msg.ToUpperInvariant());
    processed.SinkToKafka("test-output", "kafka:9093");
        
    var jobClient = await env.ExecuteAsync("TestJob");
    
    // Assert
    Assert.IsNotNull(jobClient);
    Assert.IsNotEmpty(jobClient.JobId.ToString());
}
```

For more comprehensive examples and training materials, see the [LearningCourse](../../LearningCourse/README.md).