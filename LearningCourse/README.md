# LearningCourse Integration Tests

This directory contains integration tests for the FlinkDotNet Learning Course tutorials.

## ⚠️ IMPORTANT: Running Integration Tests

**All LearningCourse integration tests MUST be run through `LocalTesting/LocalTesting.sln`** to share the same Aspire infrastructure.

### Quick Start

```bash
# Run ALL integration tests (LearningCourse)
dotnet test LearningCourse/IntegrationTests.sln --configuration Release

# Run ONLY Day01 tests
dotnet test LearningCourse/IntegrationTests.sln --configuration Release --filter "FullyQualifiedName~Day01"

# Run ONLY Day02 tests
dotnet test LearningCourse/IntegrationTests.sln --configuration Release --filter "FullyQualifiedName~Day02"

# Run specific test
dotnet test LearningCourse/IntegrationTests.sln --configuration Release --filter "FullyQualifiedName~UppercaseTransform"
```

## Why Use LearningCourse/IntegrationTests.sln?

### Architecture Explanation

LearningCourse has its own dedicated solution file for integration tests:

1. **Dedicated Infrastructure**: LearningCourse tests use their own infrastructure setup via `LearningCourseTestBase`
2. **Independent Testing**: Each Day can be tested independently without LocalTesting infrastructure
3. **Simpler Setup**: No dependency on LocalTesting.sln, cleaner separation of concerns

### What Works ✅

**Using LearningCourse solution (correct)**:
```bash
# ✅ This uses the dedicated LearningCourse solution
dotnet test LearningCourse/IntegrationTests.sln
```

**Benefits**:
- Clean separation between LearningCourse and LocalTesting tests
- Each solution manages its own infrastructure
- All tests work reliably
- No cross-solution dependencies

## Solution Structure

The `LearningCourse/IntegrationTests.sln` includes:
- **LearningCourse.IntegrationTests** - Base test infrastructure with `LearningCourseTestBase`
- **LearningCourse.Common** - Common utilities for learning course
- **Day01.IntegrationTests** - Kafka-Flink pipeline learning course tests
- **Day02.IntegrationTests** - Flink 2.1.0 fundamentals learning course tests
- **Exercise Solutions** - All exercise solution projects for each day

## Test Projects

### Day01.IntegrationTests
Located in `LearningCourse/Day01-Kafka-Flink-Data-Pipeline/Day01.IntegrationTests/`

Tests:
- `Exercise1_StringCapitalize_ShouldExecuteSuccessfully` - Tests string stream processing with capitalize transformation
- `Exercise2_BackupAggregator_ShouldExecuteSuccessfully` - Tests custom objects and backup aggregation with time windows

**Key Point**: Day01 tests inherit from `LearningCourseTestBase` and rely on the shared infrastructure.

### Day02.IntegrationTests
Located in `LearningCourse/Day02-Flink21-Fundamentals/Day02.IntegrationTests/`

Tests:
- `Exercise1_InfrastructureValidation_ShouldExecuteSuccessfully` - Tests production infrastructure validation
- `Exercise2_ProductionApp_ShouldExecuteSuccessfully` - Tests enterprise-grade streaming application
- `Exercise3_ObservabilityDashboard_ShouldExecuteSuccessfully` - Tests Google-style SRE observability patterns
- `Exercise4_LoadTesting_ShouldExecuteSuccessfully` - Tests performance validation and benchmarking

**Key Point**: Day02 tests inherit from `LearningCourseTestBase` and rely on the shared infrastructure.

## Prerequisites

Before running tests, ensure:

1. **.NET 9.0 SDK** is installed (`dotnet --version` shows 9.0.x)
2. **Docker Desktop or Podman** is running (required for Aspire containers)
3. **Maven** is available in PATH (for Java component builds)
4. **Java JDK 17** is available (automatically managed by the build)
5. **Sufficient system resources**:
   - Minimum 4GB RAM available
   - Minimum 10GB disk space

## Test Environment

The integration tests use .NET Aspire for orchestration, which automatically manages:
- Apache Flink (JobManager and TaskManager)
- Apache Kafka broker
- Temporal workflow server (if needed)
- PostgreSQL database (if needed)

All infrastructure is ephemeral and cleaned up after tests complete.

