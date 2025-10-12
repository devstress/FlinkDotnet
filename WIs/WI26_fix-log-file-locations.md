# WI26: Fix Log File Locations and Clean Up Console.WriteLine

**File**: `WIs/WI26_fix-log-file-locations.md`
**Title**: [Logging] Fix log file locations to root/LocalTesting/test-logs/ and remove Console.WriteLine from FlinkDotNet components
**Description**: Verify and fix log file locations for FlinkDotnet.log.*, Flink.JobGateway.log.* and FlinkIRRunner.log.* to be in root/LocalTesting/test-logs/. Remove Console.WriteLine statements from FlinkDotNet components. Add test-logs cleanup to LearningCourse OneTimeSetup.
**Priority**: High
**Component**: Logging Infrastructure
**Type**: Bug Fix
**Assignee**: AI Agent
**Created**: 2025-10-11
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- No directly related WIs found
### Lessons Applied  
- Will use environment variable investigation first before making changes
- Will follow proper logging configuration patterns from existing implementations
### Problems Prevented
- Will avoid breaking existing log functionality by checking current behavior first

## Phase 1: Investigation
### Requirements
- Verify current log file locations for all three components (FlinkDotNet, Flink.JobGateway, FlinkIRRunner)
- Identify where logs are currently being written
- Check environment variable usage for log paths
- Identify all Console.WriteLine usage in FlinkDotNet components
- Understand current logging configuration

### Debug Information (MANDATORY - Update this section for every investigation)
**Current State Analysis:**
1. **LocalTesting/test-logs/ directory structure:**
   - Found: `LocalTesting/test-logs/dotnet/` subdirectory
   - Contains: `flink-job-gateway-20251012.log` and `localtesting-20251012.log`
   - Missing: FlinkDotnet.log.*, FlinkIRRunner.log.* in root test-logs/

2. **FlinkIRRunner Java logging configuration:**
   - File: `FlinkIRRunner/src/main/resources/logback.xml`
   - Current path: `${LOG_FILE_PATH:-./LocalTesting/test-logs}/flink-ir-runner-%d{yyyy-MM-dd}.log`
   - Uses environment variable `LOG_FILE_PATH` with fallback to `./LocalTesting/test-logs`
   - Problem: Logs may be going to wrong location when `LOG_FILE_PATH` is not set

3. **Flink.JobGateway .NET logging:**
   - File: `FlinkDotNet/Flink.JobGateway/Program.cs`
   - Current: Uses built-in .NET logging with Console and Debug providers (line 79)
   - No file logging configured - explains why logs are in dotnet/ subdirectory (Aspire default)
   - Need to add file logging configuration

4. **Console.WriteLine usage found in FlinkDotNet components:**
   - `FlinkDotNet.DataStream/DataStream.cs`: 3 occurrences (lines 311, 321, 333)
   - `Flink.JobGateway/Services/FlinkJobManager.cs`: Multiple diagnostic console writes (lines 238-254, 265)
   - `Flink.JobBuilder/Demo/RateLimitingDemo.cs`: Demo code (acceptable - not production)
   - `FlinkDotNet/PythonAlignedExample.cs`: Example code (acceptable - not production)
   - `Flink.JobBuilder/Backpressure/BufferPool.cs`: Error logging (line 234)
   - `Flink.JobBuilder/Backpressure/MultiTierRateLimiter.cs`: Error logging (line 444)

5. **LearningCourse test infrastructure:**
   - File: `LearningCourse/LearningCourse.IntegrationTests/LearningCourseTestBase.cs`
   - Has OneTimeSetUp (line 35) but no test-logs cleanup
   - Need to add cleanup of `LocalTesting/test-logs/` directory

### Findings
**Issues Identified:**
1. ❌ FlinkIRRunner logs not in expected location (missing `flink-ir-runner-*.log` in test-logs/)
2. ❌ Flink.JobGateway logs in wrong subdirectory (`test-logs/dotnet/` instead of `test-logs/`)
3. ❌ No FlinkDotnet.log.* files found anywhere
4. ❌ Console.WriteLine used in production FlinkDotNet components (DataStream, JobGateway)
5. ❌ No test-logs cleanup in LearningCourse OneTimeSetup

**Root Causes:**
1. FlinkIRRunner uses relative path that may resolve differently depending on working directory
2. Flink.JobGateway has no file logging configured, relies on Aspire defaults
3. FlinkDotNet.DataStream uses Console.WriteLine instead of proper logging
4. No LOG_FILE_PATH environment variable set by LocalTesting AppHost
5. No cleanup logic in test infrastructure

### Lessons Learned
- Need to set LOG_FILE_PATH environment variable in LocalTesting AppHost for all components
- FlinkDotNet components need proper ILogger injection instead of Console.WriteLine
- File logging needs explicit configuration, not relying on Aspire defaults

## Phase 2: Design  
### Requirements
1. Configure LOG_FILE_PATH environment variable in LocalTesting AppHost
2. Add file logging to Flink.JobGateway with correct path
3. Replace Console.WriteLine with ILogger in FlinkDotNet components
4. Add test-logs cleanup to LearningCourse OneTimeSetup
5. Verify all logs write to `LocalTesting/test-logs/` (not subdirectories)

### Architecture Decisions
**Logging Architecture:**
- **LocalTesting AppHost**: Sets LOG_FILE_PATH environment variable for all components
- **Java Components (FlinkIRRunner)**: Use LOG_FILE_PATH from environment
- **.NET Components (JobGateway)**: Use Serilog file sink with LOG_FILE_PATH
- **FlinkDotNet Library**: NO direct file logging, only ILogger interface for consumers to configure

### Why This Approach
1. **Environment Variable Pattern**: Centralizes log path configuration in one place (AppHost)
2. **Serilog for .NET**: Industry standard with excellent file logging support
3. **ILogger Abstraction**: FlinkDotNet library stays decoupled from logging implementation
4. **Test Cleanup**: Ensures clean state for each test run

### Alternatives Considered
- ❌ Hardcode paths in each component: Violates DRY, hard to maintain
- ❌ Use Aspire default logging: Creates nested directories, hard to find logs
- ❌ Keep Console.WriteLine: Not production-ready, can't control log levels

## Phase 3: TDD/BDD
### Test Specifications
1. After running LocalTesting, verify these files exist:
   - `LocalTesting/test-logs/flink-ir-runner-YYYY-MM-DD.log`
   - `LocalTesting/test-logs/flink-job-gateway-YYYY-MM-DD.log`
2. Verify NO logs in `LocalTesting/test-logs/dotnet/` subdirectory
3. Verify LearningCourse tests clean up test-logs before execution

### Behavior Definitions
```gherkin
Given LocalTesting AppHost is configured
When components start up
Then LOG_FILE_PATH environment variable is set to absolute path
And all components write logs to LocalTesting/test-logs/
And no subdirectories are created under test-logs/

Given LearningCourse integration tests start
When OneTimeSetup executes
Then LocalTesting/test-logs/ directory is cleaned
And old log files are removed
```

## Phase 4: Implementation
*Pending completion of Phase 3*

## Phase 5: Testing & Validation
*Pending completion of Phase 4*

## Phase 6: Owner Acceptance
*Pending completion of Phase 5*

## Lessons Learned & Future Reference (MANDATORY)
*To be completed after implementation*