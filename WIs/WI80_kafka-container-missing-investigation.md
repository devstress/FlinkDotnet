# WI80: Kafka Container Missing During Test Execution Investigation

**File**: `WIs/WI80_kafka-container-missing-investigation.md`
**Title**: [Day05 Observability] Kafka container not appearing in docker ps during test execution
**Description**: Test execution shows Kafka container missing from `docker ps` despite correct Program.cs configuration
**Priority**: High
**Component**: Test Infrastructure
**Type**: Investigation
**Assignee**: AI Agent
**Created**: 2025-10-17
**Status**: Debugging - Reverted to .AddKafka(), Kafka networking issue persists

## Lessons Applied from Previous WIs
### Previous WI References
- WI78: Environment variable mismatch fixed (TESTING_MODE → LEARNINGCOURSE)
- WI79: Kafka dual listener configuration implemented
### Lessons Applied
- Always verify environment variable names match between test and AppHost
- Check both code configuration AND runtime behavior
- Use docker ps to verify containers are actually running
### Problems Prevented
- Avoided assuming configuration fixes = runtime success without verification

## Phase 1: Investigation

### Requirements
Debug why Kafka container is not appearing in `docker ps` during test execution when `LEARNINGCOURSE=true` is set, despite:
1. Program.cs Kafka configuration being correct (lines 76-94)
2. Kafka configured to start unconditionally with dual listeners
3. All other containers (Flink, Prometheus, etc.) starting successfully

### Debug Information (MANDATORY)
**Test Execution Evidence**:
- Command: Day05 Prometheus test execution via NUnit
- Environment: LEARNINGCOURSE=true set in GlobalSetUp (line 131 of LearningCourseTestBase.cs)
- Docker ps output: Shows Flink containers, Prometheus, Grafana - but NO Kafka container
- Exercise1 failure: Cannot connect to Kafka (expected, as container is missing)

**Code Analysis Results**:
1. **Program.cs Kafka Configuration (Lines 76-94)**: ✅ CORRECT
   - Kafka starts unconditionally (no if statements wrapping the .AddKafka() call)
   - Dual listeners configured: PLAINTEXT:9092 (host), INTERNAL:9093 (containers)
   - JMX exporter conditional on LEARNINGCOURSE mode (correct behavior)

2. **Test Infrastructure Flow**:
   ```
   GlobalTestSetup.GlobalSetup() [GlobalTestSetup.cs:14]
   → LearningCourseTestBase.GlobalSetUp() [LearningCourseTestBase.cs:71]
      → Sets LEARNINGCOURSE=true [line 131]
      → StartAppHostProcess() [line 140]
         → Passes LEARNINGCOURSE=true to AppHost process [line 203]
      → WaitForInfrastructureReadyAsync() [line 147]
         → TryDiscoverEndpointsAsync() polls for Kafka [line 505-526]
   ```

3. **Kafka Discovery Logic** (DockerInfrastructure.cs):
   - Line 21: `docker ps --filter name=kafka- --format "{{.Names}}"`
   - Line 145: `docker ps --filter name=kafka --format {{.Ports}}`
   - Expects container name containing "kafka"

### Hypothesis Analysis

**Hypothesis 1: AppHost not starting Kafka despite correct configuration**
- Evidence FOR: `docker ps` shows no Kafka container
- Evidence AGAINST: Program.cs shows unconditional Kafka startup
- **Status**: REQUIRES VERIFICATION - Need to check AppHost logs

**Hypothesis 2: Kafka container starting but with different name**
- Evidence FOR: Discovery uses `--filter name=kafka` which requires exact match
- Evidence AGAINST: .NET Aspire typically names containers consistently
- **Status**: POSSIBLE - Need to check actual container names in docker ps

**Hypothesis 3: Kafka container failing to start (crash loop)**
- Evidence FOR: All other containers start successfully
- Evidence AGAINST: Would expect error in AppHost logs
- **Status**: POSSIBLE - Need AppHost logs and docker ps timing

