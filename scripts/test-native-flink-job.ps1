#!/usr/bin/env pwsh
# Test script to validate Aspire Flink + Kafka infrastructure with native Flink job

param(
    [switch]$SkipBuild,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Native Flink Job Infrastructure Test" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Build the JAR with native job
if (-not $SkipBuild) {
    Write-Host "Step 1: Building FlinkIRRunner JAR..." -ForegroundColor Yellow
    Push-Location FlinkIRRunner
    try {
        & mvn clean package -DskipTests
        if ($LASTEXITCODE -ne 0) {
            throw "Maven build failed with exit code $LASTEXITCODE"
        }
        Write-Host "✓ JAR built successfully" -ForegroundColor Green
    }
    finally {
        Pop-Location
    }
} else {
    Write-Host "Step 1: Skipping build (using existing JAR)" -ForegroundColor Yellow
}

# Step 2: Verify JAR exists
$jarPath = "FlinkIRRunner/target/flink-ir-runner.jar"
if (-not (Test-Path $jarPath)) {
    Write-Host "✗ JAR not found at: $jarPath" -ForegroundColor Red
    Write-Host "  Run without -SkipBuild to build the JAR" -ForegroundColor Red
    exit 1
}
Write-Host "✓ JAR found: $jarPath" -ForegroundColor Green
Write-Host ""

# Step 3: Run the native Flink job test
Write-Host "Step 2: Running Native Flink Job Test..." -ForegroundColor Yellow
Push-Location LocalTesting/LocalTesting.IntegrationTests
try {
    $testArgs = @(
        "test"
        "--filter", "FullyQualifiedName~FlinkNativeKafkaTest"
        "--logger", "console;verbosity=detailed"
        "--configuration", "Release"
    )
    
    if ($Verbose) {
        $testArgs += @("--", "NUnit.Verbosity=2")
    }
    
    & dotnet @testArgs
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "✓ INFRASTRUCTURE VALIDATED" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "The Aspire Flink + Kafka setup is working!" -ForegroundColor Green
        Write-Host "Native Java Flink jobs can read from and write to Kafka." -ForegroundColor Green
        Write-Host ""
        Write-Host "Next step: Compare Gateway job definition with native job" -ForegroundColor Yellow
    } else {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Red
        Write-Host "✗ INFRASTRUCTURE TEST FAILED" -ForegroundColor Red
        Write-Host "========================================" -ForegroundColor Red
        Write-Host "The Aspire Flink + Kafka setup has issues." -ForegroundColor Red
        Write-Host "Fix infrastructure before testing Gateway." -ForegroundColor Red
        exit 1
    }
}
finally {
    Pop-Location
}