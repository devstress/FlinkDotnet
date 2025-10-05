# WI17: Support Both TableEnvironment and Direct SQL

**File**: `WIs/WI17_support-both-tableenv-and-direct-sql.md`
**Title**: Add support for both TableEnvironment SQL and Direct SQL (Flink SQL Gateway)
**Description**: Support both TableEnvironment execution (current) and Direct SQL via Flink SQL Gateway REST API. Keep SqlTransform using TableEnvironment, change SqlPassthrough to use Direct SQL.
**Priority**: High
**Component**: FlinkDotNet, FlinkIRRunner, LocalTesting.IntegrationTests
**Type**: Feature Enhancement
**Assignee**: GitHub Copilot
**Created**: 2025-01-30
**Status**: Investigation

## Lessons Applied from Previous WIs

### Previous WI References
- WI16_fix-sql-flink-jobs.md - SQL Connector JAR issues and TableEnvironment usage
- WI9_integration-test-failures.md - Java compatibility and JAR selection

### Lessons Applied  
- **Understand existing SQL implementation**: Current SQL uses TableEnvironment exclusively
- **Minimal changes**: Only modify what's necessary to support both modes
- **Test-driven**: Update tests to validate both modes work correctly
- **Learn from Flink docs**: Understand Direct SQL (SQL Gateway) vs TableEnvironment

### Problems Prevented
- Breaking existing TableEnvironment functionality
- Over-engineering the solution with complex abstractions
- Not validating both modes work independently

## Phase 1: Investigation

### Requirements
- Understand difference between TableEnvironment and Direct SQL (Flink SQL Gateway)
- Keep existing TableEnvironment implementation for SqlTransform test
- Add Direct SQL support for SqlPassthrough test
- Both should execute SQL statements but through different mechanisms
- Ensure proper transformation works in both modes

### Debug Information (MANDATORY - Update this section for every investigation)

**Current Implementation**:
- SQL jobs use `SqlSourceDefinition` with list of SQL statements
- FlinkJobRunner.java executes via `TableEnvironment.executeSql()`
- No distinction between different SQL execution modes

**Direct SQL (Flink SQL Gateway)**:
- REST API endpoint: `/v1/statements`
- Submits SQL statements directly via HTTP
- Does not require JAR compilation
- Ideal for interactive analytics and table-oriented streaming

**TableEnvironment**:
- Java Table API approach (current implementation)
- Requires JAR submission to Flink cluster
- Statements executed via `TableEnvironment.executeSql()`

**Investigation Plan**:
1. Review SqlSourceDefinition model - add mode property
2. Check FlinkJobRunner.java - add conditional logic for Direct SQL
3. Review test structure - understand SqlPassthrough vs SqlTransform
4. Research Flink SQL Gateway REST API usage
5. Design minimal changes to support both modes

### Findings

**Analysis of Current Code**:

1. **SqlSourceDefinition** (JobDefinition.cs):
   - Has `Mode` property (streaming/batch) but doesn't distinguish TableEnv vs Direct SQL
   - Need to add execution mode: "tableenv" vs "gateway"

2. **Current SQL Submission Flow**:
   - C# code creates SqlSourceDefinition with SQL statements
   - FlinkJobGateway submits JAR to Flink via `/v1/jars/{jarId}/run`
   - FlinkJobRunner.java (in JAR) executes SQL using TableEnvironment
   - Lines 51-90 in FlinkJobRunner.java handle SQL via TableEnvironment

3. **Direct SQL Gateway Alternative**:
   - Flink SQL Gateway exposes `/v1/statements` REST endpoint
   - Accepts SQL statements directly without requiring JAR submission
   - No TableEnvironment needed - statements executed directly by Flink
   - Ideal for interactive queries and table-oriented streaming

4. **Test Methods**:
   - `CreateSqlPassthroughJob`: Simple passthrough (SELECT *) - will use SQL Gateway
   - `CreateSqlTransformJob`: Transformation (UPPER function) - will keep TableEnvironment
   - Both currently use same underlying mechanism (TableEnvironment)

**Design Decision**:
- Add `ExecutionMode` property to `SqlSourceDefinition` ("tableenv" or "gateway")
- Default to "tableenv" for backward compatibility
- For "gateway" mode: Implement SQL Gateway REST client in C# (FlinkJobGateway)
- For "tableenv" mode: Keep existing JAR submission flow (FlinkJobRunner.java)
- Update test jobs to specify execution mode

