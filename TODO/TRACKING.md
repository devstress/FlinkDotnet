# TODO Implementation Tracking

**Purpose**: Track incremental implementation progress for Apache Flink features documented in this folder.

**Last Updated**: 2025-10-29
**Related WI**: WI5_todo-implementation-tracking.md

## Quick Status Dashboard

| Category | Total Features | Implemented | In Progress | Not Started | Priority |
|----------|---------------|-------------|-------------|-------------|----------|
| AI/ML Integration | 5 | 0 | 0 | 5 | P0 |
| Table API & SQL | 7 | 0 | 0 | 7 | P1 |
| Performance & Format | 4 | 0 | 0 | 4 | P2 |
| Materialized Tables | 1 | 0 | 0 | 1 | P0 |
| Unified Sink API v2 | 1 | 1 | 0 | 0 | P0 ✅ COMPLETE |
| Table Store (Paimon) | 1 | 0 | 0 | 1 | P1 |
| **TOTAL** | **19** | **1** | **0** | **18** | - |

## Feature Implementation Checklist

### P0 - Critical Features (Must Implement Soon)

#### AI/ML Integration (Flink 2.1) - 10-16 weeks total
- [ ] CREATE MODEL DDL syntax (2-3 weeks) 
  - **WI**: Not yet created
  - **Status**: Not started
  - **Document**: [ai-ml-integration-features.md](ai-ml-integration-features.md#1-create-model-ddl)
  
- [ ] ML_PREDICT Table Value Function (2-3 weeks)
  - **WI**: Not yet created
  - **Status**: Not started
  - **Document**: [ai-ml-integration-features.md](ai-ml-integration-features.md#2-ml_predict-table-value-function)
  
- [ ] AI Provider Integration - OpenAI (2-3 weeks)
  - **WI**: Not yet created
  - **Status**: Not started
  - **Document**: [ai-ml-integration-features.md](ai-ml-integration-features.md#3-ai-provider-integration)
  
- [ ] AI Provider Integration - Azure OpenAI (1-2 weeks)
  - **WI**: Not yet created
  - **Status**: Not started
  - **Document**: [ai-ml-integration-features.md](ai-ml-integration-features.md#3-ai-provider-integration)
  
- [ ] C# Model Management API (3-4 weeks)
  - **WI**: Not yet created
  - **Status**: Not started
  - **Document**: [ai-ml-integration-features.md](ai-ml-integration-features.md#4-c-model-management-api)

#### Materialized Tables (Flink 1.20) - 4-6 weeks
- [ ] Materialized Tables FLIP-435 (4-6 weeks)
  - **WI**: WI7_materialized-tables.md - Investigation phase started
  - **Status**: Investigation in progress (Week 1)
  - **Started**: 2025-10-29
  - **Progress**: SQL DDL syntax researched, API design planned
  - **Document**: [all-versions-coverage.md](all-versions-coverage.md#1-materialized-tables-flip-435-)

#### Unified Sink API v2 (Flink 1.20) - 3-4 weeks
- [x] Unified Sink API v2 (3-4 weeks) - **✅ COMPLETE**
  - **WI**: WI6_unified-sink-api-v2.md
  - **Status**: Implementation complete - all phases finished
  - **Started**: 2025-10-28
  - **Completed**: 2025-10-29
  - **Progress**: IR Schema ✅, C# API ✅, Java IR Runner ✅, Testing ✅
  - **Tests Added**: +24 unit tests, +6 integration tests (all passing)
  - **Commits**: 77ac813, d9f50a0, fedc628, 2e02e58
  - **Document**: [all-versions-coverage.md](all-versions-coverage.md#2-unified-sink-api-v2-replaces-legacy-sinkfunction-)


#### VARIANT Data Type (Flink 2.1) - 3-4 weeks
- [ ] VARIANT Type Support (2-3 weeks)
  - **WI**: Not yet created
  - **Status**: Not started
  - **Document**: [table-api-advanced-sql-features.md](table-api-advanced-sql-features.md#2-variant-data-type-support)
  
- [ ] PARSE_JSON/TRY_PARSE_JSON Functions (1 week)
  - **WI**: Not yet created
  - **Status**: Not started
  - **Document**: [table-api-advanced-sql-features.md](table-api-advanced-sql-features.md#2-variant-data-type-support)

### P1 - High Priority Features

#### Table Store / Apache Paimon (Flink 1.15) - 3-4 weeks
- [ ] Table Store Integration (3-4 weeks)
  - **WI**: Not yet created
  - **Status**: Not started
  - **Document**: [all-versions-coverage.md](all-versions-coverage.md#missing-from-115-may-2022)

#### Process Table Functions (Flink 2.1) - 3-4 weeks
- [ ] Process Table Functions (PTFs) (3-4 weeks)
  - **WI**: Not yet created
  - **Status**: Not started
  - **Document**: [table-api-advanced-sql-features.md](table-api-advanced-sql-features.md#3-process-table-functions-ptfs)

#### Native Table API (Flink 2.1) - 4-6 weeks
- [ ] Native Table API Programming (4-6 weeks)
  - **WI**: Not yet created
  - **Status**: Not started
  - **Document**: [table-api-advanced-sql-features.md](table-api-advanced-sql-features.md#4-native-table-api-programming)

#### Earlier Version Features - 4-6 weeks
- [ ] Catalog API (Flink 1.10) (2-3 weeks)
  - **WI**: Not yet created
  - **Status**: Not started
  - **Document**: [all-versions-coverage.md](all-versions-coverage.md#missing-from-110-feb-2020)
  
- [ ] Unified Source API (Flink 1.12) (2-3 weeks)
  - **WI**: Not yet created
  - **Status**: Not started
  - **Document**: [all-versions-coverage.md](all-versions-coverage.md#missing-from-112-dec-2020)

### P2 - Medium Priority Features

#### Performance Features
- [ ] Changelog State Backend (Flink 1.17) (2-3 weeks)
  - **WI**: Not yet created
  - **Status**: Not started
  - **Document**: [all-versions-coverage.md](all-versions-coverage.md#missing-from-117-mar-2023)
  
- [ ] DISTRIBUTED BY Clause (Flink 1.20) (1-2 weeks)
  - **WI**: Not yet created
  - **Status**: Not started
  - **Document**: [all-versions-coverage.md](all-versions-coverage.md#missing-from-120-aug-2024)
  
- [ ] Smile Format for Compiled Plans (1-2 weeks)
  - **WI**: Not yet created
  - **Status**: Not started
  - **Document**: [performance-format-features.md](performance-format-features.md#1-smile-format-for-compiled-plans)
  
- [ ] Custom Async Sink Batching (2-3 weeks)
  - **WI**: Not yet created
  - **Status**: Not started
  - **Document**: [performance-format-features.md](performance-format-features.md#2-custom-async-sink-batching-strategies)

### P3 - Low Priority Features

#### Prometheus Exporter
- [ ] Prometheus Exporter for JobGateway (8-10 days)
  - **WI**: WI74_prometheus-exporter-design.md (Design exists, deferred)
  - **Status**: Deferred - pending test fixes
  - **Document**: [prometheus-exporter-future-design.md](prometheus-exporter-future-design.md)

## Implementation Workflow

### How to Implement a Feature from TODO

1. **Select Feature**: Choose from checklist above
2. **Create Work Item**: Use template at `TODO/.implementation-template.md`
3. **Follow TDD/BDD**: Write tests first
4. **Implement Minimally**: Smallest viable feature
5. **Update This File**: Check off feature and add WI reference
6. **Update TODO/README.md**: Reflect new implementation status

### Work Item Template Location
See `TODO/.implementation-template.md` for standardized WI template.

## Implementation Roadmap

### Phase 1: Critical Flink 1.20 Features (7-10 weeks)
**Goal**: Ensure FlinkDotNet works with modern Flink patterns

**Priority**: P0 - Must complete before Flink 2.0 adoption

**Features**:
1. Unified Sink API v2 (3-4 weeks) - Required for Flink 2.0 compatibility
2. Materialized Tables (4-6 weeks) - Major productivity improvement

**Success Criteria**:
- Legacy SinkFunction replaced with Unified Sink API v2
- Materialized Tables working with auto-refresh
- All existing integration tests pass with new APIs

### Phase 2: AI/ML Integration from Flink 2.1 (10-16 weeks)
**Goal**: Support Flink 2.1's flagship AI features

**Priority**: P0 - Critical for real-time AI use cases

**Features**:
1. CREATE MODEL DDL (2-3 weeks)
2. ML_PREDICT TVF (2-3 weeks)
3. AI Providers (3-4 weeks) - OpenAI, Azure OpenAI
4. C# Model Management API (3-4 weeks)

**Success Criteria**:
- Can create and manage ML models via SQL
- ML_PREDICT works in streaming queries
- OpenAI and Azure OpenAI providers integrated
- Type-safe C# API for model operations

### Phase 3: Advanced Table Features (9-13 weeks)
**Goal**: Complete Table API parity

**Priority**: P1 - High value for Table API users

**Features**:
1. VARIANT Type & JSON Functions (3-4 weeks)
2. Native Table API (4-6 weeks)
3. Process Table Functions (3-4 weeks)

**Success Criteria**:
- Efficient JSON processing with VARIANT type
- Type-safe table transformations in C#
- Stateful UDFs with timer access

### Phase 4: Ecosystem Integration (5-7 weeks)
**Goal**: Better connector and storage integration

**Priority**: P1 - Important for data lake use cases

**Features**:
1. Table Store (Paimon) (3-4 weeks)
2. Unified Source API (2-3 weeks)

**Success Criteria**:
- ACID tables with Apache Paimon
- Modern source connectors with Unified Source API

### Phase 5: Optional Enhancements (As Needed)
**Goal**: Performance and polish

**Priority**: P2/P3 - Nice to have

**Features**:
1. Catalog API (2-3 weeks)
2. Changelog Backend (2-3 weeks)
3. Other P2/P3 features (varies)

## Progress Metrics

### Completion by Priority
- **P0 Features**: 1/7 fully implemented ✅, 1/7 in progress (WI7 Materialized Tables - Investigation)
- **P1 Features**: 0/5 (0%)
- **P2 Features**: 0/4 (0%)
- **P3 Features**: 0/1 (0%)

### Estimated Time Investment
- **Completed**: 2 weeks (Unified Sink API v2 - WI6 ✅)
- **In Progress**: 0 weeks
- **Remaining P0**: 17.5-26 weeks (6 features remaining)
- **Remaining P1**: 15-21 weeks
- **Remaining P2**: 8-11 weeks
- **Remaining P3**: 1.5-2 weeks
- **Total Remaining**: 39-58 weeks (10-14 months)

### Velocity Tracking
*Update as features are completed to track implementation velocity*

| Month | Features Completed | Features Started | Weeks Invested | Velocity (features/week) | Notes |
|-------|-------------------|------------------|----------------|--------------------------|-------|
| Oct 2025 | 1 (Unified Sink API v2) ✅ | 2 (WI6 complete, WI7 started) | 2.0 | 0.50 | WI6: Full implementation (IR + C# API + Java IR Runner, 5 tests). WI7: Investigation started (Materialized Tables) |
| Nov 2025 | 0 | 0 | 0 | - | - |
| Dec 2025 | 0 | 0 | 0 | - | - |

## Next Steps

1. ✅ **Immediate**: Validate current feature coverage (WI5) - COMPLETED
2. ✅ **Short-term**: Create WI for first P0 feature (Unified Sink API v2) - COMPLETED (WI6)
3. ✅ **Investigation & Design**: Complete Investigation and Design phases for WI6 - COMPLETED
4. ✅ **TDD & Implementation**: Implement C# API for Unified Sink v2 - COMPLETED (commits 77ac813, d9f50a0)
5. ✅ **Java IR Runner**: Implement Java IR Runner integration for WI6 - COMPLETED (commit fedc628)
6. ✅ **WI6 Complete**: All phases finished, tests passing, feature ready for production use
7. **Current**: Begin next P0 feature (Materialized Tables or AI/ML Integration)
8. **Long-term**: Continue P0 feature implementation roadmap

## Contributing

Want to help implement features from this TODO?

1. Review the feature documentation linked in checklist
2. Create a Work Item using the template
3. Follow TDD/BDD approach
4. Submit PR and update this tracking file
5. Coordinate with maintainers via GitHub issues

## References

- [TODO/README.md](README.md) - Main TODO overview
- [WI5_todo-implementation-tracking.md](../WIs/WI5_todo-implementation-tracking.md) - This tracking system's WI
- [Apache Flink 2.1 Documentation](https://nightlies.apache.org/flink/flink-docs-master/)
- [FlinkDotNet Features](../docs/features.md)

---

**Note**: This tracking file should be updated after each feature implementation. Keep it synchronized with TODO/README.md.
