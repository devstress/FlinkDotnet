# WI11: Debug and Fix Kafka-TaskManager Connectivity Issues

**File**: `LocalTesting/WIs/WI11_kafka-taskmanager-connectivity-debug.md`
**Title**: Debug root cause why TaskManagers fail to connect with Kafka in LocalTesting integration tests
**Description**: Investigate and fix the root cause of TaskManager-Kafka connectivity failures. Debug containers to understand the issue, fix containers first to prove working, then adjust Aspire project and tests.
**Priority**: High
**Component**: LocalTesting.IntegrationTests
**Type**: Bug Fix
**Assignee**: GitHub Copilot
**Created**: 2025-01-28
**Status**: Investigation

## Lessons Applied from Previous WIs

### Previous WI References
- WI2_aspire-dcp-networking-fix.md - Kafka networking configuration (ports 9092 vs 9093)
- WI6_kafka-connectivity-fix.md - Previous Kafka connectivity investigation
- WI10_fix-integration-test-failures.md - Recent integration test failures investigation

### Lessons Applied
- **Debug-first approach**: Must debug containers and gather evidence before proposing solutions
- **Container inspection**: Check actual container status, logs, and network configuration
- **Kafka port configuration**: Understanding of kafka:9092 (internal) vs localhost:9093 (external)
- **Previous attempts**: WI2 identified kafka:9093 for container-to-container, WI10 tried kafka:9092
- **Evidence-based fixes**: Prove containers work standalone before adjusting Aspire

### Problems Prevented
- Making code changes without understanding actual root cause
- Not debugging live containers to see what's actually happening
- Repeating previous failed approaches without new evidence

## Phase 1: Investigation

### Requirements
- Debug why TaskManagers cannot connect to Kafka
- Examine actual container logs and network configuration
- Identify the real root cause with evidence
- Fix containers first to prove they can work
- Then adjust Aspire project and tests to match working configuration

### Debug Information (MANDATORY - Update this section for every investigation)

**Problem Statement from User**:
- "TaskManagers fail to connect with Kafka"
- "Please debug containers to see what is the root cause"
- "Fix the containers first to prove it is working"
- "Then adjust the aspire project and the tests"

**Environment**:
- .NET: 9.0.305 ✅ Installed
- Docker: 28.0.4 ✅ Installed
- No containers currently running (clean slate)

**Historical Context**:
- WI2: Changed to kafka:9093 for container-to-container (based on Aspire source)
- WI10: Changed back to kafka:9092, but tests still failed
- Current Ports.cs: KafkaContainerBootstrap = "kafka:9092"
- Previous investigations found jobs submit and reach RUNNING but process 0 messages

**Next Steps**:
1. ✅ Check .NET version and environment
2. ⏳ Run integration tests to reproduce the failures
3. ⏳ Examine container logs during test execution
4. ⏳ Debug container networking and Kafka connectivity
5. ⏳ Identify root cause with concrete evidence
6. ⏳ Fix containers to prove connectivity works
7. ⏳ Adjust Aspire project and tests accordingly

### Findings

**CRITICAL DISCOVERY - Root Cause Identified**:

1. ✅ **Containers are running**: All containers start successfully (Kafka, JobManager, TaskManager, SQL Gateway)
2. ✅ **Kafka has correct listeners**:
   - `PLAINTEXT_HOST://0.0.0.0:9092` advertised as `localhost:42859` (dynamic host port)
   - `PLAINTEXT_INTERNAL://0.0.0.0:9093` advertised as `kafka:9093` (container network)
3. ❌ **TaskManager CANNOT resolve hostname 'kafka'**:
   - Test: `bash -c '</dev/tcp/kafka/9093'` → FAILED: "Name or service not known"
   - Test: `bash -c '</dev/tcp/172.17.0.2/9093'` → SUCCESS (Kafka IP works)
4. ❌ **Network Configuration Issue**:
   - Both containers on default `bridge` network
   - Docker default bridge does NOT provide DNS resolution between containers
   - Containers can reach each other by IP but NOT by hostname

