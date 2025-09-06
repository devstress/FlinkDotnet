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
            // Debug: First check what metrics are actually available
            var availableMetrics = await GetAvailableMetricsAsync();
            var kafkaMetricNames = availableMetrics.Where(m => m.StartsWith("kafka_")).ToList();
            
            _logger.LogInformation("Available Kafka metrics in Prometheus: {KafkaMetrics}", string.Join(", ", kafkaMetricNames));
            
            // Try multiple query patterns to find the actual metrics
            var queries = new[]
            {
                "rate(kafka_producer_messages_total[1m])",  // 1 minute range
                "rate(kafka_producer_messages_total[30s])", // 30 second range  
                "kafka_producer_messages_total",            // Instant vector
                "increase(kafka_producer_messages_total[1m])", // Increase over 1 minute
                // Try without rate if counters are not incrementing fast enough
                "{__name__=~\"kafka_producer.*\"}"           // All kafka producer metrics
            };
            
            foreach (var query in queries)
            {
                _logger.LogDebug("Trying Kafka query: {Query}", query);
                var results = await QueryPrometheusAsync(query);
                
                if (results.Count > 0)
                {
                    _logger.LogInformation("Found {Count} results with query: {Query}", results.Count, query);
                    
                    foreach (var result in results)
                    {
                        string metricKey;
                        
                        // Try different label combinations to build metric key
                        if (result.Metric.TryGetValue("topic", out var topic) &&
                            result.Metric.TryGetValue("partition", out var partition))
                        {
                            metricKey = $"kafka_producer_{topic}_partition-{partition}";
                        }
                        else if (result.Metric.TryGetValue("__name__", out var metricName))
                        {
                            metricKey = metricName;
                        }
                        else
                        {
                            // Use all available labels to create unique key
                            var labels = string.Join("_", result.Metric.Select(kvp => $"{kvp.Key}-{kvp.Value}"));
                            metricKey = $"kafka_metric_{labels}";
                        }
                        
                        var value = ParseMetricValue(result.Value);
                        if (value > 0)
                        {
                            metrics[metricKey] = value;
                            _logger.LogDebug("Added Kafka metric: {Key} = {Value}", metricKey, value);
                        }
                    }
                    
                    // If we found metrics with this query, break out of the loop
                    if (metrics.Count > 0)
                    {
                        _logger.LogInformation("Successfully retrieved {Count} Kafka metrics using query: {Query}", metrics.Count, query);
                        break;
                    }
                }
            }
            
            _logger.LogInformation("Retrieved {Count} Kafka producer metrics from Prometheus", metrics.Count);
            
            // Enhanced debugging for missing metrics
            if (metrics.Count == 0)
            {
                _logger.LogWarning("⚠️ No Kafka producer metrics found in Prometheus");
                _logger.LogInformation("Available metric names starting with 'kafka': {KafkaMetrics}", 
                    string.Join(", ", kafkaMetricNames.Take(10)));
                _logger.LogInformation("Total metrics available in Prometheus: {TotalMetrics}", availableMetrics.Count);
                
                // Wait a bit and try one more time for metrics that might just be appearing
                await Task.Delay(2000);
                var retryQuery = "kafka_producer_messages_total";
                var retryResults = await QueryPrometheusAsync(retryQuery);
                if (retryResults.Count > 0)
                {
                    _logger.LogInformation("Retry found {Count} Kafka metrics after delay", retryResults.Count);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve Kafka producer metrics from Prometheus");
            // Return empty metrics - no fallbacks
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
            // Debug: Check what Flink metrics are available
            var availableMetrics = await GetAvailableMetricsAsync();
            var flinkMetricNames = availableMetrics.Where(m => m.StartsWith("flink_")).ToList();
            
            _logger.LogInformation("Available Flink metrics in Prometheus: {FlinkMetrics}", string.Join(", ", flinkMetricNames));
            
            // Try multiple query patterns for Flink metrics
            var queries = new[]
            {
                "rate(flink_job_messages_in_total[1m])",      // Input rate
                "rate(flink_job_messages_out_total[1m])",     // Output rate
                "flink_job_messages_in_total",                // Instant input
                "flink_job_messages_out_total",               // Instant output
                "increase(flink_job_messages_in_total[1m])",  // Increase input
                "increase(flink_job_messages_out_total[1m])", // Increase output
                "{__name__=~\"flink_.*\"}"                    // All Flink metrics
            };
            
            foreach (var query in queries)
            {
                _logger.LogDebug("Trying Flink query: {Query}", query);
                var results = await QueryPrometheusAsync(query);
                
                if (results.Count > 0)
                {
                    _logger.LogInformation("Found {Count} results with Flink query: {Query}", results.Count, query);
                    
                    foreach (var result in results)
                    {
                        string metricKey;
                        
                        // Try different label combinations
                        if (result.Metric.TryGetValue("job_id", out var jobId) &&
                            result.Metric.TryGetValue("operator", out var operatorName))
                        {
                            // Determine if this is input or output based on metric name or query
                            var direction = query.Contains("_in_") || query.Contains("input") ? "input" : 
                                          query.Contains("_out_") || query.Contains("output") ? "output" : "processing";
                            metricKey = $"flink_{direction}_{jobId}_{operatorName}";
                        }
                        else if (result.Metric.TryGetValue("__name__", out var metricName))
                        {
                            metricKey = metricName;
                        }
                        else
                        {
                            var labels = string.Join("_", result.Metric.Select(kvp => $"{kvp.Key}-{kvp.Value}"));
                            metricKey = $"flink_metric_{labels}";
                        }
                        
                        var value = ParseMetricValue(result.Value);
                        if (value > 0)
                        {
                            metrics[metricKey] = value;
                            _logger.LogDebug("Added Flink metric: {Key} = {Value}", metricKey, value);
                        }
                    }
                    
                    if (metrics.Count > 0)
                    {
                        _logger.LogInformation("Successfully retrieved {Count} Flink metrics using query: {Query}", metrics.Count, query);
                        break;
                    }
                }
            }
            
            _logger.LogInformation("Retrieved {Count} Flink processing metrics from Prometheus", metrics.Count);
            
            if (metrics.Count == 0)
            {
                _logger.LogWarning("⚠️ No Flink processing metrics found in Prometheus");
                _logger.LogInformation("Available metric names starting with 'flink': {FlinkMetrics}", 
                    string.Join(", ", flinkMetricNames.Take(10)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve Flink metrics from Prometheus");
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
            // Debug: Check what Temporal metrics are available
            var availableMetrics = await GetAvailableMetricsAsync();
            var temporalMetricNames = availableMetrics.Where(m => m.StartsWith("temporal_")).ToList();
            
            _logger.LogInformation("Available Temporal metrics in Prometheus: {TemporalMetrics}", string.Join(", ", temporalMetricNames));
            
            // Try multiple query patterns for Temporal metrics
            var queries = new[]
            {
                "rate(temporal_workflow_executions_total[1m])",    // Workflow rate
                "rate(temporal_activity_executions_total[1m])",    // Activity rate
                "temporal_workflow_executions_total",              // Instant workflow
                "temporal_activity_executions_total",              // Instant activity
                "increase(temporal_workflow_executions_total[1m])", // Increase workflow
                "increase(temporal_activity_executions_total[1m])", // Increase activity
                "{__name__=~\"temporal_.*\"}"                       // All Temporal metrics
            };
            
            foreach (var query in queries)
            {
                _logger.LogDebug("Trying Temporal query: {Query}", query);
                var results = await QueryPrometheusAsync(query);
                
                if (results.Count > 0)
                {
                    _logger.LogInformation("Found {Count} results with Temporal query: {Query}", results.Count, query);
                    
                    foreach (var result in results)
                    {
                        string metricKey;
                        
                        // Try different label combinations
                        if (result.Metric.TryGetValue("workflow_type", out var workflowType))
                        {
                            metricKey = $"temporal_workflow_{workflowType}";
                        }
                        else if (result.Metric.TryGetValue("activity_type", out var activityType))
                        {
                            metricKey = $"temporal_activity_{activityType}";
                        }
                        else if (result.Metric.TryGetValue("__name__", out var metricName))
                        {
                            metricKey = metricName;
                        }
                        else
                        {
                            var labels = string.Join("_", result.Metric.Select(kvp => $"{kvp.Key}-{kvp.Value}"));
                            metricKey = $"temporal_metric_{labels}";
                        }
                        
                        var value = ParseMetricValue(result.Value);
                        if (value > 0)
                        {
                            metrics[metricKey] = value;
                            _logger.LogDebug("Added Temporal metric: {Key} = {Value}", metricKey, value);
                        }
                    }
                    
                    if (metrics.Count > 0)
                    {
                        _logger.LogInformation("Successfully retrieved {Count} Temporal metrics using query: {Query}", metrics.Count, query);
                        break;
                    }
                }
            }
            
            _logger.LogInformation("Retrieved {Count} Temporal workflow metrics from Prometheus", metrics.Count);
            
            if (metrics.Count == 0)
            {
                _logger.LogWarning("⚠️ No Temporal workflow metrics found in Prometheus");
                _logger.LogInformation("Available metric names starting with 'temporal': {TemporalMetrics}", 
                    string.Join(", ", temporalMetricNames.Take(10)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve Temporal metrics from Prometheus");
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
            // Debug: Check what Flow metrics are available
            var availableMetrics = await GetAvailableMetricsAsync();
            var flowMetricNames = availableMetrics.Where(m => m.StartsWith("flow_")).ToList();
            
            _logger.LogInformation("Available Flow metrics in Prometheus: {FlowMetrics}", string.Join(", ", flowMetricNames));
            
            // Try multiple query patterns for Flow metrics
            var queries = new[]
            {
                "rate(flow_messages_end_to_end_total[1m])",         // End-to-end rate
                "rate(flow_messages_kafka_to_flink_total[1m])",     // Kafka to Flink rate
                "rate(flow_messages_flink_to_temporal_total[1m])",  // Flink to Temporal rate
                "flow_messages_end_to_end_total",                   // Instant end-to-end
                "flow_messages_kafka_to_flink_total",               // Instant kafka to flink
                "flow_messages_flink_to_temporal_total",            // Instant flink to temporal
                "{__name__=~\"flow_.*\"}"                           // All Flow metrics
            };
            
            foreach (var query in queries)
            {
                _logger.LogDebug("Trying Flow query: {Query}", query);
                var results = await QueryPrometheusAsync(query);
                
                if (results.Count > 0)
                {
                    _logger.LogInformation("Found {Count} results with Flow query: {Query}", results.Count, query);
                    
                    foreach (var result in results)
                    {
                        string metricKey;
                        
                        // Extract flow type from metric name
                        if (result.Metric.TryGetValue("__name__", out var metricName))
                        {
                            if (metricName.Contains("kafka_to_flink"))
                                metricKey = "flow_kafka_to_flink";
                            else if (metricName.Contains("flink_to_temporal"))
                                metricKey = "flow_flink_to_temporal";
                            else if (metricName.Contains("end_to_end"))
                                metricKey = "flow_end_to_end";
                            else
                                metricKey = metricName;
                        }
                        else
                        {
                            var labels = string.Join("_", result.Metric.Select(kvp => $"{kvp.Key}-{kvp.Value}"));
                            metricKey = $"flow_metric_{labels}";
                        }
                        
                        var value = ParseMetricValue(result.Value);
                        if (value > 0)
                        {
                            metrics[metricKey] = value;
                            _logger.LogDebug("Added Flow metric: {Key} = {Value}", metricKey, value);
                        }
                    }
                    
                    if (metrics.Count > 0)
                    {
                        _logger.LogInformation("Successfully retrieved {Count} Flow metrics using query: {Query}", metrics.Count, query);
                        break;
                    }
                }
            }
            
            _logger.LogInformation("Retrieved {Count} end-to-end flow metrics from Prometheus", metrics.Count);
            
            if (metrics.Count == 0)
            {
                _logger.LogWarning("⚠️ No end-to-end flow metrics found in Prometheus");
                _logger.LogInformation("Available metric names starting with 'flow': {FlowMetrics}", 
                    string.Join(", ", flowMetricNames.Take(10)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve flow metrics from Prometheus");
            return metrics;
        }
        
        return metrics;
    }

    /// <summary>
    /// Get list of all available metric names in Prometheus for debugging
    /// </summary>
    public async Task<List<string>> GetAvailableMetricsAsync()
    {
        try
        {
            var url = "/api/v1/label/__name__/values";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get available metrics from Prometheus: {StatusCode}", response.StatusCode);
                return new List<string>();
            }
            
            var content = await response.Content.ReadAsStringAsync();
            var prometheusResponse = JsonSerializer.Deserialize<PrometheusLabelResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            if (prometheusResponse?.Status == "success" && prometheusResponse.Data != null)
            {
                return prometheusResponse.Data.ToList();
            }
            
            return new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available metrics from Prometheus");
            return new List<string>();
        }
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

/// <summary>
/// Prometheus label values API response structure
/// </summary>
public class PrometheusLabelResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("data")]
    public string[]? Data { get; set; }
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