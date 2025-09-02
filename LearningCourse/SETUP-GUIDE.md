# 🚀 Environment Setup Scripts

This directory contains cross-platform setup scripts to automatically install and configure everything needed for the FlinkDotNet Learning Course.

## 📋 Quick Start

### For All Platforms (Recommended)
```bash
# Auto-detect your platform and run the appropriate setup
./setup-environment.sh
```

### Platform-Specific Scripts

#### 🐧 Linux & 🍎 macOS
```bash
./setup-environment-linux-macos.sh
```

#### 🪟 Windows
```powershell
.\setup-environment-windows.ps1
```

## 📦 What Gets Installed

All setup scripts install the following components:

### Core Requirements
- ✅ **.NET 9.0 SDK** - Latest version with all required workloads
- ✅ **Docker Desktop** - Container runtime for LocalTesting infrastructure
- ✅ **Git** - Version control system
- ✅ **Aspire Workload** - .NET orchestration platform

### Repository Setup
- ✅ **FlinkDotNet Repository** - Cloned to `~/FlinkDotNet` (Linux/macOS) or `%USERPROFILE%\FlinkDotNet` (Windows)
- ✅ **Environment Variables** - `FLINK_DOTNET_PATH` set to repository location
- ✅ **Build Validation** - Ensures all solutions build successfully

## 🖥️ System Requirements

### Minimum Requirements
- **Memory**: 8GB RAM (16GB recommended)
- **Storage**: 10GB free space
- **OS**: 
  - Linux: Ubuntu 18.04+, RHEL 8+, or equivalent
  - macOS: 10.15+ (Catalina)
  - Windows: Windows 10 or Windows 11

### Required Permissions
- **Linux/macOS**: Sudo access for Docker installation
- **Windows**: Administrator privileges (recommended)

## 🔧 Setup Script Options

### Universal Script
```bash
./setup-environment.sh [OPTIONS]

Options:
  --help, -h    Show help information
```

### Linux/macOS Script
```bash
./setup-environment-linux-macos.sh

Features:
  • Auto-detects Linux distribution (Ubuntu, RHEL, etc.)
  • Uses package managers (apt, yum, brew)
  • Handles both Intel and Apple Silicon Macs
  • Sets up environment variables in bash/zsh
```

### Windows Script
```powershell
.\setup-environment-windows.ps1 [OPTIONS]

Options:
  -Force        Force reinstall even if components exist
  -SkipDocker   Skip Docker installation (useful for Podman users)
  -Help         Show detailed help information

Examples:
  .\setup-environment-windows.ps1                # Standard setup
  .\setup-environment-windows.ps1 -Force         # Force reinstall
  .\setup-environment-windows.ps1 -SkipDocker    # Skip Docker
```

## 🚀 After Setup

Once setup is complete, follow these steps:

### 1. Restart Your Terminal
```bash
# This ensures all environment variables are loaded
```

### 2. Navigate to Learning Course
```bash
# Linux/macOS
cd ~/FlinkDotNet/LearningCourse

# Windows
cd %USERPROFILE%\FlinkDotNet\LearningCourse
```

### 3. Read the Student Guide
```bash
# Open the comprehensive learning guide
cat README.md
```

### 4. Start LocalTesting Infrastructure
```bash
cd ../LocalTesting
dotnet run --project LocalTesting.AppHost

# Wait 90 seconds for all services to start
# Then verify: http://localhost:8081 (Flink), http://localhost:18888 (Aspire)
```

### 5. Begin Day 1
```bash
cd ../LearningCourse/Day01-Flink21-Fundamentals/Exercise-Solutions
# Follow the README.md instructions
```

## 🛠️ Troubleshooting

### Common Issues

#### .NET SDK Not Found
```bash
# Linux/macOS: Add to PATH
export PATH="$HOME/.dotnet:$PATH"

# Windows: Restart PowerShell or add to PATH manually
```

#### Docker Not Running
```bash
# Linux
sudo systemctl start docker

# macOS/Windows
# Start Docker Desktop application
```

#### Permission Denied
```bash
# Linux: Add user to docker group
sudo usermod -aG docker $USER
# Then log out and back in

# Windows: Run PowerShell as Administrator
```

#### Build Failures
```bash
# Clean and restore packages
dotnet clean
dotnet restore
dotnet build
```

### Manual Installation

If the automated scripts fail, you can install components manually:

#### .NET 9.0 SDK
- Download from: https://dotnet.microsoft.com/download/dotnet/9.0
- Install the SDK (not just runtime)

#### Docker
- **Linux**: https://docs.docker.com/engine/install/
- **macOS**: https://docs.docker.com/desktop/install/mac-install/
- **Windows**: https://docs.docker.com/desktop/install/windows-install/

#### Git
- **Linux**: `sudo apt install git` or `sudo yum install git`
- **macOS**: `brew install git` or use Xcode tools
- **Windows**: https://git-scm.com/download/win

#### Aspire Workload
```bash
dotnet workload install aspire
```

## 📞 Getting Help

### Verification Commands
```bash
# Check .NET version (should be 9.0.x)
dotnet --version

# Check Docker
docker --version
docker ps

# Check Git
git --version

# Check Aspire workload
dotnet workload list | grep aspire
```

### Support Resources
- **Repository Issues**: https://github.com/devstress/FlinkDotnet/issues
- **Learning Course Guide**: [README.md](README.md)
- **.NET Documentation**: https://docs.microsoft.com/dotnet/
- **Docker Documentation**: https://docs.docker.com/

## 🎯 Next Steps

After successful setup:

1. **📚 Review Course Overview**: Read `README.md` for the complete 14-day learning path
2. **🏃‍♂️ Start Day 1**: Navigate to `Day01-Flink21-Fundamentals/Exercise-Solutions/`
3. **💡 Join the Community**: Contribute improvements and share your learning experience
4. **🔄 Keep Updated**: Run `git pull` regularly to get the latest exercises and fixes

Happy learning! 🎓