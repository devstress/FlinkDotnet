using System.Collections.Concurrent;
using LocalTesting.WebApi.Models;

namespace LocalTesting.WebApi.Services;

/// <summary>
/// Implementation of message state tracking service for monitoring individual messages through the processing pipeline
/// </summary>
public class MessageStateService : IMessageStateService
{
    private readonly ConcurrentDictionary<string, MessageTrackingInfo> _trackedMessages = new();
    private readonly ILogger<MessageStateService> _logger;
    private readonly object _lock = new();

    public MessageStateService(ILogger<MessageStateService> logger)
    {
        _logger = logger;
    }

    public async Task<MessageTrackingInfo> StartTrackingAsync(string messageId, MessageState initialState, Dictionary<string, object?>? metadata = null)
    {
        var trackingInfo = new MessageTrackingInfo
        {
            MessageId = messageId,
            CurrentState = initialState,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            Metadata = metadata ?? new Dictionary<string, object?>()
        };

        // Add initial state transition
        trackingInfo.StateHistory.Add(new StateTransition
        {
            FromState = null,
            ToState = initialState,
            Timestamp = DateTime.UtcNow,
            Details = "Message tracking started",
            Component = "MessageStateService"
        });

        _trackedMessages[messageId] = trackingInfo;
        
        _logger.LogInformation("📍 Started tracking message {MessageId} with initial state {State}", 
            messageId, initialState);

        return await Task.FromResult(trackingInfo);
    }

    public async Task<MessageTrackingInfo?> UpdateStateAsync(string messageId, MessageState newState, string? component = null, string? details = null)
    {
        if (!_trackedMessages.TryGetValue(messageId, out var trackingInfo))
        {
            _logger.LogWarning("⚠️ Attempted to update state for untracked message {MessageId}", messageId);
            return null;
        }

        lock (_lock)
        {
            var previousState = trackingInfo.CurrentState;
            
            // Validate state transition
            if (!IsValidStateTransition(previousState, newState))
            {
                _logger.LogWarning("❌ Invalid state transition for message {MessageId}: {FromState} → {ToState}", 
                    messageId, previousState, newState);
                return trackingInfo;
            }

            trackingInfo.CurrentState = newState;
            trackingInfo.LastUpdatedAt = DateTime.UtcNow;

            // Add state transition to history
            trackingInfo.StateHistory.Add(new StateTransition
            {
                FromState = previousState,
                ToState = newState,
                Timestamp = DateTime.UtcNow,
                Details = details,
                Component = component
            });

            _logger.LogInformation("🔄 Updated message {MessageId} state: {FromState} → {ToState} by {Component}", 
                messageId, previousState, newState, component ?? "Unknown");
        }

        return await Task.FromResult(trackingInfo);
    }

    public async Task<MessageTrackingInfo?> GetMessageStateAsync(string messageId, bool includeHistory = false)
    {
        if (!_trackedMessages.TryGetValue(messageId, out var trackingInfo))
        {
            return null;
        }

        if (!includeHistory)
        {
            // Return a copy without history to avoid modification
            return await Task.FromResult(new MessageTrackingInfo
            {
                MessageId = trackingInfo.MessageId,
                CurrentState = trackingInfo.CurrentState,
                CreatedAt = trackingInfo.CreatedAt,
                LastUpdatedAt = trackingInfo.LastUpdatedAt,
                Topic = trackingInfo.Topic,
                Partition = trackingInfo.Partition,
                FlinkJobId = trackingInfo.FlinkJobId,
                TemporalWorkflowType = trackingInfo.TemporalWorkflowType,
                ErrorMessage = trackingInfo.ErrorMessage,
                Metadata = new Dictionary<string, object?>(trackingInfo.Metadata)
            });
        }

        return await Task.FromResult(trackingInfo);
    }

