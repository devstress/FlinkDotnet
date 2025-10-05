#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Debug SQL Gateway integration test with comprehensive log collection
.DESCRIPTION
    Runs the Pattern5 SQL Gateway test with extensive logging and diagnostics
    to help identify the root cause of test failures.
#>

param(
    [switch]$KeepContainersRunning = $false
)

$ErrorActionPreference = "Continue"
$DebugLogDir = Join-Path $PSScriptRoot ".." "test-logs" "sql-gateway-debug"

# Create log directory
New-Item -ItemType Directory -Force -Path $DebugLogDir | Out-Null
Write-Host "📁 Log directory: $DebugLogDir" -ForegroundColor Cyan

# Log file paths
$testOutputLog = Join-Path $DebugLogDir "test-output.log"
$containerStatusLog = Join-Path $DebugLogDir "container-status.log"
$sqlGatewayLog = Join-Path $DebugLogDir "sql-gateway-container.log"
$jobManagerLog = Join-Path $DebugLogDir "jobmanager-container.log"
$taskManagerLog = Join-Path $DebugLogDir "taskmanager-container.log"
$kafkaLog = Join-Path $DebugLogDir "kafka-container.log"
$gatewayServiceLog = Join-Path $DebugLogDir "gateway-service.log"
$networkInspectLog = Join-Path $DebugLogDir "network-inspect.log"
$endpointTestLog = Join-Path $DebugLogDir "endpoint-test.log"
$summaryLog = Join-Path $DebugLogDir "debug-summary.md"

Write-Host ""
Write-Host "🔍 ========================================" -ForegroundColor Yellow
Write-Host "🔍 SQL GATEWAY DEBUG TEST" -ForegroundColor Yellow
Write-Host "🔍 ========================================" -ForegroundColor Yellow
Write-Host ""

# Function to get container runtime
function Get-ContainerRuntime {
    if (Get-Command podman -ErrorAction SilentlyContinue) {
        $podmanInfo = podman info 2>&1 | Out-String
        if ($podmanInfo -match "running") {
            return "podman"
        }
    }
    if (Get-Command docker -ErrorAction SilentlyContinue) {
        $dockerInfo = docker info 2>&1 | Out-String
        if ($dockerInfo -notmatch "error") {
            return "docker"
        }
    }
    return $null
}

