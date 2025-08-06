# WI49: Restructure FlinkDotNet with Netflix Temporal + Flink Architecture

**File**: `WIs/WI49_restructure-flinkdotnet-netflix-temporal-flink-architecture.md`
**Title**: [Architecture] Restructure FlinkDotNet using Netflix Temporal + Flink Actor Workflows Architecture  
**Description**: Redesign FlinkDotNet using Netflix's "Actor Workflows: Reliably orchestrating thousands of Flink clusters" architecture with Temporal and Apache Flink for full fault tolerance, massive scale, exactly-once processing, no data loss, resilience, and 99.999% availability.
**Priority**: High
**Component**: Core Architecture
**Type**: Enhancement/Restructuring
**Assignee**: AI Agent
**Created**: 2025-01-28
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- Reviewed WIs folder for similar architectural changes
- WI40_refactor-flinkdotnet-to-match-python-structure.md - Architectural refactoring patterns
- WI41_flinkdotnet-vs-pyflink-architecture-clarity.md - Architecture clarity principles
- docs/flink-vs-temporal-decision-guide.md - Existing Temporal integration guidance

### Lessons Applied  
- Maintain backward compatibility with existing FlinkDotNet.DataStream API
- Use modular architecture to add new capabilities without breaking existing functionality
- Follow existing patterns for .NET 9.0 and Aspire integration
- Leverage existing Temporal infrastructure in LocalTesting project
- Apply SOLID principles for component separation

### Problems Prevented
- Avoid breaking existing FlinkJobBuilder API that users depend on
- Prevent loss of current stress testing and backpressure capabilities
- Maintain existing GitHub workflow infrastructure while enhancing it

## Phase 1: Investigation
### Requirements
Analyze Netflix's Temporal + Flink architecture and current FlinkDotNet implementation to design a restructuring plan that adds massive scale orchestration capabilities while preserving existing functionality.

### Debug Information (MANDATORY - Update this section for every investigation)
- **Error Messages**: No current errors - this is enhancement work
- **Log Locations**: N/A - architectural enhancement
- **System State**: Current FlinkDotNet works with single-cluster Flink deployments
- **Reproduction Steps**: Review video https://youtu.be/ybm86vpkpyo?si=qud4ixJctAebeZXQ for Netflix architecture
- **Evidence**: Current architecture supports single Flink cluster, needs multi-cluster orchestration

### Current Architecture Analysis
**FlinkDotNet Current Components:**
- FlinkDotNet.Common - Core types and configuration
- FlinkDotNet.DataStream - Streaming API (.NET wrapper for Flink)
- Flink.JobBuilder - Fluent C# DSL for job construction
- Flink.JobGateway - HTTP service bridging .NET apps with Flink clusters
- FlinkDotNet.Testing - Testing utilities
- LocalTesting with basic Temporal simulation

**Current Flow:**
```
.NET App → FlinkDotNet Gateway → Single Apache Flink JobManager → TaskManagers
```

**Limitations:**
- Single cluster deployment
- No automatic cluster lifecycle management
- Limited fault tolerance across clusters
- No actor-based cluster orchestration
- Basic Temporal integration (simulation only)

### Netflix Temporal + Flink Architecture Analysis

**Netflix Architecture Principles** (from video reference):
- **Actor Workflows**: Each Flink cluster managed as an actor with lifecycle
- **Temporal Orchestration**: Durable workflows for cluster management
- **Massive Scale**: Orchestrate thousands of Flink clusters
- **Resilience**: Exactly-once processing, no data loss, 99.999% availability
- **Fault Recovery**: Automatic cluster failure detection and recovery

**Key Netflix Components:**
1. **Temporal Workflows** - Cluster lifecycle management
2. **Actor System** - Individual cluster actors
3. **Cluster Autoscaler** - Dynamic provisioning
4. **Job Router** - Intelligent job placement across clusters
5. **Health Monitor** - Cluster health and performance tracking
6. **State Manager** - Durable cluster state persistence

### Required Architecture Changes

**New Architecture Flow:**
```
.NET App → FlinkDotNet.Orchestra → Temporal Workflows → Cluster Actors → Multiple Flink Clusters
```

