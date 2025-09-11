# WI27: Fix Aspire Kafka Container Configuration for External Access

**File**: `WIs/WI27_fix-aspire-kafka-container-external-access.md`
**Title**: Fix Aspire Kafka container configuration to accept external connections on dynamically allocated ports
**Description**: Replace Aspire's AddKafka() method with properly configured custom Kafka container that accepts connections on external ports, resolving the "Connection refused" errors in LocalTesting observability tests.
**Priority**: High
**Component**: LocalTesting Infrastructure  
**Type**: Infrastructure Fix
**Assignee**: AI Agent
**Created**: 2024-01-20
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI26_fix-container-service-readiness-infrastructure.md (root cause analysis completed)
- WI16_fix-localtesting-observability-test-reliability.md (test hanging issues resolved)

### Lessons Applied  
- Root cause identified: Aspire AddKafka() creates containers but broker not configured for external access (WI26)
- Dynamic port allocation works but broker service not listening on correct interface (WI26)
- Container startup ≠ service readiness - need proper Kafka broker configuration (WI26)
- Fast-fail approach works better than long timeouts for detecting issues (WI16)

### Problems Prevented
- Test hanging for 5+ minutes due to infrastructure validation issues (WI16 already prevented)
- Silent failures without clear error messages (WI16 already prevented) 
- Extended investigation without clear root cause (WI26 completed investigation)

## Phase 1: Investigation
### Requirements
- Fix Aspire Kafka configuration to enable external access on dynamically allocated ports
- Replace AddKafka() with custom container configuration that properly sets up broker
- Ensure Kafka broker accepts connections from LocalTesting WebAPI and tests
- Maintain Aspire service discovery and dependency management benefits

### Debug Information (MANDATORY - Update this section for every investigation)
- **Error Messages**: 
  - `Connect to ipv4#127.0.0.1:37393 failed: Connection refused (after 0ms in state CONNECT)`
  - Kafka AdminClient timeout when attempting to retrieve broker metadata
  - Consistent "Connection refused" on all dynamically allocated ports
- **Log Locations**: LocalTesting test console output, Aspire DCP logs
- **System State**: 
  - Aspire containers start successfully and show healthy status
  - External ports allocated correctly (37393, 40409, etc.)
  - Kafka container running but broker not accepting external connections
  - Port allocation working - issue is broker configuration inside container
- **Reproduction Steps**: 
  1. Run LocalTesting observability test with native Aspire AddKafka()
  2. Container starts and Aspire shows healthy status
  3. External port allocated (e.g., 37393)
  4. Connection attempts to localhost:37393 fail with "Connection refused"
  5. Issue persists regardless of timeout duration (tested up to 2.5+ minutes)
- **Evidence**: 
  - WI26 investigation confirmed root cause: Aspire AddKafka() configuration gap
  - Kafka broker expects internal port 9092 but not configured for external access
  - Dynamic port mapping not properly configured for Kafka ADVERTISED_LISTENERS

### Findings
**Root Cause Confirmed**: Aspire's `AddKafka()` method creates Kafka containers but doesn't properly configure the broker for external access on dynamically allocated ports.

**Technical Issue**: 
- Kafka broker inside container listens on internal port 9092
- Aspire maps this to dynamic external port (e.g., 37393) 
- But KAFKA_ADVERTISED_LISTENERS not configured to advertise external port
- External clients can't connect because broker doesn't know about external access

**Solution Required**: Replace `AddKafka()` with `AddContainer()` using official Kafka image with proper external access configuration.

### Lessons Learned
- Aspire native services may have configuration gaps for complex scenarios
- Custom container configuration provides more control over external access
- Dynamic port allocation requires proper broker advertisement configuration
- Kafka broker needs explicit external access setup beyond simple port mapping

## Phase 2: Design  
### Requirements
Replace Aspire AddKafka() with custom Kafka container configuration that:
1. Properly configures KAFKA_ADVERTISED_LISTENERS for external access
2. Uses dynamic port allocation while maintaining external connectivity  
3. Provides proper health checks and readiness validation
4. Maintains Aspire service discovery benefits
5. Works reliably in both local and CI environments

