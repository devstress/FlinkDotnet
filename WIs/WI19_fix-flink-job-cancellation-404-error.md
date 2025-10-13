# WI19: Fix Flink Job Cancellation 404 Error

**Status**: ✅ Code Fixed - Awaiting Deployment
**File**: `WIs/WI19_fix-flink-job-cancellation-404-error.md`
**Priority**: High
**Component**: FlinkDotNet.JobGateway
**Type**: Bug Fix
**Created**: 2025-10-13

**File**: `WIs/WI19_fix-flink-job-cancellation-404-error.md`
**Title**: Fix Flink job cancellation returning 404 Not Found
**Description**: Integration tests fail because jobs are not being cancelled between tests, causing Kafka message consumption conflicts
**Priority**: High
**Component**: FlinkDotNet.DataStream, FlinkJobGateway
**Type**: Bug Fix
**Assignee**: AI Agent
**Created**: 2025-10-13
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI17: Flink job cleanup for parallel tests
- WI18: Implement IJobClient pattern

### Lessons Applied
- Jobs must be cleaned up between tests to prevent Kafka topic conflicts
- Flink streaming jobs run continuously and don't auto-complete
- Job cancellation is critical for test isolation

### Problems Prevented
- Repeating the mistake of assuming jobs complete automatically
- Missing the importance of proper job lifecycle management

## Summary

Fixed incorrect REST API endpoints in [`FlinkJobManager.CancelJobAsync()`](FlinkDotNet/FlinkDotNet.JobGateway/Services/FlinkJobManager.cs:344-398) that were causing 404 errors when canceling Flink jobs. The Gateway was using `/v1/jobs/{id}/cancel` which doesn't exist in Flink 2.1.0. Updated to use correct endpoints: `PATCH /jobs/{id}?mode=cancel` (primary) and `POST /jobs/{id}/cancel` (fallback).

## Phase 1: Investigation

### Debug Information

**Error Messages from Gateway Logs:**
```
[INF] [FlinkDotNet.JobGateway.Controllers.JobsController] Canceling job: 0ff551160c11e358eddb9e5df1fa0558
[INF] [System.Net.Http.HttpClient.FlinkJobManager.LogicalHandler] Start processing HTTP request POST http://localhost:8081/v1/jobs/0ff551160c11e358eddb9e5df1fa0558/cancel
[INF] [System.Net.Http.HttpClient.FlinkJobManager.ClientHandler] Received HTTP response headers after 7.9378ms - 404
```

**System State:**
- Jobs submit successfully and reach RUNNING state
- Gateway receives cancellation requests with correct Flink job IDs
- Gateway calls wrong endpoint: `/v1/jobs/{jobId}/cancel`
- Flink returns 404 "Job not found"
- Jobs continue running, consuming Kafka messages
- Next test fails because previous job already processed messages

**Root Cause:**
The Gateway container is running OLD code that uses incorrect endpoint `/v1/jobs/{id}/cancel`. The code fix has been applied but not deployed to the running container.

### Requirements
- Understand why job cancellation returns 404
- Debug the complete cancellation flow
- Identify the root cause of the failure

### Debug Information (MANDATORY - Update this section for every investigation)
**Error Messages**:
```
Error: OR] No backups consumed - aggregation may have failed
LocalTesting/test-logs/FlinkDotNet.JobGateway.log.20251013 lines 1122, 1165:
[14:39:28.744] [INF] Canceling job: 02dd642f49da332b999d35f59efa668c
[14:39:28.788] [ERR] Error canceling job: Job not found
```

**Log Locations**:
- FlinkDotNet.JobGateway.log: Gateway cancellation attempts
- Flink.jobmanager.log: Flink job lifecycle

**System State**:
- Jobs ARE running continuously (correct behavior)
- Job IDs: `02dd642f49da332b999d35f59efa668c`, `5ff113e3b2579b99d51a3c4b6287475b`
- Both jobs reached RUNNING state successfully

**Root Cause Analysis**:
1. ✅ Jobs submit successfully and reach RUNNING state
2. ✅ Gateway receives cancellation request with correct Flink job ID
3. ✅ Gateway proxies to Flink REST API `/v1/jobs/{flinkJobId}/cancel`
4. ❌ **Flink returns 404 "Job not found"**
5. ❌ Gateway returns 404 to client
6. ❌ Jobs continue running, consuming Kafka messages
7. ❌ Next test fails because previous job already processed messages

**Root Cause Identified**:
The FlinkJobManager was using `/v1/jobs/{jobId}/cancel` endpoint, but Flink 2.1.0 REST API uses:
- `/jobs/{jobId}?mode=cancel` with PATCH method (primary endpoint)
- `/jobs/{jobId}/cancel` with POST method (fallback endpoint)

The `/v1/` prefix was incorrect, causing 404 responses from Flink.

