namespace LocalTesting.WebApi.Services;

/// <summary>
/// Prometheus warmup protocol service
/// Ensures infrastructure warmup before test execution and validates real metrics are available
/// </summary>
public class PrometheusWarmupService
{
    private readonly IInfrastructureReadinessService _infrastructureService;
    private readonly PrometheusMetricsService _prometheusService;
    private readonly KafkaProducerService _kafkaService;
    private readonly ILogger<PrometheusWarmupService> _logger;

    public PrometheusWarmupService(
        IInfrastructureReadinessService infrastructureService,
        PrometheusMetricsService prometheusService,
        KafkaProducerService kafkaService,
        ILogger<PrometheusWarmupService> logger)
    {
        _infrastructureService = infrastructureService;
        _prometheusService = prometheusService;
        _kafkaService = kafkaService;
        _logger = logger;
    }

    /// <summary>
    /// Execute complete warmup protocol before test execution
    /// This ensures Prometheus contains real infrastructure metrics before testing begins
    /// </summary>
    public async Task<WarmupProtocolResult> ExecuteWarmupProtocolAsync(WarmupProtocolRequest request)
    {
        var result = new WarmupProtocolResult();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        _logger.LogInformation("🚀 Starting Prometheus warmup protocol with {MessageCount} messages", request.MessageCount);

        try
        {
            // Phase 1: Infrastructure Health Check (30 seconds timeout)
            _logger.LogInformation("📋 Phase 1: Infrastructure Health Check (30s timeout)...");
            var healthStatus = await _infrastructureService.ValidateInfrastructureAsync(TimeSpan.FromSeconds(30));
            
            if (!healthStatus.IsReady)
            {
                throw new InfrastructureNotReadyException($"Infrastructure health check failed: {healthStatus.Message}");
            }

            result.HealthCheckPassed = true;
            _logger.LogInformation("✅ Phase 1 Complete: All infrastructure components ready");

            // Phase 2: Warmup Workload Execution (60 seconds timeout)
            _logger.LogInformation("🔄 Phase 2: Warmup Workload Execution (60s timeout)...");
            
            var warmupRequest = new WarmupRequest
            {
                MessageCount = request.MessageCount,
                Topic = request.Topic ?? "infrastructure-warmup",
                TimeoutSeconds = 60,
                RequireMetricPropagation = true
            };

            var warmupResult = await _infrastructureService.ExecuteWarmupWorkloadAsync(warmupRequest);
            
            if (!warmupResult.Success)
            {
                throw new InfrastructureNotReadyException($"Warmup workload execution failed: {warmupResult.Message}");
            }

            result.WarmupExecuted = true;
            result.MessagesProduced = warmupResult.MessagesProduced;
            result.WarmupExecutionTimeSeconds = warmupResult.ExecutionTimeSeconds;
            _logger.LogInformation("✅ Phase 2 Complete: Warmup workload executed successfully");

            // Phase 3: Metric Validation (30 seconds timeout)
            _logger.LogInformation("🔍 Phase 3: Metric Validation (30s timeout)...");
            
            var validationCriteria = new ValidationCriteria
            {
                RequiredMetrics = request.RequiredMetrics ?? new[] { "kafka_producer", "flink_job", "temporal_workflow" },
                MaxAge = TimeSpan.FromMinutes(5),
                RequireNonZeroValues = true,
                MinimumMetricCount = 1
            };

            var validationResult = await _infrastructureService.ValidatePrometheusDataAsync(validationCriteria);
            
            if (!validationResult.IsValid)
            {
                throw new InfrastructureNotReadyException($"Prometheus data validation failed: {validationResult.Message}");
            }

            result.MetricsValidated = true;
            result.ValidatedMetrics = validationResult.FoundMetrics;
            result.MetricValues = validationResult.MetricValues;
            _logger.LogInformation("✅ Phase 3 Complete: Prometheus metrics validated");

            // Phase 4: Success Criteria Validation
            _logger.LogInformation("✅ Phase 4: Success Criteria Validation...");
            
            var totalMetrics = result.MetricValues.Count;
            var nonZeroMetrics = result.MetricValues.Count(kvp => kvp.Value > 0);
            
            if (totalMetrics == 0)
            {
                throw new InfrastructureNotReadyException("No metrics found in Prometheus after warmup - infrastructure execution failed");
            }

            if (request.RequireNonZeroValues && nonZeroMetrics == 0)
            {
                throw new InfrastructureNotReadyException("All metrics have zero values - infrastructure may not be processing workload");
            }

            result.Success = true;
            result.Message = $"Prometheus warmup protocol completed successfully: {totalMetrics} metrics available, {nonZeroMetrics} with real data";
            
            stopwatch.Stop();
            result.TotalExecutionTimeSeconds = stopwatch.Elapsed.TotalSeconds;

            _logger.LogInformation("🎉 Prometheus warmup protocol completed successfully in {TotalTime:F2}s", 
                result.TotalExecutionTimeSeconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.Success = false;
            result.TotalExecutionTimeSeconds = stopwatch.Elapsed.TotalSeconds;
            result.Message = $"Prometheus warmup protocol failed: {ex.Message}";
            
            _logger.LogError(ex, "❌ Prometheus warmup protocol failed after {TotalTime:F2}s", 
                result.TotalExecutionTimeSeconds);
        }

        return result;
    }

    /// <summary>
    /// Quick metric availability check without full warmup
    /// Used to verify if warmup is needed before test execution
    /// </summary>
    public async Task<bool> AreMetricsAvailableAsync(string[] requiredMetrics)
    {
        try
        {
            _logger.LogDebug("🔍 Quick metrics availability check for {MetricCount} metrics", requiredMetrics.Length);
            
            var allMetrics = await _prometheusService.GetAllMetricsAsync();
            
            if (allMetrics.Count == 0)
            {
                _logger.LogDebug("❌ No metrics available in Prometheus");
                return false;
            }

            var availableMetrics = requiredMetrics.Count(metric => 
                allMetrics.Any(kvp => kvp.Key.Contains(metric, StringComparison.OrdinalIgnoreCase) && kvp.Value > 0));

            var isAvailable = availableMetrics == requiredMetrics.Length;
            
            _logger.LogDebug("📊 Metrics availability: {Available}/{Required} required metrics found", 
                availableMetrics, requiredMetrics.Length);
            
            return isAvailable;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "❌ Error checking metrics availability");
            return false;
        }
    }

