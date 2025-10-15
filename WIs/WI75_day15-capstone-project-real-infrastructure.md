# WI75: Day15 Capstone Project - Real Infrastructure Conversion

**File**: `WIs/WI75_day15-capstone-project-real-infrastructure.md`
**Title**: [Day15] Convert Capstone Project to Real Kafka/Flink Infrastructure
**Description**: Convert all 4 Day15 Capstone Project exercises from templates to production-ready implementations using real LocalTesting infrastructure
**Priority**: High
**Component**: LearningCourse/Day15-Capstone-Project
**Type**: Feature - Real Infrastructure Conversion
**Assignee**: AI Agent
**Created**: 2025-01-14
**Status**: Investigation

## Lessons Applied from Previous WIs

### Previous WI References
- WI38-62: Day03-13 conversions (successful patterns established)
- WI39-42: Day04 Production Backpressure (Netflix-scale patterns)
- WI44-47: Day08 Stress Testing (comprehensive validation)
- WI52-55: Day13 Advanced Patterns (event sourcing, CQRS, saga, CEP)
- WI67-70: Day14 Advanced Testing (property-based, mutation, chaos)

### Lessons Applied
- **Environment Variable Service Discovery**: All exercises will use `KAFKA_BOOTSTRAP_SERVERS`, `KAFKA_FLINK_BOOTSTRAP_SERVERS`, `FLINK_GATEWAY_URL`
- **No Hardcoded Addresses**: Zero tolerance for `localhost:9092` or similar hardcoded values
- **Console Applications Only**: NO web services with `app.RunAsync()` - must complete and exit
- **Real Infrastructure Mandate**: NO in-memory simulations - all exercises use real Kafka/Flink
- **Completion Markers**: All exercises print "COMPLETED" or "SUCCESS" for test validation
- **Production Patterns**: Apply Netflix, Uber, Google-scale patterns from previous days

### Problems Prevented
- Avoiding template-only implementations without real functionality
- Preventing simulation/mock infrastructure usage
- Ensuring exercises demonstrate multi-domain integration with real event correlation
- Maintaining consolidated test structure (single LearningCourse.IntegrationTests assembly)

## Phase 1: Investigation

### Requirements

**Current State Analysis**:
- ✅ Day15 README.md exists with comprehensive architecture documentation
- ❌ Exercise151-154 are currently just templates (1-second delays, no real work)
- ❌ No integration with real Kafka/Flink infrastructure
- ❌ No Day15Tests.cs in consolidated test structure
- ⚠️ README describes complex multi-domain platform but exercises don't implement it

**Exercise Scope**:
1. **Exercise151**: System Architecture - Multi-domain platform design
2. **Exercise152**: Implementation - Domain engines (E-commerce + Financial)
3. **Exercise153**: Integration Testing - Cross-domain event correlation
4. **Exercise154**: Production Deployment - Complete platform validation

### Debug Information (MANDATORY)

**Investigation Findings** (2025-01-14):

**Existing Exercise Structure**:
```
Day15-Capstone-Project/
├── README.md (644 lines - comprehensive architecture documentation)
├── Exercise-Solutions/
│   ├── Exercise151/ (Template only - 37 lines)
│   ├── Exercise152/ (Template only - 37 lines)
│   ├── Exercise153/ (Template only - 40 lines)
│   └── Exercise154/ (Template only - 40 lines)
└── Day15Tutorial.sln (exists)
```

**README Analysis**:
- Documents 4 domain engines: E-commerce, Financial, IoT Manufacturing, Social Media
- Describes cross-domain integration hub and event correlation
- Includes comprehensive testing strategy with NUnit test examples
- Defines performance targets: 1M+ events/sec, <100ms P99 latency, 99.9% uptime
- Specifies graduation requirements and assessment criteria

**Current Exercise Implementations**:
- All 4 exercises are basic templates with `Task.Delay(1000)` simulations
- No real Kafka producers/consumers
- No Flink job submissions
- No cross-domain event correlation
- No infrastructure connectivity

