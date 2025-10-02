using System.Diagnostics;
using System.Net.Http.Json;
using Confluent.Kafka;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

/// <summary>
/// Integration tests for native Apache Flink jobs to validate infrastructure.
/// This proves the Aspire setup works with standard Flink patterns before debugging Gateway issues.
/// </summary>
[TestFixture, NonParallelizable]
[Category("native-flink")]
public class NativeFlinkJobTests : LocalTestingTestBase
{
    // Use unique topic names per test run to prevent conflicts when tests run together
    private static string InputTopic => $"lt.native.input.{TestContext.CurrentContext.Test.ID}";
    private static string OutputTopic => $"lt.native.output.{TestContext.CurrentContext.Test.ID}";

    [Test]
    public async Task NativeFlinkJob_Should_ProcessMessagesSuccessfully()
    {
        // Arrange - Find and verify JAR exists
        var jarPath = FindNativeFlinkJar();
        TestContext.WriteLine($"Looking for JAR at: {jarPath}");
        Assert.That(File.Exists(jarPath), Is.True, $"Native Flink JAR must exist at {jarPath}. Run 'mvn clean package' in NativeFlinkJob directory first.");

        var baseToken = TestContext.CurrentContext.CancellationToken;
        using var testTimeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(baseToken, testTimeout.Token);
        var ct = linkedCts.Token;

        TestContext.WriteLine("🚀 Starting Native Flink Job Infrastructure Validation Test");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Wait for complete infrastructure
            TestContext.WriteLine("⏳ Waiting for complete infrastructure (Kafka + Flink)...");
            await WaitForFullInfrastructureAsync(includeGateway: false, ct); // No Gateway needed for native job
            TestContext.WriteLine("✅ All infrastructure components are ready");

            // Create topics
            TestContext.WriteLine($"📝 Creating Kafka topics: {InputTopic} -> {OutputTopic}");
            await CreateTopicAsync(InputTopic, 1);
            await CreateTopicAsync(OutputTopic, 1);

            // Upload JAR to Flink
            using var httpClient = new HttpClient();
            var jarId = await UploadJarToFlinkAsync(httpClient, jarPath, ct);
            TestContext.WriteLine($"✅ JAR uploaded to Flink with ID: {jarId}");

            // Submit job to Flink
            var jobId = await SubmitNativeJobAsync(httpClient, jarId, ct);
            TestContext.WriteLine($"✅ Job submitted to Flink with ID: {jobId}");

            // Wait for job to be running
            await WaitForJobRunningAsync(httpClient, jobId, timeout: TimeSpan.FromSeconds(30), ct);
            TestContext.WriteLine($"✅ Job is RUNNING");

            // Produce test messages
            var testMessages = new[] { "hello", "world", "flink", "test" };
            await ProduceMessagesAsync(InputTopic, testMessages, KafkaConnectionString!, ct);
            TestContext.WriteLine($"✅ Produced {testMessages.Length} messages to {InputTopic}");

            // Consume and verify transformed messages
            var consumedMessages = await ConsumeMessagesAsync(OutputTopic, expectedCount: testMessages.Length, timeout: TimeSpan.FromSeconds(30), KafkaConnectionString!, ct);
            TestContext.WriteLine($"✅ Consumed {consumedMessages.Count} messages from {OutputTopic}");

            // Assert - Verify all messages were transformed to uppercase
            Assert.That(consumedMessages, Has.Count.EqualTo(testMessages.Length), "All messages should be processed");
            foreach (var (expected, actual) in testMessages.Zip(consumedMessages))
            {
                Assert.That(actual, Is.EqualTo(expected.ToUpperInvariant()), $"Message '{expected}' should be transformed to uppercase");
            }

            TestContext.WriteLine($"✅ Native Flink job successfully processed all messages!");

            // Cleanup - Cancel job
            await CancelJobAsync(httpClient, jobId, ct);
            TestContext.WriteLine($"✅ Job cancelled");

            stopwatch.Stop();
            TestContext.WriteLine($"✅ Test completed successfully in {stopwatch.Elapsed.TotalSeconds:F1} seconds");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            TestContext.WriteLine($"❌ Test failed after {stopwatch.Elapsed.TotalSeconds:F1} seconds: {ex.Message}");
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

        TestContext.WriteLine($"📤 Uploading JAR to {uploadUrl}...");
        var response = await client.PostAsync(uploadUrl, content, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<FlinkJarUploadResponse>(ct);
        Assert.That(result, Is.Not.Null, "JAR upload response should not be null");
        Assert.That(result!.Filename, Is.Not.Null.And.Not.Empty, "Uploaded JAR filename should be valid");

        // Extract JAR ID from filename (e.g., "/jars/abc123-native-flink-kafka-job-1.0.0.jar" -> "abc123-native-flink-kafka-job-1.0.0.jar")
        var jarId = Path.GetFileName(result.Filename);
        return jarId;
    }

    private static async Task<string> SubmitNativeJobAsync(HttpClient client, string jarId, CancellationToken ct)
    {
        var flinkEndpoint = await GetFlinkJobManagerEndpointAsync();
        var runUrl = $"{flinkEndpoint}jars/{jarId}/run";

        // Submit job with program arguments for LocalTesting configuration
        // CRITICAL: Use container network address "kafka:9093" since Flink runs inside Docker
        var submitPayload = new
        {
            entryClass = "com.flinkdotnet.NativeKafkaJob",
            programArgs = $"--bootstrap-servers {KafkaContainerConnectionString} --input-topic {InputTopic} --output-topic {OutputTopic} --group-id native-test-consumer",
            parallelism = 1
        };

        TestContext.WriteLine($"📤 Submitting job to {runUrl}");
        TestContext.WriteLine($"   Entry Class: {submitPayload.entryClass}");
        TestContext.WriteLine($"   Args: {submitPayload.programArgs}");
        
        var response = await client.PostAsJsonAsync(runUrl, submitPayload, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<FlinkJobSubmitResponse>(ct);
        Assert.That(result, Is.Not.Null, "Job submit response should not be null");
        Assert.That(result!.JobId, Is.Not.Null.And.Not.Empty, "Job ID should be valid");

        return result.JobId;
    }

    private static async Task WaitForJobRunningAsync(HttpClient client, string jobId, TimeSpan timeout, CancellationToken ct)
    {
        var flinkEndpoint = await GetFlinkJobManagerEndpointAsync();
        var jobUrl = $"{flinkEndpoint}jobs/{jobId}";
        var deadline = DateTime.UtcNow.Add(timeout);

        TestContext.WriteLine($"⏳ Waiting for job {jobId} to reach RUNNING state...");

        while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
        {
            var response = await client.GetAsync(jobUrl, ct);
            response.EnsureSuccessStatusCode();

            var jobInfo = await response.Content.ReadFromJsonAsync<FlinkJobInfo>(ct);
            TestContext.WriteLine($"   Job status: {jobInfo?.State}");

            if (jobInfo?.State == "RUNNING")
            {
                return;
            }

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

        TestContext.WriteLine($"🛑 Cancelling job {jobId}...");
        var response = await client.PatchAsync(cancelUrl, null, ct);
        response.EnsureSuccessStatusCode();
    }

    private static async Task ProduceMessagesAsync(string topic, string[] messages, string kafkaConnectionString, CancellationToken ct)
    {
        using var producer = new ProducerBuilder<Null, string>(new ProducerConfig
        {
            BootstrapServers = kafkaConnectionString,
            ClientId = "native-flink-test-producer",
            BrokerAddressFamily = BrokerAddressFamily.V4,
            SecurityProtocol = SecurityProtocol.Plaintext
        })
        .SetLogHandler((_, _) => { /* Suppress logs */ })
        .SetErrorHandler((_, _) => { /* Suppress errors */ })
        .Build();

        foreach (var message in messages)
        {
            var result = await producer.ProduceAsync(topic, new Message<Null, string> { Value = message }, ct);
            TestContext.WriteLine($"  📤 Produced: '{message}' to partition {result.Partition} at offset {result.Offset}");
        }

        producer.Flush(TimeSpan.FromSeconds(10));
    }

    private static Task<List<string>> ConsumeMessagesAsync(string topic, int expectedCount, TimeSpan timeout, string kafkaConnectionString, CancellationToken ct)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = kafkaConnectionString,
            GroupId = $"native-flink-test-consumer-{Guid.NewGuid()}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            BrokerAddressFamily = BrokerAddressFamily.V4,
            SecurityProtocol = SecurityProtocol.Plaintext
        };

        var messages = new List<string>();
        using var consumer = new ConsumerBuilder<Ignore, string>(config)
            .SetLogHandler((_, _) => { /* Suppress logs */ })
            .SetErrorHandler((_, _) => { /* Suppress errors */ })
            .Build();
            
        consumer.Subscribe(topic);

        var deadline = DateTime.UtcNow.Add(timeout);
        TestContext.WriteLine($"📥 Starting to consume from '{topic}' (timeout: {timeout.TotalSeconds}s, expected: {expectedCount})");

        while (DateTime.UtcNow < deadline && messages.Count < expectedCount && !ct.IsCancellationRequested)
        {
            var consumeResult = consumer.Consume(TimeSpan.FromSeconds(1));
            if (consumeResult != null)
            {
                messages.Add(consumeResult.Message.Value);
                TestContext.WriteLine($"  📥 Consumed: '{consumeResult.Message.Value}' from partition {consumeResult.Partition} at offset {consumeResult.Offset}");
            }
        }

        return Task.FromResult(messages);
    }

    /// <summary>
    /// Find the native Flink JAR in the repository.
    /// </summary>
    private static string FindNativeFlinkJar()
    {
        // Start from test assembly directory and search for repository root
        var currentDir = AppContext.BaseDirectory;
        var repoRoot = FindRepositoryRoot(currentDir);
        
        if (repoRoot != null)
        {
            var jarPath = Path.Combine(repoRoot, "LocalTesting", "NativeFlinkJob", "target", "native-flink-kafka-job-1.0.0.jar");
            if (File.Exists(jarPath))
            {
                return jarPath;
            }
        }
        
        // Fallback to relative path from test directory
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "NativeFlinkJob", "target", "native-flink-kafka-job-1.0.0.jar"));
    }
    
    /// <summary>
    /// Find repository root by looking for global.json file.
    /// </summary>
    private static string? FindRepositoryRoot(string startPath)
    {
        var dir = new DirectoryInfo(startPath);
        while (dir != null)
        {
            var globalJsonPath = Path.Combine(dir.FullName, "global.json");
            if (File.Exists(globalJsonPath))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return null;
    }

    /// <summary>
    /// Get the Flink JobManager endpoint by discovering the dynamically allocated port.
    /// </summary>
    private static Task<string> GetFlinkJobManagerEndpointAsync()
    {
        try
        {
            // Discover port from Docker - Aspire DCP assigns random ports
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

                // Parse port mapping: 127.0.0.1:XXXXX->8081/tcp
                var match = System.Text.RegularExpressions.Regex.Match(output, @"127\.0\.0\.1:(\d+)->8081");
                if (match.Success)
                {
                    var hostPort = match.Groups[1].Value;
                    return Task.FromResult($"http://localhost:{hostPort}/");
                }
            }
        }
        catch
        {
            // Fall through to default
        }

        // Fallback to configured port
        return Task.FromResult($"http://localhost:{LocalTesting.FlinkSqlAppHost.Ports.JobManagerHostPort}/");
    }

    // DTOs for Flink REST API
    private record FlinkJarUploadResponse(string Status, string Filename);
    private record FlinkJobSubmitResponse(string JobId);
    private record FlinkJobInfo(string JobId, string State);
}