    public async Task<MessageStateQueryResponse> QueryMessageStatesAsync(MessageStateQueryRequest query)
    {
        var allMessages = _trackedMessages.Values.ToList();
        var filteredMessages = allMessages.AsEnumerable();

        // Apply filters
        if (query.MessageIds?.Any() == true)
        {
            filteredMessages = filteredMessages.Where(m => query.MessageIds.Contains(m.MessageId));
        }

        if (query.States?.Any() == true)
        {
            filteredMessages = filteredMessages.Where(m => query.States.Contains(m.CurrentState));
        }

        if (!string.IsNullOrEmpty(query.Topic))
        {
            filteredMessages = filteredMessages.Where(m => m.Topic == query.Topic);
        }

        if (!string.IsNullOrEmpty(query.FlinkJobId))
        {
            filteredMessages = filteredMessages.Where(m => m.FlinkJobId == query.FlinkJobId);
        }

        if (!string.IsNullOrEmpty(query.TemporalWorkflowType))
        {
            filteredMessages = filteredMessages.Where(m => m.TemporalWorkflowType == query.TemporalWorkflowType);
        }

        if (query.CreatedAfter.HasValue)
        {
            filteredMessages = filteredMessages.Where(m => m.CreatedAt >= query.CreatedAfter.Value);
        }

        if (query.CreatedBefore.HasValue)
        {
            filteredMessages = filteredMessages.Where(m => m.CreatedAt <= query.CreatedBefore.Value);
        }

        var resultMessages = filteredMessages.ToList();

        // Apply limit
        if (query.Limit.HasValue && query.Limit > 0)
        {
            resultMessages = resultMessages.Take(query.Limit.Value).ToList();
        }

        // Remove history if not requested
        if (!query.IncludeHistory)
        {
            resultMessages = resultMessages.Select(m => new MessageTrackingInfo
            {
                MessageId = m.MessageId,
                CurrentState = m.CurrentState,
                CreatedAt = m.CreatedAt,
                LastUpdatedAt = m.LastUpdatedAt,
                Topic = m.Topic,
                Partition = m.Partition,
                FlinkJobId = m.FlinkJobId,
                TemporalWorkflowType = m.TemporalWorkflowType,
                ErrorMessage = m.ErrorMessage,
                Metadata = new Dictionary<string, object?>(m.Metadata)
            }).ToList();
        }

        var summary = GenerateSummary(allMessages);

        return await Task.FromResult(new MessageStateQueryResponse
        {
            Status = "Success",
            Message = $"Found {resultMessages.Count} messages matching query criteria",
            Messages = resultMessages,
            Summary = summary
        });
    }

    public async Task<MessageStateSummary> GetSummaryAsync()
    {
        var allMessages = _trackedMessages.Values.ToList();
        return await Task.FromResult(GenerateSummary(allMessages));
    }

    public async Task<MessageTrackingInfo?> MarkAsFailedAsync(string messageId, string errorMessage, string? component = null)
    {
        if (!_trackedMessages.TryGetValue(messageId, out var trackingInfo))
        {
            _logger.LogWarning("⚠️ Attempted to mark untracked message {MessageId} as failed", messageId);
            return null;
        }

        trackingInfo.ErrorMessage = errorMessage;
        return await UpdateStateAsync(messageId, MessageState.Failed, component, $"Error: {errorMessage}");
    }

    public async Task<MessageTrackingInfo?> MarkAsDeliveredAsync(string messageId, string? component = null)
    {
        return await UpdateStateAsync(messageId, MessageState.Delivered, component, "End-to-end processing completed successfully");
    }

    public async Task<MessageTrackingInfo?> UpdateMetadataAsync(string messageId, Dictionary<string, object?> metadata)
    {
        if (!_trackedMessages.TryGetValue(messageId, out var trackingInfo))
        {
            return null;
        }

        lock (_lock)
        {
            foreach (var kvp in metadata)
            {
                trackingInfo.Metadata[kvp.Key] = kvp.Value;
            }
            trackingInfo.LastUpdatedAt = DateTime.UtcNow;
        }

        return await Task.FromResult(trackingInfo);
    }

    public async Task<int> CleanupExpiredMessagesAsync(TimeSpan maxAge)
    {
        var cutoffTime = DateTime.UtcNow - maxAge;
        var expiredMessages = _trackedMessages.Values
            .Where(m => m.CreatedAt < cutoffTime)
            .ToList();

        var cleanupCount = 0;
        foreach (var message in expiredMessages)
        {
            if (_trackedMessages.TryRemove(message.MessageId, out _))
            {
                cleanupCount++;
            }
        }

        if (cleanupCount > 0)
        {
            _logger.LogInformation("🧹 Cleaned up {Count} expired message tracking records", cleanupCount);
        }

        return await Task.FromResult(cleanupCount);
    }

    public async Task<List<MessageTrackingInfo>> GetAllTrackedMessagesAsync(bool includeHistory = false)
    {
        var messages = _trackedMessages.Values.ToList();
        
        if (!includeHistory)
        {
            messages = messages.Select(m => new MessageTrackingInfo
            {
                MessageId = m.MessageId,
                CurrentState = m.CurrentState,
                CreatedAt = m.CreatedAt,
                LastUpdatedAt = m.LastUpdatedAt,
                Topic = m.Topic,
                Partition = m.Partition,
                FlinkJobId = m.FlinkJobId,
                TemporalWorkflowType = m.TemporalWorkflowType,
                ErrorMessage = m.ErrorMessage,
                Metadata = new Dictionary<string, object?>(m.Metadata)
            }).ToList();
        }

        return await Task.FromResult(messages);
    }

