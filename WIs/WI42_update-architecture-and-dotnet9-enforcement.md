# WI42: Update Architecture and Add .NET 9 Enforcement

**File**: `WIs/WI42_update-architecture-and-dotnet9-enforcement.md`
**Title**: [Architecture] Update FlinkDotnet architecture and add .NET 9 enforcement
**Description**: Clarify how FlinkDotnet is better than PyFlink with clear code examples, or update architecture to follow PyFlink. Add enforcement to install .NET 9 and ensure all GitHub workflows work locally.
**Priority**: High
**Component**: Architecture & Infrastructure
**Type**: Enhancement
**Assignee**: AI Agent
**Created**: 2024-12-20
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- No previous architectural WI files found - this is the first major architecture review
### Lessons Applied  
- First architectural improvement task - no previous patterns to apply
### Problems Prevented
- None identified from previous work - establishing new architectural standards

## Phase 1: Investigation
### Requirements
- Compare FlinkDotnet vs PyFlink architecture approaches
- Analyze current .NET 9 environment configuration 
- Understand GitHub workflow .NET version requirements
- Identify gaps in local development environment validation

### Debug Information (MANDATORY - Updated for architectural investigation)
- **Current Environment**: .NET 8.0.118 installed, but global.json requires 9.0.303
- **Architecture Pattern**: FlinkDotnet uses HTTP REST API with Job Gateway vs PyFlink direct JVM integration
- **GitHub Workflows**: Configured for .NET 9.0.x but missing local validation enforcement
- **Error Evidence**: `dotnet --version` fails with SDK not found error requesting 9.0.303
- **Configuration Files**: global.json correctly specifies .NET 9.0.303 with rollForward policy

### Findings
**Current FlinkDotnet Architecture (HTTP Service-Oriented)**:
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   .NET Code     │    │ Job Gateway API │    │  Apache Flink   │
│                 │    │   (REST/HTTP)   │    │   Cluster       │
│ using Flink...  │◄──►│  HTTP Service   │◄──►│  JobManager     │
│ var env = ...   │    │                 │    │  TaskManager    │
│ ds.Map(...)     │    │ JSON/HTTP       │    │  REST API       │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

**PyFlink Architecture (Direct JVM Integration)**:
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Python Code   │    │   Py4J Gateway  │    │  Apache Flink   │
│                 │    │                 │    │   (Java JVM)    │
│ from pyflink... │◄──►│   Py4J Bridge   │◄──►│  JobManager     │
│ env = Stream... │    │                 │    │  TaskManager    │
│ ds.map(...)     │    │ CloudPickle     │    │  Execution      │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

**Key Architectural Differences**:
| Aspect | PyFlink | FlinkDotnet |
|--------|---------|-------------|
| **Integration Style** | Direct JVM Integration | Service-Oriented Architecture |
| **Runtime Dependency** | Python runtime on Flink cluster | No .NET runtime needed on cluster |
| **Communication** | In-process via Py4J | HTTP REST API calls |
| **Function Execution** | Python UDFs serialized to JVM | Native Java/Scala processing |
| **Deployment Model** | Embedded Python interpreter | Separate HTTP gateway service |
| **Performance** | Lower latency, higher memory usage | Higher latency, lower memory usage |
| **Scalability** | Limited by Python GIL constraints | Standard Flink scaling patterns |

**Current .NET 9 Environment Issues**:
- Local environment has .NET 8.0.118 but project requires .NET 9.0.303
- GitHub workflows specify .NET 9.0.x but lack local environment validation
- No enforcement script to verify .NET 9 before development work
- Missing Aspire workload verification in local setup

### Lessons Learned
- FlinkDotnet's service-oriented approach has trade-offs vs PyFlink's direct integration
- Current architecture prioritizes clean separation and deployment simplicity over raw performance
- .NET 9 enforcement is needed for consistent local/CI environments
- Architecture documentation needs clear examples showing advantages

