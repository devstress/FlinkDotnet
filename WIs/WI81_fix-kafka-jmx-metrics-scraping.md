# WI81: Fix Kafka JMX Metrics Scraping for Prometheus

**File**: `WIs/WI81_fix-kafka-jmx-metrics-scraping.md`
**Title**: [Observability] Fix Kafka JMX metrics export to Prometheus
**Description**: Implement proper Kafka JMX exporter configuration following best practices
**Priority**: High
**Component**: Observability
**Type**: Bug Fix
**Assignee**: AI Agent
**Created**: 2025-10-17
**Status**: Testing - SQL Gateway fix applied, Kafka JMX configured, awaiting test results

## Lessons Applied from Previous WIs
### Previous WI References
- WI80: Confirmed `.AddKafka()` is the correct approach (reverted manual container config)
- WI75: Initial Kafka metrics configuration was incomplete
### Lessons Applied
- Use Aspire's `.AddKafka()` for proper resource discovery
- Kafka JMX exporter needs explicit connection to Kafka JMX port
- Prometheus scrape configuration must target correct hostname:port
### Problems Prevented
- Avoid breaking Aspire resource discovery
- Don't use manual container configuration when Aspire provides built-in support

## Phase 1: Investigation

### Requirements
Fix Kafka JMX metrics scraping so Prometheus tests can verify Kafka metrics

### Debug Information (MANDATORY)
**Current Configuration Issues**:
1. **Kafka JMX Port**: Lines 77-83 set JMX opts but don't expose port properly
2. **JMX Exporter**: Lines 94-99 use `bitnami/jmx-exporter` but missing config and args
3. **Test Failures**: Prometheus queries return "No results found" for Kafka metrics

**User Guidance** (Expert recommendation):
```yaml
# Proper JMX exporter configuration
var jmx = builder.AddContainer("kafka-jmx-exporter","prom/jmx-exporter","0.20.0")
                 .WithArgs(new[]{"5556","kafka:9101"})  # Port and JMX target
                 .WithHttpEndpoint(targetPort: 5556, name: "metrics")
                 .WithReference(kafka);  # Ensure same network
```

**Kafka Configuration Required**:
```csharp
var kafka = builder.AddKafka("kafka")
                   .WithEnvironment("KAFKA_JMX_PORT","9101")
                   .WithEnvironment("KAFKA_JMX_HOSTNAME","0.0.0.0");
```

### Root Cause
The current Kafka JMX exporter setup is incomplete:
- Missing explicit JMX port binding (9101)
- Missing JMX exporter arguments (port + target)
- Using wrong JMX exporter image (bitnami vs prom/jmx-exporter)
- Not using `.WithReference(kafka)` for network connectivity

## Phase 2: Design

### Solution Architecture
1. **Kafka JMX Configuration**:
   - Expose JMX on port 9101 (standard non-conflicting port)
   - Bind to 0.0.0.0 for container access
   - Keep existing JMX options for security settings

2. **JMX Exporter Configuration**:
   - Use official `prom/jmx-exporter:0.20.0` image
   - Configure with args: `["5556", "kafka:9101"]` (listen port, JMX target)
   - Expose HTTP endpoint on port 5556 for Prometheus scraping
   - Use `.WithReference(kafka)` for proper network connectivity

3. **Prometheus Scraping**:
   - Already configured correctly in `prometheus-learningcourse.yml`
   - Target: `kafka-exporter:5556`
   - Job name: `kafka`

### Files to Modify
- `LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs` (lines 70-100)

## Phase 3: Implementation

### Changes Completed

**File**: `LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs`

#### 1. Kafka JMX Configuration (Lines 72-86)
```csharp
var kafkaBuilder = builder.AddKafka("kafka");

// Enable JMX for metrics export only in LEARNINGCOURSE mode
if (isLearningCourseMode)
{
    kafkaBuilder = kafkaBuilder
        .WithEnvironment("KAFKA_JMX_PORT", "9101")
        .WithEnvironment("KAFKA_JMX_HOSTNAME", "0.0.0.0")
        .WithEnvironment("KAFKA_JMX_OPTS",
            "-Dcom.sun.management.jmxremote " +
            "-Dcom.sun.management.jmxremote.authenticate=false " +
            "-Dcom.sun.management.jmxremote.ssl=false " +
            "-Djava.rmi.server.hostname=kafka " +
            "-Dcom.sun.management.jmxremote.rmi.port=9101");
    Console.WriteLine("   📊 Kafka JMX metrics enabled on port 9101");
}
```

