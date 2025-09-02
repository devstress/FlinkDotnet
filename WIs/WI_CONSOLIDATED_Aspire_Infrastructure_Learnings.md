# WI_CONSOLIDATED: Aspire Infrastructure Learnings and Patterns

**File**: `WIs/WI_CONSOLIDATED_Aspire_Infrastructure_Learnings.md`
**Title**: [Infrastructure] Consolidated learnings from Aspire container orchestration challenges  
**Description**: Comprehensive knowledge base of Aspire DCP container reconciliation, networking, and reliability patterns
**Priority**: High
**Component**: Aspire Infrastructure
**Type**: Knowledge Base
**Assignee**: AI Agent
**Created**: 2024-12-19
**Status**: Knowledge Repository

## Purpose
This document consolidates learnings from 47+ Work Items related to Aspire infrastructure challenges, providing a comprehensive reference for future Aspire deployments and troubleshooting.

## Critical Aspire Infrastructure Patterns

### 1. Container Reconciliation and DCP Timeout Issues

#### Problem Pattern
Aspire DCP (Distributed Container Platform) frequently fails with:
- Container state becomes "undetermined" 
- "object not found" errors during container inspection
- Network reconciliation failures
- 20-second default timeouts causing startup failures

#### Root Cause Analysis
- **DCP API Server IPv6 Binding**: Aspire DCP binds to IPv6 (::1) by default, causing connectivity issues
- **Parallel Container Startup**: Too many containers starting simultaneously overwhelms DCP
- **Resource Constraints**: Insufficient memory/CPU allocation for container startup
- **Network Race Conditions**: Container networking setup fails during parallel initialization

#### Proven Solutions
```csharp
// Extended DCP timeouts and container stability settings
Environment.SetEnvironmentVariable("ASPIRE_DCP_STARTUP_TIMEOUT", "300"); // 5 minutes
Environment.SetEnvironmentVariable("ASPIRE_DCP_RESOURCE_TIMEOUT", "120"); // 2 minutes per resource
Environment.SetEnvironmentVariable("ASPIRE_DCP_MAX_RETRIES", "5");
Environment.SetEnvironmentVariable("ASPIRE_DCP_RETRY_BACKOFF", "10"); // 10 seconds between retries

// IPv6 localhost connectivity enhancement
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
AppContext.SetSwitch("System.Net.Http.UseSocketsHttpHandler", true);
Environment.SetEnvironmentVariable("DOTNET_SYSTEM_NET_DISABLEIPV6", "false");
Environment.SetEnvironmentVariable("DOTNET_SYSTEM_NET_HTTP_USEIPV6", "true");

// Sequential container startup to prevent reconciliation race conditions
var flinkTaskManager1 = builder.AddContainer("flink-taskmanager-1", "flink:2.1.0")
    .WaitFor(flinkJobManager);
var flinkTaskManager2 = builder.AddContainer("flink-taskmanager-2", "flink:2.1.0")
    .WaitFor(flinkTaskManager1); // Sequential chain prevents DCP overload
```

### 2. Flink Container Configuration

#### Memory Configuration Patterns
```csharp
// Working Flink memory configuration for containers
.WithEnvironment("FLINK_PROPERTIES", """
    jobmanager.rpc.address: flink-jobmanager
    jobmanager.rpc.port: 6123
    jobmanager.memory.process.size: 1024m
    jobmanager.memory.off-heap.size: 64m
    taskmanager.numberOfTaskSlots: 8
    parallelism.default: 24
    taskmanager.memory.process.size: 1024m
    """)
```

#### Flink Version Upgrade Patterns
- **Always use latest Flink versions**: Upgraded from 1.17 to 2.1.0 for latest AI capabilities
- **Maintain version consistency**: All Flink containers must use same version
- **Test compatibility**: Ensure FlinkDotNet client compatibility with Flink server version

### 3. Kafka Cluster Configuration

#### KRaft Cluster Pattern (No Zookeeper)
```csharp
// 3-broker KRaft cluster configuration
var kafkaBroker1 = builder.AddContainer("kafka-broker-1", "apache/kafka:3.8.0")
    .WithEnvironment("KAFKA_NODE_ID", "1")
    .WithEnvironment("KAFKA_PROCESS_ROLES", "broker,controller")
    .WithEnvironment("KAFKA_CONTROLLER_QUORUM_VOTERS", "1@kafka-broker-1:9093,2@kafka-broker-2:9093,3@kafka-broker-3:9093")
    .WithEnvironment("CLUSTER_ID", "LOCAL_TESTING_KRAFT_CLUSTER_2024")
    .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "3")
    .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR", "3");
```

#### Kafka Stability Enhancements
- **Replication Factor 3**: Ensures fault tolerance in cluster
- **Auto Topic Creation**: Enabled for development convenience
- **Memory Limits**: 512MB max heap per broker for resource efficiency
- **Sequential UI Startup**: Kafka UI waits for all brokers to be ready

### 4. Temporal Workflow Integration

#### Database Configuration
```csharp
// PostgreSQL for Temporal with enhanced reliability
var temporalPostgres = builder.AddContainer("temporal-postgres", "postgres:13")
    .WithEnvironment("POSTGRES_DB", "temporal")
    .WithEnvironment("POSTGRES_HOST_AUTH_METHOD", "trust")
    .WithEnvironment("POSTGRES_MAX_CONNECTIONS", "100")
    .WithEnvironment("POSTGRES_INITDB_WAIT_TIMEOUT", "60"); // Extended init timeout
```

