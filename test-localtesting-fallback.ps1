#!/usr/bin/env pwsh
# Fallback LocalTesting Script - Works around Aspire IPv6 issues
# This script runs the LocalTesting API directly without Aspire orchestration

param(
    [switch]$StopOnly,
    [int]$TimeoutMinutes = 5
)

# Colors for output
$Green = "Green"
$Red = "Red"
$Yellow = "Yellow"
$Cyan = "Cyan"

function Write-Section {
    param([string]$Title, [string]$Color = $Green)
    Write-Host "`n$('=' * 70)" -ForegroundColor $Color
    Write-Host $Title -ForegroundColor $Color
    Write-Host "$('=' * 70)" -ForegroundColor $Color
}

function Write-Step {
    param([string]$Message, [string]$Color = $Yellow)
    Write-Host "`n🔧 $Message" -ForegroundColor $Color
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor $Green
}

function Write-Error {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor $Red
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠️ $Message" -ForegroundColor $Yellow
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ️ $Message" -ForegroundColor $Cyan
}

function Stop-LocalTestingAPI {
    Write-Section "🧹 Stopping LocalTesting API"
    
    try {
        # Stop any dotnet processes running LocalTesting
        $apiProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -like "*LocalTesting.WebApi*" }
        if ($apiProcesses) {
            $apiProcesses | Stop-Process -Force -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 3
            Write-Success "Stopped LocalTesting API processes"
        } else {
            Write-Info "No LocalTesting API processes to stop"
        }
    } catch {
        Write-Error "Error during cleanup: $($_.Exception.Message)"
    }
}

function Test-Prerequisites {
    Write-Section "📋 Testing Prerequisites"
    
    # Test .NET 8
    Write-Step "Testing .NET 8..."
    try {
        $dotnetVersion = dotnet --version 2>&1
        if ($LASTEXITCODE -eq 0 -and $dotnetVersion -like "8.*") {
            Write-Success ".NET 8 is available: $dotnetVersion"
        } else {
            Write-Error ".NET 8 is not available. Found: $dotnetVersion"
            return $false
        }
    } catch {
        Write-Error ".NET test failed: $($_.Exception.Message)"
        return $false
    }
    
    return $true
}

function Start-LocalTestingAPI {
    Write-Section "🚀 Starting LocalTesting API (Standalone Mode)"
    
    $originalLocation = Get-Location
    
    try {
        # Navigate to WebApi directory
        Write-Step "Navigating to LocalTesting WebApi directory..."
        $webApiPath = "LocalTesting/LocalTesting.WebApi"
        if (Test-Path $webApiPath) {
            Set-Location $webApiPath
            Write-Success "Changed to: $(Get-Location)"
        } else {
            Write-Error "LocalTesting WebApi directory not found: $webApiPath"
            return $false
        }
        
        # Build the project first
        Write-Step "Building LocalTesting WebApi project..."
        $buildOutput = dotnet build --configuration Release 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Build completed successfully"
        } else {
            Write-Error "Build failed:"
            Write-Host $buildOutput -ForegroundColor $Red
            return $false
        }
        
        # Set up environment variables for standalone mode
        Write-Step "Setting up environment variables for standalone mode..."
        $env:ASPNETCORE_URLS = "http://127.0.0.1:5000"
        $env:ASPNETCORE_ENVIRONMENT = "Development"
        $env:DOTNET_SYSTEM_NET_DISABLEIPV6 = "true"
        
        Write-Info "Environment variables configured:"
        Write-Host "  ASPNETCORE_URLS: $env:ASPNETCORE_URLS" -ForegroundColor $Cyan
        Write-Host "  IPv4 Only: DOTNET_SYSTEM_NET_DISABLEIPV6=$env:DOTNET_SYSTEM_NET_DISABLEIPV6" -ForegroundColor $Cyan
        
        # Start API as background process
        Write-Step "Starting LocalTesting API..."
        $apiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run", "--configuration", "Release" -PassThru -RedirectStandardOutput "api_output.log" -RedirectStandardError "api_error.log" -NoNewWindow
        $global:ApiPID = $apiProcess.Id
        Write-Success "LocalTesting API started with PID: $global:ApiPID"
        
        # Wait for startup
        Write-Step "Waiting for API to initialize (30 seconds)..."
        Start-Sleep -Seconds 30
        
        return $true
        
    } catch {
        Write-Error "Failed to start LocalTesting API: $($_.Exception.Message)"
        return $false
    } finally {
        Set-Location $originalLocation
    }
}

