using Flink.JobBuilder.Models;
using Flink.JobBuilder.Services;

namespace Flink.JobBuilder.Tests;

/// <summary>
/// Comprehensive validation tests for operation definitions in JobDefinitionValidator
/// Targets uncovered branches in operation validation logic
/// </summary>
[TestFixture]
public class JobDefinitionOperationValidationTests
{
    #region WindowOperation Validation Tests

    [Test]
    public void ValidateWindowOperation_WithNullWindowType_ReturnsInvalid()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition
            {
                Topic = "input",
                BootstrapServers = "localhost:9092",
                GroupId = "group1"
            },
            Operations =
            [
                new WindowOperationDefinition
                {
                    WindowType = null!,
                    Size = 10,
                    TimeUnit = "MINUTES"
                }
            ],
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ValidateWindowOperation_WithEmptyWindowType_ReturnsInvalid()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition
            {
                Topic = "input",
                BootstrapServers = "localhost:9092",
                GroupId = "group1"
            },
            Operations =
            [
                new WindowOperationDefinition
                {
                    WindowType = "",
                    Size = 10,
                    TimeUnit = "MINUTES"
                }
            ],
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ValidateWindowOperation_WithZeroSize_ReturnsInvalid()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition
            {
                Topic = "input",
                BootstrapServers = "localhost:9092",
                GroupId = "group1"
            },
            Operations =
            [
                new WindowOperationDefinition
                {
                    WindowType = "TUMBLING",
                    Size = 0,
                    TimeUnit = "MINUTES"
                }
            ],
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ValidateWindowOperation_WithNegativeSize_ReturnsInvalid()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition
            {
                Topic = "input",
                BootstrapServers = "localhost:9092",
                GroupId = "group1"
            },
            Operations =
            [
                new WindowOperationDefinition
                {
                    WindowType = "TUMBLING",
                    Size = -5,
                    TimeUnit = "MINUTES"
                }
            ],
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
    }

    #endregion

    #region JoinOperation Validation Tests

    [Test]
    public void ValidateJoinOperation_WithNullLeftKey_ReturnsInvalid()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition
            {
                Topic = "input",
                BootstrapServers = "localhost:9092",
                GroupId = "group1"
            },
            Operations =
            [
                new JoinOperationDefinition
                {
                    JoinType = "INNER",
                    RightSource = new KafkaSourceDefinition
                    {
                        Topic = "right",
                        BootstrapServers = "localhost:9092",
                        GroupId = "group2"
                    },
                    LeftKey = null!,
                    RightKey = "id"
                }
            ],
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ValidateJoinOperation_WithEmptyLeftKey_ReturnsInvalid()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition
            {
                Topic = "input",
                BootstrapServers = "localhost:9092",
                GroupId = "group1"
            },
            Operations =
            [
                new JoinOperationDefinition
                {
                    JoinType = "INNER",
                    RightSource = new KafkaSourceDefinition
                    {
                        Topic = "right",
                        BootstrapServers = "localhost:9092",
                        GroupId = "group2"
                    },
                    LeftKey = "",
                    RightKey = "id"
                }
            ],
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ValidateJoinOperation_WithNullRightKey_ReturnsInvalid()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition
            {
                Topic = "input",
                BootstrapServers = "localhost:9092",
                GroupId = "group1"
            },
            Operations =
            [
                new JoinOperationDefinition
                {
                    JoinType = "INNER",
                    RightSource = new KafkaSourceDefinition
                    {
                        Topic = "right",
                        BootstrapServers = "localhost:9092",
                        GroupId = "group2"
                    },
                    LeftKey = "id",
                    RightKey = null!
                }
            ],
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ValidateJoinOperation_WithEmptyRightKey_ReturnsInvalid()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition
            {
                Topic = "input",
                BootstrapServers = "localhost:9092",
                GroupId = "group1"
            },
            Operations =
            [
                new JoinOperationDefinition
                {
                    JoinType = "INNER",
                    RightSource = new KafkaSourceDefinition
                    {
                        Topic = "right",
                        BootstrapServers = "localhost:9092",
                        GroupId = "group2"
                    },
                    LeftKey = "id",
                    RightKey = ""
                }
            ],
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ValidateJoinOperation_WithNullRightSource_ReturnsInvalid()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition
            {
                Topic = "input",
                BootstrapServers = "localhost:9092",
                GroupId = "group1"
            },
            Operations =
            [
                new JoinOperationDefinition
                {
                    JoinType = "INNER",
                    RightSource = null!,
                    LeftKey = "id",
                    RightKey = "id"
                }
            ],
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
    }

    #endregion

    #region AsyncFunctionOperation Validation Tests

    [Test]
    public void ValidateAsyncFunction_WithNullFunctionType_ReturnsInvalid()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition
            {
                Topic = "input",
                BootstrapServers = "localhost:9092",
                GroupId = "group1"
            },
            Operations =
            [
                new AsyncFunctionOperationDefinition
                {
                    FunctionType = null!,
                    Url = "http://api.example.com"
                }
            ],
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ValidateAsyncFunction_WithEmptyFunctionType_ReturnsInvalid()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition
            {
                Topic = "input",
                BootstrapServers = "localhost:9092",
                GroupId = "group1"
            },
            Operations =
            [
                new AsyncFunctionOperationDefinition
                {
                    FunctionType = "",
                    Url = "http://api.example.com"
                }
            ],
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
    }

    #endregion
}
