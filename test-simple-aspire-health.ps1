#!/usr/bin/env pwsh

# Simple test to verify Aspire health checks work properly
Write-Host "🚀 Testing Aspire health check pattern..." -ForegroundColor Green

# Set environment variables
$env:PATH = "$HOME/.dotnet:$env:PATH"
$env:TESTING_MODE = "true"

Set-Location -Path "LocalTesting"

try {
    Write-Host "🧪 Running integration test with health check focus..." -ForegroundColor Yellow
    
    # Run only the integration test that tests Aspire health pattern
    dotnet test LocalTesting.IntegrationTests --configuration Release --verbosity normal --filter "TestCategory=observability"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Aspire integration test passed!" -ForegroundColor Green
        Write-Host "✅ Health checks are working within expected timeframe" -ForegroundColor Green
    } else {
        Write-Host "❌ Aspire integration test failed" -ForegroundColor Red
        Write-Host "🔍 This indicates either timeout or infrastructure issues" -ForegroundColor Yellow
        exit 1
    }
    
} catch {
    Write-Host "❌ Error during test execution: $_" -ForegroundColor Red
    exit 1
} finally {
    Set-Location -Path ".."
}

Write-Host "✅ Aspire health check test completed" -ForegroundColor Green