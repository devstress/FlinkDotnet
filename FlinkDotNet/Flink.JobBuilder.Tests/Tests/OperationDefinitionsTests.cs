using Flink.JobBuilder.Models;

namespace Flink.JobBuilder.Tests.Tests;

[TestFixture]
public class OperationDefinitionsTests
{
    #region AggregateOperationDefinition Tests

    [Test]
    public void AggregateOperationDefinition_TypeProperty_ReturnsAggregate()
    {
        var op = new AggregateOperationDefinition();

        Assert.That(op.Type, Is.EqualTo("aggregate"));
    }

    [Test]
    public void AggregateOperationDefinition_SetAllProperties_ReturnsValues()
    {
        var op = new AggregateOperationDefinition
        {
            AggregationType = "SUM",
            Field = "amount",
            Alias = "total",
            WindowSeconds = 60,
            WindowCount = 100
        };

        Assert.That(op.AggregationType, Is.EqualTo("SUM"));
        Assert.That(op.Field, Is.EqualTo("amount"));
        Assert.That(op.Alias, Is.EqualTo("total"));
        Assert.That(op.WindowSeconds, Is.EqualTo(60));
        Assert.That(op.WindowCount, Is.EqualTo(100));
    }

    [Test]
    public void AggregateOperationDefinition_WindowSeconds_SupportsNull()
    {
        var op = new AggregateOperationDefinition
        {
            WindowSeconds = null
        };

        Assert.That(op.WindowSeconds, Is.Null);
    }

    [Test]
    public void AggregateOperationDefinition_WindowCount_SupportsNull()
    {
        var op = new AggregateOperationDefinition
        {
            WindowCount = null
        };

        Assert.That(op.WindowCount, Is.Null);
    }

    #endregion

    #region GroupByOperationDefinition Tests

    [Test]
    public void GroupByOperationDefinition_TypeProperty_ReturnsGroupBy()
    {
        var op = new GroupByOperationDefinition();

        Assert.That(op.Type, Is.EqualTo("groupBy"));
    }

    [Test]
    public void GroupByOperationDefinition_SetKey_ReturnsValue()
    {
        var op = new GroupByOperationDefinition
        {
            Key = "userId"
        };

        Assert.That(op.Key, Is.EqualTo("userId"));
    }

    [Test]
    public void GroupByOperationDefinition_SetKeys_ReturnsValue()
    {
        var keys = new List<string> { "userId", "region" };
        var op = new GroupByOperationDefinition
        {
            Keys = keys
        };

        Assert.That(op.Keys, Is.EqualTo(keys));
        Assert.That(op.Keys, Has.Count.EqualTo(2));
    }

    [Test]
    public void GroupByOperationDefinition_Keys_SupportsNull()
    {
        var op = new GroupByOperationDefinition
        {
            Keys = null
        };

        Assert.That(op.Keys, Is.Null);
    }

    #endregion

    #region AsyncFunctionOperationDefinition Tests

    [Test]
    public void AsyncFunctionOperationDefinition_TypeProperty_ReturnsAsyncFunction()
    {
        var op = new AsyncFunctionOperationDefinition();

        Assert.That(op.Type, Is.EqualTo("asyncFunction"));
    }

    [Test]
    public void AsyncFunctionOperationDefinition_SetAllProperties_ReturnsValues()
    {
        var headers = new Dictionary<string, string> { { "Auth", "Bearer token" } };
        var properties = new Dictionary<string, string> { { "key", "value" } };

        var op = new AsyncFunctionOperationDefinition
        {
            FunctionType = "http",
            Url = "http://api.example.com",
            Method = "POST",
            Headers = headers,
            BodyTemplate = "{\"id\": \"{id}\"}",
            ConnectionString = "Server=localhost",
            Query = "SELECT * FROM users",
            TimeoutMs = 3000,
            MaxRetries = 5,
            StateKey = "cache_key",
            CacheTtlMs = 60000,
            Properties = properties
        };

        Assert.That(op.FunctionType, Is.EqualTo("http"));
        Assert.That(op.Url, Is.EqualTo("http://api.example.com"));
        Assert.That(op.Method, Is.EqualTo("POST"));
        Assert.That(op.Headers, Is.EqualTo(headers));
        Assert.That(op.BodyTemplate, Is.EqualTo("{\"id\": \"{id}\"}"));
        Assert.That(op.ConnectionString, Is.EqualTo("Server=localhost"));
        Assert.That(op.Query, Is.EqualTo("SELECT * FROM users"));
        Assert.That(op.TimeoutMs, Is.EqualTo(3000));
        Assert.That(op.MaxRetries, Is.EqualTo(5));
        Assert.That(op.StateKey, Is.EqualTo("cache_key"));
        Assert.That(op.CacheTtlMs, Is.EqualTo(60000));
        Assert.That(op.Properties, Is.EqualTo(properties));
    }

