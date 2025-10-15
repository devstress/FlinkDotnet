# WI68: Day14 Exercises 142-144 - Advanced Testing and Chaos Engineering

**File**: `WIs/WI68_day14-exercises142-144-testing-chaos.md`
**Title**: Implement Exercise142 (Mutation Testing), Exercise143 (Fault Injection), Exercise144 (Chaos Engineering)
**Description**: Create three advanced testing exercises using real Kafka infrastructure with AspireServiceDiscovery pattern
**Priority**: High
**Component**: LearningCourse/Day14-Advanced-Testing-Chaos-Engineering
**Type**: Feature Implementation
**Assignee**: AI Agent
**Created**: 2025-10-14
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI67: Exercise141 property-based testing pattern
- WI44-47: Day08 testing exercises conversion
- WI54-57: Day13 advanced patterns implementation

### Lessons Applied
- Use AspireServiceDiscovery for Kafka port discovery
- Console applications that complete and exit cleanly
- Remove global.json files from exercise directories
- Real Kafka infrastructure, no simulation
- Print clear completion markers
- Exit with code 0 on success
- Keep execution time under 3 minutes per exercise

### Problems Prevented
- No simulated infrastructure - use real Kafka
- No blocking operations without timeouts
- No incomplete async operations
- Proper resource disposal with using statements

## Phase 1: Investigation
### Requirements
- Exercise142: Mutation testing with Kafka message transformations
- Exercise143: Fault injection testing with Kafka operations
- Exercise144: Chaos engineering experiments with Kafka
- All exercises must use AspireServiceDiscovery pattern
- Each exercise demonstrates testing concepts practically
- Execution time < 3 minutes per exercise

### Debug Information (MANDATORY)
- No existing implementation to debug
- Pattern established in Exercise141
- AspireServiceDiscovery.cs available in LearningCourse.Common
- Kafka infrastructure available via Docker

### Findings
- Need to create three new exercise directories
- Each exercise needs .csproj with Kafka + LearningCourse.Common references
- Remove any global.json files
- Follow Exercise141 console pattern
- Demonstrate testing concepts with real Kafka operations

### Lessons Learned
- Exercise141 pattern provides solid foundation
- AspireServiceDiscovery simplifies Kafka connection
- Console applications more reliable than long-running services

## Phase 2: Design
### Requirements
Exercise142 (Mutation Testing):
- Test Kafka message transformation logic
- Apply mutations to transformations
- Verify tests catch mutations
- Demonstrate mutation testing benefits

Exercise143 (Fault Injection):
- Inject faults: timeouts, connection failures, message corruption
- Test system resilience
- Demonstrate retry logic, circuit breakers
- Graceful degradation patterns

Exercise144 (Chaos Engineering):
- Chaos experiments with Kafka
- Measure recovery time
- Validate exactly-once semantics
- Netflix-style chaos principles

### Architecture Decisions
All exercises follow Exercise141 pattern:
1. AspireServiceDiscovery for Kafka ports
2. Console application structure
3. Real Kafka operations
4. Clear completion markers
5. Proper resource disposal
6. Timeout-based execution limits

### Why This Approach
- Proven pattern from Exercise141
- Real infrastructure demonstrates practical concepts
- Console apps easier to test and debug
- Clear execution boundaries
- Practical demonstration of testing techniques

### Alternatives Considered
- Long-running services: Rejected - harder to test
- Simulated faults: Rejected - need real Kafka behavior
- Manual configuration: Rejected - AspireServiceDiscovery better

## Phase 3: TDD/BDD
### Test Specifications
Integration tests in Day14Tests.cs:
- Exercise142_MutationTesting_ShouldCompleteSuccessfully
- Exercise143_FaultInjection_ShouldCompleteSuccessfully
- Exercise144_ChaosEngineering_ShouldCompleteSuccessfully

### Behavior Definitions
Each test:
1. Starts exercise console app
2. Waits for completion (max 3 minutes)
3. Verifies exit code 0
4. Checks for completion marker in output

## Phase 4: Implementation
### Code Changes
Create three exercise directories with:
- Exercise142/Exercise142.csproj
- Exercise142/Program.cs
- Exercise143/Exercise143.csproj
- Exercise143/Program.cs
- Exercise144/Exercise144.csproj
- Exercise144/Program.cs

Update Day14Tests.cs with integration tests

### Challenges Encountered
[To be filled during implementation]

### Solutions Applied
[To be filled during implementation]

## Phase 5: Testing & Validation
### Test Results
[To be filled after testing]

### Performance Metrics
- Each exercise completes in < 3 minutes
- Proper Kafka connectivity
- Clean resource disposal

## Phase 6: Owner Acceptance
### Demonstration
[To be filled when presenting to owner]

### Owner Feedback
[To be filled after owner review]

### Final Approval
[Pending implementation and testing]

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
[To be filled after completion]

### What Could Be Improved
[To be filled after completion]

### Key Insights for Similar Tasks
[To be filled after completion]

### Specific Problems to Avoid in Future
[To be filled after completion]

### Reference for Future WIs
[To be filled after completion]