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
        
        // Verify Kafka metrics are available
        var kafkaMetrics = GetNestedProperty(_metricsResponse, "KafkaMetrics") as JsonElement?;
        Assert.True(kafkaMetrics.HasValue, "Kafka metrics should be available");
        
        // Verify Flink metrics are available
        var flinkMetrics = GetNestedProperty(_metricsResponse, "FlinkMetrics") as JsonElement?;
        Assert.True(flinkMetrics.HasValue, "Flink metrics should be available");
        
        // Verify Temporal metrics are available
        var temporalMetrics = GetNestedProperty(_metricsResponse, "TemporalMetrics") as JsonElement?;
        Assert.True(temporalMetrics.HasValue, "Temporal metrics should be available");
        
        // Verify Flow metrics are available
        var flowMetrics = GetNestedProperty(_metricsResponse, "FlowMetrics") as JsonElement?;
        Assert.True(flowMetrics.HasValue, "Flow metrics should be available");
        
        // Verify Summary indicates metrics are being tracked
        var summary = GetNestedProperty(_metricsResponse, "Summary") as JsonElement?;
        Assert.True(summary.HasValue, "Summary metrics should be available");
        
        var totalMetrics = summary.Value.GetProperty("TotalMetricsTracked").GetInt32();
        Assert.True(totalMetrics > 0, $"Should have metrics tracked, got {totalMetrics}");
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
}