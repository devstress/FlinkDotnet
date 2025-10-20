#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Terminates all LocalTesting Aspire orchestration processes
.DESCRIPTION
    This script forcefully terminates all processes related to LocalTesting Aspire orchestration,
    including DCP (Developer Control Plane), dotnet processes, and related infrastructure.
    Use this when you need to completely stop a running LocalTesting environment.
.EXAMPLE
    .\scripts\kill-localtesting-aspire.ps1
    powershell -ExecutionPolicy Bypass -File scripts/kill-localtesting-aspire.ps1
#>

$ErrorActionPreference = "Continue"

Write-Host "=====================================================================" -ForegroundColor Red
Write-Host "  Kill LocalTesting Aspire Processes" -ForegroundColor Red
Write-Host "=====================================================================" -ForegroundColor Red
Write-Host ""

# Step 1: Kill Aspire DCP processes
Write-Host "[1/4] Terminating Aspire DCP (Developer Control Plane) processes..." -ForegroundColor Yellow
$dcpProcessNames = @("dcpctrl", "dcp")
$killedDcp = 0

foreach ($processName in $dcpProcessNames) {
    $processes = Get-Process -Name $processName -ErrorAction SilentlyContinue
    if ($processes) {
        foreach ($proc in $processes) {
            try {
                Write-Host "      Killing: $processName (PID: $($proc.Id))" -ForegroundColor Yellow
                Stop-Process -Id $proc.Id -Force -ErrorAction Stop
                $killedDcp++
                Write-Host "      [OK] Terminated $processName" -ForegroundColor Green
            }
            catch {
                Write-Host "      [FAIL] Could not kill $processName (PID: $($proc.Id)): $_" -ForegroundColor Red
            }
        }
    }
}

if ($killedDcp -eq 0) {
    Write-Host "      [INFO] No Aspire DCP processes found" -ForegroundColor Gray
}
Write-Host ""

# Step 2: Kill LocalTesting dotnet processes
Write-Host "[2/4] Terminating LocalTesting dotnet processes..." -ForegroundColor Yellow
$dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
$killedDotnet = 0

if ($dotnetProcesses) {
    Write-Host "      Found $($dotnetProcesses.Count) dotnet.exe process(es), checking command lines..." -ForegroundColor Cyan
    foreach ($proc in $dotnetProcesses) {
        try {
            $cmdLine = (Get-CimInstance Win32_Process -Filter "ProcessId = $($proc.Id)").CommandLine
            
            # Check if the command line contains LocalTesting
            if ($cmdLine -match "LocalTesting") {
                Write-Host "      Killing: dotnet (PID: $($proc.Id))" -ForegroundColor Yellow
                Write-Host "        Command: $cmdLine" -ForegroundColor Gray
                Stop-Process -Id $proc.Id -Force -ErrorAction Stop
                $killedDotnet++
                Write-Host "      [OK] Terminated dotnet process" -ForegroundColor Green
            }
        }
        catch {
            Write-Host "      [WARNING] Could not process PID $($proc.Id): $_" -ForegroundColor Yellow
        }
    }
    
    if ($killedDotnet -eq 0) {
        Write-Host "      [INFO] No LocalTesting related dotnet processes found" -ForegroundColor Gray
    }
}
else {
    Write-Host "      [INFO] No dotnet.exe processes found" -ForegroundColor Gray
}
Write-Host ""

# Step 3: Kill JobGateway and related processes
Write-Host "[3/4] Terminating JobGateway and related processes..." -ForegroundColor Yellow
$relatedProcessNames = @(
    "FlinkDotNet.JobGateway",
    "Flink.JobBuilder",
    "FlinkDotNet"
)
$killedRelated = 0

foreach ($processName in $relatedProcessNames) {
    $processes = Get-Process -Name $processName -ErrorAction SilentlyContinue
    if ($processes) {
        foreach ($proc in $processes) {
            try {
                Write-Host "      Killing: $processName (PID: $($proc.Id))" -ForegroundColor Yellow
                Stop-Process -Id $proc.Id -Force -ErrorAction Stop
                $killedRelated++
                Write-Host "      [OK] Terminated $processName" -ForegroundColor Green
            }
            catch {
                Write-Host "      [FAIL] Could not kill $processName (PID: $($proc.Id)): $_" -ForegroundColor Red
            }
        }
    }
}

if ($killedRelated -eq 0) {
    Write-Host "      [INFO] No related processes found" -ForegroundColor Gray
}
Write-Host ""

# Step 4: Stop Docker containers
Write-Host "[4/5] Stopping Docker containers..." -ForegroundColor Yellow

# Check if Docker is available
$dockerAvailable = $false
try {
    $null = docker --version 2>$null
    $dockerAvailable = $true
}
catch {
    Write-Host "      [INFO] Docker not available or not running" -ForegroundColor Gray
}

$stoppedContainers = 0
if ($dockerAvailable) {
    # Get all running containers
    $runningContainers = docker ps --format "{{.ID}}|{{.Names}}" 2>$null
    
    if ($runningContainers) {
        foreach ($containerInfo in $runningContainers) {
            $parts = $containerInfo -split '\|'
            if ($parts.Length -ge 2) {
                $containerId = $parts[0]
                $containerName = $parts[1]
                
                # Stop containers related to LocalTesting (Flink, Kafka, Redis, Prometheus, Grafana, etc.)
                if ($containerName -match "flink|kafka|redis|prometheus|grafana|zookeeper|jobmanager|taskmanager") {
                    try {
                        Write-Host "      Stopping container: $containerName ($containerId)" -ForegroundColor Yellow
                        docker stop $containerId 2>$null | Out-Null
                        $stoppedContainers++
                        Write-Host "      [OK] Stopped container: $containerName" -ForegroundColor Green
                    }
                    catch {
                        Write-Host "      [WARNING] Could not stop container $containerName : $_" -ForegroundColor Yellow
                    }
                }
            }
        }
    }
    
    if ($stoppedContainers -eq 0) {
        Write-Host "      [INFO] No LocalTesting related containers found running" -ForegroundColor Gray
    }
    else {
        Write-Host "      [OK] Stopped $stoppedContainers container(s)" -ForegroundColor Green
    }
}
Write-Host ""

# Step 5: Summary
Write-Host "[5/5] Process termination summary:" -ForegroundColor Yellow
$totalKilled = $killedDcp + $killedDotnet + $killedRelated
Write-Host "      DCP processes killed: $killedDcp" -ForegroundColor Cyan
Write-Host "      dotnet processes killed: $killedDotnet" -ForegroundColor Cyan
Write-Host "      Related processes killed: $killedRelated" -ForegroundColor Cyan
Write-Host "      Docker containers stopped: $stoppedContainers" -ForegroundColor Cyan
Write-Host "      Total processes killed: $totalKilled" -ForegroundColor $(if ($totalKilled -gt 0) { "Green" } else { "Gray" })
Write-Host ""

Write-Host "=====================================================================" -ForegroundColor Green
Write-Host "  LocalTesting Aspire environment shutdown complete" -ForegroundColor Green
Write-Host "=====================================================================" -ForegroundColor Green
Write-Host ""

if ($totalKilled -gt 0 -or $stoppedContainers -gt 0) {
    Write-Host "Wait 2-3 seconds before starting LocalTesting again to ensure ports are released." -ForegroundColor Yellow
    Write-Host ""
}
else {
    Write-Host "No LocalTesting Aspire processes or containers were running." -ForegroundColor Gray
    Write-Host ""
}