**Integration Test Status**:
- ❌ No Day15Tests.cs file exists in LearningCourse.IntegrationTests/
- ❌ No project references to Exercise151-154 in LearningCourse.IntegrationTests.csproj
- ⚠️ Day15 is not included in consolidated test structure yet

### Findings

**Conversion Scope**:

Given the complexity of a full 4-domain platform (E-commerce, Financial, IoT, Social Media) and the 27-hour estimate, I propose a **focused, achievable implementation** that demonstrates core concepts without overwhelming scope:

**Recommended Approach - Simplified Multi-Domain Platform**:

1. **Exercise151: Platform Architecture Setup**
   - Deploy real Kafka topics for 2 domains (E-commerce + Financial)
   - Configure Flink cluster connectivity
   - Set up shared infrastructure (Redis for state, Kafka for events)
   - Validate infrastructure health checks
   - Output: Architecture validation report

2. **Exercise152: Domain Implementation**
   - **E-commerce Domain**: 
     - Real-time inventory tracking (Kafka → Flink → Redis)
     - Product recommendation engine (user interactions → ML scoring → recommendations)
   - **Financial Domain**:
     - Fraud detection (transactions → anomaly detection → alerts)
     - Risk scoring (transaction patterns → risk calculation → actions)
   - Each domain uses real Kafka topics and Flink jobs

3. **Exercise153: Cross-Domain Integration**
   - Event correlation hub (listen to both E-commerce and Financial events)
   - Integrated insights generation (e.g., high-risk customer + low inventory = special handling)
   - Unified monitoring dashboard data collection
   - Real-time correlation validation

4. **Exercise154: Production Deployment Validation**
   - End-to-end flow testing (generate events → process through domains → validate outputs)
   - Performance benchmarking (throughput, latency measurements)
   - Chaos resilience testing (task manager failure simulation)
   - Comprehensive health checks and operational readiness report

**Key Design Decisions**:
- Focus on 2 domains (E-commerce + Financial) for depth over breadth
- Real Kafka topics for all inter-domain communication
- Real Flink jobs for all stream processing
- Shared Redis for cross-domain state
- Console applications that execute workflows and exit (not long-running services)
- Comprehensive validation and reporting in each exercise

### Lessons Learned

**Design Philosophy**:
- Capstone should demonstrate **integration mastery**, not just feature breadth
- Better to implement 2 domains thoroughly than 4 domains superficially
- Real infrastructure validation is more valuable than extensive simulation
- Cross-domain correlation is the key learning objective
- Production readiness focus: health checks, metrics, error handling

## Phase 2: Design

### Requirements

**Architecture Design**:

**Exercise151: Platform Architecture Setup**
```csharp
// Infrastructure validation and setup
public class PlatformArchitectureValidator
{
    // 1. Validate Kafka cluster (create topics for domains)
    // 2. Validate Flink cluster (submit test job)
    // 3. Validate Redis (state storage)
    // 4. Create domain-specific Kafka topics:
    //    - ecommerce-inventory-events
    //    - ecommerce-user-interactions
    //    - financial-transactions
    //    - financial-fraud-alerts
    //    - cross-domain-correlations
    //    - integrated-insights
}
```

**Exercise152: Domain Implementation**
```csharp
// E-commerce Domain
public class EcommerceDomainEngine
{
    // Inventory Stream: Kafka → Flink → Redis
    // - Read inventory events from Kafka
    // - Process with Flink stateful operator
    // - Store current state in Redis
    // - Emit alerts to Kafka topic
    
    // Recommendation Stream: Kafka → Flink ML → Kafka
    // - Read user interactions from Kafka
    // - Simple ML scoring (product affinity)
    // - Write recommendations to Kafka
}

// Financial Domain
public class FinancialDomainEngine
{
    // Fraud Detection Stream: Kafka → Flink → Kafka
    // - Read transactions from Kafka
    // - Anomaly detection (rule-based)
    // - High-risk transactions to alerts topic
    
    // Risk Scoring Stream: Kafka → Flink → Redis
    // - Transaction pattern analysis
    // - Risk score calculation
    // - Store in Redis for quick lookup
}
```

