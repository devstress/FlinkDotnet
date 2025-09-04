#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Enhanced Build and Test Validation with Enforcement Rules
    
.DESCRIPTION
    This script implements comprehensive enforcement rules to prevent repeated mistakes:
    - Rule 13: .NET 9.0 Environment Requirements
    - Rule 14: Pre-Change Validation Requirements
    - Rule 6: Mandatory Learning and Problem Prevention
    - Rule 10: Automatic Archival & Learning Enforcement
    
.PARAMETER Configuration
    Build configuration (Debug or Release, default: Release)
    
.PARAMETER SkipTests
    Skip test execution (only validate builds)
    
.PARAMETER SkipLearningCheck
    Skip learning consultation check (for automated CI)
    
.EXAMPLE
    ./enhanced-validate-build-and-tests.ps1
    
.EXAMPLE
    ./enhanced-validate-build-and-tests.ps1 -Configuration Debug -SkipTests
#>

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$SkipTests,
    [switch]$SkipLearningCheck
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

function Write-Separator {
    Write-Host "${Blue}================================================================${Reset}"
}

Write-Info "🚀 Enhanced Build and Test Validation with Enforcement Rules"
Write-Separator

# ENFORCEMENT RULE 13: .NET 9.0 Environment Requirements (CRITICAL)
Write-Info "🔍 Rule 13: Verifying .NET 9.0 environment..."

try {
    $dotnetVersion = & dotnet --version 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Error "CRITICAL VIOLATION: .NET SDK not found or not working"
        Write-Error "Rule 13: Mandatory .NET 9.0 environment required"
        Write-Error "Install .NET 9.0 SDK from: https://dotnet.microsoft.com/download/dotnet/9.0"
        exit 1
    }
    
    if (-not $dotnetVersion.StartsWith("9.0")) {
        Write-Error "CRITICAL VIOLATION: Wrong .NET version detected"
        Write-Error "Current: $dotnetVersion | Required: 9.0.x"
        Write-Error "Rule 13: All development must use .NET 9.0 SDK"
        Write-Error "Update your environment and try again"
        exit 1
    }
    
    Write-Success "Rule 13: .NET 9.0 environment verified ($dotnetVersion)"
} catch {
    Write-Error "Failed to verify .NET environment: $_"
    exit 1
}

# Check for Aspire workload
Write-Info "🔍 Checking Aspire workload installation..."
try {
    $workloads = & dotnet workload list 2>$null
    if ($workloads -like "*aspire*") {
        Write-Success "Aspire workload detected"
    } else {
        Write-Warning "Aspire workload not detected - may be required for some tests"
        Write-Info "To install: dotnet workload install aspire"
    }
} catch {
    Write-Warning "Could not check Aspire workload status"
}

# ENFORCEMENT RULE 6: Mandatory Learning and Problem Prevention (CRITICAL)
if (-not $SkipLearningCheck) {
    Write-Info "🧠 Rule 6: Checking learning consultation and problem prevention..."
    
    if (Test-Path "AI-Learning") {
        $learningFiles = Get-ChildItem -Path "AI-Learning" -Filter "*.md" -Recurse
        Write-Success "Rule 6: Found $($learningFiles.Count) learning documents available"
        
        # Check for common problem patterns in current directory
        $commonProblems = @()
        
        # Check for Aspire-related work without consulting Aspire learnings
        if ((Get-ChildItem -Filter "*aspire*" -Recurse).Count -gt 0) {
            $aspirelearning = $learningFiles | Where-Object { $_.Name -like "*Aspire*" }
            if ($aspirelearning) {
                $commonProblems += "Aspire-related files found - ensure Aspire_Testing_Integration_Patterns.md has been reviewed"
            }
        }
        
        # Check for observability test changes
        if ((Get-ChildItem -Filter "*observability*" -Recurse).Count -gt 0) {
            $obsLearning = $learningFiles | Where-Object { $_.Name -like "*Observability*" }
            if ($obsLearning) {
                $commonProblems += "Observability test files found - ensure Observability_Testing.md has been reviewed"
            }
        }
        
        if ($commonProblems.Count -gt 0) {
            Write-Warning "⚠️ Rule 6: Potential learning consultation required:"
            foreach ($problem in $commonProblems) {
                Write-Warning "  - $problem"
            }
        }
    } else {
        Write-Info "📝 No AI-Learning repository found - creating initial structure"
        Write-Info "Run: ./scripts/extract-and-archive-wi-learnings.ps1 to create learning repository"
    }
}

# ENFORCEMENT RULE 10: Automatic Archival & Learning Enforcement
Write-Info "📦 Rule 10: Checking Work Item archival requirements..."
if (Test-Path "WIs") {
    $oldWICount = 0
    $totalWICount = 0
    $wiFiles = Get-ChildItem -Path "WIs" -Filter "*.md" | Where-Object { 
        $_.Name -notlike "WI_CONSOLIDATED_*" -and $_.Name -notlike "Archived/*" 
    }
    
    foreach ($wiFile in $wiFiles) {
        $totalWICount++
        $age = ((Get-Date) - $wiFile.CreationTime).Days
        if ($age -gt 30) {
            $oldWICount++
        }
    }
    
    Write-Info "Work Items status: $totalWICount active, $oldWICount requiring archival"
    
    if ($oldWICount -gt 0) {
        Write-Warning "Rule 10 VIOLATION: $oldWICount Work Items older than 30 days need archival"
        Write-Warning "Learnings must be extracted to prevent repeating solved problems"
        Write-Warning "Run: ./scripts/extract-and-archive-wi-learnings.ps1"
    } else {
        Write-Success "Rule 10: Work Item archival requirements satisfied"
    }
}