**New Components Needed:**
1. **FlinkDotNet.Orchestra** - Multi-cluster job orchestration service
2. **FlinkDotNet.ClusterManager** - Actor-based cluster lifecycle management
3. **FlinkDotNet.Temporal** - Full Temporal workflows integration
4. **FlinkDotNet.Resilience** - Circuit breakers, retries, health checks
5. **FlinkDotNet.Monitoring** - Cluster observability and metrics
6. **FlinkDotNet.Autoscaler** - Dynamic cluster provisioning

### Findings
- Current implementation has basic Temporal simulation in LocalTesting
- Existing FlinkDotNet.Gateway can be enhanced rather than replaced
- .NET 9.0 and Aspire provide good foundation for microservices architecture
- Need to add proper Temporal.io SDK integration
- Existing stress testing framework can validate new architecture

### Lessons Learned
- Current architecture is well-structured for extension
- Temporal integration exists but needs full implementation
- Backward compatibility is achievable through layered approach

## Phase 2: Design  
### Requirements
Design new architecture components that add Netflix-style orchestration while preserving existing functionality.

### Architecture Decisions

**1. Hybrid Approach - Preserve + Extend**
- Keep existing FlinkDotNet.DataStream API for backward compatibility
- Extend FlinkDotNet.Gateway with multi-cluster awareness
- Add new orchestration layer above existing components

**2. Actor-Based Cluster Management**
```csharp
public interface IFlinkClusterActor
{
    Task<ClusterStatus> GetStatusAsync();
    Task<JobSubmissionResult> SubmitJobAsync(FlinkJobDefinition job);
    Task<bool> ScaleAsync(int parallelism);
    Task RestartAsync();
    Task ShutdownAsync();
}
```

**3. Temporal Workflow Orchestration**
```csharp
[Workflow]
public interface IClusterOrchestratorWorkflow
{
    [WorkflowMethod]
    Task OrchestrateClustersAsync(OrchestrationRequest request);
}

[Workflow]
public interface IJobDistributionWorkflow
{
    [WorkflowMethod]
    Task DistributeJobsAsync(List<FlinkJobDefinition> jobs);
}
```

**4. Enhanced Gateway Architecture**
```csharp
public interface IFlinkOrchestra
{
    Task<JobSubmissionResult> SubmitJobAsync(FlinkJobDefinition job, SubmissionStrategy strategy);
    Task<ClusterInfo[]> GetAvailableClustersAsync();
    Task<ClusterActor> ProvisionClusterAsync(ClusterConfiguration config);
    Task<HealthReport> GetClusterHealthAsync();
}
```

### Why This Approach
- **Minimal Disruption**: Existing APIs continue to work
- **Scalability**: Can orchestrate thousands of clusters like Netflix
- **Fault Tolerance**: Temporal provides durable execution
- **Observability**: Enhanced monitoring across all clusters
- **Performance**: Intelligent job placement and load balancing

### Alternatives Considered
- **Complete Rewrite**: Rejected - too disruptive for users
- **Separate Service**: Rejected - prefer integrated approach
- **Different Orchestrator**: Considered Kubernetes Jobs, but Temporal better for complex workflows

## Phase 3: TDD/BDD
### Test Specifications
1. **Multi-Cluster Job Distribution Tests**
2. **Cluster Failure Recovery Tests**
3. **Temporal Workflow Integration Tests**
4. **Actor Lifecycle Management Tests**
5. **Backward Compatibility Tests**

### Behavior Definitions
- **Given** multiple Flink clusters are available
- **When** a job is submitted to FlinkDotNet.Orchestra  
- **Then** the job should be placed on the optimal cluster
- **And** cluster health should be monitored continuously
- **And** failures should trigger automatic recovery workflows

## Phase 4: Implementation
### Code Changes
[To be completed during implementation]

### Challenges Encountered
[To be documented during implementation]

### Solutions Applied
[To be documented during implementation]

## Phase 5: Testing & Validation
### Test Results
[To be completed after implementation]

### Performance Metrics
[To be completed after implementation]

## Phase 6: Owner Acceptance
### Demonstration
[To be completed for owner review]

### Owner Feedback
[To be collected from issue owner]

### Final Approval
[Pending implementation completion]

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
[To be documented after completion]

### What Could Be Improved  
[To be documented after completion]

### Key Insights for Similar Tasks
[To be documented after completion]

### Specific Problems to Avoid in Future
[To be documented after completion]

### Reference for Future WIs
[To be documented after completion]