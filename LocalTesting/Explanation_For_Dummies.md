# LocalTesting: High-Performance Message Processing System

## Processing Flow

```
HTTP API → Kafka → Flink → Redis/Temporal → Results
   ↓        ↓       ↓          ↓           ↓
 Send   Queue   Process   Track/Orchestrate Monitor
1M msgs  msgs    msgs      workflows    everything
```

## What Each Component Does

### WebAPI - Entry Point 
- **Purpose**: Receives your 1 million messages via HTTP POST
- **Endpoint**: `POST /api/ComplexLogicStressTest/step2/stress-test-1-million`
- **What it does**: Validates messages, adds tracking IDs, sends to Kafka

### Kafka - Message Queue
- **Purpose**: Holds and distributes your 1 million messages across partitions
- **Configuration**: 10 partitions, 100 logical queues (10 msgs/queue/sec = 1000 msg/sec total)
- **High-speed settings**: 128KB batches, LZ4 compression, 2M message buffer

### Flink - Stream Processor  
- **Purpose**: Transforms messages in real-time as they flow through
- **What it does**: Business logic, data enrichment, validation, routing decisions
- **Parallelism**: Runs across multiple workers for high throughput

### Temporal - Workflow Orchestrator
- **Purpose**: Manages complex multi-step business processes that span time
- **What it does exactly**: 
  - Handles long-running workflows (minutes/hours/days)
  - Manages retry logic and error recovery
  - Coordinates between different services
  - Maintains workflow state even if services restart
  - Example: "Send email → wait 24 hours → send reminder → wait 3 days → escalate"

### Redis - State Store
- **Purpose**: Fast memory storage for message tracking and intermediate results
- **What it stores**: Message states, processing results, cache data

## Code Flow Walkthrough

### Step 1: Send 1 Million Messages
```csharp
// KafkaProducerService.cs - High-performance producer
var config = new ProducerConfig {
    BatchSize = 131072,           // 128KB batches for speed
    LingerMs = 5,                 // Pack messages quickly  
    QueueBufferingMaxMessages = 2000000,  // 2M message buffer
    MaxInFlight = 10,             // Multiple parallel requests
    CompressionType = CompressionType.Lz4,  // Fast compression
    Acks = Acks.Leader            // Leader-only ack for speed
};

// Process partitions in parallel
var partitionTasks = messagesByPartition.Select(async partitionGroup => {
    foreach (var message in partitionMessages) {
        await producer.ProduceAsync(topicPartition, kafkaMessage);
    }
});
```

### Step 2: Kafka Distributes Messages
- **10 partitions** process messages in parallel
- **100 logical queues** (10 per partition) 
- **Rate limiting**: 100 msg/sec per logical queue = 10,000 msg/sec total
- **Each message gets**: tracking ID, correlation ID, timestamp, partition number

### Step 3: Flink Processes Messages  
```csharp
// Flink job receives messages and transforms them
DataStream<ComplexLogicMessage> stream = env
    .addSource(new KafkaSource<>(kafkaProps))
    .map(message -> {
        // Apply business logic transformations
        return processComplexLogic(message);
    })
    .keyBy(msg -> msg.getCorrelationId())
    .process(new ComplexProcessingFunction());
```

### Step 4: Temporal Orchestrates Workflows
```csharp
// TemporalArchitectureTestController.cs
@WorkflowMethod
public void processMessageWorkflow(ComplexLogicMessage message) {
    // Step 1: Validate message
    boolean isValid = activities.validateMessage(message);
    
    // Step 2: Process business logic  
    ProcessingResult result = activities.processBusinessLogic(message);
    
    // Step 3: If failed, retry with exponential backoff
    if (!result.isSuccess()) {
        Workflow.sleep(Duration.ofSeconds(30));  // Wait 30 seconds
        result = activities.retryProcessing(message);  // Retry
    }
    
    // Step 4: Send notification after processing
    activities.sendNotification(result);
    
    // Step 5: Schedule follow-up workflow for tomorrow
    Workflow.newChildWorkflowStub(FollowUpWorkflow.class)
           .execute(message.getId());
}
```

