# WI28: Fix All Exercise Numbering Inconsistencies

**File**: `WIs/WI28_fix-all-exercise-numbering-inconsistencies.md`
**Title**: [LearningCourse] Fix all exercise numbering inconsistencies  
**Description**: Comprehensive fix for ALL exercise numbering mismatches across LearningCourse including namespaces, comments, consumer groups, job names, and README files
**Priority**: High
**Component**: LearningCourse
**Type**: Bug Fix
**Assignee**: AI Agent
**Created**: 2025-01-14
**Status**: In Development

## Problem Statement

After previous renaming operations, exercises have inconsistent numbering across:
- Namespace declarations
- Exercise numbers in comments (e.g., "Exercise 6.1" vs actual folder "Exercise71")
- Consumer group IDs (e.g., "exercise61-consumer" vs "exercise71-consumer")
- Flink job names in ExecuteAsync calls
- README files and documentation

### Affected Exercises

**Day07 (Exercise71-74)**:
- Comments say "Exercise 6.x" but should be "Exercise 7.x"
- Consumer groups use "exercise7x" (correct)
- Job names use "Exercise6x" but should be "Exercise7x"

**Day08 (Exercise81-84)**:
- Namespaces were "Exercise7x" → **FIXED** to "Exercise8x"
- Comments say "Exercise 7.x" but should be "Exercise 8.x" 
- Consumer groups use "exercise7x" but should be "exercise8x"
- Job names use "Exercise7x" but should be "Exercise8x"

**Day09 (Exercise91)**:
- Consumer group uses "exercise81" but should be "exercise91"

## Phase 1: Investigation ✅

### Search Results
Found 68 instances of wrong exercise numbers in `.cs` files:
- Day07: Comments reference "Exercise 6.x", job names use "Exercise6x"
- Day08: Comments reference "Exercise 7.x", consumer groups use "exercise7x", job names use "Exercise7x"
- Day09/Exercise91: Consumer group uses "exercise81"

## Phase 2: Design

### Fix Strategy
1. Day07 exercises (71-74): Fix comments and job names only (consumer groups already correct)
2. Day08 exercises (81-84): Fix comments, consumer groups, and job names
3. Day09 Exercise91: Fix consumer group ID
4. Search and fix any README.md files with wrong exercise numbers

### Pattern Matching Rules
- **Comments**: `Exercise X.Y` where X should match day number (7, 8, 9)
- **Consumer groups**: `exerciseXY-` where XY should match exercise number (71, 81, 91, etc.)
- **Job names**: `ExerciseXY-` where XY should match exercise number
- **README files**: Any mention of wrong exercise numbers

## Phase 3: Implementation

### Day07 Fixes (Exercise71-74)

#### Exercise71
- Line 12: "Exercise 6.1" → "Exercise 7.1"
- Line 61: "Exercise 6.1" → "Exercise 7.1"  
- Line 121: "Exercise 6.1" → "Exercise 7.1"
- Line 134: "Exercise 6.1" → "Exercise 7.1"
- Line 160: "Exercise 6.1" → "Exercise 7.1"
- Line 208: `"Exercise61-OrderEnrichment"` → `"Exercise71-OrderEnrichment"`
- Line 224: `"exercise61-reference-producer"` → `"exercise71-reference-producer"`
- Line 301: `"exercise61-orders-producer"` → `"exercise71-orders-producer"`

#### Exercise72
- Line 12: "Exercise 6.2" → "Exercise 7.2"
- Line 58: "Exercise 6.2" → "Exercise 7.2"
- Line 112: "Exercise 6.2" → "Exercise 7.2"
- Line 126: "Exercise 6.2" → "Exercise 7.2"
- Line 152: "Exercise 6.2" → "Exercise 7.2"
- Line 188: `"Exercise62-FraudDetectionWindows"` → `"Exercise72-FraudDetectionWindows"`
- Line 205: `"exercise62-producer"` → `"exercise72-producer"`

