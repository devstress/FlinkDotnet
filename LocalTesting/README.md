# LocalTesting - Complete Interactive Environment & UI Monitoring Guide

This solution provides an interactive Swagger-based API for debugging and executing Complex Logic Stress Test scenarios step by step in a local development environment, with comprehensive monitoring capabilities across multiple specialized UI dashboards.

## Overview

The LocalTesting solution provides executable API endpoints for complex logic stress testing scenarios while offering real-time visibility into system behavior through multiple specialized interfaces, allowing developers to:

- Debug each step of the stress test individually
- Monitor real-time system behavior through comprehensive dashboards
- Test the latest lag-based backpressure implementation
- Interact with a full Aspire environment including Kafka 3-broker cluster, Flink, OpenTelemetry, and more
- Use step-by-step monitoring instructions with detailed UI guidance

## Architecture

### Infrastructure Components

- **3 Kafka Brokers (KRaft)**: Full KRaft cluster for production-like testing
- **Apache Flink Cluster**: JobManager + 3 TaskManagers with 10 slots each (30 total slots)
- **Flink SQL Gateway**: Interactive SQL query interface
- **Redis**: Caching and state management
- **Temporal Server + UI**: Durable workflow execution for long-running processes
- **OpenTelemetry Collector**: Distributed tracing and metrics collection
- **Prometheus**: Metrics storage and querying
- **Grafana**: Visualization dashboards
- **Kafka UI**: Cluster management and monitoring

### UI Components Overview

The LocalTesting environment includes the following interactive monitoring interfaces:

1. **Swagger API Interface** (Port 5000) - Interactive REST API for executing Complex Logic Stress Test scenarios
2. **Aspire Dashboard** (Port 18888) - System overview & container health monitoring
3. **Kafka UI** (Port 8082) - Web-based management interface for the 3-broker Kafka cluster
4. **Apache Flink Web UI** (Port 8081) - JobManager dashboard for monitoring Flink jobs and streaming operations
5. **Flink SQL Gateway** (Port 8083) - Interactive SQL interface for querying Flink streams
6. **Redis Interface** - State management and caching monitoring
7. **Grafana Dashboards** (Port 3000) - Comprehensive observability dashboards
8. **Prometheus Metrics** (Port 9090) - Metrics collection and querying interface
9. **Temporal UI** (Port 8084) - Workflow execution monitoring
10. **OpenTelemetry Collector** (Ports 8888, 8889) - Distributed tracing and telemetry collection

### Technology Decision Guide

For guidance on when to use Flink vs Temporal for different processing requirements, see our comprehensive [Flink vs Temporal Decision Guide](../docs/flink-vs-temporal-decision-guide.md).

### API Endpoints

The Web API exposes the following step-by-step endpoints:

#### Step 1: Environment Setup
- `POST /api/ComplexLogicStressTest/step1/setup-environment`
- Validates that all Aspire services are running

#### Step 2: Security Token Configuration  
- `POST /api/ComplexLogicStressTest/step2/configure-security-tokens`
- `GET /api/ComplexLogicStressTest/step2/token-status`
- Configures token renewal every N messages (default: 10,000)

#### Step 3: Backpressure Configuration
- `POST /api/ComplexLogicStressTest/step3/configure-backpressure`
- `GET /api/ComplexLogicStressTest/step3/backpressure-status`
- Sets up lag-based rate limiter that **stops token bucket refilling when consumer lag exceeds threshold**

#### Step 4: Message Production
- `POST /api/ComplexLogicStressTest/step4/produce-messages`
- Generates messages with unique correlation IDs and sends to Kafka

#### Step 5: Flink Job Management
- `POST /api/ComplexLogicStressTest/step5/start-flink-job`
- `GET /api/ComplexLogicStressTest/step5/flink-jobs`
- `GET /api/ComplexLogicStressTest/step5/flink-job/{jobId}`
- Starts and monitors Apache Flink streaming jobs

#### Step 6: Batch Processing
- `POST /api/ComplexLogicStressTest/step6/process-batches`
- Processes messages in batches through HTTP endpoint with correlation tracking

#### Step 7: Message Verification
- `POST /api/ComplexLogicStressTest/step7/verify-messages`
- Verifies correlation ID matching and displays top/last processed messages

#### Full Automation
- `POST /api/ComplexLogicStressTest/run-full-stress-test`
- `GET /api/ComplexLogicStressTest/test-status/{testId}`
- `GET /api/ComplexLogicStressTest/test-status`
- Executes complete stress test or monitors running tests