**Evidence**:
```bash
# Kafka container environment
KAFKA_ADVERTISED_LISTENERS=PLAINTEXT://localhost:29092,PLAINTEXT_HOST://localhost:42859,PLAINTEXT_INTERNAL://kafka:9093

# Network inspection
docker network ls → Only default networks (bridge, host, none)
docker inspect taskmanager → Networks: map[bridge:...]
docker inspect kafka → Networks: map[bridge:...], IP: 172.17.0.2

# Connectivity tests from TaskManager
</dev/tcp/kafka/9093 → FAILED: "Name or service not known"
</dev/tcp/172.17.0.2/9093 → SUCCESS
```

**Root Cause**:
Aspire DCP creates containers on Docker's default bridge network, which does NOT provide DNS resolution between containers. The Kafka advertised listener `kafka:9093` cannot be resolved by Flink containers, causing all jobs to fail when trying to connect to Kafka.

**Historical Context Clarification**:
- WI2 investigated this but incorrectly concluded the solution was using port 9093
- WI10 tried port 9092 but didn't identify the DNS resolution issue
- The real problem is NOT the port number but the inability to resolve the `kafka` hostname

## Phase 2: Design

### Requirements
- Fix DNS resolution between containers so Flink can resolve `kafka` hostname
- Maintain compatibility with both Docker and Podman
- Minimal changes to Aspire configuration
- All containers must be on the same custom network with DNS enabled

### Architecture Decision

**Solution**: Use Docker custom network with DNS resolution

**Approach**:
1. Create a custom Docker network programmatically (or use an existing one)
2. Add `--network` argument to all container runtime args using `WithContainerRuntimeArgs()`
3. Network must be created before any containers start
4. All containers (Kafka, JobManager, TaskManager, SQL Gateway) must join the same network

**Implementation Strategy**:
- Create network in Program.cs before building containers
- Use `WithContainerRuntimeArgs("--network", "aspire-flink-network")` for all containers
- Network name: `aspire-flink-network` (descriptive and unique)
- Check if network exists, create if not

### Why This Approach
- ✅ **Minimal changes**: Only add network args to existing containers
- ✅ **Proven to work**: Tested with docker-compose, DNS resolution confirmed
- ✅ **Compatible**: Works with both Docker and Podman
- ✅ **No Kafka config changes**: Kafka advertised listener `kafka:9093` will work
- ✅ **No port changes**: Keeps existing port configuration

### Proof of Concept Results
```bash
# With custom network (docker-compose test):
docker exec test-taskmanager getent ahosts kafka
→ 172.19.0.2 STREAM kafka  ✅ DNS WORKS

docker exec test-taskmanager bash -c '</dev/tcp/kafka/9093'
→ SUCCESS  ✅ CONNECTIVITY WORKS

# Without custom network (default bridge):
docker exec flink-taskmanager bash -c '</dev/tcp/kafka/9093'
→ FAILED: "Name or service not known"  ❌ NO DNS
```

### Alternatives Considered
1. **Use container IP addresses instead of hostnames**: Rejected - IPs are dynamic
2. **Modify /etc/hosts in containers**: Rejected - requires container modification
3. **Use host.docker.internal**: Rejected - requires routing through host, adds latency
4. **Change Kafka advertised listeners**: Rejected - would affect all clients
5. **Use Aspire service references**: Rejected - only works for Aspire-managed services, not for Kafka hostname resolution

### Changes Required
**File**: `LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs`
- Add network creation logic before builder.AddKafka()
- Add `.WithContainerRuntimeArgs("--network", "aspire-flink-network")` to all containers:
  - Kafka
  - JobManager  
  - TaskManager
  - SQL Gateway

## Phase 3: TDD/BDD
(To be completed after design)

## Phase 4: Implementation
(To be completed after testing)

## Phase 5: Testing & Validation
(To be completed after implementation)

## Phase 6: Owner Acceptance
(To be completed after validation)

## Lessons Learned & Future Reference (MANDATORY)
(To be completed at end of WI)
