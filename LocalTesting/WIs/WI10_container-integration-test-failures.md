# WI10: Investigate Container Integration Test Failures

**File**: `WIs/WI10_container-integration-test-failures.md`
**Title**: [LocalTesting] 8/9 integration tests failing - investigate containers
**Description**: Integration tests were working (7/9 passing) but now all 9 are failing. Investigate containers and fix all tests.
**Priority**: High
**Component**: LocalTesting.IntegrationTests
**Type**: Bug Fix
**Status**: Investigation - Root cause being debugged

## Lessons Applied from Previous WIs
### Previous WI References
- WI9: Maven JAR compatibility issues (Java 17 vs Java 25)

### Lessons Applied
- Debug infrastructure first before changing code
- Check container runtime (Docker vs Podman)
- Verify port mappings and network connectivity
- Log all connection attempts for debugging

### Problems Prevented
- Using wrong JAR version (now using Java 17)
- Hardcoded ports (now using dynamic port discovery)
- Missing logging (added comprehensive logging)

## Phase 1: Investigation ✅

### Requirements
- Identify why 8/9 integration tests are failing
- Check all Docker containers and their status
- Verify Kafka, Flink, and Gateway connectivity
- Fix root cause to make all tests pass

### Debug Information (MANDATORY)

#### Error Messages
```
Should consume at least 2 messages
Assert.That(consumed.Count, Is.GreaterThanOrEqualTo(expectedOutputCount))
  Expected: greater than or equal to 2
  But was:  0
```

#### Test Behavior Pattern
- ✅ Infrastructure starts successfully (Kafka, Flink JobManager, Flink TaskManager, Gateway)
- ✅ Topics created successfully  
- ✅ Job submission succeeds (returns jobId and FlinkJobId)
- ✅ Job reports as RUNNING state
- ✅ Test produces messages to input topic successfully (2 messages)
- ❌ No messages appear in output topic (0 messages consumed)
- ❌ Test times out waiting for output messages (45s)

#### Key Findings
1. **Maven Build Issue - FIXED** ✅
   - Maven path not captured correctly in Flink.JobGateway.csproj
   - Fixed by capturing `ConsoleOutput` from `which mvn`
   - Fixed shell variable expansion by wrapping in `/bin/bash -c`

2. **Maven Shade Plugin Error - FIXED** ✅
   - Error: "Could not replace original artifact with shaded artifact"
   - Fixed by using `<outputFile>` instead of `<finalName>` in pom.xml

3. **Container Runtime Issue - FIXED** ✅
   - Tests were using Podman instead of Docker
   - Added Docker detection (installation + daemon running)
   - Proper fallback to Podman if Docker unavailable

4. **Kafka Dual Listener Configuration - FIXED** ✅
   - Added KAFKA_CFG_LISTENER_SECURITY_PROTOCOL_MAP
   - Added KAFKA_CFG_LISTENERS (internal:9092, external:9093)
   - Added KAFKA_CFG_ADVERTISED_LISTENERS

5. **Critical Port Configuration Error - FIXED** ✅
   - `Ports.KafkaContainerBootstrap` was `kafka:9093` (WRONG - external listener)
   - Changed to `kafka:9092` (CORRECT - internal listener)
   - Flink containers must use internal listener

6. **Dynamic Port Discovery - IMPLEMENTED** ✅
   - Aspire maps port 9093 to dynamic host port (e.g., 32769)
   - Added `DiscoverKafkaExternalPortAsync()` with 3-attempt retry
   - Test processes use discovered external port

7. **JAR File Cleanup - COMPLETED** ✅
   - Removed all references to `flink-ir-runner.jar`
   - Now using only `flink-ir-runner-java17.jar`
   - Updated all discovery paths and tests

8. **Current Investigation: Flink Job Execution**
   - Job submission: ✅ SUCCESS
   - Job state: ✅ RUNNING  
   - Kafka bootstrap (container): ✅ `kafka:9092` (CORRECT)
   - Input messages produced: ✅ 2 messages
   - Output messages consumed: ❌ 0 messages
   
   **Hypothesis**: The Flink IR Runner JAR is running but not processing messages correctly. Possible causes:
   - JAR cannot connect to Kafka from within TaskManager
   - JAR has logic error in message processing
   - JAR is reading/writing wrong topics
   - Network connectivity issue between TaskManager and Kafka

