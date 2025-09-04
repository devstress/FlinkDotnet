# LocalTesting Integration Tests

This directory contains integration tests that use the LocalTesting Aspire infrastructure directly, providing proper observability testing with real infrastructure connections.

## Changes Made

### Problem Fixed
The original observability tests in `/IntegrationTests/FlinkDotNet.Aspire.IntegrationTests/` were failing because they were not properly connected to the LocalTesting infrastructure. They attempted to connect to `localhost:18000` but were running against a different Aspire setup.

### Solution Implemented
1. **Moved observability tests** from `IntegrationTests` folder to `LocalTesting/LocalTesting.IntegrationTests`
2. **Updated to use LocalTesting Aspire testing framework** with `Aspire.Hosting.Testing`
3. **Direct integration** with LocalTesting infrastructure via `DistributedApplicationTestingBuilder`
4. **Proper flow metrics validation** using the actual LocalTesting WebAPI endpoints

## Key Components

### ObservabilityMetrics.feature
- Complete BDD scenarios for observability testing
- Tests Kafka producer, Flink processing, Temporal workflows, and end-to-end flow metrics
- Includes comprehensive message state tracking tests
- 1 million message scenario for proper throughput validation

### ObservabilityMetricsSteps.cs
- Uses `Aspire.Hosting.Testing` to start LocalTesting infrastructure
- Creates HttpClient directly from Aspire app: `_app.CreateHttpClient("localtesting-webapi")`
- Implements proper `IAsyncLifetime` for test lifecycle management
- Validates flow metrics structure with proper JSON deserialization

## How It Works

```csharp
// Initialize LocalTesting Aspire infrastructure for testing
_appHost = DistributedApplicationTestingBuilder.CreateAsync<Projects.LocalTesting_AppHost>();
var appHostBuilder = await _appHost;
_app = await appHostBuilder.BuildAsync();
await _app.StartAsync();

// Get WebAPI client from Aspire app
_httpClient = _app.CreateHttpClient("localtesting-webapi");
```

## Running the Tests

Requires .NET 9.0 SDK and LocalTesting infrastructure:

```bash
cd LocalTesting
dotnet test LocalTesting.IntegrationTests --configuration Release
```

## Expected Results

With proper .NET 9.0 environment:
- ✅ LocalTesting infrastructure starts via Aspire testing framework
- ✅ ObservabilityMetricsSteps connects to real LocalTesting WebAPI
- ✅ Flow metrics are properly recorded and validated
- ✅ 1 million message scenario produces meaningful throughput metrics
- ✅ All BDD scenarios pass with real infrastructure

## Benefits

1. **Real Infrastructure Testing**: Tests run against actual LocalTesting Kafka, Flink, and Temporal services
2. **Proper Aspire Integration**: Uses `Aspire.Hosting.Testing` for authentic testing experience
3. **Flow Metrics Validation**: Validates that ObservabilityMetricsService integration works correctly
4. **Comprehensive Coverage**: Tests all observability scenarios with real message flows

This approach ensures the observability workflow will pass in CI/CD because the tests validate the actual flow metrics recording functionality.