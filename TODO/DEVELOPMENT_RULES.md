# FlinkDotNet Development Rules

This document outlines mandatory development rules and best practices that all contributors must follow when working on FlinkDotNet.

## Mandatory Pre-Commit Rules

### Rule 1: Always Run `dotnet format` Before Committing

**Status:** ✅ **MANDATORY - Enforced by pre-commit hook**

**Description:** All code changes MUST be formatted using `dotnet format` before committing to ensure consistent code style across the project.

**Why This Rule Exists:**
- Maintains consistent code style across the entire codebase
- Prevents merge conflicts caused by formatting differences
- Improves code readability and maintainability
- Enforces project's EditorConfig rules automatically
- Reduces code review noise by eliminating style discussions

**Manual Execution:**
```bash
# Format all code in the FlinkDotNet solution
cd FlinkDotNet
dotnet format FlinkDotNet.sln

# Format all code in the Sample solution
cd Sample
dotnet format Sample.sln

# Format all code in the LocalTesting solution
cd LocalTesting
dotnet format LocalTesting.sln

# Verify formatting without making changes
dotnet format FlinkDotNet.sln --verify-no-changes
```

**Automated Enforcement:**
The repository includes a pre-commit hook that automatically runs `dotnet format` before each commit. To install the pre-commit hook:

```bash
# From the repository root
./scripts/install-git-hooks.sh

# Or on Windows
.\scripts\install-git-hooks.ps1
```

**Pre-Commit Hook Behavior:**
1. When you run `git commit`, the pre-commit hook automatically executes
2. The hook runs `dotnet format` on all changed .cs files
3. If formatting changes are detected:
   - The hook stages the formatted files
   - The commit proceeds with properly formatted code
4. If formatting fails (e.g., syntax errors):
   - The commit is aborted
   - You must fix the errors before committing

**Bypass (NOT RECOMMENDED):**
In rare cases where you need to bypass the hook:
```bash
git commit --no-verify -m "Your message"
```
⚠️ **Warning:** Bypassing the pre-commit hook is strongly discouraged and may result in CI failures.

### Rule 2: Build Must Pass Before Committing

**Status:** ✅ **MANDATORY**

**Description:** All code must build successfully without errors before committing.

**Validation:**
```bash
# Build all solutions in Release configuration
dotnet build FlinkDotNet/FlinkDotNet.sln --configuration Release
dotnet build Sample/Sample.sln --configuration Release
dotnet build LocalTesting/LocalTesting.sln --configuration Release
```

**Pre-Commit Hook:** The pre-commit hook verifies that the code builds successfully after formatting.

### Rule 3: Run Tests Before Committing (Recommended)

**Status:** 🔶 **HIGHLY RECOMMENDED**

**Description:** Run relevant tests before committing to catch regressions early.

**Test Execution:**
```bash
# Run unit tests
dotnet test FlinkDotNet/FlinkDotNet.sln

# Run integration tests
dotnet test LocalTesting/LocalTesting.sln

# Run all tests
./validate-build-and-tests.ps1
```

**Note:** While not enforced by the pre-commit hook due to performance considerations, running tests before committing is a best practice that prevents breaking changes.

## Code Quality Rules

### Rule 4: Follow SOLID Principles

**Status:** ✅ **MANDATORY - Enforced by code review**

All code must follow SOLID principles as outlined in `.github/copilot-instructions.md`:
- **S**ingle Responsibility Principle
- **O**pen/Closed Principle
- **L**iskov Substitution Principle
- **I**nterface Segregation Principle
- **D**ependency Inversion Principle

See [GitHub Copilot Guidelines](../.github/copilot-instructions.md) for detailed enforcement rules.

### Rule 5: No Compiler Warnings

**Status:** ✅ **MANDATORY - Enforced by CI**

Code must compile without warnings in Release configuration.

### Rule 6: Follow EditorConfig Rules

**Status:** ✅ **MANDATORY - Enforced by dotnet format**

All code must follow the rules defined in `.editorconfig` at the repository root. The `dotnet format` tool automatically enforces these rules.

## Documentation Rules

### Rule 7: Update Documentation for API Changes

**Status:** ✅ **MANDATORY - Enforced by code review**

When making API changes:
- Update XML documentation comments
- Update README.md if public API changes
- Update relevant documentation in `docs/` folder
- Update TODO folder if affecting roadmap

### Rule 8: Update Architecture Documentation

**Status:** ✅ **MANDATORY - Enforced by code review**

When making architectural changes, update:
- `docs/system-architecture-diagram.png`
- `docs/system-architecture.html`
- README.md architecture section

## Git Workflow Rules

### Rule 9: Meaningful Commit Messages

**Status:** ✅ **MANDATORY - Enforced by code review**

Commit messages must:
- Be clear and descriptive
- Start with a capital letter
- Use imperative mood (e.g., "Add feature" not "Added feature")
- Reference Work Item IDs when applicable: `[WI#] Brief description`

