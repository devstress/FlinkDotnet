# WI17: Flink Job Cleanup for Parallel Test Execution

**File**: `WIs/WI17_flink-job-cleanup-for-parallel-tests.md`
**Title**: [LearningCourse] Implement proper Flink job cleanup to scale beyond 10 concurrent tests
**Description**: LearningCourse integration tests need proper Flink job cleanup to scale beyond 10 concurrent tests. Currently, Flink jobs remain running after each test completes, consuming TaskManager slots. With only 10 slots available, this prevents scaling to 100+ tests.
**Priority**: High
**Component**: LearningCourse Integration Tests
**Type**: Enhancement
**Assignee**: AI Agent
**Created**: 2025-10-13
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI16: Day02 integration tests fix - learned about test infrastructure patterns
- WI14: Integration test performance optimization - learned about parallel execution patterns

### Lessons Applied
- Each test must clean up its own resources immediately after completion
- Use thread-safe collections for tracking per-test resources
- Parse job IDs from stdout using predictable format
- Use Flink REST API directly for job cancellation

### Problems Prevented
- Job slot exhaustion preventing parallel test execution
- Tests interfering with each other due to shared job state
- Memory leaks from long-running jobs after tests complete

## Phase 1: Investigation

### Requirements
Analyze current implementation to understand:
1. How ExecuteAsync() returns job information
2. How JobGateway submits jobs and returns job IDs
3. How exercise programs currently handle job execution
4. Current test infrastructure for job lifecycle management

### Debug Information (MANDATORY - Update this section for every investigation)
**Current Implementation Analysis**:

1. **StreamExecutionEnvironment.ExecuteAsync()** (lines 406-468):
   - Returns `JobExecutionResult` with `JobId` field (line 461)
   - `JobId` is populated from `submit.FlinkJobId ?? jobToSubmit.Metadata.JobId` (line 461)
   - Already returns the Flink job ID! ✅

2. **FlinkJobGatewayService.SubmitJobAsync()** (lines 97-114):
   - Returns `JobSubmissionResult` (line 97)
   - `JobSubmissionResult` has `FlinkJobId` property (JobResults.cs line 12)
   - Gateway already returns job ID from Flink REST API! ✅

3. **JobSubmissionResult model** (JobResults.cs lines 9-50):
   - `FlinkJobId` property stores the actual Flink job ID (line 12)
   - This is the 32-character hex job ID from Flink REST API

4. **Exercise Programs** (Exercise2-BackupAggregator/Program.cs):
   - Call `BaeldungNativeApi.CreateBackup()` which calls `env.ExecuteAsync()` (line 149)
   - Currently DO NOT capture or print the job ID
   - Need to modify to capture and print job ID to stdout

5. **LearningCourseTestBase** (lines 1-753):
   - Has `GlobalTearDown()` that cancels ALL jobs (lines 606-672)
   - Does NOT have per-test cleanup
   - Does NOT parse job IDs from exercise stdout
   - Needs thread-safe per-test job ID tracking

6. **Test Infrastructure**:
   - Uses NUnit with `[TearDown]` support
   - Tests can track state in instance fields
   - `ExecuteExerciseAsync()` captures stdout (lines 677-751)

### Findings
**GOOD NEWS**: The infrastructure already returns job IDs!
- `ExecuteAsync()` already returns `JobExecutionResult.JobId` with Flink job ID
- `JobGateway.SubmitJobAsync()` already returns `JobSubmissionResult.FlinkJobId`
- No API changes needed! ✅

**CHANGES NEEDED**:
1. Exercise programs must capture and print job ID to stdout
2. Test base must parse job IDs from stdout
3. Add per-test `[TearDown]` method to cancel jobs
4. Use thread-safe collection for per-test job ID tracking

### Lessons Learned
- Always check existing API capabilities before designing new ones
- The infrastructure was already well-designed for job tracking
- stdout parsing is the right approach for test->infrastructure communication

## Phase 2: Design

### Requirements
Design solution that:
1. Captures job IDs from ExecuteAsync() return value
2. Prints job IDs to stdout in parseable format
3. Parses job IDs from test stdout capture
4. Cancels jobs in per-test TearDown
5. Supports parallel test execution safely

### Architecture Decisions
**Job ID Output Format**:
```
FLINK_JOB_ID: <32-character-hex-job-id>
```
- Simple, grep-able format
- Clear prefix prevents false matches
- Parseable with regex: `FLINK_JOB_ID:\s*([a-f0-9]{32})`

**Per-Test Job Tracking**:
- Each test instance tracks its own job IDs in a List<string>
- Instance field: `private readonly List<string> _jobIds = new();`
- Thread-safe because each test instance has its own list
- Cleared in `[TearDown]` after cancellation

**Job Cancellation Strategy**:
- Use Flink REST API directly: `PATCH http://localhost:8080/jobs/{jobId}?mode=cancel`
- Cancel in `[TearDown]` method (runs after each test)
- Best effort - log errors but don't fail tests on cleanup errors
- Timeout: 5 seconds per job cancellation

**Parallel Execution Safety**:
- Each test tracks only its own job IDs
- No shared state between test instances
- Job cancellation is idempotent (safe to call multiple times)
- Test framework ensures TearDown runs even on test failure

### Why This Approach
1. **Minimal Changes**: Uses existing API, only adds stdout output
2. **Test Isolation**: Each test cleans up only its own jobs
3. **Scalability**: Enables 100+ tests by freeing slots immediately
4. **Reliability**: Best-effort cleanup doesn't break tests
5. **Observability**: Job IDs in stdout help debugging