    public async Task<List<MessageTrackingInfo>> GetMessagesByStateAsync(MessageState state, bool includeHistory = false)
    {
        var messages = _trackedMessages.Values
            .Where(m => m.CurrentState == state)
            .ToList();

        if (!includeHistory)
        {
            messages = messages.Select(m => new MessageTrackingInfo
            {
                MessageId = m.MessageId,
                CurrentState = m.CurrentState,
                CreatedAt = m.CreatedAt,
                LastUpdatedAt = m.LastUpdatedAt,
                Topic = m.Topic,
                Partition = m.Partition,
                FlinkJobId = m.FlinkJobId,
                TemporalWorkflowType = m.TemporalWorkflowType,
                ErrorMessage = m.ErrorMessage,
                Metadata = new Dictionary<string, object?>(m.Metadata)
            }).ToList();
        }

        return await Task.FromResult(messages);
    }

    public string GenerateMessageId(string? prefix = null)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random = Random.Shared.Next(1000, 9999);
        var baseId = $"{timestamp}-{random}";
        
        return string.IsNullOrEmpty(prefix) ? baseId : $"{prefix}-{baseId}";
    }

    public async Task<bool> IsMessageTrackedAsync(string messageId)
    {
        return await Task.FromResult(_trackedMessages.ContainsKey(messageId));
    }

    public async Task<MessageState?> GetCurrentStateAsync(string messageId)
    {
        if (_trackedMessages.TryGetValue(messageId, out var trackingInfo))
        {
            return await Task.FromResult(trackingInfo.CurrentState);
        }
        return await Task.FromResult((MessageState?)null);
    }

    public async Task<TimeSpan?> GetProcessingTimeAsync(string messageId)
    {
        if (_trackedMessages.TryGetValue(messageId, out var trackingInfo))
        {
            return await Task.FromResult(trackingInfo.TotalProcessingTime);
        }
        return await Task.FromResult((TimeSpan?)null);
    }

    private static bool IsValidStateTransition(MessageState fromState, MessageState toState)
    {
        // Define valid state transitions
        return fromState switch
        {
            MessageState.Produced => toState is MessageState.Consumed or MessageState.Failed,
            MessageState.Consumed => toState is MessageState.FlinkProcessing or MessageState.Failed,
            MessageState.FlinkProcessing => toState is MessageState.FlinkProcessed or MessageState.Failed,
            MessageState.FlinkProcessed => toState is MessageState.TemporalReceived or MessageState.Delivered or MessageState.Failed,
            MessageState.TemporalReceived => toState is MessageState.TemporalProcessing or MessageState.Failed,
            MessageState.TemporalProcessing => toState is MessageState.TemporalCompleted or MessageState.Failed,
            MessageState.TemporalCompleted => toState is MessageState.Delivered or MessageState.Failed,
            MessageState.Delivered => false, // Terminal state
            MessageState.Failed => toState is MessageState.Expired, // Can only expire from failed
            MessageState.Expired => false, // Terminal state
            _ => false
        };
    }

    private static MessageStateSummary GenerateSummary(List<MessageTrackingInfo> messages)
    {
        var summary = new MessageStateSummary
        {
            TotalMessages = messages.Count
        };

        // Count messages by state
        summary.MessagesByState = messages
            .GroupBy(m => m.CurrentState)
            .ToDictionary(g => g.Key, g => g.Count());

        // Calculate other metrics
        summary.FailedMessages = summary.MessagesByState.GetValueOrDefault(MessageState.Failed, 0);
        summary.DeliveredMessages = summary.MessagesByState.GetValueOrDefault(MessageState.Delivered, 0);
        
        var processingStates = new[] 
        { 
            MessageState.Consumed, 
            MessageState.FlinkProcessing, 
            MessageState.FlinkProcessed,
            MessageState.TemporalReceived,
            MessageState.TemporalProcessing,
            MessageState.TemporalCompleted
        };
        
        summary.MessagesInProcessing = processingStates
            .Sum(state => summary.MessagesByState.GetValueOrDefault(state, 0));

        // Calculate average processing time for completed messages
        var completedMessages = messages
            .Where(m => m.CurrentState is MessageState.Delivered or MessageState.Failed)
            .ToList();

        if (completedMessages.Any())
        {
            summary.AverageProcessingTime = TimeSpan.FromMilliseconds(
                completedMessages.Average(m => m.TotalProcessingTime.TotalMilliseconds));
        }

        return summary;
    }
}