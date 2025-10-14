using Flink.JobBuilder.Models;
using Flink.JobBuilder.Services;
using NUnit.Framework;

namespace Flink.JobBuilder.Tests.Tests;

[TestFixture]
public class FlinkJobBuilderCoreTests
{
    #region Operation Methods Tests

    [Test]
    public void Where_WithExpression_AddsFilterOperation()
    {
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .Where("Amount > 100")
            .ToConsole();

        var jobDef = builder.BuildJobDefinition();
        
        Assert.That(jobDef.Operations, Has.Count.EqualTo(1));
        Assert.That(jobDef.Operations[0], Is.TypeOf<FilterOperationDefinition>());
        var filterOp = (FilterOperationDefinition)jobDef.Operations[0];
        Assert.That(filterOp.Expression, Is.EqualTo("Amount > 100"));
    }

    [Test]
    public void Where_WithLambda_AddsFilterOperation()
    {
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .Where<int>(x => x > 100)
            .ToConsole();

        var jobDef = builder.BuildJobDefinition();
        
        Assert.That(jobDef.Operations, Has.Count.EqualTo(1));
        Assert.That(jobDef.Operations[0], Is.TypeOf<FilterOperationDefinition>());
    }

    [Test]
    public void GroupBy_WithKeyField_AddsGroupByOperation()
    {
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .GroupBy("Region")
            .ToConsole();

        var jobDef = builder.BuildJobDefinition();
        
        Assert.That(jobDef.Operations, Has.Count.EqualTo(1));
        Assert.That(jobDef.Operations[0], Is.TypeOf<GroupByOperationDefinition>());
        var groupByOp = (GroupByOperationDefinition)jobDef.Operations[0];
        Assert.That(groupByOp.Key, Is.EqualTo("Region"));
    }

    [Test]
    public void GroupBy_WithLambda_AddsGroupByOperation()
    {
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .GroupBy<string, string>(x => x.ToUpper())
            .ToConsole();

        var jobDef = builder.BuildJobDefinition();
        
        Assert.That(jobDef.Operations, Has.Count.EqualTo(1));
        Assert.That(jobDef.Operations[0], Is.TypeOf<GroupByOperationDefinition>());
    }

    [Test]
    public void Aggregate_WithTypeAndField_AddsAggregateOperation()
    {
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .Aggregate("SUM", "Amount")
            .ToConsole();

        var jobDef = builder.BuildJobDefinition();
        
        Assert.That(jobDef.Operations, Has.Count.EqualTo(1));
        Assert.That(jobDef.Operations[0], Is.TypeOf<AggregateOperationDefinition>());
        var aggOp = (AggregateOperationDefinition)jobDef.Operations[0];
        Assert.That(aggOp.AggregationType, Is.EqualTo("SUM"));
        Assert.That(aggOp.Field, Is.EqualTo("Amount"));
    }

    [Test]
    public void Aggregate_WithLambda_AddsAggregateOperation()
    {
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .Aggregate<int, int>("COUNT", x => x)
            .ToConsole();

        var jobDef = builder.BuildJobDefinition();
        
        Assert.That(jobDef.Operations, Has.Count.EqualTo(1));
        Assert.That(jobDef.Operations[0], Is.TypeOf<AggregateOperationDefinition>());
    }

    [Test]
    public void Map_WithExpression_AddsMapOperation()
    {
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .Map("x => x.ToUpper()")
            .ToConsole();

        var jobDef = builder.BuildJobDefinition();
        
        Assert.That(jobDef.Operations, Has.Count.EqualTo(1));
        Assert.That(jobDef.Operations[0], Is.TypeOf<MapOperationDefinition>());
        var mapOp = (MapOperationDefinition)jobDef.Operations[0];
        Assert.That(mapOp.Expression, Is.EqualTo("x => x.ToUpper()"));
    }

    [Test]
    public void Window_WithAllParameters_AddsWindowOperation()
    {
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .Window("TUMBLING", 5, "MINUTES")
            .ToConsole();

        var jobDef = builder.BuildJobDefinition();
        
        Assert.That(jobDef.Operations, Has.Count.EqualTo(1));
        Assert.That(jobDef.Operations[0], Is.TypeOf<WindowOperationDefinition>());
        var windowOp = (WindowOperationDefinition)jobDef.Operations[0];
        Assert.That(windowOp.WindowType, Is.EqualTo("TUMBLING"));
        Assert.That(windowOp.Size, Is.EqualTo(5));
        Assert.That(windowOp.TimeUnit, Is.EqualTo("MINUTES"));
    }

