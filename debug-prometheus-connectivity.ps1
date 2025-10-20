#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Debug Prometheus connectivity with Flink JobManager, TaskManager, and Gateway
.DESCRIPTION
    Comprehensive debugging script to verify Prometheus scraping targets and metric endpoints
#>

param(
    [string]$PrometheusUrl = "http://localhost:9090"
)

Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host "  Prometheus Connectivity Debugger" -ForegroundColor Cyan
Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host ""

# Color functions
function Write-Success { param($msg) Write-Host "[OK] $msg" -ForegroundColor Green }
function Write-Failure { param($msg) Write-Host "[FAIL] $msg" -ForegroundColor Red }
function Write-Info { param($msg) Write-Host "[INFO] $msg" -ForegroundColor Cyan }
function Write-Warning { param($msg) Write-Host "[WARN] $msg" -ForegroundColor Yellow }

# Test HTTP endpoint
function Test-HttpEndpoint {
    param(
        [string]$Url,
        [string]$Name
    )
    
    try {
        $response = Invoke-WebRequest -Uri $Url -Method Get -TimeoutSec 5 -UseBasicParsing
        if ($response.StatusCode -eq 200) {
            Write-Success "$Name is accessible (HTTP $($response.StatusCode))"
            return $true
        } else {
            Write-Warning "$Name returned HTTP $($response.StatusCode)"
            return $false
        }
    } catch {
        Write-Failure "$Name is NOT accessible: $($_.Exception.Message)"
        return $false
    }
}

# Test Prometheus targets
function Test-PrometheusTargets {
    param([string]$Url)
    
    Write-Info "Checking Prometheus targets..."
    
    try {
        $targetsUrl = "$Url/api/v1/targets"
        $response = Invoke-RestMethod -Uri $targetsUrl -Method Get -TimeoutSec 10
        
        if ($response.status -eq "success") {
            $activeTargets = $response.data.activeTargets
            
            Write-Host ""
            Write-Host "Prometheus Targets Status:" -ForegroundColor Yellow
            Write-Host "-----------------------------------------------------------" -ForegroundColor Gray
            
            $upCount = 0
            $downCount = 0
            
            foreach ($target in $activeTargets) {
                $job = $target.labels.job
                $instance = $target.labels.instance
                $health = $target.health
                $lastError = $target.lastError
                
                if ($health -eq "up") {
                    Write-Success "Job: $job | Instance: $instance | Health: UP"
                    $upCount++
                } else {
                    Write-Failure "Job: $job | Instance: $instance | Health: DOWN"
                    if ($lastError) {
                        Write-Host "   Error: $lastError" -ForegroundColor Red
                    }
                    $downCount++
                }
            }
            
            Write-Host ""
            Write-Host "Summary: $upCount UP, $downCount DOWN" -ForegroundColor $(if ($downCount -eq 0) { "Green" } else { "Yellow" })
            
            return @{
                Success = $true
                UpCount = $upCount
                DownCount = $downCount
                Targets = $activeTargets
            }
        } else {
            Write-Failure "Prometheus targets API returned status: $($response.status)"
            return @{ Success = $false }
        }
    } catch {
        Write-Failure "Failed to query Prometheus targets: $($_.Exception.Message)"
        return @{ Success = $false }
    }
}

# Test individual metric endpoint
function Test-MetricEndpoint {
    param(
        [string]$Url,
        [string]$Name,
        [string]$ExpectedMetricPattern
    )
    
    Write-Info "Testing $Name endpoint: $Url"
    
    try {
        $response = Invoke-WebRequest -Uri $Url -Method Get -TimeoutSec 10 -UseBasicParsing
        
        if ($response.StatusCode -eq 200) {
            $content = $response.Content
            $contentLength = $content.Length
            
            if ($contentLength -gt 0) {
                $sizeKB = [math]::Round($contentLength / 1024, 2)
                Write-Success "$Name metrics endpoint is working ($sizeKB KB)"
                
                # Check for expected metric pattern
                if ($ExpectedMetricPattern -and $content -match $ExpectedMetricPattern) {
                    Write-Success "Found expected metric pattern: $ExpectedMetricPattern"
                } elseif ($ExpectedMetricPattern) {
                    Write-Warning "Expected metric pattern not found: $ExpectedMetricPattern"
                }
                
                # Show sample metrics
                $lines = $content -split "`n" | Where-Object { $_ -and $_ -notmatch "^#" } | Select-Object -First 5
                if ($lines) {
                    Write-Host "   Sample metrics:" -ForegroundColor Gray
                    foreach ($line in $lines) {
                        Write-Host "   $line" -ForegroundColor DarkGray
                    }
                }
                
                return $true
            } else {
                Write-Failure "$Name metrics endpoint returned empty response"
                return $false
            }
        } else {
            Write-Failure "$Name metrics endpoint returned HTTP $($response.StatusCode)"
            return $false
        }
    } catch {
        Write-Failure "$Name metrics endpoint failed: $($_.Exception.Message)"
        return $false
    }
}

