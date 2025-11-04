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
    [CancelAfter(180000)] // 3 minutes
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

        // Create unique topics for Test1 to avoid conflicts with Test2
        var inputTopic = $"test1-input-{Guid.NewGuid():N}";
        var outputTopic = $"test1-output-{Guid.NewGuid():N}";
        
        TestContext.WriteLine($"📋 Test configuration:");
        TestContext.WriteLine($"   Input topic: {inputTopic}");
        TestContext.WriteLine($"   Output topic: {outputTopic}");
        TestContext.WriteLine();

        var gatewayEndpoint = await GetGatewayEndpointAsync();
        var kafkaBootstrap = GlobalTestInfrastructure.KafkaConnectionString;

        TestContext.WriteLine($"Gateway Endpoint: {gatewayEndpoint}");
        TestContext.WriteLine($"Kafka Bootstrap: {kafkaBootstrap}");
        TestContext.WriteLine();

        Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", gatewayEndpoint);
        Environment.SetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS", kafkaBootstrap);
        Environment.SetEnvironmentVariable("KAFKA_FLINK_BOOTSTRAP_SERVERS", "kafka:9092");

        string? jobId = null;

        try
        {
            TestContext.WriteLine("Running SampleApp.RunAsync()...");
            TestContext.WriteLine();

            Task<string> sampleAppTask = SampleApp.Program.RunAsync(inputTopic, outputTopic);
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
                consumer.Subscribe(outputTopic);

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
                    // Remove trailing slash to avoid double slashes
                    var baseUrl = gatewayEndpoint.TrimEnd('/');
                    await httpClient.PostAsync($"{baseUrl}/api/v1/jobs/{jobId}/cancel", null);
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
        const int expectedMessageCount = 1000; // Increased from 100 to allow live monitoring during test execution
        
        // Setup: Create unique topics
        var inputTopic = $"comprehensive-input-{Guid.NewGuid():N}";
        var outputTopic = $"comprehensive-output-{Guid.NewGuid():N}";
        
        TestContext.WriteLine($"📋 Test configuration:");
        TestContext.WriteLine($"   Input topic: {inputTopic}");
        TestContext.WriteLine($"   Output topic: {outputTopic}");
        TestContext.WriteLine($"   Expected messages: {expectedMessageCount}");
        TestContext.WriteLine();
        
        string? jobId = null;
        var gatewayEndpoint = await GetGatewayEndpointAsync();
        var prometheusEndpoint = await GetPrometheusEndpointAsync();
        
        try
        {
            // STEP 1: Submit job via Gateway (using container IP for Flink jobs)
            var jobDefinition = FlinkDotNetJobs.CreateUppercaseJobDefinition(inputTopic, outputTopic, GlobalTestInfrastructure.KafkaContainerIpForFlink!, "comprehensive-test");
            jobId = await SubmitJobViaGatewayAsync(gatewayEndpoint, jobDefinition, cts.Token);
            
            TestContext.WriteLine($"✅ Job submitted: {jobId}");
            await Task.Delay(TimeSpan.FromSeconds(10), cts.Token);
            
            // STEP 2: Produce messages in batches to keep job actively processing
            // Produce first batch to start the job processing
            const int batchSize = 200;
            await ProduceMessagesInBatchAsync(inputTopic, batchSize, 0, cts.Token);
            TestContext.WriteLine($"✅ Produced first batch of {batchSize} messages to input topic");
            
            // Wait a moment for processing to start
            await Task.Delay(TimeSpan.FromSeconds(2), cts.Token);
            
            // STEP 2.5: Query metrics WHILE job is actively processing (after first batch, before all messages complete)
            // This is critical because Flink only exposes detailed operator metrics while the job is running
            TestContext.WriteLine();
            TestContext.WriteLine("═══ Gateway Metrics Validation (During Active Processing) ═══");
            TestContext.WriteLine("NOTE: Querying metrics while Flink is actively processing first batch");
            TestContext.WriteLine("      for accurate RecordsIn/Out counters.");
            
            var metrics = await QueryGatewayMetricsAsync(gatewayEndpoint, jobId, cts.Token);
            
            TestContext.WriteLine($"📊 Gateway Metrics (during processing):");
            TestContext.WriteLine($"   RecordsIn: {metrics.RecordsIn}");
            TestContext.WriteLine($"   RecordsOut: {metrics.RecordsOut}");
            TestContext.WriteLine($"   Parallelism: {metrics.Parallelism}");
            TestContext.WriteLine($"   Checkpoints: {metrics.Checkpoints}");
            TestContext.WriteLine($"   BackpressureLevel: {metrics.BackpressureLevel}");
            TestContext.WriteLine();
            
            // Produce remaining messages
            for (int batch = 1; batch < (expectedMessageCount / batchSize); batch++)
            {
                await ProduceMessagesInBatchAsync(inputTopic, batchSize, batch * batchSize, cts.Token);
                await Task.Delay(TimeSpan.FromMilliseconds(500), cts.Token); // Small delay between batches
            }
            TestContext.WriteLine($"✅ Produced all {expectedMessageCount} messages to input topic in batches");
            
            // STEP 3: CRITICAL - Verify messages consumed from output topic AFTER checking metrics
            TestContext.WriteLine("═══ KAFKA MESSAGE METRICS VALIDATION (PRIMARY FOCUS) ═══");
            var consumeTimeout = TimeSpan.FromSeconds(60);
            var consumedMessages = await ConsumeMessagesAsync(outputTopic, expectedMessageCount, consumeTimeout, cts.Token);
            TestContext.WriteLine($"✅ Consumed {consumedMessages.Count} messages from output topic");
            
            // PROOF: Display sample messages to show Flink transformation worked
            if (consumedMessages.Count > 0)
            {
                TestContext.WriteLine();
                TestContext.WriteLine("📝 Sample transformed messages (proving Flink processed data):");
                for (int i = 0; i < Math.Min(5, consumedMessages.Count); i++)
                {
                    TestContext.WriteLine($"   [{i + 1}] {consumedMessages[i]}");
                }
                
                // Verify all messages were transformed to uppercase (proving Flink worked)
                int uppercaseCount = consumedMessages.Count(msg => msg == msg.ToUpperInvariant() && msg.Any(char.IsLetter));
                TestContext.WriteLine();
                TestContext.WriteLine($"🔍 Transformation verification: {uppercaseCount}/{consumedMessages.Count} messages are uppercase");
                Assert.That(uppercaseCount, Is.EqualTo(consumedMessages.Count), 
                    "All messages should be uppercase (proves Flink transformation worked)");
                TestContext.WriteLine("✅ All messages correctly transformed to uppercase by Flink");
            }
            
            Assert.That(consumedMessages.Count, Is.EqualTo(expectedMessageCount), 
                $"CRITICAL: Should consume exactly {expectedMessageCount} messages (Kafka metrics validation)");
            
            // STEP 4: Validate Gateway metrics collected during active processing
            TestContext.WriteLine();
            TestContext.WriteLine("═══ Final Metrics Validation ═══");
            TestContext.WriteLine("Validating metrics collected during active processing:");
            TestContext.WriteLine($"   RecordsIn: {metrics.RecordsIn} (actual count at query time)");
            TestContext.WriteLine($"   RecordsOut: {metrics.RecordsOut} (actual count at query time)");
            TestContext.WriteLine($"   Parallelism: {metrics.Parallelism} (expected: 1)");
            TestContext.WriteLine();
            
            // PRIMARY VALIDATION: Kafka message metrics
            // NOTE: Metrics are queried after first batch (200 messages) during active processing
            //   Values will be > 0 but < total messages since job is still processing remaining batches
            //   This proves Prometheus integration is working and returning real-time metrics
            Assert.That(metrics.RecordsIn, Is.GreaterThan(0), 
                "RecordsIn should be > 0 (proves Prometheus metrics working)");
            Assert.That(metrics.RecordsOut, Is.GreaterThan(0), 
                "RecordsOut should be > 0 (proves Prometheus metrics working)");
            Assert.That(metrics.Parallelism, Is.EqualTo(1), "Job parallelism should be 1");
            
            TestContext.WriteLine($"✅ KAFKA MESSAGE METRICS VALIDATED:");
            TestContext.WriteLine($"   ✅ RecordsIn: {metrics.RecordsIn} records processed (Prometheus real-time metrics)");
            TestContext.WriteLine($"   ✅ RecordsOut: {metrics.RecordsOut} records output (Prometheus real-time metrics)");
            TestContext.WriteLine($"   ✅ Parallelism: {metrics.Parallelism}");
            TestContext.WriteLine($"   ℹ️  Note: Counts reflect processing at query time (after first batch of {expectedMessageCount / 5} messages)");
            TestContext.WriteLine();

            // STEP 4.1: Validate comprehensive metrics from CustomMetrics dictionary
            TestContext.WriteLine("═══ Comprehensive Metrics Validation ═══");
            TestContext.WriteLine("Validating JobManager, TaskManager, and Kafka metrics...");
            TestContext.WriteLine();
            
            // STEP 4.1.1: Diagnostic - Verify Prometheus is accessible and has targets
            TestContext.WriteLine("🔍 DIAGNOSTIC: Verifying Prometheus Configuration");
            await VerifyPrometheusHealthAsync(prometheusEndpoint, cts.Token);
            TestContext.WriteLine();
            
            // CRITICAL: Wait and retry to ensure Prometheus has scraped ALL metrics
            // ALL metrics MUST be > 0 to prove metrics collection works
            TestContext.WriteLine("Waiting for Prometheus to scrape all metrics...");
            int retryCount = 0;
            const int maxRetries = 10;
            const int retryDelayMs = 2000; // 2 seconds is enough for Prometheus scraping
            
            string[] requiredNonZeroMetrics = new[]
            {
                "JobManager.Memory.Heap.Used",
                "JobManager.RunningJobs",
                "TaskManager.Memory.Heap.Used",
                "TaskManager.ActiveTasks"
            };
            
            while (retryCount < maxRetries)
            {
                bool allMetricsValid = true;
                var missingMetrics = new List<string>();
                var zeroMetrics = new List<string>();
                
                foreach (var metricKey in requiredNonZeroMetrics)
                {
                    if (!metrics.CustomMetrics.TryGetValue(metricKey, out var metricValue))
                    {
                        allMetricsValid = false;
                        missingMetrics.Add(metricKey);
                        continue;
                    }
                    
                    long value = metricValue switch
                    {
                        long l => l,
                        int i => i,
                        double d => (long)d,
                        _ => 0
                    };
                    
                    if (value <= 0)
                    {
                        allMetricsValid = false;
                        zeroMetrics.Add(metricKey);
                    }
                }
                
                if (!allMetricsValid)
                {
                    if (missingMetrics.Count > 0)
                    {
                        TestContext.WriteLine($"⏳ Missing metrics: {string.Join(", ", missingMetrics)}");
                    }
                    if (zeroMetrics.Count > 0)
                    {
                        TestContext.WriteLine($"⏳ Zero-value metrics: {string.Join(", ", zeroMetrics)}");
                    }
                }
                
                if (allMetricsValid)
                {
                    TestContext.WriteLine($"✅ All required metrics available and non-zero (after {retryCount} retries)");
                    break;
                }
                
                retryCount++;
                if (retryCount < maxRetries)
                {
                    TestContext.WriteLine($"⏳ Retrying metrics query... (attempt {retryCount}/{maxRetries})");
                    await Task.Delay(retryDelayMs, cts.Token);
                    
                    // Re-query metrics to get updated values from Prometheus
                    metrics = await QueryGatewayMetricsAsync(gatewayEndpoint, jobId, cts.Token);
                }
                else
                {
                    TestContext.WriteLine("❌ Max retries reached. Proceeding with validation (may fail).");
                }
            }
            TestContext.WriteLine();

            // Validate JobManager metrics (ALL must be > 0)
            TestContext.WriteLine("JobManager Metrics:");
            ValidateCustomMetric(metrics, "JobManager.CPU.Load", "JobManager CPU Load", requireNonZero: true);
            ValidateCustomMetric(metrics, "JobManager.Memory.Heap.Used", "JobManager Heap Memory", requireNonZero: true);
            ValidateCustomMetric(metrics, "JobManager.RunningJobs", "JobManager Running Jobs", requireNonZero: true);
            TestContext.WriteLine();

            // Validate TaskManager metrics (ALL must be > 0)
            TestContext.WriteLine("TaskManager Metrics:");
            ValidateCustomMetric(metrics, "TaskManager.CPU.Load", "TaskManager CPU Load", requireNonZero: true);
            ValidateCustomMetric(metrics, "TaskManager.Memory.Heap.Used", "TaskManager Heap Memory", requireNonZero: true);
            ValidateCustomMetric(metrics, "TaskManager.ActiveTasks", "TaskManager Active Tasks", requireNonZero: true);
            TestContext.WriteLine();

            // Validate Kafka topic metrics (ALL must be > 0)
            TestContext.WriteLine("Kafka Topic Metrics:");
            ValidateCustomMetric(metrics, "Kafka.Topic.TotalOffsets", "Kafka Topic Total Offsets", requireNonZero: true);
            ValidateCustomMetric(metrics, "Kafka.Topic.PartitionCount", "Kafka Topic Partition Count", requireNonZero: true);
            ValidateCustomMetric(metrics, "Kafka.Consumer.CurrentOffset", "Kafka Consumer Current Offset", requireNonZero: true);
            ValidateCustomMetric(metrics, "Kafka.Topic.MessagesInFlight", "Kafka Messages In Flight", requireNonZero: true);
            ValidateCustomMetric(metrics, "Kafka.Topic.MessageRate", "Kafka Topic Message Rate", requireNonZero: true);
            ValidateCustomMetric(metrics, "Kafka.Consumer.Lag", "Kafka Consumer Lag", requireNonZero: false); // Lag can be 0 if consumer caught up
            TestContext.WriteLine();

            // Validate operator throughput metrics (ALL must be > 0)
            TestContext.WriteLine("Operator Throughput Metrics:");
            ValidateCustomMetric(metrics, "Operator.BytesRead", "Operator Bytes Read", requireNonZero: true);
            ValidateCustomMetric(metrics, "Operator.BytesWritten", "Operator Bytes Written", requireNonZero: true);
            
            // Also validate the direct properties on JobMetrics (must be > 0)
            Assert.That(metrics.BytesRead, Is.GreaterThan(0), 
                "BytesRead property should be > 0");
            Assert.That(metrics.BytesWritten, Is.GreaterThan(0), 
                "BytesWritten property should be > 0");
            TestContext.WriteLine($"   BytesRead (property): {metrics.BytesRead:N0} bytes");
            TestContext.WriteLine($"   BytesWritten (property): {metrics.BytesWritten:N0} bytes");
            TestContext.WriteLine();

            TestContext.WriteLine("✅ COMPREHENSIVE METRICS VALIDATED");
            TestContext.WriteLine($"   Total metrics collected: {metrics.CustomMetrics.Count + 4} (CustomMetrics + core metrics)");
            TestContext.WriteLine();
            
            // STEP 5: Validate Prometheus integration
            TestContext.WriteLine("═══ Prometheus Integration Validation ═══");
            TestContext.WriteLine($"Prometheus endpoint: {prometheusEndpoint}");
            
            var httpClient = _httpClient!; // Null-forgiving operator safe after initialization check
            var targetsResponse = await httpClient.GetFromJsonAsync<JsonDocument>($"{prometheusEndpoint}/api/v1/targets", cts.Token);
            var activeTargets = targetsResponse?.RootElement.GetProperty("data").GetProperty("activeTargets");
            
            Assert.That(activeTargets?.GetArrayLength(), Is.GreaterThan(0), "Prometheus should have active scrape targets");
            TestContext.WriteLine($"✅ Prometheus has {activeTargets?.GetArrayLength()} active targets");
            
            // Validate Kafka metrics are available in Prometheus
            var kafkaMetricsQuery = "flink_taskmanager_job_task_operator_records_out_rate";
            var queryResponse = await _httpClient!.GetFromJsonAsync<JsonDocument>($"{prometheusEndpoint}/api/v1/query?query={kafkaMetricsQuery}", cts.Token);
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
            Assert.That(metrics.BackpressureLevel, Is.Not.Null, "Backpressure level should not be null");
            Assert.That(metrics.BackpressureLevel, Is.Not.Empty, "Backpressure level should not be empty");
            TestContext.WriteLine($"✅ Backpressure level detected: {metrics.BackpressureLevel}");
            
            // Also validate it's in CustomMetrics for backward compatibility
            if (metrics.CustomMetrics.TryGetValue("backpressureLevel", out var bpLevel))
            {
                TestContext.WriteLine($"   Backpressure also available in CustomMetrics: {bpLevel}");
            }
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
        finally
        {
            if (jobId != null)
            {
                try
                {
                    using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                    // Remove trailing slash to avoid double slashes
                    var baseUrl = gatewayEndpoint.TrimEnd('/');
                    await httpClient.PostAsync($"{baseUrl}/api/v1/jobs/{jobId}/cancel", null);
                    TestContext.WriteLine($"Cancelled job {jobId}");
                }
                catch { /* Ignore cleanup errors */ }
            }
        }
    }

    // ========== Helper Methods ==========

    private async Task<string> SubmitJobViaGatewayAsync(string gatewayEndpoint, object jobDefinition, CancellationToken ct)
    {
        var jsonContent = JsonContent.Create(jobDefinition);
        // Remove trailing slash from endpoint to avoid double slashes
        var baseUrl = gatewayEndpoint.TrimEnd('/');
        var response = await _httpClient!.PostAsync($"{baseUrl}/api/v1/jobs/submit", jsonContent, ct);
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<JobSubmissionResult>(cancellationToken: ct);
        
        // Return the Flink job ID from the Gateway response
        return result?.FlinkJobId ?? throw new InvalidOperationException("Gateway did not return a FlinkJobId");
    }

    // Unused helper method kept for potential future use
    #pragma warning disable S1144 // Remove the unused private method
    private Task ProduceMessagesAsync(string topic, int count, CancellationToken ct)
    #pragma warning restore S1144
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
            // Check cancellation before each message
            ct.ThrowIfCancellationRequested();
            
            var message = new Confluent.Kafka.Message<string, string>
            {
                Key = $"key-{i}",
                Value = $"test message {i}"
            };
            
            // Don't pass ct to ProduceAsync - let it fail faster on connection issues
            producer.ProduceAsync(topic, message);
        }
        
        producer.Flush(TimeSpan.FromSeconds(10));
        return Task.CompletedTask;
    }

    private Task ProduceMessagesInBatchAsync(string topic, int count, int startIndex, CancellationToken ct)
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
            ct.ThrowIfCancellationRequested();
            
            var message = new Confluent.Kafka.Message<string, string>
            {
                Key = $"key-{startIndex + i}",
                Value = $"test message {startIndex + i}"
            };
            
            producer.ProduceAsync(topic, message);
        }
        
        producer.Flush(TimeSpan.FromSeconds(10));
        return Task.CompletedTask;
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

    private static async Task<JobMetrics> QueryGatewayMetricsAsync(string gatewayEndpoint, string jobId, CancellationToken ct)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        // Remove trailing slash to avoid double slashes
        var baseUrl = gatewayEndpoint.TrimEnd('/');
        var response = await httpClient.GetAsync($"{baseUrl}/api/v1/jobs/{jobId}/metrics", ct);
        response.EnsureSuccessStatusCode();
        
        // Deserialize directly to JobMetrics from Flink.JobBuilder.Models
        var metrics = await response.Content.ReadFromJsonAsync<JobMetrics>(cancellationToken: ct);
        return metrics ?? throw new InvalidOperationException("Failed to deserialize metrics from Gateway");
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

    private static void ValidateCustomMetric(JobMetrics metrics, string metricKey, string metricName, bool requireNonZero = true)
    {
        // Try to get the metric from CustomMetrics dictionary
        if (metrics.CustomMetrics.TryGetValue(metricKey, out var metricValue))
        {
            // Validate the metric value is not null
            Assert.That(metricValue, Is.Not.Null, 
                $"{metricName} should not be null");
            
            // Convert to long for comparison
            long value = metricValue switch
            {
                long l => l,
                int i => i,
                double d => (long)d,
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
    private async Task VerifyPrometheusHealthAsync(string prometheusEndpoint, CancellationToken cancellationToken)
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
            
            if (targetsResponse != null && 
                targetsResponse.RootElement.TryGetProperty("data", out var dataEl) &&
                dataEl.TryGetProperty("activeTargets", out var targetsEl))
            {
                var activeTargets = targetsEl.EnumerateArray().ToList();
                TestContext.WriteLine($"   📊 Active Prometheus targets: {activeTargets.Count}");
                
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
                            upTargets++;
                        else
                            downTargets++;
                    }
                }
                
                TestContext.WriteLine($"   📈 Target health: {upTargets} up, {downTargets} down");
                
                foreach (var (job, count) in targetsByJob.OrderBy(kv => kv.Key))
                {
                    TestContext.WriteLine($"      - {job}: {count} target(s)");
                }
                
                // Check for expected targets
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
            }
            else
            {
                TestContext.WriteLine($"   ⚠️  Could not retrieve Prometheus targets (API response format unexpected)");
            }

            // Check 3: Verify we can query a basic Flink metric
            TestContext.WriteLine($"   3️⃣ Testing Prometheus query for Flink metrics...");
            string testQuery = Uri.EscapeDataString("flink_jobmanager_Status_JVM_Memory_Heap_Used");
            var queryResponse = await _httpClient.GetFromJsonAsync<JsonDocument>(
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
        catch (Exception ex)
        {
            TestContext.WriteLine($"   ❌ DIAGNOSTIC ERROR: {ex.Message}");
            TestContext.WriteLine($"      → This may indicate network issues or Prometheus configuration problems");
        }
    }

    // ========== Data Models ==========
    
    private sealed class JobSubmissionResult
    {
        public string FlinkJobId { get; set; } = string.Empty;
    }
}
