# Day 8: Exactly-Once Semantics and End-to-End Guarantees

## 🗺️ Course Navigation
**[← Day 7: Stress Testing](../Day07-Stress-Testing/)** | **[Course Overview](../README.md)** | **[Next: Day 9 - Performance Optimization & Scaling →](../Day09-Performance-Optimization-Scaling/)**

---

## Overview
Master exactly-once processing semantics and end-to-end delivery guarantees for mission-critical streaming applications.

## Learning Objectives
- Implement exactly-once processing with Flink's checkpointing
- Design idempotent operations and duplicate detection
- Handle exactly-once semantics with external systems
- Build transactional outputs and two-phase commit protocols
- Optimize checkpoint performance for high-throughput scenarios

## Real-World Context
Financial institutions like JPMorgan Chase require exactly-once processing for payment transactions. A single duplicate payment or lost transaction can result in significant financial losses and regulatory violations. Their streaming payment system processes millions of transactions daily with guaranteed exactly-once semantics.

## Technical Deep Dive

### Exactly-Once with Checkpointing
```csharp
// Financial transaction processing with exactly-once guarantees
public class ExactlyOnceTransactionProcessor : KeyedProcessFunction<string, PaymentTransaction, ProcessedPayment>
{
    private ValueState<TransactionIdempotencyKey> processedTransactions;
    private ValueState<decimal> accountBalance;
    
    public override void Open(Configuration parameters)
    {
        // Configure exactly-once state
        var idempotencyDescriptor = new ValueStateDescriptor<TransactionIdempotencyKey>(
            "processed-transactions",
            TypeInformation.Of<TransactionIdempotencyKey>());
        processedTransactions = GetRuntimeContext().GetState(idempotencyDescriptor);
        
        var balanceDescriptor = new ValueStateDescriptor<decimal>(
            "account-balance",
            TypeInformation.Of<decimal>());
        accountBalance = GetRuntimeContext().GetState(balanceDescriptor);
    }
    
    public override void ProcessElement(PaymentTransaction transaction, Context context, ICollector<ProcessedPayment> output)
    {
        var idempotencyKey = new TransactionIdempotencyKey(transaction.Id, transaction.Hash);
        var lastProcessed = processedTransactions.Value();
        
        // Duplicate detection for exactly-once semantics
        if (lastProcessed != null && lastProcessed.Equals(idempotencyKey))
        {
            // Already processed - emit cached result without side effects
            output.Collect(lastProcessed.CachedResult);
            return;
        }
        
        // Process transaction exactly once
        var currentBalance = accountBalance.Value();
        var newBalance = ProcessPayment(transaction, currentBalance);
        
        var result = new ProcessedPayment
        {
            TransactionId = transaction.Id,
            AccountId = transaction.AccountId,
            Amount = transaction.Amount,
            NewBalance = newBalance,
            ProcessedAt = DateTimeOffset.UtcNow,
            CheckpointId = context.GetCheckpointId()
        };
        
        // Update state atomically
        accountBalance.Update(newBalance);
        processedTransactions.Update(new TransactionIdempotencyKey(transaction.Id, transaction.Hash)
        {
            CachedResult = result
        });
        
        output.Collect(result);
    }
}
```

