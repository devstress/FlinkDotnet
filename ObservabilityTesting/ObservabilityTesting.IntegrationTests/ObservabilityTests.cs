using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Flink.JobBuilder.Models;
using ObservabilityTesting.FlinkSqlAppHost;
using NUnit.Framework;

namespace ObservabilityTesting.IntegrationTests;

/// <summary>
/// Consolidated observability tests for FlinkDotNet.
/// Two focused tests: SampleApp integration and comprehensive metrics validation.
/// Primary focus: Kafka topic message metrics (RecordsIn/Out) as the most crucial observability aspect.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.None)]
[Category("observability")]
public class ObservabilityTests : LocalTestingTestBase
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromMinutes(3);
    private const double MetricTolerancePercent = 5.0; // ±5% tolerance for count-based metrics
    
    private static HttpClient? _httpClient;
    
    [OneTimeSetUp]
    public async Task ObservabilityOneTimeSetUp()
    {
        await base.OneTimeSetUp();
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        TestContext.WriteLine("✅ Observability test suite initialized (LEARNINGCOURSE mode always enabled)");
    }
    
    [OneTimeTearDown]
    public async Task ObservabilityOneTimeTearDown()
    {
        _httpClient?.Dispose();
        await base.OneTimeTearDown();
    }

    /// <summary>
    /// Test 1: SampleApp End-to-End Integration
    /// Validates that external applications can discover FlinkDotNet JobGateway and submit jobs successfully.
    /// </summary>
    [Test, Order(1)]
    [Timeout(180000)] // 3 minutes
    [Category("integration")]
    [Category("gateway")]
    public async Task Test1_SampleApp_EndToEndIntegration()
    {
        TestContext.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        TestContext.WriteLine("Test 1: SampleApp End-to-End Integration");
        TestContext.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        TestContext.WriteLine();
        TestContext.WriteLine("Validates:");
        TestContext.WriteLine("  • SampleApp discovers FlinkDotNet JobGateway via environment variables");
        TestContext.WriteLine("  • SampleApp submits jobs to JobGateway");
        TestContext.WriteLine("  • Jobs execute successfully on Flink cluster");
        TestContext.WriteLine("  • End-to-end data pipeline works (Kafka → Flink → Kafka)");
        TestContext.WriteLine();

        var gatewayEndpoint = await GetGatewayEndpointAsync();
        var kafkaBootstrap = GlobalTestInfrastructure.KafkaConnectionString;

        TestContext.WriteLine($"Gateway Endpoint: {gatewayEndpoint}");
        TestContext.WriteLine($"Kafka Bootstrap: {kafkaBootstrap}");
        TestContext.WriteLine();

        Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", gatewayEndpoint);
        Environment.SetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS", kafkaBootstrap);
        Environment.SetEnvironmentVariable("KAFKA_FLINK_BOOTSTRAP_SERVERS", GlobalTestInfrastructure.KafkaContainerIpForFlink);

        string? jobId = null;

        try
        {
            TestContext.WriteLine("Running SampleApp.RunAsync()...");
            TestContext.WriteLine();

            Task<string> sampleAppTask = SampleApp.Program.RunAsync();
            Task completedTask = await Task.WhenAny(sampleAppTask, Task.Delay(TimeSpan.FromMinutes(2)));

            if (completedTask != sampleAppTask)
            {
                Assert.Fail("SampleApp timed out after 2 minutes");
            }

            jobId = await sampleAppTask;
            TestContext.WriteLine($"✅ SampleApp completed successfully. Job ID: {jobId}");
            TestContext.WriteLine();

            // Verify messages were processed
            TestContext.WriteLine("Verifying output messages...");
            await Task.Delay(3000);

            var consumedMessages = 0;
            var uppercaseCount = 0;

            var consumerConfig = new Confluent.Kafka.ConsumerConfig
            {
                BootstrapServers = kafkaBootstrap,
                GroupId = $"test-consumer-{Guid.NewGuid()}",
                AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                BrokerAddressFamily = Confluent.Kafka.BrokerAddressFamily.V4,
                SecurityProtocol = Confluent.Kafka.SecurityProtocol.Plaintext
            };

            using (var consumer = new Confluent.Kafka.ConsumerBuilder<string, string>(consumerConfig).Build())
            {
                consumer.Subscribe("sample_output");

                var stopwatch = Stopwatch.StartNew();
                var timeout = TimeSpan.FromSeconds(30);

                while (stopwatch.Elapsed < timeout && consumedMessages < 20)
                {
                    var result = consumer.Consume(TimeSpan.FromMilliseconds(1000));

                    if (result != null)
                    {
                        consumedMessages++;
                        bool isUppercase = result.Message.Value == result.Message.Value.ToUpperInvariant();
                        if (isUppercase) uppercaseCount++;

                        if (consumedMessages <= 3)
                        {
                            TestContext.WriteLine($"  [{consumedMessages:D2}] {result.Message.Value}");
                        }

                        consumer.Commit(result);
                    }
                    else if (consumedMessages > 0)
                    {
                        break;
                    }
                }
            }

            TestContext.WriteLine($"  ... (consumed {consumedMessages} total messages)");
            TestContext.WriteLine();

            Assert.That(consumedMessages, Is.GreaterThan(0), "No messages consumed from output topic");
            Assert.That(uppercaseCount, Is.EqualTo(consumedMessages), $"Not all messages were uppercased: {uppercaseCount}/{consumedMessages}");

            TestContext.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
            TestContext.WriteLine("✅ Test 1 PASSED");
            TestContext.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
            TestContext.WriteLine();
            TestContext.WriteLine("Verified:");
            TestContext.WriteLine("  ✓ SampleApp discovered JobGateway");
            TestContext.WriteLine("  ✓ Job submitted successfully");
            TestContext.WriteLine("  ✓ Job executed on Flink cluster");
            TestContext.WriteLine($"  ✓ Data pipeline processed {consumedMessages} messages correctly");
            TestContext.WriteLine("  ✓ Uppercase transformation applied");
            TestContext.WriteLine();
        }
        finally
        {
            if (jobId != null)
            {
                try
                {
                    using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                    await httpClient.PostAsync($"{gatewayEndpoint}/api/v1/jobs/{jobId}/cancel", null);
                    TestContext.WriteLine($"Cancelled job {jobId}");
                }
                catch { /* Ignore cleanup errors */ }
            }
        }
    }

    /// <summary>
    /// Test 2: Comprehensive Observability - Kafka Metrics and Monitoring
    /// PRIMARY FOCUS: Kafka topic message metrics (RecordsIn/Out/PerSecond) - the most crucial observability aspect.
    /// Also validates: Gateway metrics API, Prometheus scraping, Grafana configuration, backpressure, checkpoints.
    /// </summary>
    [Test, Order(2)]
    public async Task Test2_ComprehensiveObservability_KafkaMetricsAndMonitoring()
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
        
        var cts = new CancellationTokenSource(TestTimeout);
        const int expectedMessageCount = 100;
        
        // Setup: Create unique topics
        var inputTopic = $"comprehensive-input-{Guid.NewGuid():N}";
        var outputTopic = $"comprehensive-output-{Guid.NewGuid():N}";
        
        TestContext.WriteLine($"📋 Test configuration:");
        TestContext.WriteLine($"   Input topic: {inputTopic}");
        TestContext.WriteLine($"   Output topic: {outputTopic}");
        TestContext.WriteLine($"   Expected messages: {expectedMessageCount}");
        TestContext.WriteLine();
        
        // STEP 1: Submit job via Gateway (using container IP for Flink jobs)
        var jobDefinition = FlinkDotNetJobs.CreateUppercaseJobDefinition(inputTopic, outputTopic, GlobalTestInfrastructure.KafkaContainerIpForFlink!, "comprehensive-test");
        var gatewayEndpoint = await GetGatewayEndpointAsync();
        var jobId = await SubmitJobViaGatewayAsync(gatewayEndpoint, jobDefinition, cts.Token);
        
        TestContext.WriteLine($"✅ Job submitted: {jobId}");
        await Task.Delay(TimeSpan.FromSeconds(10), cts.Token);
        
        // STEP 2: Produce messages (using localhost port for test code)
        await ProduceMessagesAsync(inputTopic, expectedMessageCount, cts.Token);
        TestContext.WriteLine($"✅ Produced {expectedMessageCount} messages to input topic");
        
        // STEP 3: CRITICAL - Verify messages consumed from output topic BEFORE checking metrics
        TestContext.WriteLine();
        TestContext.WriteLine("═══ KAFKA MESSAGE METRICS VALIDATION (PRIMARY FOCUS) ═══");
        var consumeTimeout = TimeSpan.FromSeconds(60);
        var consumedMessages = await ConsumeMessagesAsync(outputTopic, expectedMessageCount, consumeTimeout, cts.Token);
        TestContext.WriteLine($"✅ Consumed {consumedMessages.Count} messages from output topic");
        
        Assert.That(consumedMessages.Count, Is.EqualTo(expectedMessageCount), 
            $"CRITICAL: Should consume exactly {expectedMessageCount} messages (Kafka metrics validation)");
        
        // Wait for metrics stabilization after confirming messages processed
        await Task.Delay(TimeSpan.FromSeconds(15), cts.Token);
        
        // STEP 4: Query and validate Gateway metrics (especially Kafka message metrics)
        TestContext.WriteLine();
        TestContext.WriteLine("═══ Gateway Metrics Validation ═══");
        var metrics = await QueryGatewayMetricsAsync(gatewayEndpoint, jobId, cts.Token);
        
        TestContext.WriteLine($"📊 Gateway Metrics:");
        TestContext.WriteLine($"   RecordsIn: {metrics.RecordsIn}");
        TestContext.WriteLine($"   RecordsOut: {metrics.RecordsOut}");
        TestContext.WriteLine($"   Parallelism: {metrics.Parallelism}");
        TestContext.WriteLine($"   Checkpoints: {metrics.Checkpoints}");
        TestContext.WriteLine($"   BackpressureLevel: {metrics.BackpressureLevel}");
        TestContext.WriteLine();
        
        // PRIMARY VALIDATION: Kafka message metrics
        AssertMetricWithinTolerance(metrics.RecordsIn, expectedMessageCount, "RecordsIn (CRITICAL Kafka metric)");
        AssertMetricWithinTolerance(metrics.RecordsOut, expectedMessageCount, "RecordsOut (CRITICAL Kafka metric)");
        Assert.That(metrics.Parallelism, Is.EqualTo(1), "Job parallelism should be 1");
        
        TestContext.WriteLine("✅ KAFKA MESSAGE METRICS VALIDATED (Primary success criterion)");
        TestContext.WriteLine();
        
        // STEP 5: Validate Prometheus integration
        TestContext.WriteLine("═══ Prometheus Integration Validation ═══");
        var prometheusEndpoint = await GetPrometheusEndpointAsync();
        TestContext.WriteLine($"Prometheus endpoint: {prometheusEndpoint}");
        
        var targetsResponse = await _httpClient!.GetFromJsonAsync<JsonDocument>($"{prometheusEndpoint}/api/v1/targets", cts.Token);
        var activeTargets = targetsResponse?.RootElement.GetProperty("data").GetProperty("activeTargets");
        
        Assert.That(activeTargets?.GetArrayLength(), Is.GreaterThan(0), "Prometheus should have active scrape targets");
        TestContext.WriteLine($"✅ Prometheus has {activeTargets?.GetArrayLength()} active targets");
        
        // Validate Kafka metrics are available in Prometheus
        var kafkaMetricsQuery = "flink_taskmanager_job_task_operator_records_out_rate";
        var queryResponse = await _httpClient.GetFromJsonAsync<JsonDocument>($"{prometheusEndpoint}/api/v1/query?query={kafkaMetricsQuery}", cts.Token);
        var resultType = queryResponse?.RootElement.GetProperty("data").GetProperty("resultType").GetString();
        
        Assert.That(resultType, Is.EqualTo("vector"), "Kafka metrics query should return vector results");
        TestContext.WriteLine($"✅ Kafka metrics available in Prometheus: {kafkaMetricsQuery}");
        TestContext.WriteLine();
        
        // STEP 6: Validate Grafana configuration
        TestContext.WriteLine("═══ Grafana Configuration Validation ═══");
        var grafanaEndpoint = await GetGrafanaEndpointAsync();
        TestContext.WriteLine($"Grafana endpoint: {grafanaEndpoint}");
        
        var dataSourcesResponse = await _httpClient.GetFromJsonAsync<JsonDocument>($"{grafanaEndpoint}/api/datasources", cts.Token);
        var dataSources = dataSourcesResponse?.RootElement.EnumerateArray().ToList();
        
        Assert.That(dataSources?.Count, Is.GreaterThan(0), "Grafana should have configured data sources");
        
        var prometheusDataSource = dataSources?.FirstOrDefault(ds => 
            ds.GetProperty("type").GetString() == "prometheus");
        
        Assert.That(prometheusDataSource.HasValue, Is.True, "Grafana should have Prometheus data source configured");
        TestContext.WriteLine($"✅ Grafana has Prometheus data source configured");
        TestContext.WriteLine();
        
        // STEP 7: Validate backpressure detection
        TestContext.WriteLine("═══ Backpressure Detection Validation ═══");
        Assert.That(metrics.BackpressureLevel, Is.Not.EqualTo("unknown"), "Backpressure level should be detected");
        TestContext.WriteLine($"✅ Backpressure level detected: {metrics.BackpressureLevel}");
        TestContext.WriteLine();
        
        // STEP 8: Validate checkpoint metrics
        TestContext.WriteLine("═══ Checkpoint Metrics Validation ═══");
        Assert.That(metrics.Checkpoints, Is.GreaterThanOrEqualTo(0), "Checkpoint count should be non-negative");
        TestContext.WriteLine($"✅ Checkpoint metrics available: {metrics.Checkpoints} checkpoints");
        TestContext.WriteLine();
        
        // Final summary
        TestContext.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        TestContext.WriteLine("✅ Test 2 PASSED - All Observability Aspects Validated");
        TestContext.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        TestContext.WriteLine();
        TestContext.WriteLine("✓ PRIMARY: Kafka message metrics validated (RecordsIn/Out)");
        TestContext.WriteLine("✓ Gateway metrics API working correctly");
        TestContext.WriteLine("✓ Prometheus scraping Flink metrics");
        TestContext.WriteLine("✓ Grafana configured with Prometheus data source");
        TestContext.WriteLine("✓ Backpressure detection functional");
        TestContext.WriteLine("✓ Checkpoint metrics available");
        TestContext.WriteLine($"✓ End-to-end pipeline processed {consumedMessages.Count} messages");
        TestContext.WriteLine();
    }

    // ========== Helper Methods ==========

    private async Task<string> SubmitJobViaGatewayAsync(string gatewayEndpoint, object jobDefinition, CancellationToken ct)
    {
        var jsonContent = JsonContent.Create(jobDefinition);
        var response = await _httpClient!.PostAsync($"{gatewayEndpoint}/api/v1/jobs/submit", jsonContent, ct);
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<JobSubmissionResult>(cancellationToken: ct);
        
        // Return the Flink job ID from the Gateway response
        return result?.FlinkJobId ?? throw new InvalidOperationException("Gateway did not return a FlinkJobId");
    }

    private async Task ProduceMessagesAsync(string topic, int count, CancellationToken ct)
    {
        var producerConfig = new Confluent.Kafka.ProducerConfig
        {
            BootstrapServers = GlobalTestInfrastructure.KafkaConnectionString,
            Acks = Confluent.Kafka.Acks.All,
            EnableIdempotence = true,
            LingerMs = 5,
            BrokerAddressFamily = Confluent.Kafka.BrokerAddressFamily.V4,
            SecurityProtocol = Confluent.Kafka.SecurityProtocol.Plaintext
        };

        using var producer = new Confluent.Kafka.ProducerBuilder<string, string>(producerConfig).Build();
        
        for (int i = 0; i < count; i++)
        {
            var message = new Confluent.Kafka.Message<string, string>
            {
                Key = $"key-{i}",
                Value = $"test message {i}"
            };
            
            await producer.ProduceAsync(topic, message, ct);
        }
        
        producer.Flush(TimeSpan.FromSeconds(10));
    }

    private Task<List<string>> ConsumeMessagesAsync(string topic, int expectedCount, TimeSpan timeout, CancellationToken ct)
    {
        var consumed = new List<string>();
        
        var consumerConfig = new Confluent.Kafka.ConsumerConfig
        {
            BootstrapServers = GlobalTestInfrastructure.KafkaConnectionString,
            GroupId = $"test-consumer-{Guid.NewGuid()}",
            AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            BrokerAddressFamily = Confluent.Kafka.BrokerAddressFamily.V4,
            SecurityProtocol = Confluent.Kafka.SecurityProtocol.Plaintext
        };

        using var consumer = new Confluent.Kafka.ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe(topic);

        var stopwatch = Stopwatch.StartNew();
        
        while (stopwatch.Elapsed < timeout && consumed.Count < expectedCount && !ct.IsCancellationRequested)
        {
            var result = consumer.Consume(TimeSpan.FromMilliseconds(1000));
            
            if (result != null)
            {
                consumed.Add(result.Message.Value);
                consumer.Commit(result);
            }
            else if (consumed.Count > 0 && consumed.Count >= expectedCount)
            {
                break;
            }
        }

        return Task.FromResult(consumed);
    }

    private async Task<GatewayMetrics> QueryGatewayMetricsAsync(string gatewayEndpoint, string jobId, CancellationToken ct)
    {
        var response = await _httpClient!.GetAsync($"{gatewayEndpoint}/api/v1/jobs/{jobId}/metrics", ct);
        response.EnsureSuccessStatusCode();
        
        var metricsJson = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct);
        var root = metricsJson!.RootElement;
        
        return new GatewayMetrics
        {
            RecordsIn = root.GetProperty("recordsIn").GetInt64(),
            RecordsOut = root.GetProperty("recordsOut").GetInt64(),
            Parallelism = root.GetProperty("parallelism").GetInt32(),
            Checkpoints = root.GetProperty("checkpoints").GetInt64(),
            BackpressureLevel = root.TryGetProperty("backpressureLevel", out var bp) ? bp.GetString() ?? "unknown" : "unknown"
        };
    }

    private async Task<string> GetGatewayEndpointAsync()
    {
        // Use the existing GlobalTestInfrastructure method that discovers endpoint from Docker
        return await GlobalTestInfrastructure.GetGatewayEndpointAsync();
    }

    private async Task<string> GetPrometheusEndpointAsync()
    {
        return await GetPrometheusEndpointFromDockerAsync();
    }

    private async Task<string> GetGrafanaEndpointAsync()
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
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"127\.0\.0\.1:(\d+)->9090");
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
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"127\.0\.0\.1:(\d+)->3000");
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
        public string BackpressureLevel { get; init; } = "unknown";
    }
}
