#!/usr/bin/env pwsh

# Day 1 Exercise 1.4: Load Testing
# Comprehensive load testing scenarios for the production streaming application

param(
    [int]$Duration = 60,        # Test duration in seconds
    [int]$Concurrency = 10,     # Number of concurrent users
    [int]$RampUp = 5,           # Ramp-up time in seconds
    [string]$BaseUrl = "http://localhost:5001",
    [switch]$Detailed,
    [switch]$SaveResults
)

Write-Host "🚀 Day 1 Exercise 1.4: Load Testing" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "📋 Test Configuration:" -ForegroundColor Yellow
Write-Host "   Duration: $Duration seconds" -ForegroundColor Gray
Write-Host "   Concurrency: $Concurrency users" -ForegroundColor Gray
Write-Host "   Ramp-up: $RampUp seconds" -ForegroundColor Gray
Write-Host "   Base URL: $BaseUrl" -ForegroundColor Gray
Write-Host ""

# Test results collection
$testResults = @{
    Configuration = @{
        Duration = $Duration
        Concurrency = $Concurrency
        RampUp = $RampUp
        BaseUrl = $BaseUrl
        StartTime = Get-Date
    }
    Scenarios = @()
    Summary = @{}
}

function Invoke-LoadTest {
    param(
        [string]$Name,
        [string]$Endpoint,
        [string]$Method = "GET",
        [hashtable]$Headers = @{},
        [string]$Body = $null,
        [int]$Requests = 100
    )
    
    Write-Host "🔥 Load Testing: $Name" -ForegroundColor Green
    Write-Host "   Endpoint: $Method $Endpoint" -ForegroundColor Gray
    Write-Host "   Requests: $Requests" -ForegroundColor Gray
    
    $url = "$BaseUrl$Endpoint"
    $responses = @()
    $errors = @()
    $startTime = Get-Date
    
    # Pre-test health check
    try {
        $healthCheck = Invoke-RestMethod -Uri "$BaseUrl/health" -TimeoutSec 5 -ErrorAction Stop
        Write-Host "   ✅ Pre-test health check passed" -ForegroundColor Green
    }
    catch {
        Write-Host "   ⚠️  Warning: Health check failed - $($_.Exception.Message)" -ForegroundColor Yellow
    }
    
    # Execute load test
    $jobs = @()
    $requestsPerJob = [math]::Ceiling($Requests / $Concurrency)
    
    for ($i = 0; $i -lt $Concurrency; $i++) {
        $job = Start-Job -ScriptBlock {
            param($Url, $Method, $Headers, $Body, $RequestCount, $JobId)
            
            $results = @()
            for ($r = 0; $r -lt $RequestCount; $r++) {
                $reqStart = Get-Date
                try {
                    if ($Method -eq "POST" -and $Body) {
                        $response = Invoke-RestMethod -Uri $Url -Method $Method -Headers $Headers -Body $Body -TimeoutSec 30
                    } else {
                        $response = Invoke-RestMethod -Uri $Url -Method $Method -Headers $Headers -TimeoutSec 30
                    }
                    $reqEnd = Get-Date
                    $results += @{
                        Success = $true
                        Duration = ($reqEnd - $reqStart).TotalMilliseconds
                        StatusCode = 200
                        JobId = $JobId
                        RequestId = $r
                    }
                }
                catch {
                    $reqEnd = Get-Date
                    $results += @{
                        Success = $false
                        Duration = ($reqEnd - $reqStart).TotalMilliseconds
                        Error = $_.Exception.Message
                        JobId = $JobId
                        RequestId = $r
                    }
                }
                
                # Small delay to prevent overwhelming
                Start-Sleep -Milliseconds (Get-Random -Minimum 10 -Maximum 100)
            }
            return $results
        } -ArgumentList $url, $Method, $Headers, $Body, $requestsPerJob, $i
        
        $jobs += $job
        
        # Ramp-up delay
        if ($i -lt $Concurrency - 1) {
            Start-Sleep -Milliseconds ([math]::Ceiling(($RampUp * 1000) / $Concurrency))
        }
    }
    
    Write-Host "   🏃‍♂️ Running $Concurrency concurrent jobs..." -ForegroundColor Blue
    
    # Wait for all jobs to complete
    $jobs | Wait-Job | Out-Null
    
    # Collect results
    foreach ($job in $jobs) {
        $jobResults = Receive-Job -Job $job
        $responses += $jobResults
        Remove-Job -Job $job
    }
    
    $endTime = Get-Date
    $totalDuration = ($endTime - $startTime).TotalSeconds
    
    # Calculate statistics
    $successfulRequests = $responses | Where-Object { $_.Success -eq $true }
    $failedRequests = $responses | Where-Object { $_.Success -eq $false }
    
    $stats = @{
        TestName = $Name
        Endpoint = $Endpoint
        TotalRequests = $responses.Count
        SuccessfulRequests = $successfulRequests.Count
        FailedRequests = $failedRequests.Count
        SuccessRate = if ($responses.Count -gt 0) { [math]::Round(($successfulRequests.Count / $responses.Count) * 100, 2) } else { 0 }
        TotalDuration = [math]::Round($totalDuration, 2)
        RequestsPerSecond = if ($totalDuration -gt 0) { [math]::Round($responses.Count / $totalDuration, 2) } else { 0 }
        AverageResponseTime = if ($successfulRequests.Count -gt 0) { [math]::Round(($successfulRequests | Measure-Object -Property Duration -Average).Average, 2) } else { 0 }
        MinResponseTime = if ($successfulRequests.Count -gt 0) { [math]::Round(($successfulRequests | Measure-Object -Property Duration -Minimum).Minimum, 2) } else { 0 }
        MaxResponseTime = if ($successfulRequests.Count -gt 0) { [math]::Round(($successfulRequests | Measure-Object -Property Duration -Maximum).Maximum, 2) } else { 0 }
        P95ResponseTime = if ($successfulRequests.Count -gt 0) { 
            $sorted = $successfulRequests | Sort-Object Duration
            $p95Index = [math]::Ceiling($sorted.Count * 0.95) - 1
            [math]::Round($sorted[$p95Index].Duration, 2)
        } else { 0 }
    }
    
    # Display results
    Write-Host "   📊 Results:" -ForegroundColor Cyan
    Write-Host "      Total Requests: $($stats.TotalRequests)" -ForegroundColor White
    Write-Host "      Successful: $($stats.SuccessfulRequests) ($($stats.SuccessRate)%)" -ForegroundColor Green
    Write-Host "      Failed: $($stats.FailedRequests)" -ForegroundColor $(if ($stats.FailedRequests -gt 0) { "Red" } else { "Green" })
    Write-Host "      Duration: $($stats.TotalDuration)s" -ForegroundColor White
    Write-Host "      Throughput: $($stats.RequestsPerSecond) req/s" -ForegroundColor White
    Write-Host "      Avg Response: $($stats.AverageResponseTime)ms" -ForegroundColor White
    Write-Host "      P95 Response: $($stats.P95ResponseTime)ms" -ForegroundColor White
    
    if ($Detailed -and $failedRequests.Count -gt 0) {
        Write-Host "   ❌ Error Details:" -ForegroundColor Red
        $failedRequests | Select-Object -First 5 | ForEach-Object {
            Write-Host "      $($_.Error)" -ForegroundColor Red
        }
    }
    
    Write-Host ""
    
    $testResults.Scenarios += $stats
    return $stats
}

