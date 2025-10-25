using Flink.JobBuilder.Models;
using Flink.JobBuilder.Services;

namespace Flink.JobBuilder.Tests;

/// <summary>
/// Complete branch coverage tests for window operations and sink validations
/// </summary>
[TestFixture]
public class JobDefinitionValidatorWindowAndSinkTests
{
    #region WindowOperation Validation Tests

    [Test]
    public void ValidateWindowOperation_WithNullWindowType_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new WindowOperationDefinition
                {
                    WindowType = null,
                    Size = 60,
                    TimeUnit = "SECONDS"
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("windowType"));
    }

    [Test]
    public void ValidateWindowOperation_WithEmptyWindowType_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new WindowOperationDefinition
                {
                    WindowType = "",
                    Size = 60,
                    TimeUnit = "SECONDS"
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("windowType"));
    }

    [Test]
    public void ValidateWindowOperation_WithInvalidWindowType_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new WindowOperationDefinition
                {
                    WindowType = "INVALID",
                    Size = 60,
                    TimeUnit = "SECONDS"
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("windowType"));
    }

    [Test]
    public void ValidateWindowOperation_WithZeroSize_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new WindowOperationDefinition
                {
                    WindowType = "TUMBLING",
                    Size = 0,
                    TimeUnit = "SECONDS"
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("size"));
    }

    [Test]
    public void ValidateWindowOperation_WithNegativeSize_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new WindowOperationDefinition
                {
                    WindowType = "TUMBLING",
                    Size = -10,
                    TimeUnit = "SECONDS"
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("size"));
    }

    [Test]
    public void ValidateWindowOperation_WithNullTimeUnit_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new WindowOperationDefinition
                {
                    WindowType = "TUMBLING",
                    Size = 60,
                    TimeUnit = null
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("timeUnit"));
    }

    [Test]
    public void ValidateWindowOperation_WithInvalidTimeUnit_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new WindowOperationDefinition
                {
                    WindowType = "TUMBLING",
                    Size = 60,
                    TimeUnit = "DAYS"
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("timeUnit"));
    }

    [Test]
    public void ValidateWindowOperation_SlidingWithNullSlide_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new WindowOperationDefinition
                {
                    WindowType = "SLIDING",
                    Size = 60,
                    TimeUnit = "SECONDS",
                    Slide = null
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("slide"));
    }

    [Test]
    public void ValidateWindowOperation_SlidingWithZeroSlide_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new WindowOperationDefinition
                {
                    WindowType = "SLIDING",
                    Size = 60,
                    TimeUnit = "SECONDS",
                    Slide = 0
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("slide"));
    }

    [Test]
    public void ValidateWindowOperation_SlidingWithNegativeSlide_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new WindowOperationDefinition
                {
                    WindowType = "SLIDING",
                    Size = 60,
                    TimeUnit = "SECONDS",
                    Slide = -10
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("slide"));
    }

    #endregion

    #region JoinOperation Validation Tests

    [Test]
    public void ValidateJoinOperation_WithNullRightSource_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new JoinOperationDefinition
                {
                    RightSource = null,
                    LeftKey = "id",
                    RightKey = "userId"
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("rightSource"));
    }

    [Test]
    public void ValidateJoinOperation_WithNullLeftKey_AddsError()
    {
        // Arrange
        var rightSource = new KafkaSourceDefinition
        {
            Topic = "users",
            BootstrapServers = "localhost:9092",
            GroupId = "group2"
        };

        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new JoinOperationDefinition
                {
                    RightSource = rightSource,
                    LeftKey = null,
                    RightKey = "userId"
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("leftKey"));
    }

    [Test]
    public void ValidateJoinOperation_WithEmptyLeftKey_AddsError()
    {
        // Arrange
        var rightSource = new KafkaSourceDefinition
        {
            Topic = "users",
            BootstrapServers = "localhost:9092",
            GroupId = "group2"
        };

        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new JoinOperationDefinition
                {
                    RightSource = rightSource,
                    LeftKey = "",
                    RightKey = "userId"
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("leftKey"));
    }

    [Test]
    public void ValidateJoinOperation_WithNullRightKey_AddsError()
    {
        // Arrange
        var rightSource = new KafkaSourceDefinition
        {
            Topic = "users",
            BootstrapServers = "localhost:9092",
            GroupId = "group2"
        };

        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new JoinOperationDefinition
                {
                    RightSource = rightSource,
                    LeftKey = "id",
                    RightKey = null
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("rightKey"));
    }

    [Test]
    public void ValidateJoinOperation_WithEmptyRightKey_AddsError()
    {
        // Arrange
        var rightSource = new KafkaSourceDefinition
        {
            Topic = "users",
            BootstrapServers = "localhost:9092",
            GroupId = "group2"
        };

        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new JoinOperationDefinition
                {
                    RightSource = rightSource,
                    LeftKey = "id",
                    RightKey = ""
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("rightKey"));
    }

    #endregion

    #region AsyncFunctionOperation Validation Tests

    [Test]
    public void ValidateAsyncFunctionOperation_WithNullFunctionType_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new AsyncFunctionOperationDefinition
                {
                    FunctionType = null,
                    TimeoutMs = 5000,
                    MaxRetries = 3
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("functionType"));
    }

    [Test]
    public void ValidateAsyncFunctionOperation_WithEmptyFunctionType_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new AsyncFunctionOperationDefinition
                {
                    FunctionType = "",
                    TimeoutMs = 5000,
                    MaxRetries = 3
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("functionType"));
    }

    [Test]
    public void ValidateAsyncFunctionOperation_WithZeroTimeout_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new AsyncFunctionOperationDefinition
                {
                    FunctionType = "http",
                    TimeoutMs = 0,
                    MaxRetries = 3
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("timeoutMs"));
    }

    [Test]
    public void ValidateAsyncFunctionOperation_WithNegativeTimeout_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new AsyncFunctionOperationDefinition
                {
                    FunctionType = "http",
                    TimeoutMs = -100,
                    MaxRetries = 3
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("timeoutMs"));
    }

    [Test]
    public void ValidateAsyncFunctionOperation_WithTimeoutExceedingMax_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new AsyncFunctionOperationDefinition
                {
                    FunctionType = "http",
                    TimeoutMs = 1_300_000, // > 1,200,000
                    MaxRetries = 3
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("timeoutMs"));
    }

    [Test]
    public void ValidateAsyncFunctionOperation_WithNegativeMaxRetries_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new AsyncFunctionOperationDefinition
                {
                    FunctionType = "http",
                    TimeoutMs = 5000,
                    MaxRetries = -1
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
    public void ValidateAsyncFunctionOperation_WithMaxRetriesExceedingLimit_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new AsyncFunctionOperationDefinition
                {
                    FunctionType = "http",
                    TimeoutMs = 5000,
                    MaxRetries = 101 // > 100
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

    #endregion

    #region ProcessFunctionOperation Validation Tests

    [Test]
    public void ValidateProcessFunctionOperation_WithNullProcessType_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new ProcessFunctionOperationDefinition { ProcessType = null }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("processType"));
    }

    [Test]
    public void ValidateProcessFunctionOperation_WithEmptyProcessType_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new ProcessFunctionOperationDefinition { ProcessType = "" }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("processType"));
    }

    #endregion

    #region FileSink Validation Tests

    [Test]
    public void ValidateFileSink_WithNullPath_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new FileSinkDefinition { Path = null, Format = "json" }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("path"));
    }

    [Test]
    public void ValidateFileSink_WithEmptyPath_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new FileSinkDefinition { Path = "", Format = "json" }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("path"));
    }

    [Test]
    public void ValidateFileSink_WithNullFormat_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new FileSinkDefinition { Path = "/output/data", Format = null }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("format"));
    }

    [Test]
    public void ValidateFileSink_WithEmptyFormat_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new FileSinkDefinition { Path = "/output/data", Format = "" }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("format"));
    }

    #endregion

    #region HttpSink Validation Tests

    [Test]
    public void ValidateHttpSink_WithNullUrl_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new HttpSinkDefinition { Url = null, TimeoutMs = 5000 }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("url"));
    }

    [Test]
    public void ValidateHttpSink_WithEmptyUrl_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new HttpSinkDefinition { Url = "", TimeoutMs = 5000 }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("url"));
    }

    [Test]
    public void ValidateHttpSink_WithZeroTimeout_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new HttpSinkDefinition { Url = "http://api.example.com", TimeoutMs = 0 }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("timeoutMs"));
    }

    [Test]
    public void ValidateHttpSink_WithNegativeTimeout_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new HttpSinkDefinition { Url = "http://api.example.com", TimeoutMs = -100 }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("timeoutMs"));
    }

    [Test]
    public void ValidateHttpSink_WithTimeoutExceedingMax_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new HttpSinkDefinition { Url = "http://api.example.com", TimeoutMs = 1_300_000 }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("timeoutMs"));
    }

    #endregion

    #region DatabaseSink Validation Tests

    [Test]
    public void ValidateDatabaseSink_WithNullConnectionString_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new DatabaseSinkDefinition { ConnectionString = null, Table = "users" }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("connectionString"));
    }

    [Test]
    public void ValidateDatabaseSink_WithEmptyConnectionString_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new DatabaseSinkDefinition { ConnectionString = "", Table = "users" }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("connectionString"));
    }

    [Test]
    public void ValidateDatabaseSink_WithNullTable_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new DatabaseSinkDefinition { ConnectionString = "Server=localhost", Table = null }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("table"));
    }

    [Test]
    public void ValidateDatabaseSink_WithEmptyTable_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new DatabaseSinkDefinition { ConnectionString = "Server=localhost", Table = "" }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("table"));
    }

    #endregion

    #region RedisSink Validation Tests

    [Test]
    public void ValidateRedisSink_WithNullConnectionString_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new RedisSinkDefinition { ConnectionString = null, OperationType = "SET" }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("connectionString"));
    }

    [Test]
    public void ValidateRedisSink_WithEmptyConnectionString_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new RedisSinkDefinition { ConnectionString = "", OperationType = "SET" }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("connectionString"));
    }

    [Test]
    public void ValidateRedisSink_WithNullOperationType_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new RedisSinkDefinition { ConnectionString = "localhost:6379", OperationType = null }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("operationType"));
    }

    [Test]
    public void ValidateRedisSink_WithEmptyOperationType_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new RedisSinkDefinition { ConnectionString = "localhost:6379", OperationType = "" }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("operationType"));
    }

    #endregion

    #region KafkaSink Validation Tests

    [Test]
    public void ValidateKafkaSink_WithInvalidSerializer_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new KafkaSinkDefinition
            {
                Topic = "output",
                BootstrapServers = "localhost:9092",
                Serializer = "invalid"
            }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("serializer"));
    }

    #endregion
}
