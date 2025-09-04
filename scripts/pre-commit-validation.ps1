#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Pre-commit hook to enforce build and test validation
    
.DESCRIPTION
    This script automatically runs before any commit to ensure that
    functionality changes do not break builds or critical tests.
    Implements enforcement to prevent build failures from being committed.
    
.PARAMETER Force
    Force validation to run even when no staged changes are detected
    
.EXAMPLE
    ./pre-commit-validation.ps1
    
.EXAMPLE
    ./pre-commit-validation.ps1 -Force
#>

param(
    [switch]$Force
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

Write-Info "=== Pre-Commit Build and Test Validation ==="

# Check if we have any staged changes (unless forced)
if (-not $Force) {
    try {
        $stagedFiles = git diff --cached --name-only 2>$null
        if (-not $stagedFiles) {
            Write-Info "No staged changes found. Skipping validation."
            Write-Info "Use -Force parameter to run validation anyway."
            exit 0
        }
        
        # Check if staged changes include code files
        $codeChanges = $stagedFiles | Where-Object { $_ -match '\.(cs|csproj|sln|json)$' }
        if (-not $codeChanges) {
            Write-Info "No code changes detected. Skipping validation."
            Write-Info "Use -Force parameter to run validation anyway."
            exit 0
        }
        
        Write-Info "Code changes detected:"
        foreach ($file in $codeChanges) {
            Write-Info "  - $file"
        }
    } catch {
        Write-Warning "Could not check git status. Proceeding with validation."
    }
} else {
    Write-Info "Force mode enabled. Running validation regardless of staged changes."
}

# ENHANCED PRE-COMMIT VALIDATION WITH ENFORCEMENT RULES

# Rule 13: .NET 9.0 Environment Requirements (CRITICAL)
Write-Info "🔍 Checking .NET 9.0 environment requirement..."
try {
    $dotnetVersion = & dotnet --version 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Error "=== .NET ENVIRONMENT VIOLATION ==="
        Write-Error ".NET SDK not found or not working properly."
        Write-Error "ENFORCEMENT RULE 13: Mandatory .NET 9.0 environment required."
        Write-Error ""
        Write-Error "Required actions:"
        Write-Error "  1. Install .NET 9.0 SDK from https://dotnet.microsoft.com/download/dotnet/9.0"
        Write-Error "  2. Verify installation: dotnet --version"
        Write-Error "  3. Ensure version shows 9.0.x"
        Write-Error ""
        Write-Error "COMMIT BLOCKED until .NET 9.0 environment is verified."
        exit 1
    }
    
    if (-not $dotnetVersion.StartsWith("9.0")) {
        Write-Error "=== .NET VERSION VIOLATION ==="
        Write-Error "Current .NET version: $dotnetVersion"
        Write-Error "Required .NET version: 9.0.x"
        Write-Error "ENFORCEMENT RULE 13: All development must use .NET 9.0 SDK."
        Write-Error ""
        Write-Error "Required actions:"
        Write-Error "  1. Install .NET 9.0 SDK"
        Write-Error "  2. Update PATH to prioritize .NET 9.0"
        Write-Error "  3. Verify: dotnet --version shows 9.0.x"
        Write-Error ""
        Write-Error "COMMIT BLOCKED until .NET 9.0 environment is verified."
        exit 1
    }
    
    Write-Success "✅ .NET 9.0 environment verified: $dotnetVersion"
} catch {
    Write-Error "Failed to check .NET version: $_"
    Write-Error "COMMIT BLOCKED for safety."
    exit 1
}

# Rule 6: Mandatory Learning and Problem Prevention (CRITICAL)
Write-Info "🧠 Checking for repeated problems and learning application..."

# Check if AI-Learning directory exists and has been consulted
if (Test-Path "AI-Learning") {
    $learningFiles = Get-ChildItem -Path "AI-Learning" -Filter "*.md" -Recurse
    if ($learningFiles.Count -gt 0) {
        Write-Info "📚 Found $($learningFiles.Count) learning documents available for consultation."
        
        # Check if any new WI files are being committed
        try {
            $stagedWIFiles = git diff --cached --name-only | Where-Object { $_ -like "WIs/*.md" -and $_ -notlike "WIs/Archived/*" }
            if ($stagedWIFiles) {
                Write-Info "🔍 New Work Item files detected in commit:"
                foreach ($wiFile in $stagedWIFiles) {
                    Write-Info "  - $wiFile"
                }
                
                Write-Warning "⚠️ LEARNING ENFORCEMENT CHECK"
                Write-Warning "New Work Items require learning consultation per Rule 6."
                Write-Warning ""
                Write-Warning "Before proceeding, ensure you have:"
                Write-Warning "  1. Reviewed relevant AI-Learning/ documents"
                Write-Warning "  2. Applied lessons from previous similar work"
                Write-Warning "  3. Documented 'Lessons Applied from Previous WIs' section"
                Write-Warning "  4. Included specific prevention actions for known problems"
                Write-Warning ""
                Write-Warning "Available learning topics:"
                foreach ($learningFile in $learningFiles) {
                    Write-Warning "  - $($learningFile.BaseName.Replace('_', ' '))"
                }
                Write-Warning ""
                
                # Don't block but warn - rely on review process to enforce
                Write-Info "💡 Reminder: Failure to learn from previous WIs is a MAJOR violation"
            }
        } catch {
            Write-Warning "Could not check for new WI files in commit"
        }
    }
} else {
    Write-Info "📝 No AI-Learning repository found - first time setup"
}

# Rule 10: Automatic Archiving & Learning Enforcement
Write-Info "📦 Checking Work Item archival requirements..."
if (Test-Path "WIs") {
    $oldWICount = 0
    $wiFiles = Get-ChildItem -Path "WIs" -Filter "*.md" | Where-Object { $_.Name -notlike "WI_CONSOLIDATED_*" -and $_.Name -notlike "Archived/*" }
    
    foreach ($wiFile in $wiFiles) {
        $age = ((Get-Date) - $wiFile.CreationTime).Days
        if ($age -gt 30) {
            $oldWICount++
        }
    }
    
    if ($oldWICount -gt 0) {
        Write-Warning "⚠️ ARCHIVAL REQUIREMENT VIOLATION"
        Write-Warning "Found $oldWICount Work Items older than 30 days requiring archival."
        Write-Warning "Rule 10: Work Items older than 1 month must be archived with learning extraction."
        Write-Warning ""
        Write-Warning "Run learning extraction script:"
        Write-Warning "  ./scripts/extract-and-archive-wi-learnings.ps1"
        Write-Warning ""
        Write-Info "💡 This helps prevent repeating solved problems"
    }
}

# Run the main validation script
Write-Info "🔨 Running build and test validation..."

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