    [Test]
    public void Window_WithDefaultTimeUnit_UsesMinutes()
    {
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .Window("SLIDING", 10)
            .ToConsole();

        var jobDef = builder.BuildJobDefinition();
        
        var windowOp = (WindowOperationDefinition)jobDef.Operations[0];
        Assert.That(windowOp.TimeUnit, Is.EqualTo("MINUTES"));
    }

    [Test]
    public void AsyncHttp_WithAllParameters_AddsAsyncFunctionOperation()
    {
        var headers = new Dictionary<string, string> { { "Authorization", "Bearer token" } };
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .AsyncHttp("http://api.example.com/enrich", "POST", 3000, headers, "{data}");

        var jobDef = builder.BuildJobDefinition();
        
        Assert.That(jobDef.Operations, Has.Count.EqualTo(1));
        Assert.That(jobDef.Operations[0], Is.TypeOf<AsyncFunctionOperationDefinition>());
        var asyncOp = (AsyncFunctionOperationDefinition)jobDef.Operations[0];
        Assert.That(asyncOp.FunctionType, Is.EqualTo("http"));
        Assert.That(asyncOp.Url, Is.EqualTo("http://api.example.com/enrich"));
        Assert.That(asyncOp.Method, Is.EqualTo("POST"));
        Assert.That(asyncOp.TimeoutMs, Is.EqualTo(3000));
        Assert.That(asyncOp.Headers, Contains.Key("Authorization"));
        Assert.That(asyncOp.BodyTemplate, Is.EqualTo("{data}"));
    }

    [Test]
    public void AsyncHttp_WithDefaults_UsesDefaultValues()
    {
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .AsyncHttp("http://api.example.com/data")
            .ToConsole();

        var jobDef = builder.BuildJobDefinition();
        
        var asyncOp = (AsyncFunctionOperationDefinition)jobDef.Operations[0];
        Assert.That(asyncOp.Method, Is.EqualTo("GET"));
        Assert.That(asyncOp.TimeoutMs, Is.EqualTo(5000));
        Assert.That(asyncOp.Headers, Is.Not.Null);
    }

    [Test]
    public void AsyncDatabase_WithAllParameters_AddsAsyncFunctionOperation()
    {
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .AsyncDatabase("Server=localhost;Database=test", "SELECT * FROM users WHERE id = @id", 2000)
            .ToConsole();

        var jobDef = builder.BuildJobDefinition();
        
        Assert.That(jobDef.Operations, Has.Count.EqualTo(1));
        Assert.That(jobDef.Operations[0], Is.TypeOf<AsyncFunctionOperationDefinition>());
        var asyncOp = (AsyncFunctionOperationDefinition)jobDef.Operations[0];
        Assert.That(asyncOp.FunctionType, Is.EqualTo("database"));
        Assert.That(asyncOp.ConnectionString, Is.EqualTo("Server=localhost;Database=test"));
        Assert.That(asyncOp.Query, Is.EqualTo("SELECT * FROM users WHERE id = @id"));
        Assert.That(asyncOp.TimeoutMs, Is.EqualTo(2000));
    }

    [Test]
    public void WithState_WithAllParameters_AddsStateOperation()
    {
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .WithState("user-cache", "map", 60000, "{}")
            .ToConsole();

        var jobDef = builder.BuildJobDefinition();
        
        Assert.That(jobDef.Operations, Has.Count.EqualTo(1));
        Assert.That(jobDef.Operations[0], Is.TypeOf<StateOperationDefinition>());
        var stateOp = (StateOperationDefinition)jobDef.Operations[0];
        Assert.That(stateOp.StateKey, Is.EqualTo("user-cache"));
        Assert.That(stateOp.StateType, Is.EqualTo("map"));
        Assert.That(stateOp.TtlMs, Is.EqualTo(60000));
        Assert.That(stateOp.DefaultValue, Is.EqualTo("{}"));
    }

