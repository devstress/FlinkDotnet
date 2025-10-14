using Flink.JobBuilder.Models;
using NUnit.Framework;

namespace Flink.JobBuilder.Tests.Tests;

[TestFixture]
public class JobDefinitionModelTests
{
    #region JobDefinition Tests

    [Test]
    public void JobDefinition_DefaultConstructor_InitializesProperties()
    {
        var jobDef = new JobDefinition();

        Assert.That(jobDef.Source, Is.Null);
        Assert.That(jobDef.Operations, Is.Not.Null);
        Assert.That(jobDef.Operations, Is.Empty);
        Assert.That(jobDef.Sink, Is.Null);
        Assert.That(jobDef.Metadata, Is.Not.Null);
    }

    [Test]
    public void JobDefinition_SetAllProperties_ReturnsValues()
    {
        var source = new KafkaSourceDefinition { Topic = "input-topic" };
        var operations = new List<IOperationDefinition>
        {
            new FilterOperationDefinition { Expression = "x > 0" }
        };
        var sink = new KafkaSinkDefinition { Topic = "output-topic" };
        var metadata = new JobMetadata { JobId = "test-job" };

        var jobDef = new JobDefinition
        {
            Source = source,
            Operations = operations,
            Sink = sink,
            Metadata = metadata
        };

        Assert.That(jobDef.Source, Is.EqualTo(source));
        Assert.That(jobDef.Operations, Is.EqualTo(operations));
        Assert.That(jobDef.Sink, Is.EqualTo(sink));
        Assert.That(jobDef.Metadata, Is.EqualTo(metadata));
    }