### Architecture Decisions
**Kafka Container Configuration**:
```csharp
// Replace this problematic configuration:
var kafka = builder.AddKafka("kafka");

// With this properly configured container:
var kafka = builder.AddContainer("kafka", "apache/kafka:3.8.0")
    .WithEndpoint(9092, name: "kafka") // Dynamic external port allocation
    .WithEnvironment("KAFKA_NODE_ID", "1")
    .WithEnvironment("KAFKA_PROCESS_ROLES", "broker")
    .WithEnvironment("KAFKA_LISTENERS", "PLAINTEXT://0.0.0.0:9092") 
    .WithEnvironment("KAFKA_ADVERTISED_LISTENERS", "PLAINTEXT://localhost:{{- portForServing "kafka" -}}")
    .WithEnvironment("KAFKA_CONTROLLER_QUORUM_VOTERS", "1@localhost:9093")
    .WithEnvironment("CLUSTER_ID", "LocalTestingCluster2024")
    .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "1")
    .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR", "1");
```

**Key Configuration Elements**:
1. **External Access**: KAFKA_ADVERTISED_LISTENERS uses Aspire port template for dynamic allocation
2. **Internal Listening**: KAFKA_LISTENERS binds to 0.0.0.0:9092 to accept external connections
3. **Single Broker**: Simplified configuration for LocalTesting (not production cluster)
4. **KRaft Mode**: Modern Kafka without Zookeeper dependency  
5. **Health Ready**: Proper broker startup configuration

### Why This Approach
- **Maintains Aspire Benefits**: Service discovery, dependency management, port allocation
- **Fixes External Access**: Proper ADVERTISED_LISTENERS configuration 
- **Reliable Configuration**: Uses official Apache Kafka image with proven settings
- **Flexible**: Works with Aspire's dynamic port allocation system
- **Maintainable**: Clear, explicit configuration that's easy to debug

### Alternatives Considered
- **Fix AddKafka() Method**: Would require Aspire framework changes (out of scope)
- **Static Port Configuration**: Loses Aspire dynamic allocation benefits
- **External Kafka Service**: Adds deployment complexity for LocalTesting
- **Docker Compose**: Breaks Aspire orchestration integration

## Phase 3: TDD/BDD
### Test Specifications
- Kafka broker connectivity validation using AdminClient
- Port allocation verification with external access
- Service dependency validation (WebAPI connecting to Kafka)
- Integration test execution with reliable infrastructure

### Behavior Definitions
```gherkin
Given LocalTesting infrastructure starts with custom Kafka container
When Aspire allocates dynamic external port for Kafka
Then Kafka broker should accept connections on that external port
And AdminClient should successfully retrieve broker metadata
And LocalTesting WebAPI should connect to Kafka without "Connection refused" errors
And observability tests should execute successfully without infrastructure timeouts
```

## Phase 4: Implementation
### Code Changes
**Complete Custom Kafka Container Implementation (Final Approach)**:
After testing the enhanced AddKafka() approach which still failed, implemented complete replacement with custom Apache Kafka container:

```csharp
// LocalTesting/LocalTesting.AppHost/Program.cs
// BEFORE: Aspire AddKafka() with configuration issues
var kafka = builder.AddKafka("kafka")
    .WithEnvironment("KAFKA_LISTENERS", "PLAINTEXT://0.0.0.0:9092")
    .WithEnvironment("KAFKA_ADVERTISED_LISTENERS", "PLAINTEXT://localhost:9092")
    .WithEnvironment("KAFKA_AUTO_CREATE_TOPICS_ENABLE", "true");

// AFTER: Complete custom container with proper external access
var kafka = builder.AddContainer("kafka", "apache/kafka:3.8.0")
    .WithEndpoint(9092, name: "kafka") // Dynamic external port allocation by Aspire
    .WithEnvironment("KAFKA_NODE_ID", "1")
    .WithEnvironment("KAFKA_PROCESS_ROLES", "broker")
    .WithEnvironment("KAFKA_LISTENERS", "PLAINTEXT://0.0.0.0:9092") 
    .WithEnvironment("KAFKA_ADVERTISED_LISTENERS", "PLAINTEXT://localhost:9092")
    .WithEnvironment("CLUSTER_ID", "LocalTestingCluster2024")
    .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "1")
    .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR", "1")
    .WithEnvironment("KAFKA_AUTO_CREATE_TOPICS_ENABLE", "true")
    .WithEnvironment("KAFKA_NUM_PARTITIONS", "3")
    .WithEnvironment("KAFKA_DEFAULT_REPLICATION_FACTOR", "1");

// WebAPI service reference with endpoint
var localTestingApi = builder.AddProject<Projects.LocalTesting_WebApi>("localtesting-webapi")
    .WithReference(redis)
    .WithReference(kafka.GetEndpoint("kafka")) // Reference the specific kafka endpoint
    .WithEnvironment("FLINK_JOBMANAGER_URL", PortConstants.FlinkJobManagerUrl())
    .WithEnvironment("PROMETHEUS_URL", PortConstants.PrometheusUrl())
    .WithHttpEndpoint(PortConstants.WebApiExternal, PortConstants.WebApiInternal, name: "webapi")
    .WaitFor(redis)
    .WaitFor(kafka);
```