    [Test]
    public void WithState_WithDefaults_UsesDefaultValues()
    {
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .WithState("counter")
            .ToConsole();

        var jobDef = builder.BuildJobDefinition();
        
        var stateOp = (StateOperationDefinition)jobDef.Operations[0];
        Assert.That(stateOp.StateType, Is.EqualTo("value"));
        Assert.That(stateOp.TtlMs, Is.Null);
    }

    [Test]
    public void WithTimer_WithAllParameters_AddsTimerOperation()
    {
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .WithTimer(5000, "cleanup-timer", "cleanup-expired")
            .ToConsole();

        var jobDef = builder.BuildJobDefinition();
        
        Assert.That(jobDef.Operations, Has.Count.EqualTo(1));
        Assert.That(jobDef.Operations[0], Is.TypeOf<TimerOperationDefinition>());
        var timerOp = (TimerOperationDefinition)jobDef.Operations[0];
        Assert.That(timerOp.DelayMs, Is.EqualTo(5000));
        Assert.That(timerOp.TimerName, Is.EqualTo("cleanup-timer"));
        Assert.That(timerOp.Action, Is.EqualTo("cleanup-expired"));
    }

    [Test]
    public void WithRetry_WithAllParameters_AddsRetryOperation()
    {
        var delayPattern = new List<long> { 1000, 2000, 4000 };
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .WithRetry(3, delayPattern, "error.type == 'transient'", "dlq-topic");

        var jobDef = builder.BuildJobDefinition();
        
        Assert.That(jobDef.Operations, Has.Count.EqualTo(1));
        Assert.That(jobDef.Operations[0], Is.TypeOf<RetryOperationDefinition>());
        var retryOp = (RetryOperationDefinition)jobDef.Operations[0];
        Assert.That(retryOp.MaxRetries, Is.EqualTo(3));
        Assert.That(retryOp.DelayMs, Is.EqualTo(delayPattern));
        Assert.That(retryOp.RetryCondition, Is.EqualTo("error.type == 'transient'"));
        Assert.That(retryOp.DeadLetterTopic, Is.EqualTo("dlq-topic"));
    }

    [Test]
    public void WithRetry_WithDefaults_UsesDefaultDelayPattern()
    {
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .WithRetry()
            .ToConsole();

        var jobDef = builder.BuildJobDefinition();
        
        var retryOp = (RetryOperationDefinition)jobDef.Operations[0];
        Assert.That(retryOp.MaxRetries, Is.EqualTo(5));
        Assert.That(retryOp.DelayMs, Has.Count.EqualTo(5));
    }

    [Test]
    public void WithProcessFunction_WithAllParameters_AddsProcessFunctionOperation()
    {
        var parameters = new Dictionary<string, object> { { "threshold", 100 } };
        var stateKeys = new List<string> { "counter", "timer" };
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .WithProcessFunction("deduplication", parameters, stateKeys);

        var jobDef = builder.BuildJobDefinition();
        
        Assert.That(jobDef.Operations, Has.Count.EqualTo(1));
        Assert.That(jobDef.Operations[0], Is.TypeOf<ProcessFunctionOperationDefinition>());
        var processOp = (ProcessFunctionOperationDefinition)jobDef.Operations[0];
        Assert.That(processOp.ProcessType, Is.EqualTo("deduplication"));
        Assert.That(processOp.Parameters, Contains.Key("threshold"));
        Assert.That(processOp.StateKeys, Has.Count.EqualTo(2));
    }

    [Test]
    public void WithProcessFunction_WithDefaults_CreatesEmptyCollections()
    {
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .WithProcessFunction("custom")
            .ToConsole();

        var jobDef = builder.BuildJobDefinition();
        
        var processOp = (ProcessFunctionOperationDefinition)jobDef.Operations[0];
        Assert.That(processOp.Parameters, Is.Not.Null);
        Assert.That(processOp.StateKeys, Is.Not.Null);
    }

