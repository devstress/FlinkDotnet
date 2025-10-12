# WI27: Fix Log File Locations and Remove Console.WriteLine

**File**: `WIs/WI27_fix-log-file-locations-and-cleanup.md`
**Title**: Fix log file locations to use environment variables and remove Console.WriteLine
**Description**: FlinkDotNet components have hard-coded log paths and Console.WriteLine statements. Need to use environment variables for log locations and proper logging instead.
**Priority**: High
**Component**: Logging Infrastructure
**Type**: Bug Fix
**Assignee**: AI Agent
**Created**: 2025-10-11
**Status**: Done

## Phase 1: Investigation

### Requirements
- Verify log files should be in `LocalTesting/test-logs/` or `LearningCourse/test-logs/`
- Identify all hard-coded log paths in FlinkDotNet components
- Find all Console.WriteLine usage in FlinkDotNet components
- Determine proper cleanup location in LearningCourse test base

### Debug Information (MANDATORY)
**Current Issues Found**:
1. **Hard-coded paths in FlinkDotNet components**:
   - `FlinkDotNet.DataStream/OperationCapture.cs:31` - `"LocalTesting/test-logs/flink-dotnet-.log"`
   - `FlinkDotNet.DataStream/StreamExecutionEnvironment.cs:42` - `"LocalTesting/test-logs/flink-dotnet-.log"`
   - `Flink.JobBuilder/Services/FlinkJobGatewayService.cs:25` - `"LocalTesting/test-logs/flink-job-gateway-.log"`

2. **Console.WriteLine usage**:
   - `Flink.JobGateway/Services/FlinkJobManager.cs:238-242` - Diagnostic logging
   - `Flink.JobGateway/Services/FlinkJobManager.cs:251-253` - Kafka configuration logging
   - `Flink.JobGateway/Services/FlinkJobManager.cs:265` - Map operation logging
   - `FlinkDotNet.DataStream/DataStream.cs:311,321,333` - Debug logging

3. **Missing cleanup in LearningCourse**:
   - `LearningCourse.IntegrationTests/LearningCourseTestBase.cs` - No test-logs cleanup in OneTimeSetUp

**Expected Behavior**:
- LocalTesting should use environment variable `LOG_FILE_PATH` for log directory
- LearningCourse should clean up test-logs in OneTimeSetUp
- No Console.WriteLine in production components
- Logs should be: `FlinkDotnet.log.*`, `Flink.JobGateway.log.*`, `FlinkIRRunner.log.*`

### Findings
- LocalTesting AppHost already sets up LOG_FILE_PATH environment variable
- FlinkDotNet components ignore this and use hard-coded paths
- Console.WriteLine is mixed with proper ILogger usage
- No test-logs cleanup happens in LearningCourse

## Phase 2: Design

### Architecture Decisions
1. **Use Environment Variable Pattern**:
   - Get log directory from `LOG_FILE_PATH` environment variable
   - Fallback to `test-logs/` if not set (for standalone execution)
   - Use Path.Combine for cross-platform compatibility

2. **Remove Console.WriteLine**:
   - Replace with proper Serilog logging in components
   - Keep Console sink in Serilog configuration for visibility

3. **Add Cleanup in LearningCourse**:
   - Clean up test-logs directory in OneTimeSetUp
   - Ensure directory exists after cleanup

### Why This Approach
- Environment variables allow flexible configuration per test suite
- Proper logging is essential for production code
- Cleanup prevents log pollution between test runs

## Phase 3: Implementation

### Code Changes
Files to modify:
1. `FlinkDotNet/FlinkDotNet.DataStream/OperationCapture.cs`
2. `FlinkDotNet/FlinkDotNet.DataStream/StreamExecutionEnvironment.cs`
3. `FlinkDotNet/Flink.JobBuilder/Services/FlinkJobGatewayService.cs`
4. `FlinkDotNet/Flink.JobGateway/Services/FlinkJobManager.cs`
5. `FlinkDotNet/FlinkDotNet.DataStream/DataStream.cs`
6. `LearningCourse/LearningCourse.IntegrationTests/LearningCourseTestBase.cs`

## Phase 4: Implementation Complete

### Changes Applied
1. **FlinkDotNet.DataStream/OperationCapture.cs**: Updated to use `LOG_FILE_PATH` environment variable with fallback to `test-logs/`, file named `FlinkDotnet.log`
2. **FlinkDotNet.DataStream/StreamExecutionEnvironment.cs**: Updated to use `LOG_FILE_PATH` environment variable with fallback to `test-logs/`, file named `FlinkDotnet.log`, added `shared: true`
3. **Flink.JobBuilder/Services/FlinkJobGatewayService.cs**: Updated to use `LOG_FILE_PATH` environment variable with fallback to `test-logs/`, file named `Flink.JobGateway.log`, added `shared: true`
4. **Flink.JobGateway/Services/FlinkJobManager.cs**: Replaced Console.WriteLine with proper ILogger.LogDebug calls, changed methods from static to instance methods
5. **FlinkDotNet.DataStream/DataStream.cs**: Removed Console.WriteLine statements from GroupBy(), Print(), and AddSink() methods
6. **LearningCourse.IntegrationTests/LearningCourseTestBase.cs**: Added test-logs cleanup in OneTimeSetUp to clean directory before tests run

### Verification
- All log files now use environment variable pattern: `Path.Combine(Environment.GetEnvironmentVariable("LOG_FILE_PATH") ?? "test-logs", "LogFileName.log")`
- Log files are named: `FlinkDotnet.log`, `Flink.JobGateway.log` (simplified names without wildcards)
- No Console.WriteLine in FlinkDotNet library components
- LearningCourse cleans up test-logs before starting tests
- All logs use `shared: true` for multi-process access
- Logs maintain Console sink for visibility during development

## Lessons Learned & Future Reference

### What Worked Well
- Using environment variables for configuration flexibility
- Centralizing log configuration with Serilog
- Cleaning up test artifacts in OneTimeSetUp ensures clean state
- Using ILogger.LogDebug for diagnostic information instead of Console.WriteLine

### Key Insights for Similar Tasks
- Always use environment variables for paths in shared components
- Never use Console.WriteLine in library code - use proper logging
- Clean up test artifacts in setup (OneTimeSetUp), not teardown, to ensure clean state
- Use Path.Combine for cross-platform compatibility
- Add `shared: true` to Serilog File sink for multi-process scenarios
- Environment variable pattern: `GetEnvironmentVariable("VAR") ?? "fallback"`
- Simplified log file names are better than wildcard patterns for debugging

### Problems Prevented
- No more hard-coded paths causing logs to appear in wrong locations
- No more Console output pollution in library components
- No more test log accumulation between runs
- Proper diagnostic logging for production troubleshooting