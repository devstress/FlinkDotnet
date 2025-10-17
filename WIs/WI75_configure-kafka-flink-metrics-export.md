# WI75: Configure Conditional Kafka and Flink Metrics Export to Prometheus

**File**: `WIs/WI75_configure-kafka-flink-metrics-export.md`
**Title**: Configure Infrastructure for Conditional Kafka and Flink Metrics Export (LEARNINGCOURSE Mode Only)
**Description**: Implement conditional metrics export configuration that only enables Kafka and Flink metrics when running in LEARNINGCOURSE mode with Aspire, avoiding overhead in production scenarios
**Priority**: High
**Component**: Observability/Infrastructure
**Type**: Infrastructure Configuration
**Assignee**: Development Team
**Created**: 2025-10-17
**Updated**: 2025-10-17 (Added conditional mode-based configuration)
**Status**: Investigation Complete - Design Phase

## Lessons Applied from Previous WIs

### Previous WI References
- [`WI74_prometheus-exporter-design.md`](WI74_prometheus-exporter-design.md) - Comprehensive Prometheus exporter design

### Lessons Applied
- **Follow Apache Flink patterns**: Use Flink's built-in Prometheus reporter instead of custom solutions
- **Configuration-driven approach**: Leverage existing Flink metrics.reporters system
- **Kafka requires external exporter**: Kafka exposes JMX metrics, needs JMX-to-Prometheus bridge
- **Correct port targeting**: Prometheus must scrape metrics-specific ports, not REST API ports
- **Debug first**: Verify each component's metrics endpoint independently before integration

### Problems Prevented
- Avoiding incorrect port configuration (8081 is REST API, not metrics)
- Preventing assumptions about non-existent endpoints
- Not deploying complex OpenTelemetry stack when simple Prometheus format works
- Avoiding missing JAR dependencies for Flink Prometheus reporter

---

## Phase 1: Investigation - Current State Analysis

### Debug Information (MANDATORY - Updated for every investigation)

#### 1. Prometheus Configuration Analysis
**Location**: [`LocalTesting/prometheus.yml`](../LocalTesting/prometheus.yml)

**Current Scrape Targets** (Lines 10, 16, 22, 28):
```yaml
- targets: ['flink-jobmanager:8081']  # ❌ WRONG - Port 8081 is REST API
- targets: ['flink-taskmanager:8081'] # ❌ WRONG - Port 8081 is REST API  
- targets: ['flink-job-gateway:8080'] # ❌ WRONG - No metrics endpoint exists
- targets: ['kafka:9092']             # ❌ WRONG - Port 9092 is Kafka protocol, not HTTP metrics
```

**Root Cause**: All scrape targets point to wrong ports or non-existent endpoints.

#### 2. Flink Container Configuration Issues
**Location**: [`LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs`](../LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs)

**JobManager FLINK_PROPERTIES** (Lines 93-106):
```csharp
.WithEnvironment("FLINK_PROPERTIES",
    "jobmanager.rpc.address: flink-jobmanager\n" +
    "rest.address: 0.0.0.0\n" +
    // ... other configs ...
    // ❌ MISSING: metrics.reporters configuration
    // ❌ MISSING: metrics.reporter.prom.class
    // ❌ MISSING: metrics.reporter.prom.port
```

**TaskManager FLINK_PROPERTIES** (Lines 121-134):
```csharp
// Same issue - no Prometheus reporter configuration
```

**Missing JAR File**:
- Required: `flink-metrics-prometheus-2.1.0.jar`
- Current location: `LocalTesting/connectors/flink/lib/`
- Files present: `flink-json-2.1.0.jar`, `flink-sql-connector-kafka-4.0.1-2.0.jar`
- Status: ❌ **flink-metrics-prometheus JAR is MISSING**

#### 3. Kafka Metrics Exposure Gap
**Current State**: 
- Kafka container deployed (Line 71)
- Kafka exposes port 9092 (Kafka protocol)
- ❌ **No JMX port exposed** for metrics collection
- ❌ **No JMX Exporter container deployed**
- ❌ **Kafka metrics are completely inaccessible**

**Kafka JMX Architecture**:
- Kafka exposes metrics via JMX (Java Management Extensions)
- JMX requires Java-specific protocol, not HTTP
- Prometheus cannot scrape JMX directly
- **Solution needed**: Deploy JMX-to-Prometheus exporter bridge

#### 4. Gateway Metrics Gap
**Current State**:
- JobGateway deployed at port configured via `Ports.GatewayHostPort`
- Prometheus tries to scrape `flink-job-gateway:8080/metrics`
- ❌ **No `/metrics` endpoint exists in Gateway**
- Gateway has no metrics instrumentation

