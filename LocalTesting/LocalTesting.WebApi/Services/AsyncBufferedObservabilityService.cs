using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Diagnostics;

namespace LocalTesting.WebApi.Services;

/// <summary>
/// High-performance async buffered observability metrics service
/// Implements OpenTelemetry Collector pattern: App → local OTel Collector → backend
/// Designed to eliminate latency during message production through local buffering
/// </summary>
public class AsyncBufferedObservabilityService : IDisposable
{
    // OpenTelemetry Meters for each layer
    private readonly Meter _kafkaMeter;
    private readonly Meter _flinkMeter;
    private readonly Meter _temporalMeter;
    private readonly Meter _flowMeter;
    
    // Kafka Metrics
    private readonly Counter<long> _kafkaProducerMessagesTotal;
    private readonly Counter<long> _kafkaConsumerMessagesTotal;
    private readonly Counter<long> _kafkaProducerBytesTotal;
    private readonly Histogram<double> _kafkaProducerLatency;
    
    // Flink Metrics
    private readonly Counter<long> _flinkJobMessagesIn;
    private readonly Counter<long> _flinkJobMessagesOut;
    private readonly Histogram<double> _flinkJobLatency;
    
    // Temporal Metrics
    private readonly Counter<long> _temporalWorkflowExecutions;
    private readonly Counter<long> _temporalActivityExecutions;
    private readonly Histogram<double> _temporalWorkflowDuration;
    private readonly Counter<long> _temporalWorkflowCompletions;
    
    // End-to-End Flow Metrics
    private readonly Counter<long> _flowMessagesKafkaToFlink;
    private readonly Counter<long> _flowMessagesFlinkToTemporal;
    private readonly Counter<long> _flowMessagesEndToEnd;
    private readonly Histogram<double> _flowLatencyEndToEnd;
    
    private readonly ILogger<AsyncBufferedObservabilityService> _logger;
    
    // High-performance async buffering system
    private readonly ConcurrentQueue<MetricEvent> _metricsBuffer;
    private readonly Timer _flushTimer;
    private readonly SemaphoreSlim _flushSemaphore;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _backgroundFlushTask;
    
    // Performance counters for monitoring buffer health
    private long _totalMetricsBuffered;
    private long _totalMetricsFlushed;
    private long _bufferOverflows;
    
    private const int MaxBufferSize = 10000; // Large buffer for high-volume scenarios
    private const int FlushIntervalMs = 1000; // Flush every 1 second
    private const int BatchSize = 500; // Process metrics in batches

