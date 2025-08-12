# Build and Test Enforcement

This document outlines the mandatory build and test enforcement rules for FlinkDotNet to ensure code quality and prevent build failures.

## 🚨 Enforcement Rules

### 1. Mandatory Build Success
- **ALL builds MUST pass** before any code changes can be committed or merged
- **Zero tolerance** for build failures in main, develop, or feature branches
- **Automated blocking** of commits that break builds

### 2. .NET 9.0 Environment Requirements
- **Mandatory .NET 9.0.x SDK** for all development
- **Environment validation** required before any development work
- **Automated version checking** in all workflows

### 3. Multi-Solution Validation
All three solutions must build successfully:
- `FlinkDotNet/FlinkDotNet.sln` - Core library
- `Sample/Sample.sln` - Sample applications  
- `LocalTesting/LocalTesting.sln` - Local testing infrastructure

## 🛠️ Validation Tools

### Comprehensive Validation Script
```powershell
# Run complete validation (builds + tests)
./validate-build-and-tests.ps1

# Run only builds (skip tests)
./validate-build-and-tests.ps1 -SkipTests

# Run with Debug configuration
./validate-build-and-tests.ps1 -Configuration Debug
```

### Pre-Commit Validation
```powershell
# Run before committing changes
./pre-commit-validation.ps1
```

### Manual Build Commands
```bash
# Verify .NET version
dotnet --version  # Must return 9.0.x

# Build all solutions
dotnet build FlinkDotNet/FlinkDotNet.sln --configuration Release
dotnet build Sample/Sample.sln --configuration Release  
dotnet build LocalTesting/LocalTesting.sln --configuration Release

# Run tests
dotnet test FlinkDotNet/FlinkDotNet.sln --configuration Release --no-build
dotnet test Sample/Sample.sln --configuration Release --no-build
```

## 🔄 Automated Enforcement

### GitHub Actions Workflow
- **Trigger**: All pushes and pull requests
- **Validation**: Builds all solutions on .NET 9.0
- **Enforcement**: Blocks merges if builds fail
- **Testing**: Runs tests but allows merge with test failures (with warnings)

### Workflow File
`.github/workflows/build-test-validation.yml`

## 📋 Developer Workflow

### Before Making Changes
1. **Verify environment**: `dotnet --version` shows 9.0.x
2. **Baseline validation**: `./validate-build-and-tests.ps1`
3. **Ensure clean state**: All builds and critical tests pass

### During Development
1. **Incremental validation**: Build frequently with `dotnet build`
2. **Test changes**: Run relevant tests for modified components
3. **Address issues immediately**: Fix build errors as they occur

### Before Committing
1. **Run validation**: `./pre-commit-validation.ps1`
2. **Fix any failures**: Build errors must be resolved
3. **Commit only when green**: No build failures allowed

### Pull Request Requirements
1. **All CI checks pass**: GitHub Actions workflow succeeds
2. **Build validation complete**: All solutions build successfully  
3. **Test results reviewed**: Test failures investigated (if any)

## 🚫 Violation Consequences

### Build Failures
- **Immediate blocking**: Commits/merges blocked automatically
- **Required resolution**: Must fix before proceeding
- **No exceptions**: Zero tolerance policy

### Process Violations
- **Pre-commit skipping**: Strongly discouraged, use validation scripts
- **Environment non-compliance**: Update to .NET 9.0 before development
- **Incomplete validation**: Run full validation suite before commits

## 🆘 Troubleshooting

### Common Build Issues
```bash
# Issue: .NET version mismatch
# Solution: Install .NET 9.0 SDK
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version latest --channel 9.0

# Issue: Missing dependencies
# Solution: Restore packages
dotnet restore

# Issue: Stale build artifacts
# Solution: Clean and rebuild
dotnet clean && dotnet build
```

### Environment Setup
```bash
# Verify installation
dotnet --list-sdks | grep "9.0"

# Install Aspire workload (if needed)
dotnet workload install aspire

# Update PATH (Linux/macOS)
export PATH="$HOME/.dotnet:$PATH"
```

### Validation Script Issues
```powershell
# Make scripts executable (Linux/macOS)
chmod +x validate-build-and-tests.ps1
chmod +x pre-commit-validation.ps1

# Run with explicit PowerShell
pwsh ./validate-build-and-tests.ps1
```

## 📈 Continuous Improvement

### Metrics Tracked
- Build success rate across all solutions
- Time to detect and fix build failures
- Compliance with .NET 9.0 requirements
- Test success rates and trends

### Process Refinement
- Monthly review of enforcement effectiveness
- Developer feedback integration
- Tooling improvements based on usage patterns
- Documentation updates for clarity

## 🎯 Success Criteria

### Green Build Status
- ✅ All solutions build without errors or warnings
- ✅ .NET 9.0 compliance verified
- ✅ Validation scripts pass completely
- ✅ CI/CD workflows succeed

### Developer Experience
- 🚀 Fast feedback on build issues
- 🛠️ Clear guidance for issue resolution
- 📊 Transparent enforcement rules
- 🔧 Helpful tooling and automation

---

**Remember**: The goal is to maintain high code quality while providing a smooth development experience. These enforcement rules protect the codebase and help developers catch issues early.