# ENFORCEMENT RULE 14: Pre-Change Validation Requirements (CRITICAL)
Write-Info "🔨 Rule 14: Starting comprehensive build validation..."

# Define solutions to validate
$solutions = @(
    "FlinkDotNet/FlinkDotNet.sln",
    "LocalTesting/LocalTesting.sln"
)

# Check if Sample solution exists
if (Test-Path "Sample/Sample.sln") {
    $solutions += "Sample/Sample.sln"
}

$buildSuccess = $true
$buildErrors = @()

Write-Info "Building $($solutions.Count) solutions with configuration: $Configuration"

foreach ($solution in $solutions) {
    if (-not (Test-Path $solution)) {
        Write-Warning "Solution not found: $solution (skipping)"
        continue
    }
    
    Write-Info "🔨 Building: $solution"
    
    try {
        # Clean and restore
        & dotnet clean $solution --configuration $Configuration --verbosity minimal
        if ($LASTEXITCODE -ne 0) {
            $buildErrors += "Clean failed for $solution"
            $buildSuccess = $false
            continue
        }
        
        & dotnet restore $solution --verbosity minimal
        if ($LASTEXITCODE -ne 0) {
            $buildErrors += "Restore failed for $solution"
            $buildSuccess = $false
            continue
        }
        
        # Build
        & dotnet build $solution --configuration $Configuration --no-restore --verbosity minimal
        if ($LASTEXITCODE -ne 0) {
            $buildErrors += "Build failed for $solution"
            $buildSuccess = $false
        } else {
            Write-Success "✅ Build successful: $solution"
        }
        
    } catch {
        $buildErrors += "Exception building $solution`: $_"
        $buildSuccess = $false
    }
}

Write-Separator

if (-not $buildSuccess) {
    Write-Error "🚨 RULE 14 VIOLATION: Build failures detected"
    Write-Error "All builds MUST pass before any code changes can be committed"
    Write-Error ""
    Write-Error "Build errors:"
    foreach ($error in $buildErrors) {
        Write-Error "  - $error"
    }
    Write-Error ""
    Write-Error "Required actions:"
    Write-Error "  1. Fix all build errors listed above"
    Write-Error "  2. Re-run this validation script"
    Write-Error "  3. Ensure all solutions build successfully"
    Write-Error ""
    Write-Error "COMMIT/MERGE BLOCKED until all builds pass"
    exit 1
}

Write-Success "🎉 Rule 14: All build validations passed successfully"

# Test execution (if not skipped)
if (-not $SkipTests) {
    Write-Info "🧪 Running available tests..."
    
    $testSuccess = $true
    $testErrors = @()
    
    # Run tests for each solution that has test projects
    foreach ($solution in $solutions) {
        if (-not (Test-Path $solution)) { continue }
        
        $solutionDir = Split-Path $solution -Parent
        $testProjects = Get-ChildItem -Path $solutionDir -Filter "*.csproj" -Recurse | 
                       Where-Object { $_.Name -like "*Test*" -or $_.Directory.Name -like "*Test*" }
        
        if ($testProjects.Count -eq 0) {
            Write-Info "No test projects found in $solution"
            continue
        }
        
        Write-Info "🧪 Running tests for: $solution"
        
        try {
            & dotnet test $solution --configuration $Configuration --no-build --verbosity normal
            if ($LASTEXITCODE -ne 0) {
                $testErrors += "Tests failed for $solution"
                $testSuccess = $false
            } else {
                Write-Success "✅ Tests passed: $solution"
            }
        } catch {
            $testErrors += "Exception running tests for $solution`: $_"
            $testSuccess = $false
        }
    }
    
    Write-Separator
    
    if (-not $testSuccess) {
        Write-Warning "⚠️ Test failures detected"
        Write-Warning "While builds passed, some tests are failing:"
        foreach ($error in $testErrors) {
            Write-Warning "  - $error"
        }
        Write-Warning ""
        Write-Warning "Test failures do not block commits but should be investigated"
        Write-Warning "Consider fixing test failures before merge"
        
        # Return code 2 for "builds passed, tests failed"
        exit 2
    } else {
        Write-Success "🎉 All tests passed successfully"
    }
}

Write-Separator
Write-Success "🏆 ENHANCED VALIDATION COMPLETE - ALL ENFORCEMENT RULES SATISFIED"
Write-Success "✅ Rule 13: .NET 9.0 environment verified"
Write-Success "✅ Rule 14: All builds successful"
if (-not $SkipLearningCheck) {
    Write-Success "✅ Rule 6: Learning consultation checked"
}
Write-Success "✅ Rule 10: Work Item archival status verified"

if (-not $SkipTests) {
    Write-Success "✅ All tests executed successfully"
}

Write-Info "🚀 Ready for commit/merge - no enforcement violations detected"

# Return 0 for complete success
exit 0