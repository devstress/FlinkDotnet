using Flink.JobBuilder.Models;
using Flink.JobBuilder.Services;
using NUnit.Framework;

namespace Flink.JobBuilder.Tests.Tests;

public class JobDefinitionValidatorTests
{
    private static JobDefinition CreateValidJob()
    {
        return new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "j1", Version = "1.0", Parallelism = 1 },
            Source = new KafkaSourceDefinition { Topic = "t-in" },
            Operations =
            [
                new MapOperationDefinition { Expression = "x => x" },
                new WindowOperationDefinition { WindowType = "TUMBLING", Size = 5, TimeUnit = "SECONDS" }
            ],
            Sink = new KafkaSinkDefinition { Topic = "t-out", Serializer = "json" }
        };
    }

    [Test]
    public void Validate_ValidJob_IsValid()
    {
        var job = CreateValidJob();
        var result = JobDefinitionValidator.Validate(job);
        Assert.That(result.IsValid, Is.True, string.Join("; ", result.Errors));
    }

    [Test]
    public void Validate_MissingKafkaTopic_Fails()
    {
        var job = CreateValidJob();
        ((KafkaSourceDefinition)job.Source).Topic = string.Empty;
        var result = JobDefinitionValidator.Validate(job);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("source.kafka.topic"));
    }

    [Test]
    public void Validate_SlidingWindowRequiresSlide_Fails()
    {
        var job = CreateValidJob();
        job.Operations.Add(new WindowOperationDefinition { WindowType = "SLIDING", Size = 10, TimeUnit = "SECONDS" });
        var result = JobDefinitionValidator.Validate(job);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("window.slide"));
    }

    [Test]
    public void Validate_TimerDelay_OutOfRange_Fails()
    {
        var job = CreateValidJob();
        job.Operations.Add(new TimerOperationDefinition { TimerType = "processing", DelayMs = 0 });
        var result = JobDefinitionValidator.Validate(job);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("timer.delayMs"));
    }

    [Test]
    public void Validate_SqlJob_AllowsMissingSink()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "sql-job", Version = "1.0" },
            Source = new SqlSourceDefinition { Statements = new List<string> { "CREATE TABLE t(x STRING) WITH ('connector'='blackhole')" } },
            Sink = null!
        };
        var result = JobDefinitionValidator.Validate(job);
        Assert.That(result.IsValid, Is.True, string.Join("; ", result.Errors));
    }
}
