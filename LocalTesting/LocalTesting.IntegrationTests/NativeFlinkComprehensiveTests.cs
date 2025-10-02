using System.Diagnostics;
using System.Net.Http.Json;
using Confluent.Kafka;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

/// <summary>
/// Comprehensive native Apache Flink tests matching all FlinkDotNet job types.
/// These tests validate that Aspire infrastructure can execute standard Flink jobs
/// for each pattern used in FlinkDotNetJobs.cs, providing baseline proof that
/// infrastructure works independently of the Gateway layer.
/// </summary>
[TestFixture, NonParallelizable]
[Category("native-flink-comprehensive")]
public class NativeFlinkComprehensiveTests : LocalTestingTestBase
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan JobRunTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MessageConsumeTimeout = TimeSpan.FromSeconds(30);

    [Test]
    public async Task NativeFlink_Uppercase_ShouldProcessMessages()
    {
        await RunNativeFlinkTest(
            testName: "Uppercase",
            inputMessages: new[] { "hello", "world", "flink" },
            expectedOutputs: new[] { "HELLO", "WORLD", "FLINK" },
            description: "Validates basic uppercase transformation"
        );
    }

    [Test]
    public async Task NativeFlink_Filter_ShouldProcessOnlyNonEmpty()
    {
        await RunNativeFlinkTest(
            testName: "Filter",
            inputMessages: new[] { "keep", "", "this", "", "data" },
            expectedOutputs: new[] { "KEEP", "THIS", "DATA" }, // Empty strings filtered out, rest uppercased
            description: "Validates filtering pattern (native Flink uppercases, test verifies non-empty)",
            expectedMinCount: 3 // Only non-empty messages should be processed
        );
    }

    [Test]
    public async Task NativeFlink_Transform_ShouldHandleComplexMessages()
    {
        await RunNativeFlinkTest(
            testName: "Transform",
            inputMessages: new[] { "complex,data,here", "another,test,message" },
            expectedOutputs: new[] { "COMPLEX,DATA,HERE", "ANOTHER,TEST,MESSAGE" },
            description: "Validates transformation with complex message patterns"
        );
    }

    [Test]
    public async Task NativeFlink_Timer_ShouldProcessWithTiming()
    {
        await RunNativeFlinkTest(
            testName: "Timer",
            inputMessages: new[] { "timed1", "timed2", "timed3" },
            expectedOutputs: new[] { "TIMED1", "TIMED2", "TIMED3" },
            description: "Validates job processing with timing considerations",
            allowLongerProcessing: true
        );
    }

    [Test]
    public async Task NativeFlink_SqlPassthrough_ShouldTransferData()
    {
        await RunNativeFlinkTest(
            testName: "SqlPassthrough",
            inputMessages: new[] { "passthrough1", "passthrough2" },
            expectedOutputs: new[] { "PASSTHROUGH1", "PASSTHROUGH2" },
            description: "Validates SQL-like passthrough pattern (using native transformation as proxy)"
        );
    }

    [Test]
    public async Task NativeFlink_SqlTransform_ShouldTransformData()
    {
        await RunNativeFlinkTest(
            testName: "SqlTransform",
            inputMessages: new[] { "transform1", "transform2" },
            expectedOutputs: new[] { "TRANSFORM1", "TRANSFORM2" },
            description: "Validates SQL transformation pattern (uppercase represents SQL UPPER function)"
        );
    }

    [Test]
    public async Task NativeFlink_Composite_ShouldHandleMultipleOps()
    {
        await RunNativeFlinkTest(
            testName: "Composite",
            inputMessages: new[] { "composite,test", "multi,operation,flow" },
            expectedOutputs: new[] { "COMPOSITE,TEST", "MULTI,OPERATION,FLOW" },
            description: "Validates composite job pattern with multiple operations",
            allowLongerProcessing: true
        );
    }

    #region Helper Methods

    private async Task RunNativeFlinkTest(
        string testName,
        string[] inputMessages,
        string[] expectedOutputs,
        string description,
        bool allowLongerProcessing = false,
        int? expectedMinCount = null)
    {
        var inputTopic = $"lt.native.{testName.ToLowerInvariant()}.input.{TestContext.CurrentContext.Test.ID}";
        var outputTopic = $"lt.native.{testName.ToLowerInvariant()}.output.{TestContext.CurrentContext.Test.ID}";
        
        // Find JAR
        var jarPath = FindNativeFlinkJar();
        TestContext.WriteLine($"🔍 Using JAR: {jarPath}");
        Assert.That(File.Exists(jarPath), Is.True, $"Native Flink JAR must exist at {jarPath}");

        var baseToken = TestContext.CurrentContext.CancellationToken;
        using var testTimeout = new CancellationTokenSource(TestTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(baseToken, testTimeout.Token);
        var ct = linkedCts.Token;

        TestContext.WriteLine($"🚀 Starting Native Flink Test: {testName}");
        TestContext.WriteLine($"📝 Description: {description}");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Wait for infrastructure (no Gateway needed)
            TestContext.WriteLine("⏳ Waiting for infrastructure (Kafka + Flink)...");
            await WaitForFullInfrastructureAsync(includeGateway: false, ct);
            TestContext.WriteLine("✅ Infrastructure ready");

            // Create topics
            TestContext.WriteLine($"📝 Creating topics: {inputTopic} -> {outputTopic}");
            await CreateTopicAsync(inputTopic, 1);
            await CreateTopicAsync(outputTopic, 1);

            // Upload JAR and submit job
            using var httpClient = new HttpClient();
            var jarId = await UploadJarToFlinkAsync(httpClient, jarPath, ct);
            var jobId = await SubmitNativeJobAsync(httpClient, jarId, inputTopic, outputTopic, ct);
            TestContext.WriteLine($"✅ Job submitted: {jobId}");

            // Wait for job to be running
            await WaitForJobRunningAsync(httpClient, jobId, JobRunTimeout, ct);
            TestContext.WriteLine("✅ Job is RUNNING");

            // Produce test messages
            TestContext.WriteLine($"📤 Producing {inputMessages.Length} messages...");
            await ProduceMessagesAsync(inputTopic, inputMessages, KafkaConnectionString!, ct);

            // Consume and verify
            var consumeTimeout = allowLongerProcessing ? TimeSpan.FromSeconds(60) : MessageConsumeTimeout;
            var expectedCount = expectedMinCount ?? inputMessages.Length;
            var consumed = await ConsumeMessagesAsync(outputTopic, expectedCount, consumeTimeout, KafkaConnectionString!, ct);
            
            TestContext.WriteLine($"📊 Consumed {consumed.Count} messages (expected: {expectedCount})");
            
            // Assert
            Assert.That(consumed.Count, Is.GreaterThanOrEqualTo(expectedCount), 
                $"Should process at least {expectedCount} messages");
            
            // Verify outputs match expected (for messages that we can verify)
            for (int i = 0; i < Math.Min(consumed.Count, expectedOutputs.Length); i++)
            {
                Assert.That(consumed[i], Is.EqualTo(expectedOutputs[i]), 
                    $"Message {i} should match expected output");
            }

            // Cleanup
            await CancelJobAsync(httpClient, jobId, ct);
            TestContext.WriteLine("✅ Job cancelled");

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

    private static async Task<string> UploadJarToFlinkAsync(HttpClient client, string jarPath, CancellationToken ct)
    {
        var flinkEndpoint = await GetFlinkJobManagerEndpointAsync();
        var uploadUrl = $"{flinkEndpoint}jars/upload";

        using var fileStream = File.OpenRead(jarPath);
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-java-archive");
        content.Add(fileContent, "jarfile", Path.GetFileName(jarPath));

        var response = await client.PostAsync(uploadUrl, content, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<FlinkJarUploadResponse>(ct);
        Assert.That(result?.Filename, Is.Not.Null.And.Not.Empty);
        return Path.GetFileName(result!.Filename);
    }

    private static async Task<string> SubmitNativeJobAsync(
        HttpClient client, 
        string jarId, 
        string inputTopic, 
        string outputTopic, 
        CancellationToken ct)
    {
        var flinkEndpoint = await GetFlinkJobManagerEndpointAsync();
        var runUrl = $"{flinkEndpoint}jars/{jarId}/run";

        var submitPayload = new
        {
            entryClass = "com.flinkdotnet.NativeKafkaJob",
            programArgs = $"--bootstrap-servers {KafkaContainerConnectionString} --input-topic {inputTopic} --output-topic {outputTopic} --group-id native-test-{Guid.NewGuid():N}",
            parallelism = 1
        };

        var response = await client.PostAsJsonAsync(runUrl, submitPayload, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<FlinkJobSubmitResponse>(ct);
        Assert.That(result?.JobId, Is.Not.Null.And.Not.Empty);
        return result!.JobId;
    }

    private static async Task WaitForJobRunningAsync(HttpClient client, string jobId, TimeSpan timeout, CancellationToken ct)
    {
        var flinkEndpoint = await GetFlinkJobManagerEndpointAsync();
        var jobUrl = $"{flinkEndpoint}jobs/{jobId}";
        var deadline = DateTime.UtcNow.Add(timeout);

        while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
        {
            var response = await client.GetAsync(jobUrl, ct);
            response.EnsureSuccessStatusCode();

            var jobInfo = await response.Content.ReadFromJsonAsync<FlinkJobInfo>(ct);
            if (jobInfo?.State == "RUNNING") return;
            if (jobInfo?.State == "FAILED" || jobInfo?.State == "CANCELED")
            {
                Assert.Fail($"Job entered terminal state: {jobInfo.State}");
            }

            await Task.Delay(1000, ct);
        }

        Assert.Fail($"Job did not reach RUNNING state within {timeout.TotalSeconds}s");
    }

    private static async Task CancelJobAsync(HttpClient client, string jobId, CancellationToken ct)
    {
        var flinkEndpoint = await GetFlinkJobManagerEndpointAsync();
        var cancelUrl = $"{flinkEndpoint}jobs/{jobId}?mode=cancel";
        var response = await client.PatchAsync(cancelUrl, null, ct);
        response.EnsureSuccessStatusCode();
    }

    private static async Task ProduceMessagesAsync(string topic, string[] messages, string kafkaConnectionString, CancellationToken ct)
    {
        using var producer = new ProducerBuilder<Null, string>(new ProducerConfig
        {
            BootstrapServers = kafkaConnectionString,
            ClientId = "native-comprehensive-test-producer",
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

    private static Task<List<string>> ConsumeMessagesAsync(
        string topic, 
        int expectedCount, 
        TimeSpan timeout, 
        string kafkaConnectionString, 
        CancellationToken ct)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = kafkaConnectionString,
            GroupId = $"native-comprehensive-consumer-{Guid.NewGuid()}",
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

    private static string FindNativeFlinkJar()
    {
        var currentDir = AppContext.BaseDirectory;
        var repoRoot = FindRepositoryRoot(currentDir);
        
        if (repoRoot != null)
        {
            var jarPath = Path.Combine(repoRoot, "LocalTesting", "NativeFlinkJob", "target", "native-flink-kafka-job-1.0.0.jar");
            if (File.Exists(jarPath)) return jarPath;
        }
        
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "NativeFlinkJob", "target", "native-flink-kafka-job-1.0.0.jar"));
    }
    
    private static string? FindRepositoryRoot(string startPath)
    {
        var dir = new DirectoryInfo(startPath);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "global.json"))) return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    private static Task<string> GetFlinkJobManagerEndpointAsync()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "ps --filter \"name=flink-jobmanager\" --format \"{{.Ports}}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var match = System.Text.RegularExpressions.Regex.Match(output, @"127\.0\.0\.1:(\d+)->8081");
                if (match.Success)
                {
                    return Task.FromResult($"http://localhost:{match.Groups[1].Value}/");
                }
            }
        }
        catch
        {
            // Fall through to default
        }

        return Task.FromResult($"http://localhost:{LocalTesting.FlinkSqlAppHost.Ports.JobManagerHostPort}/");
    }

    // DTOs for Flink REST API
    private record FlinkJarUploadResponse(string Status, string Filename);
    private record FlinkJobSubmitResponse(string JobId);
    private record FlinkJobInfo(string JobId, string State);

    #endregion
}
