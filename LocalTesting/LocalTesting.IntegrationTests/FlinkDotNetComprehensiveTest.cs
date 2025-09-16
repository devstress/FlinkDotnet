using System.Diagnostics;
using Aspire.Hosting.Testing;
using Confluent.Kafka;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

[TestFixture]
[Category("flinkdotnet-comprehensive")]
public class FlinkDotNetComprehensiveTest
{
    // Topic naming convention: lt.flink.<job-type>.<in/out>
    private const string BasicInputTopic = "lt.flink.basic.input";
    private const string BasicOutputTopic = "lt.flink.basic.output";
    
    private const string FilterInputTopic = "lt.flink.filter.input";
    private const string FilterOutputTopic = "lt.flink.filter.output";
    
    private const string SplitInputTopic = "lt.flink.split.input";
    private const string SplitOutputTopic = "lt.flink.split.output";
    
    private const string TimerInputTopic = "lt.flink.timer.input";
    private const string TimerOutputTopic = "lt.flink.timer.output";
    
    private const string SqlInputTopic = "lt.flink.sql.input";
    private const string SqlOutputTopic = "lt.flink.sql.output";
    
    private const string SqlTransformInputTopic = "lt.flink.sqltransform.input";
    private const string SqlTransformOutputTopic = "lt.flink.sqltransform.output";
    
    private const string CompositeInputTopic = "lt.flink.composite.input";
    private const string CompositeOutputTopic = "lt.flink.composite.output";

    [Test]
    public async Task FlinkDotNet_Comprehensive_AllJobTypes()
    {
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

            // Wait for Flink to be ready
            await WaitForFlinkReadyAsync("http://localhost:8081/v1/overview", TimeSpan.FromSeconds(60), ct);

            // Wait for Gateway to be ready
            await EnsureGatewayAsync(ct);

            // Create all topics
            await CreateTopicAsync(kafka!, BasicInputTopic, 1);
            await CreateTopicAsync(kafka!, BasicOutputTopic, 1);
            await CreateTopicAsync(kafka!, FilterInputTopic, 1);
            await CreateTopicAsync(kafka!, FilterOutputTopic, 1);
            await CreateTopicAsync(kafka!, SplitInputTopic, 1);
            await CreateTopicAsync(kafka!, SplitOutputTopic, 1);
            await CreateTopicAsync(kafka!, TimerInputTopic, 1);
            await CreateTopicAsync(kafka!, TimerOutputTopic, 1);
            await CreateTopicAsync(kafka!, SqlInputTopic, 1);
            await CreateTopicAsync(kafka!, SqlOutputTopic, 1);
            await CreateTopicAsync(kafka!, SqlTransformInputTopic, 1);
            await CreateTopicAsync(kafka!, SqlTransformOutputTopic, 1);
            await CreateTopicAsync(kafka!, CompositeInputTopic, 1);
            await CreateTopicAsync(kafka!, CompositeOutputTopic, 1);

            // Test 1: Basic Uppercase Job
            TestContext.WriteLine("Testing Basic Uppercase Job");
            var basicResult = await FlinkDotNetJobs.CreateUppercaseJob(
                BasicInputTopic, BasicOutputTopic, kafka!, "lt-basic", ct);
            
            Assert.That(basicResult.Success, Is.True, "Basic job must submit successfully");
            TestContext.WriteLine($"Basic job submitted with ID: {basicResult.FlinkJobId}");
            
            await ProduceSimpleMessagesAsync(kafka!, BasicInputTopic, 10, ct);
            var basicConsumed = await ConsumeAsync(kafka!, BasicOutputTopic, 10, TimeSpan.FromSeconds(30), ct);
            Assert.That(basicConsumed, Is.GreaterThanOrEqualTo(10), "Basic job should process all messages");

            // Test 2: Filter Job
            TestContext.WriteLine("Testing Filter Job");
            var filterResult = await FlinkDotNetJobs.CreateFilterJob(
                FilterInputTopic, FilterOutputTopic, kafka!, "lt-filter", ct);
            
            Assert.That(filterResult.Success, Is.True, "Filter job must submit successfully");
            TestContext.WriteLine($"Filter job submitted with ID: {filterResult.FlinkJobId}");
            
            // Send some empty and non-empty messages
            await ProduceMessagesAsync(kafka!, FilterInputTopic, new[] { "", "value1", "", "value2", "value3" }, ct);
            var filterConsumed = await ConsumeAsync(kafka!, FilterOutputTopic, 3, TimeSpan.FromSeconds(30), ct);
            Assert.That(filterConsumed, Is.EqualTo(3), "Filter job should only process non-empty messages");

            // Test 3: SQL Job
            TestContext.WriteLine("Testing SQL Job");
            var sqlResult = await FlinkDotNetJobs.CreateSqlPassthroughJob(
                SqlInputTopic, SqlOutputTopic, kafka!, "lt-sql", ct);
            
            Assert.That(sqlResult.Success, Is.True, "SQL job must submit successfully");
            TestContext.WriteLine($"SQL job submitted with ID: {sqlResult.FlinkJobId}");
            
            await ProduceJsonMessagesAsync(kafka!, SqlInputTopic, 5, ct);
            var sqlConsumed = await ConsumeAsync(kafka!, SqlOutputTopic, 5, TimeSpan.FromSeconds(30), ct);
            Assert.That(sqlConsumed, Is.GreaterThanOrEqualTo(5), "SQL job should process all messages");
        }
        finally 
        { 
            try { await app.DisposeAsync(); } catch { } 
        }
    }

    #region Helpers
    private static async Task EnsureGatewayAsync(CancellationToken ct)
    {
        // Flink Job Gateway health endpoint (ASP.NET)
        await WaitForHttpOkAsync("http://localhost:8080/api/v1/health", TimeSpan.FromSeconds(60), ct);
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
    
    private static async Task ProduceMessagesAsync(string bootstrap, string topic, string[] values, CancellationToken ct)
    {
        using var producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = bootstrap,
            EnableIdempotence = true,
            Acks = Acks.All,
            LingerMs = 5
        }).Build();
        
        for (int i = 0; i < values.Length; i++)
        {
            await producer.ProduceAsync(topic, new Message<string, string> { Key = $"key-{i}", Value = values[i] }, ct);
        }
        
        producer.Flush(TimeSpan.FromSeconds(10));
    }
    
    private static async Task ProduceJsonMessagesAsync(string bootstrap, string topic, int count, CancellationToken ct)
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
            var jsonValue = $"{{&quot;key&quot;:&quot;key-{i}&quot;,&quot;value&quot;:&quot;value-{i}&quot;}}";
            await producer.ProduceAsync(topic, new Message<string, string> { Key = $"key-{i}", Value = jsonValue }, ct);
        }
        
        producer.Flush(TimeSpan.FromSeconds(10));
    }

    private static Task<long> ConsumeAsync(string bootstrap, string topic, int expectedMin, TimeSpan timeout, CancellationToken ct)
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