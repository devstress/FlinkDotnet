# LocalTesting - Aspire Environment for LearningCourse

This Aspire setup provides the infrastructure environment for the [LearningCourse](../LearningCourse/README.md). Please refer to the LearningCourse documentation for complete usage instructions and examples.

## Message Flow Architecture

The LocalTesting environment implements a comprehensive message processing pipeline with real-time observability:

```
📥 Ingress (Single Topic)
    ↓
🔀 Kafka Producers (10 partitions)
  • Single ingress topic: "ingress-topic"
  • Partitions: partition0, partition1, ..., partition9
  • Rate: ~80,000+ msg/sec per partition
    ↓
📨 Kafka → Flink Processing
  • Flink consumes from all partitions
  • Input rate = Kafka consuming rate  
  • Processing: Real-time stream processing
    ↓
⚡ Flink Jobs (2 parallel jobs)
  • Job: real-job-1, real-job-2
  • Input operators: kafka-source
  • Output operators: kafka-sink
  • Processing latency: ~2ms per message
    ↓
🔄 Temporal Workflows (Subset Processing)
  • Triggered by: ~10% of messages (workflow patterns)
  • Purpose: Stateful workflow orchestration
  • Types: RealWorkflow1, RealWorkflow2, RealWorkflow3
  • Activities: RealActivity1, RealActivity2
    ↓
📤 Final Output Topic
  • All messages processed through pipeline
  • Expected: Same count as ingress (no loss in healthy system)
  • Rate: ~99,000+ msg/sec end-to-end
```

### Component Performance Characteristics

- **Kafka Producing**: ~80,000 msg/sec per partition
- **Kafka Consuming**: Flink input rate (same as producing)
- **Flink Processing**: ~99,000 msg/sec (parallel jobs)
- **Temporal Processing**: ~10 workflows/sec (subset of messages, 10% volume)
- **End-to-End**: ~99,000 msg/sec (total pipeline throughput)

## Temporal Workflow Complex Orchestration Tasks

The Temporal workflows in LocalTesting implement enterprise-scale orchestration patterns for managing Flink clusters and job distribution. Here's what each workflow type actually executes:

### Core Workflow Types

#### 1. Cluster Orchestration Workflow (`IClusterOrchestratorWorkflow`)
**Purpose**: Enterprise actor workflow patterns for massive scale cluster management

**Complex Orchestration Tasks**:
- **Multi-Cluster Provisioning**: Dynamically provisions new Flink clusters based on workload demand
- **Resource Allocation**: Intelligent allocation of CPU, memory, and network resources across clusters
- **Auto-Scaling Logic**: Monitors cluster utilization and scales clusters up/down based on thresholds
- **Health Monitoring**: Continuous health checks across all managed clusters
- **Failure Recovery**: Automatic detection and recovery from cluster failures
- **Load Balancing**: Distributes workload evenly across available clusters

**Example Execution Flow**:
```
1. Receive orchestration request (target: 50 clusters, min: 5, max: 100)
2. Assess current cluster inventory and capacity
3. Calculate optimal cluster distribution across availability zones
4. Provision new clusters in parallel (if needed)
5. Configure cluster networking and security settings
6. Register clusters with service discovery
7. Validate cluster health and readiness
8. Begin workload distribution
```

#### 2. Job Distribution Workflow (`IJobDistributionWorkflow`)  
**Purpose**: Intelligent job distribution across multiple clusters with placement strategies

**Complex Orchestration Tasks**:
- **Job Placement Strategies**: Implements BestFit, LeastLoaded, RoundRobin, and LocalityFirst algorithms
- **Resource Matching**: Matches job requirements (CPU, memory, parallelism) with cluster capacity
- **Dependency Resolution**: Handles job dependencies and execution ordering
- **Backpressure Management**: Monitors and responds to cluster backpressure conditions
- **Failover Orchestration**: Automatically migrates jobs from failed clusters
- **Performance Optimization**: Continuously optimizes job placement for maximum throughput

**Example Execution Flow**:
```
1. Receive job distribution request (1000 jobs, BestFit strategy)
2. Analyze job resource requirements and constraints
3. Query cluster capacity and current utilization
4. Calculate optimal placement using BestFit algorithm
5. Submit jobs to selected clusters in parallel
6. Monitor job startup and validate successful deployment
7. Track job progress and handle any placement failures
8. Report distribution results with placement metrics
```

#### 3. Cluster Lifecycle Workflow (`IClusterLifecycleWorkflow`)
**Purpose**: Complete lifecycle management from provisioning to decommissioning

**Complex Orchestration Tasks**:
- **Cluster Provisioning**: Creates new Flink clusters with specified configurations
- **Configuration Management**: Applies cluster-specific configurations and policies
- **Service Integration**: Integrates clusters with monitoring, logging, and service mesh
- **Upgrade Management**: Handles rolling upgrades and version migrations
- **Capacity Planning**: Manages cluster scaling based on historical usage patterns
- **Decommissioning**: Safely drains and removes clusters when no longer needed

