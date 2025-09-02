# FlinkDotNet Integration Tests - Enterprise BDD Testing Infrastructure

This directory contains **comprehensive integration testing infrastructure** with enterprise-grade BDD scenarios for validating FlinkDotNet's real Apache Flink integration capabilities. Following patterns from **LocalTesting** and **LearningCourse**, this infrastructure provides production-ready testing for enterprise streaming applications.

## 🎯 Purpose and Enterprise Architecture

This infrastructure provides **enterprise-grade integration testing** for:

### 🏢 Real-World Integration Validation
- **Real job submission** to live Flink clusters via REST API
- **Production-scale infrastructure** with 3-broker Kafka clusters
- **Enterprise observability** with comprehensive PGL monitoring stack (Prometheus + Grafana + Loki)
- **Multi-environment testing** with Docker orchestration and Aspire DCP

### 🧪 BDD Testing Framework
Following **LearningCourse enterprise patterns**:
- **SpecFlow/ReqNRoll** BDD scenarios for behavioral validation
- **Given/When/Then** specifications for clear test documentation
- **Allure reporting** with professional test reports and metrics
- **Test categorization** by business impact and performance characteristics

### 🏗️ Enterprise Infrastructure Stack
Based on **LocalTesting proven patterns**:

```
┌─────────────────────────────┐   ┌──────────────────────────────┐   ┌─────────────────────────────┐
│  BDD Integration Tests      │──▶│  Enterprise Aspire AppHost   │──▶│  Production Infrastructure  │
│  (SpecFlow/ReqNRoll)        │   │  (LocalTesting Patterns)     │   │  (Flink + Kafka + PGL)     │
│                             │   │                              │   │                             │
│  ├── IntegrationTest        │   │  ├── 3-Broker Kafka KRaft   │   │  ├── Apache Flink 2.1.0    │
│  ├── stress                 │   │  ├── Redis Distributed Cache│   │  ├── Kafka UI Monitoring   │
│  ├── reliability_test       │   │  ├── Flink 2.1.0 Cluster   │   │  ├── Prometheus Metrics    │
│  ├── backpressure_test      │   │  ├── Temporal Workflows     │   │  ├── Grafana Dashboards   │
│  └── chaos_engineering      │   │  └── PGL Observability     │   │  └── Loki Log Aggregation │
└─────────────────────────────┘   └──────────────────────────────┘   └─────────────────────────────┘
```

## 📊 Integration Test Infrastructure Stack

### Minimal Configuration for CI/CD Reliability
**Integration Tests uses a simplified stack** optimized for reliable CI/CD execution:

#### **Core Services Only**
- **Aspire Dashboard**: `localhost:18889` - Container orchestration (offset to avoid LocalTesting conflicts)
- **Kafka UI**: `localhost:18001` - Message broker monitoring and debugging  
- **Flink Dashboard**: `localhost:18002` - Job management and monitoring
- **Redis**: Available for distributed caching (internal port only)

#### **3-Broker Kafka Cluster**
- **Production-grade KRaft configuration** with 3 brokers for high availability
- **Enhanced heap settings** for integration test workloads (1GB per broker)
- **Automatic topic creation** with 10 partitions and replication factor 3

#### **Flink 2.0 Cluster** 
- **JobManager**: Enhanced configuration for integration tests (2GB heap)
- **TaskManager**: 10 task slots with 2GB heap for stress testing
- **Checkpointing**: Configured for fault tolerance testing

### Port Management Strategy
**Integration Tests uses minimal ports** to prevent conflicts with LocalTesting:

| Service | LocalTesting Port | IntegrationTests Port | Purpose |
|---------|-------------------|----------------------|---------|
| Aspire Dashboard | 18888 | 18889 | Container orchestration |
| Kafka UI | 18001 | 18001 | Message broker monitoring |
| Flink Dashboard | 18002 | 18002 | Job management |
| Redis | Internal | Internal | Distributed caching |

### Observability Notes
- **Simplified for CI/CD**: Full observability stack (Prometheus, Grafana, Loki, Temporal) available in LocalTesting
- **Focus on Core Testing**: Integration tests prioritize Flink and Kafka functionality  
- **Reliable Execution**: Minimal dependencies reduce CI/CD failure points

