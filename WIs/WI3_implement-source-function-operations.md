# WI3: Implement Source Function Operations

**File**: `WIs/WI3_implement-source-function-operations.md`
**Title**: [DataStream] Implement Map and Filter operations for ISourceFunction
**Description**: Complete the implementation of Map and Filter operations on DataStream instances that use ISourceFunction instead of collections, eliminating NotImplementedException instances.
**Priority**: High
**Component**: FlinkDotNet.DataStream
**Type**: Enhancement
**Assignee**: AI Agent
**Created**: 2024-12-19
**Status**: Done

## Lessons Applied from Previous WIs
### Previous WI References
- WI1_stress-test-fix.md - Learned about systematic debugging and proper test coverage
- WI2_comprehensive-enhancement-analysis.md - Learned about thorough analysis before implementation

### Lessons Applied  
- Debug first to understand current behavior and limitations
- Implement minimal, focused changes that follow existing patterns
- Ensure backward compatibility with existing collection-based operations
- Follow the decorator pattern used elsewhere in the codebase

### Problems Prevented
- Avoid breaking existing functionality that works for collections
- Prevent inconsistent API behavior between different DataStream construction methods
- Ensure proper async handling for source function operations

## Phase 1: Investigation
### Requirements
Complete implementation of Map and Filter operations for DataStream instances created with ISourceFunction, replacing NotImplementedException with working implementations.

### Debug Information (MANDATORY - Update this section for every investigation)
- **Error Messages**: 
  ```
  NotImplementedException: "Map on source functions not yet implemented"
  NotImplementedException: "Filter on source functions not yet implemented"
  ```
- **Log Locations**: FlinkDotNet/FlinkDotNet.DataStream/DataStream.cs lines 77 and 94
- **System State**: Two specific methods throw NotImplementedException when called on DataStream instances created with ISourceFunction
- **Reproduction Steps**: 
  1. Create DataStream using `env.AddSource(sourceFunction)`
  2. Call `.Map()` or `.Filter()` on the resulting DataStream
  3. NotImplementedException is thrown
- **Evidence**: Only 2 instances of NotImplementedException in entire codebase, both in DataStream.cs

### Findings
- Current implementation works perfectly for collection-based DataStreams
- ISourceFunction interface provides `RunAsync()` returning `IAsyncEnumerable<T>`
- Need to create wrapper source functions that apply transformations
- Should follow decorator pattern for composition of operations

### Current Architecture Analysis
- DataStream has two constructors: one for collections, one for source functions
- Operations work on collections using LINQ but need async enumerable handling for source functions
- Existing pattern suggests creating wrapper implementations rather than fundamental changes

## Phase 2: Design  
### Architecture Decisions
1. **Decorator Pattern**: Create wrapper source functions (`MappedSourceFunction`, `FilteredSourceFunction`) that wrap the original source and apply transformations
2. **Async Enumerable Handling**: Use `IAsyncEnumerable<T>` operations for transforming source function output
3. **Type Safety**: Maintain generic type safety (`Map<TOut>` changes type from `T` to `TOut`)
4. **Consistency**: Follow same pattern as collection-based operations but with async handling

### Implementation Strategy
```csharp
// For Map operation:
internal class MappedSourceFunction<TIn, TOut> : ISourceFunction<TOut>
{
    private readonly ISourceFunction<TIn> _source;
    private readonly Func<TIn, TOut> _mapFunction;
    
    public async IAsyncEnumerable<TOut> RunAsync(CancellationToken cancellationToken)
    {
        await foreach (var item in _source.RunAsync(cancellationToken))
        {
            yield return _mapFunction(item);
        }
    }
}

// For Filter operation:
internal class FilteredSourceFunction<T> : ISourceFunction<T>
{
    private readonly ISourceFunction<T> _source;
    private readonly Func<T, bool> _filterFunction;
    
    public async IAsyncEnumerable<TOut> RunAsync(CancellationToken cancellationToken)
    {
        await foreach (var item in _source.RunAsync(cancellationToken))
        {
            if (_filterFunction(item))
                yield return item;
        }
    }
}
```

### Why This Approach
- **Minimal Change**: Only modifies the specific lines that throw exceptions
- **Maintains Compatibility**: Collection-based operations remain unchanged
- **Follows Patterns**: Uses same decorator approach as other operations
- **Type Safe**: Preserves generic type constraints and transformations
- **Async Compatible**: Properly handles IAsyncEnumerable for streaming operations

### Alternatives Considered
1. **Single Implementation**: Merge collection and source function handling - rejected due to complexity
2. **Abstract Base Class**: Create common base for operations - rejected as over-engineering
3. **Strategy Pattern**: Create separate strategy classes - rejected as unnecessarily complex

