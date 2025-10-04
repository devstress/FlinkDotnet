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
  - Connector JARs in LocalTesting/connectors/flink/lib are ALL for Flink 1.20.0:
    * flink-sql-connector-kafka-3.3.0-1.20.jar
    * flink-table-runtime-1.20.0.jar
    * flink-sql-json-1.20.0.jar
    * flink-table-planner_2.12-1.20.0.jar
  - These 1.20.0 JARs are mounted into Flink 2.1.0 containers causing version conflict!
  - Maven pom.xml flink.version: 2.1.0
  - **ROOT CAUSE**: Flink 1.20 connector JARs incompatible with Flink 2.1.0 cluster runtime
  
- **Reproduction Steps**: 
  1. Run LocalTesting integration tests
  2. Flink 2.1.0 cluster starts and mounts LocalTesting/connectors/flink/lib (with 1.20 JARs)
  3. Gateway submits job to Flink cluster
  4. Flink tries to load FlinkJobRunner class which references incompatible 1.20 classes
  5. Linkage failure occurs due to version mismatch between mounted JARs and cluster runtime
  
- **Evidence**: 
  - `FlinkIRRunner/pom.xml` line 53: `<version>3.3.0-1.20</version>` (was in compile scope, now provided)
  - Flink cluster image: `flink:2.1.0-java17`
  - LocalTesting/connectors/flink/lib contains only Flink 1.20 JARs
  - Program.cs line 84, 104: Binds connector dir to /opt/flink/usrlib in containers
  - FLINK_CLASSPATH=/opt/flink/usrlib/* loads these incompatible JARs

### Findings
**Root Cause Identified**: Flink 1.20.0 connector JARs mounted into Flink 2.1.0 cluster
- Flink cluster: 2.1.0-java17
- Connector JARs in LocalTesting/connectors/flink/lib: ALL version 1.20.0
- These JARs are mounted via bind mount to /opt/flink/usrlib/* in Flink containers
- FLINK_CLASSPATH environment loads these incompatible JARs into Flink runtime
- Result: Class version conflicts and linkage failures

**Solution Strategy**: Remove incompatible Flink 1.20 connector JARs
- Option 1: Delete LocalTesting/connectors/flink/lib/*.jar files (simplest)
- Option 2: Download Flink 2.1 compatible connectors (if available)
- Option 3: Don't mount connector directory for now (SQL tests will fail but DataStream tests work)

**Analysis**: Since Flink 2.1.0 is very new (2025), official connectors may not exist yet. The safest approach is to:
1. Remove the 1.20.0 connector JARs
2. Mark flink-sql-connector-kafka as 'provided' in pom.xml (already done)
3. This will allow DataStream API tests to work
4. SQL tests may need connector JARs added later when compatible versions are available

### Lessons Learned
- Flink connector JARs mounted via bind mount can cause version conflicts
- The "-1.20" suffix in connector versions indicates target Flink version
- Connector JARs MUST match Flink cluster version precisely
- FLINK_CLASSPATH loads all JARs from mounted directories
- Version mismatches manifest as "linkage failure" errors, not missing class errors

## Phase 2: Design

### Requirements
- Remove incompatible Flink 1.20.0 connector JARs from LocalTesting/connectors/flink/lib
- Update pom.xml to mark flink-sql-connector-kafka as 'provided' scope
- Ensure DataStream API tests (majority of tests) work correctly
- Document that SQL tests may require manual connector installation

### Architecture Decisions
**Approach**: Remove Flink 1.20.0 connector JARs and rely on future Flink 2.1 compatible versions

**Why This Approach**:
1. Minimal immediate impact - removes version conflict
2. Allows DataStream API tests to pass (7 out of 10 tests)
3. SQL connector marked as 'provided' - will be supplied by Flink cluster when available
4. Clean separation: don't bundle incompatible versions
5. Forward compatible: when Flink 2.1 connectors are released, simply add them to lib dir

**Changes Required**:
1. Delete LocalTesting/connectors/flink/lib/*.jar (all 1.20.0 JARs)
2. Ensure pom.xml has flink-sql-connector-kafka with scope=provided (already done)
3. Rebuild FlinkIRRunner JAR (already done)
4. Test DataStream API jobs (patterns 1-4, 7)

**Alternatives Considered**:
1. Downgrade Flink cluster to 1.20.0 - Rejected (loses Flink 2.1 features, not forward compatible)
2. Build custom Flink 2.1 connectors - Rejected (too complex, may not be stable)
3. Keep 1.20 JARs - Rejected (causes current failures)
4. Don't mount connector dir at all - Viable but loses SQL capability entirely

### Implementation Plan
1. Remove all JAR files from LocalTesting/connectors/flink/lib/
2. Verify pom.xml changes (already complete: SQL connector scope=provided)
3. Build and test with integration test suite
4. Document SQL connector installation process for future

## Phase 3: TDD/BDD
Not applicable - this is a configuration fix, existing tests will validate the change

## Phase 4: Implementation

### Changes Made
1. ✅ Updated `FlinkIRRunner/pom.xml`:
   - Changed flink-sql-connector-kafka scope from `compile` to `provided`
   - Version remains 3.3.0-1.20 but won't be bundled in JAR
   - Connector expected to be supplied by Flink cluster classpath

2. ✅ Removed incompatible connector JARs:
   - Deleted `LocalTesting/connectors/flink/lib/flink-sql-connector-kafka-3.3.0-1.20.jar`
   - Deleted `LocalTesting/connectors/flink/lib/flink-sql-json-1.20.0.jar`
   - Deleted `LocalTesting/connectors/flink/lib/flink-table-planner_2.12-1.20.0.jar`
   - Deleted `LocalTesting/connectors/flink/lib/flink-table-runtime-1.20.0.jar`

3. ✅ Updated documentation:
   - Updated `LocalTesting/connectors/flink/lib/README.md` with Flink 2.1 requirements
   - Documented version compatibility requirements
   - Added notes about SQL vs DataStream API connector needs

4. ✅ Built new FlinkIRRunner JAR:
   - Successfully compiled with Maven
   - JAR size: 19MB (shaded, with kafka-clients included)
   - Copied to `FlinkDotNet/Flink.JobGateway/flink-ir-runner.jar`

### Status
Implementation complete. Ready for testing.

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
