#!/usr/bin/env pwsh
# Rebuild FlinkIRRunner JAR and deploy to Gateway project
# This script rebuilds the Java FlinkIRRunner and copies it to the Gateway project

param(
    [switch]$SkipTests = $true
)

$ErrorActionPreference = "Stop"

Write-Host "========================================"
Write-Host "Rebuilding FlinkIRRunner JAR"
Write-Host "========================================"

# Get repository root
$repoRoot = Split-Path -Parent $PSScriptRoot

# Build the Flink.JobGateway project, which will rebuild the JAR via Maven
Write-Host ""
Write-Host "Building Flink.JobGateway project (triggers Maven build)..."
$gatewayProject = Join-Path $repoRoot "FlinkDotNet\Flink.JobGateway\Flink.JobGateway.csproj"

try {
    $buildArgs = @("build", $gatewayProject, "--configuration", "Release")
    if ($SkipTests) {
        $buildArgs += "/p:SkipTests=true"
    }
    
    Write-Host "Running: dotnet $($buildArgs -join ' ')"
    & dotnet @buildArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed with exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }
    
    Write-Host "Flink.JobGateway build completed successfully"
    
    # Verify JAR was created
    $jarPath = Join-Path $repoRoot "FlinkDotNet\Flink.JobGateway\bin\Release\net9.0\flink-ir-runner-java17.jar"
    if (Test-Path $jarPath) {
        $jarInfo = Get-Item $jarPath
        Write-Host ""
        Write-Host "JAR Information:"
        Write-Host "  Path: $jarPath"
        Write-Host "  Size: $($jarInfo.Length / 1KB) KB"
        Write-Host "  Modified: $($jarInfo.LastWriteTime)"
    } else {
        Write-Host "Warning: JAR not found at expected location: $jarPath"
    }
    
} catch {
    Write-Host "Error during build: $_"
    Write-Host $_.Exception.Message
    exit 1
}

Write-Host ""
Write-Host "========================================"
Write-Host "FlinkIRRunner JAR rebuild complete"
Write-Host "========================================"
Write-Host ""
Write-Host "Next steps:"
Write-Host "   1. Stop any running LocalTesting AppHost"
Write-Host "   2. Restart your tests - the new JAR will be deployed automatically"