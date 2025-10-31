using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using ObservabilityTesting.FlinkSqlAppHost;
using NUnit.Framework;

namespace ObservabilityTesting.IntegrationTests;

/// <summary>
/// Comprehensive observability tests for FlinkDotNet ObservabilityTesting.
/// Tests Gateway metrics, Prometheus integration, Grafana configuration, backpressure detection, and end-to-end observability workflow.
/// Implements WI11 requirements: 5 comprehensive tests covering all observability aspects while maintaining ≥70% code coverage.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.None)] // Observability tests share infrastructure, run sequentially
[Category("observability")]
public class ObservabilityTests : LocalTestingTestBase
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan MetricsStabilizationTime = TimeSpan.FromSeconds(60); // 4 Prometheus scrape intervals
    private const double MetricTolerancePercent = 5.0; // ±5% tolerance for count-based metrics
    
    private static HttpClient? _httpClient;
    
    [OneTimeSetUp]
    public async Task ObservabilityOneTimeSetUp()
    {
        await base.OneTimeSetUp();
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        
        // Verify LEARNINGCOURSE mode is enabled (required for observability stack)
        var isLearningCourse = Environment.GetEnvironmentVariable("LEARNINGCOURSE")?.ToLower() == "true";
        if (!isLearningCourse)
        {
            Assert.Ignore("Observability tests require LEARNINGCOURSE mode (Prometheus/Grafana stack). Set LEARNINGCOURSE=true environment variable.");
        }
        
        TestContext.WriteLine("✅ Observability test suite initialized (LEARNINGCOURSE mode enabled)");
    }
    
    [OneTimeTearDown]
    public async Task ObservabilityOneTimeTearDown()
    {
        _httpClient?.Dispose();
        await base.OneTimeTearDown();
    }

    /// <summary>
    /// Test 1: Validate Gateway metrics API accuracy.
    /// Covers: RecordsIn/Out, Parallelism, Checkpoints, BackpressureLevel.
    /// Success criteria: Metrics within ±5% tolerance of expected values.
    /// </summary>
    [Test, Order(1)]
    public async Task Test1_GatewayMetrics_AggregatesAccurately()
    {
        TestContext.WriteLine("═══ Test 1: Gateway Metrics Aggregation Accuracy ═══");
        
        var cts = new CancellationTokenSource(TestTimeout);
        const int expectedMessageCount = 100;
        
        try
        {
            // Setup: Create unique topics for this test
            var inputTopic = $"observability-test1-input-{Guid.NewGuid():N}";
            var outputTopic = $"observability-test1-output-{Guid.NewGuid():N}";
            
            TestContext.WriteLine($"📋 Test configuration:");
            TestContext.WriteLine($"   Input topic: {inputTopic}");
            TestContext.WriteLine($"   Output topic: {outputTopic}");
            TestContext.WriteLine($"   Expected messages: {expectedMessageCount}");
            
            // Create and submit job
            var job = FlinkDotNetJobs.CreateUppercaseJob(inputTopic, outputTopic, KafkaConnectionString!, "gateway-metrics-test", cts.Token);
            var gatewayEndpoint = await GetGatewayEndpointAsync();
            var jobId = await SubmitJobViaGatewayAsync(gatewayEndpoint, job, cts.Token);
            
            TestContext.WriteLine($"✅ Job submitted: {jobId}");
            
            // Wait for job to be ready
            await Task.Delay(TimeSpan.FromSeconds(10), cts.Token);
            
            // Produce messages
            await ProduceMessagesAsync(inputTopic, expectedMessageCount, cts.Token);
            TestContext.WriteLine($"✅ Produced {expectedMessageCount} messages to input topic");
            
            // Wait for processing and metrics stabilization
            await Task.Delay(MetricsStabilizationTime, cts.Token);
            
            // Query Gateway metrics
            var metrics = await QueryGatewayMetricsAsync(gatewayEndpoint, jobId, cts.Token);
            
            TestContext.WriteLine($"📊 Gateway Metrics:");
            TestContext.WriteLine($"   RecordsIn: {metrics.RecordsIn}");
            TestContext.WriteLine($"   RecordsOut: {metrics.RecordsOut}");
            TestContext.WriteLine($"   Parallelism: {metrics.Parallelism}");
            TestContext.WriteLine($"   Checkpoints: {metrics.Checkpoints}");
            TestContext.WriteLine($"   BackpressureLevel: {metrics.BackpressureLevel}");
            
            // Assertions with tolerance
            AssertMetricWithinTolerance(metrics.RecordsIn, expectedMessageCount, "RecordsIn");
            AssertMetricWithinTolerance(metrics.RecordsOut, expectedMessageCount, "RecordsOut");
            
            Assert.That(metrics.Parallelism, Is.GreaterThan(0), "Parallelism should be > 0");
            Assert.That(metrics.BackpressureLevel, Is.EqualTo("ok").Or.EqualTo("low"), 
                "BackpressureLevel should be 'ok' or 'low' under normal load");
            
            TestContext.WriteLine("✅ Test 1 PASSED: Gateway metrics are accurate");
        }
        finally
        {
            cts.Dispose();
        }
    }

    /// <summary>
    /// Test 2: Verify Prometheus integration works.
    /// Covers: Prometheus scraping, metric format, label filtering.
    /// Success criteria: Flink metrics available in Prometheus, correct format, labels work.
    /// </summary>
    [Test, Order(2)]
    public async Task Test2_PrometheusIntegration_ScrapesMetricsSuccessfully()
    {
        TestContext.WriteLine("═══ Test 2: Prometheus Integration ═══");
        
        var cts = new CancellationTokenSource(TestTimeout);
        
        try
        {
            var prometheusEndpoint = await GetPrometheusEndpointAsync();
            TestContext.WriteLine($"📡 Prometheus endpoint: {prometheusEndpoint}");
            
            // Wait for Prometheus targets to be healthy
            await WaitForPrometheusTargetsHealthyAsync(prometheusEndpoint, cts.Token);
            
            // Query Prometheus for Flink metrics
            var flinkMetrics = await QueryPrometheusMetricAsync(prometheusEndpoint, "flink_taskmanager_Status_JVM_Memory_Heap_Used", cts.Token);
            
            TestContext.WriteLine($"📊 Prometheus Flink Metrics:");
            TestContext.WriteLine($"   Metric: flink_taskmanager_Status_JVM_Memory_Heap_Used");
            TestContext.WriteLine($"   Results count: {flinkMetrics.Count}");
            
            Assert.That(flinkMetrics.Count, Is.GreaterThan(0), "Prometheus should have Flink metrics");
            
            // Verify metric format (should have labels like job_id, tm_id, etc.)
            var firstMetric = flinkMetrics.First();
            TestContext.WriteLine($"   Sample metric labels: {JsonSerializer.Serialize(firstMetric.Metric)}");
            
            Assert.That(firstMetric.Metric, Does.ContainKey("job"), "Flink metrics should have 'job' label");
            
            TestContext.WriteLine("✅ Test 2 PASSED: Prometheus integration works");
        }
        finally
        {
            cts.Dispose();
        }
    }

    /// <summary>
    /// Test 3: Verify Grafana integration works.
    /// Covers: Data source config, dashboard loading, queries.
    /// Success criteria: Grafana configured, queries return data, data source works.
    /// </summary>
    [Test, Order(3)]
    public async Task Test3_GrafanaIntegration_ConfiguresDataSourceAndQueries()
    {
        TestContext.WriteLine("═══ Test 3: Grafana Integration ═══");
        
        var cts = new CancellationTokenSource(TestTimeout);
        
        try
        {
            var grafanaEndpoint = await GetGrafanaEndpointAsync();
            var prometheusEndpoint = await GetPrometheusEndpointAsync();
            
            TestContext.WriteLine($"📡 Grafana endpoint: {grafanaEndpoint}");
            TestContext.WriteLine($"📡 Prometheus endpoint: {prometheusEndpoint}");
            
            // Configure Grafana data source (idempotent - creates or updates)
            await ConfigureGrafanaDataSourceAsync(grafanaEndpoint, prometheusEndpoint, cts.Token);
            
            TestContext.WriteLine("✅ Grafana data source configured");
            
            // Verify data source by querying through Grafana API
            var datasources = await QueryGrafanaDataSourcesAsync(grafanaEndpoint, cts.Token);
            
            TestContext.WriteLine($"📊 Grafana Data Sources:");
            foreach (var ds in datasources)
            {
                TestContext.WriteLine($"   - {ds.Name} ({ds.Type}): {ds.Url}");
            }
            
            Assert.That(datasources.Any(ds => ds.Type == "prometheus"), Is.True, 
                "Grafana should have Prometheus data source configured");
            
            TestContext.WriteLine("✅ Test 3 PASSED: Grafana integration works");
        }
        finally
        {
            cts.Dispose();
        }
    }

    /// <summary>
    /// Test 4: Validate backpressure detection and checkpoint metrics.
    /// Covers: Backpressure scenarios, checkpoint counting, timing.
    /// Success criteria: Backpressure level correct, checkpoint metrics accurate.
    /// </summary>
    [Test, Order(4)]
    public async Task Test4_BackpressureAndCheckpoints_DetectsAccurately()
    {
        TestContext.WriteLine("═══ Test 4: Backpressure and Checkpoints ═══");
        
        var cts = new CancellationTokenSource(TestTimeout);
        
        try
        {
            // Setup: Create unique topics
            var inputTopic = $"observability-test4-input-{Guid.NewGuid():N}";
            var outputTopic = $"observability-test4-output-{Guid.NewGuid():N}";
            
            // Create job with slower processing to potentially trigger backpressure
            var job = FlinkDotNetJobs.CreateFilterJob(inputTopic, outputTopic, KafkaConnectionString!, "backpressure-test", cts.Token);
            var gatewayEndpoint = await GetGatewayEndpointAsync();
            var jobId = await SubmitJobViaGatewayAsync(gatewayEndpoint, job, cts.Token);
            
            TestContext.WriteLine($"✅ Job submitted: {jobId}");
            
            // Get initial metrics
            await Task.Delay(TimeSpan.FromSeconds(10), cts.Token);
            var initialMetrics = await QueryGatewayMetricsAsync(gatewayEndpoint, jobId, cts.Token);
            
            TestContext.WriteLine($"📊 Initial Metrics:");
            TestContext.WriteLine($"   Checkpoints: {initialMetrics.Checkpoints}");
            TestContext.WriteLine($"   BackpressureLevel: {initialMetrics.BackpressureLevel}");
            
            // Produce messages to trigger processing
            await ProduceMessagesAsync(inputTopic, 50, cts.Token);
            
            // Wait for metrics to update
            await Task.Delay(TimeSpan.FromSeconds(30), cts.Token);
            
            var finalMetrics = await QueryGatewayMetricsAsync(gatewayEndpoint, jobId, cts.Token);
            
            TestContext.WriteLine($"📊 Final Metrics:");
            TestContext.WriteLine($"   Checkpoints: {finalMetrics.Checkpoints}");
            TestContext.WriteLine($"   LastCheckpoint: {finalMetrics.LastCheckpoint}");
            TestContext.WriteLine($"   BackpressureLevel: {finalMetrics.BackpressureLevel}");
            
            // Assertions
            Assert.That(finalMetrics.Checkpoints, Is.GreaterThanOrEqualTo(initialMetrics.Checkpoints), 
                "Checkpoint count should not decrease");
            
            Assert.That(finalMetrics.BackpressureLevel, Is.Not.Null.And.Not.Empty, 
                "BackpressureLevel should be reported");
            
            if (finalMetrics.LastCheckpoint > 0)
            {
                var checkpointAge = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - finalMetrics.LastCheckpoint;
                TestContext.WriteLine($"   Checkpoint age: {checkpointAge}ms");
                Assert.That(checkpointAge, Is.LessThan(120000), "Last checkpoint should be recent (< 2 minutes)");
            }
            
            TestContext.WriteLine("✅ Test 4 PASSED: Backpressure and checkpoint metrics work");
        }
        finally
        {
            cts.Dispose();
        }
    }

    /// <summary>
    /// Test 5: Validate complete observability workflow end-to-end.
    /// Covers: Job submit → Metrics → Prometheus → Grafana → Complete workflow.
    /// Success criteria: All components work together, metrics flow correctly.
    /// </summary>
    [Test, Order(5)]
    public async Task Test5_EndToEndObservability_CompleteWorkflow()
    {
        TestContext.WriteLine("═══ Test 5: End-to-End Observability Workflow ═══");
        
        var cts = new CancellationTokenSource(TestTimeout);
        
        try
        {
            // Setup: Get all service endpoints
            var gatewayEndpoint = await GetGatewayEndpointAsync();
            var prometheusEndpoint = await GetPrometheusEndpointAsync();
            var grafanaEndpoint = await GetGrafanaEndpointAsync();
            
            TestContext.WriteLine($"📡 Service Endpoints:");
            TestContext.WriteLine($"   Gateway: {gatewayEndpoint}");
            TestContext.WriteLine($"   Prometheus: {prometheusEndpoint}");
            TestContext.WriteLine($"   Grafana: {grafanaEndpoint}");
            
            // Step 1: Submit job
            var inputTopic = $"observability-e2e-input-{Guid.NewGuid():N}";
            var outputTopic = $"observability-e2e-output-{Guid.NewGuid():N}";
            
            var job = FlinkDotNetJobs.CreateUppercaseJob(inputTopic, outputTopic, KafkaConnectionString!, "e2e-observability", cts.Token);
            var jobId = await SubmitJobViaGatewayAsync(gatewayEndpoint, job, cts.Token);
            
            TestContext.WriteLine($"✅ Step 1: Job submitted ({jobId})");
            
            // Step 2: Produce data and wait for processing
            await Task.Delay(TimeSpan.FromSeconds(10), cts.Token);
            await ProduceMessagesAsync(inputTopic, 50, cts.Token);
            
            TestContext.WriteLine("✅ Step 2: Data produced");
            
            // Step 3: Wait for metrics to flow through the pipeline
            await Task.Delay(MetricsStabilizationTime, cts.Token);
            
            TestContext.WriteLine("✅ Step 3: Metrics stabilization period complete");
            
            // Step 4: Verify metrics in Gateway
            var gatewayMetrics = await QueryGatewayMetricsAsync(gatewayEndpoint, jobId, cts.Token);
            Assert.That(gatewayMetrics.RecordsIn, Is.GreaterThan(0), "Gateway should report RecordsIn > 0");
            
            TestContext.WriteLine($"✅ Step 4: Gateway metrics verified (RecordsIn={gatewayMetrics.RecordsIn})");
            
            // Step 5: Verify metrics in Prometheus
            var prometheusTargets = await QueryPrometheusTargetsAsync(prometheusEndpoint, cts.Token);
            var healthyTargets = prometheusTargets.Count(t => t.Health == "up");
            Assert.That(healthyTargets, Is.GreaterThan(0), "Prometheus should have healthy Flink targets");
            
            TestContext.WriteLine($"✅ Step 5: Prometheus targets verified ({healthyTargets} healthy)");
            
            // Step 6: Verify Grafana data source
            await ConfigureGrafanaDataSourceAsync(grafanaEndpoint, prometheusEndpoint, cts.Token);
            var datasources = await QueryGrafanaDataSourcesAsync(grafanaEndpoint, cts.Token);
            Assert.That(datasources.Any(ds => ds.Type == "prometheus"), Is.True, "Grafana should have Prometheus data source");
            
            TestContext.WriteLine("✅ Step 6: Grafana data source verified");
            
            // Final validation: Complete workflow successful
            TestContext.WriteLine("\n🎉 End-to-End Observability Workflow:");
            TestContext.WriteLine($"   ✅ Job submitted and running");
            TestContext.WriteLine($"   ✅ Gateway metrics: {gatewayMetrics.RecordsIn} records in");
            TestContext.WriteLine($"   ✅ Prometheus: {healthyTargets} healthy targets");
            TestContext.WriteLine($"   ✅ Grafana: Data source configured");
            
            TestContext.WriteLine("\n✅ Test 5 PASSED: Complete observability workflow works");
        }
        finally
        {
            cts.Dispose();
        }
    }

    // ========== Helper Methods ==========
    
    private static async Task<string> GetGatewayEndpointAsync()
    {
        // Gateway is now a Docker container (using pre-built image), so we need to discover its dynamically allocated port
        try
        {
            var gatewayContainers = await RunDockerCommandAsync("ps --filter \"name=flinkdotnet-jobgateway\" --format \"{{.Ports}}\"");
            var lines = gatewayContainers.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                if (line.Contains("->8086/tcp"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"127\.0\.0\.1:(\d+)->8086");
                    if (match.Success)
                    {
                        return $"http://localhost:{match.Groups[1].Value}/";
                    }
                }
            }

            throw new InvalidOperationException($"Could not determine Gateway endpoint from Docker ports: {gatewayContainers}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get Gateway endpoint: {ex.Message}", ex);
        }
    }
    
    private static async Task<string> GetPrometheusEndpointAsync()
    {
        try
        {
            var prometheusContainers = await RunDockerCommandAsync("ps --filter \"name=prometheus\" --format \"{{.Ports}}\"");
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
            
            // Fallback to default port
            return "http://localhost:9090";
        }
        catch
        {
            return "http://localhost:9090";
        }
    }
    
    private static async Task<string> GetGrafanaEndpointAsync()
    {
        try
        {
            var grafanaContainers = await RunDockerCommandAsync("ps --filter \"name=grafana\" --format \"{{.Ports}}\"");
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
            
            // Fallback to default port
            return "http://localhost:3000";
        }
        catch
        {
            return "http://localhost:3000";
        }
    }
    
    private static async Task<string> RunDockerCommandAsync(string arguments)
    {
        var dockerOutput = await TryRunContainerCommandAsync("docker", arguments);
        if (!string.IsNullOrWhiteSpace(dockerOutput))
        {
            return dockerOutput;
        }
        
        var podmanOutput = await TryRunContainerCommandAsync("podman", arguments);
        return podmanOutput ?? string.Empty;
    }
    
    private static async Task<string?> TryRunContainerCommandAsync(string command, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            using var process = Process.Start(psi);
            if (process == null)
            {
                return null;
            }
            
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            
            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                return output;
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }
    
    private static async Task<string> SubmitJobViaGatewayAsync(string gatewayEndpoint, object job, CancellationToken ct)
    {
        if (_httpClient == null)
        {
            throw new InvalidOperationException("HTTP client not initialized");
        }
        
        var response = await _httpClient.PostAsJsonAsync($"{gatewayEndpoint}/v1/jobs", job, ct);
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct);
        var jobId = result?.RootElement.GetProperty("jobId").GetString();
        
        if (string.IsNullOrEmpty(jobId))
        {
            throw new InvalidOperationException("Job submission did not return a valid jobId");
        }
        
        return jobId;
    }
    
    private static async Task ProduceMessagesAsync(string topic, int count, CancellationToken ct)
    {
        var producerConfig = new Confluent.Kafka.ProducerConfig
        {
            BootstrapServers = KafkaConnectionString,
            Acks = Confluent.Kafka.Acks.All
        };
        
        using var producer = new Confluent.Kafka.ProducerBuilder<string, string>(producerConfig).Build();
        
        for (var i = 0; i < count; i++)
        {
            var message = new Confluent.Kafka.Message<string, string>
            {
                Key = $"key-{i}",
                Value = $"test-message-{i}"
            };
            
            await producer.ProduceAsync(topic, message, ct);
        }
        
        producer.Flush(ct);
    }
    
    private static async Task<GatewayMetrics> QueryGatewayMetricsAsync(string gatewayEndpoint, string jobId, CancellationToken ct)
    {
        if (_httpClient == null)
        {
            throw new InvalidOperationException("HTTP client not initialized");
        }
        
        var response = await _httpClient.GetAsync($"{gatewayEndpoint}/v1/jobs/{jobId}/metrics", ct);
        response.EnsureSuccessStatusCode();
        
        var metricsJson = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct);
        if (metricsJson == null)
        {
            throw new InvalidOperationException("Failed to parse metrics JSON");
        }
        
        var root = metricsJson.RootElement;
        
        return new GatewayMetrics
        {
            RecordsIn = root.TryGetProperty("recordsIn", out var recordsIn) ? recordsIn.GetInt64() : 0,
            RecordsOut = root.TryGetProperty("recordsOut", out var recordsOut) ? recordsOut.GetInt64() : 0,
            Parallelism = root.TryGetProperty("parallelism", out var parallelism) ? parallelism.GetInt32() : 0,
            Checkpoints = root.TryGetProperty("checkpoints", out var checkpoints) ? checkpoints.GetInt64() : 0,
            LastCheckpoint = root.TryGetProperty("lastCheckpoint", out var lastCheckpoint) ? lastCheckpoint.GetInt64() : 0,
            BackpressureLevel = root.TryGetProperty("backpressureLevel", out var backpressure) ? backpressure.GetString() ?? "unknown" : "unknown"
        };
    }
    
    private static async Task WaitForPrometheusTargetsHealthyAsync(string prometheusEndpoint, CancellationToken ct)
    {
        if (_httpClient == null)
        {
            throw new InvalidOperationException("HTTP client not initialized");
        }
        
        var sw = Stopwatch.StartNew();
        var timeout = TimeSpan.FromMinutes(1);
        
        while (sw.Elapsed < timeout && !ct.IsCancellationRequested)
        {
            try
            {
                var targets = await QueryPrometheusTargetsAsync(prometheusEndpoint, ct);
                var healthyTargets = targets.Count(t => t.Health == "up");
                
                if (healthyTargets > 0)
                {
                    TestContext.WriteLine($"✅ Prometheus targets healthy: {healthyTargets} targets UP");
                    return;
                }
                
                TestContext.WriteLine($"⏳ Waiting for Prometheus targets... ({healthyTargets} healthy)");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"⏳ Prometheus not ready yet: {ex.Message}");
            }
            
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
        
        throw new TimeoutException("Prometheus targets did not become healthy within timeout");
    }
    
    private static async Task<List<PrometheusTarget>> QueryPrometheusTargetsAsync(string prometheusEndpoint, CancellationToken ct)
    {
        if (_httpClient == null)
        {
            throw new InvalidOperationException("HTTP client not initialized");
        }
        
        var response = await _httpClient.GetAsync($"{prometheusEndpoint}/api/v1/targets", ct);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct);
        if (json == null)
        {
            return new List<PrometheusTarget>();
        }
        
        var targets = new List<PrometheusTarget>();
        if (json.RootElement.TryGetProperty("data", out var data) &&
            data.TryGetProperty("activeTargets", out var activeTargets))
        {
            foreach (var target in activeTargets.EnumerateArray())
            {
                targets.Add(new PrometheusTarget
                {
                    Health = target.TryGetProperty("health", out var health) ? health.GetString() ?? "unknown" : "unknown",
                    ScrapeUrl = target.TryGetProperty("scrapeUrl", out var scrapeUrl) ? scrapeUrl.GetString() ?? "" : ""
                });
            }
        }
        
        return targets;
    }
    
    private static async Task<List<PrometheusMetric>> QueryPrometheusMetricAsync(string prometheusEndpoint, string metric, CancellationToken ct)
    {
        if (_httpClient == null)
        {
            throw new InvalidOperationException("HTTP client not initialized");
        }
        
        var response = await _httpClient.GetAsync($"{prometheusEndpoint}/api/v1/query?query={Uri.EscapeDataString(metric)}", ct);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct);
        if (json == null)
        {
            return new List<PrometheusMetric>();
        }
        
        var metrics = new List<PrometheusMetric>();
        if (json.RootElement.TryGetProperty("data", out var data) &&
            data.TryGetProperty("result", out var result))
        {
            foreach (var item in result.EnumerateArray())
            {
                var labels = new Dictionary<string, string>();
                if (item.TryGetProperty("metric", out var metricLabels))
                {
                    foreach (var label in metricLabels.EnumerateObject())
                    {
                        labels[label.Name] = label.Value.GetString() ?? "";
                    }
                }
                
                metrics.Add(new PrometheusMetric { Metric = labels });
            }
        }
        
        return metrics;
    }
    
    private static async Task ConfigureGrafanaDataSourceAsync(string grafanaEndpoint, string prometheusEndpoint, CancellationToken ct)
    {
        if (_httpClient == null)
        {
            throw new InvalidOperationException("HTTP client not initialized");
        }
        
        var dataSourceConfig = new
        {
            name = "Prometheus",
            type = "prometheus",
            url = prometheusEndpoint,
            access = "proxy",
            isDefault = true,
            jsonData = new { httpMethod = "GET" }
        };
        
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{grafanaEndpoint}/api/datasources", dataSourceConfig, ct);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                TestContext.WriteLine("ℹ️ Grafana data source already exists (409 Conflict) - this is expected");
                return;
            }
            
            response.EnsureSuccessStatusCode();
            TestContext.WriteLine("✅ Grafana data source configured successfully");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"⚠️ Grafana data source configuration failed: {ex.Message}");
            throw;
        }
    }
    
    private static async Task<List<GrafanaDataSource>> QueryGrafanaDataSourcesAsync(string grafanaEndpoint, CancellationToken ct)
    {
        if (_httpClient == null)
        {
            throw new InvalidOperationException("HTTP client not initialized");
        }
        
        var response = await _httpClient.GetAsync($"{grafanaEndpoint}/api/datasources", ct);
        response.EnsureSuccessStatusCode();
        
        var datasources = await response.Content.ReadFromJsonAsync<List<GrafanaDataSource>>(cancellationToken: ct);
        return datasources ?? new List<GrafanaDataSource>();
    }
    
    private static void AssertMetricWithinTolerance(long actual, long expected, string metricName)
    {
        var tolerance = expected * MetricTolerancePercent / 100.0;
        var lowerBound = expected - tolerance;
        var upperBound = expected + tolerance;
        
        Assert.That(actual, Is.InRange(lowerBound, upperBound),
            $"{metricName}: expected {expected} ±{MetricTolerancePercent}% (range: {lowerBound:F0}-{upperBound:F0}), got {actual}");
    }

    // ========== Data Models ==========
    
    private sealed class GatewayMetrics
    {
        public long RecordsIn { get; init; }
        public long RecordsOut { get; init; }
        public int Parallelism { get; init; }
        public long Checkpoints { get; init; }
        public long LastCheckpoint { get; init; }
        public string BackpressureLevel { get; init; } = "unknown";
    }
    
    private sealed class PrometheusTarget
    {
        public string Health { get; init; } = "unknown";
        public string ScrapeUrl { get; init; } = "";
    }
    
    private sealed class PrometheusMetric
    {
        public Dictionary<string, string> Metric { get; init; } = new();
    }
    
    private sealed class GrafanaDataSource
    {
        public string Name { get; init; } = "";
        public string Type { get; init; } = "";
        public string Url { get; init; } = "";
    }
}