    /// <summary>
    /// Validate that infrastructure has generated fresh metrics within the specified time window
    /// </summary>
    public async Task<bool> ValidateMetricFreshnessAsync(TimeSpan maxAge)
    {
        try
        {
            _logger.LogDebug("🔍 Validating metric freshness (max age: {MaxAge})", maxAge);
            
            // For this implementation, we'll consider metrics fresh if they exist and have non-zero values
            // In a more sophisticated implementation, we would check Prometheus timestamps
            var allMetrics = await _prometheusService.GetAllMetricsAsync();
            
            var freshMetrics = allMetrics.Count(kvp => kvp.Value > 0);
            var isFresh = freshMetrics > 0;
            
            _logger.LogDebug("📊 Metric freshness validation: {FreshMetrics} metrics with recent activity", freshMetrics);
            
            return isFresh;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "❌ Error validating metric freshness");
            return false;
        }
    }
}

/// <summary>
/// Warmup protocol request configuration
/// </summary>
public class WarmupProtocolRequest
{
    public int MessageCount { get; set; } = 1000;
    public string? Topic { get; set; } = "infrastructure-warmup";
    public string[]? RequiredMetrics { get; set; } = new[] { "kafka_producer", "flink_job", "temporal_workflow" };
    public bool RequireNonZeroValues { get; set; } = true;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(3);
}

/// <summary>
/// Warmup protocol execution result
/// </summary>
public class WarmupProtocolResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    
    // Phase completion status
    public bool HealthCheckPassed { get; set; }
    public bool WarmupExecuted { get; set; }
    public bool MetricsValidated { get; set; }
    
    // Execution metrics
    public int MessagesProduced { get; set; }
    public double WarmupExecutionTimeSeconds { get; set; }
    public double TotalExecutionTimeSeconds { get; set; }
    
    // Validation results
    public string[] ValidatedMetrics { get; set; } = Array.Empty<string>();
    public Dictionary<string, double> MetricValues { get; set; } = new();
    
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}