#### Exercise73
- Line 12: "Exercise 6.3" → "Exercise 7.3"
- Line 60: "Exercise 6.3" → "Exercise 7.3"
- Line 119: "Exercise 6.3" → "Exercise 7.3"
- Line 133: "Exercise 6.3" → "Exercise 7.3"
- Line 159: "Exercise 6.3" → "Exercise 7.3"
- Line 216: `"Exercise63-IoTCorrelation"` → `"Exercise73-IoTCorrelation"`
- Line 233: `"exercise63-production-producer"` → `"exercise73-production-producer"`
- Line 270: `"exercise63-sensor-producer"` → `"exercise73-sensor-producer"`

#### Exercise74
- Line 12: "Exercise 6.4" → "Exercise 7.4"
- Line 59: "Exercise 6.4" → "Exercise 7.4"
- Line 123: "Exercise 6.4" → "Exercise 7.4"
- Line 148: "Exercise 6.4" → "Exercise 7.4"
- Line 174: "Exercise 6.4" → "Exercise 7.4"
- Line 210: `"Exercise64-WindowingOptimization"` → `"Exercise74-WindowingOptimization"`
- Line 227: `"exercise64-high-volume-producer"` → `"exercise74-high-volume-producer"`

### Day08 Fixes (Exercise81-84)

#### Exercise81
- Line 11: "Exercise 7.1" → "Exercise 8.1"
- Line 36: `"exercise71-consumer"` → `"exercise81-consumer"`
- Line 60: "Exercise 7.1" → "Exercise 8.1"
- Line 110: "Exercise 7.1" → "Exercise 8.1"
- Line 124: "Exercise 7.1" → "Exercise 8.1"
- Line 150: "Exercise 7.1" → "Exercise 8.1"
- Line 182: `"Exercise71-StressTesting"` → `"Exercise81-StressTesting"`
- Line 242: `"exercise71-producer-"` → `"exercise81-producer-"`

#### Exercise82
- Line 11: "Exercise 7.2" → "Exercise 8.2"
- Line 36: `"exercise72-consumer"` → `"exercise82-consumer"`
- Line 60: "Exercise 7.2" → "Exercise 8.2"
- Line 110: "Exercise 7.2" → "Exercise 8.2"
- Line 124: "Exercise 7.2" → "Exercise 8.2"
- Line 150: "Exercise 7.2" → "Exercise 8.2"
- Line 182: `"Exercise72-BackpressureMonitoring"` → `"Exercise82-BackpressureMonitoring"`
- Line 245: `"exercise72-producer-"` → `"exercise82-producer-"`

#### Exercise83
- Line 11: "Exercise 7.3" → "Exercise 8.3"
- Line 36: `"exercise73-consumer"` → `"exercise83-consumer"`
- Line 61: "Exercise 7.3" → "Exercise 8.3"
- Line 111: "Exercise 7.3" → "Exercise 8.3"
- Line 125: "Exercise 7.3" → "Exercise 8.3"
- Line 151: "Exercise 7.3" → "Exercise 8.3"
- Line 183: `"Exercise73-PerformanceBenchmark"` → `"Exercise83-PerformanceBenchmark"`
- Line 247: `"exercise73-"` → `"exercise83-"`

#### Exercise84
- Line 11: "Exercise 7.4" → "Exercise 8.4"
- Line 36: `"exercise74-consumer"` → `"exercise84-consumer"`
- Line 60: "Exercise 7.4" → "Exercise 8.4"
- Line 120: "Exercise 7.4" → "Exercise 8.4"
- Line 136: "Exercise 7.4" → "Exercise 8.4"
- Line 167: "Exercise 7.4" → "Exercise 8.4"
- Line 199: `"Exercise74-ResourceMonitoring"` → `"Exercise84-ResourceMonitoring"`
- Line 258: `"exercise74-"` → `"exercise84-"`

### Day09 Fixes

#### Exercise91
- Line 37: `"exercise81-banking-consumer"` → `"exercise91-banking-consumer"`
- Line 246: `"exercise91-"` (already correct, verify consistency)

## Phase 4: Validation

### Test Plan
1. Build all affected exercises
2. Run integration tests for Day07, Day08, Day09
3. Verify consumer group IDs are unique and correct
4. Verify job names match exercise numbers
5. Check README files for correct exercise references

## Lessons Learned
- **Always verify ALL references** when renaming: namespaces, comments, strings, IDs
- **Use regex search** to find all instances before manual fixes
- **Test immediately** after renaming to catch issues early
- **Document patterns** for future consistency

## Status: In Development
Next: Apply all fixes systematically