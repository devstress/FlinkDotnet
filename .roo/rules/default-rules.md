# # GitHub Copilot Guidelines

This document defines the coding standards and best practices that GitHub Copilot should enforce during code reviews for this .NET project. These guidelines ensure adherence to SOLID principles and .NET best practices, with specialized guidance for BizTalk to Inobiz migrations using .NET 8 and direct XSLT mapping.

## SOLID Principles Enforcement

### Single Responsibility Principle (SRP)
- **Rule**: Each class should have only one reason to change
- **Enforcement**:
  - Flag classes with more than 3 public methods doing unrelated tasks
  - Identify methods longer than 20 lines that handle multiple concerns
  - Suggest splitting classes that handle both business logic and infrastructure concerns (e.g., data access + business logic)
  - Recommend separating UI logic from business logic
  - Flag constructors that perform multiple initialization tasks

**Example Violations to Flag:**
```csharp
// BAD: Class doing too many things
public class UserManager
{
    public void ValidateUser() { }      // Validation
    public void SaveToDatabase() { }    // Data access
    public void SendEmail() { }         // Communication
    public void LogActivity() { }       // Logging
    public void GenerateReport() { }    // Reporting
}
```

**Suggested Fix:**
```csharp
// GOOD: Separated responsibilities
public class UserValidator { }
public class UserRepository { }
public class EmailService { }
public class ActivityLogger { }
public class ReportGenerator { }
```

### Open/Closed Principle (OCP)
- **Rule**: Classes should be open for extension but closed for modification
- **Enforcement**:
  - Flag switch statements or long if-else chains that would require modification to add new types
  - Suggest using interfaces, abstract classes, or strategy patterns
  - Recommend sealed classes when inheritance is not intended
  - Flag hard-coded type checks using `typeof` or `is` patterns

**Example Violations to Flag:**
```csharp
// BAD: Requires modification to add new shapes
public class AreaCalculator
{
    public double Calculate(object shape)
    {
        if (shape is Circle c) return Math.PI * c.Radius * c.Radius;
        if (shape is Rectangle r) return r.Width * r.Height;
        // Adding new shape requires modifying this method
        throw new ArgumentException("Unknown shape");
    }
}
```

**Suggested Fix:**
```csharp
// GOOD: Extensible without modification
public interface IShape
{
    double CalculateArea();
}

public class Circle : IShape
{
    public double Radius { get; set; }
    public double CalculateArea() => Math.PI * Radius * Radius;
}
```

### Liskov Substitution Principle (LSP)
- **Rule**: Derived classes must be substitutable for their base classes
- **Enforcement**:
  - Flag derived classes that throw `NotImplementedException` or `NotSupportedException` for base class methods
  - Identify methods that weaken preconditions or strengthen postconditions
  - Flag inheritance hierarchies where derived classes completely change behavior expectations
  - Suggest composition over inheritance when LSP is violated

**Example Violations to Flag:**
```csharp
// BAD: Violates LSP
public class Bird
{
    public virtual void Fly() { }
}

public class Penguin : Bird
{
    public override void Fly()
    {
        throw new NotSupportedException("Penguins can't fly!");
    }
}
```

**Suggested Fix:**
```csharp
// GOOD: Proper hierarchy
public abstract class Bird { }
public abstract class FlyingBird : Bird
{
    public abstract void Fly();
}
public class Penguin : Bird { }
public class Eagle : FlyingBird { }
```

### Interface Segregation Principle (ISP)
- **Rule**: Clients should not be forced to depend on interfaces they don't use
- **Enforcement**:
  - Flag interfaces with more than 5-7 methods
  - Identify classes implementing interfaces where they throw `NotImplementedException` for some methods
  - Suggest splitting large interfaces into smaller, focused ones
  - Flag interfaces mixing different levels of abstraction

**Example Violations to Flag:**
```csharp
// BAD: Fat interface
public interface IWorker
{
    void Work();
    void Eat();
    void Sleep();
    void Program();
    void DesignUI();
    void TestSoftware();
    void WriteDocumentation();
}
```

**Suggested Fix:**
```csharp
// GOOD: Segregated interfaces
public interface IWorker
{
    void Work();
}

public interface IHuman
{
    void Eat();
    void Sleep();
}

public interface IProgrammer : IWorker
{
    void Program();
}
```

