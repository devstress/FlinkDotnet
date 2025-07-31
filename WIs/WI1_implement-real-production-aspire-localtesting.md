# WI1: Implement Real Production Environment in Aspire for LocalTesting

**File**: `WIs/WI1_implement-real-production-aspire-localtesting.md`
**Title**: [LocalTesting] Implement real production environment in Aspire for LocalTesting
**Description**: Replace simulated/mocked services with real connections to Kafka, Flink, and implement proper service health checks, security token management, and performance testing capabilities
**Priority**: High
**Component**: LocalTesting Aspire Environment
**Type**: Feature Enhancement
**Assignee**: AI Agent
**Created**: 2024-12-19
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- No previous WIs found in this repository
### Lessons Applied  
- N/A - First WI in this repository
### Problems Prevented
- N/A - First WI in this repository

## Phase 1: Investigation
### Requirements
From the issue description, need to address:
1. **Step 1**: Cannot see it ping the services to make sure all working
2. **Step 2**: What is security token service? Where is your Flink Job? If Flink Job cannot do this complex, update wiki and implementation to use temporal for durable execution
3. **Step 3**: SwaggerResponse should show { Status = status, Metrics = metrics } not verbose messages. Rate Limiter should talk with real endpoint which is Kafka
4. **Step 4**: Verify 3 kafka brokers having 1 million messages. Correlation.id should be added using Flink before sending to http endpoint
5. **Step 5**: Real Flink cluster with Aspire + temporal setup. When to use Flink vs Temporal explanation needed

### Debug Information (MANDATORY - Update this section for every investigation)
- **Error Messages**: No explicit errors, but services are simulated rather than connected to real infrastructure
- **Log Locations**: Services use ILogger but connections are mocked
- **System State**: Aspire orchestrates containers but WebAPI doesn't connect to them
- **Reproduction Steps**: Start LocalTesting.AppHost, then WebAPI - services respond but with simulated data
- **Evidence**: 
  - `FlinkJobManagementService.cs` line 44: "Simulating Flink job submission"
  - `BackpressureMonitoringService.cs` line 64: "Simulate consumer lag monitoring"
  - `SecurityTokenManagerService.cs`: Uses in-memory variables instead of real token service
  - No actual health checks to verify Aspire services are running

### Current State Analysis
**Infrastructure (Aspire AppHost)**:
- ✅ 3 Kafka brokers in KRaft mode (production-like)
- ✅ Apache Flink cluster (JobManager + 3 TaskManagers) 
- ✅ Redis, OpenTelemetry, Prometheus, Grafana stack
- ✅ Kafka UI and Flink SQL Gateway

**WebAPI Services**:
- ❌ KafkaProducerService: Uses real Kafka client but produces to topic without verifying broker health
- ❌ FlinkJobManagementService: Completely simulated, doesn't connect to real Flink cluster
- ❌ BackpressureMonitoringService: Simulates consumer lag instead of monitoring real Kafka lag
- ❌ SecurityTokenManagerService: In-memory simulation, no real token service
- ❌ ComplexLogicStressTestService: Orchestrates simulated services
- ❌ No service health checks to verify Aspire infrastructure is running

### Research: Flink vs Temporal Decision Matrix
**Apache Flink Strengths:**
- Stream processing with low latency (milliseconds)
- Built-in exactly-once processing guarantees
- Native Kafka integration for high throughput
- Stateful stream processing with checkpointing
- Complex event processing and windowing
- SQL queries on streaming data

**Temporal Strengths:**
- Durable execution with automatic retries and timeouts
- Complex workflow orchestration across services
- Long-running processes (hours/days/months)
- Visual workflow tracking and debugging
- Multi-language support (.NET, Java, Go, Python)
- Distributed transactions and saga patterns

**Decision for Each Requirement:**
1. **Security Token Renewal (every 10,000 messages)**: 
   - **Recommendation**: **Temporal** - This is a stateful, long-running workflow that needs durability and retry logic
   - **Why not Flink**: While Flink can count messages, token renewal is a external service call that benefits from Temporal's durable execution

2. **Correlation ID Addition**:
   - **Recommendation**: **Flink** - This is a real-time stream transformation that Flink excels at
   - **Why not Temporal**: This is a stateless transformation on every message, perfect for stream processing

3. **Message Batching and Splitting**:
   - **Recommendation**: **Flink** - Native stream processing windowing and grouping capabilities
   - **Why not Temporal**: Real-time batching is a streaming operation

4. **HTTP Endpoint Processing**:
   - **Recommendation**: **Temporal** - External HTTP calls benefit from durable execution, retries, and error handling
   - **Why not Flink**: While Flink can make HTTP calls, Temporal provides better resilience for external integrations

