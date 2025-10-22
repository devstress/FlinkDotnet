#!/usr/bin/env pwsh
# Validates that the latest published packages work together
# This is the FINAL step of the release workflow after publishing to NuGet.org and Docker Hub
# Usage: ./test-published-packages.ps1 [-DockerTag "1.0.0"]

param(
    [Parameter(Mandatory=$false)]
    [string]$DockerTag = "latest"
)

$ErrorActionPreference = "Stop"

Write-Host "🧪 Testing Published Release Packages" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "This validates that published packages work together:" -ForegroundColor Yellow
Write-Host "  - NuGet.org: FlinkDotnet (latest)" -ForegroundColor Yellow
Write-Host "  - Docker Hub: flinkdotnet/jobgateway:$DockerTag" -ForegroundColor Yellow
Write-Host ""

# Step 1: Pull latest Docker image from Docker Hub
Write-Host "📦 Step 1: Pulling Docker image from Docker Hub..." -ForegroundColor Yellow
Write-Host "Pulling flinkdotnet/jobgateway:$DockerTag..."
docker pull flinkdotnet/jobgateway:$DockerTag
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to pull Docker image from Docker Hub" -ForegroundColor Red
    Write-Host "   Make sure the image exists: https://hub.docker.com/r/flinkdotnet/jobgateway" -ForegroundColor Yellow
    exit 1
}
Write-Host "✅ Docker image pulled successfully" -ForegroundColor Green

# Step 2: Tag it as latest if needed
if ($DockerTag -ne "latest") {
    Write-Host "`n📦 Step 2: Tagging image as latest for compatibility..." -ForegroundColor Yellow
    docker tag flinkdotnet/jobgateway:$DockerTag flinkdotnet/jobgateway:latest
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Failed to tag Docker image" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ Image tagged" -ForegroundColor Green
}

# Step 3: Clear NuGet cache to force download from NuGet.org
Write-Host "`n📦 Step 3: Clearing NuGet cache..." -ForegroundColor Yellow
dotnet nuget locals all --clear
Write-Host "✅ NuGet cache cleared" -ForegroundColor Green

# Step 4: Restore packages from NuGet.org
Write-Host "`n📦 Step 4: Restoring packages from NuGet.org..." -ForegroundColor Yellow
Push-Location ReleasePackagesTesting.Published

dotnet restore
if ($LASTEXITCODE -ne 0) {
    Pop-Location
    Write-Host "❌ Failed to restore packages from NuGet.org" -ForegroundColor Red
    Write-Host "   Make sure FlinkDotnet package is published: https://www.nuget.org/packages/FlinkDotnet" -ForegroundColor Yellow
    exit 1
}
Write-Host "✅ Packages restored from NuGet.org" -ForegroundColor Green

# Step 5: Build solution
Write-Host "`n📦 Step 5: Building solution..." -ForegroundColor Yellow
dotnet build --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Pop-Location
    Write-Host "❌ Build failed" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Build successful" -ForegroundColor Green

# Step 6: Run integration tests
Write-Host "`n📦 Step 6: Running integration tests..." -ForegroundColor Yellow
Write-Host "Testing that published NuGet package and Docker image work together..." -ForegroundColor Yellow
dotnet test --configuration Release --no-build --verbosity normal
$testResult = $LASTEXITCODE
Pop-Location

if ($testResult -ne 0) {
    Write-Host "`n❌ Tests FAILED - Published packages have compatibility issues!" -ForegroundColor Red
    Write-Host "   This indicates the published packages on NuGet.org and Docker Hub are incompatible" -ForegroundColor Yellow
    Write-Host "   Consider releasing a hotfix or reverting the release" -ForegroundColor Yellow
    exit 1
}

Write-Host "`n✅ All tests PASSED - Published packages work correctly together!" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "✅ Published release validation complete" -ForegroundColor Cyan
Write-Host ""
Write-Host "Validated packages:" -ForegroundColor Green
Write-Host "  - FlinkDotnet from NuGet.org (latest)" -ForegroundColor Green
Write-Host "  - flinkdotnet/jobgateway:$DockerTag from Docker Hub" -ForegroundColor Green
Write-Host ""
Write-Host "🎉 Release is CONFIRMED working!" -ForegroundColor Green

exit 0
