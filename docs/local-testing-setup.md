# Local Testing Environment Setup for FlinkDotNet

## Required .NET Version

This project requires **.NET 9.0.303** as specified in `global.json`. 

## Current Environment Status

**❌ Local Environment Issue**: .NET 9.0 SDK is not currently installed in this environment. 

```bash
$ dotnet --version
The command could not be loaded, possibly because:
  * You intended to execute a .NET application:
      The application '--version' does not exist.
  * You intended to execute a .NET SDK command:
      A compatible .NET SDK was not found.

Requested SDK version: 9.0.303
global.json file: /home/runner/work/FlinkDotnet/FlinkDotnet/global.json

Installed SDKs:
8.0.118 [/usr/lib/dotnet/sdk]

Install the [9.0.303] .NET SDK or update [/home/runner/work/FlinkDotnet/FlinkDotnet/global.json] to match an installed SDK.
```

## Installation Instructions

To properly test locally, please install .NET 9.0 SDK:

### Option 1: Use the provided install script
```bash
chmod +x ./dotnet-install.sh
./dotnet-install.sh --version 9.0.303
```

### Option 2: Download from Microsoft
Visit: https://dotnet.microsoft.com/download/dotnet/9.0

### Option 3: Use package manager (Ubuntu/Debian)
```bash
# Add Microsoft package repository
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Install .NET 9.0 SDK
sudo apt update
sudo apt install -y dotnet-sdk-9.0
```

## Verification Commands

After installing .NET 9.0, verify the environment:

```bash
# Verify .NET version
dotnet --version  # Should return 9.0.x

# Verify Aspire workload (required for LocalTesting)
dotnet workload list  # Should show aspire installed

# If Aspire not installed:
dotnet workload install aspire

# Build all solutions
dotnet build FlinkDotNet/FlinkDotNet.sln --configuration Release
dotnet build Sample/Sample.sln --configuration Release  
dotnet build LocalTesting/LocalTesting.sln --configuration Release

# Test LocalTesting workflow
./test-aspire-localtesting.ps1 -MessageCount 1000
```

## GitHub Workflows Validation

Once .NET 9.0 is installed, run these workflows locally to ensure they pass:

1. **Build Workflow**: `dotnet build`
2. **Unit Tests**: Run all test projects
3. **Integration Tests**: Kafka connector tests
4. **LocalTesting**: Aspire orchestration with Docker
5. **Stress Tests**: Performance validation

## Architecture Documentation Status

✅ **Completed**: 
- Removed all committed `bin/` and `obj/` build artifacts
- Added comprehensive PyFlink vs FlinkDotNet architecture comparison
- Documented dependency differences and communication patterns

**Key Findings**:
- **PyFlink**: Direct JVM integration via Py4J, CloudPickle, python-dateutil
- **FlinkDotNet**: HTTP REST API integration via AspNetCore, System.Text.Json, HttpClient
- **Different approaches**: PyFlink = embedded runtime, FlinkDotNet = service-oriented architecture