### Architecture Decision
**Hybrid Approach**: Use both Flink and Temporal for their strengths:
- **Flink Jobs**: Real-time stream processing (correlation IDs, transformations, batching)
- **Temporal Workflows**: Durable operations (token renewal, HTTP processing, error recovery)

### Findings
1. **Missing Real Service Connectivity**: All services need actual connections to Aspire infrastructure
2. **API Response Format**: Need to standardize to `{Status, Metrics}` format
3. **Performance Testing**: Need real 1M message test with timing measurements
4. **Service Health Checks**: Need to verify all Aspire services are running before operations
5. **Flink Integration**: Need real Flink job deployment and management
6. **Temporal Integration**: Need Temporal server for durable execution workflows

### Lessons Learned
- Aspire provides excellent infrastructure orchestration but requires explicit service integration
- Simulation is useful for development but production readiness requires real connections
- Hybrid Flink+Temporal architecture leverages strengths of both platforms

## Phase 2: Design  
### Requirements
- Implement real service health checks for all Aspire services
- Replace simulated Flink service with real Flink REST API integration
- Add Temporal server to Aspire and implement token renewal workflow
- Implement real Kafka consumer lag monitoring for backpressure
- Standardize API responses to {Status, Metrics} format
- Create 1M message performance test with real Kafka brokers

### Architecture Decisions
**Service Health Check Service**: New service to ping all Aspire infrastructure
**Real Flink Integration**: Use Flink REST API to submit actual jobs
**Temporal Integration**: Add Temporal server container and .NET SDK
**Kafka Lag Monitoring**: Use Confluent.Kafka AdminClient to get real consumer lag
**Response Format**: Standardize all endpoints to return `{Status: string, Metrics: object}`

### Why This Approach
- Health checks ensure environment readiness before testing
- Real Flink integration enables actual stream processing jobs
- Temporal handles complex durable workflows better than custom retry logic
- Kafka AdminClient provides accurate lag monitoring for backpressure
- Consistent response format improves API usability

### Alternatives Considered
- **Full Simulation**: Rejected - doesn't meet production requirements
- **Flink-Only**: Rejected - doesn't handle durable execution well
- **Temporal-Only**: Rejected - not optimal for real-time stream processing

## Phase 3: TDD/BDD
### Test Specifications
- Health check tests for each Aspire service connectivity
- Real Kafka producer/consumer integration tests
- Flink job submission and monitoring tests
- Temporal workflow execution tests
- 1M message performance tests
- API response format compliance tests

### Behavior Definitions
- Given Aspire services are running, When health check is called, Then all services should be reachable
- Given Kafka cluster is healthy, When 1M messages are produced, Then all messages should be persisted with correlation IDs
- Given Flink cluster is running, When job is submitted, Then job should be deployed and return real metrics
- Given Temporal is configured, When token renewal is needed, Then workflow should execute and renew token

## Phase 4: Implementation
### Code Changes
**Step 1 Completed - Real Service Health Checks:**
- ✅ Created `AspireHealthCheckService.cs` with real connectivity checks for all Aspire services
- ✅ Updated controller constructor and Step 1 endpoint to use real health checks
- ✅ Fixed API response format to return `{Status, Metrics}` instead of verbose objects
- ✅ Implemented real Kafka broker health check using AdminClient
- ✅ Implemented Redis connectivity verification with ping test
- ✅ Implemented Flink JobManager, TaskManagers, and SQL Gateway health checks via REST API
- ✅ Implemented monitoring services health checks (Prometheus, Grafana, Kafka UI)

**Step 3 Partially Completed - Backpressure Improvements:**
- ✅ Updated backpressure monitoring to use real Kafka connectivity verification
- ✅ Fixed API response format for backpressure status endpoint
- ✅ Implemented `RealKafkaConsumerLagMonitor` that connects to actual Kafka cluster

### Challenges Encountered
- Confluent.Kafka API differences - some AdminClient methods work differently than expected
- Redis INFO command returns complex data structure, simplified to connection verification
- Need to balance real connectivity checks with performance (avoiding timeouts)

### Solutions Applied
- Used AdminClient.GetMetadata() for Kafka broker verification
- Simplified Redis health check to ping + connection status
- Used Flink REST API for real cluster status instead of simulation
- Implemented timeout-resistant health checks with proper error handling

## Phase 5: Testing & Validation
### Test Results
*[Will be filled during testing]*

### Performance Metrics
*[Will be filled during testing]*

## Phase 6: Owner Acceptance
### Demonstration
*[Will be filled during demonstration]*

### Owner Feedback
*[Will be filled after feedback]*

### Final Approval
*[Will be filled after approval]*

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
*[Will be filled at completion]*

### What Could Be Improved  
*[Will be filled at completion]*

### Key Insights for Similar Tasks
*[Will be filled at completion]*

### Specific Problems to Avoid in Future
*[Will be filled at completion]*

### Reference for Future WIs
*[Will be filled at completion]*