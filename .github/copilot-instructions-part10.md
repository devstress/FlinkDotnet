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

> **Note**: This is the final chunk of copilot-instructions.md covering error resolution, quality gates, automation tools, and failure recovery procedures. For build failure prevention, see Part 9. For AI agent validation requirements, see Parts 8-9.