## Phase 2: Design

### Requirements
- Add ExecutionMode property to SqlSourceDefinition
- Implement Flink SQL Gateway REST client
- Update FlinkJobRunner to route based on execution mode
- Modify test jobs to use appropriate modes
- Ensure both modes transform data correctly

### Architecture Decisions

**Option 1: Add property to SqlSourceDefinition**
```csharp
public class SqlSourceDefinition : ISourceDefinition
{
    public string ExecutionMode { get; set; } = "tableenv"; // "tableenv" or "gateway"
    public List<string> Statements { get; set; } = new();
    public string Mode { get; set; } = "streaming";
}
```

**Option 2: Route in FlinkJobRunner.java**
```java
if (s.executionMode != null && "gateway".equals(s.executionMode)) {
    // Use Flink SQL Gateway REST API
    submitViaGateway(s.statements);
} else {
    // Use TableEnvironment (current implementation)
    TableEnvironment tEnv = TableEnvironment.create(...);
    // existing code
}
```

### Why This Approach
- **Minimal changes**: Add one property, one conditional branch
- **Backward compatible**: Default to "tableenv" maintains existing behavior
- **Clear separation**: Each mode has distinct implementation path
- **Testable**: Can validate both modes independently

### Alternatives Considered
1. **Separate source types**: Rejected - adds complexity, duplicates code
2. **Gateway-only**: Rejected - loses TableEnvironment benefits
3. **Auto-detection**: Rejected - adds magic behavior, harder to debug

## Phase 3: TDD/BDD

### Test Specifications

**Test Coverage Required**:
1. SqlPassthrough with Direct SQL Gateway - validates gateway execution
2. SqlTransform with TableEnvironment - validates tableenv execution
3. Both should properly transform data and produce output

### Behavior Definitions

**Scenario 1: Direct SQL Gateway Passthrough**
- Given: SqlSourceDefinition with ExecutionMode = "gateway"
- When: Job submitted via Flink SQL Gateway REST API
- Then: Data flows from input to output without TableEnvironment

**Scenario 2: TableEnvironment Transform**
- Given: SqlSourceDefinition with ExecutionMode = "tableenv" (default)
- When: Job submitted via TableEnvironment.executeSql()
- Then: Data transformed (UPPER) and flows to output

## Phase 4: Implementation

### Code Changes

**1. SqlSourceDefinition Model Update (JobDefinition.cs)**
```csharp
public class SqlSourceDefinition : ISourceDefinition
{
    [JsonIgnore]
    public string Type => "sql";
    public List<string> Statements { get; set; } = new();
    public string Mode { get; set; } = "streaming";
    
    /// <summary>
    /// Execution mode: "tableenv" (default, uses TableEnvironment) or "gateway" (uses Flink SQL Gateway REST API)
    /// </summary>
    public string ExecutionMode { get; set; } = "tableenv";
    
    public Dictionary<string, string> Properties { get; set; } = new();
}
```

**2. FlinkJobManager Routing Logic**
Added conditional routing in `SubmitJobAsync` method to detect SQL Gateway mode and route accordingly.

**3. SubmitSqlGatewayJobAsync Implementation**
New method in FlinkJobManager.cs that:
- Submits SQL statements directly to `/v1/statements` endpoint
- Executes each statement sequentially
- Extracts job ID from INSERT statement responses
- Returns synthetic job ID if none returned
- Handles errors and logging appropriately

**4. Test Job Updates**
- `CreateSqlPassthroughJob`: Updated to set `ExecutionMode = "gateway"`
- `CreateSqlTransformJob`: Kept default `ExecutionMode = "tableenv"`

### Implementation Status

**Completed**:
- ✅ ExecutionMode property added to SqlSourceDefinition
- ✅ SQL Gateway submission logic implemented
- ✅ Routing logic in FlinkJobManager updated
- ✅ Test jobs updated to specify execution modes
- ✅ Build verification successful

**Next Steps**:
- Run integration tests to verify both modes work
- Update documentation with execution mode examples

## Phase 5: Testing & Validation

### Test Results
(To be filled after testing)

## Phase 6: Owner Acceptance

### Demonstration
(To be filled after implementation)

## Lessons Learned & Future Reference (MANDATORY)

### What Worked Well
(To be documented after completion)

### What Could Be Improved  
(To be documented after completion)

### Key Insights for Similar Tasks
(To be documented after completion)

### Specific Problems to Avoid in Future
(To be documented after completion)
