using Flink.JobBuilder.Models;
using Flink.JobBuilder.Services;

namespace Flink.JobBuilder.Tests;

/// <summary>
/// Tests for default/unknown cases in JobDefinitionValidator switch statements
/// These tests achieve 100% branch coverage by exercising default cases
/// </summary>
[TestFixture]
public class JobDefinitionValidatorDefaultCaseTests
{
    #region Unknown Source Type Tests

    /// <summary>
    /// Test unknown source type to cover default case in ValidateSource switch
    /// This covers line 70 default case (1 missing branch out of 10)
    /// </summary>
    [Test]
    public void ValidateSource_WithUnknownSourceType_DoesNotAddError()
    {
        // Arrange - Create a mock unknown source type
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new UnknownSourceDefinition(),
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert - Unknown source types are not validated (no error added)
        // This should pass validation since the switch doesn't have a default error case
        Assert.That(result.IsValid, Is.True);
    }

    #endregion

    #region Unknown Operation Type Tests

    /// <summary>
    /// Test unknown operation type to cover default case in ValidateOperation switch
    /// This covers line 130 default case (1 missing branch out of 24)
    /// </summary>
    [Test]
    public void ValidateOperation_WithUnknownOperationType_DoesNotAddError()
    {
        // Arrange - Create a job with unknown operation type
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition
            {
                Topic = "test",
                BootstrapServers = "localhost:9092",
                GroupId = "group1"
            },
            Operations = new List<IOperationDefinition>
            {
                new UnknownOperationDefinition()
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert - Unknown operation types are not validated (no error added)
        Assert.That(result.IsValid, Is.True);
    }

    #endregion

    #region Unknown Sink Type Tests

    /// <summary>
    /// Test unknown sink type to cover default case in ValidateSink switch  
    /// </summary>
    [Test]
    public void ValidateSink_WithUnknownSinkType_DoesNotAddError()
    {
        // Arrange - Create a job with unknown sink type
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { Version = "1.0" },
            Source = new KafkaSourceDefinition
            {
                Topic = "test",
                BootstrapServers = "localhost:9092",
                GroupId = "group1"
            },
            Sink = new UnknownSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert - Unknown sink types are not validated (no error added)
        Assert.That(result.IsValid, Is.True);
    }

    #endregion
}

#region Helper Classes for Unknown Types

/// <summary>
/// Unknown source type for testing default case in ValidateSource
/// </summary>
public class UnknownSourceDefinition : ISourceDefinition
{
    public string Type => "unknown";
}

/// <summary>
/// Unknown operation type for testing default case in ValidateOperation
/// </summary>
public class UnknownOperationDefinition : IOperationDefinition
{
    public string Type => "unknown";
}

/// <summary>
/// Unknown sink type for testing default case in ValidateSink
/// </summary>
public class UnknownSinkDefinition : ISinkDefinition
{
    public string Type => "unknown";
}

#endregion
