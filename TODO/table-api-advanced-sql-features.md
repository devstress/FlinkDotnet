# TODO: Table API and Advanced SQL Features (Flink 2.1.0)

**Status**: Partially Implemented - Medium Priority
**Created**: 2025-10-27
**Apache Flink Version**: 2.1.0
**Related WI**: WI5_flink-21-feature-coverage-audit.md

## Overview

Apache Flink 2.1.0 has extensive Table API and SQL capabilities for declarative stream processing. FlinkDotNet currently supports **basic SQL execution** via TableEnvironment but lacks comprehensive Table API and advanced SQL features.

## Current FlinkDotNet SQL Support

✅ **What Works**:
- Basic SQL execution via `TableEnvironment.ExecuteSql()`
- Table creation DDL (CREATE TABLE)
- SQL Gateway integration
- Kafka connector in SQL
- Simple transformations (SELECT, INSERT, WHERE, GROUP BY)

❌ **What's Missing**: Advanced Table API features and modern SQL capabilities.

## Missing Features

### 1. Process Table Functions (PTFs)

**What it is**: The most powerful user-defined function type in Flink, providing direct access to managed state, event-time, timers, and table changelogs.

**Flink 2.1.0 Capabilities**:
```java
// Java example of PTF
public class SessionAnalyzer extends ProcessTableFunction<Row, Row> {
    
    @StateHint("session_state")
    private ValueState<SessionData> sessionState;
    
    public void eval(Context ctx, Row input) {
        // Access to:
        // - Managed state
        // - Event-time and watermarks
        // - Timers for time-based processing
        // - Table changelogs (INSERT, UPDATE, DELETE)
        
        long eventTime = ctx.timestamp();
        SessionData session = sessionState.value();
        
        // Complex stateful logic
        if (session == null) {
            session = new SessionData(input);
            sessionState.update(session);
            ctx.registerEventTimeTimer(eventTime + Duration.ofMinutes(30).toMillis());
        }
        
        ctx.collect(Row.of(session.getId(), session.getDuration()));
    }
    
    @Override
    public void onTimer(Context ctx, OnTimerContext timerCtx) {
        // Timer callback for session timeout
        sessionState.clear();
    }
}

// Use in SQL
tableEnv.createTemporaryFunction("analyze_session", SessionAnalyzer.class);

table.addColumns("analyze_session(user_id, action, timestamp) AS (session_id, duration)");
```

**FlinkDotNet Gap**: No PTF support - cannot define stateful table functions with timer access.

**What Would Be Needed**:
```csharp
// Proposed C# API
public class SessionAnalyzer : ProcessTableFunction<Row, Row>
{
    private ValueState<SessionData> sessionState;
    
    protected override void Open(FunctionContext context)
    {
        sessionState = context.GetState(
            new ValueStateDescriptor<SessionData>("session_state"));
    }
    
    public void Eval(Context ctx, Row input)
    {
        long eventTime = ctx.Timestamp();
        var session = sessionState.Value();
        
        if (session == null)
        {
            session = new SessionData(input);
            sessionState.Update(session);
            ctx.RegisterEventTimeTimer(eventTime + TimeSpan.FromMinutes(30).TotalMilliseconds);
        }
        
        ctx.Collect(Row.Of(session.Id, session.Duration));
    }
    
    public override void OnTimer(Context ctx, OnTimerContext timerCtx)
    {
        sessionState.Clear();
    }
}

// Register and use
tEnv.CreateTemporaryFunction("analyze_session", typeof(SessionAnalyzer));
```

### 2. VARIANT Data Type Support

**What it is**: Efficient storage and processing of semi-structured JSON data with dynamic schemas.

**Flink 2.1.0 Capabilities**:
```sql
-- Create table with VARIANT column
CREATE TABLE events (
  event_id STRING,
  event_data VARIANT,  -- Can hold arbitrary JSON
  event_time TIMESTAMP(3)
) WITH (
  'connector' = 'kafka',
  'format' = 'json'
);

-- Parse JSON string to VARIANT
SELECT 
  event_id,
  PARSE_JSON(json_string) AS event_data,
  TRY_PARSE_JSON(possibly_invalid_json) AS safe_data
FROM raw_events;

-- Query nested fields in VARIANT
SELECT 
  event_id,
  event_data['user']['name'] AS user_name,
  event_data['metadata']['tags'][0] AS first_tag
FROM events;
```

