using NUnit.Framework;
using Confluent.Kafka;
using System.Text.Json;
using System.Diagnostics;
using LocalTesting.FlinkSqlAppHost;

namespace LocalTesting.IntegrationTests;

/// <summary>
/// Diagnostic test to compare Gateway job submission vs pure Apache Flink job submission.
/// This helps identify whether the issue is in Gateway or in the Kafka configuration.
/// </summary>
[TestFixture, NonParallelizable]
[Category("Diagnostic")]
public class GatewayVsPureFlinkDiagnosticTest : LocalTestingTestBase
{
    private static string PureFlinkInputTopic => $"lt.diagnostic.pureflink.input.{TestContext.CurrentContext.Test.ID}";
    private static string PureFlinkOutputTopic => $"lt.diagnostic.pureflink.output.{TestContext.CurrentContext.Test.ID}";
    private static string GatewayInputTopic => $"lt.diagnostic.gateway.input.{TestContext.CurrentContext.Test.ID}";
    private static string GatewayOutputTopic => $"lt.diagnostic.gateway.output.{TestContext.CurrentContext.Test.ID}";

    [Test]
    public async Task Diagnostic_CompareGatewayVsPureFlink_IdentifyRootCause()
    {
        TestPrerequisites.EnsureDockerAvailable();
        
        var baseToken = TestContext.CurrentContext.CancellationToken;
        using var testTimeout = new System.Threading.CancellationTokenSource(TimeSpan.FromMinutes(5));
        using var linkedCts = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(baseToken, testTimeout.Token);
        var ct = linkedCts.Token;

        try
        {
            // Wait for complete infrastructure including Gateway
            await WaitForFullInfrastructureAsync(includeGateway: true, ct);
            TestContext.WriteLine("✅ Complete infrastructure ready (Kafka + Flink + Gateway)");

            // Arrange - Create all topics
            await CreateTopicAsync(PureFlinkInputTopic);
            await CreateTopicAsync(PureFlinkOutputTopic);
            await CreateTopicAsync(GatewayInputTopic);
            await CreateTopicAsync(GatewayOutputTopic);

            TestContext.WriteLine("🔍 DIAGNOSTIC TEST: Comparing Gateway vs Pure Flink job submission");
            TestContext.WriteLine($"📝 Pure Flink: {PureFlinkInputTopic} → {PureFlinkOutputTopic}");
            TestContext.WriteLine($"📝 Gateway: {GatewayInputTopic} → {GatewayOutputTopic}");

            // Get Gateway endpoint
            var gatewayBase = $"http://localhost:{Ports.GatewayHostPort}/";

            // ============================================================
            // PART 1: Submit PURE APACHE FLINK JOB (bypass Gateway)
            // ============================================================
            TestContext.WriteLine("\n═══════════════════════════════════════════════════════════");
            TestContext.WriteLine("PART 1: Submitting PURE APACHE FLINK JOB (bypass Gateway)");
            TestContext.WriteLine("═══════════════════════════════════════════════════════════");

            var pureFlinkJobDef = new
            {
                metadata = new
                {
                    jobName = "diagnostic-pure-flink-job"
                },
                source = new
                {
                    type = "kafka",  // REQUIRED: Type discriminator for polymorphic deserialization
                    bootstrapServers = "kafka:9093",
                    topic = PureFlinkInputTopic,
                    groupId = "diagnostic-pure-flink-consumer",
                    startingOffsets = "earliest"
                },
                operations = new[]
                {
                    new
                    {
                        type = "map",
                        expression = "toUpper"
                    }
                },
                sink = new
                {
                    type = "kafka",  // REQUIRED: Type discriminator for polymorphic deserialization
                    bootstrapServers = "kafka:9093",
                    topic = PureFlinkOutputTopic
                }
            };

            var pureFlinkJson = JsonSerializer.Serialize(pureFlinkJobDef, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            TestContext.WriteLine($"📤 Pure Flink Job Definition:\n{pureFlinkJson}");

            using var pureFlinkClient = new HttpClient { BaseAddress = new Uri(gatewayBase) };
            var pureFlinkContent = new StringContent(pureFlinkJson, System.Text.Encoding.UTF8, "application/json");
            var pureFlinkResponse = await pureFlinkClient.PostAsync("/api/v1/jobs/submit", pureFlinkContent, ct);
            var pureFlinkResponseBody = await pureFlinkResponse.Content.ReadAsStringAsync(ct);

            TestContext.WriteLine($"📥 Pure Flink Response Status: {pureFlinkResponse.StatusCode}");
            TestContext.WriteLine($"📥 Pure Flink Response Body: {pureFlinkResponseBody}");

            Assert.That(pureFlinkResponse.IsSuccessStatusCode, Is.True,
                $"Pure Flink job submission failed: {pureFlinkResponseBody}");

            var pureFlinkResult = JsonSerializer.Deserialize<JsonElement>(pureFlinkResponseBody);
            var pureFlinkJobId = pureFlinkResult.GetProperty("jobId").GetString();
            var pureFlinkFlinkJobId = pureFlinkResult.GetProperty("flinkJobId").GetString();
            TestContext.WriteLine($"✅ Pure Flink Job ID: {pureFlinkJobId}");
            TestContext.WriteLine($"✅ Pure Flink Flink Job ID: {pureFlinkFlinkJobId}");

            // Wait for pure Flink job to be RUNNING (use flinkJobId for status endpoint)
            await WaitForJobRunningAsync(gatewayBase, pureFlinkFlinkJobId!, TimeSpan.FromSeconds(60), ct);
            TestContext.WriteLine($"✅ Pure Flink job {pureFlinkFlinkJobId} is RUNNING");

            // ============================================================
            // PART 2: Submit GATEWAY JOB (using job builder)
            // ============================================================
            TestContext.WriteLine("\n═══════════════════════════════════════════════════════════");
            TestContext.WriteLine("PART 2: Submitting GATEWAY JOB (using FlinkJobBuilder)");
            TestContext.WriteLine("═══════════════════════════════════════════════════════════");

            var gatewayJob = FlinkDotNet.Flink.JobBuilder
                .FromKafka(GatewayInputTopic, KafkaContainerConnectionString)
                .Map("toUpper")
                .ToKafka(GatewayOutputTopic, KafkaContainerConnectionString);

            TestContext.WriteLine($"📤 Gateway Job - Source Bootstrap: {KafkaContainerConnectionString}");
            TestContext.WriteLine($"📤 Gateway Job - Sink Bootstrap: {KafkaContainerConnectionString}");

            var submitResult = await gatewayJob.Submit("diagnostic-gateway-job", ct);

            TestContext.WriteLine($"📥 Gateway Response Success: {submitResult.Success}");
            TestContext.WriteLine($"📥 Gateway Job ID: {submitResult.FlinkJobId}");
            TestContext.WriteLine($"📥 Gateway Error: {submitResult.ErrorMessage}");

            Assert.That(submitResult.Success, Is.True, $"Gateway job submission failed: {submitResult.ErrorMessage}");

            // Wait for Gateway job to be RUNNING
            await WaitForJobRunningAsync(gatewayBase, submitResult.FlinkJobId!, TimeSpan.FromSeconds(60), ct);
            TestContext.WriteLine($"✅ Gateway job {submitResult.FlinkJobId} is RUNNING");

            // ============================================================
            // PART 3: Send test messages to BOTH jobs
            // ============================================================
            TestContext.WriteLine("\n═══════════════════════════════════════════════════════════");
            TestContext.WriteLine("PART 3: Sending test messages to BOTH jobs");
            TestContext.WriteLine("═══════════════════════════════════════════════════════════");

            var testMessages = new[] { "pure", "flink", "vs", "gateway", "test" };

            // Send to Pure Flink input topic
            await ProduceTestMessagesAsync(PureFlinkInputTopic, testMessages, ct);
            TestContext.WriteLine($"✅ Sent {testMessages.Length} messages to Pure Flink input: {PureFlinkInputTopic}");

            // Send to Gateway input topic
            await ProduceTestMessagesAsync(GatewayInputTopic, testMessages, ct);
            TestContext.WriteLine($"✅ Sent {testMessages.Length} messages to Gateway input: {GatewayInputTopic}");

            // ============================================================
            // PART 4: Consume from BOTH output topics and compare
            // ============================================================
            TestContext.WriteLine("\n═══════════════════════════════════════════════════════════");
            TestContext.WriteLine("PART 4: Consuming from BOTH output topics");
            TestContext.WriteLine("═══════════════════════════════════════════════════════════");

            // Consume from Pure Flink output
            var pureFlinkOutput = await ConsumeAsync(PureFlinkOutputTopic, testMessages.Length, TimeSpan.FromSeconds(60), ct);
            TestContext.WriteLine($"📊 Pure Flink Output: Consumed {pureFlinkOutput.Count}/{testMessages.Length} messages");
            foreach (var msg in pureFlinkOutput)
            {
                TestContext.WriteLine($"   - {msg}");
            }

            // Consume from Gateway output
            var gatewayOutput = await ConsumeAsync(GatewayOutputTopic, testMessages.Length, TimeSpan.FromSeconds(60), ct);
            TestContext.WriteLine($"📊 Gateway Output: Consumed {gatewayOutput.Count}/{testMessages.Length} messages");
            foreach (var msg in gatewayOutput)
            {
                TestContext.WriteLine($"   - {msg}");
            }

            // ============================================================
            // PART 5: Analyze results and identify root cause
            // ============================================================
            TestContext.WriteLine("\n═══════════════════════════════════════════════════════════");
            TestContext.WriteLine("PART 5: ROOT CAUSE ANALYSIS");
            TestContext.WriteLine("═══════════════════════════════════════════════════════════");

            var pureFlinkWorks = pureFlinkOutput.Count == testMessages.Length;
            var gatewayWorks = gatewayOutput.Count == testMessages.Length;

            TestContext.WriteLine($"✅ Pure Flink Works: {pureFlinkWorks} ({pureFlinkOutput.Count}/{testMessages.Length})");
            TestContext.WriteLine($"✅ Gateway Works: {gatewayWorks} ({gatewayOutput.Count}/{testMessages.Length})");

            if (pureFlinkWorks && !gatewayWorks)
            {
                TestContext.WriteLine("\n🔴 ROOT CAUSE: Gateway-specific issue");
                TestContext.WriteLine("   Pure Flink job works, but Gateway job fails.");
                TestContext.WriteLine("   Problem is in Gateway job builder or submission pipeline.");
                TestContext.WriteLine("   Bootstrap servers may be modified during Gateway processing.");
            }
            else if (!pureFlinkWorks && !gatewayWorks)
            {
                TestContext.WriteLine("\n🔴 ROOT CAUSE: Kafka configuration issue");
                TestContext.WriteLine("   Both Pure Flink and Gateway jobs fail.");
                TestContext.WriteLine("   Problem is likely in Kafka bootstrap servers configuration.");
                TestContext.WriteLine("   Flink jobs may not be able to reach 'kafka:9092'.");
            }
            else if (pureFlinkWorks && gatewayWorks)
            {
                TestContext.WriteLine("\n🟢 SUCCESS: Both jobs work correctly!");
                TestContext.WriteLine("   The issue may have been resolved.");
            }
            else if (!pureFlinkWorks && gatewayWorks)
            {
                TestContext.WriteLine("\n🟡 UNEXPECTED: Gateway works but Pure Flink fails");
                TestContext.WriteLine("   This is unexpected and requires further investigation.");
            }

            // Assert that at least one works to identify the pattern
            Assert.That(pureFlinkWorks || gatewayWorks, Is.True,
                "Both Pure Flink and Gateway jobs failed - critical infrastructure issue");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"❌ Diagnostic test failed: {ex.Message}");
            throw;
        }
    }

