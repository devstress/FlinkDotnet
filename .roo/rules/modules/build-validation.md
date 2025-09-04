# Build and Validation Enforcement

## .NET 9.0 Local Development Environment Requirements (CRITICAL - Rule 13)

### MANDATORY .NET 9.0 SDK
- **MANDATORY .NET 9.0 SDK**: All local development must use .NET 9.0.303 or later
- **Before submitting any GitHub workflow or PR**, developers MUST verify:
  - Local environment has .NET 9.0 SDK installed (`dotnet --version` returns 9.0.x)
  - Aspire workload is installed and functional
  - All solutions build successfully locally with .NET 9.0
  - LocalTesting workflow executes successfully locally

### Local Environment Setup Requirements
- .NET 9.0 SDK installation using official Microsoft installer
- Aspire workload installation (`dotnet workload install aspire`)
- Docker Desktop running for Aspire orchestration
- LocalTesting solution builds and runs without errors

### GitHub Workflow Local Validation
- ALL GitHub workflows must pass locally before submission for review
- No version compatibility issues between local and CI environments
- LocalTesting workflow must execute successfully with Aspire dashboard accessible
- Integration tests must pass locally with same results as CI

### Environment Consistency Enforcement
- Local development environment must match CI environment (.NET 9.0)
- global.json version must be respected locally
- No .NET version downgrades or workarounds permitted
- Aspire orchestration must work locally before CI submission

### Verification Commands Required Before PR Submission
```bash
# Verify .NET version
dotnet --version  # Must return 9.0.x

# Install Aspire workload
dotnet workload install aspire

# Build all solutions
dotnet build FlinkDotNet/FlinkDotNet.sln --configuration Release
dotnet build Sample/Sample.sln --configuration Release  
dotnet build LocalTesting/LocalTesting.sln --configuration Release

# Test LocalTesting workflow
./test-aspire-localtesting.ps1 -MessageCount 1000
```

### Installation Verification for New Developers
```bash
# Check if .NET 9.0 is installed
dotnet --list-sdks | grep "9.0"

# If not installed, download and install .NET 9.0 SDK
# Windows: Download from https://dotnet.microsoft.com/download/dotnet/9.0
# Linux/macOS: Use the dotnet-install script
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version latest --channel 9.0

# Install Aspire workload
dotnet workload install aspire

# Verify installation
dotnet --version  # Should show 9.0.x
```

## AI Agent Build and Test Enforcement (CRITICAL - Rule 14)

### Pre-Change Validation Requirements
- **ALWAYS validate builds and tests** before making ANY functionality changes to code
- **Zero tolerance for introducing build failures** - all builds must pass before and after changes
- **MANDATORY validation sequence** for every code change:
  1. Run `./validate-build-and-tests.ps1` before making changes (establish baseline)
  2. Make minimal, surgical code changes
  3. Run `./validate-build-and-tests.ps1` after changes (verify no regressions)
  4. Fix any new failures immediately before proceeding

### Build Validation Requirements
- All three solutions MUST build successfully: FlinkDotNet, Sample, LocalTesting
- Use Release configuration for validation: `--configuration Release`
- No warnings promoted to errors are acceptable
- All NuGet package dependencies must restore successfully

### Test Validation Requirements
- Run existing tests to ensure no regressions
- Tests that were passing must continue to pass
- Document any test failures in Work Items with root cause analysis
- New functionality must include appropriate test coverage

### Validation Script Usage (MANDATORY - Rule 15)
- **Primary validation script**: `./validate-build-and-tests.ps1`
- **Quick build-only validation**: `./validate-build-and-tests.ps1 -SkipTests`
- **Pre-commit validation**: `./pre-commit-validation.ps1`
- **ALWAYS use scripts instead of manual commands** to ensure consistency

### Build Failure Prevention Strategy (CRITICAL - Rule 16)
- **Incremental change approach**:
  - Make smallest possible changes to achieve functionality goals
  - Validate after each significant change (not just at the end)
  - If build breaks, immediately revert last change and try different approach

### Environment Verification Before Changes
```bash
# Verify .NET version is 9.0.x
dotnet --version

# Ensure clean working directory
git status

# Run baseline validation
./validate-build-and-tests.ps1
```

### Change Validation Workflow
```bash
# After making changes
git status  # Review what was changed
./validate-build-and-tests.ps1  # Validate changes

# If failures occur
git diff  # Review changes made
# Fix issues or revert problematic changes
git checkout <file>  # Revert if needed
```

### Quality Gate Enforcement (CRITICAL - Rule 18)
- **No exceptions to build success requirement** - builds MUST pass before any commit
- **Acceptable test outcomes**:
  - All tests pass: ✅ Ideal outcome
  - Tests pass with same failure count as baseline: ✅ Acceptable (no regressions)
  - Tests pass with fewer failures than baseline: ✅ Improvement
  - Tests fail with more failures than baseline: ❌ UNACCEPTABLE - must fix

### Enforcement Violations
**MAJOR VIOLATIONS (immediate work stoppage required)**:
- Making code changes without running pre-change validation
- Introducing build failures and continuing development
- Bypassing or ignoring validation script failures
- Committing code that breaks builds
- Proceeding with unresolved test regressions

**Recovery Actions**:
- Revert all changes that introduced build failures
- Re-run full validation to establish clean baseline
- Restart development with proper validation procedures
- Update Work Items with lessons learned from violations