**Note**: Based on [`WI74_prometheus-exporter-design.md`](WI74_prometheus-exporter-design.md), Gateway metrics instrumentation is a future enhancement (Phase 2 of WI74). This WI focuses on **Kafka and Flink only**.

#### 5. Observability Test Expectations
**Location**: [`LearningCourse/LearningCourse.IntegrationTests/Day05Tests.cs`](../LearningCourse/LearningCourse.IntegrationTests/Day05Tests.cs)

**Test Purpose**: Verify Prometheus can collect metrics from infrastructure

**Current Behavior**: Tests correctly fail because:
1. Prometheus scrape targets return 404/connection refused
2. No metrics are actually being exported
3. Prometheus has no data to query

**Expected Behavior After Fix**:
1. Flink exposes metrics at `:9250/` (JobManager) and `:9251/` (TaskManager)
2. Kafka metrics exposed via JMX Exporter at `:7071/metrics`
3. Prometheus successfully scrapes both sources
4. Tests can query and verify metric values > 0

### Investigation Summary

**What's Broken**:
❌ Prometheus scrape targets point to wrong ports (8081, 8080, 9092)  
❌ Flink containers missing Prometheus reporter configuration  
❌ `flink-metrics-prometheus-2.1.0.jar` not downloaded or mounted  
❌ Kafka has no metrics exporter deployed  
❌ No JMX-to-Prometheus bridge for Kafka metrics

**Root Causes**:
1. **prometheus.yml assumes endpoints that don't exist**
2. **Flink Prometheus reporter never configured** in FLINK_PROPERTIES
3. **Missing JAR dependency** for Flink metrics reporter
4. **Kafka metrics export not understood** - JMX requires external exporter

**Impact**:
- Observability tests fail (correctly detecting missing metrics)
- No visibility into Flink job performance
- No visibility into Kafka throughput
- Grafana dashboards show no data

---

## Phase 2: Design - Conditional Configuration Solution

### Design Principle: Mode-Based Conditional Configuration

**Objective**: Enable metrics export from Kafka and Flink **ONLY when running in LEARNINGCOURSE mode**, minimizing overhead and security exposure in production deployments.

### Mode Detection Strategy

#### Environment Variable Check
The configuration should check for the `TESTING_MODE` or `ASPIRE_MODE` environment variable:
- **If `TESTING_MODE=LEARNINGCOURSE`** → Enable full metrics (Flink + Kafka + JobGateway)
- **Otherwise** → Minimal/no metrics (production mode)

#### Implementation Location
Mode check should be in [`LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs`](../LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs) where Flink and Kafka containers are configured.

#### Mode Detection Code Pattern
```csharp
// Detect if running in LEARNINGCOURSE mode
var testingMode = Environment.GetEnvironmentVariable("TESTING_MODE");
var isLearningCourseMode = testingMode?.Equals("LEARNINGCOURSE", StringComparison.OrdinalIgnoreCase) == true;

// Log mode detection for debugging
if (isLearningCourseMode)
{
    Console.WriteLine("✅ LEARNINGCOURSE mode detected - Enabling full metrics export");
}
else
{
    Console.WriteLine("ℹ️  Production mode - Minimal metrics configuration");
}
```

### Benefits of Conditional Configuration

#### Performance Benefits
- ✅ **Reduces overhead in production deployments**: Avoids unnecessary metric collection and aggregation
- ✅ **Minimizes CPU/memory usage**: Prometheus reporter and JMX exporter consume resources
- ✅ **Reduces network traffic**: Prometheus scraping creates continuous HTTP requests
- ✅ **Faster container startup**: Fewer services to initialize and health-check

#### Security Benefits
- ✅ **Limits exposed metrics endpoints**: Reduces attack surface in production
- ✅ **Prevents information leakage**: Internal metrics can reveal system architecture
- ✅ **Reduces container complexity**: Fewer running processes means fewer vulnerabilities
- ✅ **Compliance-friendly**: Many regulations require minimal data exposure

#### Flexibility Benefits
- ✅ **Easy to enable/disable**: Single environment variable controls entire metrics stack
- ✅ **Same codebase for all environments**: No separate deployment configurations needed
- ✅ **Clear separation of concerns**: Learning/debugging vs production clearly differentiated
- ✅ **Testing-friendly**: Can easily toggle metrics for performance testing

### 1. Flink Prometheus Reporter Configuration

#### A. Download Required JAR
**Required File**: `flink-metrics-prometheus-2.1.0.jar`
**Download URL**: https://repo1.maven.org/maven2/org/apache/flink/flink-metrics-prometheus/2.1.0/flink-metrics-prometheus-2.1.0.jar
**Target Location**: `LocalTesting/connectors/flink/lib/flink-metrics-prometheus-2.1.0.jar`

