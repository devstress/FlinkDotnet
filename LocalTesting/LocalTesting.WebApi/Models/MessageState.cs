namespace LocalTesting.WebApi.Models;

/// <summary>
/// Represents the current state of a message in the processing pipeline
/// </summary>
public enum MessageState
{
    /// <summary>
    /// Message has been produced to Kafka
    /// </summary>
    Produced,
    
    /// <summary>
    /// Message has been consumed from Kafka
    /// </summary>
    Consumed,
    
    /// <summary>
    /// Message is currently being processed by Flink
    /// </summary>
    FlinkProcessing,
    
    /// <summary>
    /// Message has been processed by Flink
    /// </summary>
    FlinkProcessed,
    
    /// <summary>
    /// Message has been received by Temporal
    /// </summary>
    TemporalReceived,
    
    /// <summary>
    /// Message is currently being processed by Temporal workflow
    /// </summary>
    TemporalProcessing,
    
    /// <summary>
    /// Message has been processed by Temporal workflow
    /// </summary>
    TemporalCompleted,
    
    /// <summary>
    /// Message has been delivered (end-to-end processing complete)
    /// </summary>
    Delivered,
    
    /// <summary>
    /// Message processing failed at some stage
    /// </summary>
    Failed,
    
    /// <summary>
    /// Message tracking has expired (cleanup)
    /// </summary>
    Expired
}

/// <summary>
/// Contains detailed information about a message being tracked through the pipeline
/// </summary>
public class MessageTrackingInfo
{
    /// <summary>
    /// Unique identifier for the message
    /// </summary>
    public string MessageId { get; set; } = string.Empty;
    
    /// <summary>
    /// Current state of the message
    /// </summary>
    public MessageState CurrentState { get; set; }
    
    /// <summary>
    /// When the message was first created/produced
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// When the message state was last updated
    /// </summary>
    public DateTime LastUpdatedAt { get; set; }
    
    /// <summary>
    /// Total time spent in processing so far
    /// </summary>
    public TimeSpan TotalProcessingTime => LastUpdatedAt - CreatedAt;
    
    /// <summary>
    /// Kafka topic where the message was produced
    /// </summary>
    public string? Topic { get; set; }
    
    /// <summary>
    /// Kafka partition where the message was produced
    /// </summary>
    public string? Partition { get; set; }
    
    /// <summary>
    /// Flink job ID that processed the message
    /// </summary>
    public string? FlinkJobId { get; set; }
    
    /// <summary>
    /// Temporal workflow type that processed the message
    /// </summary>
    public string? TemporalWorkflowType { get; set; }
    
    /// <summary>
    /// Error message if processing failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// History of state transitions with timestamps
    /// </summary>
    public List<StateTransition> StateHistory { get; set; } = new();
    
    /// <summary>
    /// Additional metadata about the message
    /// </summary>
    public Dictionary<string, object?> Metadata { get; set; } = new();
}

/// <summary>
/// Represents a state transition in the message lifecycle
/// </summary>
public class StateTransition
{
    /// <summary>
    /// Previous state (null for initial state)
    /// </summary>
    public MessageState? FromState { get; set; }
    
    /// <summary>
    /// New state
    /// </summary>
    public MessageState ToState { get; set; }
    
    /// <summary>
    /// When the transition occurred
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Optional details about the transition
    /// </summary>
    public string? Details { get; set; }
    
    /// <summary>
    /// Component that initiated the transition
    /// </summary>
    public string? Component { get; set; }
}

/// <summary>
/// Request model for querying message states
/// </summary>
public class MessageStateQueryRequest
{
    /// <summary>
    /// Filter by specific message IDs
    /// </summary>
    public List<string>? MessageIds { get; set; }
    
    /// <summary>
    /// Filter by message states
    /// </summary>
    public List<MessageState>? States { get; set; }
    
    /// <summary>
    /// Filter by Kafka topic
    /// </summary>
    public string? Topic { get; set; }
    
    /// <summary>
    /// Filter by Flink job ID
    /// </summary>
    public string? FlinkJobId { get; set; }
    
    /// <summary>
    /// Filter by Temporal workflow type
    /// </summary>
    public string? TemporalWorkflowType { get; set; }
    
    /// <summary>
    /// Filter by creation time range (start)
    /// </summary>
    public DateTime? CreatedAfter { get; set; }
    
    /// <summary>
    /// Filter by creation time range (end)
    /// </summary>
    public DateTime? CreatedBefore { get; set; }
    
    /// <summary>
    /// Include message state history
    /// </summary>
    public bool IncludeHistory { get; set; } = false;
    
    /// <summary>
    /// Maximum number of results to return
    /// </summary>
    public int? Limit { get; set; } = 100;
}

/// <summary>
/// Response model for message state queries
/// </summary>
public class MessageStateQueryResponse
{
    /// <summary>
    /// Query execution status
    /// </summary>
    public string Status { get; set; } = "Success";
    
    /// <summary>
    /// Query execution message
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// When the query was executed
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Matching message tracking information
    /// </summary>
    public List<MessageTrackingInfo> Messages { get; set; } = new();
    
    /// <summary>
    /// Summary statistics
    /// </summary>
    public MessageStateSummary Summary { get; set; } = new();
}

/// <summary>
/// Summary statistics for message state queries
/// </summary>
public class MessageStateSummary
{
    /// <summary>
    /// Total number of messages found
    /// </summary>
    public int TotalMessages { get; set; }
    
    /// <summary>
    /// Count of messages by state
    /// </summary>
    public Dictionary<MessageState, int> MessagesByState { get; set; } = new();
    
    /// <summary>
    /// Average processing time for completed messages
    /// </summary>
    public TimeSpan? AverageProcessingTime { get; set; }
    
    /// <summary>
    /// Number of failed messages
    /// </summary>
    public int FailedMessages { get; set; }
    
    /// <summary>
    /// Number of delivered messages
    /// </summary>
    public int DeliveredMessages { get; set; }
    
    /// <summary>
    /// Current messages in processing
    /// </summary>
    public int MessagesInProcessing { get; set; }
}