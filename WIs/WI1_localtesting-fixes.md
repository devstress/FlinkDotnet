# WI1: LocalTesting Tests Fixes

**File**: `WIs/WI1_localtesting-fixes.md`
**Title**: Fix LocalTesting tests and root causes until they pass locally
**Description**: Address build failures and test failures in LocalTesting solution to ensure all tests pass locally as per requirements
**Priority**: High
**Component**: LocalTesting
**Type**: Bug Fix
**Assignee**: AI Agent
**Created**: Current
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- None (first WI)
### Lessons Applied  
- Following TDD/BDD principles
- Debug-first approach before solutions
- Pre-change validation requirements
### Problems Prevented
- Making changes without establishing baseline

## Phase 1: Investigation
### Requirements
Fix LocalTesting tests and fix the root causes until it passes locally per problem statement.

### Debug Information (MANDATORY - Update this section for every investigation)
- **Error Messages**: 
  ```
  /home/runner/work/FlinkDotnet/FlinkDotnet/LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs(46,5): error S1481: Remove the unused local variable 'gateway'. (https://rules.sonarsource.com/csharp/RSPEC-1481)
  ```
- **Log Locations**: Build output from `dotnet build LocalTesting/LocalTesting.sln --configuration Release`
- **System State**: 
  - .NET 9.0.100 installed and verified
  - Java 17 available for Flink IR Runner
  - Flink IR Runner JAR built successfully  
  - FlinkDotNet and BackPressureExample solutions build successfully
  - LocalTesting solution fails with SonarQube rule violation
- **Reproduction Steps**: 
  1. Run `./scripts/validate-build-and-tests.ps1 -SkipTests`
  2. Observe LocalTesting build failure on unused variable
- **Evidence**: Build output shows specific line 46 in Program.cs has unused variable 'gateway'

### Findings
1. **Build Issue**: LocalTesting.FlinkSqlAppHost has unused local variable 'gateway' on line 46
2. **Code Analysis**: Program.cs defines `var gateway = builder.AddProject(...)` but never uses the variable
3. **Solution Strategy**: Remove unused variable or use it appropriately
4. **Test Status**: Cannot run tests until build issues are resolved

### Lessons Learned
- Pre-change validation script effectively identified build issues
- SonarQube rules are enforced during build process
- Must fix build failures before proceeding to test execution

## Phase 2: Design  
### Requirements
Fix the unused variable issue and run LocalTesting integration tests to identify any runtime failures.

### Architecture Decisions
1. **Remove unused variable**: Simply remove the `var gateway =` assignment since the resource is registered with the builder
2. **Validate tests**: Run actual integration tests after build fixes to identify runtime issues

### Why This Approach
- Minimal change approach - only fix what's broken
- Establishes clean build baseline before investigating test failures
- Follows enforcement rules to fix builds before tests

### Alternatives Considered
- Could assign the gateway variable to a field or use it somehow, but since it's not needed, removal is cleaner

## Phase 3: TDD/BDD
### Test Specifications
- All LocalTesting builds must pass without errors
- All LocalTesting integration tests must execute successfully
- Infrastructure components (Kafka, Flink, Gateway) must be accessible

### Behavior Definitions
- GIVEN LocalTesting solution builds successfully
- WHEN integration tests are executed  
- THEN all tests should pass without connectivity or infrastructure issues

## Phase 4: Implementation
### Code Changes
1. **Fixed unused variable in LocalTesting.FlinkSqlAppHost/Program.cs**:
   - Removed `var gateway =` assignment since the resource registration doesn't need to be stored
   - This fixed SonarQube rule S1481 violation

2. **Fixed API compatibility issues in LocalTesting.IntegrationTests/FlinkDotNetJobs.cs**:
   - Changed return type from `FlinkDotNet.Pipelines.SubmitResult` to `JobSubmissionResult` 
   - Added proper using statement: `using Flink.JobBuilder.Models;`
   - Removed unnecessary using statements that were causing IDE0005 violations
   - Verified all method signatures now match the actual FlinkDotNet API

