# WI: Test Coverage 80%+ and SonarCloud Fix

**File**: `WIs/WI_coverage-80-and-sonarcloud-fix.md`
**Title**: [FlinkDotNet] Push coverage to 80%+ and fix SonarCloud submission
**Description**: Add unit tests to achieve 80%+ coverage and fix SonarCloud to receive coverage data
**Priority**: High
**Component**: FlinkDotNet
**Type**: Enhancement
**Assignee**: AI Agent
**Created**: 2025-10-14
**Status**: In Progress

## Lessons Applied from Previous WIs
### Previous WI References
- `WIs/WI_test-coverage-improvement.md` - Previous coverage improvement work
### Lessons Applied  
- Focus on high-impact, low-coverage areas first
- Ensure coverage report formats are compatible with CI tools
- Verify all CI integration works end-to-end
### Problems Prevented
- Missing coverage report formats causing CI failures
- SonarCloud not receiving coverage data

## Phase 1: Investigation
### Requirements
- Achieve at least 80% code coverage in FlinkDotNet folder
- Fix SonarCloud to receive coverage reports (currently showing 0%)

### Debug Information (MANDATORY - Update this section for every investigation)
- **Current Coverage**: 79.6% line coverage (2352/2953 covered lines)
- **Coverage Gap**: Need 0.4% more to reach 80% target
- **Missing Tests**: ~12 additional lines needed for 80% coverage
- **SonarCloud Issue**: Coverage format mismatch
  - Current: Only Cobertura format generated (`coverlet.runsettings` has `<Format>cobertura</Format>`)
  - Required: OpenCover format for SonarCloud (or both formats)
  - Workflow expects: `sonar.cs.vscoveragexml.reportsPaths` pointing to coverage files
  - Root cause: coverlet.runsettings only generates Cobertura, not OpenCover

### Coverage Analysis by Assembly
1. **Flink.JobBuilder**: 82% (good)
2. **FlinkDotNet.ClusterManager**: 100% (excellent)
3. **FlinkDotNet.Common**: 100% (excellent)
4. **FlinkDotNet.DataStream**: 98.1% (excellent)
5. **FlinkDotNet.JobGateway**: 38.2% (needs improvement but integration-focused)
6. **FlinkDotNet.Orchestration**: 100% (excellent)

### Areas Below Coverage Target
- **FlinkDotNet.JobGateway.Services.FlinkJobManager**: 30.4% coverage
  - This is integration/web service focused, harder to unit test
  - May require integration tests rather than unit tests
  
- **Flink.JobBuilder classes with room for improvement**:
  - MultiTierRateLimiter: 54.6%
  - SlidingWindowRateLimiter: 76%
  - TokenBucketRateLimiter: 85%
  - RateLimiterFactory: 72.2%
  - KafkaRateLimiterStateStorage: 79.4%

### Findings
1. **Coverage is very close to 80%** - only 0.4% gap
2. **SonarCloud issue is a configuration problem**, not a code problem:
   - coverlet.runsettings only generates Cobertura format
   - SonarCloud expects OpenCover or both formats
   - Workflow configuration expects the wrong format
3. **Strategic approach**:
   - Add a few more tests to push above 80%
   - Fix coverage format configuration for SonarCloud

### Lessons Learned
- Always verify coverage report formats match CI tool requirements
- Check both local and CI coverage reporting configurations
- SonarCloud requires OpenCover or Cobertura with correct path configuration

## Phase 2: Design  
### Requirements
1. Update coverlet.runsettings to generate both Cobertura and OpenCover formats
2. Ensure SonarCloud workflow uses correct coverage paths
3. Add minimal tests to push coverage from 79.6% to 80%+

### Architecture Decisions
- Generate both coverage formats to support local reporting (Cobertura) and SonarCloud (OpenCover)
- Target rate limiting classes with <80% coverage for test additions
- Verify end-to-end that SonarCloud receives coverage data

### Why This Approach
- Minimal changes to existing configuration
- Maintains backward compatibility with local coverage reporting
- Fixes SonarCloud integration without major refactoring

### Alternatives Considered
- Could switch entirely to OpenCover format, but Cobertura works well for local reporting
- Could add many more tests, but 80% is the target threshold

## Phase 3: TDD/BDD
### Test Specifications
- Add tests for rate limiting edge cases
- Verify error handling paths
- Test boundary conditions

### Behavior Definitions
- Rate limiters should handle edge cases correctly
- Configuration classes should validate input
- Storage implementations should handle errors gracefully

## Phase 4: Implementation
### Code Changes
*To be updated during implementation*

### Challenges Encountered
*To be updated during implementation*

### Solutions Applied
*To be updated during implementation*

## Phase 5: Testing & Validation
### Test Results
*To be updated after testing*

### Performance Metrics
*To be updated after coverage run*

## Phase 6: Owner Acceptance
### Demonstration
*To be updated when complete*

### Owner Feedback
*Pending*

### Final Approval
*Pending*

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
*To be documented*

### What Could Be Improved  
*To be documented*

### Key Insights for Similar Tasks
*To be documented*

### Specific Problems to Avoid in Future
*To be documented*

### Reference for Future WIs
*To be documented*
