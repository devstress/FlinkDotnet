# Day 9: Performance Optimization and Scaling Patterns

## 🗺️ Course Navigation
**[← Day 8: Exactly-Once Semantics](../Day08-Exactly-Once-Semantics/)** | **[Course Overview](../README.md)** | **[Next: Day 10 - Security, Privacy & Compliance →](../Day10-Security-Privacy-Compliance/)**

---

## Overview
Master advanced performance optimization techniques and scaling patterns for high-throughput streaming applications handling millions of events per second.

## Learning Objectives
- Optimize resource allocation and parallelism strategies
- Implement advanced memory management and garbage collection tuning
- Design horizontal scaling patterns for massive throughput
- Build performance monitoring and bottleneck identification systems
- Apply network optimization and serialization efficiency techniques

## Real-World Context
Netflix processes over 10 billion events per day across their streaming platform, requiring sub-10ms latency for real-time recommendations. Their optimization strategies include custom serializers, memory-mapped state backends, and dynamic parallelism adjustment based on traffic patterns.

## Technical Deep Dive

### Dynamic Parallelism and Resource Management
```csharp
// Netflix-style dynamic parallelism based on throughput
public class AdaptiveParallelismController
{
    private readonly MetricRegistry metrics;
    private readonly JobManagerGateway jobManager;
    private int currentParallelism;
    private readonly Dictionary<string, PerformanceThresholds> operatorThresholds;
    
    public async Task OptimizeParallelism(JobExecutionResult executionResult)
    {
        var throughputMetrics = await CollectThroughputMetrics();
        var latencyMetrics = await CollectLatencyMetrics();
        var resourceUtilization = await CollectResourceMetrics();
        
        foreach (var operatorId in GetBottleneckOperators(throughputMetrics, latencyMetrics))
        {
            var recommendation = CalculateParallelismRecommendation(
                operatorId, 
                throughputMetrics[operatorId], 
                resourceUtilization[operatorId]);
                
            if (recommendation.ShouldScale)
            {
                await ScaleOperator(operatorId, recommendation.NewParallelism);
                LogScalingDecision(operatorId, recommendation);
            }
        }
    }
    
    private ParallelismRecommendation CalculateParallelismRecommendation(
        string operatorId, 
        ThroughputMetric throughput, 
        ResourceMetric resources)
    {
        var currentThroughput = throughput.EventsPerSecond;
        var targetThroughput = operatorThresholds[operatorId].TargetThroughput;
        var cpuUtilization = resources.CpuUtilization;
        var memoryUtilization = resources.MemoryUtilization;
        
        // Netflix algorithm: Scale up if CPU > 70% and throughput < target
        if (cpuUtilization > 0.7 && currentThroughput < targetThroughput)
        {
            var scaleUpFactor = Math.Min(2.0, targetThroughput / currentThroughput);
            var newParallelism = (int)Math.Ceiling(currentParallelism * scaleUpFactor);
            
            return new ParallelismRecommendation
            {
                ShouldScale = true,
                NewParallelism = Math.Min(newParallelism, GetMaxParallelism()),
                Reason = $"CPU: {cpuUtilization:P}, Throughput: {currentThroughput}/s vs target {targetThroughput}/s"
            };
        }
        
        // Scale down if CPU < 30% and over-provisioned
        if (cpuUtilization < 0.3 && currentParallelism > 1)
        {
            return new ParallelismRecommendation
            {
                ShouldScale = true,
                NewParallelism = Math.Max(1, currentParallelism / 2),
                Reason = $"Under-utilized: CPU {cpuUtilization:P}"
            };
        }
        
        return new ParallelismRecommendation { ShouldScale = false };
    }
}
```

