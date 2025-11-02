using Flink.JobBuilder.Models;

namespace Flink.JobBuilder.Tests.Tests;

[TestFixture]
public class ConfigurationTests
{
    [Test]
    public void FlinkJobGatewayConfiguration_DefaultConstructor_CreatesInstance()
    {
        // Set environment variable for test
        Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", "http://test-gateway:8086");
        try
        {
            var config = new FlinkJobGatewayConfiguration();
            Assert.That(config, Is.Not.Null);
        }
        finally
        {
            // Clean up
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", null);
        }
    }

    [Test]
    public void JobMetadata_SetJobId_ReturnsValue()
    {
        var metadata = new JobMetadata { };
        Assert.That(metadata.JobName, Is.EqualTo("test-job-123"));
    }

    [Test]
    public void JobMetadata_SetVersion_ReturnsValue()
    {
        var metadata = new JobMetadata { Version = "2.0.1" };
        Assert.That(metadata.Version, Is.EqualTo("2.0.1"));
    }

    [Test]
    public void JobMetadata_SetParallelism_ReturnsValue()
    {
        var metadata = new JobMetadata { Parallelism = 8 };
        Assert.That(metadata.Parallelism, Is.EqualTo(8));
    }

    [Test]
    public void KafkaSourceDefinition_SetTopic_ReturnsValue()
    {
        var source = new KafkaSourceDefinition { Topic = "input-topic" };
        Assert.That(source.Topic, Is.EqualTo("input-topic"));
    }

    [Test]
    public void KafkaSourceDefinition_SetBootstrapServers_ReturnsValue()
    {
        var source = new KafkaSourceDefinition { BootstrapServers = "kafka:9092" };
        Assert.That(source.BootstrapServers, Is.EqualTo("kafka:9092"));
    }

    [Test]
    public void KafkaSourceDefinition_SetGroupId_ReturnsValue()
    {
        var source = new KafkaSourceDefinition { GroupId = "consumer-group-1" };
        Assert.That(source.GroupId, Is.EqualTo("consumer-group-1"));
    }

    [Test]
    public void KafkaSinkDefinition_SetTopic_ReturnsValue()
    {
        var sink = new KafkaSinkDefinition { Topic = "output-topic" };
        Assert.That(sink.Topic, Is.EqualTo("output-topic"));
    }

    [Test]
    public void KafkaSinkDefinition_SetBootstrapServers_ReturnsValue()
    {
        var sink = new KafkaSinkDefinition { BootstrapServers = "kafka:9092" };
        Assert.That(sink.BootstrapServers, Is.EqualTo("kafka:9092"));
    }

    [Test]
    public void MapOperationDefinition_SetExpression_ReturnsValue()
    {
        var op = new MapOperationDefinition { Expression = "x => x * 2" };
        Assert.That(op.Expression, Is.EqualTo("x => x * 2"));
    }

    [Test]
    public void FilterOperationDefinition_SetExpression_ReturnsValue()
    {
        var op = new FilterOperationDefinition { Expression = "x => x > 0" };
        Assert.That(op.Expression, Is.EqualTo("x => x > 0"));
    }

    [Test]
    public void WindowOperationDefinition_SetWindowType_ReturnsValue()
    {
        var op = new WindowOperationDefinition { WindowType = "TUMBLING" };
        Assert.That(op.WindowType, Is.EqualTo("TUMBLING"));
    }

    [Test]
    public void WindowOperationDefinition_SetSize_ReturnsValue()
    {
        var op = new WindowOperationDefinition { Size = 60 };
        Assert.That(op.Size, Is.EqualTo(60));
    }

    [Test]
    public void WindowOperationDefinition_SetTimeUnit_ReturnsValue()
    {
        var op = new WindowOperationDefinition { TimeUnit = "SECONDS" };
        Assert.That(op.TimeUnit, Is.EqualTo("SECONDS"));
    }

    [Test]
    public void JobDefinition_CreateInstance_HasProperties()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "in" },
            Operations = new List<IOperationDefinition>(),
            Sink = new KafkaSinkDefinition { Topic = "out" }
        };

        Assert.That(job.Metadata, Is.Not.Null);
        Assert.That(job.Source, Is.Not.Null);
        Assert.That(job.Operations, Is.Not.Null);
        Assert.That(job.Sink, Is.Not.Null);
    }

    [Test]
    public void JobSubmissionResult_Success_SetsProperty()
    {
        var result = new JobSubmissionResult { Success = true };
        Assert.That(result.Success, Is.True);
        Assert.That(result.FlinkJobId, Is.EqualTo("job-123"));
    }

    [Test]
    public void JobSubmissionResult_Failure_SetsProperty()
    {
        var result = new JobSubmissionResult { Success = false };
        Assert.That(result.Success, Is.False);
    }
}