### Two-Phase Commit for External Systems
```csharp
// Database sink with exactly-once guarantees using 2PC
public class ExactlyOnceDatabaseSink : TwoPhaseCommitSinkFunction<ProcessedPayment, DatabaseTransaction, Void>
{
    private readonly string connectionString;
    private DatabaseTransaction currentTransaction;
    
    protected override DatabaseTransaction BeginTransaction()
    {
        var connection = new SqlConnection(connectionString);
        connection.Open();
        var transaction = connection.BeginTransaction();
        
        return new DatabaseTransaction
        {
            Connection = connection,
            Transaction = transaction,
            TransactionId = Guid.NewGuid()
        };
    }
    
    protected override void Invoke(DatabaseTransaction transaction, ProcessedPayment payment, Context context)
    {
        // Write to database within transaction
        var command = new SqlCommand(@"
            INSERT INTO ProcessedPayments (TransactionId, AccountId, Amount, NewBalance, ProcessedAt, CheckpointId)
            VALUES (@TransactionId, @AccountId, @Amount, @NewBalance, @ProcessedAt, @CheckpointId)",
            transaction.Connection, transaction.Transaction);
        
        command.Parameters.Add("@TransactionId", SqlDbType.UniqueIdentifier).Value = payment.TransactionId;
        command.Parameters.Add("@AccountId", SqlDbType.VarChar).Value = payment.AccountId;
        command.Parameters.Add("@Amount", SqlDbType.Decimal).Value = payment.Amount;
        command.Parameters.Add("@NewBalance", SqlDbType.Decimal).Value = payment.NewBalance;
        command.Parameters.Add("@ProcessedAt", SqlDbType.DateTimeOffset).Value = payment.ProcessedAt;
        command.Parameters.Add("@CheckpointId", SqlDbType.BigInt).Value = payment.CheckpointId;
        
        command.ExecuteNonQuery();
    }
    
    protected override void PreCommit(DatabaseTransaction transaction)
    {
        // Phase 1: Prepare to commit
        // Validate transaction integrity
        ValidateTransactionIntegrity(transaction);
        
        // Flush any pending writes
        transaction.Transaction.Save("precommit_savepoint");
    }
    
    protected override void Commit(DatabaseTransaction transaction)
    {
        // Phase 2: Actual commit
        try
        {
            transaction.Transaction.Commit();
            LogCommitSuccess(transaction.TransactionId);
        }
        catch (Exception ex)
        {
            LogCommitFailure(transaction.TransactionId, ex);
            throw;
        }
        finally
        {
            transaction.Connection.Close();
        }
    }
    
    protected override void Abort(DatabaseTransaction transaction)
    {
        try
        {
            transaction.Transaction.Rollback();
            LogAbort(transaction.TransactionId);
        }
        finally
        {
            transaction.Connection.Close();
        }
    }
}
```

### Kafka Exactly-Once Producer
```csharp
// Kafka producer with exactly-once semantics
public class ExactlyOnceKafkaProducer : FlinkKafkaProducer<ProcessedPayment>
{
    public ExactlyOnceKafkaProducer(string topic, KafkaSerializationSchema<ProcessedPayment> schema, Properties properties)
        : base(topic, schema, properties, Semantic.ExactlyOnce)
    {
        // Configure transactional properties
        properties.SetProperty("transaction.timeout.ms", "60000");
        properties.SetProperty("transactional.id.prefix", "flink-payment-processor");
        properties.SetProperty("enable.idempotence", "true");
        properties.SetProperty("acks", "all");
        properties.SetProperty("retries", "3");
        properties.SetProperty("max.in.flight.requests.per.connection", "1");
    }
    
    protected override void Invoke(ProcessedPayment payment, Context context)
    {
        // Produce message with transaction coordinates
        var record = new ProducerRecord<byte[], byte[]>(
            GetTargetTopic(),
            SerializeKey(payment),
            SerializeValue(payment));
        
        // Add headers for traceability
        record.Headers.Add("checkpoint-id", BitConverter.GetBytes(context.GetCheckpointId()));
        record.Headers.Add("processing-time", BitConverter.GetBytes(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
        record.Headers.Add("transaction-id", Encoding.UTF8.GetBytes(payment.TransactionId.ToString()));
        
        GetProducer().Send(record);
    }
}
```

## Hands-On Exercises

### Exercise 9.1: Banking Transaction System
**📁 [Complete Solution Available →](Exercise-Solutions/)**

Build an exactly-once payment processing system that:
- Handles duplicate transaction detection using idempotency keys
- Maintains account balances with exactly-once updates
- Integrates with external banking APIs using two-phase commit
- Provides audit trails for regulatory compliance

**💡 Implementation Focus**: Financial transaction processing with exactly-once guarantees
**🔧 [Working Code & Instructions →](Exercise-Solutions/Exercise81/)**

### Exercise 9.2: E-commerce Order Processing
**📁 [Complete Solution Available →](Exercise-Solutions/)**

Create an order fulfillment system with:
- Exactly-once inventory updates across distributed systems
- Payment processing with rollback capabilities
- Order status tracking across multiple systems
- Integration with shipping and notification services

