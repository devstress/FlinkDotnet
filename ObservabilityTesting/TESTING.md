# ObservabilityTesting - How to Run Tests

## Quick Start

The ObservabilityTesting suite uses persistent containers to speed up test execution. Follow these steps:

### 1. Run All Tests (Recommended)

```bash
cd ObservabilityTesting
dotnet test ObservabilityTesting.sln --configuration Release
```

This ensures GlobalTestInfrastructure.GlobalSetUp() runs and initializes the shared AppHost.

### 2. Run Tests Without Rebuilding (Fast Iteration)

After the first build, use --no-build for faster runs:

```bash
dotnet test ObservabilityTesting.IntegrationTests/bin/Release/net10.0/ObservabilityTesting.IntegrationTests.dll --no-build
```

This skips the slow Java/Maven build and Docker image rebuild.

### 3. Run Specific Tests

```bash
dotnet test ObservabilityTesting.IntegrationTests/bin/Release/net10.0/ObservabilityTesting.IntegrationTests.dll \
  --filter "FullyQualifiedName~Test1" \
  --no-build
```

## Performance Tips

### Docker Image Caching
The FlinkDotNet Gateway Docker image is built once and cached. Subsequent test runs skip the rebuild if the image exists.

### Persistent Containers
All containers use `ContainerLifetime.Persistent`:
- Containers stay running between test executions
- Ports remain stable across runs
- Infrastructure startup happens once

### Fast vs Slow Execution

**Slow (includes build):** 3-5 minutes
```bash
dotnet test ObservabilityTesting.sln --configuration Release
```

**Fast (no build):** 10-60 seconds
```bash
dotnet test ObservabilityTesting.IntegrationTests/bin/Release/net10.0/ObservabilityTesting.IntegrationTests.dll --no-build
```

## Troubleshooting

### Tests Fail with "Connection Refused"
**Cause:** GlobalSetUp didn't run or containers were removed
**Solution:** Run all tests together (no --filter on first run)

### Tests Take Too Long
**Cause:** Rebuilding Docker images and Java artifacts
**Solution:** Use --no-build after first successful build

### Kafka Port Changes
**Cause:** AppHost was disposed and recreated
**Solution:** Ensure GlobalTestInfrastructure stays alive (don't create new AppHost instances)

## Architecture

```
GlobalTestInfrastructure (SetUpFixture)
  ├─ OneTimeSetUp: Creates shared AppHost (runs ONCE)
  │   ├─ Starts all containers with Persistent lifetime
  │   ├─ Discovers dynamic ports
  │   └─ Stores in static properties
  │
  └─ OneTimeTearDown: Disposes AppHost (after ALL tests)

ObservabilityTests (TestFixture)
  ├─ Uses GlobalTestInfrastructure.AppHost
  ├─ Uses GlobalTestInfrastructure.KafkaConnectionString
  └─ Tests run with shared infrastructure
```

## Container Management

### Check Running Containers
```bash
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
```

### Clean Up Containers Manually
```bash
docker ps -a --format "{{.Names}}" | grep b77f4790 | xargs -r docker rm -f
```

### View Container Logs
```bash
docker logs kafka-b77f4790
docker logs flink-jobmanager-b77f4790
```
