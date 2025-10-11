# WI19: Fix LearningCourse Kafka Connectivity with Container IP Discovery

**File**: `WIs/WI19_fix-learningcourse-kafka-container-ip-discovery.md`
**Title**: Fix LearningCourse tests by implementing Kafka container IP discovery
**Description**: Implement Kafka container IP discovery in LearningCourse following LocalTesting's proven pattern to fix TaskManager connectivity issues
**Priority**: High
**Component**: LearningCourse Test Infrastructure
**Type**: Bug Fix
**Created**: 2025-10-10
**Status**: In Progress

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

**1. Create Common Infrastructure Project**

**2. Port Discovery Methods to Common Project**

**3. Update LearningCourseTestBase**

**4. Update Exercise1 Program.cs**

**5. Update Exercise2 Program.cs**

## Phase 4: Testing & Validation

### Test Plan
1. Run Exercise1 test - should pass
2. Run Exercise2 test - should pass  
3. Verify TaskManager logs show correct IP (e.g., `172.17.0.2:9093`)
4. Verify no more `localhost:9093` connection errors

### Success Criteria
- All Day01 tests pass
- TaskManager successfully connects to Kafka
- Messages flow through Flink pipeline
- Exercises complete without errors

## Lessons Learned & Future Reference

### What Will Work Well
1. **Common infrastructure project**: Easy to extend for Day02, Day03, etc.
2. **Container IP discovery**: Reliable in Docker bridge network
3. **Environment variables**: Clean way to pass configuration to exercises
4. **Following LocalTesting pattern**: Proven and tested approach

### Key Insights for Similar Tasks
1. **Always discover dynamic values**: Never hardcode infrastructure addresses
2. **Docker bridge requires IPs**: DNS doesn't work between containers on default bridge
3. **Separate host and container addresses**: They serve different purposes
4. **Reuse proven patterns**: LocalTesting's approach works, copy it

### Reference for Future Exercises
**When creating new LearningCourse exercises**:
1. Reference `LearningCourse.Common` for infrastructure utilities
2. Use `KAFKA_BOOTSTRAP_SERVERS` for host-side Kafka operations
3. Use `KAFKA_FLINK_BOOTSTRAP_SERVERS` for Flink job submission
4. Never hardcode `kafka:9092` or `localhost:9093`