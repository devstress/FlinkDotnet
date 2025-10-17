# WI74: Run LearningCourse Tests One by One and Fix Failures

**File**: `WIs/WI74_run-tests-one-by-one-fix-failures.md`
**Title**: [LearningCourse] Run tests individually and fix root causes
**Description**: Execute each LearningCourse exercise test individually, analyze failures using LocalTesting/test-logs/, and fix root causes
**Priority**: High
**Component**: LearningCourse Integration Tests
**Type**: Investigation + Bug Fix
**Assignee**: AI Agent
**Created**: 2025-01-17
**Status**: In Progress

## Lessons Applied from Previous WIs

### Previous WI References
- WI73: Test validation and fixes (timeout configuration, dynamic memory)
- WI72: Remove absolute maximum timeout
- WI71: Revert Day08 progress monitoring
- WI70: Day15 Flink endpoint fix
- WI68: Day10-12 Kafka acks configuration

### Lessons Applied
- Use 45s no-output timeout (not 20s - too aggressive)
- Debug first using LocalTesting/test-logs/ before proposing solutions
- Dynamic memory allocation prevents crashes on weaker machines
- Sequential test execution prevents resource contention
- Progress-based timeout extension for long-running tests

### Problems Prevented
- Infrastructure crashes from hardcoded memory allocation
- False timeout failures from overly aggressive timeouts
- Parallel test execution causing resource exhaustion

## Phase 1: Investigation

### Requirements
Execute each test individually to identify which exercises are failing and collect debug information from LocalTesting/test-logs/

### Debug Information (MANDATORY - Update this section for every investigation)
**Approach**: Run tests one by one using filter by test name, collect logs after each failure

**Test Execution Command**:
```bash
# Run a specific test by fully qualified name
cd LearningCourse
dotnet test LearningCourse.IntegrationTests/LearningCourse.IntegrationTests.csproj `
  --filter "FullyQualifiedName~Day01Tests.Exercise1_StringCapitalize_ShouldExecuteSuccessfully" `
  --logger "console;verbosity=normal" `
  --configuration Release
```

### Current Test Status from update-LearningCourse.md

**Last Known Results** (from WI73):
- Overall: 51 PASS, 7 FAIL, 2 SKIP out of 60 tests
- Infrastructure: Dynamic memory allocation implemented, sequential execution enabled

**Known Failures**:
1. Day01 Exercise1_StringCapitalize - No messages consumed from output topic
2. Day02 Exercise23 (Observability Dashboard) - No output produced
3. Day04 Exercise44 (Production Deployment) - No output produced
4. Day10 Exercise104 (Throughput Tuning) - Timeout during execution
5. Day13 Exercise134 (CEP Security) - Timeout during multi-job submission
6. Day06 Exercise63 & Exercise64 - Skipped (Temporal workflow issues)

**Test Execution Plan**:
- Start with Day01 to establish baseline
- Execute each test individually
- Collect logs from LocalTesting/test-logs/ after each failure
- Document root cause before moving to next test
- Fix one test at a time to avoid compounding issues

### Findings

**Test: Day01 Exercise1_StringCapitalize** (2025-01-17 03:02:10 UTC)

**Failure**: Kafka connectivity timeout
- Exit code: 1
- Error: "Kafka not ready within 30 seconds"
- Attempted connection to: `127.0.0.1:41455`

**Root Cause Identified**: STALE CONTAINER ENDPOINT DISCOVERY

**Debug Evidence** (from `LocalTesting/test-logs/TestInfrastructure.Debug.log.20251017`):

1. **Line 8 - OLD Kafka container discovered** (31 minutes old from previous test run):
   ```
   96bd795ceb9c   kafka-kutqecaw   Up 31 minutes   127.0.0.1:41455->9092/tcp
   ```

2. **Line 20 - Infrastructure cached stale endpoint**:
   ```
   [DISCOVERY] Kafka host endpoint discovered: 127.0.0.1:41455
   ```

3. **Test output - NEW containers created** (but exercise given old endpoint):
   ```
   5a3257ac8e27   kafka-xuhqggpq   Up 31 seconds   127.0.0.1:36393->9092/tcp
   ```

**Problem Analysis**:
- Static endpoint variables (`KafkaHostBootstrapServers`, etc.) are NOT cleared between test runs
- When GlobalSetUp is called again (e.g., running individual tests), old endpoints remain cached
- Infrastructure discovery sees cached values and doesn't rediscover new container endpoints
- Aspire creates NEW containers with DIFFERENT random ports, but tests use OLD cached endpoints
- Exercise receives old endpoint (41455) but new Kafka is on different port (36393)
- Connection fails because cached endpoint is stale

**Why This Happens**:
```csharp
// LearningCourseTestBase.cs - Static variables retain values between test runs
public static string? KafkaFlinkBootstrapServers { get; private set; }
public static string? KafkaHostBootstrapServers { get; private set; }
// ... these are NEVER cleared in GlobalSetUp!
```

**Discovery Logic**:
```csharp
// Line 493-514: Discovery SKIPS if value already exists
if (flinkIp == null)  // Only discovers if null!
{
    flinkIp = await DockerInfrastructure.GetKafkaContainerIpAsync();
}
```

**Root Cause**: GlobalSetUp does NOT clear static endpoint variables before rediscovery

**Solution Implemented**:
Clear all cached endpoint variables at the START of GlobalSetUp:
```csharp
// Clear any cached endpoint variables from previous test runs
KafkaFlinkBootstrapServers = null;
KafkaHostBootstrapServers = null;
TemporalHostEndpoint = null;
RedisHostEndpoint = null;
PrometheusHostEndpoint = null;
GrafanaHostEndpoint = null;
_isSetupComplete = false;
```

This forces fresh endpoint discovery for each test run, ensuring exercises always get current container ports.

### Lessons Learned
(To be documented after investigation completes)

## Phase 2: Design
(To be completed after investigation identifies root causes)

## Phase 3: Implementation
(To be completed after design phase)

## Phase 4: Testing & Validation
(To be completed after implementation)

## Lessons Learned & Future Reference (MANDATORY)
(To be documented at completion)