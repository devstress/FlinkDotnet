# Release Workflow Testing Scripts

This directory contains scripts for testing the release workflow locally before submitting to CI.

## Quick Start

```bash
# Quick validation (recommended for regular testing)
./test-release-workflow-simple.sh

# Full validation with integration tests (when resources allow)
./test-release-workflow-complete.sh
```

## Scripts Overview

### test-release-workflow-simple.sh (Recommended)
**Purpose**: Fast validation of package compatibility without running resource-intensive integration tests

**What it tests**:
- ✅ FlinkDotNet solution builds successfully
- ✅ NuGet packages are created correctly
- ✅ Docker image builds successfully
- ✅ Pre-release validation projects can restore and build with local packages
- ✅ Post-release validation projects can restore and build with local packages

**Execution time**: ~3 minutes

**Resource requirements**: 
- .NET 9.0 SDK
- Docker (for building images)
- ~4GB RAM

**When to use**:
- Regular development workflow validation
- CI/CD pipeline checks
- Quick package compatibility verification
- Before submitting PRs that change package versions

### test-release-workflow-complete.sh (Thorough Testing)
**Purpose**: Full end-to-end validation including Aspire integration tests

**What it tests**:
- Everything in simple script PLUS:
- ✅ Aspire AppHost can start with all containers
- ✅ Flink cluster deploys correctly
- ✅ JobGateway accepts and processes jobs
- ✅ Kafka integration works end-to-end
- ✅ All integration test patterns pass

**Execution time**: 10-15 minutes

**Resource requirements**:
- .NET 9.0 SDK with Aspire workload
- Docker with 8GB+ RAM allocated
- Maven and JDK 17
- ~12GB RAM total

**When to use**:
- Before final release submission
- When integration tests need validation
- Debugging release workflow issues
- Comprehensive end-to-end validation

## What the Release Workflows Test

### Pre-Release Validation (ReleasePackagesTesting/)
Tests packages BEFORE publishing to ensure quality:
- Uses local NuGet packages from `./packages/`
- Uses local Docker image from `./docker/`
- Validates packages work with Flink and Kafka
- Prevents publishing broken releases

### Post-Release Validation (ReleasePackagesTesting.Published/)
Tests published packages AFTER release:
- Downloads packages from NuGet.org (or uses local as substitute)
- Pulls Docker images from Docker Hub (or uses local as substitute)
- Validates published artifacts are compatible
- Confirms release actually works

## Common Issues and Solutions

### Issue: Package Version Conflicts
**Error**: `NU1605: Detected package downgrade`

**Solution**: Check transitive dependencies and upgrade to required versions
```bash
# Example: Upgrade Confluent.Kafka to match Aspire.Hosting.Kafka requirements
dotnet add package Confluent.Kafka --version 2.11.1
```

### Issue: AppHost Reference Errors
**Error**: `CS0234: The type or namespace name 'XXX_FlinkSqlAppHost' does not exist`

**Solution**: Verify AppHost class name matches project name with underscores
```csharp
// Correct pattern:
Projects.ReleasePackagesTesting_FlinkSqlAppHost          // for ReleasePackagesTesting.FlinkSqlAppHost
Projects.ReleasePackagesTesting_Published_FlinkSqlAppHost // for ReleasePackagesTesting.Published.FlinkSqlAppHost
```

### Issue: Docker Out of Memory
**Error**: Container creation fails or tests timeout

**Solution**: Increase Docker Desktop memory allocation
- Docker Desktop → Settings → Resources
- Set Memory to at least 8GB
- Restart Docker Desktop

### Issue: Integration Tests Timeout
**Error**: `Job did not reach RUNNING state within 30s`

**Solution**: Use simplified script for quick validation, or increase test timeout
```bash
# Use simplified script instead
./test-release-workflow-simple.sh
```

## Script Usage Examples

### Basic Usage
```bash
# Run with default version (99.99.99)
./test-release-workflow-simple.sh

# Run with specific version
./test-release-workflow-simple.sh 1.2.3
```

### CI/CD Integration
```yaml
# GitHub Actions example
- name: Validate Release Packages
  run: ./test-release-workflow-simple.sh ${{ needs.calculate-version.outputs.new_version }}
```

### Local Development
```bash
# Quick check before committing
./test-release-workflow-simple.sh

# Full validation before release
./test-release-workflow-complete.sh
```

## Troubleshooting

### Check .NET Version
```bash
dotnet --version  # Should be 9.0.x
```

### Check Docker
```bash
docker --version
docker info  # Check if daemon is running
```

### Check Aspire Workload
```bash
dotnet workload list  # Should show 'aspire'
```

### Clean Build
```bash
# If validation fails, try cleaning first
dotnet clean FlinkDotNet/FlinkDotNet.sln
dotnet nuget locals all --clear
docker system prune -a  # Warning: removes all unused Docker images
```

## Related Documentation

- [ReleasePackagesTesting README](./ReleasePackagesTesting/README.md) - Pre-release validation details
- [ReleasePackagesTesting.Published README](./ReleasePackagesTesting.Published/README.md) - Post-release validation details
- [Release Workflows](./.github/workflows/) - Actual CI/CD workflows

## Support

For issues with these scripts:
1. Check the troubleshooting section above
2. Review error messages for specific package or build issues
3. Ensure environment meets all requirements
4. Check Docker logs if container-related issues occur
