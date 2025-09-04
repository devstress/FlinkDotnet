# WI11: Fix Observability Workflow YAML Syntax and Simplify GitHub Actions

**File**: `WIs/WI11_fix-observability-workflow-yaml-syntax.md`
**Title**: [GitHub Actions] Fix YAML syntax error and simplify observability tests workflow  
**Description**: Fix invalid workflow file error in .github/workflows/build.yml#L70 calling observability-tests.yml with YAML syntax error on line 145. Simplify workflow to only run dotnet test and ensure observability metrics are printed in C# tests.
**Priority**: High
**Component**: GitHub Actions / CI/CD
**Type**: Bug Fix
**Assignee**: AI Agent
**Created**: $(date)
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI5_fix-observability-test-nullref.md: NullReferenceException fixes and defensive programming patterns
- WI10_observability-tests-aspire-framework-fix.md: Aspire testing framework integration challenges
- WI1_move-observability-tests-to-localtesting.md: Moving observability tests to LocalTesting solution

### Lessons Applied  
- **Defensive Programming**: Implement null checking and proper error handling from WI5
- **Environment Requirements**: .NET 9.0 dependency from WI10 - ensure workflow works in CI environment
- **Simplified Approach**: Basic approach more reliable than complex setups (from WI5)
- **Clear Error Messages**: Helps identify infrastructure connectivity issues (from WI5)
- **Fail-Fast Pattern**: Validate prerequisites early (from WI10)

### Problems Prevented
- Avoid complex PowerShell scripts that can introduce YAML syntax errors
- Prevent environment-specific issues by keeping workflow simple
- Avoid over-engineering the workflow setup (learned from WI5 experience)

## Phase 1: Investigation
### Requirements
Fix the YAML syntax error on line 145 of .github/workflows/observability-tests.yml and simplify the GitHub Actions workflow to only run dotnet test while ensuring observability metrics messages per second are printed in C# test output.

### Debug Information (MANDATORY - Update this section for every investigation)
- **Error Messages**: 
  ```
  Invalid workflow file: .github/workflows/build.yml#L70 
  error parsing called workflow ".github/workflows/build.yml" -> "./.github/workflows/observability-tests.yml" 
  (source branch with sha:e02a1777f1e9888c9eb041da375b5b6a3face6f1) : 
  You have an error in your yaml syntax on line 145
  ```
- **Log Locations**: GitHub Actions workflow validation errors when parsing .github/workflows/observability-tests.yml
- **System State**: 
  - Current observability-tests.yml has complex PowerShell setup with 145+ lines
  - C# tests already print observability metrics per second (verified in ObservabilityMetricsSteps.cs)
  - Environment issues with .NET version requirements
- **Reproduction Steps**: 
  1. GitHub Actions tries to parse .github/workflows/build.yml
  2. build.yml calls observability-tests.yml on line 70 
  3. YAML parser fails on line 145 of observability-tests.yml
  4. Complex PowerShell script structure causing syntax issues
- **Evidence**: 
  - observability-tests.yml contains complex multi-line PowerShell scripts
  - C# ObservabilityMetricsSteps.cs already includes Console.WriteLine for metrics output
  - **ROOT CAUSE IDENTIFIED**: Line 144-155 contains PowerShell here-string with HTML content and $(Get-Date) interpolation causing YAML parser issues:
    ```powershell
    $basicReport = @"
    <!DOCTYPE html>
    <html>
    <head><title>Observability Test Report</title></head>
    <body>
    <h1>Aspire Observability Test Report</h1>
    <p>Generated: $(Get-Date)</p>
    ```
  - YAML parser cannot handle the complex PowerShell here-string syntax with HTML and string interpolation

### Findings
1. **YAML Syntax Issue**: Complex PowerShell scripts in observability-tests.yml causing syntax parsing errors
2. **Over-Engineering**: Workflow has complex setup when simple dotnet test would suffice
3. **Metrics Output Already Implemented**: C# tests already print observability metrics per second
4. **Environment Complexity**: Complex .NET workload installation and artifact handling

### Lessons Learned
- Complex workflows are prone to YAML syntax errors
- PowerShell scripts in YAML require careful escaping and formatting
- Simpler approaches are more reliable and maintainable

