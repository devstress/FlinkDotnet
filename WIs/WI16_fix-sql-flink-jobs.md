# WI16: Fix SQL Flink Job Integration Tests

**File**: `WIs/WI16_fix-sql-flink-jobs.md`
**Title**: Fix remaining integration tests - SQL Flink job architecture root cause
**Description**: Fix the 4 failing SQL Flink job tests by learning from Flink SQL online tutorials and fixing root cause architecture issues
**Priority**: High
**Component**: LocalTesting.IntegrationTests, FlinkIRRunner, Flink.JobBuilder
**Type**: Bug Fix
**Assignee**: GitHub Copilot
**Created**: 2025-01-30
**Status**: Investigation

## Lessons Applied from Previous WIs

### Previous WI References
- WI9_integration-test-failures.md - JAR selection priority fix for Java compatibility
- WI10_integration-test-loop-fix.md - Iterative debugging approach and infrastructure validation

### Lessons Applied  
- **Debug-first approach**: Must understand failures before proposing fixes
- **Run tests locally**: Reproduce issues in local environment first
- **Learn from previous work**: WI9 fixed Java compatibility, 5/9 tests now pass
- **Iterative testing**: Fix one issue at a time, retest, repeat
- **Study Flink SQL tutorials**: Understand proper Flink SQL job setup

### Problems Prevented
- Avoiding guessing without evidence
- Not making changes without understanding root cause
- Skipping SQL-specific Flink requirements

## Phase 1: Investigation

### Requirements
- Understand why SQL Flink jobs fail while DataStream jobs succeed
- Study Apache Flink SQL documentation and tutorials
- Debug SQL job submission to identify root cause
- Fix architecture issues in SQL job processing
- Ensure all 9 tests pass (currently 5/9 passing)

### Debug Information (MANDATORY - Update this section for every investigation)

**Current Test Status**: Unknown - need to run tests
**Environment**:
- .NET Version: 9.0.305
- Java: Unknown (need to check)
- Flink: 2.1.0-java17 (from WI9)
- OS: Linux (GitHub Actions runner)

**Test Execution Strategy**:
1. Build all solutions successfully
2. Run integration tests to capture current failure state
3. Identify which specific SQL tests are failing
4. Debug SQL job submission process
5. Learn from Flink SQL tutorials and documentation
6. Implement minimal fix for root cause
7. Retest until all 9 tests pass

**Expected Failures** (based on WI9):
- Gateway_Pattern5_SqlPassthrough_ShouldWork ❌
- Gateway_Pattern6_SqlTransform_ShouldWork ❌
- Possibly 2 more SQL-related tests

### Test Run #1 - Initial Status Check

**Status**: FAILED - 2/7 SQL tests failing

**Test Results**:
```
Total tests: 7
Passed: 5/7
  - Gateway_Pattern1_Uppercase_ShouldWork ✅
  - Gateway_Pattern2_Filter_ShouldWork ✅
  - Gateway_Pattern3_SplitConcat_ShouldWork ✅
  - Gateway_Pattern4_Timer_ShouldWork ✅
  - Gateway_Pattern7_Composite_ShouldWork ✅

Failed: 2/7
  - Gateway_Pattern5_SqlPassthrough_ShouldWork ❌
  - Gateway_Pattern6_SqlTransform_ShouldWork ❌
```

**Error Messages**:
```
Job must submit successfully. Error: HTTP BadRequest
org.apache.flink.runtime.rest.handler.RestHandlerException: Could not execute application
```

**Environment Details**:
- Java: OpenJDK 17.0.16 (Temurin)
- Flink: 2.1.0-java17
- .NET: 9.0.305
- Flink IR Runner JAR: flink-ir-runner-java17.jar ✅ (correct JAR selected)

### Findings

**ROOT CAUSE IDENTIFIED**: SQL Connector JARs Not Included

**Investigation Steps**:
1. ✅ DataStream jobs (Pattern 1-4, 7) all pass - Flink infrastructure working
2. ✅ SQL jobs fail with "Could not execute application" - SQL-specific issue
3. ✅ Connector JARs exist in `/LocalTesting/connectors/flink/lib/`:
   - `flink-sql-connector-kafka-4.0.1-2.0.jar` (9.7MB)
   - `flink-json-2.1.0.jar` (181KB)
4. ✅ FlinkJobManager.cs has `CollectConnectorJars()` method that finds these JARs
5. ❌ **BUG**: `CombineJarsAsync()` method does NOT actually combine JARs!

**Code Analysis** - `FlinkJobManager.cs` line 781-793:
```csharp
private Task CombineJarsAsync(string runnerJarPath, List<string> connectorJars, string outputPath)
{
    _logger.LogInformation("Combining runner JAR with {Count} connector JARs into shaded JAR", connectorJars.Count);
    File.Copy(runnerJarPath, outputPath, true);  // ✅ Copy runner JAR
    
    foreach (var connectorJar in connectorJars)
    {
        _logger.LogDebug("Would include connector JAR: {Path}", connectorJar);  // ❌ Just logging!
    }
    
    _logger.LogInformation("Created shaded JAR at {Path}", outputPath);
    return Task.CompletedTask;
}
```

