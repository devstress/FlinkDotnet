using System.Text.Json;
using Flink.JobBuilder.Models;
using FlinkDotNet.DataStream;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

/// <summary>
/// Consolidated integration tests for Table API &amp; SQL features (Flink 2.1+).
/// Tests validate VARIANT type, JSON functions, and Native Table API.
/// Maximum 5 tests per Flink version as per project guidelines.
/// </summary>
[TestFixture]
[Category("table-api")]
public class TableApiTests
{
    #region Test 1: IR Schema for Table Operations and VARIANT Support

    /// <summary>
    /// Test 1: Validates IR schema for Table API operations including:
    /// - TableSourceDefinition with VARIANT schema
    /// - ParseJsonOperationDefinition for JSON parsing
    /// - TableOperationDefinition for transformations
    /// - JSON round-trip serialization
    /// </summary>
    [Test]
    public void Test1_IRSchema_TablesAndVariantSupport()
    {
        // Arrange - Create job with Table source containing VARIANT columns
        JobDefinition jobDef = new()
        {
            Source = new TableSourceDefinition
            {
                TableName = "events",
                Schema = new Dictionary<string, string>
                {
                    { "event_id", "STRING" },
                    { "event_data", "VARIANT" },  // Semi-structured JSON data
                    { "event_time", "TIMESTAMP(3)" }
                },
                Properties = new Dictionary<string, string>
                {
                    { "connector", "kafka" },
                    { "format", "json" }
                }
            },
            Operations = new List<IOperationDefinition>
            {
                // Parse JSON from string field
                new ParseJsonOperationDefinition
                {
                    FunctionType = "TRY_PARSE_JSON",
                    SourceField = "raw_json",
                    TargetField = "parsed_data",
                    JsonPath = "$.user.name"
                },
                // Table transformation: select columns
                new TableOperationDefinition
                {
                    OperationType = "select",
                    Columns = ["event_id", "parsed_data", "event_time"]
                },
                // Table transformation: where filter
                new TableOperationDefinition
                {
                    OperationType = "where",
                    Condition = "event_id IS NOT NULL"
                }
            },
            Metadata = new JobMetadata
            {
                JobName = "Table API Test",
                Version = "1.0"
            }
        };

        // Act - Serialize and deserialize
        string json = JsonSerializer.Serialize(jobDef, new JsonSerializerOptions { WriteIndented = true });
        JobDefinition? deserialized = JsonSerializer.Deserialize<JobDefinition>(json);

        // Assert - Source structure
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.Source, Is.InstanceOf<TableSourceDefinition>());

        TableSourceDefinition? tableSource = deserialized.Source as TableSourceDefinition;
        Assert.That(tableSource, Is.Not.Null);
        Assert.That(tableSource!.Type, Is.EqualTo("table"));
        Assert.That(tableSource.TableName, Is.EqualTo("events"));
        Assert.That(tableSource.Schema["event_data"], Is.EqualTo("VARIANT"));

        // Assert - Operations
        Assert.That(deserialized.Operations, Has.Count.EqualTo(3));

        ParseJsonOperationDefinition? parseOp = deserialized.Operations[0] as ParseJsonOperationDefinition;
        Assert.That(parseOp, Is.Not.Null);
        Assert.That(parseOp!.Type, Is.EqualTo("parseJson"));
        Assert.That(parseOp.FunctionType, Is.EqualTo("TRY_PARSE_JSON"));
        Assert.That(parseOp.JsonPath, Is.EqualTo("$.user.name"));

        TableOperationDefinition? selectOp = deserialized.Operations[1] as TableOperationDefinition;
        Assert.That(selectOp, Is.Not.Null);
        Assert.That(selectOp!.OperationType, Is.EqualTo("select"));
        Assert.That(selectOp.Columns, Has.Count.EqualTo(3));

