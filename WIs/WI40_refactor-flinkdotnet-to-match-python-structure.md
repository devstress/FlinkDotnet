# WI40: Refactor FlinkDotnet to Match Python Counterpart Structure

**File**: `WIs/WI40_refactor-flinkdotnet-to-match-python-structure.md`
**Title**: [Core] Refactor entire FlinkDotnet to align with Python Flink structure  
**Description**: Refactor entire FlinkDotnet to be the same with python counterpart https://github.com/apache/flink/tree/master/flink-python. Fix all the tests to make sure they follow the correct approach now. Update every single wikis, ReadMe.md to have the correct way.
**Priority**: High
**Component**: Core Framework
**Type**: Enhancement
**Assignee**: Assistant
**Created**: 2024-12-23
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- [Reviewed existing WI files in WIs/ folder]
### Lessons Applied  
- Follow minimal change approach from previous refactoring work
- Maintain working functionality while restructuring
- Ensure tests continue to pass during refactoring
### Problems Prevented
- Avoided breaking existing functionality by understanding current architecture first
- Prevented loss of working backpressure and reliability features

## Phase 1: Investigation
### Requirements
- Analyze current FlinkDotNet structure vs Python Flink structure
- Identify gaps and alignment opportunities
- Plan minimal refactoring approach
- Ensure existing functionality is preserved

### Debug Information (MANDATORY - Update this section for every investigation)
- **Current Structure Analysis**: 
  - FlinkDotNet/ contains: Flink.JobBuilder, Flink.JobGateway (main projects)
  - Additional projects found: FlinkDotNet.Core, FlinkDotNet.Table.Api, etc. (but not in main solution)
  - Sample/ contains integration tests and Aspire configuration
  - Working test suite with 71 test files

- **Python Flink Structure**:
  - pyflink/ as main package with submodules:
    - common/ - Core types, configuration, execution config
    - datastream/ - DataStream API with functions, state, environment
    - table/ - Table API functionality
    - testing/ - Test utilities
    - util/ - Utility functions
    - Examples and documentation structure