**Exercise153: Cross-Domain Integration**
```csharp
// Event Correlation Hub
public class CrossDomainCorrelationHub
{
    // Listen to multiple domain event topics
    // Correlate events by:
    // - Customer ID
    // - Product ID
    // - Time windows
    
    // Generate integrated insights:
    // - High-risk customer + inventory event
    // - Fraud alert + transaction pattern
    // - Cross-domain anomalies
}
```

**Exercise154: Production Deployment Validation**
```csharp
// End-to-end validation suite
public class ProductionDeploymentValidator
{
    // 1. Generate realistic test data
    // 2. Process through all domains
    // 3. Validate cross-domain correlations
    // 4. Performance benchmarks
    // 5. Chaos resilience test
    // 6. Generate operational report
}
```

### Architecture Decisions

**Technology Stack**:
- **Stream Processing**: FlinkDotNet with real Flink cluster
- **Message Broker**: Kafka (3 brokers from LocalTesting)
- **State Storage**: Redis for cross-domain shared state
- **Service Discovery**: Environment variables (KAFKA_BOOTSTRAP_SERVERS, etc.)
- **Testing**: NUnit with consolidated test structure

**Topic Design**:
```
E-commerce Domain:
  - ecommerce-inventory-events (4 partitions)
  - ecommerce-user-interactions (8 partitions)
  - ecommerce-recommendations (4 partitions)

Financial Domain:
  - financial-transactions (8 partitions)
  - financial-fraud-alerts (2 partitions)
  - financial-risk-scores (4 partitions)

Cross-Domain:
  - domain-events (8 partitions) - all domains publish here
  - integrated-insights (4 partitions) - correlation results
```

**Flink Job Architecture**:
```
E-commerce Jobs:
  1. inventory-processor (parallelism=4)
  2. recommendation-engine (parallelism=4)

Financial Jobs:
  3. fraud-detector (parallelism=4)
  4. risk-scorer (parallelism=4)

Integration Jobs:
  5. event-correlator (parallelism=8)
  6. insight-generator (parallelism=4)
```

**State Management**:
- Flink managed state for per-key processing
- Redis for cross-domain shared state
- Checkpointing for fault tolerance

### Why This Approach

**Rationale**:
1. **Focused Scope**: 2 domains allow deep implementation vs shallow 4-domain coverage
2. **Real Infrastructure**: All processing uses actual Kafka/Flink, no simulation
3. **Cross-Domain Focus**: Emphasis on integration and correlation (key capstone concept)
4. **Production Patterns**: Health checks, metrics, error handling from real-world systems
5. **Demonstrable Value**: Each exercise produces measurable, validatable results
6. **Manageable Complexity**: Achievable within reasonable timeframe while maintaining quality

**Alternatives Considered**:
- ❌ 4-domain full implementation: Too complex, would compromise quality
- ❌ Single domain deep dive: Misses cross-domain integration learning objective
- ❌ Simulation-based: Violates user requirement "no simulation, only real LocalTesting"
- ✅ 2-domain focused implementation: Best balance of depth, integration, and achievability

## Phase 3: TDD/BDD

### Test Specifications

**Day15Tests.cs Structure**:
```csharp
[TestFixture]
[Category("day15-capstone-project")]
[Category("integration")]
public class Day15Tests : LearningCourseTestBase
{
    [Test]
    public async Task Exercise151_PlatformArchitecture_ShouldValidateInfrastructure()
    {
        // Validate Kafka topics created
        // Validate Flink cluster healthy
        // Validate Redis connectivity
        // Validate architecture report generated
    }
    
    [Test]
    public async Task Exercise152_DomainImplementation_ShouldProcessDomainEvents()
    {
        // Validate E-commerce inventory processing
        // Validate E-commerce recommendations
        // Validate Financial fraud detection
        // Validate Financial risk scoring
    }
    
    [Test]
    public async Task Exercise153_CrossDomainIntegration_ShouldCorrelateEvents()
    {
        // Validate event correlation
        // Validate integrated insights generation
        // Validate cross-domain state sharing
    }
    
    [Test]
    public async Task Exercise154_ProductionDeployment_ShouldMeetRequirements()
    {
        // Validate end-to-end processing
        // Validate performance benchmarks
        // Validate chaos resilience
        // Validate operational readiness
    }
}
```