**Problem**: The method says "Would include" but never actually includes the connector JARs in the shaded JAR!

**Why DataStream Jobs Work But SQL Jobs Fail**:
- DataStream jobs use Kafka producer/consumer APIs (kafka-clients library) which is already in runner JAR
- SQL jobs use Flink Table API with SQL syntax requiring:
  - `flink-sql-connector-kafka` for Kafka table connector
  - `flink-json` for JSON format support
- These SQL-specific JARs must be included in the JAR submitted to Flink

**Flink SQL Requirements** (from Apache Flink documentation):
When using SQL connectors, the connector JARs must be:
1. Available in Flink's `/opt/flink/lib` directory, OR
2. Included in the submitted application JAR (shaded/uber JAR), OR
3. Provided via Flink's classpath configuration

Current architecture chooses option #2 (shaded JAR) but implementation is incomplete.

## Phase 2: Design

### Requirements
Implement proper JAR combining/shading to include SQL connector JARs

### Architecture Decisions

**Solution**: Implement actual JAR combining in `CombineJarsAsync()` method

**JAR Combining Approach**:
Since we need to combine multiple JARs into one uber/shaded JAR, we have two options:
1. **Use Java jar tool to extract and repackage** - Complex, requires java command execution
2. **Use .NET ZipArchive to combine JAR contents** - Simple, native .NET solution

**Chosen Approach**: Use .NET `System.IO.Compression.ZipArchive` 
- JARs are ZIP files internally
- Extract all connector JARs and merge into runner JAR
- Handle duplicate entries (keep first occurrence)
- Preserve manifest and structure

**Implementation Strategy**:
```csharp
1. Copy runner JAR to output path (base JAR)
2. Open output JAR as ZipArchive in Update mode
3. For each connector JAR:
   a. Open connector JAR as ZipArchive in Read mode
   b. For each entry in connector JAR:
      - Skip if entry already exists in output (avoid duplicates)
      - Otherwise, copy entry to output JAR
4. Return shaded JAR path
```

### Why This Approach
- **Minimal change**: Single method modification
- **Pure .NET solution**: No external dependencies or Java process execution
- **Portable**: Works on Linux, Windows, macOS
- **Proper JAR structure**: Maintains ZIP format and entry hierarchy
- **Handles duplicates**: Avoids file conflicts by keeping first occurrence

### Alternatives Considered
1. **Java jar tool**: Rejected - requires process execution, platform-dependent
2. **Maven shade plugin**: Rejected - requires Maven runtime, too heavyweight
3. **Copy JARs to Flink lib**: Rejected - requires container modification
4. **Classpath parameter**: Rejected - Flink REST API doesn't support easily

## Phase 3: TDD/BDD

### Test Specifications
No new tests needed - fix resolves existing test failures

### Validation Approach
1. Implement JAR combining
2. Run SQL integration tests
3. Verify all 9 tests pass (7 Gateway + 2 others if applicable)
4. Check that shaded JAR contains connector JARs

## Phase 4: Implementation

### Code Changes

**File 1**: `FlinkDotNet/Flink.JobGateway/Services/FlinkJobManager.cs`

**Change 1**: Added `using System.IO.Compression;` for ZIP/JAR manipulation (line 4)

**Change 2**: Enhanced `CollectConnectorJars()` method (lines 726-778)
- Added AppDomain.CurrentDomain.BaseDirectory search path (works better in Aspire)
- Added diagnostic logging for connector search  
- Added warning when no connectors found with directory details

**Change 3**: Implemented actual JAR combining in `CombineJarsAsync()` (lines 781-842)
- Use `System.IO.Compression.ZipArchive` to manipulate JAR files
- Open output JAR in Update mode
- For each connector JAR:
  - Extract all entries
  - Skip duplicates (keep first occurrence)
  - Copy unique entries to output JAR
- Log entries added from each connector

### Implementation Status
✅ Code changes complete
✅ Builds successfully
⏳ Testing in progress

### Test Run #2 - After Implementation

**Status**: FAILED - Same error (investigating)

**Issue**: SQL jobs still fail with same error. Connector search logs not appearing in test output.

**Hypothesis**: Gateway runs in separate Aspire process, logs not captured in test output. Need to verify:
1. Environment variable `FLINK_CONNECTOR_PATH` is actually reaching the Gateway process
2. Connector JARs are being found and combined
3. Shaded JAR is being uploaded to Flink with connectors included

**Next Steps**:
1. Add more explicit logging or debug output
2. Check if the shaded JAR actually contains the connector classes
3. Verify the actual Flink error details (not just truncated message)

## Phase 5: Testing & Validation

### What Worked Well
(To be documented as we progress)

### Specific Problems to Avoid in Future
(To be documented based on issues encountered)

### Reference for Future WIs
(To be documented with specific files and patterns)
