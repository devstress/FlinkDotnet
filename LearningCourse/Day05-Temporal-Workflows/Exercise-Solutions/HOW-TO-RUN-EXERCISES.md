# Day 5: Step-by-Step Exercise Instructions 📚

**For Students: Follow these exact steps to complete all Day 5 workflow exercises**

> 🎯 **Goal**: By the end of this guide, you'll have successfully run all 4 Day 5 Temporal workflow exercises and understand enterprise orchestration patterns.

---

## 📋 Prerequisites (MUST DO FIRST)

### ✅ Step 1: Verify Infrastructure is Running
```bash
# Check if LocalTesting from Day 1 is still running
curl http://localhost:8084/api/v1/namespaces
curl http://localhost:7233/api/v1/health
```

**Expected Output:**
- Temporal UI should show namespaces
- Temporal server should return health status

**❌ If any fail:**
```bash
# Restart infrastructure from Day 1
cd LocalTesting
dotnet run --project LocalTesting.AppHost
# Wait 90 seconds for all services to start
```

### ✅ Step 2: Navigate to Day 5 Exercises
```bash
# Navigate to Day 5 exercise solutions
cd LearningCourse/Day05-Temporal-Workflows/Exercise-Solutions
```

---

## 🏃‍♂️ Exercise Execution (4 Workflow Exercises)

### 🚗 Exercise 5.1: Uber Trip Workflow Orchestration

**What you'll learn**: Build Uber-scale trip workflows with Temporal

```bash
# Navigate to Exercise 5.1
cd Exercise51

# Build the project
dotnet build

# Run Uber-style trip workflow orchestration
dotnet run
```

**Expected Output:**
```
🚗 Uber Trip Workflow Orchestration System
==========================================
🎯 Trip workflow registration completed
✅ Driver assignment workflows active
🔄 Real-time trip state management
📱 Customer notification workflows
✅ Uber-scale trip orchestration operational!
```

**✅ Success indicators:**
- Trip workflows registered
- Driver assignment working
- State management active

---

### 🏠 Exercise 5.2: Airbnb Booking Event Sourcing

**What you'll learn**: Implement Airbnb-style booking workflows with event sourcing

```bash
# Navigate to Exercise 5.2
cd ../Exercise52

# Build the project
dotnet build

# Run Airbnb-style booking event sourcing
dotnet run
```

**Expected Output:**
```
🏠 Airbnb Booking Event Sourcing System
=======================================
📅 Booking workflow orchestration active
🔍 Event sourcing pattern implementation
💰 Payment processing workflows enabled
🔒 Conflict resolution and consistency
✅ Airbnb-scale booking system operational!
```

**✅ Success indicators:**
- Booking workflows active
- Event sourcing working
- Payment processing enabled

---

### 💳 Exercise 5.3: Stripe Payment Processing Workflows

**What you'll learn**: Create Stripe-style payment workflows with reliability guarantees

```bash
# Navigate to Exercise 5.3
cd ../Exercise53

# Build the project
dotnet build

# Run Stripe-style payment processing workflows
dotnet run
```

**Expected Output:**
```
💳 Stripe Payment Processing Workflows
======================================
⚡ Payment workflow engine initialized
🔒 Secure payment processing active
🔄 Retry and compensation patterns
📊 Payment analytics and monitoring
✅ Stripe-scale payment workflows operational!
```

**✅ Success indicators:**
- Payment workflows initialized
- Retry patterns working
- Analytics monitoring active

---

### 🏢 Exercise 5.4: Enterprise Workflow Patterns

**What you'll learn**: Implement comprehensive enterprise workflow patterns

```bash
# Navigate to Exercise 5.4
cd ../Exercise54

# Build the project
dotnet build

# Run enterprise workflow patterns
dotnet run
```

**Expected Output:**
```
🏢 Enterprise Workflow Patterns System
======================================
🔄 Complex workflow orchestration
📊 Business process automation
🔍 Workflow monitoring and analytics
⚡ High availability and failover
✅ Enterprise workflow patterns operational!
```

**✅ Verify workflows:**
- Open http://localhost:8084 (Temporal UI)
- Should show active workflows
- Workflow history visible

---

## 🎉 Exercise Completion Checklist

Mark each exercise as complete:

- [ ] **Exercise 5.1**: Uber Trip Workflows ✅ operational
- [ ] **Exercise 5.2**: Airbnb Event Sourcing ✅ working
- [ ] **Exercise 5.3**: Stripe Payment Workflows ✅ running
- [ ] **Exercise 5.4**: Enterprise Patterns ✅ accessible

## ❓ Troubleshooting Common Issues

### Problem: "Temporal connection failed"
**Solution:**
```bash
# Check Temporal is running
curl http://localhost:8084/api/v1/namespaces
# If fails, restart LocalTesting infrastructure
```

### Problem: "Workflow registration errors"
**Solution:**
- Ensure Temporal server is fully started (may take 60 seconds)
- Restart the specific exercise
- Check available memory (need 4GB+ free)

### Problem: "Event sourcing issues"
**Solution:**
- PostgreSQL database may be initializing
- Wait 30 seconds and try again
- Focus on the workflow patterns working

## 🎯 What You've Accomplished

✅ **Trip Orchestration**: Uber-scale workflow management for millions of trips  
✅ **Event Sourcing**: Airbnb-style booking consistency and conflict resolution  
✅ **Payment Processing**: Stripe-scale financial workflow reliability  
✅ **Enterprise Patterns**: Comprehensive business process automation  

**🚀 You're ready for Day 6!**

---

## 📚 Quick Reference - Copy/Paste Commands

### Run All Day 5 Exercises:
```bash
cd LearningCourse/Day05-Temporal-Workflows/Exercise-Solutions

# Exercise 5.1: Uber Workflows
cd Exercise51 && dotnet run && cd ..

# Exercise 5.2: Airbnb Event Sourcing
cd Exercise52 && dotnet run && cd ..

# Exercise 5.3: Stripe Payments
cd Exercise53 && dotnet run && cd ..

# Exercise 5.4: Enterprise Patterns
cd Exercise54 && dotnet run && cd ..
```

### Check Temporal Status:
```bash
# Verify workflow services
curl http://localhost:8084/api/v1/namespaces  # Temporal UI
curl http://localhost:7233/api/v1/health      # Temporal Server
```