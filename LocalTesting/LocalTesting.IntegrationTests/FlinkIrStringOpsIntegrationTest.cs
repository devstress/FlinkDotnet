using System.Diagnostics;
using Aspire.Hosting.Testing;
using Confluent.Kafka;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

[TestFixture, NonParallelizable]
[Category("flinkdotnet-basic")]
public class FlinkDotNetBasicIntegrationTest
{
    private const string InputTopic = "lt.flink.basic.input";
    private const string OutputTopic = "lt.flink.basic.output";

    [Test]
    public async Task FlinkDotNet_Basic_KafkaToKafka_Test()
    {
        using var enableGateway = new EnvironmentVariableScope("INCLUDE_FLINK_GATEWAY", "1");

        // Remove forced local simulation; require real Flink cluster
        Environment.SetEnvironmentVariable("FLINK_FORCE_LOCAL", null);

        var ct = TestContext.CurrentContext.CancellationToken;
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.LocalTesting_FlinkSqlAppHost>(ct);
        var app = await appHost.BuildAsync(ct);
        await app.StartAsync(ct);

        try
        {
            // Wait for Kafka to be ready
            await app.ResourceNotifications
                .WaitForResourceHealthyAsync("kafka", ct)
                .WaitAsync(TimeSpan.FromSeconds(90), ct);

            var kafka = await app.GetConnectionStringAsync("kafka", ct);
            await WaitForKafkaReady(kafka!, TimeSpan.FromSeconds(90), ct);

            // Wait for Flink to be ready (discover actual mapped port)
            var jmBase = GetContainerHttpBase("flink-jobmanager", 8081);
            await WaitForFlinkReadyAsync($"{jmBase}v1/overview", TimeSpan.FromSeconds(60), ct);

            // Wait for Gateway to be ready (discover mapped port)
            var gatewayBase = GetContainerHttpBase("flink-job-gateway", 8080);
            await WaitForHttpOkAsync($"{gatewayBase}api/v1/health", TimeSpan.FromSeconds(60), ct);

            // Create topics
            await CreateTopicAsync(kafka!, InputTopic, 1);
            await CreateTopicAsync(kafka!, OutputTopic, 1);

            // Submit a simple DataStream job
            var job = FlinkDotNet.Flink.JobBuilder
                .FromKafka(InputTopic, kafka)
                .Map("upper")
                .ToKafka(OutputTopic, kafka);
            
            var submitResult = await job.Submit("lt-basic-test", ct);
            TestContext.WriteLine($"Job submit success={submitResult.Success}; jobId={submitResult.FlinkJobId}; error={submitResult.ErrorMessage}");
            Assert.That(submitResult.Success, Is.True, "Job must submit successfully");

            // Produce test messages
            var messageCount = 10;
            await ProduceSimpleMessagesAsync(kafka!, InputTopic, messageCount, ct);

            // Consume and verify output
            var consumedCount = await ConsumeAsync(kafka!, OutputTopic, messageCount, TimeSpan.FromSeconds(30), ct);
            TestContext.WriteLine($"Consumed {consumedCount} messages");
            Assert.That(consumedCount, Is.GreaterThanOrEqualTo(messageCount), "All messages should be processed");
        }
        finally 
        { 
            try { await app.DisposeAsync(); } catch { } 
        }
    }

    #region Helpers
    

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

    private static async Task ProduceSimpleMessagesAsync(string bootstrap, string topic, int count, CancellationToken ct)
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
            await producer.ProduceAsync(topic, new Message<string, string> { Key = $"key-{i}", Value = $"value-{i}" }, ct);
        }
        
        producer.Flush(TimeSpan.FromSeconds(10));
    }

    private static Task<long> ConsumeAsync(string bootstrap, string topic, int expectedMin, TimeSpan timeout, CancellationToken ct)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrap,
            GroupId = $"lt-flink-basic-consumer-{Guid.NewGuid()}",
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
                if ((int)resp.StatusCode >= 200 && (int)resp.StatusCode < 500) return; // tolerate 404 placeholder
            }
            catch { }
            
            await Task.Delay(500, ct);
        }
        
        throw new TimeoutException($"HTTP probe timed out for {url}");
    }

    private static string GetContainerHttpBase(string nameFilter, int containerPort)
    {
        var deadline = DateTime.UtcNow.AddSeconds(90);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var id = RunProcess("docker", $"ps -q --filter name={nameFilter}");
                var containerId = id.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                if (!string.IsNullOrEmpty(containerId))
                {
                    var portOutput = RunProcess("docker", $"port {containerId} {containerPort}/tcp");
                    var hostPort = portOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
                    if (!string.IsNullOrEmpty(hostPort))
                    {
                        var candidate = hostPort.Split(':').Last().Trim();
                        if (!string.IsNullOrEmpty(candidate))
                        {
                            return $"http://localhost:{candidate}/";
                        }
                    }
                }
            }
            catch { /* ignore and retry */ }
            Thread.Sleep(1000);
        }
        // Fallback to default
        return $"http://localhost:{containerPort}/";
    }

    private static string RunProcess(string fileName, string arguments)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var p = System.Diagnostics.Process.Start(psi)!;
        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit(10000);
        return output;
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
                    if (!string.IsNullOrEmpty(content)) return; // Consider ready
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


