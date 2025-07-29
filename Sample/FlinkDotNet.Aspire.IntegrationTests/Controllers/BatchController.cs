using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using FlinkDotNet.Aspire.IntegrationTests.StepDefinitions;

namespace FlinkDotNet.Aspire.IntegrationTests.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BatchController : ControllerBase
{
    private readonly ILogger<BatchController> _logger;
    private readonly IBatchProcessingService _batchProcessingService;

    public BatchController(ILogger<BatchController> logger, IBatchProcessingService batchProcessingService)
    {
        _logger = logger;
        _batchProcessingService = batchProcessingService;
    }

    [HttpPost("process")]
    public async Task<IActionResult> ProcessBatch([FromBody] BatchProcessingRequest request)
    {
        try
        {
            _logger.LogInformation("Processing batch with {MessageCount} messages", request.Messages.Length);

            // Validate batch size
            if (request.Messages.Length != 100)
            {
                _logger.LogWarning("Invalid batch size: {ActualSize}, expected: 100", request.Messages.Length);
                return BadRequest(new { Error = "Batch must contain exactly 100 messages", ActualSize = request.Messages.Length });
            }

            // Validate all messages have correlation IDs
            var messagesWithoutCorrelationId = request.Messages.Where(m => string.IsNullOrEmpty(m.CorrelationId)).ToList();
            if (messagesWithoutCorrelationId.Any())
            {
                _logger.LogWarning("Found {Count} messages without correlation IDs", messagesWithoutCorrelationId.Count);
                return BadRequest(new { Error = "All messages must have correlation IDs", MissingCount = messagesWithoutCorrelationId.Count });
            }

            // Save batch to memory for background processing (no immediate response)
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await _batchProcessingService.SaveBatchToMemoryAsync(request.Messages);
            stopwatch.Stop();

            _logger.LogInformation("Batch saved to memory for background processing in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

            // Return success status without processed data
            var response = new BatchProcessingResponse
            {
                Messages = Array.Empty<ComplexLogicMessage>(), // No immediate response data
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                ProcessedAt = DateTime.UtcNow,
                BatchId = Guid.NewGuid().ToString(),
                Status = "Batch saved for background processing"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing batch");
            return StatusCode(500, new { Error = "Internal server error during batch processing", Details = ex.Message });
        }
    }

    [HttpGet("pull")]
    public async Task<IActionResult> PullProcessedMessages([FromQuery] int maxMessages = 100)
    {
        try
        {
            _logger.LogInformation("Pulling up to {MaxMessages} processed messages", maxMessages);

            // Allow Flink to pull processed messages from memory
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var messages = await _batchProcessingService.PullProcessedMessagesAsync(maxMessages);
            stopwatch.Stop();

            _logger.LogInformation("Pulled {MessageCount} processed messages in {ElapsedMs}ms", messages.Length, stopwatch.ElapsedMilliseconds);

            return Ok(new
            {
                Messages = messages,
                Count = messages.Length,
                PulledAt = DateTime.UtcNow,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pulling processed messages");
            return StatusCode(500, new { Error = "Internal server error during message pulling", Details = ex.Message });
        }
    }

    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
    }

    [HttpGet("stats")]
    public IActionResult GetProcessingStats()
    {
        // Return batch processing statistics for the new architecture
        return Ok(new
        {
            MaxBatchSize = 100,
            SupportedOperations = new[] { "process", "pull" },
            ProcessingMode = "Background with Flink pulling",
            AverageProcessingTimeMs = 50,
            Status = "Available"
        });
    }
}

public class BatchProcessingRequest
{
    [Required]
    public ComplexLogicMessage[] Messages { get; set; } = Array.Empty<ComplexLogicMessage>();
}

public class BatchProcessingResponse
{
    public ComplexLogicMessage[] Messages { get; set; } = Array.Empty<ComplexLogicMessage>();
    public long ProcessingTimeMs { get; set; }
    public DateTime ProcessedAt { get; set; }
    public string BatchId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}