#!/usr/bin/env pwsh
# FlinkDotNet .NET 9 Environment Enforcement Script
# This script ensures local development environment meets .NET 9 requirements

param(
    [switch]$Install,
    [switch]$Verify,
    [switch]$All
)

# Colors for output
function Write-Success($message) { Write-Host "✅ $message" -ForegroundColor Green }
function Write-Warning($message) { Write-Host "⚠️ $message" -ForegroundColor Yellow }
function Write-Error($message) { Write-Host "❌ $message" -ForegroundColor Red }
function Write-Info($message) { Write-Host "ℹ️ $message" -ForegroundColor Cyan }
function Write-Step($message) { Write-Host "🔧 $message" -ForegroundColor Blue }

# Global variables
$RequiredDotNetVersion = "9.0.303"
$ExitCode = 0

function Test-DotNetVersion {
    Write-Step "Checking .NET version requirements..."
    
    try {
        $installedVersion = dotnet --version 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Info "Installed .NET version: $installedVersion"
            
            # Parse version numbers for comparison
            $installedMajor = [int]$installedVersion.Split('.')[0]
            $installedMinor = [int]$installedVersion.Split('.')[1]
            $installedPatch = [int]$installedVersion.Split('.')[2]
            
            $requiredMajor = [int]$RequiredDotNetVersion.Split('.')[0]
            $requiredMinor = [int]$RequiredDotNetVersion.Split('.')[1]
            $requiredPatch = [int]$RequiredDotNetVersion.Split('.')[2]
            
            if ($installedMajor -gt $requiredMajor -or 
                ($installedMajor -eq $requiredMajor -and $installedMinor -gt $requiredMinor) -or
                ($installedMajor -eq $requiredMajor -and $installedMinor -eq $requiredMinor -and $installedPatch -ge $requiredPatch)) {
                Write-Success ".NET $installedVersion meets requirement (>= $RequiredDotNetVersion)"
                return $true
            } else {
                Write-Error ".NET $installedVersion does not meet requirement (>= $RequiredDotNetVersion)"
                return $false
            }
        } else {
            Write-Error ".NET is not installed or not in PATH"
            return $false
        }
    } catch {
        Write-Error "Failed to check .NET version: $($_.Exception.Message)"
        return $false
    }
}

function Test-AspireWorkload {
    Write-Step "Checking .NET Aspire workload..."
    
    try {
        $workloads = dotnet workload list 2>$null
        if ($LASTEXITCODE -eq 0) {
            if ($workloads -match "aspire") {
                Write-Success "Aspire workload is installed"
                return $true
            } else {
                Write-Warning "Aspire workload is not installed"
                return $false
            }
        } else {
            Write-Error "Failed to check workloads"
            return $false
        }
    } catch {
        Write-Error "Failed to check Aspire workload: $($_.Exception.Message)"
        return $false
    }
}

function Install-DotNet9 {
    Write-Step "Installing .NET 9.0..."
    
    $os = $env:OS
    if ($IsLinux) {
        Write-Info "Detected Linux environment"
        
        # Download and run the install script
        try {
            Write-Info "Downloading .NET install script..."
            Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.sh" -OutFile "dotnet-install.sh"
            chmod +x dotnet-install.sh
            
            Write-Info "Installing .NET 9.0.303..."
            ./dotnet-install.sh --version $RequiredDotNetVersion --install-dir ~/.dotnet
            
            # Update PATH
            $dotnetPath = "$HOME/.dotnet"
            if ($env:PATH -notlike "*$dotnetPath*") {
                Write-Info "Adding .NET to PATH..."
                $env:PATH = "${dotnetPath}:$env:PATH"
                
                # Also add to shell profile
                $shellProfile = "$HOME/.bashrc"
                if (Test-Path $shellProfile) {
                    Add-Content $shellProfile "`nexport PATH=`$PATH:${dotnetPath}"
                }
            }
            
            Write-Success ".NET 9.0 installation completed"
            return $true
        } catch {
            Write-Error "Failed to install .NET 9.0: $($_.Exception.Message)"
            return $false
        } finally {
            # Clean up
            if (Test-Path "dotnet-install.sh") {
                Remove-Item "dotnet-install.sh" -Force
            }
        }
    } elseif ($IsWindows) {
        Write-Info "Detected Windows environment"
        Write-Warning "Please download and install .NET 9.0 SDK from: https://dotnet.microsoft.com/download/dotnet/9.0"
        Write-Info "Or use winget: winget install Microsoft.DotNet.SDK.9"
        return $false
    } elseif ($IsMacOS) {
        Write-Info "Detected macOS environment"
        Write-Warning "Please download and install .NET 9.0 SDK from: https://dotnet.microsoft.com/download/dotnet/9.0"
        Write-Info "Or use Homebrew: brew install --cask dotnet"
        return $false
    } else {
        Write-Error "Unsupported operating system"
        return $false
    }
}

function Install-AspireWorkload {
    Write-Step "Installing .NET Aspire workload..."
    
    try {
        Write-Info "Installing Aspire workload..."
        dotnet workload install aspire
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Aspire workload installed successfully"
            return $true
        } else {
            Write-Error "Failed to install Aspire workload"
            return $false
        }
    } catch {
        Write-Error "Failed to install Aspire workload: $($_.Exception.Message)"
        return $false
    }
}

