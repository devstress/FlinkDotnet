# 🎛️ Quick UI Monitoring Reference Card

## Essential Monitoring URLs

| Dashboard | URL | Primary Use |
|-----------|-----|-------------|
| 🎛️ **Aspire Dashboard** | http://localhost:18888 | System overview & container health |
| 🚀 **API & Swagger** | http://localhost:5000/swagger | Interactive API testing |
| 📝 **Kafka UI** | http://localhost:8082 | Message flow & topics |
| ⚡ **Flink Dashboard** | http://localhost:8081 | Stream processing jobs |
| 📈 **Grafana** | http://localhost:3000 | Performance metrics |
| 🔄 **Temporal UI** | http://localhost:8084 | Workflow execution |
| ❤️ **Health Check** | http://localhost:5000/health | Service status |

## Step-by-Step Monitoring Quick Guide

### Step 1: Environment Setup
- **Monitor**: Aspire Dashboard → Resources tab
- **Look for**: All containers "Running", service health indicators
- **Expected**: "Ready" status with mixed service health (acceptable)

### Step 2: Security Tokens  
- **Monitor**: Swagger UI → Step 2 endpoints
- **Look for**: "Configured" status, token renewal info
- **Expected**: Valid token with specified renewal interval

### Step 3: Backpressure
- **Monitor**: Kafka UI → Consumer Groups + Swagger response
- **Look for**: "stress-test-group" consumer group created
- **Expected**: "Configured" status, lag threshold set

### Step 4: Message Production
- **Monitor**: Kafka UI → Topics → "complex-input"
- **Look for**: Growing message count, throughput metrics
- **Expected**: "Messages_Produced" status, 100+ msgs/sec

### Step 5: Flink Job
- **Monitor**: Flink Dashboard → Jobs section
- **Look for**: New job in "RUNNING" state, task activity
- **Expected**: "Started" status, active processing graph

### Step 6: Batch Processing
- **Monitor**: Temporal UI → Workflows + API response
- **Look for**: Active workflows, batch completion progress
- **Expected**: "Completed" status, multiple batches processed

### Step 7: Verification
- **Monitor**: Kafka UI → "complex-output" topic + API response
- **Look for**: Output messages, success rate metrics
- **Expected**: "Completed" status, 85%+ success rate

## 🚨 Quick Health Indicators

| Color | Status | Action |
|-------|--------|--------|
| 🟢 Green | Healthy | Continue monitoring |
| 🟡 Yellow | Degraded | Check logs, may continue |
| 🔴 Red | Error | Investigate immediately |

## 📊 Key Metrics to Watch

- **Throughput**: Kafka UI topics (target: 100+ msgs/sec)
- **Latency**: Flink Dashboard (target: <5 seconds)
- **Success Rate**: API responses (target: >85%)
- **CPU Usage**: Aspire Dashboard (target: <80%)
- **Memory**: Aspire Dashboard (target: <90%)

## 🎯 Multi-Tab Monitoring Setup

1. **Tab 1**: Aspire Dashboard (system overview)
2. **Tab 2**: Kafka UI (message flow)
3. **Tab 3**: Flink Dashboard (processing)
4. **Tab 4**: Swagger UI (API responses)
5. **Tab 5**: Grafana (performance charts)

## 📸 Screenshot Checklist

- [ ] Aspire Dashboard showing all services
- [ ] Kafka UI with topic message counts
- [ ] Flink job execution graph
- [ ] Swagger UI successful API responses
- [ ] Grafana performance metrics
- [ ] Temporal workflow execution

## 🔧 Quick Troubleshooting

**No messages in Kafka**: Check broker status, restart Kafka container
**Flink job not starting**: Verify TaskManager status, check task slots
**API returning simulation**: Infrastructure degraded, check container logs
**High resource usage**: Monitor Aspire Dashboard, consider scaling

---
💡 **Pro Tip**: Open all monitoring dashboards in separate browser tabs before starting the stress test for real-time visibility across all systems.