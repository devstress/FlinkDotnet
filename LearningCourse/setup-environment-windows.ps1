# =============================================================================
# FlinkDotNet Learning Course Environment Setup Script for Windows
# Supports: Windows 10, Windows 11, Windows Server
# =============================================================================

param(
    [switch]$Force,
    [switch]$SkipDocker,
    [switch]$Help
)

if ($Help) {
    Write-Host @"
🚀 FlinkDotNet Learning Course Environment Setup for Windows

Usage: .\setup-environment-windows.ps1 [OPTIONS]

Options:
  -Force       Force reinstall even if components are already installed
  -SkipDocker  Skip Docker installation (useful if using Podman)
  -Help        Show this help message

Examples:
  .\setup-environment-windows.ps1                # Standard setup
  .\setup-environment-windows.ps1 -Force         # Force reinstall everything
  .\setup-environment-windows.ps1 -SkipDocker    # Skip Docker (use Podman)

"@
    exit 0
}

Write-Host "🚀 FlinkDotNet Learning Course Environment Setup" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green
Write-Host ""

# Check if running as Administrator (required for some installations)
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")

if (-not $isAdmin) {
    Write-Host "⚠️  Warning: Not running as Administrator" -ForegroundColor Yellow
    Write-Host "   Some installations may require Administrator privileges" -ForegroundColor Yellow
    Write-Host "   If you encounter issues, try running as Administrator" -ForegroundColor Yellow
    Write-Host ""
}

# Function to check if command exists
function Test-Command {
    param($Command)
    try {
        Get-Command $Command -ErrorAction Stop | Out-Null
        return $true
    } catch {
        return $false
    }
}

# Function to get available memory in GB
function Get-MemoryGB {
    $memory = Get-CimInstance -ClassName Win32_ComputerSystem
    return [Math]::Round($memory.TotalPhysicalMemory / 1GB, 1)
}

