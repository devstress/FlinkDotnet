using Flink.JobBuilder.Extensions;
using Flink.JobBuilder.Models;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Flink.JobBuilder.Tests.Tests;

[TestFixture]
public class ExtensionsTests
{
    #region ServiceCollectionExtensions Tests

    [Test]
    public void AddFlinkJobBuilder_WithoutConfiguration_RegistersServices()
    {
        var services = new ServiceCollection();

        services.AddFlinkJobBuilder();

        var serviceProvider = services.BuildServiceProvider();
        var config = serviceProvider.GetService<FlinkJobGatewayConfiguration>();
        var builder = serviceProvider.GetService<FlinkJobBuilder>();

        Assert.That(config, Is.Not.Null);
        Assert.That(builder, Is.Not.Null);
    }

    [Test]
    public void AddFlinkJobBuilder_WithConfiguration_RegistersProvidedConfiguration()
    {
        var services = new ServiceCollection();
        var customConfig = new FlinkJobGatewayConfiguration
        {
            BaseUrl = "http://custom:8080"
        };

        services.AddFlinkJobBuilder(customConfig);

        var serviceProvider = services.BuildServiceProvider();
        var config = serviceProvider.GetService<FlinkJobGatewayConfiguration>();

        Assert.That(config, Is.Not.Null);
        Assert.That(config!.BaseUrl, Is.EqualTo("http://custom:8080"));
    }

    [Test]
    public void AddFlinkJobBuilder_ReturnsServiceCollectionForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddFlinkJobBuilder();

