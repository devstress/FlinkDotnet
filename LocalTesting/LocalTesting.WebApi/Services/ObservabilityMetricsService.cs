using System.Diagnostics.Metrics;
using System.Diagnostics;

namespace LocalTesting.WebApi.Services;

/// <summary>
/// Comprehensive observability metrics service for messages-per-second tracking
/// across Kafka, Flink, Temporal, and end-to-end flow layers
/// </summary>
public class ObservabilityMetricsService
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
    private readonly ObservableGauge<double> _flinkJobThroughput;
    
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
    
    private readonly ILogger<ObservabilityMetricsService> _logger;
    
    // Rate tracking for throughput calculations
    private readonly Dictionary<string, RateTracker> _rateTrackers = new();
    private readonly object _lock = new();

    public ObservabilityMetricsService(ILogger<ObservabilityMetricsService> logger)
    {
        _logger = logger;
        
        // Initialize meters for each layer
        _kafkaMeter = new Meter("FlinkDotNet.Kafka", "1.0.0");
        _flinkMeter = new Meter("FlinkDotNet.Flink", "1.0.0");
        _temporalMeter = new Meter("FlinkDotNet.Temporal", "1.0.0");
        _flowMeter = new Meter("FlinkDotNet.Flow", "1.0.0");
        
        // Initialize Kafka metrics
        _kafkaProducerMessagesTotal = _kafkaMeter.CreateCounter<long>(
            "kafka_producer_messages_total",
            "messages",
            "Total number of messages produced to Kafka");
        
        _kafkaConsumerMessagesTotal = _kafkaMeter.CreateCounter<long>(
            "kafka_consumer_messages_total", 
            "messages",
            "Total number of messages consumed from Kafka");
        
        _kafkaProducerBytesTotal = _kafkaMeter.CreateCounter<long>(
            "kafka_producer_bytes_total",
            "bytes", 
            "Total bytes produced to Kafka");
        
        _kafkaProducerLatency = _kafkaMeter.CreateHistogram<double>(
            "kafka_producer_latency_seconds",
            "seconds",
            "Kafka producer latency");
        
        // Initialize Flink metrics
        _flinkJobMessagesIn = _flinkMeter.CreateCounter<long>(
            "flink_job_messages_in_total",
            "messages",
            "Total messages input to Flink jobs");
        
        _flinkJobMessagesOut = _flinkMeter.CreateCounter<long>(
            "flink_job_messages_out_total", 
            "messages",
            "Total messages output from Flink jobs");
        
        _flinkJobLatency = _flinkMeter.CreateHistogram<double>(
            "flink_job_latency_seconds",
            "seconds",
            "Flink job processing latency");
        
        _flinkJobThroughput = _flinkMeter.CreateObservableGauge<double>(
            "flink_job_throughput_messages_per_second",
            () => GetMessagesPerSecond("flink_throughput_default"),
            "messages/second",
            "Flink job processing throughput");
        
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
        
        _logger.LogInformation("ObservabilityMetricsService initialized with comprehensive metrics for Kafka, Flink, Temporal, and Flow layers");
    }
    
    // Kafka Metrics
    public void RecordKafkaProducerMessage(string topic, string partition, long messageCount = 1, long bytes = 0)
    {
        var tags = new KeyValuePair<string, object?>[] { 
            new("topic", topic), 
            new("partition", partition) 
        };
        _kafkaProducerMessagesTotal.Add(messageCount, tags);
        if (bytes > 0)
        {
            _kafkaProducerBytesTotal.Add(bytes, tags);
        }
        
        UpdateRateTracker($"kafka_producer_{topic}_{partition}", messageCount);
    }
    
    public void RecordKafkaConsumerMessage(string topic, string partition, string consumerGroup, long messageCount = 1)
    {
        var tags = new KeyValuePair<string, object?>[] { 
            new("topic", topic), 
            new("partition", partition), 
            new("consumer_group", consumerGroup) 
        };
        _kafkaConsumerMessagesTotal.Add(messageCount, tags);
        
        UpdateRateTracker($"kafka_consumer_{topic}_{partition}_{consumerGroup}", messageCount);
    }
    
    public void RecordKafkaProducerLatency(string topic, double latencySeconds)
    {
        var tags = new KeyValuePair<string, object?>[] { new("topic", topic) };
        _kafkaProducerLatency.Record(latencySeconds, tags);
    }
    
    // Flink Metrics
    public void RecordFlinkJobMessageIn(string jobId, string operatorName, long messageCount = 1)
    {
        var tags = new KeyValuePair<string, object?>[] { 
            new("job_id", jobId), 
            new("operator", operatorName) 
        };
        _flinkJobMessagesIn.Add(messageCount, tags);
        
        UpdateRateTracker($"flink_in_{jobId}_{operatorName}", messageCount);
    }
    
    public void RecordFlinkJobMessageOut(string jobId, string operatorName, long messageCount = 1)
    {
        var tags = new KeyValuePair<string, object?>[] { 
            new("job_id", jobId), 
            new("operator", operatorName) 
        };
        _flinkJobMessagesOut.Add(messageCount, tags);
        
        UpdateRateTracker($"flink_out_{jobId}_{operatorName}", messageCount);
    }
    
    public void RecordFlinkJobLatency(string jobId, double latencySeconds)
    {
        var tags = new KeyValuePair<string, object?>[] { new("job_id", jobId) };
        _flinkJobLatency.Record(latencySeconds, tags);
    }
    
    // Temporal Metrics
    public void RecordTemporalWorkflowExecution(string workflowType)
    {
        var tags = new KeyValuePair<string, object?>[] { new("workflow_type", workflowType) };
        _temporalWorkflowExecutions.Add(1, tags);
        
        UpdateRateTracker($"temporal_workflow_{workflowType}", 1);
    }
    
    public void RecordTemporalActivityExecution(string activityType)
    {
        var tags = new KeyValuePair<string, object?>[] { new("activity_type", activityType) };
        _temporalActivityExecutions.Add(1, tags);
        
        UpdateRateTracker($"temporal_activity_{activityType}", 1);
    }
    
    public void RecordTemporalWorkflowDuration(string workflowType, double durationSeconds)
    {
        var tags = new KeyValuePair<string, object?>[] { new("workflow_type", workflowType) };
        _temporalWorkflowDuration.Record(durationSeconds, tags);
    }
    
    public void RecordTemporalWorkflowCompletion(string workflowType)
    {
        var tags = new KeyValuePair<string, object?>[] { new("workflow_type", workflowType) };
        _temporalWorkflowCompletions.Add(1, tags);
    }
    
    // End-to-End Flow Metrics
    public void RecordFlowKafkaToFlink(long messageCount = 1)
    {
        _flowMessagesKafkaToFlink.Add(messageCount);
        UpdateRateTracker("flow_kafka_to_flink", messageCount);
    }
    
    public void RecordFlowFlinkToTemporal(long messageCount = 1)
    {
        _flowMessagesFlinkToTemporal.Add(messageCount);
        UpdateRateTracker("flow_flink_to_temporal", messageCount);
    }
    
    public void RecordFlowEndToEnd(long messageCount = 1)
    {
        _flowMessagesEndToEnd.Add(messageCount);
        UpdateRateTracker("flow_end_to_end", messageCount);
    }
    
    public void RecordFlowEndToEndLatency(double latencySeconds)
    {
        _flowLatencyEndToEnd.Record(latencySeconds);
    }
    
    // Rate tracking for messages-per-second calculations
    private void UpdateRateTracker(string key, long messageCount)
    {
        lock (_lock)
        {
            if (!_rateTrackers.ContainsKey(key))
            {
                _rateTrackers[key] = new RateTracker();
            }
            _rateTrackers[key].AddMessages(messageCount);
        }
    }
    
    public double GetMessagesPerSecond(string key)
    {
        lock (_lock)
        {
            return _rateTrackers.TryGetValue(key, out var tracker) ? tracker.GetRate() : 0.0;
        }
    }
    
    public Dictionary<string, double> GetAllMessagesPerSecondRates()
    {
        lock (_lock)
        {
            var rates = new Dictionary<string, double>();
            foreach (var kvp in _rateTrackers)
            {
                rates[kvp.Key] = kvp.Value.GetRate();
            }
            return rates;
        }
    }
}

