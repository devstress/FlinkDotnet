# FlinkDotNet Observability Framework

## Overview

The FlinkDotNet Observability Framework provides comprehensive monitoring, logging, metrics collection, and tracing capabilities following **Apache Flink 2.0 standards**. This implementation ensures compatibility with Apache Flink's observability patterns while providing native .NET integration.

## Table of Contents

1. [Architecture](#architecture)
2. [Components](#components)
3. [Configuration](#configuration)
4. [Metrics](#metrics)
5. [Tracing](#tracing)
6. [Logging](#logging)
7. [Health Monitoring](#health-monitoring)
8. [Integration Guide](#integration-guide)
9. [Best Practices](#best-practices)
10. [Troubleshooting](#troubleshooting)

## Architecture

The observability framework is built on four core pillars, aligned with Apache Flink 2.0:

```
┌─────────────────────────────────────────────────────────────┐
│                    FlinkDotNet Observability                │
├─────────────────┬─────────────────┬─────────────────┬───────┤
│    Metrics      │     Tracing     │     Logging     │Health │
│   Collection    │   Distributed   │   Structured    │Checks │
│                 │                 │                 │       │
│ • Throughput    │ • End-to-End    │ • Contextual    │• Job  │
│ • Latency       │ • Correlation   │ • Searchable    │• Op.  │
│ • Backpressure  │ • Performance   │ • Standardized  │• Res. │
│ • Errors        │ • Debugging     │ • JSON Format   │• Net. │
└─────────────────┴─────────────────┴─────────────────┴───────┘
```

### Core Principles

1. **Apache Flink Compatibility**: All metrics, naming conventions, and patterns follow Apache Flink 2.0 standards
2. **OpenTelemetry Native**: Built on OpenTelemetry for industry-standard observability
3. **Performance First**: Minimal overhead with async operations and efficient data structures
4. **Production Ready**: Comprehensive error handling, circuit breakers, and graceful degradation

## Components

### IFlinkMetrics
Provides comprehensive metrics collection following Apache Flink metric patterns:

```csharp
public interface IFlinkMetrics
{
    void RecordIncomingRecord(string operatorName, string taskId);
    void RecordOutgoingRecord(string operatorName, string taskId);
    void RecordLatency(string operatorName, string taskId, TimeSpan latency);
    void RecordBackpressure(string operatorName, string taskId, TimeSpan duration);
    void RecordCheckpointDuration(string jobId, TimeSpan duration);
    // ... additional methods
}
```

### IFlinkTracing
Enables distributed tracing across job components:

```csharp
public interface IFlinkTracing
{
    Activity? StartOperatorSpan(string operatorName, string taskId);
    Activity? StartRecordProcessingSpan(string operatorName, string taskId, string recordId);
    Activity? StartCheckpointSpan(string jobId, long checkpointId);
    // ... additional methods
}
```

### IFlinkLogger
Provides structured logging with rich context:

```csharp
public interface IFlinkLogger
{
    void LogOperatorLifecycle(LogLevel level, string operatorName, string taskId, string lifecycle);
    void LogRecordProcessing(LogLevel level, string operatorName, string taskId, string action);
    void LogCheckpoint(LogLevel level, string jobId, long checkpointId, string phase);
    // ... additional methods
}
```

### IFlinkHealthMonitor
Comprehensive health monitoring for all components:

```csharp
public interface IFlinkHealthMonitor
{
    Task<FlinkHealthCheckResult> CheckOperatorHealthAsync(string operatorName, string taskId);
    Task<FlinkHealthCheckResult> CheckJobHealthAsync(string jobId);
    Task<Dictionary<string, FlinkHealthCheckResult>> CheckOverallHealthAsync();
    // ... additional methods
}
```

## Configuration

### Basic Setup

Add FlinkDotNet observability to your application:

```csharp
// Program.cs
using FlinkDotNet.Core.Observability.Extensions;

var builder = Host.CreateApplicationBuilder(args);

// Add FlinkDotNet observability with configuration
builder.Services.AddFlinkObservability("FlinkJobSimulator", options =>
{
    options.EnableConsoleMetrics = true;
    options.EnableConsoleTracing = true;
    options.EnableOperatorMonitoring = true;
    options.EnableJobMonitoring = true;
    options.MetricsIntervalSeconds = 10;
    options.HealthCheckIntervalSeconds = 30;
});

var app = builder.Build();
```

### Advanced Configuration

For production environments:

```csharp
builder.Services.AddFlinkObservability("FlinkJobSimulator", options =>
{
    // Production-ready configuration
    options.EnablePrometheusMetrics = true;
    options.EnableJaegerTracing = true;
    options.JaegerEndpoint = "http://jaeger:14268/api/traces";
    
    // Console outputs disabled in production
    options.EnableConsoleMetrics = false;
    options.EnableConsoleTracing = false;
    
    // Monitoring configuration
    options.EnableOperatorMonitoring = true;
    options.EnableJobMonitoring = true;
    options.EnableNetworkMonitoring = true;
    options.EnableStateBackendMonitoring = true;
    
    // Intervals
    options.MetricsIntervalSeconds = 5;
    options.HealthCheckIntervalSeconds = 15;
});
```

### Environment Variables

Configure observability through environment variables:

```bash
# OpenTelemetry Configuration
OTEL_EXPORTER_OTLP_ENDPOINT=http://otlp-collector:4317
OTEL_SERVICE_NAME=FlinkJobSimulator
OTEL_SERVICE_VERSION=1.0.0

# Flink-specific Configuration
FLINK_OBSERVABILITY_METRICS_INTERVAL=10
FLINK_OBSERVABILITY_HEALTH_INTERVAL=30
FLINK_OBSERVABILITY_ENABLE_DETAILED_TRACING=true
```

## Metrics

### Apache Flink Standard Metrics

The framework implements all standard Apache Flink metrics:

#### Operator Metrics
- `flink_taskmanager_job_task_operator_numRecordsIn`: Records received by operator
- `flink_taskmanager_job_task_operator_numRecordsOut`: Records emitted by operator
- `flink_taskmanager_job_task_operator_numBytesIn`: Bytes received by operator
- `flink_taskmanager_job_task_operator_numBytesOut`: Bytes emitted by operator
- `flink_taskmanager_job_latency`: Processing latency histogram
- `flink_taskmanager_job_task_backPressuredTimeMsPerSecond`: Backpressure time

#### Job Metrics
- `flink_jobmanager_job_lastCheckpointDuration`: Checkpoint duration
- `flink_jobmanager_job_lastCheckpointSize`: Checkpoint size
- `flink_jobmanager_job_numRestarts`: Number of job restarts

#### System Metrics
- `flink_taskmanager_job_task_buffers_inPoolUsage`: Buffer pool usage
- `flink_taskmanager_job_task_operator_managedMemoryUsed`: Memory usage
- `flink_taskmanager_job_task_operator_watermarkLag`: Watermark lag

### Usage Example

```csharp
public class MyOperator : IOperatorLifecycle
{
    private readonly IFlinkMetrics _metrics;
    private readonly IFlinkLogger _logger;
    
    public void ProcessRecord(MyRecord record)
    {
        using var activity = _tracing.StartRecordProcessingSpan("MyOperator", _taskId, record.Id);
        var startTime = DateTime.UtcNow;
        
        try
        {
            // Record incoming data
            _metrics.RecordIncomingRecord("MyOperator", _taskId);
            _metrics.RecordBytesIn("MyOperator", _taskId, record.Size);
            
            // Process the record
            var result = ProcessBusinessLogic(record);
            
            // Record outgoing data
            _metrics.RecordOutgoingRecord("MyOperator", _taskId);
            _metrics.RecordBytesOut("MyOperator", _taskId, result.Size);
            
            // Record processing latency
            var latency = DateTime.UtcNow - startTime;
            _metrics.RecordLatency("MyOperator", _taskId, latency);
            
            _logger.LogRecordProcessing(LogLevel.Debug, "MyOperator", _taskId, 
                "processed", 1, latency);
        }
        catch (Exception ex)
        {
            _metrics.RecordError("MyOperator", _taskId, ex.GetType().Name);
            _logger.LogError("MyOperator", _taskId, ex, "processing");
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

## Tracing

### Distributed Tracing Architecture

FlinkDotNet tracing follows Apache Flink's trace correlation patterns:

```
Job Submission
├── Operator Chain Span
│   ├── Record Processing Span
│   │   ├── State Operation Span
│   │   └── Network Communication Span
│   └── Checkpoint Span
└── Job Completion Span
```

### Trace Context Propagation

```csharp
// Start a job-level trace
using var jobSpan = _tracing.StartJobSpan("MyStreamingJob");

// Propagate context to operators
foreach (var operator in operators)
{
    var operatorContext = _tracing.GetCurrentTraceContext();
    operator.SetTraceContext(operatorContext);
}
```

### Custom Spans

```csharp
public async Task ProcessWithTracing()
{
    using var span = _tracing.StartOperatorSpan("CustomOperator", "task-1");
    
    // Add contextual information
    _tracing.AddSpanAttribute("record.count", recordCount);
    _tracing.AddSpanAttribute("processing.mode", "streaming");
    
    // Add events for significant occurrences
    _tracing.AddSpanEvent("checkpoint.triggered", new Dictionary<string, object>
    {
        ["checkpoint.id"] = checkpointId,
        ["trigger.reason"] = "timer"
    });
    
    try
    {
        await ProcessData();
    }
    catch (Exception ex)
    {
        _tracing.RecordSpanError(ex, "Processing failed during data transformation");
        throw;
    }
}
```

## Logging

### Structured Logging Format

All logs follow a consistent JSON structure:

```json
{
  "timestamp": "2024-01-15T10:30:00.000Z",
  "level": "INFO",
  "message": "Record processing completed in operator MyOperator, task task-1: 1000 records in 150ms",
  "flink.operator.name": "MyOperator",
  "flink.task.id": "task-1",
  "flink.job.id": "job-123",
  "flink.record.count": 1000,
  "flink.processing.time.ms": 150,
  "flink.event.type": "record_processing",
  "service.name": "FlinkJobSimulator",
  "service.version": "1.0.0",
  "flink.version": "2.0"
}
```

### Log Categories

#### Operator Lifecycle
```csharp
_logger.LogOperatorLifecycle(LogLevel.Information, "MyOperator", "task-1", "started");
_logger.LogOperatorLifecycle(LogLevel.Information, "MyOperator", "task-1", "stopped");
```

#### Record Processing
```csharp
_logger.LogRecordProcessing(LogLevel.Debug, "MyOperator", "task-1", "processed", 
    recordCount: 100, processingTime: TimeSpan.FromMilliseconds(50));
```

#### Checkpointing
```csharp
_logger.LogCheckpoint(LogLevel.Information, "job-123", 1001, "completed",
    duration: TimeSpan.FromSeconds(2), sizeBytes: 1024000);
```

#### Performance Monitoring
```csharp
_logger.LogPerformance(LogLevel.Information, "MyOperator", "task-1", 
    "throughput", 1000.0, "records/sec");
```

### Contextual Logging

Create scoped loggers with additional context:

```csharp
var scopedLogger = _logger.WithContext(new Dictionary<string, object>
{
    ["batch.id"] = batchId,
    ["partition.id"] = partitionId,
    ["processing.mode"] = "exactly-once"
});

scopedLogger.LogRecordProcessing(LogLevel.Information, "MyOperator", "task-1", "batch_completed");
```

## Health Monitoring

### Health Check Categories

#### Operator Health
- Memory usage monitoring
- CPU utilization tracking
- Error rate analysis
- Processing latency validation

#### Job Health
- Execution status monitoring
- Checkpoint success rate tracking
- Throughput validation
- Resource consumption analysis

#### System Health
- Network communication validation
- State backend accessibility
- Resource availability checking

### Health Check Implementation

```csharp
// Register custom health checks
healthMonitor.RegisterCustomHealthCheck("kafka-connectivity", async cancellationToken =>
{
    try
    {
        // Test Kafka connectivity
        await TestKafkaConnection();
        
        return new FlinkHealthCheckResult
        {
            Status = FlinkHealthStatus.Healthy,
            ComponentName = "Kafka",
            Description = "Kafka connectivity is healthy",
            Data = new Dictionary<string, object>
            {
                ["brokers.available"] = availableBrokers,
                ["lag.ms"] = consumerLag
            }
        };
    }
    catch (Exception ex)
    {
        return new FlinkHealthCheckResult
        {
            Status = FlinkHealthStatus.Failed,
            ComponentName = "Kafka",
            Description = $"Kafka connectivity failed: {ex.Message}",
            Exception = ex
        };
    }
});
```

### Health Status Interpretation

- **Healthy**: Component operating within normal parameters
- **Degraded**: Component operational but performance reduced
- **Unhealthy**: Component experiencing issues but still functional
- **Failed**: Component non-operational, requires immediate attention

## Integration Guide

### FlinkJobSimulator Integration

Update your FlinkJobSimulator to use comprehensive observability:

```csharp
// Program.cs
public static async Task Main(string[] args)
{
    var builder = Host.CreateApplicationBuilder(args);
    
    // Add FlinkDotNet observability
    builder.Services.AddFlinkObservability("FlinkJobSimulator", options =>
    {
        options.EnableOperatorMonitoring = true;
        options.EnableJobMonitoring = true;
        options.MetricsIntervalSeconds = 10;
    });
    
    var host = builder.Build();
    
    // Get observability services
    var metrics = host.Services.GetRequiredService<IFlinkMetrics>();
    var tracing = host.Services.GetRequiredService<IFlinkTracing>();
    var logger = host.Services.GetRequiredService<IFlinkLogger>();
    var healthMonitor = host.Services.GetRequiredService<IFlinkHealthMonitor>();
    
    // Start job with full observability
    await RunJobWithObservability(metrics, tracing, logger, healthMonitor);
}
```

### Stress Test Integration

Configure stress tests with observability monitoring:

```powershell
# run-local-stress-tests.ps1
param(
    [switch]$EnableDetailedObservability,
    [int]$MessageCount = 1000,
    [int]$MaxTimeMs = 10000
)

# Set observability environment variables
$env:FLINK_OBSERVABILITY_ENABLE_CONSOLE_METRICS = "true"
$env:FLINK_OBSERVABILITY_ENABLE_DETAILED_TRACING = $EnableDetailedObservability.ToString().ToLower()
$env:FLINK_OBSERVABILITY_METRICS_INTERVAL = "5"
$env:FLINK_OBSERVABILITY_HEALTH_INTERVAL = "10"

# Start stress test with observability
Write-Host "🔍 Starting stress test with enhanced observability..."
```

### Reliability Test Integration

Enhance reliability tests with comprehensive monitoring:

```csharp
public class ReliabilityTestWithObservability
{
    private readonly IFlinkMetrics _metrics;
    private readonly IFlinkLogger _logger;
    private readonly IFlinkHealthMonitor _healthMonitor;
    
    public async Task RunReliabilityTest()
    {
        _logger.LogJobEvent(LogLevel.Information, "reliability-test", "ReliabilityTest", "started");
        
        using var jobSpan = _tracing.StartJobSpan("ReliabilityTest");
        
        try
        {
            // Monitor health throughout the test
            var healthTask = MonitorHealthContinuously();
            
            // Run test with metrics collection
            await RunTestWithMetrics();
            
            _logger.LogJobEvent(LogLevel.Information, "reliability-test", "ReliabilityTest", "completed");
        }
        catch (Exception ex)
        {
            _metrics.RecordError("ReliabilityTest", "main", ex.GetType().Name);
            _logger.LogError("ReliabilityTest", "main", ex, "test_execution");
            throw;
        }
    }
}
```

## Best Practices

### Performance Optimization

1. **Async Metrics Collection**: Use async patterns to avoid blocking main processing threads
2. **Batching**: Batch metric updates to reduce overhead
3. **Sampling**: Use trace sampling in high-throughput scenarios
4. **Resource Limits**: Set appropriate limits for log retention and metric storage

### Error Handling

1. **Graceful Degradation**: Continue operations even if observability fails
2. **Circuit Breakers**: Implement circuit breakers for external observability systems
3. **Fallback Strategies**: Provide local logging when remote systems are unavailable

### Security Considerations

1. **Sensitive Data**: Never log sensitive information (passwords, keys, PII)
2. **Access Control**: Implement proper access controls for observability data
3. **Data Retention**: Follow compliance requirements for log and metric retention

### Monitoring Strategies

1. **Golden Metrics**: Focus on throughput, latency, errors, and saturation
2. **Alerting**: Set up alerts for critical thresholds
3. **Dashboards**: Create comprehensive dashboards for different stakeholders

## Troubleshooting

### Common Issues

#### High Memory Usage
```csharp
// Monitor memory usage
_logger.LogPerformance(LogLevel.Warning, "System", "memory", 
    "heap_usage", GC.GetTotalMemory(false) / 1024 / 1024, "MB");

// Check for memory leaks in metrics collection
_healthMonitor.CheckResourceHealthAsync();
```

#### Missing Traces
```csharp
// Verify trace context propagation
var traceId = _tracing.GetCurrentTraceContext();
if (string.IsNullOrEmpty(traceId))
{
    _logger.LogError("Tracing", "context", new Exception("Missing trace context"), "propagation");
}
```

#### Metric Collection Failures
```csharp
// Implement fallback metrics
try
{
    _metrics.RecordIncomingRecord("MyOperator", "task-1");
}
catch (Exception ex)
{
    // Fallback to local metrics
    _localMetrics.Increment("records_in");
    _logger.LogError("Metrics", "collection", ex, "fallback_used");
}
```

### Debugging Tips

1. **Enable Debug Logging**: Set log level to Debug for detailed information
2. **Trace Correlation**: Use trace IDs to correlate logs across components
3. **Health Checks**: Regularly run health checks to identify issues early
4. **Metric Validation**: Validate metric values for consistency

### Performance Tuning

1. **Metrics Interval**: Adjust collection intervals based on system load
2. **Trace Sampling**: Use appropriate sampling rates for production
3. **Log Filtering**: Filter out verbose logs in production environments
4. **Buffer Sizes**: Tune buffer sizes for optimal performance

## Migration Guide

### From Basic Logging to Structured Observability

```csharp
// Before: Basic console logging
Console.WriteLine($"Processing record {recordId}");

// After: Structured observability
_logger.LogRecordProcessing(LogLevel.Information, "MyOperator", "task-1", "processing",
    recordCount: 1, context: new Dictionary<string, object> { ["record.id"] = recordId });
_metrics.RecordIncomingRecord("MyOperator", "task-1");
```

### Integration Checklist

- [ ] Add FlinkDotNet.Core.Observability package reference
- [ ] Configure observability services in dependency injection
- [ ] Replace console logging with structured logging
- [ ] Add metrics collection to operators
- [ ] Implement distributed tracing
- [ ] Configure health checks
- [ ] Set up monitoring dashboards
- [ ] Configure alerting rules
- [ ] Test observability in staging environment
- [ ] Deploy to production with monitoring

## References

- [Apache Flink Metrics Documentation](https://flink.apache.org/docs/stable/ops/metrics/)
- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/languages/net/)
- [FlinkDotNet Core Documentation](../README.md)
- [Monitoring Best Practices](./monitoring-best-practices.md)

---

*This documentation is maintained as part of the FlinkDotNet project. For questions or contributions, please refer to the main project repository.*