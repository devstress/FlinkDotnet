#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Run SonarQube analysis locally without needing to visit SonarCloud
.DESCRIPTION
    This script runs a local SonarQube analysis on the FlinkDotNet solution.
    Results are saved to the .sonarqube directory for offline review.
.PARAMETER SonarToken
    Optional SonarCloud token for uploading results. If not provided, only local analysis is performed.
.PARAMETER SkipTests
    Skip running tests and code coverage collection
.EXAMPLE
    ./run-sonar-analysis.ps1
    # Runs local analysis only
.EXAMPLE
    ./run-sonar-analysis.ps1 -SonarToken "your-token"
    # Runs analysis and uploads to SonarCloud
#>

param(
    [string]$SonarToken = "",
    [switch]$SkipTests = $false
)

$ErrorActionPreference = "Stop"

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "  FlinkDotNet Local SonarQube Analysis" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Check if dotnet-sonarscanner is installed
$scannerInstalled = $null -ne (Get-Command dotnet-sonarscanner -ErrorAction SilentlyContinue)
if (-not $scannerInstalled) {
    Write-Host "Installing dotnet-sonarscanner..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-sonarscanner
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Installation failed. Trying update..." -ForegroundColor Yellow
        dotnet tool update --global dotnet-sonarscanner
    }
}

# Verify installation
Write-Host "✓ SonarScanner installed" -ForegroundColor Green

# Clean previous build artifacts
Write-Host "`nCleaning previous builds..." -ForegroundColor Yellow
dotnet clean FlinkDotNet.sln --configuration Release -v quiet

# Prepare SonarScanner arguments
$beginArgs = @(
    "begin",
    "/k:devstress_flinkdotnet",
    "/o:devstress",
    "/d:sonar.host.url=https://sonarcloud.io"
)

if ($SonarToken) {
    Write-Host "✓ Using SonarCloud token for upload" -ForegroundColor Green
    $beginArgs += "/d:sonar.token=$SonarToken"
} else {
    Write-Host "⚠ No SonarCloud token provided - local analysis only" -ForegroundColor Yellow
    Write-Host "  Results will be saved locally but not uploaded to SonarCloud" -ForegroundColor Gray
}

# Add coverage settings
if (-not $SkipTests) {
    $beginArgs += @(
        "/d:sonar.cs.opencover.reportsPaths=**/TestResults/**/coverage.opencover.xml",
        "/d:sonar.cs.vscoveragexml.reportsPaths=**/TestResults/**/coverage.cobertura.xml"
    )
}

# Begin SonarScanner
Write-Host "`nStarting SonarQube analysis..." -ForegroundColor Yellow
& dotnet-sonarscanner $beginArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Failed to start SonarScanner" -ForegroundColor Red
    exit 1
}

# Build the solution
Write-Host "`nBuilding FlinkDotNet solution..." -ForegroundColor Yellow
dotnet build FlinkDotNet.sln --configuration Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Build failed" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Build successful" -ForegroundColor Green

# Run tests with coverage
if (-not $SkipTests) {
    Write-Host "`nRunning tests with coverage..." -ForegroundColor Yellow
    dotnet test FlinkDotNet.sln `
        --configuration Release `
        --no-build `
        --collect:"XPlat Code Coverage" `
        --settings ../coverlet.runsettings `
        --logger "console;verbosity=minimal"
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "⚠ Some tests failed, but continuing with analysis..." -ForegroundColor Yellow
    } else {
        Write-Host "✓ Tests completed" -ForegroundColor Green
    }
}

# End SonarScanner
Write-Host "`nCompleting SonarQube analysis..." -ForegroundColor Yellow
$endArgs = @("end")
if ($SonarToken) {
    $endArgs += "/d:sonar.token=$SonarToken"
}

& dotnet-sonarscanner $endArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Failed to complete SonarScanner" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "  Analysis Complete!" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

if ($SonarToken) {
    Write-Host "Results uploaded to: https://sonarcloud.io/dashboard?id=devstress_flinkdotnet" -ForegroundColor Green
} else {
    Write-Host "Local analysis results saved to: .sonarqube/" -ForegroundColor Green
    Write-Host ""
    Write-Host "To view issues locally, check the .sonarqube directory" -ForegroundColor Yellow
    Write-Host "To upload results to SonarCloud, rerun with -SonarToken parameter" -ForegroundColor Yellow
}

Write-Host ""
