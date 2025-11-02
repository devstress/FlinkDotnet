using Flink.JobBuilder.Models;
using Flink.JobBuilder.Services;

namespace Flink.JobBuilder.Tests;

/// <summary>
/// Complete branch coverage tests for JobDefinitionValidator to achieve 100% branch coverage
/// Focuses on all uncovered validation paths
/// </summary>
[TestFixture]
public class JobDefinitionValidatorCompleteBranchCoverageTests
{
    #region SqlSource Validation Tests

    [Test]
    public void ValidateSqlSource_WithNullStatements_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new SqlSourceDefinition { Statements = null }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("statements"));
    }

    [Test]
    public void ValidateSqlSource_WithEmptyStatements_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new SqlSourceDefinition { Statements = new List<string>() }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("statements"));
    }

    #endregion

    #region FileSource Validation Tests

    [Test]
    public void ValidateFileSource_WithNullPath_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new FileSourceDefinition { Path = null, Format = "json" },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("path"));
    }

    [Test]
    public void ValidateFileSource_WithEmptyPath_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new FileSourceDefinition { Path = "", Format = "json" },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("path"));
    }

    [Test]
    public void ValidateFileSource_WithNullFormat_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new FileSourceDefinition { Path = "/data/file.json", Format = null },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("format"));
    }

    [Test]
    public void ValidateFileSource_WithEmptyFormat_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new FileSourceDefinition { Path = "/data/file.json", Format = "" },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("format"));
    }

    #endregion

    #region HttpSource Validation Tests

    [Test]
    public void ValidateHttpSource_WithNullUrl_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new HttpSourceDefinition { Url = null, IntervalSeconds = 10 },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("url"));
    }

    [Test]
    public void ValidateHttpSource_WithEmptyUrl_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new HttpSourceDefinition { Url = "", IntervalSeconds = 10 },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("url"));
    }

    [Test]
    public void ValidateHttpSource_WithZeroIntervalSeconds_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new HttpSourceDefinition { Url = "http://api.example.com", IntervalSeconds = 0 },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("intervalSeconds"));
    }

    [Test]
    public void ValidateHttpSource_WithNegativeIntervalSeconds_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new HttpSourceDefinition { Url = "http://api.example.com", IntervalSeconds = -5 },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("intervalSeconds"));
    }

    #endregion

    #region DatabaseSource Validation Tests

    [Test]
    public void ValidateDatabaseSource_WithNullConnectionString_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new DatabaseSourceDefinition
            {
                ConnectionString = null,
                Query = "SELECT * FROM users",
                PollingIntervalSeconds = 10
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("connectionString"));
    }

    [Test]
    public void ValidateDatabaseSource_WithEmptyConnectionString_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new DatabaseSourceDefinition
            {
                ConnectionString = "",
                Query = "SELECT * FROM users",
                PollingIntervalSeconds = 10
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("connectionString"));
    }

    [Test]
    public void ValidateDatabaseSource_WithNullQuery_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new DatabaseSourceDefinition
            {
                ConnectionString = "Server=localhost;Database=test",
                Query = null,
                PollingIntervalSeconds = 10
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("query"));
    }

    [Test]
    public void ValidateDatabaseSource_WithEmptyQuery_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new DatabaseSourceDefinition
            {
                ConnectionString = "Server=localhost;Database=test",
                Query = "",
                PollingIntervalSeconds = 10
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("query"));
    }

    [Test]
    public void ValidateDatabaseSource_WithZeroPollingInterval_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new DatabaseSourceDefinition
            {
                ConnectionString = "Server=localhost;Database=test",
                Query = "SELECT * FROM users",
                PollingIntervalSeconds = 0
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("pollingIntervalSeconds"));
    }

    [Test]
    public void ValidateDatabaseSource_WithNegativePollingInterval_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new DatabaseSourceDefinition
            {
                ConnectionString = "Server=localhost;Database=test",
                Query = "SELECT * FROM users",
                PollingIntervalSeconds = -5
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("pollingIntervalSeconds"));
    }

    #endregion

    #region FilterOperation Validation Tests

    [Test]
    public void ValidateFilterOperation_WithNullExpression_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new FilterOperationDefinition { Expression = null }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("expression"));
    }

    [Test]
    public void ValidateFilterOperation_WithEmptyExpression_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new FilterOperationDefinition { Expression = "" }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("expression"));
    }

    #endregion

    #region MapOperation Validation Tests

    [Test]
    public void ValidateMapOperation_WithNullExpression_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new MapOperationDefinition { Expression = null }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("expression"));
    }

    [Test]
    public void ValidateMapOperation_WithEmptyExpression_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new MapOperationDefinition { Expression = "" }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("expression"));
    }

    #endregion

    #region GroupByOperation Validation Tests

    [Test]
    public void ValidateGroupByOperation_WithNullKeyAndNullKeys_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new GroupByOperationDefinition { Key = null, Keys = null }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("key or keys"));
    }

    [Test]
    public void ValidateGroupByOperation_WithEmptyKeyAndEmptyKeys_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new GroupByOperationDefinition { Key = "", Keys = new List<string>() }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("key or keys"));
    }

    #endregion

    #region AggregateOperation Validation Tests

    [Test]
    public void ValidateAggregateOperation_WithNullAggregationType_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new AggregateOperationDefinition { AggregationType = null, Field = "amount" }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("aggregationType"));
    }

    [Test]
    public void ValidateAggregateOperation_WithEmptyAggregationType_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new AggregateOperationDefinition { AggregationType = "", Field = "amount" }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("aggregationType"));
    }

    [Test]
    public void ValidateAggregateOperation_WithInvalidAggregationType_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new AggregateOperationDefinition { AggregationType = "INVALID", Field = "amount" }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("aggregationType"));
    }

    [Test]
    public void ValidateAggregateOperation_WithNullField_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new AggregateOperationDefinition { AggregationType = "SUM", Field = null }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("field"));
    }

    [Test]
    public void ValidateAggregateOperation_WithEmptyField_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new AggregateOperationDefinition { AggregationType = "SUM", Field = "" }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("field"));
    }

    #endregion
}