    [Test]
    public void WithSideOutput_WithAllParameters_AddsSideOutputOperation()
    {
        var sideOutputSink = new KafkaSinkDefinition { Topic = "errors-topic" };
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .WithSideOutput("errors", "error != null", sideOutputSink)
            .ToConsole();

        var jobDef = builder.BuildJobDefinition();
        
        Assert.That(jobDef.Operations, Has.Count.EqualTo(1));
        Assert.That(jobDef.Operations[0], Is.TypeOf<SideOutputOperationDefinition>());
        var sideOp = (SideOutputOperationDefinition)jobDef.Operations[0];
        Assert.That(sideOp.OutputTag, Is.EqualTo("errors"));
        Assert.That(sideOp.Condition, Is.EqualTo("error != null"));
        Assert.That(sideOp.SideOutputSink, Is.TypeOf<KafkaSinkDefinition>());
    }

    #endregion

    #region Source Methods Tests

    [Test]
    public void FromKafka_WithBootstrapServers_SetsAllProperties()
    {
        var builder = FlinkJobBuilder.FromKafka("orders-topic", "localhost:9092");

        var jobDef = builder.BuildJobDefinition();
        
        Assert.That(jobDef.Source, Is.TypeOf<KafkaSourceDefinition>());
        var kafkaSource = (KafkaSourceDefinition)jobDef.Source;
        Assert.That(kafkaSource.Topic, Is.EqualTo("orders-topic"));
        Assert.That(kafkaSource.BootstrapServers, Is.EqualTo("localhost:9092"));
    }

    [Test]
    public void FromKafka_WithoutBootstrapServers_SetsTopicOnly()
    {
        var builder = FlinkJobBuilder.FromKafka("test-topic");

        var jobDef = builder.BuildJobDefinition();
        
        var kafkaSource = (KafkaSourceDefinition)jobDef.Source;
        Assert.That(kafkaSource.Topic, Is.EqualTo("test-topic"));
        Assert.That(kafkaSource.BootstrapServers, Is.Null);
    }

    [Test]
    public void FromHttp_WithAllParameters_SetsAllProperties()
    {
        var builder = FlinkJobBuilder.FromHttp("http://api.example.com/data", "POST", 45);

        var jobDef = builder.BuildJobDefinition();
        
        Assert.That(jobDef.Source, Is.TypeOf<HttpSourceDefinition>());
        var httpSource = (HttpSourceDefinition)jobDef.Source;
        Assert.That(httpSource.Url, Is.EqualTo("http://api.example.com/data"));
        Assert.That(httpSource.Method, Is.EqualTo("POST"));
        Assert.That(httpSource.IntervalSeconds, Is.EqualTo(45));
    }

    [Test]
    public void FromHttp_WithDefaults_UsesDefaultValues()
    {
        var builder = FlinkJobBuilder.FromHttp("http://api.example.com");

        var jobDef = builder.BuildJobDefinition();
        
        var httpSource = (HttpSourceDefinition)jobDef.Source;
        Assert.That(httpSource.Method, Is.EqualTo("GET"));
        Assert.That(httpSource.IntervalSeconds, Is.EqualTo(60));
    }

    [Test]
    public void FromDatabase_WithAllParameters_SetsAllProperties()
    {
        var builder = FlinkJobBuilder.FromDatabase(
            "Server=localhost;Database=prod",
            "SELECT * FROM events",
            120);

        var jobDef = builder.BuildJobDefinition();
        
        Assert.That(jobDef.Source, Is.TypeOf<DatabaseSourceDefinition>());
        var dbSource = (DatabaseSourceDefinition)jobDef.Source;
        Assert.That(dbSource.ConnectionString, Is.EqualTo("Server=localhost;Database=prod"));
        Assert.That(dbSource.Query, Is.EqualTo("SELECT * FROM events"));
        Assert.That(dbSource.PollingIntervalSeconds, Is.EqualTo(120));
    }

    [Test]
    public void FromDatabase_WithDefaults_UsesDefaultInterval()
    {
        var builder = FlinkJobBuilder.FromDatabase(
            "Server=localhost;Database=test",
            "SELECT * FROM users");

        var jobDef = builder.BuildJobDefinition();
        
        var dbSource = (DatabaseSourceDefinition)jobDef.Source;
        Assert.That(dbSource.PollingIntervalSeconds, Is.EqualTo(30));
    }

