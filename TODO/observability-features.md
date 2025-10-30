# TODO: Observability Features

**Status**: Partially Implemented - Needs Comprehensive Testing
**Created**: 2025-10-29
**Apache Flink Version**: All versions (1.0+)
**Related WI**: None yet created

## Overview

FlinkDotNet has basic observability features implemented but lacks comprehensive testing in the release verification process. Observability is critical for production deployments to monitor job health, performance, and troubleshoot issues.

Current implementation exists in:
- Gateway metrics aggregation (`/v1/jobs/{jobId}/metrics`)
- Prometheus integration support
- Grafana dashboards
- LocalTesting validation (basic)

## What Already Exists ✅

### Gateway Metrics API
- **RecordsIn/RecordsOut**: Aggregated from all vertices
- **Parallelism**: Maximum parallelism across vertices
- **Checkpoints**: Completed checkpoint count
- **LastCheckpoint**: Last checkpoint timestamp
- **BackpressureLevel**: Worst backpressure level across vertices

### Prometheus Integration
- Flink Prometheus reporter configuration documented
- Example `flink-conf.yaml` configuration
- Metrics port exposure (9250-9260 range)
- Filter label support

### Grafana Dashboards
- Kafka metrics dashboard (`grafana-kafka-dashboard.json`)
- Dashboard provisioning configuration
- JMX exporter for Kafka metrics

### LocalTesting Coverage
- Basic health checks
- Job submission validation
- Produce/consume validation
- Metrics presence verification

## What's Missing ❌

### Comprehensive Release Verification Testing

**Problem**: Observability features are tested in LocalTesting, but this requires Aspire's internal network which isn't suitable for release package verification.

**Needed**: ReleasePackageVerification tests that validate observability without Aspire dependency.

### Missing Test Coverage

1. **Metrics Accuracy Tests**
   - Validate RecordsIn/RecordsOut counting
   - Verify parallelism aggregation logic
   - Test checkpoint metrics accuracy
   - Validate backpressure level calculation

2. **Prometheus Integration Tests**
   - Verify Prometheus scraping works
   - Test metrics format compatibility
   - Validate label filtering
   - Test multi-port configuration

3. **Grafana Dashboard Tests**
   - Verify dashboard loads correctly
   - Test data source connectivity
   - Validate panel queries
   - Test visualization rendering

4. **End-to-End Observability Tests**
   - Complete workflow: Job submission → Metrics collection → Visualization
   - Test under various load conditions
   - Validate metrics during failures
   - Test recovery scenario metrics

5. **Performance Impact Tests**
   - Measure metrics collection overhead
   - Test high-frequency metric updates
   - Validate memory usage
   - Test scalability with many jobs

## Implementation Requirements

### ReleasePackageVerification Tests Location
Tests should be added to: `ReleasePackagesTesting/ReleasePackagesTesting.IntegrationTests/`

### Test Strategy

**Why ReleasePackageVerification instead of LocalTesting?**
- LocalTesting uses Aspire's internal network for service discovery
- Release package verification tests real-world deployment scenarios
- ReleasePackageVerification tests published NuGet packages
- No Aspire dependency in production deployments

**Test Categories Needed**:

1. **Standalone Observability Tests** (No Aspire)
   ```csharp
   [TestFixture]
   [Category("observability")]
   public class ObservabilityTests
   {
       [Test]
       public async Task MetricsEndpoint_ReturnsJobMetrics()
       {
           // Test Gateway metrics API directly
       }
       
       [Test]
       public async Task PrometheusExporter_ExposesMetrics()
       {
           // Test Prometheus scraping
       }
   }
   ```

2. **Metrics Accuracy Tests**
   ```csharp
   [Test]
   public async Task RecordsInOut_CountsAccurately()
   {
       // Send known number of records
       // Verify metrics match
   }
   ```

3. **Backpressure Tests**
   ```csharp
   [Test]
   public async Task BackpressureLevel_ReflectsActualState()
   {
       // Create backpressure scenario
       // Verify metrics show correct level
   }
   ```

### Infrastructure Requirements

**What's Needed**:
- Standalone Flink cluster (no Aspire)
- Prometheus instance for scraping
- Test job that generates predictable metrics
- HTTP client for metrics API validation

**Test Environment**:
- Docker containers for Flink cluster
- Prometheus in Docker
- Test orchestration via docker-compose
- Cleanup after tests

## Priority Assessment

**Priority**: P1 - High

**Rationale**:
- Observability is critical for production deployments
- Currently only tested in LocalTesting (Aspire-dependent)
- Release verification needs standalone validation
- Users need confidence in metrics accuracy

**Estimated Effort**: 2-3 weeks

**Breakdown**:
- Week 1: Design test infrastructure (Docker setup, test framework)
- Week 2: Implement core observability tests (metrics, Prometheus)
- Week 3: Advanced tests (backpressure, performance, edge cases)

## Success Criteria

- [ ] ReleasePackageVerification has comprehensive observability tests
- [ ] Tests run without Aspire dependency
- [ ] Metrics accuracy validated (±5% tolerance)
- [ ] Prometheus integration verified
- [ ] Backpressure detection validated
- [ ] Performance overhead measured (<5% impact)
- [ ] All tests pass in CI/CD pipeline
- [ ] Documentation updated with test examples

## Integration with Existing Features

### Metrics Sources
```
Flink REST API → Gateway Aggregation → Client Metrics API
                ↓
         Prometheus Reporter → Prometheus Server → Grafana
```

### Test Data Flow
```
Test Job → Generate Metrics → Gateway Collection → Validation
         ↓
    Prometheus Scrape → Verify Format → Assert Expectations
```

## Use Cases Validated by Tests

1. **Production Monitoring**
   - Validate metrics accuracy for dashboards
   - Test alert thresholds
   - Verify metric retention

2. **Troubleshooting**
   - Backpressure detection
   - Checkpoint failure diagnosis
   - Performance bottleneck identification

3. **Capacity Planning**
   - Throughput measurement
   - Resource utilization tracking
   - Scalability validation

## References

- [Observability Documentation](../docs/observability.md)
- [Flink Metrics Documentation](https://nightlies.apache.org/flink/flink-docs-master/docs/ops/metrics/)
- [Prometheus Flink Integration](https://nightlies.apache.org/flink/flink-docs-master/docs/deployment/metric_reporters/#prometheus)
- [Grafana Dashboard Examples](../LocalTesting/grafana-kafka-dashboard.json)

## When to Implement

This should be implemented:
1. ✅ After basic observability features are working (done in LocalTesting)
2. Before releasing observability features to production
3. As part of release verification test suite expansion
4. When users request production observability validation

**Current Status**: Basic implementation exists, comprehensive testing needed for release confidence.

## Implementation Approach

### Phase 1: Test Infrastructure (1 week)
- Set up Docker-based test environment
- Create test orchestration scripts
- Implement test job with predictable metrics
- Establish baseline metrics collection

### Phase 2: Core Metrics Tests (1 week)
- RecordsIn/RecordsOut accuracy tests
- Parallelism aggregation tests
- Checkpoint metrics tests
- Gateway API response validation

### Phase 3: Integration Tests (1 week)
- Prometheus scraping tests
- Backpressure detection tests
- Performance impact tests
- End-to-end workflow tests

### Phase 4: Documentation & CI Integration
- Test documentation
- CI/CD pipeline integration
- Test maintenance guide
- Troubleshooting guide

## Notes

- Observability testing must not depend on Aspire's internal networking
- Tests should use published NuGet packages (release verification)
- Performance tests should measure metrics collection overhead
- Tests should validate real-world production scenarios