### Challenges Encountered
- **API Discovery**: Initial tests were using non-existent namespace `FlinkDotNet.Pipelines.SubmitResult`
- **Using Statement Issues**: .NET 9.0 has different implicit global using handling
- **SonarQube Rules**: Build process enforces strict code quality rules

### Solutions Applied
- **Namespace Investigation**: Found correct types in `Flink.JobBuilder.Models` namespace
- **Incremental Using Removal**: Systematically removed unnecessary using statements to fix IDE0005
- **Minimal Changes**: Only fixed what was broken, preserved all functional logic

## Phase 5: Testing & Validation
### Test Results
**Build Validation**: ✅ SUCCESSFUL
- All LocalTesting builds now pass without errors
- Fixed unused variable and API compatibility issues
- SonarQube rule violations resolved

**Runtime Testing**: ⚠️ IN PROGRESS  
- **Issue Identified**: Integration tests timeout due to infrastructure complexity
- **Root Cause Analysis**:
  1. **FLINK_RUNNER_JAR_PATH Configuration**: The hardcoded jar path `/app/flink-ir-runner.jar` likely doesn't exist in Aspire containers
  2. **Infrastructure Orchestration**: Aspire needs significant time to start Kafka + Flink + Gateway components
  3. **Network Connectivity**: Container networking between Kafka, Flink JobManager/TaskManager, and Gateway requires proper configuration

**Infrastructure Startup Evidence**:
- Logs show Kafka, Flink JobManager, and TaskManager starting successfully
- TaskManager registers with ResourceManager correctly
- Tests timeout after 60-90 seconds during infrastructure waiting phases

**Mitigation Attempted**:
- Temporarily disabled `FLINK_RUNNER_JAR_PATH` environment variable as recommended in TODO.md
- This should allow the gateway to determine jar paths internally

### Performance Metrics
- **Build Time**: ~2-5 seconds for LocalTesting solution
- **Test Infrastructure Startup**: 60+ seconds for full Aspire orchestration
- **Test Timeout**: Tests abort after 60-90 second timeout periods

## Phase 6: Owner Acceptance
### Demonstration
[To be filled when complete]

### Owner Feedback
[To be filled when complete]

### Final Approval
[To be filled when complete]

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
- **Systematic Debug Approach**: Pre-change validation script effectively identified build issues
- **Incremental Fixes**: Fixing one issue at a time (unused variable → API compatibility → using statements)
- **API Investigation**: Successfully found correct return types and namespaces by exploring FlinkDotNet structure
- **Minimal Changes**: Only modified what was broken, preserved all functional logic

### What Could Be Improved  
- **Integration Test Strategy**: Need faster feedback loops for infrastructure testing
- **Environment Documentation**: Better documentation of Aspire orchestration requirements
- **Jar Path Management**: Should implement TODO.md recommendation to remove FLINK_RUNNER_JAR_PATH dependency

### Key Insights for Similar Tasks
- **Build Before Test**: Always ensure builds pass completely before attempting integration tests
- **SonarQube Rules**: .NET 9.0 build process enforces strict code quality - handle using statements carefully  
- **API Evolution**: FlinkDotNet API has evolved - return types changed from `FlinkDotNet.Pipelines.SubmitResult` to `Flink.JobBuilder.Models.JobSubmissionResult`
- **Aspire Infrastructure**: Complex multi-container orchestration requires significant startup time and proper configuration

### Specific Problems to Avoid in Future
- **Hardcoded Container Paths**: Avoid absolute paths like `/app/flink-ir-runner.jar` in Aspire configurations
- **Assuming API Stability**: Always verify current API structure rather than assuming namespace existence
- **Ignoring Infrastructure Complexity**: Integration tests with Kafka + Flink require substantial infrastructure startup time
- **Immediate Full Testing**: Start with build validation before attempting complex integration scenarios

### Reference for Future WIs
- **LocalTesting Configuration**: Focus on jar path issues and Aspire container networking
- **Build Issues**: Use validation scripts to catch SonarQube violations early  
- **API Changes**: Check `Flink.JobBuilder.Models` namespace for job submission types
- **Infrastructure Debugging**: Allow 90+ seconds for full Aspire orchestration startup