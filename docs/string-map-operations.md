# Comprehensive String Method Support for Map Operations

This document demonstrates the comprehensive .NET string method support added to FlinkDotNet Map operations, matching Java Flink's lambda syntax capabilities.

## Overview

FlinkDotNet now supports all common .NET string methods in Map operations, providing an experience similar to Java Flink:

**Java Flink:**
```java
env.fromElements("hello", "world")
    .map(s -> s.toUpperCase())  // Lambda works!
    .print();
```

**FlinkDotNet:**
```csharp
env.FromElements("hello", "world")
    .Map(new ToUpperInvariantMapFunction())  // Same functionality!
    .Print();
```

## Supported String Operations

### 1. IMapFunction Implementations (Recommended)

All operations are available as reusable IMapFunction implementations:

```csharp
using FlinkDotNet.DataStream;

// ToUpperInvariant - converts to uppercase
var stream = env.FromKafka("input", "localhost:9092", "group", "earliest");
var upper = stream.Map(new ToUpperInvariantMapFunction());
// "hello" → "HELLO"

// ToLowerInvariant - converts to lowercase
var lower = stream.Map(new ToLowerInvariantMapFunction());
// "HELLO" → "hello"

// Trim - removes leading and trailing whitespace
var trimmed = stream.Map(new TrimMapFunction());
// "  hello  " → "hello"

// TrimStart - removes leading whitespace
var ltrimmed = stream.Map(new TrimStartMapFunction());
// "  hello  " → "hello  "

// TrimEnd - removes trailing whitespace
var rtrimmed = stream.Map(new TrimEndMapFunction());
// "  hello  " → "  hello"
```

### 2. String Expression Syntax (For Composition)

Simple string expressions for quick transformations:

```csharp
// Single operations
stream.Map("upper")    // → "UPPER"
stream.Map("lower")    // → "lower"
stream.Map("trim")     // → no whitespace
stream.Map("ltrim")    // → no leading whitespace
stream.Map("rtrim")    // → no trailing whitespace

// Composite operations (comma-separated)
stream.Map("trim,upper")  // Trim then uppercase
stream.Map("lower,trim")  // Lowercase then trim
stream.Map("ltrim,upper,rtrim")  // Multiple operations
```

## Complete Examples

### Example 1: Text Normalization Pipeline

```csharp
var env = StreamExecutionEnvironment.GetExecutionEnvironment();

// Read from Kafka, normalize text, write back
var stream = env.FromKafka("raw-text", "localhost:9092", "normalizer", "earliest");

var normalized = stream
    .Map(new TrimMapFunction())           // Remove whitespace
    .Map(new ToLowerInvariantMapFunction())  // Normalize case
    .SinkToKafka("normalized-text", "localhost:9092");

await env.ExecuteAsync("Text Normalization");
```

### Example 2: Using String Expressions

```csharp
var env = StreamExecutionEnvironment.GetExecutionEnvironment();

var stream = env.FromKafka("input", "localhost:9092", "group", "earliest");

// Chain multiple operations
var result = stream
    .Map("ltrim")      // Remove leading spaces
    .Map("upper")      // Convert to uppercase
    .Map("rtrim")      // Remove trailing spaces
    .SinkToKafka("output", "localhost:9092");

await env.ExecuteAsync("String Processing");
```

### Example 3: Composite Expression

```csharp
var env = StreamExecutionEnvironment.GetExecutionEnvironment();

var stream = env.FromKafka("input", "localhost:9092", "group", "earliest");

// Single composite expression
var result = stream
    .Map("trim,upper")  // Trim AND uppercase in one operation
    .SinkToKafka("output", "localhost:9092");

await env.ExecuteAsync("Composite Processing");
```

## Flink IR Translation

All operations are automatically translated to Flink IR for distributed execution:

| .NET Method | Flink IR Expression |
|------------|-------------------|
| ToUpperInvariant() | upper |
| ToLowerInvariant() | lower |
| ToUpper() | upper |
| ToLower() | lower |
| Trim() | trim |
| TrimStart() | ltrim |
| TrimEnd() | rtrim |

### Composite Expressions

Multiple operations are translated to comma-separated expressions:

```csharp
stream.Map("trim,upper")  // → Flink IR: "trim,upper"
stream.Map("lower,trim")  // → Flink IR: "lower,trim"
```

## Backward Compatibility

All existing code continues to work without changes:

```csharp
// Existing MapFunction implementations still work
public class MyCustomMapper : IMapFunction<string, string>
{
    public string Map(string value) => value.ToUpper();
}

stream.Map(new MyCustomMapper());  // ✓ Works as before

// Existing string expressions still work
stream.Map("upper");  // ✓ Works as before
stream.Map("lower");  // ✓ Works as before
```

## Advanced: Lambda Expression Analyzer

For future use, a LambdaExpressionAnalyzer is available that can parse expression trees:

```csharp
using System.Linq.Expressions;

// Expression tree analysis (future feature)
Expression<Func<string, string>> expr = s => s.ToUpper();
var result = LambdaExpressionAnalyzer.AnalyzeLambda(expr);
// Returns: "upper"

// Method chaining
Expression<Func<string, string>> chain = s => s.Trim().ToUpper();
result = LambdaExpressionAnalyzer.AnalyzeLambda(chain);
// Returns: "trim,upper"

// Arithmetic operations
Expression<Func<int, int>> math = i => i * 2;
result = LambdaExpressionAnalyzer.AnalyzeLambda(math);
// Returns: "multiply:$input:2"
```

## Best Practices

1. **Use IMapFunction for reusable logic:**
   ```csharp
   var normalizer = new ToUpperInvariantMapFunction();
   stream1.Map(normalizer);
   stream2.Map(normalizer);
   ```

2. **Use string expressions for quick transformations:**
   ```csharp
   stream.Map("trim,upper")  // Quick and readable
   ```

3. **Chain operations when needed:**
   ```csharp
   stream
       .Map("ltrim")
       .Map("upper")
       .Map("rtrim")
   ```

4. **Handle null values:**
   ```csharp
   // All map functions handle nulls safely
   new TrimMapFunction().Map(null)  // Returns string.Empty
   ```

## Migration from Java Flink

Java Flink code can be easily ported to FlinkDotNet:

**Java:**
```java
dataStream
    .map(s -> s.trim())
    .map(s -> s.toUpperCase())
```

**C#:**
```csharp
dataStream
    .Map(new TrimMapFunction())
    .Map(new ToUpperInvariantMapFunction())
```

Or using string expressions:
```csharp
dataStream.Map("trim,upper")
```

## Testing

All functionality is comprehensively tested:

- 22 tests for string map functions
- 19 tests for lambda expression analyzer
- Integration tests with Kafka streams
- Null handling tests
- Method chaining tests

## See Also

- [DataStream API Documentation](../FlinkDotNet.DataStream/DataStream.cs)
- [IMapFunction Interface](../FlinkDotNet.DataStream/DataStream.cs#L727)
- [String Map Functions](../FlinkDotNet.DataStream/StringMapFunctions.cs)
- [Lambda Expression Analyzer](../FlinkDotNet.DataStream/LambdaExpressionAnalyzer.cs)