**💡 Implementation Focus**: Distributed transaction management with exactly-once semantics
**🔧 [Working Code & Instructions →](Exercise-Solutions/Exercise82/)**

### Exercise 9.3: Real-time Analytics with Exactly-Once
**📁 [Complete Solution Available →](Exercise-Solutions/)**

Implement analytics aggregations that:
- Count unique events exactly once using state deduplication
- Calculate financial metrics without double-counting
- Handle late-arriving data corrections while maintaining consistency
- Maintain consistency across multiple time windows

**💡 Implementation Focus**: Exactly-once semantics in real-time analytics scenarios
**🔧 [Working Code & Instructions →](Exercise-Solutions/Exercise83/)**

### Exercise 9.4: Advanced Exactly-Once Patterns
**📁 [Complete Solution Available →](Exercise-Solutions/)**

Master advanced exactly-once techniques:
- High-performance checkpoint optimization for enterprise scale
- Complex external system integration patterns  
- Advanced recovery strategies and monitoring
- Production debugging and troubleshooting

**💡 Implementation Focus**: Enterprise-scale exactly-once optimization techniques
**🔧 [Working Code & Instructions →](Exercise-Solutions/Exercise84/)**

---

## 🚀 Get Started with Exercises

All exercises include complete working solutions, detailed documentation, and step-by-step instructions:

**[→ Start with Exercise Solutions](Exercise-Solutions/README.md)**

## Checkpoint Optimization

### High-Performance Checkpointing
```csharp
// Optimized checkpoint configuration for exactly-once
public static void ConfigureExactlyOnceCheckpointing(StreamExecutionEnvironment env)
{
    // Enable exactly-once checkpointing
    env.EnableCheckpointing(TimeSpan.FromSeconds(30), CheckpointingMode.ExactlyOnce);
    
    // Optimize checkpoint performance
    var checkpointConfig = env.GetCheckpointConfig();
    checkpointConfig.SetMinPauseBetweenCheckpoints(TimeSpan.FromSeconds(10));
    checkpointConfig.SetCheckpointTimeout(TimeSpan.FromMinutes(5));
    checkpointConfig.SetMaxConcurrentCheckpoints(1);
    checkpointConfig.EnableExternalizedCheckpoints(ExternalizedCheckpointCleanup.RetainOnCancellation);
    
    // Configure state backend for exactly-once
    env.SetStateBackend(new RocksDBStateBackend("hdfs://namenode:port/checkpoints", true));
    
    // Restart strategy for fault tolerance
    env.SetRestartStrategy(RestartStrategies.FixedDelayRestart(3, TimeSpan.FromSeconds(10)));
}
```

### Incremental Checkpointing
```csharp
// RocksDB incremental checkpointing for large state
var rocksDBConfig = new RocksDBStateBackendConfig()
{
    EnableIncrementalCheckpointing = true,
    NumberOfTransferThreads = 8,
    FilesToTransferThreshold = 5,
    IncrementalCheckpointEnabled = true
};

var incrementalBackend = new RocksDBStateBackend(rocksDBConfig);
env.SetStateBackend(incrementalBackend);
```

## Idempotency Patterns

### Idempotent Operations Design
```csharp
// Idempotent aggregation with exactly-once semantics
public class IdempotentAggregator : KeyedProcessFunction<string, Event, AggregationResult>
{
    private ValueState<AggregationState> aggregationState;
    private SetState<string> processedEventIds;
    
    public override void ProcessElement(Event event, Context context, ICollector<AggregationResult> output)
    {
        // Check if event already processed
        if (processedEventIds.Contains(event.Id))
        {
            // Already processed - skip
            return;
        }
        
        // Process event idempotently
        var currentAggregation = aggregationState.Value() ?? new AggregationState();
        var updatedAggregation = ApplyEventIdempotently(currentAggregation, event);
        
        // Update state atomically
        aggregationState.Update(updatedAggregation);
        processedEventIds.Add(event.Id);
        
        output.Collect(new AggregationResult
        {
            Key = event.Key,
            Count = updatedAggregation.Count,
            Sum = updatedAggregation.Sum,
            LastUpdated = context.Timestamp()
        });
    }
}
```