# Test Scenarios

Write-Host "🎯 Starting Load Test Scenarios..." -ForegroundColor Cyan
Write-Host ""

# Scenario 1: Health Check Load Test
Invoke-LoadTest -Name "Health Check Endpoint" -Endpoint "/health" -Requests 200

# Scenario 2: Comprehensive Health Check
Invoke-LoadTest -Name "Comprehensive Health Check" -Endpoint "/health/comprehensive" -Requests 100

# Scenario 3: Metrics Endpoint
Invoke-LoadTest -Name "Metrics Endpoint" -Endpoint "/metrics" -Requests 150

# Scenario 4: Root Endpoint
Invoke-LoadTest -Name "Root Endpoint" -Endpoint "/" -Requests 100

# Scenario 5: Stream Start (POST)
Invoke-LoadTest -Name "Stream Start" -Endpoint "/stream/start" -Method "POST" -Requests 50

# Generate Summary Report
Write-Host "📋 LOAD TEST SUMMARY REPORT" -ForegroundColor Cyan
Write-Host "============================" -ForegroundColor Cyan
Write-Host ""

$overallStats = @{
    TotalTests = $testResults.Scenarios.Count
    TotalRequests = ($testResults.Scenarios | Measure-Object -Property TotalRequests -Sum).Sum
    TotalSuccessful = ($testResults.Scenarios | Measure-Object -Property SuccessfulRequests -Sum).Sum
    TotalFailed = ($testResults.Scenarios | Measure-Object -Property FailedRequests -Sum).Sum
    OverallSuccessRate = 0
    AverageResponseTime = ($testResults.Scenarios | Where-Object { $_.AverageResponseTime -gt 0 } | Measure-Object -Property AverageResponseTime -Average).Average
    HighestThroughput = ($testResults.Scenarios | Measure-Object -Property RequestsPerSecond -Maximum).Maximum
    TestDuration = [math]::Round(((Get-Date) - $testResults.Configuration.StartTime).TotalSeconds, 2)
}