### Behavior Definitions

**Exercise151 Expected Behavior**:
- ✅ Creates all required Kafka topics
- ✅ Validates Flink cluster connectivity
- ✅ Validates Redis connectivity
- ✅ Outputs architecture validation report
- ✅ Completes in < 30 seconds

**Exercise152 Expected Behavior**:
- ✅ Submits 4 Flink jobs (2 per domain)
- ✅ Processes events through E-commerce domain
- ✅ Processes events through Financial domain
- ✅ Stores state in Redis
- ✅ Emits results to Kafka topics
- ✅ Completes in < 2 minutes

**Exercise153 Expected Behavior**:
- ✅ Correlates events from both domains
- ✅ Generates integrated insights
- ✅ Validates cross-domain patterns
- ✅ Outputs correlation statistics
- ✅ Completes in < 2 minutes

**Exercise154 Expected Behavior**:
- ✅ Runs end-to-end validation
- ✅ Reports performance metrics
- ✅ Validates chaos resilience
- ✅ Generates operational report
- ✅ Completes in < 3 minutes

## Phase 4: Implementation

### Code Changes

**Status**: Not Started

**Files to Create**:
1. Exercise151/Program.cs - Platform architecture validation
2. Exercise152/EcommerceDomainEngine.cs - E-commerce domain implementation
3. Exercise152/FinancialDomainEngine.cs - Financial domain implementation
4. Exercise153/CrossDomainCorrelationHub.cs - Event correlation
5. Exercise154/ProductionDeploymentValidator.cs - Comprehensive validation
6. LearningCourse.IntegrationTests/Day15Tests.cs - Consolidated tests

**Files to Modify**:
1. Exercise151/Exercise151.csproj - Add FlinkDotNet, Kafka, Redis dependencies
2. Exercise152/Exercise152.csproj - Add domain-specific dependencies
3. Exercise153/Exercise153.csproj - Add correlation dependencies
4. Exercise154/Exercise154.csproj - Add validation dependencies
5. LearningCourse.IntegrationTests/LearningCourse.IntegrationTests.csproj - Add Exercise151-154 references

### Challenges Encountered

**Anticipated Challenges**:
1. Complex multi-domain coordination
2. Cross-domain event correlation logic
3. Performance benchmarking accuracy
4. Chaos testing reliability
5. Comprehensive validation coverage

### Solutions Applied

**Mitigation Strategies**:
1. Start with simple domain implementations, add complexity incrementally
2. Use clear event schemas with correlation IDs
3. Use system diagnostics for accurate performance measurement
4. Use Polly for controlled chaos injection
5. Create comprehensive validation checklists

## Phase 5: Testing & Validation

### Test Results

**Status**: Not Started

### Performance Metrics

**Target Metrics**:
- Throughput: 10K+ events/second (realistic for local testing)
- Latency: <100ms P99
- Availability: 99%+ during chaos testing
- Data consistency: 100% (exactly-once processing)

## Phase 6: Owner Acceptance

### Demonstration

**Status**: Not Started

### Owner Feedback

**Status**: Pending

### Final Approval

**Status**: Pending

## Lessons Learned & Future Reference (MANDATORY)

### What Worked Well

**Status**: To be documented after completion

### What Could Be Improved

**Status**: To be documented after completion

### Key Insights for Similar Tasks

**Status**: To be documented after completion

### Specific Problems to Avoid in Future

**Status**: To be documented after completion

### Reference for Future WIs

**Status**: To be documented after completion

---

**Next Steps**:
1. Begin implementation of Exercise151 (Platform Architecture)
2. Validate infrastructure connectivity patterns
3. Create reusable domain engine base classes
4. Implement E-commerce domain
5. Implement Financial domain
6. Build cross-domain correlation hub
7. Create comprehensive validation suite
8. Add consolidated integration tests
9. Update README with real implementation details
10. Complete performance validation and documentation