## Testing Exactly-Once Semantics

### Failure Injection Testing
```csharp
[Test]
public void TestExactlyOnceWithFailures()
{
    var testHarness = new KeyedOneInputStreamOperatorTestHarness<string, PaymentTransaction, ProcessedPayment>(
        new ExactlyOnceTransactionProcessor(),
        transaction => transaction.AccountId);
    
    testHarness.Open();
    
    // Process transaction
    var transaction = new PaymentTransaction("tx1", "account1", 100.0m);
    testHarness.ProcessElement(transaction, 1000L);
    
    // Simulate checkpoint
    testHarness.Snapshot(1, 1000L);
    
    // Simulate failure and recovery
    testHarness.Close();
    testHarness = new KeyedOneInputStreamOperatorTestHarness<string, PaymentTransaction, ProcessedPayment>(
        new ExactlyOnceTransactionProcessor(),
        transaction => transaction.AccountId);
    
    // Restore from checkpoint
    testHarness.InitializeState(snapshot);
    testHarness.Open();
    
    // Replay same transaction
    testHarness.ProcessElement(transaction, 1000L);
    
    // Verify exactly-once processing
    var results = testHarness.ExtractOutputValues();
    Assert.AreEqual(1, results.Count); // Should not duplicate
}
```

## Monitoring and Observability

### Exactly-Once Metrics
```csharp
// Metrics for exactly-once processing
public class ExactlyOnceMetrics
{
    private readonly Counter duplicateEvents;
    private readonly Counter processedEvents;
    private readonly Histogram checkpointDuration;
    private readonly Gauge stateSize;
    
    public void RecordDuplicateEvent(string eventId)
    {
        duplicateEvents.Labels("duplicate_detected").Inc();
        Logger.LogWarning("Duplicate event detected: {EventId}", eventId);
    }
    
    public void RecordProcessedEvent(string eventId, long stateSize)
    {
        processedEvents.Labels("processed").Inc();
        this.stateSize.Set(stateSize);
    }
    
    public void RecordCheckpoint(TimeSpan duration, bool success)
    {
        checkpointDuration.Observe(duration.TotalMilliseconds);
        if (!success)
        {
            Logger.LogError("Checkpoint failed after {Duration}ms", duration.TotalMilliseconds);
        }
    }
}
```

## Architecture Integration
- Configure checkpoint storage with high availability
- Set up monitoring for exactly-once guarantees
- Integrate with external systems using 2PC
- Implement circuit breakers for external dependencies

## Performance Considerations
- Optimize checkpoint intervals for throughput vs latency
- Use incremental checkpointing for large state
- Configure appropriate parallelism for exactly-once operations
- Monitor backpressure and checkpoint alignment

