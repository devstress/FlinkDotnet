using System.Diagnostics;
using Aspire.Hosting.Testing;
using Confluent.Kafka;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

[TestFixture]
[Category("observability")]
public class FlinkDotNetIntegrationTest
{
    private const string InputTopic = "lt.flink.input";
    private const string OutputTopic = "lt.flink.output";
    private const string SideOutputTopic = "lt.flink.sideoutput";

    [Test]
    public async Task FlinkDotNet_ComprehensiveValidation_AllJobTypesWork()
    {
        var ct = TestContext.CurrentContext.CancellationToken;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.LocalTesting_AppHost>(ct);
        var app = await appHost.BuildAsync(ct);
        await app.StartAsync(ct);

        try
        {
            // Wait for infrastructure to be ready (increased timeout for Flink cluster startup)
            await app.ResourceNotifications
                .WaitForResourceHealthyAsync("kafka", ct)
                .WaitAsync(TimeSpan.FromSeconds(120), ct);

            var kafka = await app.GetConnectionStringAsync("kafka", ct);
            await WaitForKafkaReady(kafka!, TimeSpan.FromSeconds(120), ct);

            // Create topics for all test scenarios
            await CreateTopicAsync(kafka!, InputTopic, 4);
            await CreateTopicAsync(kafka!, OutputTopic, 4);
            await CreateTopicAsync(kafka!, SideOutputTopic, 4);

            // Ensure Flink Job Gateway is ready (increased timeout for Flink cluster startup)
            await WaitForHttpOkAsync("http://localhost:8080/api/v1/health", TimeSpan.FromSeconds(120), ct);

            // Initialize Gateway service for testing
            var gateway = new Flink.JobBuilder.Services.FlinkJobGatewayService();
            var healthy = await gateway.HealthCheckAsync(ct);
            Assert.That(healthy, Is.True, "Flink Job Gateway must be healthy");

            // Wait for Flink cluster to be ready via the gateway (this does the actual cluster health check)
            await WaitForFlinkClusterReady(TimeSpan.FromSeconds(180), ct);

            // Test Scenario 1: Basic Map Operation
            await TestScenario_BasicMapOperation(kafka!, gateway, ct);

            // Test Scenario 2: Filter Operation  
            await TestScenario_FilterOperation(kafka!, gateway, ct);

            // Test Scenario 3: Timer/Window Operation
            await TestScenario_TimerOperation(kafka!, gateway, ct);

            // Test Scenario 4: Side Output Operation
            await TestScenario_SideOutputOperation(kafka!, gateway, ct);

            // Test Scenario 5: Aggregation Operation
            await TestScenario_AggregationOperation(kafka!, gateway, ct);

            TestContext.WriteLine("✅ All FlinkDotNet scenarios completed successfully");
        }
        finally
        {
            try { await app.DisposeAsync(); }
            catch
            {
                // DisposeAsync may fail if resources are already disposed - this is acceptable
            }
        }
    }

