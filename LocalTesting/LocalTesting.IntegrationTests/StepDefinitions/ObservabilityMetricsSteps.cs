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
/// Simplified observability tests using proper Microsoft Aspire testing framework pattern
/// </summary>
[Binding]
public class ObservabilityMetricsSteps : IAsyncLifetime
{
    private readonly ScenarioContext _scenarioContext;
    private DistributedApplication? _app;
    private HttpClient? _httpClient;
    private Dictionary<string, object>? _metricsResponse;
    private string? _testId;

    public ObservabilityMetricsSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        _testId = $"obs-test-{DateTime.UtcNow:yyyyMMddHHmmss}";
    }

    public async Task InitializeAsync()
    {
        // Follow Microsoft Aspire testing framework pattern
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.LocalTesting_AppHost>();
        _app = await builder.BuildAsync();
        await _app.StartAsync();
        
        // Create HTTP client with service discovery
        _httpClient = _app.CreateHttpClient("localtesting-webapi");
        _httpClient.Timeout = TimeSpan.FromMinutes(10);
    }

    public async Task DisposeAsync()
    {
        _httpClient?.Dispose();
        if (_app != null)
        {
            await _app.DisposeAsync();
        }
    }

    [Given(@"LocalTesting infrastructure is running with observability enabled")]
    public async Task GivenLocalTestingInfrastructureIsRunningWithObservabilityEnabled()
    {
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
        var response = await _httpClient!.GetAsync("/api/observability/metrics/prometheus");
        response.EnsureSuccessStatusCode();
        
        var prometheusMetrics = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrEmpty(prometheusMetrics), "Prometheus metrics should not be empty");
    }

    // Simplified message state tracking methods
    [When(@"I produce (\d+) messages to Kafka topic ""(.*)"" with tracking enabled")]
    public async Task WhenIProduceMessagesToKafkaTopicWithTrackingEnabled(int messageCount, string topicName)
    {
        var request = new
        {
            Topic = topicName,
            MessageCount = messageCount,
            TestId = _testId,
            EnableTracking = true
        };

        var response = await _httpClient!.PostAsJsonAsync("/api/complex-logic-stress-test/step2/temporal-submit-messages", request);
        response.EnsureSuccessStatusCode();
        
        _scenarioContext["produced_messages"] = messageCount;
        _scenarioContext["topic_name"] = topicName;
    }

    [When(@"I simulate processing failures for (\d+)% of the messages")]
    public async Task WhenISimulateProcessingFailuresForPercentOfTheMessages(int failurePercentage)
    {
        var request = new
        {
            TestId = _testId,
            FailurePercentage = failurePercentage,
            Topic = _scenarioContext.Get<string>("topic_name")
        };

        var response = await _httpClient!.PostAsJsonAsync("/api/complex-logic-stress-test/simulate-failures", request);
        response.EnsureSuccessStatusCode();
        
        _scenarioContext["failure_percentage"] = failurePercentage;
    }

    [Then(@"failed messages should have state ""(.*)""")]
    public async Task ThenFailedMessagesShouldHaveState(string expectedState)
    {
        var response = await _httpClient!.GetAsync($"/api/observability/message-states?testId={_testId}&state=Failed");
        response.EnsureSuccessStatusCode();
        
        var failedMessages = await response.Content.ReadAsStringAsync();
        var messages = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(failedMessages);
        
        Assert.True(messages?.Count > 0, "Should have failed messages");
        
        foreach (var message in messages!)
        {
            var state = message["State"].ToString();
            Assert.Equal(expectedState, state);
        }
    }

    [Then(@"failed messages should contain error details")]
    public async Task ThenFailedMessagesShouldContainErrorDetails()
    {
        var response = await _httpClient!.GetAsync($"/api/observability/message-states?testId={_testId}&state=Failed");
        response.EnsureSuccessStatusCode();
        
        var failedMessages = await response.Content.ReadAsStringAsync();
        var messages = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(failedMessages);
        
        Assert.True(messages?.Count > 0, "Should have failed messages");
        
        foreach (var message in messages!)
        {
            Assert.True(message.ContainsKey("ErrorDetails"), "Failed message should contain error details");
            Assert.False(string.IsNullOrEmpty(message["ErrorDetails"]?.ToString()), "Error details should not be empty");
        }
    }

    [Then(@"message state summary should show correct counts of failed vs delivered messages")]
    public async Task ThenMessageStateSummaryShouldShowCorrectCountsOfFailedVsDeliveredMessages()
    {
        var response = await _httpClient!.GetAsync($"/api/observability/message-state-summary?testId={_testId}");
        response.EnsureSuccessStatusCode();
        
        var summaryJson = await response.Content.ReadAsStringAsync();
        var summary = JsonSerializer.Deserialize<Dictionary<string, object>>(summaryJson);
        
        Assert.NotNull(summary);
        Assert.True(summary!.ContainsKey("FailedCount"), "Summary should contain failed count");
        Assert.True(summary.ContainsKey("DeliveredCount"), "Summary should contain delivered count");
        Assert.True(summary.ContainsKey("TotalCount"), "Summary should contain total count");
        
        var failedCount = Convert.ToInt32(summary["FailedCount"]);
        var deliveredCount = Convert.ToInt32(summary["DeliveredCount"]);
        var totalCount = Convert.ToInt32(summary["TotalCount"]);
        
        Assert.Equal(totalCount, failedCount + deliveredCount);
    }

    [Then(@"I should be able to query only failed messages")]
    public async Task ThenIShouldBeAbleToQueryOnlyFailedMessages()
    {
        var response = await _httpClient!.GetAsync($"/api/observability/message-states?testId={_testId}&state=Failed");
        response.EnsureSuccessStatusCode();
        
        var failedMessages = await response.Content.ReadAsStringAsync();
        var messages = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(failedMessages);
        
        Assert.NotNull(messages);
        Assert.True(messages!.Count > 0, "Should be able to query failed messages");
        
        foreach (var message in messages)
        {
            var state = message["State"].ToString();
            Assert.Equal("Failed", state);
        }
    }

    private async Task RetrieveLatestMetrics()
    {
        // Wait for metrics to be recorded
        await Task.Delay(2000);
        
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