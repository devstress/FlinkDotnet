# LearningCourse Integration Tests

This directory contains integration tests for the FlinkDotNet Learning Course tutorials.

## ⚠️ IMPORTANT: Running Integration Tests

**All LearningCourse integration tests MUST be run through `LocalTesting/LocalTesting.sln`** to share the same Aspire infrastructure.

### Quick Start

```bash
# Run ALL integration tests (LocalTesting + LearningCourse)
dotnet test LocalTesting/LocalTesting.sln --configuration Release

# Run ONLY Day01 tests
dotnet test LocalTesting/LocalTesting.sln --configuration Release --filter "FullyQualifiedName~Day01"

# Run ONLY LocalTesting tests  
dotnet test LocalTesting/LocalTesting.sln --configuration Release --filter "FullyQualifiedName~LocalTesting"

# Run specific test
dotnet test LocalTesting/LocalTesting.sln --configuration Release --filter "FullyQualifiedName~UppercaseTransform"
```

## Why Use LocalTesting.sln?

### Architecture Explanation

LearningCourse tests are integrated into `LocalTesting.sln` because:

1. **Shared Infrastructure**: Both LocalTesting and LearningCourse tests use the same .NET Aspire application host
2. **Single Aspire Instance**: Only ONE Aspire instance can manage containers (Flink, Kafka, etc.) at a time
3. **No Container Conflicts**: Multiple Aspire instances cause containers to get stuck in "Created" state

### What Doesn't Work ❌

**Separate solution approach (broken)**:
```bash
# ❌ This creates a SECOND Aspire instance
dotnet test LearningCourse/IntegrationTests.sln
```

**Problem**: 
- Day01 tests try to create their own `GlobalTestInfrastructure`
- Results in TWO Aspire instances competing for the same containers
- Flink containers stuck in "Created" status, never reaching "Up"
- Tests fail with "Global test infrastructure is not initialized"

### What Works ✅

**Unified solution approach (correct)**:
```bash
# ✅ This uses ONE shared Aspire instance
dotnet test LocalTesting/LocalTesting.sln --filter "FullyQualifiedName~Day01"
```

**Benefits**:
- Single `GlobalTestInfrastructure` instance shared across all tests
- Clean container lifecycle management
- All tests work reliably
- No container conflicts

## Solution Structure

The `LocalTesting/LocalTesting.sln` includes:
- **LocalTesting.IntegrationTests** - Core LocalTesting integration tests
- **Day01.IntegrationTests** - Kafka-Flink pipeline learning course tests (added)
- **LocalTesting.FlinkSqlAppHost** - Shared Aspire application host
- **FlinkDotNet** - Core FlinkDotNet library  
- **Flink.JobBuilder** - Job definition and building utilities

## Test Projects

### Day01.IntegrationTests
Located in `LearningCourse/Day01-Kafka-Flink-Data-Pipeline/Day01.IntegrationTests/`

Tests:
- `PipelineDemo_IdentityTransform_ShouldPassthrough100Messages` - Tests basic pipeline functionality
- `PipelineDemo_UppercaseTransform_ShouldConvertToUppercase` - Tests uppercase transformation

**Key Point**: Day01 tests inherit from `LocalTestingTestBase` and rely on the shared `GlobalTestInfrastructure`.

## Prerequisites

Before running tests, ensure:

1. **.NET 9.0 SDK** is installed (`dotnet --version` shows 9.0.x)
2. **Docker Desktop** is running (required for Aspire containers)
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
Ensure Docker Desktop is running before executing tests:
```bash
# Windows
docker ps

# Should show containers, not an error
```

### Tests fail with "Global test infrastructure is not initialized"
This occurs when trying to run Day01 tests standalone through `LearningCourse/IntegrationTests.sln`.

**Solution**: Always run through `LocalTesting.sln`:
```bash
dotnet test LocalTesting/LocalTesting.sln --filter "FullyQualifiedName~Day01"
```

### Flink Container Issues
If tests fail with "Flink JobManager endpoint not found" or containers show "Created" status instead of "Up":
- Check Docker Desktop is running and has sufficient resources
- Verify no port conflicts (Flink uses 8081, Kafka uses 9092/9093)
- Check Docker logs: `docker logs <container-name>`
- **Most common cause**: Multiple Aspire instances - use LocalTesting.sln!

### Tests timeout or fail to start
Check Docker Desktop has sufficient resources allocated (Settings → Resources).

### Port conflict errors
Stop any services using conflicting ports or configure alternative ports in the test configuration.

### Build errors
Run a clean build before testing:
```bash
dotnet clean LocalTesting/LocalTesting.sln
dotnet build LocalTesting/LocalTesting.sln --configuration Release
```

## CI/CD Integration

The unified solution file can be easily integrated into CI/CD workflows:

```yaml
- name: Run All Integration Tests
  run: dotnet test LocalTesting/LocalTesting.sln --configuration Release --logger "trx;LogFileName=integration-test-results.trx"

- name: Run Only LearningCourse Tests
  run: dotnet test LocalTesting/LocalTesting.sln --configuration Release --filter "FullyQualifiedName~Day" --logger "trx;LogFileName=learningcourse-test-results.trx"
```

## Adding New Integration Tests

To add a new day's integration tests:

1. Create the test project in the appropriate Day folder:
   ```bash
   dotnet new nunit -n DayXX.IntegrationTests -o LearningCourse/DayXX-Topic/DayXX.IntegrationTests
   ```

2. Add project reference to `LocalTesting.sln` (NOT a separate solution):
   ```bash
   dotnet sln LocalTesting/LocalTesting.sln add LearningCourse/DayXX-Topic/DayXX.IntegrationTests/DayXX.IntegrationTests.csproj
   ```

3. Make test class inherit from `LocalTestingTestBase`:
   ```csharp
   using LocalTesting.IntegrationTests;
   
   public class DayXXTests : LocalTestingTestBase
   {
       // Tests here
   }
   ```

4. Create minimal `GlobalSetup.cs` (DO NOT create infrastructure):
   ```csharp
   using NUnit.Framework;
   
   namespace DayXX.IntegrationTests;
   
   [SetUpFixture]
   public class GlobalSetup
   {
       [OneTimeSetUp]
       public void AssemblySetUp()
       {
           // NO infrastructure setup - relies on LocalTesting
       }
   }
   ```

5. Run tests to verify:
   ```bash
   dotnet test LocalTesting/LocalTesting.sln --filter "FullyQualifiedName~DayXX"
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