### Memory Optimization and GC Tuning
```csharp
// Uber-style memory optimization for high-throughput processing
public class MemoryOptimizedProcessor : RichMapFunction<Event, ProcessedEvent>
{
    private ObjectPool<StringBuilder> stringBuilderPool;
    private ByteBuffer reusableBuffer;
    private readonly LRUCache<string, ProcessedEvent> resultCache;
    
    public override void Open(Configuration parameters)
    {
        // Configure object pools to reduce GC pressure
        stringBuilderPool = new ObjectPool<StringBuilder>(() => new StringBuilder(1024), 100);
        reusableBuffer = ByteBuffer.Allocate(8192);
        
        // LRU cache for frequently accessed results
        resultCache = new LRUCache<string, ProcessedEvent>(10000);
        
        // Configure off-heap memory for large objects
        ConfigureOffHeapMemory();
    }
    
    public override ProcessedEvent Map(Event value)
    {
        // Check cache first to avoid recomputation
        var cacheKey = GenerateCacheKey(value);
        if (resultCache.TryGetValue(cacheKey, out var cachedResult))
        {
            return cachedResult;
        }
        
        // Use object pooling for temporary objects
        var stringBuilder = stringBuilderPool.Get();
        try
        {
            // Process with minimal allocations
            var result = ProcessEventOptimized(value, stringBuilder, reusableBuffer);
            
            // Cache result for future requests
            resultCache.Put(cacheKey, result);
            
            return result;
        }
        finally
        {
            stringBuilder.Clear();
            stringBuilderPool.Return(stringBuilder);
            reusableBuffer.Clear();
        }
    }
    
    private void ConfigureOffHeapMemory()
    {
        // Configure off-heap storage for large state
        var offHeapConfig = new OffHeapStateBackendConfig
        {
            MaxOffHeapMemory = "2GB",
            CompressionEnabled = true,
            CompressionCodec = "LZ4"
        };
        
        RuntimeContext.GetExecutionConfig().SetOffHeapStateBackend(offHeapConfig);
    }
}

// JVM optimization for streaming workloads
public static class JVMOptimization
{
    public static void ConfigureForStreaming()
    {
        // G1GC configuration for low-latency streaming
        var jvmArgs = new[]
        {
            "-XX:+UseG1GC",
            "-XX:MaxGCPauseMillis=20", // Netflix: target 20ms max pause
            "-XX:G1HeapRegionSize=16m",
            "-XX:+G1UseAdaptiveIHOP",
            "-XX:G1MixedGCCountTarget=8",
            "-XX:InitiatingHeapOccupancyPercent=35",
            "-XX:+UnlockExperimentalVMOptions",
            "-XX:+UseCGroupMemoryLimitForHeap",
            "-XX:+UseContainerSupport"
        };
        
        // Memory allocation optimization
        Environment.SetEnvironmentVariable("MALLOC_ARENA_MAX", "4");
        Environment.SetEnvironmentVariable("MALLOC_MMAP_THRESHOLD_", "131072");
    }
}
```

### High-Performance Serialization
```csharp
// Custom high-performance serializer for hot path objects
public class OptimizedEventSerializer : TypeSerializer<Event>
{
    private readonly ByteBuffer buffer = ByteBuffer.Allocate(1024);
    private readonly FieldAccessor[] fieldAccessors;
    
    public OptimizedEventSerializer()
    {
        // Pre-compile field accessors for zero-reflection serialization
        fieldAccessors = CompileFieldAccessors<Event>();
    }
    
    public override void Serialize(Event record, DataOutputView target)
    {
        buffer.Clear();
        
        // Use unsafe serialization for maximum performance
        unsafe
        {
            fixed (byte* bufferPtr = buffer.Array)
            {
                // Direct memory access for primitive fields
                *(long*)(bufferPtr + 0) = record.Timestamp;
                *(int*)(bufferPtr + 8) = record.UserId.GetHashCode();
                *(double*)(bufferPtr + 12) = record.Value;
                
                // Variable-length string with length prefix
                var messageBytes = Encoding.UTF8.GetBytes(record.Message);
                *(int*)(bufferPtr + 20) = messageBytes.Length;
                Marshal.Copy(messageBytes, 0, new IntPtr(bufferPtr + 24), messageBytes.Length);
                
                var totalLength = 24 + messageBytes.Length;
                target.Write(buffer.Array, 0, totalLength);
            }
        }
    }
    
    public override Event Deserialize(DataInputView source)
    {
        var length = source.ReadInt();
        buffer.Clear();
        source.ReadFully(buffer.Array, 0, length);
        
        unsafe
        {
            fixed (byte* bufferPtr = buffer.Array)
            {
                return new Event
                {
                    Timestamp = *(long*)bufferPtr,
                    UserId = GenerateUserIdFromHash(*(int*)(bufferPtr + 8)),
                    Value = *(double*)(bufferPtr + 12),
                    Message = Encoding.UTF8.GetString(
                        buffer.Array, 
                        24, 
                        *(int*)(bufferPtr + 20))
                };
            }
        }
    }
}
```