## Latest Backpressure Implementation

The system now includes **LagBasedRateLimiter** which implements the "stops refill bucket when reaching threshold" behavior:

### How It Works

1. **Token Bucket**: Maintains a bucket of tokens that refills at a configured rate
2. **Lag Monitoring**: Continuously monitors Kafka consumer group lag
3. **Threshold Control**: When lag > threshold (default: 5 seconds):
   - **Stops token bucket refilling** (key feature)
   - Existing tokens can still be consumed
   - Creates natural backpressure
4. **Recovery**: When lag ≤ threshold:
   - **Resumes normal token bucket refilling**
   - System returns to normal operation

### Configuration Example

```json
{
  "consumerGroup": "stress-test-group",
  "lagThresholdSeconds": 5.0,
  "rateLimit": 1000.0,
  "burstCapacity": 5000.0
}
```

## Getting Started

### Prerequisites

- **.NET 9.0 SDK** (Required for Aspire workload and local testing)
- **Docker Desktop** (Required for container orchestration)
- **Aspire Workload** (`dotnet workload install aspire`)
- **At least 16GB RAM** (for all containers - 3 Kafka brokers, 3 TaskManagers, etc.)

**⚠️ Important**: Local testing GitHub workflow requires .NET 9.0.303 or later. Verify your local environment matches CI requirements:
```bash
dotnet --version  # Must return 9.0.x
dotnet workload list  # Must show aspire workload installed
```

### Environment Variables

Before running the Aspire environment, set these required environment variables:

```bash
export ASPIRE_ALLOW_UNSECURED_TRANSPORT=true
export DOTNET_DASHBOARD_OTLP_ENDPOINT_URL=http://localhost:4323
export DOTNET_DASHBOARD_OTLP_HTTP_ENDPOINT_URL=http://localhost:4324
export ASPIRE_DASHBOARD_URL=http://localhost:18888
export ASPNETCORE_URLS=http://localhost:18888
```

### Running the Environment

1. **Set Environment Variables**:
   ```bash
   # Set required Aspire environment variables
   export ASPIRE_ALLOW_UNSECURED_TRANSPORT=true
   export DOTNET_DASHBOARD_OTLP_ENDPOINT_URL=http://localhost:4323
   export DOTNET_DASHBOARD_OTLP_HTTP_ENDPOINT_URL=http://localhost:4324
   export ASPIRE_DASHBOARD_URL=http://localhost:18888
   export ASPNETCORE_URLS=http://localhost:18888
   ```

2. **Start Aspire Host**:
   ```bash
   cd LocalTesting/LocalTesting.AppHost
   dotnet run
   ```

2. **Access Dashboards**:
   - **Aspire Dashboard**: http://localhost:18888 (login required - see startup output for token)
   - **Swagger API**: http://localhost:5000
   - **Kafka UI**: http://localhost:8082
   - **Flink UI**: http://localhost:8081
   - **Grafana**: http://localhost:3000 (admin/admin)
   - **Temporal UI**: http://localhost:8084

### Quick Reference - Essential Monitoring URLs

| Dashboard | URL | Primary Use |
|-----------|-----|-------------|
| 🎛️ **Aspire Dashboard** | http://localhost:18888 | System overview & container health |
| 🚀 **API & Swagger** | http://localhost:5000/swagger | Interactive API testing |
| 📝 **Kafka UI** | http://localhost:8082 | Message flow & topics |
| ⚡ **Flink Dashboard** | http://localhost:8081 | Stream processing jobs |
| 📈 **Grafana** | http://localhost:3000 | Performance metrics |
| 🔄 **Temporal UI** | http://localhost:8084 | Workflow execution |
| ❤️ **Health Check** | http://localhost:5000/health | Service status |

3. **Execute Tests**:
   - Open Swagger UI at http://localhost:5000
   - Follow the step-by-step endpoints or use the full automation

### Example: Running a Complete Stress Test

```bash
# Using curl to start a full stress test
curl -X POST "http://localhost:5000/api/ComplexLogicStressTest/run-full-stress-test" \
  -H "Content-Type: application/json" \
  -d '{
    "messageCount": 100000,
    "batchSize": 100,
    "tokenRenewalInterval": 10000,
    "consumerGroup": "stress-test-group",
    "lagThresholdSeconds": 5.0,
    "rateLimit": 1000.0,
    "burstCapacity": 5000.0
  }'

# Check test status
curl "http://localhost:5000/api/ComplexLogicStressTest/test-status/{testId}"
```