### Dependency Inversion Principle (DIP)
- **Rule**: High-level modules should not depend on low-level modules; both should depend on abstractions
- **Enforcement**:
  - Flag direct instantiation of concrete classes using `new` in business logic classes
  - Suggest dependency injection for external dependencies (database, file system, web services)
  - Flag hard-coded connection strings, file paths, or URLs
  - Recommend abstractions for infrastructure concerns
  - Flag static method calls for non-pure functions

**Example Violations to Flag:**
```csharp
// BAD: Direct dependency on concrete class
public class OrderService
{
    public void ProcessOrder(Order order)
    {
        var emailService = new SmtpEmailService(); // Direct dependency
        var database = new SqlServerDatabase();    // Direct dependency
        
        database.Save(order);
        emailService.Send("Order processed");
    }
}
```

**Suggested Fix:**
```csharp
// GOOD: Depends on abstractions
public class OrderService
{
    private readonly IEmailService _emailService;
    private readonly IOrderRepository _repository;
    
    public OrderService(IEmailService emailService, IOrderRepository repository)
    {
        _emailService = emailService;
        _repository = repository;
    }
    
    public void ProcessOrder(Order order)
    {
        _repository.Save(order);
        _emailService.Send("Order processed");
    }
}
```

## .NET Best Practices Enforcement

### Naming Conventions
- **Classes**: PascalCase (e.g., `UserService`, `OrderProcessor`)
- **Methods**: PascalCase (e.g., `ProcessOrder`, `ValidateUser`)
- **Properties**: PascalCase (e.g., `FirstName`, `IsActive`)
- **Fields**: 
  - Private: camelCase with underscore prefix (e.g., `_userId`, `_connectionString`)
  - Public/Protected: PascalCase
- **Variables**: camelCase (e.g., `userName`, `orderTotal`)
- **Constants**: PascalCase (e.g., `MaxRetryCount`, `DefaultTimeout`)
- **Interfaces**: PascalCase with 'I' prefix (e.g., `IUserService`, `IRepository`)

### Exception Handling
- **Flag**: Empty catch blocks
- **Flag**: Catching `System.Exception` without rethrowing
- **Flag**: Using exceptions for control flow
- **Recommend**: Specific exception types
- **Recommend**: Proper logging in catch blocks
- **Recommend**: Using `using` statements for disposable resources

```csharp
// BAD
try
{
    DoSomething();
}
catch
{
    // Silent failure
}

// GOOD
try
{
    DoSomething();
}
catch (SpecificException ex)
{
    _logger.LogError(ex, "Failed to do something");
    throw; // or handle appropriately
}
```

### Async/Await Best Practices
- **Flag**: Blocking async calls (`.Wait()`, `.Result`)
- **Flag**: Not using `ConfigureAwait(false)` in library code
- **Flag**: Async methods not ending with "Async" suffix
- **Flag**: Using `async void` except for event handlers
- **Recommend**: Task-based return types for async methods

### Memory Management
- **Flag**: Not disposing `IDisposable` objects
- **Recommend**: Using `using` statements
- **Flag**: Potential memory leaks with event handlers
- **Flag**: Unnecessary string concatenation in loops

### Performance Considerations
- **Flag**: LINQ queries that could cause N+1 problems
- **Flag**: Inefficient string operations in loops
- **Flag**: Boxing/unboxing of value types
- **Recommend**: StringBuilder for multiple string concatenations
- **Recommend**: Proper collection initialization

### Security Best Practices
- **Flag**: SQL injection vulnerabilities (string concatenation in SQL queries)
- **Flag**: Hard-coded secrets or passwords
- **Flag**: Unvalidated user input
- **Recommend**: Parameterized queries
- **Recommend**: Input validation and sanitization

### Code Organization
- **Flag**: Classes longer than 300 lines
- **Flag**: Methods longer than 30 lines
- **Flag**: Excessive nesting (more than 3 levels)
- **Flag**: Duplicate code blocks
- **Recommend**: Extract method refactoring
- **Recommend**: Single file per class

### Documentation
- **Require**: XML documentation for public APIs
- **Recommend**: Clear, descriptive method and class names
- **Flag**: Magic numbers without explanation
- **Recommend**: Meaningful variable names

## Code Review Checklist

When reviewing code, ensure the following:

