# LearningCourse Integration Tests Solution

## Overview

This solution (`IntegrationTests.sln`) provides a unified way to run all LearningCourse integration tests that depend on the LocalTesting infrastructure (Kafka, Flink, Temporal, etc.).

## Architecture

The solution includes:
- **LocalTesting.FlinkSqlAppHost** - Aspire application host that orchestrates all infrastructure containers
- **LocalTesting.IntegrationTests** - Shared test infrastructure (GlobalTestInfrastructure, LocalTestingTestBase)
- **Day01.IntegrationTests** - Day 01 Kafka-Flink data pipeline integration tests

## Key Benefits

1. **Shared Infrastructure**: All test projects share the same `GlobalTestInfrastructure` which starts containers once
2. **No Duplicate Containers**: Avoids the problem of multiple Aspire instances creating conflicting containers
3. **Simple to Use**: Just build and run tests using standard .NET CLI commands
4. **Zero Build Time**: Uses pre-built DLLs for all FlinkDotNet libraries - no rebuild required

## Prerequisites

**IMPORTANT**: Before running tests, you must build the FlinkDotNet solution in Release mode to generate the required DLLs and executable:

```bash
dotnet build FlinkDotNet/FlinkDotNet.sln --configuration Release
```

This ensures all required binaries exist:
- `Flink.JobGateway.exe` at `FlinkDotNet/Flink.JobGateway/bin/Release/net9.0/` (for AppHost)
- All FlinkDotNet DLLs at `FlinkDotNet/*/bin/Release/net9.0/` (for test projects)

## How to Use

### Build the Solution

```bash
dotnet build LearningCourse/IntegrationTests.sln --configuration Release
```

### Run All Tests

```bash
dotnet test LearningCourse/IntegrationTests.sln --configuration Release
```

### Run Specific Test Project

```bash
dotnet test LearningCourse/Day01-Kafka-Flink-Data-Pipeline/Day01.IntegrationTests/Day01.IntegrationTests.csproj --configuration Release
```

## How It Works

1. **Shared Infrastructure**: Day01.IntegrationTests references `LocalTesting.IntegrationTests` project
2. **Single Setup**: The `GlobalTestInfrastructure` class in LocalTesting.IntegrationTests has a `[SetUpFixture]` that runs once per assembly
3. **Reusable Base**: Tests inherit from `LocalTestingTestBase` to access shared infrastructure properties

## Adding New Test Projects

To add a new LearningCourse test project:

1. Create the test project in the appropriate Day folder
2. Add project reference to `LocalTesting.IntegrationTests`:
   ```xml
   <ProjectReference Include="..\..\..\LocalTesting\LocalTesting.IntegrationTests\LocalTesting.IntegrationTests.csproj" />
   ```
3. Inherit test classes from `LocalTestingTestBase`
4. Add the project to this solution
5. No need to create your own `GlobalSetup.cs` - it's shared!

## System Requirements

- .NET 9.0 SDK
- Docker Desktop or Podman running
- At least 8GB RAM available for containers
- FlinkDotNet solution built in Release configuration (see Prerequisites above)

## Common Issues

### Containers Not Starting
- Ensure Docker Desktop or Podman is running
- Check available disk space and memory
- Review container logs: `docker ps -a` and `docker logs <container-name>`

### Port Conflicts
- Aspire uses dynamic port allocation in testing mode
- If you see port conflicts, stop other applications using Kafka/Flink ports

### Build Errors
- **Missing FlinkDotNet DLLs or Flink.JobGateway executable**: Build FlinkDotNet solution first: `dotnet build FlinkDotNet/FlinkDotNet.sln --configuration Release`
- Ensure all FlinkDotNet projects build successfully first
- Run `dotnet clean` and rebuild if you encounter caching issues

## Technical Details

### DLL Reference Strategy

LearningCourse uses DLL references instead of project references for all FlinkDotNet libraries:

**AppHost (Flink.JobGateway)**:
- Uses `AddExecutable` to run pre-built Gateway executable
- No project reference to Flink.JobGateway
- Manually injects Aspire service discovery environment variables

**Test Projects (FlinkDotNet libraries)**:
- Direct DLL references to all FlinkDotNet assemblies:
  - `FlinkDotNet.dll`
  - `Flink.JobBuilder.dll`
  - `FlinkDotNet.Common.dll`
  - `FlinkDotNet.DataStream.dll`
  - `FlinkDotNet.Table.dll`
  - `FlinkDotNet.Util.dll`
- All DLLs referenced from `FlinkDotNet/*/bin/Release/net9.0/`

**Example from Day01.IntegrationTests.csproj**:
```xml
<ItemGroup>
  <Reference Include="FlinkDotNet">
    <HintPath>..\..\..\FlinkDotNet\FlinkDotNet\bin\Release\net9.0\FlinkDotNet.dll</HintPath>
    <Private>true</Private>
  </Reference>
  <!-- ... other DLL references -->
</ItemGroup>
```

**Benefits**:
- **Zero build time** for FlinkDotNet libraries when building LearningCourse
- Dramatically faster test iteration cycles
- Reduced build complexity and dependencies
- Uses Release-optimized DLLs for best performance
- Simpler CI/CD pipeline for LearningCourse tests
- No transitive project reference issues

**Trade-off**:
- Requires manual rebuild of FlinkDotNet solution when libraries change
- Must maintain DLL reference paths if FlinkDotNet structure changes