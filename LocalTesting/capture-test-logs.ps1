#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Runs LocalTesting integration tests and captures container logs for debugging

.DESCRIPTION
    This script:
    1. Starts the test in background
    2. Waits for containers to start
    3. Captures logs from all running containers
    4. Waits for test to complete
    5. Provides diagnostic information

.PARAMETER TestFilter
    NUnit test filter (default: Gateway_Pattern5_DirectFlinkSQL_ShouldWork)

.PARAMETER LogDir
    Directory to save logs (default: test-logs)

.EXAMPLE
    .\capture-test-logs.ps1
    .\capture-test-logs.ps1 -TestFilter "Gateway_Pattern5" -LogDir "my-logs"
#>

param(
    [string]$TestFilter = "Gateway_Pattern5_DirectFlinkSQL_ShouldWork",
    [string]$LogDir = "test-logs"
)

$ErrorActionPreference = "Continue"

Write-Host "=== LocalTesting Container Log Capture ===" -ForegroundColor Cyan
Write-Host "Test Filter: $TestFilter" -ForegroundColor White
Write-Host "Log Directory: $LogDir" -ForegroundColor White
Write-Host ""

# Create log directory
$logPath = Join-Path $PSScriptRoot $LogDir
if (Test-Path $logPath) {
    Write-Host "Cleaning existing log directory..." -ForegroundColor Yellow
    Remove-Item $logPath -Recurse -Force
}
New-Item -ItemType Directory -Path $logPath | Out-Null
Write-Host "Created log directory: $logPath" -ForegroundColor Green
Write-Host ""

# Start test in background
Write-Host "Starting test in background..." -ForegroundColor Cyan
$testProject = Join-Path $PSScriptRoot "LocalTesting.IntegrationTests\LocalTesting.IntegrationTests.csproj"
$testCmd = "dotnet test `"$testProject`" --configuration Release --filter `"FullyQualifiedName~$TestFilter`" --logger `"console;verbosity=detailed`""

Write-Host "Test command: $testCmd" -ForegroundColor Gray
$job = Start-Job -ScriptBlock {
    param($cmd)
    Invoke-Expression $cmd
} -ArgumentList $testCmd

Write-Host "Test job started (Job ID: $($job.Id))" -ForegroundColor Green
Write-Host ""

# Wait for containers to start
Write-Host "Waiting 20 seconds for containers to start..." -ForegroundColor Cyan
Start-Sleep -Seconds 20

# Check for running containers
Write-Host "Checking for running containers..." -ForegroundColor Cyan
$containers = podman ps --format "{{.ID}} {{.Names}} {{.Status}}" 2>$null
if ($LASTEXITCODE -eq 0 -and $containers) {
    Write-Host "Found running containers:" -ForegroundColor Green
    Write-Host $containers
    Write-Host ""
    
    # Capture logs from each container
    $containerIds = podman ps --format "{{.ID}}" 2>$null
    if ($containerIds) {
        foreach ($containerId in $containerIds) {
            $containerName = (podman inspect $containerId --format "{{.Name}}" 2>$null).Trim()
            $logFile = Join-Path $logPath "$containerName-$containerId.log"
            
            Write-Host "Capturing logs from container: $containerName ($containerId)" -ForegroundColor Cyan
            podman logs $containerId > $logFile 2>&1
            
            if (Test-Path $logFile) {
                $lineCount = (Get-Content $logFile).Count
                Write-Host "  Saved $lineCount lines to: $logFile" -ForegroundColor Green
            } else {
                Write-Host "  Failed to capture logs" -ForegroundColor Red
            }
        }
        Write-Host ""
    }
    
    # Also capture detailed container info
    Write-Host "Capturing container inspection details..." -ForegroundColor Cyan
    foreach ($containerId in $containerIds) {
        $containerName = (podman inspect $containerId --format "{{.Name}}" 2>$null).Trim()
        $inspectFile = Join-Path $logPath "$containerName-$containerId-inspect.json"
        
        podman inspect $containerId > $inspectFile 2>&1
        if (Test-Path $inspectFile) {
            Write-Host "  Saved inspect to: $inspectFile" -ForegroundColor Green
        }
    }
    Write-Host ""
    
} else {
    Write-Host "No containers found running. Test may have completed already or failed to start." -ForegroundColor Yellow
    Write-Host ""
}

# Wait for test to complete (with timeout)
Write-Host "Waiting for test to complete (max 3 minutes)..." -ForegroundColor Cyan
$timeout = 180
$elapsed = 0
$checkInterval = 5

while ($elapsed -lt $timeout) {
    $jobState = (Get-Job -Id $job.Id).State
    
    if ($jobState -eq "Completed" -or $jobState -eq "Failed" -or $jobState -eq "Stopped") {
        Write-Host "Test completed after $elapsed seconds (State: $jobState)" -ForegroundColor Green
        break
    }
    
    Start-Sleep -Seconds $checkInterval
    $elapsed += $checkInterval
    
    if ($elapsed % 30 -eq 0) {
        Write-Host "  Still waiting... ($elapsed seconds elapsed)" -ForegroundColor Gray
    }
}

# Get test output
Write-Host ""
Write-Host "=== Test Output ===" -ForegroundColor Cyan
$testOutput = Receive-Job -Id $job.Id
Write-Host $testOutput

# Save test output to file
$testOutputFile = Join-Path $logPath "test-output.log"
$testOutput | Out-File -FilePath $testOutputFile -Encoding UTF8
Write-Host ""
Write-Host "Test output saved to: $testOutputFile" -ForegroundColor Green

# Cleanup job
Remove-Job -Id $job.Id -Force

# Final container check
Write-Host ""
Write-Host "=== Final Container Status ===" -ForegroundColor Cyan
$finalContainers = podman ps -a --format "table {{.ID}}\t{{.Names}}\t{{.Status}}" 2>$null
if ($LASTEXITCODE -eq 0 -and $finalContainers) {
    Write-Host $finalContainers
} else {
    Write-Host "No containers found" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
Write-Host "All logs saved to: $logPath" -ForegroundColor Green
Write-Host "Review logs to diagnose test failures" -ForegroundColor White
Write-Host ""

# List captured files
Write-Host "Captured files:" -ForegroundColor Cyan
Get-ChildItem $logPath | ForEach-Object {
    $size = if ($_.Length -gt 1KB) { "$([math]::Round($_.Length/1KB, 2)) KB" } else { "$($_.Length) bytes" }
    Write-Host "  $($_.Name) - $size" -ForegroundColor White
}