# Query Prometheus for specific metric
function Query-PrometheusMetric {
    param(
        [string]$Url,
        [string]$Metric,
        [string]$Name
    )
    
    Write-Info "Querying Prometheus for metric: $Metric"
    
    try {
        $queryUrl = "$Url/api/v1/query?query=$Metric"
        $response = Invoke-RestMethod -Uri $queryUrl -Method Get -TimeoutSec 10
        
        if ($response.status -eq "success") {
            $results = $response.data.result
            
            if ($results -and $results.Count -gt 0) {
                Write-Success "$Name metric found: $Metric ($($results.Count) result(s))"
                
                foreach ($result in $results) {
                    $value = $result.value[1]
                    $labels = $result.metric | ConvertTo-Json -Compress
                    Write-Host "   Value: $value | Labels: $labels" -ForegroundColor Gray
                }
                
                return $true
            } else {
                Write-Warning "$Name metric returned no results: $Metric"
                return $false
            }
        } else {
            Write-Failure "Prometheus query failed with status: $($response.status)"
            return $false
        }
    } catch {
        Write-Failure "Failed to query metric: $($_.Exception.Message)"
        return $false
    }
}

# Main execution
Write-Host "Starting Prometheus connectivity diagnostics..." -ForegroundColor Cyan
Write-Host ""

# Step 1: Test Prometheus itself
Write-Host "========================================================================" -ForegroundColor Gray
Write-Host "STEP 1: Testing Prometheus Server" -ForegroundColor Yellow
Write-Host "========================================================================" -ForegroundColor Gray
$prometheusOk = Test-HttpEndpoint -Url $PrometheusUrl -Name "Prometheus Server"
Write-Host ""

if (-not $prometheusOk) {
    Write-Failure "Prometheus server is not accessible. Please ensure LocalTesting is running."
    exit 1
}

# Step 2: Check Prometheus targets
Write-Host "========================================================================" -ForegroundColor Gray
Write-Host "STEP 2: Checking Prometheus Targets Status" -ForegroundColor Yellow
Write-Host "========================================================================" -ForegroundColor Gray
$targetsResult = Test-PrometheusTargets -Url $PrometheusUrl
Write-Host ""

# Step 3: Test direct metric endpoints
Write-Host "========================================================================" -ForegroundColor Gray
Write-Host "STEP 3: Testing Direct Metric Endpoints" -ForegroundColor Yellow
Write-Host "========================================================================" -ForegroundColor Gray

$endpoints = @(
    @{ Url = "http://localhost:9250/metrics"; Name = "Flink JobManager"; Pattern = "flink_jobmanager" }
    @{ Url = "http://localhost:9251/metrics"; Name = "Flink TaskManager"; Pattern = "flink_taskmanager" }
    @{ Url = "http://localhost:8080/metrics"; Name = "FlinkDotNet Gateway"; Pattern = "flinkdotnet_gateway" }
)

$endpointResults = @()
foreach ($endpoint in $endpoints) {
    $result = Test-MetricEndpoint -Url $endpoint.Url -Name $endpoint.Name -ExpectedMetricPattern $endpoint.Pattern
    $endpointResults += $result
    Write-Host ""
}

# Step 4: Query Prometheus for key metrics
Write-Host "========================================================================" -ForegroundColor Gray
Write-Host "STEP 4: Querying Key Metrics from Prometheus" -ForegroundColor Yellow
Write-Host "========================================================================" -ForegroundColor Gray

