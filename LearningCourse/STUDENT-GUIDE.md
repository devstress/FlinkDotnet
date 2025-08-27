# 📚 Complete 14-Day Course - Step-by-Step Instructions

**For Students: Your complete learning path through all 14 days**

> 🎯 **Goal**: Master enterprise streaming patterns from Netflix, Uber, LinkedIn, and other industry leaders

---

## 📅 Course Overview - What You'll Build

| Day | Topic | Company Patterns | What You'll Build |
|-----|-------|------------------|-------------------|
| **Day 1** | [Flink Fundamentals](Day01-Flink21-Fundamentals/Exercise-Solutions/HOW-TO-RUN-EXERCISES.md) | Netflix, Uber, LinkedIn | Infrastructure + AI Recommendations |
| **Day 2** | [AI Stream Processing](Day02-AI-Stream-Processing/Exercise-Solutions/HOW-TO-RUN-EXERCISES.md) | Netflix, Uber, LinkedIn, Amazon | ML Model Management + Fraud Detection |
| **Day 3** | [Production Backpressure](Day03-Production-Backpressure/Exercise-Solutions/HOW-TO-RUN-EXERCISES.md) | Netflix, Uber, LinkedIn | Global Rate Limiting + Chaos Engineering |
| **Day 4** | Enterprise Observability | Google, Datadog, Netflix | SRE Monitoring + Alert Management |
| **Day 5** | Temporal Workflows | Uber, Airbnb, Stripe | Workflow Orchestration + Event Sourcing |
| **Day 6** | Advanced Windows/Joins | LinkedIn, Twitter, Facebook | Social Graph + Real-time Analytics |
| **Day 7** | Stress Testing | Netflix, Uber, Amazon | Load Testing + Performance Validation |
| **Day 8** | Exactly-Once Semantics | Uber, Stripe, PayPal | Financial Accuracy + Transaction Processing |
| **Day 9** | Performance Optimization | Netflix, LinkedIn, Google | Auto-scaling + Resource Management |
| **Day 10** | Security & Compliance | Banking, Healthcare, Finance | GDPR + PCI DSS + SOX Compliance |
| **Day 11** | Disaster Recovery | Netflix, AWS, Azure | Multi-region + Backup/Restore |
| **Day 12** | Advanced Patterns | Uber, LinkedIn, Airbnb | Complex Event Processing + State Machines |
| **Day 13** | Testing & Chaos | Netflix, Amazon, Google | Chaos Engineering + Integration Testing |
| **Day 14** | Capstone Project | All Companies | Complete Production System |

---

## 🚀 Quick Start - Begin Your Journey

### ✅ Prerequisites (Do Once)
```bash
# 1. Verify .NET 9 is installed
dotnet --version  # Should show 9.0.x

# 2. Clone and navigate to course
cd FlinkDotNet/LearningCourse

# 3. Start infrastructure (used by all days)
cd LocalTesting
dotnet run --project LocalTesting.AppHost
# Wait 90 seconds for all services to start
```

### ✅ Verify Infrastructure is Working
Open these URLs - all should work:
- **Flink Dashboard**: http://localhost:8081
- **Kafka UI**: http://localhost:8082  
- **Temporal UI**: http://localhost:8084
- **Grafana**: http://localhost:3000
- **Aspire Dashboard**: http://localhost:18888

**✅ All working? You're ready to start Day 1!**

---

## 📖 How to Follow Each Day

Each day follows the same simple pattern:

### 📂 1. Navigate to the Day
```bash
cd Day[XX]-[Topic-Name]/Exercise-Solutions
```

### 📚 2. Open the Step-by-Step Guide
Look for: **`HOW-TO-RUN-EXERCISES.md`** in each Exercise-Solutions folder

### 🏃‍♂️ 3. Follow the Instructions
Each guide contains:
- ✅ Prerequisites check
- 🏢 Company-specific exercises (Netflix, Uber, etc.)
- 📋 Copy/paste commands
- ✅ Success indicators
- ❓ Troubleshooting help

### 🎯 4. Complete the Checklist
Mark off each exercise as you complete it

---

## 📚 Day-by-Day Quick Links

### Week 1: Foundations
- **[Day 1: Flink Fundamentals](Day01-Flink21-Fundamentals/Exercise-Solutions/HOW-TO-RUN-EXERCISES.md)** ← START HERE
- **[Day 2: AI Stream Processing](Day02-AI-Stream-Processing/Exercise-Solutions/HOW-TO-RUN-EXERCISES.md)**
- **[Day 3: Production Backpressure](Day03-Production-Backpressure/Exercise-Solutions/HOW-TO-RUN-EXERCISES.md)**
- **[Day 4: Enterprise Observability](Day04-Enterprise-Observability/Exercise-Solutions/HOW-TO-RUN-EXERCISES.md)**
- **[Day 5: Temporal Workflows](Day05-Temporal-Workflows/Exercise-Solutions/HOW-TO-RUN-EXERCISES.md)**
- **[Day 6: Advanced Windows/Joins](Day06-Advanced-Windows-Joins/Exercise-Solutions/HOW-TO-RUN-EXERCISES.md)**
- **[Day 7: Stress Testing](Day07-Stress-Testing/Exercise-Solutions/HOW-TO-RUN-EXERCISES.md)**