    public AsyncBufferedObservabilityService(ILogger<AsyncBufferedObservabilityService> logger)
    {
        _logger = logger;
        
        // Initialize OpenTelemetry meters for each observability layer
        _kafkaMeter = new Meter("FlinkDotNet.Kafka", "1.0.0");
        _flinkMeter = new Meter("FlinkDotNet.Flink", "1.0.0");
        _temporalMeter = new Meter("FlinkDotNet.Temporal", "1.0.0");
        _flowMeter = new Meter("FlinkDotNet.Flow", "1.0.0");
        
        // Initialize Kafka metrics
        _kafkaProducerMessagesTotal = _kafkaMeter.CreateCounter<long>(
            "kafka_producer_messages_total",
            "messages",
            "Total Kafka producer messages sent");
        
        _kafkaConsumerMessagesTotal = _kafkaMeter.CreateCounter<long>(
            "kafka_consumer_messages_total", 
            "messages",
            "Total Kafka consumer messages received");
        
        _kafkaProducerBytesTotal = _kafkaMeter.CreateCounter<long>(
            "kafka_producer_bytes_total",
            "bytes",
            "Total bytes sent by Kafka producers");
        
        _kafkaProducerLatency = _kafkaMeter.CreateHistogram<double>(
            "kafka_producer_latency_seconds",
            "seconds",
            "Kafka producer message latency");
        
        // Initialize Flink metrics
        _flinkJobMessagesIn = _flinkMeter.CreateCounter<long>(
            "flink_job_messages_in_total",
            "messages",
            "Total messages received by Flink jobs");
        
        _flinkJobMessagesOut = _flinkMeter.CreateCounter<long>(
            "flink_job_messages_out_total",
            "messages", 
            "Total messages output by Flink jobs");
        
        _flinkJobLatency = _flinkMeter.CreateHistogram<double>(
            "flink_job_latency_seconds",
            "seconds",
            "Flink job processing latency");
        
        // Initialize Temporal metrics
        _temporalWorkflowExecutions = _temporalMeter.CreateCounter<long>(
            "temporal_workflow_executions_total",
            "executions",
            "Total Temporal workflow executions");
        
        _temporalActivityExecutions = _temporalMeter.CreateCounter<long>(
            "temporal_activity_executions_total",
            "executions", 
            "Total Temporal activity executions");
        
        _temporalWorkflowDuration = _temporalMeter.CreateHistogram<double>(
            "temporal_workflow_duration_seconds",
            "seconds",
            "Temporal workflow execution duration");
        
        _temporalWorkflowCompletions = _temporalMeter.CreateCounter<long>(
            "temporal_workflow_completions_total",
            "completions",
            "Total Temporal workflow completions");
        
        // Initialize end-to-end flow metrics
        _flowMessagesKafkaToFlink = _flowMeter.CreateCounter<long>(
            "flow_messages_kafka_to_flink_total",
            "messages",
            "Messages flowing from Kafka to Flink");
        
        _flowMessagesFlinkToTemporal = _flowMeter.CreateCounter<long>(
            "flow_messages_flink_to_temporal_total",
            "messages", 
            "Messages flowing from Flink to Temporal");
        
        _flowMessagesEndToEnd = _flowMeter.CreateCounter<long>(
            "flow_messages_end_to_end_total",
            "messages",
            "Messages processed end-to-end through entire flow");
        
        _flowLatencyEndToEnd = _flowMeter.CreateHistogram<double>(
            "flow_latency_end_to_end_seconds",
            "seconds",
            "End-to-end message processing latency");
        
        // Initialize high-performance async buffering system
        _metricsBuffer = new ConcurrentQueue<MetricEvent>();
        _flushSemaphore = new SemaphoreSlim(1, 1);
        _cancellationTokenSource = new CancellationTokenSource();
        
        // Start background flush timer for regular metric processing
        _flushTimer = new Timer(FlushMetricsCallback, null, FlushIntervalMs, FlushIntervalMs);
        
        // Start background task for continuous metric processing
        _backgroundFlushTask = Task.Run(BackgroundFlushLoop, _cancellationTokenSource.Token);
        
        _logger.LogInformation("AsyncBufferedObservabilityService initialized with high-performance async buffering. " +
                              "Buffer size: {MaxBufferSize}, Flush interval: {FlushIntervalMs}ms, Batch size: {BatchSize}",
                              MaxBufferSize, FlushIntervalMs, BatchSize);
    }
    
    // KAFKA METRICS (Non-blocking async buffering)
    public void RecordKafkaProducerMessage(string topic, string partition, long messageCount = 1, long bytes = 0)
    {
        // FIRE-AND-FORGET: Buffer metric without blocking message production
        var metricEvent = new MetricEvent
        {
            Type = MetricType.KafkaProducerMessage,
            Topic = topic,
            Partition = partition,
            MessageCount = messageCount,
            Bytes = bytes,
            Timestamp = DateTime.UtcNow
        };
        
        BufferMetricEvent(metricEvent);
    }
    
    public void RecordKafkaConsumerMessage(string topic, string partition, string consumerGroup, long messageCount = 1)
    {
        var metricEvent = new MetricEvent
        {
            Type = MetricType.KafkaConsumerMessage,
            Topic = topic,
            Partition = partition,
            ConsumerGroup = consumerGroup,
            MessageCount = messageCount,
            Timestamp = DateTime.UtcNow
        };
        
        BufferMetricEvent(metricEvent);
    }
    
    public void RecordKafkaProducerLatency(string topic, double latencySeconds)
    {
        var metricEvent = new MetricEvent
        {
            Type = MetricType.KafkaProducerLatency,
            Topic = topic,
            LatencySeconds = latencySeconds,
            Timestamp = DateTime.UtcNow
        };
        
        BufferMetricEvent(metricEvent);
    }
    
    // FLINK METRICS (Non-blocking async buffering)
    public void RecordFlinkJobMessageIn(string jobId, string operatorName, long messageCount = 1)
    {
        var metricEvent = new MetricEvent
        {
            Type = MetricType.FlinkJobMessageIn,
            JobId = jobId,
            OperatorName = operatorName,
            MessageCount = messageCount,
            Timestamp = DateTime.UtcNow
        };
        
        BufferMetricEvent(metricEvent);
    }
    
