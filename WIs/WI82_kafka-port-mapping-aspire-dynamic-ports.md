# WI82: Fix Kafka Port Mapping - Aspire Dynamic Ports vs Configured Ports

**File**: `WIs/WI82_kafka-port-mapping-aspire-dynamic-ports.md`
**Title**: [Kafka] Fix port mapping mismatch between Aspire dynamic allocation and test discovery
**Description**: Kafka container uses dynamic ports from Aspire, but tests try to connect using different dynamic ports. Our configured static ports in Ports.cs are completely ignored by Aspire's `.AddKafka()`.
**Priority**: CRITICAL
**Component**: LocalTesting Infrastructure / Kafka Integration
**Type**: Bug Fix
**Assignee**: AI Agent
**Created**: 2025-10-17
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI80: Kafka container missing investigation - learned about Aspire service discovery
- WI81: Kafka JMX metrics scraping - learned about Kafka port configuration

### Lessons Applied
- Aspire uses dynamic port allocation by default
- Docker port mappings can be discovered via `docker ps` or service discovery
- Container-to-container vs host-to-container addressing is critical

### Problems Prevented
- Will not assume static ports work with Aspire's `.AddKafka()`
- Will verify actual port mappings before attempting fixes

## Phase 1: Investigation

### Requirements
- Understand Aspire's `.AddKafka()` port allocation behavior
- Identify why configured ports (Ports.cs) are ignored
- Find the mismatch between container ports and test discovery ports

### Debug Information (MANDATORY - Update this section for every investigation)

#### Evidence from User
```
docker ps output:
kafka-cqadguza   127.0.0.1:38815->9092/tcp, 127.0.0.1:46313->9093/tcp

Test error:
Kafka not ready within 30 seconds. Attempted to connect to: 127.0.0.1:37963
```

**Analysis**:
- Container has port mappings: 38815→9092, 46313→9093
- Test tried to connect to: 37963 (different port entirely!)
- This indicates a race condition or stale port cache

#### Code Analysis

**File: LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs (Lines 70-90)**
```csharp
var kafkaBuilder = builder.AddKafka("kafka");
```
- Aspire's `.AddKafka()` uses DEFAULT dynamic port allocation
- NO port configuration is specified
- Ports.cs constants (KafkaInternalPort=9092, KafkaExternalPort=9093) are NEVER used

**File: LocalTesting/LocalTesting.FlinkSqlAppHost/Ports.cs (Lines 9-21)**
```csharp
public const int KafkaInternalPort = 9092;  // Container network port
public const int KafkaExternalPort = 9093;  // Host machine port
public const string KafkaContainerBootstrap = "kafka:9092";  // For Flink containers
public const string KafkaHostBootstrap = "localhost:9093";   // For tests/external access
```
- These constants are DEFINED but NEVER USED in Program.cs
- They document the INTENDED configuration but Aspire doesn't respect them

**File: LearningCourse/LearningCourse.Common/DockerInfrastructure.cs (Lines 141-178)**
```csharp
public static async Task<string> GetKafkaHostEndpointAsync()
{
    var kafkaContainers = await RunDockerCommandAsync("ps --filter name=kafka --format {{.Ports}}");
    return ExtractKafkaEndpointFromPorts(kafkaContainers);
}

private static string ExtractKafkaEndpointFromPorts(string kafkaContainers)
{
    // Look for port mapping to 9092 (Kafka's default listener port)
    var match = System.Text.RegularExpressions.Regex.Match(line, @"(?:127\.0\.0\.1|0\.0\.0\.0):(\d+)->9092");
    if (match.Success)
    {
        var port = match.Groups[1].Value;
        return $"127.0.0.1:{port}";
    }
}
```
- Correctly queries `docker ps` for actual port mappings
- Looks for host port mapped to container port 9092
- Should find 38815→9092 from the evidence

#### Root Cause Analysis

**CORRECTED DIAGNOSIS** (User feedback: "kafka doesn't have any port issue"):

The problem is NOT with Kafka port configuration. Kafka containers work correctly with dynamic ports from Aspire.

**Actual Problem: Environment Variable Stale Port Propagation**

Looking at Exercise1-StringCapitalize/Program.cs (lines 36-37):
```csharp
private static string KafkaBootstrapServers =>
    Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9093";
```

The exercise code reads `KAFKA_BOOTSTRAP_SERVERS` from environment variable. If this variable contains a **stale port from a previous test run**, the exercise will attempt to connect to that old port instead of the current Kafka container's port.

**Evidence Trail**:
1. Container ports: `127.0.0.1:38815->9092/tcp` (current Kafka container)
2. Exercise tried: `127.0.0.1:37963` (old port from previous run!)
3. Mismatch: 37963 ≠ 38815

**Problem Source: LearningCourseTestBase.cs**

Looking at lines 77-82 in GlobalSetUp():
```csharp
// CRITICAL: Reset all static endpoint properties to null
KafkaFlinkBootstrapServers = null;
KafkaHostBootstrapServers = null;
TemporalHostEndpoint = null;
// ... etc
```

The static properties ARE reset, but there's a potential issue:

Looking at lines 1635-1636 in ExecuteExerciseAsync():
```csharp
psi.Environment["KAFKA_BOOTSTRAP_SERVERS"] = KafkaHostBootstrapServers;
psi.Environment["KAFKA_FLINK_BOOTSTRAP_SERVERS"] = KafkaFlinkBootstrapServers;
```

