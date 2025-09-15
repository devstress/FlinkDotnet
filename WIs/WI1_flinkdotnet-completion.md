# WI1: FlinkDotNet Completion

**File**: `WIs/WI1_flinkdotnet-completion.md`
**Title**: FlinkDotNet completion with Java build integration and LocalTesting improvements  
**Description**: Complete FlinkDotNet implementation with Java/Maven build integration, rename LocalTesting AppHost, and implement comprehensive TDD testing approach
**Priority**: High
**Component**: FlinkDotNet Gateway, LocalTesting, Java IR Runner
**Type**: Feature
**Assignee**: AI Agent
**Created**: 2024-12-19
**Status**: Implementation

## Lessons Applied from Previous WIs
### Previous WI References
- No previous WIs found in current repository
### Lessons Applied  
- Starting with proper investigation and debugging approach
- Following TDD principles with test-first development
- Ensuring comprehensive validation before implementation
### Problems Prevented
- Proceeding without understanding current system state
- Making changes without proper baseline validation

## Phase 1: Investigation
### Requirements
1. Add Java install, Maven install and build Java project as part of Gateway's build
2. Change LocalTesting's aspire project to LocalTesting.AppHost  
3. Use LocalTesting's test to TDD make sure FlinkDotnet working as expected
4. Have only 1 test in LocalTesting and multiple FlinkDotnet's jobs which covers all the implementation

### Debug Information (MANDATORY - Update this section for every investigation)
- **Error Messages**: LocalTesting integration tests failing with timeout/resource issues
- **Log Locations**: Test output shows Aspire DCP container networking issues
- **System State**: .NET 9.0.305 installed, Java 17 available, Maven 3.9.11 available, all solutions build successfully
- **Reproduction Steps**: `dotnet test LocalTesting/LocalTesting.sln` fails on container orchestration
- **Evidence**: FlinkDotNet builds (21.2s), LocalTesting builds (13.9s), Java builds (10.0s), but tests fail

### Current State Analysis
- FlinkDotNet solution exists with Gateway project - builds successfully
- LocalTesting solution has BackPressure.AppHost project (needs renaming to LocalTesting.AppHost) - builds successfully
- Java FlinkIRRunner project exists with Maven build - builds successfully with shaded JAR
- Integration tests exist but fail on container startup issues
- Gateway currently depends on FLINK_RUNNER_JAR_PATH environment variable and scripts/build_runner.ps1
- No Sample.sln found (likely not needed based on codebase structure)

### Key Issues Identified
1. Gateway build doesn't include Java project build - currently uses external PowerShell script
2. LocalTesting AppHost has wrong name (BackPressure.AppHost should be LocalTesting.AppHost)
3. Tests don't follow pure TDD approach for FlinkDotNet validation  
4. Integration tests are failing due to container orchestration issues
5. Current approach uses manual FLINK_RUNNER_JAR_PATH setup instead of automatic JAR building

### Findings
- All build systems work independently
- Gateway already has FindRepoRoot and PowerShell script execution logic
- FlinkJobManager.EnsureRunnerJarAsync() already handles JAR building via scripts/build_runner.ps1
- LocalTesting test references Projects.BackPressure_AppHost which needs to change
- Test failures seem to be infrastructure-related (container networking) not logic issues

### Lessons Learned
- Current system is closer to working than expected
- Build integration already partially exists - just needs Java/Maven setup
- Container orchestration needs debugging separate from main requirements implementation

## Phase 2: Design  
### Requirements
1. **Java Build Integration**: Add Java/Maven installation and build to Gateway's build process
2. **AppHost Rename**: Change LocalTesting BackPressure.AppHost to LocalTesting.AppHost
3. **TDD Test Design**: Create single comprehensive test for FlinkDotNet validation
4. **Remove FLINK_RUNNER_JAR_PATH dependency**: Make Gateway self-sufficient for JAR building

### Architecture Decisions
#### 1. Java Build Integration Strategy
- **Approach**: Extend existing EnsureRunnerJarAsync() in FlinkJobManager.cs
- **Method**: Use Maven exec directly instead of PowerShell script dependency
- **Installation**: Check for Java/Maven availability and provide clear error messages if missing
- **Build Path**: Use existing repo root finding logic to locate FlinkIRRunner/pom.xml
- **JAR Output**: Continue using FlinkIRRunner/target/flink-ir-runner.jar as shaded output

#### 2. AppHost Rename Strategy
- **Rename**: `LocalTesting/BackPressure.AppHost` → `LocalTesting/LocalTesting.AppHost`
- **Update Solution**: Modify LocalTesting.sln to reference new project name
- **Update Tests**: Change `Projects.BackPressure_AppHost` to `Projects.LocalTesting_AppHost`
- **Preserve Functionality**: Keep all existing container orchestration logic intact