function Test-API {
    Write-Section "🌐 Testing LocalTesting API"
    
    # Test API accessibility
    Write-Step "Testing LocalTesting API accessibility..."
    $maxRetries = 10
    $retryCount = 0
    $apiReady = $false
    
    while ($retryCount -lt $maxRetries -and -not $apiReady) {
        try {
            $response = Invoke-WebRequest -Uri "http://127.0.0.1:5000/" -TimeoutSec 5 -ErrorAction Stop
            if ($response.StatusCode -eq 200) {
                Write-Success "LocalTesting API is accessible and healthy"
                $apiReady = $true
            } else {
                Write-Warning "API returned status: $($response.StatusCode)"
            }
        } catch {
            $retryCount++
            Write-Warning "API not ready yet (attempt $retryCount/$maxRetries): $($_.Exception.Message)"
            Start-Sleep -Seconds 3
        }
    }
    
    if ($apiReady) {
        Write-Success "API is functional and ready for testing"
        
        # Show available endpoints
        Write-Info "Available endpoints:"
        Write-Host "  - Swagger UI: http://127.0.0.1:5000" -ForegroundColor $Cyan
        Write-Host "  - API Base: http://127.0.0.1:5000/api" -ForegroundColor $Cyan
        
        return $true
    } else {
        Write-Error "API is not accessible after $maxRetries attempts"
        return $false
    }
}

# Main execution
Write-Section "🧪 LocalTesting Fallback Script (Standalone Mode)" $Cyan
Write-Info "This script runs LocalTesting API directly, bypassing Aspire IPv6 issues"

if ($StopOnly) {
    Stop-LocalTestingAPI
    exit 0
}

try {
    # Test prerequisites
    if (-not (Test-Prerequisites)) {
        Write-Error "Prerequisites check failed"
        exit 1
    }
    
    # Clean up any existing processes
    Stop-LocalTestingAPI
    Start-Sleep -Seconds 3
    
    # Start API
    if (-not (Start-LocalTestingAPI)) {
        Write-Error "Failed to start LocalTesting API"
        exit 1
    }
    
    # Test API
    if (Test-API) {
        Write-Success "API testing successful"
    } else {
        Write-Error "API tests failed"
        exit 1
    }
    
    Write-Section "🎉 LocalTesting Fallback Completed Successfully" $Green
    Write-Host "LocalTesting API is running in standalone mode. Available endpoints:" -ForegroundColor $Yellow
    Write-Host "  - API Documentation: http://127.0.0.1:5000" -ForegroundColor $Cyan
    Write-Host "  - API Base URL: http://127.0.0.1:5000/api" -ForegroundColor $Cyan
    Write-Host "`nPress Ctrl+C to stop or run with -StopOnly to clean up." -ForegroundColor $Yellow
    
    # Keep running for manual testing
    Write-Host "Keeping API running for manual testing..." -ForegroundColor $Cyan
    
    # Wait for timeout or user interrupt
    try {
        $timeoutSeconds = $TimeoutMinutes * 60
        $elapsed = 0
        while ($elapsed -lt $timeoutSeconds) {
            Start-Sleep -Seconds 10
            $elapsed += 10
            Write-Host "." -NoNewline -ForegroundColor $Green
        }
        Write-Host "`nTimeout reached ($TimeoutMinutes minutes)" -ForegroundColor $Yellow
    } catch {
        Write-Host "`nReceived interrupt signal" -ForegroundColor $Yellow
    }
    
} catch {
    Write-Error "Script execution failed: $($_.Exception.Message)"
    exit 1
} finally {
    Write-Section "🧹 Final Cleanup"
    Stop-LocalTestingAPI
}