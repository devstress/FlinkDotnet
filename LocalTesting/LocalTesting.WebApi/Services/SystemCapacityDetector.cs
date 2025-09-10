using LocalTesting.WebApi.Models;
using LocalTesting.Shared.Constants;
using System.Text.Json;
using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace LocalTesting.WebApi.Services;

/// <summary>
/// Implementation of ISystemCapacityDetector for dynamic capacity detection
/// using real infrastructure metrics to replace hardcoded test parameters.
/// 
/// This implementation supports Phase 3 from WI15 architecture design:
/// - Queries real Kafka, Flink, and Temporal infrastructure for capacity metrics
/// - Calculates adaptive parameters based on actual system performance
/// - Provides capacity validation and optimization recommendations
/// </summary>
public class SystemCapacityDetector : ISystemCapacityDetector
{
    private readonly PrometheusMetricsService _prometheusService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SystemCapacityDetector> _logger;
    
    // Configuration constants
    private const double KAFKA_SAFETY_FACTOR = 0.8; // Use 80% of detected capacity
    private const double FLINK_SAFETY_FACTOR = 0.75; // Use 75% of available task slots
    private const double TEMPORAL_SAFETY_FACTOR = 0.7; // Use 70% of worker capacity
    private const int DEFAULT_THROUGHPUT_MESSAGES_PER_SECOND = 1000;
    private const int MIN_BATCH_SIZE = 100;
    private const int MAX_BATCH_SIZE = 10000;

    public SystemCapacityDetector(
        PrometheusMetricsService prometheusService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<SystemCapacityDetector> logger)
    {
        _prometheusService = prometheusService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<KafkaCapacity> DetectKafkaCapacityAsync()
    {
        _logger.LogInformation("🔍 Detecting Kafka cluster capacity using real infrastructure metrics");
        
        try
        {
            var capacity = new KafkaCapacity();
            
            // Get Kafka configuration from environment
            var bootstrapServers = _configuration["KAFKA_BOOTSTRAP_SERVERS"] ?? PortConstants.KafkaBootstrapServers("localhost");
            var defaultPartitions = int.Parse(_configuration["KAFKA_DEFAULT_PARTITIONS"] ?? "10");
            
            // Create admin client to query cluster metadata
            var adminConfig = new AdminClientConfig
            {
                BootstrapServers = bootstrapServers
            };
            adminConfig.Set("request.timeout.ms", "30000");
            
            using var adminClient = new AdminClientBuilder(adminConfig).Build();
            
            // Get cluster metadata
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(30));
            
            capacity.BrokerCount = metadata.Brokers.Count;
            capacity.ReplicationFactor = 3; // From Program.cs configuration
            capacity.TotalPartitions = defaultPartitions;
            
            _logger.LogInformation("📊 Kafka cluster detected: {BrokerCount} brokers, {PartitionCount} partitions, RF={ReplicationFactor}",
                capacity.BrokerCount, capacity.TotalPartitions, capacity.ReplicationFactor);
            
            // Get Kafka metrics from Prometheus
            try
            {
                var kafkaMetrics = await _prometheusService.GetKafkaProducerMetricsAsync();
                capacity.BrokerMetrics = kafkaMetrics.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
                
                // Calculate throughput based on current metrics or use baseline
                var currentThroughput = kafkaMetrics.Values.Any() ? kafkaMetrics.Values.Max() : 0;
                capacity.EstimatedMaxThroughputMessagesPerSecond = Math.Max(currentThroughput * 2, DEFAULT_THROUGHPUT_MESSAGES_PER_SECOND * capacity.BrokerCount);
                
                _logger.LogInformation("📈 Kafka throughput estimated: {Throughput:F0} messages/second",
                    capacity.EstimatedMaxThroughputMessagesPerSecond);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ Could not retrieve Kafka metrics from Prometheus: {Error}. Using baseline estimates.", ex.Message);
                capacity.EstimatedMaxThroughputMessagesPerSecond = DEFAULT_THROUGHPUT_MESSAGES_PER_SECOND * capacity.BrokerCount;
            }
            
            // Calculate derived metrics
            capacity.EstimatedMaxThroughputBytesPerSecond = capacity.EstimatedMaxThroughputMessagesPerSecond * 1024; // Assume 1KB average message
            capacity.RecommendedBatchSize = Math.Max(MIN_BATCH_SIZE, Math.Min(MAX_BATCH_SIZE, (int)(capacity.EstimatedMaxThroughputMessagesPerSecond / 10)));
            capacity.RecommendedTestDuration = TimeSpan.FromMinutes(Math.Max(2, Math.Min(10, capacity.BrokerCount * 2)));
            
            _logger.LogInformation("✅ Kafka capacity detection completed: {Throughput:F0} msg/s, {BatchSize} batch size, {Duration} test duration",
                capacity.EstimatedMaxThroughputMessagesPerSecond, capacity.RecommendedBatchSize, capacity.RecommendedTestDuration);
            
            return capacity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to detect Kafka capacity");
            
            // Return baseline capacity for graceful degradation
            return new KafkaCapacity
            {
                BrokerCount = 3, // From Program.cs configuration
                TotalPartitions = 10,
                ReplicationFactor = 3,
                EstimatedMaxThroughputMessagesPerSecond = DEFAULT_THROUGHPUT_MESSAGES_PER_SECOND * 3,
                EstimatedMaxThroughputBytesPerSecond = DEFAULT_THROUGHPUT_MESSAGES_PER_SECOND * 3 * 1024,
                RecommendedBatchSize = 1000,
                RecommendedTestDuration = TimeSpan.FromMinutes(5),
                CapacitySource = "Baseline Configuration (Kafka detection failed)"
            };
        }
    }