### Network Optimization
```csharp
// LinkedIn-style network optimization for data shuffling
public class OptimizedNetworkStack
{
    public static void ConfigureHighThroughputNetwork(ExecutionEnvironment env)
    {
        var networkConfig = env.GetConfiguration();
        
        // Buffer optimization for high throughput
        networkConfig.SetString("taskmanager.network.memory.fraction", "0.2");
        networkConfig.SetString("taskmanager.network.memory.min", "128mb");
        networkConfig.SetString("taskmanager.network.memory.max", "2gb");
        
        // Network buffer configuration
        networkConfig.SetString("taskmanager.network.numberOfBuffers", "8192");
        networkConfig.SetString("taskmanager.network.netty.num-arenas", "4");
        networkConfig.SetString("taskmanager.network.netty.client.numThreads", "4");
        networkConfig.SetString("taskmanager.network.netty.server.numThreads", "4");
        
        // Compression for network traffic
        networkConfig.SetString("taskmanager.network.compression.enable", "true");
        networkConfig.SetString("taskmanager.network.compression.codec", "LZ4");
        
        // TCP optimization
        networkConfig.SetString("taskmanager.network.netty.transport", "nio");
        networkConfig.SetString("taskmanager.network.netty.sendBufferSize", "32768");
        networkConfig.SetString("taskmanager.network.netty.receiveBufferSize", "32768");
    }
}
```

## Hands-On Exercises

### Exercise 1: High-Frequency Trading Optimization
Build a low-latency trading system that:
- Processes market data with sub-millisecond latency
- Implements zero-allocation processing paths
- Uses CPU affinity for consistent performance
- Measures and optimizes for 99.99th percentile latency

### Exercise 2: Real-time Analytics at Scale
Create a high-throughput analytics system that:
- Processes 1M+ events per second per node
- Implements dynamic load balancing
- Uses memory-mapped state for large aggregations
- Optimizes network shuffling for complex joins

### Exercise 3: IoT Data Processing Pipeline
Design an IoT processing system that:
- Handles millions of sensor readings per second
- Implements adaptive sampling based on data patterns
- Uses compression and batching for efficiency
- Scales dynamically based on device connectivity

## Performance Monitoring

### Comprehensive Performance Metrics
```csharp
// Google SRE-style performance monitoring
public class PerformanceMonitor
{
    private readonly Histogram processingLatency;
    private readonly Counter throughputEvents;
    private readonly Gauge memoryUsage;
    private readonly Gauge gcPauseTime;
    private readonly Counter networkBytes;
    
    public void RecordProcessingMetrics(TimeSpan latency, long memoryUsed, long networkTraffic)
    {
        processingLatency.Observe(latency.TotalMicroseconds);
        throughputEvents.Inc();
        memoryUsage.Set(memoryUsed);
        networkBytes.Inc(networkTraffic);
        
        // Alert on performance degradation
        if (latency.TotalMilliseconds > GetLatencySLA())
        {
            AlertingSystem.TriggerAlert(new PerformanceAlert
            {
                Type = AlertType.LatencyViolation,
                ActualLatency = latency,
                SLALatency = TimeSpan.FromMilliseconds(GetLatencySLA()),
                Timestamp = DateTimeOffset.UtcNow
            });
        }
    }
    
    public PerformanceReport GenerateReport(TimeSpan period)
    {
        return new PerformanceReport
        {
            AverageLatency = processingLatency.GetMean(),
            P95Latency = processingLatency.GetPercentile(95),
            P99Latency = processingLatency.GetPercentile(99),
            P999Latency = processingLatency.GetPercentile(99.9),
            TotalThroughput = throughputEvents.Value,
            PeakMemoryUsage = memoryUsage.Value,
            AverageGCPause = gcPauseTime.GetMean()
        };
    }
}
```