$containerCmd = Get-ContainerRuntime
if (-not $containerCmd) {
    Write-Host "❌ No container runtime (docker/podman) found" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Using container runtime: $containerCmd" -ForegroundColor Green

# Build the solution
Write-Host ""
Write-Host "🔨 Building LocalTesting solution..." -ForegroundColor Cyan
Push-Location (Join-Path $PSScriptRoot ".." "LocalTesting")
dotnet build LocalTesting.sln --configuration Release 2>&1 | Tee-Object -FilePath (Join-Path $DebugLogDir "build.log")
$buildResult = $LASTEXITCODE
Pop-Location

if ($buildResult -ne 0) {
    Write-Host "❌ Build failed. Check build.log for details." -ForegroundColor Red
    exit 1
}
Write-Host "✅ Build successful" -ForegroundColor Green

# Start the test in background
Write-Host ""
Write-Host "🧪 Starting Pattern5 SQL Gateway test..." -ForegroundColor Cyan
Push-Location (Join-Path $PSScriptRoot ".." "LocalTesting")

$testProcess = Start-Process -FilePath "dotnet" -ArgumentList @(
    "test",
    "LocalTesting.IntegrationTests/LocalTesting.IntegrationTests.csproj",
    "--configuration", "Release",
    "--filter", "FullyQualifiedName~Gateway_Pattern5",
    "--logger", "console;verbosity=detailed",
    "--no-build"
) -NoNewWindow -PassThru -RedirectStandardOutput $testOutputLog -RedirectStandardError (Join-Path $DebugLogDir "test-error.log")

Pop-Location

Write-Host "⏳ Waiting 30s for containers to start..." -ForegroundColor Cyan
Start-Sleep -Seconds 30

# Collect container status
Write-Host ""
Write-Host "📊 Collecting container status..." -ForegroundColor Cyan
& $containerCmd ps -a | Tee-Object -FilePath $containerStatusLog

# Find running containers
Write-Host ""
Write-Host "🔍 Discovering containers..." -ForegroundColor Cyan
$sqlGatewayContainer = & $containerCmd ps --filter "name=sql-gateway" --format "{{.Names}}" | Select-Object -First 1
$jobManagerContainer = & $containerCmd ps --filter "name=jobmanager" --format "{{.Names}}" | Select-Object -First 1
$taskManagerContainer = & $containerCmd ps --filter "name=taskmanager" --format "{{.Names}}" | Select-Object -First 1
$kafkaContainer = & $containerCmd ps --filter "name=kafka" --format "{{.Names}}" | Select-Object -First 1
$gatewayServiceContainer = & $containerCmd ps --filter "name=gateway" --format "{{.Names}}" | Select-Object -First 1

Write-Host "   SQL Gateway: $sqlGatewayContainer" -ForegroundColor $(if ($sqlGatewayContainer) { "Green" } else { "Red" })
Write-Host "   JobManager: $jobManagerContainer" -ForegroundColor $(if ($jobManagerContainer) { "Green" } else { "Red" })
Write-Host "   TaskManager: $taskManagerContainer" -ForegroundColor $(if ($taskManagerContainer) { "Green" } else { "Red" })
Write-Host "   Kafka: $kafkaContainer" -ForegroundColor $(if ($kafkaContainer) { "Green" } else { "Red" })
Write-Host "   Gateway Service: $gatewayServiceContainer" -ForegroundColor $(if ($gatewayServiceContainer) { "Green" } else { "Red" })

# Collect logs from each container
Write-Host ""
Write-Host "📝 Collecting container logs..." -ForegroundColor Cyan

if ($sqlGatewayContainer) {
    Write-Host "   Collecting SQL Gateway logs..." -ForegroundColor Cyan
    & $containerCmd logs $sqlGatewayContainer 2>&1 | Out-File -FilePath $sqlGatewayLog
    Write-Host "   ✅ Saved to: $sqlGatewayLog" -ForegroundColor Green
} else {
    "SQL Gateway container not found" | Out-File -FilePath $sqlGatewayLog
    Write-Host "   ⚠️ SQL Gateway container not found" -ForegroundColor Yellow
}

if ($jobManagerContainer) {
    Write-Host "   Collecting JobManager logs..." -ForegroundColor Cyan
    & $containerCmd logs $jobManagerContainer 2>&1 | Out-File -FilePath $jobManagerLog
    Write-Host "   ✅ Saved to: $jobManagerLog" -ForegroundColor Green
} else {
    "JobManager container not found" | Out-File -FilePath $jobManagerLog
    Write-Host "   ⚠️ JobManager container not found" -ForegroundColor Yellow
}

if ($taskManagerContainer) {
    Write-Host "   Collecting TaskManager logs..." -ForegroundColor Cyan
    & $containerCmd logs $taskManagerContainer 2>&1 | Out-File -FilePath $taskManagerLog
    Write-Host "   ✅ Saved to: $taskManagerLog" -ForegroundColor Green
}

if ($kafkaContainer) {
    Write-Host "   Collecting Kafka logs..." -ForegroundColor Cyan
    & $containerCmd logs $kafkaContainer --tail 500 2>&1 | Out-File -FilePath $kafkaLog
    Write-Host "   ✅ Saved to: $kafkaLog" -ForegroundColor Green
}

if ($gatewayServiceContainer) {
    Write-Host "   Collecting Gateway Service logs..." -ForegroundColor Cyan
    & $containerCmd logs $gatewayServiceContainer 2>&1 | Out-File -FilePath $gatewayServiceLog
    Write-Host "   ✅ Saved to: $gatewayServiceLog" -ForegroundColor Green
}

# Test SQL Gateway endpoint directly
Write-Host ""
Write-Host "🌐 Testing SQL Gateway endpoint..." -ForegroundColor Cyan
if ($sqlGatewayContainer) {
    # Get SQL Gateway port mapping
    $portMapping = & $containerCmd port $sqlGatewayContainer 8083 2>&1
    if ($portMapping -match "0\.0\.0\.0:(\d+)") {
        $sqlGatewayPort = $matches[1]
        Write-Host "   SQL Gateway exposed on port: $sqlGatewayPort" -ForegroundColor Green
        
        # Test /v1/info endpoint
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:$sqlGatewayPort/v1/info" -TimeoutSec 5 -ErrorAction Stop
            "✅ SQL Gateway /v1/info responding: $($response.StatusCode)" | Tee-Object -FilePath $endpointTestLog -Append
            $response.Content | Tee-Object -FilePath $endpointTestLog -Append
            Write-Host "   ✅ SQL Gateway REST API is accessible" -ForegroundColor Green
        } catch {
            "❌ SQL Gateway /v1/info failed: $_" | Tee-Object -FilePath $endpointTestLog -Append
            Write-Host "   ❌ SQL Gateway REST API is NOT accessible" -ForegroundColor Red
        }
    } else {
        "⚠️ Could not determine SQL Gateway port mapping" | Tee-Object -FilePath $endpointTestLog
        Write-Host "   ⚠️ Could not determine SQL Gateway port" -ForegroundColor Yellow
    }
} else {
    "❌ SQL Gateway container not running" | Tee-Object -FilePath $endpointTestLog
}