**What Temporal Does Exactly:**
- **Reliability**: If your server crashes, Temporal restarts the workflow from last checkpoint
- **Retries**: Built-in retry logic with exponential backoff
- **Scheduling**: Run workflows at specific times or after delays  
- **State Management**: Maintains workflow state across service restarts
- **Timeouts**: Handles long-running operations with timeouts
- **Error Handling**: Sophisticated error handling and compensation logic

### Step 5: Redis Tracks Everything
```csharp
// MessageStateService.cs - Tracks message through pipeline
public async Task UpdateStateAsync(string messageId, MessageState newState) {
    var tracking = new MessageTrackingInfo {
        MessageId = messageId,
        CurrentState = newState,  // Produced → Consumed → FlinkProcessing → TemporalReceived → Delivered
        LastUpdatedAt = DateTime.UtcNow
    };
    await _redis.SetAsync($"message:{messageId}", tracking);
}
```

## Producer Egress Optimization - How to Handle 1 Million Messages

### Current Bottlenecks
- **Rate limiting**: 100 msg/sec per logical queue × 100 queues = 10,000 msg/sec max
- **For 1M messages**: Would take 100 seconds (1,000,000 ÷ 10,000)

### Speed Up Producer Egress

#### 1. Increase Logical Queues
```csharp
// In ComplexLogicStressTestController.cs
[HttpPost("configure-high-speed")]
public async Task ConfigureHighSpeed() {
    var config = new BackpressureConfiguration {
        LogicalQueuesPerPartition = 100,  // Increase from 10 to 100
        MessagesPerSecondPerQueue = 100,  // Keep at 100
        TotalPartitions = 10
    };
    // New total: 100 × 100 × 10 = 100,000 msg/sec
    // 1M messages in 10 seconds
}
```

#### 2. Optimize Kafka Producer Settings
```csharp
// In KafkaProducerService.cs - Already optimized for high throughput
var config = new ProducerConfig {
    BatchSize = 262144,           // Increase to 256KB batches
    LingerMs = 1,                 // Reduce linger time
    QueueBufferingMaxMessages = 5000000,  // Increase buffer to 5M
    MaxInFlight = 20,             // More parallel requests
    CompressionType = CompressionType.Lz4,  // Fastest compression
    Acks = Acks.None              // No acks for maximum speed (use with caution)
};
```

#### 3. Add More Kafka Partitions
```csharp
// Scale from 10 to 50 partitions
var partitionCount = 50;
var logicalQueuesPerPartition = 20;  
var ratePerQueue = 100;
// Total: 50 × 20 × 100 = 100,000 msg/sec
```

#### 4. Parallel Producer Instances
```csharp
// Run multiple producer instances in parallel
var producerTasks = Enumerable.Range(0, 10).Select(async i => {
    var producer = new KafkaProducerService(/*config*/);
    var messageBatch = messages.Skip(i * batchSize).Take(batchSize);
    await producer.ProduceMessagesAsync(topic, messageBatch);
});
await Task.WhenAll(producerTasks);
```

### Maximum Theoretical Throughput
- **With optimizations**: 100,000+ messages/second
- **1 million messages**: Under 10 seconds
- **Hardware dependent**: SSD storage, fast network, sufficient RAM

## Message State Tracking

Every message flows through these states:
```
Produced → Consumed → FlinkProcessing → FlinkProcessed → 
TemporalReceived → TemporalProcessing → TemporalCompleted → Delivered
```

**Failed states** can occur at any point and trigger Temporal retry workflows.

## Monitoring and Observability

- **Grafana dashboards**: Real-time throughput, latency, error rates
- **Prometheus metrics**: kafka_producer_messages, flink_job_latency, temporal_workflow_duration  
- **Message tracking**: Query any message state by ID
- **Performance metrics**: Messages/second per partition, end-to-end latency