# Release Package Validation Workflow

## Overview

The **Release Package Validation** workflow is designed to validate NuGet packages and Docker images before they are published to public repositories. This workflow provides confidence that release artifacts work correctly in isolation before being made available to users.

## Purpose

- **Pre-Release Testing**: Validates packages work correctly with local artifacts before publishing
- **Post-Release Simulation**: Tests how packages will behave after being downloaded from public registries
- **Continuous Integration**: Runs automatically on every push to catch issues early
- **Manual Testing**: Can be triggered manually with custom version numbers for testing

## Workflow Structure

### Jobs

1. **build-test-artifacts** (20 min timeout)
   - Builds FlinkDotNet solution
   - Runs unit tests
   - Creates NuGet packages with test version
   - Builds Docker image
   - Uploads artifacts for testing

2. **pre-release-validation** (20 min timeout)
   - Downloads local NuGet packages and Docker image
   - Sets up test environment with Aspire, Java, Maven
   - Runs ReleasePackagesTesting integration tests
   - Validates packages work before publishing

3. **post-release-validation** (20 min timeout)
   - Simulates fresh environment (clears NuGet cache)
   - Downloads artifacts (simulating NuGet.org and Docker Hub)
   - Runs ReleasePackagesTesting.Published integration tests
   - Validates packages work after publishing

4. **summary**
   - Reports overall validation status
   - Fails if any validation fails

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

### Using the Test Script

A local test script is provided to run the same validation locally:

```bash
# Run with default version (99.99.99)
./test-release-package-validation.sh

# Run with specific version
./test-release-package-validation.sh 1.2.3
```

### What the Test Script Does

1. Builds FlinkDotNet solution
2. Runs unit tests
3. Creates NuGet packages
4. Builds Docker image
5. Saves Docker image to tarball
6. Sets up local NuGet feed
7. Loads Docker image
8. Runs pre-release validation tests
9. Clears NuGet cache (simulates fresh environment)
10. Runs post-release validation tests
11. Cleans up Docker containers

### Prerequisites for Local Testing

- .NET 9.0 SDK with Aspire workload
- Docker Desktop or Podman
- Java 17 JDK
- Maven 3.9.6+
- 8GB+ RAM allocated to Docker

## Differences from Release Workflows

| Aspect | Release Package Validation | Actual Release Workflow |
|--------|---------------------------|------------------------|
| **Purpose** | Test packages before release | Publish packages to production |
| **Version** | Test version (99.99.99) | Real version (e.g., 1.2.0) |
| **Publishing** | No publishing, only testing | Publishes to NuGet.org and Docker Hub |
| **Artifacts** | Uploaded for validation only | Uploaded as release assets |
| **Retention** | 1 day | 30 days |
| **Trigger** | Every push + manual | Manual only (workflow_dispatch) |

## Integration with Development Workflow

### When to Use

- **Before Opening PR**: Run locally to catch issues early
- **During PR Review**: Automatically runs on push
- **Before Release**: Manual trigger with release version for final validation
- **Debugging Release Issues**: Reproduce release environment locally

### CI/CD Integration

The workflow integrates with the development process:

1. Developer pushes code
2. Workflow automatically runs validation
3. If validation fails, developer sees failure immediately
4. Developer can debug locally using test script
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

| Workflow | Purpose | Artifacts | Duration |
|----------|---------|-----------|----------|
| **Unit Tests** | Test individual components | None | 5-10 min |
| **LocalTesting Integration Tests** | Test with project references | None | 15 min |
| **Release Package Validation** | Test with built packages | NuGet + Docker | 40-60 min |
| **Release Workflows** | Publish to production | NuGet + Docker + Release | 60-90 min |

## Best Practices

1. **Run Locally First**: Use test script before pushing
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
