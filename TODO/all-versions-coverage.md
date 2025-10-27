# TODO: Missing Features Across All Apache Flink Versions

**Status**: Comprehensive Version Coverage Analysis
**Created**: 2025-10-27
**Last Updated**: 2025-10-27
**Scope**: Apache Flink 1.0 through 2.1.0
**Related WI**: Feature coverage audit expansion

## Overview

This document tracks features from **all Apache Flink versions** (1.0 to 2.1.0) that are not yet implemented in FlinkDotNet. The original audit focused on Flink 2.1.0, but this expanded analysis covers the entire version history to ensure no features are missed.

## Version Coverage Summary

| Flink Version | Release Date | FlinkDotNet Coverage | Critical Missing Features |
|---------------|--------------|----------------------|---------------------------|
| 1.0 - 1.9 | 2016-2019 | ✅ Excellent | None - core features implemented |
| 1.10 - 1.14 | 2020-2021 | ✅ Good | Unified Source API, DDL enhancements |
| 1.15 - 1.18 | 2022-2023 | ⚠️ Partial | Table Store, Changelog backend |
| 1.19 | Mar 2024 | ⚠️ Partial | Checkpoint file merging |
| 1.20 | Aug 2024 | ❌ Limited | Materialized Tables, Unified Sink v2 |
| 2.0 | Expected late 2024 | ❌ Not Yet | Fully unified runtime |
| 2.1 | Released 2025 | ❌ Limited | AI/ML Integration (CREATE MODEL, ML_PREDICT) |

## Missing Features by Version

### Flink 1.0 - 1.9 (2016-2019): Core Foundation ✅

**Status**: FlinkDotNet has excellent coverage of these versions.

**Implemented Features**:
- ✅ Stream and batch processing unified API (1.0)
- ✅ Windows, watermarks, state management (1.0)
- ✅ Savepoints for upgrades (1.1)
- ✅ CEP library for complex event processing (1.1)
- ✅ Exactly-once guarantees for Kafka (1.2)
- ✅ RocksDB state backend (1.3)
- ✅ Dynamic scaling of jobs (1.5)
- ✅ State TTL (Time-to-Live) (1.7)
- ✅ Kubernetes integration basics (1.7)

**No critical gaps** - FlinkDotNet DataStream API covers these versions well.

---

### Flink 1.10 - 1.14 (2020-2021): Table API Maturation ⚠️

**Status**: Partial coverage - basic SQL works, advanced features missing.

#### Missing from 1.10 (Feb 2020)
- ⚠️ **Unified Table/SQL API for batch and stream**
  - FlinkDotNet: Basic SQL execution works
  - Missing: Declarative, unified API for both modes
  - Impact: Medium - users can work around with SQL strings

- ❌ **Catalog API and Table Abstractions**
  - What it is: Persistent metadata for tables, databases
  - Missing: Programmatic catalog management
  - Impact: Medium - limits metadata management
  
```java
// Flink 1.10 catalog features not in FlinkDotNet
TableEnvironment tEnv = ...;
Catalog catalog = new HiveCatalog("my_hive", "default", "/path/to/hive/conf");
tEnv.registerCatalog("my_hive", catalog);
tEnv.useCatalog("my_hive");
```

#### Missing from 1.11 (Jul 2020)
- ⚠️ **Full DDL Support in Table API**
  - FlinkDotNet: Basic CREATE TABLE works
  - Missing: Complete DDL vocabulary (CREATE DATABASE, CREATE FUNCTION, etc.)
  - Impact: Medium

#### Missing from 1.12 (Dec 2020)
- ❌ **Unified Source Connectors (FLIP-27)**
  - What it is: New source API for consistent connector development
  - FlinkDotNet: Uses older source pattern
  - Impact: Medium - affects connector ecosystem alignment
  - Note: Kafka source works via legacy API

- ❌ **Changelog Streams for State Tables**
  - What it is: Expose state changes as changelog streams
  - Missing: Cannot query/stream state table changes
  - Impact: Low - niche use case

```java
// Flink 1.12 Unified Source API not in FlinkDotNet
env.fromSource(
    KafkaSource.<String>builder()
        .setBootstrapServers("localhost:9092")
        .setTopics("input-topic")
        .setValueOnlyDeserializer(new SimpleStringSchema())
        .build(),
    WatermarkStrategy.noWatermarks(),
    "Kafka Source"
);
```

#### Missing from 1.13 (May 2021)
- ⚠️ **Fine-Grained Resource Management**
  - FlinkDotNet: Basic resource profiles exist
  - Missing: Full fine-grained control per operator
  - Impact: Low - default resource management works

