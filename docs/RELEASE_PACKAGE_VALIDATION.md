# Release Package Validation Workflow

## Overview

The **Release Package Validation** workflow is designed to validate NuGet packages and Docker images before they are published to public repositories. This workflow provides confidence that release artifacts work correctly in isolation before being made available to users.

## Purpose

- **Pre-Release Testing**: Validates packages work correctly with local artifacts before publishing
- **Post-Release Simulation**: Tests how packages will behave after being downloaded from public registries
- **Continuous Integration**: Runs automatically on every push to catch issues early
- **Manual Testing**: Can be triggered manually with custom version numbers for testing

## Workflow Structure

The workflow consists of a single job that performs all validation steps:

**validate-release-packages** (30 min timeout)
- Builds FlinkDotNet solution
- Creates NuGet packages with test version
- Builds Docker image
- Sets up local NuGet feed
- Runs ReleasePackagesTesting integration tests
- Validates packages work correctly

## Triggering the Workflow

### Automatic Trigger
Runs on every push to any branch:
```yaml
on:
  push:
```

### Manual Trigger
Can be triggered manually with custom version:
```bash
# Via GitHub UI: Actions → Release Package Validation → Run workflow
# Specify version: e.g., 99.99.99 (default)
```

## Local Testing

You can run the validation tests locally by executing `dotnet test` on the ReleasePackagesTesting projects:

```bash
# Run pre-release validation tests
cd ReleasePackagesTesting
dotnet test --configuration Release

# Run post-release validation tests
cd ../ReleasePackagesTesting.Published
dotnet test --configuration Release
```

### Prerequisites for Local Testing

- .NET 9.0 SDK with Aspire workload
- Docker Desktop or Podman
- Java 17 JDK
- Maven 3.9.6+
- 8GB+ RAM allocated to Docker
- Local NuGet packages and Docker images (if testing with local artifacts)

## Differences from Release Workflows

| Aspect | Release Package Validation | Actual Release Workflow |
|--------|---------------------------|------------------------|
| **Purpose** | Test packages before release | Publish packages to production |
| **Version** | Test version (99.99.99) | Real version (e.g., 1.2.0) |
| **Jobs** | Single job (Build + Test) | Multiple jobs (Build, Test, Publish) |
| **Publishing** | No publishing, only testing | Publishes to NuGet.org and Docker Hub |
| **Artifacts** | Not uploaded | Uploaded as release assets |
| **Trigger** | Every push + manual | Manual only (workflow_dispatch) |

## Integration with Development Workflow

### When to Use

- **Before Opening PR**: Run `dotnet test` in ReleasePackagesTesting locally to catch issues early
- **During PR Review**: Automatically runs on push
- **Before Release**: Manual trigger with release version for final validation
- **Debugging Release Issues**: Run tests locally with specific configurations

### CI/CD Integration

The workflow integrates with the development process:

1. Developer pushes code
2. Workflow automatically runs validation
3. If validation fails, developer sees failure immediately
4. Developer can debug locally by running `dotnet test` in ReleasePackagesTesting
5. Once validation passes, code is ready for merge

## Monitoring and Debugging

### Viewing Results

- **GitHub Actions UI**: View workflow runs and logs
- **Artifacts**: Download test results for detailed analysis
- **Docker Logs**: Diagnostic logs included in workflow output

### Common Issues

1. **Container Connection Failures**
   - Check Docker daemon is running
   - Verify Podman/Docker compatibility
   - Review Docker container logs in workflow output

2. **Test Timeouts**
   - Increase timeout in workflow if needed
   - Check system resources (memory, CPU)
   - Review specific test logs

3. **Package Restoration Failures**
   - Verify NuGet source configuration
   - Check package version consistency
   - Review NuGet cache state

### Diagnostic Information

The workflow automatically collects:
- Docker container status
- JobGateway logs (last 200 lines)
- JobManager logs (last 200 lines)
- Kafka logs (last 200 lines)
- Test results (uploaded as artifacts)

## Comparison with Other Testing Workflows

| Workflow | Purpose | Jobs | Duration |
|----------|---------|------|----------|
| **Unit Tests** | Test individual components | Single job | 5-10 min |
| **LocalTesting Integration Tests** | Test with project references | Single job | 15 min |
| **Release Package Validation** | Test with built packages | Single job | 20-30 min |
| **Release Workflows** | Publish to production | Multiple jobs | 60-90 min |

## Best Practices

1. **Test Locally First**: Run `dotnet test` in ReleasePackagesTesting before pushing
2. **Monitor CI**: Check workflow status after push
3. **Review Logs**: Investigate failures using diagnostic logs
4. **Clean Environment**: Workflow simulates clean environment automatically
5. **Version Testing**: Use manual trigger to test specific versions

## Future Enhancements

Potential improvements:
- Parallel test execution for faster validation
- Integration with pull request comments
- Performance benchmarking during validation
- Automatic issue creation for failures
- Support for multiple .NET versions

## Related Documentation

- [Release Workflows](.github/workflows/release-*.yml) - Production release processes
- [LocalTesting README](LocalTesting/README.md) - Local development testing
- [ReleasePackagesTesting README](ReleasePackagesTesting/README.md) - Pre-release validation
- [ReleasePackagesTesting.Published README](ReleasePackagesTesting.Published/README.md) - Post-release validation
