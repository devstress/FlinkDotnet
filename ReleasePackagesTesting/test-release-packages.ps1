#!/usr/bin/env pwsh
# Test script for validating release packages before publishing
# Usage: ./test-release-packages.ps1 -Version 1.0.0

param(
    [Parameter(Mandatory=$true)]
    [string]$Version
)

$ErrorActionPreference = "Stop"

Write-Host "🧪 Testing Release Packages for version $Version" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

# Step 1: Load Docker image
Write-Host "`n📦 Step 1: Loading Docker image..." -ForegroundColor Yellow
$dockerImagePath = "./docker/jobgateway-$Version.tar.gz"
if (-not (Test-Path $dockerImagePath)) {
    Write-Host "❌ Docker image not found at: $dockerImagePath" -ForegroundColor Red
    exit 1
}

Write-Host "Loading Docker image from $dockerImagePath..."
gunzip -c $dockerImagePath | docker load
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to load Docker image" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Docker image loaded successfully" -ForegroundColor Green

# Step 2: Add local NuGet feed
Write-Host "`n📦 Step 2: Adding local NuGet feed..." -ForegroundColor Yellow
$nugetPath = "./packages"
if (-not (Test-Path $nugetPath)) {
    Write-Host "❌ NuGet packages directory not found at: $nugetPath" -ForegroundColor Red
    exit 1
}

# Remove existing LocalFeed if it exists
dotnet nuget remove source LocalFeed 2>$null
dotnet nuget add source $nugetPath --name LocalFeed
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to add local NuGet feed" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Local NuGet feed added successfully" -ForegroundColor Green

# Step 3: Update package version in test project
Write-Host "`n📦 Step 3: Updating package version to $Version..." -ForegroundColor Yellow
$csprojPath = "./ReleasePackagesTesting/ReleasePackagesTesting.IntegrationTests/ReleasePackagesTesting.IntegrationTests.csproj"
(Get-Content $csprojPath) -replace 'PackageReference Include="FlinkDotnet" Version="\*"', "PackageReference Include=`"FlinkDotnet`" Version=`"$Version`"" | Set-Content $csprojPath
Write-Host "✅ Package version updated" -ForegroundColor Green

# Step 4: Restore packages
Write-Host "`n📦 Step 4: Restoring packages..." -ForegroundColor Yellow
Push-Location ReleasePackagesTesting
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Pop-Location
    Write-Host "❌ Failed to restore packages" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Packages restored successfully" -ForegroundColor Green

# Step 5: Build solution
Write-Host "`n📦 Step 5: Building solution..." -ForegroundColor Yellow
dotnet build --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Pop-Location
    Write-Host "❌ Build failed" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Build successful" -ForegroundColor Green

# Step 6: Run tests
Write-Host "`n📦 Step 6: Running integration tests..." -ForegroundColor Yellow
dotnet test --configuration Release --no-build --verbosity normal
$testResult = $LASTEXITCODE
Pop-Location

if ($testResult -ne 0) {
    Write-Host "`n❌ Tests FAILED - Release packages have issues!" -ForegroundColor Red
    exit 1
}

Write-Host "`n✅ All tests PASSED - Release packages are ready for publishing!" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "✅ Release validation complete for version $Version" -ForegroundColor Cyan

# Cleanup: Remove local NuGet feed
dotnet nuget remove source LocalFeed 2>$null

exit 0
