#!/usr/bin/env pwsh
# Validates that the latest published packages work together
# This ensures the latest release on NuGet.org and Docker Hub are compatible
# Usage: ./validate-latest-release.ps1

param(
    [Parameter(Mandatory=$false)]
    [string]$DockerTag = "latest"
)

$ErrorActionPreference = "Stop"

Write-Host "🧪 Validating Latest Release Packages" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "This validates that the latest published packages work together:" -ForegroundColor Yellow
Write-Host "  - NuGet.org: FlinkDotnet (latest)" -ForegroundColor Yellow
Write-Host "  - Docker Hub: flinkdotnet/jobgateway:$DockerTag" -ForegroundColor Yellow
Write-Host ""

# Step 1: Remove any local NuGet feed
Write-Host "📦 Step 1: Cleaning up local NuGet sources..." -ForegroundColor Yellow
dotnet nuget remove source LocalFeed 2>$null
Write-Host "✅ Local sources cleaned" -ForegroundColor Green

# Step 2: Ensure we're using the latest from NuGet.org
Write-Host "`n📦 Step 2: Configuring to use NuGet.org..." -ForegroundColor Yellow
# Update the csproj to NOT use conditional reference
$csprojPath = "./ReleasePackagesTesting/ReleasePackagesTesting.IntegrationTests/ReleasePackagesTesting.IntegrationTests.csproj"
$csprojContent = Get-Content $csprojPath -Raw

# Temporarily enable the package reference for this test
$updatedContent = $csprojContent -replace 'PackageReference Include="FlinkDotnet" Version="\*" Condition="\$\(UseReleasePackages\)" == ''true''"', 'PackageReference Include="FlinkDotnet" Version="*"'

# Create a temporary csproj for this test
$tempCsprojPath = "./ReleasePackagesTesting/ReleasePackagesTesting.IntegrationTests/ReleasePackagesTesting.IntegrationTests.csproj.temp"
$updatedContent | Set-Content $tempCsprojPath
Copy-Item $tempCsprojPath $csprojPath -Force
Remove-Item $tempCsprojPath

Write-Host "✅ Configured to use NuGet.org" -ForegroundColor Green

# Step 3: Pull latest Docker image
Write-Host "`n📦 Step 3: Pulling latest Docker image from Docker Hub..." -ForegroundColor Yellow
Write-Host "Pulling flinkdotnet/jobgateway:$DockerTag..."
docker pull flinkdotnet/jobgateway:$DockerTag
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to pull Docker image" -ForegroundColor Red
    Write-Host "   Make sure the image exists on Docker Hub" -ForegroundColor Yellow
    exit 1
}
Write-Host "✅ Docker image pulled successfully" -ForegroundColor Green

# Step 4: Tag it as latest for the test
if ($DockerTag -ne "latest") {
    Write-Host "`n📦 Tagging image as latest for compatibility..." -ForegroundColor Yellow
    docker tag flinkdotnet/jobgateway:$DockerTag flinkdotnet/jobgateway:latest
    Write-Host "✅ Image tagged" -ForegroundColor Green
}

# Step 5: Restore packages from NuGet.org
Write-Host "`n📦 Step 4: Restoring packages from NuGet.org..." -ForegroundColor Yellow
Push-Location ReleasePackagesTesting

# Clear local package cache to force download from NuGet.org
Write-Host "Clearing local package cache for FlinkDotnet..."
dotnet nuget locals all --clear

dotnet restore
if ($LASTEXITCODE -ne 0) {
    Pop-Location
    Write-Host "❌ Failed to restore packages from NuGet.org" -ForegroundColor Red
    Write-Host "   Make sure FlinkDotnet package is published on NuGet.org" -ForegroundColor Yellow
    exit 1
}
Write-Host "✅ Packages restored from NuGet.org" -ForegroundColor Green

# Step 6: Build solution
Write-Host "`n📦 Step 5: Building solution..." -ForegroundColor Yellow
dotnet build --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Pop-Location
    Write-Host "❌ Build failed" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Build successful" -ForegroundColor Green

# Step 7: Run tests
Write-Host "`n📦 Step 6: Running integration tests..." -ForegroundColor Yellow
Write-Host "Testing that NuGet package and Docker image work together..." -ForegroundColor Yellow
dotnet test --configuration Release --no-build --verbosity normal
$testResult = $LASTEXITCODE
Pop-Location

if ($testResult -ne 0) {
    Write-Host "`n❌ Tests FAILED - Latest release packages have compatibility issues!" -ForegroundColor Red
    Write-Host "   This indicates the published packages on NuGet.org and Docker Hub are incompatible" -ForegroundColor Yellow
    exit 1
}

Write-Host "`n✅ All tests PASSED - Latest release packages work correctly together!" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "✅ Latest release validation complete" -ForegroundColor Cyan
Write-Host ""
Write-Host "Validated packages:" -ForegroundColor Green
Write-Host "  - FlinkDotnet from NuGet.org (latest)" -ForegroundColor Green
Write-Host "  - flinkdotnet/jobgateway:$DockerTag from Docker Hub" -ForegroundColor Green

exit 0
