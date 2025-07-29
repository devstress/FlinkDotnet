# Flink.NET Samples - Real Apache Flink 2.0 Integration

This directory contains samples demonstrating Flink.NET's **real Apache Flink 2.0 integration** for production Kubernetes deployment.

## 🏗️ Architecture Overview

The samples demonstrate the new approach using **real Flink 2.0 execution**:
- **Real Job Submission** - Actual job submission to live Flink 2.0 clusters via REST API
- **Kubernetes Integration** - Production-ready deployment patterns with K8s service discovery
- **No Simulation** - All jobs execute on actual Apache Flink 2.0 JobManager and TaskManager instances

```
┌─────────────────────┐   ┌──────────────────────┐   ┌─────────────────────┐
│  .NET Application   │──▶│  Flink Job Gateway   │──▶│  Apache Flink 2.0   │
│  (Job Builder)      │   │  (REST API)          │   │  (Real Execution)   │
└─────────────────────┘   └──────────────────────┘   └─────────────────────┘
```

## 📁 Sample Projects

### 🧪 FlinkDotnetStandardReliabilityTest
**Real Flink 2.0 Integration Reliability Test** - BDD-style tests with actual job execution

**Features:**
- **Real Job Submission** - Submits actual jobs to live Flink 2.0 cluster
- **BDD Testing** - Given/When/Then scenarios using SpecFlow
- **Apache Flink Integration** - Uses real Flink Job Gateway for job submission
- **Production Testing** - Validates end-to-end reliability with real infrastructure

**Test Coverage:**
- Real job submission and execution via Flink Job Gateway to Flink 2.0 cluster
- JSON Intermediate Representation (IR) translation to actual Flink DataStream jobs
- Job Gateway communication with real Flink 2.0 JobManager and TaskManager
- Apache Flink cluster integration with actual job execution and monitoring
- End-to-End reliability with real infrastructure and actual data processing

**Usage:**
```bash
cd FlinkDotnetStandardReliabilityTest
dotnet test --verbosity normal
```

**Prerequisites:**
- Real Flink 2.0 cluster running (JobManager + TaskManager)
- Flink Job Gateway running and connected to Flink cluster
- Kafka cluster running with required topics
- All services accessible and healthy

### 💡 FlinkJobBuilder.Sample
**Basic Streaming Examples with Real Flink 2.0 Integration**

**Features:**
- **Kubernetes Service Discovery** - Locates Flink Job Gateway via K8s service names
- **Real Job Execution** - All examples execute on actual Flink 2.0 cluster
- **Production Patterns** - Demonstrates enterprise deployment patterns
- **Input Validation** - Comprehensive error handling and validation

**Examples:**
1. **Basic Streaming** - Kafka to Kafka with filtering (real execution)
2. **Aggregation** - Windowed user click counting (real execution)
3. **Windowed Processing** - Sliding window sensor data analysis (real execution)

**Usage:**
```bash
cd FlinkJobBuilder.Sample
dotnet run
```

**Configuration:**
```csharp
builder.Services.AddFlinkJobBuilder(config =>
{
    // Use Kubernetes service discovery to locate the Flink Job Gateway
    var gatewayHost = builder.Configuration.GetValue<string>("FLINK_JOB_GATEWAY_HOST") ?? "flink-job-gateway";
    var gatewayPort = builder.Configuration.GetValue<int>("FLINK_JOB_GATEWAY_PORT", 8080);
    
    config.BaseUrl = $"http://{gatewayHost}:{gatewayPort}";
    config.HttpTimeout = TimeSpan.FromMinutes(5);
    config.MaxRetries = 3;
    config.UseHttps = false; // Use HTTP for internal K8s communication
});
```

## 🚀 Getting Started

### Prerequisites
- .NET 8.0 SDK
- Apache Flink 2.0 cluster (JobManager + TaskManager)
- Flink Job Gateway service
- Kafka cluster for data streaming
- kubectl for Kubernetes deployment

### Production Deployment with Kubernetes

1. **Deploy Flink 2.0 cluster:**
   ```bash
   kubectl apply -f k8s/
   kubectl get pods -n flink-system
   ```

2. **Verify Flink cluster is running:**
   ```bash
   # Check Flink UI
   kubectl port-forward svc/flink-jobmanager-ui 8081:8081 -n flink-system
   
   # Access at http://localhost:8081
   ```

3. **Test Job Gateway connectivity:**
   ```bash
   # Access Job Gateway API
   kubectl port-forward svc/flink-job-gateway 8080:8080 -n flink-system
   
   # Test API
   curl http://localhost:8080/api/v1/health
   ```

