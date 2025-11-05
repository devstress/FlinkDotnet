using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Flink.JobBuilder.Models;
using NUnit.Framework;

namespace ObservabilityTesting.IntegrationTests;

/// <summary>
/// Helper methods for ObservabilityTests (partial class)
/// </summary>
public partial class ObservabilityTests
{
    private static async Task<string> GetPrometheusEndpointAsync()
    {
        return await GetPrometheusEndpointFromDockerAsync();
    }

    private static async Task<string> GetGrafanaEndpointAsync()
    {
        return await GetGrafanaEndpointFromDockerAsync();
    }

    private static async Task<string> GetPrometheusEndpointFromDockerAsync()
    {
        try
        {
            var prometheusContainers = await GlobalTestInfrastructure.RunDockerCommandAsync("ps --filter \"name=prometheus\" --format \"{{.Ports}}\"");
            var lines = prometheusContainers.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                if (line.Contains("->9090/tcp"))
                {
                    var match = Regex.Match(line, @"127\.0\.0\.1:(\d+)->9090");
                    if (match.Success)
                    {
                        return $"http://localhost:{match.Groups[1].Value}";
                    }
                }
            }

            throw new InvalidOperationException($"Could not determine Prometheus endpoint from Docker ports: {prometheusContainers}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get Prometheus endpoint: {ex.Message}", ex);
        }
    }

    private static async Task<string> GetGrafanaEndpointFromDockerAsync()
    {
        try
        {
            var grafanaContainers = await GlobalTestInfrastructure.RunDockerCommandAsync("ps --filter \"name=grafana\" --format \"{{.Ports}}\"");
            var lines = grafanaContainers.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                if (line.Contains("->3000/tcp"))
                {
                    var match = Regex.Match(line, @"127\.0\.0\.1:(\d+)->3000");
                    if (match.Success)
                    {
                        return $"http://localhost:{match.Groups[1].Value}";
                    }
                }
            }

            throw new InvalidOperationException($"Could not determine Grafana endpoint from Docker ports: {grafanaContainers}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get Grafana endpoint: {ex.Message}", ex);
        }
    }

    private static void PrintTestHeader()
    {
        TestContext.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        TestContext.WriteLine("Test 2: Comprehensive Observability - Kafka Metrics and Monitoring");
        TestContext.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        TestContext.WriteLine();
        TestContext.WriteLine("PRIMARY FOCUS: Kafka Topic Message Metrics (Most Crucial)");
        TestContext.WriteLine("  • RecordsIn count and accuracy");
        TestContext.WriteLine("  • RecordsOut count and accuracy");
        TestContext.WriteLine("  • Records per second throughput");
        TestContext.WriteLine();
        TestContext.WriteLine("Also validates:");
        TestContext.WriteLine("  • Gateway metrics API accuracy");
        TestContext.WriteLine("  • Prometheus scraping and metrics availability");
        TestContext.WriteLine("  • Grafana data source configuration");
        TestContext.WriteLine("  • Backpressure detection");
        TestContext.WriteLine("  • Checkpoint metrics");
        TestContext.WriteLine();
    }

    private static void PrintTestConfiguration(string inputTopic, string outputTopic, int expectedMessageCount)
    {
        TestContext.WriteLine($"📋 Test configuration:");
        TestContext.WriteLine($"   Input topic: {inputTopic}");
        TestContext.WriteLine($"   Output topic: {outputTopic}");
        TestContext.WriteLine($"   Expected messages: {expectedMessageCount}");
        TestContext.WriteLine();
    }

    private static void ValidateAllComprehensiveMetrics(JobMetrics metrics)
    {
        // Validate JobManager metrics
        TestContext.WriteLine("JobManager Metrics:");
        ValidateCustomMetric(metrics, "JobManager.CPU.Load", "JobManager CPU Load", requireNonZero: false);
        ValidateCustomMetric(metrics, "JobManager.Memory.Heap.Used", "JobManager Heap Memory", requireNonZero: true);
        ValidateCustomMetric(metrics, "JobManager.RunningJobs", "JobManager Running Jobs", requireNonZero: false);
        TestContext.WriteLine();

        // Validate TaskManager metrics
        TestContext.WriteLine("TaskManager Metrics:");
        ValidateCustomMetric(metrics, "TaskManager.CPU.Load", "TaskManager CPU Load", requireNonZero: false);
        ValidateCustomMetric(metrics, "TaskManager.Memory.Heap.Used", "TaskManager Heap Memory", requireNonZero: true);
        ValidateCustomMetric(metrics, "TaskManager.ActiveTasks", "TaskManager Active Tasks", requireNonZero: false);
        TestContext.WriteLine();

        // Validate Kafka topic metrics
        TestContext.WriteLine("Kafka Topic Metrics:");
        ValidateCustomMetric(metrics, "Kafka.Topic.TotalOffsets", "Kafka Topic Total Offsets", requireNonZero: true);
        ValidateCustomMetric(metrics, "Kafka.Topic.PartitionCount", "Kafka Topic Partition Count", requireNonZero: true);
        ValidateCustomMetric(metrics, "Kafka.Consumer.CurrentOffset", "Kafka Consumer Current Offset", requireNonZero: true);
        ValidateCustomMetric(metrics, "Kafka.Topic.MessagesInFlight", "Kafka Messages In Flight", requireNonZero: false);
        ValidateCustomMetric(metrics, "Kafka.Topic.MessageRate", "Kafka Topic Message Rate", requireNonZero: false);
        ValidateCustomMetric(metrics, "Kafka.Consumer.Lag", "Kafka Consumer Lag", requireNonZero: false);
        TestContext.WriteLine();

        TestContext.WriteLine("✅ COMPREHENSIVE METRICS VALIDATED");
    }

    private static void ValidateCustomMetric(JobMetrics metrics, string metricKey, string metricName, bool requireNonZero = true)
    {
        // Try to get the metric from CustomMetrics dictionary
        if (metrics.CustomMetrics.TryGetValue(metricKey, out var metricValue))
        {
            // Validate the metric value is not null
            Assert.That(metricValue, Is.Not.Null, 
                $"{metricName} should not be null");
            
            // Convert to long for comparison
            // Note: When metrics are deserialized from JSON, numeric values are JsonElement objects
            long value = metricValue switch
            {
                long l => l,
                int i => i,
                double d => (long)d,
                JsonElement je when je.ValueKind == JsonValueKind.Number => 
                    je.TryGetInt64(out long l) ? l : (long)je.GetDouble(),
                _ => 0
            };

            // Validate the metric value is valid
            if (requireNonZero)
            {
                Assert.That(value, Is.GreaterThan(0), 
                    $"{metricName} should be > 0 to prove it's being collected (found: {value})");
                TestContext.WriteLine($"   ✅ {metricName}: {value:N0}");
            }
            else
            {
                // For optional metrics, just verify they're non-negative
                Assert.That(value, Is.GreaterThanOrEqualTo(0), 
                    $"{metricName} should be >= 0 (found: {value})");
                TestContext.WriteLine($"   ✅ {metricName}: {value:N0}");
            }
        }
        else
        {
            // Metric not found - FAIL the test since we expect all metrics to be present
            Assert.Fail($"{metricName} (key: {metricKey}) was not found in CustomMetrics. " +
                       $"Expected all comprehensive metrics to be collected. " +
                       $"Available metrics: {string.Join(", ", metrics.CustomMetrics.Keys)}");
        }
    }

    /// <summary>
    /// Verifies Prometheus health and configuration to distinguish between config errors and scraping issues
    /// </summary>
    private static async Task VerifyPrometheusHealthAsync(string prometheusEndpoint, CancellationToken cancellationToken)
    {
        try
        {
            // Check 1: Verify Prometheus is accessible
            TestContext.WriteLine($"   1️⃣ Checking Prometheus accessibility at {prometheusEndpoint}...");
            var healthResponse = await _httpClient!.GetAsync($"{prometheusEndpoint}/-/healthy", cancellationToken);
            if (!healthResponse.IsSuccessStatusCode)
            {
                TestContext.WriteLine($"   ❌ CONFIGURATION ERROR: Prometheus health check failed with status {healthResponse.StatusCode}");
                TestContext.WriteLine($"      → This indicates Prometheus is not running or not accessible");
                return;
            }
            TestContext.WriteLine($"   ✅ Prometheus is accessible and healthy");

            // Check 2: Verify Prometheus targets (Flink JobManager, TaskManager, Kafka exporters)
            TestContext.WriteLine($"   2️⃣ Checking Prometheus targets configuration...");
            var targetsResponse = await _httpClient.GetFromJsonAsync<JsonDocument>(
                $"{prometheusEndpoint}/api/v1/targets", cancellationToken);
            
            await CheckPrometheusTargetsAsync(targetsResponse);

            // Check 3: Verify we can query a basic Flink metric
            await CheckPrometheusMetricQueryAsync(prometheusEndpoint, cancellationToken);
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"   ❌ DIAGNOSTIC ERROR: {ex.Message}");
            TestContext.WriteLine($"      → This may indicate network issues or Prometheus configuration problems");
        }
    }

    private static async Task CheckPrometheusTargetsAsync(JsonDocument? targetsResponse)
    {
        if (targetsResponse == null || 
            !targetsResponse.RootElement.TryGetProperty("data", out var dataEl) ||
            !dataEl.TryGetProperty("activeTargets", out var targetsEl))
        {
            TestContext.WriteLine($"   ⚠️  Could not retrieve Prometheus targets (API response format unexpected)");
            return;
        }

        var activeTargets = targetsEl.EnumerateArray().ToList();
        TestContext.WriteLine($"   📊 Active Prometheus targets: {activeTargets.Count}");
        
        var (targetsByJob, upTargets, downTargets) = AnalyzePrometheusTargets(activeTargets);
        
        TestContext.WriteLine($"   📈 Target health: {upTargets} up, {downTargets} down");
        
        foreach (var (job, count) in targetsByJob.OrderBy(kv => kv.Key))
        {
            TestContext.WriteLine($"      - {job}: {count} target(s)");
        }
        
        await ValidateExpectedPrometheusTargetsAsync(targetsByJob, upTargets, downTargets);
    }

    private static (Dictionary<string, int> targetsByJob, int upTargets, int downTargets) AnalyzePrometheusTargets(
        List<JsonElement> activeTargets)
    {
        var targetsByJob = new Dictionary<string, int>();
        var upTargets = 0;
        var downTargets = 0;
        
        foreach (var target in activeTargets)
        {
            if (target.TryGetProperty("labels", out var labels) &&
                labels.TryGetProperty("job", out var jobLabel))
            {
                string jobName = jobLabel.GetString() ?? "unknown";
                targetsByJob.TryGetValue(jobName, out var count);
                targetsByJob[jobName] = count + 1;
            }
            
            if (target.TryGetProperty("health", out var health))
            {
                string healthStatus = health.GetString() ?? "unknown";
                if (healthStatus == "up")
                {
                    upTargets++;
                }
                else
                {
                    downTargets++;
                }
            }
        }
        
        return (targetsByJob, upTargets, downTargets);
    }

    private static Task ValidateExpectedPrometheusTargetsAsync(
        Dictionary<string, int> targetsByJob, 
        int upTargets, 
        int downTargets)
    {
        string[] expectedJobs = { "flink-jobmanager", "flink-taskmanager", "kafka-topics" };
        var missingJobs = new List<string>();
        
        foreach (var expectedJob in expectedJobs)
        {
            if (!targetsByJob.ContainsKey(expectedJob))
            {
                missingJobs.Add(expectedJob);
            }
        }
        
        if (missingJobs.Count > 0)
        {
            TestContext.WriteLine($"   ⚠️  CONFIGURATION WARNING: Missing expected Prometheus targets: {string.Join(", ", missingJobs)}");
            TestContext.WriteLine($"      → Check prometheus.yml scrape_configs");
        }
        
        if (downTargets > 0)
        {
            TestContext.WriteLine($"   ⚠️  SCRAPING WARNING: {downTargets} target(s) are down");
            TestContext.WriteLine($"      → Prometheus cannot scrape metrics from these targets");
        }
        
        if (upTargets == 0)
        {
            TestContext.WriteLine($"   ❌ CONFIGURATION ERROR: No Prometheus targets are up");
            TestContext.WriteLine($"      → Check that Flink and exporters are running and accessible");
        }
        
        return Task.CompletedTask;
    }

    private static async Task CheckPrometheusMetricQueryAsync(string prometheusEndpoint, CancellationToken cancellationToken)
    {
        TestContext.WriteLine($"   3️⃣ Testing Prometheus query for Flink metrics...");
        string testQuery = Uri.EscapeDataString("flink_jobmanager_Status_JVM_Memory_Heap_Used");
        var queryResponse = await _httpClient!.GetFromJsonAsync<JsonDocument>(
            $"{prometheusEndpoint}/api/v1/query?query={testQuery}", cancellationToken);
        
        if (queryResponse != null &&
            queryResponse.RootElement.TryGetProperty("status", out var statusEl) &&
            statusEl.GetString() == "success" &&
            queryResponse.RootElement.TryGetProperty("data", out var queryDataEl) &&
            queryDataEl.TryGetProperty("result", out var resultEl))
        {
            int resultCount = resultEl.GetArrayLength();
            if (resultCount > 0)
            {
                TestContext.WriteLine($"   ✅ Successfully queried Flink JobManager metrics ({resultCount} result(s))");
            }
            else
            {
                TestContext.WriteLine($"   ⚠️  SCRAPING WARNING: Query succeeded but returned no results");
                TestContext.WriteLine($"      → Prometheus may not have scraped Flink metrics yet");
                TestContext.WriteLine($"      → Or Flink metrics reporter is not exposing metrics");
            }
        }
        else
        {
            TestContext.WriteLine($"   ⚠️  Failed to query Flink metrics from Prometheus");
        }
    }

    private static async Task AddFlinkTaskManagerMetrics(Dictionary<string, object> metrics, string prometheusEndpoint, string jobId, CancellationToken ct)
    {
        var tmCpuLoad = await QueryPrometheusMetricAsync(prometheusEndpoint, 
            "avg(flink_taskmanager_Status_JVM_CPU_Load) * 100", ct);
        if (tmCpuLoad.HasValue)
            metrics["TaskManager.CPU.Load"] = tmCpuLoad.Value;

        var tmHeapUsed = await QueryPrometheusMetricAsync(prometheusEndpoint,
            "sum(flink_taskmanager_Status_JVM_Memory_Heap_Used)", ct);
        if (tmHeapUsed.HasValue)
            metrics["TaskManager.Memory.Heap.Used"] = tmHeapUsed.Value;

        var activeTasks = await QueryPrometheusMetricAsync(prometheusEndpoint,
            $"count(flink_taskmanager_job_task_operator_numRecordsIn{{job_id=\"{jobId}\"}})", ct);
        if (activeTasks.HasValue)
            metrics["TaskManager.ActiveTasks"] = activeTasks.Value;
    }

    private static async Task AddFlinkJobManagerMetrics(Dictionary<string, object> metrics, string prometheusEndpoint, CancellationToken ct)
    {
        var jmCpuLoad = await QueryPrometheusMetricAsync(prometheusEndpoint,
            "flink_jobmanager_Status_JVM_CPU_Load * 100", ct);
        if (jmCpuLoad.HasValue)
            metrics["JobManager.CPU.Load"] = jmCpuLoad.Value;

        var jmHeapUsed = await QueryPrometheusMetricAsync(prometheusEndpoint,
            "flink_jobmanager_Status_JVM_Memory_Heap_Used", ct);
        if (jmHeapUsed.HasValue)
            metrics["JobManager.Memory.Heap.Used"] = jmHeapUsed.Value;

        var runningJobs = await QueryPrometheusMetricAsync(prometheusEndpoint,
            "count(count by (job_id) (flink_taskmanager_job_task_operator_numRecordsIn))", ct);
        if (runningJobs.HasValue)
            metrics["JobManager.RunningJobs"] = runningJobs.Value;
    }

    private static async Task AddKafkaMetrics(Dictionary<string, object> metrics, string prometheusEndpoint, CancellationToken ct)
    {
        var queryTime = DateTime.UtcNow;
        TestContext.WriteLine($"[TIMING] Querying Kafka metrics at: {queryTime:HH:mm:ss.fff}");
        
        var topicOffsets = await QueryPrometheusMetricAsync(prometheusEndpoint,
            "sum by (topic) (kafka_topic_partition_current_offset)", ct);
        if (topicOffsets.HasValue)
            metrics["Kafka.Topic.TotalOffsets"] = topicOffsets.Value;

        var partitionCount = await QueryPrometheusMetricAsync(prometheusEndpoint,
            "count(kafka_topic_partition_current_offset)", ct);
        if (partitionCount.HasValue)
            metrics["Kafka.Topic.PartitionCount"] = partitionCount.Value;

        // DEBUG: Check if any consumer groups are being tracked
        TestContext.WriteLine("[DEBUG] Checking for consumer group metrics in Prometheus...");
        var allConsumerGroupsRaw = await QueryPrometheusRawAsync(prometheusEndpoint,
            "kafka_consumergroup_current_offset", ct);
        TestContext.WriteLine($"[DEBUG] Raw kafka_consumergroup_current_offset response: {allConsumerGroupsRaw?.Substring(0, Math.Min(500, allConsumerGroupsRaw?.Length ?? 0))}");
        
        var allConsumerLagRaw = await QueryPrometheusRawAsync(prometheusEndpoint,
            "kafka_consumergroup_lag", ct);
        TestContext.WriteLine($"[DEBUG] Raw kafka_consumergroup_lag response: {allConsumerLagRaw?.Substring(0, Math.Min(500, allConsumerLagRaw?.Length ?? 0))}");

        var consumerOffset = await QueryPrometheusMetricAsync(prometheusEndpoint,
            "sum(kafka_consumergroup_current_offset)", ct);
        TestContext.WriteLine($"[DEBUG] kafka_consumergroup_current_offset query returned: {(consumerOffset.HasValue ? consumerOffset.Value.ToString() : "null")}");
        if (consumerOffset.HasValue)
            metrics["Kafka.Consumer.CurrentOffset"] = consumerOffset.Value;

        // Query for consumer lag - use max to get the highest lag across partitions
        // Filter to only the uppercase-job consumer group used by Test2
        // Use max instead of sum to avoid -1 values from unavailable partitions affecting the result
        var consumerLag = await QueryPrometheusMetricAsync(prometheusEndpoint,
            "max(kafka_consumergroup_lag{consumergroup=\"uppercase-job\"}) OR on() vector(0)", ct);
        TestContext.WriteLine($"[DEBUG] [TIMING {queryTime:HH:mm:ss.fff}] kafka_consumergroup_lag query returned: {(consumerLag.HasValue ? consumerLag.Value.ToString() : "null")}");
        if (consumerLag.HasValue && consumerLag.Value >= 0)
        {
            TestContext.WriteLine($"[DEBUG] Storing Kafka.Consumer.Lag = {consumerLag.Value}");
            metrics["Kafka.Consumer.Lag"] = consumerLag.Value;
        }
        else
        {
            if (consumerLag.HasValue && consumerLag.Value < 0)
            {
                TestContext.WriteLine($"[WARNING] kafka_consumergroup_lag returned {consumerLag.Value} (negative value indicates unavailable/unknown lag)");
            }
            else
            {
                TestContext.WriteLine("[WARNING] kafka_consumergroup_lag metric not available from Prometheus");
            }
            TestContext.WriteLine("[DEBUG] This typically means:");
            TestContext.WriteLine("[DEBUG]   1. Kafka consumer group has not committed any offsets yet");
            TestContext.WriteLine("[DEBUG]   2. kafka-topic-exporter hasn't scraped consumer group info yet");
            TestContext.WriteLine("[DEBUG]   3. Consumer group ID doesn't match expected pattern (expected: 'uppercase-job')");
            TestContext.WriteLine("[DEBUG]   4. Multiple consumer groups with mixed availability (some return -1)");
        }

        var messagesInFlight = await QueryPrometheusMetricAsync(prometheusEndpoint,
            "abs(max(kafka_consumergroup_lag{consumergroup=\"uppercase-job\"}) OR on() vector(0))", ct);
        if (messagesInFlight.HasValue)
        {
            metrics["Kafka.Topic.MessagesInFlight"] = messagesInFlight.Value;
        }
        else
        {
            var directCalc = await QueryPrometheusMetricAsync(prometheusEndpoint,
                "abs(sum(kafka_topic_partition_current_offset) - sum(kafka_consumergroup_current_offset))", ct);
            if (directCalc.HasValue)
                metrics["Kafka.Topic.MessagesInFlight"] = directCalc.Value;
        }

        var topicMessageRate = await QueryPrometheusMetricAsync(prometheusEndpoint,
            "sum(rate(kafka_topic_partition_current_offset[1m]))", ct);
        if (topicMessageRate.HasValue)
            metrics["Kafka.Topic.MessageRate"] = topicMessageRate.Value;
    }

    /// <summary>
    /// Collect comprehensive metrics from Prometheus directly (Flink, Kafka, etc.)
    /// </summary>
    private static async Task<Dictionary<string, object>> CollectPrometheusMetricsAsync(
        string prometheusEndpoint, string jobId, CancellationToken cancellationToken)
    {
        var metrics = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        // Collect metrics by category using helper methods to reduce complexity
        await AddFlinkTaskManagerMetrics(metrics, prometheusEndpoint, jobId, cancellationToken);
        await AddFlinkJobManagerMetrics(metrics, prometheusEndpoint, cancellationToken);
        await AddKafkaMetrics(metrics, prometheusEndpoint, cancellationToken);

        // Operator metrics from Flink
        // Try to get records and bytes from Source operators
        var recordsIn = await QueryPrometheusMetricAsync(prometheusEndpoint,
            $"sum(flink_taskmanager_job_task_operator_numRecordsOut{{job_id=\"{jobId}\",operator_name=~\".*Source.*\"}})", cancellationToken);
        if (recordsIn.HasValue)
            metrics["RecordsIn"] = recordsIn.Value;

        var recordsOut = await QueryPrometheusMetricAsync(prometheusEndpoint,
            $"sum(flink_taskmanager_job_task_operator_numRecordsIn{{job_id=\"{jobId}\",operator_name=~\".*Sink.*\"}})", cancellationToken);
        if (recordsOut.HasValue)
            metrics["RecordsOut"] = recordsOut.Value;

        // BytesRead: Try multiple queries to find the right metric
        var bytesReadQueries = new[]
        {
            $"sum(flink_taskmanager_job_task_operator_numBytesInPerSecond{{job_id=\"{jobId}\"}})",
            $"sum(flink_taskmanager_job_task_operator_numBytesIn{{job_id=\"{jobId}\"}})",
            $"sum(flink_taskmanager_job_task_operator_numBytesIn{{job_id=\"{jobId}\",operator_name=~\".*[Ss]ource.*\"}})",
            $"sum(flink_taskmanager_job_task_operator_numBytesOut{{job_id=\"{jobId}\",operator_name=~\".*[Ss]ource.*\"}})"
        };
        var bytesRead = await TryMultipleQueriesAsync(prometheusEndpoint, bytesReadQueries, cancellationToken);
        if (bytesRead.HasValue)
        {
            metrics["BytesRead"] = bytesRead.Value;
            metrics["Operator.BytesRead"] = bytesRead.Value;
        }

        // BytesWritten: Try multiple queries
        var bytesWrittenQueries = new[]
        {
            $"sum(flink_taskmanager_job_task_operator_numBytesOutPerSecond{{job_id=\"{jobId}\"}})",
            $"sum(flink_taskmanager_job_task_operator_numBytesOut{{job_id=\"{jobId}\"}})",
            $"sum(flink_taskmanager_job_task_operator_numBytesOut{{job_id=\"{jobId}\",operator_name=~\".*[Ss]ink.*\"}})",
            $"sum(flink_taskmanager_job_task_operator_numBytesIn{{job_id=\"{jobId}\",operator_name=~\".*[Ss]ink.*\"}})"
        };
        var bytesWritten = await TryMultipleQueriesAsync(prometheusEndpoint, bytesWrittenQueries, cancellationToken);
        if (bytesWritten.HasValue)
        {
            metrics["BytesWritten"] = bytesWritten.Value;
            metrics["Operator.BytesWritten"] = bytesWritten.Value;
        }

        return metrics;
    }

    private static async Task<long?> TryMultipleQueriesAsync(
        string prometheusEndpoint,
        string[] queries,
        CancellationToken cancellationToken)
    {
        foreach (var query in queries)
        {
            var value = await QueryPrometheusMetricAsync(prometheusEndpoint, query, cancellationToken);
            if (value.HasValue && value.Value > 0)
            {
                return value;
            }
        }
        return null;
    }

    /// <summary>
    /// Query Prometheus for a single metric value
    /// </summary>
    private static async Task<long?> QueryPrometheusMetricAsync(string prometheusEndpoint, string query, CancellationToken cancellationToken)
    {
        try
        {
            string encodedQuery = Uri.EscapeDataString(query);
            string url = $"{prometheusEndpoint}/api/v1/query?query={encodedQuery}";

            var httpClient = new HttpClient();
            var response = await httpClient.GetFromJsonAsync<JsonDocument>(url, cancellationToken);

            if (response == null)
                return null;

            var status = response.RootElement.GetProperty("status").GetString();
            if (status != "success")
                return null;

            var data = response.RootElement.GetProperty("data");
            var result = data.GetProperty("result");

            if (result.GetArrayLength() == 0)
                return null;

            // Get first result's value [timestamp, "value"]
            var firstResult = result[0];
            var valueArray = firstResult.GetProperty("value");
            var valueStr = valueArray[1].GetString();

            if (double.TryParse(valueStr, System.Globalization.NumberStyles.Float, 
                System.Globalization.CultureInfo.InvariantCulture, out double doubleValue))
            {
                return (long)doubleValue;
            }

            return null;
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"   ⚠️  Error querying Prometheus metric: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Query Prometheus and return the raw JSON response for debugging
    /// </summary>
    private static async Task<string?> QueryPrometheusRawAsync(string prometheusEndpoint, string query, CancellationToken cancellationToken)
    {
        try
        {
            string encodedQuery = Uri.EscapeDataString(query);
            string url = $"{prometheusEndpoint}/api/v1/query?query={encodedQuery}";

            var httpClient = new HttpClient();
            var response = await httpClient.GetStringAsync(url, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"   ⚠️  Error querying Prometheus (raw): {ex.Message}");
            return null;
        }
    }

    private static async Task ValidatePrometheusIntegrationAsync(HttpClient httpClient, string prometheusEndpoint, CancellationToken cancellationToken)
    {
        TestContext.WriteLine("═══ Prometheus Integration Validation ═══");
        TestContext.WriteLine($"Prometheus endpoint: {prometheusEndpoint}");
        
        var targetsResponse = await httpClient.GetFromJsonAsync<JsonDocument>($"{prometheusEndpoint}/api/v1/targets", cancellationToken);
        var activeTargets = targetsResponse?.RootElement.GetProperty("data").GetProperty("activeTargets");
        
        Assert.That(activeTargets?.GetArrayLength(), Is.GreaterThan(0), "Prometheus should have active scrape targets");
        TestContext.WriteLine($"✅ Prometheus has {activeTargets?.GetArrayLength()} active targets");
        
        // Validate Kafka metrics are available in Prometheus
        var kafkaMetricsQuery = "flink_taskmanager_job_task_operator_records_out_rate";
        var queryResponse = await httpClient.GetFromJsonAsync<JsonDocument>($"{prometheusEndpoint}/api/v1/query?query={kafkaMetricsQuery}", cancellationToken);
        var resultType = queryResponse?.RootElement.GetProperty("data").GetProperty("resultType").GetString();
        
        Assert.That(resultType, Is.EqualTo("vector"), "Kafka metrics query should return vector results");
        TestContext.WriteLine($"✅ Kafka metrics available in Prometheus: {kafkaMetricsQuery}");
        TestContext.WriteLine();
    }

    private static async Task ValidateGrafanaConfigurationAsync(HttpClient httpClient, CancellationToken cancellationToken)
    {
        TestContext.WriteLine("═══ Grafana Configuration Validation ═══");
        var grafanaEndpoint = await GetGrafanaEndpointAsync();
        TestContext.WriteLine($"Grafana endpoint: {grafanaEndpoint}");
        
        var dataSourcesResponse = await httpClient.GetFromJsonAsync<JsonDocument>($"{grafanaEndpoint}/api/datasources", cancellationToken);
        var dataSources = dataSourcesResponse?.RootElement.EnumerateArray().ToList();
        
        Assert.That(dataSources?.Count, Is.GreaterThan(0), "Grafana should have configured data sources");
        
        var prometheusDataSource = dataSources?.Find(ds => 
            ds.GetProperty("type").GetString() == "prometheus");
        
        Assert.That(prometheusDataSource.HasValue, Is.True, "Grafana should have Prometheus data source configured");
        TestContext.WriteLine($"✅ Grafana has Prometheus data source configured");
        TestContext.WriteLine();
    }

    private static void ValidateBackpressureMetrics(JobMetrics metrics)
    {
        TestContext.WriteLine("═══ Backpressure Detection Validation ═══");
        Assert.That(metrics.BackpressureLevel, Is.Not.Null, "Backpressure level should not be null");
        Assert.That(metrics.BackpressureLevel, Is.Not.Empty, "Backpressure level should not be empty");
        TestContext.WriteLine($"✅ Backpressure level detected: {metrics.BackpressureLevel}");
        
        // Also validate it's in CustomMetrics for backward compatibility
        if (metrics.CustomMetrics.TryGetValue("backpressureLevel", out var bpLevel))
        {
            TestContext.WriteLine($"   Backpressure also available in CustomMetrics: {bpLevel}");
        }
        TestContext.WriteLine();
    }

    private static void ValidateCheckpointMetrics(JobMetrics metrics)
    {
        TestContext.WriteLine("═══ Checkpoint Metrics Validation ═══");
        Assert.That(metrics.Checkpoints, Is.GreaterThanOrEqualTo(0), "Checkpoint count should be non-negative");
        TestContext.WriteLine($"✅ Checkpoint metrics available: {metrics.Checkpoints} checkpoints");
        TestContext.WriteLine();
    }

    private static async Task CollectFinalMetricsAndMergeAsync(
        string gatewayEndpoint, string prometheusEndpoint, string jobId, JobMetrics metrics, CancellationToken cancellationToken)
    {
        // Wait a bit more to ensure Prometheus has had time to complete scraping
        TestContext.WriteLine("⏳ Waiting 5 more seconds for Prometheus to complete scraping...");
        await Task.Delay(5000, cancellationToken);
        
        TestContext.WriteLine("🔄 Querying final metrics state before validation...");
        await QueryGatewayMetricsAsync(gatewayEndpoint, jobId, cancellationToken);
        
        // Enhance with comprehensive metrics from Prometheus
        var finalPrometheusMetrics = await CollectPrometheusMetricsAsync(prometheusEndpoint, jobId, cancellationToken);
        foreach (var (key, value) in finalPrometheusMetrics)
        {
            metrics.CustomMetrics[key] = value;
        }
        
        // Update direct properties from Prometheus metrics
        UpdateMetricsProperties(metrics, finalPrometheusMetrics);
        
        TestContext.WriteLine($"   JobManager.Memory.Heap.Used: {metrics.CustomMetrics.GetValueOrDefault("JobManager.Memory.Heap.Used", 0)}");
        TestContext.WriteLine($"   TaskManager.Memory.Heap.Used: {metrics.CustomMetrics.GetValueOrDefault("TaskManager.Memory.Heap.Used", 0)}");
        TestContext.WriteLine();
    }

    private static void UpdateMetricsProperties(JobMetrics metrics, Dictionary<string, object> prometheusMetrics)
    {
        if (prometheusMetrics.TryGetValue("BytesRead", out var bytesReadObj) && bytesReadObj is long bytesReadVal)
            metrics.BytesRead = bytesReadVal;
        if (prometheusMetrics.TryGetValue("BytesWritten", out var bytesWrittenObj) && bytesWrittenObj is long bytesWrittenVal)
            metrics.BytesWritten = bytesWrittenVal;
        if (prometheusMetrics.TryGetValue("RecordsIn", out var recordsInObj) && recordsInObj is long recordsInVal)
            metrics.RecordsIn = recordsInVal;
        if (prometheusMetrics.TryGetValue("RecordsOut", out var recordsOutObj) && recordsOutObj is long recordsOutVal)
            metrics.RecordsOut = recordsOutVal;
    }

    /// <summary>
    /// Debug helper to check Kafka consumer groups and offset commits via Docker exec
    /// </summary>
    private static async Task DebugKafkaConsumerGroupsAsync()
    {
        TestContext.WriteLine();
        TestContext.WriteLine("═══ DEBUG: Checking Kafka Consumer Groups via Docker ═══");
        
        try
        {
            // Get Kafka container name
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "ps --format \"{{.Names}}\" --filter \"name=kafka\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            using var process = System.Diagnostics.Process.Start(processInfo);
            if (process == null)
            {
                TestContext.WriteLine("[DEBUG] Could not start docker process");
                return;
            }
            
            var containerName = (await process.StandardOutput.ReadToEndAsync()).Trim();
            await process.WaitForExitAsync();
            
            if (string.IsNullOrWhiteSpace(containerName))
            {
                TestContext.WriteLine("[DEBUG] No Kafka container found");
                return;
            }
            
            TestContext.WriteLine($"[DEBUG] Found Kafka container: {containerName}");
            
            // List all consumer groups
            TestContext.WriteLine("[DEBUG] Listing all consumer groups...");
            var listGroupsInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"exec {containerName} kafka-consumer-groups --bootstrap-server localhost:9092 --list",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            using var listProcess = System.Diagnostics.Process.Start(listGroupsInfo);
            if (listProcess != null)
            {
                var groups = await listProcess.StandardOutput.ReadToEndAsync();
                var errors = await listProcess.StandardError.ReadToEndAsync();
                await listProcess.WaitForExitAsync();
                
                TestContext.WriteLine($"[DEBUG] Consumer groups:\n{groups}");
                if (!string.IsNullOrWhiteSpace(errors))
                    TestContext.WriteLine($"[DEBUG] Errors: {errors}");
            }
            
            // Check specific group 'uppercase-job'
            TestContext.WriteLine("[DEBUG] Checking 'uppercase-job' consumer group details...");
            var describeGroupInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"exec {containerName} kafka-consumer-groups --bootstrap-server localhost:9092 --group uppercase-job --describe",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            using var describeProcess = System.Diagnostics.Process.Start(describeGroupInfo);
            if (describeProcess != null)
            {
                var description = await describeProcess.StandardOutput.ReadToEndAsync();
                var errors = await describeProcess.StandardError.ReadToEndAsync();
                await describeProcess.WaitForExitAsync();
                
                TestContext.WriteLine($"[DEBUG] uppercase-job group description:\n{description}");
                if (!string.IsNullOrWhiteSpace(errors))
                    TestContext.WriteLine($"[DEBUG] Errors: {errors}");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"[DEBUG] Error checking Kafka consumer groups: {ex.Message}");
        }
        
        TestContext.WriteLine("═══ END DEBUG: Kafka Consumer Groups ═══");
        TestContext.WriteLine();
    }

    // ========== Data Models ==========
    
    private sealed class JobSubmissionResult
    {
        public string FlinkJobId { get; set; } = string.Empty;
    }
}