function Test-SolutionBuilds {
    Write-Step "Testing solution builds..."
    
    $solutions = @(
        "FlinkDotNet/FlinkDotNet.sln",
        "Sample/Sample.sln",
        "LocalTesting/LocalTesting.sln"
    )
    
    $allBuildsSucceeded = $true
    
    foreach ($solution in $solutions) {
        if (Test-Path $solution) {
            Write-Info "Building $solution..."
            try {
                dotnet restore $solution --quiet
                dotnet build $solution --configuration Release --no-restore --quiet
                
                if ($LASTEXITCODE -eq 0) {
                    Write-Success "Successfully built $solution"
                } else {
                    Write-Error "Failed to build $solution"
                    $allBuildsSucceeded = $false
                }
            } catch {
                Write-Error "Exception building ${solution}: $($_.Exception.Message)"
                $allBuildsSucceeded = $false
            }
        } else {
            Write-Warning "Solution not found: $solution"
        }
    }
    
    return $allBuildsSucceeded
}

function Test-DockerAvailable {
    Write-Step "Checking Docker availability..."
    
    try {
        docker --version | Out-Null
        if ($LASTEXITCODE -eq 0) {
            docker info | Out-Null
            if ($LASTEXITCODE -eq 0) {
                Write-Success "Docker is installed and running"
                return $true
            } else {
                Write-Warning "Docker is installed but not running"
                return $false
            }
        } else {
            Write-Warning "Docker is not installed"
            return $false
        }
    } catch {
        Write-Warning "Docker check failed: $($_.Exception.Message)"
        return $false
    }
}

function Show-EnvironmentSummary {
    Write-Host "`n" -NoNewline
    Write-Host "=" * 60 -ForegroundColor Cyan
    Write-Host "FlinkDotNet Environment Summary" -ForegroundColor Cyan
    Write-Host "=" * 60 -ForegroundColor Cyan
    
    # .NET Version
    $dotnetOk = Test-DotNetVersion
    Write-Host ".NET 9.0+ .............. " -NoNewline
    if ($dotnetOk) { Write-Host "READY" -ForegroundColor Green } else { Write-Host "MISSING" -ForegroundColor Red }
    
    # Aspire Workload
    $aspireOk = Test-AspireWorkload
    Write-Host "Aspire Workload ........ " -NoNewline
    if ($aspireOk) { Write-Host "READY" -ForegroundColor Green } else { Write-Host "MISSING" -ForegroundColor Red }
    
    # Docker
    $dockerOk = Test-DockerAvailable
    Write-Host "Docker ................. " -NoNewline
    if ($dockerOk) { Write-Host "READY" -ForegroundColor Green } else { Write-Host "WARNING" -ForegroundColor Yellow }
    
    # Solution Builds
    if ($dotnetOk -and $aspireOk) {
        $buildsOk = Test-SolutionBuilds
        Write-Host "Solution Builds ........ " -NoNewline
        if ($buildsOk) { Write-Host "READY" -ForegroundColor Green } else { Write-Host "FAILED" -ForegroundColor Red }
    } else {
        Write-Host "Solution Builds ........ " -NoNewline
        Write-Host "SKIPPED" -ForegroundColor Yellow
        $buildsOk = $false
    }
    
    Write-Host "=" * 60 -ForegroundColor Cyan
    
    # Overall Status
    if ($dotnetOk -and $aspireOk -and $buildsOk) {
        Write-Success "Environment is ready for FlinkDotNet development!"
        return $true
    } else {
        Write-Error "Environment setup is incomplete"
        
        if (-not $dotnetOk) {
            Write-Info "Run: ./verify-dotnet9-environment.ps1 -Install"
        }
        if (-not $aspireOk) {
            Write-Info "Run: dotnet workload install aspire"
        }
        if (-not $dockerOk) {
            Write-Info "Install and start Docker Desktop"
        }
        if (-not $buildsOk) {
            Write-Info "Fix build errors and run verification again"
        }
        
        return $false
    }
}

# Main execution
function Main {
    Write-Host "FlinkDotNet .NET 9 Environment Enforcement" -ForegroundColor Magenta
    Write-Host "Required Version: .NET $RequiredDotNetVersion" -ForegroundColor Magenta
    Write-Host ""
    
    if ($Install -or $All) {
        Write-Step "Installing missing components..."
        
        if (-not (Test-DotNetVersion)) {
            Install-DotNet9
        }
        
        if (-not (Test-AspireWorkload)) {
            Install-AspireWorkload
        }
    }
    
    if ($Verify -or $All -or (-not $Install)) {
        $environmentReady = Show-EnvironmentSummary
        
        if (-not $environmentReady) {
            $script:ExitCode = 1
        }
    }
    
    Write-Host ""
    exit $script:ExitCode
}

# Show help if no parameters
if (-not ($Install -or $Verify -or $All)) {
    Write-Host "FlinkDotNet .NET 9 Environment Enforcement Script" -ForegroundColor Magenta
    Write-Host ""
    Write-Host "Usage:" -ForegroundColor Cyan
    Write-Host "  ./verify-dotnet9-environment.ps1 -Verify    # Check environment only"
    Write-Host "  ./verify-dotnet9-environment.ps1 -Install   # Install missing components"
    Write-Host "  ./verify-dotnet9-environment.ps1 -All       # Install and verify"
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Cyan
    Write-Host "  # Check if environment is ready"
    Write-Host "  ./verify-dotnet9-environment.ps1 -Verify"
    Write-Host ""
    Write-Host "  # Install and verify everything"
    Write-Host "  ./verify-dotnet9-environment.ps1 -All"
    Write-Host ""
    
    # Run verification by default
    $Verify = $true
}

# Run main function
Main