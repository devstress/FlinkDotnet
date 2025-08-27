# Day 2: Step-by-Step Exercise Instructions 📚

**For Students: Follow these exact steps to complete all Day 2 AI exercises**

> 🎯 **Goal**: By the end of this guide, you'll have successfully run all 4 Day 2 AI streaming exercises and understand enterprise AI patterns.

---

## 📋 Prerequisites (MUST DO FIRST)

### ✅ Step 1: Verify Infrastructure is Running
```bash
# Check if LocalTesting from Day 1 is still running
curl http://localhost:8081/overview
curl http://localhost:5000/health
```

**Expected Output:**
- First command should return Flink cluster JSON
- Second command should return health status

**❌ If either fails:**
```bash
# Restart infrastructure from Day 1
cd LocalTesting
dotnet run --project LocalTesting.AppHost
# Wait 90 seconds for all services to start
```

### ✅ Step 2: Navigate to Day 2 Exercises
```bash
# Navigate to Day 2 exercise solutions
cd LearningCourse/Day02-AI-Stream-Processing/Exercise-Solutions
```

---

## 🏃‍♂️ Exercise Execution (4 AI Exercises)

### 🧠 Exercise 2.1: Netflix AI Model Management

**What you'll learn**: Manage 200+ ML models like Netflix's recommendation system

```bash
# Navigate to AI Model DDL project
cd AIModelDDLMastery

# Build the project
dotnet build

# Run Netflix-style AI model management
dotnet run
```

**Expected Output:**
```
🧠 AI Model DDL Mastery - Flink 2.1.0 AI Breakthrough Demo
================================================================
🎯 Registering recommendation_model_v1 with DDL
✅ Model registered successfully with metadata
🔄 Testing model versioning and inheritance
✅ Model versioning demonstration completed
📊 A/B testing configuration applied
✅ Traffic splitting between model versions active
✅ AI Model DDL Mastery demonstration completed successfully!
```

**✅ Success indicators:**
- All steps show ✅ checkmarks
- No exception messages
- Summary shows implemented AI features

---

### 🛡️ Exercise 2.2: Uber Fraud Detection System

**What you'll learn**: Build real-time fraud detection for 15M+ daily transactions

```bash
# Navigate to fraud detection project
cd ../FraudDetectionSystem

# Build the project
dotnet build

# Run Uber-style fraud detection
dotnet run
```

**Expected Output:**
```
🛡️ Fraud Detection System - Real-time ML Inference Demo
========================================================
🔍 Initializing ML_PREDICT TVF for fraud detection
✅ Fraud detection models loaded
⚡ Processing transaction stream with ML_PREDICT
🔒 Fraud pattern detected: Score 0.87 (HIGH RISK)
✅ Ensemble inference completed: 99.3% accuracy
📈 Performance: 2.3ms inference latency
✅ Fraud Detection System demonstration completed!
```

**✅ Success indicators:**
- Fraud patterns are detected
- Inference latency under 5ms
- Accuracy above 95%

---

### 💼 Exercise 2.3: LinkedIn Behavioral Analytics

**What you'll learn**: Process 900M+ user interactions for content personalization

```bash
# Navigate to ML Predict implementation
cd ../MLPredictTVFImplementation

# Build the project
dotnet build

# Run LinkedIn-style behavioral analytics
dotnet run
```

**Expected Output:**
```
💼 ML Predict TVF Implementation - LinkedIn Behavioral Analytics
===============================================================
🔄 Initializing Process Table Functions for behavioral analysis
✅ Stateful behavioral models loaded
📊 Processing user interaction patterns
🎯 Personalization score calculated: 0.923
✅ Content relevance optimized for professional network
📈 Engagement prediction: 87% likelihood
✅ LinkedIn Behavioral Analytics completed successfully!
```

**✅ Success indicators:**
- Personalization scores generated
- Engagement predictions shown
- Process Table Functions working

---

### 🛒 Exercise 2.4: Amazon Product Recommendations

**What you'll learn**: Handle 310M+ customers with dynamic schema AI processing

```bash
# Navigate to ML.NET integration project  
cd ../MLNetIntegration

# Build the project
dotnet build

# Run Amazon-style product recommendations
dotnet run
```

**Expected Output:**
```
🛒 ML.NET Integration - Amazon Product Recommendation Engine
===========================================================
📊 Loading VARIANT data types for dynamic schema processing
✅ Dynamic feature engineering pipeline initialized
🔄 Processing cross-category product data
🎯 Product recommendations generated for customer segment
✅ Lakehouse integration with Apache Paimon active
📈 Recommendation accuracy: 91.2%
✅ Amazon Product Recommendation Engine completed successfully!
```

**✅ Success indicators:**
- Dynamic schema processing working
- Product recommendations generated
- Lakehouse integration confirmed

---

## 🎉 Exercise Completion Checklist

Mark each exercise as complete:

- [ ] **Exercise 2.1**: Netflix AI Model Management ✅ completed
- [ ] **Exercise 2.2**: Uber Fraud Detection ✅ working
- [ ] **Exercise 2.3**: LinkedIn Behavioral Analytics ✅ running
- [ ] **Exercise 2.4**: Amazon Product Recommendations ✅ successful

## ❓ Troubleshooting Common Issues

### Problem: "Project won't build"
**Solution:**
```bash
# Clean and restore all projects
dotnet clean
dotnet restore
dotnet build
```

### Problem: "Models failed to load"
**Solution:**
- Ensure LocalTesting infrastructure is running
- Restart the specific exercise
- Check available memory (need 4GB+ free)

### Problem: "Low accuracy scores"
**Solution:**
- This is normal for demo data
- Real production systems use actual training data
- Focus on the technical implementation working

## 🎯 What You've Accomplished

✅ **AI Model Management**: Netflix-style ML model lifecycle management  
✅ **Real-time Inference**: Uber-scale fraud detection with ML_PREDICT  
✅ **Behavioral Analytics**: LinkedIn-style user interaction processing  
✅ **Dynamic Schema AI**: Amazon-scale product recommendation engine  

**🚀 You're ready for Day 3!**

---

## 📚 Quick Reference - Copy/Paste Commands

### Run All Day 2 Exercises:
```bash
cd LearningCourse/Day02-AI-Stream-Processing/Exercise-Solutions

# Exercise 2.1: Netflix AI Models
cd AIModelDDLMastery && dotnet run && cd ..

# Exercise 2.2: Uber Fraud Detection  
cd FraudDetectionSystem && dotnet run && cd ..

# Exercise 2.3: LinkedIn Analytics
cd MLPredictTVFImplementation && dotnet run && cd ..

# Exercise 2.4: Amazon Recommendations
cd MLNetIntegration && dotnet run && cd ..
```