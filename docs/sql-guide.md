# Flink SQL Guide

This guide shows how to run Flink SQL jobs with FlinkDotNet using the `source.sql` IR.

## Concept

- Set `source` to `{ "type": "sql", "statements": [ ... ] }`.
- Provide DDL for sources/sinks (Kafka, filesystem, etc.).
- Provide DML (`INSERT INTO ... SELECT ...`) to move data.
- No separate DSL sink is required; sinks are defined in SQL.

## Example: Kafka → Kafka (Flink SQL)

```
var sqlJob = FlinkDotNet.Pipelines.FlinkDotNet.Sql(
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
);

var result = await sqlJob.Submit("kafka-sql-pipeline");
```

Notes:
- You must ensure the Flink cluster has the relevant SQL connectors on the classpath.
- In LocalTesting, the example assumes Kafka is reachable at `kafka:9092` in the container network.

## Troubleshooting

- If the job starts but no data flows:
  - Verify the Kafka topics exist and connectors are available to the Flink cluster.
  - Check Flink UI for table/connector errors.

## References
- Flink SQL Connectors: https://nightlies.apache.org/flink/

