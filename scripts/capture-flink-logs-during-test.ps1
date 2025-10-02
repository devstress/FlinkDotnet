#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Captures Flink JobManager logs during FlinkRunnerDirectTest execution
.DESCRIPTION
    This script runs the FlinkRunnerDirectTest and captures Flink JobManager logs
    to help debug IR translation issues. The logs are saved to a timestamped file.
.PARAMETER TestFilter
    Optional test filter (default: FlinkRunner_DirectExecution_WithCorrectKafkaConfig_ShouldWork)
.PARAMETER OutputDir
    Directory to save logs (default: ./test-logs)
.EXAMPLE
    .\capture-flink-logs-during-test.ps1
    .\capture-flink-logs-during-test.ps1 -OutputDir "C:\temp\logs"
#>

param(
    [string]$TestFilter = "FlinkRunner_DirectExecution_WithCorrectKafkaConfig_ShouldWork",
    [string]$OutputDir = "./test-logs"
)

$ErrorActionPreference = "Stop"

# Create output directory
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$logDir = Join-Path $OutputDir $timestamp
New-Item -ItemType Directory -Path $logDir -Force | Out-Null

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "Flink Log Capture Script" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "Test Filter: $TestFilter" -ForegroundColor Yellow
Write-Host "Log Directory: $logDir" -ForegroundColor Yellow
Write-Host ""

# Start log capture job that will wait for container
Write-Host "[INFO] Starting background log capture job..." -ForegroundColor Cyan
$logFile = Join-Path $logDir "flink-jobmanager.log"
$logJob = Start-Job -ScriptBlock {
    param($outputFile)
    
    # Wait for Flink JobManager container to start (up to 2 minutes)
    $maxWait = 120
    $waited = 0
    $containerName = $null
    
    while ($waited -lt $maxWait) {
        $containerName = docker ps --filter "name=flink-jobmanager" --format "{{.Names}}" | Select-Object -First 1
        if ($containerName) {
            Write-Host "Found Flink container: $containerName"
            break
        }
        Start-Sleep -Seconds 2
        $waited += 2
    }
    
    if ($containerName) {
        Write-Host "Starting log capture from: $containerName"
        docker logs -f $containerName 2>&1 | Tee-Object -FilePath $outputFile
    } else {
        Write-Host "ERROR: Flink JobManager container never started"
    }
} -ArgumentList $logFile

Write-Host "[OK] Background log capture job started (Job ID: $($logJob.Id))" -ForegroundColor Green
Write-Host "   Job will wait for Flink container to start, then capture logs" -ForegroundColor Yellow
Write-Host "   Logs will be saved to: $logFile" -ForegroundColor Yellow
Write-Host ""

# Run the test
Write-Host "Running FlinkRunnerDirectTest..." -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

$testLogFile = Join-Path $logDir "test-output.log"
$testStartTime = Get-Date