    [Test]
    public void FromSql_WithStatements_SetsStatementsList()
    {
        var statements = new List<string>
        {
            "CREATE TABLE orders (id INT, amount DECIMAL)",
            "SELECT * FROM orders WHERE amount > 100"
        };
        var builder = FlinkJobBuilder.FromSql(statements);

        var jobDef = builder.BuildJobDefinition();
        
        Assert.That(jobDef.Source, Is.TypeOf<SqlSourceDefinition>());
        var sqlSource = (SqlSourceDefinition)jobDef.Source;
        Assert.That(sqlSource.Statements, Has.Count.EqualTo(2));
        Assert.That(sqlSource.Statements[0], Contains.Substring("CREATE TABLE"));
    }

    [Test]
    public void FromSql_WithNullStatements_CreatesEmptyList()
    {
        var builder = FlinkJobBuilder.FromSql(null);

        var jobDef = builder.BuildJobDefinition();
        
        var sqlSource = (SqlSourceDefinition)jobDef.Source;
        Assert.That(sqlSource.Statements, Is.Not.Null);
        Assert.That(sqlSource.Statements, Is.Empty);
    }

    #endregion

    #region Sink Methods Tests

    [Test]
    public void ToKafka_WithBootstrapServers_SetsAllProperties()
    {
        var builder = FlinkJobBuilder.FromKafka("input-topic")
            .ToKafka("output-topic", "localhost:9092");

        var jobDef = builder.BuildJobDefinition();
        
        Assert.That(jobDef.Sink, Is.TypeOf<KafkaSinkDefinition>());
        var kafkaSink = (KafkaSinkDefinition)jobDef.Sink;
        Assert.That(kafkaSink.Topic, Is.EqualTo("output-topic"));
        Assert.That(kafkaSink.BootstrapServers, Is.EqualTo("localhost:9092"));
    }

    [Test]
    public void ToKafka_WithoutBootstrapServers_SetsTopicOnly()
    {
        var builder = FlinkJobBuilder.FromKafka("input-topic")
            .ToKafka("output-topic");

        var jobDef = builder.BuildJobDefinition();
        
        var kafkaSink = (KafkaSinkDefinition)jobDef.Sink;
        Assert.That(kafkaSink.Topic, Is.EqualTo("output-topic"));
        Assert.That(kafkaSink.BootstrapServers, Is.Null);
    }

    [Test]
    public void ToConsole_CreatesConsoleSink()
    {
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .ToConsole();

        var jobDef = builder.BuildJobDefinition();
        
        Assert.That(jobDef.Sink, Is.TypeOf<ConsoleSinkDefinition>());
    }

    [Test]
    public void ToHttp_WithAllParameters_SetsAllProperties()
    {
        var headers = new Dictionary<string, string> { { "Content-Type", "application/json" } };
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .ToHttp("http://api.example.com/webhook", "PUT", headers, "{\"data\": \"@value\"}");

        var jobDef = builder.BuildJobDefinition();
        
        Assert.That(jobDef.Sink, Is.TypeOf<HttpSinkDefinition>());
        var httpSink = (HttpSinkDefinition)jobDef.Sink;
        Assert.That(httpSink.Url, Is.EqualTo("http://api.example.com/webhook"));
        Assert.That(httpSink.Method, Is.EqualTo("PUT"));
        Assert.That(httpSink.Headers, Contains.Key("Content-Type"));
        Assert.That(httpSink.BodyTemplate, Is.EqualTo("{\"data\": \"@value\"}"));
    }

    [Test]
    public void ToHttp_WithDefaults_UsesDefaultValues()
    {
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .ToHttp("http://api.example.com/data");

        var jobDef = builder.BuildJobDefinition();
        
        var httpSink = (HttpSinkDefinition)jobDef.Sink;
        Assert.That(httpSink.Method, Is.EqualTo("POST"));
        Assert.That(httpSink.Headers, Is.Not.Null);
    }

    [Test]
    public void ToDatabase_WithAllParameters_SetsAllProperties()
    {
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .ToDatabase("Server=localhost;Database=output", "results", "mysql");

        var jobDef = builder.BuildJobDefinition();
        
        Assert.That(jobDef.Sink, Is.TypeOf<DatabaseSinkDefinition>());
        var dbSink = (DatabaseSinkDefinition)jobDef.Sink;
        Assert.That(dbSink.ConnectionString, Is.EqualTo("Server=localhost;Database=output"));
        Assert.That(dbSink.Table, Is.EqualTo("results"));
        Assert.That(dbSink.DatabaseType, Is.EqualTo("mysql"));
    }