## 📊 Step-by-Step Monitoring Instructions for Complex Logic Stress Tests

This section provides detailed monitoring instructions for each step of the Complex Logic Stress Test execution using the various UI dashboards and tools available in the Aspire environment.

### Step 1: Environment Setup Monitoring

**Primary Dashboard**: Aspire Dashboard (http://localhost:18888)

**What to Monitor**:
1. **Container Status**: All service containers should be running
2. **Service Health**: Health check status for each service
3. **Resource Usage**: CPU and memory consumption

**How to Monitor**:
1. Open Aspire Dashboard
2. Navigate to "Resources" tab
3. Verify all containers show "Running" status
4. Check the health indicators (green = healthy, yellow/red = issues)
5. Monitor resource consumption graphs

**Expected Results**:
- ✅ Status: "Ready" (API is functional)
- 📊 Service Health: Mix of healthy/degraded services is acceptable
- 🐳 Containers: 6-8 containers should be running
- 💾 Resources: Reasonable CPU/memory usage

**Screenshots to Take**:
- Overview page showing all running services
- Resource usage charts
- Health status indicators

---

### Step 2: Security Token Configuration Monitoring

**Primary Dashboard**: Swagger UI (http://localhost:5000/swagger) + Aspire Logs

**What to Monitor**:
1. **Token Service Initialization**: Successful configuration response
2. **Token Renewal**: Automatic token renewal process
3. **Service Logs**: Token manager activity logs

**How to Monitor**:
1. Open Swagger UI and navigate to Step 2 endpoints
2. Execute POST `/api/ComplexLogicStressTest/step2/configure-security-tokens`
3. Monitor response for successful configuration
4. Use GET `/api/ComplexLogicStressTest/step2/token-status` to check token info
5. Check Aspire Dashboard logs for token renewal activity

**Expected Results**:
- ✅ Status: "Configured"
- 🔑 Token Info: Valid token with renewal interval
- 📝 Logs: Token initialization and renewal messages

**Screenshots to Take**:
- Swagger UI showing successful step 2 execution
- Token status response with renewal information
- Aspire logs showing token service activity

---

### Step 3: Backpressure Configuration Monitoring

**Primary Dashboard**: Kafka UI (http://localhost:8082) + Swagger UI

**What to Monitor**:
1. **Consumer Group Creation**: New consumer group appears in Kafka
2. **Lag Monitoring**: Consumer lag tracking initialization
3. **Rate Limiter**: Backpressure service configuration

**How to Monitor**:
1. Open Kafka UI and navigate to "Consumer Groups" section
2. Execute Step 3 in Swagger UI
3. Verify consumer group appears in Kafka UI
4. Check backpressure status endpoint
5. Monitor lag metrics (should start at 0)

**Expected Results**:
- ✅ Status: "Configured"
- 👥 Consumer Group: "stress-test-group" appears in Kafka UI
- ⚡ Backpressure: Lag threshold and rate limit configured
- 📊 Metrics: Initial lag = 0, rate limiter ready

**Screenshots to Take**:
- Kafka UI consumer groups page
- Swagger UI backpressure configuration response
- Backpressure status endpoint response

---

### Step 4: Message Production Monitoring

**Primary Dashboard**: Kafka UI (http://localhost:8082) + Grafana (http://localhost:3000)

**What to Monitor**:
1. **Topic Creation**: "complex-input" topic appears
2. **Message Count**: Topic message count increases
3. **Throughput**: Messages per second rate
4. **Producer Performance**: Latency and batch metrics

**How to Monitor**:
1. Open Kafka UI before starting Step 4
2. Navigate to "Topics" section
3. Execute Step 4 in Swagger UI (or use test script)
4. Watch "complex-input" topic message count increase in real-time
5. Monitor throughput metrics in Kafka UI and Grafana
6. Check producer lag and batch statistics

**Expected Results**:
- ✅ Status: "Messages_Produced"
- 📝 Topic: "complex-input" with increasing message count
- 🚀 Throughput: 100+ messages/second (varies by system)
- 🏷️ Correlation IDs: Unique correlation IDs for each message
- 📊 Metrics: Successful production with minimal errors

**Screenshots to Take**:
- Kafka UI topics page showing message count growth
- Topic details with message samples
- Grafana charts showing throughput metrics
- Swagger UI response with production statistics

---

### Step 5: Flink Job Management Monitoring

**Primary Dashboard**: Flink Dashboard (http://localhost:8081)

**What to Monitor**:
1. **Job Submission**: New Flink job appears in dashboard
2. **Job Status**: Job transitions to "RUNNING" state
3. **Task Managers**: Processing tasks and parallelism
4. **Checkpointing**: Checkpoint creation and success rate

**How to Monitor**:
1. Open Flink Dashboard before starting Step 5
2. Navigate to "Jobs" section
3. Execute Step 5 in Swagger UI
4. Watch for new job to appear
5. Click on job to view execution graph
6. Monitor task manager activity and checkpoint status
7. Check processing rates and backpressure indicators

**Expected Results**:
- ✅ Status: "Started" (or "Started_Simulation" if infrastructure degraded)
- 🔄 Job Status: "RUNNING" in Flink Dashboard
- 📊 Processing: Active task managers processing data
- ✨ Checkpoints: Regular checkpoint creation
- 🌊 Data Flow: Data flowing through processing pipeline

**Screenshots to Take**:
- Flink jobs overview page
- Job execution graph showing data flow
- Task manager details and metrics
- Checkpoint history and success rate

---

### Step 6: Batch Processing Monitoring

**Primary Dashboard**: Temporal UI (http://localhost:8084) + Swagger UI

**What to Monitor**:
1. **Workflow Execution**: Batch processing workflows
2. **Task Queue Activity**: Temporal task execution
3. **Processing Progress**: Batch completion status
4. **Security Token Renewals**: Token rotation during processing

**How to Monitor**:
1. Open Temporal UI and navigate to workflow section
2. Execute Step 6 in Swagger UI
3. Watch for new workflows to appear in Temporal UI
4. Monitor workflow execution progress
5. Check task queue activity and worker status
6. Verify batch processing completion in API response

**Expected Results**:
- ✅ Status: "Completed" (or "Completed_Simulation")
- 🔄 Workflows: Active batch processing workflows in Temporal
- 📦 Batches: Multiple batches processed successfully
- 🔑 Tokens: Token renewals as needed during processing
- 📊 Progress: Incremental batch completion

**Screenshots to Take**:
- Temporal UI workflows page
- Workflow execution details and history
- Task queue activity and worker status
- Swagger UI batch processing response

---

### Step 7: Message Verification Monitoring

**Primary Dashboard**: Swagger UI + Kafka UI (output topic)

**What to Monitor**:
1. **Output Topic**: "complex-output" topic creation and messages
2. **Correlation Tracking**: Message correlation ID matching
3. **Success Rate**: Percentage of successfully processed messages
4. **Data Integrity**: Top and last message samples

**How to Monitor**:
1. Check Kafka UI for "complex-output" topic
2. Execute Step 7 in Swagger UI
3. Review verification response with success rate
4. Compare input vs output message counts
5. Verify correlation ID consistency
6. Check top and last message samples for data integrity

**Expected Results**:
- ✅ Status: "Completed" (or "Completed_Simulation")
- 📊 Success Rate: 85%+ message processing success
- 🔗 Correlation IDs: Matching IDs between input and output
- 📝 Messages: Sample messages showing proper processing
- ✅ Verification: High data integrity and processing accuracy

**Screenshots to Take**:
- Kafka UI output topic with processed messages
- Swagger UI verification response with success metrics
- Message samples showing correlation ID tracking
- Comparison of input vs output message counts

---

### 🎯 Real-Time Monitoring During Full Test Execution

#### Continuous Monitoring Setup

1. **Open Multiple Browser Tabs**:
   - Tab 1: Aspire Dashboard (overall system health)
   - Tab 2: Kafka UI (message flow monitoring)
   - Tab 3: Flink Dashboard (stream processing)
   - Tab 4: Swagger UI (API responses)
   - Tab 5: Grafana (performance metrics)

2. **Watch Key Metrics**:
   - **Message Throughput**: Kafka UI topics section
   - **Processing Latency**: Flink Dashboard metrics
   - **System Resources**: Aspire Dashboard resources
   - **Error Rates**: All dashboards for red indicators
   - **API Responses**: Swagger UI for step completion

3. **Alert Indicators**:
   - 🟢 Green: Normal operation
   - 🟡 Yellow: Warning/degraded performance
   - 🔴 Red: Error requiring attention

#### Key Performance Indicators (KPIs)

| Metric | Location | Healthy Range | Action if Outside Range |
|--------|----------|---------------|-------------------------|
| **Message Throughput** | Kafka UI | 100+ msgs/sec | Check producer configuration |
| **Processing Latency** | Flink Dashboard | <5 seconds | Review Flink job parallelism |
| **Success Rate** | API Response | >85% | Investigate error logs |
| **System CPU** | Aspire Dashboard | <80% | Consider scaling resources |
| **Memory Usage** | Aspire Dashboard | <90% | Monitor for memory leaks |

#### 🚨 Quick Health Indicators

| Color | Status | Action |
|-------|--------|--------|
| 🟢 Green | Healthy | Continue monitoring |
| 🟡 Yellow | Degraded | Check logs, may continue |
| 🔴 Red | Error | Investigate immediately |

#### 📸 Screenshot Checklist

- [ ] Aspire Dashboard showing all services
- [ ] Kafka UI with topic message counts
- [ ] Flink job execution graph
- [ ] Swagger UI successful API responses
- [ ] Grafana performance metrics
- [ ] Temporal workflow execution

💡 **Pro Tip**: Open all monitoring dashboards in separate browser tabs before starting the stress test for real-time visibility across all systems.

## 🖥️ Detailed UI Components Guide

### 1. Swagger API Interface

**Access URL**: http://localhost:5000

The primary interface for executing and debugging Complex Logic Stress Test scenarios.

**Key Features:**
- **Step-by-Step Execution**: Execute each test phase individually
- **Interactive Configuration**: Adjust parameters dynamically 
- **Real-Time Monitoring**: Monitor test progress and results
- **Comprehensive Documentation**: Built-in API documentation

**Available Endpoints:**

#### Environment Setup (Step 1)
- `POST /api/ComplexLogicStressTest/step1/setup-environment`
- Validates all Aspire services are running and healthy

#### Security Token Management (Step 2)
- `POST /api/ComplexLogicStressTest/step2/configure-security-tokens`
- `GET /api/ComplexLogicStressTest/step2/token-status`
- Configures and monitors security token renewal (every 10K messages)

#### Backpressure Configuration (Step 3)
- `POST /api/ComplexLogicStressTest/step3/configure-backpressure`
- `GET /api/ComplexLogicStressTest/step3/backpressure-status`
- Sets up lag-based rate limiting that **stops token bucket refilling when consumer lag exceeds threshold**

#### Message Production (Step 4)
- `POST /api/ComplexLogicStressTest/step4/produce-messages`
- Generates messages with unique correlation IDs

#### Flink Job Management (Step 5)
- `POST /api/ComplexLogicStressTest/step5/start-flink-job`
- `GET /api/ComplexLogicStressTest/step5/flink-jobs`
- `GET /api/ComplexLogicStressTest/step5/flink-job/{jobId}`
- Manages Apache Flink streaming jobs

#### Batch Processing (Step 6)
- `POST /api/ComplexLogicStressTest/step6/process-batches`
- Processes messages in batches through HTTP endpoints

#### Message Verification (Step 7)
- `POST /api/ComplexLogicStressTest/step7/verify-messages`
- Verifies correlation ID matching and message processing

#### Full Automation
- `POST /api/ComplexLogicStressTest/run-full-stress-test`
- `GET /api/ComplexLogicStressTest/test-status/{testId}`
- Complete stress test execution and monitoring

### 2. Kafka UI - Cluster Management Interface

**Access URL**: http://localhost:8082

Professional web interface for managing and monitoring the 3-broker Kafka cluster.

**Key Features:**
- **3-Broker Cluster Monitoring**: Real-time status of all Kafka brokers
- **Topic Management**: Create, configure, and monitor Kafka topics
- **Consumer Group Tracking**: Monitor consumer lag and processing progress
- **Message Flow Visualization**: Real-time message throughput metrics
- **Partition Distribution**: View partition assignments across brokers

**Monitoring Capabilities:**
- **Cluster Overview**: Broker health and performance metrics, cluster-wide throughput and latency statistics
- **Topic Details**: Message count and size per topic, partition-level metrics and leader distribution
- **Consumer Group Monitoring**: **Consumer Lag Monitoring** (critical for backpressure evaluation), consumer group rebalancing status
- **Message Browser**: Browse messages in real-time, view message headers and correlation IDs

### 3. Apache Flink Web UI - Streaming Job Management

**Access URL**: http://localhost:8081

Comprehensive dashboard for monitoring Flink cluster and streaming jobs.

**Key Features:**
- **JobManager Monitoring**: Central coordination and job management
- **TaskManager Status**: 3 TaskManagers with 10 slots each (30 total slots)
- **Job Execution Monitoring**: Real-time job status and performance
- **Checkpointing Status**: Exactly-once processing guarantees
- **Backpressure Detection**: Visual backpressure indicators

**Monitoring Sections:**
- **Cluster Overview**: TaskManager resource utilization, available slots and parallelism configuration
- **Job Details**: Job execution graph and operator chain, throughput metrics per operator
- **TaskManager Metrics**: CPU and memory utilization per TaskManager, network buffer usage
- **Backpressure Monitoring**: **Visual backpressure indicators** per operator, processing rate and buffer utilization

### 4. Flink SQL Gateway - Interactive SQL Interface

**Access URL**: http://localhost:8083

Interactive SQL interface for querying Flink streams and managing SQL-based operations.

**Key Features:**
- **Interactive SQL Queries**: Query streaming data in real-time
- **Table Management**: Create and manage Flink tables
- **Streaming Analytics**: Perform real-time analytics on message streams
- **Job Submission**: Submit SQL-based Flink jobs

**Usage Examples:**

#### Stream Querying
```sql
SELECT correlation_id, message_count, processing_time
FROM complex_logic_stream
WHERE lag_threshold > 5000;
```

#### Real-Time Analytics
```sql
SELECT COUNT(*) as processed_messages, 
       AVG(processing_time) as avg_processing_time
FROM message_stream
GROUP BY TUMBLE(processing_timestamp, INTERVAL '1' MINUTE);
```

### 5. Grafana Observability Dashboards

**Access URL**: http://localhost:3000 (admin/admin)

Professional observability platform with custom dashboards for system monitoring.

**Key Features:**
- **System-Wide Metrics**: CPU, memory, network across all components
- **Custom Dashboards**: Specialized views for different system aspects
- **Real-Time Alerting**: Configurable alerts for system anomalies
- **Historical Analysis**: Long-term trend analysis and capacity planning

**Available Dashboards:**
- **System Metrics Dashboard**: Docker container resource utilization, host system performance metrics
- **Kafka Monitoring Dashboard**: **Consumer Lag Trends** (essential for backpressure monitoring), message throughput per topic
- **Flink Performance Dashboard**: Job processing rates and latency, checkpointing performance
- **Backpressure Analysis Dashboard**: **Lag-based rate limiter status**, token bucket refill rates and utilization

### 6. Prometheus Metrics Interface

**Access URL**: http://localhost:9090

Advanced metrics collection and querying platform for system monitoring.

**Key Features:**
- **PromQL Queries**: Advanced metric querying language
- **Target Monitoring**: Service discovery and health monitoring
- **Metric Storage**: Time-series data storage and retrieval
- **Alert Rules**: Configurable alerting based on metric thresholds

**Useful Queries:**

#### Consumer Lag Monitoring
```promql
kafka_consumer_lag_ms{consumer_group="stress-test-group"}
```

#### Backpressure Rate Limiting
```promql
rate_limiter_tokens_available{limiter="lag-based"}
```

#### Flink Processing Rate
```promql
flink_taskmanager_job_task_operator_numRecordsInPerSecond
```

### 7. Temporal UI - Workflow Execution Monitoring

**Access URL**: http://localhost:8084

Web interface for monitoring workflow executions and task queues.

**Key Features:**
- **Workflow Monitoring**: Real-time workflow execution status
- **Task Queue Management**: Monitor task queue activity and worker status
- **Workflow History**: Complete execution history and debugging information
- **Worker Health**: Monitor worker service status and performance

### 8. OpenTelemetry Collector Interfaces

**Metrics Port**: http://localhost:8888
**Prometheus Export**: http://localhost:8889

Distributed tracing and telemetry collection for comprehensive observability.

**Key Features:**
- **Distributed Tracing**: End-to-end request tracing across all components
- **Metrics Collection**: Standardized metric collection and export
- **Span Analysis**: Detailed performance analysis of individual operations
- **Service Mesh Visibility**: Inter-service communication monitoring

## Monitoring Workflows

### 1. Basic System Health Check

1. **Start with Swagger API** (http://localhost:5000)
   - Execute Step 1: Environment Setup
   - Verify all services are healthy

2. **Check Kafka UI** (http://localhost:8082)
   - Verify 3 brokers are running
   - Check topic creation and replication

3. **Validate Flink Cluster** (http://localhost:8081)
   - Confirm JobManager and 3 TaskManagers are active
   - Verify 30 available task slots

### 2. Stress Test Execution Monitoring

1. **Initiate Test** via Swagger API
   - Configure backpressure settings (Step 3)
   - Start message production (Step 4)
   - Launch Flink job (Step 5)

2. **Monitor Progress** across multiple UIs:
   - **Kafka UI**: Consumer lag and message flow
   - **Flink UI**: Job performance and backpressure indicators
   - **Grafana**: System-wide metrics and trends

3. **Analyze Results**:
   - **Prometheus**: Query specific metrics
   - **Grafana**: Trend analysis and correlation identification
   - **Swagger API**: Verify message correlation and test completion

### 3. Backpressure Analysis Workflow

The LocalTesting environment provides comprehensive backpressure monitoring through multiple interfaces:

#### Step 1: Configure Lag-Based Backpressure
```bash
curl -X POST "http://localhost:5000/api/ComplexLogicStressTest/step3/configure-backpressure" \
  -H "Content-Type: application/json" \
  -d '{
    "consumerGroup": "stress-test-group",
    "lagThresholdSeconds": 5.0,
    "rateLimit": 1000.0,
    "burstCapacity": 5000.0
  }'
```

#### Step 2: Monitor Through Kafka UI
- Navigate to **Consumer Groups** section
- Watch **lag progression** for `stress-test-group`
- Observe when lag exceeds 5-second threshold

#### Step 3: Observe Rate Limiting Behavior
- **Grafana Dashboard**: Monitor token bucket refill status
- **Prometheus Queries**: Track rate limiter metrics
- **Swagger API**: Check backpressure status endpoint

#### Step 4: Validate Automatic Recovery
- Watch lag reduction in Kafka UI
- Confirm token bucket refilling resumes in Grafana
- Verify throughput recovery in Flink UI

## Troubleshooting

### Common Issues and Solutions

#### Services Not Starting
**Symptoms**: Missing UIs or connection errors

**Solutions**:
1. Check Docker Desktop memory allocation (minimum 16GB)
2. Verify port availability (5000, 8080, 8081, 3000, 9090)
3. Wait 2-3 minutes for full container initialization
4. Check Aspire dashboard for service status

#### Issue 1: Services Showing as Degraded
**Symptoms**: Aspire Dashboard shows yellow/red health indicators

**Solution**:
1. Check Aspire Dashboard logs for specific error messages
2. Verify Docker containers are running: `docker ps`
3. Restart individual services if needed
4. Note: API will continue working in simulation mode

#### Issue 2: No Messages in Kafka Topics
**Symptoms**: Kafka UI shows empty topics after Step 4

**Solution**:
1. Check Kafka UI broker status
2. Verify topic creation permissions
3. Review API logs for Kafka connection errors
4. Restart Kafka container if needed

#### Issue 3: Flink Job Not Starting
**Symptoms**: Flink Dashboard shows no jobs after Step 5

**Solution**:
1. Check Flink TaskManager status
2. Verify sufficient task slots available
3. Review job submission logs in Aspire Dashboard
4. Check API response - may be running in simulation mode

#### Issue 4: Temporal Workflows Not Appearing
**Symptoms**: Temporal UI shows no workflow activity

**Solution**:
1. Verify Temporal server is running
2. Check worker service status
3. Review workflow execution logs
4. Confirm task queue configuration

#### Quick Troubleshooting Commands
```bash
# Check container status
docker ps

# Check Docker resource usage
docker stats

# Check specific container logs
docker logs <container_name>

# Restart Aspire environment
cd LocalTesting/LocalTesting.AppHost
dotnet run
```

#### High Consumer Lag
**Symptoms**: Persistent lag growth in Kafka UI

**Solutions**:
1. **Check Backpressure Configuration**:
   - Verify lag threshold settings via Swagger API
   - Confirm rate limiter is responding to lag increases

2. **Monitor Flink Performance**:
   - Check TaskManager resource utilization
   - Verify checkpoint success rates
   - Look for backpressure indicators in operator chain

3. **Analyze System Resources**:
   - Review Grafana system metrics dashboards
   - Check Docker container resource limits
   - Monitor host system performance

#### Missing Metrics in Grafana
**Symptoms**: Empty or incomplete dashboard panels

**Solutions**:
1. Verify Prometheus is collecting metrics (http://localhost:9090)
2. Check OpenTelemetry Collector status (http://localhost:8888)
3. Restart containers through Aspire dashboard
4. Verify metric export configuration in service logs

#### Flink Job Failures
**Symptoms**: Jobs failing or not processing messages

**Solutions**:
1. **Check Flink UI Job Details**:
   - Review execution graph for failed operators
   - Check checkpoint failure reasons
   - Verify TaskManager connectivity

2. **Validate Kafka Connectivity**:
   - Confirm Kafka brokers are accessible from Flink containers
   - Check topic configuration and permissions
   - Verify network connectivity between containers

### Performance Optimization

#### Resource Allocation Guidelines

**Memory Requirements**
- **Kafka Brokers**: 8GB heap each (24GB total)
- **Flink TaskManagers**: 8GB process memory each (24GB total)
- **Other Services**: ~8GB combined
- **Total Recommended**: 64GB system RAM

**CPU Requirements**
- **Minimum**: 8 cores
- **Recommended**: 16+ cores for optimal performance
- **High-Volume Testing**: 32+ cores

#### Scaling Recommendations

**For Higher Message Volumes**
1. **Increase Kafka Partitions**:
   - Modify topic configuration in Kafka UI
   - Adjust partition count for parallel processing

2. **Scale Flink Parallelism**:
   - Add more TaskManager containers
   - Increase slots per TaskManager
   - Adjust job parallelism settings

3. **Optimize Backpressure Settings**:
   - Fine-tune lag thresholds based on system capacity
   - Adjust rate limits for optimal throughput
   - Monitor token bucket utilization

## Local Testing & GitHub Workflow Requirements

To ensure the LocalTesting GitHub workflow passes in your local environment:

### Environment Setup
1. **Install .NET 9.0 SDK** (9.0.303 or later)
2. **Install Aspire workload**: `dotnet workload install aspire`
3. **Start Docker Desktop** with adequate resources (16GB+ RAM)
4. **Verify prerequisites**:
   ```bash
   dotnet --version  # Should return 9.0.x
   dotnet workload list  # Should show aspire
   docker info  # Should show Docker running
   ```

### Local Testing Commands
```bash
# Navigate to AppHost directory
cd LocalTesting/LocalTesting.AppHost

# Start Aspire environment
dotnet run

# Run the LocalTesting GitHub workflow simulation
./test-aspire-localtesting.ps1 -MessageCount 1000
```

### Expected Results
- **Step 1**: Status "Ready" (API functional with infrastructure health details)
- **Step 2-7**: All steps should pass with proper 3-broker, 3-TaskManager configuration
- **Infrastructure**: 3 Kafka brokers + 3 TaskManagers (30 total slots) + Temporal + monitoring

The workflow validates business logic even if some infrastructure services are degraded, but optimal performance requires all services healthy.

## Related Documentation

- [Complex Logic Stress Tests Documentation](../docs/wiki/Complex-Logic-Stress-Tests.md)
- [Rate Limiting Implementation Tutorial](../docs/wiki/Rate-Limiting-Implementation-Tutorial.md)
- [Aspire Local Development Setup](../docs/wiki/Aspire-Local-Development-Setup.md)
- [Flink vs Temporal Decision Guide](../docs/flink-vs-temporal-decision-guide.md)

## Development Notes

### Key Improvements Over BDD Tests

1. **Interactive Debugging**: Step through each phase manually
2. **Real-Time Monitoring**: Live dashboards and metrics
3. **Dynamic Configuration**: Change parameters without restarting tests
4. **Production-Like Environment**: 3-broker Kafka cluster with full observability
5. **Latest Backpressure**: Includes the newest lag-based rate limiting implementation

### Extending the API

To add new test scenarios:

1. Add endpoints to `ComplexLogicStressTestController`
2. Implement business logic in the service classes
3. Update Swagger documentation with `[SwaggerOperation]` attributes
4. Test through the interactive UI

## Next Steps

1. **Explore Interactive APIs**: Start with the Swagger interface to understand step-by-step execution
2. **Monitor System Behavior**: Use multiple UIs simultaneously to gain comprehensive insights
3. **Experiment with Backpressure**: Test different lag thresholds and rate limits
4. **Analyze Performance**: Use Grafana and Prometheus for detailed performance analysis
5. **Scale Testing**: Gradually increase message volumes and monitor system response

This comprehensive guide provides everything needed to effectively use the LocalTesting interactive environment for debugging and validating Complex Logic Stress Test scenarios with real-time monitoring and analysis capabilities.