        Assert.That(result, Is.SameAs(services));
    }

    [Test]
    public void AddFlinkJobBuilder_WithActionConfiguration_AppliesConfiguration()
    {
        var services = new ServiceCollection();

        services.AddFlinkJobBuilder(config =>
        {
            config.BaseUrl = "http://action-configured:9090";
            config.MaxRetries = 5;
        });

        var serviceProvider = services.BuildServiceProvider();
        var config = serviceProvider.GetService<FlinkJobGatewayConfiguration>();

        Assert.That(config, Is.Not.Null);
        Assert.That(config!.BaseUrl, Is.EqualTo("http://action-configured:9090"));
        Assert.That(config.MaxRetries, Is.EqualTo(5));
    }

    [Test]
    public void AddFlinkJobBuilder_WithActionConfiguration_ReturnsServiceCollectionForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddFlinkJobBuilder(config => { });

        Assert.That(result, Is.SameAs(services));
    }

    #endregion

    #region FlinkJobBuilderExtensions Tests

    [Test]
    public void CreateJobBuilder_FromServiceProvider_ReturnsFlinkJobBuilder()
    {
        var services = new ServiceCollection();
        services.AddFlinkJobBuilder();
        var serviceProvider = services.BuildServiceProvider();

        var builder = serviceProvider.CreateJobBuilder();

        Assert.That(builder, Is.Not.Null);
        Assert.That(builder, Is.InstanceOf<FlinkJobBuilder>());
    }

    #endregion

    #region JobDefinitionExtensions Tests

    [Test]
    public void JobDefinition_Validate_WithValidDefinition_ReturnsValid()
    {
        var jobDef = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" },
            Operations = new List<IOperationDefinition>()
        };

        var result = jobDef.Validate();

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void JobDefinition_Validate_WithNullSource_ReturnsInvalid()
    {
        var jobDef = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123" },
            Source = null!,
            Sink = new KafkaSinkDefinition { Topic = "output" },
            Operations = new List<IOperationDefinition>()
        };

        var result = jobDef.Validate();

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("Job must have a source"));
    }

    [Test]
    public void JobDefinition_Validate_WithNullSink_ReturnsInvalid()
    {
        var jobDef = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = null!,
            Operations = new List<IOperationDefinition>()
        };

        var result = jobDef.Validate();

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("Job must have a sink"));
    }

    [Test]
    public void JobDefinition_Validate_WithEmptyJobId_ReturnsInvalid()
    {
        var jobDef = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" },
            Operations = new List<IOperationDefinition>()
        };

        var result = jobDef.Validate();

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("Job must have a valid JobId"));
    }

    [Test]
    public void JobDefinition_Validate_WithNullJobId_ReturnsInvalid()
    {
        var jobDef = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = null! },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" },
            Operations = new List<IOperationDefinition>()
        };

        var result = jobDef.Validate();

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("Job must have a valid JobId"));
    }

    [Test]
    public void JobDefinition_Validate_WithKafkaSourceMissingTopic_ReturnsInvalid()
    {
        var jobDef = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123" },
            Source = new KafkaSourceDefinition { Topic = "" },
            Sink = new KafkaSinkDefinition { Topic = "output" },
            Operations = new List<IOperationDefinition>()
        };

        var result = jobDef.Validate();

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("Kafka source must specify a topic"));
    }

    [Test]
    public void JobDefinition_Validate_WithFileSourceMissingPath_ReturnsInvalid()
    {
        var jobDef = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123" },
            Source = new FileSourceDefinition { Path = "" },
            Sink = new KafkaSinkDefinition { Topic = "output" },
            Operations = new List<IOperationDefinition>()
        };

        var result = jobDef.Validate();

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("File source must specify a path"));
    }

    [Test]
    public void JobDefinition_Validate_WithValidFileSource_ReturnsValid()
    {
        var jobDef = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123" },
            Source = new FileSourceDefinition { Path = "/data/input.csv" },
            Sink = new KafkaSinkDefinition { Topic = "output" },
            Operations = new List<IOperationDefinition>()
        };

        var result = jobDef.Validate();

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void JobDefinition_Validate_WithKafkaSinkMissingTopic_ReturnsInvalid()
    {
        var jobDef = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "" },
            Operations = new List<IOperationDefinition>()
        };

        var result = jobDef.Validate();

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("Kafka sink must specify a topic"));
    }

    [Test]
    public void JobDefinition_Validate_WithFileSinkMissingPath_ReturnsInvalid()
    {
        var jobDef = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new FileSinkDefinition { Path = "" },
            Operations = new List<IOperationDefinition>()
        };

        var result = jobDef.Validate();

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("File sink must specify a path"));
    }

    [Test]
    public void JobDefinition_Validate_WithDatabaseSinkMissingConnectionString_ReturnsInvalid()
    {
        var jobDef = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new DatabaseSinkDefinition { ConnectionString = "", Table = "users" },
            Operations = new List<IOperationDefinition>()
        };

        var result = jobDef.Validate();

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("Database sink must specify a connection string"));
    }

    [Test]
    public void JobDefinition_Validate_WithDatabaseSinkMissingTable_ReturnsInvalid()
    {
        var jobDef = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new DatabaseSinkDefinition { ConnectionString = "Server=localhost", Table = "" },
            Operations = new List<IOperationDefinition>()
        };

        var result = jobDef.Validate();

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("Database sink must specify a table"));
    }

    [Test]
    public void JobDefinition_Validate_WithValidDatabaseSink_ReturnsValid()
    {
        var jobDef = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new DatabaseSinkDefinition { ConnectionString = "Server=localhost", Table = "users" },
            Operations = new List<IOperationDefinition>()
        };

        var result = jobDef.Validate();

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void JobDefinition_Validate_WithValidFileSink_ReturnsValid()
    {
        var jobDef = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new FileSinkDefinition { Path = "/data/output.csv" },
            Operations = new List<IOperationDefinition>()
        };

        var result = jobDef.Validate();

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void JobDefinition_Validate_WithFilterOperationMissingExpression_ReturnsInvalid()
    {
        var jobDef = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" },
            Operations = new List<IOperationDefinition>
            {
                new FilterOperationDefinition { Expression = "" }
            }
        };

        var result = jobDef.Validate();

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("Filter operation must have an expression"));
    }

    [Test]
    public void JobDefinition_Validate_WithMapOperationMissingExpression_ReturnsInvalid()
    {
        var jobDef = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" },
            Operations = new List<IOperationDefinition>
            {
                new MapOperationDefinition { Expression = "" }
            }
        };

        var result = jobDef.Validate();

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("Map operation must have an expression"));
    }

    [Test]
    public void JobDefinition_Validate_WithGroupByOperationMissingKey_ReturnsInvalid()
    {
        var jobDef = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" },
            Operations = new List<IOperationDefinition>
            {
                new GroupByOperationDefinition { Key = "", Keys = null }
            }
        };

        var result = jobDef.Validate();

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("GroupBy operation must specify at least one key"));
    }

    [Test]
    public void JobDefinition_Validate_WithGroupByOperationEmptyKeysList_ReturnsInvalid()
    {
        var jobDef = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" },
            Operations = new List<IOperationDefinition>
            {
                new GroupByOperationDefinition { Key = "", Keys = new List<string>() }
            }
        };

        var result = jobDef.Validate();

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("GroupBy operation must specify at least one key"));
    }

    [Test]
    public void JobDefinition_Validate_WithGroupByOperationValidKey_ReturnsValid()
    {
        var jobDef = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" },
            Operations = new List<IOperationDefinition>
            {
                new GroupByOperationDefinition { Key = "userId" }
            }
        };

        var result = jobDef.Validate();

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void JobDefinition_Validate_WithGroupByOperationValidKeys_ReturnsValid()
    {
        var jobDef = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" },
            Operations = new List<IOperationDefinition>
            {
                new GroupByOperationDefinition { Keys = new List<string> { "userId", "eventType" } }
            }
        };

        var result = jobDef.Validate();

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void JobDefinition_Validate_WithAggregateOperationMissingType_ReturnsInvalid()
    {
        var jobDef = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" },
            Operations = new List<IOperationDefinition>
            {
                new AggregateOperationDefinition { AggregationType = "", Field = "count" }
            }
        };

        var result = jobDef.Validate();

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("Aggregate operation must specify aggregation type"));
    }

    [Test]
    public void JobDefinition_Validate_WithAggregateOperationMissingField_ReturnsInvalid()
    {
        var jobDef = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" },
            Operations = new List<IOperationDefinition>
            {
                new AggregateOperationDefinition { AggregationType = "SUM", Field = "" }
            }
        };

        var result = jobDef.Validate();

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("Aggregate operation must specify field"));
    }

    [Test]
    public void JobDefinition_Validate_WithWindowOperationMissingWindowType_ReturnsInvalid()
    {
        var jobDef = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" },
            Operations = new List<IOperationDefinition>
            {
                new WindowOperationDefinition { WindowType = "", Size = 60 }
            }
        };

        var result = jobDef.Validate();

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("Window operation must specify window type"));
    }

    [Test]
    public void JobDefinition_Validate_WithWindowOperationInvalidSize_ReturnsInvalid()
    {
        var jobDef = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" },
            Operations = new List<IOperationDefinition>
            {
                new WindowOperationDefinition { WindowType = "TUMBLING", Size = 0 }
            }
        };

        var result = jobDef.Validate();

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("Window operation must have a positive size"));
    }

    [Test]
    public void JobDefinition_Validate_WithWindowOperationNegativeSize_ReturnsInvalid()
    {
        var jobDef = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" },
            Operations = new List<IOperationDefinition>
            {
                new WindowOperationDefinition { WindowType = "TUMBLING", Size = -10 }
            }
        };

        var result = jobDef.Validate();

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Contains.Item("Window operation must have a positive size"));
    }

    [Test]
    public void JobDefinition_Validate_WithValidWindowOperation_ReturnsValid()
    {
        var jobDef = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" },
            Operations = new List<IOperationDefinition>
            {
                new WindowOperationDefinition { WindowType = "TUMBLING", Size = 60 }
            }
        };

        var result = jobDef.Validate();

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void JobDefinition_Validate_WithValidFilterOperation_ReturnsValid()
    {
        var jobDef = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" },
            Operations = new List<IOperationDefinition>
            {
                new FilterOperationDefinition { Expression = "value > 10" }
            }
        };

        var result = jobDef.Validate();

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void JobDefinition_Validate_WithValidMapOperation_ReturnsValid()
    {
        var jobDef = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" },
            Operations = new List<IOperationDefinition>
            {
                new MapOperationDefinition { Expression = "value * 2" }
            }
        };

        var result = jobDef.Validate();

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void JobDefinition_Validate_WithValidAggregateOperation_ReturnsValid()
    {
        var jobDef = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" },
            Operations = new List<IOperationDefinition>
            {
                new AggregateOperationDefinition { AggregationType = "SUM", Field = "amount" }
            }
        };

        var result = jobDef.Validate();

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void JobDefinition_Validate_WithUnsupportedOperationType_ReturnsValid()
    {
        // JoinOperationDefinition is not validated by JobDefinitionExtensions but is valid
        var jobDef = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" },
            Operations = new List<IOperationDefinition>
            {
                new JoinOperationDefinition { JoinType = "INNER" }
            }
        };

        var result = jobDef.Validate();

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void JobDefinition_Validate_WithAsyncFunctionOperation_ReturnsValid()
    {
        // AsyncFunctionOperationDefinition is not validated by JobDefinitionExtensions but is valid
        var jobDef = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" },
            Operations = new List<IOperationDefinition>
            {
                new AsyncFunctionOperationDefinition { FunctionType = "http", TimeoutMs = 5000 }
            }
        };

        var result = jobDef.Validate();

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void JobDefinition_Validate_WithMultipleErrors_ReturnsAllErrors()
    {
        var jobDef = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "" },
            Source = null!,
            Sink = null!,
            Operations = new List<IOperationDefinition>()
        };

        var result = jobDef.Validate();

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Count.GreaterThan(1));
    }

    #endregion

    #region JobValidationResult Tests

    [Test]
    public void JobValidationResult_DefaultConstructor_InitializesCollections()
    {
        var result = new JobValidationResult();

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Is.Not.Null);
        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Warnings, Is.Not.Null);
        Assert.That(result.Warnings, Is.Empty);
    }

    [Test]
    public void JobValidationResult_Errors_CanAddMultipleErrors()
    {
        var result = new JobValidationResult();
        result.Errors.Add("Error 1");
        result.Errors.Add("Error 2");

        Assert.That(result.Errors, Has.Count.EqualTo(2));
        Assert.That(result.Errors[0], Is.EqualTo("Error 1"));
        Assert.That(result.Errors[1], Is.EqualTo("Error 2"));
    }

    [Test]
    public void JobValidationResult_Warnings_CanAddMultipleWarnings()
    {
        var result = new JobValidationResult();
        result.Warnings.Add("Warning 1");
        result.Warnings.Add("Warning 2");

        Assert.That(result.Warnings, Has.Count.EqualTo(2));
        Assert.That(result.Warnings[0], Is.EqualTo("Warning 1"));
        Assert.That(result.Warnings[1], Is.EqualTo("Warning 2"));
    }

    #endregion
}
