# Flink SQL Guide

This guide shows how to run Flink SQL jobs with FlinkDotNet using two different execution modes:
- **TableEnvironment Mode** (default): SQL executed via Java Table API in Flink cluster
- **SQL Gateway Mode**: SQL submitted directly via Flink SQL Gateway REST API

## Execution Modes

### TableEnvironment Mode (Default)

Uses Apache Flink's TableEnvironment to execute SQL statements within a submitted JAR. This is the traditional approach and provides full Flink Table API features.

**Use cases**:
- Complex SQL transformations requiring Table API features
- Jobs requiring custom UDFs or connectors in classpath
- Production streaming jobs with stateful operations

**How it works**:
1. SQL statements are packaged in job definition
2. JAR is submitted to Flink cluster via REST API
3. FlinkJobRunner executes SQL using TableEnvironment.executeSql()
4. Job runs asynchronously in Flink cluster

### SQL Gateway Mode

Uses Flink SQL Gateway REST API to submit SQL statements directly without requiring JAR submission. Ideal for interactive analytics and quick SQL queries.

**Use cases**:
- Interactive SQL queries and ad-hoc analytics
- Simple table-oriented streaming jobs
- Quick prototyping and testing
- BI dashboard integrations

**How it works**:
1. SQL statements are submitted directly to `/v1/statements` endpoint
2. Flink SQL Gateway translates to execution plans
3. No JAR or TableEnvironment required
4. Jobs execute directly in Flink cluster

## Concept

- Set `source` to `{ "type": "sql", "statements": [ ... ] }`.
- Provide DDL for sources/sinks (Kafka, filesystem, etc.).
- Provide DML (`INSERT INTO ... SELECT ...`) to move data.
- No separate DSL sink is required; sinks are defined in SQL.

## Example: Kafka → Kafka (TableEnvironment Mode)

```csharp
using FlinkDotNet.DataStream;

// Create execution environment
var env = Flink.GetExecutionEnvironment();

// Define SQL statements for table creation and transformation
var createInputTable = @"CREATE TABLE input (
       `key` STRING,
       `value` STRING
     ) WITH (
       'connector'='kafka',
       'topic'='input',
       'properties.bootstrap.servers'='kafka:9092',
       'properties.group.id'='flink-sql',
       'scan.startup.mode'='latest-offset',
       'format'='json'
     )";

var createOutputTable = @"CREATE TABLE output (
       `key` STRING,
       `value` STRING
     ) WITH (
       'connector'='kafka',
       'topic'='output',
       'properties.bootstrap.servers'='kafka:9092',
       'format'='json'
     )";

var insertStatement = @"INSERT INTO output SELECT `key`, UPPER(`value`) as `value` FROM input";

// Execute SQL statements via TableEnvironment
var tEnv = env.GetTableEnvironment();
tEnv.ExecuteSql(createInputTable);
tEnv.ExecuteSql(createOutputTable);
tEnv.ExecuteSql(insertStatement);

// Execute the job
await env.ExecuteAsync("kafka-sql-transform");
```

## Example: Kafka → Kafka (SQL Gateway Mode)

```csharp
using FlinkDotNet.DataStream;
using System.Net.Http;
using System.Text;
using System.Text.Json;

// Define SQL statements
var statements = new[]
{
    @"CREATE TABLE input (
       `key` STRING,
       `value` STRING
     ) WITH (
       'connector'='kafka',
       'topic'='input',
       'properties.bootstrap.servers'='kafka:9092',
       'properties.group.id'='flink-sql',
       'scan.startup.mode'='latest-offset',
       'format'='json'
     )",
    @"CREATE TABLE output (
       `key` STRING,
       `value` STRING
     ) WITH (
       'connector'='kafka',
       'topic'='output',
       'properties.bootstrap.servers'='kafka:9092',
       'format'='json'
     )",
    @"INSERT INTO output SELECT `key`, `value` FROM input"
};

// Submit directly to SQL Gateway REST API
using var httpClient = new HttpClient();
var gatewayUrl = "http://flink-sql-gateway:8083/v1/statements";

foreach (var statement in statements)
{
    var payload = JsonSerializer.Serialize(new { statement });
    var content = new StringContent(payload, Encoding.UTF8, "application/json");
    var response = await httpClient.PostAsync(gatewayUrl, content);
    response.EnsureSuccessStatusCode();
}
```

Notes:
- You must ensure the Flink cluster has the relevant SQL connectors on the classpath.
- In LocalTesting, the example assumes Kafka is reachable at `kafka:9092` in the container network.
- SQL Gateway mode requires Flink SQL Gateway to be available in the cluster
- TableEnvironment mode (default) works with any Flink cluster

## Troubleshooting

### TableEnvironment Mode

- If the job starts but no data flows:
  - Verify the Kafka topics exist and connectors are available to the Flink cluster.
  - Check Flink UI for table/connector errors.
  - Ensure JAR includes required SQL connectors

### SQL Gateway Mode

- If SQL Gateway submission fails:
  - Verify Flink SQL Gateway is enabled in your cluster
  - Check that `/v1/statements` endpoint is accessible
  - Ensure Flink cluster is healthy before submission
  - Review Flink JobManager logs for SQL execution errors
- SQL Gateway requires Flink cluster to be running (no local mode fallback)

## Choosing the Right Mode

| Feature | TableEnvironment | SQL Gateway |
|---------|-----------------|-------------|
| JAR Required | Yes | No |
| Setup Complexity | Medium | Low |
| Interactive Queries | Not ideal | Ideal |
| Custom UDFs | Supported | Limited |
| Stateful Operations | Full support | Full support |
| BI Tool Integration | Limited | Excellent |
| Production Streaming | Recommended | Suitable |

## References
- Flink SQL Connectors: https://nightlies.apache.org/flink/
- Flink SQL Gateway: https://nightlies.apache.org/flink/flink-docs-stable/docs/dev/table/sql-gateway/overview/

