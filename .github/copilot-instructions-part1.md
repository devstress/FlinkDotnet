# # GitHub Copilot Guidelines

This document defines the coding standards and best practices that GitHub Copilot should enforce during code reviews for this .NET project. These guidelines ensure adherence to SOLID principles and .NET best practices, with specialized guidance for BizTalk to Inobiz migrations using .NET 9 and direct XSLT mapping.

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

> **Note**: This chunk covers SOLID principles SRP, OCP, and LSP. For complete ISP and DIP principles, see Part 2. For .NET Best Practices, see Part 3.