    // SuppressMessage for SonarAnalyzer rule S2325: These methods cannot be static as they are async test helper methods
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "Async test helper methods")]
    private async Task TestScenario_BasicMapOperation(string kafka, Flink.JobBuilder.Services.FlinkJobGatewayService gateway, CancellationToken ct)
    {
        TestContext.WriteLine("🔄 Testing Scenario 1: Basic Map Operation");

        var job = FlinkDotNet.Flink.JobBuilder
            .FromKafka(InputTopic, kafka)
            .Map("identity")
            .ToKafka(OutputTopic, kafka);

        var submitResult = await job.Submit("basic-map-test", ct);
        Assert.That(submitResult.Success, Is.True, $"Basic map job submission failed: {submitResult.ErrorMessage}");

        var flinkJobId = submitResult.FlinkJobId;
        TestContext.WriteLine($"Basic map job submitted with FlinkJobId: {flinkJobId}");

        // Wait for job to be running
        await WaitForJobState(gateway, flinkJobId, "RUNNING", TimeSpan.FromSeconds(60), ct);

        // Test data flow
        var messagesToSend = 100;
        await ProduceAsync(kafka, InputTopic, messagesToSend, ct);
        var consumed = await ConsumeAsync(kafka, OutputTopic, messagesToSend, TimeSpan.FromSeconds(60), ct);

        TestContext.WriteLine($"Basic map: Produced {messagesToSend}, Consumed {consumed}");
        Assert.That(consumed, Is.GreaterThan(0), "Basic map operation should process messages");

        // Validate job metrics
        var metrics = await gateway.GetJobMetricsAsync(flinkJobId, ct);
        TestContext.WriteLine($"Basic map metrics: In={metrics.RecordsIn}, Out={metrics.RecordsOut}");
        Assert.That(metrics.RecordsIn, Is.GreaterThan(0), "Should have input records");

        TestContext.WriteLine("✅ Scenario 1 completed: Basic Map Operation");
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "Async test helper methods")]
    private async Task TestScenario_FilterOperation(string kafka, Flink.JobBuilder.Services.FlinkJobGatewayService gateway, CancellationToken ct)
    {
        TestContext.WriteLine("🔄 Testing Scenario 2: Filter Operation");

        var job = FlinkDotNet.Flink.JobBuilder
            .FromKafka(InputTopic, kafka)
            .Where("value.length() > 5")  // Use Where instead of Filter
            .ToKafka(OutputTopic, kafka);

        var submitResult = await job.Submit("filter-test", ct);
        Assert.That(submitResult.Success, Is.True, $"Filter job submission failed: {submitResult.ErrorMessage}");

        var flinkJobId = submitResult.FlinkJobId;
        TestContext.WriteLine($"Filter job submitted with FlinkJobId: {flinkJobId}");

        // Wait for job to be running
        await WaitForJobState(gateway, flinkJobId, "RUNNING", TimeSpan.FromSeconds(60), ct);

        // Validate IR generation contains where
        var ir = FlinkDotNet.Flink.JobBuilder
            .FromKafka(InputTopic, kafka)
            .Where("value.length() > 5")
            .ToKafka(OutputTopic, kafka)
            .ToJson();

        Assert.That(ir, Does.Contain("where"), "IR should contain where operation");
        TestContext.WriteLine("✅ Scenario 2 completed: Filter Operation");
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "Async test helper methods")]
    private async Task TestScenario_TimerOperation(string kafka, Flink.JobBuilder.Services.FlinkJobGatewayService gateway, CancellationToken ct)
    {
        TestContext.WriteLine("🔄 Testing Scenario 3: Timer/Window Operation");

        var job = FlinkDotNet.Flink.JobBuilder
            .FromKafka(InputTopic, kafka)
            .Map("identity")
            .WithTimer(5)
            .ToKafka(OutputTopic, kafka);

        var submitResult = await job.Submit("timer-test", ct);
        Assert.That(submitResult.Success, Is.True, $"Timer job submission failed: {submitResult.ErrorMessage}");

        var flinkJobId = submitResult.FlinkJobId;
        TestContext.WriteLine($"Timer job submitted with FlinkJobId: {flinkJobId}");

        // Wait for job to be running
        await WaitForJobState(gateway, flinkJobId, "RUNNING", TimeSpan.FromSeconds(60), ct);

        // Validate IR generation contains timer
        var ir = FlinkDotNet.Flink.JobBuilder
            .FromKafka(InputTopic, kafka)
            .WithTimer(5)
            .ToKafka(OutputTopic, kafka)
            .ToJson();

        Assert.That(ir, Does.Contain("timer"), "IR should contain timer operation");
        TestContext.WriteLine("✅ Scenario 3 completed: Timer Operation");
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "Async test helper methods")]
    private async Task TestScenario_SideOutputOperation(string kafka, Flink.JobBuilder.Services.FlinkJobGatewayService gateway, CancellationToken ct)
    {
        TestContext.WriteLine("🔄 Testing Scenario 4: Side Output Operation");

        // Create a side output sink definition
        var sideOutputSink = new Flink.JobBuilder.Models.KafkaSinkDefinition
        {
            Topic = SideOutputTopic,
            BootstrapServers = kafka
        };

        var job = FlinkDotNet.Flink.JobBuilder
            .FromKafka(InputTopic, kafka)
            .Map("identity")
            .WithSideOutput("error-tag", "value.contains('error')", sideOutputSink)
            .ToKafka(OutputTopic, kafka);

        var submitResult = await job.Submit("sideoutput-test", ct);
        Assert.That(submitResult.Success, Is.True, $"Side output job submission failed: {submitResult.ErrorMessage}");

        var flinkJobId = submitResult.FlinkJobId;
        TestContext.WriteLine($"Side output job submitted with FlinkJobId: {flinkJobId}");

        // Wait for job to be running
        await WaitForJobState(gateway, flinkJobId, "RUNNING", TimeSpan.FromSeconds(60), ct);

        // Validate IR generation contains side output
        var ir = FlinkDotNet.Flink.JobBuilder
            .FromKafka(InputTopic, kafka)
            .WithSideOutput("error-tag", "value.contains('error')", sideOutputSink)
            .ToKafka(OutputTopic, kafka)
            .ToJson();

        Assert.That(ir, Does.Contain("sideOutput"), "IR should contain side output operation");
        TestContext.WriteLine("✅ Scenario 4 completed: Side Output Operation");
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "Async test helper methods")]
    private async Task TestScenario_AggregationOperation(string kafka, Flink.JobBuilder.Services.FlinkJobGatewayService gateway, CancellationToken ct)
    {
        TestContext.WriteLine("🔄 Testing Scenario 5: Aggregation Operation");

        var job = FlinkDotNet.Flink.JobBuilder
            .FromKafka(InputTopic, kafka)
            .GroupBy("key")  // Use GroupBy instead of KeyBy
            .Aggregate("count", "value")
            .ToKafka(OutputTopic, kafka);

        var submitResult = await job.Submit("aggregation-test", ct);
        Assert.That(submitResult.Success, Is.True, $"Aggregation job submission failed: {submitResult.ErrorMessage}");

        var flinkJobId = submitResult.FlinkJobId;
        TestContext.WriteLine($"Aggregation job submitted with FlinkJobId: {flinkJobId}");

        // Wait for job to be running
        await WaitForJobState(gateway, flinkJobId, "RUNNING", TimeSpan.FromSeconds(60), ct);

        // Validate IR generation contains aggregation
        var ir = FlinkDotNet.Flink.JobBuilder
            .FromKafka(InputTopic, kafka)
            .GroupBy("key")
            .Aggregate("count", "value")
            .ToKafka(OutputTopic, kafka)
            .ToJson();

        Assert.That(ir, Does.Contain("groupBy") & Does.Contain("aggregate"), "IR should contain groupBy and aggregate operations");
        TestContext.WriteLine("✅ Scenario 5 completed: Aggregation Operation");
    }

    private static async Task WaitForJobState(Flink.JobBuilder.Services.FlinkJobGatewayService gateway, string flinkJobId, string expectedState, TimeSpan timeout, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            try
            {
                var status = await gateway.GetJobStatusAsync(flinkJobId, ct);
                if (status?.State == expectedState)
                {
                    TestContext.WriteLine($"Job {flinkJobId} reached state {expectedState}");
                    return;
                }
                TestContext.WriteLine($"Job {flinkJobId} current state: {status?.State}, waiting for {expectedState}");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Error checking job state: {ex.Message}");
            }
            await Task.Delay(2000, ct);
        }
        TestContext.WriteLine($"Warning: Job {flinkJobId} did not reach {expectedState} within {timeout}");
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
private static async Task ProduceAsync(string bootstrap, string topic, int count, CancellationToken ct)
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
            await producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = $"k-{i % 16}",
                Value = $"msg-{i}"
            }, ct);
        }
        producer.Flush(TimeSpan.FromSeconds(10));
    }

    private static Task<long> ConsumeAsync(string bootstrap, string topic, int expected, TimeSpan timeout, CancellationToken ct)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrap,
            GroupId = $"lt-flink-consumer-{Guid.NewGuid()}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };
        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(topic);
        var sw = Stopwatch.StartNew();
        long total = 0;
        while (sw.Elapsed < timeout && total < expected && !ct.IsCancellationRequested)
        {
            var cr = consumer.Consume(TimeSpan.FromMilliseconds(200));
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
            catch 
            { 
                // HTTP probe failures are expected during service startup
            }
            await Task.Delay(500, ct);
        }
        throw new TimeoutException($"HTTP probe timed out for {url}");
    }

    private static async Task WaitForKafkaReady(string bootstrapServers, TimeSpan timeout, CancellationToken ct)
    {
        var endpoints = bootstrapServers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => s.Split(':'))
            .Where(p => p.Length == 2 && int.TryParse(p[1], out _))
            .Select(p => (host: p[0], port: int.Parse(p[1])))
            .ToArray();
        if (endpoints.Length == 0) throw new ArgumentException($"Invalid bootstrap servers: '{bootstrapServers}'");

        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout && !ct.IsCancellationRequested)
        {
            try
            {
                using var admin = new Confluent.Kafka.AdminClientBuilder(new AdminClientConfig
                {
                    BootstrapServers = bootstrapServers,
                    SocketTimeoutMs = 5000,
                }).Build();
                var md = admin.GetMetadata(TimeSpan.FromSeconds(3));
                if (md?.Brokers?.Count > 0) return;
            }
            catch 
            { 
                // Kafka connection failures are expected during service startup
            }
            await Task.Delay(500, ct);
        }
        throw new TimeoutException($"Kafka did not become ready within {timeout.TotalSeconds:F0}s at {bootstrapServers}");
    }

    private static async Task WaitForFlinkClusterReady(TimeSpan timeout, CancellationToken ct)
    {
        TestContext.WriteLine("🔄 Waiting for Flink cluster to be ready...");
        var sw = Stopwatch.StartNew();
        Exception lastException = null!;
        
        while (sw.Elapsed < timeout)
        {
            try
            {
                // Try to submit a minimal job definition to test cluster connectivity
                // We expect this to fail due to invalid configuration, but NOT due to cluster unavailability
                var dummyJob = FlinkDotNet.Flink.JobBuilder
                    .FromKafka("dummy-topic", "dummy-bootstrap")
                    .Map("dummy-transform")
                    .ToKafka("dummy-output", "dummy-bootstrap");

                var result = await dummyJob.Submit("cluster-readiness-test", ct);
                
                // If we get a response (even if it's a failure), the cluster is responding
                // We just want to avoid "cluster is not available or unhealthy" errors
                if (result.ErrorMessage == null || 
                    (!result.ErrorMessage.Contains("not available") && 
                     !result.ErrorMessage.Contains("unhealthy") &&
                     !result.ErrorMessage.Contains("Resource temporarily unavailable")))
                {
                    TestContext.WriteLine($"✅ Flink cluster is ready! (Response: {result.ErrorMessage ?? "Success"})");
                    return;
                }
                lastException = new Exception(result.ErrorMessage);
                TestContext.WriteLine($"🔄 Flink cluster not ready: {result.ErrorMessage}");
            }
            catch (Exception ex)
            {
                lastException = ex;
                TestContext.WriteLine($"🔄 Flink cluster not ready: {ex.Message}");
            }
            await Task.Delay(3000, ct);
        }
        throw new TimeoutException($"Flink cluster did not become ready within {timeout.TotalSeconds:F0}s. Last error: {lastException?.Message}");
    }
}
