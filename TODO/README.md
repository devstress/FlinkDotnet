# FlinkDotNet TODO - Missing Apache Flink Features

This folder tracks features from **all Apache Flink versions** (1.0 through 2.1.0) that are not yet implemented in FlinkDotNet.

## 📋 Quick Navigation

### For Contributors
- **[IMPLEMENTATION_GUIDE.md](IMPLEMENTATION_GUIDE.md)** - 📖 Step-by-step guide to implementing TODO features
- **[TRACKING.md](TRACKING.md)** - 📊 Implementation progress tracking and roadmap
- **[.implementation-template.md](.implementation-template.md)** - 📝 Work Item template for TODO features

### Feature Documentation
- **[All Versions Coverage](all-versions-coverage.md)** - Comprehensive analysis of ALL Flink versions (1.0-2.1.0)
- **[AI/ML Integration (Flink 2.1)](ai-ml-integration-features.md)** - CREATE MODEL, ML_PREDICT, AI providers
- **[Table API & Advanced SQL (Flink 2.1)](table-api-advanced-sql-features.md)** - VARIANT, PTFs, native API
- **[Performance Features (Flink 2.1)](performance-format-features.md)** - Smile format, async batching
- **[Observability](observability-features.md)** - Comprehensive testing in ReleasePackageVerification
- **[Prometheus Exporter](prometheus-exporter-future-design.md)** - Custom metrics (deferred)

## Quick Status Overview

### By Flink Version

