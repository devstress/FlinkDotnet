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

## Enhanced Validation System

FlinkDotNet provides comprehensive validation through the modular `JobDefinitionValidator`:

### Validation Features
- **Modular validation methods**: Each validation concern is handled by focused, maintainable methods
- **Cognitive complexity optimized**: All validation logic keeps complexity under 15 threshold
- **Detailed error messages**: Specific guidance for resolving validation issues
- **Pre-submission validation**: Catch errors before job submission to Flink cluster

### Validation Process
```csharp
// Enhanced validation with detailed error reporting
var validationResult = JobDefinitionValidator.Validate(jobDefinition);
if (!validationResult.IsValid)
{
    Console.WriteLine("Validation errors:");
    foreach (var error in validationResult.Errors)
    {
        Console.WriteLine($"  - {error}");
    }
    return;
}
```

### Validation Rules

#### Metadata Validation
- **Job ID**: Required, non-empty string
- **Version**: Required version identifier
- **Parallelism**: Must be >= 1 when provided

#### Source Validation
- **Kafka sources**: Topic name required
- **File sources**: Path and format required
- **HTTP sources**: Valid URL and positive interval required
- **Database sources**: Connection string, query, and positive polling interval required
- **SQL sources**: At least one SQL statement required

#### Operation Validation
- **Filter operations**: Valid filter expressions
- **Map operations**: Valid transformation expressions
- **Window operations**: Valid window configurations
- **Async operations**: Proper timeout and retry configurations

#### Sink Validation
- **Kafka sinks**: Topic name required
- **File sinks**: Valid path and format
- **Database sinks**: Connection string and table name required
- **HTTP sinks**: Valid endpoint URL required

### Enhanced Error Messages

The validator now provides specific, actionable error messages:
```
❌ Before: "Invalid source"
✅ After: "source.kafka.topic is required"

❌ Before: "Bad window config"  
✅ After: "window.sliding requires slide > 0 and size > 0"

❌ Before: "Invalid metadata"
✅ After: "metadata.parallelism must be >= 1 when provided"
```

## Validation Highlights

- **Kafka topics**: Required for all Kafka sources and sinks
- **Window configurations**: 
  - `SLIDING` requires `slide > 0` and `size > 0`
  - Valid time units: `SECONDS|MINUTES|HOURS`
- **Timer operations**: `delayMs` between 1ms and 24 hours
- **HTTP operations**: Valid URLs and positive timeout values
- **SQL jobs**: At least one valid SQL statement required
- Async: `timeoutMs` up to 20 minutes; `maxRetries` 0..100
- Retry: at least one `delayMs`, all positive
- SQL: at least one statement; sink may be omitted when using SQL

Validation is enforced pre-submit by the SDK and at the Gateway. See `Flink.JobBuilder.Services.JobDefinitionValidator`.