**FlinkDotNet Gap**: No VARIANT type, no JSON parsing functions.

**What Would Be Needed**:
```csharp
// Proposed C# API
var tableEnv = env.GetTableEnvironment();

// Define table with VARIANT column
tableEnv.ExecuteSql(@"
  CREATE TABLE events (
    event_id STRING,
    event_data VARIANT,
    event_time TIMESTAMP(3)
  ) WITH ('connector' = 'kafka', 'format' = 'json')
");

// Or programmatically
var schema = Schema.NewBuilder()
    .Column("event_id", DataTypes.String())
    .Column("event_data", DataTypes.Variant())
    .Column("event_time", DataTypes.Timestamp(3))
    .Build();

// Work with VARIANT data
var result = tableEnv.SqlQuery(@"
  SELECT 
    event_id,
    PARSE_JSON(json_string) AS data,
    data['user']['name'] AS user_name
  FROM raw_events
");
```

### 3. Structured Type API Enhancements

**What it is**: Define user-defined objects directly in CREATE TABLE DDL.

**Flink 2.1.0 Capabilities**:
```sql
-- Define structured type
CREATE TYPE Address AS ROW(
  street STRING,
  city STRING,
  zip STRING,
  country STRING
);

CREATE TYPE Person AS ROW(
  id BIGINT,
  name STRING,
  address Address,
  tags ARRAY<STRING>
);

-- Use in table definition
CREATE TABLE users (
  user_id STRING,
  profile Person,
  registration_time TIMESTAMP(3)
) WITH (...);

-- Query structured fields
SELECT 
  user_id,
  profile.name,
  profile.address.city,
  profile.tags[1]
FROM users;
```

**FlinkDotNet Gap**: No structured type definition support beyond basic DDL parsing.

**What Would Be Needed**:
```csharp
// Proposed C# API for structured types
var addressType = StructuredType.NewBuilder("Address")
    .Field("street", DataTypes.String())
    .Field("city", DataTypes.String())
    .Field("zip", DataTypes.String())
    .Field("country", DataTypes.String())
    .Build();

var personType = StructuredType.NewBuilder("Person")
    .Field("id", DataTypes.BigInt())
    .Field("name", DataTypes.String())
    .Field("address", addressType)
    .Field("tags", DataTypes.Array(DataTypes.String()))
    .Build();

// Register types
tEnv.CreateType("Address", addressType);
tEnv.CreateType("Person", personType);

// Use in schema
var schema = Schema.NewBuilder()
    .Column("user_id", DataTypes.String())
    .Column("profile", personType)
    .Column("registration_time", DataTypes.Timestamp(3))
    .Build();
```

### 4. Native Table API Programming

**What it is**: Programmatic table transformations without SQL strings.

**Flink 2.1.0 Capabilities (Java)**:
```java
TableEnvironment tEnv = ...;

// Programmatic table operations
Table orders = tEnv.from("orders");

Table result = orders
    .where($("amount").isGreater(100))
    .groupBy($("customer_id"))
    .select(
        $("customer_id"),
        $("amount").sum().as("total_amount"),
        $("order_id").count().as("order_count")
    )
    .filter($("total_amount").isGreater(1000));

result.executeInsert("high_value_customers");
```

**FlinkDotNet Gap**: No fluent Table API - must use SQL strings.

**What Would Be Needed**:
```csharp
// Proposed C# Table API
var tEnv = env.GetTableEnvironment();

var orders = tEnv.From("orders");

var result = orders
    .Where(t => t.Get("amount").AsInt() > 100)
    .GroupBy(t => t.Get("customer_id"))
    .Select(t => new
    {
        CustomerId = t.Get("customer_id"),
        TotalAmount = t.Get("amount").Sum(),
        OrderCount = t.Get("order_id").Count()
    })
    .Filter(t => t.TotalAmount > 1000);

result.ExecuteInsert("high_value_customers");
```

### 5. Advanced Window Table-Valued Functions

**What it is**: Modern SQL window aggregations using table-valued functions.

