# WI14: LearningCourse Exercise2 Kafka Network Connectivity Fix

**File**: `WIs/WI14_learningcourse-exercise2-kafka-network-fix.md`
**Title**: Fix LearningCourse Exercise2 Kafka connectivity to use correct network addresses
**Description**: Exercise2 BackupAggregator Flink job cannot connect to Kafka because it's using wrong bootstrap servers
**Priority**: High
**Component**: LearningCourse
**Type**: Bug Fix
**Created**: 2025-10-10
**Status**: Investigation Complete - Ready for Implementation

## Problem Statement

Exercise 2 (BackupAggregator) integration test fails with Kafka connectivity issues. TaskManager logs show:
```
Connection to node -1 (localhost/127.0.0.1:9093) could not be established
```

The Flink job running inside containers cannot reach `localhost:9093` because:
1. Container's `localhost` refers to the container itself, not the host machine
2. Flink jobs need internal network address `kafka:9092` for container-to-container communication

## Root Cause Analysis

### Investigation Timeline

1. **Initial hypothesis**: Job definition had explicit `bootstrapServers=localhost:9093`
   - **Finding**: Removed `bootstrapServers` from Exercise2/Program.cs (lines 194, 219)
   - **Result**: Still failed - job still used `localhost:9093`

2. **Second hypothesis**: FlinkJobRunner checking environment variables
   - **Finding**: FlinkJobRunner.java checks `KAFKA_BOOTSTRAP` then defaults to `kafka:9092`
   - **Action**: Removed ALL environment variable checks from FlinkJobRunner.java
   - **Result**: Still failed - job still used `localhost:9093`

3. **Third hypothesis**: Test sets `KAFKA_BOOTSTRAP_SERVERS=localhost:9093` which propagates somehow
   - **Finding**: LearningCourseTestBase.cs sets this for .NET client code
   - **Current status**: Need to determine how this affects Flink job

### Key Code Locations

1. **FlinkJobRunner.java** (lines 93, 291, 227):
   - Now: `orElse(k.bootstrapServers, "kafka:9092")`
   - Priority: JSON field → Default (`kafka:9092`)
   - ✅ Environment variables removed

2. **Exercise2/Program.cs** (lines 194, 219):
   - `bootstrapServers` field omitted from job definition
   - ✅ Allows FlinkJobRunner to use default

3. **LearningCourseTestBase.cs** (line 33):
   - Sets `KAFKA_BOOTSTRAP_SERVERS=localhost:9093` for test environment
   - This is correct for .NET client code (Producer/Consumer)
   - ❓ Question: Does this propagate to Flink job somehow?

### Architecture Understanding

**Dual Network Setup**:
- **External**: `localhost:9093` - Host machine access (for .NET client code)
- **Internal**: `kafka:9092` - Container-to-container (for Flink jobs)

**Components**:
1. **.NET Client Code** (Producer/Consumer in Program.cs):
   - Runs on host machine
   - ✅ Should use `localhost:9093`
   - Gets this from `KAFKA_BOOTSTRAP_SERVERS` environment variable

2. **Flink Job** (FlinkJobRunner JAR running inside TaskManager):
   - Runs inside Docker container
   - ❌ **MUST** use `kafka:9092` (internal network)
   - Should get this from JobDefinition JSON or FlinkJobRunner default

### Remaining Mystery

TaskManager logs still show `localhost:9093`. Possible causes:
1. Gateway might be passing environment variable when submitting JAR
2. Job submission API might inherit environment from submission context
3. Flink might have cached configuration from previous run

## Proposed Solution

### Option 1: Explicit `kafka:9092` in Job Definition (RECOMMENDED)
Add explicit `bootstrapServers` to Exercise2/Program.cs job definition:

```csharp
source = new
{
    type = "kafka",
    topic = InputTopic,
    bootstrapServers = "kafka:9092",  // Explicit internal network address
    groupId = ConsumerGroup,
    startingOffsets = "earliest"
}
```

**Pros**:
- Clear and explicit
- No ambiguity about which address to use
- Matches actual network topology

**Cons**:
- Duplicates configuration
- Less flexible

### Option 2: Clean Environment Before Job Submission
Modify Gateway to NOT pass `KAFKA_BOOTSTRAP_SERVERS` when submitting jobs.

**Pros**:
- Keeps job definition clean
- FlinkJobRunner default works correctly

**Cons**:
- Requires Gateway changes
- Less transparent

### Option 3: Restart Flink Containers
The `localhost:9093` might be cached from previous runs.

**Action**: Restart all Flink containers to clear any cached configuration.

## Implementation Plan

### Step 1: Try Option 3 (Quick Test)
1. Stop all Flink containers
2. Restart LocalTesting AppHost
3. Run Exercise 2 test again
4. Check if problem persists

### Step 2: Implement Option 1 (If Option 3 fails)
1. Add explicit `bootstrapServers = "kafka:9092"` to Exercise2/Program.cs
2. Rebuild and test
3. Document why explicit configuration is needed

### Step 3: Verify Exercise 1 Still Works
- Ensure changes don't break Exercise 1 (StringCapitalize)
- Both exercises should pass

## Success Criteria

✅ Exercise 2 integration test passes
✅ TaskManager logs show connection to `kafka:9092` (not `localhost:9093`)
✅ Backup aggregations are consumed successfully
✅ Exercise 1 continues to work

## Lessons Learned

1. **Container networking is complex**: Always distinguish between host and container network addresses
2. **Environment variables propagate**: Be careful about which environment variables get passed where
3. **Debug first**: Thorough investigation prevents wrong solutions
4. **Test isolation**: Each test should verify its own assumptions about infrastructure

## Next Steps

1. Implement Option 3 (restart containers)
2. If that fails, implement Option 1 (explicit bootstrapServers)
3. Update WI13 with final aggregate operation implementation details
4. Close both WIs when complete