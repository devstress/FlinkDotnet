using Flink.JobBuilder.Models;
using Flink.JobBuilder.Services;

namespace Flink.JobBuilder.Tests.Tests;

/// <summary>
/// Batch 7 coverage tests - targeting remaining validation paths and edge cases
/// Focuses on JobDefinitionValidator comprehensive validation scenarios
/// </summary>
[TestFixture]
public class JobDefinitionValidatorBatch7Tests
{
    #region Source Validation Edge Cases

    [Test]
    public void Validate_WithHttpSourceNegativeInterval_ReturnsError()
    {
        // Arrange
        var jobDefinition = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new HttpSourceDefinition
            {
                Url = "https://api.example.com/data",
                IntervalSeconds = -5  // Invalid - negative interval
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(jobDefinition);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contain("intervalSeconds must be > 0"));
    }

    [Test]
    public void Validate_WithHttpSourceZeroInterval_ReturnsError()
    {
        // Arrange
        var jobDefinition = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new HttpSourceDefinition
            {
                Url = "https://api.example.com/data",
                IntervalSeconds = 0  // Invalid - zero interval
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(jobDefinition);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contain("intervalSeconds must be > 0"));
    }

    [Test]
    public void Validate_WithDatabaseSourceNegativePollingInterval_ReturnsError()
    {
        // Arrange
        var jobDefinition = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new DatabaseSourceDefinition
            {
                ConnectionString = "jdbc:postgresql://localhost/db",
                Query = "SELECT * FROM table",
                PollingIntervalSeconds = -10  // Invalid - negative
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(jobDefinition);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contain("pollingIntervalSeconds must be > 0"));
    }

    [Test]
    public void Validate_WithDatabaseSourceZeroPollingInterval_ReturnsError()
    {
        // Arrange
        var jobDefinition = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new DatabaseSourceDefinition
            {
                ConnectionString = "jdbc:postgresql://localhost/db",
                Query = "SELECT * FROM table",
                PollingIntervalSeconds = 0  // Invalid - zero
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(jobDefinition);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contain("pollingIntervalSeconds must be > 0"));
    }

    [Test]
    public void Validate_WithFileSourceMissingPath_ReturnsError()
    {
        // Arrange
        var jobDefinition = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new FileSourceDefinition
            {
                Path = "",  // Invalid - empty path
                Format = "csv"
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(jobDefinition);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contain("path is required"));
    }

    [Test]
    public void Validate_WithFileSourceMissingFormat_ReturnsError()
    {
        // Arrange
        var jobDefinition = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new FileSourceDefinition
            {
                Path = "/data/input.txt",
                Format = ""  // Invalid - empty format
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(jobDefinition);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contain("format is required"));
    }

    [Test]
    public void Validate_WithHttpSourceMissingUrl_ReturnsError()
    {
        // Arrange
        var jobDefinition = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new HttpSourceDefinition
            {
                Url = "",  // Invalid - empty URL
                IntervalSeconds = 60
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(jobDefinition);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contain("url is required"));
    }

    [Test]
    public void Validate_WithDatabaseSourceMissingConnectionString_ReturnsError()
    {
        // Arrange
        var jobDefinition = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new DatabaseSourceDefinition
            {
                ConnectionString = "",  // Invalid - empty
                Query = "SELECT * FROM table",
                PollingIntervalSeconds = 30
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(jobDefinition);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contain("connectionString is required"));
    }

    [Test]
    public void Validate_WithDatabaseSourceMissingQuery_ReturnsError()
    {
        // Arrange
        var jobDefinition = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new DatabaseSourceDefinition
            {
                ConnectionString = "jdbc:postgresql://localhost/db",
                Query = "",  // Invalid - empty
                PollingIntervalSeconds = 30
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(jobDefinition);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contain("query is required"));
    }

    [Test]
    public void Validate_WithKafkaSourceMissingTopic_ReturnsError()
    {
        // Arrange
        var jobDefinition = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition
            {
                Topic = "",  // Invalid - empty topic
                BootstrapServers = "localhost:9092",
                GroupId = "test-group"
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(jobDefinition);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contain("topic is required"));
    }

    [Test]
    public void Validate_WithSqlSourceEmptyStatements_ReturnsError()
    {
        // Arrange
        var jobDefinition = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new SqlSourceDefinition
            {
                Statements = new List<string>()  // Invalid - empty list
            }
        };

