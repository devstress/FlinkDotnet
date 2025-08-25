#!/usr/bin/env pwsh

# Day 1 Exercise 1.1: Infrastructure Validation
# Complete health checks for all production services

param(
    [switch]$Detailed,
    [switch]$Json
)

Write-Host "🚀 Day 1 Exercise 1.1: Infrastructure Validation" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

$healthChecks = @()

function Test-Service {
    param(
        [string]$Name,
        [string]$Url,
        [string]$Description
    )
    
    Write-Host "🔍 Testing $Name..." -ForegroundColor Yellow
    
    try {
        $response = Invoke-RestMethod -Uri $Url -TimeoutSec 10 -ErrorAction Stop
        $status = "✅ HEALTHY"
        $color = "Green"
        
        if ($Detailed) {
            Write-Host "   URL: $Url" -ForegroundColor Gray
            Write-Host "   Response: $($response | ConvertTo-Json -Compress)" -ForegroundColor Gray
        }
    }
    catch {
        $status = "❌ UNHEALTHY"
        $color = "Red"
        $response = $_.Exception.Message
        
        Write-Host "   Error: $response" -ForegroundColor Red
    }
    
    Write-Host "   $status - $Description" -ForegroundColor $color
    Write-Host ""
    
    $script:healthChecks += @{
        Service = $Name
        Status = if ($status.Contains("✅")) { "HEALTHY" } else { "UNHEALTHY" }
        Url = $Url
        Description = $Description
        Response = $response
        Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    }
}

# 1. Test FlinkDotNet Application Health
Test-Service -Name "FlinkDotNet Application" -Url "http://localhost:5000/health/comprehensive" -Description "Main application health endpoint"

# 2. Test Flink Cluster
Test-Service -Name "Flink Cluster" -Url "http://localhost:8081/overview" -Description "Flink JobManager overview"

# 3. Test Kafka Cluster  
Test-Service -Name "Kafka Cluster" -Url "http://localhost:8082/api/clusters/local-testing-cluster/brokers" -Description "Kafka REST API brokers"

# 4. Test Temporal Service
Test-Service -Name "Temporal Service" -Url "http://localhost:8084/api/v1/namespaces" -Description "Temporal workflow namespaces"

# 5. Test Prometheus
Test-Service -Name "Prometheus" -Url "http://localhost:9090/api/v1/targets" -Description "Prometheus targets endpoint"

# 6. Test Grafana
Test-Service -Name "Grafana" -Url "http://localhost:3000/api/health" -Description "Grafana health endpoint"

# Summary Report
Write-Host "📊 HEALTH CHECK SUMMARY" -ForegroundColor Cyan
Write-Host "========================" -ForegroundColor Cyan

$healthy = ($healthChecks | Where-Object { $_.Status -eq "HEALTHY" }).Count
$total = $healthChecks.Count

Write-Host "Services Healthy: $healthy/$total" -ForegroundColor $(if ($healthy -eq $total) { "Green" } else { "Yellow" })
Write-Host "Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host ""

# Detailed Results Table
$healthChecks | Format-Table Service, Status, Description -AutoSize

if ($Json) {
    $jsonResult = @{
        Summary = @{
            TotalServices = $total
            HealthyServices = $healthy
            HealthPercentage = [math]::Round(($healthy / $total) * 100, 2)
            Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        }
        Services = $healthChecks
    } | ConvertTo-Json -Depth 3
    
    Write-Host "📄 JSON REPORT" -ForegroundColor Cyan
    Write-Host $jsonResult
    
    # Save to file
    $jsonResult | Out-File -FilePath "health-check-results.json" -Encoding UTF8
    Write-Host "💾 Results saved to health-check-results.json" -ForegroundColor Green
}

# Exit with appropriate code
if ($healthy -eq $total) {
    Write-Host "🎉 All services are healthy! Ready for Day 1 exercises." -ForegroundColor Green
    exit 0
} else {
    Write-Host "⚠️  Some services are unhealthy. Please check the LocalTesting environment." -ForegroundColor Yellow
    exit 1
}