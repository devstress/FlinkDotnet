using Flink.JobBuilder.Models;
using FlinkDotNet.DataStream;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests;

/// <summary>
/// Branch coverage tests for DataStream methods with JobDefinition-backed streams
/// Covers the branch where _job is not null but _operationCapture is null
/// </summary>
[TestFixture]
public class DataStreamJobBackedBranchCoverageTests
{
    [Test]
    public void Map_WithJobDefinitionBackedStream_ReturnsJobBackedStream()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var jobDef = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "test-topic", BootstrapServers = "localhost:9092" },
            Operations = [],
            Metadata = new JobMetadata { JobId = "test-job", JobName = "Test Job" }
        };

        // Create a DataStream directly with JobDefinition (no OperationCapture)
        var stream = new DataStream<string>(jobDef, env);

        // Act
        var mappedStream = stream.Map(x => x.ToUpper());

        // Assert
        Assert.That(mappedStream, Is.Not.Null);
    }

    [Test]
    public void Filter_WithJobDefinitionBackedStream_ReturnsJobBackedStream()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var jobDef = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "test-topic", BootstrapServers = "localhost:9092" },
            Operations = [],
            Metadata = new JobMetadata { JobId = "test-job", JobName = "Test Job" }
        };

        // Create a DataStream directly with JobDefinition (no OperationCapture)
        var stream = new DataStream<string>(jobDef, env);

        // Act
        var filteredStream = stream.Filter(x => x.Length > 5);

        // Assert
        Assert.That(filteredStream, Is.Not.Null);
    }

    [Test]
    public void FlatMap_WithJobDefinitionBackedStream_ReturnsJobBackedStream()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var jobDef = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "test-topic", BootstrapServers = "localhost:9092" },
            Operations = [],
            Metadata = new JobMetadata { JobId = "test-job", JobName = "Test Job" }
        };

        // Create a DataStream directly with JobDefinition (no OperationCapture)
        var stream = new DataStream<string>(jobDef, env);

        // Act
        var flatMappedStream = stream.FlatMap(x => x.Split(' '));

        // Assert
        Assert.That(flatMappedStream, Is.Not.Null);
    }

    [Test]
    public void CreateJobDefinitionBackedStream_WithNullJob_CreatesNewJobDefinition()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        // Create a stream with OperationCapture (FromKafka scenario)
        var kafkaStream = env.FromKafka("test-topic", "localhost:9092");

        // Act - Map triggers CreateJobDefinitionBackedStream
        var mappedStream = kafkaStream.Map(x => x.ToUpper());

        // Assert
        Assert.That(mappedStream, Is.Not.Null);
    }
}