## 📁 Project Structure

### 🎯 Enterprise Integration Test Projects

#### **FlinkDotNet.Aspire.IntegrationTests**
**Primary BDD Integration Test Suite** - Comprehensive SpecFlow/ReqNRoll scenarios

**Test Categories (Based on LearningCourse Patterns):**
- **🧪 IntegrationTest** - Basic infrastructure and connectivity validation
- **⚡ stress** - High-throughput and scalability testing (1M+ messages)
- **🔄 reliability_test** - Back pressure, rebalance, and fault tolerance scenarios  
- **🌊 backpressure_test** - Flow control and distributed rate limiting validation
- **💥 chaos_engineering** - Failure injection and recovery validation

**Enterprise Features:**
- **Real Flink Integration**: Tests against actual Flink 2.1.0 clusters with AI capabilities
- **Production-Scale Testing**: 3-broker Kafka clusters, Redis distributed caching
- **BDD Specifications**: Clear Given/When/Then scenarios for business validation
- **Allure Reporting**: Professional test reports with metrics, charts, and traceability
- **Multi-Environment Support**: Local development, CI/CD, and cloud deployment testing

#### **FlinkDotnetStandardReliabilityTest**
**Enterprise Reliability and Performance Validation** - Long-running stability testing

**Enterprise Capabilities:**
- **End-to-End Reliability**: Continuous testing with real infrastructure over extended periods
- **Performance Benchmarking**: Throughput, latency, and resource utilization metrics
- **Fault Tolerance Validation**: Network partitions, node failures, container restarts
- **Resource Optimization**: Memory usage, CPU utilization, and auto-scaling validation
- **SLA Compliance Testing**: 99.9% uptime validation under various failure conditions

#### **FlinkJobBuilder.Sample**  
**API Integration Demonstrations** - Real-world FlinkDotNet API usage patterns

**Enterprise Integration Examples:**
- **DataStream API**: Type-safe stream operations with .NET 9.0 features
- **Connector Ecosystem**: Kafka, Redis, Temporal, and custom connector integration
- **Error Handling**: Comprehensive retry policies, circuit breakers, and recovery patterns
- **Observability Integration**: Metrics, logging, and distributed tracing examples

#### **FlinkDotNet.Aspire.AppHost**
**Enterprise Infrastructure Orchestration** - Production-grade Aspire orchestration

**LocalTesting Pattern Adoption:**
- **Sequential Container Startup**: Prevents DCP reconciliation failures
- **Extended Timeouts**: 5-minute startup timeout for complex infrastructure
- **IPv6 Connectivity Enhancement**: Proper localhost connectivity for Aspire DCP
- **3-Broker Kafka KRaft**: Production-scale messaging without Zookeeper
- **Enhanced Observability**: Complete PGL stack integration

## 🚀 Getting Started

### Prerequisites
Following **LearningCourse enterprise requirements**:

#### .NET 9.0 Environment (MANDATORY)
```bash
# Verify .NET 9.0 installation
dotnet --version  # Must return 9.0.x

# Install Aspire workload (Platform-specific requirement)
# Windows/macOS: Usually included with .NET SDK (.NET 8+)
# Linux: Manual installation required
dotnet workload install aspire

# Verify Docker Desktop
docker --version
docker-compose --version
```

#### Development Environment
- **Visual Studio 2022** or **VS Code** with C# support
- **Docker Desktop** with 8GB+ RAM allocation
- **.NET 9.0 SDK** with Aspire workload
- **PowerShell 7+** for validation scripts

### 🏃‍♂️ Quick Start

#### 1. Infrastructure Startup
```bash
# Start IntegrationTests infrastructure
cd IntegrationTests/FlinkDotNet.Aspire.AppHost
dotnet run

# Wait 3-5 minutes for complete infrastructure startup
# (Extended timeout prevents DCP reconciliation failures)
```

#### 2. Verify Integration Test Stack
Open these URLs to verify all services are healthy:

**🎛️ Core Services:**
- **Aspire Dashboard**: http://localhost:18889 (Container orchestration)  
- **Flink Dashboard**: http://localhost:18002 (Job management and monitoring)
- **Kafka UI**: http://localhost:18001 (Message broker management)

