# WI78: Flink Prometheus Metrics Export - Deep Dive Debug

**File**: `WIs/WI78_flink-prometheus-metrics-deep-dive.md`
**Title**: Debug Flink Prometheus Metrics Export Failure
**Description**: Despite proper infrastructure configuration (config files, JAR mounting, container setup), Flink is NOT exporting metrics to Prometheus endpoints (9250, 9251, 9252). Need deep dive to identify root cause.
**Priority**: High
**Component**: LearningCourse Integration Tests - Day05 Observability
**Type**: Investigation → Bug Fix
**Assignee**: AI Agent
**Created**: 2025-10-17
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI75: Initial Kafka/Flink metrics export configuration
- WI76: Day05 observability test debugging
- WI77: Prometheus observability test debugging

### Lessons Applied
- Infrastructure is properly configured (config files, volumes, JARs)
- Test framework validation is working
- Problem is specifically with Flink not initializing Prometheus reporter
- Need systematic container-level debugging to find root cause

### Problems Prevented
- Avoided assuming infrastructure issues without evidence
- Avoided modifying test code without understanding Flink behavior
- Avoided architectural changes without root cause analysis

## Phase 1: Investigation

### Requirements
- Verify LEARNINGCOURSE mode is active in test environment
- Check Flink container logs for Prometheus reporter initialization
- Verify config files and JARs are properly mounted inside containers
- Test metrics endpoints directly to determine failure point
- Identify specific error messages or missing components

### Debug Information (MANDATORY - Update this section for every investigation)

#### Step 1: Verify LEARNINGCOURSE Mode and Container Configuration
**Action**: Run test and check container ports
```bash
cd LearningCourse
dotnet test LearningCourse.IntegrationTests/LearningCourse.IntegrationTests.csproj --filter "FullyQualifiedName~Day05Tests.Should_Display_Observability_Dashboard_With_Flink_Metrics" --configuration Release

docker ps --format "table {{.Names}}\t{{.Ports}}"
```

**Expected Results**:
- Test should start LEARNINGCOURSE mode containers
- Should see ports 9250, 9251, 9252 exposed for Flink containers
- JobManager, TaskManager, SQL Gateway containers running

**Actual Results**: [TO BE FILLED]

#### Step 2: Check Flink Container Logs for Prometheus Reporter
**Action**: Examine logs for Prometheus initialization
```bash
# Get container names
docker ps --filter "name=flink" --format "{{.Names}}"

# Check each component's logs
docker logs <jobmanager-container> 2>&1 | Select-String -Pattern "prometheus|reporter|metrics" -Context 2,2
docker logs <taskmanager-container> 2>&1 | Select-String -Pattern "prometheus|reporter|metrics" -Context 2,2
docker logs <sql-gateway-container> 2>&1 | Select-String -Pattern "prometheus|reporter|metrics" -Context 2,2
```

**Look For**:
- "Started PrometheusReporter" or similar success message
- ClassNotFoundException or JAR loading errors
- Port binding errors for 9250, 9251, 9252
- Configuration parsing errors

**Actual Results**:
- Test executed successfully and started LEARNINGCOURSE mode
- Flink job submitted: JobId=1825c1daa6c2b636fd5e69223765bb6b
- Exercise1 completed successfully with 50 messages processed
- **CRITICAL FINDING**: Prometheus query returned "No results found" for `flink_taskmanager_job_task_operator_numRecordsIn`
- System uptime query worked (targets UP: 1, DOWN: 0)
- Containers stopped after test completion (test infrastructure cleanup)
- **KEY ISSUE**: Flink is running but NOT exporting metrics to Prometheus endpoints

#### Step 3: Verify Config File and JAR Mounting
**Action**: Check inside containers for mounted files
```bash
# Check config file content
docker exec <jobmanager-container> cat /opt/flink/conf/flink-conf.yaml

# Check Prometheus JAR exists
docker exec <jobmanager-container> ls -lh /opt/flink/lib/ | Select-String "prometheus"
```

**Expected Results**:
- Config file should contain `metrics.reporters: prom`
- JAR should be listed: `flink-metrics-prometheus-1.21.0.jar`