# Inspect network connectivity
Write-Host ""
Write-Host "🔌 Inspecting network connectivity..." -ForegroundColor Cyan
if ($sqlGatewayContainer -and $jobManagerContainer) {
    # Check if containers can reach each other
    "=== SQL Gateway → JobManager connectivity ===" | Tee-Object -FilePath $networkInspectLog
    & $containerCmd exec $sqlGatewayContainer ping -c 3 flink-jobmanager 2>&1 | Tee-Object -FilePath $networkInspectLog -Append
    
    "=== Network inspect ===" | Tee-Object -FilePath $networkInspectLog -Append
    & $containerCmd inspect $sqlGatewayContainer --format "{{.NetworkSettings.Networks}}" | Tee-Object -FilePath $networkInspectLog -Append
    Write-Host "   ✅ Network inspection complete" -ForegroundColor Green
}

# Wait for test to complete or timeout
Write-Host ""
Write-Host "⏳ Waiting for test to complete (max 180s)..." -ForegroundColor Cyan
$testProcess | Wait-Process -Timeout 180 -ErrorAction SilentlyContinue

if ($testProcess.HasExited) {
    $testExitCode = $testProcess.ExitCode
    if ($testExitCode -eq 0) {
        Write-Host "✅ Test passed!" -ForegroundColor Green
    } else {
        Write-Host "❌ Test failed with exit code: $testExitCode" -ForegroundColor Red
    }
} else {
    Write-Host "⚠️ Test timed out after 180s" -ForegroundColor Yellow
    $testProcess | Stop-Process -Force
    $testExitCode = -1
}

# Collect final logs
Write-Host ""
Write-Host "📝 Collecting final logs..." -ForegroundColor Cyan
if ($sqlGatewayContainer) {
    & $containerCmd logs $sqlGatewayContainer 2>&1 | Out-File -FilePath $sqlGatewayLog
}

# Generate summary
Write-Host ""
Write-Host "📄 Generating debug summary..." -ForegroundColor Cyan

$summary = @"
# SQL Gateway Test Debug Summary

**Generated**: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**Test Result**: $(if ($testExitCode -eq 0) { "✅ PASSED" } else { "❌ FAILED (exit code: $testExitCode)" })
**Container Runtime**: $containerCmd

## Container Status

``````
$(Get-Content $containerStatusLog -Raw)
``````

## Containers Found

- **SQL Gateway**: $(if ($sqlGatewayContainer) { "✅ $sqlGatewayContainer" } else { "❌ Not found" })
- **JobManager**: $(if ($jobManagerContainer) { "✅ $jobManagerContainer" } else { "❌ Not found" })
- **TaskManager**: $(if ($taskManagerContainer) { "✅ $taskManagerContainer" } else { "❌ Not found" })
- **Kafka**: $(if ($kafkaContainer) { "✅ $kafkaContainer" } else { "❌ Not found" })
- **Gateway Service**: $(if ($gatewayServiceContainer) { "✅ $gatewayServiceContainer" } else { "❌ Not found" })

## Endpoint Tests

