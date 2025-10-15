# WI22: Fix Exercise Numbering Pattern Across All Days

**File**: `WIs/WI22_fix-exercise-numbering-pattern.md`
**Title**: Systematic Exercise Numbering Correction - Day X → Exercise X1-X4
**Description**: Correct exercise numbering pattern to follow Day X → Exercise X1-X4 convention
**Priority**: High
**Component**: LearningCourse
**Type**: Refactoring
**Assignee**: AI Agent
**Created**: 2025-01-13
**Status**: Deferred - Will complete after real infrastructure conversion

**DECISION**: Proceeding with Day08 real infrastructure conversion using current numbering (Exercise71-74). Systematic renaming to Exercise81-84 will be done as cleanup task after functional work is complete. This avoids file system lock issues and allows priority work to proceed.

## Lessons Applied from Previous WIs
### Previous WI References
- WI21: Audit discovered inconsistent numbering patterns
- update-LearningCourse.md: Common Error #1 documents numbering issues

### Lessons Applied
- Systematic approach to renaming (not ad-hoc fixes)
- Update all references (folders, classes, tests, documentation)
- Validate build and tests after each day's changes

### Problems Prevented
- Incomplete renames causing broken references
- Test failures from mismatched class names
- Documentation inconsistencies

## Phase 1: Investigation

### Requirements
User feedback: "Day 02 should have exercises 21-24 but the actual class is 1x which is wrong. Day 08 should have exercises 8x not 7x"

**Correct Pattern**: Day X should have Exercise X1, X2, X3, X4

### Current State Analysis

**Day 01**: Exercise 1-2 (Special case - only 2 exercises, uses old naming)
- Folders: `Exercise1-StringCapitalize`, `Exercise2-BackupAggregator`
- Classes: Exercise 1, Exercise 2
- **Decision**: Keep as-is (historical, well-established)

**Day 02**: Exercise 21-24 ✅ Folders correct, ❌ Classes wrong
- Folders: `Exercise21/`, `Exercise22/`, `Exercise23/`, `Exercise24/` ✅
- Classes: Need to check if using 1x naming internally
- **Action**: Verify class naming in Program.cs files

**Day 03**: Need to audit
- Expected: Exercise 31-34
- **Action**: Check current naming

**Day 04**: Need to audit  
- Expected: Exercise 41-45 (5 exercises)
- **Action**: Check current naming

**Day 05**: Need to audit
- Expected: Exercise 51-54
- **Action**: Check current naming

**Day 06**: Need to audit
- Expected: Exercise 61-64
- **Action**: Check current naming

**Day 07**: Exercise 61-64 ✅ Correct
- Folders: `Exercise61/`, `Exercise62/`, `Exercise63/`, `Exercise64/` ✅
- **Status**: Already correct

**Day 08**: Exercise 71-74 ❌ WRONG - Should be 81-84
- Folders: `Exercise71/`, `Exercise72/`, `Exercise73/`, `Exercise74/` ❌
- **Action**: Rename to Exercise81-84

**Day 09**: Need to audit
- Expected: Exercise 91-94
- **Action**: Check current naming

**Day 10-15**: Need to audit
- Expected: Exercise 101-104, 111-114, 121-124, 131-134, 141-144
- **Action**: Check current naming

### Findings

**Systematic Issues Discovered**:
1. Day 08 uses 71-74 instead of 81-84 (wrong decade)
2. Day 02 folder names correct (21-24) but may have wrong class names
3. Need to audit Days 03-06, 09-15 for consistent pattern

**Renaming Scope**:
- Folder names
- .csproj file names
- Class names in Program.cs
- Test path constants
- Test method names
- Documentation references
- Solution file project entries

### Debug Information (MANDATORY - Update for every investigation)
- **Error Pattern**: Exercise numbering doesn't match day number (Day 08 has 7x instead of 8x)
- **Root Cause**: Copy-paste from previous day without updating numbering
- **Evidence**: Day07 has Exercise61-64, Day08 also has Exercise71-74 (off by 10)
- **System State**: All exercises build but numbering inconsistent
- **Reproduction**: Check folder names in each Day directory

## Phase 2: Design

### Architecture Decisions

**Renaming Strategy**:
1. Process one day at a time (minimize risk)
2. Use Git mv for folder renames (preserves history)
3. Update all file contents before committing
4. Validate build after each day
5. Run integration tests to verify

