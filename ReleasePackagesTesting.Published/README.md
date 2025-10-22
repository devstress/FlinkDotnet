# Release Packages Testing - Published

This folder validates that **published** packages from NuGet.org and Docker Hub work together correctly.

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

### In Release Workflow (Recommended)

Add as the final step after publishing:

```yaml
validate-published-packages:
  name: Validate Published Packages
  needs: [publish-nuget, publish-docker]
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
    
    - name: Validate published packages
      shell: pwsh
      run: |
        cd ReleasePackagesTesting.Published
        ./test-published-packages.ps1 -DockerTag "${{ needs.calculate-version.outputs.new_version }}"
```

### Manual Testing

```bash
# Test latest published packages
cd ReleasePackagesTesting.Published
./test-published-packages.ps1

# Test specific version
./test-published-packages.ps1 -DockerTag "1.0.0"
```

## What It Does

1. Pulls `flinkdotnet/jobgateway:VERSION` from Docker Hub
2. Clears NuGet cache
3. Restores `FlinkDotnet` package from NuGet.org
4. Builds the solution
5. Runs all integration tests
6. Reports success or failure

## Validation

✅ All tests must pass for the release to be considered successful  
✅ Validates Docker image from Docker Hub works with Flink cluster  
✅ Validates NuGet package from NuGet.org has correct dependencies  
✅ Confirms published packages are compatible  

## Difference from ReleasePackagesTesting

- **ReleasePackagesTesting**: Tests local artifacts BEFORE publishing (pre-release validation)
- **ReleasePackagesTesting.Published**: Tests published packages AFTER publishing (post-release validation)

Both are important:
- Pre-release prevents publishing broken packages
- Post-release confirms the release actually works