### Week 2: Advanced Patterns  
- **[Day 8: Exactly-Once Semantics](Day08-Exactly-Once-Semantics/Exercise-Solutions/HOW-TO-RUN-EXERCISES.md)**
- **[Day 9: Performance Optimization](Day09-Performance-Optimization-Scaling/Exercise-Solutions/HOW-TO-RUN-EXERCISES.md)**
- **[Day 10: Security & Compliance](Day10-Security-Privacy-Compliance/Exercise-Solutions/HOW-TO-RUN-EXERCISES.md)**
- **[Day 11: Disaster Recovery](Day11-Disaster-Recovery-Multi-Region/Exercise-Solutions/HOW-TO-RUN-EXERCISES.md)**
- **[Day 12: Advanced Patterns](Day12-Advanced-Streaming-Patterns/Exercise-Solutions/HOW-TO-RUN-EXERCISES.md)**
- **[Day 13: Testing & Chaos](Day13-Advanced-Testing-Chaos-Engineering/Exercise-Solutions/HOW-TO-RUN-EXERCISES.md)**
- **[Day 14: Capstone Project](Day14-Capstone-Project/Exercise-Solutions/HOW-TO-RUN-EXERCISES.md)**

---

## 🎯 Learning Path Recommendations

### 🏃‍♂️ Fast Track (1 day = 2-3 hours)
- Complete all exercises in order
- Focus on getting them running successfully
- Read theory sections for context

### 🚶‍♂️ Comprehensive (1 day = 4-6 hours)  
- Read full theory in each day's main README.md
- Complete all exercises with understanding
- Explore the company patterns and business context

### 🧠 Expert Track (1 day = 6-8 hours)
- Deep dive into source code implementations
- Modify exercises for your own use cases
- Contribute improvements back to the course

---

## ❓ Common Issues Across All Days

### Problem: Infrastructure won't start
**Solution:**
```bash
# Stop everything and restart
Ctrl+C  # Stop current processes
cd LocalTesting
dotnet run --project LocalTesting.AppHost
# Wait 90 seconds
```

### Problem: Port already in use
**Solution:**
```bash
# Find and kill conflicting processes
netstat -an | findstr "8081\|8082\|5000"
# Kill the processes, then restart
```

### Problem: Out of memory errors
**Solution:**
- Close other applications
- Restart Docker Desktop
- Ensure 8GB+ RAM available

### Problem: .NET build failures
**Solution:**
```bash
# Clean and restore all projects
dotnet clean
dotnet restore  
dotnet build
```

---

## 🏆 Completion Tracking

Track your progress through the course:

### Week 1 Progress
- [ ] **Day 1**: Netflix/Uber/LinkedIn Fundamentals ✅
- [ ] **Day 2**: AI Stream Processing ✅  
- [ ] **Day 3**: Production Backpressure ✅
- [ ] **Day 4**: Enterprise Observability ✅
- [ ] **Day 5**: Temporal Workflows ✅
- [ ] **Day 6**: Advanced Windows/Joins ✅
- [ ] **Day 7**: Stress Testing ✅

### Week 2 Progress  
- [ ] **Day 8**: Exactly-Once Semantics ✅
- [ ] **Day 9**: Performance Optimization ✅
- [ ] **Day 10**: Security & Compliance ✅
- [ ] **Day 11**: Disaster Recovery ✅
- [ ] **Day 12**: Advanced Patterns ✅
- [ ] **Day 13**: Testing & Chaos ✅
- [ ] **Day 14**: Capstone Project ✅

---

## 🎓 What You'll Achieve

By completing this 14-day course, you'll have:

✅ **Built 50+ working applications** using enterprise patterns  
✅ **Mastered Netflix-scale recommendation systems** with real-time AI  
✅ **Implemented Uber-scale financial processing** with exactly-once semantics  
✅ **Created LinkedIn-style social platforms** with 900M+ user capacity  
✅ **Applied Google SRE practices** for 99.99% uptime reliability  
✅ **Demonstrated enterprise security** meeting banking compliance standards  
✅ **Designed disaster recovery** for multi-region deployments  
✅ **Validated production systems** with chaos engineering

**🚀 Ready to become an enterprise streaming expert? Start with Day 1!**

---

## 📞 Getting Help

- **Issues with instructions**: Each day has troubleshooting sections
- **Code not working**: Check the Working Solutions in each Exercise-Solutions folder
- **Understanding concepts**: Read the theory sections in each day's main README.md

**Remember**: The goal is learning enterprise patterns, not perfection. Focus on getting the exercises running and understanding the business patterns!

---

**🎯 [START YOUR JOURNEY: Day 1 Instructions →](Day01-Flink21-Fundamentals/Exercise-Solutions/HOW-TO-RUN-EXERCISES.md)**