# Function to install Chocolatey
function Install-Chocolatey {
    Write-Host "📦 Installing Chocolatey package manager..." -ForegroundColor Blue
    
    try {
        Set-ExecutionPolicy Bypass -Scope Process -Force
        [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
        Invoke-Expression ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
        
        # Refresh environment variables
        $env:PATH = [Environment]::GetEnvironmentVariable("PATH", "Machine") + ";" + [Environment]::GetEnvironmentVariable("PATH", "User")
        
        Write-Host "   ✅ Chocolatey installed successfully" -ForegroundColor Green
        return $true
    } catch {
        Write-Host "   ❌ Chocolatey installation failed: $_" -ForegroundColor Red
        return $false
    }
}

# Function to install .NET 9.0 SDK
function Install-DotNet {
    Write-Host "📦 Installing .NET 9.0 SDK..." -ForegroundColor Blue
    
    try {
        # Try Chocolatey first
        if (Test-Command choco) {
            choco install dotnet-9.0-sdk -y
        } else {
            # Fallback to direct download
            Write-Host "   Downloading .NET 9.0 SDK installer..." -ForegroundColor Yellow
            $downloadUrl = "https://download.microsoft.com/download/e/8/4/e844ccec-64a4-4e1b-a6df-4c1b8dc12207/dotnet-sdk-9.0.304-win-x64.exe"
            $installerPath = "$env:TEMP\dotnet-sdk-9.0.304-win-x64.exe"
            
            Invoke-WebRequest -Uri $downloadUrl -OutFile $installerPath
            
            Write-Host "   Running .NET SDK installer..." -ForegroundColor Yellow
            Start-Process -FilePath $installerPath -ArgumentList "/quiet" -Wait
            
            Remove-Item $installerPath -Force
        }
        
        # Refresh environment variables
        $env:PATH = [Environment]::GetEnvironmentVariable("PATH", "Machine") + ";" + [Environment]::GetEnvironmentVariable("PATH", "User")
        
        # Verify installation
        if (Test-Command dotnet) {
            $version = & dotnet --version
            Write-Host "   ✅ .NET SDK installed: $version" -ForegroundColor Green
            return $true
        } else {
            Write-Host "   ❌ .NET SDK installation verification failed" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host "   ❌ .NET SDK installation failed: $_" -ForegroundColor Red
        return $false
    }
}

# Function to install Docker Desktop
function Install-Docker {
    Write-Host "🐳 Installing Docker Desktop..." -ForegroundColor Blue
    
    try {
        if (Test-Command choco) {
            choco install docker-desktop -y
        } else {
            Write-Host "   Please download Docker Desktop manually from:" -ForegroundColor Yellow
            Write-Host "   https://docs.docker.com/desktop/install/windows-install/" -ForegroundColor Yellow
            Write-Host ""
            $continue = Read-Host "   Press Enter after installing Docker Desktop, or 'skip' to continue without Docker"
            if ($continue -eq "skip") {
                return $false
            }
        }
        
        Write-Host "   ✅ Docker installation completed" -ForegroundColor Green
        Write-Host "   ⚠️  Please restart Docker Desktop and ensure it's running" -ForegroundColor Yellow
        return $true
    } catch {
        Write-Host "   ❌ Docker installation failed: $_" -ForegroundColor Red
        return $false
    }
}

# Function to install Git
function Install-Git {
    Write-Host "📝 Installing Git..." -ForegroundColor Blue
    
    try {
        if (Test-Command choco) {
            choco install git -y
        } else {
            Write-Host "   Please download Git manually from:" -ForegroundColor Yellow
            Write-Host "   https://git-scm.com/download/win" -ForegroundColor Yellow
            Write-Host ""
            $continue = Read-Host "   Press Enter after installing Git"
        }
        
        # Refresh environment variables
        $env:PATH = [Environment]::GetEnvironmentVariable("PATH", "Machine") + ";" + [Environment]::GetEnvironmentVariable("PATH", "User")
        
        Write-Host "   ✅ Git installed" -ForegroundColor Green
        return $true
    } catch {
        Write-Host "   ❌ Git installation failed: $_" -ForegroundColor Red
        return $false
    }
}

# Function to clone repository
function Initialize-Repository {
    Write-Host "📁 Setting up FlinkDotNet repository..." -ForegroundColor Blue
    
    $repoDir = "$env:USERPROFILE\FlinkDotNet"
    
    try {
        if (Test-Path $repoDir) {
            Write-Host "   📁 Repository already exists at $repoDir" -ForegroundColor Yellow
            Write-Host "      Updating repository..." -ForegroundColor Yellow
            Set-Location $repoDir
            & git pull origin main
        } else {
            Write-Host "   📁 Cloning repository to $repoDir..." -ForegroundColor Yellow
            & git clone https://github.com/devstress/FlinkDotnet.git $repoDir
            Set-Location $repoDir
        }
        
        Write-Host "   ✅ Repository ready at: $repoDir" -ForegroundColor Green
        
        # Set environment variable
        [Environment]::SetEnvironmentVariable("FLINK_DOTNET_PATH", $repoDir, "User")
        $env:FLINK_DOTNET_PATH = $repoDir
        
        return $repoDir
    } catch {
        Write-Host "   ❌ Repository setup failed: $_" -ForegroundColor Red
        return $null
    }
}

# Main setup function
function Start-Setup {
    Write-Host "🔍 Checking system requirements..." -ForegroundColor Blue
    Write-Host ""
    
    # Check Windows version
    $osVersion = [Environment]::OSVersion.Version
    Write-Host "🖥️  Windows Version: $($osVersion.Major).$($osVersion.Minor)" -ForegroundColor Cyan
    if ($osVersion.Major -lt 10) {
        Write-Host "   ⚠️  Warning: Windows 10 or later recommended" -ForegroundColor Yellow
    }
    
    # Check memory
    $memoryGB = Get-MemoryGB
    Write-Host "💾 Available Memory: ${memoryGB}GB" -ForegroundColor Cyan
    if ($memoryGB -lt 8) {
        Write-Host "   ⚠️  Warning: Recommended minimum is 8GB RAM" -ForegroundColor Yellow
        Write-Host "      You may experience performance issues with LocalTesting infrastructure" -ForegroundColor Yellow
    } else {
        Write-Host "   ✅ Memory requirements met" -ForegroundColor Green
    }
    Write-Host ""
    
    # Check PowerShell execution policy
    $executionPolicy = Get-ExecutionPolicy
    if ($executionPolicy -eq "Restricted") {
        Write-Host "⚠️  PowerShell execution policy is Restricted" -ForegroundColor Yellow
        Write-Host "   Setting execution policy to RemoteSigned for current user..." -ForegroundColor Yellow
        try {
            Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser -Force
            Write-Host "   ✅ Execution policy updated" -ForegroundColor Green
        } catch {
            Write-Host "   ❌ Failed to update execution policy: $_" -ForegroundColor Red
        }
    }
    Write-Host ""
    
    # Install Chocolatey if not present
    if (-not (Test-Command choco) -and -not $Force) {
        $installChoco = Read-Host "📦 Chocolatey package manager not found. Install it? (Y/n)"
        if ($installChoco -ne "n" -and $installChoco -ne "N") {
            Install-Chocolatey | Out-Null
        }
    }
    Write-Host ""
    
    # Check and install .NET 9.0
    if (Test-Command dotnet -and -not $Force) {
        $version = & dotnet --version
        if ($version -like "9.*") {
            Write-Host "✅ .NET 9.0 SDK already installed: $version" -ForegroundColor Green
        } else {
            Write-Host "❌ .NET version $version found, but need 9.0.x" -ForegroundColor Red
            Install-DotNet | Out-Null
        }
    } else {
        Install-DotNet | Out-Null
    }
    Write-Host ""
    
    # Check and install Docker
    if (-not $SkipDocker) {
        if (Test-Command docker -and -not $Force) {
            Write-Host "✅ Docker already installed" -ForegroundColor Green
            try {
                & docker version | Out-Null
                Write-Host "   ✅ Docker is running" -ForegroundColor Green
            } catch {
                Write-Host "   ⚠️  Docker is installed but not running" -ForegroundColor Yellow
                Write-Host "      Please start Docker Desktop" -ForegroundColor Yellow
            }
        } else {
            Install-Docker | Out-Null
        }
    } else {
        Write-Host "🐳 Docker installation skipped (use -SkipDocker flag)" -ForegroundColor Yellow
    }
    Write-Host ""
    
    # Check and install Git
    if (Test-Command git -and -not $Force) {
        $gitVersion = & git --version
        Write-Host "✅ Git already installed: $gitVersion" -ForegroundColor Green
    } else {
        Install-Git | Out-Null
    }
    Write-Host ""
    
    # Clone repository
    $repoPath = Initialize-Repository
    if (-not $repoPath) {
        Write-Host "❌ Failed to set up repository" -ForegroundColor Red
        exit 1
    }
    Write-Host ""
    
    # Verify .NET environment
    Write-Host "🔧 Verifying .NET environment..." -ForegroundColor Blue
    Set-Location $repoPath
    
    try {
        $dotnetVersion = & dotnet --version
        Write-Host "   ✅ .NET SDK: $dotnetVersion" -ForegroundColor Green
        
        # Check if Aspire workload is installed
        $workloads = & dotnet workload list
        if ($workloads -like "*aspire*") {
            Write-Host "   ✅ Aspire workload already installed" -ForegroundColor Green
        } else {
            Write-Host "   📦 Installing Aspire workload..." -ForegroundColor Yellow
            & dotnet workload install aspire
            Write-Host "   ✅ Aspire workload installed" -ForegroundColor Green
        }
    } catch {
        Write-Host "   ❌ .NET SDK verification failed: $_" -ForegroundColor Red
        Write-Host "      Please restart PowerShell and try again" -ForegroundColor Yellow
        exit 1
    }
    Write-Host ""
    
    # Test build
    Write-Host "🔨 Testing build environment..." -ForegroundColor Blue
    try {
        $buildResult = & .\validate-build-and-tests.ps1 -SkipTests 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   ✅ All solutions build successfully" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  Build validation had issues - this may be normal for first run" -ForegroundColor Yellow
            Write-Host "      You can run '.\validate-build-and-tests.ps1' manually later" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "   ⚠️  Build test skipped - you can run '.\validate-build-and-tests.ps1' manually" -ForegroundColor Yellow
    }
    Write-Host ""
    
    # Final instructions
    Write-Host "🎉 Setup Complete!" -ForegroundColor Green
    Write-Host "==================" -ForegroundColor Green
    Write-Host ""
    Write-Host "📍 Repository location: $repoPath" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "🚀 Quick Start:" -ForegroundColor Yellow
    Write-Host "   1. Open a new PowerShell window (to load new PATH)" -ForegroundColor White
    Write-Host "   2. cd `"$repoPath\LearningCourse`"" -ForegroundColor White
    Write-Host "   3. Read the STUDENT-GUIDE.md" -ForegroundColor White
    Write-Host "   4. Start with Day 1: cd Day01-Flink21-Fundamentals\Exercise-Solutions" -ForegroundColor White
    Write-Host ""
    Write-Host "💡 To start the LocalTesting infrastructure:" -ForegroundColor Yellow
    Write-Host "   cd `"$repoPath\LocalTesting`"" -ForegroundColor White
    Write-Host "   dotnet run --project LocalTesting.AppHost" -ForegroundColor White
    Write-Host ""
    Write-Host "📚 Learning Course Path:" -ForegroundColor Yellow
    Write-Host "   • Follow STUDENT-GUIDE.md for complete 14-day course" -ForegroundColor White
    Write-Host "   • Each day has Exercise-Solutions\README.md with step-by-step instructions" -ForegroundColor White
    Write-Host "   • All exercises are now compatible with .NET 9.0" -ForegroundColor White
    Write-Host ""
    Write-Host "🪟 Windows Notes:" -ForegroundColor Yellow
    Write-Host "   • Make sure Docker Desktop is running before starting exercises" -ForegroundColor White
    Write-Host "   • You may need to restart PowerShell for PATH changes" -ForegroundColor White
    Write-Host "   • If you see permissions errors, try running as Administrator" -ForegroundColor White
    Write-Host ""
    Write-Host "✅ Environment setup complete! Happy learning! 🎓" -ForegroundColor Green
}

# Run main setup
try {
    Start-Setup
} catch {
    Write-Host ""
    Write-Host "❌ Setup failed with error: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "💡 Troubleshooting tips:" -ForegroundColor Yellow
    Write-Host "   • Try running as Administrator" -ForegroundColor White
    Write-Host "   • Check your internet connection" -ForegroundColor White
    Write-Host "   • Restart PowerShell and try again" -ForegroundColor White
    Write-Host "   • Run with -Force to reinstall components" -ForegroundColor White
    Write-Host ""
    exit 1
}