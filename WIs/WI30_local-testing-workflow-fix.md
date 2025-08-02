# WI30: Fix Local Testing GitHub Workflow

**File**: `WIs/WI30_local-testing-workflow-fix.md`
**Title**: Fix failed Local Testing GitHub workflow - Step 4 Kafka connectivity issue  
**Description**: The Local Testing GitHub workflow is failing at Step 4 (produce-messages) with a 500 Internal Server Error due to Kafka connectivity issues between Aspire container networking and the WebAPI service.
**Priority**: High
**Component**: LocalTesting/Aspire
**Type**: Bug Fix
**Assignee**: AI Agent
**Created**: 2025-01-02
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- First WI - no previous work items to review
### Lessons Applied  
- Following TDD approach by debugging first before implementing solutions
- Using Work Item tracking for systematic problem solving
### Problems Prevented
- Avoided making changes without proper root cause analysis

## Phase 1: Investigation
### Requirements
- Fix the failed Local Testing GitHub workflow
- Ensure all business flow steps pass successfully
- Maintain Aspire orchestration architecture

### Debug Information (MANDATORY - Update this section for every investigation)
- **Error Messages**: 
  ```
  Step 4: Producing messages with correlation IDs...
  ❌ Business flow test failed: Response status code does not indicate success: 500 (Internal Server Error).
  ```
- **Updated Analysis - Actual Root Cause Found**: 
  ```
  ArgumentException: Test test-id not found
  in ComplexLogicStressTestService.ProduceMessagesAsync()
  ```
- **Log Locations**: 
  - GitHub Actions workflow logs: `.github/workflows/local-testing.yml`
  - LocalTesting API logs: confirmed via local testing
  - Aspire startup logs: `/LocalTesting/LocalTesting.AppHost/aspire_output.log`
- **System State**: 
  - Aspire environment has port conflicts from previous runs
  - Multiple containers binding to same ports causing "address already in use" errors
  - Service injection failing on Redis connection when running standalone
- **Reproduction Steps**: 
  1. Run Local Testing GitHub workflow
  2. Environment setup passes (Step 1)
  3. Token configuration passes (Step 2) 
  4. Backpressure configuration passes (Step 3)
  5. Step 4 message production fails with 500 error
- **Evidence**: 
  - ComplexLogicStressTestService.ProduceMessagesAsync() expects test to exist in _activeTests
  - Controller creates new testId but never initializes the test status
  - Missing test status causes ArgumentException which becomes 500 error
  - Kafka configuration was actually correct - not the root cause

### Findings
1. **Actual Root Cause Identified**: Missing test status initialization
   - ComplexLogicStressTestService.ProduceMessagesAsync() requires test to exist in `_activeTests` dictionary
   - ProduceMessages controller endpoint creates new testId but doesn't initialize test status
   - When service tries to get test status, it throws ArgumentException
   - This gets wrapped as 500 Internal Server Error
   
2. **Secondary Issues Found**: 
   - Aspire environment has port conflicts preventing proper startup
   - Multiple service instances trying to bind to same ports
   - Service dependencies (Redis, Kafka) must be available for full functionality

3. **Fix Applied**: Modified ProduceMessagesAsync to create test status if not exists

### Lessons Learned
- Aspire container networking requires proper service-to-service communication configuration
- Environment variables must be correctly passed from AppHost to dependent services
- Default fallback values in services should align with orchestration expectations

## Phase 2: Design  
### Requirements
- Fix the Kafka bootstrap servers configuration
- Ensure WebAPI receives correct Kafka connection string from Aspire environment
- Maintain compatibility with both local development and Aspire orchestration

### Architecture Decisions
**Approach 1: Verify Environment Variable Passing**
- Check if `KAFKA_BOOTSTRAP_SERVERS` is properly passed from AppHost to WebAPI
- Ensure AppHost configuration correctly sets the environment

**Approach 2: Add Connection String Configuration** 
- Use Aspire service discovery if environment variables aren't working
- Add fallback logic for different deployment scenarios

### Why This Approach
- Minimal change approach - fix configuration rather than architectural changes
- Maintains existing Aspire orchestration pattern
- Preserves container networking benefits

### Alternatives Considered
- Hardcoding connection strings (rejected - not configurable)
- Using different service discovery mechanism (overkill for this issue)

## Phase 3: TDD/BDD
### Test Specifications
- Environment variable should be properly set in WebAPI from AppHost
- KafkaProducerService should connect to `kafka-broker:9092` in Aspire environment
- Step 4 endpoint should succeed when Kafka is properly configured

### Behavior Definitions
```gherkin
Given the Aspire environment is running
When the WebAPI service starts
Then it should receive KAFKA_BOOTSTRAP_SERVERS environment variable
And it should connect to kafka-broker:9092
And Step 4 produce-messages should succeed
```

## Phase 4: Implementation
### Code Changes
**Fixed ComplexLogicStressTestService.ProduceMessagesAsync Method**
- **File**: `LocalTesting/LocalTesting.WebApi/Services/ComplexLogicStressTestService.cs`
- **Change**: Added logic to create test status if it doesn't exist
- **Before**: Method threw `ArgumentException` if test not found in `_activeTests`
- **After**: Method creates new `StressTestStatus` for standalone message production
- **Lines Modified**: 67-98

**Fix Details**:
```csharp
// Get or create test status for this test ID
var status = GetTestStatus(testId);
if (status == null)
{
    // Create a new test status for standalone message production
    status = new StressTestStatus
    {
        TestId = testId,
        Status = "Producing Messages", 
        TotalMessages = messageCount,
        StartTime = DateTime.UtcNow
    };
    _activeTests[testId] = status;
    status.Logs.Add($"Created standalone test {testId} for message production");
}
```

### Challenges Encountered
1. **Initial Misdiagnosis**: Originally thought issue was Kafka configuration
2. **Environment Complexity**: Aspire environment has multiple interconnected services
3. **Port Conflicts**: Previous container instances causing port binding issues
4. **Service Dependencies**: Redis and Kafka connections required for full functionality

### Solutions Applied
1. **Root Cause Analysis**: Used local testing to isolate the exact error
2. **Minimal Fix**: Modified only the failing method to handle missing test status
3. **Defensive Programming**: Added fallback logic for standalone operations

## Phase 5: Testing & Validation
### Test Results
[To be filled during testing]

### Performance Metrics
[To be filled during testing]

## Phase 6: Owner Acceptance
### Demonstration
[To be filled during demonstration]

### Owner Feedback
[To be filled during feedback]

### Final Approval
[To be filled during approval]

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
[To be updated during work]

### What Could Be Improved  
[To be updated during work]

### Key Insights for Similar Tasks
[To be updated during work]

### Specific Problems to Avoid in Future
[To be updated during work]

### Reference for Future WIs
[To be updated during work]