#### Missing from 1.14 (Sep 2021)
- ❌ **Enhanced Stateful Application Upgrades**
  - What it is: State schema evolution improvements
  - Missing: Advanced state migration features
  - Impact: Low - basic savepoints work

---

### Flink 1.15 - 1.18 (2022-2023): Unified Runtime & Table Store ❌

**Status**: Significant gaps in newer features.

#### Missing from 1.15 (May 2022)
- ❌ **Table Store (Apache Paimon)**
  - What it is: Persistent tables with ACID properties
  - Missing: Integration with Flink's lakehouse table format
  - Impact: High - important for data lake use cases
  - Priority: P1

```sql
-- Flink 1.15 Table Store not supported
CREATE CATALOG my_catalog WITH (
  'type' = 'table-store',
  'warehouse' = 's3://my-bucket/warehouse'
);

CREATE TABLE orders (
  order_id BIGINT,
  user_id BIGINT,
  amount DECIMAL(10,2),
  PRIMARY KEY (order_id) NOT ENFORCED
) WITH (
  'changelog-producer' = 'full-compaction'
);
```

#### Missing from 1.16 (Oct 2022)
- ⚠️ **Enhanced Job Lifecycle Management**
  - What it is: Better control over job submission and monitoring
  - FlinkDotNet: Basic job management exists
  - Impact: Low - core functionality works

#### Missing from 1.17 (Mar 2023)
- ❌ **Changelog State Backend**
  - What it is: New state backend with better changelog semantics
  - Missing: Cannot use changelog backend
  - Impact: Medium - performance optimization opportunity

- ⚠️ **Stateful Batch Stream Pipelines**
  - What it is: Better batch processing with stateful operators
  - FlinkDotNet: Primarily stream-focused
  - Impact: Medium

#### Missing from 1.18 (Oct 2023)
- ⚠️ **Simplified Deployment/Configuration**
  - What it is: Easier cluster configuration
  - FlinkDotNet: Works with current configuration approach
  - Impact: Low

- ❌ **Stronger State TTL Semantics**
  - What it is: Better guarantees for state expiration
  - Missing: Enhanced TTL controls
  - Impact: Low - basic TTL works

---

### Flink 1.19 (Mar 2024): Pre-2.0 Preparations ⚠️

**Status**: Moderate gaps.

#### Missing Features

- ⚠️ **Improved Checkpoint File Merging**
  - What it is: Merge small checkpoint files for better efficiency
  - Missing: Advanced checkpoint file management
  - Impact: Low - checkpointing works, just less optimal

- ❌ **API Deprecations Cleanup**
  - What it is: Removal of deprecated APIs
  - FlinkDotNet: May still reference old patterns
  - Impact: Low - current APIs work

- ⚠️ **Enhanced Configuration Management**
  - What it is: Cleaner configuration structure
  - Impact: Low

**Priority**: P2 (Low) - Version mostly about cleanup for 2.0

---

### Flink 1.20 (Aug 2024): Materialized Tables ❌

**Status**: Major gaps - important new features missing.

#### Missing Features (HIGH Priority)

##### 1. Materialized Tables (FLIP-435) ❌
**What it is**: Declarative SQL for both batch and streaming ETL with automatic refresh management.

**Impact**: HIGH - Simplifies complex pipeline development

**Flink 1.20 Capabilities**:
```sql
-- Create materialized table with freshness guarantee
CREATE MATERIALIZED TABLE dwd_orders (
    PRIMARY KEY(ds, order_id) NOT ENFORCED
) PARTITIONED BY (ds) 
  FRESHNESS = INTERVAL '3' MINUTE
AS
SELECT 
    DATE_FORMAT(order_time, 'yyyy-MM-dd') AS ds,
    order_id,
    user_id,
    SUM(amount) AS total_amount
FROM orders o
JOIN users u ON o.user_id = u.id
GROUP BY ds, order_id, user_id;

-- Manage materialized table
ALTER MATERIALIZED TABLE dwd_orders SUSPEND;
ALTER MATERIALIZED TABLE dwd_orders RESUME;
ALTER MATERIALIZED TABLE dwd_orders REFRESH PARTITION (ds='2024-10-27');
```

**FlinkDotNet Gap**: No support for materialized tables.

