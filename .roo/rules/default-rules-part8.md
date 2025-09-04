- **Enterprise-level documentation standards**:
  - Clear separation of concerns in layer descriptions
  - Professional visual design and color schemes
  - Comprehensive component descriptions with business value
  - Technology stack specifications with version requirements
  - Data flow diagrams with security considerations
  - Scalability and performance characteristics
  - Integration patterns and API design rationale
- **Quality requirements**:
  - All documentation must reflect enterprise, world-class standards
  - Visual elements must be professional and consistent
  - Technical descriptions must be precise and comprehensive
  - Documentation must be accessible to both technical and business stakeholders
- **Failure to update architecture documentation is a MAJOR violation** requiring immediate correction

## Test-Driven Development (TDD) and Behavior-Driven Development (BDD) Enforcement (MANDATORY)

### Rule 12: Test-First Development and Continuous Test Fixing (CRITICAL)
- **ALWAYS follow TDD and BDD principles** in all development work
- **Test-first approach required**:
  - Write failing tests before implementing features
  - Implement minimal code to make tests pass
  - Refactor while maintaining test coverage
  - All tests must pass before considering work complete
- **Test fixing requirements**:
  - **Fix ALL failing tests** - never leave broken tests
  - **No skipping tests** unless there's a documented infrastructure limitation
  - **Retry and debug** failing tests until they pass
  - **Document test fixes** in Work Items for future reference
- **BDD scenario requirements**:
  - All BDD scenarios must have corresponding step definitions
  - Step definitions must be implemented and working
  - Feature files must align with business requirements
  - Integration tests must validate full system behavior
- **CI/CD test requirements**:
  - All tests must pass in CI environment
  - Local and CI test results must be consistent
  - Infrastructure issues (browser installation, etc.) must be resolved, not skipped
  - Test failures in CI must be debugged and fixed immediately
- **Test coverage enforcement**:
  - Maintain or improve test coverage with each change
  - Add tests for new functionality before implementation
  - Ensure both unit and integration test coverage
  - Document test scenarios and expected outcomes
- **Debugging requirement**:
  - **Debug test failures thoroughly** before implementing fixes
  - **Document debugging process** and findings in Work Items
  - **Identify root causes** rather than applying quick fixes
  - **Test environment consistency** between local and CI must be maintained
- **Failure to fix all tests is a MAJOR violation** requiring immediate attention and resolution

## .NET 9.0 Local Development Environment Enforcement (MANDATORY)

### Rule 13: .NET 9.0 Environment Requirements (CRITICAL)
- **MANDATORY .NET 9.0 SDK**: All local development must use .NET 9.0.303 or later
- **Before submitting any GitHub workflow or PR**, developers MUST verify:
  - Local environment has .NET 9.0 SDK installed (`dotnet --version` returns 9.0.x)
  - Aspire workload is installed and functional
  - All solutions build successfully locally with .NET 9.0
  - LocalTesting workflow executes successfully locally
- **Local environment setup requirements**:
  - .NET 9.0 SDK installation using official Microsoft installer
  - Aspire workload installation (`dotnet workload install aspire`)
  - Docker Desktop running for Aspire orchestration
  - LocalTesting solution builds and runs without errors
- **GitHub workflow local validation**:
  - ALL GitHub workflows must pass locally before submission for review
  - No version compatibility issues between local and CI environments
  - LocalTesting workflow must execute successfully with Aspire dashboard accessible
  - Integration tests must pass locally with same results as CI
- **Environment consistency enforcement**:
  - Local development environment must match CI environment (.NET 9.0)
  - global.json version must be respected locally
  - No .NET version downgrades or workarounds permitted
  - Aspire orchestration must work locally before CI submission
- **Verification commands required before PR submission**:
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

> **Note**: This chunk covers architecture documentation standards, TDD/BDD enforcement, and .NET 9.0 environment requirements. For more .NET 9.0 details and AI agent build enforcement, see Part 9-10.