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
Based on investigation findings, design the restructuring approach:

**Components to Restructure:**
1. ✅ **Kafka**: Replace with `builder.AddKafka()` - COMPLETED
2. ❌ **Prometheus**: No official Aspire hosting package available - keep custom container but simplify
3. ❌ **Flink**: No official Aspire hosting package - keep custom containers but review configuration
4. ❓ **Temporal**: Has `InfinityFlow.Aspire.Temporal` package available but not currently used in setup
5. ✅ **Redis**: Already using proper Aspire pattern

**Configuration Simplification:**
- Remove unnecessary port constant complexity where Aspire handles allocation
- Maintain only essential environment variables for services that need them
- Keep performance optimizations in Kafka producer service
- Preserve observability test compatibility

### Architecture Decisions
1. **Kafka Migration Strategy**: Use `AddKafka()` + `AddKafkaProducer()` with dependency injection
2. **Prometheus Strategy**: Keep custom container but simplify configuration if possible
3. **Flink Strategy**: Keep custom containers as they're specialized infrastructure
4. **Port Management**: Let Aspire handle automatic port allocation where possible
5. **Performance Preservation**: Maintain all existing high-performance configurations

### Why This Approach
- **Incremental Migration**: Changes one component at a time to avoid breaking functionality
- **Preserve Performance**: Maintains all existing ultra-high-performance Kafka settings
- **Practical Limitations**: Uses Aspire where official packages exist, keeps custom containers where needed
- **Test Compatibility**: Ensures observability tests continue to work throughout process

### Alternatives Considered
- **Full Migration**: Would require custom Aspire extensions for Prometheus/Flink - too complex
- **No Changes**: Doesn't address user request for simplification
- **Mixed Approach**: ✅ CHOSEN - Use Aspire where available, simplify custom containers elsewhere

## Phase 3: TDD/BDD
### Requirements
Validate that observability tests continue to work with simplified Aspire setup

### Test Specifications
1. **Kafka Integration Test**: Verify producer service works with Aspire-managed Kafka
2. **Redis Connection Test**: Ensure automatic port allocation doesn't break Redis connections  
3. **Observability Test Suite**: All existing observability category tests must pass
4. **Infrastructure Startup Test**: LocalTesting.AppHost starts successfully with new configuration

### Behavior Definitions
- **Given**: LocalTesting infrastructure with simplified Aspire configuration
- **When**: Infrastructure starts up and tests execute
- **Then**: All functionality works identically to previous complex configuration
- **And**: Configuration is significantly simpler with fewer manual port specifications

## Phase 4: Implementation
### Requirements
**COMPLETED**: All major simplifications implemented

### Code Changes ✅
1. **Kafka Simplification**: 
   - Replaced 15+ environment variable custom container with `builder.AddKafka("kafka")`
   - Added `Aspire.Confluent.Kafka` client integration
   - Updated `KafkaProducerService` to use dependency-injected producer
   
2. **Redis Simplification**:
   - Removed explicit port configuration, now uses automatic Aspire allocation
   - Maintained service discovery compatibility
   
3. **Configuration Cleanup**:
   - Removed unnecessary port constant conversions
   - Added clear comments distinguishing Aspire vs custom containers
   - Maintained all custom containers for components without official Aspire support (Flink, Prometheus)

### Challenges Encountered
1. **Aspire Dashboard Configuration**: Required additional environment variables for OTLP endpoints
2. **Producer Dependency Injection**: Needed to restructure KafkaProducerService constructor pattern
3. **Port Management**: Balanced automatic allocation with services requiring specific configuration

### Solutions Applied
1. **Environment Variables**: Added required `ASPNETCORE_URLS`, `DOTNET_DASHBOARD_OTLP_ENDPOINT_URL`, `DOTNET_DASHBOARD_OTLP_HTTP_ENDPOINT_URL`  
2. **Dependency Injection**: Updated service to accept `IProducer<string, string>` directly from Aspire
3. **Hybrid Approach**: Used Aspire where available, simplified custom containers elsewhere

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