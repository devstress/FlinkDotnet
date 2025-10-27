# FlinkDotNet TODO - Missing Apache Flink 2.1.0 Features

This folder tracks features from Apache Flink 2.1.0 that are not yet implemented in FlinkDotNet.

## Quick Status Overview

| Feature Category | Status | Priority | Estimated Effort | Document |
|------------------|--------|----------|------------------|----------|
| **AI/ML Integration** | ❌ Not Implemented | **HIGH** | 10-16 weeks | [ai-ml-integration-features.md](ai-ml-integration-features.md) |
| **Table API & Advanced SQL** | ⚠️ Partial | **MEDIUM** | 12-17 weeks | [table-api-advanced-sql-features.md](table-api-advanced-sql-features.md) |
| **Performance & Format** | ❌ Not Implemented | **LOW** | 7-10 weeks | [performance-format-features.md](performance-format-features.md) |
| **Prometheus Exporter** | 📋 Planned | **LOW** | 8-10 days | [prometheus-exporter-future-design.md](prometheus-exporter-future-design.md) |

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

## What's Missing from Flink 2.1.0 ❌

### 1. AI/ML Integration Features (HIGH Priority)

Apache Flink 2.1.0's **flagship feature** is Data + AI integration. FlinkDotNet has **zero support** for these features.

**Missing**:
- ❌ AI Model DDL (CREATE MODEL, ALTER MODEL, DROP MODEL, SHOW MODELS, DESCRIBE MODEL)
- ❌ ML_PREDICT Table-Valued Function for real-time inference
- ❌ AI provider integrations (OpenAI, Azure OpenAI, custom endpoints)
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

### Phase 1: AI/ML Integration (Recommended First)
**Why**: Flink 2.1.0's flagship feature, high user value, differentiates FlinkDotNet

**Estimated**: 10-16 weeks total
1. Basic SQL DDL support (2-3 weeks)
2. ML_PREDICT TVF (2-3 weeks)
3. C# Table API for models (3-4 weeks)
4. Provider integrations (2-3 weeks each)

**Key deliverables**:
- CREATE MODEL SQL support
- ML_PREDICT for real-time inference
- OpenAI and Azure OpenAI providers
- Documentation and examples

### Phase 2: Table API Enhancements (Recommended Second)
**Why**: Improves developer experience, enables type-safe programming

**Estimated**: 12-17 weeks total
1. VARIANT type and JSON functions (3-4 weeks)
2. Native Table API (4-6 weeks)
3. Structured types (2-3 weeks)
4. Process Table Functions (3-4 weeks)

**Key deliverables**:
- JSON/semi-structured data support
- Fluent table transformation API
- Custom structured types
- Advanced stateful UDFs

### Phase 3: Performance Features (Optional)
**Why**: Expert-level optimizations for specific scenarios

**Estimated**: 7-10 weeks total
- Implement based on user demand
- Most features work by default
- Lower priority

## How to Contribute

Want to implement one of these features? Great!

1. **Review the detailed TODO document** for the feature category
2. **Create a Work Item** in `WIs/` folder following the template
3. **Follow TDD/BDD approach** - write tests first
4. **Start small** - implement minimal viable feature first
5. **Coordinate with maintainers** via GitHub issues

## References

### Official Apache Flink 2.1.0 Documentation
- [Release Notes](https://nightlies.apache.org/flink/flink-docs-master/release-notes/flink-2.1/)
- [Release Announcement](https://flink.apache.org/2025/07/31/apache-flink-2.1.0-ushers-in-a-new-era-of-unified-real-time-data--ai-with-comprehensive-upgrades/)
- [Table API Documentation](https://nightlies.apache.org/flink/flink-docs-stable/docs/dev/table/overview/)
- [Model DDL Reference](https://www.alibabacloud.com/help/en/flink/realtime-flink/developer-reference/model-ddl)

### FlinkDotNet Documentation
- [Current Features](../docs/features.md)
- [Flink 2.1 Features (Implemented)](../docs/flink-21-features.md)
- [SQL Guide](../docs/sql-guide.md)
- [API Reference](../docs/api-reference.md)

## Last Updated

**Date**: 2025-10-27
**Apache Flink Version**: 2.1.0
**FlinkDotNet Version**: Current main branch
**Audit Work Item**: WI5_flink-21-feature-coverage-audit.md
