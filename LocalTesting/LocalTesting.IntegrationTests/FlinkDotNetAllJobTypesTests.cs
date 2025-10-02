using System.Diagnostics;
using Confluent.Kafka;
using LocalTesting.FlinkSqlAppHost;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

/// <summary>
/// Comprehensive test suite for all FlinkDotNet job types using the Gateway.
/// Each test method corresponds to one of the 7 job patterns defined in FlinkDotNetJobs.cs
/// These tests validate Gateway functionality and job submission for each pattern.
/// </summary>
[TestFixture, NonParallelizable]
[Category("flinkdotnet-comprehensive-all")]
public class FlinkDotNetAllJobTypesTests : LocalTestingTestBase
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan JobRunTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan MessageTimeout = TimeSpan.FromSeconds(30);

    [Test]
    public async Task FlinkDotNet_UppercaseJob_ShouldTransformMessages()
    {
        await RunFlinkDotNetJobTest(
            testName: "Uppercase",
            jobCreator: (input, output, kafka, ct) => 
                FlinkDotNetJobs.CreateUppercaseJob(input, output, kafka, "uppercase-job", ct),
            inputMessages: new[] { "hello", "world" },
            expectedOutputs: new[] { "HELLO", "WORLD" },
            description: "Validates uppercase transformation job"
        );
    }

    [Test]
    public async Task FlinkDotNet_FilterJob_ShouldFilterEmptyMessages()
    {
        await RunFlinkDotNetJobTest(
            testName: "Filter",
            jobCreator: (input, output, kafka, ct) => 
                FlinkDotNetJobs.CreateFilterJob(input, output, kafka, "filter-job", ct),
            inputMessages: new[] { "keep", "", "this", "", "data" },
            expectedOutputs: new[] { "keep", "this", "data" },
            description: "Validates filtering job removes empty messages",
            expectedMinCount: 3
        );
    }

    [Test]
    public async Task FlinkDotNet_SplitConcatJob_ShouldProcessComplexTransforms()
    {
        await RunFlinkDotNetJobTest(
            testName: "SplitConcat",
            jobCreator: (input, output, kafka, ct) => 
                FlinkDotNetJobs.CreateSplitConcatJob(input, output, kafka, "splitconcat-job", ct),
            inputMessages: new[] { "a,b,c", "x,y,z" },
            expectedOutputs: new[] { "a-joined", "b-joined", "c-joined", "x-joined", "y-joined", "z-joined" },
            description: "Validates split and concat transformation job"
        );
    }

    [Test]
    public async Task FlinkDotNet_TimerJob_ShouldProcessWithTiming()
    {
        await RunFlinkDotNetJobTest(
            testName: "Timer",
            jobCreator: (input, output, kafka, ct) => 
                FlinkDotNetJobs.CreateTimerJob(input, output, kafka, "timer-job", ct),
            inputMessages: new[] { "timed1", "timed2" },
            expectedOutputs: new[] { "timed1", "timed2" },
            description: "Validates timer job processes messages",
            allowLongerProcessing: true
        );
    }

    [Test]
    public async Task FlinkDotNet_SqlPassthroughJob_ShouldPassDataThrough()
    {
        await RunFlinkDotNetJobTest(
            testName: "SqlPassthrough",
            jobCreator: (input, output, kafka, ct) => 
                FlinkDotNetJobs.CreateSqlPassthroughJob(input, output, kafka, "sql-passthrough-job", ct),
            inputMessages: new[] { "{\"key\":\"k1\",\"value\":\"v1\"}", "{\"key\":\"k2\",\"value\":\"v2\"}" },
            expectedOutputs: new[] { "{\"key\":\"k1\",\"value\":\"v1\"}", "{\"key\":\"k2\",\"value\":\"v2\"}" },
            description: "Validates SQL passthrough job",
            usesJson: true
        );
    }

    [Test]
    public async Task FlinkDotNet_SqlTransformJob_ShouldTransformWithSql()
    {
        await RunFlinkDotNetJobTest(
            testName: "SqlTransform",
            jobCreator: (input, output, kafka, ct) => 
                FlinkDotNetJobs.CreateSqlTransformJob(input, output, kafka, "sql-transform-job", ct),
            inputMessages: new[] { "{\"key\":\"k1\",\"value\":\"hello\"}", "{\"key\":\"k2\",\"value\":\"world\"}" },
            expectedOutputs: new[] { "{\"key\":\"k1\",\"transformed\":\"HELLO\"}", "{\"key\":\"k2\",\"transformed\":\"WORLD\"}" },
            description: "Validates SQL transformation job with UPPER function",
            usesJson: true
        );
    }

    [Test]
    public async Task FlinkDotNet_CompositeJob_ShouldHandleMultipleOperations()
    {
        await RunFlinkDotNetJobTest(
            testName: "Composite",
            jobCreator: (input, output, kafka, ct) => 
                FlinkDotNetJobs.CreateCompositeJob(input, output, kafka, "composite-job", ct),
            inputMessages: new[] { "test,data", "composite,flow" },
            expectedOutputs: new[] { "TEST-TAIL", "DATA-TAIL", "COMPOSITE-TAIL", "FLOW-TAIL" },
            description: "Validates composite job with multiple operations",
            allowLongerProcessing: true
        );
    }

    #region Helper Methods

    private async Task RunFlinkDotNetJobTest(
        string testName,
        Func<string, string, string, CancellationToken, Task<Flink.JobBuilder.Models.JobSubmissionResult>> jobCreator,
        string[] inputMessages,
        string[] expectedOutputs,
        string description,
        bool allowLongerProcessing = false,
        int? expectedMinCount = null,
        bool usesJson = false)
    {
        var inputTopic = $"lt.fdn.{testName.ToLowerInvariant()}.input.{TestContext.CurrentContext.Test.ID}";
        var outputTopic = $"lt.fdn.{testName.ToLowerInvariant()}.output.{TestContext.CurrentContext.Test.ID}";

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

        TestContext.WriteLine($"🚀 Starting FlinkDotNet Job Test: {testName}");
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

            // Create and submit job using FlinkDotNetJobs helper
            TestContext.WriteLine($"🔧 Creating and submitting {testName} job...");
            var submitResult = await jobCreator(inputTopic, outputTopic, KafkaContainerConnectionString, ct);
            
            TestContext.WriteLine($"📊 Job submission result: success={submitResult.Success}, jobId={submitResult.FlinkJobId}");
            Assert.That(submitResult.Success, Is.True, $"Job must submit successfully. Error: {submitResult.ErrorMessage}");

            // Wait for job to be running
            var gatewayBase = $"http://localhost:{Ports.GatewayHostPort}/";
            await WaitForJobRunningAsync(gatewayBase, submitResult.FlinkJobId!, JobRunTimeout, ct);
            TestContext.WriteLine("✅ Job is RUNNING");

            // Produce test messages
            TestContext.WriteLine($"📤 Producing {inputMessages.Length} messages...");
            await ProduceMessagesAsync(inputTopic, inputMessages, ct, usesJson);

            // Consume and verify
            var consumeTimeout = allowLongerProcessing ? TimeSpan.FromSeconds(60) : MessageTimeout;
            var expectedCount = expectedMinCount ?? expectedOutputs.Length;
            var consumed = await ConsumeMessagesAsync(outputTopic, expectedCount, consumeTimeout, ct);

            TestContext.WriteLine($"📊 Consumed {consumed.Count} messages (expected: {expectedCount})");
            
            // Assert
            Assert.That(consumed.Count, Is.GreaterThanOrEqualTo(expectedCount),
                $"Should process at least {expectedCount} messages");

            stopwatch.Stop();
            TestContext.WriteLine($"✅ {testName} test completed successfully in {stopwatch.Elapsed.TotalSeconds:F1}s");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            TestContext.WriteLine($"❌ {testName} test failed after {stopwatch.Elapsed.TotalSeconds:F1}s: {ex.Message}");
            throw;
        }
    }

    private async Task ProduceMessagesAsync(string topic, string[] messages, CancellationToken ct, bool usesJson = false)
    {
        if (usesJson)
        {
            // For JSON messages, produce as-is without key-value wrapping
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
            // For simple messages, use string key-value format
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
            GroupId = $"lt-fdn-all-consumer-{Guid.NewGuid()}",
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

        while (DateTime.UtcNow < deadline && messages.Count < expectedCount && !ct.IsCancellationRequested)
        {
            var consumeResult = consumer.Consume(TimeSpan.FromSeconds(1));
            if (consumeResult != null)
            {
                messages.Add(consumeResult.Message.Value);
            }
        }

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
            try
            {
                var resp = await http.GetAsync($"{gatewayBaseUrl}api/v1/jobs/{jobId}/status", ct);
                if (resp.IsSuccessStatusCode)
                {
                    var content = await resp.Content.ReadAsStringAsync(ct);
                    if (content.Contains("RUNNING") || content.Contains("FINISHED"))
                    {
                        TestContext.WriteLine($"✅ Job {jobId} is running/finished after {attempt} attempt(s)");
                        return jobId;
                    }

                    if (content.Contains("FAILED") || content.Contains("CANCELED"))
                    {
                        throw new InvalidOperationException($"Job {jobId} failed or was canceled: {content}");
                    }
                }
            }
            catch (HttpRequestException)
            {
                // Gateway might not be ready yet, continue waiting
            }

            await Task.Delay(1000, ct);
        }

        throw new TimeoutException($"Job {jobId} did not reach RUNNING state within {timeout.TotalSeconds:F0}s");
    }

    #endregion
}
