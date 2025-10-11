# 🚀 Quick Start: Build and Test Enforcement

This guide helps developers quickly set up and use the build enforcement system.

## ⚡ Quick Setup (2 minutes)

### 1. Verify .NET 9.0
```bash
dotnet --version
# Should show: 9.0.x (if not, install .NET 9.0 SDK)
```

### 1b. Verify Java and Maven (for Gateway build)
```bash
java -version   # Should be 17+
mvn -version    # Maven available on PATH
```
If Java/Maven are not available, `FlinkDotNet.JobGateway` build will fail because it prebuilds the IR Runner jar.

### 2. Quick Validation
```bash
# Run this before any development work
./scripts/validate-build-and-tests.ps1 -SkipTests
```

### 3. Pre-Commit Check
```bash
# Run this before committing changes  
./scripts/pre-commit-validation.ps1
```

## 🛠️ Daily Developer Commands

### Build Everything
```bash
# One command to build all solutions
./scripts/validate-build-and-tests.ps1 -SkipTests
```

### Build + Test Everything
```bash
# Full validation (builds + tests)
./scripts/validate-build-and-tests.ps1
```

### Manual Build (if scripts fail)
```bash
dotnet build FlinkDotNet/FlinkDotNet.sln --configuration Release
dotnet build Sample/Sample.sln --configuration Release
dotnet build LocalTesting/LocalTesting.sln --configuration Release
```

### Manual Tests (if needed)
```bash
dotnet test FlinkDotNet/FlinkDotNet.sln --configuration Release --no-build
dotnet test Sample/Sample.sln --configuration Release --no-build
```

## 🚨 Emergency Fixes

### Build Broken? 
1. **Clean rebuild**: `dotnet clean && dotnet build`
2. **Check .NET version**: `dotnet --version` (must be 9.0.x)
3. **Restore packages**: `dotnet restore`
4. **Run validation**: `./scripts/validate-build-and-tests.ps1 -SkipTests`

### Wrong .NET Version?
```bash
# Install .NET 9.0 (Linux/macOS)
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version latest --channel 9.0
export PATH="$HOME/.dotnet:$PATH"

# Windows: Download from https://dotnet.microsoft.com/download/dotnet/9.0
```

### Scripts Won't Run?
```bash
# Make executable (Linux/macOS)
chmod +x *.ps1

# Run with PowerShell explicitly
pwsh ./scripts/validate-build-and-tests.ps1
```

## ✅ Before Committing

**Always run this checklist:**
- [ ] `dotnet --version` shows 9.0.x
- [ ] `./scripts/pre-commit-validation.ps1` passes
- [ ] All build errors fixed
- [ ] Ready to commit

## 🎯 Success Indicators

You'll see these when everything works:
- ✅ .NET Version: 9.0.x (✓ .NET 9.0 compliant)  
- ✅ Build succeeded: FlinkDotNet/FlinkDotNet.sln
- ✅ Build succeeded: Sample/Sample.sln
- ✅ Build succeeded: LocalTesting/LocalTesting.sln
- ✅ === VALIDATION SUCCESSFUL ===

## 💡 Pro Tips

- **Run validation early and often** - catch issues quickly
- **Fix build errors immediately** - don't let them accumulate  
- **Use `-SkipTests` during development** - faster feedback
- **Run full validation before commits** - ensure quality

## 📞 Need Help?

- **Build failing?** → Check `docs/BUILD_ENFORCEMENT.md`
- **Environment issues?** → Verify .NET 9.0 installation
- **Script errors?** → Try manual build commands
- **Still stuck?** → Create an issue with error details

---
**Remember**: Builds MUST pass before commits. This protects code quality for everyone!