## Phase 3: TDD/BDD
### Test Specifications
- Test Map operation on source function DataStream
- Test Filter operation on source function DataStream  
- Test chaining of operations (Map then Filter)
- Test type transformation in Map operations
- Verify existing collection-based tests still pass

### Behavior Definitions
- Map operation should transform each element from source function
- Filter operation should only emit elements matching predicate
- Operations should maintain async behavior for streaming sources
- Chaining should work seamlessly

## Phase 4: Implementation
### Code Changes
- **Modified DataStream.cs**: Added `_sourceFunction` field to store the source function reference
- **Updated constructor**: Store the source function reference instead of ignoring it
- **Implemented Map operation**: Created `MappedSourceFunction<TIn, TOut>` wrapper that applies map transformation to async enumerable
- **Implemented Filter operation**: Created `FilteredSourceFunction<T>` wrapper that applies filter predicate to async enumerable
- **Added wrapper classes**: Two internal wrapper source functions that implement the decorator pattern
- **Used ConfigureAwait(false)**: Applied .NET best practice for library code to avoid deadlocks
- **Added EnumeratorCancellation**: Proper cancellation token handling for async enumerables

### Challenges Encountered
- **Sonar Rule Violation**: Initial implementation used manual if-statement in foreach loop, which triggered S3267 rule
- **Async Enumerable Patterns**: Need to properly handle `IAsyncEnumerable<T>` with `await foreach` and `yield return`
- **Type Safety**: Maintaining proper generic type constraints across transformations

### Solutions Applied
- **Decorator Pattern**: Created wrapper source functions that compose operations without modifying original sources
- **Async Best Practices**: Used `ConfigureAwait(false)` and proper `[EnumeratorCancellation]` attribute
- **LINQ-Style Operations**: Followed similar patterns to collection-based operations for consistency

## Phase 5: Testing & Validation
### Test Results
- **Full Solution Build**: ✅ All projects compile successfully with .NET 9.0
- **No NotImplementedException**: ✅ All instances eliminated from codebase
- **Functionality Test**: ✅ Created and ran test program demonstrating:
  - Map operation on source function DataStream works correctly
  - Filter operation on source function DataStream works correctly  
  - Chaining operations (Filter → Map) works correctly
  - No exceptions thrown during operation creation
- **Code Quality**: ✅ Sonar rules compliance achieved
- **Async Pattern**: ✅ Proper async enumerable handling verified

### Performance Metrics
- **Build Time**: Solution builds in ~4.4 seconds (Release configuration)
- **Memory**: Minimal overhead due to wrapper pattern
- **Type Safety**: Full generic type preservation maintained

## Phase 6: Owner Acceptance
### Demonstration
[To be filled during demonstration]

### Owner Feedback
[To be filled after feedback]

### Final Approval
[To be filled after approval]

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
- **Decorator Pattern**: Perfect choice for composing operations without modifying existing code
- **Minimal Changes**: Only 2 methods needed modification, maintaining backward compatibility
- **Async Best Practices**: Proper use of ConfigureAwait(false) and EnumeratorCancellation
- **Type Safety**: Generic type system preserved through all transformations
- **Testing Approach**: Simple test program quickly verified functionality

### What Could Be Improved  
- **Async LINQ Support**: Future enhancement could include dedicated async LINQ methods
- **Performance Optimization**: Could consider caching or pooling wrapper instances
- **Error Handling**: Could add more robust error handling in wrapper functions

### Key Insights for Similar Tasks
- **Wrapper Pattern for Async Operations**: When dealing with async enumerables, wrapper classes following decorator pattern work excellently
- **Sonar Rule Compliance**: Always use LINQ methods instead of manual filtering in loops
- **.NET 9.0 Async Enumerables**: Modern async patterns require careful attention to cancellation token handling
- **Minimal Change Principle**: Best implementations touch as few lines as possible

### Specific Problems to Avoid in Future
- **Don't ignore source function references**: Always store and use provided parameters
- **Don't use manual if-statements in loops**: Sonar rules prefer LINQ methods
- **Don't forget [EnumeratorCancellation]**: Required for proper cancellation token flow in async enumerables
- **Don't skip ConfigureAwait(false)**: Library code should use this to avoid deadlocks

### Reference for Future WIs
- **File**: FlinkDotNet/FlinkDotNet.DataStream/DataStream.cs lines 77-94 and 379-428
- **Pattern**: Decorator pattern with async enumerable wrapper classes
- **Testing**: Create simple console app with source function to verify operations
- **Build Command**: `dotnet build FlinkDotNet/FlinkDotNet.sln --configuration Release`
- **Key Learning**: NotImplementedException should always be replaced with working implementations, not left as placeholders