**Actual Results**: [TO BE FILLED]

#### Step 4: Test Metrics Endpoints Directly
**Action**: Direct HTTP requests to metrics endpoints
```bash
curl http://localhost:9250  # JobManager
curl http://localhost:9251  # TaskManager  
curl http://localhost:9252  # SQL Gateway
```

**Expected Results**:
- Should return Prometheus metrics format (key-value pairs)

**Possible Failures**:
- Connection refused → Port not exposed
- 404 → Service running but no metrics endpoint
- Empty → Reporter not initialized

**Actual Results**: [TO BE FILLED]

#### Step 5: Check Prometheus Targets Status
**Action**: Open http://localhost:9090/targets in browser

**Expected Results**:
- Flink targets (jobmanager, taskmanager, sql-gateway) should be UP (green)
- Last scrape should be successful with recent timestamp

**Actual Results**: [TO BE FILLED]

#### Step 6: Verify Flink Image and Version
**Action**: Check Docker images being used
```bash
docker ps --format "{{.Names}}\t{{.Image}}" | Select-String "flink"
```

**Expected Results**:
- Flink image version should support Prometheus reporter (Flink 1.14+ required)

**Actual Results**: [TO BE FILLED]

#### Step 7: Verify LEARNINGCOURSE Environment Variable
**Action**: Check if testing mode is detected
```bash
docker exec <jobmanager-container> printenv | Select-String "TESTING_MODE"
```

**Expected Results**: Should show `TESTING_MODE=LEARNINGCOURSE`

**Actual Results**: [TO BE FILLED]

### Root Cause Hypotheses

#### Hypothesis 1: Config File Not Being Read
- **Symptoms**: Logs don't show Prometheus configuration being applied
- **Cause**: Flink using different config file location or overridden by env vars
- **Test**: Check Flink startup logs for config file path
- **Likelihood**: Medium

#### Hypothesis 2: Prometheus Reporter JAR Not Loaded
- **Symptoms**: ClassNotFoundException in logs or no reporter initialization
- **Cause**: JAR not in classpath or loaded after Flink startup
- **Test**: Verify JAR exists in /opt/flink/lib/ before Flink starts
- **Likelihood**: High

#### Hypothesis 3: Incorrect Metrics Property Name
- **Symptoms**: Config parsed but reporter not created
- **Cause**: Flink 1.21 uses different property names than documented
- **Test**: Consult Flink 1.21 documentation for correct property syntax
- **Likelihood**: Medium

#### Hypothesis 4: Port Range Not Supported
- **Symptoms**: Single component works but not all three
- **Cause**: Config uses `9250-9252` but Flink expects single port per component
- **Test**: Try separate config files with explicit port assignment
- **Likelihood**: Low

#### Hypothesis 5: Network Connectivity Issue
- **Symptoms**: Endpoints respond but Prometheus can't scrape
- **Cause**: Containers in different Docker networks
- **Test**: Verify all containers in same network
- **Likelihood**: Low

### Findings

**ROOT CAUSE IDENTIFIED**: Environment Variable Mismatch

After analyzing the code in [`LocalTesting.FlinkSqlAppHost/Program.cs`](LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs), I discovered a critical bug:

1. **Line 31**: Checks `LEARNINGCOURSE` environment variable to determine if learning mode is enabled
2. **Line 66**: Checks `TESTING_MODE == "LEARNINGCOURSE"` to determine if metrics should be enabled
3. **Test sets**: `LEARNINGCOURSE=true` (Line 131 in LearningCourseTestBase.cs)

**The Problem**:
- The test infrastructure sets `LEARNINGCOURSE=true`
- The AppHost correctly detects this and deploys Redis/Prometheus/Grafana (Line 31-35)
- BUT the Prometheus metrics configuration checks a DIFFERENT variable: `TESTING_MODE` (Line 66)
- Since `TESTING_MODE` is never set, `isLearningCourseMode` is always `false`
- Therefore, Prometheus metrics are NEVER configured on Flink containers
- Flink runs without metrics reporters → Prometheus gets no data → Test fails with "No results found"

