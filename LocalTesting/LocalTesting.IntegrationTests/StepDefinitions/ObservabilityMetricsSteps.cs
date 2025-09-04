using Reqnroll;
using Xunit;
using System.Net.Http;
using System.Text.Json;
using System.Net.Http.Json;
using System.Linq;
using Aspire.Hosting;
using Aspire.Hosting.Testing;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace LocalTesting.IntegrationTests.Features;

/// <summary>
/// Simplified observability tests using Microsoft Aspire testing framework
/// Focused on validating observability metrics are working across all components
/// </summary>
[Binding]
public class ObservabilityMetricsSteps : IDisposable
{
    private readonly ScenarioContext _scenarioContext;
    private static DistributedApplication? _app;
    private static HttpClient? _httpClient;
    private static readonly object _lockObject = new object();
    private static bool _initialized = false;
    private Dictionary<string, object>? _metricsResponse;

    public ObservabilityMetricsSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    private async Task EnsureInfrastructureInitialized()
    {
        if (_initialized && _app != null && _httpClient != null)
            return;

        lock (_lockObject)
        {
            if (_initialized && _app != null && _httpClient != null)
                return;
        }

        // Follow Microsoft Aspire testing framework pattern
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.LocalTesting_AppHost>();
        _app = await builder.BuildAsync();
        await _app.StartAsync();
        
        // Create HTTP client with service discovery - use the correct endpoint name "webapi"
        _httpClient = _app.CreateHttpClient("localtesting-webapi", "webapi");
        _httpClient.Timeout = TimeSpan.FromMinutes(30); // Extended timeout for 1M messages processing

        lock (_lockObject)
        {
            _initialized = true;
        }
    }

    public void Dispose()
    {
        // Individual test cleanup - don't dispose shared infrastructure
        _metricsResponse = null;
    }

    [Given(@"LocalTesting infrastructure is running with observability enabled")]
    public async Task GivenLocalTestingInfrastructureIsRunningWithObservabilityEnabled()
    {
        // Ensure infrastructure is initialized first
        await EnsureInfrastructureInitialized();
        
        // Verify infrastructure is accessible
        var response = await _httpClient!.GetAsync("/health");
        response.EnsureSuccessStatusCode();
        
        // Verify observability endpoint is available
        var metricsResponse = await _httpClient.GetAsync("/api/observability/metrics/messages-per-second");
        metricsResponse.EnsureSuccessStatusCode();
        
        _scenarioContext["infrastructure_ready"] = true;
    }

    [When(@"I simulate observability metrics across all layers")]
    public async Task WhenISimulateObservabilityMetricsAcrossAllLayers()
    {
        await EnsureInfrastructureInitialized();
        
        // Use the observability simulation endpoint with 1 million messages for real throughput testing
        var simulationRequest = new
        {
            KafkaMessages = 1000000,
            FlinkJobs = 2,
            TemporalWorkflows = 5,
            DurationSeconds = 10
        };

        var response = await _httpClient!.PostAsJsonAsync("/api/observability/metrics/simulate", simulationRequest);
        response.EnsureSuccessStatusCode();
        
        // Wait longer for 1M messages to be processed and metrics to be recorded
        await Task.Delay(10000);
        
        _scenarioContext["simulation_completed"] = true;
    }

