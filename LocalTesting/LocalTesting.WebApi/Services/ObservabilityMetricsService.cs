using System.Diagnostics;
using Prometheus;

namespace LocalTesting.WebApi.Services;

/// <summary>
/// Comprehensive observability metrics service for messages-per-second tracking
/// across Kafka, Flink, Temporal, and end-to-end flow layers
/// FIXED: Uses native Prometheus metrics (not OpenTelemetry) to properly expose to /metrics endpoint
/// </summary>
public class ObservabilityMetricsService
{
    private readonly ILogger<ObservabilityMetricsService> _logger;
    
    // Native Prometheus Kafka Metrics - properly exposed to /metrics endpoint
    private readonly Counter _kafkaProducerMessagesTotal;
    private readonly Counter _kafkaConsumerMessagesTotal;
    private readonly Counter _kafkaProducerBytesTotal;
    private readonly Histogram _kafkaProducerLatency;
    
    // Native Prometheus Flink Metrics - properly exposed to /metrics endpoint
    private readonly Counter _flinkJobMessagesIn;
    private readonly Counter _flinkJobMessagesOut;
    private readonly Histogram _flinkJobLatency;
    
    // Native Prometheus Temporal Metrics - properly exposed to /metrics endpoint
    private readonly Counter _temporalWorkflowExecutions;
    private readonly Counter _temporalActivityExecutions;
    private readonly Histogram _temporalWorkflowDuration;
    private readonly Counter _temporalWorkflowCompletions;
    
    // Native Prometheus End-to-End Flow Metrics - properly exposed to /metrics endpoint
    private readonly Counter _flowMessagesKafkaToFlink;
    private readonly Counter _flowMessagesFlinkToTemporal;
    private readonly Counter _flowMessagesEndToEnd;
    private readonly Histogram _flowLatencyEndToEnd;

    public ObservabilityMetricsService(ILogger<ObservabilityMetricsService> logger)
    {
        _logger = logger;
        
        _logger.LogInformation("📊 Initializing native Prometheus metrics (FIXED: no OpenTelemetry)");
        
        // Initialize native Prometheus Kafka metrics with proper labels for Prometheus scraping
        _kafkaProducerMessagesTotal = Metrics.CreateCounter(
            "kafka_producer_messages_total",
            "Total number of messages produced to Kafka",
            new[] { "topic", "partition" });
        
        _kafkaConsumerMessagesTotal = Metrics.CreateCounter(
            "kafka_consumer_messages_total", 
            "Total number of messages consumed from Kafka",
            new[] { "topic", "partition" });
        
        _kafkaProducerBytesTotal = Metrics.CreateCounter(
            "kafka_producer_bytes_total",
            "Total bytes produced to Kafka",
            new[] { "topic", "partition" });
        
        _kafkaProducerLatency = Metrics.CreateHistogram(
            "kafka_producer_latency_seconds",
            "Kafka producer latency in seconds",
            new[] { "topic", "partition" });
        
        // Initialize native Prometheus Flink metrics with proper labels for Prometheus scraping
        _flinkJobMessagesIn = Metrics.CreateCounter(
            "flink_job_messages_in_total",
            "Total messages input to Flink jobs",
            new[] { "job_id", "operator" });
        
        _flinkJobMessagesOut = Metrics.CreateCounter(
            "flink_job_messages_out_total", 
            "Total messages output from Flink jobs",
            new[] { "job_id", "operator" });
        
        _flinkJobLatency = Metrics.CreateHistogram(
            "flink_job_latency_seconds",
            "Flink job processing latency in seconds",
            new[] { "job_id" });
        
        // Initialize native Prometheus Temporal metrics with proper labels for Prometheus scraping
        _temporalWorkflowExecutions = Metrics.CreateCounter(
            "temporal_workflow_executions_total",
            "Total Temporal workflow executions",
            new[] { "workflow_type" });
        
        _temporalActivityExecutions = Metrics.CreateCounter(
            "temporal_activity_executions_total",
            "Total Temporal activity executions",
            new[] { "activity_type" });
        
        _temporalWorkflowDuration = Metrics.CreateHistogram(
            "temporal_workflow_duration_seconds",
            "Temporal workflow execution duration in seconds",
            new[] { "workflow_type" });
        
        _temporalWorkflowCompletions = Metrics.CreateCounter(
            "temporal_workflow_completions_total",
            "Total Temporal workflow completions",
            new[] { "workflow_type" });
        
        // Initialize native Prometheus end-to-end flow metrics with proper labels for Prometheus scraping
        _flowMessagesKafkaToFlink = Metrics.CreateCounter(
            "flow_messages_kafka_to_flink_total",
            "Messages flowing from Kafka to Flink");
        
        _flowMessagesFlinkToTemporal = Metrics.CreateCounter(
            "flow_messages_flink_to_temporal_total",
            "Messages flowing from Flink to Temporal");
        
        _flowMessagesEndToEnd = Metrics.CreateCounter(
            "flow_messages_end_to_end_total",
            "Messages processed end-to-end through entire flow");
        
        _flowLatencyEndToEnd = Metrics.CreateHistogram(
            "flow_latency_end_to_end_seconds",
            "End-to-end message processing latency in seconds");
        
        _logger.LogInformation("✅ ObservabilityMetricsService initialized with native Prometheus metrics (FIXED: properly exposed to /metrics endpoint)");
    }
    