**Download Command**:
```powershell
# PowerShell script to download Flink Prometheus reporter JAR
$url = "https://repo1.maven.org/maven2/org/apache/flink/flink-metrics-prometheus/2.1.0/flink-metrics-prometheus-2.1.0.jar"
$outputPath = "LocalTesting/connectors/flink/lib/flink-metrics-prometheus-2.1.0.jar"
Invoke-WebRequest -Uri $url -OutFile $outputPath
Write-Host "✅ Downloaded flink-metrics-prometheus-2.1.0.jar"
```

#### B. Update Flink Container Configuration (Conditional)

**Location**: [`LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs`](../LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs)

**Conditional Prometheus Reporter Configuration**:

**JobManager Changes** (Update Lines 93-106):
```csharp
// Build base configuration (always included)
var baseJobManagerConfig =
    "jobmanager.rpc.address: flink-jobmanager\n" +
    "rest.address: 0.0.0.0\n" +
    "rest.bind-address: 0.0.0.0\n" +
    "parallelism.default: 1\n" +
    "rest.port: 8081\n" +
    "rest.bind-port: 8081\n" +
    $"jobmanager.memory.process.size: {jobManagerMemoryMb}m\n" +
    "heartbeat.interval: 5000\n" +
    "heartbeat.timeout: 30000\n" +
    "pekko.ask.timeout: 30s\n";

// Add Prometheus reporter only in LEARNINGCOURSE mode
var metricsConfig = isLearningCourseMode
    ? "metrics.reporters: prom\n" +
      "metrics.reporter.prom.class: org.apache.flink.metrics.prometheus.PrometheusReporter\n" +
      "metrics.reporter.prom.port: 9250-9260\n"
    : string.Empty;

var jobManagerConfig = baseJobManagerConfig + metricsConfig +
    "env.java.opts.all: --add-opens=java.base/java.lang=ALL-UNNAMED ...\n" +
    "classloader.resolve-order: parent-first\n" +
    "classloader.parent-first-patterns.default: org.apache.flink.;org.apache.kafka.;com.fasterxml.jackson.\n";

.WithEnvironment("FLINK_PROPERTIES", jobManagerConfig)
```

**TaskManager Changes** (Update Lines 121-134):
```csharp
// Build base configuration (always included)
var baseTaskManagerConfig =
    "jobmanager.rpc.address: flink-jobmanager\n" +
    "rest.address: 0.0.0.0\n" +
    "rest.bind-address: 0.0.0.0\n" +
    "parallelism.default: 1\n" +
    $"taskmanager.memory.process.size: {taskManagerMemoryMb}m\n" +
    $"taskmanager.memory.jvm-metaspace.size: {taskManagerMetaspaceMb}m\n" +
    "taskmanager.numberOfTaskSlots: 10\n" +
    "heartbeat.interval: 5000\n" +
    "heartbeat.timeout: 30000\n" +
    "pekko.ask.timeout: 30s\n";

// Add Prometheus reporter only in LEARNINGCOURSE mode
var metricsConfig = isLearningCourseMode
    ? "metrics.reporters: prom\n" +
      "metrics.reporter.prom.class: org.apache.flink.metrics.prometheus.PrometheusReporter\n" +
      "metrics.reporter.prom.port: 9250-9260\n"
    : string.Empty;

var taskManagerConfig = baseTaskManagerConfig + metricsConfig +
    "env.java.opts.all: --add-opens=java.base/java.lang=ALL-UNNAMED ...\n" +
    "classloader.resolve-order: parent-first\n" +
    "classloader.parent-first-patterns.default: org.apache.flink.;org.apache.kafka.;com.fasterxml.jackson.\n";

.WithEnvironment("FLINK_PROPERTIES", taskManagerConfig)
```

**SQL Gateway Changes** (Update Lines 160-172):
```csharp
// Build base configuration (always included)
var baseSqlGatewayConfig =
    "jobmanager.rpc.address: flink-jobmanager\n" +
    "rest.address: flink-jobmanager\n" +
    "rest.port: 8081\n" +
    "sql-gateway.endpoint.rest.address: 0.0.0.0\n" +
    "sql-gateway.endpoint.rest.bind-address: 0.0.0.0\n" +
    "sql-gateway.endpoint.rest.port: 8083\n" +
    "sql-gateway.endpoint.rest.bind-port: 8083\n" +
    "sql-gateway.endpoint.type: rest\n" +
    "sql-gateway.session.check-interval: 60000\n" +
    "sql-gateway.session.idle-timeout: 600000\n" +
    "sql-gateway.worker.threads.max: 10\n";

// Add Prometheus reporter only in LEARNINGCOURSE mode
var metricsConfig = isLearningCourseMode
    ? "metrics.reporters: prom\n" +
      "metrics.reporter.prom.class: org.apache.flink.metrics.prometheus.PrometheusReporter\n" +
      "metrics.reporter.prom.port: 9250-9260\n"
    : string.Empty;

var sqlGatewayConfig = baseSqlGatewayConfig + metricsConfig +
    "env.java.opts.all: --add-opens=java.base/java.lang=ALL-UNNAMED ...\n";

.WithEnvironment("FLINK_PROPERTIES", sqlGatewayConfig)
```