    public void RecordFlinkJobMessageOut(string jobId, string operatorName, long messageCount = 1)
    {
        var metricEvent = new MetricEvent
        {
            Type = MetricType.FlinkJobMessageOut,
            JobId = jobId,
            OperatorName = operatorName,
            MessageCount = messageCount,
            Timestamp = DateTime.UtcNow
        };
        
        BufferMetricEvent(metricEvent);
    }
    
    // TEMPORAL METRICS (Non-blocking async buffering)
    public void RecordTemporalWorkflowExecution(string workflowType, string workflowId)
    {
        var metricEvent = new MetricEvent
        {
            Type = MetricType.TemporalWorkflowExecution,
            WorkflowType = workflowType,
            WorkflowId = workflowId,
            Timestamp = DateTime.UtcNow
        };
        
        BufferMetricEvent(metricEvent);
    }
    
    public void RecordTemporalActivityExecution(string activityType, string workflowId)
    {
        var metricEvent = new MetricEvent
        {
            Type = MetricType.TemporalActivityExecution,
            ActivityType = activityType,
            WorkflowId = workflowId,
            Timestamp = DateTime.UtcNow
        };
        
        BufferMetricEvent(metricEvent);
    }
    
    // FLOW METRICS (Non-blocking async buffering)
    public void RecordFlowKafkaToFlink(long messageCount = 1)
    {
        var metricEvent = new MetricEvent
        {
            Type = MetricType.FlowKafkaToFlink,
            MessageCount = messageCount,
            Timestamp = DateTime.UtcNow
        };
        
        BufferMetricEvent(metricEvent);
    }
    
    public void RecordFlowFlinkToTemporal(long messageCount = 1)
    {
        var metricEvent = new MetricEvent
        {
            Type = MetricType.FlowFlinkToTemporal,
            MessageCount = messageCount,
            Timestamp = DateTime.UtcNow
        };
        
        BufferMetricEvent(metricEvent);
    }
    
    public void RecordFlowEndToEnd(long messageCount = 1)
    {
        var metricEvent = new MetricEvent
        {
            Type = MetricType.FlowEndToEnd,
            MessageCount = messageCount,
            Timestamp = DateTime.UtcNow
        };
        
        BufferMetricEvent(metricEvent);
    }
    
    public void RecordFlowEndToEndLatency(double latencySeconds)
    {
        var metricEvent = new MetricEvent
        {
            Type = MetricType.FlowEndToEndLatency,
            LatencySeconds = latencySeconds,
            Timestamp = DateTime.UtcNow
        };
        
        BufferMetricEvent(metricEvent);
    }
    
    // INTERNAL BUFFERING SYSTEM
    private void BufferMetricEvent(MetricEvent metricEvent)
    {
        // Check buffer capacity to prevent memory issues
        if (_metricsBuffer.Count >= MaxBufferSize)
        {
            Interlocked.Increment(ref _bufferOverflows);
            _logger.LogWarning("Metrics buffer overflow. Dropping metric event. Total overflows: {BufferOverflows}", 
                              _bufferOverflows);
            return;
        }
        
        _metricsBuffer.Enqueue(metricEvent);
        Interlocked.Increment(ref _totalMetricsBuffered);
    }
    
    private void FlushMetricsCallback(object? state)
    {
        // Timer-based flush - non-blocking trigger
        _ = Task.Run(async () => await FlushBufferedMetricsAsync());
    }
    