    [Test]
    public void JobDefinition_WithMultipleOperations_StoresAll()
    {
        var jobDef = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "test" },
            Operations = new List<IOperationDefinition>
            {
                new FilterOperationDefinition { Expression = "x > 0" },
                new MapOperationDefinition { Expression = "x => x * 2" },
                new AggregateOperationDefinition { AggregationType = "SUM", Field = "value" }
            }
        };

        Assert.That(jobDef.Operations, Has.Count.EqualTo(3));
        Assert.That(jobDef.Operations[0], Is.InstanceOf<FilterOperationDefinition>());
        Assert.That(jobDef.Operations[1], Is.InstanceOf<MapOperationDefinition>());
        Assert.That(jobDef.Operations[2], Is.InstanceOf<AggregateOperationDefinition>());
    }

    [Test]
    public void JobDefinition_WithNullSink_AllowsPureSqlJobs()
    {
        var jobDef = new JobDefinition
        {
            Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT * FROM table" } },
            Sink = null
        };

        Assert.That(jobDef.Sink, Is.Null);
        Assert.That(jobDef.Source, Is.InstanceOf<SqlSourceDefinition>());
    }

    #endregion

    #region JobMetadata Edge Cases

    [Test]
    public void JobMetadata_DefaultConstructor_InitializesEmptyStrings()
    {
        var metadata = new JobMetadata();

        Assert.That(metadata.JobId, Is.EqualTo(string.Empty));
        Assert.That(metadata.Version, Is.EqualTo(string.Empty));
        Assert.That(metadata.CreatedAt, Is.EqualTo(default(DateTime)));
    }

    [Test]
    public void JobMetadata_AllProperties_CanBeSet()
    {
        var createdAt = DateTime.UtcNow;
        var properties = new Dictionary<string, string>
        {
            { "env", "prod" },
            { "team", "data-team" }
        };

        var metadata = new JobMetadata
        {
            JobId = "job-123",
            JobName = "Test Job",
            CreatedAt = createdAt,
            Version = "1.0.0",
            Parallelism = 8,
            Properties = properties
        };

        Assert.That(metadata.JobId, Is.EqualTo("job-123"));
        Assert.That(metadata.JobName, Is.EqualTo("Test Job"));
        Assert.That(metadata.CreatedAt, Is.EqualTo(createdAt));
        Assert.That(metadata.Version, Is.EqualTo("1.0.0"));
        Assert.That(metadata.Parallelism, Is.EqualTo(8));
        Assert.That(metadata.Properties["env"], Is.EqualTo("prod"));
        Assert.That(metadata.Properties["team"], Is.EqualTo("data-team"));
    }

    [Test]
    public void JobMetadata_Properties_EmptyByDefault()
    {
        var metadata = new JobMetadata();

        Assert.That(metadata.Properties, Is.Not.Null);
        Assert.That(metadata.Properties, Is.Empty);
    }

    #endregion

    #region Source Definition Edge Cases

    [Test]
    public void KafkaSourceDefinition_SetAllProperties_ReturnsValues()
    {
        var props = new Dictionary<string, string> { { "max.poll.records", "500" } };
        var source = new KafkaSourceDefinition
        {
            Topic = "test-topic",
            BootstrapServers = "localhost:9092",
            GroupId = "test-group",
            StartingOffsets = "latest",
            Properties = props
        };

        Assert.That(source.Type, Is.EqualTo("kafka"));
        Assert.That(source.Topic, Is.EqualTo("test-topic"));
        Assert.That(source.BootstrapServers, Is.EqualTo("localhost:9092"));
        Assert.That(source.GroupId, Is.EqualTo("test-group"));
        Assert.That(source.StartingOffsets, Is.EqualTo("latest"));
        Assert.That(source.Properties, Is.EqualTo(props));
    }

    [Test]
    public void FileSourceDefinition_DefaultConstructor_SetsDefaults()
    {
        var source = new FileSourceDefinition();

        Assert.That(source.Type, Is.EqualTo("file"));
        Assert.That(source.Path, Is.EqualTo(string.Empty));
        Assert.That(source.Format, Is.EqualTo("text"));
        Assert.That(source.Properties, Is.Not.Null);
        Assert.That(source.Properties, Is.Empty);
    }

    [Test]
    public void HttpSourceDefinition_AllProperties_Functional()
    {
        var headers = new Dictionary<string, string> { { "Authorization", "Bearer token" } };
        var props = new Dictionary<string, string> { { "timeout", "30" } };

        var source = new HttpSourceDefinition
        {
            Url = "https://api.example.com/data",
            Method = "POST",
            Headers = headers,
            Body = "{\"query\":\"test\"}",
            IntervalSeconds = 120,
            AuthTokenStateKey = "auth_state",
            Properties = props
        };

        Assert.That(source.Url, Is.EqualTo("https://api.example.com/data"));
        Assert.That(source.Method, Is.EqualTo("POST"));
        Assert.That(source.Headers, Has.Count.EqualTo(1));
        Assert.That(source.Body, Is.EqualTo("{\"query\":\"test\"}"));
        Assert.That(source.IntervalSeconds, Is.EqualTo(120));
        Assert.That(source.AuthTokenStateKey, Is.EqualTo("auth_state"));
        Assert.That(source.Properties, Has.Count.EqualTo(1));
    }

    [Test]
    public void DatabaseSourceDefinition_AllProperties_Functional()
    {
        var props = new Dictionary<string, string> { { "batch_size", "1000" } };

        var source = new DatabaseSourceDefinition
        {
            ConnectionString = "Server=localhost;Database=testdb;",
            Query = "SELECT * FROM users WHERE active = 1",
            DatabaseType = "mysql",
            PollingIntervalSeconds = 60,
            Properties = props
        };

        Assert.That(source.ConnectionString, Contains.Substring("testdb"));
        Assert.That(source.Query, Contains.Substring("active = 1"));
        Assert.That(source.DatabaseType, Is.EqualTo("mysql"));
        Assert.That(source.PollingIntervalSeconds, Is.EqualTo(60));
    }

    [Test]
    public void SqlSourceDefinition_EmptyStatements_InitializedCorrectly()
    {
        var source = new SqlSourceDefinition();

        Assert.That(source.Statements, Is.Not.Null);
        Assert.That(source.Statements, Is.Empty);
        Assert.That(source.Mode, Is.EqualTo("streaming"));
        Assert.That(source.ExecutionMode, Is.EqualTo("tableenv"));
    }

    [Test]
    public void SqlSourceDefinition_MultipleStatements_StoresAll()
    {
        var statements = new List<string>
        {
            "CREATE TABLE source_table (id INT, name STRING)",
            "INSERT INTO sink_table SELECT * FROM source_table"
        };

        var source = new SqlSourceDefinition
        {
            Statements = statements,
            Mode = "batch",
            ExecutionMode = "gateway"
        };

        Assert.That(source.Statements, Has.Count.EqualTo(2));
        Assert.That(source.Mode, Is.EqualTo("batch"));
        Assert.That(source.ExecutionMode, Is.EqualTo("gateway"));
    }

    #endregion

    #region Operation Definition Edge Cases

    [Test]
    public void AggregateOperationDefinition_BothWindowTypes_CanBeSet()
    {
        var op = new AggregateOperationDefinition
        {
            AggregationType = "COUNT",
            Field = "transactions",
            WindowSeconds = 86400,
            WindowCount = 100
        };

        Assert.That(op.WindowSeconds, Is.EqualTo(86400));
        Assert.That(op.WindowCount, Is.EqualTo(100));
    }

    [Test]
    public void AggregateOperationDefinition_Alias_Optional()
    {
        var opWithAlias = new AggregateOperationDefinition
        {
            AggregationType = "AVG",
            Field = "score",
            Alias = "average_score"
        };

        var opWithoutAlias = new AggregateOperationDefinition
        {
            AggregationType = "MAX",
            Field = "value"
        };

        Assert.That(opWithAlias.Alias, Is.EqualTo("average_score"));
        Assert.That(opWithoutAlias.Alias, Is.Null);
    }

    [Test]
    public void JoinOperationDefinition_WithWindow_AllPropertiesSet()
    {
        var window = new WindowOperationDefinition
        {
            WindowType = "TUMBLING",
            Size = 300,
            TimeUnit = "SECONDS"
        };

        var op = new JoinOperationDefinition
        {
            JoinType = "LEFT",
            RightSource = new KafkaSourceDefinition { Topic = "right" },
            LeftKey = "user_id",
            RightKey = "id",
            Window = window
        };

        Assert.That(op.Window, Is.Not.Null);
        Assert.That(op.Window!.WindowType, Is.EqualTo("TUMBLING"));
        Assert.That(op.Window.Size, Is.EqualTo(300));
    }

    [Test]
    public void AsyncFunctionOperationDefinition_CacheTtl_Optional()
    {
        var opWithCache = new AsyncFunctionOperationDefinition
        {
            FunctionType = "http",
            StateKey = "cache",
            CacheTtlMs = 300000
        };

        var opWithoutCache = new AsyncFunctionOperationDefinition
        {
            FunctionType = "database"
        };

        Assert.That(opWithCache.CacheTtlMs, Is.EqualTo(300000));
        Assert.That(opWithoutCache.CacheTtlMs, Is.Null);
    }

    [Test]
    public void ProcessFunctionOperationDefinition_ComplexParameters_StoredCorrectly()
    {
        var parameters = new Dictionary<string, object>
        {
            { "timeout", 5000 },
            { "retries", 3 },
            { "enabled", true }
        };

        var op = new ProcessFunctionOperationDefinition
        {
            ProcessType = "complex",
            Parameters = parameters,
            StateKeys = new List<string> { "state1", "state2" },
            TimerNames = new List<string> { "timer1" }
        };

        Assert.That(op.Parameters, Has.Count.EqualTo(3));
        Assert.That(op.Parameters["timeout"], Is.EqualTo(5000));
        Assert.That(op.StateKeys, Has.Count.EqualTo(2));
        Assert.That(op.TimerNames, Has.Count.EqualTo(1));
    }

    [Test]
    public void StateOperationDefinition_AllStateTypes_Supported()
    {
        var valueState = new StateOperationDefinition { StateType = "value" };
        var listState = new StateOperationDefinition { StateType = "list" };
        var mapState = new StateOperationDefinition { StateType = "map" };
        var reducingState = new StateOperationDefinition { StateType = "reducing" };

        Assert.That(valueState.StateType, Is.EqualTo("value"));
        Assert.That(listState.StateType, Is.EqualTo("list"));
        Assert.That(mapState.StateType, Is.EqualTo("map"));
        Assert.That(reducingState.StateType, Is.EqualTo("reducing"));
    }

    [Test]
    public void StateOperationDefinition_WithTtl_ConfiguresCorrectly()
    {
        var op = new StateOperationDefinition
        {
            StateType = "value",
            StateKey = "session_state",
            TtlMs = 3600000,
            DefaultValue = "{}"
        };

        Assert.That(op.TtlMs, Is.EqualTo(3600000));
        Assert.That(op.DefaultValue, Is.EqualTo("{}"));
    }

    [Test]
    public void TimerOperationDefinition_BothTimerTypes_Supported()
    {
        var processingTimer = new TimerOperationDefinition
        {
            TimerType = "processing",
            DelayMs = 60000
        };

        var eventTimer = new TimerOperationDefinition
        {
            TimerType = "event",
            DelayMs = 120000
        };

        Assert.That(processingTimer.TimerType, Is.EqualTo("processing"));
        Assert.That(eventTimer.TimerType, Is.EqualTo("event"));
    }

    [Test]
    public void RetryOperationDefinition_CustomDelays_StoresCorrectly()
    {
        var customDelays = new List<long> { 1000, 5000, 15000, 60000 };

        var op = new RetryOperationDefinition
        {
            MaxRetries = 4,
            DelayMs = customDelays,
            RetryCondition = "statusCode >= 500",
            DeadLetterTopic = "dlq"
        };

        Assert.That(op.DelayMs, Has.Count.EqualTo(4));
        Assert.That(op.DelayMs[0], Is.EqualTo(1000));
        Assert.That(op.DelayMs[3], Is.EqualTo(60000));
    }

    [Test]
    public void SideOutputOperationDefinition_WithCondition_ConfiguresCorrectly()
    {
        var op = new SideOutputOperationDefinition
        {
            OutputTag = "errors",
            Condition = "errorCode != null",
            SideOutputSink = new KafkaSinkDefinition { Topic = "errors" }
        };

        Assert.That(op.OutputTag, Is.EqualTo("errors"));
        Assert.That(op.Condition, Is.EqualTo("errorCode != null"));
        Assert.That(op.SideOutputSink, Is.InstanceOf<KafkaSinkDefinition>());
    }

    #endregion

    #region Sink Definition Edge Cases

    [Test]
    public void RedisSinkDefinition_Configuration_StoredCorrectly()
    {
        var config = new Dictionary<string, object>
        {
            { "ttl", 3600 },
            { "db", 0 },
            { "async", true }
        };

        var sink = new RedisSinkDefinition
        {
            ConnectionString = "redis://localhost:6379",
            Key = "counter:{id}",
            OperationType = "increment",
            Configuration = config
        };

        Assert.That(sink.Configuration, Has.Count.EqualTo(3));
        Assert.That(sink.Configuration["ttl"], Is.EqualTo(3600));
        Assert.That(sink.Configuration["async"], Is.EqualTo(true));
    }

    [Test]
    public void HttpSinkDefinition_AllProperties_Functional()
    {
        var headers = new Dictionary<string, string> { { "Content-Type", "application/json" } };
        var props = new Dictionary<string, string> { { "retry", "3" } };

        var sink = new HttpSinkDefinition
        {
            Url = "https://webhook.example.com/data",
            Method = "PUT",
            Headers = headers,
            BodyTemplate = "{\"data\": {value}}",
            AuthTokenStateKey = "auth",
            TimeoutMs = 10000,
            Properties = props
        };

        Assert.That(sink.Url, Contains.Substring("webhook"));
        Assert.That(sink.Method, Is.EqualTo("PUT"));
        Assert.That(sink.TimeoutMs, Is.EqualTo(10000));
        Assert.That(sink.BodyTemplate, Contains.Substring("{value}"));
    }

    #endregion

    #region Window Operation Edge Cases

    [Test]
    public void WindowOperationDefinition_SlidingWindow_WithSlide()
    {
        var op = new WindowOperationDefinition
        {
            WindowType = "SLIDING",
            Size = 60,
            Slide = 30,
            TimeUnit = "MINUTES"
        };

        Assert.That(op.WindowType, Is.EqualTo("SLIDING"));
        Assert.That(op.Size, Is.EqualTo(60));
        Assert.That(op.Slide, Is.EqualTo(30));
    }

    [Test]
    public void WindowOperationDefinition_WithTimeField_EventTime()
    {
        var op = new WindowOperationDefinition
        {
            WindowType = "TUMBLING",
            Size = 10,
            TimeUnit = "SECONDS",
            TimeField = "eventTimestamp"
        };

        Assert.That(op.TimeField, Is.EqualTo("eventTimestamp"));
    }

    [Test]
    public void WindowOperationDefinition_AllTimeUnits_Supported()
    {
        var seconds = new WindowOperationDefinition { TimeUnit = "SECONDS" };
        var minutes = new WindowOperationDefinition { TimeUnit = "MINUTES" };
        var hours = new WindowOperationDefinition { TimeUnit = "HOURS" };

        Assert.That(seconds.TimeUnit, Is.EqualTo("SECONDS"));
        Assert.That(minutes.TimeUnit, Is.EqualTo("MINUTES"));
        Assert.That(hours.TimeUnit, Is.EqualTo("HOURS"));
    }

    #endregion
}
