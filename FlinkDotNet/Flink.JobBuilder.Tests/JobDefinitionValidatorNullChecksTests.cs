using Flink.JobBuilder.Models;
using Flink.JobBuilder.Services;

namespace Flink.JobBuilder.Tests;

/// <summary>
/// Comprehensive validation tests for JobDefinitionValidator
/// Targets uncovered branches in validation logic for null/empty values
/// Only includes tests that expose actual validation failures
/// </summary>
[TestFixture]
public class JobDefinitionValidatorNullChecksTests
{
    #region SqlSource Validation Tests

    [Test]
    public void ValidateSqlSource_WithNullStatements_ReturnsInvalid()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new SqlSourceDefinition
            {
                Statements = null!
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ValidateSqlSource_WithEmptyStatements_ReturnsInvalid()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new SqlSourceDefinition
            {
                Statements = []
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
    }

    #endregion

    #region KafkaSource Validation Tests

    [Test]
    public void ValidateKafkaSource_WithNullTopic_ReturnsInvalid()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition
            {
                Topic = null!,
                BootstrapServers = "localhost:9092",
                GroupId = "group1"
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("Topic").Or.Contains("topic"));
    }

    [Test]
    public void ValidateKafkaSource_WithEmptyTopic_ReturnsInvalid()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition
            {
                Topic = "",
                BootstrapServers = "localhost:9092",
                GroupId = "group1"
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
    }

    #endregion

    #region FileSource Validation Tests

    [Test]
    public void ValidateFileSource_WithNullPath_ReturnsInvalid()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new FileSourceDefinition
            {
                Path = null!
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ValidateFileSource_WithEmptyPath_ReturnsInvalid()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new FileSourceDefinition
            {
                Path = ""
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
    }

    #endregion

    #region Sink Validation Tests

    [Test]
    public void ValidateKafkaSink_WithNullTopic_ReturnsInvalid()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition
            {
                Topic = "input",
                BootstrapServers = "localhost:9092",
                GroupId = "group1"
            },
            Sink = new KafkaSinkDefinition
            {
                Topic = null!,
                BootstrapServers = "localhost:9092"
            }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ValidateKafkaSink_WithEmptyTopic_ReturnsInvalid()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition
            {
                Topic = "input",
                BootstrapServers = "localhost:9092",
                GroupId = "group1"
            },
            Sink = new KafkaSinkDefinition
            {
                Topic = "",
                BootstrapServers = "localhost:9092"
            }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ValidateFileSink_WithNullPath_ReturnsInvalid()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition
            {
                Topic = "input",
                BootstrapServers = "localhost:9092",
                GroupId = "group1"
            },
            Sink = new FileSinkDefinition
            {
                Path = null!
            }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ValidateFileSink_WithEmptyPath_ReturnsInvalid()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition
            {
                Topic = "input",
                BootstrapServers = "localhost:9092",
                GroupId = "group1"
            },
            Sink = new FileSinkDefinition
            {
                Path = ""
            }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
    }

    #endregion
}
