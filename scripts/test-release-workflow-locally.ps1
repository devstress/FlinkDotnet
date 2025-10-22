#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Test release workflows locally with mocked NuGet and Docker registries.

.DESCRIPTION
    This script simulates the release workflow process locally:
    1. Builds FlinkDotNet packages
    2. Builds Docker image
    3. Tests with local packages (pre-release validation)
    4. Simulates publishing to local registries
    5. Tests with "published" packages (post-release validation)

.PARAMETER Version
    Version to use for testing (default: 99.99.99-test)

.PARAMETER SkipBuild
    Skip building packages and Docker image

.PARAMETER SkipPreRelease
    Skip pre-release validation tests

.PARAMETER SkipPostRelease
    Skip post-release validation tests

.EXAMPLE
    ./test-release-workflow-locally.ps1
    
.EXAMPLE
    ./test-release-workflow-locally.ps1 -Version "1.2.3-test"

.EXAMPLE
    ./test-release-workflow-locally.ps1 -SkipBuild
#>

param(
    [string]$Version = "99.99.99-test",
    [switch]$SkipBuild,
    [switch]$SkipPreRelease,
    [switch]$SkipPostRelease
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Testing Release Workflow Locally" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Version: $Version" -ForegroundColor Yellow
Write-Host "Repository Root: $repoRoot" -ForegroundColor Yellow
Write-Host ""

# Create test directories
$testDir = Join-Path $repoRoot "test-release"
$packagesDir = Join-Path $testDir "packages"
$dockerDir = Join-Path $testDir "docker"
$localNugetDir = Join-Path $testDir "local-nuget"

if (Test-Path $testDir) {
    Write-Host "Cleaning up previous test directory..." -ForegroundColor Yellow
    Remove-Item -Path $testDir -Recurse -Force
}

New-Item -ItemType Directory -Path $testDir -Force | Out-Null
New-Item -ItemType Directory -Path $packagesDir -Force | Out-Null
New-Item -ItemType Directory -Path $dockerDir -Force | Out-Null
New-Item -ItemType Directory -Path $localNugetDir -Force | Out-Null

Write-Host "✅ Test directories created" -ForegroundColor Green
Write-Host ""

# Step 1: Build packages and Docker image
if (-not $SkipBuild) {
    Write-Host "======================================" -ForegroundColor Cyan
    Write-Host "Step 1: Building Packages and Docker Image" -ForegroundColor Cyan
    Write-Host "======================================" -ForegroundColor Cyan
    Write-Host ""

    # Update version in project file temporarily
    $projectFile = Join-Path $repoRoot "FlinkDotNet/FlinkDotNet.DataStream/FlinkDotNet.DataStream.csproj"
    Write-Host "Updating version in project file to $Version..." -ForegroundColor Yellow
    $projectContent = Get-Content $projectFile -Raw
    $originalContent = $projectContent
    $projectContent = $projectContent -replace '<PackageVersion>.*</PackageVersion>', "<PackageVersion>$Version</PackageVersion>"
    Set-Content -Path $projectFile -Value $projectContent

    try {
        # Restore dependencies
        Write-Host "Restoring dependencies..." -ForegroundColor Yellow
        Push-Location (Join-Path $repoRoot "FlinkDotNet")
        dotnet restore FlinkDotNet.sln
        if ($LASTEXITCODE -ne 0) { throw "Failed to restore dependencies" }

        # Build solution
        Write-Host "Building solution..." -ForegroundColor Yellow
        dotnet build FlinkDotNet.sln --configuration Release --no-restore
        if ($LASTEXITCODE -ne 0) { throw "Failed to build solution" }

        # Run unit tests
        Write-Host "Running unit tests..." -ForegroundColor Yellow
        dotnet test FlinkDotNet.sln --configuration Release --no-build --verbosity normal
        if ($LASTEXITCODE -ne 0) { throw "Unit tests failed" }

        # Create NuGet package
        Write-Host "Creating NuGet package..." -ForegroundColor Yellow
        dotnet pack FlinkDotNet.DataStream/FlinkDotNet.DataStream.csproj `
            --configuration Release `
            --no-build `
            --output $packagesDir `
            -p:PackageVersion=$Version
        if ($LASTEXITCODE -ne 0) { throw "Failed to create NuGet package" }

        Pop-Location

        # Build Docker image
        Write-Host "Building Docker image..." -ForegroundColor Yellow
        Push-Location $repoRoot
        docker build `
            -f FlinkDotNet/FlinkDotNet.JobGateway/Dockerfile `
            -t flinkdotnet/jobgateway:$Version `
            -t flinkdotnet/jobgateway:latest `
            .
        if ($LASTEXITCODE -ne 0) { throw "Failed to build Docker image" }

        # Save Docker image
        Write-Host "Saving Docker image to tarball..." -ForegroundColor Yellow
        $dockerTarball = Join-Path $dockerDir "jobgateway-$Version.tar"
        docker save flinkdotnet/jobgateway:$Version -o $dockerTarball
        if ($LASTEXITCODE -ne 0) { throw "Failed to save Docker image" }

        # Compress tarball
        Write-Host "Compressing Docker image tarball..." -ForegroundColor Yellow
        if (Get-Command gzip -ErrorAction SilentlyContinue) {
            gzip $dockerTarball
        } else {
            # Use PowerShell compression if gzip is not available
            $gzipPath = "$dockerTarball.gz"
            $tarballContent = [System.IO.File]::ReadAllBytes($dockerTarball)
            $gzipStream = New-Object System.IO.FileStream($gzipPath, [System.IO.FileMode]::Create)
            $gzipEncoder = New-Object System.IO.Compression.GZipStream($gzipStream, [System.IO.Compression.CompressionMode]::Compress)
            $gzipEncoder.Write($tarballContent, 0, $tarballContent.Length)
            $gzipEncoder.Close()
            $gzipStream.Close()
            Remove-Item $dockerTarball
        }

        Pop-Location

        Write-Host "✅ Build completed successfully" -ForegroundColor Green
        Write-Host "   - NuGet packages: $packagesDir" -ForegroundColor Gray
        Write-Host "   - Docker image: $dockerDir" -ForegroundColor Gray
        Write-Host ""

    } finally {
        # Restore original project file
        Set-Content -Path $projectFile -Value $originalContent
        Write-Host "Restored original project file" -ForegroundColor Gray
    }
} else {
    Write-Host "Skipping build (using existing packages)" -ForegroundColor Yellow
    Write-Host ""
}

# Step 2: Pre-Release Validation
if (-not $SkipPreRelease) {
    Write-Host "======================================" -ForegroundColor Cyan
    Write-Host "Step 2: Pre-Release Validation" -ForegroundColor Cyan
    Write-Host "======================================" -ForegroundColor Cyan
    Write-Host ""

    # Add local NuGet source
    Write-Host "Adding local NuGet source..." -ForegroundColor Yellow
    $nugetSources = dotnet nuget list source
    if ($nugetSources -notmatch "LocalTestFeed") {
        dotnet nuget add source $packagesDir --name LocalTestFeed
    }

    # Load Docker image
    Write-Host "Loading Docker image..." -ForegroundColor Yellow
    $dockerTarballGz = Join-Path $dockerDir "jobgateway-$Version.tar.gz"
    if (Test-Path $dockerTarballGz) {
        if (Get-Command gunzip -ErrorAction SilentlyContinue) {
            gunzip -c $dockerTarballGz | docker load
        } else {
            # Use PowerShell to decompress
            $gzipStream = New-Object System.IO.FileStream($dockerTarballGz, [System.IO.FileMode]::Open)
            $gzipDecoder = New-Object System.IO.Compression.GZipStream($gzipStream, [System.IO.Compression.CompressionMode]::Decompress)
            $outputStream = New-Object System.IO.MemoryStream
            $gzipDecoder.CopyTo($outputStream)
            $gzipDecoder.Close()
            $gzipStream.Close()
            $outputStream.Position = 0
            
            # Write to temp file and load
            $tempTar = Join-Path $dockerDir "temp.tar"
            [System.IO.File]::WriteAllBytes($tempTar, $outputStream.ToArray())
            docker load -i $tempTar
            Remove-Item $tempTar
            $outputStream.Close()
        }
        if ($LASTEXITCODE -ne 0) { throw "Failed to load Docker image" }
    }

    # Run pre-release tests
    Write-Host "Running pre-release integration tests..." -ForegroundColor Yellow
    Push-Location (Join-Path $repoRoot "ReleasePackagesTesting")
    dotnet test --configuration Release --verbosity normal
    $preReleaseTestResult = $LASTEXITCODE
    Pop-Location

    # Remove local NuGet source
    dotnet nuget remove source LocalTestFeed

    if ($preReleaseTestResult -ne 0) {
        Write-Host "❌ Pre-release validation failed!" -ForegroundColor Red
        exit 1
    }

    Write-Host "✅ Pre-release validation passed!" -ForegroundColor Green
    Write-Host ""
}

# Step 3: Simulate Publishing to Local Registries
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Step 3: Simulating Package Publishing" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Copy packages to local NuGet feed
Write-Host "Setting up local NuGet feed..." -ForegroundColor Yellow
Copy-Item -Path "$packagesDir/*" -Destination $localNugetDir -Recurse -Force
Write-Host "✅ Local NuGet feed created at: $localNugetDir" -ForegroundColor Green

# Docker image is already loaded, so it's available locally
Write-Host "✅ Docker image available locally: flinkdotnet/jobgateway:$Version" -ForegroundColor Green
Write-Host ""

# Step 4: Post-Release Validation
if (-not $SkipPostRelease) {
    Write-Host "======================================" -ForegroundColor Cyan
    Write-Host "Step 4: Post-Release Validation" -ForegroundColor Cyan
    Write-Host "======================================" -ForegroundColor Cyan
    Write-Host ""

    # Add local NuGet source for "published" packages
    Write-Host "Adding local NuGet feed as 'published' source..." -ForegroundColor Yellow
    $nugetSources = dotnet nuget list source
    if ($nugetSources -notmatch "LocalPublishedFeed") {
        dotnet nuget add source $localNugetDir --name LocalPublishedFeed
    }

    # Clear NuGet cache
    Write-Host "Clearing NuGet cache..." -ForegroundColor Yellow
    dotnet nuget locals all --clear

    # Run post-release tests
    Write-Host "Running post-release integration tests..." -ForegroundColor Yellow
    Push-Location (Join-Path $repoRoot "ReleasePackagesTesting.Published")
    dotnet test --configuration Release --verbosity normal
    $postReleaseTestResult = $LASTEXITCODE
    Pop-Location

    # Remove local NuGet source
    dotnet nuget remove source LocalPublishedFeed

    if ($postReleaseTestResult -ne 0) {
        Write-Host "❌ Post-release validation failed!" -ForegroundColor Red
        exit 1
    }

    Write-Host "✅ Post-release validation passed!" -ForegroundColor Green
    Write-Host ""
}

# Summary
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Testing Complete!" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "✅ All validation tests passed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Test artifacts location: $testDir" -ForegroundColor Gray
Write-Host ""
Write-Host "To clean up test artifacts, run:" -ForegroundColor Yellow
Write-Host "  Remove-Item -Path '$testDir' -Recurse -Force" -ForegroundColor Yellow
Write-Host ""