**💡 Full Observability Stack Available in LocalTesting:**
- For complete observability (Prometheus, Grafana, Loki, Temporal), see [LocalTesting](../LocalTesting/README.md)
- IntegrationTests focuses on core Flink and Kafka functionality for reliable CI/CD

#### 3. Execute BDD Integration Tests
```bash
# Run all integration tests
cd IntegrationTests
dotnet test --configuration Release

# Run specific test categories
dotnet test --filter "Category=IntegrationTest"     # Basic infrastructure tests
dotnet test --filter "Category=stress"             # High-throughput tests  
dotnet test --filter "Category=reliability_test"   # Fault tolerance tests
dotnet test --filter "Category=backpressure_test"  # Flow control tests
```

#### 4. View Enterprise Test Reports
```bash
# Generate Allure reports (if configured)
allure serve allure-results

# View test results in Visual Studio Test Explorer
# Professional test categorization and traceability
```

## 🧪 BDD Test Categories and Scenarios

### 🧪 IntegrationTest Category
**Purpose**: Basic infrastructure connectivity and health validation

**Example Scenarios:**
```gherkin
Feature: Infrastructure Health Validation
  As a platform operator
  I want to validate all infrastructure components are healthy
  So that integration tests can run reliably

  Scenario: All services start successfully
    Given the Aspire orchestration is running
    When I check all service health endpoints
    Then all services should report healthy status
    And all monitoring dashboards should be accessible
```

### ⚡ stress Category  
**Purpose**: High-throughput and scalability testing (Based on LearningCourse Day 7 patterns)

**Example Scenarios:**
```gherkin
Feature: Enterprise Scale Stress Testing
  As a platform engineer
  I want to validate system performance under high load
  So that I can ensure production readiness

  Scenario: Process 1 Million Messages with Exactly-Once Semantics
    Given a 3-broker Kafka cluster is running
    And Flink cluster has 24 task slots available
    When I produce 1,000,000 messages across 100 partitions
    And I start a Flink streaming job with exactly-once semantics
    Then all messages should be processed successfully
    And processing latency should remain under 50ms p99
    And no messages should be lost or duplicated
```

### 🔄 reliability_test Category
**Purpose**: Fault tolerance and recovery validation (Based on LearningCourse Day 11 patterns)

**Example Scenarios:**
```gherkin
Feature: Disaster Recovery and Fault Tolerance
  As a reliability engineer  
  I want to validate system behavior under failure conditions
  So that I can ensure business continuity

  Scenario: Kafka broker failure recovery
    Given a 3-broker Kafka cluster is processing messages
    When one Kafka broker fails unexpectedly
    Then the system should continue processing without data loss
    And recovery should complete within 30 seconds
    And replication factor should be maintained
```

### 🌊 backpressure_test Category
**Purpose**: Flow control and distributed rate limiting (Based on LearningCourse Day 3 patterns)

**Example Scenarios:**
```gherkin
Feature: Production Backpressure Handling
  As a streaming engineer
  I want to validate backpressure handling under load
  So that the system remains stable under varying throughput

  Scenario: Graceful backpressure with Redis rate limiting
    Given Flink job is processing at maximum capacity
    When message production rate exceeds processing capacity
    Then Redis-based rate limiting should activate
    And system should gracefully handle backpressure
    And no messages should be dropped
```

## 🏗️ Architecture Best Practices

### 🎯 LocalTesting Pattern Integration
**Following proven LocalTesting enterprise patterns:**

#### Sequential Container Startup
```csharp
// Prevent DCP reconciliation failures with sequential startup
var flinkTaskManager1 = builder.AddContainer("flink-taskmanager-1", "flink:2.1.0")
    .WaitFor(flinkJobManager);
var flinkTaskManager2 = builder.AddContainer("flink-taskmanager-2", "flink:2.1.0")
    .WaitFor(flinkTaskManager1); // Sequential prevents race conditions
var flinkTaskManager3 = builder.AddContainer("flink-taskmanager-3", "flink:2.1.0")
    .WaitFor(flinkTaskManager2);
```