1. **SOLID Principles**
   - [ ] Single Responsibility: Each class has one clear purpose
   - [ ] Open/Closed: Code is extensible without modification
   - [ ] Liskov Substitution: Inheritance hierarchies are proper
   - [ ] Interface Segregation: Interfaces are focused and cohesive
   - [ ] Dependency Inversion: Dependencies are injected, not instantiated

2. **Error Handling**
   - [ ] No empty catch blocks
   - [ ] Appropriate exception types used
   - [ ] Resources properly disposed
   - [ ] Logging included where appropriate

3. **Performance**
   - [ ] No obvious performance bottlenecks
   - [ ] Efficient algorithms and data structures used
   - [ ] Async/await used properly
   - [ ] Memory management considered

4. **Security**
   - [ ] No hard-coded secrets
   - [ ] Input validation implemented
   - [ ] SQL injection prevented
   - [ ] Authentication/authorization considered

5. **Maintainability**
   - [ ] Code is readable and well-organized
   - [ ] Naming conventions followed
   - [ ] Comments explain 'why', not 'what'
   - [ ] No duplicate code

6. **Testing**
   - [ ] Unit tests provided for business logic
   - [ ] Test coverage is adequate
   - [ ] Tests are readable and maintainable

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

### Domain Models
```csharp
// Enforce encapsulation
public class Order
{
    private List<OrderItem> _items = new();
    
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();
    
    public void AddItem(OrderItem item)
    {
        // Business logic here
        _items.Add(item);
    }
}
```

This configuration should guide Copilot to enforce SOLID principles and .NET best practices in all code reviews.

## Test Coverage Requirements

### Frontend Test Coverage
- **Minimum Coverage**: 70% line coverage required for all frontend tests
- **Testing Framework**: Vitest with @vue/test-utils for Vue 3 components
- **Coverage Tools**: Built-in V8 coverage reporting via Vitest
- **Test Types Required**:
  - Component tests for all Vue components
  - Service tests for API clients and utilities
  - Composable tests for Vue composition functions
  - Integration tests where applicable
- **Coverage Enforcement**: Frontend CI workflow fails if coverage drops below 70%

### Backend Test Coverage  
- **Minimum Coverage**: 70% line coverage required for all backend tests
- **Testing Framework**: xUnit with Moq for mocking
- **Coverage Tools**: dotnet test with XPlat Code Coverage and ReportGenerator
- **Test Types Required**:
  - Unit tests for all business logic and services
  - Integration tests for API controllers
  - Repository pattern tests
  - Domain model validation tests
- **Coverage Enforcement**: Backend CI workflow fails if coverage drops below 70%

### Bundle Integration Test Coverage
- **BDD Testing**: SpecFlow with xUnit for behavior-driven development tests
- **Test Focus**: Full-stack integration scenarios, not code coverage
- **Test Types Required**:
  - End-to-end application startup tests
  - Frontend-backend integration tests
  - API endpoint integration tests
  - Error handling and resilience tests
- **No Coverage Requirement**: Bundle tests focus on behavior validation, not code coverage metrics

### Test Quality Standards
- **Test Naming**: Tests should clearly describe the scenario being tested
- **Test Structure**: Follow Arrange-Act-Assert (AAA) pattern
- **Mocking Strategy**: Mock external dependencies, test internal logic
- **Test Data**: Use meaningful test data that represents real-world scenarios
- **Error Testing**: Include both happy path and error path testing
- **Async Testing**: Proper handling of async operations in tests

### Enforcement Rules
- All CI workflows must pass their respective coverage thresholds
- Pull requests that reduce coverage below thresholds will be rejected
- New features must include comprehensive tests before merge
- Refactoring must maintain or improve test coverage
- Test failures block deployment regardless of coverage metrics

## REALITY FILTER - AI Agent Enforcement Rules

• **Never present generated, inferred, speculated, or deduced content as fact.**
• **If you cannot verify something directly, say:**
  - "I cannot verify this."
  - "I do not have access to that information."
  - "My knowledge base does not contain that."
• **Label unverified content at the start of a sentence:**
  - [Inference] [Speculation] [Unverified]
• **Ask for clarification if information is missing. Do not guess or fill gaps.**
• **If any part is unverified, label the entire response.**
• **Do not paraphrase or reinterpret my input unless I request it.**
• **If you use these words, label the claim unless sourced:**
  - Prevent, Guarantee, Will never, Fixes, Eliminates, Ensures that