#### Temporal Server Configuration
```csharp
// Temporal server with proper database connectivity
var temporalServer = builder.AddContainer("temporal-server", "temporalio/auto-setup:latest")
    .WithEnvironment("DB", "postgres12")
    .WithEnvironment("POSTGRES_SEEDS", "temporal-postgres")
    .WithEnvironment("AUTO_SETUP", "true")
    .WithEnvironment("LOG_LEVEL", "warn") // Reduce log noise for stability
    .WaitFor(temporalPostgres); // Proper dependency chain
```

### 5. Observability Stack (PGL - Prometheus + Grafana + Loki)

#### Stable Observability Configuration
```csharp
// Prometheus with extended retention
var prometheus = builder.AddContainer("prometheus", "prom/prometheus:latest")
    .WithEnvironment("PROMETHEUS_STORAGE_TSDB_RETENTION_TIME", "7d")
    .WithArgs("--storage.tsdb.retention.time=7d", "--log.level=warn");

// Grafana with anonymous access for development
var grafana = builder.AddContainer("grafana", "grafana/grafana:latest")
    .WithEnvironment("GF_AUTH_ANONYMOUS_ENABLED", "true")
    .WithEnvironment("GF_AUTH_ANONYMOUS_ORG_ROLE", "Admin")
    .WithEnvironment("GF_SERVER_HTTP_ADDR", "0.0.0.0") // Force IPv4
    .WithEnvironment("GF_LOG_LEVEL", "warn"); // Reduce log noise
```

#### OpenTelemetry Collector Integration
```csharp
// OTel collector with minimal stable configuration
var otelCollector = builder.AddContainer("otel-collector", "otel/opentelemetry-collector-contrib:latest")
    .WithBindMount("./otel-config-training-minimal.yaml", "/etc/otelcol-contrib/otel-collector-config.yaml")
    .WaitFor(prometheus); // Simple dependency chain
```

### 6. GitHub Workflow CI/CD Patterns

#### Local Testing Workflow Failures
**Common Issues:**
- IPv6 connectivity in CI environments
- Resource constraints in GitHub Actions
- Container startup timeout failures
- Port conflicts between services

**Solutions:**
- Use IPv4-only networking configuration in CI
- Implement retry logic for container startup
- Extend timeouts for GitHub Actions environment
- Use different port ranges to avoid conflicts

#### Environment Variable Configuration
```bash
# CI-specific environment variables
ASPIRE_ALLOW_UNSECURED_TRANSPORT=true
DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true
ASPIRE_DCP_STARTUP_TIMEOUT=300
DOTNET_SYSTEM_NET_DISABLEIPV6=false
DOTNET_SYSTEM_NET_HTTP_USEIPV6=true
```

### 7. .NET 9.0 Upgrade Patterns

#### Migration Considerations
- **Aspire Workload**: Must install aspire workload for .NET 9.0
- **Project File Updates**: All projects must target net9.0
- **global.json**: Pin to .NET 9.0 SDK version
- **Compatibility**: Ensure all NuGet packages support .NET 9.0

#### Validation Commands
```bash
# Verify .NET 9.0 installation
dotnet --version  # Must return 9.0.x
dotnet workload list  # Must show aspire installed

# Build validation
dotnet build --configuration Release
./validate-build-and-tests.ps1
```

## Common Failure Patterns and Prevention

### 1. Container Reconciliation Failures
**Symptoms:** "undetermined" container states, "object not found" errors
**Prevention:** Sequential startup, extended timeouts, IPv6 configuration

### 2. Network Connectivity Issues
**Symptoms:** Connection refused, no data available errors
**Prevention:** IPv4 binding, proper network configuration, port management

### 3. Resource Exhaustion
**Symptoms:** Container OOM kills, startup timeouts
**Prevention:** Memory limits, sequential startup, resource monitoring

### 4. Dependency Chain Failures
**Symptoms:** Services starting before dependencies ready
**Prevention:** Proper WaitFor chains, health checks, startup delays

## Port Management Strategy

### LocalTesting Ports (18000-18999)
- 18000: LocalTesting WebAPI
- 18001: Kafka UI
- 18002: Flink Dashboard
- 18003: Temporal Server
- 18004: Temporal UI
- 18005: Loki
- 18006: Prometheus
- 18010: Grafana
- 18888: Aspire Dashboard

### IntegrationTests Ports (18000-18999 + offset)
- 18889: Aspire Dashboard (offset to avoid conflicts)
- Other services use same ranges but managed by different Aspire instances

## Lessons for Future Aspire Deployments

### 1. Always Debug First
- Collect container logs before attempting fixes
- Check Docker daemon connectivity and resources
- Verify network configuration and port availability
- Monitor resource utilization during startup

### 2. Use Proven Patterns
- Sequential container startup for complex dependencies
- Extended timeouts for resource-intensive containers
- IPv6 configuration for localhost connectivity
- Memory limits and health checks for stability

### 3. Environment-Specific Configuration
- CI environments need different timeout and resource settings
- Local development can use faster startup times
- Production environments need enhanced monitoring and logging

### 4. Continuous Validation
- Build and test locally before CI submission
- Use validation scripts for consistent results
- Monitor long-term stability and performance
- Document all configuration changes and their reasons

## References to Source WIs
This consolidates learnings from:
- WI74-WI79: Aspire container reconciliation and networking fixes
- WI1-WI5: Initial infrastructure setup and configuration
- WI32-WI33: Environment variable and container configuration
- WI52: Aspire BDD test integration
- WI2: .NET 9.0 upgrade and compatibility

## Action Items for Future Work
1. Create automated validation scripts for Aspire infrastructure health
2. Implement monitoring and alerting for container reconciliation failures
3. Develop retry and recovery patterns for CI/CD environments
4. Document environment-specific configuration templates
5. Create troubleshooting runbooks for common failure scenarios