#### 3. TDD Test Architecture
- **Single Test Principle**: One comprehensive integration test in LocalTesting.IntegrationTests
- **Test Name**: `FlinkDotNet_ComprehensiveValidation_AllJobTypesWork`
- **Multiple Job Scenarios**: Execute different FlinkDotNet job patterns within single test
- **Validation Points**: 
  - Gateway JAR building works automatically
  - Various FlinkDotNet API patterns function correctly
  - End-to-end Kafka input/output validation
  - Metrics and monitoring integration

#### 4. Dependency Removal Plan
- **Remove**: FLINK_RUNNER_JAR_PATH environment variable from AppHost configuration
- **Self-Sufficient Gateway**: Gateway builds Java JAR on-demand when needed
- **Error Handling**: Clear error messages when Java/Maven not available
- **Fallback**: Maintain existing script path for backward compatibility during transition

### Why This Approach
- **Minimal Changes**: Leverages existing infrastructure and patterns
- **Backward Compatible**: Doesn't break existing functionality during transition
- **Clear Separation**: Each requirement addresses a distinct concern
- **Testable**: Each change can be validated independently
- **Maintainable**: Reduces external dependencies and scripts

### Alternatives Considered
- **Alternative 1**: Create new build system → Rejected (too complex, breaks existing patterns)
- **Alternative 2**: Keep PowerShell scripts → Rejected (doesn't meet requirement for Java integration)
- **Alternative 3**: Multiple test projects → Rejected (doesn't meet single test requirement)

### Technical Implementation Details
#### Java Build Integration
- Extend `FlinkJobManager.EnsureRunnerJarAsync()` to:
  1. Check for Java 17+ availability
  2. Check for Maven 3.6+ availability  
  3. Execute `mvn clean package -DskipTests` in FlinkIRRunner directory
  4. Verify JAR output at expected location
  5. Upload JAR to Flink cluster via existing upload logic

#### Project Rename Process
1. Rename directory: `BackPressure.AppHost` → `LocalTesting.AppHost`
2. Update .csproj name and namespace
3. Update solution file references
4. Update test project references
5. Update any hardcoded paths or names

#### Single Test Design
- **Structure**: Setup → Multiple Job Scenarios → Validation → Cleanup
- **Job Scenarios**: Map, Filter, Aggregation, Side Output, SQL queries
- **Infrastructure**: Reuse existing Kafka, Flink, Gateway orchestration
- **Assertions**: Each scenario validates specific FlinkDotNet functionality

## Phase 3: TDD/BDD
[To be filled]

## Phase 4: Implementation
### Code Changes
#### 1. Java Build Integration (✅ Completed)
- **Enhanced**: `FlinkDotNet/Flink.JobGateway/Services/FlinkJobManager.cs`
- **Added**: `BuildJavaProjectAsync()` method with Maven integration
- **Added**: `CheckJavaAvailabilityAsync()` and `CheckMavenAvailabilityAsync()` methods
- **Replaced**: PowerShell script dependency with direct Maven execution
- **Improved**: Error handling with clear Java/Maven requirement messages

#### 2. AppHost Rename (✅ Completed)
- **Renamed**: `LocalTesting/BackPressure.AppHost` → `LocalTesting/LocalTesting.AppHost`
- **Updated**: `LocalTesting.sln` to reference new project name
- **Updated**: Integration test references from `Projects.BackPressure_AppHost` to `Projects.LocalTesting_AppHost`
- **Removed**: FLINK_RUNNER_JAR_PATH environment variable dependency from AppHost
- **Updated**: Project file with proper AssemblyName and RootNamespace

#### 3. Single Comprehensive Test (✅ Completed)
- **Replaced**: Multiple separate tests with single `FlinkDotNet_ComprehensiveValidation_AllJobTypesWork` test
- **Removed**: `FlinkSqlIntegrationTest.cs` (consolidated into main test)
- **Added**: Five test scenarios covering all FlinkDotNet functionality:
  - Basic Map Operation
  - Filter (Where) Operation
  - Timer/Window Operation
  - Side Output Operation
  - Aggregation (GroupBy + Aggregate) Operation
- **Added**: `WaitForJobState` method for proper TDD validation
- **Fixed**: All API calls to use correct FlinkJobBuilder methods (Where vs Filter, GroupBy vs KeyBy)

#### 4. Validation Scripts (✅ Completed)
- **Updated**: `scripts/validate-build-and-tests.ps1` to include LocalTesting solution
- **Verified**: Both FlinkDotNet and LocalTesting solutions build successfully

### Implementation Details
- **Java Integration**: Gateway now checks for Java 17+ and Maven 3.6+ automatically
- **Maven Build**: Uses `mvn clean package -DskipTests` in FlinkIRRunner directory
- **Error Handling**: Clear messages when Java/Maven not available
- **Backward Compatibility**: Still respects FLINK_RUNNER_JAR_PATH if set for transition period
- **API Compatibility**: Test uses actual FlinkJobBuilder API methods (discovered via investigation)

## Phase 5: Testing & Validation
[To be filled]

## Phase 6: Owner Acceptance
[To be filled]

## Lessons Learned & Future Reference (MANDATORY)
[To be filled at completion]