#!/usr/bin/env pwsh
# Simple Docker Compose startup script for CI
# Just starts services and waits for them to be ready

param(
    [switch]$StopOnly,
    [int]$TimeoutMinutes = 10
)

# Colors for output
$Green = "Green"
$Red = "Red"
$Yellow = "Yellow"
$Cyan = "Cyan"

function Write-Step {
    param([string]$Message, [string]$Color = $Yellow)
    Write-Host "🔧 $Message" -ForegroundColor $Color
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor $Green
}

function Write-Error {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor $Red
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ️ $Message" -ForegroundColor $Cyan
}

if ($StopOnly) {
    Write-Step "Stopping Docker Compose services..."
    Push-Location LocalTesting
    try {
        docker compose -f docker-compose.ci.yml down -v 2>&1 | Out-Null
        Write-Success "Docker Compose services stopped"
    }
    catch {
        Write-Error "Error stopping services: $($_.Exception.Message)"
    }
    finally {
        Pop-Location
    }
    exit 0
}

try {
    # Navigate to LocalTesting directory
    Write-Step "Starting Docker Compose services..."
    Push-Location LocalTesting
    
    # Start Docker Compose services
    $composeOutput = docker compose -f docker-compose.ci.yml up -d 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Docker Compose services started"
    } else {
        Write-Error "Docker Compose startup failed:"
        Write-Host $composeOutput -ForegroundColor $Red
        exit 1
    }
    
    # Wait for core services to become healthy
    Write-Step "Waiting for core services to become healthy..."
    $maxWait = $TimeoutMinutes * 60
    $waited = 0
    $checkInterval = 10
    
    while ($waited -lt $maxWait) {
        Write-Host "." -NoNewline -ForegroundColor $Green
        
        # Check critical services (only ones with reliable health checks)
        $redis = docker compose -f docker-compose.ci.yml ps redis --format "{{.Health}}" 2>$null
        $kafka = docker compose -f docker-compose.ci.yml ps kafka-broker --format "{{.Health}}" 2>$null
        $postgres = docker compose -f docker-compose.ci.yml ps temporal-postgres --format "{{.Health}}" 2>$null
        $flink = docker compose -f docker-compose.ci.yml ps flink-jobmanager --format "{{.Health}}" 2>$null
        
        $healthyCount = 0
        if ($redis -eq "healthy") { $healthyCount++ }
        if ($kafka -eq "healthy") { $healthyCount++ }
        if ($postgres -eq "healthy") { $healthyCount++ }
        if ($flink -eq "healthy") { $healthyCount++ }
        
        # Only require 3 out of 4 to be healthy (temporal-server takes longer)
        if ($healthyCount -ge 3) {
            Write-Host ""
            Write-Success "Core services are healthy ($healthyCount/4: Redis, Kafka, PostgreSQL, Flink)"
            break
        }
        
        Start-Sleep -Seconds $checkInterval
        $waited += $checkInterval
    }
    
    if ($waited -ge $maxWait) {
        Write-Host ""
        Write-Error "Timeout waiting for services to become healthy"
        
        # Show service status for debugging
        Write-Info "Service status:"
        docker compose -f docker-compose.ci.yml ps
        exit 1
    }
    
    Write-Success "All core services are ready"
    exit 0
    
} catch {
    Write-Error "Failed to start services: $($_.Exception.Message)"
    exit 1
} finally {
    Pop-Location
}