    [Test]
    public void AsyncFunctionOperationDefinition_DefaultValues_AreSet()
    {
        var op = new AsyncFunctionOperationDefinition();

        Assert.That(op.Method, Is.EqualTo("GET"));
        Assert.That(op.TimeoutMs, Is.EqualTo(5000));
        Assert.That(op.MaxRetries, Is.EqualTo(3));
        Assert.That(op.Headers, Is.Not.Null);
        Assert.That(op.Properties, Is.Not.Null);
    }

    #endregion

    #region JoinOperationDefinition Tests

    [Test]
    public void JoinOperationDefinition_TypeProperty_ReturnsJoin()
    {
        var op = new JoinOperationDefinition();

        Assert.That(op.Type, Is.EqualTo("join"));
    }

    [Test]
    public void JoinOperationDefinition_SetAllProperties_ReturnsValues()
    {
        var rightSource = new KafkaSourceDefinition { Topic = "right-topic" };
        var window = new WindowOperationDefinition { WindowType = "TUMBLING", Size = 60 };

        var op = new JoinOperationDefinition
        {
            JoinType = "LEFT",
            RightSource = rightSource,
            LeftKey = "userId",
            RightKey = "id",
            Window = window
        };

        Assert.That(op.JoinType, Is.EqualTo("LEFT"));
        Assert.That(op.RightSource, Is.EqualTo(rightSource));
        Assert.That(op.LeftKey, Is.EqualTo("userId"));
        Assert.That(op.RightKey, Is.EqualTo("id"));
        Assert.That(op.Window, Is.EqualTo(window));
    }

    [Test]
    public void JoinOperationDefinition_DefaultJoinType_IsInner()
    {
        var op = new JoinOperationDefinition();

        Assert.That(op.JoinType, Is.EqualTo("INNER"));
    }

    #endregion

    #region ProcessFunctionOperationDefinition Tests

    [Test]
    public void ProcessFunctionOperationDefinition_TypeProperty_ReturnsProcessFunction()
    {
        var op = new ProcessFunctionOperationDefinition();

        Assert.That(op.Type, Is.EqualTo("processFunction"));
    }

    [Test]
    public void ProcessFunctionOperationDefinition_SetAllProperties_ReturnsValues()
    {
        var parameters = new Dictionary<string, object> { { "timeout", 5000 } };
        var stateKeys = new List<string> { "state1", "state2" };
        var timerNames = new List<string> { "timer1" };
        var properties = new Dictionary<string, string> { { "key", "value" } };

        var op = new ProcessFunctionOperationDefinition
        {
            ProcessType = "authTokenManager",
            Parameters = parameters,
            StateKeys = stateKeys,
            TimerNames = timerNames,
            Properties = properties
        };

        Assert.That(op.ProcessType, Is.EqualTo("authTokenManager"));
        Assert.That(op.Parameters, Is.EqualTo(parameters));
        Assert.That(op.StateKeys, Is.EqualTo(stateKeys));
        Assert.That(op.TimerNames, Is.EqualTo(timerNames));
        Assert.That(op.Properties, Is.EqualTo(properties));
    }

    #endregion

    #region StateOperationDefinition Tests

    [Test]
    public void StateOperationDefinition_TypeProperty_ReturnsState()
    {
        var op = new StateOperationDefinition();

        Assert.That(op.Type, Is.EqualTo("state"));
    }

    [Test]
    public void StateOperationDefinition_SetAllProperties_ReturnsValues()
    {
        var properties = new Dictionary<string, string> { { "key", "value" } };

        var op = new StateOperationDefinition
        {
            StateType = "list",
            StateKey = "myState",
            ValueType = "integer",
            TtlMs = 60000,
            DefaultValue = "0",
            Properties = properties
        };

        Assert.That(op.StateType, Is.EqualTo("list"));
        Assert.That(op.StateKey, Is.EqualTo("myState"));
        Assert.That(op.ValueType, Is.EqualTo("integer"));
        Assert.That(op.TtlMs, Is.EqualTo(60000));
        Assert.That(op.DefaultValue, Is.EqualTo("0"));
        Assert.That(op.Properties, Is.EqualTo(properties));
    }