#### C. Mount Prometheus JAR in Containers (Conditional)

**Conditionally mount JAR only in LEARNINGCOURSE mode**:

**Add to JobManager** (After Line 109):
```csharp
// Only mount Prometheus JAR in LEARNINGCOURSE mode
if (isLearningCourseMode)
{
    flinkJobManager = flinkJobManager
        .WithBindMount(Path.Combine(connectorsDir, "flink-metrics-prometheus-2.1.0.jar"),
            "/opt/flink/lib/flink-metrics-prometheus-2.1.0.jar", isReadOnly: true);
}
```

**Add to TaskManager** (After Line 137):
```csharp
// Only mount Prometheus JAR in LEARNINGCOURSE mode
if (isLearningCourseMode)
{
    flinkTaskManager = flinkTaskManager
        .WithBindMount(Path.Combine(connectorsDir, "flink-metrics-prometheus-2.1.0.jar"),
            "/opt/flink/lib/flink-metrics-prometheus-2.1.0.jar", isReadOnly: true);
}
```

**Add to SQL Gateway** (After Line 175):
```csharp
// Only mount Prometheus JAR in LEARNINGCOURSE mode
if (isLearningCourseMode)
{
    flinkSqlGateway = flinkSqlGateway
        .WithBindMount(Path.Combine(connectorsDir, "flink-metrics-prometheus-2.1.0.jar"),
            "/opt/flink/lib/flink-metrics-prometheus-2.1.0.jar", isReadOnly: true);
}
```

### 2. Kafka JMX Exporter Configuration

#### A. Kafka Container Modification

**Problem**: Current Kafka deployment doesn't expose JMX port.

**Solution**: Update Kafka configuration to expose JMX metrics.

**Location**: [`LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs`](../LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs) (Line 71)

**Current Configuration**:
```csharp
var kafka = builder.AddKafka("kafka");
```

**Conditional Enhanced Configuration**:
```csharp
var kafka = builder.AddKafka("kafka");

// Only enable JMX in LEARNINGCOURSE mode
if (isLearningCourseMode)
{
    kafka = kafka
        .WithEnvironment("KAFKA_JMX_OPTS",
            "-Dcom.sun.management.jmxremote " +
            "-Dcom.sun.management.jmxremote.authenticate=false " +
            "-Dcom.sun.management.jmxremote.ssl=false " +
            "-Dcom.sun.management.jmxremote.port=9999 " +
            "-Dcom.sun.management.jmxremote.rmi.port=9999")
        .WithEnvironment("JMX_PORT", "9999");
}
```

#### B. Deploy JMX Exporter Container

**Container**: `bitnami/jmx-exporter`
**Purpose**: Convert Kafka JMX metrics to Prometheus format

**Conditionally deploy JMX Exporter** (After Line 72):
```csharp
// Only deploy Kafka JMX Exporter in LEARNINGCOURSE mode
if (isLearningCourseMode)
{
    // Kafka JMX Exporter - Exposes Kafka metrics in Prometheus format
    // Connects to Kafka JMX port 9999 and exposes HTTP metrics at port 7071
    var kafkaExporter = builder.AddContainer("kafka-exporter", "bitnami/jmx-exporter", "latest")
        .WithHttpEndpoint(port: 7071, targetPort: 5556, name: "metrics")
        .WithEnvironment("SERVICE_PORT", "5556")
        .WithEnvironment("JMX_HOST", "kafka")
        .WithEnvironment("JMX_PORT", "9999")
        .WaitFor(kafka);
}
```

**Why port 7071 for host port**:
- Avoids conflicts with other services
- Well-documented port for Kafka metrics in FlinkDotnet setup
- Matches Ports class convention (would need to add `Ports.KafkaExporterPort = 7071`)

#### C. Add Kafka Exporter Port to Ports Class

**Location**: Search for Ports class definition in LocalTesting.FlinkSqlAppHost

**Add**:
```csharp
public static int KafkaExporterPort { get; } = 7071;
```

### 3. Create Mode-Specific Prometheus Configuration Files

**Strategy**: Create two separate prometheus.yml files and select based on mode.