``````
$(if (Test-Path $endpointTestLog) { Get-Content $endpointTestLog -Raw } else { "No endpoint tests performed" })
``````

## Log Files

- Test output: ``test-output.log``
- SQL Gateway logs: ``sql-gateway-container.log``
- JobManager logs: ``jobmanager-container.log``
- TaskManager logs: ``taskmanager-container.log``
- Kafka logs: ``kafka-container.log``
- Gateway Service logs: ``gateway-service.log``
- Network inspection: ``network-inspect.log``

## Key Findings

### SQL Gateway Log Analysis

``````
$(if ($sqlGatewayContainer -and (Test-Path $sqlGatewayLog)) {
    $log = Get-Content $sqlGatewayLog -Raw
    if ($log -match "ERROR") {
        "⚠️ ERRORS FOUND in SQL Gateway logs"
        $log | Select-String -Pattern "ERROR" | Select-Object -First 10
    } elseif ($log -match "Started SqlGateway") {
        "✅ SQL Gateway started successfully"
    } else {
        "⚠️ No clear startup confirmation in logs"
    }
} else {
    "❌ SQL Gateway logs not available"
})
``````

### Test Output Analysis

``````
$(if (Test-Path $testOutputLog) {
    $testLog = Get-Content $testOutputLog -Raw
    if ($testLog -match "TaskCanceledException") {
        "❌ Test timed out waiting for SQL Gateway response"
    } elseif ($testLog -match "SQL Gateway session created") {
        "✅ SQL Gateway session creation succeeded"
    } elseif ($testLog -match "Failed Gateway_Pattern5") {
        "❌ Pattern5 test failed"
    }
    $testLog | Select-String -Pattern "(✅|❌|Error|Failed|Success)" | Select-Object -First 20
} else {
    "❌ Test output not available"
})
``````

## Recommendations

$(if ($testExitCode -ne 0) {
    if (-not $sqlGatewayContainer) {
        "1. ❌ SQL Gateway container is not starting - check AppHost configuration"
    } elseif ((Test-Path $endpointTestLog) -and (Get-Content $endpointTestLog -Raw) -match "NOT accessible") {
        "1. ❌ SQL Gateway container is running but REST API not responding - check service startup"
    } else {
        "1. ⚠️ Review SQL Gateway logs for startup errors"
    }
    "2. Check JobManager connectivity from SQL Gateway container"
    "3. Verify FLINK_PROPERTIES configuration in AppHost"
    "4. Test manual SQL Gateway endpoint: ``curl http://localhost:<port>/v1/info``"
} else {
    "✅ Test passed! No issues found."
})
``````

"@

$summary | Out-File -FilePath $summaryLog
Write-Host "✅ Summary saved to: $summaryLog" -ForegroundColor Green

# Cleanup containers unless requested to keep them
if (-not $KeepContainersRunning) {
    Write-Host ""
    Write-Host "🧹 Cleaning up containers..." -ForegroundColor Cyan
    if ($sqlGatewayContainer) { & $containerCmd stop $sqlGatewayContainer 2>&1 | Out-Null }
    if ($jobManagerContainer) { & $containerCmd stop $jobManagerContainer 2>&1 | Out-Null }
    if ($taskManagerContainer) { & $containerCmd stop $taskManagerContainer 2>&1 | Out-Null }
    if ($kafkaContainer) { & $containerCmd stop $kafkaContainer 2>&1 | Out-Null }
    if ($gatewayServiceContainer) { & $containerCmd stop $gatewayServiceContainer 2>&1 | Out-Null }
    Write-Host "✅ Containers stopped" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "ℹ️ Containers left running for manual inspection" -ForegroundColor Cyan
}

# Final summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "DEBUG COLLECTION COMPLETE" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""
Write-Host "📁 All logs saved to: $DebugLogDir" -ForegroundColor Cyan
Write-Host "📄 Read the summary: $summaryLog" -ForegroundColor Cyan
Write-Host ""

if ($testExitCode -ne 0) {
    Write-Host "❌ Test FAILED - Review logs for root cause" -ForegroundColor Red
    exit 1
} else {
    Write-Host "✅ Test PASSED" -ForegroundColor Green
    exit 0
}