    [Test]
    public void StateOperationDefinition_DefaultStateType_IsValue()
    {
        var op = new StateOperationDefinition();

        Assert.That(op.StateType, Is.EqualTo("value"));
    }

    #endregion

    #region TimerOperationDefinition Tests

    [Test]
    public void TimerOperationDefinition_TypeProperty_ReturnsTimer()
    {
        var op = new TimerOperationDefinition();

        Assert.That(op.Type, Is.EqualTo("timer"));
    }

    [Test]
    public void TimerOperationDefinition_SetAllProperties_ReturnsValues()
    {
        var parameters = new Dictionary<string, object> { { "action", "cleanup" } };

        var op = new TimerOperationDefinition
        {
            TimerType = "event",
            DelayMs = 30000,
            TimerName = "cleanupTimer",
            Action = "cleanup",
            Parameters = parameters
        };

        Assert.That(op.TimerType, Is.EqualTo("event"));
        Assert.That(op.DelayMs, Is.EqualTo(30000));
        Assert.That(op.TimerName, Is.EqualTo("cleanupTimer"));
        Assert.That(op.Action, Is.EqualTo("cleanup"));
        Assert.That(op.Parameters, Is.EqualTo(parameters));
    }

    [Test]
    public void TimerOperationDefinition_DefaultTimerType_IsProcessing()
    {
        var op = new TimerOperationDefinition();

        Assert.That(op.TimerType, Is.EqualTo("processing"));
    }

    #endregion

    #region RetryOperationDefinition Tests

    [Test]
    public void RetryOperationDefinition_TypeProperty_ReturnsRetry()
    {
        var op = new RetryOperationDefinition();

        Assert.That(op.Type, Is.EqualTo("retry"));
    }

    [Test]
    public void RetryOperationDefinition_SetAllProperties_ReturnsValues()
    {
        var delays = new List<long> { 1000, 2000, 4000 };
        var properties = new Dictionary<string, string> { { "key", "value" } };

        var op = new RetryOperationDefinition
        {
            MaxRetries = 3,
            DelayMs = delays,
            RetryCondition = "status == 500",
            DeadLetterTopic = "dlq-topic",
            StateKey = "custom_retry_state",
            Properties = properties
        };

        Assert.That(op.MaxRetries, Is.EqualTo(3));
        Assert.That(op.DelayMs, Is.EqualTo(delays));
        Assert.That(op.RetryCondition, Is.EqualTo("status == 500"));
        Assert.That(op.DeadLetterTopic, Is.EqualTo("dlq-topic"));
        Assert.That(op.StateKey, Is.EqualTo("custom_retry_state"));
        Assert.That(op.Properties, Is.EqualTo(properties));
    }

    [Test]
    public void RetryOperationDefinition_DefaultMaxRetries_Is5()
    {
        var op = new RetryOperationDefinition();

        Assert.That(op.MaxRetries, Is.EqualTo(5));
    }

    [Test]
    public void RetryOperationDefinition_DefaultDelays_AreSet()
    {
        var op = new RetryOperationDefinition();

        Assert.That(op.DelayMs, Is.Not.Null);
        Assert.That(op.DelayMs, Has.Count.EqualTo(5));
        Assert.That(op.DelayMs[0], Is.EqualTo(300000)); // 5min
        Assert.That(op.DelayMs[4], Is.EqualTo(86400000)); // 1day
    }

    [Test]
    public void RetryOperationDefinition_DefaultStateKey_IsSet()
    {
        var op = new RetryOperationDefinition();

        Assert.That(op.StateKey, Is.EqualTo("retry_state"));
    }

    #endregion

    #region SideOutputOperationDefinition Tests

    [Test]
    public void SideOutputOperationDefinition_TypeProperty_ReturnsSideOutput()
    {
        var op = new SideOutputOperationDefinition();

        Assert.That(op.Type, Is.EqualTo("sideOutput"));
    }

    [Test]
    public void SideOutputOperationDefinition_SetAllProperties_ReturnsValues()
    {
        var sink = new KafkaSinkDefinition { Topic = "error-topic" };
        var properties = new Dictionary<string, string> { { "key", "value" } };

        var op = new SideOutputOperationDefinition
        {
            OutputTag = "errors",
            Condition = "status == 'ERROR'",
            SideOutputSink = sink,
            Properties = properties
        };

        Assert.That(op.OutputTag, Is.EqualTo("errors"));
        Assert.That(op.Condition, Is.EqualTo("status == 'ERROR'"));
        Assert.That(op.SideOutputSink, Is.EqualTo(sink));
        Assert.That(op.Properties, Is.EqualTo(properties));
    }