    [Test]
    public void ToDatabase_WithDefaults_UsesPostgresql()
    {
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .ToDatabase("Server=localhost", "table1");

        var jobDef = builder.BuildJobDefinition();
        
        var dbSink = (DatabaseSinkDefinition)jobDef.Sink;
        Assert.That(dbSink.DatabaseType, Is.EqualTo("postgresql"));
    }

    [Test]
    public void ToRedis_WithAllParameters_SetsAllProperties()
    {
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .ToRedis("counter:key", "localhost:6380", "set");

        var jobDef = builder.BuildJobDefinition();
        
        Assert.That(jobDef.Sink, Is.TypeOf<RedisSinkDefinition>());
        var redisSink = (RedisSinkDefinition)jobDef.Sink;
        Assert.That(redisSink.Key, Is.EqualTo("counter:key"));
        Assert.That(redisSink.ConnectionString, Is.EqualTo("localhost:6380"));
        Assert.That(redisSink.OperationType, Is.EqualTo("set"));
        Assert.That(redisSink.Configuration, Contains.Key("exactly_once"));
    }

    [Test]
    public void ToRedis_WithDefaults_UsesDefaultValues()
    {
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .ToRedis("mykey");

        var jobDef = builder.BuildJobDefinition();
        
        var redisSink = (RedisSinkDefinition)jobDef.Sink;
        Assert.That(redisSink.ConnectionString, Is.EqualTo("localhost:6379"));
        Assert.That(redisSink.OperationType, Is.EqualTo("increment"));
    }

    #endregion

    #region BuildJobDefinition Tests

    [Test]
    public void BuildJobDefinition_WithoutSource_ThrowsInvalidOperationException()
    {
        var builder = new FlinkJobBuilder();

        var ex = Assert.Throws<InvalidOperationException>(() => builder.BuildJobDefinition());
        Assert.That(ex.Message, Contains.Substring("Job must have a source"));
    }

    [Test]
    public void BuildJobDefinition_WithoutSinkAndNonSqlSource_ThrowsInvalidOperationException()
    {
        var builder = FlinkJobBuilder.FromKafka("test-topic");

        var ex = Assert.Throws<InvalidOperationException>(() => builder.BuildJobDefinition());
        Assert.That(ex.Message, Contains.Substring("Job must have a sink"));
    }

    [Test]
    public void BuildJobDefinition_WithSqlSourceAndNoSink_Succeeds()
    {
        var builder = FlinkJobBuilder.FromSql(new[] { "SELECT * FROM table1" });

        var jobDef = builder.BuildJobDefinition();
        
        Assert.That(jobDef.Source, Is.TypeOf<SqlSourceDefinition>());
        Assert.That(jobDef.Sink, Is.Null);
    }

    [Test]
    public void BuildJobDefinition_WithCompleteJob_SetsAllProperties()
    {
        var builder = FlinkJobBuilder.FromKafka("input-topic")
            .Where("Amount > 100")
            .Map("x => x.ToUpper()")
            .ToKafka("output-topic");

        var jobDef = builder.BuildJobDefinition();
        
        Assert.That(jobDef.Source, Is.Not.Null);
        Assert.That(jobDef.Operations, Has.Count.EqualTo(2));
        Assert.That(jobDef.Sink, Is.Not.Null);
        Assert.That(jobDef.Metadata, Is.Not.Null);
        Assert.That(jobDef.Metadata.JobId, Is.Not.Empty);
        Assert.That(jobDef.Metadata.Version, Is.EqualTo("1.0"));
    }

    [Test]
    public void BuildJobDefinition_GeneratesUniqueJobId()
    {
        var builder1 = FlinkJobBuilder.FromKafka("test-topic").ToConsole();
        var builder2 = FlinkJobBuilder.FromKafka("test-topic").ToConsole();

        var jobDef1 = builder1.BuildJobDefinition();
        var jobDef2 = builder2.BuildJobDefinition();
        
        Assert.That(jobDef1.Metadata.JobId, Is.Not.EqualTo(jobDef2.Metadata.JobId));
    }

    #endregion

    #region Method Chaining Tests

