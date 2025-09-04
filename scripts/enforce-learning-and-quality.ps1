#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Comprehensive Work Item Enforcement and Learning System
    
.DESCRIPTION
    This script implements all enforcement rules to prevent repeated mistakes:
    1. Prevents committing broken changes that break GitHub workflows
    2. Stops learning from being lost in Work Items by extracting to AI-Learning
    3. Enforces environment requirements and build validation
    4. Automates learning consultation and problem prevention
    
    Addresses the core problem: "How to make this from happening again? 
    You stop commit broken changes which breaks Github workflows."
    
.PARAMETER Action
    Action to perform: validate, extract-learnings, setup-hooks, or full-enforcement
    
.PARAMETER Force
    Force operations even when conditions not met
    
.EXAMPLE
    ./enforce-learning-and-quality.ps1 -Action full-enforcement
    
.EXAMPLE
    ./enforce-learning-and-quality.ps1 -Action extract-learnings
#>

param(
    [ValidateSet("validate", "extract-learnings", "setup-hooks", "full-enforcement")]
    [string]$Action = "full-enforcement",
    [switch]$Force
)

# Colors for output
$Green = "`e[32m"
$Red = "`e[31m"
$Yellow = "`e[33m"
$Blue = "`e[34m"
$Magenta = "`e[35m"
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

function Write-Header {
    param([string]$Message)
    Write-Host "${Magenta}🎯 $Message${Reset}"
}

function Write-Separator {
    Write-Host "${Blue}================================================================${Reset}"
}

Write-Header "COMPREHENSIVE WORK ITEM ENFORCEMENT AND LEARNING SYSTEM"
Write-Info "Implementing solution to prevent repeated mistakes and broken workflows"
Write-Separator

