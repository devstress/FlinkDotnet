using Flink.JobBuilder.Models;
using Flink.JobBuilder.Services;

namespace Flink.JobBuilder.Tests;

/// <summary>
/// Additional source validation tests for JobDefinitionValidator
/// Targets uncovered branches for HttpSource and DatabaseSource validation
/// </summary>
[TestFixture]
public class JobDefinitionSourceValidationTests
{
    #region HttpSource Validation Tests

    [Test]
    public void ValidateHttpSource_WithNullUrl_ReturnsInvalid()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new HttpSourceDefinition
            {
                Url = null!,
                Method = "GET"
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ValidateHttpSource_WithEmptyUrl_ReturnsInvalid()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new HttpSourceDefinition
            {
                Url = "",
                Method = "GET"
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ValidateHttpSource_WithZeroIntervalSeconds_ReturnsInvalid()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new HttpSourceDefinition
            {
                Url = "http://api.example.com",
                Method = "GET",
                IntervalSeconds = 0
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ValidateHttpSource_WithNegativeIntervalSeconds_ReturnsInvalid()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new HttpSourceDefinition
            {
                Url = "http://api.example.com",
                Method = "GET",
                IntervalSeconds = -10
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
    }

    #endregion

    #region DatabaseSource Validation Tests

    [Test]
    public void ValidateDatabaseSource_WithNullConnectionString_ReturnsInvalid()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new DatabaseSourceDefinition
            {
                ConnectionString = null!,
                Query = "SELECT * FROM users"
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ValidateDatabaseSource_WithEmptyConnectionString_ReturnsInvalid()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new DatabaseSourceDefinition
            {
                ConnectionString = "",
                Query = "SELECT * FROM users"
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ValidateDatabaseSource_WithNullQuery_ReturnsInvalid()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new DatabaseSourceDefinition
            {
                ConnectionString = "Server=localhost;Database=test;",
                Query = null!
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ValidateDatabaseSource_WithEmptyQuery_ReturnsInvalid()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new DatabaseSourceDefinition
            {
                ConnectionString = "Server=localhost;Database=test;",
                Query = ""
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ValidateDatabaseSource_WithZeroPollingInterval_ReturnsInvalid()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new DatabaseSourceDefinition
            {
                ConnectionString = "Server=localhost;Database=test;",
                Query = "SELECT * FROM users",
                PollingIntervalSeconds = 0
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ValidateDatabaseSource_WithNegativePollingInterval_ReturnsInvalid()
    {
        // Arrange
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "test", Version = "1.0" },
            Source = new DatabaseSourceDefinition
            {
                ConnectionString = "Server=localhost;Database=test;",
                Query = "SELECT * FROM users",
                PollingIntervalSeconds = -5
            },
            Sink = new ConsoleSinkDefinition()
        };

        // Act
        var result = JobDefinitionValidator.Validate(job);

        // Assert
        Assert.That(result.IsValid, Is.False);
    }

    #endregion
}
