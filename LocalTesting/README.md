# LocalTesting - Aspire Environment for LearningCourse

This Aspire setup provides the infrastructure environment for the [LearningCourse](../LearningCourse/README.md). Please refer to the LearningCourse documentation for complete usage instructions and examples.

## Prerequisites

### .NET SDK Requirements
- **.NET 9.0 SDK or later** is required for proper Aspire testing framework functionality
- Check your version: `dotnet --version` (should show 9.0.x)
- Install from: https://dotnet.microsoft.com/download/dotnet/9.0

### Why .NET 9.0 is Required
- Aspire testing framework (`Aspire.Hosting.Testing`) is designed for .NET 9.0
- Integration tests will fail to build or run properly with .NET 8.0
- The observability test uses `DistributedApplicationTestingBuilder` which requires .NET 9.0

### Environment Verification
```bash
# Verify .NET version
dotnet --version  # Should show 9.0.x

# Build LocalTesting solution
dotnet build LocalTesting.sln --configuration Release

# Run integration tests
dotnet test LocalTesting.IntegrationTests/LocalTesting.IntegrationTests.csproj
```