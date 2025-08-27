# Day 4: Step-by-Step Exercise Instructions 📚

**For Students: Follow these exact steps to complete all Day 4 observability exercises**

> 🎯 **Goal**: By the end of this guide, you'll have successfully run all 4 Day 4 enterprise observability exercises and understand Google SRE practices.

---

## 📋 Prerequisites (MUST DO FIRST)

### ✅ Step 1: Verify Infrastructure is Running
```bash
# Check if LocalTesting from Day 1 is still running
curl http://localhost:8081/overview
curl http://localhost:3000/api/health
curl http://localhost:9090/api/v1/targets
```

**Expected Output:**
- Flink cluster should show running state
- Grafana should return health status
- Prometheus should show monitoring targets

**❌ If any fail:**
```bash
# Restart infrastructure from Day 1
cd LocalTesting
dotnet run --project LocalTesting.AppHost
# Wait 90 seconds for all services to start
```

### ✅ Step 2: Navigate to Day 4 Exercises
```bash
# Navigate to Day 4 exercise solutions
cd LearningCourse/Day04-Enterprise-Observability/Exercise-Solutions
```

---

## 🏃‍♂️ Exercise Execution (4 Observability Exercises)

### 📊 Exercise 4.1: Google SRE SLI/SLO Management

**What you'll learn**: Implement Google-style service level monitoring

```bash
# Navigate to Exercise 4.1
cd Exercise41

# Build the project
dotnet build

# Run Google SRE SLI/SLO management system
dotnet run
```

**Expected Output:**
```
📊 Google SRE SLI/SLO Management System
======================================
🎯 Initializing Service Level Indicators (SLIs)
✅ SLO monitoring configured (99.9% availability)
📈 Error budget tracking active
🔍 SLI metrics collection started
✅ Google SRE patterns operational!
```

**✅ Success indicators:**
- SLI monitoring active
- Error budget tracking working
- No monitoring failures

---

### 🚨 Exercise 4.2: Datadog-Style Alerting

**What you'll learn**: Build enterprise alerting like Datadog's monitoring platform

```bash
# Navigate to Exercise 4.2
cd ../Exercise42

# Build the project
dotnet build

# Run Datadog-style enterprise alerting
dotnet run
```

**Expected Output:**
```
🚨 Datadog-Style Enterprise Alerting Engine
==========================================
⚡ Real-time alert processing initialized
📱 Multi-channel notification system active
🔔 Alert correlation and deduplication working
📊 Alert escalation policies configured
✅ Enterprise alerting patterns operational!
```

**✅ Success indicators:**
- Alert processing working
- Notification channels active
- Escalation policies configured

---

### 🔍 Exercise 4.3: Netflix Distributed Tracing

**What you'll learn**: Implement distributed tracing like Netflix's microservices

```bash
# Navigate to Exercise 4.3
cd ../Exercise43

# Build the project
dotnet build

# Run Netflix-style distributed tracing
dotnet run
```

**Expected Output:**
```
🔍 Netflix Distributed Tracing System
=====================================
🌐 Trace correlation across microservices
⚡ OpenTelemetry integration active
📊 Span collection and analysis working
🔗 Service dependency mapping enabled
✅ Netflix-scale distributed tracing operational!
```

**✅ Success indicators:**
- Trace correlation working
- OpenTelemetry integration active
- Service mapping enabled

---

### 📈 Exercise 4.4: Production Metrics Dashboard

**What you'll learn**: Create comprehensive production dashboards

```bash
# Navigate to Exercise 4.4
cd ../Exercise44

# Build the project
dotnet build

# Run production metrics dashboard
dotnet run
```

**Expected Output:**
```
📈 Production Metrics Dashboard System
======================================
📊 Real-time metrics aggregation active
🎯 Custom business metrics collection
📈 Performance trend analysis working
🔧 Operational insights dashboard ready
✅ Production metrics system operational!
```

**✅ Verify dashboard:**
- Open http://localhost:5000/dashboard
- Should show real-time metrics
- Performance trends visible

---

## 🎉 Exercise Completion Checklist

Mark each exercise as complete:

- [ ] **Exercise 4.1**: Google SRE SLI/SLO Management ✅ operational
- [ ] **Exercise 4.2**: Datadog-Style Alerting ✅ working
- [ ] **Exercise 4.3**: Netflix Distributed Tracing ✅ running
- [ ] **Exercise 4.4**: Production Metrics Dashboard ✅ accessible

## ❓ Troubleshooting Common Issues

### Problem: "Grafana connection failed"
**Solution:**
```bash
# Check Grafana is running
curl http://localhost:3000/api/health
# If fails, restart LocalTesting infrastructure
```

### Problem: "Prometheus targets down"
**Solution:**
- Check http://localhost:9090/targets
- Some targets may be down initially - this is normal
- Focus on the implementation patterns working

### Problem: "OpenTelemetry errors"
**Solution:**
- Ensure sufficient memory (need 3GB+ free)
- Restart the specific exercise
- Tracing may take 30 seconds to initialize

## 🎯 What You've Accomplished

✅ **SRE Practices**: Google-style SLI/SLO monitoring and error budgets  
✅ **Enterprise Alerting**: Datadog-scale alert processing and escalation  
✅ **Distributed Tracing**: Netflix-style microservice visibility  
✅ **Production Dashboards**: Real-time operational insights  

**🚀 You're ready for Day 5!**

---

## 📚 Quick Reference - Copy/Paste Commands

### Run All Day 4 Exercises:
```bash
cd LearningCourse/Day04-Enterprise-Observability/Exercise-Solutions

# Exercise 4.1: Google SRE
cd Exercise41 && dotnet run && cd ..

# Exercise 4.2: Datadog Alerting  
cd Exercise42 && dotnet run && cd ..

# Exercise 4.3: Netflix Tracing
cd Exercise43 && dotnet run && cd ..

# Exercise 4.4: Production Dashboard
cd Exercise44 && dotnet run && cd ..
```

### Check Observability Stack:
```bash
# Verify monitoring services
curl http://localhost:3000/api/health  # Grafana
curl http://localhost:9090/api/v1/targets  # Prometheus
curl http://localhost:18888  # Aspire Dashboard
```