## Troubleshooting

### Tests fail with "Docker daemon not running"
Ensure Docker Desktop or Podman is running before executing tests:
```bash
# Windows
docker ps

# Should show containers, not an error
```

### Tests fail with "Global test infrastructure is not initialized"
This occurs when infrastructure is not properly initialized.

**Solution**: Ensure you're running through the correct solution:
```bash
dotnet test LearningCourse/IntegrationTests.sln --filter "FullyQualifiedName~Day01"
```

### Flink Container Issues
If tests fail with "Flink JobManager endpoint not found" or containers show "Created" status instead of "Up":
- Check Docker Desktop or Podman is running and has sufficient resources
- Verify no port conflicts (Flink uses 8081, Kafka uses 9092/9093)
- Check Docker logs: `docker logs <container-name>`

### Tests timeout or fail to start
Check Docker Desktop or Podman has sufficient resources allocated (Settings → Resources for Docker Desktop).

### Port conflict errors
Stop any services using conflicting ports or configure alternative ports in the test configuration.

### Build errors
Run a clean build before testing:
```bash
dotnet clean LearningCourse/IntegrationTests.sln
dotnet build LearningCourse/IntegrationTests.sln --configuration Release
```

## CI/CD Integration

The LearningCourse solution can be easily integrated into CI/CD workflows:

```yaml
- name: Run All Integration Tests
  run: dotnet test LearningCourse/IntegrationTests.sln --configuration Release --logger "trx;LogFileName=integration-test-results.trx"

- name: Run Only Day01 Tests
  run: dotnet test LearningCourse/IntegrationTests.sln --configuration Release --filter "FullyQualifiedName~Day01" --logger "trx;LogFileName=day01-test-results.trx"

- name: Run Only Day02 Tests
  run: dotnet test LearningCourse/IntegrationTests.sln --configuration Release --filter "FullyQualifiedName~Day02" --logger "trx;LogFileName=day02-test-results.trx"
```

## Adding New Integration Tests

To add a new day's integration tests:

1. Create the test project in the appropriate Day folder:
   ```bash
   dotnet new nunit -n DayXX.IntegrationTests -o LearningCourse/DayXX-Topic/DayXX.IntegrationTests
   ```

2. Add project reference to `LearningCourse/IntegrationTests.sln`:
   ```bash
   cd LearningCourse
   dotnet sln IntegrationTests.sln add DayXX-Topic/DayXX.IntegrationTests/DayXX.IntegrationTests.csproj
   ```

3. Add exercise solution projects to the solution:
   ```bash
   dotnet sln IntegrationTests.sln add DayXX-Topic/Exercise-Solutions/Exercise1/Exercise1.csproj
   ```

4. Make test class inherit from `LearningCourseTestBase`:
   ```csharp
   using LearningCourse.IntegrationTests;
   
   public class DayXXTests : LearningCourseTestBase
   {
       // Tests here
   }
   ```

5. Run tests to verify:
   ```bash
   dotnet test LearningCourse/IntegrationTests.sln --filter "FullyQualifiedName~DayXX"
   ```

## Migration from PowerShell Script

Previous versions used `run-all-integration-tests.ps1` PowerShell script. This has been replaced by the solution file approach.

**Before** (❌ deprecated):
```powershell
.\LearningCourse\run-all-integration-tests.ps1
```

**Now** (✅ correct):
```bash
dotnet test LocalTesting/LocalTesting.sln --configuration Release --filter "FullyQualifiedName~Day"
```

The solution file approach offers several advantages:
- Better Visual Studio/VS Code integration
- Consistent with standard .NET testing workflows  
- Easier to run specific tests or test categories
- No PowerShell-specific quirks or compatibility issues
- Standard NUnit test filtering and reporting
- **Shared infrastructure** prevents container conflicts
- Works on all platforms (Windows, Linux, macOS)

## Deprecated Files

The following files are deprecated and should not be used:

- ❌ `LearningCourse/IntegrationTests.sln` - Creates duplicate Aspire instance (if it exists, delete it)
- ❌ `LearningCourse/run-all-integration-tests.ps1` - Replaced by solution file approach (deleted)

**Always use**: `LocalTesting/LocalTesting.sln` for running all integration tests.
