using LocalTesting.WebApi.Models;

namespace LocalTesting.WebApi.Services;

/// <summary>
/// Service interface for tracking individual message states through the processing pipeline
/// </summary>
public interface IMessageStateService
{
    /// <summary>
    /// Start tracking a new message with initial state
    /// </summary>
    /// <param name="messageId">Unique message identifier</param>
    /// <param name="initialState">Initial state of the message</param>
    /// <param name="metadata">Optional metadata about the message</param>
    /// <returns>Created message tracking information</returns>
    Task<MessageTrackingInfo> StartTrackingAsync(string messageId, MessageState initialState, Dictionary<string, object?>? metadata = null);
    
    /// <summary>
    /// Update the state of a tracked message
    /// </summary>
    /// <param name="messageId">Message identifier</param>
    /// <param name="newState">New state for the message</param>
    /// <param name="component">Component initiating the state change</param>
    /// <param name="details">Optional details about the state change</param>
    /// <returns>Updated message tracking information</returns>
    Task<MessageTrackingInfo?> UpdateStateAsync(string messageId, MessageState newState, string? component = null, string? details = null);
    
    /// <summary>
    /// Get tracking information for a specific message
    /// </summary>
    /// <param name="messageId">Message identifier</param>
    /// <param name="includeHistory">Whether to include state transition history</param>
    /// <returns>Message tracking information or null if not found</returns>
    Task<MessageTrackingInfo?> GetMessageStateAsync(string messageId, bool includeHistory = false);
    
    /// <summary>
    /// Query message states based on filters
    /// </summary>
    /// <param name="query">Query parameters for filtering messages</param>
    /// <returns>Query response with matching messages and summary</returns>
    Task<MessageStateQueryResponse> QueryMessageStatesAsync(MessageStateQueryRequest query);
    
    /// <summary>
    /// Get summary statistics for all tracked messages
    /// </summary>
    /// <returns>Summary statistics</returns>
    Task<MessageStateSummary> GetSummaryAsync();
    
    /// <summary>
    /// Mark a message as failed with error details
    /// </summary>
    /// <param name="messageId">Message identifier</param>
    /// <param name="errorMessage">Error description</param>
    /// <param name="component">Component where the failure occurred</param>
    /// <returns>Updated message tracking information</returns>
    Task<MessageTrackingInfo?> MarkAsFailedAsync(string messageId, string errorMessage, string? component = null);
    
    /// <summary>
    /// Mark a message as delivered (end-to-end processing complete)
    /// </summary>
    /// <param name="messageId">Message identifier</param>
    /// <param name="component">Component completing the delivery</param>
    /// <returns>Updated message tracking information</returns>
    Task<MessageTrackingInfo?> MarkAsDeliveredAsync(string messageId, string? component = null);
    
    /// <summary>
    /// Update message metadata
    /// </summary>
    /// <param name="messageId">Message identifier</param>
    /// <param name="metadata">Metadata to add or update</param>
    /// <returns>Updated message tracking information</returns>
    Task<MessageTrackingInfo?> UpdateMetadataAsync(string messageId, Dictionary<string, object?> metadata);
    
    /// <summary>
    /// Clean up expired message tracking data
    /// </summary>
    /// <param name="maxAge">Maximum age for tracked messages</param>
    /// <returns>Number of messages cleaned up</returns>
    Task<int> CleanupExpiredMessagesAsync(TimeSpan maxAge);
    
    /// <summary>
    /// Get all messages currently being tracked
    /// </summary>
    /// <param name="includeHistory">Whether to include state transition history</param>
    /// <returns>All tracked messages</returns>
    Task<List<MessageTrackingInfo>> GetAllTrackedMessagesAsync(bool includeHistory = false);
    
    /// <summary>
    /// Get messages by current state
    /// </summary>
    /// <param name="state">State to filter by</param>
    /// <param name="includeHistory">Whether to include state transition history</param>
    /// <returns>Messages in the specified state</returns>
    Task<List<MessageTrackingInfo>> GetMessagesByStateAsync(MessageState state, bool includeHistory = false);
    
    /// <summary>
    /// Generate a unique message ID for tracking
    /// </summary>
    /// <param name="prefix">Optional prefix for the ID</param>
    /// <returns>Unique message identifier</returns>
    string GenerateMessageId(string? prefix = null);
    
    /// <summary>
    /// Check if a message is currently being tracked
    /// </summary>
    /// <param name="messageId">Message identifier</param>
    /// <returns>True if the message is being tracked</returns>
    Task<bool> IsMessageTrackedAsync(string messageId);
    
    /// <summary>
    /// Get the current state of a message
    /// </summary>
    /// <param name="messageId">Message identifier</param>
    /// <returns>Current message state or null if not tracked</returns>
    Task<MessageState?> GetCurrentStateAsync(string messageId);
    
    /// <summary>
    /// Get processing time for a message
    /// </summary>
    /// <param name="messageId">Message identifier</param>
    /// <returns>Processing time or null if not tracked</returns>
    Task<TimeSpan?> GetProcessingTimeAsync(string messageId);
}