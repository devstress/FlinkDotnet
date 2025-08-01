#!/usr/bin/env pwsh
# Docker Compose LocalTesting Environment Test Script
# Alternative to Aspire for CI environments

param(
    [switch]$StopOnly,
    [int]$MessageCount = 1000,
    [int]$TimeoutMinutes = 20
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

function Stop-DockerComposeEnvironment {
    Write-Section "🧹 Cleaning up Docker Compose LocalTesting Environment"
    
    try {
        Push-Location LocalTesting
        
        Write-Step "Stopping Docker Compose services..."
        $stopOutput = docker compose -f docker-compose.ci.yml down -v 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Docker Compose services stopped"
        } else {
            Write-Warning "Docker Compose stop completed with warnings"
            Write-Host $stopOutput -ForegroundColor $Yellow
        }
        
        Write-Step "Cleaning up dangling containers and volumes..."
        docker container prune -f 2>$null
        docker volume prune -f 2>$null
        Write-Success "Cleanup completed"
        
    } catch {
        Write-Error "Error during cleanup: $($_.Exception.Message)"
    } finally {
        Pop-Location
    }
}

function Test-Prerequisites {
    Write-Section "📋 Testing Prerequisites for Docker Compose"
    
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
    
    # Test Docker Compose
    Write-Step "Testing Docker Compose..."
    try {
        $composeVersion = docker compose version 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Docker Compose is available: $composeVersion"
        } else {
            Write-Error "Docker Compose is not available"
            return $false
        }
    } catch {
        Write-Error "Docker Compose test failed: $($_.Exception.Message)"
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
    
    return $true
}

function Start-DockerComposeEnvironment {
    Write-Section "🚀 Starting Docker Compose LocalTesting Environment"
    
    $originalLocation = Get-Location
    
    try {
        # Navigate to LocalTesting directory
        Write-Step "Navigating to LocalTesting directory..."
        $localTestingPath = "LocalTesting"
        if (Test-Path $localTestingPath) {
            Set-Location $localTestingPath
            Write-Success "Changed to: $(Get-Location)"
        } else {
            Write-Error "LocalTesting directory not found: $localTestingPath"
            return $false
        }
        
        # Build the WebAPI project first
        Write-Step "Building LocalTesting WebApi project..."
        $buildOutput = dotnet build LocalTesting.WebApi/LocalTesting.WebApi.csproj --configuration Release 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Build completed successfully"
        } else {
            Write-Error "Build failed:"
            Write-Host $buildOutput -ForegroundColor $Red
            return $false
        }
        
        # Start Docker Compose services
        Write-Step "Starting Docker Compose services..."
        $composeOutput = docker compose -f docker-compose.ci.yml up -d 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Docker Compose services started"
        } else {
            Write-Error "Docker Compose startup failed:"
            Write-Host $composeOutput -ForegroundColor $Red
            return $false
        }
        
        # Wait for services to become healthy
        Write-Step "Waiting for services to become healthy..."
        $maxWait = 180  # 3 minutes
        $waited = 0
        $checkInterval = 10
        
        while ($waited -lt $maxWait) {
            Write-Host "." -NoNewline -ForegroundColor $Green
            
            # Check service health using simple docker compose ps
            $psOutput = docker compose -f docker-compose.ci.yml ps --format "table {{.Service}}\t{{.State}}\t{{.Health}}" 2>$null
            $healthyCount = ($psOutput | Select-String -Pattern "healthy|running" | Measure-Object).Count
            $totalServices = ($psOutput | Select-String -Pattern "flink-jobmanager|redis|kafka-broker|temporal-postgres|grafana" | Measure-Object).Count
            
            if ($healthyCount -ge 5) {  # Core services: Redis, Kafka, Postgres, Flink JM, Grafana
                Write-Host ""
                Write-Success "Core services are healthy ($healthyCount services healthy)"
                break
            }
            
            Start-Sleep -Seconds $checkInterval
            $waited += $checkInterval
        }
        
        if ($waited -ge $maxWait) {
            Write-Host ""
            Write-Warning "Timeout waiting for all services to become healthy"
            Write-Info "Continuing with available services..."
        }
        
        return $true
        
    } catch {
        Write-Error "Failed to start Docker Compose environment: $($_.Exception.Message)"
        return $false
    } finally {
        Set-Location $originalLocation
    }
}

