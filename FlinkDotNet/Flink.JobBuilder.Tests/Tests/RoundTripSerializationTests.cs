using System.Text.Json;
using Flink.JobBuilder.Models;
using NUnit.Framework;

namespace Flink.JobBuilder.Tests.Tests;

public class RoundTripSerializationTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        TypeInfoResolver = null
    };

    [Test]
    public void JobDefinition_RoundTrip_PreservesTypesAndValues()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata
            {
                JobId = "job-1",
                JobName = "Test Job",
                Version = "1.0",
                Parallelism = 2
            },
            Source = new KafkaSourceDefinition
            {
                Topic = "input-topic",
                BootstrapServers = "localhost:9092",
                GroupId = "group-1",
                StartingOffsets = "latest"
            },
            Operations =
            [
                new MapOperationDefinition { Expression = "x => x" },
                new FilterOperationDefinition { Expression = "x != null" },
                new WindowOperationDefinition { WindowType = "TUMBLING", Size = 1, TimeUnit = "MINUTES" }
            ],
            Sink = new KafkaSinkDefinition
            {
                Topic = "output-topic",
                Serializer = "json"
            }
        };

        var json = JsonSerializer.Serialize(job, Options);
        var roundTrip = JsonSerializer.Deserialize<JobDefinition>(json, Options);

        Assert.That(roundTrip, Is.Not.Null);
        Assert.That(roundTrip!.Metadata.JobId, Is.EqualTo("job-1"));
        Assert.That(roundTrip.Source, Is.TypeOf<KafkaSourceDefinition>());
        Assert.That(roundTrip.Operations.Count, Is.EqualTo(3));
        Assert.That(roundTrip.Operations[0], Is.TypeOf<MapOperationDefinition>());
        Assert.That(roundTrip.Operations[1], Is.TypeOf<FilterOperationDefinition>());
        Assert.That(roundTrip.Operations[2], Is.TypeOf<WindowOperationDefinition>());
        Assert.That(roundTrip.Sink, Is.TypeOf<KafkaSinkDefinition>());
        Assert.That(roundTrip.Sink, Is.Not.Null);
        Assert.That(((KafkaSinkDefinition)roundTrip.Sink!).Topic, Is.EqualTo("output-topic"));
    }

    [Test]
    public void SqlJob_RoundTrip_AllowsSinkless()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "sql-1", Version = "1.0", Parallelism = 1 },
            Source = new SqlSourceDefinition
            {
                Statements = new List<string>
                {
                    "CREATE TABLE input (id STRING) WITH ('connector'='datagen')",
                    "CREATE TABLE output (id STRING) WITH ('connector'='blackhole')",
                    "INSERT INTO output SELECT id FROM input"
                }
            },
            Sink = null!
        };

        var json = JsonSerializer.Serialize(job, Options);
        var roundTrip = JsonSerializer.Deserialize<JobDefinition>(json, Options);

        Assert.That(roundTrip, Is.Not.Null);
        Assert.That(roundTrip!.Source, Is.TypeOf<SqlSourceDefinition>());
        Assert.That(((SqlSourceDefinition)roundTrip.Source).Statements.Count, Is.EqualTo(3));
    }
}
