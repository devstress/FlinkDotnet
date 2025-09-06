# WI4: Kafka Producer Performance Improvement

**File**: `WIs/WI4_kafka-producer-performance-improvement.md`
**Title**: [Kafka] Improve producer throughput from 18 msg/sec to thousands msg/sec per partition while maintaining FIFO ordering  
**Description**: Current Kafka producer shows very low throughput (18 msg/sec per partition) instead of expected thousands of messages per second. Need to investigate and optimize for high-performance message production while maintaining FIFO ordering within partitions.
**Priority**: High
**Component**: Kafka Producer
**Type**: Performance Enhancement
**Assignee**: AI Agent
**Created**: 2024-12-19
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI1, WI2, WI3: Observability metrics fixes - learned importance of proper debugging and root cause analysis
### Lessons Applied  
- Debug first before proposing solutions
- Test locally to verify fixes work
- Make minimal, surgical changes
### Problems Prevented
- Avoiding premature optimization without understanding root cause
- Ensuring changes are validated locally before committing

## Phase 1: Investigation
### Requirements
Investigate current Kafka producer performance bottlenecks and identify root causes for low throughput.

### Debug Information (MANDATORY - Update this section for every investigation)
- **Error Messages**: No errors, but extremely low throughput: 18.00 msg/sec per partition
- **Expected Performance**: Thousands of messages per second per partition
- **Current Behavior**: All messages going to partition-0, other partitions showing 0.00 msg/sec
- **System State**: Kafka cluster running, messages being produced but inefficiently
- **Evidence**: Observability metrics showing partition-0: 18.00 msg/sec, partitions 1-9: 0.00 msg/sec

### Root Cause Analysis
1. **Partition Distribution Issue**: 
   - Messages created in ComplexLogicStressTestService.ProduceMessagesAsync without setting PartitionNumber
   - All messages default to partition 0 instead of being distributed across 10 partitions
   - GetPartitionNumber() method exists but not used during message creation

2. **Sequential Processing Bottleneck**:
   - KafkaProducerService.ProduceMessagesAsync processes messages sequentially in foreach loop
   - Each message waits for previous completion, limiting throughput
   - No parallel processing across partitions

3. **Suboptimal Producer Configuration**:
   - Current batch size: 32768 bytes (may be too small for high throughput)
   - Linger time: 5ms (may be too low for batch efficiency)
   - No specific partitioning strategy configured

### Performance Requirements
- **FIFO Ordering**: Must maintain message order within each partition
- **High Throughput**: Target thousands of messages per second per partition
- **Even Distribution**: Messages should be evenly distributed across all 10 partitions
- **No Data Loss**: All messages must be reliably delivered

## Phase 2: Design
### Architecture Decisions
1. **Partition Assignment Strategy**:
   - Set PartitionNumber during message creation using MessageId % 10
   - Ensure even distribution across all 10 partitions

2. **Parallel Production Approach**:
   - Group messages by partition before sending
   - Produce partitions in parallel while maintaining FIFO within each partition
   - Use Task.WhenAll for parallel execution across partitions

3. **Producer Configuration Optimization**:
   - Increase batch size for better throughput
   - Adjust linger time for optimal batching
   - Configure compression for network efficiency

### Why This Approach
- **Parallel by Partition**: Maintains FIFO ordering within partitions while maximizing throughput
- **Proper Partitioning**: Ensures load is distributed evenly across all Kafka partitions
- **Batch Optimization**: Reduces network overhead and increases overall throughput

### Alternatives Considered
- **Full Parallel**: Would break FIFO ordering within partitions
- **Sequential**: Current approach, too slow
- **Custom Partitioner**: More complex, current hash-based approach sufficient

## Phase 3: TDD/BDD
### Test Specifications
- Verify messages are distributed across all 10 partitions
- Measure throughput per partition (should be > 1000 msg/sec)
- Confirm FIFO ordering within each partition
- Validate no message loss during high-throughput production

## Phase 4: Implementation
### Code Changes
**1. Fixed Message Partition Assignment** (`ComplexLogicStressTestService.cs` lines 108-121):
- Added `PartitionNumber = (i - 1) % 10` to distribute messages evenly across 10 partitions (0-9)
- Ensures all partitions receive equal load instead of all messages going to partition 0

**2. Optimized Kafka Producer Configuration** (`KafkaProducerService.cs` lines 33-48):
- Increased `BatchSize` from 32768 to 65536 bytes for better throughput
- Increased `LingerMs` from 5 to 10ms for improved batching efficiency  
- Added `QueueBufferingMaxMessages = 1000000` for high-throughput buffer
- Added `QueueBufferingMaxKbytes = 1MB` for large message buffer
- Added retry configuration for reliability

**3. Implemented Parallel Production by Partition** (`KafkaProducerService.cs` ProduceMessagesAsync method):
- **Partition Grouping**: Messages grouped by partition number before production
- **Parallel Processing**: Each partition processed in parallel using Task.WhenAll
- **FIFO Preservation**: Messages within each partition processed sequentially to maintain ordering
- **Explicit Partition Assignment**: Using TopicPartition to guarantee partition placement
- **Enhanced Logging**: Per-partition throughput reporting for performance monitoring

### Technical Implementation Details
**Parallel Strategy:**
```csharp
// Group by partition for parallel processing
var messagesByPartition = messages
    .GroupBy(m => m.PartitionNumber)
    .ToDictionary(g => g.Key, g => g.OrderBy(m => m.MessageId).ToList());

// Process partitions in parallel
var partitionTasks = messagesByPartition.Select(async partitionGroup => {
    // Within each partition, maintain FIFO order
    foreach (var message in partitionMessages) {
        // Use explicit partition assignment
        var topicPartition = new TopicPartition(topic, new Partition(partitionNumber));
        await producer.ProduceAsync(topicPartition, kafkaMessage);
    }
});

await Task.WhenAll(partitionTasks);
```

**Performance Optimizations:**
- **Buffer Size**: Increased to 1GB for high-throughput scenarios
- **Batching**: Optimized batch size and linger time for network efficiency
- **Compression**: Snappy compression enabled for network optimization
- **Partition Distribution**: Even load distribution across all 10 partitions

### Challenges Encountered
- **FIFO Ordering**: Required careful design to maintain message order within partitions while parallelizing across partitions
- **Thread Safety**: Used Interlocked operations for thread-safe counters across parallel tasks
- **Explicit Partitioning**: Needed to use TopicPartition instead of relying on message key for partition assignment

### Solutions Applied
- **Grouped Processing**: Messages grouped by partition, then processed in parallel groups
- **Sequential Within Partition**: Maintained FIFO by processing messages sequentially within each partition
- **Explicit Assignment**: Used TopicPartition for guaranteed partition placement

## Phase 5: Testing & Validation
### Test Results
[To be completed during testing]

## Phase 6: Owner Acceptance
### Demonstration
[To be completed during demonstration]

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
[To be documented after completion]
### What Could Be Improved  
[To be documented after completion]
### Key Insights for Similar Tasks
[To be documented after completion]
### Specific Problems to Avoid in Future
[To be documented after completion]
### Reference for Future WIs
[To be documented after completion]