## Phase 2: Design  
### Requirements
Create a simplified observability workflow that:
1. Sets up .NET 9.0 environment
2. Builds LocalTesting solution 
3. Runs dotnet test for observability tests
4. Relies on existing C# test output for metrics display

### Architecture Decisions
- **Minimal Workflow**: Remove complex PowerShell scripting
- **Standard Actions**: Use established GitHub Actions patterns
- **Direct Test Execution**: Run dotnet test directly without complex setup
- **Leverage Existing Code**: Use existing C# metrics output in ObservabilityMetricsSteps.cs

### Why This Approach
- **Reliability**: Simpler workflows are less prone to syntax errors
- **Maintainability**: Standard GitHub Actions patterns are easier to maintain
- **Debugging**: Fewer layers of abstraction make issues easier to diagnose
- **Performance**: Less overhead from complex setup scripts

### Alternatives Considered
1. **Fix existing complex workflow**: Risk of introducing more syntax issues
2. **Completely remove workflow**: Would lose observability testing in CI
3. **Move to different CI system**: Unnecessary complexity for this issue

## Phase 3: TDD/BDD
### Test Specifications
- Validate YAML syntax is correct
- Ensure workflow runs successfully in CI environment
- Verify observability metrics are displayed in test output
- Confirm simplified workflow maintains test functionality

### Behavior Definitions
```yaml
# Expected workflow behavior:
# 1. Setup .NET 9.0 environment
# 2. Build LocalTesting solution
# 3. Run observability tests with dotnet test
# 4. Display metrics output from C# tests
```

## Phase 4: Implementation
### Code Changes

**1. Simplified observability-tests.yml workflow**
- **Removed**: Complex PowerShell scripting that was causing YAML syntax errors
- **Removed**: Complex Allure report generation with here-strings and string interpolation
- **Removed**: Multi-line PowerShell scripts with HTML content generation
- **Simplified**: Workflow now uses basic bash commands instead of PowerShell
- **Maintained**: Core functionality - builds LocalTesting solution and runs observability tests
- **Maintained**: .NET 9.0 setup and Aspire workload installation
- **Maintained**: Environment variables for test configuration

**Key Changes Made:**
```yaml
# Before: Complex PowerShell with here-strings causing YAML parsing errors
defaults:
  run:
    shell: pwsh
# Complex PowerShell scripts with HTML generation

# After: Simple bash commands
steps:
  - name: Run Observability Tests
    run: |
      echo "🎭 Running observability tests..."
      cd LocalTesting
      dotnet test LocalTesting.IntegrationTests/LocalTesting.IntegrationTests.csproj \
        --configuration Release \
        --logger "console;verbosity=detailed" \
        --no-build \
        --filter "Category=observability"
```

**2. Verified C# test output already includes metrics per second**
- ObservabilityMetricsSteps.cs lines 120-170 already include comprehensive console output:
  ```csharp
  Console.WriteLine($"📊 Kafka Producer Rate: {producerRate.GetDouble():F2} messages/second");
  Console.WriteLine($"⚡ Flink Processing Rate: {processingRate.GetDouble():F2} messages/second");
  Console.WriteLine($"🔄 Temporal Workflow Rate: {workflowRate.GetDouble():F2} workflows/second");
  Console.WriteLine($"🚀 End-to-End Flow Rate: {endToEndRate.GetDouble():F2} messages/second");
  ```

### Challenges Encountered
- **YAML Syntax Complexity**: PowerShell here-strings with HTML content and string interpolation were incompatible with YAML parsing
- **Over-Engineering**: Previous workflow had unnecessary complexity for Allure report generation
- **Multi-line String Handling**: YAML parser struggled with complex PowerShell @" "@ here-string syntax

### Solutions Applied
- **Workflow Simplification**: Removed all complex PowerShell scripting in favor of simple bash commands
- **Focused Approach**: Kept only essential functionality - build solution and run tests
- **Leverage Existing Code**: Relied on existing C# test output for metrics display instead of workflow-level formatting
- **Standard GitHub Actions**: Used established patterns instead of custom PowerShell scripts

## Phase 5: Testing & Validation
### Test Results
[To be filled during testing]

### Performance Metrics
[To be filled during testing]

## Phase 6: Owner Acceptance
### Demonstration
[To be filled during acceptance]

### Owner Feedback
[To be filled during acceptance]

### Final Approval
[To be filled during acceptance]

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