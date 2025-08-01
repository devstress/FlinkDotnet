# WI1: Fix Failed GitHub Workflows for .NET 9 Upgrade

**File**: `WIs/WI1_fix-github-workflows-net9.md`
**Title**: [GitHub Workflows] Fix all failed GitHub workflows until everything working  
**Description**: Address comment from @devstress to fix all failed GitHub workflows after .NET 9 upgrade
**Priority**: High
**Component**: GitHub Workflows
**Type**: Bug Fix  
**Assignee**: AI Agent
**Created**: 2024-12-28
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- No previous WIs in this repository yet
### Lessons Applied  
- Starting with systematic investigation before making changes
### Problems Prevented
- N/A - First WI in repository

## Phase 1: Investigation
### Requirements
Fix all failed GitHub workflows to make them working with .NET 9 upgrade. The comment specifically requests "Fix all failed GitHub workflows until everything working".

### Debug Information (MANDATORY - Update this section for every investigation)
- **Error Messages**: Local environment shows SDK not found error: "Requested SDK version: 9.0.303 [...] Installed SDKs: 8.0.118"
- **Log Locations**: GitHub Actions logs (need to check workflow runs)
- **System State**: Repository upgraded to .NET 9.0.303 in global.json, but workflows contain mixed references
- **Reproduction Steps**: 
  1. Repository was upgraded from .NET 8 to .NET 9
  2. Workflows still contain some .NET 8 references in comments and paths
  3. Build artifact paths reference `net8.0` instead of `net9.0`
- **Evidence**: Analysis of workflow files shows:
  - Comments still say "Set up .NET 8.0" (should be .NET 9.0)
  - Build artifact checks reference `net8.0/*.dll` paths (should be `net9.0`)
  - Upload artifact paths include `net8.0` directories (should be `net9.0`)

### Findings
**Issues Identified:**
1. **Inconsistent comments**: Multiple workflows have comments saying "Set up .NET 8.0" but correctly use `dotnet-version: '9.0.x'`
2. **Wrong build artifact paths**: Several workflows check for `net8.0/*.dll` files but should check `net9.0/*.dll`
3. **Wrong upload paths**: Artifact uploads reference `net8.0/` directories but should reference `net9.0/`

**Affected Workflows:**
- `unit-tests.yml`: Line 41 checks `net8.0/*.dll`
- `integration-tests.yml`: Lines 38, 109, 110 reference `net8.0` paths  
- `reliability-tests.yml`: Lines 42, 97, 98 reference `net8.0` paths
- `stress-tests-confluent.yml`: Lines 42, 97, 98 reference `net8.0` paths
- `backpressure-tests.yml`: Lines 42, 100, 101 reference `net8.0` paths
- Multiple files have "Set up .NET 8.0" comments

### Lessons Learned
- Systematic file analysis revealed the scope of changes needed
- The .NET version upgrade requires updating both runtime configuration AND build artifact paths

## Phase 2: Design  
### Requirements
Update all GitHub workflow files to properly reference .NET 9 and `net9.0` paths

### Architecture Decisions
1. **Comment Updates**: Change all "Set up .NET 8.0" to "Set up .NET 9.0" for consistency
2. **Build Artifact Path Updates**: Change all `net8.0` references to `net9.0` in:
   - Build artifact existence checks  
   - Artifact upload path specifications
3. **Validation Strategy**: Update each file systematically and verify changes

### Why This Approach
- Ensures all workflows consistently reference the correct .NET version
- Prevents build failures due to missing artifact paths
- Maintains clarity in workflow documentation

### Alternatives Considered
- Could leave comments as-is, but that would be confusing
- Could use wildcard paths, but explicit paths are clearer

## Phase 3: TDD/BDD
### Test Specifications
- Each workflow file should reference .NET 9 consistently
- All `net8.0` paths should be changed to `net9.0`
- No workflow should have mixed version references

### Behavior Definitions
- Comments should accurately reflect the .NET version being used
- Build artifact paths should match the actual output directories
- Upload paths should match the build output structure

## Phase 4: Implementation
### Code Changes
Will update the following files:
1. `.github/workflows/build.yml` - comment update
2. `.github/workflows/unit-tests.yml` - comment + build artifact path
3. `.github/workflows/integration-tests.yml` - comment + build artifact paths + upload paths
4. `.github/workflows/reliability-tests.yml` - comment + build artifact paths + upload paths  
5. `.github/workflows/stress-tests-confluent.yml` - comment + build artifact paths + upload paths
6. `.github/workflows/backpressure-tests.yml` - comment + build artifact paths + upload paths

### Challenges Encountered
- Multiple files need coordinated updates
- Need to ensure no paths are missed

### Solutions Applied
- Systematic file-by-file approach
- Careful validation of each change

## Phase 5: Testing & Validation
### Test Results
- TBD after implementation

### Performance Metrics
- TBD after implementation

## Phase 6: Owner Acceptance
### Demonstration
- TBD

### Owner Feedback
- TBD

### Final Approval
- TBD

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
- TBD after completion

### What Could Be Improved  
- TBD after completion

### Key Insights for Similar Tasks
- TBD after completion

### Specific Problems to Avoid in Future
- TBD after completion

### Reference for Future WIs
- TBD after completion