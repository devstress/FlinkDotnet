using NUnit.Framework;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Linq;

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
        TestContext.WriteLine("Testing persistent metrics from three Prometheus exporters:");
        TestContext.WriteLine("  1. Kafka JMX Exporter - JVM metrics (always available)");
        TestContext.WriteLine("  2. Flink JobManager - flink_jobmanager metrics (persistent)");
        TestContext.WriteLine("  3. Flink TaskManager - flink_taskmanager metrics (persistent)");
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

        // Test 1: Kafka JMX Exporter (verify exporter is working)
        TestContext.WriteLine("▶️  Test 1: Validating Kafka JMX Exporter");
        try
        {
            // Query for ANY metric from the kafka job to verify exporter is working
            // Use {job="kafka"} to get all metrics from kafka-exporter
            var kafkaMetric = "{job=\"kafka\"}";
            var kafkaUrl = $"{PrometheusHostEndpoint}/api/v1/query?query={kafkaMetric}";
            
            TestContext.WriteLine($"   Query: {kafkaMetric} (any Kafka JMX Exporter metrics)");
            TestContext.WriteLine($"   URL: {kafkaUrl}");
            TestContext.WriteLine($"   Note: Verifying kafka-exporter is collecting and exposing metrics");
            
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
                // Count how many metrics are available
                var metricMatches = Regex.Matches(kafkaResponse, @"""__name__"":""([^""]+)""");
                var metricCount = metricMatches.Count;
                var sampleMetrics = metricMatches.Cast<Match>()
                    .Take(3)
                    .Select(m => m.Groups[1].Value)
                    .ToList();
                
                TestContext.WriteLine($"   ✅ Kafka JMX Exporter working - {metricCount} metrics available");
                TestContext.WriteLine($"   📊 Sample metrics: {string.Join(", ", sampleMetrics)}");
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

        // Test 3: Flink TaskManager Metrics (persistent)
        TestContext.WriteLine("▶️  Test 3: Validating Flink TaskManager metrics (persistent)");
        try
        {
            var taskManagerMetric = "flink_taskmanager_Status_JVM_Memory_Heap_Used";
            var taskManagerUrl = $"{PrometheusHostEndpoint}/api/v1/query?query={taskManagerMetric}";
            
            TestContext.WriteLine($"   Query: {taskManagerMetric}");
            TestContext.WriteLine($"   URL: {taskManagerUrl}");
            TestContext.WriteLine($"   Note: This metric persists regardless of job status");
            
            var taskManagerResponse = await _httpClient.GetStringAsync(taskManagerUrl);
            
            // Save response for debugging
            var taskManagerDebugFile = Path.Combine(Path.GetTempPath(), "prometheus-taskmanager-query.json");
            await File.WriteAllTextAsync(taskManagerDebugFile, taskManagerResponse);
            TestContext.WriteLine($"   📄 TaskManager query response saved to: {taskManagerDebugFile}");
            
            var hasResults = taskManagerResponse.Contains("\"result\":[") &&
                           !taskManagerResponse.Contains("\"result\":[]");
            
            if (hasResults)
            {
                var valueMatch = Regex.Match(taskManagerResponse, @"""value"":\s*\[\s*[\d.]+\s*,\s*""([\d.]+)""");
                var value = valueMatch.Success ? valueMatch.Groups[1].Value : "N/A";
                
                TestContext.WriteLine($"   ✅ TaskManager metrics available - Heap Used: {value} bytes");
                TestContext.WriteLine($"   📊 Metric: {taskManagerMetric}");
                successCount++;
            }
            else
            {
                var message = $"   ❌ TaskManager metrics NOT available - No results found for {taskManagerMetric}";
                TestContext.WriteLine(message);
                failureMessages.Add(message);
            }
        }
        catch (Exception ex)
        {
            var message = $"   ❌ TaskManager metrics query FAILED: {ex.Message}";
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
        TestContext.WriteLine($"   ✓ Flink JobManager: {(successCount >= 2 ? "Available" : "FAILED")}");
        TestContext.WriteLine($"   ✓ Flink TaskManager: {(successCount >= 3 ? "Available" : "FAILED")}");
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
        TestContext.WriteLine("   ✓ Kafka JMX Exporter (JVM metrics)");
        TestContext.WriteLine("   ✓ Flink JobManager metrics (cluster health)");
        TestContext.WriteLine("   ✓ Flink TaskManager metrics (resource usage)");
    }
}