**Changes**:
- ✅ Added `KAFKA_JMX_PORT=9101` (explicit port binding)
- ✅ Added `KAFKA_JMX_HOSTNAME=0.0.0.0` (allows container access)
- ✅ Updated RMI port to 9101 (matches JMX port, avoids random ports)

#### 2. JMX Exporter Configuration (Lines 89-102)
```csharp
// Kafka JMX Exporter - only in LEARNINGCOURSE mode
if (isLearningCourseMode)
{
    Console.WriteLine("   📊 Deploying Kafka JMX Exporter for metrics collection");
    var kafkaExporter = builder.AddContainer("kafka-exporter", "prom/jmx-exporter", "0.20.0")
        .WithArgs("5556", "kafka:9101")  // Listen port, JMX target
        .WithHttpEndpoint(targetPort: 5556, name: "metrics")
        .WithReference(kafka);  // Ensure same Docker network
    
    Console.WriteLine("   📊 Kafka JMX Exporter configured: kafka:9101 → :5556/metrics");
}
```

**Changes**:
- ✅ Changed from `bitnami/jmx-exporter` to `prom/jmx-exporter:0.20.0` (official image)
- ✅ Added explicit args: `["5556", "kafka:9101"]` (listen port, JMX target)
- ✅ Added `.WithReference(kafka)` (ensures same Docker network)
- ✅ Removed environment variables (not needed with args-based config)

#### 3. Prometheus Configuration Consolidation
**File**: `LocalTesting/prometheus.yml`

- ✅ Deleted duplicate `prometheus-learningcourse.yml`
- ✅ Merged all scrape configs into single `prometheus.yml`
- ✅ Kafka scrape target: `kafka-exporter:5556`
- ✅ Updated Program.cs to always use `prometheus.yml` (line 432)

#### 4. SQL Gateway Fix (Lines 305-308)
```csharp
sqlGatewayBuilder = sqlGatewayBuilder
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithEnvironment("LOG_FILE_PATH", "/opt/flink/test-logs")
    .WithEnvironment("FLINK_PROPERTIES", sqlGatewayFlinkProperties);  // ALWAYS set
```

**Changes**:
- ✅ SQL Gateway now ALWAYS gets `FLINK_PROPERTIES` (even in LEARNINGCOURSE mode)
- ✅ Fixes "Missing required options: address" error
- ✅ Ensures SQL Gateway starts correctly

## Phase 4: Testing & Validation

### Build Status: ✅ SUCCESS
```
Build succeeded.
    2 Warning(s) (non-critical)
    0 Error(s)
Time Elapsed 00:00:19.10
```

### Test Execution Results

**Test Run 1** (After Kafka JMX configuration):
- ✅ **Grafana test PASSED**
- ✅ **Exercise1 completed successfully** (50/50 messages)
- ❌ **Prometheus test FAILED** - "No results found" for Flink metrics
- ⚠️ **Prometheus targets**: 1 UP, 3 DOWN (should have 4 UP)

**Issues Identified**:
1. **SQL Gateway failing to start** - Missing `address` configuration (NOW FIXED)
2. **Flink metrics not exported** - Prometheus can't scrape JobManager/TaskManager
3. **Kafka JMX exporter status unknown** - Need to verify it's running and scraping

### Next Test Run Expected Results

With SQL Gateway fix applied, we expect:
- ✅ SQL Gateway starts successfully
- ✅ All 4 Prometheus targets UP (JobManager, TaskManager, Kafka, Prometheus)
- ✅ Flink metrics appear in Prometheus queries
- ✅ Kafka metrics appear in Prometheus queries
- ✅ Both Day05 tests PASS

## Phase 5: Verification Commands

### Manual Verification (when tests run)
```bash
# Check all containers are running
docker ps --filter "name=kafka|flink|prometheus"

# Verify Kafka JMX exporter is accessible
curl http://localhost:5556/metrics

# Verify Flink JobManager metrics
curl http://localhost:9250

# Verify Flink TaskManager metrics
curl http://localhost:9251

# Check Prometheus targets status
curl http://localhost:9090/api/v1/targets | jq '.data.activeTargets[] | {job, health, lastError}'
```

### Expected Prometheus Metrics

**Kafka Metrics** (from JMX Exporter):
```promql
# Controller status (should be 1)
kafka_controller_kafkacontroller_activecontrollercount

# Messages in per second
rate(kafka_server_brokertopicmetrics_messages_in_total[5m])
```

