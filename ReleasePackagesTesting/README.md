# Release Packages Testing

This folder validates release packages before publishing to NuGet.org and Docker Hub.

## Purpose

Tests the actual release artifacts to ensure they work correctly:
- NuGet packages (FlinkDotnet)
- Docker image (flinkdotnet/jobgateway)

## Structure

- `ReleasePackagesTesting.FlinkSqlAppHost` - Aspire AppHost using Docker image
- `ReleasePackagesTesting.IntegrationTests` - Integration tests using NuGet packages
- Same test scenarios as LocalTesting but using release artifacts

## Usage

Run from release workflows after building packages and Docker image:

```bash
# Load Docker image
gunzip -c ./docker/jobgateway-VERSION.tar.gz | docker load

# Add local NuGet feed
dotnet nuget add source ./packages --name LocalFeed

# Run tests
dotnet test ReleasePackagesTesting/ReleasePackagesTesting.sln
```

## Validation

- All tests must pass before publishing to NuGet.org
- Validates Docker image works with Flink cluster
- Validates NuGet packages have correct dependencies
