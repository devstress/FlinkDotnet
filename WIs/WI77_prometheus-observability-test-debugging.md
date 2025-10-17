# WI77: Debug and Fix Prometheus Observability Test

**File**: `WIs/WI77_prometheus-observability-test-debugging.md`
**Title**: [Testing] Debug Prometheus Observability Test After Port Conflict Fixes
**Description**: Execute observability test to verify it properly fails on empty metrics or passes with real data after port conflict fixes and Prometheus configuration updates
**Priority**: High
**Component**: LearningCourse Integration Tests
**Type**: Debug
**Assignee**: AI Agent
**Created**: 2025-10-17
**Status**: Investigation

## Lessons Applied from Previous WIs

### Previous WI References
- WI74: Fixed test assertions to detect empty metrics and require values > 0
- WI75: Implemented conditional metrics export for LEARNINGCOURSE mode
- WI76: Fixed stale port caching issue in GlobalSetUp()
- Port conflict fixes: JobManager:9250, TaskManager:9251, SQL Gateway:9252
- Prometheus config updates in prometheus-learningcourse.yml

### Lessons Applied
- Test must fail immediately when metrics are empty (assertions working)
- Test must pass only when real metric values > 0 are received
- Port conflicts can cause metrics export failures
- Prometheus configuration must match actual container ports

### Problems Prevented
- Silent test passes with empty data (WI74 fixed this)
- Port collisions preventing metrics export (WI76 fixed this)
- Incorrect Prometheus scrape targets (updated in configuration)

## Phase 1: Investigation

### Requirements
- Execute Day05 observability test
- Verify test behavior with current infrastructure state
- Determine if test fails properly on empty metrics OR passes with real data
- Collect diagnostic information for next steps

### Debug Information (MANDATORY - Update this section for every investigation)

#### Pre-Test Environment Verification
```bash
# Verify .NET version
dotnet --version  # Expected: 9.0.x

# Check current directory
pwd  # Expected: c:\GitHub\FlinkDotnet

# Verify test project exists
Test-Path LearningCourse/LearningCourse.IntegrationTests/Day05Tests.cs
```

#### Test Execution Command
```bash
cd LearningCourse
dotnet test LearningCourse.IntegrationTests/LearningCourse.IntegrationTests.csproj \
  --filter "FullyQualifiedName~Day05Tests.Should_Display_Observability_Dashboard_With_Flink_Metrics" \
  --configuration Release \
  --logger "console;verbosity=detailed"
```

#### Expected Test Outcomes

**Scenario 1: Test Fails with Empty Metrics (Assertions Working)**
- Error message: "Prometheus query returned no results" or similar
- Test fails immediately when detecting empty data
- Proves test assertions are functioning correctly

**Scenario 2: Test Passes with Real Data (Infrastructure Working)**
- Test passes with metric values > 0
- Console shows extracted metric values
- Proves Flink/Kafka metrics are being collected

#### Diagnostic Commands for Failure Analysis

**Check Flink Container Logs:**
```bash
docker logs <flink-jobmanager-container> 2>&1 | Select-String "prometheus"
docker logs <flink-taskmanager-container> 2>&1 | Select-String "prometheus"
```

**Verify Prometheus Scraping:**
- Access Prometheus UI: http://localhost:9090
- Check "Status" → "Targets" for endpoint status
- Verify ports: jobmanager:9250, taskmanager:9251, gateway:9252

**Test Metric Endpoints:**
```bash
curl http://localhost:9250  # JobManager metrics
curl http://localhost:9251  # TaskManager metrics
curl http://localhost:9252  # SQL Gateway metrics
```

**Check Port Bindings:**
```bash
docker ps --format "table {{.Names}}\t{{.Ports}}"
```

### Root Cause Investigation Areas

If test fails with empty metrics:
1. **Prometheus Reporter Not Starting**: JAR loading errors in Flink logs
2. **Network Connectivity**: Prometheus cannot reach Flink containers
3. **Scrape Configuration**: Syntax errors in prometheus-learningcourse.yml
4. **Timing Issues**: Metrics need more time to accumulate
5. **LEARNINGCOURSE Mode**: TESTING_MODE environment variable not set

### Findings

#### Test Execution Results (2025-10-17 16:15)

**✅ SUCCESS: Test assertions are working correctly!**

The test failed immediately when metrics were empty, which is the **expected and desired behavior**:

```
❌ PROMETHEUS QUERY RETURNED NO RESULTS
System.InvalidOperationException : Prometheus query returned 'No results found'.
Metrics are not being collected or scraped.
```

