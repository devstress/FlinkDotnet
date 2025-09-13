# DSL Guide

The .NET SDK (`Flink.JobBuilder`) produces an Intermediate Representation (IR) describing a Flink job. This guide maps the DSL objects to IR and highlights constraints enforced by validation.

## Core Types

- `JobDefinition`
  - `source` (one of): `kafka`, `file`, `http`, `database`
  - `source.sql`: Execute Flink SQL (DDL/DML) via `statements[]` (sink defined in SQL)
  - `operations` (0..N):
    - `filter`, `map`, `groupBy`, `aggregate`, `window`, `timer`
    - `asyncFunction` (HTTP): asynchronous HTTP call with `timeoutMs`, `headers`, `bodyTemplate`. Combine with `retry` op to configure backoff.
    - `state`: value-state with optional TTL (v1.1 minimal touch function)
    - `sideOutput`: route to side output when `condition` matches (supports `nonempty`), sink currently supports Kafka
    - `retry`: optional backoff for the next async op (applied to HTTP)
  - `sink` (one of): `kafka`, `console`, `file`, `database`, `http`, `redis`
  - `metadata`: job id, version, parallelism, properties

See `docs/ir-schema-v1.json` for the canonical JSON schema.

## Common Patterns

- Kafka → Transform → Kafka
```
Source: Kafka(topic: input)
Ops: Map, Filter, Window (optional)
Sink:  Kafka(topic: output)
```

- HTTP Enrichment with Retry
```
Ops: asyncFunction(functionType: http, timeoutMs, maxRetries)
     retry(maxRetries, delayMs[])
```

- Flink SQL (Table API)
```
Source: sql(statements: [
  'CREATE TABLE input (...) WITH (...)',
  'CREATE TABLE output (...) WITH (...)',
  'INSERT INTO output SELECT ... FROM input'
])
// No DSL sink needed; sink defined in SQL
```

## Validation Highlights

- Kafka topics required for sources/sinks
- Window: `SLIDING` requires `slide > 0`; `size > 0`; unit in `SECONDS|MINUTES|HOURS`
- Timer: `delayMs` between 1 ms and 1 day
- Async: `timeoutMs` up to 20 minutes; `maxRetries` 0..100
- Retry: at least one `delayMs`, all positive
- SQL: at least one statement; sink may be omitted when using SQL

Validation is enforced pre-submit by the SDK and at the Gateway. See `Flink.JobBuilder.Services.JobDefinitionValidator`.
