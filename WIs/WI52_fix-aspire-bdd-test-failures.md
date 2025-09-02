# WI52: Fix Aspire BDD Test Failures

**File**: `WIs/WI52_fix-aspire-bdd-test-failures.md`
**Title**: [Tests] Fix Aspire BDD integration test failures with missing step definitions  
**Description**: Fix failing Aspire integration tests due to missing BDD step definitions and compilation errors
**Priority**: High
**Component**: Aspire Integration Tests
**Type**: Bug Fix
**Assignee**: Copilot
**Created**: 2024-12-19
**Status**: Debugging

## Lessons Applied from Previous WIs
### Previous WI References
- No previous WIs found for this specific issue
### Lessons Applied  
- Always check .NET version compatibility first
- Debug compilation errors before running tests
- Verify all step definitions exist for BDD scenarios
### Problems Prevented
- Environment compatibility issues by installing .NET 9.0 first

## Phase 1: Investigation
### Requirements
- Fix all Aspire integration test failures
- Ensure all BDD scenarios have proper step definitions
- Resolve compilation errors
- Make GitHub workflows pass

### Debug Information (MANDATORY - Update this section for every investigation)
- **Error Messages**: 
  ```
  CS0103: The name '_testData' does not exist in the current context
  CS0103: The name '_output' does not exist in the current context
  ```
  From ComplexLogicStressTestStepDefinitions.cs around lines 755, 756, 767, etc.

- **Log Locations**: Build output from dotnet build command
- **System State**: 
  - .NET 9.0.100 installed and functional
  - Aspire workload installed
  - All step definition files exist: BackpressureTestStepDefinitions.cs, ComplexLogicStressTestStepDefinitions.cs, ReliabilityTestStepDefinitions.cs, StressTestStepDefinitions.cs

- **Reproduction Steps**: 
  1. `export PATH="/home/runner/.dotnet:$PATH"`
  2. `cd /home/runner/work/FlinkDotnet/FlinkDotnet/Sample/FlinkDotNet.Aspire.IntegrationTests`
  3. `dotnet build --configuration Release`
  4. Compilation fails with CS0103 errors

- **Evidence**: The ComplexLogicStressTestStepDefinitions.cs file shows proper class structure with _testData and _output fields declared, but compilation errors suggest scope issues

### Findings
- .NET 9.0 environment now correctly set up
- Compilation errors in ComplexLogicStressTestStepDefinitions.cs preventing tests from running
- File structure appears correct but there may be missing step definitions causing context issues
- Need to identify and fix missing step definitions that are causing test failures

### Lessons Learned
- Always verify .NET version compatibility before attempting builds
- Compilation errors must be resolved before running BDD tests

## Phase 2: Design  
### Requirements
- Fix compilation errors in ComplexLogicStressTestStepDefinitions.cs
- Add any missing step definitions for BDD scenarios  
- Ensure all GitHub workflows pass locally

### Architecture Decisions
- Focus on minimal changes to fix compilation issues
- Add only missing step definitions required for tests to compile and run
- Maintain existing BDD test structure and patterns

### Why This Approach
- Surgical fix approach to avoid breaking existing functionality
- Target specific compilation errors rather than rewriting files
- Focus on making tests executable first, then address test logic

### Alternatives Considered
- Complete rewrite of step definition files (rejected - too risky)
- Skip failing tests (rejected - need all tests working)

## Phase 3: TDD/BDD
### Test Specifications
- All BDD scenarios should compile without errors
- Integration tests should run without missing step definition errors
- GitHub workflows should pass

### Behavior Definitions
- Fix compilation errors in ComplexLogicStressTestStepDefinitions.cs
- Verify all feature files have corresponding step definitions
- Ensure tests can run locally before committing

## Phase 4: Implementation
### Code Changes
[To be updated as changes are made]

### Challenges Encountered
[To be updated as work progresses]

### Solutions Applied
[To be updated as solutions are implemented]

## Phase 5: Testing & Validation
### Test Results
[To be updated after fixes are implemented]

### Performance Metrics
[To be updated after successful test runs]

## Phase 6: Owner Acceptance
### Demonstration
[To be provided after fixes are complete]

### Owner Feedback
[To be captured]

### Final Approval
[Pending]

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
[To be documented after completion]

### What Could Be Improved  
[To be documented after completion]

### Key Insights for Similar Tasks
[To be documented after completion]

### Specific Problems to Avoid in Future
[To be documented after completion]

### Reference for Future WIs
[To be documented after completion]