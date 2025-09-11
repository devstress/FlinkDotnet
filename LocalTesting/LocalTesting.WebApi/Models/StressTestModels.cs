namespace LocalTesting.WebApi.Models;

public class ComplexLogicMessage
{
    public long MessageId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string? SendingID { get; set; }
    public string? LogicalQueueName { get; set; }
    public string Payload { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int BatchNumber { get; set; }
    public int PartitionNumber { get; set; }
    public string? SecurityToken { get; set; }
    public string? ProcessingStage { get; set; } = "initial";
    public bool IsConcatenated { get; set; } = false;
    public bool IsSplit { get; set; } = false;
    
    // Display properties for debugging - updated format per requirements
    public string Content => ProcessingStage switch
    {
        "initial" => $"message content {MessageId}",
        "processed" => $"Complex logic msg {MessageId}: Correlation tracked, security token renewed",
        "concatenated" => $"Concat msg {MessageId}: Combined from 100 messages with security token",
        "split" => $"Split msg {MessageId}: Restored with sending ID and logical queue",
        "final" => $"Complex logic msg {MessageId}: Correlation tracked, security token renewed, HTTP batch processed",
        _ => $"message content {MessageId}"
    };
    
    // Calculate proper logical queue based on MessageId for even distribution across 100 queues (1 queue per customer)
    private string GetLogicalQueueName()
    {
        // Use hash-based distribution to ensure messages spread across all 100 logical queues (1 per customer)
        var queueIndex = MessageId % 100;
        return $"customer-queue-{queueIndex}";
    }
    
    // Calculate partition number using hash for proper load balancing across 20 partitions
    private int GetPartitionNumber()
    {
        // Use message ID hash to distribute across 20 partitions for better throughput
        return (int)(MessageId % 20);
    }
    
    public Dictionary<string, string> Headers => ProcessingStage switch
    {
        "initial" => new Dictionary<string, string>
        {
            ["kafka.topic"] = "complex-input",
            ["sender.id"] = "Darren",
            ["logical.queue"] = LogicalQueueName ?? GetLogicalQueueName(),
            ["partition.number"] = PartitionNumber > 0 ? PartitionNumber.ToString() : GetPartitionNumber().ToString()
        },
        "processed" => new Dictionary<string, string>
        {
            ["kafka.topic"] = "complex-input",
            ["correlation.id"] = CorrelationId,
            ["security.token"] = SecurityToken ?? "token-" + MessageId,
            ["sender.id"] = "Darren",
            ["logical.queue"] = LogicalQueueName ?? GetLogicalQueueName(),
            ["partition.number"] = PartitionNumber > 0 ? PartitionNumber.ToString() : GetPartitionNumber().ToString()
        },
        "concatenated" => new Dictionary<string, string>
        {
            ["kafka.topic"] = "concat-output",
            ["correlation.id"] = CorrelationId,
            ["security.token"] = SecurityToken ?? "token-" + MessageId,
            ["batch.size"] = "100",
            ["logical.queue"] = "concat-queue-" + (MessageId % 10),
            ["partition.number"] = PartitionNumber > 0 ? PartitionNumber.ToString() : GetPartitionNumber().ToString()
        },
        "split" => new Dictionary<string, string>
        {
            ["kafka.topic"] = "api-retrieved-messages", 
            ["correlation.id"] = CorrelationId,
            ["sending.id"] = SendingID ?? $"send-{MessageId:D6}",
            ["logical.queue"] = LogicalQueueName ?? GetLogicalQueueName(),
            ["partition.number"] = PartitionNumber > 0 ? PartitionNumber.ToString() : GetPartitionNumber().ToString()
        },
        "final" => new Dictionary<string, string>
        {
            ["kafka.topic"] = "sample_response",
            ["correlation.id"] = CorrelationId,
            ["batch.number"] = BatchNumber.ToString(),
            ["sending.id"] = SendingID ?? $"send-{MessageId:D6}",
            ["logical.queue"] = LogicalQueueName ?? GetLogicalQueueName(),
            ["partition.number"] = PartitionNumber > 0 ? PartitionNumber.ToString() : GetPartitionNumber().ToString(),
            ["backpressure.rate"] = "100.0"
        },
        _ => new Dictionary<string, string>
        {
            ["kafka.topic"] = "complex-input",
            ["sender.id"] = "Darren",
            ["logical.queue"] = LogicalQueueName ?? GetLogicalQueueName(),
            ["partition.number"] = PartitionNumber > 0 ? PartitionNumber.ToString() : GetPartitionNumber().ToString()
        }
    };
    
    public string HeadersDisplay => string.Join(", ", Headers.Select(kv => $"{kv.Key}={kv.Value}"));
    
    // PowerShell-compatible headers string for workflow display
    public string HeadersString => string.Join(", ", Headers.Select(kv => $"{kv.Key}={kv.Value}"));
}

public class StressTestConfiguration
{
    public int MessageCount { get; set; } = 1000000;
    public int BatchSize { get; set; } = 100;
    public int TokenRenewalInterval { get; set; } = 10000;
    public string ConsumerGroup { get; set; } = "stress-test-group";
    public TimeSpan LagThreshold { get; set; } = TimeSpan.FromSeconds(5);
    public double RateLimit { get; set; } = 100.0; // Changed from 1000 to 100 per logical queue
    public double BurstCapacity { get; set; } = 5000.0;
    public int PartitionCount { get; set; } = 10;
    public int LogicalQueueCount { get; set; } = 100;
    public bool UseTemporalJobs { get; set; } = true;
    public string SampleResponseTopic { get; set; } = "sample_response";
}

public class StressTestStatus
{
    public string TestId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalMessages { get; set; }
    public int ProcessedMessages { get; set; }
    public int TokenRenewals { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public List<string> Logs { get; set; } = new();
    public Dictionary<string, object> Metrics { get; set; } = new();
}

public class SecurityTokenInfo
{
    public string CurrentToken { get; set; } = string.Empty;
    public int RenewalCount { get; set; }
    public int MessagesSinceRenewal { get; set; }
    public int RenewalInterval { get; set; }
    public DateTime LastRenewal { get; set; }
    public bool IsRenewing { get; set; }
}

public class FlinkJobInfo
{
    public string JobId { get; set; } = string.Empty;
    public string JobName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public Dictionary<string, object> Configuration { get; set; } = new();
    public List<FlinkTaskManagerInfo> TaskManagers { get; set; } = new();
}

public class FlinkTaskManagerInfo
{
    public string TaskManagerId { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int SlotsTotal { get; set; }
    public int SlotsAvailable { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class BackpressureStatus
{
    public bool IsBackpressureActive { get; set; }
    public TimeSpan CurrentLag { get; set; }
    public TimeSpan LagThreshold { get; set; }
    public double CurrentTokens { get; set; }
    public double MaxTokens { get; set; }
    public double RateLimit { get; set; }
    public bool IsRefillPaused { get; set; }
    public DateTime LastCheck { get; set; }
    public string RateLimiterType { get; set; } = string.Empty;
}

public class BatchProcessingResult
{
    public int BatchNumber { get; set; }
    public int MessageCount { get; set; }
    public bool Success { get; set; }
    public string Status { get; set; } = string.Empty;
    public TimeSpan ProcessingTime { get; set; }
    public List<string> CorrelationIds { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class MessageVerificationResult
{
    public int TotalMessages { get; set; }
    public int VerifiedMessages { get; set; }
    public double SuccessRate { get; set; }
    public List<ComplexLogicMessage> TopMessages { get; set; } = new();
    public List<ComplexLogicMessage> LastMessages { get; set; } = new();
    public List<string> MissingCorrelationIds { get; set; } = new();
    public Dictionary<string, int> ErrorCounts { get; set; } = new();
}

// New models for the updated business flow
public class TemporalJobRequest
{
    public string JobId { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public int RetryPolicy { get; set; } = 3;
}

public class LogicalQueueConfiguration
{
    public int PartitionCount { get; set; } = 10;
    public int LogicalQueueCount { get; set; } = 100;
    public double MessagesPerSecondPerQueue { get; set; } = 100.0;
    public Dictionary<string, string> KafkaHeaders { get; set; } = new();
}

public class FlinkConcatJobConfiguration
{
    public int BatchSize { get; set; } = 100;
    public string SecurityTokenSource { get; set; } = "saved_token_service";
    public string LocalTestingApiEndpoint { get; set; } = "/api/batch/process";
    public string OutputTopic { get; set; } = "concat-output";
}

public class FlinkSplitJobConfiguration
{
    public string InputTopic { get; set; } = "api-retrieved-messages";
    public string OutputTopic { get; set; } = "sample_response";
    public bool AddSendingId { get; set; } = true;
    public bool AddLogicalQueueName { get; set; } = true;
    public bool UseCorrelationMatching { get; set; } = true;
}

public class KafkaInSinkConfiguration
{
    public string LocalTestingApiEndpoint { get; set; } = "/api/batch/process";
    public int PollIntervalMs { get; set; } = 1000;
    public string OutputTopic { get; set; } = "api-retrieved-messages";
}

public class MessageVerificationConfiguration
{
    public string TargetTopic { get; set; } = "sample_response";
    public int TopMessageCount { get; set; } = 10;
    public int LastMessageCount { get; set; } = 10;
    public bool VerifyHeaders { get; set; } = true;
    public bool VerifyContent { get; set; } = true;
}

// Additional request/response models needed for the API
public class BackpressureConfiguration
{
    public string ConsumerGroup { get; set; } = "stress-test-group";
    public double LagThresholdSeconds { get; set; } = 5.0;
    public double RateLimit { get; set; } = 100.0; // 100 messages/second per logical queue
    public double BurstCapacity { get; set; } = 5000.0;
}

public class MessageProductionRequest
{
    public int MessageCount { get; set; } = 1000000;
    public string? TestId { get; set; }
    public bool UseTemporalSubmission { get; set; } = true;
    public int PartitionCount { get; set; } = 10;
    public int LogicalQueueCount { get; set; } = 100;
}

public class FlinkJobConfiguration
{
    public string JobName { get; set; } = "ComplexLogicStressJob";
    public string ConsumerGroup { get; set; } = "complex-logic-group";
    public string InputTopic { get; set; } = "complex-input";
    public string OutputTopic { get; set; } = "complex-output";
    public bool EnableCorrelationTracking { get; set; } = true;
    public int BatchSize { get; set; } = 100;
    public int Parallelism { get; set; } = 10;
    public int CheckpointingInterval { get; set; } = 60000;
}

public class MessageVerificationRequest
{
    public string TestId { get; set; } = string.Empty;
    public string TargetTopic { get; set; } = "sample_response";
    public int TopCount { get; set; } = 10;
    public int LastCount { get; set; } = 10;
    public bool VerifyHeaders { get; set; } = true;
    public bool VerifyContent { get; set; } = true;
}

public class BatchProcessingRequest
{
    public string TestId { get; set; } = string.Empty;
    public int BatchSize { get; set; } = 100;
}

/// <summary>
/// WI26: Kafka broker health check result for infrastructure readiness validation
/// </summary>
public class KafkaHealthCheckResult
{
    public bool IsHealthy { get; set; }
    public string? ErrorMessage { get; set; }
    public string BrokerInfo { get; set; } = string.Empty;
}