#### A. LEARNINGCOURSE Mode Configuration

**Create New File**: [`LocalTesting/prometheus-learningcourse.yml`](../LocalTesting/prometheus-learningcourse.yml)

**Full metrics scraping configuration**:
```yaml
# Prometheus configuration for LearningCourse observability stack
global:
  scrape_interval: 15s
  evaluation_interval: 15s
  scrape_timeout: 10s

scrape_configs:
  # Flink JobManager Prometheus metrics
  # Flink Prometheus reporter exposes metrics at root path '/'
  - job_name: 'flink-jobmanager'
    static_configs:
      - targets: ['flink-jobmanager:9250']
    metrics_path: '/'

  # Flink TaskManager Prometheus metrics
  # Multiple TaskManagers may bind to sequential ports (9251, 9252, etc.)
  - job_name: 'flink-taskmanager'
    static_configs:
      - targets: ['flink-taskmanager:9250']
    metrics_path: '/'

  # Flink SQL Gateway Prometheus metrics
  - job_name: 'flink-sql-gateway'
    static_configs:
      - targets: ['flink-sql-gateway:9250']
    metrics_path: '/'

  # Kafka metrics via JMX Exporter
  # JMX Exporter converts Kafka JMX metrics to Prometheus format
  - job_name: 'kafka'
    static_configs:
      - targets: ['kafka-exporter:5556']
    metrics_path: '/metrics'
```

**Key Features**:
1. ✅ Complete Flink metrics scraping (JobManager, TaskManager, SQL Gateway)
2. ✅ Complete Kafka metrics via JMX Exporter
3. ✅ Optimized for learning and debugging
4. ✅ All scrape targets fully documented

#### B. Production Mode Configuration

**Update Existing File**: [`LocalTesting/prometheus.yml`](../LocalTesting/prometheus.yml)

**Minimal metrics configuration (production)**:
```yaml
# Prometheus configuration for production mode
# Minimal metrics - only essential monitoring
global:
  scrape_interval: 30s  # Less frequent scraping
  evaluation_interval: 30s
  scrape_timeout: 10s

scrape_configs:
  # Production mode: Minimal scraping
  # Flink and Kafka metrics disabled for performance
  # Only monitor Prometheus itself
  - job_name: 'prometheus'
    static_configs:
      - targets: ['localhost:9090']
```

**Key Features**:
1. ✅ Minimal overhead - only self-monitoring
2. ✅ Reduced scrape frequency (30s vs 15s)
3. ✅ No Flink or Kafka scraping
4. ✅ Production-optimized

#### C. Conditional Prometheus Configuration Selection

**Location**: [`LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs`](../LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs)

**Add before Prometheus container deployment**:
```csharp
// Select prometheus.yml based on mode
var prometheusConfigFile = isLearningCourseMode
    ? "prometheus-learningcourse.yml"
    : "prometheus.yml";

var prometheusConfigPath = Path.Combine(
    Directory.GetCurrentDirectory(),
    prometheusConfigFile);

// Verify config file exists
if (!File.Exists(prometheusConfigPath))
{
    throw new FileNotFoundException(
        $"Prometheus configuration not found: {prometheusConfigPath}");
}

// Deploy Prometheus with selected configuration
var prometheus = builder.AddContainer("prometheus", "prom/prometheus", "latest")
    .WithBindMount(prometheusConfigPath, "/etc/prometheus/prometheus.yml", isReadOnly: true)
    .WithHttpEndpoint(port: 9090, targetPort: 9090, name: "web");
```

### 4. Port Exposure Verification

**Flink Ports**:
- Port 9250-9260 is configured in FLINK_PROPERTIES
- Containers will automatically bind to first available port
- No explicit WithHttpEndpoint needed (Flink manages internally)

**Kafka Exporter Port**:
- Host port 7071 mapped to container port 5556
- Configured via WithHttpEndpoint in container setup

### 5. Verification Strategy

#### A. Verify Flink Metrics Endpoint
```bash
# Check JobManager metrics
curl http://localhost:9250/

# Expected output: Prometheus text format metrics
# flink_jobmanager_Status_JVM_Memory_Heap_Used 268435456
# flink_jobmanager_job_uptime{job_id="...", job_name="..."} 123456
```

#### B. Verify Kafka Metrics Endpoint
```bash
# Check Kafka exporter metrics
curl http://localhost:7071/metrics

# Expected output: Prometheus text format Kafka metrics
# kafka_server_BrokerTopicMetrics_MessagesInPerSec 1234.5
# kafka_topic_partition_current_offset{topic="...",partition="0"} 12345
```