**Evidence**:
```csharp
// Line 31 - THIS variable is checked and works correctly
var isLearningCourse = Environment.GetEnvironmentVariable("LEARNINGCOURSE")?.ToLower() == "true";

// Line 66 - THIS variable is checked for metrics config but is NEVER set
var isLearningCourseMode = Environment.GetEnvironmentVariable("TESTING_MODE") == "LEARNINGCOURSE";
```

**Impact**:
- Flink containers start without Prometheus reporter configuration
- No metrics JARs are mounted
- No metrics ports (9250-9252) are exposed
- Prometheus cannot scrape any Flink metrics
- Tests fail with "No results found" error

**Why This Wasn't Caught Earlier**:
- Infrastructure appears to work (containers start, Prometheus/Grafana deploy)
- Only the metrics export functionality is silently disabled
- No error messages since it's just a conditional feature enable

### Lessons Learned
[TO BE FILLED AFTER ROOT CAUSE IDENTIFIED]

## Phase 2: Design

### Solution: Fix Environment Variable Check

**Change Required**: Update [`Program.cs`](LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs:66) to use the correct environment variable.

**Option 1 (Recommended)**: Use `LEARNINGCOURSE` consistently everywhere
```csharp
// Line 66 - Change from:
var isLearningCourseMode = Environment.GetEnvironmentVariable("TESTING_MODE") == "LEARNINGCOURSE";

// To:
var isLearningCourseMode = Environment.GetEnvironmentVariable("LEARNINGCOURSE")?.ToLower() == "true";
```

**Option 2**: Set both variables in test infrastructure
```csharp
// In LearningCourseTestBase.cs GlobalSetUp:
Environment.SetEnvironmentVariable("LEARNINGCOURSE", "true");
Environment.SetEnvironmentVariable("TESTING_MODE", "LEARNINGCOURSE");  // Add this line
```

**Recommendation**: Use Option 1 (single source of truth)
- Simpler and less error-prone
- Consistent with rest of codebase
- Already used on Line 31 for the same purpose
- Eliminates duplicate environment variable management

### Architecture Decision

**Why This Bug Occurred**:
- Code evolution: Originally used `TESTING_MODE`, later changed to `LEARNINGCOURSE`
- Incomplete refactoring: Not all references were updated
- No validation: No checks to ensure environment variables are correctly set

**Prevention Strategy**:
1. Use a single, well-documented environment variable
2. Create a helper method for checking learning course mode
3. Add validation to log which mode is detected
4. Add integration test to verify metrics endpoints are accessible

## Phase 3: TDD/BDD

### Test Specifications

**Verification Test**: After applying fix, verify metrics are exported
```csharp
[Test]
public async Task VerifyFlinkMetricsExportedInLearningCourseMode()
{
    // Arrange: Ensure LEARNINGCOURSE mode is active
    Assert.That(Environment.GetEnvironmentVariable("LEARNINGCOURSE"), Is.EqualTo("true"));
    
    // Act: Wait for Flink to start and metrics to be available
    await Task.Delay(5000);
    
    // Assert: Verify each Flink component exposes metrics
    var jobManagerMetrics = await GetAsync("http://localhost:9250");
    var taskManagerMetrics = await GetAsync("http://localhost:9251");
    var sqlGatewayMetrics = await GetAsync("http://localhost:9252");
    
    Assert.That(jobManagerMetrics, Does.Contain("flink_"));
    Assert.That(taskManagerMetrics, Does.Contain("flink_"));
    Assert.That(sqlGatewayMetrics, Does.Contain("flink_"));
}
```

**Regression Test**: Ensure regular mode doesn't break
```csharp
[Test]
public async Task VerifyFlinkWorksWithoutMetricsInProductionMode()
{
    // Arrange: Ensure LEARNINGCOURSE mode is NOT active
    Environment.SetEnvironmentVariable("LEARNINGCOURSE", null);
    
    // Act: Start infrastructure
    await GlobalSetUp();
    
    // Assert: Flink works but ports 9250-9252 are not exposed
    var flinkHealth = await GetAsync("http://localhost:8080/api/v1/health");
    Assert.That(flinkHealth, Is.Not.Null);
    
    // Metrics ports should not be accessible
    Assert.ThrowsAsync<HttpRequestException>(() => GetAsync("http://localhost:9250"));
}
```

