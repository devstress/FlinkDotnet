using Microsoft.AspNetCore.Mvc;
using LocalTesting.WebApi.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace LocalTesting.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ObservabilityController : ControllerBase
{
    private readonly ObservabilityMetricsService _metricsService;
    private readonly ILogger<ObservabilityController> _logger;

    public ObservabilityController(ObservabilityMetricsService metricsService, ILogger<ObservabilityController> logger)
    {
        _metricsService = metricsService;
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
}

public class MetricsSimulationRequest
{
    public int KafkaMessages { get; set; } = 100;
    public int FlinkJobs { get; set; } = 2;
    public int TemporalWorkflows { get; set; } = 5;
    public int DurationSeconds { get; set; } = 10;
}