#### Extended DCP Timeouts
```csharp
// Extended timeouts for complex infrastructure startup
builder.Services.Configure<Microsoft.Extensions.Hosting.HostOptions>(options =>
{
    options.StartupTimeout = TimeSpan.FromMinutes(5); // Prevents timeout failures
    options.ShutdownTimeout = TimeSpan.FromMinutes(2);
});

Environment.SetEnvironmentVariable("ASPIRE_DCP_STARTUP_TIMEOUT", "300");
Environment.SetEnvironmentVariable("ASPIRE_DCP_RESOURCE_TIMEOUT", "120");
```

#### IPv6 Connectivity Enhancement
```csharp
// Proper localhost connectivity for Aspire DCP
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
Environment.SetEnvironmentVariable("DOTNET_SYSTEM_NET_DISABLEIPV6", "false");
Environment.SetEnvironmentVariable("DOTNET_SYSTEM_NET_HTTP_USEIPV6", "true");
```

### 🎓 LearningCourse Pattern Integration
**Adopting enterprise patterns from LearningCourse:**

#### Real-World Company Patterns
- **Netflix Patterns**: Real-time recommendation systems, global rate limiting
- **Uber Patterns**: Financial processing, distributed sagas, fault tolerance
- **LinkedIn Patterns**: Social graph processing, complex analytics
- **Google SRE Patterns**: 99.99% uptime validation, comprehensive monitoring

#### Progressive Test Complexity
- **Foundation Tests**: Basic connectivity and health validation
- **Enterprise Tests**: Production-scale load and performance validation
- **Advanced Tests**: Chaos engineering and fault injection
- **Integration Tests**: End-to-end business workflow validation

## 🔧 Troubleshooting Guide

### Common Issues and Solutions

#### Container Startup Failures
**Problem**: "Container state became undetermined" or DCP reconciliation failures
**Solution**: 
- Ensure Docker Desktop has 8GB+ RAM allocated
- Verify sequential startup patterns are followed
- Check extended timeout configuration
- Use IPv6 connectivity enhancement

#### Port Conflicts
**Problem**: Services fail to bind to ports
**Solution**:
- Stop LocalTesting if running simultaneously
- Check port availability: `netstat -an | findstr "18001\|18002\|18889"`
- Use different port offset for parallel testing

#### Test Failures
**Problem**: BDD scenarios fail due to infrastructure issues
**Solution**:
- Verify core services are accessible: Aspire (18889), Flink (18002), Kafka UI (18001)
- Check service health endpoints (see curl commands above)
- For full observability testing, use LocalTesting environment

### Performance Optimization

#### Resource Allocation
```yaml
# Docker Desktop recommended settings
Memory: 8GB minimum, 16GB recommended
CPU: 4 cores minimum, 8 cores recommended
Disk: 50GB available space
```

#### Flink Cluster Tuning
```properties
# Production-grade Flink configuration
jobmanager.memory.process.size: 2048m
taskmanager.memory.process.size: 2048m
taskmanager.numberOfTaskSlots: 8
parallelism.default: 24
taskmanager.network.memory.fraction: 0.2
```

## 📞 Support and Continuous Improvement

### Documentation References
- **[LocalTesting Patterns](../LocalTesting/README.md)** - Infrastructure best practices
- **[LearningCourse Enterprise Patterns](../LearningCourse/README.md)** - Real-world company implementations
- **[FlinkDotNet API Documentation](../FlinkDotNet/README.md)** - Core library integration

### Continuous Learning Integration
- **BDD Scenarios**: Continuously updated based on production feedback
- **Performance Benchmarks**: Regular validation against enterprise SLAs
- **Chaos Engineering**: Monthly failure injection and recovery validation
- **Company Pattern Updates**: Quarterly integration of latest industry practices

### Contributing Guidelines
- **New Test Scenarios**: Follow BDD patterns with clear business value
- **Infrastructure Changes**: Adopt LocalTesting proven patterns
- **Performance Requirements**: Maintain enterprise SLA standards
- **Documentation**: Update with real-world learnings and troubleshooting

---

**🎯 Enterprise Integration Testing Excellence**

This IntegrationTests infrastructure represents **enterprise-grade testing capabilities** following proven patterns from LocalTesting and LearningCourse. The combination of **real infrastructure**, **comprehensive BDD scenarios**, and **production-scale observability** provides confidence for production deployments at scale.