#### Log Locations
- TaskManager logs: Added `LogTaskManagerStatusAsync()` in GlobalTestInfrastructure
- Gateway logs: Comprehensive HTTP logging in JobsController and FlinkJobManager
- Test logs: Added Kafka bootstrap and topic logging in job submission

### Findings
**Build Issues** (RESOLVED):
- Maven command was failing with "mvn: not found"  
- Shell variable `$PATH` not expanding in MSBuild Exec task
- Maven shade plugin error due to artifact replacement conflict

**Container Runtime** (RESOLVED):
- Tests were defaulting to Podman instead of Docker
- Aspire DCP was using Podman which has networking issues

**Kafka Configuration** (RESOLVED):
- Kafka needed dual listener configuration for container/host networks
- Container bootstrap address was wrong (kafka:9093 instead of kafka:9092)

**Current Issue** (IN PROGRESS):
- Jobs submit and run successfully but don't produce output
- Need TaskManager logs to see what's happening inside Flink containers

### Lessons Learned
- Always check which container runtime is actually being used
- Dual listeners are essential for Kafka in container environments
- Internal vs external addresses matter for containerized services
- Comprehensive logging at every integration point is critical

## Phase 2: Design ✅

### Requirements
Fix multiple infrastructure issues:
1. Maven build execution
2. Container runtime selection  
3. Kafka dual listener configuration
4. Port configuration and discovery
5. Logging and debugging infrastructure

### Architecture Decisions

**1. Maven Path Resolution**:
- Capture full Maven path using `ConsoleOutput` from `which mvn`
- Wrap Maven command in `/bin/bash -c` for proper variable expansion
- Use absolute paths instead of relying on PATH

**2. Container Runtime Detection**:
- Check Docker first (installation AND daemon running)
- Fallback to Podman if Docker not available
- Clear error messages for troubleshooting

**3. Kafka Dual Listener Pattern**:
- Internal listener: `kafka:9092` (for Flink containers)
- External listener: `localhost:9093` (for test processes, mapped to dynamic port)
- Proper advertised listeners for both networks

**4. Dynamic Port Discovery**:
- Query Docker for actual port mappings
- Retry logic (3 attempts, 2s delay) for timing issues
- Fallback to Aspire connection string if discovery fails

**5. Comprehensive Logging**:
- TaskManager logs during infrastructure setup
- Kafka bootstrap addresses in job submission
- Complete HTTP request/response tracing in Gateway
- Formatted log headers for visual clarity

### Why This Approach
- Fixes build blockers first (Maven)
- Ensures correct container runtime (Docker preferred)
- Solves multi-network Kafka connectivity
- Provides maximum debugging visibility

### Alternatives Considered
- **Manual AppHost**: Would require significant test infrastructure changes
- **Testcontainers.NET**: Different framework, learning curve
- **Hardcoded ports**: Doesn't work with Aspire's dynamic allocation

## Phase 3: TDD/BDD ✅

### Test Specifications
No new tests needed - fixes enable existing tests to pass

### Validation Approach
1. Build LocalTesting.sln successfully
2. Verify flink-ir-runner-java17.jar is built
3. Run integration tests
4. Analyze TaskManager logs for job execution details
5. Verify all 9 tests pass

## Phase 4: Implementation ✅

### Code Changes

**1. Flink.JobGateway.csproj** - Maven path and shell execution
- Line 295-303: Capture Maven full path from `ConsoleOutput`
- Line 309-315: Wrap Maven command in `/bin/bash -c`
- Result: Maven builds successfully

**2. LocalTesting.FlinkSqlAppHost/Program.cs** - Container runtime detection
- Added `IsDockerCommandAvailable()` - checks Docker CLI exists
- Added `IsDockerDaemonRunning()` - checks daemon with `docker info`
- Added `IsPodmanCommandAvailable()` - checks Podman CLI exists
- Added `IsPodmanMachineRunning()` - checks Podman machine status
- Modified `ConfigureContainerRuntime()` - Docker first, Podman fallback
- Added Kafka dual listener environment variables

**3. LocalTesting.FlinkSqlAppHost/Ports.cs** - CRITICAL FIX
- Changed `KafkaContainerBootstrap` from `kafka:9093` to `kafka:9092`
- Containers must use internal listener, not external