try {
    # Run test and capture output
    dotnet test ./LocalTesting/LocalTesting.IntegrationTests/LocalTesting.IntegrationTests.csproj `
        --filter "FullyQualifiedName~$TestFilter" `
        --logger "console;verbosity=detailed" `
        --configuration Release `
        --no-build `
        2>&1 | Tee-Object -FilePath $testLogFile
    
    $testExitCode = $LASTEXITCODE
    $testEndTime = Get-Date
    $testDuration = $testEndTime - $testStartTime
    
    Write-Host ""
    Write-Host "============================================================" -ForegroundColor Cyan
    Write-Host "Test completed in $($testDuration.TotalSeconds) seconds" -ForegroundColor Yellow
    Write-Host "Exit code: $testExitCode" -ForegroundColor $(if ($testExitCode -eq 0) { "Green" } else { "Red" })
    
} catch {
    Write-Host "[ERROR] running test: $_" -ForegroundColor Red
    $testExitCode = 1
} finally {
    # Wait a bit more to capture any final logs
    Write-Host ""
    Write-Host "[INFO] Waiting 5 seconds to capture final logs..." -ForegroundColor Cyan
    Start-Sleep -Seconds 5
    
    # Stop log capture
    Write-Host "[INFO] Stopping log capture..." -ForegroundColor Cyan
    Stop-Job -Job $logJob
    Remove-Job -Job $logJob -Force
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "Log Capture Complete" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "[INFO] Logs saved to:" -ForegroundColor Yellow
Write-Host "   Flink logs:  $logFile" -ForegroundColor White
Write-Host "   Test output: $testLogFile" -ForegroundColor White
Write-Host ""

# Extract key information from Flink logs
Write-Host "[INFO] Analyzing Flink logs..." -ForegroundColor Cyan
$analysisFile = Join-Path $logDir "analysis.txt"

@"
FLINK LOG ANALYSIS
==================
Test: $TestFilter
Timestamp: $timestamp
Duration: $($testDuration.TotalSeconds) seconds
Test Result: $(if ($testExitCode -eq 0) { "PASSED" } else { "FAILED" })

SEARCHING FOR KEY PATTERNS:
"@ | Out-File -FilePath $analysisFile

# Search for errors
Write-Host "   - Searching for errors..." -ForegroundColor Yellow
$errors = Select-String -Path $logFile -Pattern "ERROR|Exception|Failed|failed" -Context 2, 2
if ($errors) {
    "`n=== ERRORS FOUND ===" | Out-File -FilePath $analysisFile -Append
    $errors | ForEach-Object { $_.Line } | Out-File -FilePath $analysisFile -Append
    Write-Host "     Found $($errors.Count) error patterns" -ForegroundColor Red
} else {
    "`n=== NO ERRORS FOUND ===" | Out-File -FilePath $analysisFile -Append
    Write-Host "     No error patterns found" -ForegroundColor Green
}

# Search for IR deserialization
Write-Host "   - Searching for IR deserialization..." -ForegroundColor Yellow
$irLogs = Select-String -Path $logFile -Pattern "KAFKA SOURCE|KAFKA SINK|MAP OPERATION|FlinkJobRunner" -Context 1, 1
if ($irLogs) {
    "`n=== IR RUNNER LOGS ===" | Out-File -FilePath $analysisFile -Append
    $irLogs | ForEach-Object { $_.Line } | Out-File -FilePath $analysisFile -Append
    Write-Host "     Found $($irLogs.Count) IR runner log entries" -ForegroundColor Green
} else {
    "`n=== NO IR RUNNER LOGS FOUND ===" | Out-File -FilePath $analysisFile -Append
    Write-Host "     No IR runner logs found (this may indicate the job never started)" -ForegroundColor Red
}

# Search for job submission
Write-Host "   - Searching for job submission..." -ForegroundColor Yellow
$jobSubmission = Select-String -Path $logFile -Pattern "job.*submitted|Job.*running|JobManager" -Context 2, 2
if ($jobSubmission) {
    "`n=== JOB SUBMISSION LOGS ===" | Out-File -FilePath $analysisFile -Append
    $jobSubmission | ForEach-Object { $_.Line } | Out-File -FilePath $analysisFile -Append
    Write-Host "     Found $($jobSubmission.Count) job submission entries" -ForegroundColor Green
}

Write-Host ""
Write-Host "[INFO] Analysis saved to: $analysisFile" -ForegroundColor Yellow
Write-Host ""

# Display summary
if ($testExitCode -eq 0) {
    Write-Host "[PASS] TEST PASSED" -ForegroundColor Green
} else {
    Write-Host "[FAIL] TEST FAILED" -ForegroundColor Red
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Review the Flink logs: $logFile" -ForegroundColor White
    Write-Host "2. Review the test output: $testLogFile" -ForegroundColor White
    Write-Host "3. Check the analysis: $analysisFile" -ForegroundColor White
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan

exit $testExitCode