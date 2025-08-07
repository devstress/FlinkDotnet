#!/usr/bin/env pwsh
#requires -version 7.0

<#
.SYNOPSIS
    Cross-platform build script for Flink.NET repository
    
.DESCRIPTION
    This PowerShell script builds all solutions in the Flink.NET repository.
    It works on both Windows and Linux using PowerShell Core 7.0+.
    
    The script:
    - Detects the operating system and adjusts accordingly
    - Sets up .NET 9.0 and Java 17 requirements
    - Installs required .NET Aspire workload
    - Restores all workloads and dependencies
    - Builds all main solutions:
      * FlinkDotNet/FlinkDotNet.sln
      * Sample/Sample.sln
    - Provides comprehensive error reporting and cleanup
    
.PARAMETER Configuration
    Build configuration (Debug or Release). Default: Release
    
.PARAMETER SkipRestore
    Skip NuGet package and workload restore. Default: false
    
.PARAMETER Verbose
    Enable verbose output for troubleshooting. Default: false
    
.PARAMETER OutputPath
    Custom output path for build artifacts. Default: standard bin folders
    
.EXAMPLE
    ./build-all.ps1
    Build all solutions with Release configuration
    
.EXAMPLE
    ./build-all.ps1 -Configuration Debug -VerboseOutput
    Build all solutions with Debug configuration and verbose output
    
.EXAMPLE
    ./build-all.ps1 -SkipRestore
    Build without restoring packages (useful for incremental builds)
    
.NOTES
    Requirements:
    - PowerShell Core 7.0 or higher
    - .NET 9.0 SDK
    - Java 17 JDK (for Flink components)
    - Git (for version information)
    
    Cross-platform compatibility:
    - Windows (PowerShell Core or Windows PowerShell 5.1+)
    - Linux (PowerShell Core)
    - macOS (PowerShell Core)
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    
    [Parameter(Mandatory = $false)]
    [switch]$SkipRestore,
    
    [Parameter(Mandatory = $false)]
    [switch]$VerboseOutput,
    
    [Parameter(Mandatory = $false)]
    [string]$OutputPath,
    
    [Parameter(Mandatory = $false)]
    [switch]$Help
)

# Enable strict mode for better error handling
Set-StrictMode -Version 3.0
$ErrorActionPreference = "Stop"

# Global variables
$script:StartTime = Get-Date
$script:BuildSuccessful = $true
$script:BuildErrors = @()
$script:IsWindowsPlatform = $PSVersionTable.Platform -eq "Win32NT" -or $env:OS -eq "Windows_NT"
$script:IsLinuxPlatform = $PSVersionTable.Platform -eq "Unix" -and (Test-Path "/proc/version" -ErrorAction SilentlyContinue) -and (Get-Content /proc/version -ErrorAction SilentlyContinue) -match "Linux"
$script:IsMacOSPlatform = $PSVersionTable.Platform -eq "Unix" -and -not $script:IsLinuxPlatform

# ANSI color codes for cross-platform colored output
$script:Colors = @{
    Red = "`e[31m"
    Green = "`e[32m"
    Yellow = "`e[33m"
    Blue = "`e[34m"
    Magenta = "`e[35m"
    Cyan = "`e[36m"
    White = "`e[37m"
    Reset = "`e[0m"
}

# Main solutions to build
$script:Solutions = @(
    @{
        Name = "FlinkDotNet Core"
        Path = "FlinkDotNet/FlinkDotNet.sln"
        Description = "Core Flink.NET libraries and job gateway"
    },
    @{
        Name = "Sample Applications"
        Path = "Sample/Sample.sln"
        Description = "Sample applications and Aspire orchestration"
    }
)

#region Utility Functions

function Write-ColoredOutput {
    param(
        [string]$Message,
        [string]$Color = "White",
        [switch]$NoNewline
    )
    
    if ($script:Colors.ContainsKey($Color)) {
        $colorCode = $script:Colors[$Color]
        $resetCode = $script:Colors["Reset"]
        
        if ($NoNewline) {
            Write-Host "$colorCode$Message$resetCode" -NoNewline
        } else {
            Write-Host "$colorCode$Message$resetCode"
        }
    } else {
        if ($NoNewline) {
            Write-Host $Message -NoNewline
        } else {
            Write-Host $Message
        }
    }
}

function Write-Header {
    param([string]$Title)
    
    Write-ColoredOutput "`n===================================================" "Cyan"
    Write-ColoredOutput "🔨 $Title" "Cyan"
    Write-ColoredOutput "===================================================" "Cyan"
}

function Write-Success {
    param([string]$Message)
    Write-ColoredOutput "✅ $Message" "Green"
}

