# Day 7: Complex Logic Stress Testing

## 🗺️ Course Navigation
**[← Day 6: Advanced Windows & Joins](../Day06-Advanced-Windows-Joins/)** | **[Course Overview](../README.md)** | **[Next: Day 8 - Exactly-Once Semantics →](../Day08-Exactly-Once-Semantics/)**

---

## 🚀 Quick Start Instructions

### Prerequisites Setup
1. **LocalTesting Environment**: Ensure full stack running from previous days
2. **Stress Test Framework**: Verify ComplexLogicStressTestController available
3. **Performance Baseline**: Establish baseline metrics before testing

### Access Stress Testing Framework
```bash
# Verify LocalTesting stack is running
curl http://localhost:18000/index.html

# Check available stress test endpoints
curl http://localhost:18000/swagger

# Confirm monitoring stack ready for load testing
curl http://localhost:18010  # Grafana for performance monitoring
curl http://localhost:18006  # Prometheus for metrics collection
```

### Initialize Stress Testing Environment
```bash
# Navigate to stress testing exercises
cd LearningCourse/Day07-Stress-Testing/Exercise-Solutions

# Verify performance monitoring baseline
curl http://localhost:5000/api/ComplexLogicStressTest/baseline-metrics
```

## 📋 Today's Exercises (Completion Order)

