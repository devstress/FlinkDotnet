# WI1: Fix Build Errors and Clean Up FlinkDotNet

**File**: `WIs/WI1_fix-build-errors-clean-flinkdotnet.md`
**Title**: Fix Build Errors and Clean Up FlinkDotNet
**Description**: Fix immediate build errors, remove placeholders/simulated functionality, clean up unused components, and ensure LearningCourse exercises work
**Priority**: High
**Component**: FlinkDotNet Core
**Type**: Bug Fix + Enhancement
**Assignee**: AI Agent
**Created**: 2024-12-28
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- No previous WIs found
### Lessons Applied  
- This is the first WI for this project
### Problems Prevented
- Starting with thorough investigation before making changes

## Phase 1: Investigation
### Requirements
- Fix build errors preventing successful compilation
- Remove placeholder/simulated code throughout repo
- Remove unused projects that don't support Apache Flink
- Verify LearningCourse exercises work properly

### Debug Information (MANDATORY - Update this section for every investigation)
- **Error Messages**: 
  1. CS1061: 'List<TopicMetadata>' does not contain a definition for 'Where' - missing System.Linq
  2. CS0246: The type or namespace name 'List<>' could not be found - missing System.Collections.Generic
  3. CS1061: 'List<TopicPartitionOffset>' does not contain a definition for 'FirstOrDefault' - missing System.Linq
  4. S4487: Remove this unread private field '_redisConfig' - unused field in FlinkRedisSink.cs
- **Log Locations**: Build output from dotnet build FlinkDotNet/FlinkDotNet.sln
- **System State**: .NET 9.0.305 installed, FlinkDotNet.sln exists, LocalTesting.sln missing
- **Reproduction Steps**: 
  1. cd /home/runner/work/FlinkDotnet/FlinkDotnet
  2. export PATH="/home/runner/.dotnet:$PATH"
  3. dotnet build FlinkDotNet/FlinkDotNet.sln --configuration Release
- **Evidence**: Build fails with 4 errors and 12 warnings, specifically in Flink.JobBuilder project

### Findings
1. **Build Errors**: Primary issue is missing using directives in LagBasedRateLimiter.cs for System.Linq and System.Collections.Generic
2. **Code Quality**: Multiple SonarQube warnings about complexity and unused code
3. **Validation Script**: References non-existent LocalTesting.sln
4. **Repository Structure**: Contains many projects, need to evaluate which support Apache Flink

### Lessons Learned
- Always verify environment setup before investigating code issues
- Build errors often indicate missing namespace imports in C#
- Need to establish which projects are core vs auxiliary

## Phase 2: Design  
### Requirements
- Fix immediate build errors with minimal changes
- Identify and document which projects should be retained vs removed
- Plan cleanup of placeholder implementations

### Architecture Decisions
- Fix using statements first to unblock builds
- Address SonarQube issues systematically 
- Evaluate project dependencies before removal

### Why This Approach
- Prioritize build success to enable further analysis
- Make minimal changes to fix immediate issues
- Defer large architectural changes until build stability achieved

### Alternatives Considered
- Could rewrite entire LagBasedRateLimiter class, but too invasive
- Could ignore SonarQube warnings, but affects code quality

## Phase 3: TDD/BDD
### Test Specifications
- Build must succeed without errors
- All existing tests must continue to pass
- No functional regressions introduced

### Behavior Definitions
- Given a FlinkDotNet solution build
- When dotnet build is executed
- Then build should succeed with 0 errors

## Phase 4: Implementation
### Code Changes
[To be filled during implementation]

### Challenges Encountered
[To be filled during implementation]

### Solutions Applied
[To be filled during implementation]

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
[To be documented at completion]

### What Could Be Improved  
[To be documented at completion]

### Key Insights for Similar Tasks
[To be documented at completion]

### Specific Problems to Avoid in Future
[To be documented at completion]

### Reference for Future WIs
[To be documented at completion]