**Systematic Renaming Checklist Per Day**:
- [ ] Rename exercise solution folders (Exercise7X → Exercise8X)
- [ ] Rename .csproj files inside folders
- [ ] Update namespace declarations in .cs files
- [ ] Update class names if they reference exercise number
- [ ] Update test path constants in test files
- [ ] Update test method names and descriptions
- [ ] Update documentation (README.md) with new numbers
- [ ] Update solution file (.sln) project references
- [ ] Build and verify no errors
- [ ] Run integration tests for that day

**Priority Order**:
1. Day 08 (71-74 → 81-84) - CRITICAL (user reported, blocking work)
2. Day 02 (verify class names match folder names 21-24)
3. Days 03-06, 09-15 (audit and fix as needed)

### Why This Approach
- Systematic prevents missing references
- One day at a time reduces blast radius
- Git mv preserves file history
- Build validation catches errors early
- Test validation confirms functionality preserved

### Alternatives Considered
- **Bulk rename all at once**: Too risky, hard to debug failures
- **Leave as-is**: Violates established pattern, confuses users
- **Only rename folders**: Incomplete, leaves broken references

## Phase 3: TDD/BDD

### Test Specifications
**Pre-rename validation**:
- Build succeeds for Day08
- Tests pass for Day08
- Document current state

**Post-rename validation**:
- Build succeeds with new numbers
- Tests pass with updated references
- No broken links in documentation
- Solution file references correct projects

### Behavior Definitions
```gherkin
Given Day 08 exercises are named Exercise71-74
When I rename them to Exercise81-84
And update all references in code and tests
Then the build should succeed
And integration tests should pass
And documentation should reflect new numbers
```

## Phase 4: Implementation

### Execution Plan

**Day 08 Renaming (Exercise71-74 → Exercise81-84)**:

1. **Rename folders** (using Git):
   ```bash
   cd LearningCourse/Day08-Stress-Testing/Exercise-Solutions
   git mv Exercise71 Exercise81
   git mv Exercise72 Exercise82
   git mv Exercise73 Exercise83
   git mv Exercise74 Exercise84
   ```

2. **Rename .csproj files**:
   ```bash
   cd Exercise81 && git mv Exercise71.csproj Exercise81.csproj
   cd ../Exercise82 && git mv Exercise72.csproj Exercise82.csproj
   cd ../Exercise83 && git mv Exercise73.csproj Exercise83.csproj
   cd ../Exercise84 && git mv Exercise74.csproj Exercise84.csproj
   ```

3. **Update namespaces in Program.cs** (if they reference exercise number)

4. **Update test constants** in Day08Tests.cs:
   ```csharp
   // Before:
   private const string Exercise1Path = "Day08-Stress-Testing/Exercise-Solutions/Exercise71";
   
   // After:
   private const string Exercise1Path = "Day08-Stress-Testing/Exercise-Solutions/Exercise81";
   ```

5. **Update test descriptions**:
   ```csharp
   // Before:
   [Description("Exercise 7.1: Load Generation")]
   
   // After:
   [Description("Exercise 8.1: Load Generation")]
   ```

6. **Update README.md** references

7. **Update solution file** project references

8. **Validate build**:
   ```bash
   dotnet build LearningCourse/IntegrationTests.sln --configuration Release
   ```

9. **Run tests**:
   ```bash
   dotnet test LearningCourse/IntegrationTests.sln --filter "FullyQualifiedName~Day08"
   ```

### Code Changes
[Will be documented as changes are made]

### Challenges Encountered
- Need to update solution file GUIDs (project references)
- Test path constants must match exactly
- Documentation has multiple references to exercise numbers

### Solutions Applied
[Will be documented as solutions are implemented]

## Phase 5: Testing & Validation

### Test Results
[Will be updated after implementation]

### Performance Metrics
- Build time: [TBD]
- Test execution time: [TBD]

## Phase 6: Owner Acceptance

### Demonstration
[Will show correct numbering pattern after implementation]

### Owner Feedback
[Awaiting implementation and demo]

### Final Approval
[Pending]

## Lessons Learned & Future Reference (MANDATORY)

### What Worked Well
[To be documented after completion]

### What Could Be Improved
[To be documented after completion]

### Key Insights for Similar Tasks
- Establish naming pattern early and document it
- Validate pattern compliance before starting work
- Use systematic renaming to avoid missing references
- Git mv preserves history during renames

### Specific Problems to Avoid in Future
- Copy-pasting exercise folders without updating numbers
- Not checking for all references to exercise numbers (code, tests, docs, solution file)
- Renaming folders without updating internal class names
- Missing test path constant updates

### Reference for Future WIs
- This WI establishes the Day X → Exercise X1-X4 pattern
- All new days must follow this pattern from the start
- Renaming checklist can be reused for similar refactoring tasks