function Write-Warning {
    param([string]$Message)
    Write-ColoredOutput "⚠️  $Message" "Yellow"
}

function Write-Error {
    param([string]$Message)
    Write-ColoredOutput "❌ $Message" "Red"
}

function Write-Info {
    param([string]$Message)
    Write-ColoredOutput "ℹ️  $Message" "Blue"
}

function Write-Progress {
    param([string]$Activity, [string]$Status)
    Write-ColoredOutput "🔄 $Activity" "Magenta"
    if ($Status) {
        Write-ColoredOutput "   $Status" "White"
    }
}

function Get-PlatformInfo {
    $platform = "Unknown"
    $architecture = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture
    
    if ($script:IsWindowsPlatform) {
        $platform = "Windows"
    } elseif ($script:IsLinuxPlatform) {
        $platform = "Linux"
    } elseif ($script:IsMacOSPlatform) {
        $platform = "macOS"
    }
    
    return @{
        Platform = $platform
        Architecture = $architecture
        PowerShellVersion = $PSVersionTable.PSVersion
        IsElevated = ([System.Security.Principal.WindowsPrincipal][System.Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator) -and $script:IsWindowsPlatform
    }
}

function Test-CommandExists {
    param([string]$Command)
    
    try {
        $null = Get-Command $Command -ErrorAction Stop
        return $true
    } catch {
        return $false
    }
}

function Invoke-BuildCommand {
    param(
        [string]$Command,
        [string]$Arguments,
        [string]$WorkingDirectory = $PWD,
        [string]$Description
    )
    
    if ($Description) {
        Write-Progress $Description
    }
    
    $startLocation = Get-Location
    try {
        Set-Location $WorkingDirectory
        
        if ($VerboseOutput) {
            Write-ColoredOutput "   Command: $Command $Arguments" "Cyan"
            Write-ColoredOutput "   Working Directory: $WorkingDirectory" "Cyan"
        }
        
        $processInfo = @{
            FileName = $Command
            Arguments = $Arguments
            UseShellExecute = $false
            RedirectStandardOutput = $true
            RedirectStandardError = $true
            CreateNoWindow = $true
        }
        
        $process = New-Object System.Diagnostics.Process
        $process.StartInfo = New-Object System.Diagnostics.ProcessStartInfo $processInfo
        
        $outputBuilder = New-Object System.Text.StringBuilder
        $errorBuilder = New-Object System.Text.StringBuilder
        
        $outputEvent = Register-ObjectEvent -InputObject $process -EventName OutputDataReceived -Action {
            if ($Event.SourceEventArgs.Data) {
                $outputBuilder.AppendLine($Event.SourceEventArgs.Data) | Out-Null
                if ($using:VerboseOutput) {
                    Write-Host $Event.SourceEventArgs.Data
                }
            }
        }
        
        $errorEvent = Register-ObjectEvent -InputObject $process -EventName ErrorDataReceived -Action {
            if ($Event.SourceEventArgs.Data) {
                $errorBuilder.AppendLine($Event.SourceEventArgs.Data) | Out-Null
                Write-ColoredOutput $Event.SourceEventArgs.Data "Red"
            }
        }
        
        $process.Start() | Out-Null
        $process.BeginOutputReadLine()
        $process.BeginErrorReadLine()
        $process.WaitForExit()
        
        $output = $outputBuilder.ToString()
        $errorOutput = $errorBuilder.ToString()
        $exitCode = $process.ExitCode
        
        # Cleanup events
        Unregister-Event -SourceIdentifier $outputEvent.Name
        Unregister-Event -SourceIdentifier $errorEvent.Name
        $process.Dispose()
        
        if ($exitCode -ne 0) {
            $errorMessage = "Command failed with exit code $exitCode"
            if ($errorOutput) {
                $errorMessage += ": $errorOutput"
            }
            throw $errorMessage
        }
        
        return @{
            ExitCode = $exitCode
            Output = $output
            Error = $errorOutput
            Success = $exitCode -eq 0
        }
    } catch {
        throw "Failed to execute command '$Command $Arguments': $($_.Exception.Message)"
    } finally {
        Set-Location $startLocation
    }
}

function Test-Prerequisites {
    Write-Header "Checking Prerequisites"
    
    $platformInfo = Get-PlatformInfo
    Write-Info "Platform: $($platformInfo.Platform) $($platformInfo.Architecture)"
    Write-Info "PowerShell: $($platformInfo.PowerShellVersion)"
    
    # Check .NET SDK
    Write-Progress "Checking .NET SDK"
    if (-not (Test-CommandExists "dotnet")) {
        throw ".NET SDK not found. Please install .NET 9.0 SDK from https://dotnet.microsoft.com/download"
    }
    
    $dotnetInfo = dotnet --info | Out-String
    if ($dotnetInfo -match "8\.0\.\d+") {
        Write-Success ".NET 9.0 SDK found"
    } else {
        Write-Warning ".NET 9.0 SDK not detected. Some features may not work correctly."
        Write-Info "Installed .NET versions:"
        dotnet --list-sdks | ForEach-Object { Write-Info "  $_" }
    }
    
    # Check Java (required for Flink components)
    Write-Progress "Checking Java JDK"
    if (Test-CommandExists "java") {
        try {
            $javaVersion = java -version 2>&1 | Select-Object -First 1
            if ($javaVersion -match "17\." -or $javaVersion -match "1\.8\.") {
                Write-Success "Java JDK found: $javaVersion"
            } else {
                Write-Warning "Java 17 or 8 recommended for Flink compatibility. Found: $javaVersion"
            }
        } catch {
            Write-Warning "Java found but version check failed"
        }
    } else {
        Write-Warning "Java JDK not found. Java 17 is recommended for Apache Flink components."
    }
    
    # Check Git (for version info)
    Write-Progress "Checking Git"
    if (Test-CommandExists "git") {
        $gitVersion = git --version
        Write-Success "Git found: $gitVersion"
    } else {
        Write-Warning "Git not found. Version information will be limited."
    }
    
    Write-Success "Prerequisites check completed"
}

function Install-AspireWorkload {
    Write-Header "Installing .NET Aspire Workload"
    
    try {
        Write-Progress "Installing Aspire workload" "This may take a few minutes..."
        Invoke-BuildCommand "dotnet" "workload install aspire" -Description "Installing .NET Aspire workload"
        Write-Success "Aspire workload installed successfully"
    } catch {
        Write-Error "Failed to install Aspire workload: $($_.Exception.Message)"
        $script:BuildErrors += "Aspire workload installation failed"
        $script:BuildSuccessful = $false
    }
}

function Restore-Workloads {
    Write-Header "Restoring .NET Workloads"
    
    foreach ($solution in $script:Solutions) {
        if (Test-Path $solution.Path) {
            try {
                Write-Progress "Restoring workloads for $($solution.Name)"
                Invoke-BuildCommand "dotnet" "workload restore `"$($solution.Path)`"" -Description "Restoring workloads for $($solution.Name)"
                Write-Success "Workloads restored for $($solution.Name)"
            } catch {
                Write-Error "Failed to restore workloads for $($solution.Name): $($_.Exception.Message)"
                $script:BuildErrors += "Workload restore failed for $($solution.Name)"
            }
        } else {
            Write-Warning "Solution not found: $($solution.Path)"
        }
    }
}

function Restore-Packages {
    Write-Header "Restoring NuGet Packages"
    
    foreach ($solution in $script:Solutions) {
        if (Test-Path $solution.Path) {
            try {
                Write-Progress "Restoring packages for $($solution.Name)"
                Invoke-BuildCommand "dotnet" "restore `"$($solution.Path)`"" -Description "Restoring packages for $($solution.Name)"
                Write-Success "Packages restored for $($solution.Name)"
            } catch {
                Write-Error "Failed to restore packages for $($solution.Name): $($_.Exception.Message)"
                $script:BuildErrors += "Package restore failed for $($solution.Name)"
            }
        }
    }
}

function Build-Solutions {
    Write-Header "Building Solutions"
    
    foreach ($solution in $script:Solutions) {
        if (Test-Path $solution.Path) {
            try {
                Write-Progress "Building $($solution.Name)" $solution.Description
                
                $buildArgs = "build `"$($solution.Path)`" --configuration $Configuration --no-restore"
                if ($OutputPath) {
                    $buildArgs += " --output `"$OutputPath`""
                }
                
                Invoke-BuildCommand "dotnet" $buildArgs -Description "Building $($solution.Name)"
                Write-Success "$($solution.Name) built successfully"
            } catch {
                Write-Error "Failed to build $($solution.Name): $($_.Exception.Message)"
                $script:BuildErrors += "Build failed for $($solution.Name)"
                $script:BuildSuccessful = $false
            }
        } else {
            Write-Warning "Solution not found: $($solution.Path)"
            $script:BuildErrors += "Solution not found: $($solution.Path)"
        }
    }
}

