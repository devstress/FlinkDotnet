# GitHub Copilot Guidelines - Part 9 of 9
## AI Agent Build and Test Enforcement

> **Navigation**: [Part 8](./default-rules-part-8.md) | [All Parts Index](./README.md)

> **Context from Part 8**: .NET 9.0 environment requirements and local development setup validation

## AI Agent Build and Test Enforcement (MANDATORY)

### Rule 14: Pre-Change Validation Requirements (CRITICAL)
- **ALWAYS validate builds and tests** before making ANY functionality changes to code
- **Zero tolerance for introducing build failures** - all builds must pass before and after changes
- **MANDATORY validation sequence** for every code change:
  1. Run `./validate-build-and-tests.ps1` before making changes (establish baseline)
  2. Make minimal, surgical code changes
  3. Run `./validate-build-and-tests.ps1` after changes (verify no regressions)
  4. Fix any new failures immediately before proceeding
- **Build validation requirements**:
  - All three solutions MUST build successfully: FlinkDotNet, Sample, LocalTesting
  - Use Release configuration for validation: `--configuration Release`
  - No warnings promoted to errors are acceptable
  - All NuGet package dependencies must restore successfully
- **Test validation requirements**:
  - Run existing tests to ensure no regressions
  - Tests that were passing must continue to pass
  - Document any test failures in Work Items with root cause analysis
  - New functionality must include appropriate test coverage

### Rule 15: Validation Script Usage (MANDATORY)
- **Primary validation script**: `./validate-build-and-tests.ps1`
- **Quick build-only validation**: `./validate-build-and-tests.ps1 -SkipTests`
- **Pre-commit validation**: `./pre-commit-validation.ps1`
- **ALWAYS use scripts instead of manual commands** to ensure consistency
- **Script failure handling**:
  - If validation script fails, STOP all development work
  - Debug and fix root cause before proceeding
  - Do NOT bypass or ignore script failures
  - Document failures and resolutions in Work Items

### Rule 16: Build Failure Prevention Strategy (CRITICAL)
- **Incremental change approach**:
  - Make smallest possible changes to achieve functionality goals
  - Validate after each significant change (not just at the end)
  - If build breaks, immediately revert last change and try different approach
- **Environment verification before changes**:
  ```bash
  # Verify .NET version is 9.0.x
  dotnet --version
  
  # Ensure clean working directory
  git status
  
  # Run baseline validation
  ./validate-build-and-tests.ps1
  ```
- **Change validation workflow**:
  ```bash
  # After making changes
  git status  # Review what was changed
  ./validate-build-and-tests.ps1  # Validate changes
  
  # If failures occur
  git diff  # Review changes made
  # Fix issues or revert problematic changes
  git checkout <file>  # Revert if necessary
  ```

### Rule 17: Error Resolution Requirements (MANDATORY)
- **Build errors must be fixed immediately** - no partial commits with build failures
- **Test regression handling**:
  - New failing tests must be investigated and documented
  - Known flaky tests should be identified and handled appropriately
  - Test infrastructure issues (missing browsers, etc.) must be resolved, not ignored
- **Common error resolution patterns**:
  - **Missing using statements**: Add required namespace imports
  - **Assembly reference issues**: Verify project references and NuGet packages
  - **Version compatibility**: Ensure all projects target same .NET version (9.0)
  - **API breaking changes**: Update calling code to match new signatures
- **Documentation requirements**:
  - Document all error resolution steps in Work Items
  - Include error messages, root cause analysis, and solution applied
  - Create searchable knowledge base for future similar issues

### Rule 18: Quality Gate Enforcement (CRITICAL)
- **No exceptions to build success requirement** - builds MUST pass before any commit
- **Acceptable test outcomes**:
  - All tests pass: ✅ Ideal outcome
  - Tests pass with same failure count as baseline: ✅ Acceptable (no regressions)
  - Tests pass with fewer failures than baseline: ✅ Improvement
  - Tests fail with more failures than baseline: ❌ UNACCEPTABLE - must fix
- **Integration with Work Item lifecycle**:
  - Cannot proceed from Investigation to Design phase without clean builds
  - Cannot proceed from Design to Implementation without test plans
  - Cannot proceed from Implementation to Testing without successful builds
  - Cannot close Work Item without full validation success

### Rule 19: Automation and Tool Usage (MANDATORY)
- **Always use existing automation** rather than manual processes
- **Available validation tools**:
  - `./validate-build-and-tests.ps1` - Comprehensive validation
  - `./pre-commit-validation.ps1` - Quick pre-commit checks
  - `./build-all.ps1` - Build all solutions
  - `./test-aspire-localtesting.ps1` - LocalTesting validation
- **PowerShell script execution requirements**:
  - Ensure PowerShell execution policy allows script execution
  - Use `-ExecutionPolicy Bypass` if needed for validation scripts
  - Report any script execution issues immediately
- **Tool enhancement**:
  - If validation tools are missing capabilities, enhance them first
  - Don't work around tool limitations - fix the tools
  - Maintain and improve automation continuously

### Rule 20: Failure Recovery Procedures (CRITICAL)
- **When builds fail after changes**:
  1. Immediately run `git diff` to review all changes made
  2. Identify the minimal change that might have caused the failure
  3. Use `git checkout <file>` to revert suspect changes
  4. Re-run validation to confirm recovery
  5. Approach the problem differently with smaller changes
- **When tests fail after changes**:
  1. Determine if failures are new (regression) or pre-existing
  2. For new failures: debug root cause and fix immediately
  3. For pre-existing failures: document and proceed (no regression)
  4. NEVER ignore test failures without understanding root cause
- **Environment recovery**:
  - If .NET environment becomes inconsistent, reinstall .NET 9.0 SDK
  - If dependencies are corrupted, run `dotnet clean` and `dotnet restore`
  - If workspace is polluted, start with clean git checkout
- **Escalation procedures**:
  - If issues cannot be resolved quickly, document in Work Item and ask for guidance
  - Include full error messages, environment details, and steps attempted
  - Don't continue with unresolved build/test failures

### Enforcement Violations and Consequences

**MAJOR VIOLATIONS (immediate work stoppage required)**:
- Making code changes without running pre-change validation
- Introducing build failures and continuing development
- Bypassing or ignoring validation script failures
- Committing code that breaks builds
- Proceeding with unresolved test regressions

**MINOR VIOLATIONS (immediate correction required)**:
- Using manual commands instead of validation scripts
- Incomplete error documentation in Work Items
- Not following incremental change approach

**Recovery Actions**:
- Revert all changes that introduced build failures
- Re-run full validation to establish clean baseline
- Restart development with proper validation procedures
- Update Work Items with lessons learned from violations

---

> **Complete Guidelines**: This concludes the 9-part GitHub Copilot Guidelines. For navigation, see [All Parts Index](./README.md)