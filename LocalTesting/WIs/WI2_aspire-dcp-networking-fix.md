# WI2: Aspire DCP Container Networking for Kafka Connectivity

**File**: `LocalTesting/WIs/WI2_aspire-dcp-networking-fix.md`
**Title**: [LocalTesting] Fix Flink containers' inability to reach Kafka in Aspire DCP
**Description**: Flink jobs reach RUNNING state but process 0 messages because Flink containers cannot resolve Kafka hostname `kafka:9092` in Aspire DCP environment.
**Priority**: High
**Component**: LocalTesting Infrastructure
**Type**: Bug Fix
**Assignee**: AI Debug Agent
**Created**: 2025-10-02T16:08:00Z
**Status**: Investigation Complete - Root Cause Identified

## Phase 1: Investigation

### Debug Information (MANDATORY)
- **Error Pattern**: Jobs reach RUNNING state successfully, but consume/produce 0 messages
- **Symptoms**: 
  - Both Pure Flink and Gateway jobs show identical behavior
  - Test code can reach Kafka at `localhost:dynamicPort`
  - Flink containers cannot reach Kafka at `kafka:9092`
- **Environment**: Aspire DCP (Docker Compose Provider) for testing
- **Reproduced With**: GatewayVsPureFlinkDiagnosticTest showing consistent 0/5 messages processed

### Root Cause Analysis
**CONFIRMED ROOT CAUSE**: Aspire DCP creates Docker containers on the default `bridge` network without DNS service discovery between containers. Unlike docker-compose which creates custom networks with automatic DNS resolution, Aspire DCP's default bridge network requires explicit container linking or IP addresses for inter-container communication.

**Evidence**:
1. `docker network ls` shows only default networks (bridge, host, none)
2. Aspire `.WaitFor()` creates startup dependencies but NOT network connectivity
3. Kafka's "internal" endpoint is designed for service-to-service references within Aspire's orchestration layer, not for raw container networking
4. `GetEndpoint("internal")` returns an `EndpointReference` object (not usable as environment variable string)

### Attempted Solutions That Failed
1. ❌ Added `.WaitFor(kafka)` to Flink containers - Creates dependency but not DNS resolution
2. ❌ Tried `.WithEnvironment("KAFKA_BOOTSTRAP", kafka.GetEndpoint("internal"))` - Type mismatch (EndpointReference vs string)
3. ❌ Attempted `.WithEndpoint(port: 9092, targetPort: 9092, name: "internal")` - Conflicts with Kafka's auto-created endpoint

### Why This Is Hard
- Aspire DCP is designed for orchestrating services, not raw container networking
- Docker bridge network doesn't provide DNS between containers by default
- Aspire's endpoint references are for service discovery within its own abstraction layer
- Cannot force Aspire to use docker-compose networking model

## Phase 2: Design

### Solution Options Evaluated

#### Option A: Use Host Network Mode (NOT RECOMMENDED)
- Use `--network=host` for Flink containers
- **Pros**: Containers can reach localhost services
- **Cons**: Breaks container isolation, port conflicts likely, not cross-platform

#### Option B: Custom Docker Network (COMPLEX)
- Create custom Docker network before Aspire starts
- Configure all containers to use it
- **Pros**: Proper DNS resolution
- **Cons**: Requires pre-test setup, conflicts with Aspire's network management

#### Option C: Use Kafka's External Port with host.docker.internal (RECOMMENDED)
- Configure Flink jobs to use `host.docker.internal:{externalPort}` instead of `kafka:9092`
- **Pros**: Works with Aspire's port allocation, no custom networks needed
- **Cons**: Requires dynamic port discovery, different config for container vs host

#### Option D: Switch to Docker Compose for Testing (NUCLEAR OPTION)
- Abandon Aspire testing, use raw docker-compose
- **Pros**: Full control over networking
- **Cons**: Loses Aspire integration benefits, major rewrite

### Selected Approach: Option C with Dynamic Configuration
Use Aspire's connection string discovery to get the actual Kafka external port, then inject `host.docker.internal:{port}` into Flink containers' job definitions.

## Phase 3: Implementation Plan

### Changes Required
1. Modify `Ports.cs` to support dynamic Kafka port discovery
2. Update `LocalTestingTestBase` to inject actual Kafka connection string into test environment
3. Modify job submission to use `host.docker.internal:{dynamicPort}` for container-based Flink jobs
4. Keep test code using `localhost:{dynamicPort}` as it runs on host

### Implementation Steps
1. Capture Kafka's external port from Aspire at test startup
2. Store both host connection string (`localhost:port`) and container connection string (`host.docker.internal:port`)
3. Update `KafkaContainerConnectionString` to return the dynamic value
4. Test with diagnostic test to confirm message processing works

## Phase 4: Testing & Validation
- Run GatewayVsPureFlinkDiagnosticTest
- Verify jobs process all 5 messages successfully
- Confirm both Pure Flink and Gateway approaches work

## Lessons Learned & Future Reference

### Key Insights
- **Aspire DCP != Docker Compose**: Different networking models, cannot assume service discovery
- **Container vs Host Networking**: Always maintain separate connection strings for each context
- **Endpoint References**: Aspire's `EndpointReference` is not a string, cannot be used directly as env var
- **Test Infrastructure Complexity**: Integration tests require careful handling of network boundaries

### For Future Work
- Always verify container-to-container connectivity in Aspire tests
- Document network topology expectations clearly
- Consider docker-compose for pure container integration tests
- Aspire is best for service orchestration, not raw container networking

## ✅ SOLUTION IMPLEMENTED

### Root Cause Confirmed
Aspire's Kafka container has **TWO internal listeners**:
1. **PLAINTEXT_HOST** on port **9092**: For external access from host machine
2. **PLAINTEXT_INTERNAL** on port **9093**: For container-to-container communication

Source: https://github.com/dotnet/aspire/blob/main/src/Aspire.Hosting.Kafka/KafkaBuilderExtensions.cs

### Fix Applied
Changed container-to-container Kafka bootstrap from `"kafka:9092"` → `"kafka:9093"`

**Files Modified**:
1. `LocalTesting/LocalTesting.FlinkSqlAppHost/Ports.cs` - Updated `KafkaContainerBootstrap` constant
2. `LocalTesting/LocalTesting.IntegrationTests/LocalTestingTestBase.cs` - Updated documentation
3. `LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs` - Removed incorrect `.WithEnvironment()` calls
4. `LocalTesting/LocalTesting.IntegrationTests/GatewayVsPureFlinkDiagnosticTest.cs` - Updated test to use port 9093

### Test Results
✅ **Gateway jobs**: 5/5 messages processed successfully using `kafka:9093`
✅ **Pure Flink jobs**: Now using `kafka:9093` (validated in diagnostic test)
✅ **Diagnostic test**: PASSED

### Lessons Learned
1. **Aspire DCP networking is NOT the same as docker-compose** - containers use bridge network but Kafka has specific listener configuration
2. **Always check Aspire's source code** for internal implementation details
3. **Port 9092 vs 9093**: 9092 is external listener, 9093 is internal listener for container-to-container
4. **`kafka.GetEndpoint("internal")` returns EndpointReference object**, not a string - cannot be used directly in environment variables

### Reference
- Aspire Kafka Implementation: https://github.com/dotnet/aspire/blob/main/src/Aspire.Hosting.Kafka/KafkaBuilderExtensions.cs
- Aspire Networking Docs: https://aka.ms/dotnet/aspire/networking
- Docker Bridge Network: https://docs.docker.com/network/bridge/
- Related: BackPressureExample works because it only has Kafka (no inter-container communication needed)
