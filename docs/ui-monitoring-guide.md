# UI Monitoring Guide for Complex Logic Stress Test

This guide provides comprehensive instructions for monitoring each step of the Complex Logic Stress Test using the various UI dashboards and tools available in the Aspire environment.

## 🎛️ Monitoring Dashboard Overview

The LocalTesting environment provides multiple monitoring interfaces to track different aspects of the stress test execution:

| Dashboard | URL | Purpose | Key Features |
|-----------|-----|---------|--------------|
| **Aspire Dashboard** | http://localhost:18888 | Overall system monitoring | Container status, logs, metrics, distributed tracing |
| **Swagger API UI** | http://localhost:5000/swagger | API testing and monitoring | Interactive API testing, response viewing, test execution |
| **Kafka UI** | http://localhost:8082 | Message queue monitoring | Topics, messages, consumer groups, throughput metrics |
| **Flink Dashboard** | http://localhost:8081 | Stream processing monitoring | Job execution, task managers, processing graphs |
| **Grafana** | http://localhost:3000 | System metrics visualization | Performance charts, system metrics, custom dashboards |
| **Temporal UI** | http://localhost:8084 | Workflow monitoring | Workflow execution, task queues, activity history |
| **Health Check API** | http://localhost:5000/health | Service health monitoring | Overall system health, service availability status |

## 📊 Step-by-Step Monitoring Instructions

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

## 🎯 Real-Time Monitoring During Full Test Execution

### Continuous Monitoring Setup

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

### Key Performance Indicators (KPIs)

| Metric | Location | Healthy Range | Action if Outside Range |
|--------|----------|---------------|-------------------------|
| **Message Throughput** | Kafka UI | 100+ msgs/sec | Check producer configuration |
| **Processing Latency** | Flink Dashboard | <5 seconds | Review Flink job parallelism |
| **Success Rate** | API Response | >85% | Investigate error logs |
| **System CPU** | Aspire Dashboard | <80% | Consider scaling resources |
| **Memory Usage** | Aspire Dashboard | <90% | Monitor for memory leaks |

## 🚨 Troubleshooting Common Issues

### Issue 1: Services Showing as Degraded

**Symptoms**: Aspire Dashboard shows yellow/red health indicators

**Solution**:
1. Check Aspire Dashboard logs for specific error messages
2. Verify Docker containers are running: `docker ps`
3. Restart individual services if needed
4. Note: API will continue working in simulation mode

### Issue 2: No Messages in Kafka Topics

**Symptoms**: Kafka UI shows empty topics after Step 4

**Solution**:
1. Check Kafka UI broker status
2. Verify topic creation permissions
3. Review API logs for Kafka connection errors
4. Restart Kafka container if needed

### Issue 3: Flink Job Not Starting

**Symptoms**: Flink Dashboard shows no jobs after Step 5

**Solution**:
1. Check Flink TaskManager status
2. Verify sufficient task slots available
3. Review job submission logs in Aspire Dashboard
4. Check API response - may be running in simulation mode

### Issue 4: Temporal Workflows Not Appearing

**Symptoms**: Temporal UI shows no workflow activity

**Solution**:
1. Verify Temporal server is running
2. Check worker service status
3. Review workflow execution logs
4. Confirm task queue configuration

## 📈 Performance Optimization Tips

1. **Resource Allocation**: Monitor Aspire Dashboard for resource bottlenecks
2. **Parallel Processing**: Adjust Flink parallelism based on available resources
3. **Batch Sizing**: Optimize batch sizes based on memory usage
4. **Network Performance**: Monitor Kafka producer/consumer lag
5. **Error Handling**: Review error logs for optimization opportunities

## 🎥 Video Tutorial Recommendations

1. **Record Screen During Test Execution**: Show real-time monitoring across all dashboards
2. **Demonstrate Navigation**: How to switch between different monitoring views
3. **Highlight Key Metrics**: Point out important indicators during each step
4. **Show Troubleshooting**: Demonstrate how to identify and resolve common issues
5. **Performance Analysis**: How to interpret metrics and optimize performance

This monitoring guide ensures comprehensive visibility into every aspect of the Complex Logic Stress Test execution, enabling effective debugging, performance optimization, and system validation.