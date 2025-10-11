# WI19: Fix LearningCourse Kafka Connectivity with Container IP Discovery

**File**: `WIs/WI19_fix-learningcourse-kafka-container-ip-discovery.md`
**Title**: Fix LearningCourse tests by implementing Kafka container IP discovery
**Description**: Implement Kafka container IP discovery in LearningCourse following LocalTesting's proven pattern to fix TaskManager connectivity issues
**Priority**: High
**Component**: LearningCourse Test Infrastructure
**Type**: Bug Fix
**Created**: 2025-10-10
**Status**: Complete

## Lessons Applied from Previous WIs
### Previous WI References
- WI18: Logging infrastructure revealed root cause - exercises hardcode `kafka:9092` which fails in Docker bridge network
- LocalTesting tests pass because they discover Kafka container IP and pass it to Flink jobs

### Lessons Applied
- Docker bridge network does NOT support DNS resolution between containers
- Must use actual container IP addresses (e.g., `172.17.0.2:9093`) for container-to-container communication
- LocalTesting's pattern of discovering container IPs works reliably
- Create common/shared infrastructure to avoid code duplication across test projects

### Problems Prevented
- No more hardcoded addresses that break in Docker environments
- Consistent pattern across all LearningCourse exercises
- Reusable infrastructure for future exercises

## Phase 1: Investigation

### Requirements
Fix LearningCourse Day01 tests by:
1. Creating common infrastructure project for shared test utilities
2. Implementing Kafka container IP discovery like LocalTesting
3. Updating LearningCourseTestBase to discover and pass container IP
4. Updating Exercise1 and Exercise2 to use discovered addresses

### Root Cause (From WI18 Investigation)
**Exercise1 hardcodes `kafka:9092`**:
- Docker bridge network doesn't resolve DNS names between containers
- Kafka broker redirects from `kafka:9092` to `localhost:9093` via advertised listeners
- TaskManager cannot reach `localhost:9093` from inside container
- Results in: `Connection to node 1 (localhost/127.0.0.1:9093) could not be established`

**LocalTesting works because**:
- Discovers actual Kafka container IP (e.g., `172.17.0.2:9093`)
- Passes discovered IP to Flink jobs
- TaskManager can reach Kafka via Docker bridge network using IP address

## Phase 2: Design

### Architecture Decisions

**1. Create Common Test Infrastructure Project**
- **Project**: `LearningCourse/LearningCourse.Common/`
- **Purpose**: Shared utilities for all LearningCourse test projects
- **Contents**:
  - Kafka container IP discovery
  - Docker command execution helpers
  - Common test infrastructure patterns
- **Why**: Avoid code duplication across Day01, Day02, etc.

**2. Port LocalTesting's Discovery Methods**
From `GlobalTestInfrastructure.cs`:
- `GetKafkaContainerIpAsync()` - Discovers Kafka container IP
- `RunDockerCommandAsync()` - Executes Docker commands
- Helper methods for IP extraction