## References
- [Apache Flink Exactly-Once Documentation](https://nightlies.apache.org/flink/flink-docs-release-1.18/docs/learn-flink/fault_tolerance/)
- [Confluent: Exactly-Once Semantics in Kafka](https://www.confluent.io/blog/exactly-once-semantics-are-possible-heres-how-apache-kafka-does-it/)
- [Google Cloud: Dataflow Exactly-Once Processing](https://cloud.google.com/dataflow/docs/concepts/exactly-once)
- [Two-Phase Commit Protocol (Princeton CS)](https://www.cs.princeton.edu/courses/archive/fall16/cos418/docs/L6-2pc.pdf)

## Next Steps
Day 9 focuses on performance optimization and scaling patterns for high-throughput streaming applications.
---

## 🗺️ Course Navigation
**[← Day 7: Stress Testing](../Day07-Stress-Testing/)** | **[Course Overview](../README.md)** | **[Next: Day 9 - Performance Optimization & Scaling →](../Day09-Performance-Optimization-Scaling/)**

**Course Progress**: Day 8 of 14 Complete ✅

## Running Exercises Manually

The exercises can be run manually outside of the integration tests. This requires starting the infrastructure and setting environment variables that are normally discovered automatically by the test framework.

### Step 1: Start Infrastructure

From the repository root, start the LocalTesting infrastructure in LearningCourse mode:

```bash
# Linux/macOS
cd LocalTesting
./run-learningcourse.sh

# Windows (PowerShell)
cd LocalTesting
$env:LEARNINGCOURSE="true"
dotnet run --project LocalTesting.FlinkSqlAppHost --configuration Release
```

This starts:
- Apache Flink cluster (JobManager + TaskManager + SQL Gateway)
- Apache Kafka with JMX metrics
- FlinkDotNet Gateway (port 8086)
- Temporal workflow server (optional, for Day06+)
- Redis (for state management)
- Prometheus (metrics collection)
- Grafana (metrics visualization)

Wait approximately 60 seconds for all containers to be ready.

### Step 2: Discover Service Endpoints

The infrastructure uses dynamic port allocation. You need to discover the actual ports assigned:

1. **Open Aspire Dashboard**: The AppHost will display a URL like `http://localhost:15000`
2. **Find Kafka Port**: Look for "kafka" service, note the host port (e.g., `localhost:32785`)
3. **Find Flink JobManager Port**: Look for "flink-jobmanager-jm-http" service, note the port (e.g., `localhost:32787`)

### Step 3: Set Environment Variables

Before running an exercise, set these environment variables:

```bash
# Linux/macOS
export KAFKA_BOOTSTRAP_SERVERS="localhost:XXXXX"  # Replace XXXXX with discovered Kafka host port
export KAFKA_FLINK_BOOTSTRAP_SERVERS="kafka:9093"  # Fixed container-to-container address
export FLINK_JOB_GATEWAY_URL="http://localhost:8086/"  # Fixed JobGateway port
export FLINK_JOBMANAGER_URL="http://localhost:YYYYY"  # Replace YYYYY with discovered Flink port

# Windows (PowerShell)
$env:KAFKA_BOOTSTRAP_SERVERS="localhost:XXXXX"
$env:KAFKA_FLINK_BOOTSTRAP_SERVERS="kafka:9093"
$env:FLINK_JOB_GATEWAY_URL="http://localhost:8086/"
$env:FLINK_JOBMANAGER_URL="http://localhost:YYYYY"
```

**Optional environment variables** (depending on the exercise):
```bash
# For Day06 Temporal exercises
export TEMPORAL_ENDPOINT="localhost:ZZZZZ"  # Replace with discovered Temporal port

# For exercises using Redis
export REDIS_ENDPOINT="localhost:WWWWW"  # Replace with discovered Redis port
```

### Step 4: Run Exercise

Navigate to the exercise directory and run:

```bash
cd Day01-Kafka-Flink-Data-Pipeline/Exercise-Solutions/Exercise1-StringCapitalize
dotnet run --configuration Release
```

### Environment Variable Reference

| Variable | Purpose | Example Value |
|----------|---------|---------------|
| `KAFKA_BOOTSTRAP_SERVERS` | Kafka address for producer/consumer on host | `localhost:32785` |
| `KAFKA_FLINK_BOOTSTRAP_SERVERS` | Kafka address for Flink jobs (container-to-container) | `kafka:9093` |
| `FLINK_JOB_GATEWAY_URL` | FlinkDotNet Gateway endpoint for job submission | `http://localhost:8086/` |
| `FLINK_JOBMANAGER_URL` | Flink JobManager REST API for health checks | `http://localhost:32787` |
| `TEMPORAL_ENDPOINT` | Temporal server endpoint (Day06+) | `localhost:32789` |
| `REDIS_ENDPOINT` | Redis endpoint for state management | `localhost:32783` |

### Why Dynamic Ports?

The test infrastructure uses .NET Aspire which assigns dynamic ports to avoid conflicts. This is why you need to discover ports from the Aspire Dashboard rather than using hardcoded values.

### Alternative: Use Integration Tests

For automated testing with automatic port discovery, use the integration test framework:

```bash
# Run all Day01 tests
dotnet test LearningCourse/IntegrationTests.sln --filter "FullyQualifiedName~Day01Tests"
```

The integration tests automatically:
- Start the infrastructure
- Discover service endpoints
- Set environment variables
- Run exercises
- Validate results
- Clean up resources

