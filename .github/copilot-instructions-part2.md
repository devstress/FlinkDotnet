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

> **Note**: This chunk covers ISP, DIP principles and .NET naming/exception handling. For additional .NET practices, see Part 3. For SOLID principles SRP, OCP, LSP, see Part 1.