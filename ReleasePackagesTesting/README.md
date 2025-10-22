# Release Packages Testing - Pre-Release

This folder validates release artifacts **BEFORE** publishing to NuGet.org and Docker Hub.

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
    
    - name: Test release packages
      shell: pwsh
      run: |
        ./ReleasePackagesTesting/test-release-packages.ps1 -Version ${{ needs.calculate-version.outputs.new_version }}
```

### Manual Testing

```bash
# Test local release artifacts
./ReleasePackagesTesting/test-release-packages.ps1 -Version 1.0.0
```

## What It Does

1. Loads Docker image from `./docker/jobgateway-VERSION.tar.gz`
2. Adds `./packages` as local NuGet source
3. Restores packages from local feed
4. Builds the solution
5. Runs all integration tests
6. Reports success or failure

## Validation

✅ All tests must pass BEFORE publishing to NuGet.org and Docker Hub  
✅ Validates local Docker image works with Flink cluster  
✅ Validates local NuGet packages have correct dependencies  
✅ Prevents publishing broken releases  

## Difference from ReleasePackagesTesting.Published

- **ReleasePackagesTesting** (this folder): Tests local artifacts BEFORE publishing (pre-release validation)
- **ReleasePackagesTesting.Published**: Tests published packages AFTER publishing (post-release validation)

Both are important:
- Pre-release prevents publishing broken packages (**this folder**)
- Post-release confirms the release actually works
