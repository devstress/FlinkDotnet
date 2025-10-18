using NUnit.Framework;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace LearningCourse.IntegrationTests;

/// <summary>
/// Integration test for Day 5: Prometheus Observability - Persistent Metrics Validation
/// 
/// This test validates that metrics from all three sources (Kafka, Flink, FlinkDotNet Gateway)
/// are available and persistent (do not disappear after job completion).
/// </summary>
[TestFixture]
[Category("day05-prometheus-metrics")]
[Category("integration")]
public class Day05PrometheusMetricsTest : LearningCourseTestBase
{
    private static readonly HttpClient _httpClient = new();

    [Test]
    [Description("Validate persistent metrics from Kafka, Flink, and FlinkDotNet Gateway")]
    public async Task Day05_PrometheusMetricsAreAvailable()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Day 5: Prometheus Persistent Metrics Validation");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine();
        TestContext.WriteLine("Testing persistent metrics from three sources:");
        TestContext.WriteLine("  1. Kafka JMX Exporter - kafka_server metrics");
        TestContext.WriteLine("  2. Flink Cluster - flink_jobmanager metrics (persistent)");
        TestContext.WriteLine("  3. FlinkDotNet Gateway - flinkdotnet_gateway metrics");
        TestContext.WriteLine();

        // CRITICAL: Ensure infrastructure is running before testing metrics
        // The test base class sets up infrastructure in GlobalSetUp, but we need to ensure it completed
        TestContext.WriteLine("Ensuring infrastructure is ready...");
        
        // If PrometheusHostEndpoint is null, infrastructure hasn't been set up yet
        if (string.IsNullOrEmpty(PrometheusHostEndpoint))
        {
            TestContext.WriteLine("Infrastructure not ready, triggering GlobalSetUp...");
            await GlobalSetUp();
        }
        
        // Verify Prometheus is actually reachable
        if (string.IsNullOrEmpty(PrometheusHostEndpoint))
        {
            Assert.Fail("Prometheus endpoint not available after setup. Ensure LEARNINGCOURSE=true and infrastructure started correctly.");
        }
        
        TestContext.WriteLine($"Infrastructure ready - Prometheus endpoint: {PrometheusHostEndpoint}");
        TestContext.WriteLine();
        
        // Wait for Prometheus to scrape metrics from all targets
        // Prometheus needs time to: start up (~10s) + first scrape interval (15s) + targets to initialize
        // Total minimum: ~60 seconds for reliable metric availability
        TestContext.WriteLine("Waiting for Prometheus to scrape initial metrics (60 seconds)...");
        TestContext.WriteLine("   - Prometheus startup and config load: ~10 seconds");
        TestContext.WriteLine("   - First scrape interval (scrape_interval: 15s): ~15 seconds");
        TestContext.WriteLine("   - Target initialization and metrics collection: ~35 seconds");
        
