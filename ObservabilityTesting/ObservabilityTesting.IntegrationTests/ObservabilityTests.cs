using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Flink.JobBuilder.Models;
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
public partial class ObservabilityTests : LocalTestingTestBase
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
        PrintTestHeader();
        
        var testStartTime = DateTime.UtcNow;
        TestContext.WriteLine($"[TIMING] Test started at: {testStartTime:HH:mm:ss.fff}");
        
        using var cts = new CancellationTokenSource(TestTimeout);
        const int expectedMessageCount = 10000;
        
        var inputTopic = $"comprehensive-input-{Guid.NewGuid():N}";
        var outputTopic = $"comprehensive-output-{Guid.NewGuid():N}";
        
        PrintTestConfiguration(inputTopic, outputTopic, expectedMessageCount);
        
        string? jobId = null;
        var gatewayEndpoint = await GetGatewayEndpointAsync();
        var prometheusEndpoint = await GetPrometheusEndpointAsync();
        
        try
        {
            // STEP 1: Submit job via Gateway (using container IP for Flink jobs)
            var jobSubmitTime = DateTime.UtcNow;
            var jobDefinition = FlinkDotNetJobs.CreateUppercaseJobDefinition(inputTopic, outputTopic, GlobalTestInfrastructure.KafkaContainerIpForFlink!, "comprehensive-test");
            jobId = await SubmitJobViaGatewayAsync(gatewayEndpoint, jobDefinition, cts.Token);
            
            TestContext.WriteLine($"✅ Job submitted: {jobId}");
            TestContext.WriteLine($"[TIMING] Job submitted at: {jobSubmitTime:HH:mm:ss.fff} (elapsed: {(jobSubmitTime - testStartTime).TotalSeconds:F1}s)");
            await Task.Delay(TimeSpan.FromSeconds(10), cts.Token);
            
            // STEP 2: Start producing messages asynchronously to keep job actively processing
            // This runs in parallel with metrics validation so Prometheus can scrape while messages flow
            var messageProductionStartTime = DateTime.UtcNow;
            TestContext.WriteLine($"[TIMING] Starting message production at: {messageProductionStartTime:HH:mm:ss.fff} (elapsed: {(messageProductionStartTime - testStartTime).TotalSeconds:F1}s)");
            var messageProducingTask = StartMessageProductionAsync(
                inputTopic, expectedMessageCount, ProduceMessagesInBatchAsync, cts.Token);
            
            // STEP 2.5: Poll Prometheus metrics until they're available (run in parallel with message production)
            // CRITICAL: Don't cancel job until Prometheus has scraped metrics
            TestContext.WriteLine();
            TestContext.WriteLine("═══ Gateway Metrics Validation (During Active Processing) ═══");
            TestContext.WriteLine("NOTE: Continuously checking Prometheus metrics while job is running");
            TestContext.WriteLine("      Waiting for Prometheus to scrape and return non-zero values");
            TestContext.WriteLine("      Consumer lag may be unavailable during initial collection (expected)");
            TestContext.WriteLine("      kafka-topic-exporter needs 5-10s to discover new consumer groups");
            
            // Initial delay to allow Prometheus first scrape cycle (1s interval + processing time)
            await Task.Delay(TimeSpan.FromSeconds(3), cts.Token);
            
            const int maxRetries = 30; // Increased to allow more time for Prometheus scraping
            const int retryDelayMs = 2000; // 2 seconds between checks
            
            // Require metrics that should exist during active processing
            // These metrics prove Prometheus is scraping while job is running
            // NOTE: Consumer lag metrics may NOT be available yet during initial collection
            //       because kafka-topic-exporter needs time to discover the consumer group.
            //       This is expected and acceptable - final validation will check consumer lag.
            // Note: Operator.BytesRead/BytesWritten are not available from custom Kafka source
            // Only RecordsIn/RecordsOut are tracked by our custom source implementation
            string[] requiredNonZeroMetrics = new[]
            {
                "JobManager.Memory.Heap.Used",
                "TaskManager.Memory.Heap.Used"
            };
            
            var metricsCollectionStartTime = DateTime.UtcNow;
            TestContext.WriteLine($"[TIMING] Starting metrics collection at: {metricsCollectionStartTime:HH:mm:ss.fff} (elapsed: {(metricsCollectionStartTime - testStartTime).TotalSeconds:F1}s)");
            var metrics = await PollPrometheusMetricsAsync(
                gatewayEndpoint, prometheusEndpoint, jobId, 
                requiredNonZeroMetrics, maxRetries, retryDelayMs, cts.Token);
            
            var metricsCollectionEndTime = DateTime.UtcNow;
            TestContext.WriteLine($"[TIMING] Metrics collected at: {metricsCollectionEndTime:HH:mm:ss.fff} (elapsed: {(metricsCollectionEndTime - testStartTime).TotalSeconds:F1}s)");
            
            // Display collected metrics
            TestContext.WriteLine();
            TestContext.WriteLine($"📊 Gateway Metrics (during processing):");
            TestContext.WriteLine($"   RecordsIn: {metrics.RecordsIn}");
            TestContext.WriteLine($"   RecordsOut: {metrics.RecordsOut}");
            TestContext.WriteLine($"   Parallelism: {metrics.Parallelism}");
            TestContext.WriteLine($"   Checkpoints: {metrics.Checkpoints}");
            TestContext.WriteLine($"   BackpressureLevel: {metrics.BackpressureLevel}");
            TestContext.WriteLine();
            
            // Wait for message production to complete
            await messageProducingTask;
            
            // STEP 3: CRITICAL - Verify messages consumed from output topic AFTER checking metrics
            TestContext.WriteLine("═══ KAFKA MESSAGE METRICS VALIDATION (PRIMARY FOCUS) ═══");
            // Increase timeout for 10,000 messages - give 2 minutes to consume all messages
            var consumeTimeout = TimeSpan.FromSeconds(120);
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

            // DEBUG: Check Kafka consumer groups and offset commits before querying metrics
            await DebugKafkaConsumerGroupsAsync();

            // CRITICAL: Always query fresh metrics right before validation
            await CollectFinalMetricsAndMergeAsync(gatewayEndpoint, prometheusEndpoint, jobId, metrics, cts.Token);

            // Validate all comprehensive metrics (JobManager, TaskManager, Kafka)
            ValidateAllComprehensiveMetrics(metrics);
            
            // Note: Operator.BytesRead/BytesWritten metrics are not available from our custom Kafka source implementation
            // These would only be available if using Flink's official Kafka connector
            // RecordsIn/Out metrics are sufficient to validate the pipeline is working
            TestContext.WriteLine($"   Total metrics collected: {metrics.CustomMetrics.Count + 4} (CustomMetrics + core metrics)");
            TestContext.WriteLine();
            
            // STEP 5: Validate Prometheus integration
            await ValidatePrometheusIntegrationAsync(_httpClient!, prometheusEndpoint, cts.Token);
            
            // STEP 6: Validate Grafana configuration
            await ValidateGrafanaConfigurationAsync(_httpClient!, cts.Token);
            
            // STEP 7: Validate backpressure detection
            ValidateBackpressureMetrics(metrics);
            
            // STEP 8: Validate checkpoint metrics
            ValidateCheckpointMetrics(metrics);
            
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

    private static async Task<JobMetrics> PollPrometheusMetricsAsync(
        string gatewayEndpoint, 
        string prometheusEndpoint, 
        string jobId, 
        string[] requiredNonZeroMetrics, 
        int maxRetries,
        int retryDelayMs,
        CancellationToken ct)
    {
        JobMetrics? metrics = null;
        int retryCount = 0;
        bool allMetricsValid = false;

        while (retryCount < maxRetries && !allMetricsValid)
        {
            metrics = await CollectAndMergeMetricsAsync(gatewayEndpoint, prometheusEndpoint, jobId, ct);
            allMetricsValid = ValidateRequiredMetrics(metrics, requiredNonZeroMetrics, retryCount, maxRetries);

            if (!allMetricsValid && retryCount < maxRetries)
            {
                retryCount++;
                await Task.Delay(retryDelayMs, ct);
            }
            else if (allMetricsValid)
            {
                TestContext.WriteLine($"✅ All required metrics have non-zero values (after {retryCount} retries)");
                break;
            }
        }

        if (!allMetricsValid)
        {
            TestContext.WriteLine($"⚠️  Warning: Some metrics still zero after {maxRetries} retries, proceeding anyway");
        }

        return metrics ?? throw new InvalidOperationException("Failed to collect metrics");
    }

    private static async Task<JobMetrics> CollectAndMergeMetricsAsync(
        string gatewayEndpoint,
        string prometheusEndpoint,
        string jobId,
        CancellationToken ct)
    {
        var metrics = await QueryGatewayMetricsAsync(gatewayEndpoint, jobId, ct);
        var prometheusMetrics = await CollectPrometheusMetricsAsync(prometheusEndpoint, jobId, ct);

        foreach (var (key, value) in prometheusMetrics)
        {
            metrics.CustomMetrics[key] = value;
        }

        // Update direct properties if available
        if (prometheusMetrics.TryGetValue("BytesRead", out var bytesReadObj) && bytesReadObj is long bytesReadVal)
            metrics.BytesRead = bytesReadVal;
        if (prometheusMetrics.TryGetValue("BytesWritten", out var bytesWrittenObj) && bytesWrittenObj is long bytesWrittenVal)
            metrics.BytesWritten = bytesWrittenVal;
        if (prometheusMetrics.TryGetValue("RecordsIn", out var recordsInObj) && recordsInObj is long recordsInVal)
            metrics.RecordsIn = recordsInVal;
        if (prometheusMetrics.TryGetValue("RecordsOut", out var recordsOutObj) && recordsOutObj is long recordsOutVal)
            metrics.RecordsOut = recordsOutVal;

        return metrics;
    }

    private static bool ValidateRequiredMetrics(JobMetrics metrics, string[] requiredNonZeroMetrics, int retryCount, int maxRetries)
    {
        bool allValid = true;
        var missingMetrics = new List<string>();
        var zeroMetrics = new List<string>();

        foreach (var metricKey in requiredNonZeroMetrics)
        {
            if (!metrics.CustomMetrics.TryGetValue(metricKey, out var metricValue))
            {
                allValid = false;
                missingMetrics.Add(metricKey);
                continue;
            }

            long value = ConvertMetricValueToLong(metricValue);
            if (value <= 0)
            {
                allValid = false;
                zeroMetrics.Add(metricKey);
            }
        }

        if (!allValid)
        {
            if (missingMetrics.Count > 0)
            {
                TestContext.WriteLine($"⏳ Missing metrics: {string.Join(", ", missingMetrics)} (attempt {retryCount + 1}/{maxRetries})");
            }
            if (zeroMetrics.Count > 0)
            {
                TestContext.WriteLine($"⏳ Zero-value metrics: {string.Join(", ", zeroMetrics)} (attempt {retryCount + 1}/{maxRetries})");
            }
        }

        return allValid;
    }

    private static long ConvertMetricValueToLong(object metricValue)
    {
        return metricValue switch
        {
            long l => l,
            int i => i,
            double d => (long)d,
            JsonElement je when je.ValueKind == JsonValueKind.Number =>
                je.TryGetInt64(out long l) ? l : (long)je.GetDouble(),
            _ => 0
        };
    }

    private static Task StartMessageProductionAsync(
        string inputTopic,
        int expectedMessageCount,
        Func<string, int, int, CancellationToken, Task> produceMethod,
        CancellationToken ct)
    {
        return Task.Run(async () =>
        {
            try
            {
                const int batchSize = 200;
                await produceMethod(inputTopic, batchSize, 0, ct);
                TestContext.WriteLine($"✅ Produced first batch of {batchSize} messages to input topic");

                for (int batch = 1; batch < (expectedMessageCount / batchSize); batch++)
                {
                    await produceMethod(inputTopic, batchSize, batch * batchSize, ct);
                    await Task.Delay(TimeSpan.FromMilliseconds(500), ct);
                }
                TestContext.WriteLine($"✅ Produced all {expectedMessageCount} messages to input topic in batches");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"⚠️  Message production error: {ex.Message}");
                throw;
            }
        }, ct);
    }

    private static async Task<string> SubmitJobViaGatewayAsync(string gatewayEndpoint, object jobDefinition, CancellationToken ct)
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

    private async Task ProduceMessagesInBatchAsync(string topic, int count, int startIndex, CancellationToken ct)
    {
        var produceStartTime = DateTime.UtcNow;
        TestContext.WriteLine($"[TIMING] [PRODUCE] Starting production of {count} messages at: {produceStartTime:HH:mm:ss.fff}");
        
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
        
        var tasks = new List<Task>();
        for (int i = 0; i < count; i++)
        {
            ct.ThrowIfCancellationRequested();
            
            var message = new Confluent.Kafka.Message<string, string>
            {
                Key = $"key-{startIndex + i}",
                Value = $"test message {startIndex + i}"
            };
            
            // Await the async produce to ensure all messages are queued
            tasks.Add(producer.ProduceAsync(topic, message));
        }
        
        // Wait for all produce tasks to complete
        await Task.WhenAll(tasks);
        
        // Flush to ensure all messages are sent
        producer.Flush(TimeSpan.FromSeconds(10));
        
        var produceEndTime = DateTime.UtcNow;
        var duration = (produceEndTime - produceStartTime).TotalSeconds;
        TestContext.WriteLine($"[TIMING] [PRODUCE] Completed production of {count} messages at: {produceEndTime:HH:mm:ss.fff} (duration: {duration:F2}s, rate: {count/duration:F0} msg/s)");
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
        int consecutiveEmptyPolls = 0;
        const int maxEmptyPollsBeforeExit = 5; // Exit after 5 consecutive empty polls
        
        while (stopwatch.Elapsed < timeout && consumed.Count < expectedCount && !ct.IsCancellationRequested)
        {
            var result = consumer.Consume(TimeSpan.FromMilliseconds(1000));
            
            if (result != null)
            {
                consumed.Add(result.Message.Value);
                consumer.Commit(result);
                consecutiveEmptyPolls = 0; // Reset counter on successful consume
            }
            else
            {
                // No message received
                consecutiveEmptyPolls++;
                
                // If we've consumed some messages and had several empty polls, likely done
                if (consumed.Count > 0 && consecutiveEmptyPolls >= maxEmptyPollsBeforeExit)
                {
                    TestContext.WriteLine($"   ℹ️  Exiting consumer after {consecutiveEmptyPolls} consecutive empty polls with {consumed.Count} messages");
                    break;
                }
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

    private static async Task<string> GetGatewayEndpointAsync()
    {
        // Use the existing GlobalTestInfrastructure method that discovers endpoint from Docker
        return await GlobalTestInfrastructure.GetGatewayEndpointAsync();
    }
}
