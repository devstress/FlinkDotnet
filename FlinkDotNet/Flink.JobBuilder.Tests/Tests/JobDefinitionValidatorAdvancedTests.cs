using Flink.JobBuilder.Models;
using Flink.JobBuilder.Services;

namespace Flink.JobBuilder.Tests.Tests;

[TestFixture]
public class JobDefinitionValidatorAdvancedTests
{
    #region AsyncFunctionOperation Validation Tests

    [Test]
    public void Validate_AsyncFunctionOperation_WithEmptyFunctionType_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new AsyncFunctionOperationDefinition { FunctionType = "" }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].asyncFunction.functionType is required"));
    }

    [Test]
    public void Validate_AsyncFunctionOperation_WithZeroTimeout_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new AsyncFunctionOperationDefinition
                {
                    FunctionType = "http",
                    TimeoutMs = 0
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].asyncFunction.timeoutMs must be between 1 and 1200000"));
    }

    [Test]
    public void Validate_AsyncFunctionOperation_WithTimeoutTooLarge_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new AsyncFunctionOperationDefinition
                {
                    FunctionType = "http",
                    TimeoutMs = 1_300_000
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].asyncFunction.timeoutMs must be between 1 and 1200000"));
    }

    [Test]
    public void Validate_AsyncFunctionOperation_WithNegativeRetries_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new AsyncFunctionOperationDefinition
                {
                    FunctionType = "http",
                    MaxRetries = -1
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].asyncFunction.maxRetries must be between 0 and 100"));
    }

    [Test]
    public void Validate_AsyncFunctionOperation_WithRetriesTooLarge_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new AsyncFunctionOperationDefinition
                {
                    FunctionType = "http",
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
    public void Validate_AsyncFunctionOperation_WithValidValues_IsValid()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new AsyncFunctionOperationDefinition
                {
                    FunctionType = "http",
                    TimeoutMs = 5000,
                    MaxRetries = 3
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.True);
    }

    #endregion

    #region ProcessFunctionOperation Validation Tests

    [Test]
    public void Validate_ProcessFunctionOperation_WithEmptyProcessType_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new ProcessFunctionOperationDefinition { ProcessType = "" }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].processFunction.processType is required"));
    }

    [Test]
    public void Validate_ProcessFunctionOperation_WithValidProcessType_IsValid()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new ProcessFunctionOperationDefinition { ProcessType = "authTokenManager" }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.True);
    }

    #endregion

    #region StateOperation Validation Tests

    [Test]
    public void Validate_StateOperation_WithInvalidStateType_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new StateOperationDefinition { StateType = "invalid", StateKey = "key1" }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.Contains("stateType must be one of")), Is.True);
    }

    [Test]
    public void Validate_StateOperation_WithEmptyStateKey_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new StateOperationDefinition { StateType = "value", StateKey = "" }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].state.stateKey is required"));
    }

    [Test]
    public void Validate_StateOperation_WithNegativeTtl_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new StateOperationDefinition { StateType = "value", StateKey = "key1", TtlMs = -1 }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].state.ttlMs must be > 0 when provided"));
    }

    [Test]
    public void Validate_StateOperation_WithZeroTtl_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new StateOperationDefinition { StateType = "value", StateKey = "key1", TtlMs = 0 }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].state.ttlMs must be > 0 when provided"));
    }

    [Test]
    public void Validate_StateOperation_WithAllStateTypes_IsValid()
    {
        var stateTypes = new[] { "value", "list", "map", "reducing" };

        foreach (var stateType in stateTypes)
        {
            var job = new JobDefinition
            {
                Metadata = new JobMetadata { Version = "1.0" },
                Source = new KafkaSourceDefinition { Topic = "input" },
                Operations = new List<IOperationDefinition>
                {
                    new StateOperationDefinition { StateType = stateType, StateKey = "key1" }
                },
                Sink = new KafkaSinkDefinition { Topic = "output" }
            };

            var result = JobDefinitionValidator.Validate(job);

            Assert.That(result.IsValid, Is.True, $"State type '{stateType}' should be valid");
        }
    }

    #endregion

    #region TimerOperation Validation Tests

    [Test]
    public void Validate_TimerOperation_WithInvalidTimerType_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new TimerOperationDefinition { TimerType = "invalid", DelayMs = 1000 }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.Contains("timerType must be one of")), Is.True);
    }

    [Test]
    public void Validate_TimerOperation_WithZeroDelay_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new TimerOperationDefinition { TimerType = "processing", DelayMs = 0 }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].timer.delayMs must be between 1 and 86400000"));
    }

    [Test]
    public void Validate_TimerOperation_WithDelayTooLarge_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new TimerOperationDefinition { TimerType = "processing", DelayMs = 86_400_001 }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].timer.delayMs must be between 1 and 86400000"));
    }

    [Test]
    public void Validate_TimerOperation_WithValidValues_IsValid()
    {
        var timerTypes = new[] { "processing", "event" };

        foreach (var timerType in timerTypes)
        {
            var job = new JobDefinition
            {
                Metadata = new JobMetadata { Version = "1.0" },
                Source = new KafkaSourceDefinition { Topic = "input" },
                Operations = new List<IOperationDefinition>
                {
                    new TimerOperationDefinition { TimerType = timerType, DelayMs = 5000 }
                },
                Sink = new KafkaSinkDefinition { Topic = "output" }
            };

            var result = JobDefinitionValidator.Validate(job);

            Assert.That(result.IsValid, Is.True, $"Timer type '{timerType}' should be valid");
        }
    }

    #endregion

    #region RetryOperation Validation Tests

    [Test]
    public void Validate_RetryOperation_WithNegativeMaxRetries_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new RetryOperationDefinition
                {
                    MaxRetries = -1,
                    DelayMs = new List<long> { 1000 },
                    StateKey = "retry_key"
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].retry.maxRetries must be between 0 and 100"));
    }

    [Test]
    public void Validate_RetryOperation_WithMaxRetriesTooLarge_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new RetryOperationDefinition
                {
                    MaxRetries = 101,
                    DelayMs = new List<long> { 1000 },
                    StateKey = "retry_key"
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
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new RetryOperationDefinition
                {
                    MaxRetries = 3,
                    DelayMs = null!,
                    StateKey = "retry_key"
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
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new RetryOperationDefinition
                {
                    MaxRetries = 3,
                    DelayMs = new List<long>(),
                    StateKey = "retry_key"
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].retry.delayMs must contain at least 1 value"));
    }

    [Test]
    public void Validate_RetryOperation_WithNegativeDelayValue_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new RetryOperationDefinition
                {
                    MaxRetries = 3,
                    DelayMs = new List<long> { 1000, -500, 2000 },
                    StateKey = "retry_key"
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].retry.delayMs values must be > 0"));
    }

    [Test]
    public void Validate_RetryOperation_WithEmptyStateKey_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new RetryOperationDefinition
                {
                    MaxRetries = 3,
                    DelayMs = new List<long> { 1000 },
                    StateKey = ""
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].retry.stateKey is required"));
    }

    [Test]
    public void Validate_RetryOperation_WithValidValues_IsValid()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new RetryOperationDefinition
                {
                    MaxRetries = 5,
                    DelayMs = new List<long> { 1000, 2000, 3000 },
                    StateKey = "retry_key"
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.True);
    }

    #endregion

    #region SideOutputOperation Validation Tests

    [Test]
    public void Validate_SideOutputOperation_WithEmptyOutputTag_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new SideOutputOperationDefinition
                {
                    OutputTag = "",
                    Condition = "x => x.Error",
                    SideOutputSink = new KafkaSinkDefinition { Topic = "errors" }
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].sideOutput.outputTag is required"));
    }

    [Test]
    public void Validate_SideOutputOperation_WithEmptyCondition_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new SideOutputOperationDefinition
                {
                    OutputTag = "error-tag",
                    Condition = "",
                    SideOutputSink = new KafkaSinkDefinition { Topic = "errors" }
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].sideOutput.condition is required"));
    }

    [Test]
    public void Validate_SideOutputOperation_WithNullSink_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new SideOutputOperationDefinition
                {
                    OutputTag = "error-tag",
                    Condition = "x => x.Error",
                    SideOutputSink = null!
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("operations[0].sideOutput.sideOutputSink is required"));
    }

    [Test]
    public void Validate_SideOutputOperation_WithValidValues_IsValid()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new SideOutputOperationDefinition
                {
                    OutputTag = "error-tag",
                    Condition = "x => x.Error",
                    SideOutputSink = new KafkaSinkDefinition { Topic = "errors" }
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.True);
    }

    #endregion

    #region Sink Validation Tests

    [Test]
    public void Validate_KafkaSink_WithInvalidSerializer_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output", Serializer = "xml" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("sink.kafka.serializer must be 'json' or 'string' when provided"));
    }

    [Test]
    public void Validate_KafkaSink_WithValidSerializers_IsValid()
    {
        var serializers = new[] { "json", "string" };

        foreach (var serializer in serializers)
        {
            var job = new JobDefinition
            {
                Metadata = new JobMetadata { Version = "1.0" },
                Source = new KafkaSourceDefinition { Topic = "input" },
                Sink = new KafkaSinkDefinition { Topic = "output", Serializer = serializer }
            };

            var result = JobDefinitionValidator.Validate(job);

            Assert.That(result.IsValid, Is.True, $"Serializer '{serializer}' should be valid");
        }
    }

    [Test]
    public void Validate_RedisSink_WithEmptyConnectionString_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new RedisSinkDefinition { ConnectionString = "", OperationType = "increment" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("sink.redis.connectionString is required"));
    }

    [Test]
    public void Validate_RedisSink_WithEmptyOperationType_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new RedisSinkDefinition { ConnectionString = "localhost:6379", OperationType = "" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("sink.redis.operationType is required"));
    }

    [Test]
    public void Validate_RedisSink_WithValidValues_IsValid()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new RedisSinkDefinition { ConnectionString = "localhost:6379", OperationType = "increment" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.True);
    }

    #endregion

    #region HttpSink Validation Tests

    [Test]
    public void Validate_HttpSink_WithEmptyUrl_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new HttpSinkDefinition { Url = "", TimeoutMs = 5000 }
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
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new HttpSinkDefinition { Url = "http://api.example.com", TimeoutMs = 0 }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("sink.http.timeoutMs must be between 1 and 1200000"));
    }

    [Test]
    public void Validate_HttpSink_WithTimeoutTooLarge_ReturnsError()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new HttpSinkDefinition { Url = "http://api.example.com", TimeoutMs = 1_300_000 }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("sink.http.timeoutMs must be between 1 and 1200000"));
    }

    #endregion

    #region Multiple Errors Test

    [Test]
    public void Validate_ComplexJobWithMultipleErrors_ReturnsAllErrors()
    {
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "" },
            Source = new KafkaSourceDefinition { Topic = "" },
            Operations = new List<IOperationDefinition>
            {
                new FilterOperationDefinition { Expression = "" },
                new AsyncFunctionOperationDefinition { FunctionType = "", TimeoutMs = 0 },
                new StateOperationDefinition { StateType = "invalid", StateKey = "" }
            },
            Sink = new KafkaSinkDefinition { Topic = "" }
        };

        var result = JobDefinitionValidator.Validate(job);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Count, Is.GreaterThanOrEqualTo(8)); // Multiple validation errors
        Assert.That(result.Errors, Contains.Item("metadata.jobId is required"));
        Assert.That(result.Errors, Contains.Item("metadata.version is required"));
        Assert.That(result.Errors, Contains.Item("source.kafka.topic is required"));
        Assert.That(result.Errors, Contains.Item("operations[0].filter.expression is required"));
        Assert.That(result.Errors, Contains.Item("sink.kafka.topic is required"));
    }

    #endregion
}
