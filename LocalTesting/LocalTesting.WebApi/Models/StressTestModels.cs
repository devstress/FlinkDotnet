namespace LocalTesting.WebApi.Models;

public class ComplexLogicMessage
{
    public long MessageId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string? SendingID { get; set; }
    public string Payload { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int BatchNumber { get; set; }
    
    // Display properties for debugging
    public string Content => $"Complex logic msg {MessageId}: Correlation tracked, security token renewed, HTTP batch processed";
    public Dictionary<string, string> Headers => new Dictionary<string, string>
    {
        ["kafka.topic"] = BatchNumber == 1 ? "complex-input" : "complex-output",
        ["correlation.id"] = CorrelationId,
        ["batch.number"] = BatchNumber.ToString(),
        ["sending.id"] = SendingID ?? "pending"
    };
    public string HeadersDisplay => string.Join(", ", Headers.Select(kv => $"{kv.Key}={kv.Value}"));
}

public class StressTestConfiguration
{
    public int MessageCount { get; set; } = 1000000;
    public int BatchSize { get; set; } = 100;
    public int TokenRenewalInterval { get; set; } = 10000;
    public string ConsumerGroup { get; set; } = "stress-test-group";
    public TimeSpan LagThreshold { get; set; } = TimeSpan.FromSeconds(5);
    public double RateLimit { get; set; } = 1000.0;
    public double BurstCapacity { get; set; } = 5000.0;
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