• **For LLM behavior claims (including yourself), include:**
  - [Inference] or [Unverified], with a note that it's based on observed patterns
• **If you break this directive, say:**
  > Correction: I previously made an unverified claim. That was incorrect and should have been labeled
• **Never override or alter my input unless asked.**

# Work Item Enforcement Rule

## Policy Statement
Every task must be recorded as a Work Item (WI) in the tracking system. Each distinct task requires its own dedicated Work Item to ensure proper tracking, accountability, and process adherence.

## Work Item Lifecycle
All work items must follow this mandatory progression:

### 1. Investigation Phase
- **Requirements**: Research problem scope, gather requirements, analyze dependencies
- **Deliverables**: Problem statement, scope definition, dependency analysis
- **Status**: WI marked as "Investigation"

### 2. Design Phase
- **Requirements**: Create technical design, architecture decisions, interface specifications
- **Deliverables**: Design document, API contracts, system architecture
- **Status**: WI marked as "Design"

### 3. Test-Driven Development (TDD/BDD) Phase
- **Requirements**: Write failing tests first, define behavior specifications
- **Deliverables**: Unit tests, integration tests, behavior specifications
- **Status**: WI marked as "Test Design"

### 4. Coding Phase
- **Requirements**: Implement solution to make tests pass
- **Deliverables**: Production code, code reviews completed
- **Status**: WI marked as "In Development"

### 5. Debug Phase
- **Requirements**: Fix issues, optimize performance, handle edge cases
- **Deliverables**: Bug fixes, performance improvements, edge case handling
- **Status**: WI marked as "Debugging"

### 6. Testing Validation Phase
- **Requirements**: All tests must pass (unit, integration, system, acceptance)
- **Deliverables**: Test execution reports, quality gates passed
- **Status**: WI marked as "Testing"

### 7. Commit Phase
- **Requirements**: Code review approved, all checks passed, ready for deployment
- **Deliverables**: Merged code, deployment artifacts
- **Status**: WI marked as "Done"

### 8. Owner Acceptance Phase
- **Requirements**: Present completed work to task owner for final approval
- **Deliverables**: Owner confirmation of satisfaction with deliverables
- **Status**: WI marked as "Pending Owner Review"

### 9. Work Item Closure Phase
- **Requirements**: Owner approval received, all acceptance criteria met
- **Deliverables**: Work Item deletion/archival, cleanup of related artifacts
- **Status**: WI marked as "Closed" then deleted

## Enforcement Rules

### Rule 1: One Functionality, One Work Item (MANDATORY)
- Each distinct functionality requires exactly ONE Work Item document
- All phases, iterations, and decisions must be documented within the same WI file
- The entire workflow from Investigation → Closure must be visible in one document
- NO separate WIs for different phases of the same functionality
- Sub-tasks may be tracked within sections but must remain in the same WI file
- This enables complete learning and traceability for future reference

### Rule 2: WIs Folder Structure (MANDATORY)
- All Work Items must be created as files in the `WIs/` folder
- File naming convention: `WI[#]_[brief-description].md`
- Example: `WIs/WI1_stress-test-fix.md`
- WI files must contain all phase documentation and progress tracking

### Rule 3: Single Document Lifecycle (MANDATORY)
- Work Items cannot skip phases within the same document
- Each phase must be completed before advancing to the next
- Phase completion requires explicit approval/verification
- ALL phase documentation, iterations, failures, and learnings must be recorded in the SAME WI file
- Include WHY decisions were made, what was tried, what failed, and lessons learned
- Never run into the same solutions and problems twice from the history of the WI.
- Document iterations and refinements within the same WI for complete context

### Rule 4: Traceability Requirements
- All code commits must reference the associated Work Item ID
- All design decisions must be linked to their Work Item
- All test cases must be traceable to their Work Item
- WI file path must be referenced in commit messages

### Rule 5: Status Accuracy
- Work Item status must reflect actual progress
- Status updates are mandatory at phase transitions
- Stale Work Items (>5 days without update) trigger automatic review
- WI file must be updated with each status change