        TableOperationDefinition? whereOp = deserialized.Operations[2] as TableOperationDefinition;
        Assert.That(whereOp, Is.Not.Null);
        Assert.That(whereOp!.OperationType, Is.EqualTo("where"));
    }

    #endregion

    #region Test 2: Table API Fluent Interface

    /// <summary>
    /// Test 2: Validates Native Table API fluent interface including:
    /// - Table creation from DataStream
    /// - Select, Where, GroupBy operations
    /// - Operation chaining
    /// - SQL generation from API calls
    /// </summary>
    [Test]
    public void Test2_TableAPI_FluentInterface()
    {
        // Arrange
        StreamExecutionEnvironment env = StreamExecutionEnvironment.GetExecutionEnvironment();
        DataStream<int> stream = env.FromCollection([1, 2, 3, 4, 5]);

        // Act - Create table and apply transformations
        Table table = stream.ToTable("my_table", new Dictionary<string, string>
        {
            { "id", "INT" },
            { "value", "STRING" },
            { "timestamp", "TIMESTAMP(3)" }
        });

        // Assert - Table creation
        Assert.That(table, Is.Not.Null);
        Assert.That(table.TableName, Is.EqualTo("my_table"));
        Assert.That(table.Definition.Schema, Has.Count.EqualTo(3));

        // Act - Apply select operation
        Table selectedTable = table.Select("id", "value");
        Assert.That(selectedTable.Operations, Has.Count.EqualTo(1));
        TableOperationDefinition? selectOp = selectedTable.Operations[0] as TableOperationDefinition;
        Assert.That(selectOp?.OperationType, Is.EqualTo("select"));
        Assert.That(selectOp?.Columns, Has.Count.EqualTo(2));

        // Act - Apply where filter
        Table filteredTable = selectedTable.Where("value IS NOT NULL");
        Assert.That(filteredTable.Operations, Has.Count.EqualTo(2));
        TableOperationDefinition? whereOp = filteredTable.Operations[1] as TableOperationDefinition;
        Assert.That(whereOp?.OperationType, Is.EqualTo("where"));

        // Act - Generate SQL
        string sql = filteredTable.ToSql();
        Assert.That(sql, Does.Contain("SELECT"));
        Assert.That(sql, Does.Contain("id, value"));
        Assert.That(sql, Does.Contain("FROM my_table"));
        Assert.That(sql, Does.Contain("WHERE value IS NOT NULL"));
    }

    #endregion

    #region Test 3: VARIANT Type and JSON Functions

    /// <summary>
    /// Test 3: Validates VARIANT data type and JSON parsing functions including:
    /// - PARSE_JSON vs TRY_PARSE_JSON behavior
    /// - JSON path navigation
    /// - Multiple JSON column additions
    /// - SQL generation with VARIANT operations
    /// </summary>
    [Test]
    public void Test3_VariantType_JsonFunctions()
    {
        // Arrange
        Table table = new(new TableSourceDefinition
        {
            TableName = "raw_events",
            Schema = new Dictionary<string, string>
            {
                { "event_id", "STRING" },
                { "raw_json", "STRING" }
            }
        });

        // Act - Add JSON column with TRY_PARSE_JSON (lenient)
        Table table1 = table.AddJsonColumn("raw_json", "user_name", "$.user.name", strict: false);

        // Assert - TRY_PARSE_JSON operation
        Assert.That(table1.Operations, Has.Count.EqualTo(1));
        ParseJsonOperationDefinition? parseOp1 = table1.Operations[0] as ParseJsonOperationDefinition;
        Assert.That(parseOp1?.FunctionType, Is.EqualTo("TRY_PARSE_JSON"));
        Assert.That(parseOp1?.SourceField, Is.EqualTo("raw_json"));
        Assert.That(parseOp1?.TargetField, Is.EqualTo("user_name"));
        Assert.That(parseOp1?.JsonPath, Is.EqualTo("$.user.name"));

        // Act - Add another JSON column with PARSE_JSON (strict)
        Table table2 = table1.AddJsonColumn("raw_json", "user_id", "$.user.id", strict: true);

        // Assert - PARSE_JSON operation
        Assert.That(table2.Operations, Has.Count.EqualTo(2));
        ParseJsonOperationDefinition? parseOp2 = table2.Operations[1] as ParseJsonOperationDefinition;
        Assert.That(parseOp2?.FunctionType, Is.EqualTo("PARSE_JSON"));

        // Act - Generate SQL
        string sql = table2.ToSql();

        // Assert - SQL contains JSON functions
        Assert.That(sql, Does.Contain("TRY_PARSE_JSON"));
        Assert.That(sql, Does.Contain("PARSE_JSON"));
        Assert.That(sql, Does.Contain("::VARIANT"));
        Assert.That(sql, Does.Contain("$.user.name"));
        Assert.That(sql, Does.Contain("$.user.id"));
    }

    #endregion

    #region Test 4: Table GroupBy and Aggregations

    /// <summary>
    /// Test 4: Validates Table API grouping and aggregations including:
    /// - GroupBy operation
    /// - Multiple aggregation functions
    /// - Grouped table operations
    /// - Aggregation SQL generation
    /// </summary>
    [Test]
    public void Test4_TableAPI_GroupByAndAggregations()
    {
        // Arrange
        Table table = new("orders");

        // Act - Group by customer_id and aggregate
        GroupedTable groupedTable = table.GroupBy("customer_id");
        Table aggregatedTable = groupedTable.Aggregate(
            "COUNT(*) AS order_count",
            "SUM(amount) AS total_amount",
            "AVG(amount) AS avg_amount"
        );

        // Assert - Operations structure
        Assert.That(aggregatedTable.Operations, Has.Count.EqualTo(1));
        TableOperationDefinition? aggOp = aggregatedTable.Operations[0] as TableOperationDefinition;
        Assert.That(aggOp, Is.Not.Null);
        Assert.That(aggOp!.OperationType, Is.EqualTo("aggregate"));
        Assert.That(aggOp.GroupByKeys, Has.Count.EqualTo(1));
        Assert.That(aggOp.GroupByKeys[0], Is.EqualTo("customer_id"));
        Assert.That(aggOp.Aggregations, Has.Count.EqualTo(3));
        Assert.That(aggOp.Aggregations[0], Is.EqualTo("COUNT(*) AS order_count"));
        Assert.That(aggOp.Aggregations[1], Is.EqualTo("SUM(amount) AS total_amount"));
        Assert.That(aggOp.Aggregations[2], Is.EqualTo("AVG(amount) AS avg_amount"));

        // Act - Use GroupedTable.Select shorthand
        GroupedTable groupedTable2 = table.GroupBy("product_id", "category");
        Table selectedTable = groupedTable2.Select(
            "COUNT(*) AS count",
            "MAX(price) AS max_price"
        );

        // Assert - Multiple group keys
        Assert.That(selectedTable.Operations, Has.Count.EqualTo(1));
        TableOperationDefinition? selectOp = selectedTable.Operations[0] as TableOperationDefinition;
        Assert.That(selectOp?.GroupByKeys, Has.Count.EqualTo(2));
        Assert.That(selectOp?.Aggregations, Has.Count.EqualTo(2));
    }

    #endregion

    #region Test 5: Complex Table API Workflow with VARIANT and PTFs

    /// <summary>
    /// Test 5: Validates complex Table API workflow combining all features:
    /// - VARIANT columns in schema
    /// - JSON parsing and path navigation
    /// - Select, Where, GroupBy operations
    /// - Process Table Functions (PTFs) with state and timers
    /// - Complete SQL generation
    /// - End-to-end transformation pipeline
    /// </summary>
    [Test]
    public void Test5_ComplexWorkflow_VariantTableAPIAndPTFs()
    {
        // Part A: Table API with VARIANT (existing functionality)
        Table table = new(new TableSourceDefinition
        {
            TableName = "user_events",
            Schema = new Dictionary<string, string>
            {
                { "user_id", "STRING" },
                { "event_data", "VARIANT" },
                { "event_timestamp", "TIMESTAMP(3)" }
            },
            Properties = new Dictionary<string, string>
            {
                { "connector", "kafka" },
                { "topic", "user-events" },
                { "format", "json" }
            }
        });

        Table result = table
            .AddJsonColumn("event_data", "event_type", "$.type", strict: false)
            .AddJsonColumn("event_data", "event_value", "$.value", strict: false)
            .Select("user_id", "event_type", "event_value", "event_timestamp")
            .Where("event_type IS NOT NULL");

        Assert.That(result.Operations, Has.Count.EqualTo(4));

        // Part B: Process Table Function (PTF) IR Schema
        JobDefinition ptfJobDef = new()
        {
            Source = new TableSourceDefinition
            {
                TableName = "sessions",
                Schema = new Dictionary<string, string>
                {
                    { "user_id", "STRING" },
                    { "action", "STRING" },
                    { "event_time", "TIMESTAMP(3)" }
                }
            },
            Operations = new List<IOperationDefinition>
            {
                new ProcessTableFunctionDefinition
                {
                    FunctionName = "analyze_session",
                    ClassName = "SessionAnalyzer",
                    InputColumns = ["user_id", "action", "event_time"],
                    OutputColumns = ["session_id", "session_duration", "action_count"],
                    StateDescriptors = new Dictionary<string, string>
                    {
                        { "session_state", "ValueState<SessionData>" },
                        { "action_list", "ListState<String>" }
                    },
                    UsesEventTimeTimers = true,
                    UsesProcessingTimeTimers = false,
                    Properties = new Dictionary<string, string>
                    {
                        { "timeout_minutes", "30" },
                        { "min_actions", "2" }
                    }
                }
            },
            Metadata = new JobMetadata
            {
                JobName = "PTF Test",
                Version = "1.0"
            }
        };

        // Assert PTF IR serialization
        string ptfJson = JsonSerializer.Serialize(ptfJobDef, new JsonSerializerOptions { WriteIndented = true });
        JobDefinition? ptfDeserialized = JsonSerializer.Deserialize<JobDefinition>(ptfJson);

        Assert.That(ptfDeserialized, Is.Not.Null);
        Assert.That(ptfDeserialized!.Operations, Has.Count.EqualTo(1));

        ProcessTableFunctionDefinition? ptfOp = ptfDeserialized.Operations[0] as ProcessTableFunctionDefinition;
        Assert.That(ptfOp, Is.Not.Null);
        Assert.That(ptfOp!.Type, Is.EqualTo("processTableFunction"));
        Assert.That(ptfOp.FunctionName, Is.EqualTo("analyze_session"));
        Assert.That(ptfOp.ClassName, Is.EqualTo("SessionAnalyzer"));
        Assert.That(ptfOp.InputColumns, Has.Count.EqualTo(3));
        Assert.That(ptfOp.OutputColumns, Has.Count.EqualTo(3));
        Assert.That(ptfOp.StateDescriptors, Has.Count.EqualTo(2));
        Assert.That(ptfOp.UsesEventTimeTimers, Is.True);
        Assert.That(ptfOp.Properties["timeout_minutes"], Is.EqualTo("30"));

        // Part C: PTF C# API - Stateful Session Processing
        TestSessionAnalyzer sessionAnalyzer = new();
        FunctionContext functionContext = new();
        sessionAnalyzer.TestOpen(functionContext);

        ProcessingContext processingContext = new()
        {
            Timestamp = 1000L,
            CurrentWatermark = 900L
        };

        // Simulate processing events
        TestEvent event1 = new()
        {
            UserId = "user1",
            Action = "click",
            EventTime = 1000L
        };
        sessionAnalyzer.Eval(processingContext, event1);

        // Assert output collected
        IReadOnlyList<object> output1 = processingContext.GetOutput();
        Assert.That(output1, Has.Count.EqualTo(1));
        Assert.That(output1[0], Is.InstanceOf<TestSessionOutput>());
        TestSessionOutput session1 = (TestSessionOutput) output1[0];
        Assert.That(session1.SessionId, Is.Not.Null);
        Assert.That(session1.ActionCount, Is.EqualTo(1));

        // Assert event-time timer registered
        IReadOnlyList<long> eventTimers = processingContext.GetEventTimeTimers();
        Assert.That(eventTimers, Has.Count.EqualTo(1));
        Assert.That(eventTimers[0], Is.EqualTo(1000L + 30 * 60 * 1000)); // 30 minutes timeout

        // Process another event in same session
        processingContext.ClearOutput();
        TestEvent event2 = new()
        {
            UserId = "user1",
            Action = "purchase",
            EventTime = 2000L
        };
        processingContext.Timestamp = 2000L;
        sessionAnalyzer.Eval(processingContext, event2);

        IReadOnlyList<object> output2 = processingContext.GetOutput();
        Assert.That(output2, Has.Count.EqualTo(1));
        TestSessionOutput session2 = (TestSessionOutput) output2[0];
        Assert.That(session2.ActionCount, Is.EqualTo(2));

        // Part D: PTF Timer Callback
        OnTimerContext timerContext = new()
        {
            TimerTimestamp = 1000L + 30 * 60 * 1000,
            TimerType = TimerType.EventTime
        };

        processingContext.ClearOutput();
        sessionAnalyzer.OnTimer(processingContext, timerContext);

        // Verify state cleared after timeout
        TestSessionData? stateValue = sessionAnalyzer.GetSessionState();
        Assert.That(stateValue, Is.Null);

        // Part E: Edge cases validation
        // Test: Empty table name should throw
        Assert.Throws<ArgumentException>(() => new Table(""));

        // Test: Invalid JSON path should still create operation
        Table invalidPathTable = table.AddJsonColumn("event_data", "invalid", "$.invalid.path[999]");
        Assert.That(invalidPathTable.Operations, Has.Count.EqualTo(1));

        // Test: Multiple GroupBy keys
        GroupedTable groupedResult = result.GroupBy("user_id", "event_type");
        Table aggregated = groupedResult.Aggregate("COUNT(*) AS event_count");
        Assert.That((aggregated.Operations[4] as TableOperationDefinition)?.GroupByKeys, Has.Count.EqualTo(2));

        // Part F: SQL Generation from Table API
        string sql = result.ToSql();
        Assert.That(sql, Does.Contain("TRY_PARSE_JSON"));
        Assert.That(sql, Does.Contain("::VARIANT"));
        Assert.That(sql, Does.Contain("WHERE event_type IS NOT NULL"));
    }

    #endregion
}