### Alternatives Considered
1. **Modify API to return job client**: Too invasive, breaks existing code
2. **Global job tracking**: Not thread-safe for parallel tests
3. **Database for job tracking**: Overkill, adds complexity
4. **Process-level cleanup**: Too late, slots already consumed

## Phase 3: TDD/BDD

### Test Specifications
Manual testing approach (integration tests test themselves):
1. Run single test - verify job ID in stdout
2. Run single test - verify job cancelled after test
3. Run 2 tests sequentially - verify each cleans up its job
4. Run 10 tests in parallel - verify slots freed between batches
5. Run test that fails - verify cleanup still happens

### Behavior Definitions
**Given** an exercise that submits a Flink job
**When** the test executes the exercise
**Then** the exercise should print "FLINK_JOB_ID: <id>" to stdout
**And** the test should parse the job ID
**And** the test should cancel the job in TearDown
**And** the Flink job should no longer be running

## Phase 4: Implementation

### Code Changes

#### 1. Update StreamExecutionEnvironment.ExecuteAsync() - NO CHANGES NEEDED ✅
Already returns JobExecutionResult with JobId field.

#### 2. Update Exercise Programs
Modify all exercise Program.cs files to:
```csharp
// After ExecuteAsync() call:
var result = await env.ExecuteAsync("Job Name");
Console.WriteLine($"FLINK_JOB_ID: {result.JobId}");
```

Files to update:
- Exercise1-StringCapitalize/Program.cs
- Exercise2-BackupAggregator/Program.cs (via BaeldungNativeApi.cs)
- Exercise21/Program.cs
- Exercise22/Program.cs
- Exercise23/Program.cs
- Exercise24/Program.cs

#### 3. Update LearningCourseTestBase
Add per-test job tracking and cleanup:
```csharp
// Instance field for per-test job tracking
private readonly List<string> _jobIds = new();

// Modify ExecuteExerciseAsync to parse job IDs
protected async Task<(int exitCode, string output, string error)> ExecuteExerciseAsync(...)
{
    // ... existing code ...
    
    // Parse job IDs from output
    ParseJobIds(output);
    
    return (exitCode, output, error);
}

// Parse job IDs from stdout
private void ParseJobIds(string output)
{
    var matches = System.Text.RegularExpressions.Regex.Matches(
        output, 
        @"FLINK_JOB_ID:\s*([a-f0-9]{32})");
    
    foreach (System.Text.RegularExpressions.Match match in matches)
    {
        if (match.Groups.Count > 1)
        {
            var jobId = match.Groups[1].Value;
            _jobIds.Add(jobId);
            TestContext.WriteLine($"📋 Captured Flink job ID: {jobId}");
        }
    }
}

// Add TearDown method for per-test cleanup
[TearDown]
public async Task TearDown()
{
    if (_jobIds.Count == 0)
    {
        return;
    }
    
    TestContext.WriteLine($"🧹 Cleaning up {_jobIds.Count} Flink job(s) for this test...");
    
    var flinkGatewayUrl = Environment.GetEnvironmentVariable("FLINK_GATEWAY_URL") ?? "http://localhost:8080";
    using var httpClient = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(5) };
    
    foreach (var jobId in _jobIds)
    {
        try
        {
            var response = await httpClient.PatchAsync(
                $"{flinkGatewayUrl}/jobs/{jobId}?mode=cancel", 
                null);
            
            if (response.IsSuccessStatusCode)
            {
                TestContext.WriteLine($"   ✅ Cancelled job {jobId}");
            }
            else
            {
                TestContext.WriteLine($"   ⚠️ Failed to cancel job {jobId}: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"   ⚠️ Error cancelling job {jobId}: {ex.Message}");
        }
    }
    
    _jobIds.Clear();
}
```

### Challenges Encountered
- None yet - straightforward implementation

### Solutions Applied
- Use instance fields for thread-safe per-test tracking
- Parse job IDs with regex for reliability
- Best-effort cleanup to avoid test failures

## Phase 5: Testing & Validation

### Test Results
- TBD after implementation

### Performance Metrics
- Baseline: 10 tests max (10 slots)
- Target: 100+ tests with slot reuse
- Expected: Each test frees 1 slot immediately

## Phase 6: Owner Acceptance

### Demonstration
- TBD

### Owner Feedback
- TBD

### Final Approval
- TBD

## Lessons Learned & Future Reference (MANDATORY)

### What Worked Well
- Existing API already supported job ID tracking
- stdout parsing is simple and reliable
- Per-test cleanup is straightforward with NUnit TearDown

### What Could Be Improved
- Could add metrics for job slot utilization
- Could add warning if jobs don't cancel within timeout

### Key Insights for Similar Tasks
- Always investigate existing capabilities before designing new ones
- stdout is a reliable communication channel for test infrastructure
- Per-test cleanup is essential for parallel test scalability

### Specific Problems to Avoid in Future
- Don't assume APIs need modification without investigation
- Don't use global cleanup for parallel tests
- Don't fail tests due to cleanup errors (best effort)

### Reference for Future WIs
- This pattern (stdout job ID + per-test cleanup) should be used for all test suites that submit Flink jobs
- The regex pattern `FLINK_JOB_ID:\s*([a-f0-9]{32})` is the standard format
- TearDown should always be best-effort to avoid masking real test failures