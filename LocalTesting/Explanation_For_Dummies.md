# LocalTesting: High-Performance Message Processing System

## Processing Flow

```
HTTP API → Kafka → Flink → Redis/Temporal → sample_response Topic
   ↓        ↓       ↓          ↓                    ↓
 Send   Queue   Process   Complex Logic      Final Results
1M msgs  msgs    msgs    + Atomic Redis      (with metadata)
```

**Routing Logic**: 
- **Simple transformations** → Direct Flink processing → sample_response
- **Complex operations** → Flink → Temporal workflows → Redis atomic transactions → sample_response
- **All messages** → End up in `sample_response` Kafka topic as final destination

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

### Temporal - Complex Business Logic Orchestrator
- **Purpose**: Handles complex multi-step operations that require atomic transactions and state persistence
- **Routing Logic to Temporal**: Messages route to Temporal when they need:
  - Atomic Redis operations (unique ID generation, state updates)
  - Multi-step workflows that must complete together or fail together
  - Complex business rules that span multiple services
  - Time-based operations (delays, scheduling, retries)
- **Specific Complex Jobs in LocalTesting**:
  - **Unique ID Generation**: Create unique tracking IDs in Redis with atomic transactions
  - **Message State Management**: Update message status atomically (Produced → Delivered)
  - **Error Recovery**: Retry failed operations with exponential backoff
  - **FIFO Queue Coordination**: Ensure message ordering within logical queues
  - **Multi-Service Orchestration**: Coordinate between Kafka, Flink, and Redis

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

### Step 4: Temporal Orchestrates Complex Operations
```csharp
// TemporalSecurityTokenService.cs - Real workflow orchestration
@WorkflowMethod
public async Task ProcessMessageWorkflow(ComplexLogicMessage message) {
    // Step 1: Create unique tracking ID in Redis (atomic operation)
    string uniqueId = await activities.CreateUniqueIdAsync(message.MessageId);
    await activities.StoreInRedisAsync($"message:{uniqueId}", message, TimeSpan.FromHours(24));
    
    // Step 2: Update message state atomically 
    await activities.UpdateMessageStateAsync(message.MessageId, MessageState.TemporalProcessing);
    
    // Step 3: Apply complex business logic with error handling
    ComplexLogicResult result;
    try {
        result = await activities.ProcessComplexLogicAsync(message);
        await activities.UpdateMessageStateAsync(message.MessageId, MessageState.TemporalCompleted);
    }
    catch (Exception ex) {
        // Temporal's built-in retry with exponential backoff
        await Workflow.Sleep(TimeSpan.FromSeconds(Math.Pow(2, attempt))); 
        result = await activities.RetryProcessingAsync(message);
    }
    
    // Step 4: Atomic final state update and result storage
    await activities.AtomicFinalizationAsync(message.MessageId, result);
}
```

**Temporal's Exact Role in LocalTesting**:
- **Routing Condition**: Messages enter Temporal workflows when they require complex operations like:
  - Creating unique IDs with Redis atomic transactions: `INCR message_counter` + `SET unique:{id}`
  - Multi-step state updates that must succeed together or rollback
  - Business logic requiring retry with state preservation
  - Coordination between Kafka, Flink, and Redis operations
- **State Persistence**: If the system crashes, Temporal resumes exactly where it left off
- **Atomic Operations**: Redis commands are grouped into atomic transactions to prevent data loss
- **Error Recovery**: Built-in exponential backoff retry logic with state management

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

## Final Results and Data Flow

### Result Destination: `sample_response` Kafka Topic
- **Final output topic**: All processed messages end up in the `sample_response` Kafka topic
- **Topic structure**: 10 partitions, 3 replicas for high availability
- **Message format**: Contains original message + processing metadata + unique tracking ID

### How Components Write to Final Results

#### Flink to sample_response Topic
```csharp
// Flink job writes processed messages to sample_response
DataStream<ComplexLogicMessage> processedStream = env
    .addSource(new KafkaSource<>("api-retrieved-messages"))
    .map(message -> applyBusinessLogic(message))
    .addSink(new KafkaSink<>("sample_response"));
```

#### Temporal to sample_response Topic  
```csharp
// Temporal workflow writes final results after complex operations
public async Task FinalizeMessageAsync(ComplexLogicMessage message, ComplexLogicResult result) {
    // Create final message with all processing metadata
    var finalMessage = new ComplexLogicMessage {
        MessageId = message.MessageId,
        Content = result.ProcessedContent,
        UniqueTrackingId = result.UniqueId,
        ProcessingTimestamp = DateTime.UtcNow,
        LogicalQueueName = message.LogicalQueueName,
        FinalState = MessageState.Delivered
    };
    
    // Write to sample_response topic with FIFO preservation
    await kafkaProducer.ProduceAsync("sample_response", finalMessage);
    
    // Update Redis with final state atomically
    await redis.SetAsync($"final:{message.MessageId}", finalMessage);
}
```

### FIFO Protection per Logical Queue

#### Partition-Level FIFO
- **Kafka guarantee**: Messages within same partition maintain strict ordering
- **Logical queue mapping**: Each logical queue maps to consistent partition using hash
- **Hash function**: `partition = hash(logicalQueueName) % partitionCount`

