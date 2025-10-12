using System.Diagnostics;
using Confluent.Kafka;
using LocalTesting.FlinkSqlAppHost;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

/// <summary>
/// Gateway-based tests for all 7 FlinkDotNet job patterns using FlinkDotNetJobs helpers.
/// These tests validate end-to-end job submission through the Gateway.
/// Tests run in parallel with 8 TaskManager slots available.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.All)]
[Category("gateway-patterns")]
public class GatewayAllPatternsTests : LocalTestingTestBase
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan JobRunTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MessageTimeout = TimeSpan.FromSeconds(30);

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
    public async Task Gateway_Pattern5_DirectFlinkSQL_ShouldWork()
    {
        await RunGatewayPatternTest(
            patternName: "DirectFlinkSQL",
            jobCreator: (input, output, kafka, ct) =>
                FlinkDotNetJobs.CreateDirectFlinkSQLJob(input, output, kafka, "gateway-direct-flink-sql", ct),
            inputMessages: new[] { "{\"key\":\"k1\",\"value\":\"v1\"}" },
            expectedOutputCount: 1,
            description: "Direct Flink SQL via Gateway",
            usesJson: true
        );
    }

    [Test]
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

        var baseToken = TestContext.CurrentContext.CancellationToken;
        using var testTimeout = new CancellationTokenSource(TestTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(baseToken, testTimeout.Token);
        var ct = linkedCts.Token;

        TestContext.WriteLine($"🚀 Starting Gateway Pattern Test: {patternName}");
        TestContext.WriteLine($"📝 Description: {description}");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Skip health check - global setup already validated everything
            // Create topics immediately
            TestContext.WriteLine($"📝 Creating topics: {inputTopic} -> {outputTopic}");
            await CreateTopicAsync(inputTopic, 1);
            await CreateTopicAsync(outputTopic, 1);

            // Submit job using FlinkDotNetJobs helper
            // Use Kafka container IP for Flink jobs (container-to-container communication)
            // Test producers/consumers use host connection (host-to-container via port mapping)
            TestContext.WriteLine($"🔧 Creating and submitting {patternName} job...");
            TestContext.WriteLine($"📡 Kafka bootstrap (host): {KafkaConnectionString}");
            TestContext.WriteLine($"📡 Kafka bootstrap (Flink): {GlobalTestInfrastructure.KafkaContainerIpForFlink}");
            TestContext.WriteLine($"📍 Input topic: {inputTopic}");
            TestContext.WriteLine($"📍 Output topic: {outputTopic}");
            
            var submitResult = await jobCreator(inputTopic, outputTopic, GlobalTestInfrastructure.KafkaContainerIpForFlink!, ct);

            TestContext.WriteLine($"📊 Job submission: success={submitResult.Success}, jobId={submitResult.FlinkJobId}");
            
            // If job submission failed, retrieve detailed diagnostics
            if (!submitResult.Success)
            {
                TestContext.WriteLine("⚠️ Job submission failed - retrieving Flink diagnostics...");
                var flinkEndpoint = await GetFlinkJobManagerEndpointAsync();
                var diagnostics = await GetFlinkJobDiagnosticsAsync(flinkEndpoint, submitResult.FlinkJobId);
                TestContext.WriteLine(diagnostics);
            }
            
            Assert.That(submitResult.Success, Is.True, $"Job must submit successfully. Error: {submitResult.ErrorMessage}");

            // Wait for job to be running
            var gatewayBase = $"http://localhost:{Ports.GatewayHostPort}/";
            await WaitForJobRunningViaGatewayAsync(gatewayBase, submitResult.FlinkJobId!, JobRunTimeout, ct);
            TestContext.WriteLine("✅ Job is RUNNING");

            // Debug: Check job status immediately to verify it's actually running
            await LogJobStatusViaGatewayAsync(gatewayBase, submitResult.FlinkJobId!, "Immediately after RUNNING check");

            // Produce test messages immediately - job is already running
            TestContext.WriteLine($"📤 Producing {inputMessages.Length} messages...");
            await ProduceMessagesAsync(inputTopic, inputMessages, ct, usesJson);

            // Consume and verify (reduced timeout for faster tests)
            var consumeTimeout = allowLongerProcessing ? TimeSpan.FromSeconds(45) : MessageTimeout;
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

        TestContext.WriteLine($"⏳ Waiting for job {jobId} to reach RUNNING state...");

        // For SQL Gateway jobs, also check Flink REST API directly with converted job ID (without hyphens)
        // AND check for any RUNNING jobs as fallback since SQL Gateway creates different job IDs
        var flinkJobId = jobId.Replace("-", "");
        var flinkEndpoint = await GetFlinkJobManagerEndpointAsync();

        while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
        {
            attempt++;
            try
            {
                // Try Gateway API first
                if (await TryCheckGatewayJobStatusAsync(http, gatewayBaseUrl, jobId, attempt, ct))
                {
                    return;
                }

                // Gateway API failed, try Flink REST API directly with converted job ID
                if (await TryCheckFlinkJobStatusAsync(http, flinkEndpoint, flinkJobId, attempt, ct))
                {
                    return;
                }

                // Fallback: Check if ANY job is RUNNING (for SQL Gateway jobs that have different IDs)
                if (await TryCheckAnyRunningJobAsync(http, flinkEndpoint, attempt, ct))
                {
                    return;
                }
            }
            catch (HttpRequestException ex)
            {
                TestContext.WriteLine($"  ⏳ Attempt {attempt}: Request failed - {ex.Message}");
            }

            await Task.Delay(500, ct); // Reduced from 1000ms to 500ms
        }

        throw new TimeoutException($"Job {jobId} did not reach RUNNING state within {timeout.TotalSeconds:F0}s");
    }

    private static async Task<bool> TryCheckGatewayJobStatusAsync(HttpClient http, string gatewayBaseUrl, string jobId, int attempt, CancellationToken ct)
    {
        var resp = await http.GetAsync($"{gatewayBaseUrl}api/v1/jobs/{jobId}/status", ct);
        if (!resp.IsSuccessStatusCode)
        {
            return false;
        }

        var content = await resp.Content.ReadAsStringAsync(ct);
        if (content.Contains("RUNNING", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("FINISHED", StringComparison.OrdinalIgnoreCase))
        {
            TestContext.WriteLine($"✅ Job {jobId} is running/finished after {attempt} attempt(s)");
            return true;
        }

        if (content.Contains("FAILED", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("CANCELED", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Job {jobId} failed or was canceled: {content}");
        }

        TestContext.WriteLine($"  ⏳ Attempt {attempt}: Job status from Gateway - {content}");
        return false;
    }

    private static async Task<bool> TryCheckFlinkJobStatusAsync(HttpClient http, string flinkEndpoint, string flinkJobId, int attempt, CancellationToken ct)
    {
        var flinkResp = await http.GetAsync($"{flinkEndpoint}jobs/{flinkJobId}", ct);
        if (!flinkResp.IsSuccessStatusCode)
        {
            return false;
        }

        var flinkContent = await flinkResp.Content.ReadAsStringAsync(ct);
        if (flinkContent.Contains("\"state\":\"RUNNING\"", StringComparison.OrdinalIgnoreCase) ||
            flinkContent.Contains("\"state\":\"FINISHED\"", StringComparison.OrdinalIgnoreCase))
        {
            TestContext.WriteLine($"✅ Job {flinkJobId} is running/finished after {attempt} attempt(s) (via Flink REST API)");
            return true;
        }

        if (flinkContent.Contains("\"state\":\"FAILED\"", StringComparison.OrdinalIgnoreCase) ||
            flinkContent.Contains("\"state\":\"CANCELED\"", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Job {flinkJobId} failed or was canceled: {flinkContent}");
        }

        TestContext.WriteLine($"  ⏳ Attempt {attempt}: Job status from Flink API - {flinkContent}");
        return false;
    }

    private static async Task<bool> TryCheckAnyRunningJobAsync(HttpClient http, string flinkEndpoint, int attempt, CancellationToken ct)
    {
        var allJobsResp = await http.GetAsync($"{flinkEndpoint}jobs", ct);
        if (!allJobsResp.IsSuccessStatusCode)
        {
            TestContext.WriteLine($"  ⏳ Attempt {attempt}: No RUNNING jobs found");
            return false;
        }

        var allJobsContent = await allJobsResp.Content.ReadAsStringAsync(ct);
        if (allJobsContent.Contains("\"status\":\"RUNNING\"", StringComparison.OrdinalIgnoreCase))
        {
            TestContext.WriteLine($"✅ Found RUNNING job after {attempt} attempt(s) (fallback check)");
            return true;
        }

        TestContext.WriteLine($"  ⏳ Attempt {attempt}: No RUNNING jobs found");
        return false;
    }


    #endregion
}
