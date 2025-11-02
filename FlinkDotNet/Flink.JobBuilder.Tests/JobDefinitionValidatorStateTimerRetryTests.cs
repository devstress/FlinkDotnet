using Flink.JobBuilder.Models;
using Flink.JobBuilder.Services;

namespace Flink.JobBuilder.Tests;

/// <summary>
/// Complete branch coverage tests for State, Timer, Retry, and SideOutput operation validations
/// </summary>
[TestFixture]
public class JobDefinitionValidatorStateTimerRetryTests
{
    #region StateOperation Validation Tests

    [Test]
    public void ValidateStateOperation_WithNullStateType_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new StateOperationDefinition
                {
                    StateType = null,
                    StateKey = "myKey"
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("stateType"));
    }

    [Test]
    public void ValidateStateOperation_WithEmptyStateType_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new StateOperationDefinition
                {
                    StateType = "",
                    StateKey = "myKey"
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("stateType"));
    }

    [Test]
    public void ValidateStateOperation_WithInvalidStateType_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new StateOperationDefinition
                {
                    StateType = "invalid",
                    StateKey = "myKey"
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("stateType"));
    }

    [Test]
    public void ValidateStateOperation_WithNullStateKey_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new StateOperationDefinition
                {
                    StateType = "value",
                    StateKey = null
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("stateKey"));
    }

    [Test]
    public void ValidateStateOperation_WithEmptyStateKey_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new StateOperationDefinition
                {
                    StateType = "value",
                    StateKey = ""
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("stateKey"));
    }

    [Test]
    public void ValidateStateOperation_WithZeroTtlMs_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new StateOperationDefinition
                {
                    StateType = "value",
                    StateKey = "myKey",
                    TtlMs = 0
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("ttlMs"));
    }

    [Test]
    public void ValidateStateOperation_WithNegativeTtlMs_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new StateOperationDefinition
                {
                    StateType = "value",
                    StateKey = "myKey",
                    TtlMs = -1000
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("ttlMs"));
    }

    #endregion

    #region TimerOperation Validation Tests

    [Test]
    public void ValidateTimerOperation_WithNullTimerType_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new TimerOperationDefinition
                {
                    TimerType = null,
                    DelayMs = 5000
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("timerType"));
    }

    [Test]
    public void ValidateTimerOperation_WithEmptyTimerType_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new TimerOperationDefinition
                {
                    TimerType = "",
                    DelayMs = 5000
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("timerType"));
    }

    [Test]
    public void ValidateTimerOperation_WithInvalidTimerType_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new TimerOperationDefinition
                {
                    TimerType = "invalid",
                    DelayMs = 5000
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("timerType"));
    }

    [Test]
    public void ValidateTimerOperation_WithZeroDelayMs_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new TimerOperationDefinition
                {
                    TimerType = "processing",
                    DelayMs = 0
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("delayMs"));
    }

    [Test]
    public void ValidateTimerOperation_WithNegativeDelayMs_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new TimerOperationDefinition
                {
                    TimerType = "processing",
                    DelayMs = -1000
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("delayMs"));
    }

    [Test]
    public void ValidateTimerOperation_WithDelayMsExceedingMax_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new TimerOperationDefinition
                {
                    TimerType = "processing",
                    DelayMs = 86_500_000 // > 86_400_000
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("delayMs"));
    }

    #endregion

    #region RetryOperation Validation Tests

    [Test]
    public void ValidateRetryOperation_WithNegativeMaxRetries_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new RetryOperationDefinition
                {
                    MaxRetries = -1,
                    DelayMs = new List<long> { 1000 },
                    StateKey = "retryKey"
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("maxRetries"));
    }

    [Test]
    public void ValidateRetryOperation_WithMaxRetriesExceedingLimit_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new RetryOperationDefinition
                {
                    MaxRetries = 101, // > 100
                    DelayMs = new List<long> { 1000 },
                    StateKey = "retryKey"
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("maxRetries"));
    }

    [Test]
    public void ValidateRetryOperation_WithNullDelayMs_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new RetryOperationDefinition
                {
                    MaxRetries = 3,
                    DelayMs = null,
                    StateKey = "retryKey"
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("delayMs"));
    }

    [Test]
    public void ValidateRetryOperation_WithEmptyDelayMs_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new RetryOperationDefinition
                {
                    MaxRetries = 3,
                    DelayMs = new List<long>(),
                    StateKey = "retryKey"
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("delayMs"));
    }

    [Test]
    public void ValidateRetryOperation_WithNegativeDelayMsValue_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new RetryOperationDefinition
                {
                    MaxRetries = 3,
                    DelayMs = new List<long> { 1000, -500, 2000 },
                    StateKey = "retryKey"
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("delayMs values"));
    }

    [Test]
    public void ValidateRetryOperation_WithZeroDelayMsValue_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new RetryOperationDefinition
                {
                    MaxRetries = 3,
                    DelayMs = new List<long> { 1000, 0, 2000 },
                    StateKey = "retryKey"
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("delayMs values"));
    }

    [Test]
    public void ValidateRetryOperation_WithNullStateKey_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new RetryOperationDefinition
                {
                    MaxRetries = 3,
                    DelayMs = new List<long> { 1000 },
                    StateKey = null
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("stateKey"));
    }

    [Test]
    public void ValidateRetryOperation_WithEmptyStateKey_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new RetryOperationDefinition
                {
                    MaxRetries = 3,
                    DelayMs = new List<long> { 1000 },
                    StateKey = ""
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("stateKey"));
    }

    #endregion

    #region SideOutputOperation Validation Tests

    [Test]
    public void ValidateSideOutputOperation_WithNullOutputTag_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new SideOutputOperationDefinition
                {
                    OutputTag = null,
                    Condition = "value > 100",
                    SideOutputSink = new ConsoleSinkDefinition()
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("outputTag"));
    }

    [Test]
    public void ValidateSideOutputOperation_WithEmptyOutputTag_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new SideOutputOperationDefinition
                {
                    OutputTag = "",
                    Condition = "value > 100",
                    SideOutputSink = new ConsoleSinkDefinition()
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("outputTag"));
    }

    [Test]
    public void ValidateSideOutputOperation_WithNullCondition_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new SideOutputOperationDefinition
                {
                    OutputTag = "high-values",
                    Condition = null,
                    SideOutputSink = new ConsoleSinkDefinition()
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("condition"));
    }

    [Test]
    public void ValidateSideOutputOperation_WithEmptyCondition_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new SideOutputOperationDefinition
                {
                    OutputTag = "high-values",
                    Condition = "",
                    SideOutputSink = new ConsoleSinkDefinition()
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("condition"));
    }

    [Test]
    public void ValidateSideOutputOperation_WithNullSideOutputSink_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new SideOutputOperationDefinition
                {
                    OutputTag = "high-values",
                    Condition = "value > 100",
                    SideOutputSink = null
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("sideOutputSink"));
    }

    #endregion
}
