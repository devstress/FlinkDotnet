#!/usr/bin/env pwsh

# Simple test to verify Aspire can start the LocalTesting project
Write-Host "🚀 Testing Aspire LocalTesting startup..." -ForegroundColor Green

# Set environment variables
$env:PATH = "$HOME/.dotnet:$env:PATH"
$env:TESTING_MODE = "true"

# Build the AppHost first
Write-Host "📦 Building LocalTesting.AppHost..." -ForegroundColor Yellow
Set-Location -Path "LocalTesting/LocalTesting.AppHost"

try {
    dotnet build --configuration Release
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ AppHost build failed" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ AppHost build succeeded" -ForegroundColor Green
    
    # Try running the AppHost for 30 seconds to see startup behavior
    Write-Host "🔍 Testing AppHost startup (30 second timeout)..." -ForegroundColor Yellow
    
    $job = Start-Job -ScriptBlock {
        param($appHostPath)
        Set-Location -Path $appHostPath
        dotnet run --configuration Release
    } -ArgumentList (Get-Location)
    
    Start-Sleep -Seconds 30
    Stop-Job -Job $job -ErrorAction SilentlyContinue
    Remove-Job -Job $job -ErrorAction SilentlyContinue
    
    Write-Host "✅ AppHost startup test completed" -ForegroundColor Green
    
} catch {
    Write-Host "❌ Error during AppHost testing: $_" -ForegroundColor Red
    exit 1
} finally {
    Set-Location -Path "../.."
}

Write-Host "✅ Aspire startup test completed successfully" -ForegroundColor Green