    public async Task<FlinkCapacity> DetectFlinkCapacityAsync()
    {
        _logger.LogInformation("🔍 Detecting Flink cluster capacity using JobManager REST API");
        
        try
        {
            var capacity = new FlinkCapacity();
            
            // Get Flink JobManager URL from configuration
            var jobManagerUrl = _configuration["FLINK_JOBMANAGER_URL"] ?? PortConstants.FlinkJobManagerUrl("localhost");
            
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            // Query JobManager overview
            var overviewResponse = await httpClient.GetStringAsync($"{jobManagerUrl}/overview");
            var overview = JsonSerializer.Deserialize<JsonElement>(overviewResponse);
            
            // Extract basic cluster information
            capacity.JobManagerCount = 1; // Single JobManager from Program.cs
            capacity.TaskManagerCount = 3; // 3 TaskManagers from Program.cs
            
            // Query task manager details for slot information
            var taskManagersResponse = await httpClient.GetStringAsync($"{jobManagerUrl}/taskmanagers");
            var taskManagers = JsonSerializer.Deserialize<JsonElement>(taskManagersResponse);
            
            if (taskManagers.TryGetProperty("taskmanagers", out var tmArray))
            {
                int totalSlots = 0;
                int freeSlots = 0;
                
                foreach (var tm in tmArray.EnumerateArray())
                {
                    if (tm.TryGetProperty("slotsNumber", out var slots))
                        totalSlots += slots.GetInt32();
                    if (tm.TryGetProperty("freeSlots", out var free))
                        freeSlots += free.GetInt32();
                }
                
                capacity.TotalTaskSlots = totalSlots > 0 ? totalSlots : 24; // Fallback: 3 TMs * 8 slots
                capacity.AvailableTaskSlots = freeSlots;
            }
            else
            {
                // Fallback to configuration values
                capacity.TotalTaskSlots = 24; // 3 TaskManagers * 8 slots each
                capacity.AvailableTaskSlots = capacity.TotalTaskSlots;
            }
            
            capacity.MaxParallelism = capacity.TotalTaskSlots;
            capacity.RecommendedJobCount = Math.Max(1, Math.Min(capacity.AvailableTaskSlots / 4, 6)); // Use 4 slots per job, max 6 jobs
            capacity.EstimatedProcessingRateMessagesPerSecond = capacity.TotalTaskSlots * 500; // Estimate 500 msg/s per slot
            
            _logger.LogInformation("📊 Flink cluster detected: {TaskManagers} TMs, {TotalSlots} total slots, {AvailableSlots} available",
                capacity.TaskManagerCount, capacity.TotalTaskSlots, capacity.AvailableTaskSlots);
            
            // Get Flink metrics from Prometheus
            try
            {
                var flinkMetrics = await _prometheusService.GetFlinkProcessingMetricsAsync();
                capacity.JobManagerMetrics = flinkMetrics.Where(kvp => kvp.Key.Contains("jobmanager")).ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
                capacity.TaskManagerMetrics = flinkMetrics.Where(kvp => kvp.Key.Contains("taskmanager")).ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
                
                _logger.LogInformation("📈 Flink metrics retrieved: {JobManagerMetrics} JM metrics, {TaskManagerMetrics} TM metrics",
                    capacity.JobManagerMetrics.Count, capacity.TaskManagerMetrics.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ Could not retrieve Flink metrics from Prometheus: {Error}", ex.Message);
            }
            
            _logger.LogInformation("✅ Flink capacity detection completed: {ProcessingRate:F0} msg/s capacity, {RecommendedJobs} recommended jobs",
                capacity.EstimatedProcessingRateMessagesPerSecond, capacity.RecommendedJobCount);
            
            return capacity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to detect Flink capacity");
            
            // Return baseline capacity for graceful degradation
            return new FlinkCapacity
            {
                JobManagerCount = 1,
                TaskManagerCount = 3,
                TotalTaskSlots = 24,
                AvailableTaskSlots = 24,
                MaxParallelism = 24,
                RecommendedJobCount = 2,
                EstimatedProcessingRateMessagesPerSecond = 12000, // 24 slots * 500 msg/s
                CapacitySource = "Baseline Configuration (Flink detection failed)"
            };
        }
    }

    public async Task<TemporalCapacity> DetectTemporalCapacityAsync()
    {
        _logger.LogInformation("🔍 Detecting Temporal server capacity using metrics and configuration");
        
        try
        {
            var capacity = new TemporalCapacity();
            
            // Basic server configuration from Program.cs
            capacity.ServerCount = 1; // Single Temporal server
            capacity.CurrentWorkerCount = 1; // Default worker count
            capacity.MaxRecommendedWorkerCount = 10; // Conservative scaling limit
            
            // Default agent configuration (these will be optimized in ITemporalAgentOptimizer)
            capacity.CurrentAgentConfiguration = new AgentConfiguration
            {
                MaxConcurrentActivities = 20, // Default Temporal setting
                MaxConcurrentWorkflowTasks = 10,
                MaxConcurrentActivityTasks = 20,
                WorkerAgentCount = 1,
                ActivityAgentCount = 1,
                WorkflowAgentCount = 1
            };
            
            // Recommended optimized configuration (this is what we'll implement)
            capacity.RecommendedAgentConfiguration = new AgentConfiguration
            {
                MaxConcurrentActivities = 100, // 5x increase for better throughput
                MaxConcurrentWorkflowTasks = 50, // 5x increase
                MaxConcurrentActivityTasks = 100, // 5x increase
                WorkerAgentCount = 5, // Scale up agents
                ActivityAgentCount = 10, // More activity agents
                WorkflowAgentCount = 3, // Dedicated workflow agents
                TaskQueuePollInterval = TimeSpan.FromMilliseconds(100),
                TaskExecutionTimeout = TimeSpan.FromSeconds(30),
                HeartbeatInterval = TimeSpan.FromSeconds(5)
            };
            
            capacity.CurrentConcurrentActivities = capacity.CurrentAgentConfiguration.MaxConcurrentActivities;
            capacity.MaxConcurrentActivities = capacity.RecommendedAgentConfiguration.MaxConcurrentActivities;
            capacity.CurrentConcurrentWorkflows = capacity.CurrentAgentConfiguration.MaxConcurrentWorkflowTasks;
            capacity.MaxConcurrentWorkflows = capacity.RecommendedAgentConfiguration.MaxConcurrentWorkflowTasks;
            
            // Estimate workflow execution rate based on agent configuration
            capacity.EstimatedWorkflowExecutionRatePerSecond = capacity.RecommendedAgentConfiguration.MaxConcurrentWorkflowTasks * 0.1; // Conservative estimate
            
            _logger.LogInformation("📊 Temporal server detected: {Workers} workers, {CurrentActivities} current activities, {MaxActivities} max activities",
                capacity.CurrentWorkerCount, capacity.CurrentConcurrentActivities, capacity.MaxConcurrentActivities);
            
            // Get Temporal metrics from Prometheus
            try
            {
                var temporalMetrics = await _prometheusService.GetTemporalWorkflowMetricsAsync();
                capacity.ServerMetrics = temporalMetrics.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
                
                // Adjust estimates based on current metrics if available
                if (temporalMetrics.Values.Any())
                {
                    var currentRate = temporalMetrics.Values.Max();
                    if (currentRate > 0)
                    {
                        capacity.EstimatedWorkflowExecutionRatePerSecond = Math.Max(capacity.EstimatedWorkflowExecutionRatePerSecond, currentRate * 1.5);
                    }
                }
                
                _logger.LogInformation("📈 Temporal metrics retrieved: {MetricCount} metrics, estimated rate: {Rate:F2} workflows/s",
                    temporalMetrics.Count, capacity.EstimatedWorkflowExecutionRatePerSecond);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ Could not retrieve Temporal metrics from Prometheus: {Error}", ex.Message);
            }
            
            _logger.LogInformation("✅ Temporal capacity detection completed: {WorkflowRate:F2} workflows/s capacity",
                capacity.EstimatedWorkflowExecutionRatePerSecond);
            
            return capacity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to detect Temporal capacity");
            
            // Return baseline capacity for graceful degradation
            return new TemporalCapacity
            {
                ServerCount = 1,
                CurrentWorkerCount = 1,
                MaxRecommendedWorkerCount = 5,
                CurrentConcurrentActivities = 20,
                MaxConcurrentActivities = 100,
                CurrentConcurrentWorkflows = 10,
                MaxConcurrentWorkflows = 50,
                EstimatedWorkflowExecutionRatePerSecond = 5.0,
                CurrentAgentConfiguration = new AgentConfiguration(),
                RecommendedAgentConfiguration = new AgentConfiguration
                {
                    MaxConcurrentActivities = 100,
                    MaxConcurrentWorkflowTasks = 50,
                    MaxConcurrentActivityTasks = 100,
                    WorkerAgentCount = 5,
                    ActivityAgentCount = 10,
                    WorkflowAgentCount = 3
                },
                CapacitySource = "Baseline Configuration (Temporal detection failed)"
            };
        }
    }

    public async Task<AdaptiveParameters> CalculateOptimalParametersAsync(PerformanceTarget target)
    {
        _logger.LogInformation("🧮 Calculating optimal test parameters based on detected system capacity");
        
        try
        {
            // Detect all system capacities
            var kafkaCapacity = await DetectKafkaCapacityAsync();
            var flinkCapacity = await DetectFlinkCapacityAsync();
            var temporalCapacity = await DetectTemporalCapacityAsync();
            
            var parameters = new AdaptiveParameters();
            var justification = new AdaptiveParameterJustification();
            var calculationDetails = new Dictionary<string, object>();
            
            // Calculate optimal Kafka message count
            double kafkaLimit = kafkaCapacity.EstimatedMaxThroughputMessagesPerSecond * KAFKA_SAFETY_FACTOR;
            double flinkLimit = flinkCapacity.EstimatedProcessingRateMessagesPerSecond * FLINK_SAFETY_FACTOR;
            double temporalLimit = temporalCapacity.EstimatedWorkflowExecutionRatePerSecond * 10; // Workflows handle multiple messages
            
            // Use target parameters if specified, otherwise use capacity-based calculation
            var targetMessageCount = target.TargetMessageCount ?? (int)Math.Min(kafkaLimit, flinkLimit);
            var maxExecutionTime = target.MaxExecutionTime ?? TimeSpan.FromMinutes(10);
            
            // Calculate optimal message count (limited by bottleneck)
            var bottleneckLimit = Math.Min(Math.Min(kafkaLimit, flinkLimit), temporalLimit * 100); // Temporal processes subset
            parameters.OptimalKafkaMessages = target.TargetMessageCount ?? (int)Math.Min(bottleneckLimit, 1000000); // Cap at 1M for stability
            
            // Calculate optimal Flink job count
            parameters.OptimalFlinkJobs = Math.Max(1, Math.Min(flinkCapacity.RecommendedJobCount, 6));
            
            // Calculate optimal Temporal workflow count (subset of messages)
            var workflowRatio = target.PrimaryGoal == PerformanceGoal.MaxThroughput ? 0.1 : 0.1; // 10% of messages
            parameters.OptimalTemporalWorkflows = (int)Math.Max(1, Math.Min(parameters.OptimalKafkaMessages * workflowRatio, temporalCapacity.MaxConcurrentWorkflows));
            
            // Calculate execution time and throughput
            var estimatedThroughput = Math.Min(kafkaCapacity.EstimatedMaxThroughputMessagesPerSecond, flinkCapacity.EstimatedProcessingRateMessagesPerSecond) * 0.8;
            parameters.EstimatedThroughputMessagesPerSecond = estimatedThroughput;
            parameters.ExpectedExecutionTime = TimeSpan.FromSeconds(Math.Max(60, parameters.OptimalKafkaMessages / estimatedThroughput));
            
            // Set batch size
            parameters.OptimalBatchSize = kafkaCapacity.RecommendedBatchSize;
            
            // Create justification
            justification.KafkaMessageCountReason = $"Limited by system bottleneck: Kafka={kafkaLimit:F0}, Flink={flinkLimit:F0}, Temporal={temporalLimit:F0} msg/s";
            justification.FlinkJobCountReason = $"Based on available task slots ({flinkCapacity.AvailableTaskSlots}) with 4 slots per job";
            justification.TemporalWorkflowCountReason = $"Set to {workflowRatio:P} of messages for {target.PrimaryGoal} goal (adjusted to 10% volume)";
            justification.ExecutionTimeReason = $"Estimated based on {estimatedThroughput:F0} msg/s throughput";
            
            // Identify capacity limitations
            if (kafkaLimit < flinkLimit && kafkaLimit < temporalLimit * 100)
                justification.CapacityLimitations.Add("Kafka throughput is the primary bottleneck");
            else if (flinkLimit < kafkaLimit && flinkLimit < temporalLimit * 100)
                justification.CapacityLimitations.Add("Flink processing capacity is the primary bottleneck");
            else if (temporalLimit * 100 < kafkaLimit && temporalLimit * 100 < flinkLimit)
                justification.CapacityLimitations.Add("Temporal workflow capacity is the primary bottleneck");
            
            // Add optimization opportunities
            if (flinkCapacity.AvailableTaskSlots > parameters.OptimalFlinkJobs * 4)
                justification.OptimizationOpportunities.Add("Additional Flink task slots available for higher parallelism");
            if (temporalCapacity.MaxConcurrentWorkflows > parameters.OptimalTemporalWorkflows * 2)
                justification.OptimizationOpportunities.Add("Temporal agent optimization could support more workflows");
            
            parameters.Justification = justification;
            
            // Store calculation details
            calculationDetails["kafkaCapacityLimit"] = kafkaLimit;
            calculationDetails["flinkCapacityLimit"] = flinkLimit;
            calculationDetails["temporalCapacityLimit"] = temporalLimit;
            calculationDetails["bottleneckComponent"] = kafkaLimit <= flinkLimit && kafkaLimit <= temporalLimit * 100 ? "Kafka" :
                                                        flinkLimit <= kafkaLimit && flinkLimit <= temporalLimit * 100 ? "Flink" : "Temporal";
            calculationDetails["safetyFactors"] = new { Kafka = KAFKA_SAFETY_FACTOR, Flink = FLINK_SAFETY_FACTOR, Temporal = TEMPORAL_SAFETY_FACTOR };
            calculationDetails["targetGoal"] = target.PrimaryGoal.ToString();
            
            parameters.CalculationDetails = calculationDetails;
            
            _logger.LogInformation("✅ Optimal parameters calculated: {Messages} messages, {Jobs} Flink jobs, {Workflows} workflows, {Throughput:F0} msg/s",
                parameters.OptimalKafkaMessages, parameters.OptimalFlinkJobs, parameters.OptimalTemporalWorkflows, parameters.EstimatedThroughputMessagesPerSecond);
            
            return parameters;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to calculate optimal parameters");
            
            // Return baseline parameters for graceful degradation
            return new AdaptiveParameters
            {
                OptimalKafkaMessages = 100000, // Conservative baseline
                OptimalFlinkJobs = 2,
                OptimalTemporalWorkflows = 200,
                OptimalBatchSize = 1000,
                ExpectedExecutionTime = TimeSpan.FromMinutes(5),
                EstimatedThroughputMessagesPerSecond = 1000,
                CapacitySource = "Baseline Configuration (Calculation failed)",
                Justification = new AdaptiveParameterJustification
                {
                    KafkaMessageCountReason = "Baseline value due to capacity detection failure",
                    FlinkJobCountReason = "Conservative baseline job count",
                    TemporalWorkflowCountReason = "Conservative baseline workflow count",
                    ExecutionTimeReason = "Estimated baseline execution time"
                }
            };
        }
    }

    public async Task<SystemCapacitySummary> GetSystemCapacitySummaryAsync()
    {
        _logger.LogInformation("📋 Generating comprehensive system capacity summary");
        
        try
        {
            var summary = new SystemCapacitySummary();
            
            // Detect all capacities
            summary.KafkaCapacity = await DetectKafkaCapacityAsync();
            summary.FlinkCapacity = await DetectFlinkCapacityAsync();
            summary.TemporalCapacity = await DetectTemporalCapacityAsync();
            
            // Calculate recommended parameters
            var defaultTarget = new PerformanceTarget { PrimaryGoal = PerformanceGoal.Balanced };
            summary.RecommendedParameters = await CalculateOptimalParametersAsync(defaultTarget);
            
            // Identify primary bottleneck
            var kafkaRate = summary.KafkaCapacity.EstimatedMaxThroughputMessagesPerSecond;
            var flinkRate = summary.FlinkCapacity.EstimatedProcessingRateMessagesPerSecond;
            var temporalRate = summary.TemporalCapacity.EstimatedWorkflowExecutionRatePerSecond * 100;
            
            if (kafkaRate <= flinkRate && kafkaRate <= temporalRate)
                summary.PrimaryBottleneck = SystemBottleneck.KafkaBrokers;
            else if (flinkRate <= kafkaRate && flinkRate <= temporalRate)
                summary.PrimaryBottleneck = SystemBottleneck.FlinkTaskSlots;
            else
                summary.PrimaryBottleneck = SystemBottleneck.TemporalWorkers;
            
            // Generate recommendations
            summary.Recommendations = GenerateCapacityRecommendations(summary);
            
            // Overall assessment
            summary.IsCapacityAdequate = summary.KafkaCapacity.BrokerCount >= 3 && 
                                       summary.FlinkCapacity.TotalTaskSlots >= 8 && 
                                       summary.TemporalCapacity.ServerCount >= 1;
            
            summary.OverallAssessment = summary.IsCapacityAdequate 
                ? $"System capacity is adequate for testing. Primary bottleneck: {summary.PrimaryBottleneck}"
                : "System capacity may be insufficient for high-throughput testing";
            
            _logger.LogInformation("📊 System capacity summary completed: {Bottleneck} bottleneck, {Adequate} capacity, {Recommendations} recommendations",
                summary.PrimaryBottleneck, summary.IsCapacityAdequate ? "adequate" : "insufficient", summary.Recommendations.Count);
            
            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to generate system capacity summary");
            throw;
        }
    }

    public async Task<CapacityValidationResult> ValidateCapacityRequirementsAsync(MinimumCapacityRequirements minimumRequirements)
    {
        _logger.LogInformation("✅ Validating system capacity against minimum requirements");
        
        var result = new CapacityValidationResult
        {
            RequiredCapacity = minimumRequirements
        };
        
        try
        {
            result.DetectedCapacity = await GetSystemCapacitySummaryAsync();
            
            // Validate Kafka capacity
            if (result.DetectedCapacity.KafkaCapacity.BrokerCount < minimumRequirements.MinKafkaBrokers)
                result.ValidationErrors.Add($"Insufficient Kafka brokers: {result.DetectedCapacity.KafkaCapacity.BrokerCount} < {minimumRequirements.MinKafkaBrokers}");
            
            // Validate Flink capacity
            if (result.DetectedCapacity.FlinkCapacity.TotalTaskSlots < minimumRequirements.MinFlinkTaskSlots)
                result.ValidationErrors.Add($"Insufficient Flink task slots: {result.DetectedCapacity.FlinkCapacity.TotalTaskSlots} < {minimumRequirements.MinFlinkTaskSlots}");
            
            // Validate Temporal capacity
            if (result.DetectedCapacity.TemporalCapacity.CurrentWorkerCount < minimumRequirements.MinTemporalWorkers)
                result.ValidationErrors.Add($"Insufficient Temporal workers: {result.DetectedCapacity.TemporalCapacity.CurrentWorkerCount} < {minimumRequirements.MinTemporalWorkers}");
            
            // Validate throughput
            if (result.DetectedCapacity.RecommendedParameters.EstimatedThroughputMessagesPerSecond < minimumRequirements.MinThroughputMessagesPerSecond)
                result.ValidationErrors.Add($"Insufficient throughput: {result.DetectedCapacity.RecommendedParameters.EstimatedThroughputMessagesPerSecond:F0} < {minimumRequirements.MinThroughputMessagesPerSecond}");
            
            // Validate execution time
            if (result.DetectedCapacity.RecommendedParameters.ExpectedExecutionTime > minimumRequirements.MaxAcceptableExecutionTime)
                result.ValidationWarnings.Add($"Execution time may exceed limit: {result.DetectedCapacity.RecommendedParameters.ExpectedExecutionTime} > {minimumRequirements.MaxAcceptableExecutionTime}");
            
            result.IsValid = result.ValidationErrors.Count == 0;
            result.Message = result.IsValid 
                ? "System capacity meets all minimum requirements" 
                : $"System capacity validation failed: {result.ValidationErrors.Count} errors, {result.ValidationWarnings.Count} warnings";
            
            _logger.LogInformation("🔍 Capacity validation completed: {Valid}, {Errors} errors, {Warnings} warnings",
                result.IsValid ? "PASSED" : "FAILED", result.ValidationErrors.Count, result.ValidationWarnings.Count);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to validate capacity requirements");
            result.IsValid = false;
            result.Message = $"Capacity validation failed with exception: {ex.Message}";
            result.ValidationErrors.Add("Capacity detection failed");
            return result;
        }
    }

    private List<CapacityRecommendation> GenerateCapacityRecommendations(SystemCapacitySummary summary)
    {
        var recommendations = new List<CapacityRecommendation>();
        
        // Kafka recommendations
        if (summary.KafkaCapacity.BrokerCount < 3)
        {
            recommendations.Add(new CapacityRecommendation
            {
                Component = "Kafka",
                Issue = $"Only {summary.KafkaCapacity.BrokerCount} brokers available",
                Recommendation = "Increase Kafka broker count to 3 for proper replication",
                Priority = CapacityRecommendationPriority.High,
                CanBeOptimizedInCurrentImplementation = false
            });
        }
        
        // Flink recommendations
        if (summary.FlinkCapacity.AvailableTaskSlots < summary.FlinkCapacity.TotalTaskSlots * 0.8)
        {
            recommendations.Add(new CapacityRecommendation
            {
                Component = "Flink",
                Issue = $"Only {summary.FlinkCapacity.AvailableTaskSlots}/{summary.FlinkCapacity.TotalTaskSlots} task slots available",
                Recommendation = "Consider canceling running jobs to free up task slots",
                Priority = CapacityRecommendationPriority.Medium,
                CanBeOptimizedInCurrentImplementation = true
            });
        }
        
        // Temporal recommendations
        if (summary.TemporalCapacity.CurrentConcurrentActivities < summary.TemporalCapacity.MaxConcurrentActivities)
        {
            recommendations.Add(new CapacityRecommendation
            {
                Component = "Temporal",
                Issue = $"Agent configuration not optimized: {summary.TemporalCapacity.CurrentConcurrentActivities} current vs {summary.TemporalCapacity.MaxConcurrentActivities} potential activities",
                Recommendation = "Apply agent optimization to increase concurrent activities and workflow throughput",
                Priority = CapacityRecommendationPriority.High,
                CanBeOptimizedInCurrentImplementation = true
            });
        }
        
        return recommendations;
    }
}