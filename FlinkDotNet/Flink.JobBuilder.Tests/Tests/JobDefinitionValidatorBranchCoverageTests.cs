using Flink.JobBuilder.Models;
using Flink.JobBuilder.Services;

namespace Flink.JobBuilder.Tests.Tests;

/// <summary>
/// Comprehensive tests to achieve 100% branch coverage for JobDefinitionValidator
/// </summary>
[TestFixture]
public class JobDefinitionValidatorBranchCoverageTests
{
    #region Metadata Edge Cases

    [Test]
    public void Validate_WithWhitespaceJobId_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "   ", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test" },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("metadata.jobId is required"));
    }

    [Test]
    public void Validate_WithWhitespaceVersion_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "  " },
            Source = new KafkaSourceDefinition { Topic = "test" },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("metadata.version is required"));
    }

    #endregion

    #region Job Structure Edge Cases

    [Test]
    public void Validate_WithSqlSourceAndNoSink_IsValid()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT * FROM table" } },
            Sink = null
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_WithNonSqlSourceAndNoSink_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = null
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("sink is required"));
    }

    #endregion

    #region Source Validation - All Source Types

    [Test]
    public void Validate_SqlSource_WithNullStatements_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new SqlSourceDefinition { Statements = null }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("source.sql.statements must contain at least one statement"));
    }

    [Test]
    public void Validate_SqlSource_WithEmptyStatements_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new SqlSourceDefinition { Statements = new List<string>() }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("source.sql.statements must contain at least one statement"));
    }

    [Test]
    public void Validate_KafkaSource_WithWhitespaceTopic_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "  " },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("source.kafka.topic is required"));
    }

    [Test]
    public void Validate_FileSource_WithWhitespacePath_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new FileSourceDefinition { Path = "  ", Format = "csv" },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("source.file.path is required"));
    }

    [Test]
    public void Validate_FileSource_WithWhitespaceFormat_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new FileSourceDefinition { Path = "/data/file.csv", Format = "  " },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("source.file.format is required"));
    }

    [Test]
    public void Validate_HttpSource_WithWhitespaceUrl_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new HttpSourceDefinition { Url = "  ", IntervalSeconds = 10 },
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
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new HttpSourceDefinition { Url = "http://example.com", IntervalSeconds = 0 },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("source.http.intervalSeconds must be > 0"));
    }

    [Test]
    public void Validate_HttpSource_WithNegativeInterval_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new HttpSourceDefinition { Url = "http://example.com", IntervalSeconds = -1 },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("source.http.intervalSeconds must be > 0"));
    }

    [Test]
    public void Validate_DatabaseSource_WithWhitespaceConnectionString_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new DatabaseSourceDefinition
            {
                ConnectionString = "  ",
                Query = "SELECT * FROM table",
                PollingIntervalSeconds = 10
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("source.database.connectionString is required"));
    }

    [Test]
    public void Validate_DatabaseSource_WithWhitespaceQuery_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new DatabaseSourceDefinition
            {
                ConnectionString = "Server=localhost",
                Query = "  ",
                PollingIntervalSeconds = 10
            },
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
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new DatabaseSourceDefinition
            {
                ConnectionString = "Server=localhost",
                Query = "SELECT * FROM table",
                PollingIntervalSeconds = 0
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("source.database.pollingIntervalSeconds must be > 0"));
    }

    [Test]
    public void Validate_DatabaseSource_WithNegativePollingInterval_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new DatabaseSourceDefinition
            {
                ConnectionString = "Server=localhost",
                Query = "SELECT * FROM table",
                PollingIntervalSeconds = -1
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("source.database.pollingIntervalSeconds must be > 0"));
    }

    #endregion

    #region Operations Validation

    [Test]
    public void Validate_FilterOperation_WithWhitespaceExpression_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new FilterOperationDefinition { Expression = "  " }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].filter.expression is required"));
    }

    [Test]
    public void Validate_MapOperation_WithWhitespaceExpression_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new MapOperationDefinition { Expression = "  " }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].map.expression is required"));
    }

    [Test]
    public void Validate_GroupByOperation_WithNoKeyOrKeys_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new GroupByOperationDefinition { Key = null, Keys = null }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].groupBy.key or keys is required"));
    }

    [Test]
    public void Validate_GroupByOperation_WithEmptyKeyAndEmptyKeys_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new GroupByOperationDefinition { Key = "", Keys = new List<string>() }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].groupBy.key or keys is required"));
    }

    [Test]
    public void Validate_GroupByOperation_WithWhitespaceKeyAndEmptyKeys_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new GroupByOperationDefinition { Key = "  ", Keys = new List<string>() }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].groupBy.key or keys is required"));
    }

    [Test]
    public void Validate_AggregateOperation_WithInvalidAggregationType_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new AggregateOperationDefinition { AggregationType = "INVALID", Field = "value" }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].aggregate.aggregationType must be one of SUM, COUNT, AVG, MIN, MAX, COLLECT"));
    }

    [Test]
    public void Validate_AggregateOperation_WithWhitespaceAggregationType_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new AggregateOperationDefinition { AggregationType = "  ", Field = "value" }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].aggregate.aggregationType must be one of SUM, COUNT, AVG, MIN, MAX, COLLECT"));
    }

    [Test]
    public void Validate_AggregateOperation_WithWhitespaceField_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new AggregateOperationDefinition { AggregationType = "SUM", Field = "  " }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].aggregate.field is required"));
    }

    [Test]
    public void Validate_WindowOperation_WithInvalidWindowType_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new WindowOperationDefinition
                {
                    WindowType = "INVALID",
                    Size = 10,
                    TimeUnit = "SECONDS"
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].window.windowType must be one of TUMBLING, SLIDING, SESSION"));
    }

    [Test]
    public void Validate_WindowOperation_WithWhitespaceWindowType_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new WindowOperationDefinition
                {
                    WindowType = "  ",
                    Size = 10,
                    TimeUnit = "SECONDS"
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].window.windowType must be one of TUMBLING, SLIDING, SESSION"));
    }

    [Test]
    public void Validate_WindowOperation_WithZeroSize_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new WindowOperationDefinition
                {
                    WindowType = "TUMBLING",
                    Size = 0,
                    TimeUnit = "SECONDS"
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].window.size must be > 0"));
    }

    [Test]
    public void Validate_WindowOperation_WithNegativeSize_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new WindowOperationDefinition
                {
                    WindowType = "TUMBLING",
                    Size = -1,
                    TimeUnit = "SECONDS"
                }
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
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new WindowOperationDefinition
                {
                    WindowType = "TUMBLING",
                    Size = 10,
                    TimeUnit = "DAYS"
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].window.timeUnit must be one of SECONDS, MINUTES, HOURS"));
    }

    [Test]
    public void Validate_WindowOperation_WithWhitespaceTimeUnit_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new WindowOperationDefinition
                {
                    WindowType = "TUMBLING",
                    Size = 10,
                    TimeUnit = "  "
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].window.timeUnit must be one of SECONDS, MINUTES, HOURS"));
    }

    [Test]
    public void Validate_WindowOperation_SlidingWithNoSlide_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new WindowOperationDefinition
                {
                    WindowType = "SLIDING",
                    Size = 10,
                    TimeUnit = "SECONDS",
                    Slide = null
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].window.slide is required and must be > 0 for SLIDING windows"));
    }

    [Test]
    public void Validate_WindowOperation_SlidingWithZeroSlide_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new WindowOperationDefinition
                {
                    WindowType = "SLIDING",
                    Size = 10,
                    TimeUnit = "SECONDS",
                    Slide = 0
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].window.slide is required and must be > 0 for SLIDING windows"));
    }

    [Test]
    public void Validate_WindowOperation_SlidingWithNegativeSlide_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new WindowOperationDefinition
                {
                    WindowType = "SLIDING",
                    Size = 10,
                    TimeUnit = "SECONDS",
                    Slide = -1
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].window.slide is required and must be > 0 for SLIDING windows"));
    }

    [Test]
    public void Validate_WindowOperation_SlidingCaseInsensitive_WithNoSlide_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new WindowOperationDefinition
                {
                    WindowType = "sliding",
                    Size = 10,
                    TimeUnit = "SECONDS",
                    Slide = null
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].window.slide is required and must be > 0 for SLIDING windows"));
    }

    [Test]
    public void Validate_JoinOperation_WithNullRightSource_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new JoinOperationDefinition
                {
                    RightSource = null,
                    LeftKey = "id",
                    RightKey = "id"
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].join.rightSource is required"));
    }

    [Test]
    public void Validate_JoinOperation_WithWhitespaceLeftKey_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new JoinOperationDefinition
                {
                    RightSource = new KafkaSourceDefinition { Topic = "right" },
                    LeftKey = "  ",
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
    public void Validate_JoinOperation_WithWhitespaceRightKey_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new JoinOperationDefinition
                {
                    RightSource = new KafkaSourceDefinition { Topic = "right" },
                    LeftKey = "id",
                    RightKey = "  "
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].join.rightKey is required"));
    }

    [Test]
    public void Validate_AsyncFunctionOperation_WithWhitespaceFunctionType_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new AsyncFunctionOperationDefinition
                {
                    FunctionType = "  ",
                    TimeoutMs = 1000,
                    MaxRetries = 3
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].asyncFunction.functionType is required"));
    }

    [Test]
    public void Validate_AsyncFunctionOperation_WithNegativeTimeout_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new AsyncFunctionOperationDefinition
                {
                    FunctionType = "http",
                    TimeoutMs = -1,
                    MaxRetries = 3
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].asyncFunction.timeoutMs must be between 1 and 1200000"));
    }

    [Test]
    public void Validate_AsyncFunctionOperation_WithExcessiveRetries_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new AsyncFunctionOperationDefinition
                {
                    FunctionType = "http",
                    TimeoutMs = 1000,
                    MaxRetries = 101
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].asyncFunction.maxRetries must be between 0 and 100"));
    }

    [Test]
    public void Validate_ProcessFunctionOperation_WithWhitespaceProcessType_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new ProcessFunctionOperationDefinition { ProcessType = "  " }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].processFunction.processType is required"));
    }

    [Test]
    public void Validate_StateOperation_WithInvalidStateType_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new StateOperationDefinition
                {
                    StateType = "invalid",
                    StateKey = "key1"
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].state.stateType must be one of value, list, map, reducing"));
    }

    [Test]
    public void Validate_StateOperation_WithWhitespaceStateType_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new StateOperationDefinition
                {
                    StateType = "  ",
                    StateKey = "key1"
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].state.stateType must be one of value, list, map, reducing"));
    }

    [Test]
    public void Validate_StateOperation_WithWhitespaceStateKey_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new StateOperationDefinition
                {
                    StateType = "value",
                    StateKey = "  "
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].state.stateKey is required"));
    }

    [Test]
    public void Validate_StateOperation_WithZeroTtl_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new StateOperationDefinition
                {
                    StateType = "value",
                    StateKey = "key1",
                    TtlMs = 0
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].state.ttlMs must be > 0 when provided"));
    }

    [Test]
    public void Validate_StateOperation_WithNegativeTtl_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new StateOperationDefinition
                {
                    StateType = "value",
                    StateKey = "key1",
                    TtlMs = -1
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].state.ttlMs must be > 0 when provided"));
    }

    [Test]
    public void Validate_TimerOperation_WithInvalidTimerType_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new TimerOperationDefinition
                {
                    TimerType = "invalid",
                    DelayMs = 1000
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].timer.timerType must be one of processing, event"));
    }

    [Test]
    public void Validate_TimerOperation_WithWhitespaceTimerType_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new TimerOperationDefinition
                {
                    TimerType = "  ",
                    DelayMs = 1000
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].timer.timerType must be one of processing, event"));
    }

    [Test]
    public void Validate_TimerOperation_WithZeroDelay_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new TimerOperationDefinition
                {
                    TimerType = "processing",
                    DelayMs = 0
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].timer.delayMs must be between 1 and 86400000"));
    }

    [Test]
    public void Validate_TimerOperation_WithNegativeDelay_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new TimerOperationDefinition
                {
                    TimerType = "processing",
                    DelayMs = -1
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].timer.delayMs must be between 1 and 86400000"));
    }

    [Test]
    public void Validate_TimerOperation_WithExcessiveDelay_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new TimerOperationDefinition
                {
                    TimerType = "processing",
                    DelayMs = 86_400_001
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].timer.delayMs must be between 1 and 86400000"));
    }

    [Test]
    public void Validate_RetryOperation_WithNegativeMaxRetries_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new RetryOperationDefinition
                {
                    MaxRetries = -1,
                    DelayMs = new List<long> { 1000 },
                    StateKey = "retry-state"
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].retry.maxRetries must be between 0 and 100"));
    }

    [Test]
    public void Validate_RetryOperation_WithExcessiveMaxRetries_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new RetryOperationDefinition
                {
                    MaxRetries = 101,
                    DelayMs = new List<long> { 1000 },
                    StateKey = "retry-state"
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].retry.maxRetries must be between 0 and 100"));
    }

    [Test]
    public void Validate_RetryOperation_WithNullDelayMs_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new RetryOperationDefinition
                {
                    MaxRetries = 3,
                    DelayMs = null,
                    StateKey = "retry-state"
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].retry.delayMs must contain at least 1 value"));
    }

    [Test]
    public void Validate_RetryOperation_WithEmptyDelayMs_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new RetryOperationDefinition
                {
                    MaxRetries = 3,
                    DelayMs = new List<long>(),
                    StateKey = "retry-state"
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].retry.delayMs must contain at least 1 value"));
    }

    [Test]
    public void Validate_RetryOperation_WithZeroDelayMs_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new RetryOperationDefinition
                {
                    MaxRetries = 3,
                    DelayMs = new List<long> { 1000, 0, 3000 },
                    StateKey = "retry-state"
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].retry.delayMs values must be > 0"));
    }

    [Test]
    public void Validate_RetryOperation_WithNegativeDelayMs_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new RetryOperationDefinition
                {
                    MaxRetries = 3,
                    DelayMs = new List<long> { 1000, -1, 3000 },
                    StateKey = "retry-state"
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].retry.delayMs values must be > 0"));
    }

    [Test]
    public void Validate_RetryOperation_WithWhitespaceStateKey_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new RetryOperationDefinition
                {
                    MaxRetries = 3,
                    DelayMs = new List<long> { 1000 },
                    StateKey = "  "
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].retry.stateKey is required"));
    }

    [Test]
    public void Validate_SideOutputOperation_WithWhitespaceOutputTag_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new SideOutputOperationDefinition
                {
                    OutputTag = "  ",
                    Condition = "value > 100",
                    SideOutputSink = new KafkaSinkDefinition { Topic = "side" }
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].sideOutput.outputTag is required"));
    }

    [Test]
    public void Validate_SideOutputOperation_WithWhitespaceCondition_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new SideOutputOperationDefinition
                {
                    OutputTag = "high-values",
                    Condition = "  ",
                    SideOutputSink = new KafkaSinkDefinition { Topic = "side" }
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].sideOutput.condition is required"));
    }

    [Test]
    public void Validate_SideOutputOperation_WithNullSideOutputSink_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new SideOutputOperationDefinition
                {
                    OutputTag = "high-values",
                    Condition = "value > 100",
                    SideOutputSink = null
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].sideOutput.sideOutputSink is required"));
    }

    #endregion

    #region Sink Validation - All Sink Types

    [Test]
    public void Validate_KafkaSink_WithWhitespaceTopic_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "  " }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("sink.kafka.topic is required"));
    }

    [Test]
    public void Validate_KafkaSink_WithInvalidSerializer_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output", Serializer = "avro" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("sink.kafka.serializer must be 'json' or 'string' when provided"));
    }

    [Test]
    public void Validate_FileSink_WithWhitespacePath_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new FileSinkDefinition { Path = "  ", Format = "csv" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("sink.file.path is required"));
    }

    [Test]
    public void Validate_FileSink_WithWhitespaceFormat_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new FileSinkDefinition { Path = "/data/output.csv", Format = "  " }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("sink.file.format is required"));
    }

    [Test]
    public void Validate_HttpSink_WithWhitespaceUrl_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new HttpSinkDefinition { Url = "  ", TimeoutMs = 5000 }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("sink.http.url is required"));
    }

    [Test]
    public void Validate_HttpSink_WithZeroTimeout_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new HttpSinkDefinition { Url = "http://example.com", TimeoutMs = 0 }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("sink.http.timeoutMs must be between 1 and 1200000"));
    }

    [Test]
    public void Validate_HttpSink_WithNegativeTimeout_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new HttpSinkDefinition { Url = "http://example.com", TimeoutMs = -1 }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("sink.http.timeoutMs must be between 1 and 1200000"));
    }

    [Test]
    public void Validate_HttpSink_WithExcessiveTimeout_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new HttpSinkDefinition { Url = "http://example.com", TimeoutMs = 1_300_000 }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("sink.http.timeoutMs must be between 1 and 1200000"));
    }

    [Test]
    public void Validate_DatabaseSink_WithWhitespaceConnectionString_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new DatabaseSinkDefinition { ConnectionString = "  ", Table = "output_table" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("sink.database.connectionString is required"));
    }

    [Test]
    public void Validate_DatabaseSink_WithWhitespaceTable_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new DatabaseSinkDefinition { ConnectionString = "Server=localhost", Table = "  " }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("sink.database.table is required"));
    }

    [Test]
    public void Validate_RedisSink_WithWhitespaceConnectionString_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new RedisSinkDefinition { ConnectionString = "  ", OperationType = "SET" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("sink.redis.connectionString is required"));
    }

    [Test]
    public void Validate_RedisSink_WithWhitespaceOperationType_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new RedisSinkDefinition { ConnectionString = "localhost:6379", OperationType = "  " }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("sink.redis.operationType is required"));
    }

    #endregion

    #region ConsoleSinkDefinition Tests (Not in switch statement)

    [Test]
    public void Validate_WithConsoleSink_IsValid()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new ConsoleSinkDefinition()
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.True);
    }

    #endregion
}