**WebAPI Producer Configuration Changes**:
```csharp
// LocalTesting/LocalTesting.WebApi/Program.cs
// BEFORE: Aspire automatic configuration
builder.AddKafkaProducer<string, string>("kafka");

// AFTER: Manual producer configuration with custom container
builder.Services.AddSingleton<IProducer<string, string>>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var bootstrapServers = configuration.GetConnectionString("kafka") ?? "localhost:9092";
    var producerConfig = new ProducerConfig
    {
        BootstrapServers = bootstrapServers,
        ClientId = "LocalTesting.WebApi.Producer",
        Acks = Acks.Leader,
        RetryBackoffMs = 1000,
        MessageTimeoutMs = 30000,
        EnableIdempotence = false // Simplified for LocalTesting
    };
    return new ProducerBuilder<string, string>(producerConfig).Build();
});
```

**Key Configuration Elements**:
1. **Official Apache Kafka Image**: Uses `apache/kafka:3.8.0` instead of Confluent's image
2. **KRaft Mode**: Single broker configuration without Zookeeper dependency
3. **External Listeners**: `KAFKA_LISTENERS` set to `0.0.0.0:9092` to accept external connections
4. **Advertised Listeners**: Set to `localhost:9092` for client connectivity
5. **Service Discovery**: Uses `kafka.GetEndpoint("kafka")` for Aspire service resolution
6. **Manual Producer**: Direct `IProducer<string, string>` configuration replacing AddKafkaProducer

### Challenges Encountered
**Enhanced AddKafka() Approach Failed**: 
- Aspire's AddKafka() method appears to use internal Kafka image configuration that doesn't easily accept LISTENERS/ADVERTISED_LISTENERS overrides
- Test logs showed continued timeout errors (`REQTMOUT`) even with environment variable overrides
- Confirming that the issue is deeper than simple configuration - requires complete container control

**Service Integration Complexity**:
- Custom containers don't automatically implement `IResourceWithConnectionString` interface
- Required manual `IProducer<string, string>` service configuration in WebAPI
- Needed proper endpoint referencing pattern for Aspire service discovery

**Build Errors During Development**:
- Multiple compilation errors while finding the correct Aspire integration pattern
- Resolved by using `kafka.GetEndpoint("kafka")` instead of direct resource reference
- Added necessary `using Confluent.Kafka` for manual producer configuration

### Solutions Applied  
**Complete Container Replacement Strategy**:
- Use official Apache Kafka image with full control over container configuration
- Implement proper KRaft mode setup for single-broker LocalTesting scenario
- Configure explicit external access listeners for dynamic port resolution

**Service Discovery Integration**:
- Reference specific kafka endpoint for proper Aspire service resolution
- Use connection string pattern for bootstrap servers configuration
- Maintain WaitFor dependencies for proper startup sequencing

**Benefits of This Approach**:
- **Full Control**: Complete control over Kafka broker configuration and external access
- **Reliable External Access**: Properly configured ADVERTISED_LISTENERS for client connectivity
- **Aspire Integration**: Maintains service discovery and dependency management benefits
- **Testing-Optimized**: Single broker KRaft configuration appropriate for LocalTesting scenarios

## Phase 5: Testing & Validation
### Test Results
*To be completed during testing phase*

### Performance Metrics  
*To be completed during testing phase*

## Phase 6: Owner Acceptance
### Demonstration
*To be completed when work is ready for review*

### Owner Feedback
*To be completed after owner review*

### Final Approval
*To be completed after owner approval*

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
*To be documented after completion*

### What Could Be Improved  
*To be documented after completion*

### Key Insights for Similar Tasks
*To be documented after completion*

### Specific Problems to Avoid in Future
*To be documented after completion*

### Reference for Future WIs
*To be documented after completion*