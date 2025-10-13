using Flink.JobBuilder;
using Flink.JobBuilder.Models;
using Xunit;

namespace Flink.JobBuilder.Tests.Tests;

public class FlinkJobBuilderApiTests
{
    [Fact]
    public void FromKafka_CreatesBuilderWithKafkaSource()
    {
        // Act
        var builder = FlinkJobBuilder.FromKafka("test-topic", "localhost:9092");
        var jobDef = builder.ToConsole().BuildJobDefinition();

        // Assert
        Assert.NotNull(jobDef.Source);
        Assert.IsType<KafkaSourceDefinition>(jobDef.Source);
        var source = (KafkaSourceDefinition)jobDef.Source;
        Assert.Equal("test-topic", source.Topic);
        Assert.Equal("localhost:9092", source.BootstrapServers);
    }

    [Fact]
    public void FromHttp_CreatesBuilderWithHttpSource()
    {
        // Act
        var builder = FlinkJobBuilder.FromHttp("https://api.example.com/data", "GET", 30);
        var jobDef = builder.ToConsole().BuildJobDefinition();

        // Assert
        Assert.NotNull(jobDef.Source);
        Assert.IsType<HttpSourceDefinition>(jobDef.Source);
        var source = (HttpSourceDefinition)jobDef.Source;
        Assert.Equal("https://api.example.com/data", source.Url);
        Assert.Equal("GET", source.Method);
        Assert.Equal(30, source.IntervalSeconds);
    }

    [Fact]
    public void FromDatabase_CreatesBuilderWithDatabaseSource()
    {
        // Act
        var builder = FlinkJobBuilder.FromDatabase("Server=localhost", "SELECT * FROM users", 60);
        var jobDef = builder.ToConsole().BuildJobDefinition();

        // Assert
        Assert.NotNull(jobDef.Source);
        Assert.IsType<DatabaseSourceDefinition>(jobDef.Source);
        var source = (DatabaseSourceDefinition)jobDef.Source;
        Assert.Equal("Server=localhost", source.ConnectionString);
        Assert.Equal("SELECT * FROM users", source.Query);
        Assert.Equal(60, source.PollingIntervalSeconds);
    }

    [Fact]
    public void FromSql_CreatesBuilderWithSqlSource()
    {
        // Arrange
        var statements = new List<string> { "CREATE TABLE users (id INT)", "INSERT INTO users VALUES (1)" };

        // Act
        var builder = FlinkJobBuilder.FromSql(statements);
        var jobDef = builder.BuildJobDefinition();

        // Assert
        Assert.NotNull(jobDef.Source);
        Assert.IsType<SqlSourceDefinition>(jobDef.Source);
        var source = (SqlSourceDefinition)jobDef.Source;
        Assert.Equal(2, source.Statements.Count);
    }

    [Fact]
    public void Where_AddsFilterOperation()
    {
        // Act
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .Where("value > 100")
            .ToConsole();
        var jobDef = builder.BuildJobDefinition();

        // Assert
        Assert.Single(jobDef.Operations);
        Assert.IsType<FilterOperationDefinition>(jobDef.Operations[0]);
        var filter = (FilterOperationDefinition)jobDef.Operations[0];
        Assert.Equal("value > 100", filter.Expression);
    }

    [Fact]
    public void GroupBy_AddsGroupByOperation()
    {
        // Act
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .GroupBy("region")
            .ToConsole();
        var jobDef = builder.BuildJobDefinition();

        // Assert
        Assert.Single(jobDef.Operations);
        Assert.IsType<GroupByOperationDefinition>(jobDef.Operations[0]);
        var groupBy = (GroupByOperationDefinition)jobDef.Operations[0];
        Assert.Equal("region", groupBy.Key);
    }

    [Fact]
    public void Aggregate_AddsAggregateOperation()
    {
        // Act
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .Aggregate("SUM", "amount")
            .ToConsole();
        var jobDef = builder.BuildJobDefinition();

        // Assert
        Assert.Single(jobDef.Operations);
        Assert.IsType<AggregateOperationDefinition>(jobDef.Operations[0]);
        var agg = (AggregateOperationDefinition)jobDef.Operations[0];
        Assert.Equal("SUM", agg.AggregationType);
        Assert.Equal("amount", agg.Field);
    }

    [Fact]
    public void Map_AddsMapOperation()
    {
        // Act
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .Map("x => x.ToUpper()")
            .ToConsole();
        var jobDef = builder.BuildJobDefinition();

        // Assert
        Assert.Single(jobDef.Operations);
        Assert.IsType<MapOperationDefinition>(jobDef.Operations[0]);
        var map = (MapOperationDefinition)jobDef.Operations[0];
        Assert.Equal("x => x.ToUpper()", map.Expression);
    }