        // Progress indicator every 10 seconds
        for (int i = 10; i <= 60; i += 10)
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
            TestContext.WriteLine($"   ... {i}/60 seconds elapsed");
        }
        
        TestContext.WriteLine("Proceeding with metric validation...");
        TestContext.WriteLine();

        TestContext.WriteLine($"📊 Prometheus endpoint: {PrometheusHostEndpoint}");
        TestContext.WriteLine();
        
        // Check Prometheus targets status before validating metrics
        TestContext.WriteLine("🎯 Checking Prometheus targets status...");
        try
        {
            var targetsUrl = $"{PrometheusHostEndpoint}/api/v1/targets";
            var targetsResponse = await _httpClient.GetStringAsync(targetsUrl);
            
            // Save full JSON response for debugging
            var debugFile = Path.Combine(Path.GetTempPath(), "prometheus-targets-debug.json");
            await File.WriteAllTextAsync(debugFile, targetsResponse);
            TestContext.WriteLine($"   📄 Full targets response saved to: {debugFile}");
            TestContext.WriteLine();
            
            // Parse and display target health
            var upTargets = Regex.Matches(targetsResponse, @"""health"":""up""").Count;
            var downTargets = Regex.Matches(targetsResponse, @"""health"":""down""").Count;
            
            TestContext.WriteLine($"   Targets UP: {upTargets}");
            TestContext.WriteLine($"   Targets DOWN: {downTargets}");
            TestContext.WriteLine();
            
            // Extract all target details with job names
            var jobMatches = Regex.Matches(targetsResponse, @"""job"":""([^""]+)""[^}]*""health"":""(up|down)""");
            foreach (Match match in jobMatches)
            {
                var job = match.Groups[1].Value;
                var health = match.Groups[2].Value;
                var symbol = health == "up" ? "✅" : "❌";
                TestContext.WriteLine($"   {symbol} Job '{job}': {health.ToUpper()}");
            }
            TestContext.WriteLine();
            
            if (downTargets > 0)
            {
                TestContext.WriteLine("   ⚠️  Some targets are DOWN - extracting error details...");
                
                // More comprehensive error extraction
                var targetSections = Regex.Matches(targetsResponse, @"""labels"":\{[^}]*""job"":""([^""]+)""[^}]*\}[^{]*""health"":""down""[^{]*""lastError"":""([^""]+)""");
                foreach (Match match in targetSections)
                {
                    var job = match.Groups[1].Value;
                    var error = match.Groups[2].Value;
                    TestContext.WriteLine($"   ❌ {job}: {error}");
                }
                TestContext.WriteLine();
            }
            else if (upTargets > 0)
            {
                TestContext.WriteLine("   ✅ All targets are UP and scraping");
            }
            else
            {
                TestContext.WriteLine("   ⚠️  No active targets found - check Prometheus configuration");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"   ⚠️  Could not check targets status: {ex.Message}");
        }
        TestContext.WriteLine();

        var successCount = 0;
        var failureMessages = new List<string>();

        // Test 1: Kafka JMX Metrics (always available)
        TestContext.WriteLine("▶️  Test 1: Validating Kafka JMX metrics");
        try
        {
            var kafkaMetric = "kafka_server_brokertopicmetrics_messagesinpersec";
            var kafkaUrl = $"{PrometheusHostEndpoint}/api/v1/query?query={kafkaMetric}";
            
            TestContext.WriteLine($"   Query: {kafkaMetric}");
            TestContext.WriteLine($"   URL: {kafkaUrl}");
            
            var kafkaResponse = await _httpClient.GetStringAsync(kafkaUrl);
            
            // Save response for debugging
            var kafkaDebugFile = Path.Combine(Path.GetTempPath(), "prometheus-kafka-query.json");
            await File.WriteAllTextAsync(kafkaDebugFile, kafkaResponse);
            TestContext.WriteLine($"   📄 Kafka query response saved to: {kafkaDebugFile}");
            
            // Parse JSON response to check for results
            var hasResults = kafkaResponse.Contains("\"result\":[") && 
                           !kafkaResponse.Contains("\"result\":[]");
            
            if (hasResults)
            {
                // Extract metric value if available
                var valueMatch = Regex.Match(kafkaResponse, @"""value"":\s*\[\s*[\d.]+\s*,\s*""([\d.]+)""");
                var value = valueMatch.Success ? valueMatch.Groups[1].Value : "N/A";
                
                TestContext.WriteLine($"   ✅ Kafka metrics available - Value: {value}");
                TestContext.WriteLine($"   📊 Metric: {kafkaMetric}");
                successCount++;
            }
            else
            {
                var message = $"   ❌ Kafka metrics NOT available - No results found for {kafkaMetric}";
                TestContext.WriteLine(message);
                failureMessages.Add(message);
            }
        }
        catch (Exception ex)
        {
            var message = $"   ❌ Kafka metrics query FAILED: {ex.Message}";
            TestContext.WriteLine(message);
            failureMessages.Add(message);
        }

        TestContext.WriteLine();

        // Test 2: Flink Cluster Metrics (persistent - not job-specific)
        TestContext.WriteLine("▶️  Test 2: Validating Flink cluster metrics (persistent)");
        try
        {
            var flinkMetric = "flink_jobmanager_numRegisteredTaskManagers";
            var flinkUrl = $"{PrometheusHostEndpoint}/api/v1/query?query={flinkMetric}";
            
            TestContext.WriteLine($"   Query: {flinkMetric}");
            TestContext.WriteLine($"   URL: {flinkUrl}");
            TestContext.WriteLine($"   Note: This metric persists regardless of job status");
            
            var flinkResponse = await _httpClient.GetStringAsync(flinkUrl);
            
            // Save response for debugging
            var flinkDebugFile = Path.Combine(Path.GetTempPath(), "prometheus-flink-query.json");
            await File.WriteAllTextAsync(flinkDebugFile, flinkResponse);
            TestContext.WriteLine($"   📄 Flink query response saved to: {flinkDebugFile}");
            
            var hasResults = flinkResponse.Contains("\"result\":[") && 
                           !flinkResponse.Contains("\"result\":[]");
            
            if (hasResults)
            {
                var valueMatch = Regex.Match(flinkResponse, @"""value"":\s*\[\s*[\d.]+\s*,\s*""([\d.]+)""");
                var value = valueMatch.Success ? valueMatch.Groups[1].Value : "N/A";
                
                TestContext.WriteLine($"   ✅ Flink cluster metrics available - TaskManagers: {value}");
                TestContext.WriteLine($"   📊 Metric: {flinkMetric}");
                successCount++;
            }
            else
            {
                var message = $"   ❌ Flink cluster metrics NOT available - No results found for {flinkMetric}";
                TestContext.WriteLine(message);
                failureMessages.Add(message);
            }
        }
        catch (Exception ex)
        {
            var message = $"   ❌ Flink cluster metrics query FAILED: {ex.Message}";
            TestContext.WriteLine(message);
            failureMessages.Add(message);
        }

        TestContext.WriteLine();

        // Test 3: FlinkDotNet Gateway Metrics (persistent)
        TestContext.WriteLine("▶️  Test 3: Validating FlinkDotNet Gateway metrics (persistent)");
        try
        {
            var gatewayMetric = "flinkdotnet_gateway_jobs_submitted_total";
            var gatewayUrl = $"{PrometheusHostEndpoint}/api/v1/query?query={gatewayMetric}";
            
            TestContext.WriteLine($"   Query: {gatewayMetric}");
            TestContext.WriteLine($"   URL: {gatewayUrl}");
            TestContext.WriteLine($"   Note: This metric accumulates across all job submissions");
            
            var gatewayResponse = await _httpClient.GetStringAsync(gatewayUrl);
            
            // Save response for debugging
            var gatewayDebugFile = Path.Combine(Path.GetTempPath(), "prometheus-gateway-query.json");
            await File.WriteAllTextAsync(gatewayDebugFile, gatewayResponse);
            TestContext.WriteLine($"   📄 Gateway query response saved to: {gatewayDebugFile}");
            
            var hasResults = gatewayResponse.Contains("\"result\":[") && 
                           !gatewayResponse.Contains("\"result\":[]");
            
            if (hasResults)
            {
                var valueMatch = Regex.Match(gatewayResponse, @"""value"":\s*\[\s*[\d.]+\s*,\s*""([\d.]+)""");
                var value = valueMatch.Success ? valueMatch.Groups[1].Value : "0";
                
                TestContext.WriteLine($"   ✅ Gateway metrics available - Jobs submitted: {value}");
                TestContext.WriteLine($"   📊 Metric: {gatewayMetric}");
                successCount++;
            }
            else
            {
                var message = $"   ❌ Gateway metrics NOT available - No results found for {gatewayMetric}";
                TestContext.WriteLine(message);
                TestContext.WriteLine("   ⚠️  This may indicate the Gateway Prometheus exporter is not configured");
                failureMessages.Add(message);
            }
        }
        catch (Exception ex)
        {
            var message = $"   ❌ Gateway metrics query FAILED: {ex.Message}";
            TestContext.WriteLine(message);
            failureMessages.Add(message);
        }

        TestContext.WriteLine();
        TestContext.WriteLine("╔═══════════════════════════════════════════════════════════╗");
        TestContext.WriteLine("║    PROMETHEUS METRICS VALIDATION SUMMARY                  ║");
        TestContext.WriteLine("╚═══════════════════════════════════════════════════════════╝");
        TestContext.WriteLine();
        TestContext.WriteLine($"   Sources Validated: {successCount}/3");
        TestContext.WriteLine($"   ✓ Kafka JMX Exporter: {(successCount >= 1 ? "Available" : "FAILED")}");
        TestContext.WriteLine($"   ✓ Flink Cluster: {(successCount >= 2 ? "Available" : "FAILED")}");
        TestContext.WriteLine($"   ✓ FlinkDotNet Gateway: {(successCount >= 3 ? "Available" : "FAILED")}");
        TestContext.WriteLine();

        if (failureMessages.Count > 0)
        {
            TestContext.WriteLine("❌ FAILURES DETECTED:");
            foreach (var msg in failureMessages)
            {
                TestContext.WriteLine(msg);
            }
            TestContext.WriteLine();
        }

        // Assert that ALL three sources are available
        Assert.That(successCount, Is.EqualTo(3), 
            $"Expected all 3 metric sources to be available, but only {successCount} succeeded.\n" +
            $"Failures:\n{string.Join("\n", failureMessages)}");

        TestContext.WriteLine("✅ All persistent metrics validated successfully");
        TestContext.WriteLine("   Complete observability stack operational:");
        TestContext.WriteLine("   ✓ Kafka metrics (message throughput)");
        TestContext.WriteLine("   ✓ Flink metrics (cluster health)");
        TestContext.WriteLine("   ✓ Gateway metrics (job submissions)");
    }
}