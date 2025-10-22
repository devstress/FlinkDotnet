# Release Packages Testing

This folder validates release packages before and after publishing to NuGet.org and Docker Hub.

## Purpose

Tests the actual release artifacts to ensure they work correctly:
- NuGet packages (FlinkDotnet)
- Docker image (flinkdotnet/jobgateway)

## Structure

- `ReleasePackagesTesting.FlinkSqlAppHost` - Aspire AppHost using Docker image
- `ReleasePackagesTesting.IntegrationTests` - Integration tests using NuGet packages
- Same test scenarios as LocalTesting but using release artifacts

## Usage

### Pre-Release Validation

Run from release workflows after building packages and Docker image (before publishing):

```bash
# Load Docker image
gunzip -c ./docker/jobgateway-VERSION.tar.gz | docker load

# Add local NuGet feed
dotnet nuget add source ./packages --name LocalFeed

# Run tests
dotnet test ReleasePackagesTesting/ReleasePackagesTesting.sln
```

Or use the automated script:

```bash
./ReleasePackagesTesting/test-release-packages.ps1 -Version 1.0.0
```

### Post-Release Validation

Run after publishing to verify latest packages work together:

```bash
# Validates latest published packages from NuGet.org and Docker Hub
./ReleasePackagesTesting/validate-latest-release.ps1

# Or test a specific Docker tag
./ReleasePackagesTesting/validate-latest-release.ps1 -DockerTag "1.0.0"
```

This ensures:
- Latest FlinkDotnet package on NuGet.org
- Latest flinkdotnet/jobgateway image on Docker Hub
- Both packages work together correctly

## Validation

- All tests must pass before publishing to NuGet.org
- Validates Docker image works with Flink cluster
- Validates NuGet packages have correct dependencies
- Post-release validation ensures published packages are compatible
