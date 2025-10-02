#!/usr/bin/env pwsh
# Diagnostic script to inspect running Flink job configuration and logs

param(
    [string]$FlinkJobManagerUrl = "http://localhost:8081",
    [int]$WaitSeconds = 30
)

Write-Host "=== Flink Job Diagnostic Tool ===" -ForegroundColor Cyan
Write-Host "Flink JobManager URL: $FlinkJobManagerUrl" -ForegroundColor Yellow
Write-Host "Will monitor for $WaitSeconds seconds..." -ForegroundColor Yellow
Write-Host ""

$deadline = (Get-Date).AddSeconds($WaitSeconds)

while ((Get-Date) -lt $deadline) {
    try {
        # Get all jobs
        $jobsResponse = Invoke-RestMethod -Uri "$FlinkJobManagerUrl/jobs/overview" -Method Get -ErrorAction Stop
        
        if ($jobsResponse.jobs -and $jobsResponse.jobs.Count -gt 0) {
            Write-Host "Found $($jobsResponse.jobs.Count) job(s):" -ForegroundColor Green
            
            foreach ($job in $jobsResponse.jobs) {
                $jobId = $job.jid
                $jobName = $job.name
                $jobState = $job.state
                
                Write-Host "`n  Job: $jobName" -ForegroundColor White
                Write-Host "  ID: $jobId" -ForegroundColor Gray
                Write-Host "  State: $jobState" -ForegroundColor $(if ($jobState -eq "RUNNING") { "Green" } else { "Yellow" })
                
                # Get job details
                try {
                    $jobDetails = Invoke-RestMethod -Uri "$FlinkJobManagerUrl/jobs/$jobId" -Method Get -ErrorAction Stop
                    Write-Host "  Start Time: $($jobDetails.'start-time')" -ForegroundColor Gray
                    Write-Host "  Duration: $($jobDetails.duration) ms" -ForegroundColor Gray
                    
                    # Get job configuration
                    $jobConfig = Invoke-RestMethod -Uri "$FlinkJobManagerUrl/jobs/$jobId/config" -Method Get -ErrorAction SilentlyContinue
                    if ($jobConfig) {
                        Write-Host "  Configuration:" -ForegroundColor Cyan
                        $jobConfig.PSObject.Properties | ForEach-Object {
                            if ($_.Name -like "*kafka*" -or $_.Name -like "*bootstrap*") {
                                Write-Host "    $($_.Name): $($_.Value)" -ForegroundColor Yellow
                            }
                        }
                    }
                    
                    # Get job exceptions
                    $exceptions = Invoke-RestMethod -Uri "$FlinkJobManagerUrl/jobs/$jobId/exceptions" -Method Get -ErrorAction SilentlyContinue
                    if ($exceptions -and $exceptions.'all-exceptions') {
                        Write-Host "  Exceptions ($($exceptions.'all-exceptions'.Count)):" -ForegroundColor Red
                        foreach ($ex in $exceptions.'all-exceptions' | Select-Object -First 3) {
                            Write-Host "    - $($ex.exception)" -ForegroundColor Red
                            Write-Host "      Timestamp: $($ex.timestamp)" -ForegroundColor Gray
                        }
                    } else {
                        Write-Host "  No exceptions" -ForegroundColor Green
                    }
                    
                    # Get vertices (operators)
                    if ($jobDetails.vertices) {
                        Write-Host "  Vertices:" -ForegroundColor Cyan
                        foreach ($vertex in $jobDetails.vertices) {
                            Write-Host "    - $($vertex.name) [$($vertex.status)]" -ForegroundColor Gray
                            
                            # Get vertex metrics
                            $vertexId = $vertex.id
                            $metrics = Invoke-RestMethod -Uri "$FlinkJobManagerUrl/jobs/$jobId/vertices/$vertexId/metrics?get=numRecordsIn,numRecordsOut" -Method Get -ErrorAction SilentlyContinue
                            if ($metrics) {
                                foreach ($metric in $metrics) {
                                    Write-Host "      $($metric.id): $($metric.value)" -ForegroundColor Gray
                                }
                            }
                        }
                    }
                } catch {
                    Write-Host "  Could not get details: $_" -ForegroundColor Red
                }
            }
        } else {
            Write-Host "No jobs found yet..." -ForegroundColor Yellow
        }
    } catch {
        Write-Host "Could not connect to Flink: $_" -ForegroundColor Red
    }
    
    Write-Host "`nWaiting 5 seconds before next check..." -ForegroundColor Gray
    Start-Sleep -Seconds 5
}

Write-Host "`n=== Diagnostic complete ===" -ForegroundColor Cyan