**Test Flow:**
1. ✅ Exercise1 started successfully - Flink job submitted (JobId: 3f0cf83568f75e0955c0709969b895b4)
2. ✅ Prometheus UI accessible (http://127.0.0.1:45545)
3. ✅ Query interface detected and working
4. ✅ System uptime query succeeded (1 target UP)
5. ❌ Flink metrics query returned "No results found" - **TEST FAILED AS EXPECTED**

**Key Evidence:**
- Exercise1 processed 50 messages successfully (input → Flink → output)
- Flink job was RUNNING and completed successfully
- Prometheus is accessible and responding
- System `up` metric returned data (Targets UP: 1)
- Flink-specific metrics (`flink_taskmanager_job_task_operator_numRecordsIn`) returned empty

**Root Cause Analysis:**
The issue is **NOT with the test** - the test is working perfectly. The issue is that **Flink metrics are not being exported to Prometheus** despite:
- Flink job running successfully
- Messages being processed
- Prometheus being accessible
- Port configurations updated (JobManager:9250, TaskManager:9251, SQL Gateway:9252)

### Root Cause Investigation

Based on test results, the problem is in the **metrics export chain**:

1. **Flink Job Execution**: ✅ Working (50 messages processed successfully)
2. **Prometheus Server**: ✅ Working (accessible, responding to queries)
3. **Metrics Export**: ❌ BROKEN (Flink metrics not reaching Prometheus)

**Possible Root Causes:**

1. **Prometheus Reporter JAR Not Loaded**
   - The `flink-metrics-prometheus-1.21.0.jar` may not be in the Flink container
   - Or JAR is present but not registered with Flink

2. **Metrics Reporter Configuration Not Applied**
   - Environment variables for LEARNINGCOURSE mode may not be setting metrics configuration
   - Flink `metrics.reporters` configuration may not be active

3. **Network Connectivity Issue**
   - Prometheus cannot reach Flink containers despite port mappings
   - Scrape targets may be misconfigured

4. **Conditional Metrics Export Logic**
   - WI75 implemented conditional export for LEARNINGCOURSE mode
   - Logic may not be triggering correctly

5. **Timing Issue**
   - Metrics may need more time to accumulate after job completion
   - Test may be querying too soon after job submission

### Lessons Learned

1. **Test Design Success**: The test correctly detects empty metrics and fails immediately - this is proper test behavior
2. **Assertion Quality**: The test assertions from WI74 are working as designed
3. **Infrastructure Separation**: The problem is infrastructure (metrics export), not test logic
4. **Diagnostic Value**: Test provided clear failure point (Flink metrics query) for targeted debugging

## Phase 2: Next Steps - Infrastructure Debugging

### Investigation Required

Now that we've confirmed the test assertions work correctly, we need to debug why Flink metrics are not being exported to Prometheus.

### Debugging Steps (When Infrastructure is Running)

1. **Check Flink Container Configuration**
   ```bash
   # Check if Prometheus JAR is present
   docker exec <flink-jobmanager-container> ls -la /opt/flink/lib/ | grep prometheus
   docker exec <flink-taskmanager-container> ls -la /opt/flink/lib/ | grep prometheus
   ```

2. **Check Flink Configuration**
   ```bash
   # Verify metrics.reporters configuration
   docker exec <flink-jobmanager-container> cat /opt/flink/conf/flink-conf.yaml | grep -A 10 metrics
   ```

3. **Check Flink Logs for Prometheus Reporter**
   ```bash
   # Look for Prometheus reporter initialization
   docker logs <flink-jobmanager-container> 2>&1 | grep -i prometheus
   docker logs <flink-taskmanager-container> 2>&1 | grep -i prometheus
   ```

4. **Test Metric Endpoints Directly**
   ```bash
   curl http://localhost:9250  # JobManager metrics
   curl http://localhost:9251  # TaskManager metrics
   curl http://localhost:9252  # SQL Gateway metrics
   ```

5. **Check Prometheus Targets**
   - Navigate to http://localhost:9090/targets
   - Verify all Flink endpoints are listed
   - Check if they show as UP or DOWN

### Recommended Fixes

Based on the root cause investigation, likely fixes include:

1. **If Prometheus JAR is Missing**:
   - Add `flink-metrics-prometheus-1.21.0.jar` to Flink Docker image
   - Update Dockerfile or docker-compose to include JAR

2. **If Configuration is Missing**:
   - Ensure LEARNINGCOURSE mode sets these environment variables:
     ```
     metrics.reporters: prom
     metrics.reporter.prom.factory.class: org.apache.flink.metrics.prometheus.PrometheusReporterFactory
     metrics.reporter.prom.port: 9250 (JobManager) or 9251 (TaskManager)
     ```

3. **If Network Connectivity Issue**:
   - Verify Prometheus can reach Flink containers on the network
   - Check prometheus-learningcourse.yml has correct container names/IPs

4. **If Conditional Logic Issue**:
   - Review WI75 implementation
   - Ensure TESTING_MODE=LEARNINGCOURSE triggers metrics export

### Success Metrics for Fix Verification

When fixed, the test should:
- ✅ Connect to Prometheus successfully
- ✅ Query `flink_taskmanager_job_task_operator_numRecordsIn`
- ✅ Extract metric values > 0
- ✅ PASS the test with real data

## Success Criteria

### ✅ ACHIEVED: Test Behavior Validation
- ✅ Test demonstrates proper failure behavior on empty metrics
- ✅ Clear diagnostic output showing "No results found"
- ✅ Test assertions from WI74 are working correctly
- ✅ Action plan established for infrastructure debugging

### 🔄 PENDING: Infrastructure Fix
- ⏳ Debug why Flink metrics are not exported
- ⏳ Fix metrics export configuration
- ⏳ Re-run test to verify it passes with real data

**CRITICAL SUCCESS**: Test MUST fail when metrics are empty ✅ **VERIFIED**

The test is working perfectly - the infrastructure needs fixing, not the test.