## Phase 4: Implementation

### Code Changes Required

**File**: [`LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs`](LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs:66)

```csharp
// BEFORE (Line 66):
var isLearningCourseMode = Environment.GetEnvironmentVariable("TESTING_MODE") == "LEARNINGCOURSE";

// AFTER:
var isLearningCourseMode = Environment.GetEnvironmentVariable("LEARNINGCOURSE")?.ToLower() == "true";
```

**Validation**: Add logging to confirm mode detection
```csharp
// Add after Line 67:
if (isLearningCourseMode)
{
    Console.WriteLine("✅ Prometheus metrics ENABLED for Flink components (ports 9250-9252)");
}
else
{
    Console.WriteLine("ℹ️  Prometheus metrics DISABLED (production mode)");
}
```

### Expected Behavior After Fix

**With LEARNINGCOURSE=true**:
- Flink JobManager exposes metrics on port 9250
- Flink TaskManager exposes metrics on port 9251
- Flink SQL Gateway exposes metrics on port 9252
- Prometheus JAR mounted to all Flink containers
- Config file with metrics reporter mounted
- Prometheus successfully scrapes metrics
- Test passes with actual metric values

**Without LEARNINGCOURSE** (production mode):
- No metrics ports exposed
- No Prometheus JAR mounted
- No metrics configuration
- Smaller container footprint
- Faster startup (no metrics overhead)

## Phase 5: Testing & Validation

### Validation Steps

1. **Apply the fix** to Program.cs
2. **Clean build** to ensure changes are compiled
3. **Run the test** that was failing
4. **Verify metrics endpoints** respond with Flink metrics
5. **Check Prometheus targets** show all Flink components as UP
6. **Confirm test passes** with actual metric values extracted

### Success Criteria

- ✅ Test `UIVideoTest_PrometheusMetrics_ShouldNavigateSuccessfully` passes
- ✅ Prometheus query returns actual metric values (not "No results found")
- ✅ All three Flink components export metrics (JobManager, TaskManager, SQL Gateway)
- ✅ Ports 9250, 9251, 9252 are accessible and return Prometheus format data
- ✅ No regression in production mode (metrics disabled when LEARNINGCOURSE not set)

## Phase 6: Owner Acceptance
[TO BE COMPLETED AFTER TESTING]

## Lessons Learned & Future Reference (MANDATORY)

### What Worked Well
- Systematic code analysis identified the root cause quickly
- Clear separation of concerns (LEARNINGCOURSE mode vs production)
- Infrastructure design was correct, just environment variable mismatch

### What Could Be Improved
- **Environment Variable Management**: Need centralized, validated approach
- **Code Review**: Should catch inconsistent environment variable names
- **Testing**: Should have integration test to verify metrics endpoints are accessible
- **Documentation**: Should document which environment variables control which features

### Key Insights for Similar Tasks
- **Always check environment variable names for consistency** across entire codebase
- **Log environment detection** so issues are visible in logs
- **Add validation tests** for conditional features
- **Use helper methods** instead of inline environment variable checks

### Specific Problems to Avoid in Future
1. **Multiple environment variables for same purpose** (TESTING_MODE vs LEARNINGCOURSE)
2. **Incomplete refactoring** when changing variable names
3. **Silent feature disabling** with no error messages
4. **Lack of validation** for environment-dependent behavior

### Reference for Future WIs
**When debugging "feature not working" issues**:
1. Check if feature is conditionally enabled
2. Verify environment variables are correctly set
3. Trace feature flag checks through entire codebase
4. Add logging to make feature detection visible
5. Create tests that verify conditional features work correctly

**Environment Variable Best Practices**:
- Use a single, clearly named variable
- Create constants or enums for variable names
- Add validation and logging for detection
- Document in README which variables control which features
- Add integration tests for environment-dependent features