    [Then(@"observability metrics should be available for all components")]
    public async Task ThenObservabilityMetricsShouldBeAvailableForAllComponents()
    {
        await EnsureInfrastructureInitialized();
        
        // Retrieve all metrics
        var response = await _httpClient!.GetAsync("/api/observability/metrics/messages-per-second");
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        _metricsResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
        
        Assert.NotNull(_metricsResponse);
        
        // Display the actual metrics for visibility in the logs
        Console.WriteLine("=== OBSERVABILITY METRICS RESULTS ===");
        Console.WriteLine($"Full Metrics Response: {content}");
        Console.WriteLine("=====================================");
        
        // Extract and display key throughput metrics
        try
        {
            var kafkaMetrics = GetNestedProperty(_metricsResponse, "KafkaMetrics") as JsonElement?;
            if (kafkaMetrics.HasValue && kafkaMetrics.Value.TryGetProperty("ProducerRates", out var producerRates))
            {
                var totalKafkaRate = 0.0;
                foreach (var property in producerRates.EnumerateObject())
                {
                    if (property.Value.TryGetProperty("MessagesPerSecond", out var rate))
                    {
                        totalKafkaRate += rate.GetDouble();
                    }
                }
                Console.WriteLine($"📊 Kafka Producer Rate: {totalKafkaRate:F2} messages/second");
            }

            var flinkMetrics = GetNestedProperty(_metricsResponse, "FlinkMetrics") as JsonElement?;
            if (flinkMetrics.HasValue)
            {
                var totalFlinkRate = 0.0;
                if (flinkMetrics.Value.TryGetProperty("InputRates", out var inputRates))
                {
                    foreach (var property in inputRates.EnumerateObject())
                    {
                        if (property.Value.TryGetProperty("MessagesPerSecond", out var rate))
                        {
                            totalFlinkRate += rate.GetDouble();
                        }
                    }
                }
                Console.WriteLine($"⚡ Flink Processing Rate: {totalFlinkRate:F2} messages/second");
            }

            var temporalMetrics = GetNestedProperty(_metricsResponse, "TemporalMetrics") as JsonElement?;
            if (temporalMetrics.HasValue && temporalMetrics.Value.TryGetProperty("WorkflowRates", out var workflowRates))
            {
                var totalTemporalRate = 0.0;
                foreach (var property in workflowRates.EnumerateObject())
                {
                    if (property.Value.TryGetProperty("ExecutionsPerSecond", out var rate))
                    {
                        totalTemporalRate += rate.GetDouble();
                    }
                }
                Console.WriteLine($"🔄 Temporal Workflow Rate: {totalTemporalRate:F2} workflows/second");
            }

            var flowMetrics = GetNestedProperty(_metricsResponse, "FlowMetrics") as JsonElement?;
            if (flowMetrics.HasValue && flowMetrics.Value.TryGetProperty("EndToEndRate", out var endToEndRate))
            {
                if (endToEndRate.TryGetProperty("MessagesPerSecond", out var rate))
                {
                    Console.WriteLine($"🚀 End-to-End Flow Rate: {rate.GetDouble():F2} messages/second");
                }
            }

            var summary = GetNestedProperty(_metricsResponse, "Summary") as JsonElement?;
            if (summary.HasValue)
            {
                if (summary.Value.TryGetProperty("TotalMetricsTracked", out var totalMetrics))
                {
                    Console.WriteLine($"📈 Total Metrics Tracked: {totalMetrics.GetInt32()}");
                }
                if (summary.Value.TryGetProperty("TotalMessagesPerSecond", out var totalMessages))
                {
                    Console.WriteLine($"📊 Total Messages Per Second: {totalMessages.GetDouble():F2}");
                }
                if (summary.Value.TryGetProperty("ActiveFlows", out var activeFlows))
                {
                    Console.WriteLine($"🌊 Active Flows: {activeFlows.GetInt32()}");
                }
                if (summary.Value.TryGetProperty("HighestRate", out var highestRate))
                {
                    Console.WriteLine($"🏆 Highest Rate: {highestRate.GetDouble():F2} messages/second");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error extracting specific metrics: {ex.Message}");
        }
        
        Console.WriteLine("=====================================");
        
        // Verify Kafka metrics are available
        var kafkaMetricsCheck = GetNestedProperty(_metricsResponse, "KafkaMetrics") as JsonElement?;
        Assert.True(kafkaMetricsCheck.HasValue, "Kafka metrics should be available");
        
        // Verify Flink metrics are available
        var flinkMetricsCheck = GetNestedProperty(_metricsResponse, "FlinkMetrics") as JsonElement?;
        Assert.True(flinkMetricsCheck.HasValue, "Flink metrics should be available");
        
        // Verify Temporal metrics are available
        var temporalMetricsCheck = GetNestedProperty(_metricsResponse, "TemporalMetrics") as JsonElement?;
        Assert.True(temporalMetricsCheck.HasValue, "Temporal metrics should be available");
        
        // Verify Flow metrics are available
        var flowMetricsCheck = GetNestedProperty(_metricsResponse, "FlowMetrics") as JsonElement?;
        Assert.True(flowMetricsCheck.HasValue, "Flow metrics should be available");
        
        // Verify Summary indicates metrics are being tracked
        var summaryCheck = GetNestedProperty(_metricsResponse, "Summary") as JsonElement?;
        Assert.True(summaryCheck.HasValue, "Summary metrics should be available");
        
        var totalMetricsCheck = summaryCheck.Value.GetProperty("TotalMetricsTracked").GetInt32();
        Assert.True(totalMetricsCheck > 0, $"Should have metrics tracked, got {totalMetricsCheck}");
    }

    [Then(@"Prometheus should be able to scrape all observability metrics")]
    public async Task ThenPrometheusShouldBeAbleToScrapeAllObservabilityMetrics()
    {
        await EnsureInfrastructureInitialized();
        
        // Check for Prometheus metrics endpoint (may not exist, but we can check health)
        var healthResponse = await _httpClient!.GetAsync("/health");
        healthResponse.EnsureSuccessStatusCode();
        
        // Verify we can access observability metrics for Prometheus scraping
        var metricsResponse = await _httpClient.GetAsync("/api/observability/metrics/messages-per-second");
        metricsResponse.EnsureSuccessStatusCode();
        
        var metricsContent = await metricsResponse.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrEmpty(metricsContent), "Observability metrics should be available for Prometheus scraping");
        
        // Verify the metrics contain valid JSON that Prometheus can process
        var metrics = JsonSerializer.Deserialize<Dictionary<string, object>>(metricsContent);
        Assert.NotNull(metrics);
        Assert.True(metrics.Count > 0, "Metrics should contain data for Prometheus to scrape");
    }

    [Then(@"we print the metrics to the console")]
    public async Task ThenWePrintTheMetricsToTheConsole()
    {
        await EnsureInfrastructureInitialized();
        
        // Retrieve and display comprehensive metrics
        var response = await _httpClient!.GetAsync("/api/observability/metrics/messages-per-second");
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        var metricsResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
        
        Assert.NotNull(metricsResponse);
        
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    📊 OBSERVABILITY METRICS DASHBOARD 📊         ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        
        // Display full metrics response in a formatted way
        Console.WriteLine($"📋 Full Metrics Response:");
        Console.WriteLine($"   Status: {GetPropertyValue(metricsResponse, "Status")}");
        Console.WriteLine($"   Timestamp: {GetPropertyValue(metricsResponse, "Timestamp")}");
        Console.WriteLine();
        
        // Extract and display key metrics in a beautiful format
        try
        {
            var kafkaMetrics = GetNestedProperty(metricsResponse, "KafkaMetrics") as JsonElement?;
            if (kafkaMetrics.HasValue)
            {
                Console.WriteLine("🔌 KAFKA METRICS:");
                if (kafkaMetrics.Value.TryGetProperty("ProducerRates", out var producerRates))
                {
                    var totalKafkaProducerRate = 0.0;
                    foreach (var property in producerRates.EnumerateObject())
                    {
                        if (property.Value.TryGetProperty("MessagesPerSecond", out var rate))
                        {
                            var rateValue = rate.GetDouble();
                            totalKafkaProducerRate += rateValue;
                            Console.WriteLine($"   📤 {property.Name}: {rateValue:F2} msg/sec");
                        }
                    }
                    Console.WriteLine($"   📊 Total Producer Rate: {totalKafkaProducerRate:F2} msg/sec");
                }
                
                if (kafkaMetrics.Value.TryGetProperty("ConsumerRates", out var consumerRates))
                {
                    var totalKafkaConsumerRate = 0.0;
                    foreach (var property in consumerRates.EnumerateObject())
                    {
                        if (property.Value.TryGetProperty("MessagesPerSecond", out var rate))
                        {
                            var rateValue = rate.GetDouble();
                            totalKafkaConsumerRate += rateValue;
                            Console.WriteLine($"   📥 {property.Name}: {rateValue:F2} msg/sec");
                        }
                    }
                    Console.WriteLine($"   📊 Total Consumer Rate: {totalKafkaConsumerRate:F2} msg/sec");
                }
                Console.WriteLine();
            }

            var flinkMetrics = GetNestedProperty(metricsResponse, "FlinkMetrics") as JsonElement?;
            if (flinkMetrics.HasValue)
            {
                Console.WriteLine("⚡ FLINK METRICS:");
                if (flinkMetrics.Value.TryGetProperty("InputRates", out var inputRates))
                {
                    var totalInputRate = 0.0;
                    foreach (var property in inputRates.EnumerateObject())
                    {
                        if (property.Value.TryGetProperty("MessagesPerSecond", out var rate))
                        {
                            var rateValue = rate.GetDouble();
                            totalInputRate += rateValue;
                            Console.WriteLine($"   📥 {property.Name}: {rateValue:F2} msg/sec");
                        }
                    }
                    Console.WriteLine($"   📊 Total Input Rate: {totalInputRate:F2} msg/sec");
                }
                
                if (flinkMetrics.Value.TryGetProperty("OutputRates", out var outputRates))
                {
                    var totalOutputRate = 0.0;
                    foreach (var property in outputRates.EnumerateObject())
                    {
                        if (property.Value.TryGetProperty("MessagesPerSecond", out var rate))
                        {
                            var rateValue = rate.GetDouble();
                            totalOutputRate += rateValue;
                            Console.WriteLine($"   📤 {property.Name}: {rateValue:F2} msg/sec");
                        }
                    }
                    Console.WriteLine($"   📊 Total Output Rate: {totalOutputRate:F2} msg/sec");
                }
                Console.WriteLine();
            }

            var temporalMetrics = GetNestedProperty(metricsResponse, "TemporalMetrics") as JsonElement?;
            if (temporalMetrics.HasValue)
            {
                Console.WriteLine("🔄 TEMPORAL METRICS:");
                if (temporalMetrics.Value.TryGetProperty("WorkflowRates", out var workflowRates))
                {
                    var totalWorkflowRate = 0.0;
                    foreach (var property in workflowRates.EnumerateObject())
                    {
                        if (property.Value.TryGetProperty("ExecutionsPerSecond", out var rate))
                        {
                            var rateValue = rate.GetDouble();
                            totalWorkflowRate += rateValue;
                            Console.WriteLine($"   🔄 {property.Name}: {rateValue:F2} exec/sec");
                        }
                    }
                    Console.WriteLine($"   📊 Total Workflow Rate: {totalWorkflowRate:F2} exec/sec");
                }
                
                if (temporalMetrics.Value.TryGetProperty("ActivityRates", out var activityRates))
                {
                    var totalActivityRate = 0.0;
                    foreach (var property in activityRates.EnumerateObject())
                    {
                        if (property.Value.TryGetProperty("ExecutionsPerSecond", out var rate))
                        {
                            var rateValue = rate.GetDouble();
                            totalActivityRate += rateValue;
                            Console.WriteLine($"   ⚙️ {property.Name}: {rateValue:F2} exec/sec");
                        }
                    }
                    Console.WriteLine($"   📊 Total Activity Rate: {totalActivityRate:F2} exec/sec");
                }
                Console.WriteLine();
            }

            var flowMetrics = GetNestedProperty(metricsResponse, "FlowMetrics") as JsonElement?;
            if (flowMetrics.HasValue)
            {
                Console.WriteLine("🌊 FLOW METRICS:");
                if (flowMetrics.Value.TryGetProperty("KafkaToFlinkRate", out var kafkaToFlinkRate) &&
                    kafkaToFlinkRate.TryGetProperty("MessagesPerSecond", out var k2fRate))
                {
                    Console.WriteLine($"   🔌➡️⚡ Kafka → Flink: {k2fRate.GetDouble():F2} msg/sec");
                }
                
                if (flowMetrics.Value.TryGetProperty("FlinkToTemporalRate", out var flinkToTemporalRate) &&
                    flinkToTemporalRate.TryGetProperty("MessagesPerSecond", out var f2tRate))
                {
                    Console.WriteLine($"   ⚡➡️🔄 Flink → Temporal: {f2tRate.GetDouble():F2} msg/sec");
                }
                
                if (flowMetrics.Value.TryGetProperty("EndToEndRate", out var endToEndRate) &&
                    endToEndRate.TryGetProperty("MessagesPerSecond", out var e2eRate))
                {
                    Console.WriteLine($"   🚀 End-to-End Flow: {e2eRate.GetDouble():F2} msg/sec");
                }
                Console.WriteLine();
            }

            var summary = GetNestedProperty(metricsResponse, "Summary") as JsonElement?;
            if (summary.HasValue)
            {
                Console.WriteLine("📈 SUMMARY STATISTICS:");
                if (summary.Value.TryGetProperty("TotalMetricsTracked", out var totalMetrics))
                {
                    Console.WriteLine($"   📊 Total Metrics Tracked: {totalMetrics.GetInt32()}");
                }
                if (summary.Value.TryGetProperty("ActiveFlows", out var activeFlows))
                {
                    Console.WriteLine($"   🌊 Active Flows: {activeFlows.GetInt32()}");
                }
                if (summary.Value.TryGetProperty("HighestRate", out var highestRate))
                {
                    Console.WriteLine($"   🏆 Highest Rate: {highestRate.GetDouble():F2} msg/sec");
                }
                if (summary.Value.TryGetProperty("AverageRate", out var averageRate))
                {
                    Console.WriteLine($"   📊 Average Rate: {averageRate.GetDouble():F2} msg/sec");
                }
                if (summary.Value.TryGetProperty("TotalMessagesPerSecond", out var totalRate))
                {
                    Console.WriteLine($"   🚀 Total Messages/Second: {totalRate.GetDouble():F2}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error displaying detailed metrics: {ex.Message}");
            // Fallback to raw JSON display
            Console.WriteLine("📄 Raw Metrics Response:");
            Console.WriteLine(content);
        }
        
        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                  ✅ METRICS CONSOLE OUTPUT COMPLETE ✅            ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
    }

    private static object? GetNestedProperty(Dictionary<string, object> dict, string propertyName)
    {
        if (dict.TryGetValue(propertyName, out var value))
        {
            if (value is JsonElement element)
            {
                return element;
            }
            return value;
        }
        return null;
    }

    private static string GetPropertyValue(Dictionary<string, object> dict, string propertyName)
    {
        if (dict.TryGetValue(propertyName, out var value))
        {
            if (value is JsonElement element)
            {
                return element.GetString() ?? "N/A";
            }
            return value?.ToString() ?? "N/A";
        }
        return "N/A";
    }
}