## Automatic Checks

The following should be automatically flagged during code review:

- Methods with cyclomatic complexity > 10
- Classes with more than 500 lines
- Files with more than 1000 lines
- Public members without XML documentation
- Use of `var` where the type is not obvious
- Missing null checks for nullable parameters
- Incorrect disposal patterns
- Thread safety issues in shared code

## Review Guidelines for Common Patterns

### Repository Pattern
```csharp
// Enforce interface segregation
public interface IUserRepository
{
    Task<User> GetByIdAsync(int id);
    Task SaveAsync(User user);
}

// Don't create generic catch-all repositories
// BAD: public interface IRepository<T> { /* too many methods */ }
```

### Service Layer
```csharp
// Enforce single responsibility
public interface IOrderService
{
    Task ProcessOrderAsync(Order order);
}

// Don't mix concerns
// BAD: IOrderServiceWithEmailAndLogging
```

## AI Agent Build and Test Enforcement (MANDATORY)

### Rule 14: Pre-Change Validation Requirements (CRITICAL)
- **ALWAYS validate builds and tests** before making ANY functionality changes to code
- **Zero tolerance for introducing build failures** - all builds must pass before and after changes
- **MANDATORY validation sequence** for every code change:
  1. Run `./validate-build-and-tests.ps1` before making changes (establish baseline)
  2. Make minimal, surgical code changes
  3. Run `./validate-build-and-tests.ps1` after changes (verify no regressions)
  4. Fix any new failures immediately before proceeding

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

> **Note**: This chunk covers .NET 9.0 environment details and AI agent build enforcement. For error resolution and quality gates, see Part 10. For TDD/BDD enforcement, see Part 8.