4. **Run sample applications:**
   ```bash
   # Basic examples with real job execution
   cd FlinkJobBuilder.Sample
   dotnet run
   
   # Reliability tests with real Flink integration
   cd FlinkDotnetStandardReliabilityTest
   dotnet test
   ```

## 🔧 Configuration

### Kubernetes Service Discovery

Applications automatically discover Flink Job Gateway using Kubernetes service names:

```yaml
# Environment variables for service discovery
- name: FLINK_JOB_GATEWAY_HOST
  value: "flink-job-gateway"
- name: FLINK_JOB_GATEWAY_PORT
  value: "8080"
```

### Real Flink 2.0 Integration

All samples integrate with actual Apache Flink 2.0 clusters:

```csharp
// Real job submission to Flink 2.0 cluster
var submissionResult = await jobBuilder.Submit("RealFlinkTest");
if (submissionResult.IsSuccess) {
    await MonitorRealJobExecution(submissionResult.FlinkJobId);
}
```

### Multi-Environment Support

Configure different environments for development and production:

```yaml
# Development environment (local Flink cluster)
FLINK_JOB_GATEWAY_HOST: "localhost"
FLINK_JOB_GATEWAY_PORT: "8080"

# Production environment (K8s Flink cluster)
FLINK_JOB_GATEWAY_HOST: "flink-job-gateway.flink-system.svc.cluster.local"
FLINK_JOB_GATEWAY_PORT: "8080"
```

## 🧪 Testing Strategy

### Real Integration Testing
- **Infrastructure validation** - Real Flink 2.0 cluster connectivity
- **Job submission** - Actual job submission and execution
- **End-to-end validation** - Complete workflow testing with real data processing
- **Performance testing** - Real-world throughput and latency validation

### BDD Test Scenarios
All tests use SpecFlow with real Flink execution:
- Application startup with real Flink connectivity
- Job submission and execution validation
- Error handling and resilience testing with real infrastructure
- Performance and reliability testing under load

## 📊 Monitoring and Observability

### Real-time Monitoring
When connected to actual Flink 2.0 cluster:

```bash
# Flink 2.0 Web UI (real cluster)
http://localhost:8081

# Job Gateway Health (real API)
curl http://localhost:8080/actuator/health

# Real job metrics and status
curl http://localhost:8081/api/v1/jobs
curl http://localhost:8081/api/v1/jobs/{job-id}/metrics
```

### Production Metrics
- **Real job execution metrics** from Flink 2.0 cluster
- **Throughput and latency** measurements from actual data processing
- **Resource utilization** monitoring of JobManager and TaskManager instances
- **Health checks** for all real infrastructure components

## 🔄 CI/CD Integration

### GitHub Actions for Real Integration
```yaml
- name: Deploy Flink 2.0 Cluster
  run: |
    kubectl apply -f k8s/
    kubectl wait --for=condition=Ready pod -l app=flink-jobmanager -n flink-system

- name: Run Real Integration Tests
  run: |
    cd Sample/FlinkDotnetStandardReliabilityTest
    dotnet test --verbosity normal

- name: Test Sample Applications
  run: |
    cd Sample/FlinkJobBuilder.Sample
    dotnet run
```

### Deployment Pipeline with Real Validation
1. **Deploy** - Real Flink 2.0 cluster and Job Gateway
2. **Validate** - Health checks for all real services  
3. **Test** - Execute samples with real job submission
4. **Monitor** - Validate real job execution and performance

## 📚 Documentation

### Sample Documentation
- **FlinkJobBuilder.Sample/README.md** - Real integration examples
- **k8s/README.md** - Kubernetes deployment guide for Flink 2.0

### API Documentation
- **Flink Job Gateway** - Real REST API documentation
- **Apache Flink 2.0** - Official Flink documentation and monitoring
- **FlinkJobBuilder SDK** - XML documentation for .NET integration

## 🤝 Contributing

When adding new samples:

1. **Use real Flink 2.0 integration** - No simulation or mocking
2. **Include integration tests** with actual job execution
3. **Follow Kubernetes patterns** for production deployment
4. **Document real-world usage** and configuration patterns
5. **Test with actual infrastructure** for validation

## 🔗 Related Resources

- [Apache Flink 2.0 Documentation](https://flink.apache.org/docs/)
- [Kubernetes Documentation](https://kubernetes.io/docs/)
- [Flink REST API Documentation](https://nightlies.apache.org/flink/flink-docs-stable/docs/ops/rest_api/)
- [.NET Integration Patterns](https://docs.microsoft.com/en-us/dotnet/)

---

**All samples demonstrate real Apache Flink 2.0 integration with actual job execution and production-ready Kubernetes deployment patterns.**