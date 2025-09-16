using System.Diagnostics;
using System.Text.Json;
using Aspire.Hosting.Testing;
using Confluent.Kafka;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

[TestFixture]
[Category("gateway-bundling")]
public class GatewayAutomaticBundlingTest
{
    private const string TestInputTopic = "lt.gateway.bundling.input";
    private const string TestOutputTopic = "lt.gateway.bundling.output";

    [Test]
    public async Task Gateway_AutomaticBundling_WithoutPrebuiltJar_SuccessfullyRunsJob()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.LocalTesting_FlinkSqlAppHost>(ct);
        var app = await appHost.BuildAsync(ct);
        await app.StartAsync(ct);

        try
        {
            // Wait for infrastructure to be ready
            await app.ResourceNotifications
                .WaitForResourceHealthyAsync("kafka", ct)
                .WaitAsync(TimeSpan.FromSeconds(90), ct);

            var kafka = await app.GetConnectionStringAsync("kafka", ct);
            await WaitForKafkaReady(kafka!, TimeSpan.FromSeconds(90), ct);

            // Wait for Flink to be ready
            await WaitForFlinkReadyAsync("http://localhost:8081/v1/overview", TimeSpan.FromSeconds(60), ct);

            // Wait for Gateway to be ready
            await WaitForHttpOkAsync("http://localhost:8080/api/v1/health", TimeSpan.FromSeconds(60), ct);

            // Create test topics
            await CreateTopicAsync(kafka!, TestInputTopic, 1);
            await CreateTopicAsync(kafka!, TestOutputTopic, 1);

            TestContext.WriteLine("Testing Gateway Automatic Bundling without prebuilt JAR or env vars");
            
            // Submit a simple job that should trigger automatic jar building
            var jobResult = await FlinkDotNetJobs.CreateUppercaseJob(
                TestInputTopic, TestOutputTopic, kafka!, "auto-bundle-test", ct);
            
            // The gateway should automatically build the JAR and submit successfully
            Assert.That(jobResult.Success, Is.True, 
                $"Job should submit successfully with automatic bundling. Error: {jobResult.ErrorMessage}");
            TestContext.WriteLine($"Job submitted successfully with ID: {jobResult.FlinkJobId}");
            
            // Wait for job to reach running state
            var jobState = await WaitForJobRunningAsync(jobResult.FlinkJobId!, TimeSpan.FromSeconds(120), ct);
            TestContext.WriteLine($"Job reached state: {jobState}");
            
            // Test the job by sending data
            await ProduceTestMessagesAsync(kafka!, TestInputTopic, 5, ct);
            var consumed = await ConsumeAsync(kafka!, TestOutputTopic, 5, TimeSpan.FromSeconds(30), ct);
            Assert.That(consumed, Is.GreaterThanOrEqualTo(5), "Job should process all messages through automatic bundling");
            
            TestContext.WriteLine("✅ Gateway automatic bundling test passed successfully");
        }
        finally 
        { 
            try { await app.DisposeAsync(); } catch { } 
        }
    }

    #region Helpers
    private static async Task<string> WaitForJobRunningAsync(string jobId, TimeSpan timeout, CancellationToken ct)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var sw = Stopwatch.StartNew();
        
        while (sw.Elapsed < timeout && !ct.IsCancellationRequested)
        {
            try
            {
                var response = await http.GetAsync($"http://localhost:8080/api/v1/jobs/{jobId}/status", ct);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(ct);
                    var statusObj = JsonSerializer.Deserialize<JsonElement>(content);
                    
                    if (statusObj.TryGetProperty("state", out var stateElement))
                    {
                        var state = stateElement.GetString();
                        TestContext.WriteLine($"Job {jobId} state: {state}");
                        
                        if (state == "RUNNING" || state == "FINISHED")
                        {
                            return state;
                        }
                        
                        if (state == "FAILED" || state == "CANCELED")
                        {
                            throw new InvalidOperationException($"Job {jobId} failed with state: {state}");
                        }
                    }
                }
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                TestContext.WriteLine($"Error checking job status: {ex.Message}");
            }
            
            await Task.Delay(2000, ct);
        }
        
        throw new TimeoutException($"Job {jobId} did not reach RUNNING or FINISHED state within {timeout.TotalSeconds:F0}s");
    }

    private static async Task CreateTopicAsync(string bootstrapServers, string topic, int partitions)
    {
        using var admin = new Confluent.Kafka.AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrapServers }).Build();
        try
        {
            await admin.CreateTopicsAsync(new[] { new Confluent.Kafka.Admin.TopicSpecification { Name = topic, NumPartitions = partitions, ReplicationFactor = 1 } });
        }
        catch (Confluent.Kafka.Admin.CreateTopicsException ex)
        {
            if (!ex.Results.Any(r => r.Error.Code == Confluent.Kafka.ErrorCode.TopicAlreadyExists))
                throw;
        }
    }

    private static async Task ProduceTestMessagesAsync(string bootstrap, string topic, int count, CancellationToken ct)
    {
        using var producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = bootstrap,
            EnableIdempotence = true,
            Acks = Acks.All,
            LingerMs = 5
        }).Build();
        
        for (int i = 0; i < count; i++)
        {
            await producer.ProduceAsync(topic, new Message<string, string> { Key = $"test-key-{i}", Value = $"test-value-{i}" }, ct);
        }
        
        producer.Flush(TimeSpan.FromSeconds(10));
    }

    private static Task<long> ConsumeAsync(string bootstrap, string topic, int expectedMin, TimeSpan timeout, CancellationToken ct)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrap,
            GroupId = $"auto-bundle-test-consumer-{Guid.NewGuid()}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };
        
        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(topic);
        var sw = Stopwatch.StartNew();
        long total = 0;
        
        while (sw.Elapsed < timeout && total < expectedMin && !ct.IsCancellationRequested)
        {
            var cr = consumer.Consume(TimeSpan.FromMilliseconds(250));
            if (cr != null) total++;
        }
        
        consumer.Close();
        return Task.FromResult(total);
    }

    private static async Task WaitForHttpOkAsync(string url, TimeSpan timeout, CancellationToken ct)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var sw = Stopwatch.StartNew();
        
        while (sw.Elapsed < timeout)
        {
            try
            {
                var resp = await http.GetAsync(url, ct);
                if ((int)resp.StatusCode >= 200 && (int)resp.StatusCode < 500) return;
            }
            catch { }
            
            await Task.Delay(500, ct);
        }
        
        throw new TimeoutException($"HTTP probe timed out for {url}");
    }

    private static async Task WaitForFlinkReadyAsync(string overviewUrl, TimeSpan timeout, CancellationToken ct)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var sw = Stopwatch.StartNew();
        
        while (sw.Elapsed < timeout && !ct.IsCancellationRequested)
        {
            try
            {
                var resp = await http.GetAsync(overviewUrl, ct);
                if (resp.IsSuccessStatusCode)
                {
                    var content = await resp.Content.ReadAsStringAsync(ct);
                    if (!string.IsNullOrEmpty(content)) return;
                }
            }
            catch { }
            
            await Task.Delay(1000, ct);
        }
        
        throw new TimeoutException("Flink JobManager REST API not ready: " + overviewUrl);
    }

    private static async Task WaitForKafkaReady(string bootstrapServers, TimeSpan timeout, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        
        while (sw.Elapsed < timeout && !ct.IsCancellationRequested)
        {
            try
            {
                using var admin = new Confluent.Kafka.AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrapServers, SocketTimeoutMs = 5000 }).Build();
                var md = admin.GetMetadata(TimeSpan.FromSeconds(3));
                if (md?.Brokers?.Count > 0) return;
            }
            catch { }
            
            await Task.Delay(500, ct);
        }
        
        throw new TimeoutException($"Kafka did not become ready within {timeout.TotalSeconds:F0}s at {bootstrapServers}");
    }
    #endregion
}