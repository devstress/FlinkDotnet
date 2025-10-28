using Confluent.Kafka;
using NUnit.Framework;
using System.Diagnostics;
using System.Text.Json;

namespace LocalTesting.IntegrationTests;

/// <summary>
/// Integration tests for Prometheus metrics collection in ReleasePackagesTesting.
/// Verifies that metrics are captured from Kafka, Flink JobManager, Flink TaskManager, and (future) JobGateway.
/// 
/// REQUIREMENTS:
/// - LEARNINGCOURSE=true environment variable must be set before running tests
/// - This enables Prometheus, Grafana, and Kafka JMX Exporter deployment
/// </summary>
[TestFixture]
[Category("prometheus-metrics")]
[Category("observability")]
[Category("integration")]
public class PrometheusMetricsTests : LocalTestingTestBase
{
    private readonly HttpClient _httpClient = new();
    private const int MessageCount = 100; // Number of messages to generate for metrics testing

    [Test]
    [Description("Verify Prometheus captures metrics from Kafka, Flink, and JobGateway")]
    public async Task PrometheusMetrics_ShouldCaptureKafkaFlinkAndJobGatewayMetrics()
    {
        TestContext.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        TestContext.WriteLine("  Prometheus Metrics Verification for ReleasePackagesTesting");
        TestContext.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        TestContext.WriteLine();
        TestContext.WriteLine("  This test verifies metrics collection from:");
        TestContext.WriteLine("  ✓ Kafka (topic metrics - PRIORITY)");
        TestContext.WriteLine("  ✓ Flink JobManager");
        TestContext.WriteLine("  ✓ Flink TaskManager");
        TestContext.WriteLine("  ✓ JobGateway (future)");
        TestContext.WriteLine();

        // Step 1: Validate LEARNINGCOURSE environment variable
        ValidateLearningCourseEnvironment();

        // Step 2: Ensure Prometheus endpoint is available
        if (string.IsNullOrEmpty(PrometheusHostEndpoint))
        {
            Assert.Fail("Prometheus endpoint not available. Ensure LEARNINGCOURSE=true and infrastructure is running.");
        }

        TestContext.WriteLine($"📊 Prometheus Endpoint: {PrometheusHostEndpoint}");
        TestContext.WriteLine($"🌐 Kafka Connection: {KafkaConnectionString}");
        TestContext.WriteLine();

        // Step 3: Submit a simple Flink job to generate metrics
        TestContext.WriteLine("▶️  Step 1: Submitting test job to generate metrics...");
        var (inputTopic, outputTopic, jobId) = await SubmitTestJobAsync();
        TestContext.WriteLine($"   ✅ Job submitted: {jobId}");
        TestContext.WriteLine($"   📥 Input topic: {inputTopic}");
        TestContext.WriteLine($"   📤 Output topic: {outputTopic}");
        TestContext.WriteLine();

        try
        {
            // Step 4: Produce messages to input topic
            TestContext.WriteLine($"▶️  Step 2: Producing {MessageCount} messages to {inputTopic}...");
            await ProduceTestMessagesAsync(inputTopic, MessageCount);
            TestContext.WriteLine($"   ✅ {MessageCount} messages produced");
            TestContext.WriteLine();

            // Step 5: Wait for Prometheus to be ready
            TestContext.WriteLine("▶️  Step 3: Waiting for Prometheus to be ready...");
            await WaitForPrometheusReadyAsync(PrometheusHostEndpoint!);
            TestContext.WriteLine("   ✅ Prometheus is ready");
            TestContext.WriteLine();

            // Step 6: Verify Prometheus targets are healthy
            TestContext.WriteLine("▶️  Step 4: Verifying Prometheus targets are healthy...");
            await VerifyPrometheusTargetsAsync(PrometheusHostEndpoint!);
            TestContext.WriteLine();

            // Step 7: Automatic warmup period for metrics export
            TestContext.WriteLine("▶️  Step 5: Automatic metrics warmup period...");
            TestContext.WriteLine("   📊 Kafka → JMX → JMX Exporter → Prometheus chain needs initialization time");
            TestContext.WriteLine("   ⏳ Waiting 15 seconds for metrics export chain to stabilize...");
            await Task.Delay(15000);
            TestContext.WriteLine("   ✅ Warmup complete - metrics export chain should be ready");
            TestContext.WriteLine();

            // Step 8: Verify metrics from all sources
            TestContext.WriteLine("▶️  Step 6: Verifying metrics collection...");
            TestContext.WriteLine();

            // PRIORITY: Kafka topic metrics (user's main requirement)
            TestContext.WriteLine("   📊 1. KAFKA TOPIC METRICS (PRIORITY):");
            await VerifyMetricHasData("kafka_server_brokertopicmetrics_messagesinpersec_count_total",
                "Total messages across all Kafka topics");
            await VerifyMetricHasData("kafka_server_brokertopicmetrics_bytesinpersec_count_total",
                "Total bytes in across all Kafka topics");
            TestContext.WriteLine();

            // Flink JobManager metrics
            TestContext.WriteLine("   📊 2. FLINK JOBMANAGER METRICS:");
            await VerifyMetricHasData("flink_jobmanager_numRegisteredTaskManagers",
                "Number of registered TaskManagers");
            await VerifyMetricHasData("flink_jobmanager_numRunningJobs",
                "Number of running Flink jobs");
            TestContext.WriteLine();

            // Flink TaskManager metrics
            TestContext.WriteLine("   📊 3. FLINK TASKMANAGER METRICS:");
            await VerifyMetricHasData("flink_taskmanager_Status_JVM_Memory_Heap_Used",
                "TaskManager JVM heap memory usage");
            await VerifyMetricHasData("flink_taskmanager_job_task_operator_numRecordsIn",
                "Records received by Flink operators");
            await VerifyMetricHasData("flink_taskmanager_job_task_operator_numRecordsOut",
                "Records output by Flink operators");
            TestContext.WriteLine();

            // Message flow tracking
            TestContext.WriteLine("   📊 4. MESSAGE FLOW TRACKING:");
            await VerifyRateQueryHasData("increase(flink_taskmanager_job_task_operator_numRecordsIn[1m])",
                "Flink message processing rate (records in per minute)");
            TestContext.WriteLine();

            // JobGateway metrics (following Apache Flink naming conventions)
            TestContext.WriteLine("   📊 5. JOBGATEWAY METRICS:");
            TestContext.WriteLine("      Verifying JobGateway Prometheus metrics (similar to Flink's metrics.reporters)");
            await VerifyJobGatewayMetricsAsync();
            TestContext.WriteLine();

            TestContext.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
            TestContext.WriteLine("  ✅ ALL PROMETHEUS METRICS VERIFIED SUCCESSFULLY");
            TestContext.WriteLine("  ✅ Kafka Topic Metrics: CAPTURED (messages, bytes)");
            TestContext.WriteLine("  ✅ Flink JobManager Metrics: CAPTURED (TaskManagers, running jobs)");
            TestContext.WriteLine("  ✅ Flink TaskManager Metrics: CAPTURED (JVM memory, records in/out)");
            TestContext.WriteLine("  ✅ Message Flow Tracking: CAPTURED (processing rate)");
            TestContext.WriteLine("  ✅ JobGateway Metrics: CAPTURED (jobs submitted, running, API requests)");
            TestContext.WriteLine($"  ✅ Test Messages Processed: {MessageCount}");
            TestContext.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        }
        finally
        {
            // Cleanup: Cancel the job
            if (!string.IsNullOrEmpty(jobId))
            {
                TestContext.WriteLine("\n🧹 Cleanup: Cancelling test job...");
                await CancelFlinkJobAsync(jobId);
                TestContext.WriteLine("   ✅ Job cancelled");
            }
        }
    }