function Start-LocalTestingAPI {
    Write-Section "🌐 Starting LocalTesting WebAPI"
    
    $originalLocation = Get-Location
    
    try {
        # Navigate to WebAPI directory
        Push-Location LocalTesting/LocalTesting.WebApi
        
        # Set environment variables for the API
        $env:KAFKA_BOOTSTRAP_SERVERS = "localhost:9092"
        $env:FLINK_JOBMANAGER_URL = "http://localhost:8081"
        $env:TEMPORAL_SERVER_URL = "localhost:7233"
        $env:ASPNETCORE_URLS = "http://localhost:5000"
        $env:ASPNETCORE_ENVIRONMENT = "Development"
        
        Write-Step "Starting LocalTesting WebAPI..."
        
        # Start the API as background process
        $apiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run", "--configuration", "Release" -PassThru -RedirectStandardOutput "api_output.log" -RedirectStandardError "api_error.log" -NoNewWindow
        $global:ApiPID = $apiProcess.Id
        Write-Success "LocalTesting WebAPI started with PID: $global:ApiPID"
        
        # Wait for API to be ready
        Write-Step "Waiting for API to be ready..."
        $maxRetries = 30
        $retryCount = 0
        $apiReady = $false
        
        while ($retryCount -lt $maxRetries -and -not $apiReady) {
            try {
                $response = Invoke-WebRequest -Uri "http://localhost:5000/health" -TimeoutSec 5 -ErrorAction Stop
                if ($response.StatusCode -eq 200) {
                    Write-Success "LocalTesting API is accessible and healthy"
                    $apiReady = $true
                } else {
                    Write-Warning "API returned status: $($response.StatusCode)"
                }
            } catch {
                $retryCount++
                Write-Host "." -NoNewline -ForegroundColor $Yellow
                Start-Sleep -Seconds 2
            }
        }
        
        if (-not $apiReady) {
            Write-Error "API failed to become ready after $($maxRetries * 2) seconds"
            return $false
        }
        
        return $true
        
    } catch {
        Write-Error "Failed to start LocalTesting API: $($_.Exception.Message)"
        return $false
    } finally {
        Pop-Location
    }
}

function Test-ServicesAccessibility {
    Write-Section "🔍 Testing Service Accessibility"
    
    $testEndpoints = @(
        @{Port=6379; Name="Redis"; Test="redis-cli ping"; Critical=$true},
        @{Port=9092; Name="Kafka"; Test="kafka-topics"; Critical=$true},
        @{Port=8082; Name="Kafka UI"; Test="http"; Critical=$false},
        @{Port=5432; Name="PostgreSQL"; Test="pg_isready"; Critical=$true},
        @{Port=7233; Name="Temporal Server"; Test="temporal"; Critical=$true},
        @{Port=8084; Name="Temporal UI"; Test="http"; Critical=$false},
        @{Port=8081; Name="Flink JobManager"; Test="http"; Critical=$true},
        @{Port=3000; Name="Grafana"; Test="http"; Critical=$false},
        @{Port=5000; Name="LocalTesting API"; Test="http"; Critical=$true}
    )
    
    $accessibleServices = @()
    $failedServices = @()
    
    foreach ($endpoint in $testEndpoints) {
        try {
            Write-Step "Testing $($endpoint.Name) on port $($endpoint.Port)..."
            
            if ($endpoint.Test -eq "http") {
                $uri = "http://localhost:$($endpoint.Port)/"
                $response = Invoke-WebRequest -Uri $uri -TimeoutSec 10 -ErrorAction SilentlyContinue
                if ($response.StatusCode -eq 200) {
                    Write-Success "$($endpoint.Name) is accessible (Status: $($response.StatusCode))"
                    $accessibleServices += $endpoint.Name
                } else {
                    throw "HTTP $($response.StatusCode)"
                }
            } else {
                # For non-HTTP services, just check if the port is listening
                $connection = Test-NetConnection -ComputerName localhost -Port $endpoint.Port -InformationLevel Quiet
                if ($connection) {
                    Write-Success "$($endpoint.Name) port $($endpoint.Port) is accessible"
                    $accessibleServices += $endpoint.Name
                } else {
                    throw "Port not accessible"
                }
            }
        }
        catch {
            Write-Warning "$($endpoint.Name) not accessible: $($_.Exception.Message)"
            $failedServices += $endpoint.Name
        }
    }
    
    # Check critical services
    $criticalFailed = $testEndpoints | Where-Object { $_.Critical -and $_.Name -in $failedServices }
    if ($criticalFailed.Count -gt 0) {
        Write-Error "Critical services failed: $($criticalFailed.Name -join ', ')"
        return $false
    }
    
    Write-Success "All critical services are accessible"
    if ($failedServices.Count -gt 0) {
        Write-Warning "Non-critical services not accessible: $($failedServices -join ', ')"
    }
    
    return $true
}