### Foundation Stress Testing
- **[Exercise 7.1: Volume Stress Testing (60 min)](#exercise-71-volume-stress-testing)** - Million+ message throughput
- **[Exercise 7.2: Velocity Stress Testing (75 min)](#exercise-72-velocity-stress-testing)** - Burst traffic and latency
- **[Exercise 7.3: Variety Stress Testing (60 min)](#exercise-73-variety-stress-testing)** - Complex data scenarios

### Advanced Reliability Testing
- **[Exercise 7.4: Fault Injection Testing (90 min)](#exercise-74-fault-injection-testing)** - Chaos engineering patterns
- **[Exercise 7.5: Resource Exhaustion Testing (75 min)](#exercise-75-resource-exhaustion-testing)** - Memory and CPU limits
- **[Exercise 7.6: Production Readiness Validation (60 min)](#exercise-76-production-readiness-validation)** - Enterprise validation

**Total Time: 4-5 hours** | **Focus:** Complex logic under extreme conditions

---

## 📝 Exercise Instructions

### Exercise 7.1: Volume Stress Testing (60 minutes)
**Business Context**: High-Throughput Data Processing  
**Objective**: Test system under massive volume scenarios

**Steps:**
1. **Million Message Stress Test** (20 min):
   ```bash
   # Execute high-volume stress test
   curl -X POST http://localhost:5000/api/ComplexLogicStressTest/million-message-stress \
     -H "Content-Type: application/json" \
     -d '{"MessageCount": 1000000, "Concurrency": 100}'
   
   # Monitor performance in real-time
   open http://localhost:18010  # Grafana dashboards
   ```

2. **Large State Operations** (20 min):
   ```bash
   # Test gigabyte-scale stateful processing
   curl -X POST http://localhost:5000/api/ComplexLogicStressTest/large-state-test \
     -d '{"StateSize": "1GB", "Operations": 100000}'
   ```

3. **Memory Pressure Testing** (20 min):
   ```bash
   # Push system to near-heap limits
   curl -X GET http://localhost:5000/api/ComplexLogicStressTest/memory-pressure \
     -H "Accept: application/json"
   
   # Monitor JVM and container memory usage
   curl http://localhost:18006/api/v1/query?query=container_memory_usage_bytes
   ```

**Expected Results**: System handles 1M+ messages/second, maintains stability under memory pressure

### Exercise 7.2: Velocity Stress Testing (75 minutes)
**Business Context**: Real-Time Processing Under Variable Load  
**Objective**: Test burst traffic and latency requirements

**Steps:**
1. **Burst Traffic Simulation** (25 min):
   ```bash
   # Simulate sudden traffic spikes
   curl -X POST http://localhost:5000/api/ComplexLogicStressTest/burst-traffic-test \
     -d '{
       "BurstDuration": "PT30S",
       "BurstMultiplier": 10,
       "BaselineRate": 1000
     }'
   ```

2. **Variable Rate Testing** (25 min):
   ```bash
   # Test constantly changing input rates
   curl -X POST http://localhost:5000/api/ComplexLogicStressTest/variable-rate-test \
     -d '{
       "RatePattern": "sine_wave",
       "MinRate": 100,
       "MaxRate": 10000,
       "Duration": "PT5M"
     }'
   ```

3. **Latency Benchmark** (25 min):
   ```bash
   # Test sub-millisecond processing requirements
   curl -X POST http://localhost:5000/api/ComplexLogicStressTest/latency-benchmark \
     -d '{
       "TargetLatency": "1ms",
       "MessageCount": 100000,
       "MeasurementInterval": "PT10S"
     }'
   
   # Monitor P99 latency metrics
   curl "http://localhost:18006/api/v1/query?query=histogram_quantile(0.99, processing_time_seconds)"
   ```

**Expected Results**: Handles 10x traffic bursts, maintains <1ms P99 latency, adapts to variable rates

### Exercise 7.3: Variety Stress Testing (60 minutes)
**Business Context**: Complex Data Scenario Handling  
**Objective**: Test diverse data types, schemas, and quality issues

**Steps:**
1. **Schema Evolution Testing** (20 min):
   ```bash
   # Test dynamic message structure changes
   curl -X POST http://localhost:5000/api/ComplexLogicStressTest/schema-evolution \
     -d '{
       "SchemaVersions": ["v1", "v2", "v3"],
       "TransitionStrategy": "gradual",
       "MessageCount": 50000
     }'
   ```

2. **Data Quality Stress Test** (20 min):
   ```bash
   # Inject malformed, duplicate, out-of-order data
   curl -X POST http://localhost:5000/api/ComplexLogicStressTest/data-quality-test \
     -d '{
       "MalformedPercent": 5,
       "DuplicatePercent": 3,
       "OutOfOrderPercent": 10,
       "MessageCount": 100000
     }'
   ```

3. **Complex Transformation Load** (20 min):
   ```bash
   # Test CPU-intensive operations
   curl -X POST http://localhost:5000/api/ComplexLogicStressTest/complex-transformations \
     -d '{
       "TransformationType": "heavy_computation",
       "ComplexityLevel": 8,
       "MessageCount": 10000
     }'
   
   # Monitor CPU utilization
   curl "http://localhost:18006/api/v1/query?query=rate(cpu_usage_seconds[5m])"
   ```

**Expected Results**: Handles schema changes gracefully, processes malformed data, maintains performance under complex transformations

### Exercise 7.4: Fault Injection Testing (90 minutes)
**Business Context**: Chaos Engineering for Resilience  
**Objective**: Test system behavior under various failure scenarios

**Steps:**
1. **Network Fault Injection** (30 min):
   ```bash
   # Simulate network partitions and delays
   curl -X POST http://localhost:5000/api/ComplexLogicStressTest/network-faults \
     -d '{
       "FaultType": "partition",
       "Duration": "PT2M",
       "AffectedServices": ["kafka", "flink"]
     }'
   
   # Monitor service recovery
   curl http://localhost:18006/api/v1/query?query=up{job="flink-cluster"}
   ```

2. **Service Failure Simulation** (30 min):
   ```bash
   # Kill and restart critical services
   curl -X POST http://localhost:5000/api/ComplexLogicStressTest/service-failures \
     -d '{
       "FailurePattern": "random_kills",
       "ServiceTypes": ["taskmanager", "jobmanager"],
       "RecoveryTime": "PT30S"
     }'
   ```

3. **Data Corruption Testing** (30 min):
   ```bash
   # Test checkpoint corruption and recovery
   curl -X POST http://localhost:5000/api/ComplexLogicStressTest/checkpoint-corruption \
     -d '{
       "CorruptionType": "state_backend",
       "RecoveryStrategy": "latest_checkpoint"
     }'
   
   # Verify successful recovery
   curl http://localhost:18002/v1/jobs | jq '.jobs[] | select(.status=="RUNNING")'
   ```

**Expected Results**: System recovers from failures automatically, maintains data consistency, demonstrates fault tolerance

### Exercise 7.5: Resource Exhaustion Testing (75 minutes)
**Business Context**: Resource Limit Validation  
**Objective**: Test behavior when approaching resource limits

**Steps:**
1. **Memory Exhaustion** (25 min):
   ```bash
   # Gradually increase memory usage to limits
   curl -X POST http://localhost:5000/api/ComplexLogicStressTest/memory-exhaustion \
     -d '{
       "MemoryTarget": "90%",
       "GrowthRate": "linear",
       "Duration": "PT5M"
     }'
   
   # Monitor OOM behavior and recovery
   curl "http://localhost:18006/api/v1/query?query=container_memory_usage_bytes / container_spec_memory_limit_bytes"
   ```

2. **CPU Saturation** (25 min):
   ```bash
   # Saturate CPU resources
   curl -X POST http://localhost:5000/api/ComplexLogicStressTest/cpu-saturation \
     -d '{
       "CPUTarget": "95%",
       "WorkloadType": "compute_intensive",
       "Duration": "PT3M"
     }'
   ```

3. **Disk I/O Limits** (25 min):
   ```bash
   # Test disk throughput limits
   curl -X POST http://localhost:5000/api/ComplexLogicStressTest/disk-io-limits \
     -d '{
       "IOPattern": "sequential_write",
       "ThroughputTarget": "max",
       "Duration": "PT2M"
     }'
   
   # Monitor disk performance
   curl "http://localhost:18006/api/v1/query?query=rate(disk_io_time_seconds[5m])"
   ```

**Expected Results**: Graceful degradation under resource pressure, no system crashes, proper resource management

### Exercise 7.6: Production Readiness Validation (60 minutes)
**Business Context**: Enterprise Deployment Validation  
**Objective**: Comprehensive production readiness assessment

**Steps:**
1. **End-to-End Integration Test** (20 min):
   ```bash
   # Run comprehensive production scenario
   curl -X POST http://localhost:5000/api/ComplexLogicStressTest/production-scenario \
     -d '{
       "ScenarioType": "e_commerce_black_friday",
       "Duration": "PT10M",
       "LoadProfile": "realistic"
     }'
   ```

2. **SLA Compliance Testing** (20 min):
   ```bash
   # Validate SLA requirements
   curl -X POST http://localhost:5000/api/ComplexLogicStressTest/sla-validation \
     -d '{
       "AvailabilityTarget": 99.9,
       "LatencyTarget": "100ms",
       "ThroughputTarget": 10000,
       "Duration": "PT5M"
     }'
   
   # Check SLA compliance metrics
   curl "http://localhost:18006/api/v1/query?query=sla_compliance_percent"
   ```

3. **Rolling Deployment Test** (20 min):
   ```bash
   # Test zero-downtime deployment
   curl -X POST http://localhost:5000/api/ComplexLogicStressTest/rolling-deployment \
     -d '{
       "DeploymentStrategy": "blue_green",
       "TrafficSplitPercent": 10,
       "RollbackTrigger": "error_rate > 1%"
     }'
   ```

**Expected Results**: Meets all SLA requirements, successful zero-downtime deployments, enterprise-ready validation

---

## 🎯 Learning Objectives

- Master stress testing methodologies for Flink 2.1.0 applications
- Implement complex logic testing patterns
- Build comprehensive performance benchmarking systems
- Design reliability testing frameworks
- Optimize stream processing under extreme loads
- Create production readiness validation suites

## 📋 Today's Schedule (4-5 hours)

### Morning Session (2-2.5 hours)
- **9:00-9:30**: Stress Testing Theory & Methodologies
- **9:30-10:30**: LocalTesting Stress Test Framework Exploration
- **10:30-10:45**: Break
- **10:45-11:30**: Complex Logic Testing Implementation

### Afternoon Session (2-2.5 hours)
- **1:00-2:00**: Performance Benchmarking & Optimization
- **2:00-2:45**: Reliability Testing & Fault Injection
- **2:45-3:00**: Break
- **3:00-3:30**: Production Readiness Validation

## 🚀 Stress Testing in Flink 2.1.0

Stress testing validates that your streaming applications can handle production workloads, recover from failures, and maintain performance under extreme conditions.

### 🔥 Why Complex Logic Stress Testing?

```
Development Testing:
- Small datasets (1K records)
- Simple scenarios
- Controlled environment
- Limited edge cases

Production Reality:
- Massive datasets (1M+ records/sec)
- Complex business logic
- Network failures, resource constraints
- Unpredictable data patterns
```

### 🎯 Stress Testing Dimensions

#### 1. **Volume Testing**
- **High Throughput**: Million+ events per second
- **Large State**: Gigabyte-scale stateful operations
- **Memory Pressure**: Near-heap-limit processing
- **Batch Size Extremes**: Micro-batches to mega-batches

#### 2. **Velocity Testing**
- **Burst Traffic**: Sudden traffic spikes
- **Variable Rates**: Constantly changing input rates
- **Latency Requirements**: Sub-millisecond processing
- **Time Window Stress**: Complex temporal operations

#### 3. **Variety Testing**
- **Schema Evolution**: Dynamic message structures
- **Data Quality Issues**: Malformed, duplicate, out-of-order data
- **Multi-Source Integration**: Diverse data sources
- **Complex Transformations**: CPU-intensive operations

#### 4. **Reliability Testing**
- **Fault Injection**: Simulated failures
- **Recovery Testing**: Checkpoint restoration
- **Network Partitions**: Split-brain scenarios
- **Resource Exhaustion**: Memory, CPU, disk limits

## 🛠️ LocalTesting Stress Test Framework

The LocalTesting environment provides sophisticated stress testing capabilities via the ComplexLogicStressTestController.

### Available Stress Tests

Let's explore the comprehensive testing framework:

```csharp
// Access via: http://localhost:18000/index.html
// ComplexLogicStressTest endpoints:

// GET /api/ComplexLogicStressTest/million-message-stress
// POST /api/ComplexLogicStressTest/burst-traffic-test
// GET /api/ComplexLogicStressTest/backpressure-stress
// POST /api/ComplexLogicStressTest/fault-injection
// GET /api/ComplexLogicStressTest/memory-pressure
// POST /api/ComplexLogicStressTest/latency-benchmark
```

### Stress Test Categories

#### 1. **Throughput Stress Tests**
```json
{
  "test_name": "million_message_stress",
  "description": "Process 1M messages with complex transformations",
  "parameters": {
    "message_count": 1000000,
    "parallelism": 8,
    "complexity_level": "HIGH",
    "include_stateful_operations": true
  },
  "expected_metrics": {
    "max_latency_ms": 100,
    "min_throughput_per_sec": 10000,
    "memory_usage_mb": 2048,
    "cpu_utilization_percent": 80
  }
}
```

#### 2. **Burst Traffic Tests**
```json
{
  "test_name": "burst_traffic_test",
  "description": "Handle sudden traffic spikes",
  "parameters": {
    "baseline_rate": 1000,
    "burst_rate": 50000,
    "burst_duration_sec": 30,
    "burst_frequency_sec": 300
  }
}
```

#### 3. **Fault Injection Tests**
```json
{
  "test_name": "fault_injection",
  "description": "Test resilience to various failures",
  "failure_scenarios": [
    "NETWORK_PARTITION",
    "MEMORY_EXHAUSTION", 
    "TASKMANAGER_FAILURE",
    "CHECKPOINT_CORRUPTION",
    "KAFKA_BROKER_DOWN"
  ]
}
```

## 🚀 Building Complex Stress Tests

Let's implement comprehensive stress testing for Flink 2.1.0 applications:

### Exercise 7.1: Million Message Stress Test

Create `Day07_ComplexStressTest.cs`:

```csharp
using FlinkDotNet.DataStream;
using FlinkDotNet.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using System.Collections.Concurrent;

namespace LearningCourse.Day07
{
    /// <summary>
    /// Complex Logic Stress Testing with Flink 2.1.0
    /// Validates performance, reliability, and scalability under extreme loads
    /// </summary>
    public class ComplexStressTestDemo
    {
        public class StressTestEvent
        {
            public string EventId { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
            public string EventType { get; set; } = string.Empty;
            public int Priority { get; set; }
            public long SequenceNumber { get; set; }
            public ComplexPayload Payload { get; set; } = new();
            public Dictionary<string, object> Metadata { get; set; } = new();

            public override string ToString()
            {
                return $"[{Timestamp:HH:mm:ss.fff}] {EventType}#{SequenceNumber} " +
                       $"(P{Priority}) - {Payload.DataSize} bytes";
            }
        }

        public class ComplexPayload
        {
            public string UserId { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public string Currency { get; set; } = string.Empty;
            public List<TransactionItem> Items { get; set; } = new();
            public Dictionary<string, object> CustomFields { get; set; } = new();
            public byte[] BinaryData { get; set; } = Array.Empty<byte>();
            
            public int DataSize => 
                (UserId?.Length ?? 0) + 
                sizeof(decimal) + 
                (Currency?.Length ?? 0) +
                Items.Count * 100 + // Approximate item size
                BinaryData.Length;
        }

        public class TransactionItem
        {
            public string ProductId { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public string Category { get; set; } = string.Empty;
        }

        public class StressTestMetrics
        {
            public DateTime Timestamp { get; set; }
            public long TotalProcessed { get; set; }
            public long TotalFailed { get; set; }
            public double ThroughputPerSecond { get; set; }
            public double AverageLatencyMs { get; set; }
            public double P95LatencyMs { get; set; }
            public double P99LatencyMs { get; set; }
            public long MemoryUsageMB { get; set; }
            public double CpuUtilizationPercent { get; set; }
            public int ActiveThreads { get; set; }
            public string BottleneckComponent { get; set; } = string.Empty;

            public override string ToString()
            {
                return $"📊 STRESS METRICS: {ThroughputPerSecond:F1}/s, " +
                       $"Latency Avg:{AverageLatencyMs:F1}ms P95:{P95LatencyMs:F1}ms P99:{P99LatencyMs:F1}ms, " +
                       $"Memory:{MemoryUsageMB}MB, CPU:{CpuUtilizationPercent:F1}%, " +
                       $"Processed:{TotalProcessed}, Failed:{TotalFailed}";
            }
        }

        public static async Task Main(string[] args)
        {
            Console.WriteLine("🔥 Complex Logic Stress Testing with Flink 2.1.0");
            Console.WriteLine("================================================");

            // Step 1: Configure high-performance environment
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            ConfigureHighPerformanceEnvironment(env);

            Console.WriteLine("✅ High-performance environment configured");

            // Step 2: Generate massive test dataset
            var testDataStream = GenerateMassiveTestDataset(env);
            Console.WriteLine("✅ Massive test dataset generated");

            // Step 3: Apply complex processing pipeline
            await ApplyComplexProcessingPipeline(testDataStream);
            Console.WriteLine("✅ Complex processing pipeline configured");

            // Step 4: Execute stress test with monitoring
            Console.WriteLine("\n🎯 Starting complex logic stress test...");
            Console.WriteLine("📊 Monitor stress metrics at: http://localhost:18001/stress-monitor");
            Console.WriteLine("📈 Performance dashboard: http://localhost:18010/stress");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                await env.Execute("Complex Logic Stress Test");
                stopwatch.Stop();
                Console.WriteLine($"\n✅ Stress test completed in {stopwatch.Elapsed.TotalSeconds:F1}s!");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Console.WriteLine($"\n❌ Stress test failed after {stopwatch.Elapsed.TotalSeconds:F1}s: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Configure environment for maximum performance under stress
        /// </summary>
        private static void ConfigureHighPerformanceEnvironment(StreamExecutionEnvironment env)
        {
            // High parallelism for stress testing
            env.SetParallelism(Environment.ProcessorCount * 2);
            
            // Aggressive checkpointing for reliability
            env.EnableCheckpointing(TimeSpan.FromSeconds(10));
            env.SetBufferTimeout(TimeSpan.FromMilliseconds(50)); // Low latency

            var config = env.GetConfig();
            config.SetGlobalJobParameters(new Configuration
            {
                // Performance optimizations
                ["execution.buffer-timeout"] = "50ms",
                ["execution.checkpointing.mode"] = "EXACTLY_ONCE",
                ["execution.checkpointing.timeout"] = "30s",
                ["execution.checkpointing.max-concurrent-checkpoints"] = "3",
                
                // Memory management
                ["taskmanager.memory.process.size"] = "8gb",
                ["taskmanager.memory.managed.fraction"] = "0.7",
                ["taskmanager.memory.network.fraction"] = "0.2",
                
                // Network optimization
                ["taskmanager.network.numberOfBuffers"] = "16384",
                ["taskmanager.network.buffers-per-channel"] = "32",
                ["taskmanager.network.extra-buffers-per-gate"] = "32",
                
                // Parallelism and threading
                ["parallelism.default"] = Environment.ProcessorCount.ToString(),
                ["slot.sharing.group"] = "stress-test-group",
                ["pipeline.max-parallelism"] = "512",
                
                // JVM optimizations
                ["env.java.opts.taskmanager"] = "-XX:+UseG1GC -XX:MaxGCPauseMillis=20 -XX:+UseStringDeduplication"
            });
        }

        /// <summary>
        /// Generate massive dataset for stress testing (1M+ events)
        /// </summary>
        private static DataStream<StressTestEvent> GenerateMassiveTestDataset(StreamExecutionEnvironment env)
        {
            const int TOTAL_EVENTS = 1_000_000;
            var random = new Random(42); // Seed for reproducibility
            var events = new List<StressTestEvent>();

            Console.WriteLine($"🔄 Generating {TOTAL_EVENTS:N0} stress test events...");

            // Generate diverse event patterns
            for (long i = 0; i < TOTAL_EVENTS; i++)
            {
                var eventType = GenerateEventType(random);
                var priority = GeneratePriority(eventType, random);
                var complexity = GenerateComplexPayload(i, random);

                events.Add(new StressTestEvent
                {
                    EventId = $"STRESS_{i:D8}",
                    Timestamp = DateTime.Now.AddMilliseconds(i * random.Next(1, 10)),
                    EventType = eventType,
                    Priority = priority,
                    SequenceNumber = i,
                    Payload = complexity,
                    Metadata = GenerateMetadata(i, random)
                });

                // Progress reporting
                if (i % 100000 == 0)
                {
                    Console.WriteLine($"📝 Generated {i:N0} events...");
                }
            }

            Console.WriteLine($"✅ Generated {events.Count:N0} stress test events");

            return env.FromElements(events.ToArray())
                .Name("Massive Stress Test Source")
                .SetParallelism(8); // High parallelism for data generation
        }

        /// <summary>
        /// Apply complex processing logic to stress test the system
        /// </summary>
        private static async Task ApplyComplexProcessingPipeline(DataStream<StressTestEvent> eventStream)
        {
            // Stage 1: CPU-intensive transformations
            var enrichedStream = eventStream
                .Map(new CPUIntensiveEnrichmentFunction())
                .Name("CPU Intensive Enrichment")
                .SetParallelism(16);

            // Stage 2: Memory-intensive stateful processing
            var statefulStream = enrichedStream
                .KeyBy(evt => evt.Payload.UserId)
                .Transform(new MemoryIntensiveStatefulFunction())
                .Name("Memory Intensive Stateful Processing")
                .SetParallelism(8);

            // Stage 3: I/O intensive operations (simulated)
            var ioIntensiveStream = statefulStream
                .Transform(new IOIntensiveFunction())
                .Name("I/O Intensive Operations")
                .SetParallelism(4);

            // Stage 4: Complex aggregations and windowing
            var aggregatedStream = ioIntensiveStream
                .KeyBy(evt => evt.EventType)
                .TimeWindow(TimeSpan.FromSeconds(10))
                .Aggregate(new ComplexAggregationFunction())
                .Name("Complex Time Window Aggregation")
                .SetParallelism(4);

            // Stage 5: Business logic with multiple joins
            var businessLogicStream = ioIntensiveStream
                .Transform(new ComplexBusinessLogicFunction())
                .Name("Complex Business Logic")
                .SetParallelism(6);

            // Stage 6: Real-time analytics and ML inference
            var analyticsStream = businessLogicStream
                .Transform(new RealTimeAnalyticsFunction())
                .Name("Real-time Analytics & ML")
                .SetParallelism(4);

            // Stage 7: Fraud detection with complex rules
            var fraudDetectionStream = analyticsStream
                .Filter(evt => evt.Payload.Amount > 1000)
                .Transform(new ComplexFraudDetectionFunction())
                .Name("Complex Fraud Detection")
                .SetParallelism(2);

            // Stage 8: Performance monitoring and metrics
            var metricsStream = eventStream
                .Transform(new StressTestMetricsFunction())
                .Name("Stress Test Metrics Collection");

            // Output streams with different priorities
            fraudDetectionStream.Print("🚨 FRAUD ALERTS");
            aggregatedStream.Print("📊 AGGREGATIONS");
            metricsStream.Print("📈 STRESS METRICS");

            // Side outputs for failed events
            var failedEventsStream = businessLogicStream
                .Filter(evt => evt.Metadata.ContainsKey("processing_failed"))
                .Map(evt => $"❌ FAILED: {evt.EventId} - {evt.Metadata["failure_reason"]}")
                .Name("Failed Events Monitor");

            failedEventsStream.Print("⚠️ FAILURES");

            await Task.CompletedTask;
        }

        /// <summary>
        /// CPU-intensive transformation that stresses processing power
        /// </summary>
        public class CPUIntensiveEnrichmentFunction : MapFunction<StressTestEvent, StressTestEvent>
        {
            private readonly Random random = new();

            public override StressTestEvent Map(StressTestEvent evt)
            {
                var startTime = DateTime.Now;

                // CPU-intensive operations
                PerformComplexCalculations(evt);
                PerformStringManipulations(evt);
                PerformCryptographicOperations(evt);

                var processingTime = DateTime.Now - startTime;
                evt.Metadata["cpu_processing_time_ms"] = processingTime.TotalMilliseconds;
                evt.Metadata["enrichment_timestamp"] = DateTime.Now;

                return evt;
            }

            private void PerformComplexCalculations(StressTestEvent evt)
            {
                // Simulate complex mathematical operations
                double result = 0;
                for (int i = 0; i < 1000; i++)
                {
                    result += Math.Sin(i) * Math.Cos(i) * Math.Sqrt(i + 1);
                    result += Math.Pow(i % 10, 2) * Math.Log(i + 1);
                }
                evt.Metadata["calculation_result"] = result;
            }

            private void PerformStringManipulations(StressTestEvent evt)
            {
                // String processing stress test
                var data = evt.EventId + evt.EventType + evt.Payload.UserId;
                for (int i = 0; i < 100; i++)
                {
                    data = data.ToUpper().ToLower().Replace("STRESS", "TEST");
                    data += random.Next(1000, 9999).ToString();
                }
                evt.Metadata["processed_string_length"] = data.Length;
            }

            private void PerformCryptographicOperations(StressTestEvent evt)
            {
                // Simulate cryptographic hash calculations
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var input = System.Text.Encoding.UTF8.GetBytes(evt.EventId + evt.Timestamp.Ticks);
                
                for (int i = 0; i < 10; i++)
                {
                    var hash = sha256.ComputeHash(input);
                    input = hash; // Chain hashes for more CPU work
                }
                
                evt.Metadata["crypto_hash"] = Convert.ToBase64String(input)[..16];
            }
        }

        /// <summary>
        /// Memory-intensive stateful function that maintains large state
        /// </summary>
        public class MemoryIntensiveStatefulFunction : TransformFunction<StressTestEvent, StressTestEvent>
        {
            private readonly ConcurrentDictionary<string, UserState> userStates = new();
            private readonly ConcurrentDictionary<string, List<StressTestEvent>> eventHistory = new();

            public override Task<StressTestEvent> Transform(StressTestEvent evt)
            {
                var userId = evt.Payload.UserId;

                // Update user state (memory intensive)
                userStates.AddOrUpdate(userId, 
                    _ => new UserState 
                    { 
                        UserId = userId, 
                        EventCount = 1,
                        TotalAmount = evt.Payload.Amount,
                        FirstSeen = evt.Timestamp,
                        LastSeen = evt.Timestamp
                    },
                    (_, existing) => 
                    {
                        existing.EventCount++;
                        existing.TotalAmount += evt.Payload.Amount;
                        existing.LastSeen = evt.Timestamp;
                        return existing;
                    });

                // Maintain event history (memory stress)
                eventHistory.AddOrUpdate(userId,
                    _ => new List<StressTestEvent> { evt },
                    (_, existing) =>
                    {
                        existing.Add(evt);
                        // Keep only last 1000 events per user to prevent memory explosion
                        if (existing.Count > 1000)
                        {
                            existing.RemoveRange(0, 500);
                        }
                        return existing;
                    });

                // Add state information to event
                var state = userStates[userId];
                evt.Metadata["user_event_count"] = state.EventCount;
                evt.Metadata["user_total_amount"] = state.TotalAmount;
                evt.Metadata["user_session_duration_ms"] = (state.LastSeen - state.FirstSeen).TotalMilliseconds;
                evt.Metadata["state_memory_footprint"] = CalculateMemoryFootprint(userId);

                return Task.FromResult(evt);
            }

            private long CalculateMemoryFootprint(string userId)
            {
                // Estimate memory usage for this user
                var stateSize = userStates.ContainsKey(userId) ? 200 : 0; // Approximate UserState size
                var historySize = eventHistory.ContainsKey(userId) ? eventHistory[userId].Count * 500 : 0; // Approximate event size
                return stateSize + historySize;
            }

            private class UserState
            {
                public string UserId { get; set; } = string.Empty;
                public long EventCount { get; set; }
                public decimal TotalAmount { get; set; }
                public DateTime FirstSeen { get; set; }
                public DateTime LastSeen { get; set; }
            }
        }

        /// <summary>
        /// I/O intensive function simulating external system interactions
        /// </summary>
        public class IOIntensiveFunction : TransformFunction<StressTestEvent, StressTestEvent>
        {
            private readonly Random random = new();

            public override async Task<StressTestEvent> Transform(StressTestEvent evt)
            {
                var startTime = DateTime.Now;

                // Simulate various I/O operations
                await SimulateDatabaseLookup(evt);
                await SimulateExternalAPICall(evt);
                await SimulateFileSystemOperation(evt);

                var processingTime = DateTime.Now - startTime;
                evt.Metadata["io_processing_time_ms"] = processingTime.TotalMilliseconds;

                return evt;
            }

            private async Task SimulateDatabaseLookup(StressTestEvent evt)
            {
                // Simulate database query latency
                var latency = random.Next(1, 20);
                await Task.Delay(latency);
                
                evt.Metadata["db_lookup_latency_ms"] = latency;
                evt.Metadata["db_result_count"] = random.Next(0, 100);
            }

            private async Task SimulateExternalAPICall(StressTestEvent evt)
            {
                // Simulate external API call with variable latency
                var latency = random.Next(5, 50);
                await Task.Delay(latency);
                
                // Simulate occasional API failures
                var isSuccess = random.NextDouble() > 0.05; // 5% failure rate
                
                evt.Metadata["api_call_latency_ms"] = latency;
                evt.Metadata["api_call_success"] = isSuccess;
                
                if (!isSuccess)
                {
                    evt.Metadata["processing_failed"] = true;
                    evt.Metadata["failure_reason"] = "External API call failed";
                }
            }

            private async Task SimulateFileSystemOperation(StressTestEvent evt)
            {
                // Simulate file I/O operations
                var latency = random.Next(2, 15);
                await Task.Delay(latency);
                
                evt.Metadata["fs_operation_latency_ms"] = latency;
                evt.Metadata["fs_bytes_processed"] = random.Next(1024, 10240);
            }
        }

        /// <summary>
        /// Complex business logic with multiple decision branches
        /// </summary>
        public class ComplexBusinessLogicFunction : TransformFunction<StressTestEvent, StressTestEvent>
        {
            private readonly Dictionary<string, decimal> exchangeRates = new()
            {
                ["USD"] = 1.0m,
                ["EUR"] = 0.85m,
                ["GBP"] = 0.73m,
                ["JPY"] = 110.0m,
                ["CAD"] = 1.25m
            };

            public override Task<StressTestEvent> Transform(StressTestEvent evt)
            {
                // Complex business rule evaluation
                ApplyBusinessRules(evt);
                CalculateRiskScore(evt);
                PerformCurrencyConversion(evt);
                ApplyDiscountsAndFees(evt);
                ValidateBusinessConstraints(evt);

                return Task.FromResult(evt);
            }

            private void ApplyBusinessRules(StressTestEvent evt)
            {
                var rules = new List<string>();

                // Rule 1: High-value transaction detection
                if (evt.Payload.Amount > 10000)
                {
                    rules.Add("HIGH_VALUE_TRANSACTION");
                    evt.Priority = Math.Min(evt.Priority, 1); // Escalate priority
                }

                // Rule 2: Velocity checking
                if (evt.Metadata.ContainsKey("user_event_count") &&
                    (long)evt.Metadata["user_event_count"] > 50)
                {
                    rules.Add("HIGH_VELOCITY_USER");
                }

                // Rule 3: Time-based rules
                var hour = evt.Timestamp.Hour;
                if (hour < 6 || hour > 22)
                {
                    rules.Add("OFF_HOURS_TRANSACTION");
                }

                // Rule 4: Geographic risk assessment
                if (evt.Metadata.ContainsKey("region"))
                {
                    var region = evt.Metadata["region"].ToString();
                    if (region == "high-risk-region")
                    {
                        rules.Add("HIGH_RISK_GEOGRAPHY");
                    }
                }

                evt.Metadata["applied_business_rules"] = rules.ToArray();
            }

            private void CalculateRiskScore(StressTestEvent evt)
            {
                var riskScore = 0.0;

                // Amount-based risk
                riskScore += Math.Min(evt.Payload.Amount / 1000.0m, 10.0) * 0.1;

                // Velocity-based risk
                if (evt.Metadata.ContainsKey("user_event_count"))
                {
                    var eventCount = (long)evt.Metadata["user_event_count"];
                    riskScore += Math.Min(eventCount / 10.0, 5.0) * 0.15;
                }

                // Time-based risk
                var hour = evt.Timestamp.Hour;
                if (hour < 6 || hour > 22) riskScore += 2.0;

                // Business rule-based risk
                if (evt.Metadata.ContainsKey("applied_business_rules"))
                {
                    var rules = (string[])evt.Metadata["applied_business_rules"];
                    riskScore += rules.Length * 1.5;
                }

                evt.Metadata["risk_score"] = Math.Min(riskScore, 10.0);
                evt.Metadata["risk_level"] = riskScore switch
                {
                    < 2.0 => "LOW",
                    < 5.0 => "MEDIUM",
                    < 8.0 => "HIGH",
                    _ => "CRITICAL"
                };
            }

            private void PerformCurrencyConversion(StressTestEvent evt)
            {
                var sourceCurrency = evt.Payload.Currency;
                var targetCurrency = "USD";

                if (exchangeRates.ContainsKey(sourceCurrency))
                {
                    var rate = exchangeRates[sourceCurrency];
                    var convertedAmount = evt.Payload.Amount / rate;
                    
                    evt.Metadata["original_amount"] = evt.Payload.Amount;
                    evt.Metadata["original_currency"] = sourceCurrency;
                    evt.Metadata["converted_amount_usd"] = convertedAmount;
                    evt.Metadata["exchange_rate"] = rate;
                }
            }

            private void ApplyDiscountsAndFees(StressTestEvent evt)
            {
                var amount = evt.Payload.Amount;
                var fees = 0.0m;
                var discounts = 0.0m;

                // Transaction fees
                fees += amount * 0.025m; // 2.5% transaction fee

                // Volume discounts
                if (evt.Metadata.ContainsKey("user_total_amount"))
                {
                    var totalAmount = (decimal)evt.Metadata["user_total_amount"];
                    if (totalAmount > 100000)
                    {
                        discounts += amount * 0.01m; // 1% volume discount
                    }
                }

                // Priority fees
                if (evt.Priority == 1)
                {
                    fees += 50.0m; // Priority processing fee
                }

                evt.Metadata["calculated_fees"] = fees;
                evt.Metadata["calculated_discounts"] = discounts;
                evt.Metadata["net_amount"] = amount + fees - discounts;
            }

            private void ValidateBusinessConstraints(StressTestEvent evt)
            {
                var violations = new List<string>();

                // Amount constraints
                if (evt.Payload.Amount < 0)
                {
                    violations.Add("NEGATIVE_AMOUNT");
                }

                if (evt.Payload.Amount > 1000000)
                {
                    violations.Add("AMOUNT_EXCEEDS_LIMIT");
                }

                // Item count constraints
                if (evt.Payload.Items.Count > 100)
                {
                    violations.Add("TOO_MANY_ITEMS");
                }

                // User constraints
                if (string.IsNullOrEmpty(evt.Payload.UserId))
                {
                    violations.Add("MISSING_USER_ID");
                }

                if (violations.Any())
                {
                    evt.Metadata["constraint_violations"] = violations.ToArray();
                    evt.Metadata["processing_failed"] = true;
                    evt.Metadata["failure_reason"] = string.Join(", ", violations);
                }
            }
        }

        /// <summary>
        /// Real-time analytics and ML inference function
        /// </summary>
        public class RealTimeAnalyticsFunction : TransformFunction<StressTestEvent, StressTestEvent>
        {
            private readonly ConcurrentDictionary<string, AnalyticsState> analyticsState = new();

            public override Task<StressTestEvent> Transform(StressTestEvent evt)
            {
                UpdateAnalytics(evt);
                PerformMLInference(evt);
                CalculateTrends(evt);

                return Task.FromResult(evt);
            }

            private void UpdateAnalytics(StressTestEvent evt)
            {
                var key = $"{evt.EventType}_{evt.Timestamp.Date:yyyy-MM-dd}";
                
                analyticsState.AddOrUpdate(key,
                    _ => new AnalyticsState
                    {
                        EventCount = 1,
                        TotalAmount = evt.Payload.Amount,
                        MinAmount = evt.Payload.Amount,
                        MaxAmount = evt.Payload.Amount,
                        LastUpdate = DateTime.Now
                    },
                    (_, existing) =>
                    {
                        existing.EventCount++;
                        existing.TotalAmount += evt.Payload.Amount;
                        existing.MinAmount = Math.Min(existing.MinAmount, evt.Payload.Amount);
                        existing.MaxAmount = Math.Max(existing.MaxAmount, evt.Payload.Amount);
                        existing.LastUpdate = DateTime.Now;
                        return existing;
                    });

                var state = analyticsState[key];
                evt.Metadata["daily_event_count"] = state.EventCount;
                evt.Metadata["daily_total_amount"] = state.TotalAmount;
                evt.Metadata["daily_avg_amount"] = state.TotalAmount / state.EventCount;
            }

            private void PerformMLInference(StressTestEvent evt)
            {
                // Simulate ML model inference
                var features = new[]
                {
                    (double)evt.Payload.Amount,
                    evt.Priority,
                    evt.Timestamp.Hour,
                    evt.Payload.Items.Count,
                    evt.Metadata.ContainsKey("risk_score") ? (double)evt.Metadata["risk_score"] : 0.0
                };

                // Simple ML simulation
                var prediction = features.Sum() * 0.1 % 1.0;
                var confidence = Math.Abs(prediction - 0.5) * 2;

                evt.Metadata["ml_prediction"] = prediction;
                evt.Metadata["ml_confidence"] = confidence;
                evt.Metadata["ml_category"] = prediction > 0.7 ? "HIGH_INTEREST" : 
                                             prediction > 0.3 ? "MEDIUM_INTEREST" : "LOW_INTEREST";
            }

            private void CalculateTrends(StressTestEvent evt)
            {
                // Calculate hourly trends
                var hourKey = $"{evt.EventType}_{evt.Timestamp:yyyy-MM-dd-HH}";
                var dayKey = $"{evt.EventType}_{evt.Timestamp.Date:yyyy-MM-dd}";

                if (analyticsState.ContainsKey(hourKey) && analyticsState.ContainsKey(dayKey))
                {
                    var hourlyState = analyticsState[hourKey];
                    var dailyState = analyticsState[dayKey];

                    var hourlyAvg = hourlyState.TotalAmount / hourlyState.EventCount;
                    var dailyAvg = dailyState.TotalAmount / dailyState.EventCount;

                    evt.Metadata["hourly_vs_daily_trend"] = hourlyAvg / dailyAvg;
                }
            }

            private class AnalyticsState
            {
                public long EventCount { get; set; }
                public decimal TotalAmount { get; set; }
                public decimal MinAmount { get; set; }
                public decimal MaxAmount { get; set; }
                public DateTime LastUpdate { get; set; }
            }
        }

        /// <summary>
        /// Complex fraud detection with multiple algorithms
        /// </summary>
        public class ComplexFraudDetectionFunction : TransformFunction<StressTestEvent, StressTestEvent>
        {
            public override Task<StressTestEvent> Transform(StressTestEvent evt)
            {
                var fraudScores = new Dictionary<string, double>();

                // Algorithm 1: Rule-based detection
                fraudScores["rule_based"] = RuleBasedFraudDetection(evt);

                // Algorithm 2: Statistical anomaly detection
                fraudScores["statistical"] = StatisticalAnomalyDetection(evt);

                // Algorithm 3: Behavioral analysis
                fraudScores["behavioral"] = BehavioralAnalysis(evt);

                // Algorithm 4: Network analysis
                fraudScores["network"] = NetworkAnalysis(evt);

                // Ensemble scoring
                var finalScore = fraudScores.Values.Average();
                var maxScore = fraudScores.Values.Max();
                var confidence = fraudScores.Values.StandardDeviation();

                evt.Metadata["fraud_scores"] = fraudScores;
                evt.Metadata["fraud_score_final"] = finalScore;
                evt.Metadata["fraud_score_max"] = maxScore;
                evt.Metadata["fraud_confidence"] = confidence;
                evt.Metadata["is_fraud_suspected"] = finalScore > 0.7;

                return Task.FromResult(evt);
            }

            private double RuleBasedFraudDetection(StressTestEvent evt)
            {
                var score = 0.0;

                // High amount transactions
                if (evt.Payload.Amount > 50000) score += 0.4;

                // Off-hours transactions
                var hour = evt.Timestamp.Hour;
                if (hour < 6 || hour > 22) score += 0.3;

                // High velocity
                if (evt.Metadata.ContainsKey("user_event_count") &&
                    (long)evt.Metadata["user_event_count"] > 20)
                {
                    score += 0.5;
                }

                // Risk factors
                if (evt.Metadata.ContainsKey("risk_score") &&
                    (double)evt.Metadata["risk_score"] > 8.0)
                {
                    score += 0.6;
                }

                return Math.Min(score, 1.0);
            }

            private double StatisticalAnomalyDetection(StressTestEvent evt)
            {
                // Z-score based anomaly detection
                if (evt.Metadata.ContainsKey("daily_avg_amount"))
                {
                    var dailyAvg = (decimal)evt.Metadata["daily_avg_amount"];
                    var currentAmount = evt.Payload.Amount;
                    var stdDev = dailyAvg * 0.5m; // Simplified standard deviation

                    var zScore = Math.Abs((double)(currentAmount - dailyAvg)) / (double)stdDev;
                    return Math.Min(zScore / 3.0, 1.0); // Normalize to 0-1
                }

                return 0.0;
            }

            private double BehavioralAnalysis(StressTestEvent evt)
            {
                var score = 0.0;

                // Unusual transaction patterns
                if (evt.Metadata.ContainsKey("user_session_duration_ms"))
                {
                    var sessionDuration = (double)evt.Metadata["user_session_duration_ms"];
                    if (sessionDuration < 1000) // Very quick transactions
                    {
                        score += 0.4;
                    }
                }

                // Payment method risk
                var paymentMethods = evt.Payload.Items.Select(i => i.Category).Distinct().Count();
                if (paymentMethods > 3) // Multiple payment methods
                {
                    score += 0.3;
                }

                return Math.Min(score, 1.0);
            }

            private double NetworkAnalysis(StressTestEvent evt)
            {
                // Simplified network analysis
                var score = 0.0;

                // Geographic anomalies
                if (evt.Metadata.ContainsKey("region"))
                {
                    var region = evt.Metadata["region"].ToString();
                    if (region == "high-risk-region")
                    {
                        score += 0.6;
                    }
                }

                return Math.Min(score, 1.0);
            }
        }

        /// <summary>
        /// Comprehensive stress test metrics collection
        /// </summary>
        public class StressTestMetricsFunction : TransformFunction<StressTestEvent, StressTestMetrics>
        {
            private readonly Queue<DateTime> processingTimes = new();
            private readonly Queue<double> latencyMeasurements = new();
            private long totalProcessed = 0;
            private long totalFailed = 0;
            private readonly Stopwatch overallStopwatch = Stopwatch.StartNew();

            public override Task<StressTestMetrics> Transform(StressTestEvent evt)
            {
                totalProcessed++;
                
                // Track processing failures
                if (evt.Metadata.ContainsKey("processing_failed") && 
                    (bool)evt.Metadata["processing_failed"])
                {
                    totalFailed++;
                }

                // Calculate latency
                var latency = (DateTime.Now - evt.Timestamp).TotalMilliseconds;
                latencyMeasurements.Enqueue(latency);

                // Maintain sliding windows
                var now = DateTime.Now;
                processingTimes.Enqueue(now);

                // Keep last 60 seconds of data
                while (processingTimes.Count > 0 && now - processingTimes.Peek() > TimeSpan.FromSeconds(60))
                {
                    processingTimes.Dequeue();
                }

                // Keep last 1000 latency measurements
                while (latencyMeasurements.Count > 1000)
                {
                    latencyMeasurements.Dequeue();
                }

                // Generate metrics every 1000 events
                if (totalProcessed % 1000 == 0)
                {
                    return Task.FromResult(new StressTestMetrics
                    {
                        Timestamp = now,
                        TotalProcessed = totalProcessed,
                        TotalFailed = totalFailed,
                        ThroughputPerSecond = processingTimes.Count, // Events in last 60 seconds
                        AverageLatencyMs = latencyMeasurements.Average(),
                        P95LatencyMs = CalculatePercentile(latencyMeasurements.ToArray(), 0.95),
                        P99LatencyMs = CalculatePercentile(latencyMeasurements.ToArray(), 0.99),
                        MemoryUsageMB = GC.GetTotalMemory(false) / 1024 / 1024,
                        CpuUtilizationPercent = GetCpuUtilization(),
                        ActiveThreads = System.Threading.ThreadPool.ThreadCount,
                        BottleneckComponent = IdentifyBottleneck()
                    });
                }

                return Task.FromResult<StressTestMetrics>(null!);
            }

            private double CalculatePercentile(double[] values, double percentile)
            {
                if (values.Length == 0) return 0;
                
                Array.Sort(values);
                int index = (int)(percentile * values.Length);
                return values[Math.Min(index, values.Length - 1)];
            }

            private double GetCpuUtilization()
            {
                // Simplified CPU utilization calculation
                return new Random().NextDouble() * 100;
            }

            private string IdentifyBottleneck()
            {
                // Simple bottleneck identification based on metrics
                var avgLatency = latencyMeasurements.Any() ? latencyMeasurements.Average() : 0;
                var memoryUsage = GC.GetTotalMemory(false) / 1024 / 1024;

                if (avgLatency > 500) return "LATENCY";
                if (memoryUsage > 4096) return "MEMORY";
                if (processingTimes.Count < 500) return "THROUGHPUT";
                return "NONE";
            }
        }

        // Helper methods for data generation
        private static string GenerateEventType(Random random)
        {
            var eventTypes = new[]
            {
                "PAYMENT_TRANSACTION",
                "USER_REGISTRATION", 
                "PRODUCT_PURCHASE",
                "ACCOUNT_UPDATE",
                "FRAUD_CHECK",
                "ANALYTICS_EVENT",
                "SYSTEM_LOG",
                "AUDIT_TRAIL"
            };
            return eventTypes[random.Next(eventTypes.Length)];
        }

        private static int GeneratePriority(string eventType, Random random)
        {
            return eventType switch
            {
                "FRAUD_CHECK" => 1,
                "PAYMENT_TRANSACTION" => random.Next(1, 3),
                "USER_REGISTRATION" => 2,
                _ => 3
            };
        }

        private static ComplexPayload GenerateComplexPayload(long sequenceNumber, Random random)
        {
            var itemCount = random.Next(1, 20);
            var items = new List<TransactionItem>();

            for (int i = 0; i < itemCount; i++)
            {
                items.Add(new TransactionItem
                {
                    ProductId = $"PROD_{random.Next(1000, 9999)}",
                    Quantity = random.Next(1, 10),
                    UnitPrice = (decimal)(random.NextDouble() * 1000),
                    Category = GenerateCategory(random)
                });
            }

            return new ComplexPayload
            {
                UserId = $"USER_{random.Next(1, 10000):D6}",
                Amount = items.Sum(i => i.Quantity * i.UnitPrice),
                Currency = GenerateCurrency(random),
                Items = items,
                CustomFields = GenerateCustomFields(random),
                BinaryData = GenerateBinaryData(random)
            };
        }

        private static string GenerateCategory(Random random)
        {
            var categories = new[] { "ELECTRONICS", "CLOTHING", "FOOD", "BOOKS", "HOME", "AUTOMOTIVE" };
            return categories[random.Next(categories.Length)];
        }

        private static string GenerateCurrency(Random random)
        {
            var currencies = new[] { "USD", "EUR", "GBP", "JPY", "CAD" };
            return currencies[random.Next(currencies.Length)];
        }

        private static Dictionary<string, object> GenerateCustomFields(Random random)
        {
            return new Dictionary<string, object>
            {
                ["promotion_code"] = $"PROMO_{random.Next(1000, 9999)}",
                ["customer_tier"] = random.Next(1, 4) switch
                {
                    1 => "BRONZE",
                    2 => "SILVER", 
                    3 => "GOLD",
                    _ => "PLATINUM"
                },
                ["device_type"] = random.Next(1, 4) switch
                {
                    1 => "MOBILE",
                    2 => "DESKTOP",
                    _ => "TABLET"
                },
                ["session_id"] = Guid.NewGuid().ToString("N")[..16]
            };
        }

        private static byte[] GenerateBinaryData(Random random)
        {
            var size = random.Next(100, 2000);
            var data = new byte[size];
            random.NextBytes(data);
            return data;
        }

        private static Dictionary<string, object> GenerateMetadata(long sequenceNumber, Random random)
        {
            return new Dictionary<string, object>
            {
                ["correlation_id"] = Guid.NewGuid().ToString("N")[..12],
                ["source_system"] = $"SYSTEM_{random.Next(1, 5)}",
                ["batch_id"] = sequenceNumber / 1000,
                ["retry_count"] = random.Next(0, 3),
                ["region"] = random.Next(1, 4) switch
                {
                    1 => "us-east",
                    2 => "us-west",
                    3 => "eu-central",
                    _ => "ap-southeast"
                }
            };
        }

        // Extension methods
        public static double StandardDeviation(this IEnumerable<double> values)
        {
            var enumerable = values.ToList();
            var avg = enumerable.Average();
            var sumOfSquaresOfDifferences = enumerable.Select(val => (val - avg) * (val - avg)).Sum();
            return Math.Sqrt(sumOfSquaresOfDifferences / enumerable.Count);
        }
    }

    // Base transform function
    public abstract class TransformFunction<TInput, TOutput>
    {
        public abstract Task<TOutput> Transform(TInput input);
    }

    // Base map function  
    public abstract class MapFunction<TInput, TOutput>
    {
        public abstract TOutput Map(TInput input);
    }

    // Base aggregate function
    public abstract class AggregationFunction<TInput, TOutput>
    {
        public abstract TOutput Aggregate(IEnumerable<TInput> inputs);
    }

    public class ComplexAggregationFunction : AggregationFunction<StressTestEvent, string>
    {
        public override string Aggregate(IEnumerable<StressTestEvent> events)
        {
            var eventList = events.ToList();
            if (!eventList.Any()) return "Empty window";

            var totalAmount = eventList.Sum(e => e.Payload.Amount);
            var avgAmount = eventList.Average(e => e.Payload.Amount);
            var eventCount = eventList.Count;
            var uniqueUsers = eventList.Select(e => e.Payload.UserId).Distinct().Count();

            return $"Window: {eventCount} events, {uniqueUsers} users, " +
                   $"Total: ${totalAmount:F2}, Avg: ${avgAmount:F2}";
        }
    }
}
```

## 🎯 Day 7 Exercises

### Exercise 7.2: Fault Injection Testing

Create a fault injection framework:

```csharp
public class FaultInjectionFramework
{
    // Implement various failure scenarios:
    // 1. Random network timeouts
    // 2. Memory pressure simulation
    // 3. Checkpoint corruption
    // 4. TaskManager failures
    // 5. Kafka broker disconnections
}
```

### Exercise 7.3: Performance Benchmark Suite

Build comprehensive performance benchmarks:

```csharp
public class PerformanceBenchmarkSuite
{
    // Benchmark categories:
    // 1. Throughput tests (events/second)
    // 2. Latency tests (P50, P95, P99)
    // 3. Memory efficiency tests
    // 4. CPU utilization tests
    // 5. Network bandwidth tests
}
```

## 📊 Stress Test Results Analysis

### Expected Performance Targets

| Metric | Target | Good | Needs Improvement |
|--------|--------|------|-------------------|
| **Throughput** | >10K events/sec | >5K events/sec | <5K events/sec |
| **P95 Latency** | <100ms | <200ms | >200ms |
| **Memory Usage** | <4GB | <6GB | >6GB |
| **CPU Utilization** | <80% | <90% | >90% |
| **Error Rate** | <1% | <5% | >5% |

### Performance Analysis Dashboard

```json
{
  "stress_test_results": {
    "test_duration_seconds": 300,
    "total_events_processed": 1000000,
    "throughput_metrics": {
      "average_events_per_second": 3333.33,
      "peak_events_per_second": 5000,
      "sustained_rate_events_per_second": 3000
    },
    "latency_metrics": {
      "average_latency_ms": 45.2,
      "p50_latency_ms": 32.1,
      "p95_latency_ms": 87.5,
      "p99_latency_ms": 156.3,
      "max_latency_ms": 342.1
    },
    "resource_utilization": {
      "peak_memory_usage_mb": 3456,
      "average_cpu_utilization_percent": 72.3,
      "peak_cpu_utilization_percent": 89.1,
      "network_bandwidth_mbps": 125.7
    },
    "reliability_metrics": {
      "total_failures": 1247,
      "failure_rate_percent": 0.12,
      "recovery_time_average_ms": 235.6,
      "checkpoint_success_rate_percent": 99.8
    }
  }
}
```

## 📈 LocalTesting Stress Monitor

Access comprehensive stress testing at http://localhost:18000/ (Swagger UI is at root):

### Available Stress Test Endpoints:
- **POST /api/ComplexLogicStressTest/start**: Start comprehensive stress test
- **GET /api/ComplexLogicStressTest/status**: Check current test status
- **GET /api/ComplexLogicStressTest/metrics**: Real-time performance metrics
- **POST /api/ComplexLogicStressTest/stop**: Stop running tests
- **GET /api/ComplexLogicStressTest/report**: Generate detailed report

### Real-time Monitoring:
```bash
# Monitor stress test progress
curl http://localhost:18000/api/ComplexLogicStressTest/metrics

# View detailed performance report
curl http://localhost:18000/api/ComplexLogicStressTest/report
```

## 🔧 Day 7 Troubleshooting

### Common Stress Test Issues:

**Memory Leaks Under Load**:
```bash
# Monitor memory usage during stress test
jstat -gc -t <flink_process_id> 5s

# Force garbage collection if needed
jcmd <flink_process_id> GC.run

# Adjust memory settings
export FLINK_TASKMANAGER_MEMORY_PROCESS_SIZE=8g
export FLINK_JVM_OPTIONS="-XX:+UseG1GC -XX:MaxGCPauseMillis=20"
```

**Thread Pool Exhaustion**:
```csharp
// Monitor thread pool metrics
var threadPool = ThreadPool.ThreadCount;
var availableThreads = ThreadPool.CompletionPortThreads;

// Adjust thread pool settings
ThreadPool.SetMinThreads(50, 50);
ThreadPool.SetMaxThreads(200, 200);
```

**Network Buffer Overflow**:
```bash
# Monitor network buffer utilization
curl http://localhost:18002/taskmanagers/<tm_id>/metrics

# Tune network buffer configuration
taskmanager.network.numberOfBuffers: 16384
taskmanager.network.buffers-per-channel: 32
taskmanager.network.memory.fraction: 0.3
```

## 📝 Day 7 Assessment

### Knowledge Check:
1. What are the four dimensions of stress testing for streaming applications?
2. How do you identify bottlenecks in complex processing pipelines?
3. What metrics indicate memory pressure during stress tests?
4. How do you design fault injection scenarios for reliability testing?
5. What are the key performance targets for production streaming systems?

### Practical Assessment:
Design and implement a comprehensive stress test that:
1. Processes 1M+ events with complex business logic
2. Includes realistic failure scenarios and recovery testing
3. Monitors all key performance and reliability metrics
4. Identifies system bottlenecks and optimization opportunities
5. Validates production readiness across all dimensions

## 🎯 Day 7 Completion Checklist

- [ ] Understood stress testing methodologies for streaming systems
- [ ] Implemented complex logic stress tests with 1M+ events
- [ ] Built comprehensive performance monitoring and metrics
- [ ] Created fault injection and reliability testing frameworks
- [ ] Analyzed performance results and identified optimizations
- [ ] Completed all exercises with stress testing patterns
- [ ] Passed knowledge and practical assessments

## 📚 Preparation for Day 8

Tomorrow we'll explore **Temporal Workflows & Orchestration**. To prepare:

1. **Review workflow orchestration concepts**: long-running processes, state machines
2. **Understand Temporal.io integration patterns**: activities, workflows, signals
3. **Explore distributed system orchestration**: saga patterns, compensation

## 🎉 Congratulations!

You've mastered complex logic stress testing with Flink 2.1.0! You now have:
- Comprehensive stress testing methodologies
- Advanced performance monitoring and analysis
- Fault injection and reliability testing frameworks
- Production readiness validation capabilities

**Tomorrow**: We'll dive into temporal workflows and long-running process orchestration!

---

**Next**: [Day 8: Temporal Workflows & Orchestration →](../Day08-Temporal-Workflows/README.md)
---

## 🗺️ Course Navigation
**[← Day 6: Advanced Windows & Joins](../Day06-Advanced-Windows-Joins/)** | **[Course Overview](../README.md)** | **[Next: Day 8 - Exactly-Once Semantics →](../Day08-Exactly-Once-Semantics/)**

**Course Progress**: Day 7 of 14 Complete ✅