**Flink Metrics** (from Prometheus Reporter):
```promql
# Records received by TaskManager
flink_taskmanager_job_task_operator_numRecordsIn

# Records sent by TaskManager
flink_taskmanager_job_task_operator_numRecordsOut
```


### Changes Required

**File**: `LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs`

**Lines 72-86**: Update Kafka configuration
```csharp
var kafkaBuilder = builder.AddKafka("kafka");

// Enable JMX for metrics export only in LEARNINGCOURSE mode
if (isLearningCourseMode)
{
    kafkaBuilder = kafkaBuilder
        .WithEnvironment("KAFKA_JMX_PORT", "9101")
        .WithEnvironment("KAFKA_JMX_HOSTNAME", "0.0.0.0")
        .WithEnvironment("KAFKA_JMX_OPTS",
            "-Dcom.sun.management.jmxremote " +
            "-Dcom.sun.management.jmxremote.authenticate=false " +
            "-Dcom.sun.management.jmxremote.ssl=false " +
            "-Djava.rmi.server.hostname=kafka " +
            "-Dcom.sun.management.jmxremote.rmi.port=9101");
    Console.WriteLine("   📊 Kafka JMX metrics enabled on port 9101");
}
```

**Lines 89-100**: Update JMX Exporter configuration
```csharp
// Kafka JMX Exporter - only in LEARNINGCOURSE mode
if (isLearningCourseMode)
{
    Console.WriteLine("   📊 Deploying Kafka JMX Exporter for metrics collection");
    var kafkaExporter = builder.AddContainer("kafka-exporter", "prom/jmx-exporter", "0.20.0")
        .WithArgs("5556", "kafka:9101")  // Listen port, JMX target
        .WithHttpEndpoint(targetPort: 5556, name: "metrics")
        .WithReference(kafka);  // Ensure same Docker network
    
    Console.WriteLine("   📊 Kafka JMX Exporter configured: kafka:9101 → :5556/metrics");
}
```

### Why This Works
1. **Port 9101**: Non-conflicting standard JMX port
2. **Hostname binding**: `0.0.0.0` allows container-to-container access
3. **RMI port**: Explicitly set to match JMX port (9101) to avoid random ports
4. **Official exporter**: `prom/jmx-exporter` is the standard Prometheus JMX exporter
5. **Explicit args**: `["5556", "kafka:9101"]` tells exporter where to scrape JMX
6. **WithReference**: Ensures kafka-exporter and kafka share the same Docker network

## Phase 4: Testing & Validation

### Test Metrics to Verify

**Kafka Broker Health**:
```promql
# Controller status (should be 1)
kafka_controller_kafkacontroller_activecontrollercount

# Offline partitions (should be 0)
kafka_controller_kafkacontroller_offlinepartitionscount

# Under-replicated partitions (should be 0)
kafka_server_replicamanager_underreplicatedpartitions
```

**Kafka Throughput**:
```promql
# Messages in per second
rate(kafka_server_brokertopicmetrics_messages_in_total[5m])

# Bytes in/out per second
rate(kafka_server_brokertopicmetrics_bytes_in_total[5m])
rate(kafka_server_brokertopicmetrics_bytes_out_total[5m])
```

### Validation Steps
1. Build solution: `dotnet build LocalTesting/LocalTesting.sln --configuration Release`
2. Run Day05 tests: `dotnet test LearningCourse/IntegrationTests.sln --filter Day05Tests`
3. Verify Prometheus can scrape Kafka metrics
4. Verify test assertions pass with real metric values (not "No results found")

## Lessons Learned & Future Reference (MANDATORY)

### What Worked Well
- User provided expert guidance on proper JMX exporter configuration
- Official Prometheus JMX exporter is the correct tool for the job

### What Could Be Improved
- Should have used official exporter from the start
- JMX port configuration needs to be explicit (9101) not relying on defaults

### Key Insights for Similar Tasks
- Always use official exporters when available (`prom/jmx-exporter` not `bitnami/jmx-exporter`)
- JMX configuration requires explicit port binding and RMI port matching
- `.WithReference()` is critical for container-to-container communication
- Test with actual PromQL queries to verify metrics are exported

### Specific Problems to Avoid in Future
- ❌ Don't use generic JMX exporters without proper configuration
- ❌ Don't assume JMX ports are automatically configured
- ✅ Always verify exporter can reach JMX endpoint before adding to Prometheus
- ✅ Test metrics scraping before running full integration tests

### Reference for Future WIs
- Use this pattern for other JMX-based services (e.g., Java applications)
- JMX exporter args format: `["listen_port", "target_host:jmx_port"]`
- Always mount JMX config files when using custom metrics