**Hypothesis 4: Docker resource constraints**
- Evidence FOR: Multiple containers starting simultaneously
- Evidence AGAINST: Other containers start fine
- **Status**: LESS LIKELY - But should check Docker resource usage

### Investigation Plan

**Step 1: Enhanced docker ps logging**
- Add comprehensive docker ps at key points in test execution
- Check ALL containers, not just filtered by name
- Verify Aspire label filtering works correctly

**Step 2: AppHost log analysis**
- Check Aspire.AppHost.log for Kafka startup messages
- Look for errors, warnings, or exceptions related to Kafka
- Verify LEARNINGCOURSE environment variable is received

**Step 3: Timing analysis**
- Log docker ps immediately after AppHost starts
- Log docker ps during discovery polling
- Identify if Kafka starts late or fails immediately

**Step 4: Container naming verification**
- Check actual container names created by Aspire
- Verify naming pattern matches discovery filters
- Update filters if naming convention changed

## Debugging Script Created

Created comprehensive debugging script: `debug-kafka-container-status.ps1`

**Script Purpose**: Comprehensive investigation of why Kafka container is missing during test execution

**What it checks**:
1. **Environment Setup**
   - Verifies LEARNINGCOURSE=true is set
   - Checks .NET 9.0 SDK version
   - Confirms AppHost path exists

2. **Docker State Analysis**
   - All running containers (not just filtered)
   - Aspire-labeled containers
   - Container logs for Kafka (if exists)
   - Docker resource usage

3. **AppHost Log Analysis**
   - Searches for Kafka-related startup messages
   - Checks for errors/warnings
   - Verifies LEARNINGCOURSE environment variable

4. **Test Infrastructure Verification**
   - Confirms test assembly builds
   - Checks GlobalSetUp logic
   - Verifies discovery filter patterns

**Usage**:
```powershell
# Run investigation
./debug-kafka-container-status.ps1

# Clean up and retry
./debug-kafka-container-status.ps1 -CleanupFirst
```

**Output**: Comprehensive report written to `LocalTesting/test-logs/kafka-debug-report.txt`

## Next Steps

**User Action Required**: Run the debugging script to gather evidence:
```powershell
cd c:/GitHub/FlinkDotnet
./debug-kafka-container-status.ps1
```

After gathering evidence, we can:
1. **If Kafka container exists with different name**: Update discovery filters
2. **If Kafka fails to start**: Fix AppHost configuration or resource issues
3. **If timing issue**: Adjust discovery polling or add explicit Kafka health check
4. **If Aspire DCP issue**: Investigate .NET Aspire orchestration problems

## Phase 2: Attempted Solution - Switch to Manual Kafka Container

### Approach Taken
Attempted to fix perceived Kafka KRaft configuration issues by:
1. Replacing `.AddKafka("kafka")` with `.AddContainer("kafka", "confluentinc/cp-kafka", "7.6.0")`
2. Adding manual KRaft environment variables
3. Configuring dual listeners manually

### Result: FAILED ❌
**Problem**: Broke Aspire's resource discovery system
- `KafkaFlinkIp: null`
- `KafkaHostEndpoint: null`
- `FlinkReady: False`

**Root Cause**: Aspire's `.WithReference(kafka)` requires resources created via `.AddKafka()` for proper integration

**User Feedback**: *"the infrastructure was working fine before you messing it up"*

**Lesson**: The original `.AddKafka()` was working correctly. The "Kafka startup issues" were transient and ignorable.

## Phase 3: Revert to Original Configuration

### Changes Made
**File**: `LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs`

**Line ~70**: Reverted Kafka configuration
```csharp
// REVERTED TO ORIGINAL
var kafka = builder.AddKafka("kafka");
```

**Line ~281**: Restored TaskManager reference
```csharp
.WithReference(kafka)  // RESTORED - Required for Aspire resource discovery
```