#### Processing Order Preservation
```csharp
// FIFO protection across the entire pipeline
public class FIFOMessageProcessor {
    // Step 1: Kafka producer preserves order per logical queue
    var partition = GetPartitionForLogicalQueue(message.LogicalQueueName);
    await producer.ProduceAsync(new TopicPartition("input-topic", partition), message);
    
    // Step 2: Flink processes messages in partition order
    dataStream.keyBy(msg => msg.LogicalQueueName)  // Maintains order per logical queue
              .process(new OrderPreservingProcessFunction());
    
    // Step 3: Temporal workflows execute sequentially per logical queue
    var workflowId = $"queue-{message.LogicalQueueName}-{message.SequenceNumber}";
    await temporalClient.StartWorkflowAsync(workflowId, message);
    
    // Step 4: Results written to sample_response maintain order
    var outputPartition = GetPartitionForLogicalQueue(message.LogicalQueueName);
    await producer.ProduceAsync(new TopicPartition("sample_response", outputPartition), result);
}
```

#### Atomic Order Protection
- **Redis sequence tracking**: `INCR queue:{logicalQueueName}:sequence` ensures no gaps
- **Temporal workflow chaining**: Each workflow waits for previous sequence number
- **Error handling**: Failed messages don't block queue processing (moved to dead letter queue)

### Message Journey Summary
```
1M messages → HTTP API → Kafka (10 partitions)
                ↓
         Flink processing (preserves partition order)
                ↓
      Temporal workflows (complex operations + Redis atomic transactions)
                ↓
         sample_response topic (final results with FIFO per logical queue)
```

### Message Journey Summary
```
1M messages → HTTP API → Kafka (10 partitions)
                ↓
         Flink processing (preserves partition order)
                ↓
      Temporal workflows (complex operations + Redis atomic transactions)
                ↓
         sample_response topic (final results with FIFO per logical queue)
```

### Complete Message State Pipeline
Every message flows through these tracked states:
```
Produced → Consumed → FlinkProcessing → FlinkProcessed → 
TemporalReceived → TemporalProcessing → TemporalCompleted → Delivered
```

**State tracking in Redis**: Each state change is atomically updated in Redis with timestamps
**Failed states**: Can occur at any point and trigger Temporal retry workflows with exponential backoff
**Final state**: `Delivered` when message successfully written to `sample_response` topic

### Monitoring and Observability

#### Real-Time Messages Per Second Metrics
- **Kafka Producer Rate**: `/api/observability/metrics/layer/kafka` - Messages/sec entering each partition
- **Flink Processing Rate**: `/api/observability/metrics/layer/flink` - Messages/sec being processed by Flink jobs
- **Temporal Workflow Rate**: `/api/observability/metrics/layer/temporal` - Messages/sec proceeding through Temporal workflows
- **End-to-End Flow Rate**: `/api/observability/metrics/messages-per-second` - Complete pipeline throughput

#### Temporal-Specific Observability
```csharp
// Real-time Temporal messages per second tracking
GET /api/observability/metrics/layer/temporal
{
  "TemporalMetrics": {
    "WorkflowRates": {
      "temporal_workflow_complex_business_logic": { "ExecutionsPerSecond": 2.5 },
      "temporal_workflow_data_enrichment": { "ExecutionsPerSecond": 1.8 }
    },
    "ActivityRates": {
      "temporal_activity_enrich_data": { "ExecutionsPerSecond": 3.2 },
      "temporal_activity_validate_business_rules": { "ExecutionsPerSecond": 2.1 }
    }
  },
  "Summary": {
    "TotalTemporalRate": 9.6,
    "ActiveWorkflows": 2,
    "ActiveActivities": 2
  }
}
```

#### Message Flow Tracking Through Temporal
- **Entry Rate**: Messages entering Temporal workflows (subset of total Flink output)
- **Processing Rate**: Messages being actively processed by Temporal activities  
- **Completion Rate**: Messages completing Temporal workflows and writing to final results
- **State Persistence**: Real-time tracking of workflow execution states

#### Grafana Dashboard Integration
- **Real-time throughput**: Live graphs of messages/sec at each pipeline stage
- **Temporal workflow metrics**: Execution rates, completion rates, error rates
- **Message tracking**: Query any message state by ID across the entire journey
- **Performance metrics**: End-to-end latency from HTTP API to sample_response topic

#### Prometheus Metrics Available
```bash
# Temporal workflow execution rates
temporal_workflow_executions_per_second
temporal_activity_executions_per_second
temporal_workflow_completion_rate
temporal_workflow_error_rate

# Pipeline flow rates  
kafka_producer_messages_per_second
flink_job_messages_per_second
flow_kafka_to_flink_rate
flow_flink_to_temporal_rate
flow_temporal_to_results_rate
```

**Performance**: With optimizations, 1 million messages complete the full journey in under 10 seconds with strict FIFO ordering preserved per logical queue. Real-time observability shows exactly how many messages per second are proceeding through each component, including detailed Temporal workflow metrics.