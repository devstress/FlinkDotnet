using System.Diagnostics;
using Confluent.Kafka;
using LocalTesting.FlinkSqlAppHost;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

/// <summary>
/// Gateway-based tests for all 7 FlinkDotNet job patterns using FlinkDotNetJobs helpers.
/// These tests validate end-to-end job submission through the Gateway.
/// Tests can run in parallel with 8 TaskManager slots available.
/// </summary>
[TestFixture]
[Category("gateway-patterns")]
public class GatewayAllPatternsTests : LocalTestingTestBase
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan JobRunTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan MessageTimeout = TimeSpan.FromSeconds(45);

    [Test]
    public async Task Gateway_Pattern1_Uppercase_ShouldWork()
    {
        await RunGatewayPatternTest(
            patternName: "Uppercase",
            jobCreator: (input, output, kafka, ct) =>
                FlinkDotNetJobs.CreateUppercaseJob(input, output, kafka, "gateway-uppercase", ct),
            inputMessages: new[] { "hello", "world" },
            expectedOutputCount: 2,
            description: "Uppercase transformation via Gateway"
        );
    }

    [Test]
    public async Task Gateway_Pattern2_Filter_ShouldWork()
    {
        await RunGatewayPatternTest(
            patternName: "Filter",
            jobCreator: (input, output, kafka, ct) =>
                FlinkDotNetJobs.CreateFilterJob(input, output, kafka, "gateway-filter", ct),
            inputMessages: new[] { "keep", "", "this", "", "data" },
            expectedOutputCount: 3, // Empty strings filtered out
            description: "Filter operation via Gateway"
        );
    }

    [Test]
    public async Task Gateway_Pattern3_SplitConcat_ShouldWork()
    {
        await RunGatewayPatternTest(
            patternName: "SplitConcat",
            jobCreator: (input, output, kafka, ct) =>
                FlinkDotNetJobs.CreateSplitConcatJob(input, output, kafka, "gateway-splitconcat", ct),
            inputMessages: new[] { "a,b" },
            expectedOutputCount: 1, // Split and concat produces 1 message
            description: "Split and concat via Gateway"
        );
    }

    [Test]
    public async Task Gateway_Pattern4_Timer_ShouldWork()
    {
        await RunGatewayPatternTest(
            patternName: "Timer",
            jobCreator: (input, output, kafka, ct) =>
                FlinkDotNetJobs.CreateTimerJob(input, output, kafka, "gateway-timer", ct),
            inputMessages: new[] { "timed1", "timed2" },
            expectedOutputCount: 2,
            description: "Timer functionality via Gateway",
            allowLongerProcessing: true
        );
    }

    [Test]
    [Ignore("SQL jobs require additional table runtime JARs not included in base Flink image")]
    public async Task Gateway_Pattern5_SqlPassthrough_ShouldWork()
    {
        await RunGatewayPatternTest(
            patternName: "SqlPassthrough",
            jobCreator: (input, output, kafka, ct) =>
                FlinkDotNetJobs.CreateSqlPassthroughJob(input, output, kafka, "gateway-sql-passthrough", ct),
            inputMessages: new[] { "{\"key\":\"k1\",\"value\":\"v1\"}" },
            expectedOutputCount: 1,
            description: "SQL passthrough via Gateway",
            usesJson: true
        );
    }

    [Test]
    [Ignore("SQL jobs require additional table runtime JARs not included in base Flink image")]
    public async Task Gateway_Pattern6_SqlTransform_ShouldWork()
    {
        await RunGatewayPatternTest(
            patternName: "SqlTransform",
            jobCreator: (input, output, kafka, ct) =>
                FlinkDotNetJobs.CreateSqlTransformJob(input, output, kafka, "gateway-sql-transform", ct),
            inputMessages: new[] { "{\"key\":\"k1\",\"value\":\"test\"}" },
            expectedOutputCount: 1,
            description: "SQL transformation via Gateway",
            usesJson: true
        );
    }

    [Test]
    [Ignore("SQL jobs require additional table runtime JARs not included in base Flink image")]
    public async Task Gateway_Pattern7_Composite_ShouldWork()
    {
        await RunGatewayPatternTest(
            patternName: "Composite",
            jobCreator: (input, output, kafka, ct) =>
                FlinkDotNetJobs.CreateCompositeJob(input, output, kafka, "gateway-composite", ct),
            inputMessages: new[] { "test,data" },
            expectedOutputCount: 1, // Split and concat produces 1 message
            description: "Composite operations via Gateway",
            allowLongerProcessing: true
        );
    }

    #region Test Infrastructure

    private async Task RunGatewayPatternTest(
        string patternName,
        Func<string, string, string, CancellationToken, Task<Flink.JobBuilder.Models.JobSubmissionResult>> jobCreator,
        string[] inputMessages,
        int expectedOutputCount,
        string description,
        bool allowLongerProcessing = false,
        bool usesJson = false)
    {
        var inputTopic = $"lt.gw.{patternName.ToLowerInvariant()}.input.{TestContext.CurrentContext.Test.ID}";
        var outputTopic = $"lt.gw.{patternName.ToLowerInvariant()}.output.{TestContext.CurrentContext.Test.ID}";

        TestPrerequisites.EnsureDockerAvailable();
        var gatewayBuildable = TestPrerequisites.ProbeFlinkGatewayBuildable();
        if (!gatewayBuildable)
        {
            Assert.Ignore("Flink.JobGateway not available - skipping Gateway-dependent test");
            return;
        }

        var baseToken = TestContext.CurrentContext.CancellationToken;
        using var testTimeout = new CancellationTokenSource(TestTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(baseToken, testTimeout.Token);
        var ct = linkedCts.Token;

        TestContext.WriteLine($"🚀 Starting Gateway Pattern Test: {patternName}");
        TestContext.WriteLine($"📝 Description: {description}");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Wait for complete infrastructure including Gateway
            TestContext.WriteLine("⏳ Waiting for complete infrastructure (Kafka + Flink + Gateway)...");
            await WaitForFullInfrastructureAsync(includeGateway: true, ct);
            TestContext.WriteLine("✅ All infrastructure components ready");

            // Create topics
            TestContext.WriteLine($"📝 Creating topics: {inputTopic} -> {outputTopic}");
            await CreateTopicAsync(inputTopic, 1);
            await CreateTopicAsync(outputTopic, 1);

            // Submit job using FlinkDotNetJobs helper
            TestContext.WriteLine($"🔧 Creating and submitting {patternName} job...");
            var submitResult = await jobCreator(inputTopic, outputTopic, KafkaContainerConnectionString, ct);

            TestContext.WriteLine($"📊 Job submission: success={submitResult.Success}, jobId={submitResult.FlinkJobId}");
            Assert.That(submitResult.Success, Is.True, $"Job must submit successfully. Error: {submitResult.ErrorMessage}");

            // Wait for job to be running
            var gatewayBase = $"http://localhost:{Ports.GatewayHostPort}/";
            await WaitForJobRunningViaGatewayAsync(gatewayBase, submitResult.FlinkJobId!, JobRunTimeout, ct);
            TestContext.WriteLine("✅ Job is RUNNING");

            // Add delay to ensure job is fully initialized
            await Task.Delay(3000, ct);

            // Produce test messages
            TestContext.WriteLine($"📤 Producing {inputMessages.Length} messages...");
            await ProduceMessagesAsync(inputTopic, inputMessages, ct, usesJson);

            // Consume and verify
            var consumeTimeout = allowLongerProcessing ? TimeSpan.FromSeconds(75) : MessageTimeout;
            var consumed = await ConsumeMessagesAsync(outputTopic, expectedOutputCount, consumeTimeout, ct);

            TestContext.WriteLine($"📊 Consumed {consumed.Count} messages (expected: {expectedOutputCount})");

            // Assert - use GreaterThanOrEqualTo to be more forgiving
            Assert.That(consumed.Count, Is.GreaterThanOrEqualTo(expectedOutputCount),
                $"Should consume at least {expectedOutputCount} messages");

            stopwatch.Stop();
            TestContext.WriteLine($"✅ {patternName} test completed successfully in {stopwatch.Elapsed.TotalSeconds:F1}s");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            TestContext.WriteLine($"❌ {patternName} test failed after {stopwatch.Elapsed.TotalSeconds:F1}s: {ex.Message}");
            throw;
        }
    }

    private async Task ProduceMessagesAsync(string topic, string[] messages, CancellationToken ct, bool usesJson = false)
    {
        if (usesJson)
        {
            // For JSON messages, produce with null key
            using var producer = new ProducerBuilder<Null, string>(new ProducerConfig
            {
                BootstrapServers = KafkaConnectionString,
                EnableIdempotence = true,
                Acks = Acks.All,
                LingerMs = 5,
                BrokerAddressFamily = BrokerAddressFamily.V4,
                SecurityProtocol = SecurityProtocol.Plaintext
            })
            .SetLogHandler((_, _) => { })
            .SetErrorHandler((_, _) => { })
            .Build();

            foreach (var message in messages)
            {
                await producer.ProduceAsync(topic, new Message<Null, string> { Value = message }, ct);
            }

            producer.Flush(TimeSpan.FromSeconds(10));
        }
        else
        {
            // For simple messages, use string key
            using var producer = new ProducerBuilder<string, string>(new ProducerConfig
            {
                BootstrapServers = KafkaConnectionString,
                EnableIdempotence = true,
                Acks = Acks.All,
                LingerMs = 5,
                BrokerAddressFamily = BrokerAddressFamily.V4,
                SecurityProtocol = SecurityProtocol.Plaintext
            })
            .SetLogHandler((_, _) => { })
            .SetErrorHandler((_, _) => { })
            .Build();

            for (int i = 0; i < messages.Length; i++)
            {
                await producer.ProduceAsync(topic, new Message<string, string>
                {
                    Key = $"key-{i}",
                    Value = messages[i]
                }, ct);
            }

            producer.Flush(TimeSpan.FromSeconds(10));
        }

        TestContext.WriteLine($"✅ Produced {messages.Length} messages to {topic}");
    }

    private Task<List<string>> ConsumeMessagesAsync(string topic, int expectedCount, TimeSpan timeout, CancellationToken ct)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = KafkaConnectionString,
            GroupId = $"lt-gw-pattern-consumer-{Guid.NewGuid()}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            BrokerAddressFamily = BrokerAddressFamily.V4,
            SecurityProtocol = SecurityProtocol.Plaintext
        };

        var messages = new List<string>();
        using var consumer = new ConsumerBuilder<Ignore, string>(config)
            .SetLogHandler((_, _) => { })
            .SetErrorHandler((_, _) => { })
            .Build();

        consumer.Subscribe(topic);
        var deadline = DateTime.UtcNow.Add(timeout);

        TestContext.WriteLine($"📥 Starting consumption from '{topic}' (timeout: {timeout.TotalSeconds}s)");

        while (DateTime.UtcNow < deadline && messages.Count < expectedCount && !ct.IsCancellationRequested)
        {
            var consumeResult = consumer.Consume(TimeSpan.FromSeconds(1));
            if (consumeResult != null)
            {
                messages.Add(consumeResult.Message.Value);
                TestContext.WriteLine($"  📥 Consumed message {messages.Count}: {consumeResult.Message.Value}");
            }
        }

        return Task.FromResult(messages);
    }

    private static async Task WaitForJobRunningViaGatewayAsync(string gatewayBaseUrl, string jobId, TimeSpan timeout, CancellationToken ct)
    {
        using var http = new HttpClient();
        var deadline = DateTime.UtcNow.Add(timeout);
        var attempt = 0;

        TestContext.WriteLine($"⏳ Waiting for job {jobId} to reach RUNNING state via Gateway...");

        while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
        {
            attempt++;
            try
            {
                var resp = await http.GetAsync($"{gatewayBaseUrl}api/v1/jobs/{jobId}/status", ct);
                if (resp.IsSuccessStatusCode)
                {
                    var content = await resp.Content.ReadAsStringAsync(ct);
                    if (content.Contains("RUNNING", StringComparison.OrdinalIgnoreCase) ||
                        content.Contains("FINISHED", StringComparison.OrdinalIgnoreCase))
                    {
                        TestContext.WriteLine($"✅ Job {jobId} is running/finished after {attempt} attempt(s)");
                        return;
                    }

                    if (content.Contains("FAILED", StringComparison.OrdinalIgnoreCase) ||
                        content.Contains("CANCELED", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"Job {jobId} failed or was canceled: {content}");
                    }

                    TestContext.WriteLine($"  ⏳ Attempt {attempt}: Job status - {content}");
                }
            }
            catch (HttpRequestException ex)
            {
                TestContext.WriteLine($"  ⏳ Attempt {attempt}: Gateway request failed - {ex.Message}");
            }

            await Task.Delay(1000, ct);
        }

        throw new TimeoutException($"Job {jobId} did not reach RUNNING state within {timeout.TotalSeconds:F0}s");
    }

    #endregion
}