**Ready to validate your enterprise streaming applications?** Start with the infrastructure setup and explore the comprehensive BDD test scenarios!

**Examples:**
- **Job submission** patterns for integration testing
- **Configuration validation** for different environments
- **Error handling** and resilience testing patterns

### 🏗️ Infrastructure (Aspire AppHost)

Modern Aspire orchestration with enterprise observability stack:

#### **Core Infrastructure**
- **3-Broker Kafka Cluster** - Production-scale messaging with KRaft
- **Redis Cluster** - Distributed caching and session management
- **Flink 2.1.0 Cluster** - JobManager + TaskManager for real job execution

#### **Observability Stack**
- **Prometheus** - Metrics collection and alerting
- **Grafana** - Unified dashboards and visualization
- **Tempo** - Distributed tracing and performance analysis
- **Mimir** - Long-term metrics storage and analysis
- **OpenTelemetry** - Complete observability data collection

#### **Enhanced Features**
- **IPv6 Support** - Modern networking with proper localhost connectivity
- **Extended Timeouts** - Stability improvements for complex container orchestration
- **Sequential Startup** - Prevents DCP reconciliation failures
- **Health Monitoring** - Comprehensive service health checks

## 🚀 Getting Started

### Prerequisites
- .NET 9.0 SDK (required for all projects)
- Docker Desktop with sufficient resources (8GB+ RAM recommended)
- Aspire workload installed (`dotnet workload install aspire`)
  - **Note**: On Windows/macOS, Aspire is typically included with .NET SDK (.NET 8+), but verify installation
  - **Note**: On Linux, Aspire workload must be manually installed as it's not bundled with base SDK packages

### Quick Start for Integration Testing

1. **Start Infrastructure:**
   ```bash
   cd IntegrationTests/FlinkDotNet.Aspire.AppHost
   dotnet run
   # Wait 90-120 seconds for all services to start
   ```

2. **Verify Infrastructure is Running:**
   - **Aspire Dashboard**: http://localhost:18888 (Main orchestration)
   - **Flink Dashboard**: http://localhost:18002 (Job management)
   - **Kafka UI**: http://localhost:18001 (Message broker)
   - **Grafana**: http://localhost:18010 (Observability dashboards)
   - **Prometheus**: http://localhost:18006 (Metrics collection)

3. **Run Integration Tests:**
   ```bash
   # Run all BDD integration tests
   dotnet test IntegrationTests/FlinkDotNet.Aspire.IntegrationTests --configuration Release
   
   # Run specific test categories
   dotnet test --filter "Category=IntegrationTest"  # Basic infrastructure tests
   dotnet test --filter "Category=stress"          # High-throughput tests  
   dotnet test --filter "Category=reliability_test" # Reliability scenarios
   dotnet test --filter "Category=backpressure_test" # Flow control tests
   ```

### Integration Test Categories

| Category | Purpose | Duration | Resource Requirements |
|----------|---------|----------|----------------------|
| **IntegrationTest** | Basic container infrastructure validation | 2-5 min | Standard |
| **stress** | 1M+ message throughput testing | 10-30 min | High CPU/Memory |
| **reliability_test** | Back pressure and rebalance scenarios | 15-25 min | High Network I/O |
| **backpressure_test** | Flow control and rate limiting | 5-15 min | Standard |

## 🔧 Advanced Configuration

### Multi-Environment Support

Configure different environments for various testing scenarios:

```yaml
# Local Development Environment
FLINK_JOB_GATEWAY_HOST: "localhost"
FLINK_JOB_GATEWAY_PORT: "8080"
INTEGRATION_TEST_MODE: "LOCAL"

# CI/CD Environment  
FLINK_JOB_GATEWAY_HOST: "flink-jobmanager"
FLINK_JOB_GATEWAY_PORT: "8081"
INTEGRATION_TEST_MODE: "CI"
GITHUB_ACTIONS: "true"

# Production Testing Environment
FLINK_JOB_GATEWAY_HOST: "flink-job-gateway.flink-system.svc.cluster.local"
FLINK_JOB_GATEWAY_PORT: "8080"
INTEGRATION_TEST_MODE: "PRODUCTION"
```

### Observability Configuration

