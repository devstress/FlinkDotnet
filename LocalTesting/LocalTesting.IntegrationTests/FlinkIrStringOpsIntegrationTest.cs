using System.Diagnostics;
using Aspire.Hosting.Testing;
using Confluent.Kafka;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

[TestFixture, NonParallelizable]
[Category("flink-string-ops")]
public class FlinkIrStringOpsIntegrationTest
{
    private const string InputTopic = "lt.flink.stringops.input";
    private const string OutputTopic = "lt.flink.stringops.output";

    [Test]
    public async Task FlinkIrStringOps_KafkaToKafka_WithStringTransformation_Test()
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
        
        TestContext.WriteLine("🚀 Starting Flink IR String Operations Integration Test");
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.LocalTesting_FlinkSqlAppHost>(ct);
            var app = await appHost.BuildAsync(ct);
            using var startCts = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(ct);
            startCts.CancelAfter(TimeSpan.FromMinutes(10));
            await app.StartAsync(startCts.Token);

            try
            {
                TestContext.WriteLine("⏳ Waiting for Kafka to be healthy...");
                await app.ResourceNotifications
                    .WaitForResourceHealthyAsync("kafka", ct)
                    .WaitAsync(TimeSpan.FromSeconds(120), ct);
                TestContext.WriteLine("✅ Kafka is healthy");

                var kafka = $"localhost:{Ports.KafkaPort}";

                TestContext.WriteLine("⏳ Waiting for infrastructure to be ready...");
                await WaitForKafkaReady(kafka!, TimeSpan.FromSeconds(120), ct);
                var jmBase = $"http://localhost:{Ports.JobManagerHostPort}/";
                var gatewayBase = $"http://localhost:{Ports.GatewayHostPort}/";

                await Task.WhenAll(
                    WaitForFlinkReadyAsync($"{jmBase}v1/overview", TimeSpan.FromSeconds(120), ct),
                    WaitForHttpOkAsync($"{gatewayBase}api/v1/health", TimeSpan.FromSeconds(120), ct));
                TestContext.WriteLine("✅ All infrastructure components are ready");

                TestContext.WriteLine($"📝 Creating Kafka topics: {InputTopic} -> {OutputTopic}");
                await CreateTopicAsync(kafka!, InputTopic, 1);
                await CreateTopicAsync(kafka!, OutputTopic, 1);

                TestContext.WriteLine("🔧 Creating Flink job for string operations (uppercase transformation)");
                var job = FlinkDotNet.Flink.JobBuilder
                    .FromKafka(InputTopic, kafka)
                    .Map("upper")
                    .ToKafka(OutputTopic, kafka);
                
                var submitResult = await job.Submit("lt-stringops-test", ct);
                TestContext.WriteLine($"📊 Job submission result: success={submitResult.Success}, jobId={submitResult.FlinkJobId}, error={submitResult.ErrorMessage}");
                Assert.That(submitResult.Success, Is.True, $"Job must submit successfully. Error: {submitResult.ErrorMessage}");

                var messageCount = 10;
                TestContext.WriteLine($"📤 Producing {messageCount} test messages to {InputTopic}");
                await ProduceSimpleMessagesAsync(kafka!, InputTopic, messageCount, ct);

                TestContext.WriteLine($"📥 Consuming processed messages from {OutputTopic}");
                var consumedCount = await ConsumeAsync(kafka!, OutputTopic, messageCount, TimeSpan.FromSeconds(60), ct);
                TestContext.WriteLine($"📊 Consumed {consumedCount} messages (expected: {messageCount})");
                Assert.That(consumedCount, Is.GreaterThanOrEqualTo(messageCount), $"All {messageCount} messages should be processed and consumed");
                
                stopwatch.Stop();
                TestContext.WriteLine($"✅ Test completed successfully in {stopwatch.Elapsed.TotalSeconds:F1} seconds");
            }
            finally 
            { 
                try 
                { 
                    TestContext.WriteLine("🧹 Cleaning up application resources...");
                    await app.DisposeAsync(); 
                } 
                catch (Exception ex) 
                { 
                    TestContext.WriteLine($"⚠️ [diag] Dispose failed: {ex.Message}"); 
                } 
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            TestContext.WriteLine($"❌ Test failed after {stopwatch.Elapsed.TotalSeconds:F1} seconds: {ex.Message}");
            throw;
        }
    }

    #region Enhanced Helper Methods

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
            TestContext.WriteLine($"✅ Topic '{topic}' created successfully");
        }
        catch (Confluent.Kafka.Admin.CreateTopicsException ex)
        {
            if (!ex.Results.Any(r => r.Error.Code == Confluent.Kafka.ErrorCode.TopicAlreadyExists))
            {
                TestContext.WriteLine($"❌ Failed to create topic '{topic}': {ex.Message}");
                throw;
            }
            TestContext.WriteLine($"ℹ️ Topic '{topic}' already exists");
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
        
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < count; i++)
        {
            try
            {
                await producer.ProduceAsync(topic, new Message<string, string> { Key = $"key-{i}", Value = $"value-{i}" }, ct);
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"❌ Failed to produce message {i}: {ex.Message}");
                throw;
            }
        }
        
        producer.Flush(TimeSpan.FromSeconds(10));
        stopwatch.Stop();
        TestContext.WriteLine($"📤 Produced {count} messages in {stopwatch.Elapsed.TotalMilliseconds:F0}ms");
    }

    private static Task<long> ConsumeAsync(string bootstrap, string topic, int expectedMin, TimeSpan timeout, CancellationToken ct)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrap,
            GroupId = $"lt-flink-stringops-consumer-{Guid.NewGuid()}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            BrokerAddressFamily = BrokerAddressFamily.V4,
            SecurityProtocol = SecurityProtocol.Plaintext
        };
        
        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(topic);
        var sw = Stopwatch.StartNew();
        long total = 0;
        
        TestContext.WriteLine($"📥 Starting to consume from topic '{topic}' (timeout: {timeout.TotalSeconds}s, expected minimum: {expectedMin})");
        
        while (sw.Elapsed < timeout && total < expectedMin && !ct.IsCancellationRequested)
        {
            try
            {
                var cr = consumer.Consume(TimeSpan.FromMilliseconds(250));
                if (cr != null) 
                {
                    total++;
                    TestContext.WriteLine($"📨 Consumed message {total}: key='{cr.Message.Key}', value='{cr.Message.Value}'");
                }
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"⚠️ Error consuming message: {ex.Message}");
            }
        }
        
        consumer.Close();
        TestContext.WriteLine($"📊 Total consumption completed: {total} messages in {sw.Elapsed.TotalSeconds:F1}s");
        return Task.FromResult(total);
    }

    private static async Task WaitForHttpOkAsync(string url, TimeSpan timeout, CancellationToken ct)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var sw = Stopwatch.StartNew();
        
        TestContext.WriteLine($"⏳ Waiting for HTTP endpoint: {url}");
        
        while (sw.Elapsed < timeout)
        {
            try
            {
                var resp = await http.GetAsync(url, ct);
                if ((int)resp.StatusCode >= 200 && (int)resp.StatusCode < 500)
                {
                    TestContext.WriteLine($"✅ HTTP endpoint ready: {url} (status: {resp.StatusCode})");
                    return;
                }
                TestContext.WriteLine($"⏳ HTTP endpoint not ready: {url} (status: {resp.StatusCode})");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"⏳ HTTP probe exception for {url}: {ex.GetType().Name}: {ex.Message}");
            }
            
            await Task.Delay(500, ct);
        }
        
        throw new TimeoutException($"HTTP probe timed out for {url} after {timeout.TotalSeconds}s");
    }

    private static async Task WaitForFlinkReadyAsync(string overviewUrl, TimeSpan timeout, CancellationToken ct)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var sw = Stopwatch.StartNew();
        
        TestContext.WriteLine($"⏳ Waiting for Flink JobManager: {overviewUrl}");
        
        while (sw.Elapsed < timeout && !ct.IsCancellationRequested)
        {
            try
            {
                var resp = await http.GetAsync(overviewUrl, ct);
                if (resp.IsSuccessStatusCode)
                {
                    var content = await resp.Content.ReadAsStringAsync(ct);
                    if (!string.IsNullOrEmpty(content))
                    {
                        TestContext.WriteLine($"✅ Flink JobManager ready: {overviewUrl}");
                        return;
                    }
                }
                TestContext.WriteLine($"⏳ Flink JobManager not ready yet (status: {resp.StatusCode})");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"⏳ Flink readiness exception: {ex.GetType().Name}: {ex.Message}");
            }
            
            await Task.Delay(1000, ct);
        }
        
        throw new TimeoutException($"Flink JobManager REST API not ready: {overviewUrl} after {timeout.TotalSeconds}s");
    }

    private static async Task WaitForKafkaReady(string bootstrapServers, TimeSpan timeout, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        
        TestContext.WriteLine($"⏳ Waiting for Kafka: {bootstrapServers}");
        
        while (sw.Elapsed < timeout && !ct.IsCancellationRequested)
        {
            try
            {
                using var admin = new Confluent.Kafka.AdminClientBuilder(new AdminClientConfig { 
                    BootstrapServers = bootstrapServers, 
                    SocketTimeoutMs = 5000, 
                    BrokerAddressFamily = BrokerAddressFamily.V4, 
                    SecurityProtocol = SecurityProtocol.Plaintext 
                }).Build();
                
                var md = admin.GetMetadata(TimeSpan.FromSeconds(3));
                if (md?.Brokers?.Count > 0)
                {
                    TestContext.WriteLine($"✅ Kafka ready: {bootstrapServers} ({md.Brokers.Count} brokers)");
                    return;
                }
                TestContext.WriteLine($"⏳ Kafka not ready yet (brokers: {md?.Brokers?.Count ?? 0})");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"⏳ Kafka readiness exception: {ex.GetType().Name}: {ex.Message}");
            }
            
            await Task.Delay(500, ct);
        }
        
        throw new TimeoutException($"Kafka did not become ready within {timeout.TotalSeconds:F0}s at {bootstrapServers}");
    }
    #endregion
}

































































































































































































































































































































