**What Would Be Needed**:
```csharp
// Proposed C# API
var tEnv = env.GetTableEnvironment();

tEnv.ExecuteSql(@"
  CREATE MATERIALIZED TABLE dwd_orders (
    PRIMARY KEY(ds, order_id) NOT ENFORCED
  ) PARTITIONED BY (ds)
    FRESHNESS = INTERVAL '3' MINUTE
  AS SELECT ...
");

// Programmatic API
var materializedTable = tEnv.CreateMaterializedTable("dwd_orders")
    .WithQuery("SELECT ... FROM orders ...")
    .WithPartitioning("ds")
    .WithFreshness(TimeSpan.FromMinutes(3))
    .WithPrimaryKey("ds", "order_id")
    .Create();

// Management operations
materializedTable.Suspend();
materializedTable.Resume();
materializedTable.RefreshPartition("ds='2024-10-27'");
```

**Priority**: P0 - Critical for modern data pipeline development

**Estimated Effort**: 4-6 weeks

##### 2. Unified Sink API v2 (Replaces Legacy SinkFunction) ❌
**What it is**: Modern sink API that replaces deprecated SinkFunction.

**Impact**: HIGH - Future connector compatibility

**Flink 1.20**: Legacy SinkFunction deprecated, Unified Sink v2 is @Public API.

**FlinkDotNet Gap**: Still uses older SinkFunction pattern.

**What Would Be Needed**:
```csharp
// Current FlinkDotNet (legacy pattern)
public class MyCustomSink : SinkFunction<Event>
{
    public void Invoke(Event value, Context context) { }
}

// Proposed Unified Sink v2 API
public class MyUnifiedSink : SinkWriter<Event>
{
    public override void Write(Event element, Context context) { }
    
    public override List<CommittableMessage> PrepareCommit() { }
    
    public override void Flush(bool endOfInput) { }
}

// Builder pattern
var sink = Sink.Create<Event>()
    .WithWriter((context) => new MyUnifiedSink(context))
    .WithCommitter(new MyCommitter())
    .WithGlobalCommitter(new MyGlobalCommitter())
    .Build();

stream.SinkTo(sink);
```

**Priority**: P0 - Required for Flink 2.0 compatibility

**Estimated Effort**: 3-4 weeks

##### 3. Unified File Merging for Checkpoints (FLIP-306) ⚠️
**What it is**: Merge many small checkpoint files into fewer large files.

**Impact**: MEDIUM - Performance optimization

**FlinkDotNet Gap**: No explicit API, may work transparently.

**Priority**: P1

##### 4. DISTRIBUTED BY Clause in SQL ⚠️
**What it is**: Bucketing support in SQL for better data distribution.

**Flink 1.20 Capability**:
```sql
CREATE TABLE orders (
  order_id BIGINT,
  user_id BIGINT,
  amount DECIMAL(10,2)
) DISTRIBUTED BY (user_id) INTO 16 BUCKETS;
```

**FlinkDotNet Gap**: No DISTRIBUTED BY support.

**Priority**: P2

**Estimated Effort**: 1-2 weeks

##### 5. Data Skew Metrics in Dashboard ⚠️
**What it is**: Dashboard shows data skew scores for operators.

**Impact**: LOW - Monitoring enhancement

**FlinkDotNet Gap**: Depends on Flink dashboard, not C# API concern.

**Priority**: P3 - Transparent feature

---

### Flink 2.0 (Expected Late 2024) ❌

**Status**: Not released yet, but roadmap is known.

**Expected Major Changes**:
- Fully unified runtime for batch/streaming (no separate modes)
- Complete removal of deprecated APIs (legacy SinkFunction, etc.)
- Enhanced state handling and checkpointing
- Cleaner configuration structure

**FlinkDotNet Preparation Needed**:
- Migrate to Unified Sink API v2 (before 2.0 releases)
- Remove any deprecated API usage
- Test with 2.0 release candidates

**Priority**: P0 when released

---

### Flink 2.1 (Released 2025) ❌

**Status**: Major gaps in flagship features (already documented).

See existing TODO documents:
- [ai-ml-integration-features.md](ai-ml-integration-features.md) - CREATE MODEL, ML_PREDICT
- [table-api-advanced-sql-features.md](table-api-advanced-sql-features.md) - VARIANT, PTFs
- [performance-format-features.md](performance-format-features.md) - Smile format, async batching

**Critical Missing**:
- ❌ AI/ML Integration (CREATE MODEL, ML_PREDICT, AI providers)
- ❌ DeltaJoin operator
- ❌ VARIANT data type for JSON
- ❌ Process Table Functions (PTFs)

---

## Consolidated Priority List

### P0 - Critical (Must Implement Soon)

| Feature | Version | Effort | Impact | Document |
|---------|---------|--------|--------|----------|
| AI/ML Integration | 2.1 | 10-16 weeks | Very High | [ai-ml-integration-features.md](ai-ml-integration-features.md) |
| Materialized Tables | 1.20 | 4-6 weeks | High | This document |
| Unified Sink API v2 | 1.20 | 3-4 weeks | High | This document |
| VARIANT Data Type | 2.1 | 3-4 weeks | High | [table-api-advanced-sql-features.md](table-api-advanced-sql-features.md) |

