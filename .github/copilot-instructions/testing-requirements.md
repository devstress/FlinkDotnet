# Test Coverage Requirements

## Frontend Test Coverage
- **Minimum Coverage**: 70% line coverage required for all frontend tests
- **Testing Framework**: Vitest with @vue/test-utils for Vue 3 components
- **Coverage Tools**: Built-in V8 coverage reporting via Vitest
- **Test Types Required**:
  - Component tests for all Vue components
  - Service tests for API clients and utilities
  - Composable tests for Vue composition functions
  - Integration tests where applicable
- **Coverage Enforcement**: Frontend CI workflow fails if coverage drops below 70%

## Backend Test Coverage  
- **Minimum Coverage**: 70% line coverage required for all backend tests
- **Testing Framework**: xUnit with Moq for mocking
- **Coverage Tools**: dotnet test with XPlat Code Coverage and ReportGenerator
- **Test Types Required**:
  - Unit tests for all business logic and services
  - Integration tests for API controllers
  - Repository pattern tests
  - Domain model validation tests
- **Coverage Enforcement**: Backend CI workflow fails if coverage drops below 70%

## Bundle Integration Test Coverage
- **BDD Testing**: SpecFlow with xUnit for behavior-driven development tests
- **Test Focus**: Full-stack integration scenarios, not code coverage
- **Test Types Required**:
  - End-to-end application startup tests
  - Frontend-backend integration tests
  - API endpoint integration tests
  - Error handling and resilience tests
- **No Coverage Requirement**: Bundle tests focus on behavior validation, not code coverage metrics

## Test Quality Standards
- **Test Naming**: Tests should clearly describe the scenario being tested
- **Test Structure**: Follow Arrange-Act-Assert (AAA) pattern
- **Mocking Strategy**: Mock external dependencies, test internal logic
- **Test Data**: Use meaningful test data that represents real-world scenarios
- **Error Testing**: Include both happy path and error path testing
- **Async Testing**: Proper handling of async operations in tests

## Enforcement Rules
- All CI workflows must pass their respective coverage thresholds
- Pull requests that reduce coverage below thresholds will be rejected
- New features must include comprehensive tests before merge
- Refactoring must maintain or improve test coverage
- Test failures block deployment regardless of coverage metrics

## Test-Driven Development (TDD) and Behavior-Driven Development (BDD) Enforcement

### Test-First Development and Continuous Test Fixing (CRITICAL)
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