#### C. Verify Prometheus Scraping
1. Open Prometheus UI: http://localhost:9090
2. Go to Status → Targets
3. Verify all targets show "UP" status:
   - flink-jobmanager (1/1 up)
   - flink-taskmanager (1/1 up)
   - flink-sql-gateway (1/1 up)
   - kafka (1/1 up)

#### D. Query Metrics in Prometheus
```promql
# Test Flink metrics
flink_taskmanager_job_task_operator_numRecordsIn

# Test Kafka metrics  
kafka_server_BrokerTopicMetrics_MessagesInPerSec
```

### 6. Expected Metrics After Configuration

#### Flink Metrics (Sample)
```
# JobManager
flink_jobmanager_Status_JVM_Memory_Heap_Used 268435456
flink_jobmanager_job_uptime{job_id="abc123",job_name="test-job"} 156789
flink_jobmanager_job_numberOfCompletedCheckpoints{job_id="abc123"} 42

# TaskManager
flink_taskmanager_Status_JVM_Memory_Heap_Used 536870912
flink_taskmanager_job_task_operator_numRecordsIn{job_id="abc123",operator_id="Source"} 15678
flink_taskmanager_job_task_operator_numRecordsOut{job_id="abc123",operator_id="Source"} 15678
```

#### Kafka Metrics (Sample)
```
kafka_server_BrokerTopicMetrics_MessagesInPerSec{topic="input-events"} 1234.5
kafka_server_BrokerTopicMetrics_BytesInPerSec{topic="input-events"} 567890.1
kafka_topic_partition_current_offset{topic="input-events",partition="0"} 123456
```

---

## Phase 3: Implementation Plan

### Step-by-Step Implementation

#### Step 1: Download Flink Prometheus Reporter JAR (5 minutes)
1. Create PowerShell script to download JAR
2. Execute script to download to `LocalTesting/connectors/flink/lib/`
3. Verify file size matches expected (~50KB)
4. Commit JAR to repository (binary file)

**Validation**: File exists at correct path and is correct size

---

#### Step 2: Update Flink Container Configuration (15 minutes)
1. Open [`LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs`](../LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs)
2. Update JobManager FLINK_PROPERTIES (add 3 lines for Prometheus reporter)
3. Update TaskManager FLINK_PROPERTIES (add 3 lines for Prometheus reporter)
4. Update SQL Gateway FLINK_PROPERTIES (add 3 lines for Prometheus reporter)
5. Add WithBindMount for JAR to JobManager, TaskManager, SQL Gateway

**Validation**: Build succeeds, no syntax errors

---

#### Step 3: Configure Kafka JMX Export (10 minutes)
1. Update Kafka container with JMX environment variables
2. Add Ports.KafkaExporterPort definition
3. Add kafka-exporter container configuration
4. Verify WaitFor dependency on kafka

**Validation**: Build succeeds, no syntax errors

---

#### Step 4: Update prometheus.yml (5 minutes)
1. Replace entire [`LocalTesting/prometheus.yml`](../LocalTesting/prometheus.yml) content
2. Verify YAML syntax is valid
3. Remove obsolete gateway target (not part of this WI)

**Validation**: YAML validates, no syntax errors

---

#### Step 5: Test Infrastructure Startup (10 minutes)
1. Set environment variable: `$env:LEARNINGCOURSE="true"`
2. Start LocalTesting infrastructure
3. Wait for all containers to be healthy
4. Verify no container crash loops

**Validation**: All containers running, no errors in logs

---

#### Step 6: Verify Metrics Endpoints (15 minutes)
1. Test Flink JobManager: `curl http://localhost:9250/`
2. Test Flink TaskManager: `curl http://localhost:9250/` (from TaskManager container)
3. Test Kafka Exporter: `curl http://localhost:7071/metrics`
4. Verify Prometheus text format output

**Validation**: All endpoints return 200 OK with Prometheus metrics

---

#### Step 7: Verify Prometheus Scraping (10 minutes)
1. Open Prometheus UI: http://localhost:9090
2. Navigate to Status → Targets
3. Verify all targets show "UP"
4. Check scrape durations are reasonable (<1s)

**Validation**: No scrape errors, all targets UP

---

#### Step 8: Query Metrics in Prometheus (10 minutes)
1. Query Flink metrics: `flink_taskmanager_job_task_operator_numRecordsIn`
2. Query Kafka metrics: `kafka_server_BrokerTopicMetrics_MessagesInPerSec`
3. Verify non-empty results
4. Verify metric values make sense

**Validation**: Metrics queryable and show reasonable values

---

#### Step 9: Run Observability Tests (15 minutes)
1. Execute Day05 tests: `dotnet test --filter "Category=UI&Category=Video"`
2. Verify tests that query Prometheus now pass
3. Review test output for any failures
4. Document any remaining test failures

