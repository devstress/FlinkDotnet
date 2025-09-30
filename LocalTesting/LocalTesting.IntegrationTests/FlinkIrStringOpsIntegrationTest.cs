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
        Environment.SetEnvironmentVariable("FLINK_FORCE_LOCAL", null);

        TestPrerequisites.EnsureDockerAvailable();
        var gatewayBuildable = TestPrerequisites.ProbeFlinkGatewayBuildable();
        if (!gatewayBuildable)
        {
            Assert.Fail("Flink.JobGateway or Runner JAR not available. Please build the gateway and run scripts/ensure-flink-runner.ps1 to produce the runner JAR before running tests.");
            return;
        }

        using var enableGateway = new EnvironmentVariableScope("INCLUDE_FLINK_GATEWAY", "1");

        var baseToken = TestContext.CurrentContext.CancellationToken;
        using var testTimeout = new System.Threading.CancellationTokenSource(TimeSpan.FromMinutes(10));
        using var linkedCts = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(baseToken, testTimeout.Token);
        var ct = linkedCts.Token;
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.LocalTesting_FlinkSqlAppHost>(ct);
        var app = await appHost.BuildAsync(ct);
        using var startCts = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(ct);
        startCts.CancelAfter(TimeSpan.FromMinutes(10));
        await app.StartAsync(startCts.Token);

        try
        {
            await app.ResourceNotifications
                .WaitForResourceHealthyAsync("kafka", ct)
                .WaitAsync(TimeSpan.FromSeconds(120), ct);

            var kafka = $"localhost:{Ports.KafkaPort}";

            await WaitForKafkaReady(kafka!, TimeSpan.FromSeconds(120), ct);
            var jmBase = $"http://localhost:{Ports.JobManagerHostPort}/";
            var gatewayBase = $"http://localhost:{Ports.GatewayHostPort}/";

            await Task.WhenAll(
                WaitForFlinkReadyAsync($"{jmBase}v1/overview", TimeSpan.FromSeconds(120), ct),
                WaitForHttpOkAsync($"{gatewayBase}api/v1/health", TimeSpan.FromSeconds(120), ct));

            await CreateTopicAsync(kafka!, InputTopic, 1);
            await CreateTopicAsync(kafka!, OutputTopic, 1);

            var job = FlinkDotNet.Flink.JobBuilder
                .FromKafka(InputTopic, kafka)
                .Map("upper")
                .ToKafka(OutputTopic, kafka);
            
            var submitResult = await job.Submit("lt-basic-test", ct);
            TestContext.WriteLine($"Job submit success={submitResult.Success}; jobId={submitResult.FlinkJobId}; error={submitResult.ErrorMessage}");
            Assert.That(submitResult.Success, Is.True, "Job must submit successfully");

            var messageCount = 10;
            await ProduceSimpleMessagesAsync(kafka!, InputTopic, messageCount, ct);

            var consumedCount = await ConsumeAsync(kafka!, OutputTopic, messageCount, TimeSpan.FromSeconds(60), ct);
            TestContext.WriteLine($"Consumed {consumedCount} messages");
            Assert.That(consumedCount, Is.GreaterThanOrEqualTo(messageCount), "All messages should be processed");
        }
        finally 
        { 
            try { await app.DisposeAsync(); } catch (Exception ex) { TestContext.WriteLine($"[diag] Dispose failed: {ex.Message}"); } 
        }
    }

    #region Helpers
    

    private static async Task CreateTopicAsync(string bootstrapServers, string topic, int partitions)
    {
        using var admin = new Confluent.Kafka.AdminClientBuilder(new AdminClientConfig { 
            BootstrapServers = bootstrapServers,
            BrokerAddressFamily = BrokerAddressFamily.V4,
            SecurityProtocol = SecurityProtocol.Plaintext
        }).Build();
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
            LingerMs = 5,
            BrokerAddressFamily = BrokerAddressFamily.V4,
            SecurityProtocol = SecurityProtocol.Plaintext
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
            EnableAutoCommit = false,
            BrokerAddressFamily = BrokerAddressFamily.V4,
            SecurityProtocol = SecurityProtocol.Plaintext
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
            catch (Exception ex)
            {
                TestContext.WriteLine($"[diag] HTTP probe exception: {ex.Message}");
            }
            
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
                    if (!string.IsNullOrEmpty(content)) return; // Consider ready
                }
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"[diag] Flink readiness exception: {ex.Message}");
            }
            
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
                using var admin = new Confluent.Kafka.AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrapServers, SocketTimeoutMs = 5000, BrokerAddressFamily = BrokerAddressFamily.V4, SecurityProtocol = SecurityProtocol.Plaintext }).Build();
                var md = admin.GetMetadata(TimeSpan.FromSeconds(3));
                if (md?.Brokers?.Count > 0) return;
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"[diag] Kafka readiness exception: {ex.Message}");
            }
            
            await Task.Delay(500, ct);
        }
        
        throw new TimeoutException($"Kafka did not become ready within {timeout.TotalSeconds:F0}s at {bootstrapServers}");
    }
    #endregion
}

































































































































































































































































































































































