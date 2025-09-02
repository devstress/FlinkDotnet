# FlinkDotNet Integration Tests - BDD Test Environment

This directory contains **integration test infrastructure** with comprehensive BDD scenarios for validating FlinkDotNet's real Apache Flink integration capabilities. The focus is on **integration testing**, not sample code.

## 🧪 Purpose and Architecture

This infrastructure provides automated integration testing for:
- **Real job submission** to live Flink clusters via REST API
- **BDD testing scenarios** using SpecFlow/ReqNRoll
- **Enterprise observability** with comprehensive monitoring stack
- **Multi-environment testing** with Docker orchestration

```
┌─────────────────────┐   ┌──────────────────────┐   ┌─────────────────────┐
│  Integration Tests  │──▶│  Aspire AppHost      │──▶│  Apache Flink       │
│  (BDD Scenarios)    │   │  (Modern Stack)      │   │  (Real Execution)   │
└─────────────────────┘   └──────────────────────┘   └─────────────────────┘
```

## 📁 Project Structure

### 🎯 Integration Test Projects

#### **FlinkDotNet.Aspire.IntegrationTests**
**BDD Integration Test Suite** - SpecFlow/ReqNRoll scenarios for comprehensive testing

**Test Categories:**
- **IntegrationTest** - Basic container infrastructure validation
- **stress** - High throughput and scalability testing (1M+ messages)
- **reliability_test** - Back pressure and rebalance scenarios
- **backpressure_test** - Flow control and rate limiting validation

**Features:**
- **Real Flink Integration** - Tests against actual Flink clusters
- **BDD Scenarios** - Given/When/Then test specifications
- **Allure Reporting** - Professional test reports with charts and metrics
- **Multi-environment** - Local, CI/CD, and production testing support

#### **FlinkDotnetStandardReliabilityTest**
**Reliability and Performance Validation** - Long-running stability tests

**Features:**
- **End-to-end reliability** testing with real infrastructure
- **Performance benchmarking** with throughput and latency metrics
- **Fault tolerance** validation under various failure conditions
- **Resource utilization** monitoring and optimization

#### **FlinkJobBuilder.Sample**
**API Integration Examples** - Demonstrates FlinkDotNet API usage in test scenarios

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
curl http://localhost:18002/api/v1/cluster/overview  # Flink health
curl http://localhost:18888  # Aspire dashboard health
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