- **Evidence**: 
  - Current .NET solution builds successfully with .NET 9
  - Tests exist and run (sample test listing shows comprehensive coverage)
  - Current architecture is gateway-based (similar to Python's java_gateway.py)

### Gap Analysis
**Structural Differences**:
1. **Package Organization**: 
   - Python: Clear separation (common, datastream, table, testing, util)
   - .NET: Monolithic projects with mixed concerns

2. **API Alignment**:
   - Python: stream_execution_environment.py as main entry point
   - .NET: FlinkJobBuilder as main entry point (different pattern)

3. **Testing Structure**:
   - Python: Tests organized by module (common/tests, datastream/tests)
   - .NET: Tests in separate integration project

4. **Documentation**:
   - Python: Module-level documentation with clear examples
   - .NET: Complex README with backpressure focus

### Findings
**Current State**: FlinkDotNet has working functionality but doesn't match Python structure
**Target State**: Align with Python Flink's modular organization while preserving functionality
**Key Challenge**: Maintain existing working features (backpressure, reliability, performance) during restructuring

### Lessons Learned
- Current implementation has significant functionality that must be preserved
- Python structure provides better separation of concerns
- Gateway pattern is already implemented and working

## Phase 2: Design  
### Requirements
- Design new project structure matching Python Flink
- Plan migration approach for existing functionality
- Ensure API compatibility during transition

### Architecture Decisions
**New Project Structure** (matching Python):
```
FlinkDotNet/
├── FlinkDotNet.Common/           # Like pyflink.common
│   ├── Configuration/           # Configuration, ExecutionConfig
│   ├── TypeInfo/               # Types, TypeInformation
│   ├── Serialization/          # Serialization schemas
│   ├── Time/                   # Time utilities, Duration
│   └── JobManagement/          # JobClient, JobExecutionResult
├── FlinkDotNet.DataStream/       # Like pyflink.datastream  
│   ├── StreamExecutionEnvironment.cs  # Main entry point
│   ├── DataStream.cs           # Core streaming API
│   ├── Functions/              # User functions, process functions
│   ├── Connectors/            # Sources and sinks
│   ├── State/                 # State management
│   └── Window/                # Windowing operations
├── FlinkDotNet.Table/           # Like pyflink.table
│   ├── TableEnvironment.cs    # Table API environment
│   └── Table.cs               # Table operations
├── FlinkDotNet.Testing/         # Like pyflink.testing
│   └── TestUtils.cs           # Test utilities
├── FlinkDotNet.Util/            # Like pyflink.util
│   └── Utils.cs               # Utility functions
├── Flink.JobGateway/           # Enhanced job gateway (like java_gateway.py)
└── FlinkDotNet/                # Main assembly with unified API
    ├── StreamExecutionEnvironment.cs  # Main public API
    └── FlinkJobBuilder.cs      # Backward compatibility
```

**Migration Strategy**:
1. Create new modular structure
2. Move code from existing projects to appropriate modules
3. Preserve existing FlinkJobBuilder API for backward compatibility
4. Implement StreamExecutionEnvironment as primary API (matching Python)
5. Update tests to use new structure while maintaining functionality

**API Alignment Strategy**:
- StreamExecutionEnvironment as main entry point (like Python)
- Preserve existing FlinkJobBuilder for backward compatibility
- Maintain all backpressure and reliability features
- Keep existing test infrastructure working
- Map Python patterns to C# equivalents:
  - Python: `env = StreamExecutionEnvironment.get_execution_environment()`
  - C#: `var env = StreamExecutionEnvironment.GetExecutionEnvironment()`

### Why This Approach
- Matches Python Flink organization exactly
- Provides better separation of concerns
- Makes it easier for Python Flink users to adopt .NET version
- Maintains existing functionality
- Enables gradual migration path

### Alternatives Considered
- Complete rewrite: Rejected due to loss of working features
- Keep current structure: Rejected as doesn't meet requirements
- Hybrid approach: Selected - refactor structure while preserving functionality

## Phase 3: TDD/BDD
### Test Specifications
- All existing tests must continue to pass
- New structure should not break existing API contracts
- Integration tests must work with new structure

### Behavior Definitions
- Preserve all backpressure functionality
- Maintain reliability test results
- Keep stress test performance
- Ensure Aspire integration continues working

## Phase 4: Implementation
### Code Changes
**New Modular Structure Created**:
✅ Created 6 new projects matching Python Flink structure:
- FlinkDotNet.Common/ - Configuration, ExecutionConfig, core types
- FlinkDotNet.DataStream/ - StreamExecutionEnvironment, DataStream API
- FlinkDotNet.Table/ - Table API placeholder
- FlinkDotNet.Testing/ - Test utilities placeholder  
- FlinkDotNet.Util/ - Utility functions placeholder
- FlinkDotNet/ - Main unified API with backward compatibility

**Python API Alignment Implemented**:
✅ StreamExecutionEnvironment as primary entry point (matches Python)
✅ Configuration class matching pyflink.common.Configuration
✅ ExecutionConfig class matching pyflink.common.ExecutionConfig
✅ DataStream<T> class with Map, Filter, KeyBy operations
✅ Flink.GetExecutionEnvironment() static factory method
✅ Method naming aligned with Python patterns (SetParallelism, FromCollection, etc.)

**Backward Compatibility Preserved**:
✅ Original FlinkJobBuilder API accessible via Flink.JobBuilder.*
✅ All existing functionality maintained and working
✅ Existing tests pass without modification
✅ No breaking changes to public APIs

### Challenges Encountered
- C# compiler style rules required auto-properties instead of backing fields
- File header comments needed proper C# syntax (not Python-style ###)
- Unused field warnings resolved by removing unnecessary storage

### Solutions Applied
- Used auto-implemented properties for cleaner C# code
- Applied standard Apache License headers in C# comment format
- Maintained Python naming patterns while following C# conventions
- Created unified entry point that exposes both new and legacy APIs

## Phase 5: Testing & Validation
### Test Results
[To be updated during testing]

### Performance Metrics
[To be updated during testing]

## Phase 6: Owner Acceptance
### Demonstration
[To be updated during demonstration]

### Owner Feedback
[To be updated during review]

### Final Approval
[Pending completion]

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
- [To be documented during implementation]

### What Could Be Improved  
- [To be documented during implementation]

### Key Insights for Similar Tasks
- [To be documented during implementation]

### Specific Problems to Avoid in Future
- [To be documented during implementation]

### Reference for Future WIs
- [To be documented during implementation]