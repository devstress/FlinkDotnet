using Microsoft.AspNetCore.Mvc;
using LocalTesting.WebApi.Services;
using LocalTesting.WebApi.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace LocalTesting.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ObservabilityController : ControllerBase
{
    private readonly ObservabilityMetricsService _metricsService;
    private readonly IMessageStateService _messageStateService;
    private readonly ILogger<ObservabilityController> _logger;

    public ObservabilityController(ObservabilityMetricsService metricsService, IMessageStateService messageStateService, ILogger<ObservabilityController> logger)
    {
        _metricsService = metricsService;
        _messageStateService = messageStateService;
        _logger = logger;
    }

    [HttpGet("metrics/messages-per-second")]
    [SwaggerOperation(
        Summary = "Get Messages Per Second Metrics",
        Description = "Retrieve real-time messages-per-second metrics across all layers: Kafka, Flink, Temporal, and end-to-end flow"
    )]
    [SwaggerResponse(200, "Messages per second metrics retrieved successfully")]
    public IActionResult GetMessagesPerSecondMetrics()
    {
        try
        {
            _logger.LogInformation("📊 Retrieving messages-per-second metrics across all layers");

            var allRates = _metricsService.GetAllMessagesPerSecondRates();
            
            var metrics = new
            {
                Status = "Success",
                Message = "Messages-per-second metrics across Kafka → Flink → Temporal → End-to-End flow",
                Timestamp = DateTime.UtcNow,
                
                // Kafka Layer Metrics
                KafkaMetrics = new
                {
                    ProducerRates = allRates
                        .Where(kvp => kvp.Key.StartsWith("kafka_producer_"))
                        .ToDictionary(kvp => kvp.Key, kvp => new { MessagesPerSecond = Math.Round(kvp.Value, 2) }),
                    
                    ConsumerRates = allRates
                        .Where(kvp => kvp.Key.StartsWith("kafka_consumer_"))
                        .ToDictionary(kvp => kvp.Key, kvp => new { MessagesPerSecond = Math.Round(kvp.Value, 2) })
                },
                
                // Flink Layer Metrics
                FlinkMetrics = new
                {
                    InputRates = allRates
                        .Where(kvp => kvp.Key.StartsWith("flink_in_"))
                        .ToDictionary(kvp => kvp.Key, kvp => new { MessagesPerSecond = Math.Round(kvp.Value, 2) }),
                    
                    OutputRates = allRates
                        .Where(kvp => kvp.Key.StartsWith("flink_out_"))
                        .ToDictionary(kvp => kvp.Key, kvp => new { MessagesPerSecond = Math.Round(kvp.Value, 2) })
                },
                
                // Temporal Layer Metrics
                TemporalMetrics = new
                {
                    WorkflowRates = allRates
                        .Where(kvp => kvp.Key.StartsWith("temporal_workflow_"))
                        .ToDictionary(kvp => kvp.Key, kvp => new { ExecutionsPerSecond = Math.Round(kvp.Value, 2) }),
                    
                    ActivityRates = allRates
                        .Where(kvp => kvp.Key.StartsWith("temporal_activity_"))
                        .ToDictionary(kvp => kvp.Key, kvp => new { ExecutionsPerSecond = Math.Round(kvp.Value, 2) })
                },
                
                // End-to-End Flow Metrics
                FlowMetrics = new
                {
                    KafkaToFlinkRate = new { MessagesPerSecond = Math.Round(_metricsService.GetMessagesPerSecond("flow_kafka_to_flink"), 2) },
                    FlinkToTemporalRate = new { MessagesPerSecond = Math.Round(_metricsService.GetMessagesPerSecond("flow_flink_to_temporal"), 2) },
                    EndToEndRate = new { MessagesPerSecond = Math.Round(_metricsService.GetMessagesPerSecond("flow_end_to_end"), 2) }
                },
                
                // Summary Statistics
                Summary = new
                {
                    TotalMetricsTracked = allRates.Count,
                    ActiveFlows = allRates.Count(kvp => kvp.Value > 0),
                    HighestRate = allRates.Count > 0 ? Math.Round(allRates.Values.Max(), 2) : 0,
                    AverageRate = allRates.Count > 0 ? Math.Round(allRates.Values.Average(), 2) : 0,
                    TotalMessagesPerSecond = Math.Round(allRates.Values.Sum(), 2)
                }
            };

            _logger.LogInformation("✅ Messages-per-second metrics retrieved: {TotalMetrics} tracked, {ActiveFlows} active flows", 
                allRates.Count, allRates.Count(kvp => kvp.Value > 0));
            
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to retrieve messages-per-second metrics");
            return StatusCode(500, new { 
                Status = "Failed", 
                Error = ex.Message, 
                Timestamp = DateTime.UtcNow 
            });
        }
    }

    [HttpGet("metrics/layer/{layer}")]
    [SwaggerOperation(
        Summary = "Get Layer-Specific Metrics",
        Description = "Retrieve messages-per-second metrics for a specific layer: kafka, flink, temporal, or flow"
    )]
    [SwaggerResponse(200, "Layer-specific metrics retrieved successfully")]
    [SwaggerResponse(400, "Invalid layer specified")]
    public IActionResult GetLayerMetrics(string layer)
    {
        try
        {
            if (string.IsNullOrEmpty(layer))
                return BadRequest("Layer parameter is required");

            var normalizedLayer = layer.ToLowerInvariant();
            var validLayers = new[] { "kafka", "flink", "temporal", "flow" };
            
            if (!validLayers.Contains(normalizedLayer))
                return BadRequest($"Invalid layer. Valid options: {string.Join(", ", validLayers)}");

            _logger.LogInformation("📊 Retrieving {Layer} layer metrics", normalizedLayer);

            var allRates = _metricsService.GetAllMessagesPerSecondRates();
            Dictionary<string, double> layerRates;
            
            switch (normalizedLayer)
            {
                case "kafka":
                    layerRates = allRates.Where(kvp => kvp.Key.StartsWith("kafka_")).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    break;
                case "flink":
                    layerRates = allRates.Where(kvp => kvp.Key.StartsWith("flink_")).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    break;
                case "temporal":
                    layerRates = allRates.Where(kvp => kvp.Key.StartsWith("temporal_")).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    break;
                case "flow":
                    layerRates = allRates.Where(kvp => kvp.Key.StartsWith("flow_")).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    break;
                default:
                    return BadRequest($"Unsupported layer: {layer}");
            }

            var metrics = new
            {
                Status = "Success",
                Layer = normalizedLayer.ToUpperInvariant(),
                Message = $"Messages-per-second metrics for {normalizedLayer} layer",
                Timestamp = DateTime.UtcNow,
                Metrics = layerRates.ToDictionary(kvp => kvp.Key, kvp => new 
                { 
                    MessagesPerSecond = Math.Round(kvp.Value, 2),
                    Active = kvp.Value > 0
                }),
                Summary = new
                {
                    TotalMetrics = layerRates.Count,
                    ActiveMetrics = layerRates.Count(kvp => kvp.Value > 0),
                    HighestRate = layerRates.Count > 0 ? Math.Round(layerRates.Values.Max(), 2) : 0,
                    TotalRate = Math.Round(layerRates.Values.Sum(), 2)
                }
            };

            _logger.LogInformation("✅ {Layer} layer metrics retrieved: {TotalMetrics} metrics, {ActiveMetrics} active", 
                normalizedLayer, layerRates.Count, layerRates.Count(kvp => kvp.Value > 0));
            
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to retrieve {Layer} layer metrics", layer);
            return StatusCode(500, new { 
                Status = "Failed", 
                Error = ex.Message, 
                Layer = layer,
                Timestamp = DateTime.UtcNow 
            });
        }
    }

    [HttpPost("metrics/simulate")]
    [SwaggerOperation(
        Summary = "Simulate Messages Per Second Metrics",
        Description = "Generate sample metrics data for testing and demonstration of observability capabilities"
    )]
    [SwaggerResponse(200, "Metrics simulation completed successfully")]
    public IActionResult SimulateMetrics([FromBody] MetricsSimulationRequest? request = null)
    {
        try
        {
            var simRequest = request ?? new MetricsSimulationRequest
            {
                KafkaMessages = 100,
                FlinkJobs = 2,
                TemporalWorkflows = 5,
                DurationSeconds = 10
            };

            _logger.LogInformation("🎯 Simulating metrics: {KafkaMessages} Kafka messages, {FlinkJobs} Flink jobs, {TemporalWorkflows} Temporal workflows for {Duration}s", 
                simRequest.KafkaMessages, simRequest.FlinkJobs, simRequest.TemporalWorkflows, simRequest.DurationSeconds);

            // Simulate Kafka metrics
            for (int i = 0; i < simRequest.KafkaMessages; i++)
            {
                var topic = $"test-topic-{i % 3}";
                var partition = (i % 10).ToString();
                _metricsService.RecordKafkaProducerMessage(topic, partition, 1, 1024);
                _metricsService.RecordKafkaConsumerMessage(topic, partition, "test-consumer-group", 1);
            }

            // Simulate Flink metrics
            for (int i = 0; i < simRequest.FlinkJobs; i++)
            {
                var jobId = $"sim-job-{i + 1}";
                _metricsService.RecordFlinkJobMessageIn(jobId, "source", simRequest.KafkaMessages / simRequest.FlinkJobs);
                _metricsService.RecordFlinkJobMessageOut(jobId, "sink", simRequest.KafkaMessages / simRequest.FlinkJobs);
                _metricsService.RecordFlinkJobLatency(jobId, 0.050); // 50ms latency
            }

            // Simulate Temporal metrics
            for (int i = 0; i < simRequest.TemporalWorkflows; i++)
            {
                var workflowType = $"TestWorkflow{i % 3 + 1}";
                _metricsService.RecordTemporalWorkflowExecution(workflowType);
                _metricsService.RecordTemporalActivityExecution($"Activity{i % 2 + 1}");
                _metricsService.RecordTemporalWorkflowDuration(workflowType, 2.5); // 2.5s duration
                _metricsService.RecordTemporalWorkflowCompletion(workflowType);
            }

            // Simulate end-to-end flow metrics
            _metricsService.RecordFlowKafkaToFlink(simRequest.KafkaMessages);
            _metricsService.RecordFlowFlinkToTemporal(simRequest.KafkaMessages);
            _metricsService.RecordFlowEndToEnd(simRequest.KafkaMessages);
            _metricsService.RecordFlowEndToEndLatency(1.5); // 1.5s end-to-end latency

            var result = new
            {
                Status = "Simulation_Completed",
                Message = "Metrics simulation completed successfully",
                Simulation = simRequest,
                Timestamp = DateTime.UtcNow,
                SimulatedMetrics = new
                {
                    KafkaProducerMessages = simRequest.KafkaMessages,
                    KafkaConsumerMessages = simRequest.KafkaMessages,
                    FlinkJobMessages = simRequest.KafkaMessages,
                    TemporalWorkflowExecutions = simRequest.TemporalWorkflows,
                    EndToEndFlowMessages = simRequest.KafkaMessages
                }
            };

            _logger.LogInformation("✅ Metrics simulation completed successfully");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to simulate metrics");
            return StatusCode(500, new { 
                Status = "Failed", 
                Error = ex.Message, 
                Timestamp = DateTime.UtcNow 
            });
        }
    }

    #region Message State Tracking Endpoints

    [HttpGet("messages/state")]
    [SwaggerOperation(
        Summary = "Get Message State Summary",
        Description = "Retrieve summary statistics for all tracked messages across the processing pipeline"
    )]
    [SwaggerResponse(200, "Message state summary retrieved successfully")]
    public async Task<IActionResult> GetMessageStateSummary()
    {
        try
        {
            _logger.LogInformation("📊 Retrieving message state summary");

            var summary = await _messageStateService.GetSummaryAsync();
            
            var response = new
            {
                Status = "Success",
                Message = "Message state summary retrieved successfully",
                Timestamp = DateTime.UtcNow,
                Summary = summary,
                StateDistribution = summary.MessagesByState.ToDictionary(
                    kvp => kvp.Key.ToString(), 
                    kvp => new { Count = kvp.Value, Percentage = summary.TotalMessages > 0 ? Math.Round((double)kvp.Value / summary.TotalMessages * 100, 2) : 0 }
                )
            };

            _logger.LogInformation("✅ Message state summary retrieved: {TotalMessages} total, {DeliveredMessages} delivered, {FailedMessages} failed", 
                summary.TotalMessages, summary.DeliveredMessages, summary.FailedMessages);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to retrieve message state summary");
            return StatusCode(500, new { 
                Status = "Failed", 
                Error = ex.Message, 
                Timestamp = DateTime.UtcNow 
            });
        }
    }

    [HttpGet("messages/state/{messageId}")]
    [SwaggerOperation(
        Summary = "Get Message State",
        Description = "Retrieve detailed state information for a specific message including processing history"
    )]
    [SwaggerResponse(200, "Message state retrieved successfully")]
    [SwaggerResponse(404, "Message not found")]
    public async Task<IActionResult> GetMessageState(string messageId, [FromQuery] bool includeHistory = false)
    {
        try
        {
            _logger.LogInformation("🔍 Retrieving state for message {MessageId}", messageId);

            var messageInfo = await _messageStateService.GetMessageStateAsync(messageId, includeHistory);
            
            if (messageInfo == null)
            {
                return NotFound(new { 
                    Status = "NotFound", 
                    Message = $"Message {messageId} is not being tracked", 
                    MessageId = messageId,
                    Timestamp = DateTime.UtcNow 
                });
            }

            var response = new
            {
                Status = "Success",
                Message = $"Message state retrieved for {messageId}",
                Timestamp = DateTime.UtcNow,
                MessageInfo = messageInfo,
                ProcessingMetrics = new
                {
                    ProcessingTime = messageInfo.TotalProcessingTime,
                    IsCompleted = messageInfo.CurrentState is MessageState.Delivered or MessageState.Failed,
                    StateTransitions = includeHistory ? messageInfo.StateHistory.Count : 0
                }
            };

            _logger.LogInformation("✅ Message {MessageId} state: {CurrentState}, Processing time: {ProcessingTime}", 
                messageId, messageInfo.CurrentState, messageInfo.TotalProcessingTime);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to retrieve message state for {MessageId}", messageId);
            return StatusCode(500, new { 
                Status = "Failed", 
                Error = ex.Message,
                MessageId = messageId,
                Timestamp = DateTime.UtcNow 
            });
        }
    }

    [HttpPost("messages/state/query")]
    [SwaggerOperation(
        Summary = "Query Message States",
        Description = "Query message states with advanced filtering options"
    )]
    [SwaggerResponse(200, "Message state query executed successfully")]
    public async Task<IActionResult> QueryMessageStates([FromBody] MessageStateQueryRequest query)
    {
        try
        {
            _logger.LogInformation("🔍 Executing message state query with filters");

            var response = await _messageStateService.QueryMessageStatesAsync(query);
            
            _logger.LogInformation("✅ Message state query completed: {TotalMessages} results", response.Messages.Count);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to execute message state query");
            return StatusCode(500, new { 
                Status = "Failed", 
                Error = ex.Message, 
                Timestamp = DateTime.UtcNow 
            });
        }
    }

    [HttpGet("messages/state/by-state/{state}")]
    [SwaggerOperation(
        Summary = "Get Messages by State",
        Description = "Retrieve all messages currently in a specific state"
    )]
    [SwaggerResponse(200, "Messages retrieved successfully")]
    [SwaggerResponse(400, "Invalid state specified")]
    public async Task<IActionResult> GetMessagesByState(string state, [FromQuery] bool includeHistory = false)
    {
        try
        {
            if (!Enum.TryParse<MessageState>(state, true, out var messageState))
            {
                var validStates = Enum.GetNames<MessageState>();
                return BadRequest(new { 
                    Status = "InvalidState", 
                    Message = $"Invalid state '{state}'. Valid states: {string.Join(", ", validStates)}", 
                    ValidStates = validStates,
                    Timestamp = DateTime.UtcNow 
                });
            }

            _logger.LogInformation("🔍 Retrieving messages in state {State}", messageState);

            var messages = await _messageStateService.GetMessagesByStateAsync(messageState, includeHistory);
            
            var response = new
            {
                Status = "Success",
                Message = $"Found {messages.Count} messages in state {messageState}",
                State = messageState.ToString(),
                Timestamp = DateTime.UtcNow,
                Messages = messages,
                Count = messages.Count
            };

            _logger.LogInformation("✅ Retrieved {Count} messages in state {State}", messages.Count, messageState);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to retrieve messages by state {State}", state);
            return StatusCode(500, new { 
                Status = "Failed", 
                Error = ex.Message,
                State = state,
                Timestamp = DateTime.UtcNow 
            });
        }
    }

    [HttpPost("messages/track")]
    [SwaggerOperation(
        Summary = "Start Message Tracking",
        Description = "Start tracking a new message through the processing pipeline"
    )]
    [SwaggerResponse(200, "Message tracking started successfully")]
    public async Task<IActionResult> StartMessageTracking([FromBody] StartTrackingRequest request)
    {
        try
        {
            _logger.LogInformation("📍 Starting tracking for message {MessageId} with state {InitialState}", 
                request.MessageId, request.InitialState);

            var trackingInfo = await _messageStateService.StartTrackingAsync(
                request.MessageId, 
                request.InitialState, 
                request.Metadata);
            
            var response = new
            {
                Status = "TrackingStarted",
                Message = $"Started tracking message {request.MessageId}",
                Timestamp = DateTime.UtcNow,
                TrackingInfo = trackingInfo
            };

            _logger.LogInformation("✅ Started tracking message {MessageId} with initial state {State}", 
                request.MessageId, request.InitialState);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to start tracking message {MessageId}", request.MessageId);
            return StatusCode(500, new { 
                Status = "Failed", 
                Error = ex.Message,
                MessageId = request.MessageId,
                Timestamp = DateTime.UtcNow 
            });
        }
    }

    [HttpPut("messages/state/{messageId}")]
    [SwaggerOperation(
        Summary = "Update Message State",
        Description = "Update the state of a tracked message"
    )]
    [SwaggerResponse(200, "Message state updated successfully")]
    [SwaggerResponse(404, "Message not found")]
    public async Task<IActionResult> UpdateMessageState(string messageId, [FromBody] UpdateStateRequest request)
    {
        try
        {
            _logger.LogInformation("🔄 Updating state for message {MessageId} to {NewState}", 
                messageId, request.NewState);

            var updatedInfo = await _messageStateService.UpdateStateAsync(
                messageId, 
                request.NewState, 
                request.Component, 
                request.Details);
            
            if (updatedInfo == null)
            {
                return NotFound(new { 
                    Status = "NotFound", 
                    Message = $"Message {messageId} is not being tracked", 
                    MessageId = messageId,
                    Timestamp = DateTime.UtcNow 
                });
            }

            var response = new
            {
                Status = "StateUpdated",
                Message = $"Updated state for message {messageId} to {request.NewState}",
                Timestamp = DateTime.UtcNow,
                UpdatedInfo = updatedInfo
            };

            _logger.LogInformation("✅ Updated message {MessageId} state to {NewState}", messageId, request.NewState);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to update state for message {MessageId}", messageId);
            return StatusCode(500, new { 
                Status = "Failed", 
                Error = ex.Message,
                MessageId = messageId,
                Timestamp = DateTime.UtcNow 
            });
        }
    }

    [HttpPost("messages/cleanup")]
    [SwaggerOperation(
        Summary = "Cleanup Expired Messages",
        Description = "Remove expired message tracking data to free up memory"
    )]
    [SwaggerResponse(200, "Cleanup completed successfully")]
    public async Task<IActionResult> CleanupExpiredMessages([FromQuery] int maxAgeHours = 24)
    {
        try
        {
            _logger.LogInformation("🧹 Starting cleanup of messages older than {MaxAgeHours} hours", maxAgeHours);

            var maxAge = TimeSpan.FromHours(maxAgeHours);
            var cleanupCount = await _messageStateService.CleanupExpiredMessagesAsync(maxAge);
            
            var response = new
            {
                Status = "CleanupCompleted",
                Message = $"Cleaned up {cleanupCount} expired message tracking records",
                Timestamp = DateTime.UtcNow,
                CleanupCount = cleanupCount,
                MaxAgeHours = maxAgeHours
            };

            _logger.LogInformation("✅ Cleanup completed: {CleanupCount} expired messages removed", cleanupCount);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to cleanup expired messages");
            return StatusCode(500, new { 
                Status = "Failed", 
                Error = ex.Message, 
                Timestamp = DateTime.UtcNow 
            });
        }
    }

    [HttpPost("messages/simulate-tracking")]
    [SwaggerOperation(
        Summary = "Simulate Message State Tracking",
        Description = "Generate sample message tracking data for testing and demonstration"
    )]
    [SwaggerResponse(200, "Message tracking simulation completed successfully")]
    public async Task<IActionResult> SimulateMessageTracking([FromBody] MessageTrackingSimulationRequest? request = null)
    {
        try
        {
            var simRequest = request ?? new MessageTrackingSimulationRequest
            {
                MessageCount = 10,
                SimulateFailures = true,
                FailureRate = 0.1
            };

            _logger.LogInformation("🎯 Simulating message tracking for {MessageCount} messages", simRequest.MessageCount);

            var simulatedMessages = new List<string>();
            var random = new Random();

            for (int i = 0; i < simRequest.MessageCount; i++)
            {
                var messageId = _messageStateService.GenerateMessageId($"sim-{i:D3}");
                simulatedMessages.Add(messageId);

                // Start tracking
                await _messageStateService.StartTrackingAsync(messageId, MessageState.Produced, new Dictionary<string, object?>
                {
                    ["simulation"] = true,
                    ["batch"] = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"),
                    ["index"] = i
                });

                // Simulate message flow
                await _messageStateService.UpdateStateAsync(messageId, MessageState.Consumed, "KafkaConsumer");
                await Task.Delay(50); // Simulate processing time

                await _messageStateService.UpdateStateAsync(messageId, MessageState.FlinkProcessing, "FlinkJob");
                await Task.Delay(100);

                await _messageStateService.UpdateStateAsync(messageId, MessageState.FlinkProcessed, "FlinkJob");
                await Task.Delay(50);

                // Simulate failures based on failure rate
                if (simRequest.SimulateFailures && random.NextDouble() < simRequest.FailureRate)
                {
                    await _messageStateService.MarkAsFailedAsync(messageId, "Simulated processing failure", "TemporalWorkflow");
                }
                else
                {
                    await _messageStateService.UpdateStateAsync(messageId, MessageState.TemporalReceived, "TemporalWorkflow");
                    await Task.Delay(200);

                    await _messageStateService.UpdateStateAsync(messageId, MessageState.TemporalProcessing, "TemporalWorkflow");
                    await Task.Delay(300);

                    await _messageStateService.UpdateStateAsync(messageId, MessageState.TemporalCompleted, "TemporalWorkflow");
                    await Task.Delay(50);

                    await _messageStateService.MarkAsDeliveredAsync(messageId, "EndToEndFlow");
                }
            }

            var summary = await _messageStateService.GetSummaryAsync();

            var result = new
            {
                Status = "SimulationCompleted",
                Message = "Message tracking simulation completed successfully",
                Simulation = simRequest,
                Timestamp = DateTime.UtcNow,
                SimulatedMessages = simulatedMessages,
                Summary = summary
            };

            _logger.LogInformation("✅ Message tracking simulation completed: {MessageCount} messages processed", simRequest.MessageCount);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to simulate message tracking");
            return StatusCode(500, new { 
                Status = "Failed", 
                Error = ex.Message, 
                Timestamp = DateTime.UtcNow 
            });
        }
    }

    #endregion
}

public class MetricsSimulationRequest
{
    public int KafkaMessages { get; set; } = 100;
    public int FlinkJobs { get; set; } = 2;
    public int TemporalWorkflows { get; set; } = 5;
    public int DurationSeconds { get; set; } = 10;
}

public class StartTrackingRequest
{
    public string MessageId { get; set; } = string.Empty;
    public MessageState InitialState { get; set; } = MessageState.Produced;
    public Dictionary<string, object?>? Metadata { get; set; }
}

public class UpdateStateRequest
{
    public MessageState NewState { get; set; }
    public string? Component { get; set; }
    public string? Details { get; set; }
}

public class MessageTrackingSimulationRequest
{
    public int MessageCount { get; set; } = 10;
    public bool SimulateFailures { get; set; } = true;
    public double FailureRate { get; set; } = 0.1;
}