The integration test environment includes comprehensive observability:

- **Distributed Tracing**: All test scenarios generate traces for analysis
- **Custom Metrics**: Business metrics collection during test execution
- **Real-time Dashboards**: Live monitoring during long-running tests
- **Alert Integration**: Configurable alerts for test environment health

## 🧪 Testing Strategy

### BDD Test Framework
All integration tests use behavior-driven development (BDD) with SpecFlow/ReqNRoll:

```gherkin
Feature: Stress Test - High Throughput Message Processing
  
  Scenario: Process 1 million messages under high load
    Given the Flink cluster is running and healthy
    And the message throughput target is 1000000 messages
    When I submit a high-throughput streaming job
    And I send 1000000 messages through the pipeline
    Then all messages should be processed successfully
    And the average latency should be less than 50ms
    And no messages should be lost or duplicated
```

### Test Data Management
- **Synthetic Data Generation**: Realistic test data that simulates production scenarios
- **Volume Testing**: Configurable message volumes from thousands to millions
- **Error Injection**: Controlled failure scenarios for resilience testing
- **Performance Baselines**: Established performance thresholds for regression detection

## 📊 Monitoring and Observability

### Real-time Monitoring During Tests
```bash
# Flink Web UI - Job status and metrics
http://localhost:18002

# Aspire Dashboard - Container orchestration
http://localhost:18888

# Grafana Dashboards - Unified observability
http://localhost:18010

# Prometheus Metrics - Raw metrics and alerts
http://localhost:18006
```

### Test Reporting
- **Allure Reports**: Professional HTML reports with charts and trend analysis
- **Performance Metrics**: Throughput, latency, resource utilization trends
- **Error Analysis**: Detailed failure analysis with stack traces and logs
- **Regression Detection**: Automated comparison against historical test runs

## 🔄 CI/CD Integration

### GitHub Actions Integration
The integration tests are fully integrated with GitHub Actions:

```yaml
# Workflows that use this infrastructure:
- integration-tests.yml      # Basic infrastructure validation
- stress-tests-confluent.yml # High-throughput scenarios  
- reliability-tests.yml      # Back pressure and rebalance
- backpressure-tests.yml     # Flow control validation
```

### Automated Quality Gates
- **Build Validation**: All solutions must build successfully
- **Test Execution**: All BDD scenarios must pass
- **Performance Thresholds**: Latency and throughput requirements must be met
- **Resource Monitoring**: Memory and CPU usage within acceptable limits

## 🛠️ Troubleshooting

### Common Issues

**Issue: Infrastructure fails to start**
```bash
# Solution: Clean restart
Ctrl+C  # Stop current processes
docker system prune -f  # Clean up containers
cd IntegrationTests/FlinkDotNet.Aspire.AppHost
dotnet run
```

**Issue: Out of memory during stress tests**
```bash
# Solution: Increase Docker resources
# Docker Desktop → Settings → Resources → Memory → 8GB+
# Then restart infrastructure
```

**Issue: Tests fail intermittently**
```bash
# Solution: Check service health first
curl http://localhost:18002/overview  # Flink health check
curl http://localhost:18889  # Aspire dashboard health check  
curl http://localhost:18001/api/actuator/health  # Kafka UI health
```

### Performance Tuning
- **Container Resources**: Ensure Docker has adequate CPU and memory
- **Network Configuration**: Verify no port conflicts with other services
- **Disk Space**: Ensure sufficient space for logs and test artifacts
- **Concurrent Tests**: Avoid running multiple test suites simultaneously

## 📚 Documentation and References

### Integration Test Documentation
- **BDD Scenarios**: See `Features/` folders in test projects for complete scenario definitions
- **Step Definitions**: Detailed step implementations in `StepDefinitions/` folders
- **Test Configuration**: Environment-specific settings in `appsettings.json` files

### Related Resources
- **[LearningCourse](../LearningCourse/README.md)** - Comprehensive training materials
- **[LocalTesting](../LocalTesting/README.md)** - Simple learning environment
- **[Main Project](../README.md)** - FlinkDotNet project overview

---

**This integration test infrastructure provides enterprise-grade testing capabilities for validating FlinkDotNet's real Apache Flink integration with comprehensive observability and professional reporting.**