**Examples:**
```
Add dotnet format pre-commit hook
Fix null reference exception in JobMaster
[WI1] Implement JobManager REST API endpoints
```

### Rule 10: Small, Focused Commits

**Status:** ✅ **MANDATORY - Enforced by code review**

- Make small, incremental commits
- Each commit should represent a single logical change
- Avoid mixing unrelated changes in one commit

## Testing Rules

### Rule 11: Test Coverage Requirements

**Status:** ✅ **MANDATORY - Enforced by CI**

- Frontend tests: Minimum 70% line coverage
- Backend tests: Minimum 70% line coverage
- All new features must include tests
- Follow Arrange-Act-Assert (AAA) pattern

### Rule 12: No Skipped Tests

**Status:** ✅ **MANDATORY - Enforced by CI**

- Fix all failing tests, don't skip them
- If infrastructure prevents test execution, fix the infrastructure
- Document any temporary test skips with issue tracking

## Security Rules

### Rule 13: Run CodeQL Security Scanner

**Status:** ✅ **MANDATORY - Enforced by CI**

Before finalizing work:
- Run CodeQL security scanner
- Fix all discovered vulnerabilities
- Document any false positives

### Rule 14: No Secrets in Code

**Status:** ✅ **MANDATORY - Enforced by CI and code review**

Never commit:
- API keys
- Passwords
- Connection strings with credentials
- Access tokens
- Private keys

Use configuration files, environment variables, or secret management systems.

## Performance Rules

### Rule 15: Validate Performance Impact

**Status:** 🔶 **RECOMMENDED - Enforced by code review for critical paths**

For changes affecting performance-critical code:
- Run performance benchmarks
- Document performance impact in PR description
- Ensure no significant regressions

## Enforcement Summary

| Rule | Enforcement Method | Strictness |
|------|-------------------|------------|
| 1. dotnet format | Pre-commit hook | ✅ Mandatory |
| 2. Build passes | Pre-commit hook | ✅ Mandatory |
| 3. Run tests | Developer responsibility | 🔶 Recommended |
| 4. SOLID principles | Code review | ✅ Mandatory |
| 5. No warnings | CI build | ✅ Mandatory |
| 6. EditorConfig | dotnet format | ✅ Mandatory |
| 7. API documentation | Code review | ✅ Mandatory |
| 8. Architecture docs | Code review | ✅ Mandatory |
| 9. Commit messages | Code review | ✅ Mandatory |
| 10. Small commits | Code review | ✅ Mandatory |
| 11. Test coverage | CI tests | ✅ Mandatory |
| 12. No skipped tests | CI tests | ✅ Mandatory |
| 13. CodeQL scan | CI security | ✅ Mandatory |
| 14. No secrets | CI + Review | ✅ Mandatory |
| 15. Performance | Code review | 🔶 Recommended |

## Installing Development Tools

### Prerequisites

- .NET 9.0 SDK or later
- Git
- Docker Desktop (for integration tests)

### Setup Steps

1. **Clone the repository:**
   ```bash
   git clone https://github.com/devstress/FlinkDotnet.git
   cd FlinkDotnet
   ```

2. **Install pre-commit hooks:**
   ```bash
   # Linux/macOS
   ./scripts/install-git-hooks.sh
   
   # Windows
   .\scripts\install-git-hooks.ps1
   ```

3. **Verify dotnet format works:**
   ```bash
   cd FlinkDotNet
   dotnet format FlinkDotNet.sln --verify-no-changes
   ```

4. **Build all solutions:**
   ```bash
   ./validate-build-and-tests.ps1
   ```

## Troubleshooting

### Pre-Commit Hook Issues

**Problem:** Pre-commit hook fails with "dotnet format not found"
```bash
# Solution: Ensure .NET SDK is installed and in PATH
dotnet --version
```

**Problem:** Pre-commit hook takes too long
```bash
# The hook only formats changed files, not the entire solution
# If it's still too slow, temporarily bypass with --no-verify
git commit --no-verify -m "Your message"
```

**Problem:** Pre-commit hook not executing
```bash
# Solution: Ensure the hook is executable
chmod +x .git/hooks/pre-commit

# Or reinstall
./scripts/install-git-hooks.sh
```

### Formatting Issues

**Problem:** dotnet format changes too many files
```bash
# Solution: Only format your changes
dotnet format FlinkDotNet.sln --include <your-file>.cs
```

**Problem:** Formatting conflicts with my editor settings
```bash
# Solution: Configure your editor to use .editorconfig
# Most modern editors support EditorConfig natively
```

## Contributing

For complete contribution guidelines, see [CONTRIBUTING.md](../CONTRIBUTING.md).

For project structure and architecture, see [README.md](README.md).

---

**Last Updated:** November 2025
**Status:** Active and enforced