/// <summary>
/// Helper class to track message rates over time windows
/// </summary>
internal class RateTracker
{
    private readonly Queue<(DateTime timestamp, long messageCount)> _measurements = new();
    private readonly TimeSpan _windowSize = TimeSpan.FromSeconds(30); // 30-second rolling window for better test responsiveness
    
    public void AddMessages(long messageCount)
    {
        var now = DateTime.UtcNow;
        _measurements.Enqueue((now, messageCount));
        
        // Remove old measurements outside the window
        while (_measurements.Count > 0 && now - _measurements.Peek().timestamp > _windowSize)
        {
            _measurements.Dequeue();
        }
    }
    
    public double GetRate()
    {
        if (_measurements.Count == 0) return 0.0;
        
        var now = DateTime.UtcNow;
        var totalMessages = 0L;
        var oldestTimestamp = now;
        
        foreach (var (timestamp, messageCount) in _measurements)
        {
            if (now - timestamp <= _windowSize)
            {
                totalMessages += messageCount;
                if (timestamp < oldestTimestamp)
                    oldestTimestamp = timestamp;
            }
        }
        
        var windowDuration = (now - oldestTimestamp).TotalSeconds;
        
        // For testing scenarios, if we have recent activity but very short duration,
        // calculate rate based on a minimum 1-second window to avoid infinity
        var effectiveWindow = Math.Max(windowDuration, 1.0);
        return totalMessages / effectiveWindow;
    }
}