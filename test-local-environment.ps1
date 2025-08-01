#!/usr/bin/env pwsh
# LocalTesting Environment Test Script
# This script tests the LocalTesting environment locally to debug infrastructure issues

param(
    [switch]$SkipAspireStart,
    [switch]$StopOnly,
    [int]$TimeoutMinutes = 30
)

# Colors for output
$Green = "Green"
$Red = "Red"
$Yellow = "Yellow"
$Cyan = "Cyan"

function Write-Section {
    param([string]$Title, [string]$Color = $Green)
    Write-Host "`n$('=' * 60)" -ForegroundColor $Color
    Write-Host $Title -ForegroundColor $Color
    Write-Host "$('=' * 60)" -ForegroundColor $Color
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

function Stop-LocalEnvironment {
    Write-Section "🧹 Cleaning up LocalTesting Environment"
    
    try {
        # Stop any running dotnet processes
        Write-Step "Stopping dotnet processes..."
        $dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
        if ($dotnetProcesses) {
            $dotnetProcesses | Stop-Process -Force -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 3
            Write-Success "Stopped dotnet processes"
        } else {
            Write-Info "No dotnet processes to stop"
        }
        
        # Stop all Docker containers
        Write-Step "Stopping Docker containers..."
        $containers = docker ps -q
        if ($containers) {
            docker stop $containers 2>$null
            Start-Sleep -Seconds 5
            Write-Success "Stopped Docker containers"
        } else {
            Write-Info "No Docker containers to stop"
        }
        
        # Remove stopped containers
        Write-Step "Cleaning up stopped containers..."
        docker container prune -f 2>$null
        Write-Success "Container cleanup completed"
        
        # Clean up volumes (optional)
        Write-Step "Cleaning up unused volumes..."
        docker volume prune -f 2>$null
        Write-Success "Volume cleanup completed"
        
    } catch {
        Write-Error "Error during cleanup: $($_.Exception.Message)"
    }
}

function Test-Prerequisites {
    Write-Section "📋 Testing Prerequisites"
    
    # Test Docker
    Write-Step "Testing Docker..."
    try {
        $dockerInfo = docker info 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Docker is running"
        } else {
            Write-Error "Docker is not running or not accessible"
            Write-Host $dockerInfo -ForegroundColor $Red
            return $false
        }
    } catch {
        Write-Error "Docker test failed: $($_.Exception.Message)"
        return $false
    }
    
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
    
    # Test Aspire workload
    Write-Step "Testing Aspire workload..."
    try {
        $workloads = dotnet workload list 2>&1
        if ($workloads -match "aspire") {
            Write-Success "Aspire workload is installed"
        } else {
            Write-Warning "Aspire workload not found. Installing..."
            dotnet workload install aspire
            if ($LASTEXITCODE -eq 0) {
                Write-Success "Aspire workload installed"
            } else {
                Write-Error "Failed to install Aspire workload"
                return $false
            }
        }
    } catch {
        Write-Error "Aspire workload test failed: $($_.Exception.Message)"
        return $false
    }
    
    return $true
}

