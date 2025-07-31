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
    private readonly AspireHealthCheckService _healthCheckService;
    private readonly ILogger<ComplexLogicStressTestController> _logger;

    public ComplexLogicStressTestController(
        ComplexLogicStressTestService stressTestService,
        SecurityTokenManagerService tokenManager,
        KafkaProducerService kafkaProducer,
        FlinkJobManagementService flinkJobService,
        BackpressureMonitoringService backpressureService,
        AspireHealthCheckService healthCheckService,
        ILogger<ComplexLogicStressTestController> logger)
    {
        _stressTestService = stressTestService;
        _tokenManager = tokenManager;
        _kafkaProducer = kafkaProducer;
        _flinkJobService = flinkJobService;
        _backpressureService = backpressureService;
        _healthCheckService = healthCheckService;
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
            
            // Real health check of all Aspire services
            var healthCheckResults = await _healthCheckService.CheckAllServicesAsync();
            
            var overallHealth = healthCheckResults["overallHealth"] as dynamic;
            var isHealthy = overallHealth?.IsHealthy ?? false;
            
            var status = isHealthy ? "Ready" : "Degraded";
            var metrics = healthCheckResults;

            if (isHealthy)
            {
                _logger.LogInformation("✅ Aspire test environment setup completed - all services healthy");
            }
            else
            {
                _logger.LogWarning("⚠️ Aspire test environment setup completed with issues - some services unhealthy");
            }

            return Ok(new { Status = status, Metrics = metrics });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to setup Aspire test environment");
            return StatusCode(500, new { 
                Status = "Failed", 
                Metrics = new { Error = ex.Message, Timestamp = DateTime.UtcNow } 
            });
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
        
        var statusValue = status.IsBackpressureActive ? "Active" : "Inactive";
        
        return Ok(new { Status = statusValue, Metrics = metrics });
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

            var startTime = DateTime.UtcNow;
            var testId = request.TestId ?? Guid.NewGuid().ToString();
            
            // Use Kafka producer for real message production
            var messages = await _stressTestService.ProduceMessagesAsync(testId, request.MessageCount);
            
            var endTime = DateTime.UtcNow;
            var totalDuration = endTime - startTime;
            var messagesPerSecond = messages.Count / totalDuration.TotalSeconds;
            
            // Verify Kafka broker health and message persistence
            var healthCheck = await _healthCheckService.CheckAllServicesAsync();
            var kafkaHealth = healthCheck["services"] as Dictionary<string, object>;
            var kafkaBrokerStatus = kafkaHealth?["kafkaBrokers"] as ServiceHealthStatus;
            
            var metrics = new Dictionary<string, object>
            {
                ["messageCount"] = messages.Count,
                ["totalDurationSeconds"] = Math.Round(totalDuration.TotalSeconds, 2),
                ["messagesPerSecond"] = Math.Round(messagesPerSecond, 2),
                ["throughputMBps"] = Math.Round((messages.Count * 1024) / (1024 * 1024 * totalDuration.TotalSeconds), 2), // Estimate 1KB per message
                ["kafkaBrokersHealthy"] = kafkaBrokerStatus?.IsHealthy ?? false,
                ["kafkaBrokerCount"] = kafkaBrokerStatus?.Details.TryGetValue("brokerCount", out var count) == true ? count : 0,
                ["correlationIdSample"] = messages.Take(3).Select(m => m.CorrelationId).ToArray(),
                ["messageIdRange"] = new { First = messages.FirstOrDefault()?.MessageId, Last = messages.LastOrDefault()?.MessageId },
                ["batchCount"] = messages.Select(m => m.BatchNumber).Distinct().Count(),
                ["averageMessageSize"] = 1024, // Estimated
                ["testId"] = testId,
                ["timestamp"] = DateTime.UtcNow
            };

            var status = request.MessageCount >= 1000000 ? "1M_Messages_Produced" : "Messages_Produced";
            
            _logger.LogInformation("✅ Message production completed: {MessageCount:N0} messages in {Duration:F2}s ({Throughput:F2} msgs/sec)", 
                messages.Count, totalDuration.TotalSeconds, messagesPerSecond);
            
            if (request.MessageCount >= 1000000)
            {
                _logger.LogInformation("🎉 1 MILLION MESSAGE MILESTONE: Produced {MessageCount:N0} messages in {Duration:F2} seconds", 
                    messages.Count, totalDuration.TotalSeconds);
            }

            return Ok(new { Status = status, Metrics = metrics });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to produce messages");
            return StatusCode(500, new { 
                Status = "Failed", 
                Metrics = new { Error = ex.Message, Timestamp = DateTime.UtcNow } 
            });
        }
    }

    [HttpPost("step4/produce-1million-messages")]
    [SwaggerOperation(
        Summary = "Step 4: Performance Test - Produce 1 Million Messages",
        Description = "Specialized endpoint to produce exactly 1 million messages and measure performance with 3 Kafka brokers"
    )]
    [SwaggerResponse(200, "1 million messages produced successfully")]
    [SwaggerResponse(500, "1 million message test failed")]
    public async Task<IActionResult> Produce1MillionMessages()
    {
        try
        {
            _logger.LogInformation("🚀 Starting 1 MILLION MESSAGE PERFORMANCE TEST");

            var testId = $"1M-test-{DateTime.UtcNow:yyyyMMddHHmmss}";
            var messageCount = 1000000;
            
            // Pre-test health check
            var preHealthCheck = await _healthCheckService.CheckAllServicesAsync();
            var preKafkaHealth = preHealthCheck["services"] as Dictionary<string, object>;
            var preKafkaBrokerStatus = preKafkaHealth?["kafkaBrokers"] as ServiceHealthStatus;
            
            if (preKafkaBrokerStatus?.IsHealthy != true)
            {
                return StatusCode(500, new { 
                    Status = "Failed", 
                    Metrics = new { Error = "Kafka brokers not healthy before test", Timestamp = DateTime.UtcNow } 
                });
            }

            var startTime = DateTime.UtcNow;
            _logger.LogInformation("⏱️ Production start time: {StartTime}", startTime);
            
            // Produce 1 million messages
            var messages = await _stressTestService.ProduceMessagesAsync(testId, messageCount);
            
            var endTime = DateTime.UtcNow;
            var totalDuration = endTime - startTime;
            var messagesPerSecond = messages.Count / totalDuration.TotalSeconds;
            
            // Post-test health check
            var postHealthCheck = await _healthCheckService.CheckAllServicesAsync();
            var postKafkaHealth = postHealthCheck["services"] as Dictionary<string, object>;
            var postKafkaBrokerStatus = postKafkaHealth?["kafkaBrokers"] as ServiceHealthStatus;
            
            var metrics = new Dictionary<string, object>
            {
                ["testType"] = "1_Million_Message_Performance_Test",
                ["messageCount"] = messages.Count,
                ["targetMessageCount"] = messageCount,
                ["testSuccessful"] = messages.Count == messageCount,
                ["totalDurationSeconds"] = Math.Round(totalDuration.TotalSeconds, 2),
                ["totalDurationMinutes"] = Math.Round(totalDuration.TotalMinutes, 2),
                ["messagesPerSecond"] = Math.Round(messagesPerSecond, 2),
                ["throughputMBps"] = Math.Round((messages.Count * 1024) / (1024 * 1024 * totalDuration.TotalSeconds), 2),
                ["preTestKafkaBrokersHealthy"] = preKafkaBrokerStatus?.IsHealthy ?? false,
                ["postTestKafkaBrokersHealthy"] = postKafkaBrokerStatus?.IsHealthy ?? false,
                ["kafkaBrokerCount"] = postKafkaBrokerStatus?.Details.TryGetValue("brokerCount", out var count) == true ? count : 0,
                ["correlationIdVerification"] = new {
                    First = messages.FirstOrDefault()?.CorrelationId,
                    Last = messages.LastOrDefault()?.CorrelationId,
                    Sample = messages.Where((_, i) => i % 100000 == 0).Select(m => m.CorrelationId).ToArray() // Every 100k messages
                },
                ["messageIdRange"] = new { 
                    First = messages.FirstOrDefault()?.MessageId, 
                    Last = messages.LastOrDefault()?.MessageId,
                    ExpectedLast = messageCount
                },
                ["batchCount"] = messages.Select(m => m.BatchNumber).Distinct().Count(),
                ["estimatedDataSizeMB"] = Math.Round((messages.Count * 1024) / (1024.0 * 1024.0), 2),
                ["performanceRating"] = messagesPerSecond > 10000 ? "Excellent" : messagesPerSecond > 5000 ? "Good" : "Needs_Optimization",
                ["testId"] = testId,
                ["startTime"] = startTime,
                ["endTime"] = endTime,
                ["timestamp"] = DateTime.UtcNow
            };

            var status = messages.Count == messageCount ? "1M_Messages_Success" : "1M_Messages_Partial";
            
            _logger.LogInformation("🎉 1 MILLION MESSAGE TEST COMPLETED!");
            _logger.LogInformation("📊 Results: {MessageCount:N0} messages in {Duration:F2} seconds ({Throughput:F2} msgs/sec)", 
                messages.Count, totalDuration.TotalSeconds, messagesPerSecond);
            _logger.LogInformation("📈 Throughput: {ThroughputMBps:F2} MB/s", 
                (messages.Count * 1024) / (1024 * 1024 * totalDuration.TotalSeconds));

            return Ok(new { Status = status, Metrics = metrics });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 1 Million message test failed");
            return StatusCode(500, new { 
                Status = "Failed", 
                Metrics = new { 
                    Error = ex.Message, 
                    TestType = "1_Million_Message_Performance_Test",
                    Timestamp = DateTime.UtcNow 
                } 
            });
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