### Rule 6: Mandatory Learning and Problem Prevention (CRITICAL)
- **ALL learnings, failures, and solutions** must be documented in the WI with detailed explanations
- **BEFORE starting any new WI**, you MUST review existing WI files and existing AI-Learning files to learn from previous work
- **Search for similar problems** in the WIs folder or AI-Learning folder and apply lessons learned to avoid repetition
- Document in each new WI: "Lessons Applied from Previous WIs" section referencing specific WI files
- Include specific actions taken to prevent repeating known problems
- **Failure to learn from previous WIs and AI-Learning files and repeat solved problems is a MAJOR violation**
- Each WI must end with actionable lessons for future similar work

### Rule 7: Mandatory Debug-First Investigation (CRITICAL)
- **ALWAYS debug first** to find the root cause during the Investigation phase
- **Cannot proceed to solutions without proper debugging** and evidence collection
- **Must document debug findings** in the WI for future learning and reference
- **Debug section must be updated** for every investigation to save space and maintain consistency
- **Debug information required**:
  - Error messages and stack traces
  - Log file locations and key excerpts
  - System state at time of failure
  - Environment configuration details
  - Reproduction steps and conditions
- **Purpose**: Evidence-based problem solving and knowledge preservation for future debugging
- **Failure to debug first before proposing solutions is a MAJOR violation**

### Rule 8: User Action Prompting (MANDATORY)
- **NEVER wait silently** for user actions without explicit prompts
- **ALWAYS ask user directly** when their action is required to proceed
- **Script Design Philosophy**: Scripts should work standalone first, then fallback to manual instructions
- **Examples of required prompts**:
  - "Please restart Docker Desktop now and let me know when it's ready"
  - "Please run these commands manually: [commands]"
  - "Please check [status] and confirm when complete"
- **Password Prompting**: NEVER attempt interactive password prompting - use manual fallback instead
- **Clear instructions**: Provide specific steps the user needs to take
- **Explicit confirmation**: Ask user to confirm completion before proceeding
- **Purpose**: Prevent confusion about what the system is waiting for
- **Failure to prompt for user actions is a MAJOR violation**

### Rule 9: Prohibition of IMPLEMENTATION_SUMMARY.md Files (MANDATORY)
- **NEVER create IMPLEMENTATION_SUMMARY.md files** - this violates Work Item enforcement
- **ALL work must be tracked through WIs folder** using proper Work Item files (`WIs/WI[#]_[description].md`)
- **Cleanup workflow automatically removes WIs folders** after merge to maintain repository cleanliness
- **Implementation summaries belong in Work Items**, not as standalone files
- **Documentation belongs in the appropriate places**:
  - Technical details: In WI files during development
  - User documentation: In README.md or docs/ folder
  - API documentation: Inline code comments and generated docs
- **Purpose**: Enforce proper work tracking and prevent documentation pollution
- **Failure to follow this rule is a MAJOR violation** requiring immediate file removal and proper WI creation

### Rule 10: Automatic Archiving & Learning Enforcement (CRITICAL)
- ALL Work Items older than 1 month must be reviewed, learned from, and archived
- Learnings from these old WIs must be extracted and written to the AI-Learning/ folder, grouped by topic
- This ensures the agent and developers do not repeat mistakes and continuously improve
- AI agent should remove outdated WIs and enforce learning extraction
- Failure to archive and write learnings after 1 month is a MAJOR violation

## Violations and Consequences

### Minor Violations
- Missing WI references in commits → Automatic rejection
- Incorrect status updates → Warning and mandatory correction

### Major Violations  
- Skipping phases → Work rejection and rework requirement
- Multiple tasks in single WI → WI split mandate
- Untracked work → Immediate work stoppage until WI created
- **Repeating known problems without learning from previous WIs → Complete task restart with mandatory review**
- **Insufficient learning documentation → Work rejection until proper lessons are documented**
- **Failure to update CLAUDE.md when learning new project knowledge → Work rejection and mandatory documentation**
- **Proceeding to solutions without proper debugging → Work rejection and mandatory investigation restart**
- **Creating IMPLEMENTATION_SUMMARY.md files → Immediate file removal and WI creation mandate**

## Implementation Guidelines