function Show-BuildSummary {
    Write-Header "Build Summary"
    
    $endTime = Get-Date
    $duration = $endTime - $script:StartTime
    
    Write-Info "Build started: $($script:StartTime.ToString('yyyy-MM-dd HH:mm:ss'))"
    Write-Info "Build completed: $($endTime.ToString('yyyy-MM-dd HH:mm:ss'))"
    Write-Info "Total duration: $($duration.ToString('mm\:ss'))"
    Write-Info "Configuration: $Configuration"
    
    if ($script:BuildSuccessful -and $script:BuildErrors.Count -eq 0) {
        Write-Success "🎉 All solutions built successfully!"
        Write-Info "Build artifacts are available in the respective bin/$Configuration folders"
        
        if ($OutputPath) {
            Write-Info "Custom output path: $OutputPath"
        }
    } else {
        Write-Error "❌ Build completed with errors:"
        foreach ($error in $script:BuildErrors) {
            Write-Error "  • $error"
        }
        Write-Info ""
        Write-Info "To troubleshoot:"
        Write-Info "  1. Run with -VerboseOutput flag for detailed output"
        Write-Info "  2. Check that all prerequisites are installed"
        Write-Info "  3. Ensure all solutions are compatible with .NET 9.0"
        Write-Info "  4. Try cleaning and rebuilding: dotnet clean && ./build-all.ps1"
    }
    
    # Show next steps
    Write-Info ""
    Write-Info "Next steps:"
    Write-Info "  • Run tests: dotnet test"
    Write-Info "  • Start Aspire AppHost: cd Sample/FlinkDotNetAspire.AppHost.AppHost && dotnet run"
    Write-Info "  • View documentation: ./docs/wiki/Getting-Started.md"
}