    [Fact]
    public void Window_AddsWindowOperation()
    {
        // Act
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .Window("TUMBLING", 5, "MINUTES")
            .ToConsole();
        var jobDef = builder.BuildJobDefinition();

        // Assert
        Assert.Single(jobDef.Operations);
        Assert.IsType<WindowOperationDefinition>(jobDef.Operations[0]);
        var window = (WindowOperationDefinition)jobDef.Operations[0];
        Assert.Equal("TUMBLING", window.WindowType);
        Assert.Equal(5, window.Size);
        Assert.Equal("MINUTES", window.TimeUnit);
    }

    [Fact]
    public void AsyncHttp_AddsAsyncHttpOperation()
    {
        // Act
        var headers = new Dictionary<string, string> { ["Authorization"] = "Bearer token" };
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .AsyncHttp("https://api.example.com", "POST", 3000, headers, "{\"key\": \"value\"}")
            .ToConsole();
        var jobDef = builder.BuildJobDefinition();

        // Assert
        Assert.Single(jobDef.Operations);
        Assert.IsType<AsyncFunctionOperationDefinition>(jobDef.Operations[0]);
        var asyncOp = (AsyncFunctionOperationDefinition)jobDef.Operations[0];
        Assert.Equal("http", asyncOp.FunctionType);
        Assert.Equal("https://api.example.com", asyncOp.Url);
        Assert.Equal("POST", asyncOp.Method);
        Assert.Equal(3000, asyncOp.TimeoutMs);
        Assert.Contains("Authorization", asyncOp.Headers.Keys);
    }

    [Fact]
    public void AsyncDatabase_AddsAsyncDatabaseOperation()
    {
        // Act
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .AsyncDatabase("Server=localhost", "SELECT * FROM lookup", 2000)
            .ToConsole();
        var jobDef = builder.BuildJobDefinition();

        // Assert
        Assert.Single(jobDef.Operations);
        Assert.IsType<AsyncFunctionOperationDefinition>(jobDef.Operations[0]);
        var asyncOp = (AsyncFunctionOperationDefinition)jobDef.Operations[0];
        Assert.Equal("database", asyncOp.FunctionType);
        Assert.Equal("Server=localhost", asyncOp.ConnectionString);
        Assert.Equal("SELECT * FROM lookup", asyncOp.Query);
        Assert.Equal(2000, asyncOp.TimeoutMs);
    }

    [Fact]
    public void WithState_AddsStateOperation()
    {
        // Act
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .WithState("counter", "value", 60000, "0")
            .ToConsole();
        var jobDef = builder.BuildJobDefinition();

        // Assert
        Assert.Single(jobDef.Operations);
        Assert.IsType<StateOperationDefinition>(jobDef.Operations[0]);
        var state = (StateOperationDefinition)jobDef.Operations[0];
        Assert.Equal("counter", state.StateKey);
        Assert.Equal("value", state.StateType);
        Assert.Equal(60000, state.TtlMs);
        Assert.Equal("0", state.DefaultValue);
    }

    [Fact]
    public void WithTimer_AddsTimerOperation()
    {
        // Act
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .WithTimer(5000, "cleanup", "cleanupAction")
            .ToConsole();
        var jobDef = builder.BuildJobDefinition();

        // Assert
        Assert.Single(jobDef.Operations);
        Assert.IsType<TimerOperationDefinition>(jobDef.Operations[0]);
        var timer = (TimerOperationDefinition)jobDef.Operations[0];
        Assert.Equal(5000, timer.DelayMs);
        Assert.Equal("cleanup", timer.TimerName);
        Assert.Equal("cleanupAction", timer.Action);
    }

    [Fact]
    public void WithRetry_AddsRetryOperation()
    {
        // Act
        var delayPattern = new List<long> { 1000, 2000, 4000 };
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .WithRetry(3, delayPattern, "error != null", "dead-letter-topic")
            .ToConsole();
        var jobDef = builder.BuildJobDefinition();

        // Assert
        Assert.Single(jobDef.Operations);
        Assert.IsType<RetryOperationDefinition>(jobDef.Operations[0]);
        var retry = (RetryOperationDefinition)jobDef.Operations[0];
        Assert.Equal(3, retry.MaxRetries);
        Assert.Equal(3, retry.DelayMs.Count);
        Assert.Equal("error != null", retry.RetryCondition);
        Assert.Equal("dead-letter-topic", retry.DeadLetterTopic);
    }