### P1 - High (Important Enhancements)

| Feature | Version | Effort | Impact | Document |
|---------|---------|--------|--------|----------|
| Table Store Integration | 1.15 | 3-4 weeks | High | This document |
| Native Table API | 2.1 | 4-6 weeks | Medium | [table-api-advanced-sql-features.md](table-api-advanced-sql-features.md) |
| Process Table Functions | 2.1 | 3-4 weeks | Medium | [table-api-advanced-sql-features.md](table-api-advanced-sql-features.md) |
| Catalog API | 1.10 | 2-3 weeks | Medium | This document |
| Unified Source API | 1.12 | 2-3 weeks | Medium | This document |

### P2 - Medium (Nice to Have)

| Feature | Version | Effort | Impact | Document |
|---------|---------|--------|--------|----------|
| Changelog State Backend | 1.17 | 2-3 weeks | Medium | This document |
| DISTRIBUTED BY Clause | 1.20 | 1-2 weeks | Low | This document |
| Enhanced DDL Support | 1.11 | 2 weeks | Low | This document |
| File Merging for Checkpoints | 1.20 | 1 week | Low | This document |

### P3 - Low (Optional)

| Feature | Version | Effort | Impact | Document |
|---------|---------|--------|--------|----------|
| Stronger State TTL | 1.18 | 1 week | Low | This document |
| Enhanced Configuration | 1.19 | 1 week | Low | This document |
| Data Skew Metrics | 1.20 | N/A | Low | Transparent |

---

## Recommended Implementation Roadmap

### Phase 1: Critical Flink 1.20 Features (7-10 weeks)
**Goal**: Ensure FlinkDotNet works with modern Flink patterns

1. **Unified Sink API v2** (3-4 weeks) - Required for Flink 2.0 compatibility
2. **Materialized Tables** (4-6 weeks) - Major productivity improvement

### Phase 2: AI/ML Integration from Flink 2.1 (10-16 weeks)
**Goal**: Support Flink 2.1's flagship AI features

3. **CREATE MODEL DDL** (2-3 weeks)
4. **ML_PREDICT TVF** (2-3 weeks)
5. **AI Providers** (3-4 weeks)
6. **C# Model Management API** (3-4 weeks)

### Phase 3: Advanced Table Features (9-13 weeks)
**Goal**: Complete Table API parity

7. **VARIANT Type & JSON Functions** (3-4 weeks)
8. **Native Table API** (4-6 weeks)
9. **Process Table Functions** (3-4 weeks)

### Phase 4: Ecosystem Integration (5-7 weeks)
**Goal**: Better connector and storage integration

10. **Table Store (Paimon)** (3-4 weeks)
11. **Unified Source API** (2-3 weeks)

### Phase 5: Optional Enhancements (As Needed)
**Goal**: Performance and polish

12. **Catalog API** (2-3 weeks)
13. **Changelog Backend** (2-3 weeks)
14. **Other P2/P3 features** (varies)

---

## Total Estimated Implementation Effort

**P0 Features Only**: 20-29 weeks (5-7 months)
**P0 + P1 Features**: 35-50 weeks (9-12 months)
**Complete Parity**: 50+ weeks (12+ months)

---

## References

### Official Apache Flink Documentation
- [Flink Version History](https://flink.apache.org/downloads/)
- [Flink 1.20 Release Notes](https://nightlies.apache.org/flink/flink-docs-master/release-notes/flink-1.20/)
- [Flink 2.1 Release Notes](https://nightlies.apache.org/flink/flink-docs-master/release-notes/flink-2.1/)
- [Materialized Tables Documentation](https://nightlies.apache.org/flink/flink-docs-master/docs/dev/table/materialized-table/)
- [Unified Sink API](https://nightlies.apache.org/flink/flink-docs-master/docs/dev/datastream/fault-tolerance/unified_sink/)

### Release Announcements
- [Flink 1.20 Announcement](https://flink.apache.org/2024/08/02/announcing-the-release-of-apache-flink-1.20/)
- [Flink 2.1 Announcement](https://flink.apache.org/2025/07/31/apache-flink-2.1.0-ushers-in-a-new-era-of-unified-real-time-data--ai-with-comprehensive-upgrades/)

---

## Last Updated

**Date**: 2025-10-27
**Scope**: Apache Flink 1.0 through 2.1.0
**Next Review**: When Flink 2.2 or later is released
