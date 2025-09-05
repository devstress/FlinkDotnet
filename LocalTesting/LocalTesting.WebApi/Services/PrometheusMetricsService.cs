using System.Text.Json;
using System.Text.Json.Serialization;

namespace LocalTesting.WebApi.Services;

/// <summary>
/// Service to query real metrics from Prometheus infrastructure
/// Replaces fake metric simulation with actual observability data
/// </summary>
public class PrometheusMetricsService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PrometheusMetricsService> _logger;
    private readonly string _prometheusBaseUrl;

    public PrometheusMetricsService(HttpClient httpClient, ILogger<PrometheusMetricsService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Use internal Prometheus URL for container-to-container communication
        _prometheusBaseUrl = configuration.GetValue<string>("PROMETHEUS_URL") ?? "http://prometheus:9090";
        
        _httpClient.BaseAddress = new Uri(_prometheusBaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(60); // Increased timeout for slow scraping
        
        _logger.LogInformation("PrometheusMetricsService initialized with base URL: {PrometheusUrl}", _prometheusBaseUrl);
        
        // Test connectivity to ensure real Prometheus connection
        _ = Task.Run(async () =>
        {
            try
            {
                // Wait a bit for Prometheus to start scraping metrics
                await Task.Delay(5000);
                
                var healthResponse = await _httpClient.GetAsync("/api/v1/label/__name__/values");
                if (healthResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✅ Prometheus connectivity confirmed - can retrieve real metrics");
                }
                else
                {
                    _logger.LogWarning("⚠️ Prometheus connectivity test failed with status {StatusCode}", healthResponse.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Prometheus connectivity test failed - will use fallback values until connection available");
            }
        });
    }

    /// <summary>
    /// Get real Kafka producer metrics with per-partition and per-producer granularity
    /// </summary>
    public async Task<Dictionary<string, double>> GetKafkaProducerMetricsAsync()
    {
        var metrics = new Dictionary<string, double>();
        
        try
        {
            // Query for Kafka producer rate metrics per partition and producer
            // Use shorter time range for more immediate results
            var query = "rate(kafka_producer_messages_total[30s])";
            var results = await QueryPrometheusAsync(query);
            
            foreach (var result in results)
            {
                if (result.Metric.TryGetValue("topic", out var topic) &&
                    result.Metric.TryGetValue("partition", out var partition))
                {
                    var metricKey = $"kafka_producer_{topic}_partition-{partition}";
                    var rate = ParseMetricValue(result.Value);
                    if (rate > 0)
                    {
                        metrics[metricKey] = rate;
                    }
                }
            }
            
            // If no rate metrics, try instant vector (current values)
            if (metrics.Count == 0)
            {
                _logger.LogInformation("No rate metrics found, trying instant vector query");
                var instantQuery = "kafka_producer_messages_total";
                var instantResults = await QueryPrometheusAsync(instantQuery);
                
                foreach (var result in instantResults)
                {
                    if (result.Metric.TryGetValue("topic", out var topic) &&
                        result.Metric.TryGetValue("partition", out var partition))
                    {
                        var metricKey = $"kafka_producer_{topic}_partition-{partition}";
                        var value = ParseMetricValue(result.Value);
                        if (value > 0)
                        {
                            // Use simplified rate calculation for instant values
                            metrics[metricKey] = Math.Round(value / 60.0, 2); // Approximate rate
                        }
                    }
                }
            }
            
            _logger.LogInformation("Retrieved {Count} Kafka producer metrics from Prometheus", metrics.Count);
            
            // Log information about missing metrics but don't fail - allow Prometheus time to scrape
            if (metrics.Count == 0)
            {
                _logger.LogInformation("ℹ️ No Kafka producer metrics found in Prometheus yet - metrics may need more time to be scraped");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve Kafka producer metrics from Prometheus - Prometheus may not be available yet.");
            // Return empty metrics instead of fake fallback values - only use real data when available
            return metrics;
        }
        
        return metrics;
    }

    /// <summary>
    /// Get real Flink processing metrics (includes Kafka consuming as part of Flink)
    /// </summary>
    public async Task<Dictionary<string, double>> GetFlinkProcessingMetricsAsync()
    {
        var metrics = new Dictionary<string, double>();
        
        try
        {
            // Query Flink job input rate (this IS the Kafka consuming rate)
            var inputQuery = "rate(flink_job_messages_in_total[30s])";
            var inputResults = await QueryPrometheusAsync(inputQuery);
            
            foreach (var result in inputResults)
            {
                if (result.Metric.TryGetValue("job_id", out var jobId) &&
                    result.Metric.TryGetValue("operator", out var operatorName))
                {
                    var metricKey = $"flink_input_{jobId}_{operatorName}";
                    var rate = ParseMetricValue(result.Value);
                    if (rate > 0)
                    {
                        metrics[metricKey] = rate;
                    }
                }
            }
            
            // Query Flink job output rate  
            var outputQuery = "rate(flink_job_messages_out_total[30s])";
            var outputResults = await QueryPrometheusAsync(outputQuery);
            
            foreach (var result in outputResults)
            {
                if (result.Metric.TryGetValue("job_id", out var jobId) &&
                    result.Metric.TryGetValue("operator", out var operatorName))
                {
                    var metricKey = $"flink_output_{jobId}_{operatorName}";
                    var rate = ParseMetricValue(result.Value);
                    if (rate > 0)
                    {
                        metrics[metricKey] = rate;
                    }
                }
            }
            
            // If no rate metrics, try instant vector queries
            if (metrics.Count == 0)
            {
                _logger.LogInformation("No Flink rate metrics found, trying instant vector queries");
                
                var instantInputQuery = "flink_job_messages_in_total";
                var instantInputResults = await QueryPrometheusAsync(instantInputQuery);
                
                foreach (var result in instantInputResults)
                {
                    if (result.Metric.TryGetValue("job_id", out var jobId) &&
                        result.Metric.TryGetValue("operator", out var operatorName))
                    {
                        var metricKey = $"flink_input_{jobId}_{operatorName}";
                        var value = ParseMetricValue(result.Value);
                        if (value > 0)
                        {
                            metrics[metricKey] = Math.Round(value / 60.0, 2); // Approximate rate
                        }
                    }
                }
            }
            
            _logger.LogInformation("Retrieved {Count} Flink processing metrics from Prometheus", metrics.Count);
            
            // Log information about missing metrics but don't fail
            if (metrics.Count == 0)
            {
                _logger.LogInformation("ℹ️ No Flink processing metrics found in Prometheus yet - metrics may need more time to be scraped");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve Flink metrics from Prometheus - Prometheus may not be available yet.");
            // Return empty metrics instead of fake fallback values - only use real data when available
            return metrics;
        }
        
        return metrics;
    }

    /// <summary>
    /// Get real Temporal workflow metrics (processes subset of messages through workflows)
    /// </summary>
    public async Task<Dictionary<string, double>> GetTemporalWorkflowMetricsAsync()
    {
        var metrics = new Dictionary<string, double>();
        
        try
        {
            // Query Temporal workflow execution rate
            var workflowQuery = "rate(temporal_workflow_executions_total[30s])";
            var workflowResults = await QueryPrometheusAsync(workflowQuery);
            
            foreach (var result in workflowResults)
            {
                if (result.Metric.TryGetValue("workflow_type", out var workflowType))
                {
                    var metricKey = $"temporal_workflow_{workflowType}";
                    var rate = ParseMetricValue(result.Value);
                    if (rate > 0)
                    {
                        metrics[metricKey] = rate;
                    }
                }
            }
            
            // Query Temporal activity execution rate
            var activityQuery = "rate(temporal_activity_executions_total[30s])";
            var activityResults = await QueryPrometheusAsync(activityQuery);
            
            foreach (var result in activityResults)
            {
                if (result.Metric.TryGetValue("activity_type", out var activityType))
                {
                    var metricKey = $"temporal_activity_{activityType}";
                    var rate = ParseMetricValue(result.Value);
                    if (rate > 0)
                    {
                        metrics[metricKey] = rate;
                    }
                }
            }
            
            // If no rate metrics, try instant vector queries
            if (metrics.Count == 0)
            {
                _logger.LogInformation("No Temporal rate metrics found, trying instant vector queries");
                
                var instantWorkflowQuery = "temporal_workflow_executions_total";
                var instantWorkflowResults = await QueryPrometheusAsync(instantWorkflowQuery);
                
                foreach (var result in instantWorkflowResults)
                {
                    if (result.Metric.TryGetValue("workflow_type", out var workflowType))
                    {
                        var metricKey = $"temporal_workflow_{workflowType}";
                        var value = ParseMetricValue(result.Value);
                        if (value > 0)
                        {
                            metrics[metricKey] = Math.Round(value / 60.0, 2); // Approximate rate
                        }
                    }
                }
            }
            
            _logger.LogInformation("Retrieved {Count} Temporal workflow metrics from Prometheus", metrics.Count);
            
            // Log information about missing metrics but don't fail
            if (metrics.Count == 0)
            {
                _logger.LogInformation("ℹ️ No Temporal workflow metrics found in Prometheus yet - metrics may need more time to be scraped");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve Temporal metrics from Prometheus - Prometheus may not be available yet.");
            // Return empty metrics instead of fake fallback values - only use real data when available
            return metrics;
        }
        
        return metrics;
    }

    /// <summary>
    /// Get real end-to-end flow metrics 
    /// </summary>
    public async Task<Dictionary<string, double>> GetEndToEndFlowMetricsAsync()
    {
        var metrics = new Dictionary<string, double>();
        
        try
        {
            // Query end-to-end flow metrics
            var flowQuery = "rate(flow_messages_end_to_end_total[1m])";
            var flowResults = await QueryPrometheusAsync(flowQuery);
            
            foreach (var result in flowResults)
            {
                var metricKey = "flow_end_to_end";
                var rate = ParseMetricValue(result.Value);
                if (rate > 0)
                {
                    metrics[metricKey] = rate;
                }
            }
            
            // Query Kafka to Flink flow
            var kafkaToFlinkQuery = "rate(flow_messages_kafka_to_flink_total[1m])";
            var kafkaToFlinkResults = await QueryPrometheusAsync(kafkaToFlinkQuery);
            
            foreach (var result in kafkaToFlinkResults)
            {
                var metricKey = "flow_kafka_to_flink";
                var rate = ParseMetricValue(result.Value);
                if (rate > 0)
                {
                    metrics[metricKey] = rate;
                }
            }
            
            // Query Flink to Temporal flow
            var flinkToTemporalQuery = "rate(flow_messages_flink_to_temporal_total[1m])";
            var flinkToTemporalResults = await QueryPrometheusAsync(flinkToTemporalQuery);
            
            foreach (var result in flinkToTemporalResults)
            {
                var metricKey = "flow_flink_to_temporal";
                var rate = ParseMetricValue(result.Value);
                if (rate > 0)
                {
                    metrics[metricKey] = rate;
                }
            }
            
            _logger.LogInformation("Retrieved {Count} end-to-end flow metrics from Prometheus", metrics.Count);
            
            // Log information about missing metrics but don't fail - allow local metrics to be used
            if (metrics.Count == 0)
            {
                _logger.LogInformation("ℹ️ No end-to-end flow metrics found in Prometheus yet - this is normal for recent workload execution");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve flow metrics from Prometheus - Prometheus may not be available yet.");
            // Return empty metrics instead of fake fallback values - only use real data when available
            return metrics;
        }
        
        return metrics;
    }

    /// <summary>
    /// Query Prometheus with PromQL and return results
    /// </summary>
    private async Task<List<PrometheusQueryResult>> QueryPrometheusAsync(string query)
    {
        try
        {
            var encodedQuery = Uri.EscapeDataString(query);
            var url = $"/api/v1/query?query={encodedQuery}";
            
            _logger.LogDebug("Querying Prometheus: {Query}", query);
            
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Prometheus query failed with status {StatusCode}: {Query}", response.StatusCode, query);
                return new List<PrometheusQueryResult>();
            }
            
            var content = await response.Content.ReadAsStringAsync();
            var prometheusResponse = JsonSerializer.Deserialize<PrometheusResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            if (prometheusResponse?.Status != "success" || prometheusResponse.Data?.Result == null)
            {
                _logger.LogWarning("Prometheus returned unsuccessful response for query: {Query}", query);
                return new List<PrometheusQueryResult>();
            }
            
            return prometheusResponse.Data.Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying Prometheus with query: {Query}", query);
            return new List<PrometheusQueryResult>();
        }
    }

    /// <summary>
    /// Parse metric value from Prometheus result
    /// </summary>
    private static double ParseMetricValue(object[]? value)
    {
        if (value == null || value.Length < 2) return 0.0;
        
        if (value[1] is JsonElement element && element.ValueKind == JsonValueKind.String)
        {
            if (double.TryParse(element.GetString(), out var result))
            {
                return result;
            }
        }
        else if (value[1] is string stringValue)
        {
            if (double.TryParse(stringValue, out var result))
            {
                return result;
            }
        }
        
        return 0.0;
    }

    /// <summary>
    /// Get all metrics for comprehensive observability
    /// </summary>
    public async Task<Dictionary<string, double>> GetAllMetricsAsync()
    {
        var allMetrics = new Dictionary<string, double>();
        
        // Get metrics from all layers
        var kafkaMetrics = await GetKafkaProducerMetricsAsync();
        var flinkMetrics = await GetFlinkProcessingMetricsAsync();
        var temporalMetrics = await GetTemporalWorkflowMetricsAsync();
        var flowMetrics = await GetEndToEndFlowMetricsAsync();
        
        // Combine all metrics
        foreach (var kvp in kafkaMetrics) allMetrics[kvp.Key] = kvp.Value;
        foreach (var kvp in flinkMetrics) allMetrics[kvp.Key] = kvp.Value;
        foreach (var kvp in temporalMetrics) allMetrics[kvp.Key] = kvp.Value;
        foreach (var kvp in flowMetrics) allMetrics[kvp.Key] = kvp.Value;
        
        _logger.LogInformation("Retrieved {Count} total metrics from Prometheus infrastructure", allMetrics.Count);
        
        return allMetrics;
    }
}

/// <summary>
/// Prometheus API response structure
/// </summary>
public class PrometheusResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("data")]
    public PrometheusData? Data { get; set; }
}

public class PrometheusData
{
    [JsonPropertyName("resultType")]
    public string ResultType { get; set; } = string.Empty;
    
    [JsonPropertyName("result")]
    public List<PrometheusQueryResult> Result { get; set; } = new();
}

public class PrometheusQueryResult
{
    [JsonPropertyName("metric")]
    public Dictionary<string, string> Metric { get; set; } = new();
    
    [JsonPropertyName("value")]
    public object[]? Value { get; set; }
}