**4. FlinkIRRunner/pom.xml** - Maven shade plugin fix
- Changed from `<finalName>` to `<outputFile>` in shade plugin configuration
- Prevents "Could not replace original artifact" error

**5. GlobalTestInfrastructure.cs** - Port discovery and logging
- Added `DiscoverKafkaExternalPortAsync()` with retry logic
- Added `FindKafkaContainerAsync()` - container discovery
- Added `GetPortMappingAsync()` - port mapping retrieval
- Added `ParsePortMapping()` - port parsing
- Added `LogTaskManagerStatusAsync()` - TaskManager diagnostics
- Enhanced infrastructure setup logging

**6. LocalTestingTestBase.cs** - Connection logging
- Added formatted log headers for Kafka, Flink, Gateway connections
- Shows exact URLs, ports, timeouts for all service connections

**7. JobsController.cs & FlinkJobManager.cs** - Gateway HTTP logging
- Added comprehensive request/response logging
- Tracks job submission pipeline end-to-end
- Shows Flink JobManager and SQL Gateway HTTP calls

**8. GatewayAllPatternsTests.cs** - Job submission logging
- Added Kafka bootstrap address logging
- Added input/output topic logging
- Shows what addresses jobs are actually using

**9. JAR Cleanup** - Removed flink-ir-runner.jar references
- Updated FlinkJobManager.cs Maven build output path
- Updated Program.cs JAR discovery candidates  
- Removed legacy fallback paths
- Removed backward compatibility file copy

### Build Validation
```bash
$ dotnet build LocalTesting/LocalTesting.sln --configuration Release
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Maven Validation  
```
[INFO] BUILD SUCCESS
[INFO] Total time:  5.935 s
flink-ir-runner-java17.jar: EXISTS ✅
```

## Phase 5: Testing & Validation

### Test Results

**Build**: ✅ SUCCESS
**Maven**: ✅ SUCCESS  
**Unit Tests**: ✅ PASS (flink-ir-runner-java17.jar check)

**Integration Tests Status**: 8/9 FAIL (under investigation)

**What Works**:
- ✅ Infrastructure startup (Kafka, Flink JobManager, TaskManager, Gateway)
- ✅ Topic creation
- ✅ Job submission
- ✅ Job runs (RUNNING state)
- ✅ Message production to input topics

**What Doesn't Work**:
- ❌ No output messages produced by Flink jobs
- ❌ Jobs don't process data from Kafka

**Next Steps**:
1. Capture TaskManager logs during infrastructure setup
2. Verify Flink job can actually connect to Kafka from within container
3. Check if Flink IR Runner JAR has logic errors
4. Verify topic names and message formats

## Phase 6: Owner Acceptance
### Demonstration
Pending - awaiting test success

### Owner Feedback
In progress

### Final Approval
Pending

## Lessons Learned & Future Reference (MANDATORY)

### What Worked Well
- Systematic debugging from infrastructure up
- Comprehensive logging at every integration point
- Fixing one issue at a time and validating
- Docker runtime detection prevents Podman networking issues

### What Could Be Improved
- Could have added TaskManager logging earlier
- Should verify container network connectivity first
- Need automated health checks for Kafka connectivity from containers

### Key Insights for Similar Tasks
- Container runtime matters - Docker vs Podman have different behaviors
- Multi-network Kafka requires dual listener configuration
- Internal vs external addresses are different in containers
- Aspire dynamically allocates ports - must discover at runtime
- Build JDK != Runtime JDK in containers
- Comprehensive logging is essential for distributed systems

### Specific Problems to Avoid in Future
- Don't assume Maven is in PATH - use full path
- Don't use external Kafka listener for container-to-container communication
- Don't hardcode ports when using Aspire
- Don't skip infrastructure validation before running tests
- Don't forget to log what addresses are actually being used

### Reference for Future WIs
- **Maven Fix**: Flink.JobGateway.csproj lines 295-315
- **Container Runtime**: LocalTesting.FlinkSqlAppHost/Program.cs
- **Kafka Config**: Ports.KafkaContainerBootstrap = "kafka:9092"  
- **Port Discovery**: GlobalTestInfrastructure.cs DiscoverKafkaExternalPortAsync()
- **Logging Patterns**: Formatted headers with box-drawing characters
