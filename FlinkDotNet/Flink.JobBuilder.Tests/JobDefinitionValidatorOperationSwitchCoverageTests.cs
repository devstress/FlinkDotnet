using Flink.JobBuilder.Models;
using Flink.JobBuilder.Services;

namespace Flink.JobBuilder.Tests;

/// <summary>
/// Tests to cover all switch statement branches in JobDefinitionValidator.ValidateOperation
/// and other uncovered validation branches to improve code coverage from 88.2% to 90.2%+
/// </summary>
[TestFixture]
public class JobDefinitionValidatorOperationSwitchCoverageTests
{
    #region ValidateOperation Switch Statement - All 12 Cases

    [Test]
    public void ValidateOperation_WithFilterOperation_CallsFilterValidation()
    {
        // Arrange - FilterOperation with invalid expression
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations =
            [
                new FilterOperationDefinition { Expression = "" } // Invalid: empty expression
            ],
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert - Should trigger ValidateFilterOperation
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("operations[0].filter.expression is required"));
    }

    [Test]
    public void ValidateOperation_WithMapOperation_CallsMapValidation()
    {
        // Arrange - MapOperation with invalid expression
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations =
            [
                new MapOperationDefinition { Expression = "" } // Invalid: empty expression
            ],
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert - Should trigger ValidateMapOperation
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("operations[0].map.expression is required"));
    }

    [Test]
    public void ValidateOperation_WithGroupByOperation_CallsGroupByValidation()
    {
        // Arrange - GroupByOperation with no key or keys
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations =
            [
                new GroupByOperationDefinition { Key = "", Keys = [] } // Invalid: no key or keys
            ],
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert - Should trigger ValidateGroupByOperation
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("operations[0].groupBy.key or keys is required"));
    }

    [Test]
    public void ValidateOperation_WithAggregateOperation_CallsAggregateValidation()
    {
        // Arrange - AggregateOperation with invalid aggregation type
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations =
            [
                new AggregateOperationDefinition { AggregationType = "INVALID", Field = "amount" } // Invalid type
            ],
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert - Should trigger ValidateAggregateOperation
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("operations[0].aggregate.aggregationType must be one of"));
    }

    [Test]
    public void ValidateOperation_WithWindowOperation_CallsWindowValidation()
    {
        // Arrange - WindowOperation with invalid window type
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations =
            [
                new WindowOperationDefinition { WindowType = "INVALID", Size = 10, TimeUnit = "SECONDS" } // Invalid type
            ],
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert - Should trigger ValidateWindowOperation
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("operations[0].window.windowType must be one of"));
    }

    [Test]
    public void ValidateOperation_WithJoinOperation_CallsJoinValidation()
    {
        // Arrange - JoinOperation with missing rightSource
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations =
            [
                new JoinOperationDefinition { RightSource = null, LeftKey = "id", RightKey = "id" } // Invalid: no rightSource
            ],
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert - Should trigger ValidateJoinOperation
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("operations[0].join.rightSource is required"));
    }

    [Test]
    public void ValidateOperation_WithAsyncFunctionOperation_CallsAsyncFunctionValidation()
    {
        // Arrange - AsyncFunctionOperation with invalid timeout
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations =
            [
                new AsyncFunctionOperationDefinition
                {
                    FunctionType = "http",
                    TimeoutMs = 0, // Invalid: timeout <= 0
                    MaxRetries = 3
                }
            ],
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert - Should trigger ValidateAsyncFunctionOperation
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("operations[0].asyncFunction.timeoutMs must be between 1 and 1200000"));
    }

    [Test]
    public void ValidateOperation_WithProcessFunctionOperation_CallsProcessFunctionValidation()
    {
        // Arrange - ProcessFunctionOperation with missing processType
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations =
            [
                new ProcessFunctionOperationDefinition { ProcessType = "" } // Invalid: empty processType
            ],
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert - Should trigger ValidateProcessFunctionOperation
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("operations[0].processFunction.processType is required"));
    }

    [Test]
    public void ValidateOperation_WithStateOperation_CallsStateValidation()
    {
        // Arrange - StateOperation with invalid state type
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations =
            [
                new StateOperationDefinition { StateType = "invalid", StateKey = "key1" } // Invalid type
            ],
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert - Should trigger ValidateStateOperation
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("operations[0].state.stateType must be one of"));
    }

    [Test]
    public void ValidateOperation_WithTimerOperation_CallsTimerValidation()
    {
        // Arrange - TimerOperation with invalid timer type
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations =
            [
                new TimerOperationDefinition { TimerType = "invalid", DelayMs = 1000 } // Invalid type
            ],
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert - Should trigger ValidateTimerOperation
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("operations[0].timer.timerType must be one of"));
    }

    [Test]
    public void ValidateOperation_WithRetryOperation_CallsRetryValidation()
    {
        // Arrange - RetryOperation with no delayMs values
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations =
            [
                new RetryOperationDefinition
                {
                    MaxRetries = 3,
                    DelayMs = [], // Invalid: no delay values
                    StateKey = "retry-state"
                }
            ],
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert - Should trigger ValidateRetryOperation
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("operations[0].retry.delayMs must contain at least 1 value"));
    }

    [Test]
    public void ValidateOperation_WithSideOutputOperation_CallsSideOutputValidation()
    {
        // Arrange - SideOutputOperation with missing outputTag
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations =
            [
                new SideOutputOperationDefinition
                {
                    OutputTag = "", // Invalid: empty outputTag
                    Condition = "amount > 1000",
                    SideOutputSink = new ConsoleSinkDefinition()
                }
            ],
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert - Should trigger ValidateSideOutputOperation
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("operations[0].sideOutput.outputTag is required"));
    }

    #endregion

    #region ValidateSource Switch Statement - All 5 Cases

    [Test]
    public void ValidateSource_WithSqlSource_CallsSqlSourceValidation()
    {
        // Arrange - SqlSource with no statements
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new SqlSourceDefinition { Statements = [] }, // Invalid: no statements
            Sink = null // SQL jobs don't require sink
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert - Should trigger ValidateSqlSource
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("source.sql.statements must contain at least one statement"));
    }

    [Test]
    public void ValidateSource_WithFileSource_CallsFileSourceValidation()
    {
        // Arrange - FileSource with no path
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new FileSourceDefinition { Path = "", Format = "json" }, // Invalid: no path
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert - Should trigger ValidateFileSource
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("source.file.path is required"));
    }

    [Test]
    public void ValidateSource_WithHttpSource_CallsHttpSourceValidation()
    {
        // Arrange - HttpSource with invalid interval
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new HttpSourceDefinition { Url = "http://example.com", IntervalSeconds = 0 }, // Invalid: interval <= 0
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert - Should trigger ValidateHttpSource
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("source.http.intervalSeconds must be > 0"));
    }

    [Test]
    public void ValidateSource_WithDatabaseSource_CallsDatabaseSourceValidation()
    {
        // Arrange - DatabaseSource with no connection string
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new DatabaseSourceDefinition
            {
                ConnectionString = "", // Invalid
                Query = "SELECT * FROM users",
                PollingIntervalSeconds = 10
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert - Should trigger ValidateDatabaseSource
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("source.database.connectionString is required"));
    }

    #endregion

    #region ValidateSink Switch Statement - All 6 Cases

    [Test]
    public void ValidateSink_WithKafkaSink_CallsKafkaSinkValidation()
    {
        // Arrange - KafkaSink with invalid serializer
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new KafkaSinkDefinition
            {
                Topic = "output",
                BootstrapServers = "localhost:9092",
                Serializer = "invalid" // Invalid: must be 'json' or 'string'
            }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert - Should trigger ValidateKafkaSink
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("sink.kafka.serializer must be 'json' or 'string' when provided"));
    }

    [Test]
    public void ValidateSink_WithFileSink_CallsFileSinkValidation()
    {
        // Arrange - FileSink with no format
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new FileSinkDefinition { Path = "/tmp/output", Format = "" } // Invalid: no format
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert - Should trigger ValidateFileSink
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("sink.file.format is required"));
    }

    [Test]
    public void ValidateSink_WithHttpSink_CallsHttpSinkValidation()
    {
        // Arrange - HttpSink with invalid timeout
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new HttpSinkDefinition { Url = "http://example.com", TimeoutMs = 0 } // Invalid: timeout <= 0
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert - Should trigger ValidateHttpSink
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("sink.http.timeoutMs must be between 1 and 1200000"));
    }

    [Test]
    public void ValidateSink_WithDatabaseSink_CallsDatabaseSinkValidation()
    {
        // Arrange - DatabaseSink with no table
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new DatabaseSinkDefinition
            {
                ConnectionString = "Server=localhost",
                Table = "" // Invalid: no table
            }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert - Should trigger ValidateDatabaseSink
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("sink.database.table is required"));
    }

    [Test]
    public void ValidateSink_WithRedisSink_CallsRedisSinkValidation()
    {
        // Arrange - RedisSink with no operationType
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new RedisSinkDefinition
            {
                ConnectionString = "localhost:6379",
                OperationType = "" // Invalid: no operationType
            }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert - Should trigger ValidateRedisSink
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("sink.redis.operationType is required"));
    }

    [Test]
    public void ValidateSink_WithConsoleSink_CallsConsoleSinkValidation()
    {
        // Arrange - ConsoleSink (has no required validation, but tests the case)
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new ConsoleSinkDefinition() // Valid console sink
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert - Should pass validation (ConsoleSink has no required fields)
        Assert.That(result.IsValid, Is.True);
    }

    #endregion

    #region Additional Branch Coverage - Null/Empty Collections

    [Test]
    public void Validate_WithNullOperations_NoOperationValidationCalled()
    {
        // Arrange - Job with null operations
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = null, // Null operations
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert - Should be valid (null operations is allowed)
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_WithEmptyOperations_NoOperationValidationCalled()
    {
        // Arrange - Job with empty operations list
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = [], // Empty operations
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert - Should be valid (empty operations is allowed)
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_WithNullSource_AddsSourceRequiredError()
    {
        // Arrange - Job with null source
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = null, // Null source
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert - Should add "source is required" error
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("source is required"));
    }

    [Test]
    public void Validate_WithNullSinkForNonSqlJob_AddsSinkRequiredError()
    {
        // Arrange - Non-SQL job with null sink
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = null // Null sink for non-SQL job
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert - Should add "sink is required" error
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("sink is required"));
    }

    [Test]
    public void Validate_WithNullSinkForSqlJob_NoSinkError()
    {
        // Arrange - SQL job with null sink (allowed for SQL jobs)
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new SqlSourceDefinition { Statements = ["SELECT * FROM table1"] },
            Sink = null // Null sink is allowed for SQL jobs
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert - Should be valid (SQL jobs don't require sink)
        Assert.That(result.IsValid, Is.True);
    }

    #endregion
}
