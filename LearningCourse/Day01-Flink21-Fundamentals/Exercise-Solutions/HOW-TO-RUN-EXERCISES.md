# Day 1: Step-by-Step Exercise Instructions 📚

**For Students: Follow these exact steps to complete all Day 1 exercises**

> 🎯 **Goal**: By the end of this guide, you'll have successfully run all 8 Day 1 exercises and understand enterprise streaming patterns.

---

## 📋 Prerequisites (MUST DO FIRST)

### ✅ Step 1: Verify Your Environment
Copy and paste these commands in your terminal:

```bash
# Check .NET version (must be 9.0.x)
dotnet --version

# Check Docker is running
docker version

# Check available memory (need 8GB+)
docker system info | grep -i memory
```

**Expected Output:**
- `dotnet --version` should show `9.0.x`
- `docker version` should show version info without errors
- Memory should show 8GB+ available

**❌ If any fail:**
- Install .NET 9: https://dotnet.microsoft.com/download/dotnet/9.0
- Start Docker Desktop
- Close other applications to free memory

### ✅ Step 2: Start LocalTesting Infrastructure
```bash
# Navigate to repository root
cd FlinkDotNet

# Start the complete infrastructure (takes 60-90 seconds)
cd LocalTesting
dotnet run --project LocalTesting.AppHost
```

**Expected Output:**
```
✅ FLINK CLUSTER STATUS: JobManager: RUNNING (http://localhost:8081)
✅ KAFKA CLUSTER STATUS: Brokers: 3/3 ONLINE
✅ TEMPORAL CLUSTER STATUS: Server: RUNNING
✅ OBSERVABILITY STACK STATUS: All components running
✅ INFRASTRUCTURE READY FOR PRODUCTION WORKLOADS
```

**❌ If it fails:**
- Check ports aren't in use: `netstat -an | findstr "8081"`
- Restart Docker Desktop
- Wait 2 minutes and try again

### ✅ Step 3: Verify Infrastructure is Working
Open these URLs in your browser (should all load):

- **Flink Dashboard**: http://localhost:8081 ← Should show cluster with 3 TaskManagers
- **Kafka UI**: http://localhost:8082 ← Should show 3 brokers
- **Temporal UI**: http://localhost:8084 ← Should show namespace "default"
- **Grafana**: http://localhost:3000 ← Should show login screen
- **Aspire Dashboard**: http://localhost:18888 ← Should show running services

**✅ All URLs work? Great! Continue to exercises.**
**❌ Any URL fails? Re-run LocalTesting setup above.**

---

## 🏃‍♂️ Exercise Execution (8 Exercises)

### 🏢 Exercise 1.1: Netflix Infrastructure Validation

**What you'll learn**: Validate enterprise infrastructure like Netflix SRE teams

```bash
# Navigate to Exercise Solutions
cd LearningCourse/Day01-Flink21-Fundamentals/Exercise-Solutions

# Run Netflix-style infrastructure validation
pwsh ./infrastructure-validation.ps1
```

**Expected Output:**
```
🔍 FlinkDotNet Production Stack Validation
✅ FLINK CLUSTER STATUS: 3/3 TaskManagers HEALTHY
✅ KAFKA CLUSTER STATUS: 3/3 Brokers ONLINE  
✅ TEMPORAL CLUSTER STATUS: Server RUNNING
✅ OBSERVABILITY STACK STATUS: All systems operational
🎯 INFRASTRUCTURE READY - Netflix reliability standards met
```

**✅ Success indicators:**
- All checks show ✅ green checkmarks
- No ❌ red error messages
- Message shows "INFRASTRUCTURE READY"

---

### 🏢 Exercise 1.2: Uber State Backend Configuration

**What you'll learn**: Configure RocksDB for Uber-scale state management

```bash
# Navigate to ProductionApp
cd ProductionApp

# Build the application
dotnet build

# Run with RocksDB state backend configuration
dotnet run --configuration=RocksDBStateBackend
```

**Expected Output:**
```
🚀 Day 1 Production App with configuration: RocksDBStateBackend
💾 Configuring RocksDB State Backend for Uber-Scale Operations
🚀 Day 1 Production Streaming Application starting...
info: Microsoft.Hosting.Lifetime[14] Now listening on: http://localhost:5000
```

**✅ Test the state backend:**
```bash
# Open new terminal and test state performance endpoint
curl http://localhost:5000/state/performance
```

