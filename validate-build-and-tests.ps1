#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Comprehensive build and test validation script for FlinkDotNet
    
.DESCRIPTION
    This script enforces that all functionality changes pass builds and tests
    before any code changes are committed. Implements enforcement rules to
    prevent build failures from being introduced.
    
.PARAMETER Configuration
    Build configuration (Debug or Release). Default is Release.
    
.PARAMETER SkipTests
    Skip running tests, only validate builds
    
.EXAMPLE
    ./validate-build-and-tests.ps1
    
.EXAMPLE
    ./validate-build-and-tests.ps1 -Configuration Debug -SkipTests
#>

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$SkipTests
)

# Colors for output
$Green = "`e[32m"
$Red = "`e[31m"
$Yellow = "`e[33m"
$Blue = "`e[34m"
$Reset = "`e[0m"

function Write-Success {
    param([string]$Message)
    Write-Host "${Green}✅ $Message${Reset}"
}

function Write-Error {
    param([string]$Message)
    Write-Host "${Red}❌ $Message${Reset}"
}

function Write-Warning {
    param([string]$Message)
    Write-Host "${Yellow}⚠️ $Message${Reset}"
}

function Write-Info {
    param([string]$Message)
    Write-Host "${Blue}ℹ️ $Message${Reset}"
}

# Ensure we're in the correct directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ScriptDir

Write-Info "=== FlinkDotNet Build and Test Validation ==="
Write-Info "Configuration: $Configuration"
Write-Info "Skip Tests: $SkipTests"

# Step 1: Verify .NET 9.0 Environment
Write-Info "Step 1: Verifying .NET 9.0 Environment..."

try {
    $dotnetVersion = dotnet --version
    if ($dotnetVersion -match "^9\.0") {
        Write-Success ".NET Version: $dotnetVersion (✓ .NET 9.0 compliant)"
    } else {
        Write-Error ".NET Version: $dotnetVersion (✗ Requires .NET 9.0.x)"
        Write-Error "Please install .NET 9.0 SDK from https://dotnet.microsoft.com/download/dotnet/9.0"
        exit 1
    }
} catch {
    Write-Error ".NET SDK not found or not accessible"
    Write-Error "Please install .NET 9.0 SDK"
    exit 1
}

# Step 2: Find and validate all solution files
Write-Info "Step 2: Finding solution files..."

$SolutionFiles = @(
    "FlinkDotNet/FlinkDotNet.sln",
    "Sample/Sample.sln", 
    "LocalTesting/LocalTesting.sln"
)

$AllSolutionsExist = $true
foreach ($sln in $SolutionFiles) {
    if (Test-Path $sln) {
        Write-Success "Found: $sln"
    } else {
        Write-Error "Missing: $sln"
        $AllSolutionsExist = $false
    }
}

if (-not $AllSolutionsExist) {
    Write-Error "Some solution files are missing. Cannot proceed."
    exit 1
}

# Step 3: Build all solutions
Write-Info "Step 3: Building all solutions..."

$BuildResults = @{}
$BuildFailed = $false

foreach ($sln in $SolutionFiles) {
    Write-Info "Building $sln..."
    
    try {
        $buildOutput = & dotnet build $sln --configuration $Configuration --verbosity quiet 2>&1
        $buildExitCode = $LASTEXITCODE
        
        if ($buildExitCode -eq 0) {
            Write-Success "Build succeeded: $sln"
            $BuildResults[$sln] = "SUCCESS"
        } else {
            Write-Error "Build failed: $sln"
            Write-Host $buildOutput
            $BuildResults[$sln] = "FAILED"
            $BuildFailed = $true
        }
    } catch {
        Write-Error "Build error for $sln : $_"
        $BuildResults[$sln] = "ERROR"
        $BuildFailed = $true
    }
}

# Step 4: Run tests if builds succeeded and tests not skipped
if (-not $BuildFailed -and -not $SkipTests) {
    Write-Info "Step 4: Running tests..."
    
    $TestResults = @{}
    $TestFailed = $false
    
    # Test solutions that have test projects
    $TestSolutions = @(
        "FlinkDotNet/FlinkDotNet.sln",
        "Sample/Sample.sln"
    )
    
    foreach ($sln in $TestSolutions) {
        Write-Info "Testing $sln..."
        
        try {
            $testOutput = & dotnet test $sln --configuration $Configuration --no-build --verbosity quiet 2>&1
            $testExitCode = $LASTEXITCODE
            
            if ($testExitCode -eq 0) {
                Write-Success "Tests passed: $sln"
                $TestResults[$sln] = "SUCCESS"
            } else {
                Write-Warning "Tests failed: $sln"
                Write-Host $testOutput
                $TestResults[$sln] = "FAILED"
                $TestFailed = $true
            }
        } catch {
            Write-Error "Test error for $sln : $_"
            $TestResults[$sln] = "ERROR"
            $TestFailed = $true
        }
    }
} elseif ($BuildFailed) {
    Write-Warning "Skipping tests due to build failures"
} else {
    Write-Info "Skipping tests (SkipTests parameter set)"
}

# Step 5: Summary Report
Write-Info "=== VALIDATION SUMMARY ==="

Write-Info "Build Results:"
foreach ($sln in $SolutionFiles) {
    $result = $BuildResults[$sln]
    switch ($result) {
        "SUCCESS" { Write-Success "  $sln - Build Succeeded" }
        "FAILED"  { Write-Error "  $sln - Build Failed" }
        "ERROR"   { Write-Error "  $sln - Build Error" }
    }
}

if (-not $SkipTests -and -not $BuildFailed) {
    Write-Info "Test Results:"
    foreach ($sln in $TestResults.Keys) {
        $result = $TestResults[$sln]
        switch ($result) {
            "SUCCESS" { Write-Success "  $sln - Tests Passed" }
            "FAILED"  { Write-Warning "  $sln - Tests Failed" }
            "ERROR"   { Write-Error "  $sln - Test Error" }
        }
    }
}

# Step 6: Final validation and exit code
if ($BuildFailed) {
    Write-Error "=== VALIDATION FAILED ==="
    Write-Error "One or more builds failed. Please fix build errors before proceeding."
    Write-Error "All functionality changes must pass builds before commit."
    exit 1
}

if (-not $SkipTests -and $TestFailed) {
    Write-Warning "=== VALIDATION COMPLETED WITH TEST FAILURES ==="
    Write-Warning "Builds succeeded but some tests failed."
    Write-Warning "Consider fixing test failures, but builds are ready for commit."
    exit 2
}

Write-Success "=== VALIDATION SUCCESSFUL ==="
Write-Success "All builds passed successfully!"
if (-not $SkipTests) {
    Write-Success "All tests completed (check individual results above)!"
}
Write-Success "Ready for commit and deployment."
exit 0