if ($overallStats.TotalRequests -gt 0) {
    $overallStats.OverallSuccessRate = [math]::Round(($overallStats.TotalSuccessful / $overallStats.TotalRequests) * 100, 2)
}

Write-Host "📊 Overall Performance:" -ForegroundColor Yellow
Write-Host "   Total Requests: $($overallStats.TotalRequests)" -ForegroundColor White
Write-Host "   Success Rate: $($overallStats.OverallSuccessRate)%" -ForegroundColor $(if ($overallStats.OverallSuccessRate -ge 95) { "Green" } elseif ($overallStats.OverallSuccessRate -ge 80) { "Yellow" } else { "Red" })
Write-Host "   Average Response Time: $([math]::Round($overallStats.AverageResponseTime, 2))ms" -ForegroundColor White
Write-Host "   Peak Throughput: $($overallStats.HighestThroughput) req/s" -ForegroundColor White
Write-Host "   Test Duration: $($overallStats.TestDuration)s" -ForegroundColor White
Write-Host ""

# Performance Assessment
Write-Host "🎯 Performance Assessment:" -ForegroundColor Yellow
if ($overallStats.OverallSuccessRate -ge 95) {
    Write-Host "   ✅ EXCELLENT - Success rate above 95%" -ForegroundColor Green
} elseif ($overallStats.OverallSuccessRate -ge 80) {
    Write-Host "   ⚠️  ACCEPTABLE - Success rate 80-95%" -ForegroundColor Yellow
} else {
    Write-Host "   ❌ POOR - Success rate below 80%" -ForegroundColor Red
}

if ($overallStats.AverageResponseTime -lt 100) {
    Write-Host "   ✅ FAST - Average response time under 100ms" -ForegroundColor Green
} elseif ($overallStats.AverageResponseTime -lt 500) {
    Write-Host "   ⚠️  MODERATE - Average response time 100-500ms" -ForegroundColor Yellow
} else {
    Write-Host "   ❌ SLOW - Average response time over 500ms" -ForegroundColor Red
}

Write-Host ""

# Detailed Results Table
Write-Host "📋 Detailed Results by Scenario:" -ForegroundColor Cyan
$testResults.Scenarios | Format-Table TestName, TotalRequests, SuccessRate, RequestsPerSecond, AverageResponseTime, P95ResponseTime -AutoSize

# Save results if requested
if ($SaveResults) {
    $testResults.Summary = $overallStats
    $testResults.Configuration.EndTime = Get-Date
    
    $resultsJson = $testResults | ConvertTo-Json -Depth 4
    $filename = "load-test-results-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    $resultsJson | Out-File -FilePath $filename -Encoding UTF8
    
    Write-Host "💾 Results saved to: $filename" -ForegroundColor Green
    
    # Also create a CSV summary
    $csvFilename = "load-test-summary-$(Get-Date -Format 'yyyyMMdd-HHmmss').csv"
    $testResults.Scenarios | Export-Csv -Path $csvFilename -NoTypeInformation
    Write-Host "📊 CSV summary saved to: $csvFilename" -ForegroundColor Green
}

Write-Host ""
Write-Host "🎉 Load testing complete!" -ForegroundColor Green
Write-Host "Use these results to understand your application's performance characteristics." -ForegroundColor White

# Exit with appropriate code based on performance
if ($overallStats.OverallSuccessRate -ge 95 -and $overallStats.AverageResponseTime -lt 500) {
    exit 0
} else {
    exit 1
}