### Work Item Creation Template
```markdown
# WI[#]: [Functionality Name]

**File**: `WIs/WI[#]_[brief-description].md`
**Title**: [Component] Brief description  
**Description**: Clear problem statement and acceptance criteria
**Priority**: [High|Medium|Low]
**Component**: [System component]
**Type**: [Investigation|Feature|Bug Fix|Enhancement]
**Assignee**: [Developer responsible]
**Created**: [Date]
**Status**: [Current phase]

## Lessons Applied from Previous WIs
### Previous WI References
- [List specific WI files reviewed]
### Lessons Applied  
- [Specific actions taken to avoid known problems]
### Problems Prevented
- [Specific issues avoided based on previous learnings]

## Phase 1: Investigation
### Requirements
### Debug Information (MANDATORY - Update this section for every investigation)
- **Error Messages**: [Exact error messages and stack traces]
- **Log Locations**: [Specific log files and key excerpts]
- **System State**: [Configuration, environment, running processes]
- **Reproduction Steps**: [Exact steps to reproduce the issue]
- **Evidence**: [Screenshots, command outputs, file contents]
### Findings
### Lessons Learned

## Phase 2: Design  
### Requirements
### Architecture Decisions
### Why This Approach
### Alternatives Considered

## Phase 3: TDD/BDD
### Test Specifications
### Behavior Definitions

## Phase 4: Implementation
### Code Changes
### Challenges Encountered
### Solutions Applied

## Phase 5: Testing & Validation
### Test Results
### Performance Metrics

## Phase 6: Owner Acceptance
### Demonstration
### Owner Feedback
### Final Approval

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
- [Document successful approaches for reuse]
### What Could Be Improved  
- [Document specific improvements for next time]
### Key Insights for Similar Tasks
- [Actionable insights for similar future work]
### Specific Problems to Avoid in Future
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

## Premium AI Usage Tracking (MANDATORY)

### Rule 13: Premium Request Logging and Cost Management (CRITICAL)
- **ALWAYS log premium AI requests** in the premium-request-tracker folder
- **Premium request triggers** include:
  - Advanced code analysis beyond basic capabilities
  - Complex code generation requiring multiple iterations
  - Enhanced debugging with sophisticated reasoning
  - Premium AI features like advanced completions
  - High-complexity problem solving requiring premium models
  - Extended context analysis exceeding standard limits
- **Logging requirements**:
  - **Log immediately** when initiating premium requests
  - **Use structured format**: `TIMESTAMP | REQUEST_TYPE | CONTEXT | JUSTIFICATION | COST_IMPACT`
  - **File naming**: `premium-requests-YYYY-MM.log` in premium-request-tracker folder
  - **Monthly summaries**: Create summary reports using template provided
- **Cost impact classification**:
  - **High Cost**: Complex multi-step analysis, advanced code generation, extended context
  - **Medium Cost**: Standard premium features, moderate complexity analysis
  - **Low Cost**: Basic premium features, simple enhancements
- **Tracking categories**:
  - **ADVANCED_ANALYSIS**: Complex code or system analysis
  - **PREMIUM_COMPLETION**: Advanced code generation and completions
  - **ENHANCED_DEBUGGING**: Sophisticated debugging and troubleshooting
  - **COMPLEX_REASONING**: Multi-step problem solving and planning
  - **EXTENDED_CONTEXT**: Large context analysis and processing
- **Monitoring requirements**:
  - **Weekly review**: Check premium usage patterns
  - **Monthly reporting**: Generate summary reports with cost analysis
  - **Optimization**: Identify opportunities to reduce premium usage
  - **Justification**: Document business value of premium requests
- **Usage optimization**:
  - **Prefer standard features** when sufficient for the task
  - **Batch similar requests** to reduce individual premium calls
  - **Document alternatives** that were considered before using premium features
  - **Regular review** of usage patterns for optimization opportunities

**Example Log Entries:**
```
2025-01-07T14:30:00Z | ADVANCED_ANALYSIS | WI9 | Complex code analysis for premium usage tracking rule | Medium cost
2025-01-07T14:35:00Z | PREMIUM_COMPLETION | WI9 | Advanced code generation for enforcement mechanisms | High cost
2025-01-07T15:00:00Z | ENHANCED_DEBUGGING | WI9 | Sophisticated debugging of test failures | Medium cost
```

**Monthly Summary Requirements:**
- Use template in premium-request-tracker/premium-summary-template.md
- Include cost analysis and optimization recommendations
- Track trends and patterns in premium usage
- Provide business justification for premium requests

- **Failure to log premium requests is a MAJOR violation** requiring immediate logging and process review

---
**Authority**: Engineering Leadership  
**Effective Date**: Implementation Date  
**Review Cycle**: Quarterly  
**Compliance Level**: Mandatory