### Bottleneck Detection
```csharp
// Automated bottleneck detection system
public class BottleneckDetector
{
    public List<PerformanceBottleneck> DetectBottlenecks(List<OperatorMetrics> operatorMetrics)
    {
        var bottlenecks = new List<PerformanceBottleneck>();
        
        foreach (var metric in operatorMetrics)
        {
            // CPU bottleneck detection
            if (metric.CpuUtilization > 0.85 && metric.BackpressureRatio > 0.5)
            {
                bottlenecks.Add(new PerformanceBottleneck
                {
                    Type = BottleneckType.CPU,
                    OperatorId = metric.OperatorId,
                    Severity = CalculateSeverity(metric.CpuUtilization),
                    Recommendation = "Increase parallelism or optimize algorithm"
                });
            }
            
            // Memory bottleneck detection
            if (metric.MemoryUtilization > 0.9 || metric.GcPauseTime > TimeSpan.FromMilliseconds(100))
            {
                bottlenecks.Add(new PerformanceBottleneck
                {
                    Type = BottleneckType.Memory,
                    OperatorId = metric.OperatorId,
                    Severity = CalculateSeverity(metric.MemoryUtilization),
                    Recommendation = "Optimize memory usage or increase heap size"
                });
            }
            
            // Network bottleneck detection
            if (metric.NetworkUtilization > 0.8 && metric.SerializationTime > TimeSpan.FromMicroseconds(1000))
            {
                bottlenecks.Add(new PerformanceBottleneck
                {
                    Type = BottleneckType.Network,
                    OperatorId = metric.OperatorId,
                    Severity = CalculateSeverity(metric.NetworkUtilization),
                    Recommendation = "Optimize serialization or enable compression"
                });
            }
        }
        
        return bottlenecks.OrderByDescending(b => b.Severity).ToList();
    }
}
```

## Testing Performance Optimizations

### Load Testing Framework
```csharp
[Test]
public async Task TestHighThroughputPerformance()
{
    var testEnvironment = CreateOptimizedTestEnvironment();
    var eventGenerator = new HighVolumeEventGenerator(1_000_000); // 1M events/sec
    
    var startTime = DateTimeOffset.UtcNow;
    var processedCount = 0L;
    var latencies = new ConcurrentBag<TimeSpan>();
    
    await testEnvironment.Execute(async () =>
    {
        await foreach (var events in eventGenerator.GenerateAsync())
        {
            var processingStart = DateTimeOffset.UtcNow;
            await ProcessBatch(events);
            var processingEnd = DateTimeOffset.UtcNow;
            
            Interlocked.Add(ref processedCount, events.Count);
            latencies.Add(processingEnd - processingStart);
        }
    });
    
    var totalTime = DateTimeOffset.UtcNow - startTime;
    var throughput = processedCount / totalTime.TotalSeconds;
    var p99Latency = latencies.OrderByDescending(l => l).Take((int)(latencies.Count * 0.01)).First();
    
    Assert.That(throughput, Is.GreaterThan(800_000)); // 800K events/sec minimum
    Assert.That(p99Latency, Is.LessThan(TimeSpan.FromMilliseconds(10))); // 10ms p99
}
```

## Architecture Integration
- Configure cluster for optimal resource utilization
- Set up automated performance monitoring dashboards
- Implement alerting for performance degradation
- Create capacity planning tools based on historical metrics

## References
- [Netflix Tech Blog: Keystone Real-time Stream Processing](https://netflixtechblog.com/keystone-real-time-stream-processing-platform-a3ee651812a)
- [LinkedIn Engineering: Brooklin - Real-time Data Streaming](https://engineering.linkedin.com/blog/2019/brooklin-real-time-data-streaming)
- [Uber Engineering: Real-time Analytics at Scale](https://eng.uber.com/logging/)
- [Google SRE Book: Monitoring and Alerting](https://sre.google/sre-book/)
- [JVM Performance Tuning Guide](https://docs.oracle.com/javase/8/docs/technotes/guides/vm/gctuning/)

## Next Steps
Day 10 focuses on security patterns and data privacy compliance for enterprise streaming applications.
---

## 🗺️ Course Navigation
**[← Day 8: Exactly-Once Semantics](../Day08-Exactly-Once-Semantics/)** | **[Course Overview](../README.md)** | **[Next: Day 10 - Security, Privacy & Compliance →](../Day10-Security-Privacy-Compliance/)**

**Course Progress**: Day 9 of 14 Complete ✅

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