    [Test]
    public void MethodChaining_AllMethodsReturnBuilder()
    {
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .Where("x > 0")
            .GroupBy("Region")
            .Aggregate("SUM", "Amount")
            .Map("x => x.ToUpper()")
            .Window("TUMBLING", 5)
            .AsyncHttp("http://api.example.com")
            .WithState("cache")
            .WithTimer(1000)
            .WithRetry(3)
            .WithProcessFunction("custom")
            .ToKafka("output-topic");

        var jobDef = builder.BuildJobDefinition();
        
        Assert.That(jobDef.Operations, Has.Count.EqualTo(10));
    }

    [Test]
    public void MethodChaining_CanApplyMultipleSameOperations()
    {
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .Where("Amount > 100")
            .Where("Status == 'ACTIVE'")
            .Map("x => x.ToUpper()")
            .Map("x => x.Trim()")
            .ToConsole();

        var jobDef = builder.BuildJobDefinition();
        
        Assert.That(jobDef.Operations, Has.Count.EqualTo(4));
    }

    #endregion

    #region ToJson Tests

    [Test]
    public void ToJson_ReturnsValidJsonString()
    {
        var builder = FlinkJobBuilder.FromKafka("test-topic")
            .Where("Amount > 100")
            .ToConsole();

        var json = builder.ToJson();
        
        Assert.That(json, Is.Not.Null);
        Assert.That(json, Is.Not.Empty);
        Assert.That(json, Contains.Substring("source"));
        Assert.That(json, Contains.Substring("sink"));
        Assert.That(json, Contains.Substring("operations"));
    }

    [Test]
    public void ToJson_IncludesMetadata()
    {
        var builder = FlinkJobBuilder.FromKafka("test-topic").ToConsole();

        var json = builder.ToJson();
        
        Assert.That(json, Contains.Substring("metadata"));
        Assert.That(json, Contains.Substring("jobId"));
        Assert.That(json, Contains.Substring("version"));
    }

    #endregion

    #region Constructor Tests

    [Test]
    public void Constructor_WithNullParameters_CreatesDefaultService()
    {
        var builder = new FlinkJobBuilder(null, null);

        Assert.That(builder, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithProvidedService_UsesProvidedService()
    {
        var service = new TestMockFlinkJobGatewayService();
        var builder = new FlinkJobBuilder(service);

        Assert.That(builder, Is.Not.Null);
    }

    #endregion
}

#region Mock Service for Testing

/// <summary>
/// Mock implementation of IFlinkJobGatewayService for testing
/// </summary>
internal class TestMockFlinkJobGatewayService : IFlinkJobGatewayService
{
    public JobSubmissionResult? LastSubmittedResult { get; set; }
    public JobStatus? StatusToReturn { get; set; }
    public bool HealthCheckResult { get; set; } = true;

    public System.Threading.Tasks.Task<JobSubmissionResult> SubmitJobAsync(JobDefinition jobDefinition, System.Threading.CancellationToken cancellationToken = default)
    {
        LastSubmittedResult = JobSubmissionResult.CreateSuccess(
            jobDefinition.Metadata.JobId,
            $"flink-{System.Guid.NewGuid()}");
        return System.Threading.Tasks.Task.FromResult(LastSubmittedResult);
    }

    public System.Threading.Tasks.Task<JobStatus> GetJobStatusAsync(string flinkJobId, System.Threading.CancellationToken cancellationToken = default)
    {
        return System.Threading.Tasks.Task.FromResult(StatusToReturn ?? new JobStatus
        {
            FlinkJobId = flinkJobId,
            State = "RUNNING"
        });
    }

    public System.Threading.Tasks.Task<JobMetrics> GetJobMetricsAsync(string flinkJobId, System.Threading.CancellationToken cancellationToken = default)
    {
        return System.Threading.Tasks.Task.FromResult(new JobMetrics { FlinkJobId = flinkJobId });
    }

    public System.Threading.Tasks.Task<bool> CancelJobAsync(string flinkJobId, System.Threading.CancellationToken cancellationToken = default)
    {
        return System.Threading.Tasks.Task.FromResult(true);
    }

    public System.Threading.Tasks.Task<bool> HealthCheckAsync(System.Threading.CancellationToken cancellationToken = default)
    {
        return System.Threading.Tasks.Task.FromResult(HealthCheckResult);
    }
}

#endregion
