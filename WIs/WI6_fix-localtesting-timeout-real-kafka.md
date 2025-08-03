# WI6: Fix LocalTesting Workflow - Timeout Issues and Real Kafka Integration

**File**: `WIs/WI6_fix-localtesting-timeout-real-kafka.md`
**Title**: Fix LocalTesting timeout issues, implement real Kafka messages, and correct message distribution  
**Description**: Address HTTP timeout errors in Steps 3-7, show real Kafka messages instead of simulated data, fix message distribution calculations, and implement proper partition hashing
**Priority**: High
**Component**: LocalTesting.WebApi
**Type**: Bug Fix
**Assignee**: AI Assistant  
**Created**: 2025-01-27
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI5_stress-test-fix.md - Learned about timeout handling and service integration
### Lessons Applied  
- Need to check infrastructure availability before running operations
- HTTP timeouts need to be configured appropriately for long-running operations
- Service failures should fallback gracefully with meaningful error messages
### Problems Prevented
- Avoiding duplicate method definitions that caused CS0111 errors
- Ensuring proper error handling for infrastructure dependencies

## Phase 1: Investigation
### Requirements
Fix multiple critical issues identified by Darren:
1. HTTP timeout errors in Steps 3-7 (30-second timeout)
2. Show real Kafka messages instead of simulated/sample data
3. Fix Step 1 message content - show logical queue count explicitly  
4. Fix Step 2 backpressure calculations (100,000 inserted, 900,000 waiting)
5. Fix partition distribution - last message should be in queue-999, not queue-0
6. Implement real partition hashing for load balancing
7. Ensure all previous steps are actually working before Step 8 verification

### Debug Information (MANDATORY - Update this section for every investigation)
- **Error Messages**: `The request was canceled due to the configured HttpClient.Timeout of 30 seconds elapsing` in Steps 3-7
- **Log Locations**: GitHub Actions workflow output showing timeout failures
- **System State**: LocalTesting API is running but operations timeout after 30 seconds
- **Reproduction Steps**: Run LocalTesting workflow, observe Step 3-7 failures
- **Evidence**: All steps after Step 2 fail with identical timeout errors

### Root Cause Analysis
1. **HTTP Timeout Issue**: PowerShell script uses `-TimeoutSec 30` but operations may take longer
2. **Simulated vs Real Data**: Current implementation generates sample messages instead of reading from actual Kafka topics
3. **Infrastructure Dependencies**: Services expect Kafka to be available but may not be properly connected
4. **Message Distribution Logic**: Hardcoded queue-0 instead of proper hash-based distribution across 1000 queues
5. **Backpressure Calculation**: Shows inserted=10,000 instead of correct 100,000 based on rate limiting

### Findings
- KafkaProducerService has 30-second timeouts configured
- ComplexLogicStressTestService generates simulated messages instead of reading from Kafka
- Message distribution uses modulo logic but doesn't properly distribute to queue-999 for last message
- Workflow doesn't verify Kafka connectivity before executing business flow steps

### Lessons Learned
- Infrastructure readiness checks are critical before executing business flows
- Timeout configurations must match expected operation durations
- Real integration testing requires actual infrastructure connectivity

## Phase 2: Design  
### Requirements
1. Increase HTTP timeout configurations in both API and PowerShell workflow
2. Implement real Kafka message reading for Steps 3-8
3. Add infrastructure connectivity checks before executing steps
4. Fix message distribution to properly use hash-based partitioning across 1000 logical queues
5. Implement graceful fallbacks when infrastructure is unavailable

### Architecture Decisions
- Extend HTTP timeouts to 120 seconds for complex operations
- Implement Kafka connectivity validation before business flow execution
- Add real Kafka topic reading for message verification steps
- Use proper hash-based partition assignment for message distribution
- Maintain backward compatibility with simulation mode for development

### Why This Approach
- Longer timeouts allow infrastructure to fully initialize
- Real Kafka integration provides actual message verification
- Hash-based partitioning ensures proper load distribution
- Graceful fallbacks maintain functionality during development

### Alternatives Considered
- Could use async/await patterns instead of timeout increases
- Could mock Kafka entirely, but Darren specifically wants real Kafka messages
- Could simplify to single partition, but requirements specify 100 partitions and 1000 logical queues

## Phase 3: TDD/BDD
### Test Specifications
- Verify HTTP operations complete within 120-second timeout
- Confirm real Kafka messages are read and displayed in workflow output
- Validate message distribution spreads across all 1000 logical queues
- Test backpressure calculations show correct inserted/waiting ratios

### Behavior Definitions
- Given LocalTesting environment is running
- When executing 8-step business flow
- Then all steps should complete without timeout errors
- And real Kafka messages should be displayed
- And message distribution should span queue-0 to queue-999

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
[To be filled during demonstration]

### Owner Feedback
[To be filled after owner review]

### Final Approval
[To be filled after approval]

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