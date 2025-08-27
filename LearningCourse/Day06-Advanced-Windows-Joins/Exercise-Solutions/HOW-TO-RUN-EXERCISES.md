# Day 6: Step-by-Step Exercise Instructions 📚

**For Students: Follow these exact steps to complete all Day 6 advanced windowing exercises**

> 🎯 **Goal**: By the end of this guide, you'll have successfully run all 4 Day 6 advanced windowing and joins exercises and understand social platform patterns.

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

**❌ If any fail:**
```bash
# Restart infrastructure from Day 1
cd LocalTesting
dotnet run --project LocalTesting.AppHost
# Wait 90 seconds for all services to start
```

### ✅ Step 2: Navigate to Day 6 Exercises
```bash
# Navigate to Day 6 exercise solutions
cd LearningCourse/Day06-Advanced-Windows-Joins/Exercise-Solutions
```

---

## 🏃‍♂️ Exercise Execution (4 Advanced Windowing Exercises)

### 💼 Exercise 6.1: LinkedIn Social Graph Processing

**What you'll learn**: Process 900M+ professional connections with advanced windowing

```bash
# Navigate to Exercise 6.1
cd Exercise61

# Build the project
dotnet build

# Run LinkedIn-style social graph processing
dotnet run
```

**Expected Output:**
```
💼 LinkedIn Social Graph Processing System
==========================================
🌐 Social graph windowing patterns active
📊 Professional connection analysis working
🔍 Advanced join operations processing
📈 Real-time graph analytics enabled
✅ LinkedIn-scale social processing operational!
```

**✅ Success indicators:**
- Social graph processing active
- Connection analysis working
- Join operations successful

---

### 🐦 Exercise 6.2: Twitter Real-time Trend Analysis

**What you'll learn**: Implement Twitter-style trending topics with tumbling windows

```bash
# Navigate to Exercise 6.2
cd ../Exercise62

# Build the project
dotnet build

# Run Twitter-style real-time trend analysis
dotnet run
```

**Expected Output:**
```
🐦 Twitter Real-time Trend Analysis System
==========================================
📊 Tumbling window aggregations active
🔥 Trending topic detection working
⚡ Real-time sentiment analysis enabled
📈 Viral content identification ready
✅ Twitter-scale trend analysis operational!
```

**✅ Success indicators:**
- Tumbling windows working
- Trend detection active
- Sentiment analysis enabled

---

### 📘 Exercise 6.3: Facebook Activity Stream Processing

**What you'll learn**: Build Facebook-style activity streams with session windows

```bash
# Navigate to Exercise 6.3
cd ../Exercise63

# Build the project
dotnet build

# Run Facebook-style activity stream processing
dotnet run
```

**Expected Output:**
```
📘 Facebook Activity Stream Processing System
============================================
👥 Session window processing active
📱 User activity aggregation working
🔔 Real-time notification generation
📊 Engagement metrics calculation ready
✅ Facebook-scale activity processing operational!
```

**✅ Success indicators:**
- Session windows processing
- Activity aggregation working
- Notification generation active

---

### ⚡ Exercise 6.4: Advanced Join Patterns

**What you'll learn**: Implement complex join patterns for social platforms

```bash
# Navigate to Exercise 6.4
cd ../Exercise64

# Build the project
dotnet build

# Run advanced join patterns demonstration
dotnet run
```

**Expected Output:**
```
⚡ Advanced Join Patterns System
===============================
🔗 Interval joins processing active
⏰ Temporal join patterns working
📊 Complex event correlation enabled
🌐 Multi-stream synchronization ready
✅ Advanced join patterns operational!
```

**✅ Success indicators:**
- Interval joins working
- Temporal patterns active
- Event correlation enabled

---

## 🎉 Exercise Completion Checklist

Mark each exercise as complete:

- [ ] **Exercise 6.1**: LinkedIn Social Graph ✅ operational
- [ ] **Exercise 6.2**: Twitter Trend Analysis ✅ working
- [ ] **Exercise 6.3**: Facebook Activity Streams ✅ running
- [ ] **Exercise 6.4**: Advanced Join Patterns ✅ processing

## ❓ Troubleshooting Common Issues

### Problem: "Window operations slow"
**Solution:**
- Windows may take 30-60 seconds to show results
- This is normal for demo data
- Focus on the windowing patterns working

### Problem: "Join operations failing"
**Solution:**
- Ensure sufficient memory (need 4GB+ free)
- Complex joins may take time to initialize
- Restart specific exercise if needed

### Problem: "Social graph processing errors"
**Solution:**
- Large graph processing is memory intensive
- Close other applications
- Restart LocalTesting if memory issues persist

## 🎯 What You've Accomplished

✅ **Social Graph Processing**: LinkedIn-scale professional network analysis  
✅ **Real-time Analytics**: Twitter-style trending topic detection  
✅ **Activity Streams**: Facebook-scale user engagement processing  
✅ **Advanced Joins**: Complex temporal and interval join patterns  

**🚀 You're ready for Day 7!**

---

## 📚 Quick Reference - Copy/Paste Commands

### Run All Day 6 Exercises:
```bash
cd LearningCourse/Day06-Advanced-Windows-Joins/Exercise-Solutions

# Exercise 6.1: LinkedIn Social Graph
cd Exercise61 && dotnet run && cd ..

# Exercise 6.2: Twitter Trends
cd Exercise62 && dotnet run && cd ..

# Exercise 6.3: Facebook Activity
cd Exercise63 && dotnet run && cd ..

# Exercise 6.4: Advanced Joins
cd Exercise64 && dotnet run && cd ..
```

### Monitor Windowing Performance:
```bash
# Check Flink dashboard for window operations
# Open http://localhost:8081 and look for running jobs
# Window operations may take 30-60 seconds to show metrics
```