**Flink 2.1.0 Capabilities**:
```sql
-- Tumbling windows
SELECT 
  window_start,
  window_end,
  COUNT(*) AS event_count,
  SUM(amount) AS total_amount
FROM TABLE(
  TUMBLE(TABLE events, DESCRIPTOR(event_time), INTERVAL '1' HOUR)
)
GROUP BY window_start, window_end;

-- Hop (sliding) windows
SELECT window_time, user_id, COUNT(*) as action_count
FROM TABLE(
  HOP(TABLE user_actions, DESCRIPTOR(action_time), INTERVAL '5' MINUTE, INTERVAL '1' HOUR)
)
GROUP BY window_time, user_id;

-- Cumulative windows
SELECT window_time, product_id, SUM(quantity) AS cumulative_sales
FROM TABLE(
  CUMULATE(TABLE sales, DESCRIPTOR(sale_time), INTERVAL '1' HOUR, INTERVAL '1' DAY)
)
GROUP BY window_time, product_id;
```

**FlinkDotNet Gap**: Basic window support in DataStream API but not modern Table API window TVFs.

### 6. DeltaJoin Configuration

**What it is**: Optimized streaming join operator that reduces state overhead.

**Flink 2.1.0 Capabilities**:
- Enabled by default in Flink 2.1.0
- Reduces state size for joins by tracking deltas
- Improves performance and stability

**FlinkDotNet Gap**: 
- May work transparently if enabled in Flink runtime
- No explicit C# API to enable/disable or configure

**What Might Be Needed**:
```csharp
// Proposed API for join optimization hints
var joined = stream1
    .Join(stream2)
    .Where(s1 => s1.Key)
    .EqualTo(s2 => s2.Key)
    .Window(TumblingEventTimeWindows.Of(Time.Minutes(5)))
    .WithJoinStrategy(JoinStrategy.DeltaJoin)  // Explicit control
    .Apply(new JoinFunction());
```

## Implementation Priority

### High Priority (P0)
1. **VARIANT Data Type** - Critical for modern JSON/semi-structured data
2. **PARSE_JSON Functions** - Enable JSON processing
3. **Native Table API** - Reduce reliance on SQL strings

### Medium Priority (P1)
4. **Structured Type API** - Better type safety
5. **Window TVFs** - Modern SQL window syntax
6. **Process Table Functions** - Advanced stateful UDFs

### Lower Priority (P2)
7. **DeltaJoin Hints** - May work by default
8. **Advanced Table Operations** - Over windows, pattern matching

## Use Cases

### 1. Semi-Structured Data Processing
```csharp
// Process dynamic JSON events
var events = tEnv.From("raw_events");
var parsed = events.Select(
    e => new {
        EventId = e.Get("event_id"),
        UserName = e.Get("event_data").ParseJson()["user"]["name"],
        Tags = e.Get("event_data").ParseJson()["tags"].AsArray()
    });
```

### 2. Declarative Aggregations
```csharp
// Complex aggregations without SQL strings
var hourlyStats = tEnv.From("events")
    .Window(Tumble.Over(Time.Hours(1)).On("event_time").As("w"))
    .GroupBy("w, event_type")
    .Select(t => new {
        WindowStart = t.Get("w").Start(),
        EventType = t.Get("event_type"),
        Count = t.Count(),
        AvgDuration = t.Get("duration").Avg()
    });
```

## Estimated Implementation Effort

### Phase 1: VARIANT Type (3-4 weeks)
- VARIANT data type mapping
- PARSE_JSON/TRY_PARSE_JSON functions
- JSON path navigation
- Integration with Flink's VARIANT implementation

### Phase 2: Native Table API (4-6 weeks)
- Fluent table transformation API
- Expression API for columns/functions
- Table registration and catalog integration
- Window API

### Phase 3: Structured Types (2-3 weeks)
- Type definition API
- Schema builder enhancements
- DDL generation

### Phase 4: Process Table Functions (3-4 weeks)
- PTF base class and context
- State and timer integration
- Function registration
- Changelog support

**Total Estimated Effort**: 12-17 weeks for complete Table API parity

## References

- [Flink Table API Documentation](https://nightlies.apache.org/flink/flink-docs-stable/docs/dev/table/overview/)
- [Flink 2.1.0 VARIANT Type](https://nightlies.apache.org/flink/flink-docs-master/release-notes/flink-2.1/)
- [Process Table Functions](https://nightlies.apache.org/flink/flink-docs-stable/docs/dev/table/functions/udfs/)

## When to Implement

Implement when:
1. ✅ Basic SQL support is stable
2. ✅ Users need complex table transformations
3. Need better type safety than SQL strings
4. Semi-structured data (JSON) use cases emerge
5. Advanced stateful processing required

**Current Status**: Basic SQL works. Table API would significantly improve developer experience and enable modern Flink 2.1.0 use cases.
