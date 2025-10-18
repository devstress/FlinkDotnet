# WI85: Fix Prometheus Metrics "Connection Refused" Errors

**File**: `WIs/WI85_fix-prometheus-metrics-connection-refused.md`
**Title**: Fix Prometheus scraping targets showing "connection refused"
**Description**: 3 out of 5 Prometheus targets are DOWN with "connection refused" errors
**Priority**: High
**Component**: Observability Infrastructure
**Type**: Bug Fix
**Created**: 2025-10-17T21:21:00Z
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI81: Kafka JMX metrics scraping fixed
- WI83: Gateway Prometheus exporter implemented
- WI84: Initial investigation of targets DOWN issue

### Lessons Applied
- Debug first with actual error messages from Prometheus targets API
- Use JSON debug files to capture full error details
- Connection refused means port not listening, not DNS issues

## Phase 1: Investigation

### Debug Information (MANDATORY)
**Error Messages from Prometheus Targets API:**
```json
{
  "flink-jobmanager:9250": {
    "health": "down",
    "lastError": "Get \"http://flink-jobmanager:9250/\": dial tcp 10.89.1.6:9250: connect: connection refused"
  },
  "flink-taskmanager:9251": {
    "health": "down",
    "lastError": "Get \"http://flink-taskmanager:9251/\": dial tcp 10.89.1.4:9251: connect: connection refused"
  },
  "host.docker.internal:8080": {
    "health": "down",
    "lastError": "Get \"http://host.docker.internal:8080/metrics\": dial tcp 10.89.1.1:8080: connect: connection refused"
  },
  "kafka-exporter:5556": {
    "health": "up",
    "lastError": ""
  },
  "localhost:9090": {
    "health": "up",
    "lastError": ""
  }
}
```

**System State:**
- Prometheus container running and accessible
- All 10 infrastructure containers running
- Kafka metrics working (kafka-exporter UP)
- Prometheus self-monitoring working
- Flink containers running but metrics ports not listening
- Gateway running on host but not accessible from Prometheus container

**Reproduction Steps:**
1. Set `LEARNINGCOURSE=true`
2. Run Day05 Prometheus metrics test
3. Wait 60 seconds for scraping
4. Check targets API - 3/5 targets DOWN

**Evidence:**
- `prometheus-targets-debug.json` - Full targets status
- `prometheus-kafka-query.json` - Kafka query (empty results)
- `prometheus-flink-query.json` - Flink query (empty results)
- `prometheus-gateway-query.json` - Gateway query (empty results)

### Root Cause Analysis

#### Issue 1: Flink Prometheus Reporter Not Starting HTTP Server
**Problem:** Flink's `PrometheusReporter` is configured but ports 9250/9251 show "connection refused"

**Investigation:**
- FLINK_PROPERTIES includes `metrics.reporter.prom.port: 9250`
- Prometheus metrics JAR is mounted in containers
- But HTTP server is not listening on these ports

**Root Cause:** Flink's PrometheusReporter requires **explicit configuration** to start the HTTP server:
```yaml
metrics.reporter.prom.factory.class: org.apache.flink.metrics.prometheus.PrometheusReporterFactory
```

Without the `factory.class`, Flink uses the old deprecated configuration which may not start the HTTP endpoint properly.

#### Issue 2: Gateway Not Accessible via host.docker.internal
**Problem:** Gateway running on `localhost:8080` but Prometheus can't reach it via `host.docker.internal:8080`

**Investigation:**
- Gateway starts successfully on localhost:8080
- LEARNINGCOURSE environment variable now passed to Gateway (WI85 fix)
- Prometheus configured to scrape `host.docker.internal:8080/metrics`
- Connection refused from Prometheus container

**Root Cause:** The Gateway is listening on `localhost:8080` which only binds to loopback interface. To be accessible from Docker containers via `host.docker.internal`, it must bind to `0.0.0.0:8080` or `*:8080`.

Current config:
```csharp
.WithEnvironment("ASPNETCORE_URLS", $"http://localhost:{Ports.GatewayHostPort}")
```

Should be:
```csharp
.WithEnvironment("ASPNETCORE_URLS", $"http://0.0.0.0:{Ports.GatewayHostPort}")
```