**3. Update LearningCourseTestBase**
- Add Kafka container IP discovery in GlobalSetUp()
- Set `KAFKA_FLINK_BOOTSTRAP_SERVERS` environment variable with discovered IP
- Keep `KAFKA_BOOTSTRAP_SERVERS` for host-to-Kafka communication (exercises' own producers/consumers)

**4. Update Exercises**
- Exercise1: Read `KAFKA_FLINK_BOOTSTRAP_SERVERS` for Flink job submission
- Exercise2: Read `KAFKA_FLINK_BOOTSTRAP_SERVERS` for Flink job submission
- Keep existing `KAFKA_BOOTSTRAP_SERVERS` usage for host-side Kafka operations

### Why This Approach
1. **Proven Pattern**: LocalTesting uses this successfully
2. **Reusable**: Common project benefits all future exercises
3. **Clear Separation**: Different addresses for host vs container contexts
4. **Maintainable**: Centralized infrastructure code

## Phase 3: Implementation

### Code Changes

**1. Created Common Infrastructure Project** ✅
- Created `LearningCourse/LearningCourse.Common/LearningCourse.Common.csproj`
- Added Docker.DotNet package reference for container inspection
- Targets .NET 9.0 framework

**2. Ported Discovery Methods to Common Project** ✅
- Created `LearningCourse/LearningCourse.Common/DockerInfrastructure.cs`
- Ported `GetKafkaContainerIpAsync()` from LocalTesting
- Ported `RunDockerCommandAsync()` for Docker command execution
- Added timeout and error handling

**3. Updated LearningCourseTestBase** ✅
- Added reference to `LearningCourse.Common` project
- Modified `GlobalSetUp()` to discover Kafka container IP
- Set `KAFKA_FLINK_BOOTSTRAP_SERVERS` environment variable with `{containerIp}:9093`
- Kept `KAFKA_BOOTSTRAP_SERVERS=localhost:9093` for host-side operations
- Added `IsFlinkHealthyAsync()` method to check Flink JobManager health
- Modified `WaitForInfrastructureReadyAsync()` to poll both Kafka and Flink readiness
- Smart polling exits early when both Kafka endpoints discovered AND Flink is healthy

**4. Updated Exercise1 Program.cs** ✅
- Modified to read `KAFKA_FLINK_BOOTSTRAP_SERVERS` environment variable
- Changed from hardcoded `"kafka:9092"` to dynamic `kafkaBootstrapServers`
- Added validation with clear error message if environment variable not set
- **Fixed static initialization bug**: Changed from `static readonly` field to lazy property using `=>` syntax

**5. Updated Exercise2 Program.cs** ✅
- Modified to read `KAFKA_FLINK_BOOTSTRAP_SERVERS` environment variable
- Changed from hardcoded addresses to dynamic configuration
- Added validation with clear error message if environment variable not set
- **Fixed static initialization bug**: Changed from `static readonly` field to lazy property using `=>` syntax

### Critical Bug Fix: Static Initialization
**Problem**: Original code used `static readonly` fields that were evaluated at class load time (before tests set environment variables):
```csharp
// BAD - evaluates when class loads
private static readonly string KafkaBootstrapServers =
    Environment.GetEnvironmentVariable("KAFKA_FLINK_BOOTSTRAP_SERVERS") ?? "kafka:9092";
```

**Solution**: Changed to lazy properties that evaluate when first accessed:
```csharp
// GOOD - evaluates when first accessed
private static string KafkaBootstrapServers =>
    Environment.GetEnvironmentVariable("KAFKA_FLINK_BOOTSTRAP_SERVERS")
    ?? throw new InvalidOperationException("KAFKA_FLINK_BOOTSTRAP_SERVERS not set");
```

## Phase 4: Testing & Validation

### Test Results ✅

**Build**: Clean build with zero warnings
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:02.29
```

**Test Execution**: All tests passing
```
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 37 ms - LearningCourse.IntegrationTests.dll
Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2, Duration: 39 s - Day01.IntegrationTests.dll
```

**Performance**: Within target
- Total test suite duration: ~39 seconds
- Infrastructure setup: Smart polling with early exit
- Flink health check: Prevents premature test execution
- Exercise1 test: Fast execution with string capitalization
- Exercise2 test: Fast execution with 10-second window

### Verification ✅
- ✅ All 3 tests pass (1 infrastructure + 2 exercises)
- ✅ Kafka container IP discovered successfully
- ✅ Flink health check ensures cluster readiness
- ✅ TaskManager connects to Kafka using container IP
- ✅ Messages flow through Flink pipeline
- ✅ No more `localhost:9093` connection errors
- ✅ Zero build warnings or errors

## Phase 5: Owner Acceptance

### Deliverables ✅
All work items successfully completed:
1. ✅ Comprehensive logging implementation (Serilog + SLF4J/Logback)
2. ✅ Kafka container IP discovery infrastructure
3. ✅ Fixed static initialization bug in exercises
4. ✅ Flink health check implementation
5. ✅ All tests passing with optimal performance

### Test Evidence
- **LearningCourse.IntegrationTests**: 1/1 passing (37ms)
- **Day01.IntegrationTests**: 2/2 passing (39s total)
- **Zero warnings, zero errors**
- **Performance**: 39s total (within <45s target)

## Lessons Learned & Future Reference

### What Worked Well ✅
1. **Common infrastructure project**: Successfully shared across all test projects, easy to extend for Day02, Day03, etc.
2. **Container IP discovery**: Reliable in Docker bridge network, eliminates DNS resolution issues
3. **Environment variables**: Clean separation between host and container contexts
4. **Following LocalTesting pattern**: Proven and tested approach that worked immediately
5. **Lazy property evaluation**: Fixed static initialization timing bug elegantly
6. **Flink health check**: Prevents premature test execution, ensures cluster readiness
7. **Smart polling with early exit**: Optimal performance without blind waits

### Critical Bug Discovered: Static Initialization Timing ⚠️
**Problem**: `static readonly` fields evaluate at **class load time** (when CLR first references the type), NOT at runtime when environment variables are set by tests.

**Symptoms**:
- Environment variables set in test setup are not visible to static fields
- Static fields always get default/fallback values
- Hard to debug because timing is non-obvious

**Solution**: Use lazy properties with `=>` syntax:
```csharp
// BAD - evaluates at class load time
private static readonly string Value = Environment.GetEnvironmentVariable("KEY") ?? "default";

// GOOD - evaluates when first accessed at runtime
private static string Value => Environment.GetEnvironmentVariable("KEY") ?? "default";
```

**Future Prevention**:
- ⚠️ Never use `static readonly` with `Environment.GetEnvironmentVariable()` in test contexts
- ✅ Always use lazy properties (`=>`) for environment variable reads
- ✅ Add validation to throw clear exceptions if required variables are missing
- ✅ Document this pattern for all future exercises

### Key Insights for Similar Tasks
1. **Always discover dynamic values**: Never hardcode infrastructure addresses (kafka:9092, localhost:9093)
2. **Docker bridge requires IPs**: DNS doesn't work between containers on default bridge network
3. **Separate host and container addresses**: They serve different purposes (`KAFKA_BOOTSTRAP_SERVERS` vs `KAFKA_FLINK_BOOTSTRAP_SERVERS`)
4. **Reuse proven patterns**: LocalTesting's approach works, copy it rather than reinventing
5. **Static initialization matters**: Understand when static fields are evaluated vs when your code runs
6. **Health checks are essential**: Don't proceed until infrastructure is truly ready
7. **Smart polling beats blind waits**: Early exit when ready, but verify ALL components

### Specific Problems Prevented in Future
1. ✅ **No more hardcoded Kafka addresses** - all exercises use environment variables
2. ✅ **No more static initialization bugs** - documented pattern prevents recurrence
3. ✅ **No more premature test execution** - Flink health check ensures readiness
4. ✅ **No more DNS resolution failures** - container IPs work reliably
5. ✅ **No more code duplication** - common infrastructure shared across exercises

### Reference for Future Exercises

**When creating new LearningCourse exercises**:

1. **Infrastructure Access**:
   - Reference `LearningCourse.Common` project for Docker utilities
   - Use `DockerInfrastructure.GetKafkaContainerIpAsync()` for discovery

2. **Environment Variables**:
   - Use `KAFKA_BOOTSTRAP_SERVERS` for host-side Kafka operations (producers/consumers in exercise code)
   - Use `KAFKA_FLINK_BOOTSTRAP_SERVERS` for Flink job submission (TaskManager-to-Kafka connectivity)
   - Never hardcode `kafka:9092` or `localhost:9093`

3. **Configuration Pattern**:
   ```csharp
   // ALWAYS use lazy properties for environment variables
   private static string KafkaBootstrapServers =>
       Environment.GetEnvironmentVariable("KAFKA_FLINK_BOOTSTRAP_SERVERS")
       ?? throw new InvalidOperationException("KAFKA_FLINK_BOOTSTRAP_SERVERS not set");
   ```

4. **Test Base Class**:
   - Inherit from `LearningCourseTestBase` for automatic infrastructure setup
   - Infrastructure discovers Kafka IPs and checks Flink health automatically
   - Smart polling ensures everything is ready before tests run

5. **Debugging**:
   - Check logs in `LocalTesting/test-logs/` for detailed execution traces
   - Serilog logs show .NET component behavior
   - Logback logs show Java FlinkJobRunner behavior

### Architecture Impact
This fix establishes the foundation for all future LearningCourse exercises:
- ✅ Reliable infrastructure discovery pattern
- ✅ Clear separation of host vs container contexts
- ✅ Reusable test base class
- ✅ Comprehensive logging for debugging
- ✅ Health check validation pattern
- ✅ Optimal test performance

**Next exercises (Day02+) can simply**:
1. Inherit from `LearningCourseTestBase`
2. Use `KAFKA_FLINK_BOOTSTRAP_SERVERS` in their Flink jobs
3. Follow the lazy property pattern for configuration
4. All infrastructure discovery and health checking handled automatically