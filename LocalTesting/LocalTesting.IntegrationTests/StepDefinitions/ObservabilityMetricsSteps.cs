using Reqnroll;
using Xunit;
using System.Net.Http;
using System.Text.Json;
using System.Net.Http.Json;
using Aspire.Hosting;
using Aspire.Hosting.Testing;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace LocalTesting.IntegrationTests.Features;

/// <summary>
/// Simplified observability tests - just run the flow and print metrics
/// </summary>
[Binding]
public class ObservabilityMetricsSteps : IDisposable
{
    private readonly ScenarioContext _scenarioContext;
    private static DistributedApplication? _app;
    private static HttpClient? _httpClient;
    private static readonly object _lockObject = new object();
    private static bool _initialized = false;

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
        
        // Create HTTP client with service discovery
        _httpClient = _app.CreateHttpClient("localtesting-webapi", "webapi");
        _httpClient.Timeout = TimeSpan.FromMinutes(30);

        lock (_lockObject)
        {
            _initialized = true;
        }
    }

    public void Dispose()
    {
        // Individual test cleanup
    }

    [When(@"I run the entire flow")]
    public async Task WhenIRunTheEntireFlow()
    {
        await EnsureInfrastructureInitialized();
        
        // Verify infrastructure is accessible
        var response = await _httpClient!.GetAsync("/health");
        response.EnsureSuccessStatusCode();
        
        // Run the simulation to generate metrics
        var simulationRequest = new
        {
            KafkaMessages = 1000000,
            FlinkJobs = 2,
            TemporalWorkflows = 5,
            DurationSeconds = 10
        };

        var simulationResponse = await _httpClient.PostAsJsonAsync("/api/observability/metrics/simulate", simulationRequest);
        simulationResponse.EnsureSuccessStatusCode();
        
        // Wait for processing
        await Task.Delay(10000);
        
        _scenarioContext["flow_completed"] = true;
    }

    [Then(@"we print the metrics to the console")]
    public async Task ThenWePrintTheMetricsToTheConsole()
    {
        await EnsureInfrastructureInitialized();
        
        // Retrieve and display metrics
        var response = await _httpClient!.GetAsync("/api/observability/metrics/messages-per-second");
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        var metricsResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
        
        Assert.NotNull(metricsResponse);
        
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    📊 OBSERVABILITY METRICS DASHBOARD 📊         ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        
        // Display metrics summary
        Console.WriteLine($"📋 Status: {GetPropertyValue(metricsResponse, "Status")}");
        Console.WriteLine($"📅 Timestamp: {GetPropertyValue(metricsResponse, "Timestamp")}");
        Console.WriteLine();
        
        // Display key metrics
        try
        {
            var summary = GetNestedProperty(metricsResponse, "Summary") as JsonElement?;
            if (summary.HasValue)
            {
                Console.WriteLine("📈 SUMMARY STATISTICS:");
                if (summary.Value.TryGetProperty("TotalMessagesPerSecond", out var totalRate))
                {
                    Console.WriteLine($"   🚀 Total Messages/Second: {totalRate.GetDouble():F2}");
                }
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
            }

            // Display component metrics briefly
            var kafkaMetrics = GetNestedProperty(metricsResponse, "KafkaMetrics") as JsonElement?;
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
                Console.WriteLine($"   🔌 Kafka Rate: {totalKafkaRate:F2} msg/sec");
            }

            var flinkMetrics = GetNestedProperty(metricsResponse, "FlinkMetrics") as JsonElement?;
            if (flinkMetrics.HasValue && flinkMetrics.Value.TryGetProperty("InputRates", out var inputRates))
            {
                var totalFlinkRate = 0.0;
                foreach (var property in inputRates.EnumerateObject())
                {
                    if (property.Value.TryGetProperty("MessagesPerSecond", out var rate))
                    {
                        totalFlinkRate += rate.GetDouble();
                    }
                }
                Console.WriteLine($"   ⚡ Flink Rate: {totalFlinkRate:F2} msg/sec");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error displaying metrics: {ex.Message}");
            Console.WriteLine("📄 Raw Metrics Response:");
            Console.WriteLine(content);
        }
        
        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                  ✅ METRICS DISPLAY COMPLETE ✅                   ║");
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