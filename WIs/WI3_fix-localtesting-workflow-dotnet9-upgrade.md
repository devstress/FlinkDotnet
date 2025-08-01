# WI3: Fix Failed Local Testing Workflow

**File**: `WIs/WI3_fix-localtesting-workflow-dotnet9-upgrade.md`
**Title**: [LocalTesting] Fix failed local testing workflow with .NET 9.0 upgrade  
**Description**: Fix failed local testing workflow and ensure all GitHub workflows pass locally by upgrading to .NET 9.0 and updating enforcement rules
**Priority**: High
**Component**: LocalTesting, GitHub Workflows, Infrastructure
**Type**: Bug Fix
**Assignee**: AI Agent
**Created**: 2024-12-20
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- `WIs/WI1_fix-github-workflows-net9.md` - Previous .NET 9 workflow fixes
- `WIs/WI2_fix-localtesting-workflow-dotnet9-upgrade.md` - Previous LocalTesting upgrade attempts
- `WIs/WI1_implement-real-production-aspire-localtesting.md` - Aspire LocalTesting implementation

### Lessons Applied  
- Always check .NET SDK version compatibility before running workflows
- Update global.json and all project files consistently when upgrading .NET versions
- Ensure Aspire workloads are compatible with target .NET version
- Test LocalTesting workflow after any .NET version changes

### Problems Prevented
- Inconsistent .NET versions between local and CI environments
- Aspire workload compatibility issues with mismatched .NET versions
- LocalTesting workflow failures due to SDK version mismatches

## Phase 1: Investigation
### Requirements
- Investigate why LocalTesting workflow is failing locally
- Identify .NET version mismatches between requirements and environment
- Understand Aspire LocalTesting dependencies and requirements
- Review reference project https://github.com/InfinityFlowApp/aspire-temporal for guidance

### Debug Information (MANDATORY - Update this section for every investigation)
- **Error Messages**: 
  ```
  The command could not be loaded, possibly because:
    * You intended to execute a .NET SDK command:
        A compatible .NET SDK was not found.
  
  Requested SDK version: 9.0.303
  global.json file: /home/runner/work/FlinkDotnet/FlinkDotnet/global.json
  
  Installed SDKs:
  8.0.118 [/usr/lib/dotnet/sdk]
  ```

- **Log Locations**: Terminal output from `dotnet --version` and `dotnet workload list` commands
- **System State**: 
  - Current environment has .NET 8.0.118 
  - Repository requires .NET 9.0.303 (specified in global.json)
  - LocalTesting workflow uses Aspire orchestration requiring .NET 9.0
  - GitHub workflows configured for .NET 9.0 but local environment mismatch

- **Reproduction Steps**: 
  1. Navigate to repository root
  2. Run `dotnet --version` - shows .NET 8.0.118
  3. Try `dotnet workload list` - fails due to SDK version mismatch
  4. Try running LocalTesting workflow - fails immediately

- **Evidence**: 
  - `global.json` contains `"version": "9.0.303"`
  - `.github/workflows/build.yml` sets up .NET 9.0.x
  - `.github/workflows/local-testing.yml` requires .NET 9.0 and Aspire workload
  - `LocalTesting/` solution requires .NET 9.0 for Aspire integration

### Findings
1. **Root Cause**: Environment has .NET 8.0.118 but repository requires .NET 9.0.303
2. **Impact**: Cannot run any .NET commands, LocalTesting workflow fails, Aspire workload cannot be installed
3. **Scope**: Affects all local development, testing, and validation workflows
4. **Dependencies**: 
   - .NET 9.0 SDK installation required
   - Aspire workload requires .NET 9.0
   - LocalTesting.AppHost and LocalTesting.WebApi use net9.0 target framework
   - All GitHub workflows expect .NET 9.0 environment

### Lessons Learned
- Always verify local environment matches repository requirements before starting work
- .NET version mismatches cause complete workflow failures, not partial issues
- Aspire orchestration has strict .NET version dependencies