| Flink Version | Release | Coverage | Critical Missing | Details |
|---------------|---------|----------|------------------|---------|
| 1.0 - 1.9 | 2016-2019 | ✅ Excellent | None | Core features implemented |
| 1.10 - 1.14 | 2020-2021 | ✅ Good | Unified Source, DDL | [View Details](all-versions-coverage.md#flink-110---114-2020-2021-table-api-maturation-) |
| 1.15 - 1.18 | 2022-2023 | ⚠️ Partial | Table Store, Changelog | [View Details](all-versions-coverage.md#flink-115---118-2022-2023-unified-runtime--table-store-) |
| 1.19 | Mar 2024 | ⚠️ Partial | Checkpoint merging | [View Details](all-versions-coverage.md#flink-119-mar-2024-pre-20-preparations-) |
| 1.20 | Aug 2024 | ❌ Limited | Materialized Tables, Unified Sink v2 | [View Details](all-versions-coverage.md#flink-120-aug-2024-materialized-tables-) |
| 2.0 | Expected | ❌ Not Yet | Unified runtime | [View Details](all-versions-coverage.md#flink-20-expected-late-2024-) |
| 2.1 | 2025 | ❌ Limited | AI/ML Integration | [View Details](all-versions-coverage.md#flink-21-released-2025-) |

### By Feature Category

### By Feature Category

| Feature Category | Status | Priority | Estimated Effort | Document |
|------------------|--------|----------|------------------|----------|
| **AI/ML Integration (2.1)** | ❌ Not Implemented | **P0 - Critical** | 10-16 weeks | [ai-ml-integration-features.md](ai-ml-integration-features.md) |
| **Materialized Tables (1.20)** | ❌ Not Implemented | **P0 - Critical** | 4-6 weeks | [all-versions-coverage.md](all-versions-coverage.md#1-materialized-tables-flip-435-) |
| **Unified Sink API v2 (1.20)** | ❌ Not Implemented | **P0 - Critical** | 3-4 weeks | [all-versions-coverage.md](all-versions-coverage.md#2-unified-sink-api-v2-replaces-legacy-sinkfunction-) |
| **Table API & Advanced SQL (2.1)** | ⚠️ Partial | **P1 - High** | 12-17 weeks | [table-api-advanced-sql-features.md](table-api-advanced-sql-features.md) |
| **Observability Testing** | ⚠️ Partial | **P1 - High** | 2-3 weeks | [observability-features.md](observability-features.md) |
| **Table Store/Paimon (1.15)** | ❌ Not Implemented | **P1 - High** | 3-4 weeks | [all-versions-coverage.md](all-versions-coverage.md#missing-from-115-may-2022) |
| **Performance & Format (2.1)** | ❌ Not Implemented | **P2 - Medium** | 7-10 weeks | [performance-format-features.md](performance-format-features.md) |
| **Prometheus Exporter** | 📋 Planned | **P3 - Low** | 8-10 days | [prometheus-exporter-future-design.md](prometheus-exporter-future-design.md) |

## What FlinkDotNet Already Supports ✅

FlinkDotNet has **excellent coverage** of core Apache Flink 2.1.0 features:

### DataStream API (Complete)
- ✅ All transformation operators (map, filter, flatMap, reduce, aggregate, etc.)
- ✅ Event-time processing with watermarks
- ✅ All window types (tumbling, sliding, session, count-based)
- ✅ Advanced partitioning (rebalance, rescale, forward, shuffle, broadcast, custom)
- ✅ Savepoint and checkpoint operations
- ✅ Restart strategies (exponential delay, fixed delay, failure rate)
- ✅ Resource management (slot sharing, resource profiles)
- ✅ Adaptive scheduler and reactive mode
- ✅ Job monitoring and control

### Sources & Sinks
- ✅ Kafka source and sink integration
- ✅ Custom sources and sinks
- ✅ Collection sources for testing

### Basic SQL Support
- ✅ SQL execution via TableEnvironment
- ✅ Table creation DDL
- ✅ SQL Gateway integration
- ✅ Kafka connector in SQL
- ✅ Basic transformations (SELECT, INSERT, WHERE, GROUP BY)

## What's Missing Across All Flink Versions ❌

### P0 - Critical Features (Must Implement Soon)

**From Flink 2.1 (2025)**:
- ❌ **AI/ML Integration** - CREATE MODEL, ML_PREDICT, AI providers (10-16 weeks)
  - Real-time AI inference in streaming pipelines
  - OpenAI, Azure OpenAI, custom model providers
  - [Details: ai-ml-integration-features.md](ai-ml-integration-features.md)

**From Flink 1.20 (Aug 2024)**:
- ❌ **Materialized Tables (FLIP-435)** - Declarative ETL with auto-refresh (4-6 weeks)
  - Simplifies batch/stream pipeline development
  - Automatic freshness management
  - [Details: all-versions-coverage.md](all-versions-coverage.md#1-materialized-tables-flip-435-)

- ❌ **Unified Sink API v2** - Modern sink pattern (3-4 weeks)
  - Replaces deprecated SinkFunction
  - Required for Flink 2.0+ compatibility
  - [Details: all-versions-coverage.md](all-versions-coverage.md#2-unified-sink-api-v2-replaces-legacy-sinkfunction-)

**From Flink 2.1 (2025)**:
- ❌ **VARIANT Data Type** - Semi-structured JSON data (3-4 weeks)
  - Efficient JSON processing
  - PARSE_JSON/TRY_PARSE_JSON functions
  - [Details: table-api-advanced-sql-features.md](table-api-advanced-sql-features.md#2-variant-data-type-support)

### P1 - High Priority Features

**From Flink 1.15 (May 2022)**:
- ❌ **Table Store (Apache Paimon)** - Lakehouse table format (3-4 weeks)
  - ACID properties for persistent tables
  - Data lake integration
  - [Details: all-versions-coverage.md](all-versions-coverage.md#missing-from-115-may-2022)

**From Flink 2.1 (2025)**:
- ❌ **Process Table Functions (PTFs)** - Advanced stateful UDFs (3-4 weeks)
- ❌ **Native Table API** - Type-safe table transformations (4-6 weeks)
  - [Details: table-api-advanced-sql-features.md](table-api-advanced-sql-features.md)

**From Earlier Versions**:
- ⚠️ **Catalog API (1.10)** - Metadata management (2-3 weeks)
- ⚠️ **Unified Source API (1.12)** - Modern source connectors (2-3 weeks)

### P2 - Medium Priority Features

- ⚠️ **Changelog State Backend (1.17)** - Performance optimization (2-3 weeks)
- ⚠️ **DISTRIBUTED BY Clause (1.20)** - SQL bucketing (1-2 weeks)
- ⚠️ **Enhanced DDL Support (1.11)** - Complete DDL vocabulary (2 weeks)

### P3 - Low Priority Features

- Performance optimizations (Smile format, checkpoint merging)
- Configuration enhancements
- Transparent features (data skew metrics)

**See [all-versions-coverage.md](all-versions-coverage.md) for complete details on all versions.**
- ❌ Model management in Table API

**Impact**: Cannot build real-time AI inference pipelines (sentiment analysis, fraud detection, content moderation, predictive maintenance)

**Details**: [ai-ml-integration-features.md](ai-ml-integration-features.md)

### 2. Table API & Advanced SQL (MEDIUM Priority)

FlinkDotNet supports basic SQL but lacks comprehensive Table API features.

**Missing**:
- ❌ Process Table Functions (PTFs) - Advanced stateful UDFs with timer access
- ❌ VARIANT data type for semi-structured JSON data
- ❌ PARSE_JSON/TRY_PARSE_JSON functions
- ❌ Structured Type API for user-defined types
- ❌ Native Table API programming (fluent table transformations)
- ❌ Modern window TVFs (TUMBLE, HOP, CUMULATE)
- ❌ DeltaJoin explicit configuration

**Impact**: Limited to basic SQL, no type-safe table operations, can't efficiently process JSON data

**Details**: [table-api-advanced-sql-features.md](table-api-advanced-sql-features.md)

### 3. Performance & Format Features (LOW Priority)

These features may work transparently through Flink runtime or are expert-level optimizations.

**Missing**:
- ❌ Smile format for compiled plans (binary JSON optimization)
- ❌ Custom async sink batching strategies
- ⚠️ MultiJoin optimization configuration (may work by default)
- ⚠️ Fine-grained state backend tuning (basic support exists)

**Impact**: Minor - most applications work fine with defaults

**Details**: [performance-format-features.md](performance-format-features.md)

### 4. Prometheus Exporter (LOW Priority - Deferred)

Custom Prometheus metrics for FlinkDotNet JobGateway.

**Status**: Design exists but deferred pending test fixes

**Details**: [prometheus-exporter-future-design.md](prometheus-exporter-future-design.md)

## Implementation Roadmap

### Phase 1: Critical Flink 1.20 Features (7-10 weeks)
**Goal**: Ensure FlinkDotNet works with modern Flink patterns

1. **Unified Sink API v2** (3-4 weeks) - Required for Flink 2.0 compatibility
2. **Materialized Tables** (4-6 weeks) - Major productivity improvement

### Phase 2: AI/ML Integration from Flink 2.1 (10-16 weeks)
**Goal**: Support Flink 2.1's flagship AI features

3. **CREATE MODEL DDL** (2-3 weeks)
4. **ML_PREDICT TVF** (2-3 weeks)
5. **AI Providers** (3-4 weeks) - OpenAI, Azure OpenAI
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

**Total Effort Estimates**:
- P0 Features Only: 20-29 weeks (5-7 months)
- P0 + P1 Features: 35-50 weeks (9-12 months)
- Complete Parity: 50+ weeks (12+ months)

## How to Contribute

Want to implement one of these features? Great!

**📖 START HERE**: [IMPLEMENTATION_GUIDE.md](IMPLEMENTATION_GUIDE.md) - Complete step-by-step guide

### Quick Overview

1. **Review the detailed TODO document** for the feature category
2. **Create a Work Item** using the [implementation template](.implementation-template.md)
3. **Follow TDD/BDD approach** - write tests first
4. **Start small** - implement minimal viable feature first
5. **Coordinate with maintainers** via GitHub issues

**New Contributors**: See [IMPLEMENTATION_GUIDE.md](IMPLEMENTATION_GUIDE.md) for detailed walkthrough, best practices, and common patterns.

## References

### Official Apache Flink Documentation
- [Apache Flink Version History](https://flink.apache.org/downloads/)
- [Flink 1.20 Release Notes](https://nightlies.apache.org/flink/flink-docs-master/release-notes/flink-1.20/)
- [Flink 2.1 Release Notes](https://nightlies.apache.org/flink/flink-docs-master/release-notes/flink-2.1/)
- [Flink 1.20 Announcement](https://flink.apache.org/2024/08/02/announcing-the-release-of-apache-flink-1.20/)
- [Flink 2.1 Announcement](https://flink.apache.org/2025/07/31/apache-flink-2.1.0-ushers-in-a-new-era-of-unified-real-time-data--ai-with-comprehensive-upgrades/)
- [Materialized Tables Documentation](https://nightlies.apache.org/flink/flink-docs-master/docs/dev/table/materialized-table/)
- [Unified Sink API](https://nightlies.apache.org/flink/flink-docs-master/docs/dev/datastream/fault-tolerance/unified_sink/)
- [Table API Documentation](https://nightlies.apache.org/flink/flink-docs-stable/docs/dev/table/overview/)
- [Model DDL Reference](https://www.alibabacloud.com/help/en/flink/realtime-flink/developer-reference/model-ddl)

### FlinkDotNet Documentation
- [Current Features](../docs/features.md)
- [Flink 2.1 Features (Implemented)](../docs/flink-21-features.md)
- [SQL Guide](../docs/sql-guide.md)
- [API Reference](../docs/api-reference.md)

## Last Updated

**Date**: 2025-10-28
**Changes**: Added IMPLEMENTATION_GUIDE.md and .implementation-template.md for contributors
**Scope**: Apache Flink 1.0 through 2.1.0 (all versions)
**Next Review**: When Flink 2.2 or later is released