    private async Task BackgroundFlushLoop()
    {
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                await FlushBufferedMetricsAsync();
                await Task.Delay(FlushIntervalMs / 2, _cancellationTokenSource.Token); // Check twice per flush interval
            }
            catch (OperationCanceledException)
            {
                break; // Expected during shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background flush loop");
                await Task.Delay(1000, _cancellationTokenSource.Token); // Wait before retry
            }
        }
    }
    
    private async Task FlushBufferedMetricsAsync()
    {
        if (!await _flushSemaphore.WaitAsync(100))
        {
            return; // Skip if another flush is in progress
        }
        
        try
        {
            var metricsToFlush = new List<MetricEvent>();
            
            // Dequeue metrics in batches for efficient processing
            for (int i = 0; i < BatchSize && _metricsBuffer.TryDequeue(out var metricEvent); i++)
            {
                metricsToFlush.Add(metricEvent);
            }
            
            if (metricsToFlush.Count == 0)
            {
                return; // No metrics to flush
            }
            
            // Process metrics by type for efficient OpenTelemetry recording
            await ProcessMetricsBatch(metricsToFlush);
            
            Interlocked.Add(ref _totalMetricsFlushed, metricsToFlush.Count);
            
            _logger.LogDebug("Flushed {MetricCount} metrics. Total buffered: {TotalBuffered}, Total flushed: {TotalFlushed}, Buffer overflows: {BufferOverflows}", 
                            metricsToFlush.Count, _totalMetricsBuffered, _totalMetricsFlushed, _bufferOverflows);
        }
        finally
        {
            _flushSemaphore.Release();
        }
    }
    
    private async Task ProcessMetricsBatch(List<MetricEvent> metrics)
    {
        // Group metrics by type for efficient OpenTelemetry processing
        var metricsByType = metrics.GroupBy(m => m.Type);
        
        foreach (var typeGroup in metricsByType)
        {
            try
            {
                switch (typeGroup.Key)
                {
                    case MetricType.KafkaProducerMessage:
                        ProcessKafkaProducerMessages(typeGroup);
                        break;
                    case MetricType.KafkaConsumerMessage:
                        ProcessKafkaConsumerMessages(typeGroup);
                        break;
                    case MetricType.KafkaProducerLatency:
                        ProcessKafkaProducerLatencies(typeGroup);
                        break;
                    case MetricType.FlinkJobMessageIn:
                        ProcessFlinkJobMessagesIn(typeGroup);
                        break;
                    case MetricType.FlinkJobMessageOut:
                        ProcessFlinkJobMessagesOut(typeGroup);
                        break;
                    case MetricType.TemporalWorkflowExecution:
                        ProcessTemporalWorkflowExecutions(typeGroup);
                        break;
                    case MetricType.TemporalActivityExecution:
                        ProcessTemporalActivityExecutions(typeGroup);
                        break;
                    case MetricType.FlowKafkaToFlink:
                        ProcessFlowKafkaToFlink(typeGroup);
                        break;
                    case MetricType.FlowFlinkToTemporal:
                        ProcessFlowFlinkToTemporal(typeGroup);
                        break;
                    case MetricType.FlowEndToEnd:
                        ProcessFlowEndToEnd(typeGroup);
                        break;
                    case MetricType.FlowEndToEndLatency:
                        ProcessFlowEndToEndLatency(typeGroup);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing metrics batch for type {MetricType}", typeGroup.Key);
            }
        }
        
        // Small delay to allow OpenTelemetry to process the metrics
        await Task.Delay(1);
    }
    
    private void ProcessKafkaProducerMessages(IGrouping<MetricType, MetricEvent> metrics)
    {
        foreach (var metric in metrics)
        {
            var tags = new KeyValuePair<string, object?>[] { 
                new("topic", metric.Topic), 
                new("partition", metric.Partition) 
            };
            _kafkaProducerMessagesTotal.Add(metric.MessageCount, tags);
            
            if (metric.Bytes > 0)
            {
                _kafkaProducerBytesTotal.Add(metric.Bytes, tags);
            }
        }
    }
    
    private void ProcessKafkaConsumerMessages(IGrouping<MetricType, MetricEvent> metrics)
    {
        foreach (var metric in metrics)
        {
            var tags = new KeyValuePair<string, object?>[] { 
                new("topic", metric.Topic), 
                new("partition", metric.Partition), 
                new("consumer_group", metric.ConsumerGroup) 
            };
            _kafkaConsumerMessagesTotal.Add(metric.MessageCount, tags);
        }
    }
    
    private void ProcessKafkaProducerLatencies(IGrouping<MetricType, MetricEvent> metrics)
    {
        foreach (var metric in metrics)
        {
            var tags = new KeyValuePair<string, object?>[] { new("topic", metric.Topic) };
            _kafkaProducerLatency.Record(metric.LatencySeconds, tags);
        }
    }
    
    private void ProcessFlinkJobMessagesIn(IGrouping<MetricType, MetricEvent> metrics)
    {
        foreach (var metric in metrics)
        {
            var tags = new KeyValuePair<string, object?>[] { 
                new("job_id", metric.JobId), 
                new("operator", metric.OperatorName) 
            };
            _flinkJobMessagesIn.Add(metric.MessageCount, tags);
        }
    }
    
    private void ProcessFlinkJobMessagesOut(IGrouping<MetricType, MetricEvent> metrics)
    {
        foreach (var metric in metrics)
        {
            var tags = new KeyValuePair<string, object?>[] { 
                new("job_id", metric.JobId), 
                new("operator", metric.OperatorName) 
            };
            _flinkJobMessagesOut.Add(metric.MessageCount, tags);
        }
    }
    
    private void ProcessTemporalWorkflowExecutions(IGrouping<MetricType, MetricEvent> metrics)
    {
        foreach (var metric in metrics)
        {
            var tags = new KeyValuePair<string, object?>[] { 
                new("workflow_type", metric.WorkflowType), 
                new("workflow_id", metric.WorkflowId) 
            };
            _temporalWorkflowExecutions.Add(1, tags);
        }
    }
    
    private void ProcessTemporalActivityExecutions(IGrouping<MetricType, MetricEvent> metrics)
    {
        foreach (var metric in metrics)
        {
            var tags = new KeyValuePair<string, object?>[] { 
                new("activity_type", metric.ActivityType), 
                new("workflow_id", metric.WorkflowId) 
            };
            _temporalActivityExecutions.Add(1, tags);
        }
    }
    
    private void ProcessFlowKafkaToFlink(IGrouping<MetricType, MetricEvent> metrics)
    {
        foreach (var metric in metrics)
        {
            _flowMessagesKafkaToFlink.Add(metric.MessageCount);
        }
    }
    
    private void ProcessFlowFlinkToTemporal(IGrouping<MetricType, MetricEvent> metrics)
    {
        foreach (var metric in metrics)
        {
            _flowMessagesFlinkToTemporal.Add(metric.MessageCount);
        }
    }
    
    private void ProcessFlowEndToEnd(IGrouping<MetricType, MetricEvent> metrics)
    {
        foreach (var metric in metrics)
        {
            _flowMessagesEndToEnd.Add(metric.MessageCount);
        }
    }
    
    private void ProcessFlowEndToEndLatency(IGrouping<MetricType, MetricEvent> metrics)
    {
        foreach (var metric in metrics)
        {
            _flowLatencyEndToEnd.Record(metric.LatencySeconds);
        }
    }
    
    public void Dispose()
    {
        _logger.LogInformation("Disposing AsyncBufferedObservabilityService. Final flush of remaining metrics...");
        
        // Stop background processing
        _cancellationTokenSource.Cancel();
        _flushTimer?.Dispose();
        
        // Final flush of remaining metrics
        FlushBufferedMetricsAsync().GetAwaiter().GetResult();
        
        // Wait for background task to complete
        try
        {
            _backgroundFlushTask?.Wait(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error waiting for background flush task to complete");
        }
        
        // Dispose resources
        _flushSemaphore?.Dispose();
        _cancellationTokenSource?.Dispose();
        _kafkaMeter?.Dispose();
        _flinkMeter?.Dispose();
        _temporalMeter?.Dispose();
        _flowMeter?.Dispose();
        
        _logger.LogInformation("AsyncBufferedObservabilityService disposed. Total metrics: Buffered={TotalBuffered}, Flushed={TotalFlushed}, Overflows={BufferOverflows}",
                              _totalMetricsBuffered, _totalMetricsFlushed, _bufferOverflows);
    }
}

// Metric event data structure for buffering
internal class MetricEvent
{
    public MetricType Type { get; set; }
    public DateTime Timestamp { get; set; }
    
    // Kafka fields
    public string? Topic { get; set; }
    public string? Partition { get; set; }
    public string? ConsumerGroup { get; set; }
    public long MessageCount { get; set; }
    public long Bytes { get; set; }
    public double LatencySeconds { get; set; }
    
    // Flink fields
    public string? JobId { get; set; }
    public string? OperatorName { get; set; }
    
    // Temporal fields
    public string? WorkflowType { get; set; }
    public string? WorkflowId { get; set; }
    public string? ActivityType { get; set; }
}

internal enum MetricType
{
    KafkaProducerMessage,
    KafkaConsumerMessage,
    KafkaProducerLatency,
    FlinkJobMessageIn,
    FlinkJobMessageOut,
    TemporalWorkflowExecution,
    TemporalActivityExecution,
    FlowKafkaToFlink,
    FlowFlinkToTemporal,
    FlowEndToEnd,
    FlowEndToEndLatency
}