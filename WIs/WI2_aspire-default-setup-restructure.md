# WI2: Restructure LocalTesting to Use Default Aspire Components

**File**: `WIs/WI2_aspire-default-setup-restructure.md`
**Title**: [LocalTesting] Restructure to use default Aspire setup for all components
**Description**: Replace complex custom container configurations with standard Aspire hosting components like Aspire.Hosting.Kafka to simplify setup and ensure reliability
**Priority**: High
**Component**: LocalTesting.AppHost
**Type**: Enhancement
**Assignee**: @copilot
**Created**: 2024-12-19
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI1: LocalTesting cleanup (file removal and organization)
### Lessons Applied  
- Maintain working observability tests while making changes
- Use validation scripts to ensure no regressions
- Keep changes focused and incremental
### Problems Prevented
- Breaking existing functionality during restructuring
- Removing essential files accidentally

## Phase 1: Investigation
### Requirements
The user (@devstress) requested restructuring LocalTesting to use default Aspire components instead of complex custom container setups. Specifically mentioned:
- Use `Aspire.Hosting.Kafka` instead of complicated images and ports
- Apply this approach to everything 
- Ensure observability tests work and pass
- Reference: https://learn.microsoft.com/en-us/dotnet/aspire/messaging/kafka-integration?tabs=dotnet-cli

### Debug Information (MANDATORY - Update this section for every investigation)
- **Current Setup Analysis**: 
  - Currently using custom container configurations for Kafka, Redis, Prometheus, Flink
  - Package `Aspire.Hosting.Kafka` v9.1.0 already referenced but not used
  - Package `Aspire.Hosting.Redis` v9.1.0 already referenced but used correctly
  - Custom Kafka container setup at lines 34-46 in Program.cs
  - Complex port configuration system in PortConstants.cs

- **Current Working Components**:
  - Redis: Already using `builder.AddRedis()` - correct Aspire pattern
  - Kafka: Using custom container setup - needs restructuring
  - Flink: Using custom containers - may need custom approach due to lack of official Aspire support
  - Prometheus: Using custom container - should check for Aspire alternatives

- **Observability Test Requirements**:
  - Tests in `LocalTesting.IntegrationTests` with category "observability"
  - Validates LocalTesting infrastructure starts correctly
  - Connects to WebAPI and validates metrics
  - Must continue to pass after restructuring

### Findings
1. **Redis**: Already properly configured with Aspire.Hosting.Redis
2. **Kafka**: Can be replaced with AddKafka() from Aspire.Hosting.Kafka
3. **Flink**: No official Aspire hosting extension - may need to keep custom containers
4. **Prometheus**: Check if Aspire has built-in support or continue with custom container
5. **Port Constants**: May need simplification as Aspire handles port allocation automatically

### Investigation Plan
1. Research available Aspire hosting extensions for each component
2. Identify which components can use default Aspire setup vs custom containers
3. Plan incremental migration approach
4. Ensure observability tests continue to work

### Lessons Learned
- Need to balance simplification with functionality requirements
- Some components may not have official Aspire support yet

## Phase 2: Design  
### Requirements
[To be completed]

## Phase 3: TDD/BDD
### Requirements
[To be completed]

## Phase 4: Implementation
### Requirements
[To be completed]

## Phase 5: Testing & Validation
### Requirements
[To be completed]

## Phase 6: Owner Acceptance
### Requirements
[To be completed]

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
[To be documented during implementation]

### What Could Be Improved  
[To be documented during implementation]

### Key Insights for Similar Tasks
[To be documented during implementation]

### Specific Problems to Avoid in Future
[To be documented during implementation]

### Reference for Future WIs
[To be documented during implementation]