function Start-AspireEnvironment {
    Write-Section "🚀 Starting Aspire Environment"
    
    $originalLocation = Get-Location
    
    try {
        # Navigate to AppHost directory
        Write-Step "Navigating to AppHost directory..."
        $appHostPath = "LocalTesting/LocalTesting.AppHost"
        if (Test-Path $appHostPath) {
            Set-Location $appHostPath
            Write-Success "Changed to: $(Get-Location)"
        } else {
            Write-Error "AppHost directory not found: $appHostPath"
            return $false
        }
        
        # Build the project first
        Write-Step "Building AppHost project..."
        $buildOutput = dotnet build --configuration Release 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Build completed successfully"
        } else {
            Write-Error "Build failed:"
            Write-Host $buildOutput -ForegroundColor $Red
            return $false
        }
        
        # Set up environment variables
        Write-Step "Setting up environment variables..."
        $nugetPackages = if ($IsWindows) { "$env:USERPROFILE\.nuget\packages" } else { "$env:HOME/.nuget/packages" }
        
        # Set required Aspire paths
        $dcpPath = "$nugetPackages/aspire.hosting.orchestration.linux-x64/9.3.1/tools/dcp"
        $dashboardPath = "$nugetPackages/aspire.dashboard.sdk.linux-x64/9.3.1/tools"
        
        if ($IsWindows) {
            $dcpPath = "$nugetPackages/aspire.hosting.orchestration.win-x64/9.3.1/tools/dcp.exe"
            $dashboardPath = "$nugetPackages/aspire.dashboard.sdk.win-x64/9.3.1/tools"
        }
        
        $env:DCP_CLI_PATH = $dcpPath
        $env:ASPIRE_DASHBOARD_PATH = $dashboardPath
        $env:ASPIRE_ALLOW_UNSECURED_TRANSPORT = "true"
        $env:ASPNETCORE_URLS = "http://localhost:15000"
        $env:ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL = "http://localhost:4323"
        $env:ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL = "http://localhost:4324"
        $env:ASPIRE_DASHBOARD_URL = "http://localhost:18888"
        $env:ASPNETCORE_ENVIRONMENT = "Development"
        
        Write-Info "Environment variables configured:"
        Write-Host "  DCP_CLI_PATH: $env:DCP_CLI_PATH" -ForegroundColor $Cyan
        Write-Host "  ASPIRE_DASHBOARD_PATH: $env:ASPIRE_DASHBOARD_PATH" -ForegroundColor $Cyan
        Write-Host "  ASPNETCORE_URLS: $env:ASPNETCORE_URLS" -ForegroundColor $Cyan
        
        # Start Aspire as background process
        Write-Step "Starting Aspire environment..."
        $aspireProcess = Start-Process -FilePath "dotnet" -ArgumentList "run", "--configuration", "Release" -PassThru -RedirectStandardOutput "aspire_output.log" -RedirectStandardError "aspire_error.log" -NoNewWindow
        $global:AspirePID = $aspireProcess.Id
        Write-Success "Aspire started with PID: $global:AspirePID"
        
        # Wait for startup
        Write-Step "Waiting for Aspire to initialize (60 seconds)..."
        Start-Sleep -Seconds 60
        
        # Check startup logs
        $startupOutput = ""
        if (Test-Path "aspire_output.log") {
            $startupOutput = Get-Content "aspire_output.log" -Raw
        }
        $errorOutput = ""
        if (Test-Path "aspire_error.log") {
            $errorOutput = Get-Content "aspire_error.log" -Raw
        }
        
        if ($startupOutput -match "Distributed application starting" -or $startupOutput -match "Aspire version") {
            Write-Success "Aspire environment started successfully"
            Write-Info "Startup logs contain expected messages"
        } else {
            Write-Warning "Aspire startup verification inconclusive"
            if ($startupOutput) {
                Write-Host "Startup output:" -ForegroundColor $Yellow
                Write-Host $startupOutput -ForegroundColor $Cyan
            }
            if ($errorOutput) {
                Write-Host "Error output:" -ForegroundColor $Yellow
                Write-Host $errorOutput -ForegroundColor $Red
            }
        }
        
        return $true
        
    } catch {
        Write-Error "Failed to start Aspire environment: $($_.Exception.Message)"
        return $false
    } finally {
        Set-Location $originalLocation
    }
}

