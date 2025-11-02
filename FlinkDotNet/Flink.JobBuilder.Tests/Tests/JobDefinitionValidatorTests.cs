using Flink.JobBuilder.Models;
using Flink.JobBuilder.Services;

namespace Flink.JobBuilder.Tests.Tests;

[TestFixture]
public class JobDefinitionValidatorTests
{
    #region IrValidationResult Tests

    [Test]
    public void IrValidationResult_IsValid_ReturnsTrueWhenNoErrors()
    {
        var result = new IrValidationResult();

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void IrValidationResult_IsValid_ReturnsFalseWhenHasErrors()
    {
        var result = new IrValidationResult();
        result.Errors.Add("Error message");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Count.EqualTo(1));
    }

    #endregion

    #region Metadata Validation Tests

    [Test]
    public void Validate_WithNullMetadata_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = null!,
            Source = new KafkaSourceDefinition { Topic = "test" },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("metadata is required"));
    }

    [Test]
    public void Validate_WithEmptyJobId_ReturnsError()
    {
        // JobId is no longer required - test now validates job is valid without it
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
            Sink = new KafkaSinkDefinition { Topic = "output", BootstrapServers = "localhost:9092" }
        };

        var result = JobDefinitionValidator.Validate(job);

        // Should be valid now since JobId is no longer required
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_WithEmptyVersion_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "" },
            Source = new KafkaSourceDefinition { Topic = "test" },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("metadata.version is required"));
    }

    [Test]
    public void Validate_WithInvalidParallelism_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0", Parallelism = 0 },
            Source = new KafkaSourceDefinition { Topic = "test" },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("metadata.parallelism must be >= 1 when provided"));
    }

    [Test]
    public void Validate_WithNegativeParallelism_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0", Parallelism = -1 },
            Source = new KafkaSourceDefinition { Topic = "test" },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("metadata.parallelism must be >= 1 when provided"));
    }

    #endregion

    #region Job Structure Validation Tests

    [Test]
    public void Validate_WithNullSource_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = null!,
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("source is required"));
    }

    [Test]
    public void Validate_WithNullSinkForNonSqlJob_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test" },
            Sink = null
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("sink is required"));
    }

    [Test]
    public void Validate_SqlJobWithoutSink_IsValid()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT * FROM table" } },
            Sink = null
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.True);
    }

    #endregion

    #region Source Validation Tests

    [Test]
    public void Validate_SqlSource_WithEmptyStatements_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new SqlSourceDefinition { Statements = new List<string>() },
            Sink = null
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("source.sql.statements must contain at least one statement"));
    }

    [Test]
    public void Validate_KafkaSource_WithEmptyTopic_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "" },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("source.kafka.topic is required"));
    }

    [Test]
    public void Validate_FileSource_WithEmptyPath_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new FileSourceDefinition { Path = "", Format = "json" },
            Sink = new FileSinkDefinition { Path = "/output", Format = "json" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("source.file.path is required"));
    }

    [Test]
    public void Validate_FileSource_WithEmptyFormat_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new FileSourceDefinition { Path = "/data", Format = "" },
            Sink = new FileSinkDefinition { Path = "/output", Format = "json" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("source.file.format is required"));
    }

    [Test]
    public void Validate_HttpSource_WithEmptyUrl_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new HttpSourceDefinition { Url = "" },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("source.http.url is required"));
    }

    [Test]
    public void Validate_HttpSource_WithZeroInterval_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new HttpSourceDefinition { Url = "http://api.example.com", IntervalSeconds = 0 },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("source.http.intervalSeconds must be > 0"));
    }

    [Test]
    public void Validate_DatabaseSource_WithEmptyConnectionString_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new DatabaseSourceDefinition { ConnectionString = "", Query = "SELECT *" },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("source.database.connectionString is required"));
    }

    [Test]
    public void Validate_DatabaseSource_WithEmptyQuery_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new DatabaseSourceDefinition { ConnectionString = "Server=localhost", Query = "" },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("source.database.query is required"));
    }

    [Test]
    public void Validate_DatabaseSource_WithZeroPollingInterval_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new DatabaseSourceDefinition
            {
                ConnectionString = "Server=localhost",
                Query = "SELECT *",
                PollingIntervalSeconds = 0
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("source.database.pollingIntervalSeconds must be > 0"));
    }

    #endregion

    #region Operation Validation Tests

    [Test]
    public void Validate_FilterOperation_WithEmptyExpression_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new FilterOperationDefinition { Expression = "" }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].filter.expression is required"));
    }

    [Test]
    public void Validate_MapOperation_WithEmptyExpression_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new MapOperationDefinition { Expression = "" }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].map.expression is required"));
    }

    [Test]
    public void Validate_GroupByOperation_WithNoKeys_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new GroupByOperationDefinition { Key = "", Keys = null }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].groupBy.key or keys is required"));
    }

    [Test]
    public void Validate_AggregateOperation_WithInvalidType_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new AggregateOperationDefinition { AggregationType = "INVALID", Field = "value" }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.Contains("aggregationType must be one of")), Is.True);
    }

    [Test]
    public void Validate_AggregateOperation_WithEmptyField_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new AggregateOperationDefinition { AggregationType = "SUM", Field = "" }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].aggregate.field is required"));
    }

    [Test]
    public void Validate_WindowOperation_WithInvalidType_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new WindowOperationDefinition { WindowType = "INVALID", Size = 60 }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.Contains("windowType must be one of")), Is.True);
    }

    [Test]
    public void Validate_WindowOperation_WithZeroSize_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new WindowOperationDefinition { WindowType = "TUMBLING", Size = 0 }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].window.size must be > 0"));
    }

    [Test]
    public void Validate_WindowOperation_WithInvalidTimeUnit_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new WindowOperationDefinition { WindowType = "TUMBLING", Size = 60, TimeUnit = "DAYS" }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.Contains("timeUnit must be one of")), Is.True);
    }

    [Test]
    public void Validate_SlidingWindow_WithoutSlide_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new WindowOperationDefinition { WindowType = "SLIDING", Size = 60, TimeUnit = "SECONDS" }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.Contains("slide is required") && e.Contains("SLIDING")), Is.True);
    }

    [Test]
    public void Validate_JoinOperation_WithoutRightSource_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new JoinOperationDefinition { RightSource = null!, LeftKey = "id", RightKey = "id" }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].join.rightSource is required"));
    }

    [Test]
    public void Validate_JoinOperation_WithoutLeftKey_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new JoinOperationDefinition
                {
                    RightSource = new KafkaSourceDefinition { Topic = "right" },
                    LeftKey = "",
                    RightKey = "id"
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].join.leftKey is required"));
    }

    [Test]
    public void Validate_JoinOperation_WithoutRightKey_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new JoinOperationDefinition
                {
                    RightSource = new KafkaSourceDefinition { Topic = "right" },
                    LeftKey = "id",
                    RightKey = ""
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].join.rightKey is required"));
    }

    [Test]
    public void Validate_ValidJob_ReturnsNoErrors()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0", Parallelism = 4 },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new MapOperationDefinition { Expression = "x => x.ToUpper()" },
                new FilterOperationDefinition { Expression = "x => x.Length > 0" }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    #endregion
}