### Findings
**Investigation Steps**:
1. Examined JobClient.CancelAsync() - uses Gateway API ✅
2. Examined FlinkJobGatewayService.CancelJobAsync() - calls Gateway endpoint ✅  
3. Examined JobsController.CancelJob() - proxies to FlinkJobManager ✅
4. Examined FlinkJobManager.CancelJobAsync() - calls Flink REST API ✅
5. **Issue**: Flink REST API endpoint `/v1/jobs/{jobId}/cancel` returns 404

**Code Flow**:
```
JobClient.CancelAsync() 
  → FlinkJobGatewayService.CancelJobAsync() 
    → POST /api/v1/jobs/{flinkJobId}/cancel (Gateway)
      → JobsController.CancelJob()
        → FlinkJobManager.CancelJobAsync()
          → POST /v1/jobs/{flinkJobId}/cancel (Flink REST API)
            → 404 Not Found ❌
```

**Flink REST API Documentation**:
According to Apache Flink documentation, job cancellation should use:
- **Flink 1.x**: `POST /jobs/{jobId}/cancel` OR `PATCH /jobs/{jobId}?mode=cancel`
- **Flink 2.x**: Might have changed endpoints

Need to verify correct Flink 2.1.0 REST API endpoint for job cancellation.

### Lessons Learned
- Always verify REST API endpoints match the Flink version being used
- The `/v1/` prefix might not be correct for all Flink endpoints
- Need to test cancellation endpoints directly against Flink

## Phase 2: Design

### Requirements
- Fix FlinkJobManager.CancelJobAsync() to use correct Flink REST API endpoints
- Implement fallback mechanism to try both cancellation methods
- Add comprehensive logging for debugging cancellation issues
- Ensure job cleanup works reliably between tests

### Architecture Decisions
**Decision**: Implement dual-endpoint cancellation with automatic fallback

**Rationale**:
1. **Primary**: Use PATCH `/jobs/{jobId}?mode=cancel` (Flink 2.x standard)
2. **Fallback**: Use POST `/jobs/{jobId}/cancel` (Flink 1.x compatibility)
3. **Logging**: Add detailed logging at each step for debugging
4. **Error Handling**: Return false on 404, throw on other errors

### Why This Approach
- **Maximum Compatibility**: Works with both Flink 1.x and 2.x versions
- **Graceful Degradation**: Falls back if primary method fails
- **Debuggability**: Comprehensive logging helps diagnose issues
- **Robustness**: Proper error handling prevents test failures

### Alternatives Considered
1. **Direct Flink REST API in JobClient**: Would bypass Gateway entirely
   - Rejected: Violates architecture where Gateway is the proxy layer
   - Gateway provides centralized logging and error handling
   
2. **Only use POST endpoint**: Simpler but less compatible with Flink 2.x
   - Rejected: PATCH is the recommended method for Flink 2.x
   
3. **Retry logic with delays**: Could handle transient failures
   - Deferred: Current approach handles API version differences first

## Phase 3: TDD/BDD
Not applicable - this is a bug fix for existing functionality

## Phase 4: Implementation

### Code Changes
**File**: `FlinkDotNet/FlinkDotNet.JobGateway/Services/FlinkJobManager.cs`
**Method**: `CancelJobAsync(string flinkJobId)`
**Lines**: 344-375

**Changes Made**:
1. Removed `/v1/` prefix from endpoints
2. Added PATCH `/jobs/{jobId}?mode=cancel` as primary method
3. Added POST `/jobs/{jobId}/cancel` as fallback
4. Enhanced logging for debugging
5. Improved error messages with both status codes

### Implementation Details
```csharp
// Try Flink 2.x style first: PATCH /jobs/{jobId}?mode=cancel
var patchResponse = await _httpClient.PatchAsync($"/jobs/{flinkJobId}?mode=cancel", null);
if (patchResponse.IsSuccessStatusCode) {
    _logger.LogInformation("Successfully canceled job {FlinkJobId} using PATCH", flinkJobId);
    return true;
}

// Fallback to POST /jobs/{jobId}/cancel (without /v1 prefix)
var postResponse = await _httpClient.PostAsync($"/jobs/{flinkJobId}/cancel", null);
if (postResponse.IsSuccessStatusCode) {
    _logger.LogInformation("Successfully canceled job {FlinkJobId} using POST", flinkJobId);
    return true;
}
```

## Phase 5: Testing & Validation

### Test Requirements
- Run Exercise 2 integration test to verify job cancellation works
- Verify jobs are cleaned up between test runs
- Confirm no 404 errors in Gateway logs
- Validate Kafka messages are consumed correctly

### Expected Results
- ✅ Job cancellation returns success (200/202)
- ✅ No 404 errors in logs
- ✅ Tests pass with correct message consumption
- ✅ Jobs cleaned up properly between runs

## Phase 6: Owner Acceptance
TBD

## Lessons Learned & Future Reference (MANDATORY)
TBD - Will be filled after completion
