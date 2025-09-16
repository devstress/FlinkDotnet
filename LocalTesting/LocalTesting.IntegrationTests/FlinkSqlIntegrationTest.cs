using System.Diagnostics;
using Aspire.Hosting.Testing;
using Confluent.Kafka;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

[TestFixture]
[Category("sql")]
public class FlinkSqlIntegrationTest
{
    private const string InputTopic = "lt.flink.sql.input";
    private const string OutputTopic = "lt.flink.sql.output";

    [Test]
    public async Task FlinkSql_KafkaToKafka_WorksWhenConnectorsPresent()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.LocalTesting_FlinkSqlAppHost>(ct);
        var app = await appHost.BuildAsync(ct);
        await app.StartAsync(ct);

        try
        {
            await app.ResourceNotifications
                .WaitForResourceHealthyAsync("kafka", ct)
                .WaitAsync(TimeSpan.FromSeconds(60), ct);

            var kafka = await app.GetConnectionStringAsync("kafka", ct);
            await WaitForKafkaReady(kafka!, TimeSpan.FromSeconds(60), ct);

            await CreateTopicAsync(kafka!, InputTopic, 4);
            await CreateTopicAsync(kafka!, OutputTopic, 4);

            // Ensure Gateway up
            await WaitForHttpOkAsync("http://localhost:8080/api/v1/health", TimeSpan.FromSeconds(60), ct);

            // Submit SQL job (Kafka -> Kafka)
            var statements = new[]
            {
                $@"CREATE TABLE input (
                    `key` STRING,
                    `value` STRING
                  ) WITH (
                    'connector'='kafka',
                    'topic'='{InputTopic}',
                    'properties.bootstrap.servers'='{kafka}',
                    'properties.group.id'='flink-sql-it',
                    'scan.startup.mode'='earliest-offset',
                    'format'='json'
                  )",
                $@"CREATE TABLE output (
                    `key` STRING,
                    `value` STRING
                  ) WITH (
                    'connector'='kafka',
                    'topic'='{OutputTopic}',
                    'properties.bootstrap.servers'='{kafka}',
                    'format'='json'
                  )",
                "INSERT INTO output SELECT `key`, `value` FROM input"
            };

            var job = FlinkDotNet.Pipelines.FlinkDotNet.Sql(statements);
            var submitResult = await job.Submit("lt-sql-pipeline", ct);

            if (!submitResult.Success)
            {
                Assert.Fail("SQL submission failed: " + submitResult.ErrorMessage);
            }

            // Produce data to input
            await ProduceAsync(kafka!, InputTopic, 100, ct);
            var consumed = await ConsumeAsync(kafka!, OutputTopic, 100, TimeSpan.FromSeconds(60), ct);
            TestContext.WriteLine($"SQL pipeline consumed {consumed} records");
            Assert.That(consumed, Is.GreaterThan(0));
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
            GroupId = $"lt-flink-sql-consumer-{Guid.NewGuid()}",
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
}