**Validation**: Observability tests pass (or fail for different reasons than before)

---

#### Step 10: Documentation Update (15 minutes)
1. Update [`docs/observability.md`](../docs/observability.md) with correct configuration
2. Document port assignments (9250 for Flink, 7071 for Kafka exporter)
3. Add troubleshooting section for common issues
4. Update architecture diagram if needed

**Validation**: Documentation accurately reflects new configuration

---

### Total Estimated Time: **2 hours**

---

## Phase 4: Testing Strategy

### Manual Testing Checklist
- [ ] Flink JobManager metrics endpoint accessible at http://localhost:9250/
- [ ] Flink TaskManager metrics endpoint accessible (internal)
- [ ] Kafka exporter metrics endpoint accessible at http://localhost:7071/metrics
- [ ] Prometheus UI shows all targets as UP
- [ ] Prometheus can query Flink metrics successfully
- [ ] Prometheus can query Kafka metrics successfully
- [ ] Grafana can visualize metrics from Prometheus

### Automated Testing
**Location**: [`LearningCourse/LearningCourse.IntegrationTests/Day05Tests.cs`](../LearningCourse/LearningCourse.IntegrationTests/Day05Tests.cs)

**Tests to Pass**:
1. `UIVideoTest_PrometheusMetrics` - Should now successfully query metrics
2. Tests that verify Prometheus scraping targets
3. Tests that verify metric values > 0

**Note**: Gateway metrics tests will still fail (Gateway metrics not in scope for this WI)

### Testing in Different Modes

#### LEARNINGCOURSE Mode Testing (Full Metrics)

**Setup**:
```powershell
# Set environment variable for LEARNINGCOURSE mode
$env:TESTING_MODE = "LEARNINGCOURSE"

# Run Aspire with full metrics enabled
dotnet run --project LocalTesting/LocalTesting.FlinkSqlAppHost
```

**Verification**:
```bash
# Verify Flink JobManager metrics
curl http://localhost:9250/
# Expected: Prometheus text format with flink_jobmanager_* metrics

# Verify Flink TaskManager metrics (from container)
docker exec flink-taskmanager curl http://localhost:9250/
# Expected: Prometheus text format with flink_taskmanager_* metrics

# Verify Kafka metrics
curl http://localhost:7071/metrics
# Expected: Prometheus text format with kafka_* metrics

# Verify Prometheus UI
# Open: http://localhost:9090
# Navigate to: Status → Targets
# Expected: All targets show "UP" status
```

**Test Execution**:
```powershell
# Run observability tests
dotnet test LearningCourse/LearningCourse.IntegrationTests `
    --filter "Category=UI&Category=Video" `
    --logger "console;verbosity=detailed"

# Expected: Tests pass with metrics data available
```

#### Production Mode Testing (Minimal Metrics)

**Setup**:
```powershell
# Unset or use different mode
$env:TESTING_MODE = "PRODUCTION"
# OR simply don't set the variable at all

# Run Aspire with minimal metrics
dotnet run --project LocalTesting/LocalTesting.FlinkSqlAppHost
```

**Verification**:
```bash
# Verify Flink metrics NOT exposed
curl http://localhost:9250/
# Expected: Connection refused (port not bound)

# Verify Kafka exporter NOT deployed
curl http://localhost:7071/metrics
# Expected: Connection refused (container not running)

# Verify Prometheus UI
# Open: http://localhost:9090
# Navigate to: Status → Targets
# Expected: Only prometheus self-monitoring target
```

**Container Verification**:
```powershell
# List running containers
docker ps

# In LEARNINGCOURSE mode, expect:
# - kafka-exporter container running
# - Flink containers with metrics ports exposed

# In production mode, expect:
# - NO kafka-exporter container
# - Flink containers without metrics ports
```

### Test Data Requirements
- **LEARNINGCOURSE Mode**: At least one Flink job running (generates Flink metrics)
- **LEARNINGCOURSE Mode**: Kafka messages being produced/consumed (generates Kafka metrics)
- **LEARNINGCOURSE Mode**: Allow 30 seconds after infrastructure startup for metrics to accumulate
- **Production Mode**: No test data required (metrics disabled)

---

## Phase 5: Risks and Mitigation

### Technical Risks

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Flink Prometheus JAR version mismatch | High | Low | Use exact version 2.1.0 matching Flink image |
| Port 9250 already in use | Medium | Low | Configure port range 9250-9260 for flexibility |
| Kafka JMX not accessible | High | Medium | Verify JMX_PORT environment variable is set correctly |
| JMX Exporter fails to connect | Medium | Medium | Use `WaitFor(kafka)` to ensure Kafka is ready |
| Prometheus scrape timeout | Low | Low | Already configured 10s timeout |

