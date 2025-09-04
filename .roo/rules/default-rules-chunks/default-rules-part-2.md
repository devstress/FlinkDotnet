# GitHub Copilot Guidelines - Part 2 of 9
## SOLID Principles Enforcement (Part 2) + .NET Best Practices

> **Navigation**: [Part 1](./default-rules-part-1.md) | [Part 3](./default-rules-part-3.md) | [All Parts Index](./README.md)

> **Context from Part 1**: Interface Segregation Principle enforcement rules and examples

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

> **Continues in**: [Part 3](./default-rules-part-3.md) - Exception Handling, Async/Await, and Code Review Guidelines