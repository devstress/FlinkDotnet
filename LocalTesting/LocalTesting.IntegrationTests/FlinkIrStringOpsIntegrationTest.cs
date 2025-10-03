using System.Diagnostics;
using Confluent.Kafka;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

[TestFixture]
[Category("flink-string-ops")]
public class FlinkIrStringOpsIntegrationTest : LocalTestingTestBase
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

        var baseToken = TestContext.CurrentContext.CancellationToken;
        using var testTimeout = new System.Threading.CancellationTokenSource(TimeSpan.FromMinutes(5)); // Reduced timeout
        using var linkedCts = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(baseToken, testTimeout.Token);
        var ct = linkedCts.Token;
        
        TestContext.WriteLine("🚀 Starting Flink IR String Operations Integration Test");
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Use base class infrastructure setup (AppHost, Kafka already initialized)
            TestContext.WriteLine("⏳ Waiting for complete infrastructure (Kafka + Flink + Gateway)...");
            await WaitForFullInfrastructureAsync(includeGateway: true, ct);
            TestContext.WriteLine("✅ All infrastructure components are ready");

            // Create test topics using base class method
            TestContext.WriteLine($"📝 Creating Kafka topics: {InputTopic} -> {OutputTopic}");
            await CreateTopicAsync(InputTopic, 1);
            await CreateTopicAsync(OutputTopic, 1);

            // Create and submit Flink job
            TestContext.WriteLine("🔧 Creating Flink job for string operations (uppercase transformation)");
            // CRITICAL: Use container network address for Kafka since Flink runs inside Docker
            // The test code uses KafkaConnectionString (localhost:port) to produce/consume
            // but Flink containers must use kafka:9092 (container network name)
            var job = FlinkDotNet.Flink.JobBuilder
                .FromKafka(InputTopic, KafkaContainerConnectionString)
                .Map("toUpper")
                .ToKafka(OutputTopic, KafkaContainerConnectionString);
            
            var submitResult = await job.Submit("lt-stringops-test", ct);
            TestContext.WriteLine($"📊 Job submission result: success={submitResult.Success}, jobId={submitResult.FlinkJobId}, error={submitResult.ErrorMessage}");
            Assert.That(submitResult.Success, Is.True, $"Job must submit successfully. Error: {submitResult.ErrorMessage}");

            // Produce test messages
            var messageCount = 10;
            TestContext.WriteLine($"📤 Producing {messageCount} test messages to {InputTopic}");
            await ProduceSimpleMessagesAsync(InputTopic, messageCount, ct);

            // Consume and verify processed messages
            TestContext.WriteLine($"📥 Consuming processed messages from {OutputTopic}");
            var consumedCount = await ConsumeMessagesAsync(OutputTopic, messageCount, TimeSpan.FromSeconds(60), ct);
            TestContext.WriteLine($"📊 Consumed {consumedCount} messages (expected: {messageCount})");
            Assert.That(consumedCount, Is.GreaterThanOrEqualTo(messageCount), $"All {messageCount} messages should be processed and consumed");
            
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

    #region Helper Methods

    private async Task ProduceSimpleMessagesAsync(string topic, int count, CancellationToken ct)
    {
        using var producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = KafkaConnectionString,
            EnableIdempotence = true,
            Acks = Acks.All,
            LingerMs = 5,
            BrokerAddressFamily = BrokerAddressFamily.V4,
            SecurityProtocol = SecurityProtocol.Plaintext
        })
        .SetLogHandler((_, _) => { /* Suppress logs */ })
        .SetErrorHandler((_, _) => { /* Suppress errors */ })
        .Build();
        
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < count; i++)
        {
            try
            {
                await producer.ProduceAsync(topic, new Message<string, string> 
                { 
                    Key = $"key-{i}", 
                    Value = $"value-{i}" 
                }, ct);
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

    private Task<long> ConsumeMessagesAsync(string topic, int expectedMin, TimeSpan timeout, CancellationToken ct)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = KafkaConnectionString,
            GroupId = $"lt-flink-stringops-consumer-{Guid.NewGuid()}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            BrokerAddressFamily = BrokerAddressFamily.V4,
            SecurityProtocol = SecurityProtocol.Plaintext
        };
        
        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetLogHandler((_, _) => { /* Suppress logs */ })
            .SetErrorHandler((_, _) => { /* Suppress errors */ })
            .Build();
            
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
                    if (total <= 5 || total % 5 == 0) // Log first 5 and every 5th message
                    {
                        TestContext.WriteLine($"📨 Consumed message {total}: key='{cr.Message.Key}', value='{cr.Message.Value}'");
                    }
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

    #endregion
}