**ROOT CAUSE IDENTIFIED**:
- Exercise processes inherit environment variables from test process
- If `KAFKA_BOOTSTRAP_SERVERS` was set globally (not in `psi.Environment`), it persists across runs
- Tests discover NEW ports for each run, but exercise may read OLD environment variable

### Findings

**Root Cause: Environment Variable Not Updated Between Runs**

**Scenario**:
1. Test Run 1: Kafka starts with port 37963
2. GlobalSetUp sets `KAFKA_BOOTSTRAP_SERVERS=127.0.0.1:37963`
3. Test Run 1 completes, containers stop
4. Test Run 2: Kafka restarts with NEW port 38815
5. GlobalSetUp SHOULD update to `127.0.0.1:38815`
6. **BUT** exercise reads stale `KAFKA_BOOTSTRAP_SERVERS=127.0.0.1:37963`

**Questions to Answer**:
1. Is test infrastructure correctly updating static properties before running exercises?
2. Are exercises reading from ProcessStartInfo.Environment or global Environment?
3. Is there a race condition where exercise starts before port discovery completes?

### Next Steps for Investigation
1. Check if `LearningCourseTestBase.GlobalSetUp()` caches ports across test runs
2. Verify if old Kafka containers persist after failed tests
3. Add logging to show when ports are discovered and what containers are found

## Phase 2: Design

### Requirements
- Force Aspire to use configured static ports from Ports.cs
- Ensure test infrastructure correctly discovers the configured ports
- Maintain compatibility with both Docker and Podman runtimes

### Architecture Decisions

**Solution: Configure Kafka with Static Ports Using `.WithEndpoint()`**

Aspire's `.AddKafka()` doesn't expose port configuration parameters directly, but we can use `.WithEndpoint()` to force specific port mappings:

```csharp
var kafka = builder.AddKafka("kafka")
    .WithEndpoint("internal", endpoint => endpoint
        .WithTargetPort(9092)  // Container internal port
        .WithPort(9092)        // Host port (same for simplicity)
        .WithScheme("tcp"))
    .WithEndpoint("external", endpoint => endpoint
        .WithTargetPort(9093)  // Container external port
        .WithPort(9093)        // Host port (same for simplicity)
        .WithScheme("tcp"));
```

**However**, there's a better approach using Aspire's built-in Kafka configuration:

Looking at Aspire source code, `.AddKafka()` creates a KRaft-mode Kafka container with specific listener configuration. The issue is that Aspire doesn't expose easy static port configuration for Kafka because it's designed for dynamic allocation.

**RECOMMENDED SOLUTION**: Use `.WithEnvironment()` to configure Kafka listeners explicitly:

```csharp
var kafka = builder.AddKafka("kafka")
    .WithEnvironment("KAFKA_ADVERTISED_LISTENERS",
        "PLAINTEXT://kafka:9092,PLAINTEXT_HOST://localhost:9093")
    .WithEnvironment("KAFKA_LISTENER_SECURITY_PROTOCOL_MAP",
        "PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT")
    .WithEnvironment("KAFKA_LISTENERS",
        "PLAINTEXT://0.0.0.0:9092,PLAINTEXT_HOST://0.0.0.0:9093");
```

Then use `.WithHttpEndpoint()` or similar to map the host ports:

```csharp
var kafka = builder.AddKafka("kafka")
    .WithHttpEndpoint(port: Ports.KafkaInternalPort, targetPort: 9092, name: "kafka-internal")
    .WithHttpEndpoint(port: Ports.KafkaExternalPort, targetPort: 9093, name: "kafka-external");
```

**Wait** - `.WithHttpEndpoint()` is for HTTP services only. For TCP ports, we need a different approach.

**ACTUAL SOLUTION**: Use `.WithContainerRuntimeArgs()` to explicitly publish ports:

```csharp
var kafka = builder.AddKafka("kafka")
    .WithContainerRuntimeArgs("--publish", $"{Ports.KafkaInternalPort}:9092")
    .WithContainerRuntimeArgs("--publish", $"{Ports.KafkaExternalPort}:9093");
```

### Why This Approach
- **Direct control**: Forces Docker/Podman to use specific host ports
- **Explicit mapping**: No dynamic allocation ambiguity
- **Compatible**: Works with both Docker and Podman runtimes
- **Maintainable**: Uses Ports.cs constants as single source of truth

### Alternatives Considered

**Alternative 1: Keep Dynamic Ports, Fix Discovery**
- Pros: No changes to infrastructure, works with Aspire defaults
- Cons: Tests must always discover ports, potential race conditions remain

**Alternative 2: Use Docker Compose Instead of Aspire**
- Pros: Full control over port configuration
- Cons: Loses Aspire service discovery benefits, requires major refactoring

**Why We Chose Static Ports**:
- Eliminates port discovery race conditions
- Simpler test infrastructure (no dynamic discovery needed)
- Easier debugging (ports are always the same)
- Matches documented Ports.cs configuration

## Phase 3: TDD/BDD
(Pending - will write tests after design)

## Phase 4: Implementation
(Pending - will implement after tests)

## Phase 5: Testing & Validation
(Pending - will validate after implementation)

## Phase 6: Owner Acceptance
(Pending - awaiting owner review)

## Lessons Learned & Future Reference (MANDATORY)
(Will be filled as work progresses)

### What Worked Well
(TBD)

### What Could Be Improved
(TBD)

### Key Insights for Similar Tasks
(TBD)

### Specific Problems to Avoid in Future
(TBD)

### Reference for Future WIs
(TBD)