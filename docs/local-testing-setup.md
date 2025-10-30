# Local Testing Environment Setup for FlinkDotNet

## Required .NET Version

This project requires **.NET 9.0.303** as specified in `global.json`. 

## Aspire Tooling Platform Requirements

**Critical Platform Difference**: .NET Aspire workload availability varies by operating system:

### Windows and macOS
- **Aspire tooling is INCLUDED** with .NET SDK (.NET 8 onward)
- **Usually no manual installation required**

### Linux
- **Aspire tooling is NOT bundled** with base .NET SDK packages
- **Manual installation REQUIRED**: `dotnet workload install aspire`
- **This is by design**: Linux package managers typically distribute minimal SDK packages

### Why This Difference Exists
Microsoft bundles Aspire tooling in their official Windows/macOS .NET SDK installers, but Linux distributions via package managers (apt, yum, dnf) typically provide base SDK packages without optional workloads to keep package sizes minimal and allow users to install only needed components. 

## Verify .NET SDK

Check your local .NET version matches `global.json` (9.0.x).
```bash
dotnet --version
```

## Installation Instructions

To properly test locally, please install .NET 9.0 SDK:

### Option 1: Use the provided install script
```bash
chmod +x ./scripts/dotnet-install.sh
./scripts/dotnet-install.sh --version 9.0.303
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

# Verify Java and Maven (auto-installed if not found, required for Gateway build that prebuilds the IR Runner jar)
java -version     # Java 17 required (auto-installed if not found)
mvn -version      # Maven 3.9.6+ (auto-installed if not found)

# Install Aspire workload if needed (required on Linux):
dotnet workload install aspire

# Build all solutions
dotnet build FlinkDotNet/FlinkDotNet.sln --configuration Release
dotnet build Sample/Sample.sln --configuration Release  
dotnet build LocalTesting/LocalTesting.sln --configuration Release

# Start LocalTesting host (Kafka + Flink + Gateway)
dotnet run --project LocalTesting/LocalTesting.FlinkSqlAppHost/LocalTesting.FlinkSqlAppHost.csproj

# In a separate shell, run integration test that submits a job
dotnet test LocalTesting/LocalTesting.IntegrationTests/LocalTesting.IntegrationTests.csproj -c Debug --filter TestCategory=gateway-bundling
```

## GitHub Workflows Validation

Once .NET 9.0 is installed, run these workflows locally to ensure they pass:

1. **Build Workflow**: `dotnet build`
2. **Unit Tests**: Run all test projects
3. **Integration Tests**: Kafka connector tests
4. **LocalTesting**: Aspire orchestration with Docker
5. **Stress Tests**: Performance validation

## Notes

- Linux may require installing the Aspire workload separately: `dotnet workload install aspire`.
- Ensure Docker Desktop, Podman, or a compatible container runtime is available for Aspire resources.
- Place optional Flink connector JARs under `LocalTesting/connectors/flink/lib/` so the LocalTesting gateway bundles them; copy the same jars into `/opt/flink/lib` when targeting a real Flink cluster.
- Java 17 and Maven 3.9.6 are auto-installed if not found; the Gateway build prebuilds `flink-ir-runner-java17.jar`.

## Podman on Windows: Host Networking Configuration

When using **Podman on Windows** (instead of Docker Desktop), containers run inside a lightweight WSL2 VM (`podman-machine-default`). This affects how containers communicate with host services like JobGateway.

### Automatic Fix (Implemented)

The LocalTesting infrastructure **automatically detects Podman** and configures networking correctly:

1. **Gateway IP Detection**: Queries the Podman VM gateway IP dynamically
2. **Host Mapping**: Adds `--add-host=host.containers.internal:<gatewayIP>` to containers
3. **JobGateway Binding**: Binds to `0.0.0.0:8086` (all interfaces) instead of `localhost:8086`

### Expected Console Output

When Podman is detected, you'll see:
```
✅ Using Podman as container runtime (Docker not available)
🔧 Podman: Mapping host.containers.internal to gateway IP 172.20.112.1
🔧 This allows Prometheus to reach JobGateway on host
```

### Troubleshooting Podman Networking

If Prometheus cannot connect to JobGateway (target shows as DOWN):

1. **Verify Gateway IP Detection**:
   ```bash
   wsl -d podman-machine-default -- ip -4 route show default
   # Expected output: default via 172.20.x.x dev eth0 ...
   ```

2. **Check Podman Machine Status**:
   ```bash
   podman machine list
   # Ensure podman-machine-default is RUNNING
   ```

3. **Verify JobGateway Metrics Endpoint**:
   ```bash
   # From host, should work
   curl http://localhost:8086/metrics
   
   # Check Prometheus targets
   # Open: http://localhost:9090/targets
   # jobgateway should be UP (green)
   ```

### Why This Is Needed

- **Docker Desktop**: `host.containers.internal` automatically resolves to physical host
- **Podman**: `host.containers.internal` resolves to VM localhost, requiring gateway IP mapping
- **Solution**: Dynamic detection and --add-host argument ensures compatibility with both runtimes

For more details, see the implementation in `LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs` (lines 337, 432-451, and GetPodmanGatewayIP function).