# Main execution
Write-Section "🧪 Docker Compose LocalTesting Environment Test Script" $Cyan
Write-Info "Using Docker Compose for CI-compatible environment testing"

if ($StopOnly) {
    Stop-DockerComposeEnvironment
    exit 0
}

try {
    # Test prerequisites
    if (-not (Test-Prerequisites)) {
        Write-Error "Prerequisites check failed"
        exit 1
    }
    
    # Clean up any existing environment
    Stop-DockerComposeEnvironment
    Start-Sleep -Seconds 5
    
    # Start Docker Compose environment
    if (-not (Start-DockerComposeEnvironment)) {
        Write-Error "Failed to start Docker Compose environment"
        exit 1
    }
    
    # Start LocalTesting API
    if (-not (Start-LocalTestingAPI)) {
        Write-Error "Failed to start LocalTesting API"
        exit 1
    }
    
    # Test service accessibility
    if (-not (Test-ServicesAccessibility)) {
        Write-Error "Service accessibility tests failed"
        exit 1
    }
    
    Write-Section "🎉 Docker Compose LocalTesting Completed Successfully" $Green
    Write-Host "Environment is running with Docker Compose orchestration. Available endpoints:" -ForegroundColor $Yellow
    Write-Host "  - LocalTesting API: http://localhost:5000" -ForegroundColor $Cyan
    Write-Host "  - Kafka UI: http://localhost:8082" -ForegroundColor $Cyan
    Write-Host "  - Temporal UI: http://localhost:8084" -ForegroundColor $Cyan
    Write-Host "  - Flink JobManager: http://localhost:8081" -ForegroundColor $Cyan
    Write-Host "  - Grafana: http://localhost:3000" -ForegroundColor $Cyan
    Write-Host "  - Health Check: http://localhost:5000/health" -ForegroundColor $Cyan
    Write-Host "`nPress Ctrl+C to stop or run with -StopOnly to clean up." -ForegroundColor $Yellow
    
    # Keep running for manual testing
    Write-Host "Keeping Docker Compose environment running for manual testing..." -ForegroundColor $Cyan
    
    # Wait for user interrupt
    try {
        while ($true) {
            Start-Sleep -Seconds 30
            Write-Host "." -NoNewline -ForegroundColor $Green
        }
    } catch {
        Write-Host "`nReceived interrupt signal" -ForegroundColor $Yellow
    }
    
} catch {
    Write-Error "Script execution failed: $($_.Exception.Message)"
    exit 1
} finally {
    Write-Section "🧹 Final Cleanup"
    
    # Stop API process if running
    try {
        if ($global:ApiPID) {
            Write-Step "Stopping LocalTesting API process (PID: $global:ApiPID)..."
            Stop-Process -Id $global:ApiPID -Force -ErrorAction SilentlyContinue
        }
    } catch {
        Write-Warning "API process may have already stopped"
    }
    
    Stop-DockerComposeEnvironment
}