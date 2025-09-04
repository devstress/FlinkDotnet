# GitHub Copilot Guidelines - Part 7 of 9
## Work Item Templates + Architecture Documentation + TDD Enforcement

> **Navigation**: [Part 6](./default-rules-part-6.md) | [Part 8](./default-rules-part-8.md) | [All Parts Index](./README.md)

> **Context from Part 6**: Work Item template structure and required sections for problem tracking

- [Detailed list of problems and how to prevent them]
### Reference for Future WIs
- [What future developers should know before starting similar work]
```

### Commit Message Format
```
[WI#] Brief description of change

Detailed description of what was changed and why.

Work Item: WI#
Phase: [Investigation|Design|Test Design|Development|Debugging|Testing]
```

## Tools and Integration
- Work Item tracking system integration required
- Automated phase transition notifications
- Commit hooks for WI reference validation
- Dashboard for WI lifecycle visibility

## Review and Compliance
- Weekly WI hygiene reviews
- Monthly process compliance audits
- Quarterly rule effectiveness assessment

## Architecture Documentation Maintenance (MANDATORY)

### Rule 11: System Architecture Documentation Updates (CRITICAL)
- **ALWAYS update system architecture documentation** when making architecture or system design changes
- **Required file updates for architecture changes**:
  - `docs/system-architecture-diagram.png` - Visual system architecture diagram
  - `docs/system-architecture.html` - Interactive HTML architecture documentation
  - `README.md` - System design section and architecture overview
- **Architecture change triggers** include:
  - New API endpoints or protocols (REST, GraphQL, gRPC)
  - Database schema changes or new database providers
  - New infrastructure components (caching, message queues, search engines)
  - Authentication/authorization mechanism changes
  - New external integrations or client interfaces
  - Performance optimization changes affecting system behavior
  - Security enhancements that modify data flow
  - Deployment or hosting configuration changes
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

> **Continues in**: [Part 8](./default-rules-part-8.md) - .NET 9.0 Environment Requirements and Local Development Setup