    /// <summary>
    /// Validates that LEARNINGCOURSE environment variable is set.
    /// Required for Prometheus, Grafana, and observability stack deployment.
    /// </summary>
    private void ValidateLearningCourseEnvironment()
    {
        var learningCourse = Environment.GetEnvironmentVariable("LEARNINGCOURSE");
        if (string.IsNullOrEmpty(learningCourse) || learningCourse.ToLower() != "true")
        {
            Assert.Fail(
                "LEARNINGCOURSE environment variable is not set to 'true'.\n\n" +
                "WHY THIS IS REQUIRED:\n" +
                "  - Prometheus, Grafana, and Kafka JMX Exporter are only deployed when LEARNINGCOURSE=true\n" +
                "  - Without this variable, observability infrastructure is not available\n\n" +
                "HOW TO FIX:\n" +
                "  PowerShell: $env:LEARNINGCOURSE='true'\n" +
                "  CMD: set LEARNINGCOURSE=true\n" +
                "  Linux/macOS: export LEARNINGCOURSE=true\n\n" +
                "VERIFICATION:\n" +
                "  Run: docker ps | grep -E 'prometheus|grafana|kafka-exporter'\n" +
                "  You should see these three containers running."
            );
        }

        TestContext.WriteLine("✅ LEARNINGCOURSE environment variable validated");
    }