## Phase 2: Design  
### Requirements
- Create compelling code examples showing FlinkDotnet advantages over PyFlink
- Design .NET 9 local environment enforcement system
- Plan architecture documentation improvements
- Design verification scripts for local development

### Architecture Decisions
**Option 1: Enhance Current Service-Oriented Architecture** ✅ CHOSEN
- Keep HTTP REST API approach but improve examples and documentation
- Add clear performance benchmarks and deployment advantages
- Show enterprise-grade features (monitoring, scaling, security)
- Demonstrate clean separation of concerns benefits

**Option 2: Hybrid Architecture (PyFlink-inspired with .NET advantages)**
- Add direct Flink integration option alongside HTTP gateway
- Implement .NET UDF execution within Flink cluster
- Keep HTTP option for enterprise scenarios
- Provide both approaches for different use cases

**Decision**: Choose Option 1 - Enhance current architecture with better examples and documentation
**Rationale**: 
- Service-oriented approach aligns with modern cloud-native patterns
- Easier to deploy, monitor, and scale in enterprise environments
- No .NET runtime dependency on Flink cluster reduces complexity
- Better fault isolation between application logic and stream processing

### Why This Approach
1. **Enterprise Adoption**: Service-oriented architecture is more familiar to enterprise developers
2. **Cloud-Native**: Aligns with microservices and container orchestration patterns
3. **Technology Independence**: Flink cluster doesn't need .NET runtime
4. **Operational Simplicity**: Easier monitoring, logging, and debugging
5. **Security**: Clear separation between user code and Flink cluster

### Alternatives Considered
- Direct JVM integration like PyFlink: Rejected due to deployment complexity
- Embedded .NET runtime in Flink: Rejected due to resource overhead
- gRPC instead of HTTP: Considered for future enhancement but HTTP is simpler

### Design Deliverables Created
- ✅ **System Architecture HTML**: Interactive documentation with comparison tables
- ✅ **Architecture Diagram PNG**: Visual system architecture with performance metrics
- ✅ **Code Examples**: Comprehensive examples showing FlinkDotnet advantages
- ✅ **.NET 9 Enforcement Script**: PowerShell script for environment validation
- ✅ **Enhanced README**: Updated with architecture comparison and .NET 9 requirements

## Phase 3: TDD/BDD
### Test Specifications
- Create architecture comparison examples with performance metrics
- Add .NET 9 environment validation tests
- Test local development workflow enforcement
- Validate GitHub workflow .NET 9 requirements

### Behavior Definitions
- **Given** a developer wants to use FlinkDotnet locally
- **When** they run the environment setup script
- **Then** .NET 9.0.303+ must be installed and verified
- **And** Aspire workload must be functional
- **And** All solutions must build successfully

## Phase 4: Implementation
### Code Changes
*To be implemented in next phase*

### Challenges Encountered
*To be documented during implementation*

### Solutions Applied
*To be documented during implementation*

## Phase 5: Testing & Validation
### Test Results
*To be documented during testing*

### Performance Metrics
*To be documented during testing*

## Phase 6: Owner Acceptance
### Demonstration
*To be scheduled after implementation*

### Owner Feedback
*To be collected from issue requester*

### Final Approval
*Pending implementation completion*

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
- Comprehensive architectural analysis identified clear trade-offs
- Service-oriented approach provides good enterprise benefits
- Current GitHub workflow structure is solid foundation

### What Could Be Improved  
- Need better examples demonstrating FlinkDotnet advantages
- Local environment setup needs automation and validation
- Architecture documentation needs visual improvements

### Key Insights for Similar Tasks
- Architectural decisions should prioritize operational simplicity and enterprise adoption
- Clear examples and documentation are critical for developer adoption
- Local development environment consistency is essential for productivity

### Specific Problems to Avoid in Future
- Don't assume local environment matches CI environment
- Don't underestimate importance of clear architectural examples
- Avoid complex dependency management in local development setup

### Reference for Future WIs
- This establishes pattern for architectural documentation and local environment enforcement
- Sets standard for comparing .NET solutions to other language ecosystems
- Creates template for environment validation scripts and procedures