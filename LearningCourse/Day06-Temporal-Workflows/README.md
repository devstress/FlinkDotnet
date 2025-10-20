# Day 6: Temporal Workflows - Real Infrastructure Integration

## 🎯 Learning Objectives

Master **Temporal workflow orchestration** with real LocalTesting infrastructure connections. No simulations - all exercises use production-ready Temporal server (temporalio/auto-setup:1.22.4) with PostgreSQL backend.

**Time:** 4-6 hours | **Difficulty:** Intermediate | **Infrastructure:** LocalTesting Temporal + PostgreSQL

---

## 🏗️ Architecture: Real Temporal Infrastructure

Your LocalTesting environment provides a **complete Temporal platform**:

| Component | Endpoint | Purpose |
|-----------|----------|---------|
| **Temporal Server** | `localhost:7233` (gRPC) | Workflow execution engine |
| **Temporal Web UI** | `http://localhost:8088` | Workflow monitoring dashboard |
| **PostgreSQL** | `localhost:5432` | Durable workflow state storage |
| **Worker Processes** | .NET Applications | Activity execution runtime |

### Infrastructure Validation

```bash
# Verify Temporal server health
curl -s http://localhost:7233/api/v1/cluster/health
# Expected: {"status":"SERVING"}

# Access Temporal Web UI
open http://localhost:8088
```

---

## 📚 Exercise Overview

All exercises use **real Temporal infrastructure** - no mocking, no simulation:

### Exercise 6.1: Basic Workflow Definition
**File:** [`Exercise-Solutions/Exercise61/Program.cs`](Exercise-Solutions/Exercise61/Program.cs)  
**Pattern:** OrderProcessingWorkflow with sequential activities  
**Concepts:** Workflow definition, activity execution, basic orchestration  
**Duration:** ~15 minutes

### Exercise 6.2: Activity Patterns with Retry Logic
**File:** [`Exercise-Solutions/Exercise62/Program.cs`](Exercise-Solutions/Exercise62/Program.cs)  
**Pattern:** PaymentRetryWorkflow with exponential backoff  
**Concepts:** RetryPolicy, non-retryable errors, activity timeout management  
**Duration:** ~20 minutes

### Exercise 6.3: Error Handling with Saga Pattern
**File:** [`Exercise-Solutions/Exercise63/Program.cs`](Exercise-Solutions/Exercise63/Program.cs)  
**Pattern:** BookingSagaWorkflow with compensation logic  
**Concepts:** Saga pattern, reverse-order compensation, distributed transactions  
**Duration:** ~25 minutes

### Exercise 6.4: Advanced Patterns - Signals & Queries
**File:** [`Exercise-Solutions/Exercise64/Program.cs`](Exercise-Solutions/Exercise64/Program.cs)  
**Pattern:** SupportTicketWorkflow with dynamic behavior  
**Concepts:** Workflow signals, queries, WaitCondition, external interaction  
**Duration:** ~20 minutes

---

## 🚀 Getting Started

### Prerequisites

1. **LocalTesting Running** with Temporal infrastructure:
   ```bash
   cd LocalTesting
   dotnet run --project LocalTesting.FlinkSqlAppHost
   ```

2. **Verify Temporal Connectivity**:
   ```bash
   # Check if Temporal is accessible
   curl http://localhost:7233/api/v1/cluster/health
   
   # Open Temporal Web UI
   open http://localhost:8088
   ```

### Environment Variables

All exercises use **service discovery** via environment variables:

```csharp
// Temporal endpoint discovery (automatically configured by LocalTesting)
var temporalEndpoint = Environment.GetEnvironmentVariable("TEMPORAL_ENDPOINT") 
    ?? "http://localhost:7233";
```

---

## 📝 Exercise Instructions

### Exercise 6.1: Basic Workflow Definition

**Objective:** Implement OrderProcessingWorkflow with sequential activity execution

```bash
cd LearningCourse/Day06-Temporal-Workflows/Exercise-Solutions/Exercise61
dotnet build
dotnet run
```

**What You'll Learn:**
- Creating workflow interfaces with `[Workflow]` attribute
- Implementing `[WorkflowRun]` methods
- Executing activities sequentially with `Workflow.ExecuteActivityAsync()`
- Using `worker.ExecuteAsync()` pattern for workflow execution

