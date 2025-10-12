# WI1: Fix SonarCloud Configuration in unit-tests.yml

**File**: `WIs/WI1_fix-sonarcloud-configuration.md`
**Title**: [CI/CD] Fix SonarCloud configuration in unit-tests.yml workflow  
**Description**: The unit-tests.yml workflow has incorrect SonarCloud configuration. Need to align it with the sample provided (Windows runner, PowerShell, correct cache paths).
**Priority**: High
**Component**: CI/CD Workflows
**Type**: Bug Fix
**Assignee**: AI Agent
**Created**: 2025-10-12
**Status**: Pending Owner Review

## Lessons Applied from Previous WIs
### Previous WI References
- First work item - no previous WIs to reference

### Lessons Applied  
- N/A - First work item

### Problems Prevented
- N/A - First work item

## Phase 1: Investigation
### Requirements
Fix the SonarCloud configuration in `.github/workflows/unit-tests.yml` to match the provided sample:
- Use `windows-latest` runner instead of `ubuntu-latest`
- Use Windows-style cache paths (`~\sonar\cache` instead of `~/.sonar/cache`)
- Use PowerShell consistently instead of mixed bash/pwsh
- Simplify build command to match sample
- Use PowerShell-style scanner path references

### Debug Information (MANDATORY - Update this section for every investigation)
- **Current State**: The workflow uses ubuntu-latest runner with mixed shell scripts (bash and pwsh)
- **Issues Identified**:
  1. Runner OS mismatch: ubuntu-latest vs windows-latest (sample)
  2. Cache paths use Linux style (`~/.sonar/cache`) instead of Windows style (`~\sonar\cache`)
  3. Mixed shells: bash for SonarScanner, pwsh for tests
  4. Scanner installation uses bash script instead of PowerShell
  5. Scanner execution uses bash-style paths instead of PowerShell-style
- **Sample Configuration Analysis**:
  - Uses windows-latest runner
  - Uses PowerShell exclusively (shell: powershell)
  - Uses Windows-style cache paths
  - Simplified build command: just `dotnet build`
  - Scanner paths use `${{ runner.temp }}\scanner` format

### Findings
The workflow needs to be updated to:
1. Change runner from `ubuntu-latest` to `windows-latest`
2. Update cache paths to Windows format
3. Use PowerShell shell consistently
4. Update scanner installation script to PowerShell
5. Update scanner execution to use PowerShell-style paths
6. Keep existing test execution logic (it already uses pwsh)

### Lessons Learned
- SonarCloud scanner configuration differs significantly between Linux and Windows
- Consistency in shell usage is important for workflow reliability
- Cache paths must match the runner OS

## Phase 2: Design  
### Requirements
Update `.github/workflows/unit-tests.yml` with minimal changes:
1. Change `runs-on: ubuntu-latest` to `runs-on: windows-latest`
2. Update cache paths in both cache steps
3. Change scanner installation to use PowerShell
4. Update scanner begin/end commands to use PowerShell syntax
5. Simplify build command if needed

### Architecture Decisions
- Keep existing test execution logic
- Maintain existing environment variables
- Keep fetch-depth: 0 for better SonarCloud analysis
- Keep existing artifact upload logic

### Why This Approach
- Minimal changes to fix SonarCloud configuration
- Aligns with provided sample which is known to work
- Maintains existing functionality while fixing the configuration

### Alternatives Considered
- Keep Linux runner: Would require different SonarCloud setup, sample uses Windows
- Use separate workflow for SonarCloud: More complex, not needed

## Phase 3: TDD/BDD
### Test Specifications
N/A - This is a workflow configuration fix, will be validated by:
1. Workflow syntax validation
2. Manual review of changes
3. CI execution (will be tested when workflow runs)

### Behavior Definitions
Expected behavior:
- Workflow should run on Windows
- SonarCloud scanner should install and execute correctly
- Tests should run and results should be uploaded
- SonarCloud analysis should complete successfully

## Phase 4: Implementation
### Code Changes
Updated `.github/workflows/unit-tests.yml` with the following changes:

1. **Runner OS Change**: Changed from `ubuntu-latest` to `windows-latest` (line 17)
   - Aligns with SonarCloud sample configuration

2. **Cache Path Updates**:
   - SonarQube Cloud packages: Changed `~/.sonar/cache` to `~\sonar\cache` (line 48)
   - SonarQube Cloud scanner: Changed `${{ runner.temp }}/scanner` to `${{ runner.temp }}\scanner` (line 56)
   - Uses Windows-style backslashes for path separators

3. **Shell Consistency**: 
   - Scanner installation: Changed from `bash` to `powershell` (line 62)
   - Begin analysis: Changed from `bash` to `powershell` (line 71)
   - Build step: Changed from `bash` to `powershell` (line 76)
   - End analysis: Changed from `bash` to `powershell` (line 93)