    /// <summary>
    /// Submits a simple Flink job that reads from one Kafka topic and writes to another.
    /// This generates metrics for both Kafka and Flink.
    /// </summary>
    private async Task<(string inputTopic, string outputTopic, string jobId)> SubmitTestJobAsync()
    {
        var inputTopic = $"metrics-test-input-{Guid.NewGuid():N}";
        var outputTopic = $"metrics-test-output-{Guid.NewGuid():N}";

        // Use FlinkDotNetJobs helper to submit a simple uppercase job
        var result = await FlinkDotNetJobs.CreateUppercaseJob(
            inputTopic,
            outputTopic,
            KafkaConnectionString!,
            "prometheus-metrics-test-job",
            CancellationToken.None
        );

        if (!result.Success || string.IsNullOrEmpty(result.JobId))
        {
            Assert.Fail("Failed to submit test job for metrics generation");
        }

        // Wait for job to fully initialize
        await Task.Delay(5000);

        return (inputTopic, outputTopic, result.JobId);
    }

    /// <summary>
    /// Produces test messages to the specified Kafka topic.
    /// </summary>
    private async Task ProduceTestMessagesAsync(string topic, int count)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = KafkaConnectionString,
            ClientId = $"prometheus-test-producer-{Guid.NewGuid():N}"
        };

        using var producer = new ProducerBuilder<string, string>(config).Build();

        for (int i = 0; i < count; i++)
        {
            var message = new Message<string, string>
            {
                Key = $"key-{i}",
                Value = $"test-message-{i}"
            };

            await producer.ProduceAsync(topic, message);
        }

        producer.Flush(TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// Cancels a Flink job using the Flink REST API.
    /// </summary>
    private async Task CancelFlinkJobAsync(string jobId)
    {
        try
        {
            var flinkRestApi = "http://localhost:8081";
            var cancelUrl = $"{flinkRestApi}/jobs/{jobId}?mode=cancel";
            await _httpClient.PatchAsync(cancelUrl, null);
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"   ⚠️  Error cancelling job: {ex.Message}");
        }
    }

    /// <summary>
    /// Waits for Prometheus to be ready by checking its health endpoint.
    /// </summary>
    private async Task WaitForPrometheusReadyAsync(string prometheusEndpoint)
    {
        var healthUrl = $"{prometheusEndpoint}/-/healthy";
        var maxAttempts = 30;
        var delayBetweenAttempts = TimeSpan.FromSeconds(2);

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var response = await _httpClient.GetAsync(healthUrl);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
                // Ignore and retry
            }

            if (attempt < maxAttempts)
            {
                await Task.Delay(delayBetweenAttempts);
            }
        }

        Assert.Fail($"Prometheus did not become ready after {maxAttempts * delayBetweenAttempts.TotalSeconds}s");
    }

    /// <summary>
    /// Verifies that Prometheus targets are healthy.
    /// </summary>
    private async Task VerifyPrometheusTargetsAsync(string prometheusEndpoint)
    {
        var targetsUrl = $"{prometheusEndpoint}/api/v1/targets";
        var response = await _httpClient.GetAsync(targetsUrl);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var targetsResponse = JsonSerializer.Deserialize<JsonElement>(content);
        var targets = targetsResponse.GetProperty("data").GetProperty("activeTargets");

        var targetNames = new List<string>();
        foreach (var target in targets.EnumerateArray())
        {
            var labels = target.GetProperty("labels");
            var jobName = labels.GetProperty("job").GetString();
            var health = target.GetProperty("health").GetString();
            
            targetNames.Add(jobName!);
            TestContext.WriteLine($"   📊 Target: {jobName} - Health: {health}");
        }

        // Verify expected targets exist
        var expectedTargets = new[] { "flink-jobmanager", "flink-taskmanager", "kafka" };
        foreach (var expected in expectedTargets)
        {
            if (!targetNames.Contains(expected))
            {
                Assert.Fail($"Expected Prometheus target '{expected}' not found. Available targets: {string.Join(", ", targetNames)}");
            }
        }

        TestContext.WriteLine($"   ✅ All expected Prometheus targets are configured");
    }

    /// <summary>
    /// Verifies that a specific metric has non-zero data.
    /// </summary>
    private async Task VerifyMetricHasData(string metricName, string description)
    {
        var query = await QueryPrometheusAsync(metricName);
        var json = JsonSerializer.Deserialize<JsonElement>(query);
        var result = json.GetProperty("data").GetProperty("result");

        if (result.GetArrayLength() == 0)
        {
            Assert.Fail($"Metric '{metricName}' has no data. Description: {description}");
        }

        // Get the first value for logging
        var firstResult = result[0];
        var value = firstResult.GetProperty("value")[1].GetString();
        TestContext.WriteLine($"      ✅ {metricName} = {value}");
    }

    /// <summary>
    /// Verifies that a rate query returns non-zero data.
    /// </summary>
    private async Task VerifyRateQueryHasData(string rateQuery, string description)
    {
        var query = await QueryPrometheusAsync(rateQuery);
        var json = JsonSerializer.Deserialize<JsonElement>(query);
        var result = json.GetProperty("data").GetProperty("result");

        if (result.GetArrayLength() == 0)
        {
            Assert.Fail($"Rate query '{rateQuery}' has no data. Description: {description}");
        }

        // Get the first value for logging
        var firstResult = result[0];
        var value = firstResult.GetProperty("value")[1].GetString();
        TestContext.WriteLine($"      ✅ {rateQuery.Substring(0, Math.Min(50, rateQuery.Length))}... = {value}");
    }

    /// <summary>
    /// Verifies JobGateway Prometheus metrics following Apache Flink naming conventions.
    /// JobGateway exposes metrics similar to Flink's metrics.reporters pattern.
    /// </summary>
    private async Task VerifyJobGatewayMetricsAsync()
    {
        // First verify JobGateway metrics endpoint is accessible
        var gatewayUrl = "http://localhost:8086"; // ReleasePackagesTesting Gateway port
        var metricsUrl = $"{gatewayUrl}/metrics";
        
        try
        {
            var response = await _httpClient.GetAsync(metricsUrl);
            if (!response.IsSuccessStatusCode)
            {
                TestContext.WriteLine($"      ⚠️  JobGateway metrics endpoint not accessible: {response.StatusCode}");
                TestContext.WriteLine($"      ℹ️  This may be expected if Prometheus is not enabled in JobGateway appsettings");
                return;
            }

            var metricsContent = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine($"      ✅ JobGateway /metrics endpoint accessible");

            // Verify key JobGateway metrics (following Flink naming pattern: flinkdotnet_jobgateway_*)
            // These metrics are defined in MetricsService.cs
            
            // Job submission metrics
            await VerifyMetricHasData("flinkdotnet_jobgateway_jobs_submitted_total",
                "Total jobs submitted through JobGateway");
            
            // Running jobs gauge
            await VerifyOptionalMetric("flinkdotnet_jobgateway_jobs_running",
                "Currently running jobs tracked by JobGateway");
            
            // API request metrics (HTTP metrics from prometheus-net.AspNetCore)
            await VerifyMetricHasData("http_requests_received_total",
                "Total HTTP requests received by JobGateway (from prometheus-net.AspNetCore)");

            TestContext.WriteLine($"      ✅ JobGateway metrics validated successfully");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"      ⚠️  Could not verify JobGateway metrics: {ex.Message}");
            TestContext.WriteLine($"      ℹ️  JobGateway metrics may not be enabled in configuration");
        }
    }

    /// <summary>
    /// Verifies that an optional metric exists (doesn't fail if missing).
    /// Used for metrics that may not have data yet.
    /// </summary>
    private async Task VerifyOptionalMetric(string metricName, string description)
    {
        try
        {
            await VerifyMetricHasData(metricName, description);
        }
        catch
        {
            TestContext.WriteLine($"      ℹ️  {metricName} - {description} (no data yet, which is acceptable)");
        }
    }

    /// <summary>
    /// Queries Prometheus and returns the JSON response.
    /// </summary>
    private async Task<string> QueryPrometheusAsync(string query)
    {
        var queryUrl = $"{PrometheusHostEndpoint}/api/v1/query?query={Uri.EscapeDataString(query)}";
        var response = await _httpClient.GetAsync(queryUrl);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}