**Expected Output:**
```
Exercise 6.1: Basic Workflow Definition
========================================
Processing order: ORDER-001
  ✓ Validate order
  ✓ Process payment
  ✓ Create shipment
Order ORDER-001 completed successfully

3 workflows executed successfully
Exercise 6.1 completed successfully
```

**Monitor in Temporal UI:** http://localhost:8088 → View workflow execution history

---

### Exercise 6.2: Activity Patterns with Retry Logic

**Objective:** Implement PaymentRetryWorkflow demonstrating sophisticated retry patterns

```bash
cd LearningCourse/Day06-Temporal-Workflows/Exercise-Solutions/Exercise62
dotnet build
dotnet run
```

**What You'll Learn:**
- Configuring `RetryPolicy` with exponential backoff
- Setting `InitialInterval`, `MaximumInterval`, `BackoffCoefficient`
- Defining `NonRetryableErrorTypes` for business logic errors
- Handling temporary failures vs permanent failures

**Expected Output:**
```
Exercise 6.2: Activity Patterns & Retry Logic
=============================================
Payment PAY-001: Success (First attempt)
Payment PAY-002: Temporary Failure → Retry → Success
Payment PAY-003: Insufficient Funds (Non-retryable, immediate failure)

Retry patterns demonstrated successfully
Exercise 6.2 completed successfully
```

**Key Code Pattern:**
```csharp
var retryPolicy = new()
{
    InitialInterval = TimeSpan.FromSeconds(1),
    MaximumInterval = TimeSpan.FromSeconds(30),
    BackoffCoefficient = 2.0f,
    MaximumAttempts = 5,
    NonRetryableErrorTypes = ["InsufficientFundsException", "InvalidPaymentMethodException"]
};
```

---

### Exercise 6.3: Error Handling with Saga Pattern

**Objective:** Implement BookingSagaWorkflow with compensation for distributed transactions

```bash
cd LearningCourse/Day06-Temporal-Workflows/Exercise-Solutions/Exercise63
dotnet build
dotnet run
```

**What You'll Learn:**
- Implementing Saga pattern for multi-step distributed transactions
- Building compensation activity chain
- Executing compensation in **reverse order** on failure
- Handling partial failures in distributed systems

**Expected Output:**
```
Exercise 6.3: Error Handling & Saga Pattern
===========================================
Booking BOOK-001: ✓ Success (All steps completed)
Booking BOOK-002: ✗ Payment Failed → Compensating...
  Compensated: Cancel hotel reservation
  Compensated: Cancel flight reservation
Booking BOOK-003: ✗ Shipment Failed → Compensating...
  Compensated: Refund payment
  Compensated: Cancel flight reservation
  Compensated: Cancel hotel reservation

Saga pattern demonstrated: 1 success, 2 compensated
Exercise 6.3 completed successfully
```

**Saga Pattern Architecture:**
```
Step 1: Reserve Hotel    → Compensation: Cancel Hotel
Step 2: Reserve Flight   → Compensation: Cancel Flight
Step 3: Process Payment  → Compensation: Refund Payment
Step 4: Create Shipment  → Compensation: Cancel Shipment

On Failure: Execute compensations in REVERSE order (4 → 3 → 2 → 1)
```

---

### Exercise 6.4: Advanced Patterns - Signals & Queries

**Objective:** Implement SupportTicketWorkflow with signals and queries for external interaction

```bash
cd LearningCourse/Day06-Temporal-Workflows/Exercise-Solutions/Exercise64
dotnet build
dotnet run
```

**What You'll Learn:**
- Using `[WorkflowSignal]` to modify workflow state externally
- Using `[WorkflowQuery]` to inspect workflow state non-blocking
- Implementing `WaitConditionAsync()` for conditional waiting
- Handling dynamic workflow behavior based on external events

**Expected Output:**
```
Exercise 6.4: Advanced Workflow Patterns
========================================
Creating support ticket: TICKET-001

Workflow State Updates (via Signals):
  → Adding comment: "User reported issue"
  → Escalating priority: Normal → High
  → Adding comment: "Engineering team investigating"
  → Resolving ticket

Workflow State Queries:
  Status: Open → In Progress → Resolved
  Priority: Normal → High
  Comments: 3 total

Signals and queries demonstrated successfully
Exercise 6.4 completed successfully
```