        // Act
        var result = JobDefinitionValidator.Validate(jobDefinition);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contain("statements must contain at least one statement"));
    }

    [Test]
    public void Validate_WithSqlSourceNullStatements_ReturnsError()
    {
        // Arrange
        var jobDefinition = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new SqlSourceDefinition
            {
                Statements = null!  // Invalid - null
            }
        };

        // Act
        var result = JobDefinitionValidator.Validate(jobDefinition);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contain("statements must contain at least one statement"));
    }

    #endregion

    #region Metadata Validation Edge Cases

    [Test]
    public void Validate_WithNullMetadata_ReturnsError()
    {
        // Arrange
        var jobDefinition = new JobDefinition
        {
            Metadata = null!,  // Invalid
            Source = new KafkaSourceDefinition
            {
                Topic = "test",
                BootstrapServers = "localhost:9092"
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(jobDefinition);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contain("metadata is required"));
    }

    [Test]
    public void Validate_WithEmptyJobId_ReturnsError()
    {
        // JobId is no longer required - test now validates job is valid without it
        var jobDefinition = new JobDefinition
        {
            Metadata = new JobMetadata
            {
                Version = "1.0"
            },
            Source = new KafkaSourceDefinition
            {
                Topic = "test",
                BootstrapServers = "localhost:9092"
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(jobDefinition);

        // Assert - Should be valid now since JobId is no longer required
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_WithWhitespaceJobId_ReturnsError()
    {
        // JobId is no longer required - test now validates job is valid without it
        var jobDefinition = new JobDefinition
        {
            Metadata = new JobMetadata
            {
                Version = "1.0"
            },
            Source = new KafkaSourceDefinition
            {
                Topic = "test",
                BootstrapServers = "localhost:9092"
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(jobDefinition);

        // Assert - Should be valid now since JobId is no longer required
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_WithEmptyVersion_ReturnsError()
    {
        // Arrange
        var jobDefinition = new JobDefinition
        {
            Metadata = new JobMetadata
            {
                                Version = ""  // Invalid
            },
            Source = new KafkaSourceDefinition
            {
                Topic = "test",
                BootstrapServers = "localhost:9092"
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(jobDefinition);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contain("version is required"));
    }

    [Test]
    public void Validate_WithZeroParallelism_ReturnsError()
    {
        // Arrange
        var jobDefinition = new JobDefinition
        {
            Metadata = new JobMetadata
            {
                                Version = "1.0",
                Parallelism = 0  // Invalid
            },
            Source = new KafkaSourceDefinition
            {
                Topic = "test",
                BootstrapServers = "localhost:9092"
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(jobDefinition);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contain("parallelism must be >= 1"));
    }

    [Test]
    public void Validate_WithNegativeParallelism_ReturnsError()
    {
        // Arrange
        var jobDefinition = new JobDefinition
        {
            Metadata = new JobMetadata
            {
                                Version = "1.0",
                Parallelism = -5  // Invalid
            },
            Source = new KafkaSourceDefinition
            {
                Topic = "test",
                BootstrapServers = "localhost:9092"
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(jobDefinition);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contain("parallelism must be >= 1"));
    }

    #endregion

    #region Job Structure Validation

    [Test]
    public void Validate_WithNullSource_ReturnsError()
    {
        // Arrange
        var jobDefinition = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = null!,  // Invalid
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(jobDefinition);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contain("source is required"));
    }

    [Test]
    public void Validate_WithNonSqlJobMissingSink_ReturnsError()
    {
        // Arrange
        var jobDefinition = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition
            {
                Topic = "test",
                BootstrapServers = "localhost:9092"
            },
            Sink = null  // Invalid for non-SQL jobs
        };

        // Act
        var result = JobDefinitionValidator.Validate(jobDefinition);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contain("sink is required"));
    }

    [Test]
    public void Validate_WithSqlJobMissingSink_IsValid()
    {
        // Arrange - SQL jobs don't require sinks
        var jobDefinition = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new SqlSourceDefinition
            {
                Statements = new List<string> { "SELECT * FROM table" }
            },
            Sink = null  // Valid for SQL jobs
        };

        // Act
        var result = JobDefinitionValidator.Validate(jobDefinition);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    #endregion

    #region Valid Scenarios

    [Test]
    public void Validate_WithValidNonSqlJob_IsValid()
    {
        // Arrange
        var jobDefinition = new JobDefinition
        {
            Metadata = new JobMetadata
            {
                                Version = "1.0",
                Parallelism = 4
            },
            Source = new KafkaSourceDefinition
            {
                Topic = "events",
                BootstrapServers = "localhost:9092",
                GroupId = "test-group"
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(jobDefinition);

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Validate_WithValidHttpSource_IsValid()
    {
        // Arrange
        var jobDefinition = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new HttpSourceDefinition
            {
                Url = "https://api.example.com/data",
                IntervalSeconds = 60
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(jobDefinition);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_WithValidDatabaseSource_IsValid()
    {
        // Arrange
        var jobDefinition = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new DatabaseSourceDefinition
            {
                ConnectionString = "jdbc:postgresql://localhost/db",
                Query = "SELECT * FROM events",
                PollingIntervalSeconds = 30
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(jobDefinition);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    #endregion
}
