# Release Packages Testing - Pre-Release

This folder validates release artifacts **BEFORE** publishing to NuGet.org and Docker Hub using Microsoft Aspire integration tests.

## Purpose

This is a **PRE-RELEASE** validation step that runs **BEFORE** publishing to prevent broken releases.

Tests local artifacts:
- NuGet packages from `./packages/*.nupkg` (not NuGet.org)
- Docker image from `./docker/*.tar.gz` (not Docker Hub)

## When to Use

**Run this in the release workflow BEFORE publishing** after:
1. ✅ Building NuGet packages
2. ✅ Building Docker image

This prevents publishing broken packages to NuGet.org and Docker Hub.

## Structure

- `ReleasePackagesTesting.FlinkSqlAppHost` - Aspire AppHost using local Docker image
- `ReleasePackagesTesting.IntegrationTests` - Integration tests using local NuGet packages
- Same test scenarios as LocalTesting but using release artifacts (before publishing)

## Usage

### Using Aspire Integration Tests

The testing is done through Microsoft Aspire's integration testing framework, identical to LocalTesting:

```bash
# Set up local NuGet source
dotnet nuget add source ./packages --name LocalFeed

# Load Docker image
gunzip -c ./docker/jobgateway-VERSION.tar.gz | docker load

# Run Aspire integration tests
cd ReleasePackagesTesting
dotnet test --configuration Release
```

### In Release Workflow (Recommended)

Add before publishing:

```yaml
test-release-packages:
  name: Test Release Packages (Pre-Release)
  needs: [calculate-version, build-docker-image, build-and-package]
  runs-on: ubuntu-latest
  steps:
    - name: Checkout code
      uses: actions/checkout@v4
    
    - name: Set up .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'
    
    - name: Set up JDK 17
      uses: actions/setup-java@v4
      with:
        java-version: '17'
        distribution: 'temurin'
    
    - name: Install Maven
      uses: stCarolas/setup-maven@v4
      with:
        maven-version: '3.9.6'
    
    - name: Download NuGet packages
      uses: actions/download-artifact@v4
      with:
        name: nuget-packages
        path: ./packages
    
    - name: Download Docker image
      uses: actions/download-artifact@v4
      with:
        name: docker-image
        path: ./docker
    
    - name: Add local NuGet source
      run: dotnet nuget add source ./packages --name LocalFeed
    
    - name: Load Docker image
      run: gunzip -c ./docker/jobgateway-${{ needs.calculate-version.outputs.new_version }}.tar.gz | docker load
    
    - name: Run Aspire integration tests
      run: |
        cd ReleasePackagesTesting
        dotnet test --configuration Release --verbosity normal
```

## What It Tests

Uses Microsoft Aspire integration testing framework to:
1. Start Aspire AppHost with local Docker image
2. Deploy Flink cluster, Kafka, and other infrastructure
3. Run integration tests against JobGateway using local NuGet packages
4. Verify all Flink job patterns work correctly
5. Validate end-to-end functionality

## Validation

✅ All tests must pass BEFORE publishing to NuGet.org and Docker Hub  
✅ Validates local Docker image works with Flink cluster  
✅ Validates local NuGet packages have correct dependencies  
✅ Uses same Aspire testing infrastructure as LocalTesting  
✅ Prevents publishing broken releases  

## Difference from ReleasePackagesTesting.Published

- **ReleasePackagesTesting** (this folder): Tests local artifacts BEFORE publishing (pre-release validation)
- **ReleasePackagesTesting.Published**: Tests published packages AFTER publishing (post-release validation)

Both use Microsoft Aspire integration testing framework for comprehensive validation.