    // Kafka Metrics - FIXED: Use native Prometheus API calls
    public void RecordKafkaProducerMessage(string topic, string partition, long messageCount = 1, long bytes = 0)
    {
        _kafkaProducerMessagesTotal.WithLabels(topic, partition).Inc(messageCount);
        if (bytes > 0)
        {
            _kafkaProducerBytesTotal.WithLabels(topic, partition).Inc(bytes);
        }
        _logger.LogDebug("📊 Kafka producer message recorded: {Topic}:{Partition} = {MessageCount} messages, {Bytes} bytes", 
            topic, partition, messageCount, bytes);
    }
    
    public void RecordKafkaConsumerMessage(string topic, string partition, string consumerGroup, long messageCount = 1)
    {
        _kafkaConsumerMessagesTotal.WithLabels(topic, partition).Inc(messageCount);
        _logger.LogDebug("📊 Kafka consumer message recorded: {Topic}:{Partition} = {MessageCount} messages", 
            topic, partition, messageCount);
    }
    
    public void RecordKafkaProducerLatency(string topic, double latencySeconds)
    {
        _kafkaProducerLatency.WithLabels(topic, "").Observe(latencySeconds);
        _logger.LogDebug("📊 Kafka producer latency recorded: {Topic} = {Latency:F4}s", topic, latencySeconds);
    }
    
    // Flink Metrics - FIXED: Use native Prometheus API calls
    public void RecordFlinkJobMessageIn(string jobId, string operatorName, long messageCount = 1)
    {
        _flinkJobMessagesIn.WithLabels(jobId, operatorName).Inc(messageCount);
        _logger.LogDebug("📊 Flink job input recorded: {JobId}:{Operator} = {MessageCount} messages", 
            jobId, operatorName, messageCount);
    }
    
    public void RecordFlinkJobMessageOut(string jobId, string operatorName, long messageCount = 1)
    {
        _flinkJobMessagesOut.WithLabels(jobId, operatorName).Inc(messageCount);
        _logger.LogDebug("📊 Flink job output recorded: {JobId}:{Operator} = {MessageCount} messages", 
            jobId, operatorName, messageCount);
    }
    
    public void RecordFlinkJobLatency(string jobId, double latencySeconds)
    {
        _flinkJobLatency.WithLabels(jobId).Observe(latencySeconds);
        _logger.LogDebug("📊 Flink job latency recorded: {JobId} = {Latency:F4}s", jobId, latencySeconds);
    }
    
    // Temporal Metrics - FIXED: Use native Prometheus API calls  
    public void RecordTemporalWorkflowExecution(string workflowType)
    {
        _temporalWorkflowExecutions.WithLabels(workflowType).Inc();
        _logger.LogDebug("📊 Temporal workflow execution recorded: {WorkflowType}", workflowType);
    }
    
    public void RecordTemporalActivityExecution(string activityType)
    {
        _temporalActivityExecutions.WithLabels(activityType).Inc();
        _logger.LogDebug("📊 Temporal activity execution recorded: {ActivityType}", activityType);
    }
    
    public void RecordTemporalWorkflowDuration(string workflowType, double durationSeconds)
    {
        _temporalWorkflowDuration.WithLabels(workflowType).Observe(durationSeconds);
        _logger.LogDebug("📊 Temporal workflow duration recorded: {WorkflowType} = {Duration:F4}s", workflowType, durationSeconds);
    }
    
    public void RecordTemporalWorkflowCompletion(string workflowType)
    {
        _temporalWorkflowCompletions.WithLabels(workflowType).Inc();
        _logger.LogDebug("📊 Temporal workflow completion recorded: {WorkflowType}", workflowType);
    }
    
    // End-to-End Flow Metrics - FIXED: Use native Prometheus API calls
    public void RecordFlowKafkaToFlink(long messageCount = 1)
    {
        _flowMessagesKafkaToFlink.Inc(messageCount);
        _logger.LogDebug("📊 Kafka→Flink flow recorded: {MessageCount} messages", messageCount);
    }
    
    public void RecordFlowFlinkToTemporal(long messageCount = 1)
    {
        _flowMessagesFlinkToTemporal.Inc(messageCount);
        _logger.LogDebug("📊 Flink→Temporal flow recorded: {MessageCount} messages", messageCount);
    }
    
    public void RecordFlowEndToEnd(long messageCount = 1)
    {
        _flowMessagesEndToEnd.Inc(messageCount);
        _logger.LogDebug("📊 End-to-end flow recorded: {MessageCount} messages", messageCount);
    }
    
    public void RecordFlowEndToEndLatency(double latencySeconds)
    {
        _flowLatencyEndToEnd.Observe(latencySeconds);
        _logger.LogDebug("📊 End-to-end latency recorded: {Latency:F4}s", latencySeconds);
    }
}