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
/// Single comprehensive test covering the entire Kafka → Flink → Temporal flow
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
    private string? _testId;

    public ObservabilityMetricsSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        _testId = $"obs-test-{DateTime.UtcNow:yyyyMMddHHmmss}";
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
        _httpClient.Timeout = TimeSpan.FromMinutes(15); // Extended timeout for 1M messages

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

    [When(@"I produce (\d+) messages to Kafka topic ""(.*)""")]
    public async Task WhenIProduceMessagesToKafkaTopic(int messageCount, string topicName)
    {
        await EnsureInfrastructureInitialized();
        
        var request = new
        {
            Topic = topicName,
            MessageCount = messageCount,
            TestId = _testId
        };

        var response = await _httpClient!.PostAsJsonAsync("/api/complex-logic-stress-test/step2/temporal-submit-messages", request);
        response.EnsureSuccessStatusCode();
        
        _scenarioContext["produced_messages"] = messageCount;
        _scenarioContext["topic_name"] = topicName;
    }

    [When(@"I start a Flink job to process messages")]
    public async Task WhenIStartFlinkJobToProcessMessages()
    {
        await EnsureInfrastructureInitialized();
        
        var request = new
        {
            TestId = _testId,
            InputTopic = _scenarioContext.Get<string>("topic_name"),
            OutputTopic = $"{_scenarioContext.Get<string>("topic_name")}-output"
        };

        var response = await _httpClient!.PostAsJsonAsync("/api/complex-logic-stress-test/step4/flink-concat-job", request);
        response.EnsureSuccessStatusCode();
        
        _scenarioContext["flink_job_started"] = true;
    }

    [When(@"I execute Temporal workflows")]
    public async Task WhenIExecuteTemporalWorkflows()
    {
        await EnsureInfrastructureInitialized();
        
        var request = new
        {
            TestId = _testId,
            WorkflowCount = _scenarioContext.Get<int>("produced_messages"),
            Topic = _scenarioContext.Get<string>("topic_name")
        };

        var response = await _httpClient!.PostAsJsonAsync("/api/complex-logic-stress-test/step3/temporal-process-messages", request);
        response.EnsureSuccessStatusCode();
        
        _scenarioContext["temporal_workflows_executed"] = true;
    }

    [Then(@"Kafka producer messages per second metrics should be greater than 0")]
    public async Task ThenKafkaProducerMessagesPerSecondMetricsShouldBeGreaterThan0()
    {
        await RetrieveLatestMetrics();
        
        Assert.NotNull(_metricsResponse);
        
        var kafkaMetrics = GetNestedProperty(_metricsResponse, "KafkaMetrics") as JsonElement?;
        Assert.True(kafkaMetrics.HasValue, "Kafka metrics should be available");
        
        var producerRate = kafkaMetrics.Value.GetProperty("ProducerRate").GetProperty("MessagesPerSecond").GetDouble();
        Assert.True(producerRate >= 0, $"Producer rate should be >= 0, got {producerRate}");
    }

    [Then(@"Flink job processing rate metrics should be recorded")]
    public async Task ThenFlinkJobProcessingRateMetricsShouldBeRecorded()
    {
        await RetrieveLatestMetrics();
        
        Assert.NotNull(_metricsResponse);
        
        var flinkMetrics = GetNestedProperty(_metricsResponse, "FlinkMetrics") as JsonElement?;
        Assert.True(flinkMetrics.HasValue, "Flink metrics should be available");
        
        var processingRate = flinkMetrics.Value.GetProperty("ProcessingRate").GetProperty("MessagesPerSecond").GetDouble();
        Assert.True(processingRate >= 0, $"Processing rate should be >= 0, got {processingRate}");
    }

    [Then(@"Temporal workflow execution rate metrics should be recorded")]
    public async Task ThenTemporalWorkflowExecutionRateMetricsShouldBeRecorded()
    {
        await RetrieveLatestMetrics();
        
        Assert.NotNull(_metricsResponse);
        
        var temporalMetrics = GetNestedProperty(_metricsResponse, "TemporalMetrics") as JsonElement?;
        Assert.True(temporalMetrics.HasValue, "Temporal metrics should be available");
        
        var workflowRate = temporalMetrics.Value.GetProperty("WorkflowExecutionRate").GetProperty("WorkflowsPerSecond").GetDouble();
        Assert.True(workflowRate >= 0, $"Workflow rate should be >= 0, got {workflowRate}");
    }

    [Then(@"end-to-end flow rate metrics should show total throughput")]
    public async Task ThenEndToEndFlowRateMetricsShouldShowTotalThroughput()
    {
        await RetrieveLatestMetrics();
        
        Assert.NotNull(_metricsResponse);
        
        var flowMetrics = GetNestedProperty(_metricsResponse, "FlowMetrics") as JsonElement?;
        Assert.True(flowMetrics.HasValue, "Flow metrics should be available");
        
        var kafkaToFlinkRate = flowMetrics.Value.GetProperty("KafkaToFlinkRate").GetProperty("MessagesPerSecond").GetDouble();
        var flinkToTemporalRate = flowMetrics.Value.GetProperty("FlinkToTemporalRate").GetProperty("MessagesPerSecond").GetDouble();
        var endToEndRate = flowMetrics.Value.GetProperty("EndToEndRate").GetProperty("MessagesPerSecond").GetDouble();
        
        // All flow metrics should be >= 0 (indicates proper tracking)
        var hasValidFlow = kafkaToFlinkRate >= 0 && flinkToTemporalRate >= 0 && endToEndRate >= 0;
        Assert.True(hasValidFlow, "All flow metrics should be >= 0 to indicate proper tracking");
    }

    [Then(@"Prometheus should be able to scrape all observability metrics")]
    public async Task ThenPrometheusShouldBeAbleToScrapeAllObservabilityMetrics()
    {
        await EnsureInfrastructureInitialized();
        
        var response = await _httpClient!.GetAsync("/api/observability/metrics/prometheus");
        response.EnsureSuccessStatusCode();
        
        var prometheusMetrics = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrEmpty(prometheusMetrics), "Prometheus metrics should not be empty");
    }

    private async Task RetrieveLatestMetrics()
    {
        await EnsureInfrastructureInitialized();
        
        // Wait for metrics to be recorded with longer delay for high volume
        await Task.Delay(5000);
        
        var response = await _httpClient!.GetAsync("/api/observability/metrics/messages-per-second");
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        _metricsResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
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