### Findings Summary
1. ✅ Kafka metrics working perfectly (kafka-exporter UP)
2. ❌ Flink metrics configured but HTTP server not starting (missing factory.class)
3. ❌ Gateway metrics implemented but not accessible from Docker (localhost vs 0.0.0.0 binding)

## Phase 2: Design

### Solution 1: Fix Flink Prometheus Reporter Configuration
**Approach:** Add `metrics.reporter.prom.factory.class` to FLINK_PROPERTIES

**Configuration Changes:**
```csharp
// JobManager
var jobManagerFlinkProperties = isLearningCourseMode
    ? baseJobManagerFlinkProperties +
      "metrics.reporters: prom\n" +
      "metrics.reporter.prom.factory.class: org.apache.flink.metrics.prometheus.PrometheusReporterFactory\n" +
      "metrics.reporter.prom.port: 9250\n"
    : baseJobManagerFlinkProperties;

// TaskManager  
var taskManagerFlinkProperties = isLearningCourseMode
    ? baseTaskManagerFlinkProperties +
      "metrics.reporters: prom\n" +
      "metrics.reporter.prom.factory.class: org.apache.flink.metrics.prometheus.PrometheusReporterFactory\n" +
      "metrics.reporter.prom.port: 9251\n"
    : baseTaskManagerFlinkProperties;
```

**Why This Works:**
- `PrometheusReporterFactory` is the correct factory class for Flink 2.1.0
- Explicitly specifies HTTP server mode (not push gateway)
- Matches Flink documentation for Prometheus integration

### Solution 2: Fix Gateway Binding to Accept Docker Connections
**Approach:** Change Gateway ASPNETCORE_URLS to bind to 0.0.0.0 instead of localhost

**Configuration Change:**
```csharp
.WithEnvironment("ASPNETCORE_URLS", $"http://0.0.0.0:{Ports.GatewayHostPort}")
```

**Why This Works:**
- `0.0.0.0` binds to all network interfaces (localhost + external)
- Allows Docker containers to reach host via `host.docker.internal`
- Still accessible from host machine via `localhost:8080`

### Alternatives Considered
1. **Use Docker network mode "host" for Gateway** - Rejected: Breaks Aspire orchestration
2. **Expose Gateway container instead of host process** - Rejected: Gateway needs to be on host for development
3. **Use actual host IP instead of host.docker.internal** - Rejected: IP changes, not portable

## Phase 3: Implementation

### Files to Modify
1. `LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs`
   - Add `factory.class` to Flink metrics configuration
   - Change Gateway binding from localhost to 0.0.0.0

### Implementation Steps
1. Update JobManager FLINK_PROPERTIES (line ~143-148)
2. Update TaskManager FLINK_PROPERTIES (line ~204-209)
3. Update Gateway ASPNETCORE_URLS (line ~336)
4. Re-run test to verify all 5 targets UP
5. Validate metrics queries return data

## Phase 4: Testing & Validation

### Test Plan
1. Run `Day05PrometheusMetricsTest` with LEARNINGCOURSE=true
2. Verify all 5 Prometheus targets show UP status
3. Verify Kafka metrics return data
4. Verify Flink metrics return data
5. Verify Gateway metrics return data
6. Check debug JSON files for confirmation

### Expected Results
- Targets UP: 5/5 (all targets healthy)
- Kafka query: Returns messagesinpersec metric
- Flink query: Returns numRegisteredTaskManagers metric
- Gateway query: Returns jobs_submitted_total metric

## Lessons Learned & Future Reference

### What Worked Well
- Debug-first approach with actual error messages
- Using JSON debug files to capture full context
- Understanding connection refused vs DNS resolution errors

### Key Insights for Similar Tasks
- Flink Prometheus integration requires explicit factory class configuration
- ASP.NET Core services must bind to 0.0.0.0 to accept Docker container connections
- host.docker.internal only works if host service binds to all interfaces

### Problems to Avoid in Future
- Don't assume deprecated Prometheus reporter configs still work
- Always verify network binding when services need cross-container access
- Test metrics endpoints directly before configuring Prometheus scraping

### Reference for Future WIs
- Flink Prometheus Reporter requires `factory.class` configuration
- Gateway services for Docker must bind to `0.0.0.0`, not `localhost`
- Use Prometheus targets API JSON for detailed debugging