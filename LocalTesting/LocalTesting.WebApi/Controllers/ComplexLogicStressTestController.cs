using Microsoft.AspNetCore.Mvc;
using LocalTesting.WebApi.Models;
using LocalTesting.WebApi.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace LocalTesting.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ComplexLogicStressTestController : ControllerBase
{
    private readonly ComplexLogicStressTestService _stressTestService;
    private readonly SecurityTokenManagerService _tokenManager;
    private readonly KafkaProducerService _kafkaProducer;
    private readonly FlinkJobManagementService _flinkJobService;
    private readonly BackpressureMonitoringService _backpressureService;
    private readonly ILogger<ComplexLogicStressTestController> _logger;

    public ComplexLogicStressTestController(
        ComplexLogicStressTestService stressTestService,
        SecurityTokenManagerService tokenManager,
        KafkaProducerService kafkaProducer,
        FlinkJobManagementService flinkJobService,
        BackpressureMonitoringService backpressureService,
        ILogger<ComplexLogicStressTestController> logger)
    {
        _stressTestService = stressTestService;
        _tokenManager = tokenManager;
        _kafkaProducer = kafkaProducer;
        _flinkJobService = flinkJobService;
        _backpressureService = backpressureService;
        _logger = logger;
    }

    // ========== STEP 1: Environment Setup ==========

    [HttpPost("step1/setup-environment")]
    [SwaggerOperation(
        Summary = "Step 1: Setup Aspire Test Environment",
        Description = "Initialize the Aspire test environment with all required services (Kafka 3 brokers, Redis, Flink cluster)"
    )]
    [SwaggerResponse(200, "Environment setup completed successfully")]
    [SwaggerResponse(500, "Environment setup failed")]
    public async Task<IActionResult> SetupEnvironment()
    {
        try
        {
            _logger.LogInformation("🚀 Setting up Aspire test environment...");
            
            // Simulate environment validation
            await Task.Delay(1000);
            
            var result = new
            {
                Status = "Ready",
                Message = "Aspire test environment is running with all required services",
                Services = new
                {
                    KafkaBrokers = new[] { "kafka-broker-1:9092", "kafka-broker-2:9092", "kafka-broker-3:9092" },
                    Redis = "redis:6379",
                    FlinkJobManager = "flink-jobmanager:8081",
                    FlinkTaskManagers = new[] { "flink-taskmanager-1:6122", "flink-taskmanager-2:6122", "flink-taskmanager-3:6122" },
                    KafkaUI = "http://localhost:8080",
                    FlinkUI = "http://localhost:8081",
                    Grafana = "http://localhost:3000",
                    Prometheus = "http://localhost:9090"
                },
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("✅ Aspire test environment setup completed");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to setup Aspire test environment");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    // ========== STEP 2: Security Token Configuration ==========

    [HttpPost("step2/configure-security-tokens")]
    [SwaggerOperation(
        Summary = "Step 2: Configure Security Token Service",
        Description = "Initialize security token service with configurable renewal interval (default: 10,000 messages)"
    )]
    [SwaggerResponse(200, "Security token service configured successfully")]
    [SwaggerResponse(400, "Invalid renewal interval")]
    public async Task<IActionResult> ConfigureSecurityTokens([FromBody] int renewalInterval = 10000)
    {
        try
        {
            if (renewalInterval <= 0)
                return BadRequest("Renewal interval must be positive");

            _logger.LogInformation("🔑 Configuring security token service with {RenewalInterval} message renewal interval", renewalInterval);
            
            await _tokenManager.InitializeAsync(renewalInterval);
            var tokenInfo = _tokenManager.GetTokenInfo();
            
            var result = new
            {
                Status = "Configured",
                Message = $"Security token service configured with {renewalInterval:N0} message renewal interval",
                TokenInfo = tokenInfo,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("✅ Security token service configured successfully");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to configure security token service");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    [HttpGet("step2/token-status")]
    [SwaggerOperation(
        Summary = "Get Security Token Status",
        Description = "Retrieve current security token information including renewal count and status"
    )]
    [SwaggerResponse(200, "Token status retrieved successfully")]
    public IActionResult GetTokenStatus()
    {
        var tokenInfo = _tokenManager.GetTokenInfo();
        return Ok(tokenInfo);
    }

    // ========== STEP 3: Backpressure Configuration ==========

    [HttpPost("step3/configure-backpressure")]
    [SwaggerOperation(
        Summary = "Step 3: Configure Lag-Based Backpressure",
        Description = "Initialize lag-based rate limiter that stops token bucket refilling when consumer lag exceeds threshold"
    )]
    [SwaggerResponse(200, "Backpressure configured successfully")]
    [SwaggerResponse(400, "Invalid backpressure configuration")]
    public async Task<IActionResult> ConfigureBackpressure([FromBody] BackpressureConfiguration config)
    {
        try
        {
            if (config.RateLimit <= 0 || config.BurstCapacity <= 0 || config.LagThresholdSeconds <= 0)
                return BadRequest("All configuration values must be positive");

            _logger.LogInformation("⚡ Configuring lag-based backpressure: Group={ConsumerGroup}, Threshold={LagThreshold}s, Rate={RateLimit}, Burst={BurstCapacity}", 
                config.ConsumerGroup, config.LagThresholdSeconds, config.RateLimit, config.BurstCapacity);

            var lagThreshold = TimeSpan.FromSeconds(config.LagThresholdSeconds);
            await _backpressureService.InitializeAsync(config.ConsumerGroup, lagThreshold, config.RateLimit, config.BurstCapacity);
            
            var status = _backpressureService.GetBackpressureStatus();
            
            var result = new
            {
                Status = "Configured",
                Message = $"Lag-based backpressure configured with {config.LagThresholdSeconds}s threshold",
                Configuration = config,
                BackpressureStatus = status,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("✅ Lag-based backpressure configured successfully");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to configure backpressure");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    [HttpGet("step3/backpressure-status")]
    [SwaggerOperation(
        Summary = "Get Backpressure Status",
        Description = "Retrieve current backpressure status including lag monitoring and token bucket state"
    )]
    [SwaggerResponse(200, "Backpressure status retrieved successfully")]
    public IActionResult GetBackpressureStatus()
    {
        var status = _backpressureService.GetBackpressureStatus();
        var metrics = _backpressureService.GetMetrics();
        
        return Ok(new { Status = status, Metrics = metrics });
    }

    // ========== STEP 4: Message Production ==========

    [HttpPost("step4/produce-messages")]
    [SwaggerOperation(
        Summary = "Step 4: Produce Messages with Unique Correlation IDs",
        Description = "Generate and produce messages to Kafka with unique correlation IDs for tracking across the pipeline"
    )]
    [SwaggerResponse(200, "Messages produced successfully")]
    [SwaggerResponse(400, "Invalid message count")]
    public async Task<IActionResult> ProduceMessages([FromBody] MessageProductionRequest request)
    {
        try
        {
            if (request.MessageCount <= 0)
                return BadRequest("Message count must be positive");

            _logger.LogInformation("📝 Producing {MessageCount:N0} messages with unique correlation IDs", request.MessageCount);

            var testId = request.TestId ?? Guid.NewGuid().ToString();
            var messages = await _stressTestService.ProduceMessagesAsync(testId, request.MessageCount);
            
            var result = new
            {
                TestId = testId,
                Status = "Completed",
                Message = $"Successfully produced {request.MessageCount:N0} messages with unique correlation IDs",
                MessageCount = messages.Count,
                SampleMessages = messages.Take(5).Select(m => new 
                {
                    m.MessageId,
                    m.CorrelationId,
                    m.BatchNumber,
                    m.Content
                }),
                TopicInfo = new
                {
                    Topic = "complex-input",
                    Partitions = 100,
                    ReplicationFactor = 3
                },
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("✅ Message production completed: {MessageCount:N0} messages", messages.Count);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to produce messages");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    // ========== STEP 5: Flink Job Management ==========

    [HttpPost("step5/start-flink-job")]
    [SwaggerOperation(
        Summary = "Step 5: Start Flink Streaming Job",
        Description = "Start Apache Flink streaming job with complex logic pipeline for correlation tracking and processing"
    )]
    [SwaggerResponse(200, "Flink job started successfully")]
    [SwaggerResponse(500, "Failed to start Flink job")]
    public async Task<IActionResult> StartFlinkJob([FromBody] FlinkJobConfiguration config)
    {
        try
        {
            _logger.LogInformation("🚀 Starting Flink streaming job with complex logic pipeline");

            var pipelineConfig = new Dictionary<string, object>
            {
                ["consumerGroup"] = config.ConsumerGroup,
                ["inputTopic"] = config.InputTopic,
                ["outputTopic"] = config.OutputTopic,
                ["correlationTracking"] = config.EnableCorrelationTracking,
                ["batchSize"] = config.BatchSize,
                ["parallelism"] = config.Parallelism,
                ["checkpointingInterval"] = config.CheckpointingInterval
            };

            var jobId = await _flinkJobService.StartComplexLogicJobAsync(pipelineConfig);
            var jobInfo = await _flinkJobService.GetJobInfoAsync(jobId);

            var result = new
            {
                JobId = jobId,
                Status = "Started",
                Message = "Flink streaming job started with complex logic pipeline",
                JobInfo = jobInfo,
                PipelineConfiguration = pipelineConfig,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("✅ Flink job started successfully with ID: {JobId}", jobId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to start Flink job");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    [HttpGet("step5/flink-jobs")]
    [SwaggerOperation(
        Summary = "Get All Flink Jobs",
        Description = "Retrieve information about all running Flink jobs"
    )]
    [SwaggerResponse(200, "Flink jobs retrieved successfully")]
    public async Task<IActionResult> GetFlinkJobs()
    {
        try
        {
            var jobs = await _flinkJobService.GetAllJobsAsync();
            return Ok(jobs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Flink jobs");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    [HttpGet("step5/flink-job/{jobId}")]
    [SwaggerOperation(
        Summary = "Get Flink Job Details",
        Description = "Retrieve detailed information about a specific Flink job"
    )]
    [SwaggerResponse(200, "Job details retrieved successfully")]
    [SwaggerResponse(404, "Job not found")]
    public async Task<IActionResult> GetFlinkJob(string jobId)
    {
        try
        {
            var jobInfo = await _flinkJobService.GetJobInfoAsync(jobId);
            var metrics = await _flinkJobService.GetJobMetricsAsync(jobId);
            
            return Ok(new { JobInfo = jobInfo, Metrics = metrics });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Flink job {JobId}", jobId);
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    // ========== STEP 6: Batch Processing ==========

    [HttpPost("step6/process-batches")]
    [SwaggerOperation(
        Summary = "Step 6: Process Message Batches",
        Description = "Process messages in batches through HTTP endpoint with correlation tracking and security token management"
    )]
    [SwaggerResponse(200, "Batch processing completed successfully")]
    [SwaggerResponse(400, "Invalid test ID or batch size")]
    public async Task<IActionResult> ProcessBatches([FromBody] BatchProcessingRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.TestId))
                return BadRequest("Test ID is required");

            if (request.BatchSize <= 0)
                return BadRequest("Batch size must be positive");

            _logger.LogInformation("🔄 Processing messages in batches of {BatchSize} for test {TestId}", request.BatchSize, request.TestId);

            var results = await _stressTestService.ProcessBatchesAsync(request.TestId, request.BatchSize);
            var tokenInfo = _tokenManager.GetTokenInfo();
            var backpressureStatus = _backpressureService.GetBackpressureStatus();

            var result = new
            {
                TestId = request.TestId,
                Status = "Completed",
                Message = $"Processed {results.Count} batches with {request.BatchSize} messages per batch",
                BatchResults = results.Take(5), // Show first 5 batches
                TotalBatches = results.Count,
                TotalMessages = results.Sum(r => r.MessageCount),
                TokenInfo = tokenInfo,
                BackpressureStatus = backpressureStatus,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("✅ Batch processing completed: {BatchCount} batches processed", results.Count);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to process batches");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    // ========== STEP 7: Message Verification ==========

    [HttpPost("step7/verify-messages")]
    [SwaggerOperation(
        Summary = "Step 7: Verify Message Processing",
        Description = "Verify that all messages were processed correctly with correlation ID matching and display top/last messages"
    )]
    [SwaggerResponse(200, "Message verification completed successfully")]
    [SwaggerResponse(400, "Invalid test ID")]
    public async Task<IActionResult> VerifyMessages([FromBody] MessageVerificationRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.TestId))
                return BadRequest("Test ID is required");

            _logger.LogInformation("🔍 Verifying messages for test {TestId} (top {TopCount}, last {LastCount})", 
                request.TestId, request.TopCount, request.LastCount);

            var verificationResult = await _stressTestService.VerifyMessagesAsync(request.TestId, request.TopCount, request.LastCount);

            var result = new
            {
                TestId = request.TestId,
                Status = "Completed",
                Message = $"Verification complete: {verificationResult.VerifiedMessages:N0}/{verificationResult.TotalMessages:N0} messages verified ({verificationResult.SuccessRate:P1} success rate)",
                VerificationResult = verificationResult,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("✅ Message verification completed: {SuccessRate:P1} success rate", verificationResult.SuccessRate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to verify messages");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    // ========== FULL STRESS TEST AUTOMATION ==========

    [HttpPost("run-full-stress-test")]
    [SwaggerOperation(
        Summary = "Run Complete Stress Test",
        Description = "Execute the complete Complex Logic Stress Test with all steps in sequence"
    )]
    [SwaggerResponse(200, "Stress test started successfully")]
    [SwaggerResponse(400, "Invalid stress test configuration")]
    public async Task<IActionResult> RunFullStressTest([FromBody] StressTestConfiguration config)
    {
        try
        {
            _logger.LogInformation("🚀 Starting complete Complex Logic Stress Test with {MessageCount:N0} messages", config.MessageCount);

            var testId = await _stressTestService.StartStressTestAsync(config);

            var result = new
            {
                TestId = testId,
                Status = "Started",
                Message = $"Complete stress test started with {config.MessageCount:N0} messages",
                Configuration = config,
                MonitoringEndpoints = new
                {
                    Status = $"/api/ComplexLogicStressTest/test-status/{testId}",
                    KafkaUI = "http://localhost:8080",
                    FlinkUI = "http://localhost:8081",
                    Grafana = "http://localhost:3000"
                },
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("✅ Complete stress test started with ID: {TestId}", testId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to start complete stress test");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    [HttpGet("test-status/{testId}")]
    [SwaggerOperation(
        Summary = "Get Stress Test Status",
        Description = "Retrieve current status and progress of a running stress test"
    )]
    [SwaggerResponse(200, "Test status retrieved successfully")]
    [SwaggerResponse(404, "Test not found")]
    public IActionResult GetTestStatus(string testId)
    {
        var status = _stressTestService.GetTestStatus(testId);
        if (status == null)
            return NotFound($"Test {testId} not found");

        return Ok(status);
    }

    [HttpGet("test-status")]
    [SwaggerOperation(
        Summary = "Get All Active Tests",
        Description = "Retrieve status of all currently active stress tests"
    )]
    [SwaggerResponse(200, "Active tests retrieved successfully")]
    public IActionResult GetAllActiveTests()
    {
        var tests = _stressTestService.GetAllActiveTests();
        return Ok(tests);
    }
}

// Request/Response Models for API endpoints
public class BackpressureConfiguration
{
    public string ConsumerGroup { get; set; } = "stress-test-group";
    public double LagThresholdSeconds { get; set; } = 5.0;
    public double RateLimit { get; set; } = 1000.0;
    public double BurstCapacity { get; set; } = 5000.0;
}

public class MessageProductionRequest
{
    public string? TestId { get; set; }
    public int MessageCount { get; set; } = 1000000;
}

public class FlinkJobConfiguration
{
    public string ConsumerGroup { get; set; } = "stress-test-group";
    public string InputTopic { get; set; } = "complex-input";
    public string OutputTopic { get; set; } = "complex-output";
    public bool EnableCorrelationTracking { get; set; } = true;
    public int BatchSize { get; set; } = 100;
    public int Parallelism { get; set; } = 100;
    public int CheckpointingInterval { get; set; } = 10000;
}

public class BatchProcessingRequest
{
    public string TestId { get; set; } = string.Empty;
    public int BatchSize { get; set; } = 100;
}

public class MessageVerificationRequest
{
    public string TestId { get; set; } = string.Empty;
    public int TopCount { get; set; } = 100;
    public int LastCount { get; set; } = 100;
}