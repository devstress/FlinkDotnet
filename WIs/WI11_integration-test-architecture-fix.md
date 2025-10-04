# WI11: Fix Integration Test Architecture - Replace DistributedApplicationTestingBuilder

**File**: `WIs/WI11_integration-test-architecture-fix.md`
**Title**: Fix integration test architecture to use real containers in CI
**Description**: Replace DistributedApplicationTestingBuilder with actual container orchestration for CI compatibility
**Priority**: High
**Component**: LocalTesting.IntegrationTests
**Type**: Architecture Fix
**Assignee**: GitHub Copilot
**Created**: 2025-10-04
**Status**: Investigation

## Lessons Applied from Previous WIs

### Previous WI References
- WI10_integration-test-loop-fix.md - Identified DistributedApplicationTestingBuilder doesn't start real containers in CI
- WI9_integration-test-failures.md - JAR selection priority fix (already applied)
- LocalTesting/WIs/WI1_localtesting-integration-tests-fix.md - Original infrastructure patterns

### Lessons Applied  
- **Debug-first approach**: Must understand architectural limitations before proposing fixes
- **Run tests locally**: Reproduce issues to understand real vs testing environments
- **Infrastructure validation**: DistributedApplicationTestingBuilder is for config testing, not integration testing
- **CI/Local differences**: CI environment lacks Aspire Dashboard/DCP for container orchestration

### Problems Prevented
- Avoiding band-aid fixes that don't address root cause
- Not assuming testing framework behavior without verification
- Skipping architectural analysis in favor of quick patches

## Phase 1: Investigation

### Requirements
- Understand why DistributedApplicationTestingBuilder doesn't start containers in CI
- Design proper architecture for integration tests that works in both local and CI environments
- Implement minimal changes to fix root cause
- Ensure all 9 tests pass reliably

### Debug Information (MANDATORY - Update this section for every investigation)

**Problem Statement from WI10**:
- `DistributedApplicationTestingBuilder` is designed for **unit testing configurations**, not **integration testing with real containers**
- In CI environment without Aspire Dashboard/DCP:
  - The testing builder does NOT actually start Docker/Podman containers
  - It validates configuration correctness, not runtime infrastructure
  - Real container orchestration requires Aspire Dashboard or DCP running
- Tests fail with: "Could not determine Flink JobManager endpoint from Docker ports"
- Container status: No containers running (all cleaned up after test)

**Root Cause**:
The fundamental architectural problem is:
```
GlobalTestInfrastructure.cs uses:
  DistributedApplicationTestingBuilder.CreateAsync<AppHost>()
    ↓
  In CI: Validates config only, doesn't start containers
    ↓
  Tests try to connect to containers that don't exist
    ↓
  ❌ All tests fail: "Could not determine Flink JobManager endpoint"
```

**Architecture Options**:

1. **Option A: Process-based AppHost (Start real Aspire process)**
   - Start AppHost as a real process using `dotnet run`
   - Aspire DCP will start actual containers
   - Tests connect to real infrastructure
   - Pros: Uses Aspire as designed, full container orchestration
   - Cons: More complex teardown, requires Aspire DCP installation in CI

2. **Option B: Direct docker-compose/podman-compose**
   - Replace Aspire testing with docker-compose
   - Define infrastructure in docker-compose.yml
   - Tests connect to compose-managed containers
   - Pros: Simple, works everywhere, no Aspire dependency for tests
   - Cons: Duplicates infrastructure definition, loses Aspire benefits

3. **Option C: Manual container management**
   - Use Docker/Podman APIs directly to start containers
   - Replicate Aspire's container configuration manually
   - Tests manage container lifecycle
   - Pros: Fine-grained control, no external dependencies
   - Cons: Most complex, reimplements what Aspire already does

### Findings

**Investigation Results**:
- WI10 attempted Option A (process-based AppHost) but reverted due to complexity
- Current state uses DistributedApplicationTestingBuilder (doesn't work in CI)
- Docker is available in CI environment (Docker version 28.0.4)
- All 9 tests are integration tests requiring real infrastructure

**Recommended Approach**: **Option A - Process-based AppHost**

**Why Option A**:
1. Uses existing AppHost infrastructure definition (no duplication)
2. Aspire DCP handles container lifecycle properly
3. Works in both local and CI environments
4. Maintains consistency between dev and test environments
5. Minimal changes to existing test code

**Implementation Strategy**:
1. Start LocalTesting.FlinkSqlAppHost as a real process
2. Wait for Aspire to start all containers
3. Extract connection endpoints from running containers
4. Run tests against real infrastructure
5. Clean shutdown of AppHost process and containers

## Phase 2: Design ✅

### Requirements
Replace DistributedApplicationTestingBuilder with process-based AppHost startup for CI compatibility

### Architecture Decisions

**Selected Approach**: Process-based AppHost (Option A)

**Implementation Plan**:
1. Replace `DistributedApplicationTestingBuilder` with `Process.Start("dotnet run")`
2. Start AppHost in background process
3. Wait for containers to be healthy (query Docker for actual containers)
4. Extract endpoints from running containers (not from Aspire API)
5. Run tests against real infrastructure
6. Graceful shutdown: Stop AppHost process, containers auto-cleanup

**Key Changes**:
- `GlobalTestInfrastructure.cs`:
  - Remove: `DistributedApplicationTestingBuilder.CreateAsync<AppHost>()`
  - Add: Start AppHost as real process
  - Change: Query Docker directly for container endpoints
  - Fix: Don't rely on Aspire testing framework APIs

**Critical Insight**:
The AppHost already has all the container definitions. We just need to run it as a real process instead of using the testing builder. Aspire DCP will handle all container orchestration.

### Why This Approach
1. **Minimal code changes**: Replace one initialization method
2. **Uses existing infrastructure**: AppHost already defines everything correctly
3. **CI compatible**: Real process with real containers
4. **Local/CI parity**: Same behavior in both environments
5. **Aspire native**: Uses Aspire as designed (DCP + containers)

### Alternatives Considered
1. **docker-compose**: Rejected - duplicates infrastructure definition
2. **Manual containers**: Rejected - too complex, reinvents Aspire
3. **Fix Aspire DCP in CI**: Rejected - out of scope, infrastructure problem

### Implementation Details

**Process Startup**:
```csharp
var appHostProcess = new Process
{
    StartInfo = new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = "run --project LocalTesting/LocalTesting.FlinkSqlAppHost --no-build",
        WorkingDirectory = repoRoot,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    }
};
appHostProcess.Start();
```

**Wait Strategy**:
- Poll Docker for containers (not Aspire API)
- Check container health: Kafka, Flink JobManager, Flink TaskManager, Gateway
- Extract actual ports from `docker ps`
- Timeout if containers don't start

**Cleanup Strategy**:
```csharp
appHostProcess.Kill();
appHostProcess.WaitForExit();
// Aspire DCP will auto-cleanup containers when process exits
```

## Phase 3: TDD/BDD

(To be filled after design)

## Phase 4: Implementation

(To be filled after TDD/BDD)

## Phase 5: Testing & Validation

(To be filled after implementation)

## Phase 6: Owner Acceptance

(To be filled after validation)

## Lessons Learned & Future Reference (MANDATORY)

### What Worked Well
(To be documented as we progress)

### What Could Be Improved  
(To be documented based on issues encountered)

### Specific Problems to Avoid in Future
(To be documented to prevent recurrence)

### Reference for Future WIs
(To be documented with specific files and patterns)