**Example Execution Flow**:
```
1. Start cluster lifecycle management (cluster-id: prod-cluster-01)
2. Provision infrastructure resources (VMs, networking, storage)
3. Install and configure Flink software stack
4. Set up monitoring and alerting for the cluster
5. Register cluster with load balancer and service discovery
6. Run health validation tests
7. Mark cluster as ready for workload
8. Monitor throughout operational lifetime
9. Handle upgrade/migration requests
10. Gracefully decommission when lifecycle ends
```

#### 4. Auto-Scaling Workflow (`IAutoScalingWorkflow`)
**Purpose**: Continuous monitoring and automatic scaling based on demand

**Complex Orchestration Tasks**:
- **Metrics Collection**: Continuously gathers CPU, memory, and throughput metrics
- **Threshold Analysis**: Analyzes metrics against configured scaling thresholds
- **Scaling Decisions**: Calculates optimal scaling actions (scale up/down/maintain)
- **Coordination**: Coordinates scaling actions across multiple clusters
- **Cooldown Management**: Implements cooldown periods to prevent oscillation
- **Predictive Scaling**: Uses historical patterns for proactive scaling

**Example Execution Flow**:
```
1. Start continuous auto-scaling monitoring
2. Collect metrics from all managed clusters every 5 minutes
3. Analyze CPU utilization against thresholds (scale up >80%, scale down <30%)
4. Calculate scaling requirements based on current and predicted load
5. If scaling needed, coordinate with cluster orchestration workflow
6. Execute scaling actions (add/remove task managers)
7. Wait for cooldown period (10 minutes)
8. Validate scaling effectiveness
9. Continue monitoring loop
```

### Workflow Execution Context

#### Activity Types Executed

**RealActivity1 - Resource Provisioning**:
- Creates cloud infrastructure resources (VMs, networks, storage)
- Configures security groups and access policies
- Sets up monitoring and logging agents

**RealActivity2 - Configuration Management**:
- Applies Flink cluster configurations
- Manages application-specific settings
- Handles configuration validation and rollback

**RealActivity3 - Health Validation** (Additional):
- Performs cluster health checks
- Validates network connectivity
- Tests job submission capabilities

#### Workflow Patterns and Execution

**Message Volume Triggering**:
- **10% of messages** now trigger Temporal workflows (updated from 0.2%)
- Higher volume enables more comprehensive orchestration testing
- Workflows execute in parallel for maximum throughput

**Execution Characteristics**:
- **Parallel Execution**: Multiple workflows run concurrently for different clusters
- **State Management**: Workflows maintain state across long-running operations
- **Error Handling**: Comprehensive retry logic and failure recovery
- **Timeout Management**: Configurable timeouts for different operation types

**Performance Impact**:
- Temporal workflows handle complex stateful orchestration tasks
- Each workflow may manage multiple clusters simultaneously  
- Execution time varies from seconds (health checks) to hours (cluster lifecycle)
- Resource requirements scale with cluster count and complexity

### Integration with Flink Pipeline

The Temporal workflows integrate with the main Flink processing pipeline in several ways:

1. **Triggered by Message Patterns**: Specific message types trigger workflow execution
2. **Cluster Management**: Workflows manage the Flink clusters that process the main message flow
3. **Dynamic Scaling**: Auto-scaling workflows adjust cluster capacity based on message throughput
4. **Job Distribution**: Job distribution workflows place Flink jobs for optimal message processing

This creates a complete orchestration system where Temporal manages the infrastructure that processes the high-volume message streams.

### Observability Metrics

Real-time metrics are available via:
- **Prometheus**: http://localhost:18006 (metrics collection)
- **Grafana**: http://localhost:18007 (dashboards)
- **WebAPI**: http://localhost:44273/api/observability/metrics/messages-per-second

## Prerequisites

### .NET SDK Requirements
- **.NET 9.0 SDK or later** is required for proper Aspire testing framework functionality
- Check your version: `dotnet --version` (should show 9.0.x)
- Install from: https://dotnet.microsoft.com/download/dotnet/9.0

### Why .NET 9.0 is Required
- Aspire testing framework (`Aspire.Hosting.Testing`) is designed for .NET 9.0
- Integration tests will fail to build or run properly with .NET 8.0
- The observability test uses `DistributedApplicationTestingBuilder` which requires .NET 9.0

### Environment Verification
```bash
# Verify .NET version
dotnet --version  # Should show 9.0.x

# Build LocalTesting solution
dotnet build LocalTesting.sln

# Run LocalTesting Aspire orchestrator
cd LocalTesting.AppHost && dotnet run

# Run Observability tests
dotnet test LocalTesting/LocalTesting.IntegrationTests/LocalTesting.IntegrationTests.csproj --filter "Category=observability"
```