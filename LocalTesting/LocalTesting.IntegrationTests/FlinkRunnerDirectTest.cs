using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Flink.JobBuilder.Models;
using LocalTesting.FlinkSqlAppHost;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

/// <summary>
/// Tests FlinkJobRunner.java directly (bypassing Gateway) to isolate configuration issues.
/// This proves the JAR works correctly before testing Gateway integration.
/// </summary>
[TestFixture]
[Category("flink-runner-direct")]
public class FlinkRunnerDirectTest : LocalTestingTestBase
{
    // Use unique topic names per test run to prevent conflicts when tests run together
    private static string InputTopic => $"lt.runner.direct.input.{TestContext.CurrentContext.Test.ID}";
    private static string OutputTopic => $"lt.runner.direct.output.{TestContext.CurrentContext.Test.ID}";

    [Test]
    public async Task FlinkRunner_DirectExecution_WithCorrectKafkaConfig_ShouldWork()
    {
        TestPrerequisites.EnsureDockerAvailable();
        
        var baseToken = TestContext.CurrentContext.CancellationToken;
        using var testTimeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(baseToken, testTimeout.Token);
        var ct = linkedCts.Token;
        
        TestContext.WriteLine("🚀 Testing FlinkJobRunner.java Direct Execution");
        TestContext.WriteLine("   Purpose: Verify JAR configuration without Gateway interference");
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Wait for infrastructure
            TestContext.WriteLine("⏳ Waiting for Kafka and Flink infrastructure...");
            await WaitForFullInfrastructureAsync(includeGateway: false, ct);
            TestContext.WriteLine("✅ Infrastructure ready");

            // Create topics
            TestContext.WriteLine($"📝 Creating Kafka topics: {InputTopic} -> {OutputTopic}");
            await CreateTopicAsync(InputTopic, 1);
            await CreateTopicAsync(OutputTopic, 1);

            // Create job definition manually (what Gateway should create)
            var jobDef = new JobDefinition
            {
                Source = new KafkaSourceDefinition
                {
                    Topic = InputTopic,
                    BootstrapServers = KafkaContainerConnectionString, // kafka:9093
                    GroupId = "runner-direct-test"
                },
                Operations = new List<IOperationDefinition>
                {
                    new MapOperationDefinition { Expression = "toUpper" }
                },
                Sink = new KafkaSinkDefinition
                {
                    Topic = OutputTopic,
                    BootstrapServers = KafkaContainerConnectionString // kafka:9093
                },
                Metadata = new JobMetadata
                {
                    JobId = Guid.NewGuid().ToString(),
                    JobName = "Runner Direct Test",
                    Parallelism = 1
                }
            };

            // Serialize to JSON and Base64 (what Gateway does)
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(jobDef, options);
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            
            TestContext.WriteLine("📦 Job Definition JSON:");
            TestContext.WriteLine(json);
            TestContext.WriteLine("");
            TestContext.WriteLine($"📦 Base64 length: {base64.Length} characters");

            // Submit job directly to Flink using REST API (bypassing Gateway)
            TestContext.WriteLine("🚀 Submitting job directly to Flink REST API...");
            var jobId = await SubmitJobDirectlyAsync(base64, ct);
            TestContext.WriteLine($"✅ Job submitted: {jobId}");
            
            // Wait for job to start
            await Task.Delay(TimeSpan.FromSeconds(10), ct);

            // Produce test messages
            var messageCount = 10;
            TestContext.WriteLine($"📤 Producing {messageCount} test messages");
            await ProduceMessagesAsync(InputTopic, messageCount, ct);

            // Consume and verify
            TestContext.WriteLine($"📥 Consuming processed messages from {OutputTopic}");
            var consumed = await ConsumeMessagesAsync(OutputTopic, messageCount, TimeSpan.FromSeconds(60), KafkaConnectionString!, ct);
            
            TestContext.WriteLine($"📊 Consumed {consumed.Count} messages (expected: {messageCount})");
            
            if (consumed.Count > 0)
            {
                var allUppercase = consumed.All(msg => msg.Value == msg.Value.ToUpper());
                TestContext.WriteLine($"✓ All messages uppercase: {allUppercase}");
                Assert.That(allUppercase, Is.True, "All messages should be uppercase");
            }
            
            Assert.That(consumed.Count, Is.GreaterThanOrEqualTo(messageCount), 
                $"Should consume at least {messageCount} messages");
            
            stopwatch.Stop();
            TestContext.WriteLine($"✅ DIRECT RUNNER TEST PASSED in {stopwatch.Elapsed.TotalSeconds:F1}s");
            TestContext.WriteLine("   FlinkJobRunner.java works correctly with kafka:9093");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            TestContext.WriteLine($"❌ Test failed after {stopwatch.Elapsed.TotalSeconds:F1}s");
            TestContext.WriteLine($"   Error: {ex.Message}");
            throw;
        }
    }

    private async Task<string> SubmitJobDirectlyAsync(string irBase64, CancellationToken ct)
    {
        // Find and upload JAR
        var jarPath = FindFlinkRunnerJar();
        if (string.IsNullOrEmpty(jarPath))
        {
            throw new FileNotFoundException("flink-ir-runner.jar not found");
        }

        TestContext.WriteLine($"📦 Using JAR: {jarPath}");
        
        var flinkEndpoint = await GetFlinkJobManagerEndpointAsync();
        using var httpClient = new HttpClient { BaseAddress = new Uri(flinkEndpoint) };
        
        // Upload JAR
        using var form = new MultipartFormDataContent();
        using var fileStream = File.OpenRead(jarPath);
        form.Add(new StreamContent(fileStream), "jarfile", Path.GetFileName(jarPath));
        
        var uploadResponse = await httpClient.PostAsync("/jars/upload", form, ct);
        uploadResponse.EnsureSuccessStatusCode();
        
        // Parse the JAR ID from Flink's response
        var uploadResult = await uploadResponse.Content.ReadAsStringAsync(ct);
        var uploadDoc = JsonDocument.Parse(uploadResult);
        var filename = uploadDoc.RootElement.GetProperty("filename").GetString();
        
        // Extract just the filename part from the full path
        var jarId = Path.GetFileName(filename);
        
        TestContext.WriteLine($"📦 JAR uploaded successfully");
        TestContext.WriteLine($"   Full path: {filename}");
        TestContext.WriteLine($"   JAR ID: {jarId}");
        
        await Task.Delay(TimeSpan.FromSeconds(2), ct);
        
        // Submit job with IR
        var runRequest = new
        {
            entryClass = "com.flink.jobgateway.FlinkJobRunner",
            programArgsList = new[] { "--irBase64", irBase64 },
            parallelism = 1,
            jobName = "Runner Direct Test"
        };
        
        var requestJson = JsonSerializer.Serialize(runRequest);
        TestContext.WriteLine($"🚀 Submitting with entry class: com.flink.jobgateway.FlinkJobRunner");
        
        var runResponse = await httpClient.PostAsync(
            $"/jars/{jarId}/run",
            new StringContent(requestJson, Encoding.UTF8, "application/json"),
            ct);
        
        var runResult = await runResponse.Content.ReadAsStringAsync(ct);
        
        if (!runResponse.IsSuccessStatusCode)
        {
            TestContext.WriteLine($"❌ Flink returned HTTP {(int)runResponse.StatusCode} {runResponse.StatusCode}");
            TestContext.WriteLine($"📄 Response body:");
            TestContext.WriteLine(runResult);
            runResponse.EnsureSuccessStatusCode(); // This will throw with the status code
        }
        
        var doc = JsonDocument.Parse(runResult);
        var jobId = doc.RootElement.GetProperty("jobid").GetString() 
                    ?? doc.RootElement.GetProperty("jobId").GetString();
        
        return jobId!;
    }

    private static string? FindFlinkRunnerJar()
    {
        var searchPaths = new[]
        {
            Path.Combine(TestContext.CurrentContext.TestDirectory, "flink-ir-runner.jar"),
            Path.Combine(Directory.GetCurrentDirectory(), "FlinkIRRunner", "target", "flink-ir-runner.jar"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "FlinkIRRunner", "target", "flink-ir-runner.jar"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "FlinkIRRunner", "target", "flink-ir-runner.jar")
        };

        return searchPaths.FirstOrDefault(File.Exists);
    }

    private async Task ProduceMessagesAsync(string topic, int count, CancellationToken ct)
    {
        using var producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = KafkaConnectionString,
            EnableIdempotence = true,
            Acks = Acks.All
        }).Build();

        for (int i = 0; i < count; i++)
        {
            var value = $"test-{i}";
            await producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = $"key-{i}",
                Value = value
            }, ct);
            TestContext.WriteLine($"  Produced: {value}");
        }

        producer.Flush(TimeSpan.FromSeconds(10));
    }

    private static Task<List<Message<string, string>>> ConsumeMessagesAsync(
        string topic, int expectedCount, TimeSpan timeout, string kafkaConnectionString, CancellationToken ct)
    {
        var messages = new List<Message<string, string>>();
        
        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = kafkaConnectionString,
            GroupId = $"runner-direct-consumer-{Guid.NewGuid()}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        }).Build();

        consumer.Subscribe(topic);
        var sw = Stopwatch.StartNew();

        while (sw.Elapsed < timeout && messages.Count < expectedCount && !ct.IsCancellationRequested)
        {
            var cr = consumer.Consume(TimeSpan.FromMilliseconds(500));
            if (cr != null)
            {
                messages.Add(cr.Message);
                TestContext.WriteLine($"  Consumed: {cr.Message.Value}");
            }
        }

        consumer.Close();
        return Task.FromResult(messages);
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
        return Task.FromResult($"http://localhost:{Ports.JobManagerHostPort}/");
    }
}