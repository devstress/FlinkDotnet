#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Forcefully terminates processes that may lock build files
.DESCRIPTION
    This script aggressively terminates all processes that could lock DLL files
    during builds, including by specific PID if provided.
.EXAMPLE
    .\scripts\kill-locked-processes.ps1
    .\scripts\kill-locked-processes.ps1 -PID 24384
#>

param(
    [int]$ProcessId = 0
)

$ErrorActionPreference = "Continue"

Write-Host "=====================================================================" -ForegroundColor Red
Write-Host "  Kill Locked Processes Script" -ForegroundColor Red
Write-Host "=====================================================================" -ForegroundColor Red
Write-Host ""

# Kill by specific ProcessId if provided
if ($ProcessId -gt 0) {
    Write-Host "[1/3] Attempting to kill process PID: $ProcessId" -ForegroundColor Yellow
    try {
        $process = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
        if ($process) {
            Write-Host "      Found: $($process.ProcessName) (PID: $ProcessId)" -ForegroundColor Cyan
            Stop-Process -Id $ProcessId -Force
            Write-Host "      [OK] Process terminated" -ForegroundColor Green
        }
        else {
            Write-Host "      [INFO] Process $ProcessId not found (may have already terminated)" -ForegroundColor Gray
        }
    }
    catch {
        Write-Host "      [WARNING] Could not kill process $ProcessId : $_" -ForegroundColor Yellow
    }
    Write-Host ""
}

# Kill by process names
Write-Host "[2/3] Killing processes by name..." -ForegroundColor Yellow
$processesToKill = @(
    "Exercise1-StringCapitalize",
    "Exercise2-BackupAggregator",
    "Flink.JobGateway",
    "Flink.JobBuilder",
    "FlinkDotNet",
    "testhost",
    "vstest.console",
    "dcpctrl",  # Aspire DCP Control - orchestrator that holds ports
    "dcp"       # Alternative Aspire DCP process name
)

$killedCount = 0
foreach ($processName in $processesToKill) {
    $processes = Get-Process -Name $processName -ErrorAction SilentlyContinue
    if ($processes) {
        foreach ($proc in $processes) {
            try {
                Write-Host "      Killing: $processName (PID: $($proc.Id))" -ForegroundColor Yellow
                Stop-Process -Id $proc.Id -Force -ErrorAction Stop
                $killedCount++
                Write-Host "      [OK] Terminated $processName" -ForegroundColor Green
            }
            catch {
                Write-Host "      [FAIL] Could not kill $processName (PID: $($proc.Id)): $_" -ForegroundColor Red
            }
        }
    }
}

if ($killedCount -eq 0) {
    Write-Host "      [INFO] No processes found to kill" -ForegroundColor Gray
}
Write-Host ""

# Kill dotnet and dotnet test processes related to LocalTesting or LearningCourse
Write-Host "[3/3] Killing dotnet processes related to LocalTesting or LearningCourse..." -ForegroundColor Yellow
$dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
$killedDotnetCount = 0

if ($dotnetProcesses) {
    Write-Host "      Found $($dotnetProcesses.Count) dotnet.exe process(es), checking command lines..." -ForegroundColor Cyan
    foreach ($proc in $dotnetProcesses) {
        try {
            $cmdLine = (Get-CimInstance Win32_Process -Filter "ProcessId = $($proc.Id)").CommandLine
            
            # Check if the command line contains LocalTesting or LearningCourse
            if ($cmdLine -match "LocalTesting|LearningCourse") {
                Write-Host "      Killing: dotnet (PID: $($proc.Id))" -ForegroundColor Yellow
                Write-Host "        Command: $cmdLine" -ForegroundColor Gray
                Stop-Process -Id $proc.Id -Force -ErrorAction Stop
                $killedDotnetCount++
                Write-Host "      [OK] Terminated dotnet process" -ForegroundColor Green
            }
            else {
                Write-Host "      [SKIP] PID $($proc.Id): Not related to LocalTesting/LearningCourse" -ForegroundColor Gray
            }
        }
        catch {
            Write-Host "      [WARNING] Could not process PID $($proc.Id): $_" -ForegroundColor Yellow
        }
    }
    
    if ($killedDotnetCount -eq 0) {
        Write-Host "      [INFO] No LocalTesting or LearningCourse related dotnet processes found" -ForegroundColor Gray
    }
    else {
        Write-Host "      [OK] Killed $killedDotnetCount LocalTesting/LearningCourse dotnet process(es)" -ForegroundColor Green
    }
}
else {
    Write-Host "      [OK] No dotnet.exe processes found" -ForegroundColor Green
}
Write-Host ""

Write-Host "=====================================================================" -ForegroundColor Green
Write-Host "  Process termination complete" -ForegroundColor Green
Write-Host "=====================================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Wait 2-3 seconds, then try building again:" -ForegroundColor Yellow
Write-Host "  powershell -ExecutionPolicy Bypass -File scripts/clean-build-day01.ps1" -ForegroundColor White
Write-Host ""