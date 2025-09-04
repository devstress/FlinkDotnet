# GitHub Copilot Guidelines - Part 3 of 9
## .NET Best Practices (Continued) + Code Review + Automatic Checks

> **Navigation**: [Part 2](./default-rules-part-2.md) | [Part 4](./default-rules-part-4.md) | [All Parts Index](./README.md)

> **Context from Part 2**: .NET naming conventions for classes, methods, properties, etc.

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

> **Continues in**: [Part 4](./default-rules-part-4.md) - Review Guidelines, Test Coverage Requirements, and Reality Filter