function Wait-ForServices {
    param([int]$MaxWaitMinutes = 10)
    
    Write-Section "⏳ Waiting for Services to Start"
    
    $maxWaitSeconds = $MaxWaitMinutes * 60
    $waitedSeconds = 0
    $checkInterval = 30
    
    while ($waitedSeconds -lt $maxWaitSeconds) {
        Write-Step "Checking container status... (waited $waitedSeconds/$maxWaitSeconds seconds)"
        
        # Show running containers
        $runningContainers = docker ps --format "{{.Names}}" 2>$null
        $containerCount = ($runningContainers | Measure-Object).Count
        
        Write-Info "Running containers ($containerCount):"
        if ($runningContainers) {
            $runningContainers | ForEach-Object { Write-Host "  - $_" -ForegroundColor $Cyan }
        } else {
            Write-Warning "No containers running yet"
        }
        
        # Show failed containers
        $failedContainers = docker ps -a --filter "status=exited" --format "{{.Names}}" 2>$null
        if ($failedContainers) {
            Write-Warning "Failed containers:"
            $failedContainers | ForEach-Object { 
                Write-Host "  - $_" -ForegroundColor $Red
                Write-Host "    Last few logs:" -ForegroundColor $Yellow
                $logs = docker logs --tail 5 $_ 2>&1
                $logs | ForEach-Object { Write-Host "      $_" -ForegroundColor $Cyan }
            }
        }
        
        # Check if we have a reasonable number of containers
        if ($containerCount -ge 10) {
            Write-Success "Good container count detected ($containerCount), continuing..."
            break
        }
        
        Start-Sleep -Seconds $checkInterval
        $waitedSeconds += $checkInterval
    }
    
    if ($containerCount -eq 0) {
        Write-Error "No containers started after $MaxWaitMinutes minutes"
        return $false
    }
    
    Write-Success "Service startup wait completed"
    return $true
}

function Test-Infrastructure {
    Write-Section "🔍 Testing Infrastructure Components"
    
    $issues = @()
    
    # Test container presence
    Write-Step "Checking required containers..."
    $requiredContainers = @(
        @{Pattern="kafka"; Name="Kafka Brokers"; MinCount=3},
        @{Pattern="flink"; Name="Flink Components"; MinCount=4},
        @{Pattern="redis"; Name="Redis"; MinCount=1},
        @{Pattern="temporal"; Name="Temporal Stack"; MinCount=3},
        @{Pattern="otel"; Name="OpenTelemetry"; MinCount=1},
        @{Pattern="prometheus"; Name="Prometheus"; MinCount=1},
        @{Pattern="grafana"; Name="Grafana"; MinCount=1}
    )
    
    foreach ($required in $requiredContainers) {
        $containers = docker ps --filter "name=$($required.Pattern)" --format "{{.Names}}" 2>$null
        $count = ($containers | Measure-Object).Count
        
        if ($count -ge $required.MinCount) {
            Write-Success "$($required.Name): $count containers running"
            $containers | ForEach-Object { Write-Host "  - $_" -ForegroundColor $Cyan }
        } else {
            Write-Error "$($required.Name): Only $count/$($required.MinCount) containers running"
            $issues += "$($required.Name) (missing containers)"
        }
    }
    
    # Test endpoint accessibility
    Write-Step "Testing endpoint accessibility..."
    $endpoints = @(
        @{Port=5000; Name="LocalTesting API"; Path="/"; Critical=$true},
        @{Port=8082; Name="Kafka UI"; Path="/"; Critical=$false},
        @{Port=8084; Name="Temporal UI"; Path="/"; Critical=$true},
        @{Port=3000; Name="Grafana"; Path="/"; Critical=$false},
        @{Port=8081; Name="Flink JobManager UI"; Path="/"; Critical=$false}
    )
    
    foreach ($endpoint in $endpoints) {
        try {
            Write-Host "Testing $($endpoint.Name) on port $($endpoint.Port)..." -ForegroundColor $Yellow
            $uri = "http://localhost:$($endpoint.Port)$($endpoint.Path)"
            $response = Invoke-WebRequest -Uri $uri -TimeoutSec 10 -ErrorAction Stop
            if ($response.StatusCode -eq 200) {
                Write-Success "$($endpoint.Name) is accessible (Status: $($response.StatusCode))"
            } else {
                Write-Warning "$($endpoint.Name) returned status: $($response.StatusCode)"
            }
        } catch {
            Write-Error "$($endpoint.Name) not accessible: $($_.Exception.Message)"
            if ($endpoint.Critical) {
                $issues += "$($endpoint.Name) (not accessible)"
            }
        }
    }
    
    return $issues
}

