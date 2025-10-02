#!/usr/bin/env pwsh

# Script to run integration test and capture Flink logs before cleanup
# This helps debug why messages aren't being processed

Write-Host "Starting integration test with log capture..." -ForegroundColor Cyan

# Start the test in background
$testJob = Start-Job -ScriptBlock {
    Set-Location "C:\GitHub\FlinkDotnet"
    dotnet test LocalTesting/LocalTesting.IntegrationTests/LocalTesting.IntegrationTests.csproj `
        --filter "FullyQualifiedName~FlinkIrStringOpsIntegrationTest" `
        --logger "console;verbosity=normal" `
        --configuration Release
}

# Wait for containers to start
Write-Host "Waiting 30 seconds for infrastructure to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

# Find Flink JobManager container
$flinkContainer = docker ps --filter "ancestor=apache/flink:1.20.0" --filter "name=jobmanager" --format "{{.Names}}" | Select-Object -First 1

if ($flinkContainer) {
    Write-Host "Found Flink JobManager container: $flinkContainer" -ForegroundColor Green
    
    # Wait a bit more for job to be submitted
    Write-Host "Waiting 20 more seconds for job submission..." -ForegroundColor Yellow
    Start-Sleep -Seconds 20
    
    Write-Host "`n===============================================================" -ForegroundColor Cyan
    Write-Host "  FLINK JOBMANAGER LOGS (Last 200 lines)" -ForegroundColor Cyan
    Write-Host "===============================================================" -ForegroundColor Cyan
    docker logs $flinkContainer --tail 200
    
    # Try to get TaskManager logs too
    $taskManagerContainer = docker ps --filter "ancestor=apache/flink:1.20.0" --filter "name=taskmanager" --format "{{.Names}}" | Select-Object -First 1
    
    if ($taskManagerContainer) {
        Write-Host "`n===============================================================" -ForegroundColor Cyan
        Write-Host "  FLINK TASKMANAGER LOGS (Last 200 lines)" -ForegroundColor Cyan
        Write-Host "===============================================================" -ForegroundColor Cyan
        docker logs $taskManagerContainer --tail 200
    }
    
    # List running jobs
    Write-Host "`n===============================================================" -ForegroundColor Cyan
    Write-Host "  FLINK JOBS" -ForegroundColor Cyan
    Write-Host "===============================================================" -ForegroundColor Cyan
    
    $flinkPort = docker port $flinkContainer 8081 | ForEach-Object { $_.Split(":")[1] }
    if ($flinkPort) {
        try {
            $jobs = Invoke-RestMethod -Uri "http://localhost:$flinkPort/v1/jobs" -Method Get
            $jobs.jobs | ForEach-Object {
                Write-Host "Job ID: $($_.id), Status: $($_.status)" -ForegroundColor Yellow
            }
        } catch {
            Write-Host "Could not query Flink REST API: $_" -ForegroundColor Red
        }
    }
    
} else {
    Write-Host "No Flink JobManager container found!" -ForegroundColor Red
}

# Wait for test to complete
Write-Host "`nWaiting for test to complete..." -ForegroundColor Yellow
$testResult = Wait-Job $testJob | Receive-Job
$testJob | Remove-Job

Write-Host "`n===============================================================" -ForegroundColor Cyan
Write-Host "  TEST OUTPUT" -ForegroundColor Cyan
Write-Host "===============================================================" -ForegroundColor Cyan
$testResult

Write-Host "`nDebug session complete" -ForegroundColor Green