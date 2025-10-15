# WI27: Fix Exercise Numbering to Match Day Numbers

**File**: `WIs/WI27_fix-exercise-numbering-across-all-days.md`
**Title**: Fix Exercise Numbering Pattern Across Entire LearningCourse
**Description**: Rename all exercises to match their day numbers (Day07 → Exercise71-74, etc.)
**Priority**: High
**Component**: LearningCourse
**Type**: Refactoring
**Assignee**: AI Agent
**Created**: 2025-01-14
**Status**: Investigation

## Problem Statement

User feedback: "Day07 must have Exercise71-74. Please fix the entire LearningCourse to have exercise number matching day number."

**Current State**: Exercise numbering doesn't match day numbers
- Day07 has Exercise61-64 (should be Exercise71-74)
- Day08 has Exercise71-74 (should be Exercise81-84)
- Day09 has Exercise91-94 (correct ✅)
- Other days need verification

**Required**: Systematic renaming so exercise first digit matches day number

## Lessons Applied from Previous WIs
### Previous WI References
- WI22: Attempted similar fix but incomplete
- Scripts exist: `rename-exercises-to-match-days.ps1`, `update-test-references.ps1`

## Phase 1: Investigation

### Current Exercise Numbering Audit

**Day01**: Uses descriptive names (not numbered) ✅
**Day02**: Exercise21-24 ✅ (matches Day02)
**Day03**: Need to check
**Day04**: Need to check
**Day05**: Need to check
**Day06**: Need to check
**Day07**: Exercise61-64 ❌ (should be Exercise71-74)
**Day08**: Exercise71-74 ❌ (should be Exercise81-84)
**Day09**: Exercise91-94 ✅ (correct)
**Day10**: Need to check
**Day11**: Need to check
**Day12**: Need to check
**Day13**: Need to check
**Day14**: Need to check
**Day15**: Need to check

### Files Requiring Updates
1. **Exercise folder names**: `Exercise61` → `Exercise71`
2. **Project files**: `Exercise61.csproj` → `Exercise71.csproj`
3. **Namespace declarations**: `namespace Exercise61` → `namespace Exercise71`
4. **Integration test paths**: Update all references in `Day07Tests.cs`
5. **Solution files**: Update project references if they exist
6. **Consumer group names**: `exercise61-consumer` → `exercise71-consumer`

### Scope of Work
- Rename ~40-50 exercise folders
- Update ~40-50 .csproj files
- Update ~40-50 Program.cs namespace declarations
- Update ~15 integration test files
- Update any solution file references
- Ensure all references are consistent

## Phase 2: Design

### Renaming Strategy

**Pattern**: `ExerciseXY` where X = Day number, Y = Exercise number within day
- Day07, Exercise 1 = Exercise71
- Day07, Exercise 2 = Exercise72
- Day10, Exercise 3 = Exercise103

### PowerShell Script Approach

```powershell
# Rename-LearningCourseExercises.ps1
# Systematically rename all exercises to match day numbers

param(
    [int]$DryRun = 1  # Set to 0 to execute renames
)

$renameMap = @{
    # Day07
    "Day07-Advanced-Windows-Joins/Exercise-Solutions/Exercise61" = "Exercise71"
    "Day07-Advanced-Windows-Joins/Exercise-Solutions/Exercise62" = "Exercise72"
    "Day07-Advanced-Windows-Joins/Exercise-Solutions/Exercise63" = "Exercise73"
    "Day07-Advanced-Windows-Joins/Exercise-Solutions/Exercise64" = "Exercise74"
    
    # Day08 - already has Exercise71-74, need to rename to Exercise81-84
    "Day08-Stress-Testing/Exercise-Solutions/Exercise71" = "Exercise81"
    "Day08-Stress-Testing/Exercise-Solutions/Exercise72" = "Exercise82"
    "Day08-Stress-Testing/Exercise-Solutions/Exercise73" = "Exercise83"
    "Day08-Stress-Testing/Exercise-Solutions/Exercise74" = "Exercise84"
    
    # Add more as we discover them
}

# For each rename operation:
# 1. Rename directory
# 2. Rename .csproj file
# 3. Update namespace in Program.cs
# 4. Update consumer group IDs
# 5. Update test file references
# 6. Update solution file references
```

### Steps to Execute (in order to avoid conflicts)

1. **Backup current state**: Commit all changes first
2. **Rename Day08 first** (Exercise71-74 → Exercise81-84) to free up naming
3. **Rename Day07** (Exercise61-64 → Exercise71-74)
4. **Rename remaining days** systematically
5. **Update test references** for each day after renaming
6. **Run tests** to verify no broken references

## Phase 3: Implementation Plan

### Step 1: Audit All Days
Create complete inventory of current exercise numbering

### Step 2: Create Comprehensive Rename Script
Build script that handles:
- Directory renames
- File renames
- Content updates (namespaces, consumer groups)
- Test reference updates
- Solution file updates

### Step 3: Execute Renames (Day by Day)
Start with least conflicting renames first

### Step 4: Validate Each Day
Run integration tests after each day's rename

## Current Status: Investigation

**Next Actions**:
1. Complete audit of all days (Day03-Day15)
2. Build comprehensive rename map
3. Test rename script with DryRun
4. Execute actual renames day by day
5. Validate with integration tests

## Notes
- Day09 exercises are already correctly numbered (Exercise91-94)
- Must preserve real infrastructure implementations
- Tests are passing for current naming, so we know functionality is good
- This is purely a naming/organizational refactor