## Phase 2: Design  
### Requirements
- Install .NET 9.0 SDK to match global.json requirements
- Update copilot-instructions.md to enforce .NET 9.0 locally before GitHub workflow submission
- Ensure LocalTesting workflow can run successfully with Aspire orchestration
- Validate all GitHub workflows pass locally after .NET 9.0 upgrade

### Architecture Decisions
- Use official Microsoft .NET 9.0 installation method for consistency
- Update copilot-instructions.md with explicit .NET 9.0 enforcement rules
- Follow patterns from reference project aspire-temporal for Aspire setup
- Maintain existing LocalTesting solution structure, only fixing environment issues

### Why This Approach
- Minimal code changes - primarily environment and documentation updates
- Follows established patterns from previous WI learnings
- Aligns with GitHub workflows already configured for .NET 9.0
- Ensures local-to-CI consistency

### Alternatives Considered
- **Option 1**: Downgrade global.json to .NET 8.0 
  - **Rejected**: GitHub workflows already use .NET 9.0, would require extensive changes
- **Option 2**: Use .NET 8.0 locally and .NET 9.0 in CI
  - **Rejected**: Creates inconsistency and potential compatibility issues
- **Option 3**: Upgrade to .NET 9.0 locally (SELECTED)
  - **Reason**: Matches existing CI configuration, minimal changes required

## Phase 3: TDD/BDD
### Test Specifications
- .NET 9.0 SDK installation verification test
- Aspire workload installation verification test  
- LocalTesting workflow execution test
- GitHub workflow local execution tests

### Behavior Definitions
```gherkin
Feature: Local Testing Workflow Execution
  Scenario: Developer runs LocalTesting workflow locally
    Given .NET 9.0 SDK is installed
    And Aspire workload is available
    When developer runs LocalTesting workflow
    Then all services start successfully
    And LocalTesting API is accessible
    And Aspire dashboard is functional

Feature: GitHub Workflow Local Validation
  Scenario: All GitHub workflows pass locally
    Given .NET 9.0 environment is configured
    When developer runs each GitHub workflow locally
    Then all workflows complete successfully
    And no version compatibility errors occur
```

## Phase 4: Implementation
### Code Changes
1. **Installed .NET 9.0.303 SDK** locally using official Microsoft installation script
2. **Installed Aspire workload** (`dotnet workload install aspire`) compatible with .NET 9.0
3. **Updated copilot-instructions.md** to enforce .NET 9.0 locally:
   - Changed reference from ".NET 8" to ".NET 9" in main description
   - Added comprehensive Rule 13: .NET 9.0 Environment Requirements (CRITICAL)
   - Added mandatory verification commands for PR submission
   - Added environment consistency enforcement rules

### Challenges Encountered
1. **Initial .NET Version Mismatch**: Repository required .NET 9.0.303 but environment had .NET 8.0.118
   - **Solution**: Downloaded and installed .NET 9.0.303 using official installation script
   
2. **PATH Environment Configuration**: New .NET installation required PATH update for session
   - **Solution**: Added `export PATH="/home/runner/.dotnet:$PATH"` to session commands

3. **Aspire Workload Compatibility**: Needed Aspire workload for LocalTesting functionality  
   - **Solution**: Installed aspire workload after .NET 9.0 installation succeeded

### Solutions Applied
- **Environment Setup**: Followed Microsoft official .NET 9.0 installation procedures
- **Verification Testing**: Built all solutions (FlinkDotNet, Sample, LocalTesting) successfully
- **Documentation Updates**: Added comprehensive .NET 9.0 enforcement rules to copilot-instructions.md
- **Consistency Validation**: Ensured local environment matches CI environment requirements

## Phase 5: Testing & Validation
### Test Results
*To be documented during testing*

### Performance Metrics
*To be documented during testing*

## Phase 6: Owner Acceptance
### Demonstration
*To be documented during owner review*

### Owner Feedback
*To be documented during owner review*

### Final Approval
*To be documented during owner review*

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
*To be documented after completion*

### What Could Be Improved  
*To be documented after completion*

### Key Insights for Similar Tasks
*To be documented after completion*

### Specific Problems to Avoid in Future
*To be documented after completion*

### Reference for Future WIs
*To be documented after completion*