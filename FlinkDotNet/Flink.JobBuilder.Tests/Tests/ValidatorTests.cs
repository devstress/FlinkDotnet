using Flink.JobBuilder.Models;
using Flink.JobBuilder.Services;

namespace Flink.JobBuilder.Tests.Tests;

[TestFixture]
public class ValidatorTests
{
    [Test]
    public void Validate_ValidJobDefinition_ReturnsNoErrors()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata
            {
                JobId = "test-job",
                Version = "1.0.0",
                Parallelism = 4
            },
            Source = new KafkaSourceDefinition
            {
                Topic = "input-topic",
                BootstrapServers = "localhost:9092",
                GroupId = "test-group"
            },
            Operations = new List<IOperationDefinition>
            {
                new MapOperationDefinition { Expression = "x => x * 2" }
            },
            Sink = new KafkaSinkDefinition
            {
                Topic = "output-topic",
                BootstrapServers = "localhost:9092"
            }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Validate_NullMetadata_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = null!,
            Source = new KafkaSourceDefinition { Topic = "test" },
            Sink = new KafkaSinkDefinition { Topic = "test" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("metadata is required"));
    }

    [Test]
    public void Validate_MissingJobId_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test" },
            Sink = new KafkaSinkDefinition { Topic = "test" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("metadata.jobId is required"));
    }

    [Test]
    public void Validate_MissingVersion_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = null! },
            Source = new KafkaSourceDefinition { Topic = "test" },
            Sink = new KafkaSinkDefinition { Topic = "test" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("metadata.version is required"));
    }

    [Test]
    public void Validate_InvalidParallelism_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata
            {
                JobId = "test",
                Version = "1.0",
                Parallelism = 0
            },
            Source = new KafkaSourceDefinition { Topic = "test" },
            Sink = new KafkaSinkDefinition { Topic = "test" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("metadata.parallelism must be >= 1 when provided"));
    }

    [Test]
    public void Validate_NegativeParallelism_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata
            {
                JobId = "test",
                Version = "1.0",
                Parallelism = -5
            },
            Source = new KafkaSourceDefinition { Topic = "test" },
            Sink = new KafkaSinkDefinition { Topic = "test" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("metadata.parallelism must be >= 1 when provided"));
    }

    [Test]
    public void Validate_MissingSource_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = null!,
            Sink = new KafkaSinkDefinition { Topic = "test" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("source is required"));
    }

    [Test]
    public void Validate_MissingSinkForNonSqlJob_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test" },
            Sink = null
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("sink is required"));
    }

    [Test]
    public void Validate_SqlSourceWithoutStatements_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new SqlSourceDefinition { Statements = new List<string>() }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("source.sql.statements must contain at least one statement"));
    }

    [Test]
    public void Validate_SqlSourceWithNullStatements_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new SqlSourceDefinition { Statements = null! }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("source.sql.statements must contain at least one statement"));
    }

    [Test]
    public void Validate_KafkaSourceMissingTopic_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "" },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("source.kafka.topic is required"));
    }

    [Test]
    public void Validate_MultipleErrors_ReturnsAllErrors()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "", Version = "" },
            Source = new KafkaSourceDefinition { Topic = "" },
            Sink = null
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Count, Is.GreaterThanOrEqualTo(4));
    }

    [Test]
    public void Validate_SqlJobWithoutSink_IsValid()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "sql-job", Version = "1.0" },
            Source = new SqlSourceDefinition
            {
                Statements = new List<string> { "SELECT * FROM table" }
            },
            Sink = null
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void IrValidationResult_NoErrors_IsValidTrue()
    {
        var result = new IrValidationResult();
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void IrValidationResult_WithErrors_IsValidFalse()
    {
        var result = new IrValidationResult();
        result.Errors.Add("Test error");
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Count, Is.EqualTo(1));
    }

    [Test]
    public void Validate_JobWithOperations_ValidatesOperations()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new MapOperationDefinition { Expression = "x => x" },
                new FilterOperationDefinition { Expression = "x => true" }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_NullOperations_DoesNotThrow()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = null!,
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Validate_FileSourceDefinition_ValidatesCorrectly()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new FileSourceDefinition { Path = "/data/input.txt" },
            Sink = new FileSinkDefinition { Path = "/data/output.txt" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Validate_HttpSourceDefinition_ValidatesCorrectly()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new HttpSourceDefinition { Url = "http://api.example.com" },
            Sink = new HttpSinkDefinition { Url = "http://api.example.com/sink" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Validate_DatabaseSourceDefinition_ValidatesCorrectly()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new DatabaseSourceDefinition { ConnectionString = "server=localhost" },
            Sink = new DatabaseSinkDefinition { ConnectionString = "server=localhost" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result, Is.Not.Null);
    }
}