    #region Helper Methods
    private async Task ProduceTestMessagesAsync(string topic, string[] messages, CancellationToken ct)
    {
        using var producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = KafkaConnectionString,
            EnableIdempotence = true,
            Acks = Acks.All,
            LingerMs = 5,
            BrokerAddressFamily = BrokerAddressFamily.V4,
            SecurityProtocol = SecurityProtocol.Plaintext
        })
        .SetLogHandler((_, _) => { /* Suppress logs */ })
        .SetErrorHandler((_, _) => { /* Suppress errors */ })
        .Build();

        for (int i = 0; i < messages.Length; i++)
        {
            await producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = $"k-{i % 16}",
                Value = messages[i]
            }, ct);
        }
        producer.Flush(TimeSpan.FromSeconds(10));
    }

    private Task<List<string>> ConsumeAsync(string topic, int expected, TimeSpan timeout, CancellationToken ct)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = KafkaConnectionString,
            GroupId = $"lt-diagnostic-consumer-{Guid.NewGuid()}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            BrokerAddressFamily = BrokerAddressFamily.V4,
            SecurityProtocol = SecurityProtocol.Plaintext
        };
        
        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetLogHandler((_, _) => { /* Suppress logs */ })
            .SetErrorHandler((_, _) => { /* Suppress errors */ })
            .Build();
            
        consumer.Subscribe(topic);
        
        var sw = Stopwatch.StartNew();
        var messages = new List<string>();
        
        TestContext.WriteLine($"🔍 Consuming up to {expected} messages from {topic}...");
        
        while (sw.Elapsed < timeout && messages.Count < expected && !ct.IsCancellationRequested)
        {
            var cr = consumer.Consume(TimeSpan.FromMilliseconds(200));
            if (cr?.Message?.Value is { } value)
            {
                messages.Add(value);
                TestContext.WriteLine($"   Consumed: {value}");
            }
        }
        
        consumer.Close();
        TestContext.WriteLine($"✅ Consumed {messages.Count} messages in {sw.Elapsed.TotalSeconds:F1}s");
        return Task.FromResult(messages);
    }

    private static async Task<string> WaitForJobRunningAsync(string gatewayBaseUrl, string jobId, TimeSpan timeout, CancellationToken ct)
    {
        using var http = new HttpClient();
        var sw = Stopwatch.StartNew();
        var attempt = 0;
        
        TestContext.WriteLine($"⏳ Waiting for job {jobId} to reach RUNNING state...");
        
        while (sw.Elapsed < timeout && !ct.IsCancellationRequested)
        {
            attempt++;
            var jobReady = await CheckJobStatusAsync(http, gatewayBaseUrl, jobId, attempt, ct);
            if (jobReady)
            {
                return jobId;
            }
            
            await Task.Delay(1000, ct);
        }
        
        throw new TimeoutException($"Job {jobId} did not reach RUNNING state within {timeout.TotalSeconds:F0}s");
    }

    private static async Task<bool> CheckJobStatusAsync(HttpClient http, string gatewayBaseUrl, string jobId, int attempt, CancellationToken ct)
    {
        try
        {
            var resp = await http.GetAsync($"{gatewayBaseUrl}api/v1/jobs/{jobId}/status", ct);
            if (!resp.IsSuccessStatusCode)
            {
                TestContext.WriteLine($"⏳ Attempt {attempt}: HTTP {resp.StatusCode}");
                return false;
            }

            var content = await resp.Content.ReadAsStringAsync(ct);
            return ValidateJobStatus(content, jobId, attempt);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"⏳ Attempt {attempt}: {ex.GetType().Name} - {ex.Message}");
            return false;
        }
    }

    private static bool ValidateJobStatus(string content, string jobId, int attempt)
    {
        if (content.Contains("RUNNING") || content.Contains("FINISHED"))
        {
            TestContext.WriteLine($"✅ Job {jobId} is running/finished after {attempt} attempt(s)");
            return true;
        }
        
        if (content.Contains("FAILED") || content.Contains("CANCELED"))
        {
            throw new InvalidOperationException($"Job {jobId} failed or was canceled: {content}");
        }
        
        TestContext.WriteLine($"⏳ Attempt {attempt}: Job status - {content}");
        return false;
    }
    #endregion
}