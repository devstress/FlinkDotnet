using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using Confluent.Kafka;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

/// <summary>
/// Native Apache Flink test to validate Aspire infrastructure independently of the Gateway.
/// Runs a basic native Flink job to prove the infrastructure works correctly.
/// Tests run in parallel with 8 TaskManager slots available.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.All)]
[Category("native-flink-patterns")]
public class NativeFlinkAllPatternsTests : LocalTestingTestBase
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan JobRunTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ConsumeTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Pattern 1: Uppercase transformation
    /// Validates basic map operation (input -> uppercase -> output)
    /// This single test proves that native Apache Flink jobs work correctly through the infrastructure.
    /// NOTE: Currently ignored - use Gateway pattern tests instead for production workflows.
    /// </summary>
    [Test]
    public async Task Pattern1_Uppercase_ShouldTransformMessages()
    {
        await RunNativeFlinkPattern(
            patternName: "Uppercase",
            inputMessages: ["hello", "world"],
            expectedOutputs: ["HELLO", "WORLD"],
            description: "Basic uppercase transformation"
        );
    }

    #region Test Infrastructure

    [SuppressMessage("SonarQube", "S4040:Strings should be normalized to uppercase", Justification = "Kafka topic names must be lowercase")]
    private async Task RunNativeFlinkPattern(
        string patternName,
        string[] inputMessages,
        string[] expectedOutputs,
        string description,
        bool allowLongerProcessing = false)
    {
        // Kafka topic names must be lowercase, so ToLowerInvariant is correct here
        string inputTopic = $"lt.pattern.{patternName.ToLowerInvariant()}.input.{TestContext.CurrentContext.Test.ID}";
        string outputTopic = $"lt.pattern.{patternName.ToLowerInvariant()}.output.{TestContext.CurrentContext.Test.ID}";

        // Find and verify JAR exists
        string jarPath = FindNativeFlinkJar();
        TestContext.WriteLine($"🔍 Using JAR: {jarPath}");
        Assert.That(File.Exists(jarPath), Is.True, $"Native Flink JAR must exist at {jarPath}");

        CancellationToken baseToken = TestContext.CurrentContext.CancellationToken;
        using CancellationTokenSource testTimeout = new CancellationTokenSource(TestTimeout);
        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(baseToken, testTimeout.Token);
        CancellationToken ct = linkedCts.Token;

        TestContext.WriteLine($"🚀 Starting Native Flink Pattern Test: {patternName}");
        TestContext.WriteLine($"📝 Description: {description}");
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            // Skip health check - global setup already validated everything
            // Create topics immediately
            TestContext.WriteLine($"📝 Creating topics: {inputTopic} -> {outputTopic}");
            await CreateTopicAsync(inputTopic, 1);
            await CreateTopicAsync(outputTopic, 1);

            // Upload JAR and submit job
            using HttpClient httpClient = new HttpClient();
            string jarId = await UploadJarToFlinkAsync(httpClient, jarPath, ct);
            string jobId = await SubmitNativeJobAsync(httpClient, jarId, inputTopic, outputTopic, ct);
            TestContext.WriteLine($"✅ Job submitted: {jobId}");

            // Wait for job to be running
            await WaitForJobRunningAsync(httpClient, jobId, JobRunTimeout, ct);
            TestContext.WriteLine("✅ Job is RUNNING");

            // Produce test messages immediately - job is already running
            TestContext.WriteLine($"📤 Producing {inputMessages.Length} messages...");
            await ProduceMessagesAsync(inputTopic, inputMessages, KafkaConnectionString!, ct);

            // Consume and verify
            TimeSpan consumeTimeout = allowLongerProcessing ? TimeSpan.FromSeconds(60) : ConsumeTimeout;
            List<string> consumed = await ConsumeMessagesAsync(outputTopic, expectedOutputs.Length, consumeTimeout, KafkaConnectionString!, ct);

            TestContext.WriteLine($"📊 Consumed {consumed.Count} messages (expected: {expectedOutputs.Length})");

            // Assert
            Assert.That(consumed.Count, Is.EqualTo(expectedOutputs.Length),
                $"Should consume exactly {expectedOutputs.Length} messages");

            for (int i = 0; i < expectedOutputs.Length; i++)
            {
                Assert.That(consumed[i], Is.EqualTo(expectedOutputs[i]),
                    $"Message {i} should match expected output");
            }

            // Cleanup
            await CancelJobAsync(httpClient, jobId, ct);
            TestContext.WriteLine("✅ Job cancelled");

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

    private static async Task<string> UploadJarToFlinkAsync(HttpClient client, string jarPath, CancellationToken ct)
    {
        string flinkEndpoint = await GetFlinkJobManagerEndpointAsync();
        string uploadUrl = $"{flinkEndpoint}jars/upload";

        using FileStream fileStream = File.OpenRead(jarPath);
        using MultipartFormDataContent content = new MultipartFormDataContent();
        using StreamContent fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-java-archive");
        content.Add(fileContent, "jarfile", Path.GetFileName(jarPath));

        HttpResponseMessage response = await client.PostAsync(uploadUrl, content, ct);
        response.EnsureSuccessStatusCode();

        FlinkJarUploadResponse? result = await response.Content.ReadFromJsonAsync<FlinkJarUploadResponse>(ct);
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
        string flinkEndpoint = await GetFlinkJobManagerEndpointAsync();
        string runUrl = $"{flinkEndpoint}jars/{jarId}/run";

        // Use dynamically discovered Kafka container IP for Flink job connectivity
        // Docker bridge network doesn't support DNS between containers
        string? kafkaBootstrap = GlobalTestInfrastructure.KafkaContainerIpForFlink;
        var submitPayload = new
        {
            entryClass = "com.flinkdotnet.NativeKafkaJob",
            programArgsList = new[]
            {
                "--bootstrap-servers", kafkaBootstrap,
                "--input-topic", inputTopic,
                "--output-topic", outputTopic,
                "--group-id", $"native-pattern-test-{Guid.NewGuid():N}"
            },
            parallelism = 1
        };

        HttpResponseMessage response = await client.PostAsJsonAsync(runUrl, submitPayload, ct);
        response.EnsureSuccessStatusCode();

        FlinkJobSubmitResponse? result = await response.Content.ReadFromJsonAsync<FlinkJobSubmitResponse>(ct);
        Assert.That(result?.JobId, Is.Not.Null.And.Not.Empty);
        return result!.JobId;
    }

    private static async Task WaitForJobRunningAsync(HttpClient client, string jobId, TimeSpan timeout, CancellationToken ct)
    {
        string flinkEndpoint = await GetFlinkJobManagerEndpointAsync();
        string jobUrl = $"{flinkEndpoint}jobs/{jobId}";
        DateTime deadline = DateTime.UtcNow.Add(timeout);

        while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
        {
            HttpResponseMessage response = await client.GetAsync(jobUrl, ct);
            response.EnsureSuccessStatusCode();

            FlinkJobInfo? jobInfo = await response.Content.ReadFromJsonAsync<FlinkJobInfo>(ct);
            if (jobInfo?.State == "RUNNING")
                return;
            if (jobInfo?.State == "FAILED" || jobInfo?.State == "CANCELED")
            {
                Assert.Fail($"Job entered terminal state: {jobInfo.State}");
            }

            await Task.Delay(500, ct); // Reduced from 1000ms to 500ms
        }

        Assert.Fail($"Job did not reach RUNNING state within {timeout.TotalSeconds}s");
    }

    private static async Task CancelJobAsync(HttpClient client, string jobId, CancellationToken ct)
    {
        string flinkEndpoint = await GetFlinkJobManagerEndpointAsync();
        string cancelUrl = $"{flinkEndpoint}jobs/{jobId}?mode=cancel";
        HttpResponseMessage response = await client.PatchAsync(cancelUrl, null, ct);
        response.EnsureSuccessStatusCode();
    }

    private static async Task ProduceMessagesAsync(string topic, string[] messages, string kafkaConnectionString, CancellationToken ct)
    {
        using IProducer<Null, string> producer = new ProducerBuilder<Null, string>(new ProducerConfig
        {
            BootstrapServers = kafkaConnectionString,
            ClientId = "native-pattern-test-producer",
            BrokerAddressFamily = BrokerAddressFamily.V4,
            SecurityProtocol = SecurityProtocol.Plaintext
        })
        .SetLogHandler((_, _) => { })
        .SetErrorHandler((_, _) => { })
        .Build();

        foreach (string message in messages)
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
        ConsumerConfig config = new ConsumerConfig
        {
            BootstrapServers = kafkaConnectionString,
            GroupId = $"native-pattern-consumer-{Guid.NewGuid()}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            BrokerAddressFamily = BrokerAddressFamily.V4,
            SecurityProtocol = SecurityProtocol.Plaintext
        };

        List<string> messages = new List<string>();
        using IConsumer<Ignore, string> consumer = new ConsumerBuilder<Ignore, string>(config)
            .SetLogHandler((_, _) => { })
            .SetErrorHandler((_, _) => { })
            .Build();

        consumer.Subscribe(topic);
        DateTime deadline = DateTime.UtcNow.Add(timeout);

        while (DateTime.UtcNow < deadline && messages.Count < expectedCount && !ct.IsCancellationRequested)
        {
            ConsumeResult<Ignore, string> consumeResult = consumer.Consume(TimeSpan.FromSeconds(1));
            if (consumeResult != null)
            {
                messages.Add(consumeResult.Message.Value);
            }
        }

        return Task.FromResult(messages);
    }

    private static string FindNativeFlinkJar()
    {
        string currentDir = AppContext.BaseDirectory;
        string? repoRoot = FindRepositoryRoot(currentDir);

        if (repoRoot != null)
        {
            string jarPath = Path.Combine(repoRoot, "LocalTesting", "NativeFlinkJob", "target", "native-flink-kafka-job-1.0.0.jar");
            if (File.Exists(jarPath))
                return jarPath;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "NativeFlinkJob", "target", "native-flink-kafka-job-1.0.0.jar"));
    }

    private static string? FindRepositoryRoot(string startPath)
    {
        DirectoryInfo? dir = new DirectoryInfo(startPath);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "global.json")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    private static new Task<string> GetFlinkJobManagerEndpointAsync()
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "ps --filter \"name=flink-jobmanager\" --format \"{{.Ports}}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process? process = Process.Start(psi);
            if (process != null)
            {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(output, @"127\.0\.0\.1:(\d+)->8081");
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

        throw new InvalidOperationException("Could not discover Flink JobManager host port from docker ps output.");
    }

    // DTOs for Flink REST API
    private record FlinkJarUploadResponse(string Status, string Filename);
    private record FlinkJobSubmitResponse(string JobId);
    private record FlinkJobInfo(string JobId, string State);

    #endregion
}