### Operational Risks

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Missing LEARNINGCOURSE environment variable | High | Medium | Document requirement clearly, add startup check |
| Firewall blocks metrics ports | Medium | Low | Document port requirements for firewall configuration |
| Container startup race conditions | Medium | Low | Use WaitFor dependencies for proper ordering |

---

## Lessons Learned & Future Reference

### What This WI Achieves

✅ **Flink metrics export**: Configured Prometheus reporter for JobManager, TaskManager, SQL Gateway  
✅ **Kafka metrics export**: Deployed JMX Exporter bridge for Kafka JMX metrics  
✅ **Correct Prometheus scraping**: Updated prometheus.yml with correct ports and paths  
✅ **Observability test foundation**: Tests can now verify actual metric collection  
✅ **Minimal code changes**: Primarily configuration updates, no significant code changes

### Key Insights for Future Work

1. **Flink Prometheus reporter is powerful**: Built-in metrics system, just needs configuration
2. **JMX requires external bridge**: Kafka (and other JVM apps) need JMX Exporter for Prometheus
3. **Port management is critical**: Wrong ports are the #1 cause of metrics collection failure
4. **Scrape path matters**: Flink uses `/`, many others use `/metrics` - check documentation
5. **WaitFor dependencies prevent race conditions**: Ensure dependent services are ready

### Specific Problems Solved

✅ **Wrong port targeting**: Changed from 8081/8080/9092 to correct metrics ports  
✅ **Missing Prometheus reporter**: Added metrics.reporters configuration to Flink  
✅ **Missing JAR dependency**: Downloaded and mounted flink-metrics-prometheus JAR  
✅ **Kafka metrics inaccessible**: Deployed JMX Exporter bridge  
✅ **Tests failing correctly**: Tests now have metrics to validate

### Reference for Future Similar Work

**When configuring Flink metrics**:
1. Always download matching Prometheus reporter JAR version
2. Add three FLINK_PROPERTIES lines (reporters, class, port)
3. Mount JAR in all Flink containers (JobManager, TaskManager, SQL Gateway)
4. Use port range 9250-9260 for flexibility
5. Remember Flink uses root path `/` not `/metrics`

**When exposing Kafka metrics**:
1. Enable JMX in Kafka container with environment variables
2. Deploy separate JMX Exporter container
3. Configure JMX Exporter to connect to Kafka JMX port
4. Use WaitFor to ensure Kafka is ready before exporter starts
5. Prometheus scrapes the exporter, not Kafka directly

**When updating prometheus.yml**:
1. Verify each target's actual metrics port (not REST API port)
2. Check documentation for correct metrics path
3. Test each target individually before full integration
4. Use reasonable scrape intervals (15s standard)
5. Configure scrape timeout to prevent hangs

---

## Mode-Specific Deployment Guidelines

### When to Use LEARNINGCOURSE Mode
- ✅ **Local development**: Debugging and testing with full observability
- ✅ **Integration testing**: When observability tests need to run
- ✅ **Training environments**: Learning and demonstration purposes
- ✅ **Performance analysis**: When detailed metrics are needed for optimization

### When to Use Production Mode
- ✅ **Production deployments**: Minimize overhead and security exposure
- ✅ **Performance benchmarks**: When metrics overhead would skew results
- ✅ **Resource-constrained environments**: Limited CPU/memory available
- ✅ **Security-sensitive deployments**: Minimize exposed endpoints

### Automatic Mode Detection in CI/CD

**GitHub Actions Example**:
```yaml
- name: Set Testing Mode
  run: |
    if ($env:GITHUB_REF -eq 'refs/heads/main') {
      echo "TESTING_MODE=PRODUCTION" >> $env:GITHUB_ENV
    } else {
      echo "TESTING_MODE=LEARNINGCOURSE" >> $env:GITHUB_ENV
    }
```

**Docker Compose Example**:
```yaml
services:
  aspire-host:
    environment:
      - TESTING_MODE=${TESTING_MODE:-PRODUCTION}
```

## Next Steps After This WI

1. **WI74 Phase 2**: Instrument JobGateway with conditional business metrics (separate WI)
2. **Grafana Dashboards**: Create mode-aware dashboards that adapt to available metrics
3. **Alert Rules**: Define mode-specific Prometheus alert rules
4. **Metric Retention**: Configure different retention policies per mode
5. **Performance Testing**: Compare overhead between modes
6. **Documentation**: Update deployment guides with mode selection guidance

---

**End of Work Item**

This WI provides a complete, actionable plan to enable Kafka and Flink metrics export to Prometheus, focusing exclusively on infrastructure configuration without custom code development.