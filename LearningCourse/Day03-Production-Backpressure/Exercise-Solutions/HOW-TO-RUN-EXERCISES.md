# Day 3: Step-by-Step Exercise Instructions 📚

**For Students: Follow these exact steps to complete all Day 3 backpressure exercises**

> 🎯 **Goal**: By the end of this guide, you'll have successfully run all 4 Day 3 production backpressure exercises and understand enterprise rate limiting patterns.

---

## 📋 Prerequisites (MUST DO FIRST)

### ✅ Step 1: Verify Infrastructure is Running
```bash
# Check if LocalTesting from Day 1 is still running
curl http://localhost:8081/overview
curl http://localhost:8082/api/clusters
```

**Expected Output:**
- Flink cluster should show running TaskManagers
- Kafka cluster should show 3 brokers

**❌ If either fails:**
```bash
# Restart infrastructure from Day 1
cd LocalTesting
dotnet run --project LocalTesting.AppHost
# Wait 90 seconds for all services to start
```

### ✅ Step 2: Navigate to Day 3 Exercises
```bash
# Navigate to Day 3 exercise solutions
cd LearningCourse/Day03-Production-Backpressure/Exercise-Solutions
```

---

## 🏃‍♂️ Exercise Execution (4 Backpressure Exercises)

### 🌐 Exercise 3.1: Netflix Global Rate Limiting

**What you'll learn**: Coordinate rate limits across 2000+ microservices like Netflix

```bash
# Navigate to Exercise 3.1
cd Exercise31

# Build the project
dotnet build

# Run Netflix-style global rate limiting controller
dotnet run
```

**Expected Output:**
```
🌐 Netflix Global Rate Limiting Controller
=========================================
🎯 Initializing Global Quota Controller (GQC)
✅ Epoch-based budget minting every 250ms
🔄 Cross-region coordination prevention active
✅ Policy distribution to regional banks
📊 Pre-mint budget futures configured
✅ Netflix-scale global rate limiting operational!
```

**✅ Success indicators:**
- Epoch-based budget minting working
- Cross-region coordination active
- No error messages

---

### 🗃️ Exercise 3.2: Uber Regional Budget Coordination

**What you'll learn**: Handle 15M+ ride requests with Redis coordination like Uber

```bash
# Navigate to Exercise 3.2
cd ../Exercise32

# Build the project  
dotnet build

# Run Uber-style regional budget coordination
dotnet run
```

**Expected Output:**
```
🗃️ Uber Regional Budget Bank Coordination
=========================================
⚡ Atomic Redis DECRBY operations initialized
🕐 TTL management for budget expiration active
🔄 Regional failover handling configured
📈 Background refill patterns (250ms intervals)
✅ Fair allocation across 15M+ requests achieved
✅ Uber-scale regional coordination operational!
```

**✅ Success indicators:**
- Redis DECRBY operations working
- TTL management active
- Background refill operational

---

### 🚪 Exercise 3.3: LinkedIn High-Performance Gateway

**What you'll learn**: Process 900M+ user requests with gRPC optimization like LinkedIn

```bash
# Navigate to Exercise 3.3
cd ../Exercise33

# Build the project
dotnet build

# Run LinkedIn-style high-performance gateway
dotnet run
```

**Expected Output:**
```
🚪 LinkedIn High-Performance gRPC Gateway
==========================================
🎯 Local token buckets for hot path limiting
🔒 "Safe by default" startup behavior (SEVERE pause)
🔄 Background refill coordination active
⚡ gRPC streaming optimization enabled
📊 Processing 900M+ user requests efficiently
✅ LinkedIn-scale gateway performance achieved!
```

**✅ Success indicators:**
- Local token buckets operational
- Safe startup behavior confirmed
- gRPC optimization working

---

### 🧪 Exercise 3.4: Chaos Engineering Validation

**What you'll learn**: Test compound failures like Netflix/Uber production validation

```bash
# Navigate to Exercise 3.4
cd ../Exercise34

# Build the project
dotnet build

# Run chaos engineering validation
dotnet run
```

**Expected Output:**
```
🧪 Chaos Engineering Production Validation
===========================================
💥 Testing compound failure scenarios
🔄 Gateway restart + Redis partition simulation
🌐 Network delay injection testing
🔍 Circuit breaker integration validation
📊 End-to-end flow control verification
✅ Production resilience patterns validated!
✅ Chaos engineering tests passed!
```

**✅ Success indicators:**
- Compound failure scenarios tested
- Circuit breaker validation passed
- End-to-end flow control verified

---

## 🎉 Exercise Completion Checklist

Mark each exercise as complete:

- [ ] **Exercise 3.1**: Netflix Global Rate Limiting ✅ operational
- [ ] **Exercise 3.2**: Uber Regional Coordination ✅ working  
- [ ] **Exercise 3.3**: LinkedIn High-Performance Gateway ✅ running
- [ ] **Exercise 3.4**: Chaos Engineering Validation ✅ passed

## ❓ Troubleshooting Common Issues

### Problem: "Redis connection failed"
**Solution:**
```bash
# Check if Redis is running in LocalTesting
curl http://localhost:6379/ping
# If fails, restart LocalTesting infrastructure
```

### Problem: "Budget allocation errors"
**Solution:**
- Ensure all previous exercises are stopped (Ctrl+C)
- Redis may have stale data - this is normal for demo
- Focus on the rate limiting logic working

### Problem: "gRPC streaming issues"  
**Solution:**
- Check available memory (need 2GB+ free)
- Restart the exercise
- gRPC streaming simulation may take 10-15 seconds to initialize

## 🎯 What You've Accomplished

✅ **Global Rate Limiting**: Netflix-style cross-region coordination  
✅ **Regional Coordination**: Uber-scale Redis budget management  
✅ **High-Performance Gateway**: LinkedIn-style gRPC optimization  
✅ **Chaos Engineering**: Production resilience validation  

**🚀 You're ready for Day 4!**

---

## 📚 Quick Reference - Copy/Paste Commands

### Run All Day 3 Exercises:
```bash
cd LearningCourse/Day03-Production-Backpressure/Exercise-Solutions

# Exercise 3.1: Netflix Global Rate Limiting
cd Exercise31 && dotnet run && cd ..

# Exercise 3.2: Uber Regional Coordination
cd Exercise32 && dotnet run && cd ..

# Exercise 3.3: LinkedIn Gateway  
cd Exercise33 && dotnet run && cd ..

# Exercise 3.4: Chaos Engineering
cd Exercise34 && dotnet run && cd ..
```

### Check Infrastructure Status:
```bash
# Verify all services running
curl http://localhost:8081/overview     # Flink
curl http://localhost:8082/api/clusters # Kafka  
curl http://localhost:6379/ping         # Redis
```