/// <summary>
/// Test implementation of a Process Table Function for session analysis
/// </summary>
public class TestSessionAnalyzer : ProcessTableFunction<TestEvent, TestSessionOutput>
{
    private IPtfValueState<TestSessionData>? _sessionState;

    public void TestOpen(FunctionContext context)
    {
        this.Open(context);
    }

    protected override void Open(FunctionContext context)
    {
        this._sessionState = context.GetState(new ValueStateDescriptor<TestSessionData>("session_state"));
    }

    public override void Eval(ProcessingContext context, TestEvent input)
    {
        TestSessionData? session = this._sessionState?.Value();

        session ??= new TestSessionData
        {
            SessionId = Guid.NewGuid().ToString(),
            UserId = input.UserId,
            StartTime = input.EventTime,
            ActionCount = 0
        };

        session.ActionCount++;
        session.LastActionTime = input.EventTime;

        this._sessionState?.Update(session);

        // Register timeout timer (30 minutes)
        context.RegisterEventTimeTimer(input.EventTime + 30 * 60 * 1000);

        // Emit session output
        context.Collect(new TestSessionOutput
        {
            SessionId = session.SessionId,
            ActionCount = session.ActionCount,
            Duration = input.EventTime - session.StartTime
        });
    }

    public override void OnTimer(ProcessingContext context, OnTimerContext timerContext)
    {
        // Session timed out - clear state
        this._sessionState?.Clear();
    }

    public TestSessionData? GetSessionState()
    {
        return this._sessionState?.Value();
    }
}

/// <summary>
/// Test event for PTF input
/// </summary>
public class TestEvent
{
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public long EventTime
    {
        get; set;
    }
}

/// <summary>
/// Test session data for PTF state
/// </summary>
public class TestSessionData
{
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public long StartTime
    {
        get; set;
    }
    public long LastActionTime
    {
        get; set;
    }
    public int ActionCount
    {
        get; set;
    }
}

/// <summary>
/// Test session output from PTF
/// </summary>
public class TestSessionOutput
{
    public string SessionId { get; set; } = string.Empty;
    public int ActionCount
    {
        get; set;
    }
    public long Duration
    {
        get; set;
    }
}