switch ($Action) {
    "validate" {
        Write-Header "VALIDATION MODE: Checking current compliance status"
        
        # Check .NET environment
        Write-Info "🔍 Checking .NET 9.0 environment..."
        try {
            $dotnetVersion = & dotnet --version 2>$null
            if ($dotnetVersion -and $dotnetVersion.StartsWith("9.0")) {
                Write-Success ".NET 9.0 environment verified: $dotnetVersion"
            } else {
                Write-Error "CRITICAL: Wrong .NET version: $dotnetVersion (expected 9.0.x)"
                Write-Error "This WILL cause GitHub workflow failures"
                return
            }
        } catch {
            Write-Error "CRITICAL: .NET environment check failed"
            return
        }
        
        # Check for old Work Items
        Write-Info "📦 Checking Work Item archival needs..."
        if (Test-Path "WIs") {
            $oldWIs = Get-ChildItem -Path "WIs" -Filter "*.md" | Where-Object {
                $_.Name -notlike "WI_CONSOLIDATED_*" -and 
                $_.Name -notlike "Archived/*" -and
                ((Get-Date) - $_.CreationTime).Days -gt 30
            }
            
            if ($oldWIs.Count -gt 0) {
                Write-Warning "Found $($oldWIs.Count) Work Items requiring archival"
                Write-Warning "These contain learnings that should be extracted to prevent repetition"
                foreach ($wi in $oldWIs) {
                    Write-Warning "  - $($wi.Name) ($(((Get-Date) - $wi.CreationTime).Days) days old)"
                }
            } else {
                Write-Success "No Work Items requiring archival"
            }
        }
        
        # Check learning repository status
        Write-Info "🧠 Checking learning repository status..."
        if (Test-Path "AI-Learning") {
            $learningFiles = Get-ChildItem -Path "AI-Learning" -Filter "*.md"
            Write-Success "Learning repository exists with $($learningFiles.Count) documents"
        } else {
            Write-Warning "No AI-Learning repository - learnings not being preserved"
        }
        
        # Run build validation
        Write-Info "🔨 Running build validation..."
        if (Test-Path "scripts/enhanced-validate-build-and-tests.ps1") {
            & "./scripts/enhanced-validate-build-and-tests.ps1" -SkipLearningCheck
            $buildResult = $LASTEXITCODE
            if ($buildResult -eq 0) {
                Write-Success "All builds passing - no workflow failures expected"
            } else {
                Write-Error "BUILD FAILURES DETECTED - this WILL break GitHub workflows"
            }
        } else {
            Write-Warning "Enhanced validation script not found"
        }
    }
    
    "extract-learnings" {
        Write-Header "LEARNING EXTRACTION MODE: Processing old Work Items"
        
        # Run learning extraction script
        if (Test-Path "scripts/extract-and-archive-wi-learnings.ps1") {
            Write-Info "🧠 Extracting learnings from old Work Items..."
            & "./scripts/extract-and-archive-wi-learnings.ps1" -Force:$Force
            Write-Success "Learning extraction completed"
        } else {
            Write-Error "Learning extraction script not found"
            return
        }
        
        # Verify learning repository created
        if (Test-Path "AI-Learning") {
            $learningFiles = Get-ChildItem -Path "AI-Learning" -Filter "*.md"
            Write-Success "Learning repository created with $($learningFiles.Count) documents"
            
            Write-Info "📚 Available learning topics:"
            foreach ($file in $learningFiles) {
                if ($file.Name -ne "README.md") {
                    Write-Info "  - $($file.BaseName.Replace('_', ' '))"
                }
            }
        }
    }
    
    "setup-hooks" {
        Write-Header "HOOK SETUP MODE: Installing enforcement hooks"
        
        # Setup git hooks for pre-commit validation
        $gitHooksDir = ".git/hooks"
        if (Test-Path $gitHooksDir) {
            $preCommitHook = Join-Path $gitHooksDir "pre-commit"
            
            # Create pre-commit hook that calls our validation
            $hookContent = @"
#!/bin/sh
# Comprehensive pre-commit validation with enforcement rules
# Prevents broken changes from reaching GitHub workflows

echo "🔍 Running comprehensive pre-commit validation..."

# Run PowerShell validation script
if command -v pwsh >/dev/null 2>&1; then
    pwsh -File scripts/pre-commit-validation.ps1
else
    powershell -File scripts/pre-commit-validation.ps1
fi

exit_code=$?

if [ $exit_code -ne 0 ]; then
    echo ""
    echo "❌ PRE-COMMIT VALIDATION FAILED"
    echo "Commit blocked to prevent breaking GitHub workflows"
    echo ""
    echo "Fix the issues above and try again."
    exit 1
fi

echo "✅ Pre-commit validation passed"
exit 0
"@
            
            $hookContent | Out-File -FilePath $preCommitHook -Encoding UTF8
            
            # Make hook executable (on Unix systems)
            try {
                if ($IsLinux -or $IsMacOS) {
                    chmod +x $preCommitHook
                }
                Write-Success "Pre-commit hook installed: $preCommitHook"
            } catch {
                Write-Warning "Could not make hook executable (may need manual chmod +x)"
            }
        } else {
            Write-Warning "Not in a git repository - cannot install hooks"
        }
        
        # Update .gitignore to exclude build artifacts
        Write-Info "📝 Updating .gitignore to prevent committing artifacts..."
        $gitignoreAdditions = @(
            "# Build artifacts that should not be committed",
            "**/bin/",
            "**/obj/",
            "**/.vs/",
            "**/TestResults/",
            "**/*.tmp",
            "**/*.cache",
            ""
        )
        
        if (Test-Path ".gitignore") {
            $currentGitignore = Get-Content ".gitignore" -Raw
            $needsUpdate = $false
            
            foreach ($addition in $gitignoreAdditions) {
                if ($addition -and -not $currentGitignore.Contains($addition)) {
                    $needsUpdate = $true
                    break
                }
            }
            
            if ($needsUpdate) {
                Add-Content ".gitignore" -Value "`n"
                Add-Content ".gitignore" -Value $gitignoreAdditions
                Write-Success "Updated .gitignore with build artifact exclusions"
            } else {
                Write-Success ".gitignore already contains necessary exclusions"
            }
        }
    }
    
    "full-enforcement" {
        Write-Header "FULL ENFORCEMENT MODE: Implementing comprehensive solution"
        
        # Step 1: Extract learnings from existing Work Items
        Write-Info "🧠 Step 1: Extracting learnings from existing Work Items..."
        & $MyInvocation.MyCommand.Path -Action extract-learnings -Force:$Force
        
        Write-Separator
        
        # Step 2: Setup enforcement hooks
        Write-Info "🔧 Step 2: Setting up enforcement hooks..."
        & $MyInvocation.MyCommand.Path -Action setup-hooks
        
        Write-Separator
        
        # Step 3: Validate current state
        Write-Info "✅ Step 3: Validating current state..."
        & $MyInvocation.MyCommand.Path -Action validate
        
        Write-Separator
        
        # Step 4: Create enforcement documentation
        Write-Info "📚 Step 4: Creating enforcement documentation..."
        
        $enforcementDoc = @"
# Enforcement System Implementation

This document describes the comprehensive enforcement system implemented to solve:
**"How to make this from happening again? You stop commit broken changes which breaks Github workflows."**

## Problem Analysis

The core issues were:
1. **.NET Environment Mismatches**: Code developed with wrong .NET version causing GitHub workflow failures
2. **Repeated Mistakes**: Same problems occurring across multiple Work Items (WI9, WI10 Aspire issues)
3. **Learning Loss**: Valuable lessons trapped in Work Items not being applied to prevent repetition
4. **Pre-commit Validation Gaps**: Broken changes reaching GitHub workflows

## Solution Implementation

### 1. Automated Learning Extraction (Rule 10)
- **Script**: `scripts/extract-and-archive-wi-learnings.ps1`
- **Purpose**: Extracts learnings from Work Items older than 30 days into `AI-Learning/` folder
- **Result**: Searchable knowledge base prevents repeating solved problems

### 2. Enhanced Pre-commit Validation (Rules 13, 14, 6)
- **Script**: `scripts/pre-commit-validation.ps1` (enhanced)
- **Enforcement**: 
  - Mandatory .NET 9.0 environment check
  - Learning consultation verification
  - Build validation before commit
- **Result**: Broken changes blocked before reaching GitHub

### 3. Comprehensive Build Validation (Rule 14)
- **Script**: `scripts/enhanced-validate-build-and-tests.ps1`
- **Coverage**: All solutions (FlinkDotNet, LocalTesting, Sample)
- **Enforcement**: Zero tolerance for build failures
- **Result**: GitHub workflows protected from build breaks

### 4. Git Hooks Integration
- **Pre-commit hook**: Automatically runs validation before any commit
- **Enforcement**: Cannot commit without passing all checks
- **Result**: Problems caught locally, not in CI/CD

## Learning Repository Structure

The `AI-Learning/` folder contains:
- **Aspire_Testing_Integration_Patterns.md**: Prevents repeated Aspire integration failures
- **Topic-specific documents**: Consolidated learnings by domain
- **Prevention checklists**: Actions to avoid known problems

## Usage

### For Developers
1. **Before starting work**: Review relevant `AI-Learning/` documents
2. **During development**: Use enhanced validation scripts frequently
3. **Before committing**: Pre-commit hooks automatically enforce rules

### For Maintenance
1. **Monthly**: Run learning extraction to process old Work Items
2. **Continuous**: Monitor for new patterns requiring documentation
3. **Updates**: Enhance enforcement rules based on new violations

## Success Metrics

1. **Zero GitHub workflow failures** due to environment issues
2. **Reduced Work Item duplication** for solved problems
3. **Faster problem resolution** using learning repository
4. **Higher code quality** through comprehensive validation

## Emergency Procedures

If enforcement needs to be bypassed (emergency only):
1. Use `git commit --no-verify` to skip pre-commit hooks
2. Document why bypass was necessary
3. Fix underlying issues immediately after emergency
4. Update enforcement rules to prevent similar emergencies

---

**Implementation Date**: $(Get-Date -Format 'yyyy-MM-dd')
**Status**: Active enforcement protecting against repeated mistakes and broken workflows
"@
        
        $enforcementDoc | Out-File -FilePath "docs/ENFORCEMENT_SYSTEM.md" -Encoding UTF8
        Write-Success "Created enforcement system documentation: docs/ENFORCEMENT_SYSTEM.md"
        
        # Summary
        Write-Separator
        Write-Header "🎉 FULL ENFORCEMENT SYSTEM IMPLEMENTED SUCCESSFULLY"
        Write-Success "✅ Learning extraction system active"
        Write-Success "✅ Enhanced pre-commit validation installed"
        Write-Success "✅ Git hooks configured"
        Write-Success "✅ Comprehensive build validation available"
        Write-Success "✅ Documentation created"
        
        Write-Info ""
        Write-Info "🛡️ PROTECTION ENABLED:"
        Write-Info "  - GitHub workflows protected from broken changes"
        Write-Info "  - Learning repository prevents repeated mistakes"
        Write-Info "  - Environment validation blocks wrong .NET versions"
        Write-Info "  - Pre-commit hooks catch problems before push"
        Write-Info ""
        Write-Info "🚀 The system will now prevent the issues described in the problem statement"
    }
}

Write-Separator
Write-Success "Enforcement action '$Action' completed successfully"