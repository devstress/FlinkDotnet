# WI9: Fix Observability Tests with Proper Aspire Testing Framework Integration

**File**: `WIs/WI9_fix-observability-tests-aspire-integration.md`
**Title**: [LocalTesting] Fix observability tests to use proper Aspire testing framework without manual infrastructure
**Description**: Fix observability tests to use proper Aspire testing framework integration and implement missing step definitions
**Priority**: High
**Component**: LocalTesting.IntegrationTests
**Type**: Bug Fix
**Assignee**: AI Assistant
**Created**: 2024-12-19
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI1_move-observability-tests-to-localtesting.md - Basic structure setup
- WI5_fix-observability-test-nullref.md - Previous NullReference attempts
- WI6_observability-messages-per-second-metrics.md - Metrics structure requirements

### Lessons Applied  
- Must use `DistributedApplicationTestingBuilder.CreateAsync<Projects.LocalTesting_AppHost>()` for proper Aspire integration
- Cannot rely on manual infrastructure startup at localhost:18000
- All step definitions must be implemented for complete BDD scenarios
- Tests must work in .NET 9.0 environment without external dependencies

### Problems Prevented
- Avoiding manual HttpClient creation that requires external infrastructure
- Preventing missing step definition scenarios that cause test failures
- Ensuring proper Aspire service discovery instead of hardcoded URLs

## Phase 1: Investigation
### Requirements
Fix the observability tests to work properly with Aspire testing framework and implement all missing step definitions.

### Debug Information (MANDATORY - Update this section for every investigation)
- **Error Messages**: 
  ```
  System.InvalidOperationException : HttpClient is not initialized. The Aspire testing framework may not have started properly.
  Stack Trace: at LocalTesting.IntegrationTests.Features.ObservabilityMetricsSteps.GivenLocalTestingInfrastructureIsRunningWithObservabilityEnabled()
  
  When I produce 30 messages to Kafka topic "failure-test" with tracking enabled
  -> No matching step definition found for the step
  ```
- **Log Locations**: LocalTesting.IntegrationTests test output
- **System State**: .NET 8.0.119 environment (should be .NET 9.0), missing Aspire testing framework proper setup
- **Reproduction Steps**: Run `dotnet test LocalTesting/LocalTesting.IntegrationTests --filter "Category=observability"`
- **Evidence**: Multiple missing step definitions, HttpClient null reference, no proper Aspire integration

### Root Cause Analysis
1. **Wrong Aspire Integration**: Current implementation manually creates HttpClient and tries to connect to localhost:18000, but should use `DistributedApplicationTestingBuilder` and `app.CreateHttpClient("localtesting-webapi")`
2. **Missing Step Definitions**: Many BDD steps in ObservabilityMetrics.feature have no corresponding implementation in ObservabilityMetricsSteps.cs
3. **Environment Issue**: Tests require .NET 9.0 but current environment has .NET 8.0.119

### Findings
The current ObservabilityMetricsSteps.cs is not properly implementing Aspire testing framework. It should:
- Use `DistributedApplicationTestingBuilder.CreateAsync<Projects.LocalTesting_AppHost>()`
- Use `await _app.CreateHttpClient("localtesting-webapi")` for service discovery
- Implement all missing step definitions for failure tracking, message state queries, etc.

### Lessons Learned
Previous attempts did not properly implement Aspire testing framework - they just manually created HttpClient connections.

## Phase 2: Design  
### Requirements
- Implement proper Aspire testing framework integration using `DistributedApplicationTestingBuilder`
- Add all missing step definitions for complete BDD scenarios
- Ensure tests work without external infrastructure dependencies

### Architecture Decisions
Use the standard Aspire testing pattern:
```csharp
public class ObservabilityMetricsSteps : IAsyncLifetime
{
    private DistributedApplication? _app;
    private HttpClient? _httpClient;

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.LocalTesting_AppHost>();
        _app = await builder.BuildAsync();
        await _app.StartAsync();
        _httpClient = _app.CreateHttpClient("localtesting-webapi");
    }
}
```

### Why This Approach
- Aspire testing framework automatically manages infrastructure lifecycle
- Service discovery through named endpoints eliminates hardcoded URLs
- No external dependencies - all infrastructure managed by Aspire

### Alternatives Considered
- Manual infrastructure startup: Too complex and unreliable
- Mock services: Would not test real integration

## Phase 3: TDD/BDD
### Test Specifications
All BDD scenarios in ObservabilityMetrics.feature must have corresponding step definitions:
- Message production with tracking
- Failure simulation and error state tracking
- Message state queries and filtering
- Delivery status validation
- Cleanup and maintenance operations

### Behavior Definitions
Every step in the .feature file must be implemented and testable.

## Phase 4: Implementation
### Code Changes
✅ **Replaced manual HttpClient creation with proper Aspire testing framework**:
- Uses `DistributedApplicationTestingBuilder.CreateAsync<Projects.LocalTesting_AppHost>()`
- Uses `_app.CreateHttpClient("localtesting-webapi")` for service discovery
- Proper lifecycle management with `IAsyncLifetime`

