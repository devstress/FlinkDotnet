# Release Packages Testing - Published

This folder validates that **published** packages from NuGet.org and Docker Hub work together correctly using Microsoft Aspire integration tests.

## Purpose

This is the **FINAL step** of the release workflow, run **AFTER** publishing to NuGet.org and Docker Hub to confirm the release is working.

Tests:
- `FlinkDotnet` package from **NuGet.org** (not local packages)
- `flinkdotnet/jobgateway` image from **Docker Hub** (not local Docker image)

## When to Use

**Run this as the last step in the release workflow** after:
1. ✅ Publishing NuGet packages to NuGet.org
2. ✅ Publishing Docker image to Docker Hub

This validates the published artifacts are compatible and working.

## Structure

- `ReleasePackagesTesting.Published.FlinkSqlAppHost` - Aspire AppHost using Docker Hub image
- `ReleasePackagesTesting.Published.IntegrationTests` - Integration tests using NuGet.org packages
- Same test scenarios as LocalTesting but using **published packages**

## Usage

### Using Aspire Integration Tests

The testing is done through Microsoft Aspire's integration testing framework, identical to LocalTesting:

```bash
# Pull Docker image from Docker Hub
docker pull flinkdotnet/jobgateway:VERSION

# Tag as latest if needed
docker tag flinkdotnet/jobgateway:VERSION flinkdotnet/jobgateway:latest

# Clear NuGet cache to force download from NuGet.org
dotnet nuget locals all --clear

# Run Aspire integration tests
cd ReleasePackagesTesting.Published
dotnet test --configuration Release
```

### In Release Workflow (Recommended)

Add as the final step after publishing:

```yaml
validate-published-packages:
  name: Validate Published Packages (Post-Release)
  needs: [calculate-version, publish-nuget, publish-docker]
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
    
    - name: Pull Docker image from Docker Hub
      run: |
        docker pull flinkdotnet/jobgateway:${{ needs.calculate-version.outputs.new_version }}
        docker tag flinkdotnet/jobgateway:${{ needs.calculate-version.outputs.new_version }} flinkdotnet/jobgateway:latest
    
    - name: Clear NuGet cache
      run: dotnet nuget locals all --clear
    
    - name: Run Aspire integration tests
      run: |
        cd ReleasePackagesTesting.Published
        dotnet test --configuration Release --verbosity normal
```

## What It Tests

Uses Microsoft Aspire integration testing framework to:
1. Pull Docker image from Docker Hub
2. Start Aspire AppHost with published Docker image
3. Deploy Flink cluster, Kafka, and other infrastructure
4. Install NuGet packages from NuGet.org
5. Run integration tests against JobGateway
6. Verify all Flink job patterns work correctly
7. Validate end-to-end functionality with published packages

## Validation

✅ All tests must pass for the release to be considered successful  
✅ Validates Docker image from Docker Hub works with Flink cluster  
✅ Validates NuGet package from NuGet.org has correct dependencies  
✅ Confirms published packages are compatible  
✅ Uses same Aspire testing infrastructure as LocalTesting  

## Difference from ReleasePackagesTesting

- **ReleasePackagesTesting**: Tests local artifacts BEFORE publishing (pre-release validation)
- **ReleasePackagesTesting.Published** (this folder): Tests published packages AFTER publishing (post-release validation)

Both use Microsoft Aspire integration testing framework:
- Pre-release prevents publishing broken packages
- Post-release confirms the release actually works