    [Fact]
    public void WithProcessFunction_AddsProcessFunctionOperation()
    {
        // Act
        var parameters = new Dictionary<string, object> { ["threshold"] = 100 };
        var stateKeys = new List<string> { "state1", "state2" };
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .WithProcessFunction("custom", parameters, stateKeys)
            .ToConsole();
        var jobDef = builder.BuildJobDefinition();

        // Assert
        Assert.Single(jobDef.Operations);
        Assert.IsType<ProcessFunctionOperationDefinition>(jobDef.Operations[0]);
        var process = (ProcessFunctionOperationDefinition)jobDef.Operations[0];
        Assert.Equal("custom", process.ProcessType);
        Assert.Contains("threshold", process.Parameters.Keys);
        Assert.Equal(2, process.StateKeys.Count);
    }

    [Fact]
    public void WithSideOutput_AddsSideOutputOperation()
    {
        // Act
        var sideOutputSink = new KafkaSinkDefinition { Topic = "errors" };
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .WithSideOutput("errors", "isError == true", sideOutputSink)
            .ToConsole();
        var jobDef = builder.BuildJobDefinition();

        // Assert
        Assert.Single(jobDef.Operations);
        Assert.IsType<SideOutputOperationDefinition>(jobDef.Operations[0]);
        var sideOutput = (SideOutputOperationDefinition)jobDef.Operations[0];
        Assert.Equal("errors", sideOutput.OutputTag);
        Assert.Equal("isError == true", sideOutput.Condition);
        Assert.NotNull(sideOutput.SideOutputSink);
    }

    [Fact]
    public void ToKafka_SetsKafkaSink()
    {
        // Act
        var builder = FlinkJobBuilder.FromKafka("input-topic")
            .ToKafka("output-topic", "localhost:9092");
        var jobDef = builder.BuildJobDefinition();

        // Assert
        Assert.NotNull(jobDef.Sink);
        Assert.IsType<KafkaSinkDefinition>(jobDef.Sink);
        var sink = (KafkaSinkDefinition)jobDef.Sink;
        Assert.Equal("output-topic", sink.Topic);
        Assert.Equal("localhost:9092", sink.BootstrapServers);
    }

    [Fact]
    public void ToConsole_SetsConsoleSink()
    {
        // Act
        var builder = FlinkJobBuilder.FromKafka("input-topic")
            .ToConsole();
        var jobDef = builder.BuildJobDefinition();

        // Assert
        Assert.NotNull(jobDef.Sink);
        Assert.IsType<ConsoleSinkDefinition>(jobDef.Sink);
    }

    [Fact]
    public void ToHttp_SetsHttpSink()
    {
        // Act
        var headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" };
        var builder = FlinkJobBuilder.FromKafka("input-topic")
            .ToHttp("https://api.example.com/events", "POST", headers, "{\"data\": \"$value\"}");
        var jobDef = builder.BuildJobDefinition();

        // Assert
        Assert.NotNull(jobDef.Sink);
        Assert.IsType<HttpSinkDefinition>(jobDef.Sink);
        var sink = (HttpSinkDefinition)jobDef.Sink;
        Assert.Equal("https://api.example.com/events", sink.Url);
        Assert.Equal("POST", sink.Method);
        Assert.Contains("Content-Type", sink.Headers.Keys);
        Assert.Equal("{\"data\": \"$value\"}", sink.BodyTemplate);
    }

    [Fact]
    public void ToDatabase_SetsDatabaseSink()
    {
        // Act
        var builder = FlinkJobBuilder.FromKafka("input-topic")
            .ToDatabase("Server=localhost", "events", "mysql");
        var jobDef = builder.BuildJobDefinition();

        // Assert
        Assert.NotNull(jobDef.Sink);
        Assert.IsType<DatabaseSinkDefinition>(jobDef.Sink);
        var sink = (DatabaseSinkDefinition)jobDef.Sink;
        Assert.Equal("Server=localhost", sink.ConnectionString);
        Assert.Equal("events", sink.Table);
        Assert.Equal("mysql", sink.DatabaseType);
    }

    [Fact]
    public void ToRedis_SetsRedisSink()
    {
        // Act
        var builder = FlinkJobBuilder.FromKafka("input-topic")
            .ToRedis("counter", "localhost:6379", "increment");
        var jobDef = builder.BuildJobDefinition();

        // Assert
        Assert.NotNull(jobDef.Sink);
        Assert.IsType<RedisSinkDefinition>(jobDef.Sink);
        var sink = (RedisSinkDefinition)jobDef.Sink;
        Assert.Equal("counter", sink.Key);
        Assert.Equal("localhost:6379", sink.ConnectionString);
        Assert.Equal("increment", sink.OperationType);
        Assert.True((bool)sink.Configuration["exactly_once"]);
    }

