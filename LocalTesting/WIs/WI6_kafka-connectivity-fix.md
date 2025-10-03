# WI6: Fix LocalTesting Kafka Connectivity Issues

**File**: `WIs/WI6_kafka-connectivity-fix.md`
**Title**: Debug and fix Kafka connectivity failures in LocalTesting integration tests
**Description**: Fix 6 failing tests where Flink jobs cannot reach Kafka, causing messages to not be processed
**Priority**: High
**Component**: LocalTesting.IntegrationTests
**Type**: Bug Fix
**Assignee**: GitHub Copilot
**Created**: 2025-10-03
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI1: LocalTesting integration tests fix - learned test infrastructure patterns
- WI5: LocalTesting parallel test optimization - learned about shared infrastructure

### Lessons Applied
- Debug-first approach: Run tests to understand actual failures
- Infrastructure validation: Verify Docker network connectivity
- Minimal changes: Only fix what's broken

### Problems Prevented
- Not making changes without understanding root cause
- Not breaking working tests while fixing failing ones

## Phase 1: Investigation

### Requirements
- Fix 6 failing tests out of 16 total tests
- All failures are Kafka connectivity related
- Preserve working test infrastructure
- Maintain parallel test execution capability

### Debug Information (MANDATORY)

#### Test Results
```
Test summary: total: 16, failed: 6, succeeded: 10, skipped: 0, duration: 317.5s
```

#### Failing Tests
1. `FlinkDotNet_Comprehensive_AllJobTypes` - No messages consumed from output topics
2. `FlinkIrStringOps_KafkaToKafka_WithStringTransformation_Test` - No messages consumed
3. `FlinkRunner_DirectExecution_WithCorrectKafkaConfig_ShouldWork` - No messages consumed
4. `GatewayVsPureFlinkDiagnosticTest` - Both Gateway and Pure Flink fail (0/5 messages)
5. `Gateway_AutomaticBundling_WithoutPrebuiltJar_SuccessfullyRunsJob` - No messages consumed
6. `NativeFlinkJob_Should_ProcessMessagesSuccessfully` - No messages consumed (0/4 messages)

#### Passing Tests (10 tests)
- `KafkaAndFlink_StartWithoutGateway_Succeeds` ✅
- `Gateway_Pattern1_Uppercase_ShouldWork` ✅
- `Gateway_Pattern2_Filter_ShouldWork` ✅
- `Gateway_Pattern3_SplitConcat_ShouldWork` ✅
- `Gateway_Pattern4_Timer_ShouldWork` ✅
- `Gateway_Pattern5_SqlPassthrough_ShouldWork` ✅
- `Gateway_Pattern6_SqlTransform_ShouldWork` ✅
- `Gateway_Pattern7_Composite_ShouldWork` ✅
- `DockerNetwork_FlinkCanReachKafka_ShouldSucceed` ✅
- `NativeFlinkAllPatterns_Should_ProcessAllPatternsSuccessfully` ✅

#### Key Observations
1. **Jobs submit successfully** - Flink accepts and runs the jobs
2. **Jobs reach RUNNING state** - No errors in job submission or execution
3. **Messages sent to input topics** - Producer works correctly
4. **NO messages consumed from output topics** - Consumer gets 0 messages

#### Diagnostic Test Output
The `GatewayVsPureFlinkDiagnosticTest` reveals:
```
✅ Pure Flink Job ID: c750c3eb-b511-4fbd-9607-c1890fe2934f
✅ Pure Flink Flink Job ID: 5c14d8401d55e7627eed28f01b503887
✅ Pure Flink job is RUNNING

✅ Gateway Job ID: d33ab455f18aac785fe7864c241a5854  
✅ Gateway job is RUNNING

📊 Pure Flink Output: Consumed 0/5 messages
📊 Gateway Output: Consumed 0/5 messages

🔴 ROOT CAUSE: Kafka configuration issue
   Both Pure Flink and Gateway jobs fail.
   Problem is likely in Kafka bootstrap servers configuration.
   Flink jobs may not be able to reach 'kafka:9093'.
```

#### Current Configuration
From `LocalTesting.FlinkSqlAppHost/Ports.cs`:
```csharp
// CRITICAL: Aspire's Kafka uses port 9093 for PLAINTEXT_INTERNAL listener (container-to-container)
// Port 9092 is PLAINTEXT_HOST listener (external access from host machine)
public const string KafkaContainerBootstrap = "kafka:9093";
```

#### Pattern Analysis
- **Passing tests**: All 7 Gateway pattern tests pass, suggesting Gateway can reach Kafka
- **Failing tests**: Tests that expect message processing from Flink jobs fail
- **Critical difference**: Passing tests may use different Kafka bootstrap configuration

### Findings

**Root Cause Identified**: The Kafka bootstrap server configuration used by Flink jobs (`kafka:9093`) is incorrect or unreachable from within Flink containers.

**Evidence**:
1. Jobs submit successfully and reach RUNNING state (Flink cluster is healthy)
2. Messages are produced to input topics successfully (Producer works)
3. ZERO messages consumed from output topics (Flink jobs can't reach Kafka)
4. Both Gateway-submitted and directly-submitted jobs fail the same way
5. Diagnostic test explicitly shows: "Problem is likely in Kafka bootstrap servers configuration"

**Hypothesis**: Aspire's Kafka might use port 9092 (not 9093) for container-to-container communication, or the internal listener might be on a different port than documented.

### Next Steps
1. Inspect actual Kafka container configuration to see which ports are exposed
2. Check which port Flink containers should use to reach Kafka
3. Update configuration to use correct Kafka bootstrap server address
4. Verify fix with failing tests

### Additional Investigation

#### Port Change Experiment (9093 → 9092)
- Changed `Ports.KafkaContainerBootstrap` from "kafka:9093" to "kafka:9092"
- Test result: Still fails with 0 messages consumed
- Conclusion: Port number alone is not the issue

#### Critical Observation
- **Passing tests**: All 7 Gateway pattern tests (`Gateway_Pattern1_Uppercase_ShouldWork`, etc.)
- **Failing tests**: Direct job submission tests (`NativeFlinkJob_Should_ProcessMessagesSuccessfully`, etc.)
- **Key difference**: Gateway uses FlinkJobBuilder API vs direct Flink REST API submission

#### Gateway Pattern Tests Analysis
The passing tests all:
1. Use `FlinkDotNetJobs.CreateUppercaseJob(input, output, kafka, jobName, ct)`
2. Pass `KafkaContainerConnectionString` (which is "kafka:9093")
3. Submit via Gateway which uses FlinkJobBuilder
4. **Work perfectly** - messages are processed correctly

#### Native/Direct Tests Analysis
The failing tests:
1. Use direct Flink REST API (`/jars/{id}/run`)
2. Pass `KafkaContainerConnectionString` (which is "kafka:9093")
3. Job reaches RUNNING state
4. **Fail** - 0 messages consumed

### Hypothesis Revision
The issue is NOT the port number (9092 vs 9093). The issue appears to be:
1. **Gateway-submitted jobs work** - suggesting Gateway does something special
2. **Direct-submitted jobs fail** - suggesting raw Flink job submission is missing configuration

**Possible causes**:
- Gateway might inject additional environment variables into jobs
- Gateway might use a different Kafka connector configuration
- Gateway might set up jobs differently than direct submission
- Native Java job might have a bug or misconfiguration
-  Flink containers might not be able to resolve the "kafka" hostname

### Action Plan
1. Compare Gateway job submission vs direct job submission
2. Check what additional configuration Gateway adds to jobs
3. Verify Flink containers can resolve "kafka" hostname
4. Check Flink connector library configuration
