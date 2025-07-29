# Flink JobManager Async Pattern Compatibility Guide

## Overview

When developing streaming applications with Flink.NET, it's important to understand the difference between code that runs locally during development/testing and code that gets submitted to and executed by Apache Flink's JobManager.

## The Problem

Certain async patterns that work perfectly in local unit tests may fail when the same code is submitted to Flink's JobManager for execution in a distributed environment. This happens because:

1. **Serialization Context**: Code submitted to Flink JobManager gets serialized and distributed across the cluster
2. **Execution Context**: The .NET execution environment changes from local to distributed
3. **Task Scheduling**: SynchronizationContext and Task scheduling behavior differs
4. **State Machine Preservation**: Async state machines may not serialize/deserialize correctly

## Problematic Pattern (Avoid in Flink Jobs)

```csharp
// ❌ PROBLEMATIC - Works in tests but may fail in Flink JobManager
if (await rateLimiter.TryAcquireAsync())
{
    // Process your message
    await ProcessMessage(message);
}
```

**Why this fails:**
- Async state machines don't serialize properly across job boundaries
- Task scheduling context differs in distributed execution
- SynchronizationContext may not be available in Flink execution environment

## Recommended Patterns

### 1. Synchronous Pattern (Recommended)

```csharp
// ✅ RECOMMENDED - Works reliably in both local and Flink execution
if (rateLimiter.TryAcquire())
{
    // Process your message synchronously
    ProcessMessageSync(message);
}
```

### 2. Safe Async Pattern (Use with Caution)

```csharp
// ⚠️ ALTERNATIVE - Use ConfigureAwait(false) for Flink compatibility
if (rateLimiter.TryAcquire())
{
    try
    {
        // Use ConfigureAwait(false) to avoid SynchronizationContext dependencies
        await ProcessMessage(message).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        // Handle gracefully for Flink job stability
        logger.LogError(ex, "Error processing message");
    }
}
```

### 3. Bulk Processing Pattern (High Throughput)

```csharp
// 🚀 BULK PROCESSING - Optimized for high-throughput scenarios
var requiredPermits = messages.Length;
if (rateLimiter.TryAcquire(requiredPermits))
{
    // Process all messages in the batch
    foreach (var message in messages)
    {
        ProcessMessageSync(message);
    }
}
```

## API Changes

### New Synchronous Methods

We've added synchronous versions of rate limiting methods specifically for Flink JobManager compatibility:

```csharp
public interface IRateLimitingStrategy
{
    // Existing async method (use in local tests)
    Task<bool> TryAcquireAsync(int permits = 1, CancellationToken cancellationToken = default);
    
    // New sync method (use in Flink jobs)
    bool TryAcquire(int permits = 1);
}
```

### Updated Sample Code

The sample code in this repository has been updated to demonstrate the correct patterns:

- `Sample/FlinkJobBuilder.Sample/FlinkJobManagerCompatibilityExamples.cs` - Complete examples
- `Sample/FlinkDotNet.Aspire.IntegrationTests/Unit/ImprovedRateLimiterAsyncTests.cs` - Updated test patterns

## When to Use Which Pattern

| Context | Pattern | Reason |
|---------|---------|---------|
| **Local Development/Testing** | `await TryAcquireAsync()` | Full async benefits, proper test isolation |
| **Flink Job Submission** | `TryAcquire()` | Guaranteed compatibility with serialization |
| **High-Throughput Jobs** | `TryAcquire(bulkSize)` | Reduced overhead, better performance |
| **Mixed Environments** | Conditional logic | Detect context and use appropriate pattern |

## Testing Your Code

### Local Testing
```csharp
[Test]
public async Task LocalTest_CanUseAsyncPatterns()
{
    var rateLimiter = new TokenBucketRateLimiter(10.0, 20.0);
    
    // This works fine in local tests
    if (await rateLimiter.TryAcquireAsync())
    {
        await ProcessMessage("test");
    }
}
```

### Flink Compatibility Testing
```csharp
[Test]
public void FlinkCompatibilityTest_UseSyncPatterns()
{
    var rateLimiter = new TokenBucketRateLimiter(10.0, 20.0);
    
    // This works in both local and Flink execution
    if (rateLimiter.TryAcquire())
    {
        ProcessMessageSync("test");
    }
}
```

## Migration Guide

If you have existing code using async patterns that will be submitted to Flink JobManager:

1. **Identify Async Rate Limiting**: Search for `await.*TryAcquireAsync` patterns
2. **Replace with Sync**: Change to `TryAcquire()` method calls
3. **Update Message Processing**: Consider using synchronous processing or proper ConfigureAwait(false)
4. **Test Both Contexts**: Ensure code works in both local and distributed execution

## Best Practices

1. **Use Sync for Jobs**: Always use synchronous rate limiting patterns in code that will be submitted to Flink
2. **Use Async for Clients**: Continue using async patterns in client applications and local tests
3. **Document Intent**: Comment your code to indicate which execution context it's designed for
4. **Error Handling**: Add proper exception handling that doesn't rely on specific synchronization contexts
5. **Performance Testing**: Test performance characteristics in both local and distributed environments

## Example Job Implementation

```csharp
public class FlinkCompatibleStreamingJob
{
    private readonly IRateLimitingStrategy _rateLimiter;
    private readonly ILogger _logger;

    public FlinkCompatibleStreamingJob(IRateLimitingStrategy rateLimiter, ILogger logger)
    {
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    public void ProcessMessage(StreamingMessage message)
    {
        // ✅ Flink JobManager compatible pattern
        if (_rateLimiter.TryAcquire())
        {
            try
            {
                // Synchronous processing - works reliably in Flink
                var result = ProcessBusinessLogic(message);
                EmitResult(result);
                
                _logger.LogInformation($"Processed message: {message.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process message: {message.Id}");
                // Handle gracefully - don't let exceptions escape job boundaries
            }
        }
        else
        {
            _logger.LogWarning($"Rate limited - message dropped: {message.Id}");
        }
    }
}
```

This approach ensures your streaming jobs work reliably whether running locally during development or distributed across a Flink cluster in production.