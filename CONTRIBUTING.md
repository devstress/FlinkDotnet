# Contributing to FlinkDotNet

Thank you for your interest in contributing to FlinkDotNet! This document provides guidelines for contributing to this .NET Apache Flink integration solution.

## Table of Contents

- [Getting Started](#getting-started)
- [Development Environment Setup](#development-environment-setup)
- [Project Structure](#project-structure)
- [Development Workflow](#development-workflow)
- [Coding Standards](#coding-standards)
- [Testing Requirements](#testing-requirements)
- [Pull Request Process](#pull-request-process)
- [Documentation Guidelines](#documentation-guidelines)
- [Release Process](#release-process)
- [Getting Help](#getting-help)

## Getting Started

FlinkDotNet is a comprehensive Apache Flink integration solution that enables .NET developers to build and submit streaming jobs to Apache Flink clusters. The project includes:

- **FlinkDotNet.DataStream**: Modern streaming API aligned with Python Flink
- **Flink.JobBuilder**: Legacy fluent C# DSL (backward compatible)
- **FlinkDotNet.Orchestration**: Multi-cluster orchestration with Temporal workflows
- **FlinkDotNet.Gateway**: HTTP service bridging .NET and Apache Flink

### Prerequisites

- **.NET 9.0 SDK** or higher ([Download](https://dotnet.microsoft.com/download/dotnet/9.0))
- **Docker Desktop** for local development and testing
- **Git** for version control
- **IDE**: Visual Studio, JetBrains Rider, or VS Code
- **Java 17** for Apache Flink integration
- **Apache Kafka** for stream processing (included in Docker setup)

## Development Environment Setup

### 1. Install .NET 9.0 SDK

```bash
# Verify .NET version
dotnet --version  # Should return 9.0.x

# If not installed, download from https://dotnet.microsoft.com/download/dotnet/9.0
# Or use the included script:
./dotnet-install.sh --version 9.0.303
```

### 2. Install Required Workloads

```bash
# Install Aspire workload for local orchestration
dotnet workload install aspire

# Verify Aspire installation
dotnet workload list  # Should show aspire as installed
```

### 3. Clone and Setup Repository

```bash
# Clone the repository
git clone https://github.com/devstress/FlinkDotnet.git
cd FlinkDotnet

# Restore all solutions
dotnet restore FlinkDotNet/FlinkDotNet.sln
dotnet restore Sample/Sample.sln
dotnet restore LocalTesting/LocalTesting.sln
```

### 4. Build All Components

```bash
# Use the provided build script
./build-all.ps1

# Or build individually
dotnet build FlinkDotNet/FlinkDotNet.sln --configuration Release
dotnet build Sample/Sample.sln --configuration Release
dotnet build LocalTesting/LocalTesting.sln --configuration Release
```

### 5. Local Development with Aspire

```bash
# Start local development environment
cd LocalTesting
dotnet run --project LocalTesting.AppHost

# This starts:
# - Apache Flink cluster
# - Apache Kafka
# - FlinkDotNet Gateway
# - Test applications
```

## Project Structure

```
FlinkDotNet/
├── FlinkDotNet/                    # Core libraries
│   ├── FlinkDotNet.Common/         # Core types and configuration
│   ├── FlinkDotNet.DataStream/     # Modern streaming API
│   ├── FlinkDotNet.Orchestration/  # Multi-cluster orchestration
│   ├── FlinkDotNet.ClusterManager/ # Cluster management
│   ├── FlinkDotNet.Temporal/       # Temporal workflow definitions
│   ├── FlinkDotNet.Resilience/     # Fault tolerance patterns
│   ├── Flink.JobBuilder/           # Legacy fluent API
│   └── FlinkDotNet.Testing/        # Testing utilities
├── Sample/                         # Example applications
│   ├── FlinkJobBuilder.Sample/     # Basic examples
│   ├── FlinkDotNet.Aspire.AppHost/ # Aspire orchestration
│   └── FlinkDotNet.Aspire.IntegrationTests/ # BDD tests
├── LocalTesting/                   # Local development environment
├── docs/                          # Documentation
│   ├── wiki/                      # Detailed guides
│   └── observability/             # Monitoring guides
├── .github/workflows/             # CI/CD workflows
└── WIs/                          # Work Items (development tracking)
```

## Development Workflow

### Work Item Enforcement

**EVERY task must be tracked as a Work Item (WI) in the `WIs/` folder.**

1. **Create Work Item**:
   ```bash
   # Create new WI file
   touch WIs/WI[#]_[brief-description].md
   ```

2. **Work Item Lifecycle**:
   - Investigation → Design → TDD/BDD → Coding → Debug → Testing → Commit → Owner Acceptance → Closure

3. **Required Phases**:
   - **Investigation**: Research, requirements gathering, dependency analysis
   - **Design**: Technical design, architecture decisions
   - **Test-Driven Development**: Write failing tests first
   - **Implementation**: Code to make tests pass
   - **Testing**: All tests must pass
   - **Documentation**: Update relevant documentation

### Branch Strategy

```bash
# Create feature branch
git checkout -b feature/WI[#]-brief-description

# Create bugfix branch
git checkout -b bugfix/WI[#]-brief-description

# Create documentation branch
git checkout -b docs/WI[#]-brief-description
```

### Commit Message Format

```
[WI#] Brief description of change

Detailed description of what was changed and why.

Work Item: WI#
Phase: [Investigation|Design|Development|Testing]
```

## Coding Standards

### .NET Best Practices

#### SOLID Principles Enforcement

**Single Responsibility Principle (SRP)**
- Each class should have only one reason to change
- Flag classes with more than 3 public methods doing unrelated tasks
- Separate business logic from infrastructure concerns

```csharp
// BAD: Class doing too many things
public class UserManager
{
    public void ValidateUser() { }      // Validation
    public void SaveToDatabase() { }    // Data access
    public void SendEmail() { }         // Communication
}

// GOOD: Separated responsibilities
public class UserValidator { }
public class UserRepository { }
public class EmailService { }
```

**Dependency Inversion Principle (DIP)**
- Depend on abstractions, not concrete implementations
- Use dependency injection for external dependencies

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
}
```

#### Naming Conventions

- **Classes**: PascalCase (`UserService`, `OrderProcessor`)
- **Methods**: PascalCase (`ProcessOrder`, `ValidateUser`)
- **Properties**: PascalCase (`FirstName`, `IsActive`)
- **Private Fields**: camelCase with underscore prefix (`_userId`, `_connectionString`)
- **Variables**: camelCase (`userName`, `orderTotal`)
- **Constants**: PascalCase (`MaxRetryCount`, `DefaultTimeout`)
- **Interfaces**: PascalCase with 'I' prefix (`IUserService`, `IRepository`)

#### Exception Handling

```csharp
// GOOD: Specific exception handling with logging
try
{
    await ProcessOrderAsync(order);
}
catch (OrderValidationException ex)
{
    _logger.LogError(ex, "Order validation failed for order {OrderId}", order.Id);
    throw; // Re-throw to maintain stack trace
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error processing order {OrderId}", order.Id);
    throw;
}
```

#### Async/Await Best Practices

```csharp
// GOOD: Proper async method naming and return types
public async Task<OrderResult> ProcessOrderAsync(Order order)
{
    using var httpClient = _httpClientFactory.CreateClient();
    var response = await httpClient.PostAsJsonAsync("/api/orders", order)
        .ConfigureAwait(false); // Use ConfigureAwait(false) in library code
    
    return await response.Content.ReadFromJsonAsync<OrderResult>()
        .ConfigureAwait(false);
}
```

### Code Quality Requirements

- **Methods**: Maximum 30 lines
- **Classes**: Maximum 300 lines
- **Cyclomatic Complexity**: Maximum 10
- **Nesting Levels**: Maximum 3 levels
- **XML Documentation**: Required for all public APIs

## Testing Requirements

### Test Coverage Standards

- **Minimum Coverage**: 70% line coverage required for both frontend and backend
- **Frontend**: Vitest with @vue/test-utils for Vue 3 components
- **Backend**: xUnit with Moq for mocking
- **BDD Integration**: SpecFlow with xUnit for behavior-driven development

### Test Categories

#### Unit Tests
```bash
# Run unit tests
dotnet test --filter "Category=unit_test"

# Check coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

#### Integration Tests
```bash
# Run integration tests
dotnet test --filter "Category=integration_test"
```

#### BDD Tests (Behavior-Driven Development)
```bash
# Run BDD scenarios
cd Sample/FlinkDotNet.Aspire.IntegrationTests
dotnet test --filter "Category=stress_test"
dotnet test --filter "Category=complex_logic_test"
dotnet test --filter "Category=backpressure_test"
```

#### Test-Driven Development (TDD) Requirements

**ALWAYS follow TDD principles:**

1. **Write failing tests first**
2. **Implement minimal code to make tests pass**
3. **Refactor while maintaining test coverage**
4. **Fix ALL failing tests** - never leave broken tests
5. **Document test scenarios and expected outcomes**

```csharp
// Example TDD approach
[Fact]
public async Task ProcessOrder_WithValidOrder_ShouldReturnSuccess()
{
    // Arrange
    var order = new Order { Id = 1, Amount = 100 };
    var service = new OrderService(_mockRepository.Object, _mockLogger.Object);
    
    // Act
    var result = await service.ProcessOrderAsync(order);
    
    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(order.Id, result.OrderId);
}
```

### Local Testing Workflow

```bash
# Run local testing with Aspire
./test-aspire-localtesting.ps1 -MessageCount 1000

# Verify all tests pass locally before PR submission
dotnet test
```

## Pull Request Process

### Before Submitting PR

1. **Environment Verification**:
   ```bash
   # Verify .NET 9.0 environment
   dotnet --version  # Must return 9.0.x
   dotnet workload list  # Must show aspire installed
   
   # Build all solutions locally
   dotnet build FlinkDotNet/FlinkDotNet.sln --configuration Release
   dotnet build Sample/Sample.sln --configuration Release
   dotnet build LocalTesting/LocalTesting.sln --configuration Release
   ```

2. **Test Validation**:
   ```bash
   # All tests must pass locally
   dotnet test
   
   # LocalTesting workflow must execute successfully
   ./test-aspire-localtesting.ps1 -MessageCount 1000
   ```

3. **Code Quality Checks**:
   ```bash
   # Run linting (if available)
   dotnet format --verify-no-changes
   
   # Check for common issues
   dotnet build --verbosity normal
   ```

### PR Requirements

- [ ] Work Item (WI) file created and updated throughout development
- [ ] All tests pass locally and in CI
- [ ] Code coverage maintains or improves existing coverage (70% minimum)
- [ ] Documentation updated (if applicable)
- [ ] Architecture documentation updated (if system design changes)
- [ ] No failing tests or build errors
- [ ] .NET 9.0 environment requirements met

### PR Description Template

```markdown
## Work Item Reference
- **WI File**: `WIs/WI[#]_[description].md`
- **Type**: [Feature|Bug Fix|Enhancement|Documentation]

## Changes Made
- Brief list of changes

## Testing
- [ ] All unit tests pass
- [ ] Integration tests pass  
- [ ] LocalTesting workflow executes successfully
- [ ] Coverage requirements met (70%+)

## Documentation
- [ ] Code changes documented
- [ ] Architecture documentation updated (if applicable)
- [ ] API documentation updated (if applicable)

## Checklist
- [ ] .NET 9.0 environment verified
- [ ] All solutions build successfully
- [ ] No failing tests
- [ ] Work Item updated with learnings
```

## Documentation Guidelines

### Required Documentation Updates

#### Architecture Changes
When making architecture or system design changes, update:
- `docs/system-architecture-diagram.png` - Visual system diagram
- `docs/system-architecture.html` - Interactive HTML documentation  
- `README.md` - System design section

#### Code Documentation
- **XML Documentation**: Required for all public APIs
- **Inline Comments**: Explain 'why', not 'what'
- **README Updates**: For significant feature additions

#### Work Item Documentation
- **Learning Documentation**: Document failures, solutions, and lessons learned
- **Problem Prevention**: Review existing WIs to avoid repeating solved problems
- **Debug Information**: Document debugging process and findings

### Documentation Standards

- **Enterprise-level quality**: Professional visual design and comprehensive content
- **Accessibility**: Technical and business stakeholder friendly
- **Precision**: Accurate technical descriptions
- **Consistency**: Uniform terminology and formatting

## Release Process

### Version Strategy

FlinkDotNet follows semantic versioning (SemVer):
- **Major**: Breaking changes
- **Minor**: New features, backward compatible
- **Patch**: Bug fixes, backward compatible

### Release Workflow

1. **Feature Complete**: All planned features implemented and tested
2. **Quality Assurance**: All test suites pass
3. **Documentation**: All documentation updated
4. **Performance Validation**: Stress tests and performance benchmarks pass
5. **Release Notes**: Comprehensive change documentation

## Getting Help

### Community Resources

- **GitHub Issues**: For bug reports and feature requests
- **Discussions**: For questions and community support
- **Wiki**: Comprehensive guides and examples

### Development Support

- **Architecture Questions**: Review `docs/wiki/` for system design patterns
- **Testing Help**: See `docs/wiki/Getting-Started.md` for BDD testing examples
- **Local Development**: Use `docs/wiki/Aspire-Local-Development-Setup.md`

### Code Review

- **Review Guidelines**: Focus on SOLID principles and .NET best practices
- **Quality Standards**: Maintain high code quality and test coverage
- **Feedback**: Constructive feedback focused on code improvement

## Automated Checks

The following are automatically enforced in CI:

- **Code Coverage**: Minimum 70% line coverage
- **Build Validation**: All solutions must build successfully
- **Test Execution**: All test categories must pass
- **Environment Consistency**: .NET 9.0 SDK requirements
- **Code Quality**: SonarCloud analysis and quality gates

## Violations and Consequences

### Major Violations
- **Skipping TDD/BDD**: Work rejection and rework requirement  
- **Failing Tests**: Immediate fix required before PR acceptance
- **Missing Work Items**: Work stoppage until WI created
- **Environment Issues**: .NET 9.0 requirements not met

### Quality Assurance
- **Continuous Integration**: All workflows must pass
- **Code Review**: Mandatory review for all PRs
- **Documentation**: Required for architectural changes

---

Thank you for contributing to FlinkDotNet! Your contributions help make .NET stream processing with Apache Flink more accessible and powerful for the entire community.