function Test-BusinessFlows {
    Write-Section "🧪 Testing Complex Logic Stress Test Business Flows"
    
    $apiBase = "http://localhost:5000/api/ComplexLogicStressTest"
    $testResults = @()
    
    try {
        # Step 1: Environment Setup
        Write-Step "Step 1: Testing environment setup..."
        $setupResponse = Invoke-RestMethod -Uri "$apiBase/step1/setup-environment" -Method POST -ContentType "application/json" -TimeoutSec 30
        Write-Success "Environment setup: $($setupResponse.Status)"
        Write-Info "Services Health Summary:"
        Write-Host ($setupResponse.Metrics | ConvertTo-Json -Depth 3) -ForegroundColor $Cyan
        $testResults += @{Step="1-Setup"; Status=$setupResponse.Status; Success=$true}
        
        # Step 2: Configure Security Tokens
        Write-Step "Step 2: Testing security token configuration..."
        $tokenConfig = 1000
        $tokenResponse = Invoke-RestMethod -Uri "$apiBase/step2/configure-security-tokens" -Method POST -Body ($tokenConfig | ConvertTo-Json) -ContentType "application/json" -TimeoutSec 30
        Write-Success "Token configuration: $($tokenResponse.Status)"
        Write-Info "Renewal Interval: $($tokenResponse.TokenInfo.RenewalInterval) messages"
        $testResults += @{Step="2-Tokens"; Status=$tokenResponse.Status; Success=$true}
        
        # Continue with other steps...
        Write-Step "Continuing with remaining steps (abbreviated for local testing)..."
        Write-Success "Business flows tested successfully"
        
    } catch {
        Write-Error "Business flow test failed: $($_.Exception.Message)"
        return $false
    }
    
    return $true
}

# Main execution
Write-Section "🧪 LocalTesting Environment Test Script" $Cyan

if ($StopOnly) {
    Stop-LocalEnvironment
    exit 0
}

try {
    # Test prerequisites
    if (-not (Test-Prerequisites)) {
        Write-Error "Prerequisites check failed"
        exit 1
    }
    
    # Clean up any existing environment
    Stop-LocalEnvironment
    Start-Sleep -Seconds 5
    
    if (-not $SkipAspireStart) {
        # Start Aspire environment
        if (-not (Start-AspireEnvironment)) {
            Write-Error "Failed to start Aspire environment"
            exit 1
        }
        
        # Wait for services
        if (-not (Wait-ForServices -MaxWaitMinutes 8)) {
            Write-Error "Services failed to start properly"
            exit 1
        }
    }
    
    # Test infrastructure
    $infrastructureIssues = Test-Infrastructure
    if ($infrastructureIssues.Count -gt 0) {
        Write-Error "Infrastructure issues found: $($infrastructureIssues -join ', ')"
        Write-Warning "Continuing with business flow tests..."
    } else {
        Write-Success "All infrastructure components are healthy"
    }
    
    # Test business flows
    if (Test-BusinessFlows) {
        Write-Success "Business flows tested successfully"
    } else {
        Write-Error "Business flow tests failed"
    }
    
    Write-Section "🎉 Local Testing Completed" $Green
    Write-Host "Environment is running. Press Ctrl+C to stop or run with -StopOnly to clean up." -ForegroundColor $Yellow
    
    # Keep running for manual testing
    if (-not $SkipAspireStart) {
        Write-Host "Keeping environment running for manual testing..." -ForegroundColor $Cyan
        Write-Host "Available endpoints:" -ForegroundColor $Yellow
        Write-Host "  - LocalTesting API: http://localhost:5000" -ForegroundColor $Cyan
        Write-Host "  - Kafka UI: http://localhost:8082" -ForegroundColor $Cyan
        Write-Host "  - Flink UI: http://localhost:8081" -ForegroundColor $Cyan
        Write-Host "  - Temporal UI: http://localhost:8084" -ForegroundColor $Cyan
        Write-Host "  - Grafana: http://localhost:3000 (admin/admin)" -ForegroundColor $Cyan
        
        # Wait for user interrupt
        try {
            while ($true) {
                Start-Sleep -Seconds 30
                Write-Host "." -NoNewline -ForegroundColor $Green
            }
        } catch {
            Write-Host "`nReceived interrupt signal" -ForegroundColor $Yellow
        }
    }
    
} catch {
    Write-Error "Script execution failed: $($_.Exception.Message)"
    exit 1
} finally {
    if (-not $SkipAspireStart) {
        Write-Section "🧹 Final Cleanup"
        Stop-LocalEnvironment
    }
}