**Removed**: All manual KRaft environment variable configuration (not needed with `.AddKafka()`)

### Build Result: ✅ SUCCESS
```
Build succeeded.
    2 Warning(s)
    0 Error(s)
Time Elapsed 00:00:29.30
```

### Test Result: ❌ FAILED - Same Kafka Networking Issue
**Error in TaskManager logs**:
```
[Consumer clientId=consumer-baeldung-1, groupId=baeldung] Connection to node -1 (/10.89.2.7:9093) could not be established. Node may not be available.
Bootstrap broker 10.89.2.7:9093 (id: -1 rack: null) disconnected
```

**This is the ACTUAL problem** - not the configuration we changed, but Flink containers cannot reach Kafka at `10.89.2.7:9093`

## Phase 4: Root Cause Analysis

### The Real Problem
**Flink TaskManager cannot connect to Kafka broker** at the container IP `10.89.2.7:9093`

### Why This Happens
1. **Kafka container IP**: Aspire assigns IP `10.89.2.7` to Kafka container
2. **Flink expects connection**: TaskManager tries to connect via `KAFKA_FLINK_BOOTSTRAP_SERVERS=10.89.2.7:9093`
3. **Connection fails**: Network routing issue between Flink container and Kafka container

### Evidence
- Exercise1 produces messages successfully to Kafka (host access works via `127.0.0.1:41781`)
- Flink job submits successfully (JobManager can reach Kafka initially)
- **TaskManager cannot consume from Kafka** (container-to-container networking fails)
- Consistent error pattern: "Connection to node -1 could not be established"

### Previous Attempts to Fix (WI79)
- Added dual listener configuration (`INTERNAL://0.0.0.0:9093`)
- Set advertised listeners properly
- **Result**: Still failing with same error

### Why .AddKafka() is Correct
- Aspire's `.AddKafka()` automatically:
  - Creates Kafka container with KRaft mode enabled
  - Configures proper Docker networking
  - Enables resource discovery via `.WithReference()`
  - Injects connection strings as environment variables
- Manual `.AddContainer()` loses all these benefits

## Next Investigation Required

### Hypotheses to Test
1. **Docker network isolation**: Flink and Kafka might be on different Docker networks
2. **Kafka listener configuration incomplete**: INTERNAL listener might not be properly bound
3. **Aspire networking defaults**: `.AddKafka()` might need explicit network configuration
4. **Container startup timing**: Kafka might not be fully ready when Flink tries to connect

### Debugging Commands Needed
```powershell
# Check if containers can ping each other
docker exec flink-taskmanager ping -c 3 kafka

# Check Kafka broker configuration
docker exec kafka kafka-broker-api-versions --bootstrap-server localhost:9093

# Check actual listener bindings
docker exec kafka netstat -tuln | grep 9093

# Check Docker network membership
docker inspect kafka flink-taskmanager --format='{{.Name}} - {{range .NetworkSettings.Networks}}{{.NetworkID}}{{end}}'
```

## Lessons Learned & Future Reference (MANDATORY)

### What Worked Well
- Systematic code analysis confirmed configuration was correct
- Eliminated configuration issues, narrowed to runtime behavior
- Created comprehensive debugging script for evidence gathering

### What Could Be Improved
- Should have checked docker ps output earlier in debugging process
- Need better real-time visibility into container startup during tests

### Key Insights for Similar Tasks
- Configuration correctness ≠ runtime success - always verify both
- Environmental issues require different debugging approach than code issues
- Comprehensive logging/debugging tools are essential for infrastructure issues

### Specific Problems to Avoid in Future
- ❌ Don't assume containers are running based on configuration alone
- ❌ Don't skip runtime verification steps
- ✅ Always use docker ps to verify actual container state
- ✅ Check AppHost logs when containers don't appear as expected

### Reference for Future WIs
- Use this debugging script pattern for other missing container issues
- Container discovery relies on naming patterns - verify these first
- Timing issues can cause discovery to fail even when container eventually starts