function Show-Help {
    Write-Header "Flink.NET Cross-Platform Build Script"
    
    Write-Info "This script builds all Flink.NET solutions on Windows, Linux, and macOS."
    Write-Info ""
    Write-Info "Usage:"
    Write-Info "  ./build-all.ps1 [options]"
    Write-Info ""
    Write-Info "Options:"
    Write-Info "  -Configuration <Debug|Release>  Build configuration (default: Release)"
    Write-Info "  -SkipRestore                    Skip package and workload restore"
    Write-Info "  -VerboseOutput                        Enable detailed output"
    Write-Info "  -OutputPath <path>              Custom output directory"
    Write-Info "  -Help                           Show this help message"
    Write-Info ""
    Write-Info "Examples:"
    Write-Info "  ./build-all.ps1                           # Build with Release configuration"
    Write-Info "  ./build-all.ps1 -Configuration Debug     # Build with Debug configuration"
    Write-Info "  ./build-all.ps1 -SkipRestore -VerboseOutput    # Skip restore, show verbose output"
    Write-Info ""
    Write-Info "Requirements:"
    Write-Info "  • PowerShell Core 7.0+"
    Write-Info "  • .NET 9.0 SDK"
    Write-Info "  • Java 17 JDK (for Flink components)"
}

#endregion

#region Main Execution

function Main {
    try {
        # Handle help parameter
        if ($Help) {
            Show-Help
            return 0
        }
        
        Write-Header "Flink.NET Cross-Platform Build Script"
        Write-Info "Building Flink.NET repository with configuration: $Configuration"
        Write-Info "Platform: $(if($script:IsWindowsPlatform){'Windows'}elseif($script:IsLinuxPlatform){'Linux'}elseif($script:IsMacOSPlatform){'macOS'}else{'Unknown'})"
        
        # Verify we're in the correct directory
        if (-not (Test-Path "FlinkDotNet") -or -not (Test-Path "Sample")) {
            throw "Please run this script from the root of the Flink.NET repository"
        }
        
        # Step 1: Check prerequisites
        Test-Prerequisites
        
        # Step 2: Install Aspire workload (if not skipping restore)
        if (-not $SkipRestore) {
            Install-AspireWorkload
        }
        
        # Step 3: Restore workloads and packages (if not skipping restore)
        if (-not $SkipRestore) {
            Restore-Workloads
            Restore-Packages
        } else {
            Write-Warning "Skipping restore operations as requested"
        }
        
        # Step 4: Build all solutions
        Build-Solutions
        
        # Step 5: Show summary
        Show-BuildSummary
        
        # Return appropriate exit code
        if ($script:BuildSuccessful -and $script:BuildErrors.Count -eq 0) {
            return 0
        } else {
            return 1
        }
        
    } catch {
        Write-Error "Build script failed: $($_.Exception.Message)"
        Write-Error "Stack trace: $($_.ScriptStackTrace)"
        return 1
    }
}

# Execute main function if script is run directly
if ($MyInvocation.InvocationName -eq $MyInvocation.MyCommand.Name) {
    $exitCode = Main @args
    exit $exitCode
}

#endregion