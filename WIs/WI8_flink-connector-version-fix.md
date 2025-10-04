# WI8: Fix Flink SQL Connector Version Mismatch

**File**: `WIs/WI8_flink-connector-version-fix.md`
**Title**: Fix Flink SQL Connector Version Compatibility Issue
**Description**: Integration tests failing with "linkage failure" error due to Flink SQL connector version mismatch (using 1.20 connector with Flink 2.1.0 cluster)
**Priority**: High
**Component**: FlinkIRRunner
**Type**: Bug Fix
**Assignee**: AI Agent
**Created**: 2024-10-04
**Status**: Investigation

## Problem Statement
GitHub integration test workflow shows 2/10 tests passing with error:
```
❌ Composite test failed after 28.2s: Job must submit successfully. 
Error: HTTP BadRequest: {"jobId":"406ec41c-889d-4d4e-a812-74c97d04b5b6","flinkJobId":"","success":false,
"errorMessage":"Flink run failed: InternalServerError - {\"errors\":[\"Internal server error.\",
\"<Exception on server side:\\njava.util.concurrent.CompletionException: 
org.apache.flink.client.program.ProgramInvocationException: The program's entry point class 
'com.flink.jobgateway.FlinkJobRunner' could not be loaded due to a linkage failure.
```

## Lessons Applied from Previous WIs
### Previous WI References
- Reviewed existing WI files for similar Java/Flink integration issues
- No direct precedent found for version mismatch issues

### Lessons Applied
- Always verify dependency version compatibility before deployment
- Check Flink cluster version matches connector versions
- Use debug-first approach to identify root cause

### Problems Prevented
- Will document proper version alignment for future reference

## Phase 1: Investigation

### Requirements
- Identify root cause of linkage failure
- Verify Flink cluster version vs connector version compatibility
- Determine correct connector version for Flink 2.1.0

### Debug Information (MANDATORY - Update this section for every investigation)
- **Error Messages**: 
  - "The program's entry point class 'com.flink.jobgateway.FlinkJobRunner' could not be loaded due to a linkage failure"
  - This is a Java ClassLoader error indicating version incompatibility
  
- **Log Locations**: 
  - GitHub Actions workflow: `.github/workflows/localtesting-integration-tests.yml`
  - Test file: `LocalTesting/LocalTesting.IntegrationTests/GatewayAllPatternsTests.cs`
  
- **System State**: 
  - Flink cluster version: 2.1.0-java17 (from `LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs` line 68, 89)
  - Current flink-sql-connector-kafka version: 3.3.0-1.20 (designed for Flink 1.20.x)
  - Maven pom.xml flink.version: 2.1.0
  - Version MISMATCH detected: Connector is for Flink 1.20, cluster runs Flink 2.1.0
  
- **Reproduction Steps**: 
  1. Run LocalTesting integration tests
  2. Gateway submits job to Flink cluster
  3. Flink tries to load FlinkJobRunner class from JAR
  4. Linkage failure occurs due to incompatible connector dependencies
  
- **Evidence**: 
  - `FlinkIRRunner/pom.xml` line 53: `<version>3.3.0-1.20</version>` for flink-sql-connector-kafka
  - Flink cluster image: `flink:2.1.0-java17`
  - Connector suffix "-1.20" indicates Flink 1.20.x compatibility

### Findings
**Root Cause Identified**: Version mismatch between Flink SQL Connector and Flink cluster
- Flink cluster: 2.1.0
- Kafka SQL connector: 3.3.0-1.20 (for Flink 1.20.x)
- The connector is designed for an older Flink version, causing class incompatibility

**Solution**: Update flink-sql-connector-kafka to version compatible with Flink 2.1.0
- According to Apache Flink connector versioning, we need connector version 3.3.0-2.1 or similar
- Alternative: Use Flink 2.x native Kafka connector

### Lessons Learned
- Flink connector versions are tightly coupled to Flink runtime versions
- The version suffix (e.g., "-1.20") indicates target Flink version
- Always verify connector compatibility when upgrading Flink

## Phase 2: Design

### Requirements
- Update pom.xml to use Flink 2.1.0 compatible Kafka connector
- Ensure no other version mismatches exist
- Maintain backward compatibility with existing job definitions

### Architecture Decisions
**Approach**: Update flink-sql-connector-kafka version in pom.xml

**Why This Approach**:
- Minimal change - only version number update
- Maintains existing code structure
- Aligns connector with Flink cluster version
- Follows Flink's recommended versioning practices

**Alternatives Considered**:
1. Downgrade Flink cluster to 1.20.x - Rejected (loses Flink 2.x features)
2. Build custom connector - Rejected (unnecessary complexity)
3. Remove SQL connector dependency - Rejected (breaks SQL job support)

### Implementation Plan
1. Research correct connector version for Flink 2.1.0
2. Update FlinkIRRunner/pom.xml with correct version
3. Rebuild JAR and verify no compilation errors
4. Test with integration tests

## Phase 3: TDD/BDD
Not applicable - this is a configuration fix, existing tests will validate the change

## Phase 4: Implementation
Status: Pending

## Phase 5: Testing & Validation
Status: Pending

## Phase 6: Owner Acceptance
Status: Pending

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
- Debug-first approach quickly identified root cause
- Clear error message pointed to classloading issue
- Version suffix pattern made diagnosis straightforward

### What Could Be Improved
- Better version validation in CI pipeline
- Automated checks for dependency version compatibility

### Key Insights for Similar Tasks
- Always check connector version suffixes match Flink cluster version
- Linkage failures often indicate version mismatches, not code errors
- Maven dependency tree analysis helpful for complex version issues

### Specific Problems to Avoid in Future
- Never mix Flink connector versions across major versions
- Document required connector versions in README/docs
- Add version compatibility checks to build process

### Reference for Future WIs
- Flink connector naming: `version-flinkVersion` (e.g., 3.3.0-2.1)
- Always verify compatibility matrix before upgrading
- Test integration after any Flink/connector version changes