    [Fact]
    public void BuildJobDefinition_ThrowsWhenNoSource()
    {
        // Arrange
        var builder = new FlinkJobBuilder();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => builder.BuildJobDefinition());
    }

    [Fact]
    public void BuildJobDefinition_ThrowsWhenNoSink_AndNotSql()
    {
        // Arrange
        var builder = FlinkJobBuilder.FromKafka("test-topic");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => builder.BuildJobDefinition());
    }

    [Fact]
    public void BuildJobDefinition_AllowsNoSink_ForSql()
    {
        // Arrange
        var builder = FlinkJobBuilder.FromSql(new List<string> { "SELECT * FROM table" });

        // Act
        var jobDef = builder.BuildJobDefinition();

        // Assert
        Assert.NotNull(jobDef);
        Assert.Null(jobDef.Sink);
    }

    [Fact]
    public void ToJson_ReturnsValidJson()
    {
        // Act
        var json = FlinkJobBuilder.FromKafka("test-topic")
            .Map("x => x.ToUpper()")
            .ToConsole()
            .ToJson();

        // Assert
        Assert.NotNull(json);
        Assert.Contains("test-topic", json);
        Assert.Contains("ToUpper", json);
    }

    [Fact]
    public void ComplexPipeline_BuildsCorrectly()
    {
        // Act
        var builder = FlinkJobBuilder.FromKafka("input-topic", "localhost:9092")
            .Where("amount > 0")
            .Map("x => x.ToUpper()")
            .GroupBy("region")
            .Aggregate("SUM", "amount")
            .Window("TUMBLING", 1, "MINUTES")
            .WithState("counter", "value")
            .ToKafka("output-topic", "localhost:9092");
        
        var jobDef = builder.BuildJobDefinition();

        // Assert
        Assert.NotNull(jobDef);
        Assert.IsType<KafkaSourceDefinition>(jobDef.Source);
        Assert.IsType<KafkaSinkDefinition>(jobDef.Sink);
        Assert.Equal(6, jobDef.Operations.Count);
        Assert.IsType<FilterOperationDefinition>(jobDef.Operations[0]);
        Assert.IsType<MapOperationDefinition>(jobDef.Operations[1]);
        Assert.IsType<GroupByOperationDefinition>(jobDef.Operations[2]);
        Assert.IsType<AggregateOperationDefinition>(jobDef.Operations[3]);
        Assert.IsType<WindowOperationDefinition>(jobDef.Operations[4]);
        Assert.IsType<StateOperationDefinition>(jobDef.Operations[5]);
    }

    [Fact]
    public void Where_WithLambda_AddsFilterOperation()
    {
        // Act
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .Where<string>(x => x.Length > 5)
            .ToConsole();
        var jobDef = builder.BuildJobDefinition();

        // Assert
        Assert.Single(jobDef.Operations);
        Assert.IsType<FilterOperationDefinition>(jobDef.Operations[0]);
    }

    [Fact]
    public void GroupBy_WithLambda_AddsGroupByOperation()
    {
        // Act
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .GroupBy<string, int>(x => x.Length)
            .ToConsole();
        var jobDef = builder.BuildJobDefinition();

        // Assert
        Assert.Single(jobDef.Operations);
        Assert.IsType<GroupByOperationDefinition>(jobDef.Operations[0]);
    }

    [Fact]
    public void Aggregate_WithLambda_AddsAggregateOperation()
    {
        // Act
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .Aggregate<string, int>("SUM", x => x.Length)
            .ToConsole();
        var jobDef = builder.BuildJobDefinition();

        // Assert
        Assert.Single(jobDef.Operations);
        Assert.IsType<AggregateOperationDefinition>(jobDef.Operations[0]);
    }

    [Fact]
    public void BuildJobDefinition_SetsMetadata()
    {
        // Act
        var jobDef = FlinkJobBuilder.FromKafka("test-topic")
            .ToConsole()
            .BuildJobDefinition();

        // Assert
        Assert.NotNull(jobDef.Metadata);
        Assert.NotNull(jobDef.Metadata.JobId);
        Assert.NotEqual(Guid.Empty.ToString(), jobDef.Metadata.JobId);
        Assert.Equal("1.0", jobDef.Metadata.Version);
        Assert.True((DateTime.UtcNow - jobDef.Metadata.CreatedAt).TotalSeconds < 2);
    }
}