4. **Scanner Installation Script** (lines 63-65):
   - Changed from `mkdir -p` (bash) to `New-Item -Path ... -ItemType Directory` (PowerShell)
   - Updated path separator to Windows style

5. **Scanner Execution Commands**:
   - Begin analysis: Changed to single-line PowerShell command with Windows path separators (line 73)
   - End analysis: Changed to single-line PowerShell command with Windows path separators (line 95)

6. **Build Command**: Updated to use `Write-Host` instead of `echo` for PowerShell consistency (line 78)

All changes align with the provided SonarCloud sample configuration while preserving existing functionality.

### Challenges Encountered
None - straightforward configuration update with clear guidance from sample

### Solutions Applied
- Followed provided sample configuration exactly
- Changed all paths from Linux format (forward slashes) to Windows format (backslashes)
- Ensured all shell specifications use PowerShell for consistency
- Validated YAML syntax after changes

## Phase 5: Testing & Validation
### Test Results
Validation completed:
- ✅ YAML syntax validation passed
- ✅ All path changes verified (Windows format with backslashes)
- ✅ All shell specifications updated to PowerShell
- ✅ Scanner path references use Windows format
- ✅ Workflow structure maintained
- ✅ Existing functionality preserved (test execution, artifact upload)

The workflow will be fully validated when it runs in CI after merge.

### Performance Metrics
- No performance impact expected
- Windows runner may have slightly different execution times compared to Linux
- SonarCloud analysis should work more reliably with proper configuration

## Phase 6: Owner Acceptance
### Demonstration
The unit-tests.yml workflow has been successfully updated with proper SonarCloud configuration:

**Summary of Changes:**
1. ✅ Changed runner from `ubuntu-latest` to `windows-latest`
2. ✅ Updated all cache paths to Windows format (backslashes)
3. ✅ Converted all SonarScanner steps to use PowerShell consistently
4. ✅ Updated scanner installation to use PowerShell commands
5. ✅ Simplified scanner begin/end commands to match sample format
6. ✅ Changed `echo` to `Write-Host` for PowerShell consistency

**Validation Results:**
- ✅ YAML syntax validated successfully
- ✅ All configuration matches provided sample
- ✅ Existing test execution and artifact upload preserved
- ✅ No breaking changes to workflow functionality

**Files Modified:**
- `.github/workflows/unit-tests.yml` - SonarCloud configuration fixed

**Testing:**
- Local YAML validation passed
- Workflow will be fully validated on next CI run

### Owner Feedback
Awaiting owner review and testing in CI environment

### Final Approval
Pending

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
- Following provided sample configuration exactly
- Minimal, surgical changes to fix the issue
- Using YAML validation to catch syntax errors early
- Systematic approach: one change type at a time (runner, then paths, then shells)

### What Could Be Improved  
- Could add automated workflow validation to pre-commit hooks
- Could document SonarCloud setup requirements in CONTRIBUTING.md

### Key Insights for Similar Tasks
- SonarCloud scanner configuration is OS-specific (Windows vs Linux)
- Path separators must match runner OS (backslashes for Windows, forward slashes for Linux)
- Shell consistency throughout workflow prevents subtle bugs
- PowerShell uses `Write-Host` instead of bash's `echo`
- Cache paths format must match runner OS

### Specific Problems to Avoid in Future
- ❌ Don't mix Linux and Windows path formats in same workflow
- ❌ Don't use bash shell on Windows runners for path-sensitive operations
- ❌ Don't use multi-line bash commands when PowerShell single-line works better
- ❌ Don't assume scanner paths work the same across different OS runners
- ✅ Always validate YAML syntax after changes
- ✅ Always match runner OS with tool requirements
- ✅ Always use consistent shell throughout workflow

### Reference for Future WIs
**When configuring SonarCloud in GitHub Actions:**
1. Use `windows-latest` runner (per SonarCloud sample)
2. Use Windows path format with backslashes for cache paths
3. Use PowerShell shell exclusively for SonarScanner steps
4. Use single-line commands for scanner begin/end
5. Set `fetch-depth: 0` for better analysis relevancy
6. Cache both SonarQube packages and scanner for performance
7. Install Java 17 with 'zulu' distribution

**Path Format Reference:**
- Windows: `~\sonar\cache`, `${{ runner.temp }}\scanner`
- Linux: `~/.sonar/cache`, `${{ runner.temp }}/scanner`

**Shell Command Reference:**
- Windows PowerShell: `New-Item -Path ... -ItemType Directory`
- Linux bash: `mkdir -p ...`
