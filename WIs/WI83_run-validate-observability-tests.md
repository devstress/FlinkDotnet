# WI83: Run and Validate Prometheus and Grafana Observability Tests

**File**: `WIs/WI83_run-validate-observability-tests.md`
**Title**: [Testing] Run and validate complete observability test suite
**Description**: Execute both Prometheus metrics test and Grafana UI video test to verify complete Day05 observability implementation
**Priority**: High
**Component**: LearningCourse.IntegrationTests
**Type**: Testing
**Assignee**: AI Agent
**Created**: 2025-10-18
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI75: Kafka/Flink metrics export configuration
- WI76: Day05 observability test debugging
- WI77: Prometheus test debugging
- WI78: Flink Prometheus metrics deep dive
- WI79: Prometheus test status checks
- WI80: Kafka container missing investigation
- WI81: Kafka JMX metrics scraping fixes
- WI82: Kafka port mapping with Aspire dynamic ports

### Lessons Applied
- Gateway lifecycle fixes already applied (targetPort, 0.0.0.0 binding, LEARNINGCOURSE env var)
- All 5 Prometheus targets configured and should be UP
- Kafka JMX exporter properly configured with dynamic port mapping
- Grafana anonymous access configured

### Problems Prevented
- Gateway premature shutdown issues (fixed in Program.cs)
- Kafka metrics scraping failures (JMX exporter configured)
- Port mapping issues with Aspire dynamic ports (host.docker.internal used)

## Phase 1: Investigation

### Requirements
1. Run Prometheus metrics test to validate all 5 targets UP
2. Run Grafana UI video test to validate dashboard navigation
3. Document any failures with detailed debugging
4. Verify all acceptance criteria met

### Debug Information (MANDATORY - Update this section for every investigation)
- **Error Messages**: [To be captured from test execution]
- **Log Locations**: LocalTesting/test-logs/FlinkDotNet.JobGateway.log*
- **System State**: .NET 9.0.303, Docker Desktop running, Aspire workload installed
- **Reproduction Steps**: Execute test commands with LEARNINGCOURSE=true environment variable
- **Evidence**: Test output logs, Prometheus target status, Gateway health checks

### Test Execution Plan

#### Test 1: Prometheus Metrics Validation
**Command**:
```bash
cmd /c "set LEARNINGCOURSE=true && dotnet test LearningCourse/LearningCourse.IntegrationTests/LearningCourse.IntegrationTests.csproj --filter FullyQualifiedName~Day05PrometheusMetricsTest.Day05_PrometheusMetricsAreAvailable --configuration Release --logger console;verbosity=detailed"
```

**Expected Results**:
- ✅ All 5 Prometheus targets UP:
  - flink-jobmanager:9250 (Flink JobManager metrics)
  - flink-taskmanager:9251 (Flink TaskManager metrics)
  - kafka-exporter:5556 (Kafka JMX metrics)
  - host.docker.internal:8080 (Gateway metrics)
  - prometheus:9090 (Prometheus self-monitoring)
- ✅ Kafka metrics query returns results
- ✅ Flink metrics query returns numRegisteredTaskManagers >= 1
- ✅ Gateway metrics query returns job submission count >= 0

#### Test 2: Grafana UI Navigation
**Command**:
```bash
cmd /c "set LEARNINGCOURSE=true && dotnet test LearningCourse/LearningCourse.IntegrationTests/LearningCourse.IntegrationTests.csproj --filter FullyQualifiedName~Day05Tests.UIVideoTest_GrafanaDashboard_ShouldNavigateSuccessfully --configuration Release --logger console;verbosity=detailed"
```

**Expected Results**:
- ✅ Grafana anonymous access working (no login page)
- ✅ Dashboard discovery and navigation
- ✅ Data source (Prometheus) connected
- ✅ Flink job status visible
- ✅ Video recording captured (WebM format)

### Findings

**Root Cause Identified**: Aspire configuration error in Program.cs line 370

**Error Message**:
```
System.InvalidOperationException: The endpoint 'gateway-http' for resource 'flink-job-gateway' requested a proxy (IsProxied is true). Non-container resources cannot be proxied when both TargetPort and Port are specified with the same value.
```

**Problem**: The Gateway endpoint configuration had both `port` and `targetPort` set to the same value (`Ports.GatewayHostPort`), which Aspire doesn't allow for non-container resources with proxying enabled.

**Fix Applied**: Removed the `targetPort` parameter from line 370:
- Before: `.WithHttpEndpoint(port: Ports.GatewayHostPort, targetPort: Ports.GatewayHostPort, name: "gateway-http")`
- After: `.WithHttpEndpoint(port: Ports.GatewayHostPort, name: "gateway-http")`

**Status**: Fix applied, LocalTesting solution building, ready to re-run tests

### Lessons Learned

1. **Aspire Proxying Rules**: When using `.AddProject()` for non-container resources, cannot specify both `port` and `targetPort` with same value
2. **Build Requirements**: Changes to AppHost Program.cs require rebuilding LocalTesting solution before tests will use updated configuration
3. **Testing Workflow**: Always verify AppHost starts successfully before assuming test infrastructure issues

## Phase 2: Design
[To be completed if fixes are needed]

## Phase 3: TDD/BDD
[Test specifications already exist in test files]

## Phase 4: Implementation
[To be completed if fixes are needed]

## Phase 5: Testing & Validation
[To be completed after test execution]

## Phase 6: Owner Acceptance
[To be completed after validation]

## Lessons Learned & Future Reference (MANDATORY)
[To be completed at end of task]