**Expected Response:**
```json
{
  "StateBackend": "RocksDB",
  "CheckpointPerformance": {
    "AverageCheckpointDuration": "950ms",
    "CheckpointSize": "250MB"
  },
  "StateOperations": {
    "ConcurrentOperations": "75000/sec"
  }
}
```

**🛑 Stop the app**: Press `Ctrl+C` before next exercise

---

### 🏢 Exercise 1.3: LinkedIn Load Management 

**What you'll learn**: Monitor backpressure like LinkedIn's feed generation system

```bash
# Open observability dashboard
start observability-dashboard.html
# OR on Mac/Linux: open observability-dashboard.html

# Run load testing in another terminal
pwsh ./load-testing.ps1
```

**Expected Output:**
```
🚀 Starting Production Load Test
📊 Generating realistic traffic patterns...
✅ Test completed: 10,000 requests in 45 seconds
📈 Average response time: 87ms
📊 99th percentile: 245ms
🎯 LinkedIn-scale performance achieved
```

**✅ Verify in dashboard:**
- Open the HTML file in browser
- Should show graphs with realistic data
- Response times should be under 100ms average

---

### 🏢 Exercise 1.4: Financial Services Security

**What you'll learn**: Implement banking-grade security compliance

```bash
# Run comprehensive security validation
pwsh ./infrastructure-validation.ps1 -SecurityValidation
```

**Expected Output:**
```
🔒 Security Validation - Financial Services Grade
✅ Fine-grained RBAC: Configured
✅ End-to-end encryption: Active
✅ Audit logging: Comprehensive
✅ Secret management: Integrated
🏛️ PCI DSS compliance requirements met
```

---

### 🏢 Exercise 1.5: Netflix Recommendation System

**What you'll learn**: Build Netflix-style AI recommendation engine

```bash
# Run the Netflix configuration
cd ProductionApp
dotnet run --configuration=RecommendationEngine
```

**Expected Output:**
```
🎯 Configuring Netflix-Style Recommendation Engine
info: Microsoft.Hosting.Lifetime[14] Now listening on: http://localhost:5000
```

**✅ Test Netflix recommendations:**
```bash
# Open new terminal and test recommendations
curl http://localhost:5000/recommendations/user123

# Check Netflix metrics
curl http://localhost:5000/netflix-metrics
```

**Expected Response for recommendations:**
```json
{
  "UserId": "user123",
  "PersonalizedContent": [
    {
      "ContentId": "movie_1234",
      "Title": "AI-Generated Thriller",
      "Score": 0.95,
      "Genre": "Sci-Fi"
    }
  ],
  "ResponseTimeMs": 23,
  "ABTestGroup": "ModelA"
}
```

**Expected Response for metrics:**
```json
{
  "ViewingHours": "2.5B+ daily",
  "RecommendationAccuracy": "87%",
  "ResponseLatency": "23ms",
  "GlobalUsers": "250M+"
}
```

**🛑 Stop the app**: Press `Ctrl+C` before next exercise

---

### 🏢 Exercise 1.6: Uber Dynamic Pricing

**What you'll learn**: Implement Uber-scale dynamic pricing engine

```bash
# Run the Uber configuration
dotnet run --configuration=DynamicPricingEngine
```

**Expected Output:**
```
🚗 Configuring Uber-Scale Dynamic Pricing Engine
info: Microsoft.Hosting.Lifetime[14] Now listening on: http://localhost:5000
```

**✅ Test Uber pricing:**
```bash
# Calculate dynamic pricing
curl -X POST http://localhost:5000/pricing/calculate -d '{"pickup":"downtown","destination":"airport"}' -H "Content-Type: application/json"

# Check driver matching
curl http://localhost:5000/driver-matching/downtown

# View Uber metrics
curl http://localhost:5000/uber-metrics
```

**Expected Response for pricing:**
```json
{
  "RideId": "a1b2c3d4",
  "BaseFare": 12.5,
  "SurgeMultiplier": 1.85,
  "FinalPrice": 23.13,
  "CalculationTimeMs": 12,
  "Demand": "78%",
  "Supply": "45%"
}
```

**🛑 Stop the app**: Press `Ctrl+C` before next exercise

---

### 🏢 Exercise 1.7: LinkedIn Feed Generation

**What you'll learn**: Build LinkedIn-style professional feed system