**Signal vs Query:**
- **Signal:** Modifies workflow state (AddComment, UpdatePriority, ResolveTicket)
- **Query:** Reads workflow state non-blocking (GetStatus, GetHistory, GetPriority)

---

## 🧪 Running Integration Tests

All exercises have comprehensive integration tests:

```bash
cd LearningCourse

# Run all Day 6 tests
dotnet test IntegrationTests.sln --filter "Category=day06-temporal-workflows"

# Run specific exercise test
dotnet test IntegrationTests.sln --filter "FullyQualifiedName~Exercise61"
dotnet test IntegrationTests.sln --filter "FullyQualifiedName~Exercise62"
dotnet test IntegrationTests.sln --filter "FullyQualifiedName~Exercise63"
dotnet test IntegrationTests.sln --filter "FullyQualifiedName~Exercise64"
```

**Test Coverage:**
- ✅ Exercise 6.1: OrderProcessingWorkflow execution
- ✅ Exercise 6.2: Retry pattern validation
- ✅ Exercise 6.3: Saga compensation verification
- ✅ Exercise 6.4: Signals and queries testing

---

## 📊 Monitoring & Observability

### Temporal Web UI (http://localhost:8088)

Navigate to view:
- **Workflows** → See all executed workflows
- **Workflow History** → Complete execution trace
- **Activity Execution** → Individual activity details
- **Error Details** → Retry attempts and failures

### Key Metrics to Observe

1. **Workflow Duration:** Time from start to completion
2. **Activity Retry Count:** Number of retry attempts per activity
3. **Compensation Execution:** Steps executed during rollback
4. **Signal Processing:** External signal handling timeline

---

## 🎯 Learning Outcomes

After completing Day 6, you will understand:

### Core Concepts
- ✅ Temporal workflow architecture and execution model
- ✅ Activity patterns and retry policies
- ✅ Saga pattern for distributed transaction coordination
- ✅ Workflow state management with signals and queries

### Production Patterns
- ✅ Exponential backoff retry strategies
- ✅ Compensation logic for partial failures
- ✅ External workflow interaction patterns
- ✅ Durable workflow state persistence

### Integration Skills
- ✅ Real Temporal infrastructure connectivity
- ✅ Environment-based service discovery
- ✅ Production monitoring and debugging
- ✅ Enterprise workflow orchestration

---

## 🔗 References

### Temporal Documentation
- [Temporal .NET SDK Guide](https://docs.temporal.io/dev-guide/dotnet)
- [Workflow Patterns](https://docs.temporal.io/workflows)
- [Activity Best Practices](https://docs.temporal.io/activities)
- [Saga Pattern Implementation](https://docs.temporal.io/activities#compensation)

### Project Documentation
- [Flink vs Temporal Decision Guide](../../docs/flink-vs-temporal-decision-guide.md)
- [LocalTesting Infrastructure](../../LocalTesting/README.md)

---

## ⚠️ Important Notes

### When to Use Temporal vs Flink

**Use Temporal when:**
- ✅ Complex .NET business logic that cannot run in Flink JVM
- ✅ Workflows spanning hours, days, or requiring human interaction
- ✅ Multi-step processes with compensation/rollback needs
- ✅ Saga patterns for distributed transactions

**Use Flink when:**
- ✅ Real-time stream processing (millisecond latency)
- ✅ Simple transformations and filtering
- ✅ High-throughput data pipelines
- ✅ Stateless or simple stateful operations

### Recommended Architecture

```
Flink (Stream Processing)
    ↓ (Complex workflows trigger)
Temporal (Workflow Orchestration)
    ↓ (Results back to)
Flink (Continue Stream Processing)
```

---

## 🎉 Congratulations!

You've completed **Day 6: Temporal Workflows** with real infrastructure integration!

**Next Steps:**
- **Day 7:** Advanced Windows & Joins
- **Day 8:** Performance Testing & Optimization

---

## 🗺️ Course Navigation

**[← Day 5: Enterprise Observability](../Day05-Enterprise-Observability/)** | **[Course Overview](../README.md)** | **[Day 7: Advanced Windows & Joins →](../Day07-Advanced-Windows-Joins/)**

**Course Progress:** Day 6 of 15 Complete ✅