$metrics = @(
    @{ Metric = "flink_jobmanager_numRegisteredTaskManagers"; Name = "Flink JobManager" }
    @{ Metric = "flink_taskmanager_Status_JVM_Memory_Heap_Used"; Name = "Flink TaskManager" }
    @{ Metric = "flinkdotnet_gateway_jobs_submitted_total"; Name = "FlinkDotNet Gateway" }
)

$metricResults = @()
foreach ($metric in $metrics) {
    $result = Query-PrometheusMetric -Url $PrometheusUrl -Metric $metric.Metric -Name $metric.Name
    $metricResults += $result
    Write-Host ""
}

# Final summary
Write-Host "========================================================================" -ForegroundColor Gray
Write-Host "DIAGNOSTIC SUMMARY" -ForegroundColor Yellow
Write-Host "========================================================================" -ForegroundColor Gray

Write-Host ""
Write-Host "Prometheus Server: $(if ($prometheusOk) { '[OK] UP' } else { '[FAIL] DOWN' })" -ForegroundColor $(if ($prometheusOk) { 'Green' } else { 'Red' })

if ($targetsResult.Success) {
    Write-Host "Prometheus Targets: $($targetsResult.UpCount) UP, $($targetsResult.DownCount) DOWN" -ForegroundColor $(if ($targetsResult.DownCount -eq 0) { 'Green' } else { 'Yellow' })
}

Write-Host ""
Write-Host "Metric Endpoints:" -ForegroundColor Cyan
Write-Host "  JobManager:  $(if ($endpointResults[0]) { '[OK] Working' } else { '[FAIL] Failed' })" -ForegroundColor $(if ($endpointResults[0]) { 'Green' } else { 'Red' })
Write-Host "  TaskManager: $(if ($endpointResults[1]) { '[OK] Working' } else { '[FAIL] Failed' })" -ForegroundColor $(if ($endpointResults[1]) { 'Green' } else { 'Red' })
Write-Host "  Gateway:     $(if ($endpointResults[2]) { '[OK] Working' } else { '[FAIL] Failed' })" -ForegroundColor $(if ($endpointResults[2]) { 'Green' } else { 'Red' })

Write-Host ""
Write-Host "Prometheus Queries:" -ForegroundColor Cyan
Write-Host "  JobManager Metrics:  $(if ($metricResults[0]) { '[OK] Available' } else { '[FAIL] Missing' })" -ForegroundColor $(if ($metricResults[0]) { 'Green' } else { 'Red' })
Write-Host "  TaskManager Metrics: $(if ($metricResults[1]) { '[OK] Available' } else { '[FAIL] Missing' })" -ForegroundColor $(if ($metricResults[1]) { 'Green' } else { 'Red' })
Write-Host "  Gateway Metrics:     $(if ($metricResults[2]) { '[OK] Available' } else { '[FAIL] Missing' })" -ForegroundColor $(if ($metricResults[2]) { 'Green' } else { 'Red' })

Write-Host ""
Write-Host "================================================================================================" -ForegroundColor Cyan

# Recommendations
$hasIssues = -not ($endpointResults[0] -and $endpointResults[1] -and $endpointResults[2])

if ($hasIssues) {
    Write-Host ""
    Write-Host "RECOMMENDATIONS:" -ForegroundColor Yellow
    Write-Host ""
    
    if (-not $endpointResults[1]) {
        Write-Host "TaskManager Metrics Issue:" -ForegroundColor Red
        Write-Host "  1. Check TaskManager logs for Prometheus reporter initialization" -ForegroundColor Gray
        Write-Host "  2. Verify config.yaml has Prometheus reporter settings" -ForegroundColor Gray
        Write-Host "  3. Ensure port 9251 is exposed in Aspire configuration" -ForegroundColor Gray
        Write-Host ""
    }
    
    if (-not $endpointResults[2]) {
        Write-Host "Gateway Metrics Issue:" -ForegroundColor Red
        Write-Host "  1. Install prometheus-net.AspNetCore NuGet package" -ForegroundColor Gray
        Write-Host "  2. Add app.MapMetrics call in Program.cs" -ForegroundColor Gray
        Write-Host "  3. Verify port 8080 is exposed" -ForegroundColor Gray
        Write-Host ""
    }
} else {
    Write-Host ""
    Write-Success "All Prometheus integrations are working correctly"
}