```bash
# Run the LinkedIn configuration
dotnet run --configuration=FeedGenerationEngine
```

**Expected Output:**
```
💼 Configuring LinkedIn Feed Generation Engine
info: Microsoft.Hosting.Lifetime[14] Now listening on: http://localhost:5000
```

**✅ Test LinkedIn feed:**
```bash
# Generate personalized feed
curl http://localhost:5000/feed/user456

# Test fraud detection
curl -X POST http://localhost:5000/fraud-detection -d '{"userId":"user456","activity":"rapid_posting"}' -H "Content-Type: application/json"

# View LinkedIn metrics
curl http://localhost:5000/linkedin-metrics
```

**Expected Response for feed:**
```json
{
  "UserId": "user456",
  "FeedItems": [
    {
      "Type": "job_post",
      "Content": "Senior Flink Engineer at Netflix",
      "Relevance": 0.94,
      "Engagement": "High"
    }
  ],
  "GenerationTimeMs": 18,
  "PersonalizationScore": 0.923
}
```

**🛑 Stop the app**: Press `Ctrl+C` before next exercise

---

### 🏢 Exercise 1.8: Google SRE Observability

**What you'll learn**: Implement Google-style SRE monitoring practices

```bash
# Run SRE monitoring validation
pwsh ./infrastructure-validation.ps1 -SREMonitoring

# Open comprehensive observability dashboard
start observability-dashboard.html
```

**Expected Output:**
```
📊 Google SRE Practices Validation
✅ SLI/SLO monitoring: Active
✅ Error budget tracking: Configured
✅ Distributed tracing: Enabled
✅ Predictive capacity planning: Running
🎯 Google-level reliability standards achieved
```

**✅ Verify SRE dashboard:**
- Dashboard opens in browser
- Shows multiple monitoring panels
- Data is updating in real-time
- No error messages displayed

---

## 🎉 Exercise Completion Checklist

Mark each exercise as complete:

- [ ] **Exercise 1.1**: Infrastructure validation ✅ passed
- [ ] **Exercise 1.2**: RocksDB state backend responded correctly  
- [ ] **Exercise 1.3**: Load testing completed successfully
- [ ] **Exercise 1.4**: Security validation ✅ passed
- [ ] **Exercise 1.5**: Netflix recommendations working
- [ ] **Exercise 1.6**: Uber pricing calculations working
- [ ] **Exercise 1.7**: LinkedIn feed generation working
- [ ] **Exercise 1.8**: SRE monitoring dashboard opened

## ❓ Troubleshooting Common Issues

### Problem: "Port already in use" 
**Solution:**
```bash
# Find what's using the port
netstat -an | findstr "5000"
# Kill the process and try again
```

### Problem: "dotnet build failed"
**Solution:**
```bash
# Clean and restore
dotnet clean
dotnet restore
dotnet build
```

### Problem: "curl command not found"
**Solution:**
- **Windows**: Use PowerShell or install curl
- **Alternative**: Open URLs in browser instead of curl

### Problem: Infrastructure won't start
**Solution:**
```bash
# Restart Docker Desktop
# Wait 2 minutes
# Re-run: dotnet run --project LocalTesting.AppHost
```

## 🎯 What You've Accomplished

✅ **Infrastructure Skills**: Set up enterprise-grade streaming infrastructure  
✅ **Netflix Skills**: Built AI recommendation systems at scale  
✅ **Uber Skills**: Implemented dynamic pricing with state management  
✅ **LinkedIn Skills**: Created professional content generation systems  
✅ **Google Skills**: Applied SRE practices for reliability engineering  
✅ **Security Skills**: Implemented financial-grade compliance patterns  

**🚀 You're ready for Day 2!**

---

## 📚 Quick Reference - Copy/Paste Commands

### Start Infrastructure:
```bash
cd LocalTesting && dotnet run --project LocalTesting.AppHost
```

### Run Netflix Exercise:
```bash
cd ProductionApp && dotnet run --configuration=RecommendationEngine
```

### Run Uber Exercise:
```bash
cd ProductionApp && dotnet run --configuration=DynamicPricingEngine  
```

### Run LinkedIn Exercise:
```bash
cd ProductionApp && dotnet run --configuration=FeedGenerationEngine
```

### Test Any Configuration:
```bash
curl http://localhost:5000/health
curl http://localhost:5000/metrics
```