using NUnit.Framework;
using Flink.JobBuilder.Models;
using Flink.JobBuilder.Services;
using System.Collections.Generic;

namespace Flink.JobBuilder.Tests;

/// <summary>
/// Comprehensive branch coverage tests to achieve 100% branch coverage for JobDefinitionValidator
/// Focuses on all condition combinations and edge cases
/// </summary>
[TestFixture]
public class JobDefinitionValidatorFullBranchCoverageTests
{
    #region Metadata Validation - All Branch Combinations

    [Test]
    public void ValidateMetadata_WithNullMetadata_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = null,
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("metadata is required"));
    }

    [Test]
    public void ValidateMetadata_WithValidParallelism_NoError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0", Parallelism = 5 },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateMetadata_WithZeroParallelism_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0", Parallelism = 0 },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("parallelism must be >= 1"));
    }

    [Test]
    public void ValidateMetadata_WithNegativeParallelism_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0", Parallelism = -1 },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("parallelism must be >= 1"));
    }

    [Test]
    public void ValidateMetadata_WithoutParallelism_NoError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" }, // Parallelism not set
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    #endregion

    #region JobStructure Validation - SQL Job with No Sink

    [Test]
    public void ValidateJobStructure_SqlJobWithNoSink_NoError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
            Sink = null // SQL jobs don't require sink
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateJobStructure_NonSqlJobWithNoSink_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = null
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("sink is required"));
    }

    [Test]
    public void ValidateJobStructure_NonSqlJobWithSink_NoError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    #endregion

    #region GroupBy Operation - Key vs Keys Branch Coverage

    [Test]
    public void ValidateGroupByOperation_WithKey_NoError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new GroupByOperationDefinition { Key = "field1" }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateGroupByOperation_WithKeys_NoError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new GroupByOperationDefinition { Keys = new List<string> { "field1", "field2" } }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateGroupByOperation_WithNullKeyAndNullKeys_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
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
        Assert.That(result.Errors, Has.Some.Contains("key or keys is required"));
    }

    [Test]
    public void ValidateGroupByOperation_WithEmptyKeyAndEmptyKeys_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
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
        Assert.That(result.Errors, Has.Some.Contains("key or keys is required"));
    }

    [Test]
    public void ValidateGroupByOperation_WithWhitespaceKeyAndEmptyKeys_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new GroupByOperationDefinition { Key = "   ", Keys = new List<string>() }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("key or keys is required"));
    }

    #endregion

    #region Aggregate Operation - All Valid Types Coverage

    [Test]
    public void ValidateAggregateOperation_WithCountType_NoError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new AggregateOperationDefinition { AggregationType = "COUNT", Field = "field1" }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateAggregateOperation_WithAvgType_NoError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new AggregateOperationDefinition { AggregationType = "AVG", Field = "field1" }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateAggregateOperation_WithMinType_NoError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new AggregateOperationDefinition { AggregationType = "MIN", Field = "field1" }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateAggregateOperation_WithMaxType_NoError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new AggregateOperationDefinition { AggregationType = "MAX", Field = "field1" }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateAggregateOperation_WithCollectType_NoError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new AggregateOperationDefinition { AggregationType = "COLLECT", Field = "field1" }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateAggregateOperation_WithInvalidType_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new AggregateOperationDefinition { AggregationType = "INVALID", Field = "field1" }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("aggregationType must be one of"));
    }

    #endregion

    #region Window Operation - All Window Types and Time Units

    [Test]
    public void ValidateWindowOperation_WithSlidingType_NoError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new WindowOperationDefinition { WindowType = "SLIDING", Size = 10, TimeUnit = "SECONDS", Slide = 5 }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateWindowOperation_WithSessionType_NoError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new WindowOperationDefinition { WindowType = "SESSION", Size = 10, TimeUnit = "MINUTES" }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateWindowOperation_WithTumblingType_NoError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new WindowOperationDefinition { WindowType = "TUMBLING", Size = 1, TimeUnit = "HOURS" }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.True);
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
                new WindowOperationDefinition { WindowType = "INVALID", Size = 10, TimeUnit = "SECONDS" }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("windowType must be one of"));
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
                new WindowOperationDefinition { WindowType = "TUMBLING", Size = 10, TimeUnit = "DAYS" }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("timeUnit must be one of"));
    }

    [Test]
    public void ValidateWindowOperation_SlidingWithoutSlide_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new WindowOperationDefinition { WindowType = "SLIDING", Size = 10, TimeUnit = "SECONDS", Slide = null }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("slide is required"));
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
                new WindowOperationDefinition { WindowType = "SLIDING", Size = 10, TimeUnit = "SECONDS", Slide = 0 }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("slide is required and must be > 0"));
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
                new WindowOperationDefinition { WindowType = "SLIDING", Size = 10, TimeUnit = "SECONDS", Slide = -5 }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("slide is required and must be > 0"));
    }

    #endregion

    #region AsyncFunction Operation - Boundary Value Tests

    [Test]
    public void ValidateAsyncFunctionOperation_WithTimeoutAtBoundary_NoError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new AsyncFunctionOperationDefinition { FunctionType = "enrichment", TimeoutMs = 1_200_000, MaxRetries = 0 }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateAsyncFunctionOperation_WithTimeoutAboveBoundary_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new AsyncFunctionOperationDefinition { FunctionType = "enrichment", TimeoutMs = 1_200_001, MaxRetries = 0 }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("timeoutMs must be between 1 and 1200000"));
    }

    [Test]
    public void ValidateAsyncFunctionOperation_WithRetriesAtMaxBoundary_NoError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new AsyncFunctionOperationDefinition { FunctionType = "enrichment", TimeoutMs = 5000, MaxRetries = 100 }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateAsyncFunctionOperation_WithRetriesAboveMax_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new AsyncFunctionOperationDefinition { FunctionType = "enrichment", TimeoutMs = 5000, MaxRetries = 101 }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("maxRetries must be between 0 and 100"));
    }

    #endregion

    #region State Operation - All State Types

    [Test]
    public void ValidateStateOperation_WithValueType_NoError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new StateOperationDefinition { StateType = "value", StateKey = "myState" }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateStateOperation_WithListType_NoError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new StateOperationDefinition { StateType = "list", StateKey = "myState" }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateStateOperation_WithMapType_NoError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new StateOperationDefinition { StateType = "map", StateKey = "myState" }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateStateOperation_WithReducingType_NoError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new StateOperationDefinition { StateType = "reducing", StateKey = "myState" }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateStateOperation_WithTtl_NoError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new StateOperationDefinition { StateType = "value", StateKey = "myState", TtlMs = 10000 }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateStateOperation_WithZeroTtl_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new StateOperationDefinition { StateType = "value", StateKey = "myState", TtlMs = 0 }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("ttlMs must be > 0"));
    }

    [Test]
    public void ValidateStateOperation_WithNegativeTtl_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new StateOperationDefinition { StateType = "value", StateKey = "myState", TtlMs = -1 }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("ttlMs must be > 0"));
    }

    #endregion

    #region Timer Operation - Timer Types

    [Test]
    public void ValidateTimerOperation_WithProcessingTimerType_NoError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new TimerOperationDefinition { TimerType = "processing", DelayMs = 5000 }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateTimerOperation_WithEventTimerType_NoError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new TimerOperationDefinition { TimerType = "event", DelayMs = 5000 }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateTimerOperation_WithDelayAtMaxBoundary_NoError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new TimerOperationDefinition { TimerType = "processing", DelayMs = 86_400_000 }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateTimerOperation_WithDelayAboveMax_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new TimerOperationDefinition { TimerType = "processing", DelayMs = 86_400_001 }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("delayMs must be between 1 and 86400000"));
    }

    #endregion

    #region Retry Operation - Comprehensive Tests

    [Test]
    public void ValidateRetryOperation_WithValidDelayMs_NoError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new RetryOperationDefinition 
                { 
                    MaxRetries = 3, 
                    DelayMs = new List<long> { 100, 200, 300 },
                    StateKey = "retryState"
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateRetryOperation_WithNullDelayMs_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new RetryOperationDefinition 
                { 
                    MaxRetries = 3, 
                    DelayMs = null,
                    StateKey = "retryState"
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("delayMs must contain at least 1 value"));
    }

    [Test]
    public void ValidateRetryOperation_WithEmptyDelayMs_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new RetryOperationDefinition 
                { 
                    MaxRetries = 3, 
                    DelayMs = new List<long>(),
                    StateKey = "retryState"
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("delayMs must contain at least 1 value"));
    }

    [Test]
    public void ValidateRetryOperation_WithZeroInDelayMs_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new RetryOperationDefinition 
                { 
                    MaxRetries = 3, 
                    DelayMs = new List<long> { 100, 0, 300 },
                    StateKey = "retryState"
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("delayMs values must be > 0"));
    }

    [Test]
    public void ValidateRetryOperation_WithNegativeInDelayMs_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new RetryOperationDefinition 
                { 
                    MaxRetries = 3, 
                    DelayMs = new List<long> { 100, 200, -50 },
                    StateKey = "retryState"
                }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("delayMs values must be > 0"));
    }

    #endregion

    #region Kafka Sink - Serializer Validation

    [Test]
    public void ValidateKafkaSink_WithJsonSerializer_NoError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "source", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new KafkaSinkDefinition { Topic = "sink", BootstrapServers = "localhost:9092", Serializer = "json" }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateKafkaSink_WithStringSerializer_NoError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "source", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new KafkaSinkDefinition { Topic = "sink", BootstrapServers = "localhost:9092", Serializer = "string" }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateKafkaSink_WithNullSerializer_NoError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "source", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new KafkaSinkDefinition { Topic = "sink", BootstrapServers = "localhost:9092", Serializer = null }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateKafkaSink_WithInvalidSerializer_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "source", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new KafkaSinkDefinition { Topic = "sink", BootstrapServers = "localhost:9092", Serializer = "avro" }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("serializer must be 'json' or 'string'"));
    }

    #endregion

    #region HttpSink - Timeout Boundary Tests

    [Test]
    public void ValidateHttpSink_WithTimeoutAtMinBoundary_NoError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new HttpSinkDefinition { Url = "http://example.com", TimeoutMs = 1 }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateHttpSink_WithTimeoutAtMaxBoundary_NoError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new HttpSinkDefinition { Url = "http://example.com", TimeoutMs = 1_200_000 }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateHttpSink_WithTimeoutAboveMax_AddsError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Sink = new HttpSinkDefinition { Url = "http://example.com", TimeoutMs = 1_200_001 }
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("timeoutMs must be between 1 and 1200000"));
    }

    #endregion

    #region Multiple Operations Coverage

    [Test]
    public void Validate_WithMultipleOperations_ProcessesAll()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>
            {
                new FilterOperationDefinition { Expression = "x > 10" },
                new MapOperationDefinition { Expression = "x * 2" },
                new GroupByOperationDefinition { Key = "userId" }
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_WithNoOperations_NoError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = null,
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_WithEmptyOperations_NoError()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092", GroupId = "group1" },
            Operations = new List<IOperationDefinition>(),
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    #endregion
}
