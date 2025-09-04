# GitHub Copilot Guidelines - Part 4 of 9
## Review Guidelines + Test Coverage + Reality Filter

> **Navigation**: [Part 3](./default-rules-part-3.md) | [Part 5](./default-rules-part-5.md) | [All Parts Index](./README.md)

> **Context from Part 3**: Automatic checks for code quality and complexity metrics

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

> **Continues in**: [Part 5](./default-rules-part-5.md) - Work Item Enforcement Rules and Core Behavioral Enforcements