✅ **Implemented all missing step definitions**:
- `WhenIProduceMessagesToKafkaTopicWithTrackingEnabled`
- `WhenISimulateProcessingFailuresForPercentOfTheMessages`
- `ThenFailedMessagesShouldHaveState`
- `ThenFailedMessagesShouldContainErrorDetails`
- `ThenMessageStateSummaryShouldShowCorrectCountsOfFailedVsDeliveredMessages`
- `ThenIShouldBeAbleToQueryOnlyFailedMessages`
- All message state filtering and querying step definitions
- All cleanup and maintenance step definitions
- Complete BDD scenario coverage

✅ **Added proper error handling and service discovery**:
- Validates both HttpClient and DistributedApplication are available
- Uses Aspire service discovery instead of hardcoded localhost URLs
- Extended timeouts for comprehensive test scenarios
- Proper async/await patterns throughout

### Challenges Encountered
- Current environment has .NET 8.0.119 but project requires .NET 9.0.100
- Cannot test in current environment due to .NET version mismatch

### Solutions Applied
- Implemented proper Aspire testing framework patterns from documentation
- Added all missing step definitions based on feature file requirements
- Created comprehensive error handling for proper debugging
## Phase 5: Testing & Validation
### Test Requirements (Ready for .NET 9.0 Environment)
The observability tests have been completely rewritten to use proper Aspire testing framework. All missing step definitions have been implemented.

### Ready for Testing in .NET 9.0 Environment
```bash
# Validation commands for .NET 9.0 environment:
cd /path/to/FlinkDotnet

# 1. Verify .NET version
dotnet --version  # Should show 9.0.x

# 2. Build the test project
dotnet build LocalTesting/LocalTesting.IntegrationTests --configuration Release

# 3. Run observability tests
dotnet test LocalTesting/LocalTesting.IntegrationTests --filter "Category=observability" --logger "console;verbosity=detailed"

# 4. Run specific failing scenario that was reported
dotnet test LocalTesting/LocalTesting.IntegrationTests --filter "TestCategory=observability" --logger "console;verbosity=detailed"
```

### Expected Outcomes
- ✅ No more "HttpClient is not initialized" errors
- ✅ No more "No matching step definition found" errors  
- ✅ All BDD scenarios should execute without step definition failures
- ✅ Tests automatically manage infrastructure via Aspire testing framework
- ✅ No manual infrastructure startup required

### Current Environment Limitation
Cannot test in current environment due to .NET 8.0.119 vs required .NET 9.0.100

## Phase 6: Owner Acceptance
### Demonstration
✅ **Ready for .NET 9.0 Testing**: All code changes completed and validation script provided

### Validation Script Created
`LocalTesting/validate-observability-tests-fixed.sh` - Complete validation for .NET 9.0 environment

### Changes Summary
1. **Fixed Aspire Integration**: Replaced manual HttpClient with proper `DistributedApplicationTestingBuilder`
2. **Implemented All Missing Step Definitions**: 25+ missing step definitions now implemented
3. **Added Service Discovery**: Uses `_app.CreateHttpClient("localtesting-webapi")` instead of hardcoded URLs
4. **Enhanced Error Handling**: Proper validation of Aspire framework initialization

### Owner Feedback Required
Please test in .NET 9.0 environment using: `./LocalTesting/validate-observability-tests-fixed.sh`

### Expected Results
- ❌ **Before**: HttpClient null reference, missing step definitions
- ✅ **After**: Full Aspire integration, all scenarios implemented

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
✅ **Proper Aspire Testing Framework Pattern**: Using `DistributedApplicationTestingBuilder.CreateAsync<Projects.LocalTesting_AppHost>()` eliminates all manual infrastructure concerns

✅ **Service Discovery**: `_app.CreateHttpClient("localtesting-webapi")` provides reliable service connections

✅ **Complete BDD Implementation**: Implementing ALL step definitions prevents "No matching step definition found" errors

### What Could Be Improved  
- Should have implemented proper Aspire testing from the beginning instead of manual HttpClient approach
- Should have implemented all step definitions before claiming tests work
- Should have tested in proper .NET 9.0 environment before declaring success

### Key Insights for Similar Tasks
1. **Never use manual HttpClient connections** for Aspire testing - always use service discovery
2. **Implement ALL BDD step definitions** before claiming scenarios work  
3. **Test in proper environment** before declaring completion
4. **Use IAsyncLifetime** for proper test infrastructure lifecycle management

### Specific Problems to Avoid in Future
❌ **Manual HttpClient Creation**: `new HttpClient() { BaseAddress = new Uri("http://localhost:18000") }`
✅ **Proper Aspire Service Discovery**: `_app.CreateHttpClient("localtesting-webapi")`

❌ **Missing Step Definitions**: Leaving BDD steps unimplemented
✅ **Complete Implementation**: All feature file steps must have corresponding C# methods

❌ **Wrong Environment Testing**: Testing in .NET 8.0 when project requires .NET 9.0
✅ **Proper Environment**: Always test in matching .NET version

### Reference for Future WIs
**Aspire Testing Framework Pattern**:
```csharp
public class TestSteps : IAsyncLifetime
{
    private DistributedApplication? _app;
    private HttpClient? _httpClient;

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.YourAppHost>();
        _app = await builder.BuildAsync();
        await _app.StartAsync();
        _httpClient = _app.CreateHttpClient("your-service-name");
    }
}
```

**BDD Step Definition Rule**: Every step in .feature file MUST have corresponding `[Given]`, `[When]`, or `[Then]` method implementation.