    #endregion

    #region MapOperationDefinition Tests

    [Test]
    public void MapOperationDefinition_TypeProperty_ReturnsMap()
    {
        var op = new MapOperationDefinition();

        Assert.That(op.Type, Is.EqualTo("map"));
    }

    [Test]
    public void MapOperationDefinition_SetExpression_ReturnsValue()
    {
        var op = new MapOperationDefinition
        {
            Expression = "x => x.ToUpper()"
        };

        Assert.That(op.Expression, Is.EqualTo("x => x.ToUpper()"));
    }

    [Test]
    public void MapOperationDefinition_OutputType_SupportsNull()
    {
        var op = new MapOperationDefinition
        {
            Expression = "x => x.ToUpper()",
            OutputType = null
        };

        Assert.That(op.OutputType, Is.Null);
    }

    [Test]
    public void MapOperationDefinition_OutputType_CanBeSet()
    {
        var op = new MapOperationDefinition
        {
            Expression = "x => x.ToUpper()",
            OutputType = "String"
        };

        Assert.That(op.OutputType, Is.EqualTo("String"));
    }

    #endregion

    #region FilterOperationDefinition Tests

    [Test]
    public void FilterOperationDefinition_TypeProperty_ReturnsFilter()
    {
        var op = new FilterOperationDefinition();

        Assert.That(op.Type, Is.EqualTo("filter"));
    }

    [Test]
    public void FilterOperationDefinition_SetExpression_ReturnsValue()
    {
        var op = new FilterOperationDefinition
        {
            Expression = "x => x.Length > 5"
        };

        Assert.That(op.Expression, Is.EqualTo("x => x.Length > 5"));
    }

    [Test]
    public void FilterOperationDefinition_DefaultExpression_IsEmpty()
    {
        var op = new FilterOperationDefinition();

        Assert.That(op.Expression, Is.EqualTo(string.Empty));
    }

    #endregion

    #region WindowOperationDefinition Tests

    [Test]
    public void WindowOperationDefinition_Slide_SupportsNull()
    {
        var op = new WindowOperationDefinition
        {
            WindowType = "TUMBLING",
            Size = 60,
            Slide = null
        };

        Assert.That(op.Slide, Is.Null);
    }

    [Test]
    public void WindowOperationDefinition_Slide_CanBeSet()
    {
        var op = new WindowOperationDefinition
        {
            WindowType = "SLIDING",
            Size = 60,
            Slide = 30
        };

        Assert.That(op.Slide, Is.EqualTo(30));
    }

    [Test]
    public void WindowOperationDefinition_TimeField_SupportsNull()
    {
        var op = new WindowOperationDefinition
        {
            WindowType = "TUMBLING",
            Size = 60,
            TimeField = null
        };

        Assert.That(op.TimeField, Is.Null);
    }

    [Test]
    public void WindowOperationDefinition_TimeField_CanBeSet()
    {
        var op = new WindowOperationDefinition
        {
            WindowType = "TUMBLING",
            Size = 60,
            TimeField = "timestamp"
        };

        Assert.That(op.TimeField, Is.EqualTo("timestamp"));
    }

    [Test]
    public void WindowOperationDefinition_DefaultTimeUnit_IsMinutes()
    {
        var op = new WindowOperationDefinition();

        Assert.That(op.TimeUnit, Is.EqualTo("MINUTES"));
    }

    [Test]
    public void WindowOperationDefinition_TypeProperty_ReturnsWindow()
    {
        var op = new WindowOperationDefinition();

        Assert.That(op.Type, Is.EqualTo("window"));
    }

    [Test]
    public void WindowOperationDefinition_SetAllProperties_ReturnsValues()
    {
        var op = new WindowOperationDefinition
        {
            WindowType = "SLIDING",
            Size = 60,
            TimeUnit = "SECONDS",
            Slide = 30,
            TimeField = "eventTime"
        };

        Assert.That(op.WindowType, Is.EqualTo("SLIDING"));
        Assert.That(op.Size, Is.EqualTo(60));
        Assert.That(op.TimeUnit, Is.EqualTo("SECONDS"));
        Assert.That(op.Slide, Is.EqualTo(30));
        Assert.That(op.TimeField, Is.EqualTo("eventTime"));
    }

    #endregion
}
