# FlinkDotNet Scripts

This directory contains utility scripts for development, testing, and CI/CD workflows.

## Git Hooks

### Pre-Commit Hook

The pre-commit hook automatically runs `dotnet format` on all changed C# files before each commit. This ensures consistent code formatting across the project.

**Installation:**

Linux/macOS:
```bash
./scripts/install-git-hooks.sh
```

Windows:
```powershell
.\scripts\install-git-hooks.ps1
```

**Features:**
- ✅ Automatically formats C# code before commit
- ✅ Only processes changed files for performance
- ✅ Works with multiple solution files
- ✅ Provides clear success/error messages
- ✅ Re-stages formatted files automatically

**Files:**
- `pre-commit` - The actual pre-commit hook script (bash)
- `install-git-hooks.sh` - Installation script for Linux/macOS
- `install-git-hooks.ps1` - Installation script for Windows

### Manual Hook Management

**View installed hooks:**
```bash
ls -la .git/hooks/
```

**Temporarily disable hook:**
```bash
git commit --no-verify -m "Your message"
```

**Uninstall hook:**
```bash
rm .git/hooks/pre-commit
```

**Restore from backup:**
```bash
# If you have a backup from installation
cp .git/hooks/pre-commit.backup.YYYYMMDD_HHMMSS .git/hooks/pre-commit
chmod +x .git/hooks/pre-commit
```

## Build and Test Scripts

### validate-build-and-tests.ps1

Validates that all solutions build and all tests pass. This is the comprehensive validation script that should be run before submitting PRs.

**Usage:**
```powershell
# Run all builds and tests
.\validate-build-and-tests.ps1

# Skip tests (build only)
.\validate-build-and-tests.ps1 -SkipTests
```

### pre-commit-validation.ps1

Quick pre-commit validation for faster feedback. Runs a subset of checks suitable for pre-commit validation.

**Usage:**
```powershell
.\pre-commit-validation.ps1
```

## Other Scripts

Additional scripts in this directory support various development workflows. See individual script files for documentation.

## Contributing Scripts

When adding new scripts:

1. **Add documentation** - Include comments explaining what the script does
2. **Make executable** - For shell scripts: `chmod +x script-name.sh`
3. **Test thoroughly** - Test on both Linux/macOS and Windows if applicable
4. **Update this README** - Add the new script to this documentation
5. **Follow conventions**:
   - Shell scripts: `.sh` extension, bash shebang
   - PowerShell scripts: `.ps1` extension, proper error handling
   - Use descriptive names: `verb-noun.ext` (e.g., `install-git-hooks.sh`)

---

For complete development guidelines, see [TODO/DEVELOPMENT_RULES.md](../TODO/DEVELOPMENT_RULES.md).
