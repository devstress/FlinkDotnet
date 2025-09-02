# WI76: Kafka Version Fix and LGTM Observability Stack

**File**: `WIs/WI76_kafka-observability-improvements.md`
**Title**: [Infrastructure] Fix Kafka version detection and implement LGTM observability stack  
**Description**: Fix Kafka version showing as "1.0-Unknown" in UI, configure 10 partitions across all components, and implement full LGTM observability stack (Loki, Grafana, Tempo, Mimir)
**Priority**: High
**Component**: Infrastructure/Observability
**Type**: Enhancement
**Assignee**: AI Agent
**Created**: 2025-01-07
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI75: Aspire startup debugging and configuration
### Lessons Applied  
- Use systematic debugging approach for infrastructure issues
- Test configuration changes incrementally
- Ensure environment variables are properly set for all containers
### Problems Prevented
- Skip assumptions about working configurations
- Validate observability stack integration before proceeding

## Phase 1: Investigation
### Requirements
1. **Kafka Version Issue**: Investigate why Kafka UI shows "1.0-Unknown" instead of proper version
2. **Partition Configuration**: Set up 10 partitions as default for all topics
3. **LGTM Stack**: Research and implement Loki + Grafana + Tempo + Mimir observability
4. **Code Updates**: Update all LocalTesting and LearningCourse components to use 10 partitions

### Debug Information (MANDATORY)
- **Kafka Version Detection**: UI connecting to Apache Kafka containers but not detecting version
- **Current Partition Setup**: Using 15 partitions in environment variables
- **Observability Gap**: Missing centralized logging (Loki) and distributed tracing (Tempo)
- **Code Analysis**: Need to audit LocalTesting and LearningCourse for partition configuration

### Findings
- Apache Kafka image may need specific version configuration for UI detection
- Current observability setup only has Prometheus + Grafana, missing Loki and Tempo
- Partition count needs to be coordinated across Kafka config and application code

### Lessons Learned
- Container image versions need explicit configuration for proper detection
- Observability requires full LGTM stack for production-ready monitoring
- Partition configuration must be consistent across infrastructure and application layers

## Phase 2: Design  
### Requirements
1. **Kafka Configuration**: Explicit version setting and 10-partition default
2. **LGTM Stack Architecture**: Loki (logs), Grafana (visualization), Tempo (tracing), Mimir (metrics)
3. **Application Updates**: Systematic update of partition usage across codebase
4. **Integration**: Ensure all components work together seamlessly

### Architecture Decisions
- Keep Apache Kafka image but add explicit version environment variables
- Add Loki for centralized log aggregation
- Add Tempo for distributed tracing
- Use Mimir for long-term metrics storage (or continue with Prometheus for simplicity)
- Update all Kafka producers/consumers to use 10 partitions

### Why This Approach
- Maintains working Kafka setup while fixing version detection
- LGTM stack provides enterprise-grade observability
- 10 partitions provide good parallelism without excessive overhead
- Systematic codebase updates ensure consistency

### Alternatives Considered
- Different Kafka image (rejected - current setup works)
- Partial observability stack (rejected - need full visibility)
- Keep 15 partitions (rejected - user requested 10)

## Phase 3: TDD/BDD
### Test Specifications
- Kafka UI displays correct version information
- All topics created with 10 partitions by default
- Loki collects logs from all containers
- Tempo traces requests across services
- Grafana dashboards show metrics, logs, and traces
- LocalTesting services use 10 partitions correctly

### Behavior Definitions
```gherkin
Feature: Kafka Version and Observability
  Scenario: Kafka version is properly detected
    Given Apache Kafka cluster is running
    When I access Kafka UI
    Then the cluster version should be displayed correctly
    And should not show "1.0-Unknown"
    
  Scenario: Topics use 10 partitions
    Given Kafka cluster is configured
    When topics are created
    Then they should have 10 partitions by default
    
  Scenario: LGTM observability stack works
    Given all observability components are running
    When I generate application activity
    Then logs should appear in Loki
    And traces should appear in Tempo
    And metrics should appear in Grafana
```

## Phase 4: Implementation
### Code Changes
[To be filled during implementation]

### Challenges Encountered
[To be documented during implementation]

### Solutions Applied
[To be documented during implementation]

## Phase 5: Testing & Validation
### Test Results
[To be filled during testing]

### Performance Metrics
[To be documented during validation]

## Phase 6: Owner Acceptance
### Demonstration
[To be documented during demonstration]

### Owner Feedback
[To be documented after owner review]

### Final Approval
[To be confirmed by owner]

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