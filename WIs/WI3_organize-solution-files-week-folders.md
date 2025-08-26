# WI3: Organize Solution Files into Correct Week Folders

**File**: `WIs/WI3_organize-solution-files-week-folders.md`
**Title**: [LearningCourse] Move solution files into their corresponding Day folders
**Description**: Move tutorial solution files from LearningCourse root into their respective Day##-* folders and update project references to use relative paths
**Priority**: Medium
**Component**: LearningCourse Structure
**Type**: Enhancement
**Assignee**: AI Agent
**Created**: 2024-08-26
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI2: Update All Testing Infrastructure for Temporal Durable Workflow Architecture (ongoing)
### Lessons Applied  
- Start with proper environment setup (.NET 9.0 validation)
- Create baseline validation before making changes
- Make incremental changes with frequent validation
- Update documentation alongside code changes
### Problems Prevented
- Breaking build references by updating paths incrementally
- Environment compatibility issues by validating .NET version first

## Phase 1: Investigation
### Requirements
Reorganize solution file structure in LearningCourse directory to improve organization and maintainability.

### Debug Information (MANDATORY - Update this section for every investigation)
- **Current Structure**: Solution files (Day01Tutorial.sln, Day02Tutorial.sln, Day07Tutorial.sln, Day14Tutorial.sln) are in LearningCourse root
- **Target Structure**: Each solution should be in its corresponding Day##-* folder
- **Path Issues**: Project references use relative paths from LearningCourse root that will need updating
- **Build System**: Uses .NET 9.0 (currently not installed in environment)

### Current File Analysis
**Solution Files Found**:
- `/LearningCourse/Day01Tutorial.sln` → should move to `/LearningCourse/Day01-Flink20-Fundamentals/`
- `/LearningCourse/Day02Tutorial.sln` → should move to `/LearningCourse/Day02-AI-Stream-Processing/`
- `/LearningCourse/Day07Tutorial.sln` → should move to `/LearningCourse/Day07-Stress-Testing/`
- `/LearningCourse/Day14Tutorial.sln` → should move to `/LearningCourse/Day14-Capstone-Project/`

**Project References Analysis**:
- Day01Tutorial.sln references: `Day01-Flink20-Fundamentals\Exercise-Solutions\*\*.csproj`
- Day02Tutorial.sln references: `Day02-AI-Stream-Processing\Exercise-Solutions\*\*.csproj`
- Day07Tutorial.sln references: `Day07-Stress-Testing\Exercise-Solutions\*\*.csproj`
- Day14Tutorial.sln references: `Day14-Capstone-Project\Exercise-Solutions\*\*.csproj`

### Findings
1. Solution files are currently in LearningCourse root for organizational convenience
2. They reference projects in their respective Day##-* folders using relative paths
3. Moving them into the Day folders will require updating project paths to remove the Day##-* prefix
4. This will create a cleaner structure where each Day folder is self-contained
5. Build system requires .NET 9.0 SDK which needs to be installed first

### Lessons Learned
- Current structure separates solutions from their projects, making navigation less intuitive
- Each Day folder should be self-contained with its own solution file
- Path updates are predictable: remove the Day##-* prefix from existing relative paths

## Phase 2: Design  
### Requirements
Design the reorganization to maintain build functionality while improving structure.

### Architecture Decisions
**New Structure**:
```
LearningCourse/
├── Day01-Flink20-Fundamentals/
│   ├── Day01Tutorial.sln           # Moved from root
│   ├── Exercise-Solutions/
│   └── README.md
├── Day02-AI-Stream-Processing/
│   ├── Day02Tutorial.sln           # Moved from root
│   ├── Exercise-Solutions/
│   └── README.md
├── Day07-Stress-Testing/
│   ├── Day07Tutorial.sln           # Moved from root
│   ├── Exercise-Solutions/
│   └── README.md
├── Day14-Capstone-Project/
│   ├── Day14Tutorial.sln           # Moved from root
│   ├── Exercise-Solutions/
│   └── README.md
└── README.md
```

**Path Update Pattern**:
- Before: `Day01-Flink20-Fundamentals\Exercise-Solutions\ProductionApp\ProductionApp.csproj`
- After: `Exercise-Solutions\ProductionApp\ProductionApp.csproj`

### Why This Approach
- Creates self-contained Day folders that can be worked on independently
- Simplifies navigation - solution file is in the same folder as projects it references
- Follows common .NET project organization patterns
- Maintains backward compatibility for projects that don't have solution files

### Alternatives Considered
- **Option 1**: Keep solutions in root → Rejected (maintains confusing structure)
- **Option 2**: Create Week subfolders → Rejected (adds unnecessary nesting)
- **Option 3**: Move directly into Day folders → **Selected** (cleanest approach)

## Phase 3: TDD/BDD
### Test Specifications
**Validation Tests**:
1. All moved solution files build successfully
2. Project references resolve correctly  
3. Relative paths work from new locations
4. No broken references in any solution
5. Build scripts still work if they reference solutions

### Behavior Definitions
**Given** solution files are in LearningCourse root
**When** they are moved to their respective Day folders
**Then** all project references should still resolve correctly
**And** solution files should build without errors

## Phase 4: Implementation
### Code Changes
[To be updated during implementation]

### Challenges Encountered
[To be updated during implementation]

### Solutions Applied
[To be updated during implementation]

## Phase 5: Testing & Validation
### Test Results
[To be updated during testing]

### Performance Metrics
[To be updated during testing]

## Phase 6: Owner Acceptance
### Demonstration
[To be updated during demonstration]

### Owner Feedback
[To be updated after feedback]

### Final Approval
[To be updated after approval]

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