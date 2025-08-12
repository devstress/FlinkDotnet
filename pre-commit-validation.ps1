#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Pre-commit hook to enforce build and test validation
    
.DESCRIPTION
    This script automatically runs before any commit to ensure that
    functionality changes do not break builds or critical tests.
    Implements enforcement to prevent build failures from being committed.
    
.EXAMPLE
    ./pre-commit-validation.ps1
#>

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

Write-Info "=== Pre-Commit Build and Test Validation ==="

# Check if we have any staged changes
try {
    $stagedFiles = git diff --cached --name-only 2>$null
    if (-not $stagedFiles) {
        Write-Info "No staged changes found. Skipping validation."
        exit 0
    }
    
    # Check if staged changes include code files
    $codeChanges = $stagedFiles | Where-Object { $_ -match '\.(cs|csproj|sln|json)$' }
    if (-not $codeChanges) {
        Write-Info "No code changes detected. Skipping validation."
        exit 0
    }
    
    Write-Info "Code changes detected:"
    foreach ($file in $codeChanges) {
        Write-Info "  - $file"
    }
} catch {
    Write-Warning "Could not check git status. Proceeding with validation."
}

# Run the main validation script
Write-Info "Running build and test validation..."

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ValidationScript = Join-Path $ScriptDir "validate-build-and-tests.ps1"

if (-not (Test-Path $ValidationScript)) {
    Write-Error "Validation script not found: $ValidationScript"
    Write-Error "Cannot proceed with commit."
    exit 1
}

try {
    # Run validation with Release configuration
    & $ValidationScript -Configuration Release
    $validationExitCode = $LASTEXITCODE
    
    switch ($validationExitCode) {
        0 {
            Write-Success "=== PRE-COMMIT VALIDATION PASSED ==="
            Write-Success "All builds and tests passed. Commit allowed."
            exit 0
        }
        1 {
            Write-Error "=== PRE-COMMIT VALIDATION FAILED ==="
            Write-Error "Build failures detected. Commit BLOCKED."
            Write-Error ""
            Write-Error "ENFORCEMENT RULE: All functionality changes must build successfully."
            Write-Error "Please fix build errors before committing:"
            Write-Error "  1. Run: ./validate-build-and-tests.ps1"
            Write-Error "  2. Fix any build errors"
            Write-Error "  3. Re-run validation"
            Write-Error "  4. Try commit again"
            exit 1
        }
        2 {
            Write-Warning "=== PRE-COMMIT VALIDATION: BUILD PASSED, TESTS FAILED ==="
            Write-Warning "Builds succeeded but some tests failed."
            Write-Warning "Commit allowed, but consider fixing test failures."
            Write-Info ""
            Write-Info "To fix test failures:"
            Write-Info "  1. Run: ./validate-build-and-tests.ps1"
            Write-Info "  2. Review test failure details"
            Write-Info "  3. Fix failing tests"
            exit 0
        }
        default {
            Write-Error "=== PRE-COMMIT VALIDATION ERROR ==="
            Write-Error "Unexpected validation result. Commit BLOCKED for safety."
            exit 1
        }
    }
} catch {
    Write-Error "=== PRE-COMMIT VALIDATION ERROR ==="
    Write-Error "Failed to run validation: $_"
    Write-Error "Commit BLOCKED for safety."
    exit 1
}