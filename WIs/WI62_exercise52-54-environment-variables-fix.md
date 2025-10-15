# WI62: Exercise52-54 Environment Variables and Package Version Fix

**File**: `WIs/WI62_exercise52-54-environment-variables-fix.md`
**Status**: Complete
**Created**: 2025-01-14
**Completed**: 2025-01-14
**Type**: Batch Fix - Environment Variables + Package Versions
**Component**: Day05 Enterprise Observability Exercises

## Summary

Fix hardcoded observability endpoints and outdated package versions in Exercise52 (Distributed Tracing), Exercise53 (Log Aggregation), and Exercise54 (Alert Configuration). These exercises are simulation-based and should remain so, but must follow environment variable patterns for infrastructure endpoints per update-LearningCourse.md guidelines.

## Lessons Applied from Previous WIs

### Previous WI References
- WI61: Exercise51 conversion - learned package version consistency requirements
- update-LearningCourse.md (lines 2236-2270): Aspire Service Discovery patterns
- Common Error #15: Simulation vs Real Infrastructure decision criteria

### Lessons Applied
- Check FlinkDotNet.DataStream package versions first before making changes
- Use environment variables for ALL infrastructure endpoints (no hardcoded localhost)
- Simulation-based exercises are appropriate when teaching patterns, not infrastructure integration
- Update package versions to match FlinkDotNet.DataStream dependencies
- Include fallback values for manual testing without test infrastructure

## Phase 1: Investigation

### Debug Information (MANDATORY)

**Exercise52 Analysis** - Distributed Tracing:
- Hardcoded endpoint: Line 469 `options.Endpoint = new Uri("http://localhost:18009");`
- Educational Goal: OpenTelemetry distributed tracing patterns (Uber microservices simulation)
- Decision: Keep simulation - teaches trace propagation, not infrastructure setup
- Package issues: Serilog 5.0.0, Microsoft.Extensions 8.0.0 need updating

**Exercise53 Analysis** - Log Aggregation:
- Hardcoded endpoints: Lines 895-896, 927-928 (Loki, Grafana URLs in console output)
- Educational Goal: ELK stack log aggregation patterns (enterprise logging simulation)
- Decision: Keep simulation - teaches structured logging, not infrastructure
- Package issues: Same as Exercise52

**Exercise54 Analysis** - Alert Configuration:
- Hardcoded endpoints: Lines 859-860, 889-890 (Grafana, Prometheus URLs in console output)
- Educational Goal: Google SRE alerting with SLI/SLO monitoring
- Decision: Keep simulation - teaches alerting principles, not infrastructure
- Package issues: Same as Exercise52

### Findings Summary
**Hardcoded Endpoints**: 3 exercises with 6 total hardcoded URLs
**Package Updates Needed**: 4 packages × 3 projects = 12 package reference updates

## Phase 2: Design

### Environment Variable Pattern
Add these static properties to each exercise class:

Exercise52 needs:
- OTEL_COLLECTOR_URL (fallback: http://localhost:18009)

Exercise53 needs:
- LOKI_URL (fallback: http://localhost:18005)
- GRAFANA_URL (fallback: http://localhost:18010)

Exercise54 needs:
- GRAFANA_URL (fallback: http://localhost:18010)
- PROMETHEUS_URL (fallback: http://localhost:18006)

### Package Version Updates
All three .csproj files:
- Serilog.Sinks.Console: 5.0.0 → 6.0.0
- Serilog.Sinks.File: 5.0.0 → 6.0.0
- Microsoft.Extensions.Hosting: 8.0.0 → 8.0.1
- Microsoft.Extensions.DependencyInjection: 8.0.0 → 8.0.1

## Phase 3: Implementation

**Status**: ✅ Complete - All builds successful (0 Errors, 45 pre-existing warnings)

### Exercise52 Changes ✅
**Files Modified**: Exercise52/Program.cs, Exercise52.csproj

**Program.cs Changes**:
- Added static `OtelCollectorUrl` property to `Program` class (lines 18-19)
- Environment variable: `OTEL_COLLECTOR_URL` with fallback `http://localhost:18009`
- Updated line 474 to use property: `options.Endpoint = new Uri(OtelCollectorUrl);`
- Updated console output (line 462) to use property

**Exercise52.csproj Changes**:
- Updated Serilog.Sinks.Console: 5.0.0 → 6.0.0
- Updated Serilog.Sinks.File: 5.0.0 → 6.0.0
- Updated Microsoft.Extensions.Hosting: 8.0.0 → 8.0.1
- Updated Microsoft.Extensions.DependencyInjection: 8.0.0 → 8.0.1

**Build Result**: ✅ Success - 0 Errors, 0 Warnings

### Exercise53 Changes ✅
**Files Modified**: Exercise53/Program.cs, Exercise53.csproj

**Program.cs Changes**:
- Added static properties to `Program` class:
  - `LokiUrl` property (lines 878-879): LOKI_URL env var, fallback `http://localhost:18005`
  - `GrafanaUrl` property (lines 881-882): GRAFANA_URL env var, fallback `http://localhost:18010`
- Updated console output lines 903-904, 935-936 to use properties with string interpolation
- **Fix Applied**: Moved properties from `EnterpriseLogAggregationService` class to `Program` class to fix scope issues

**Exercise53.csproj Changes**:
- Updated Serilog.Sinks.Console: 5.0.0 → 6.0.0
- Updated Serilog.Sinks.File: 5.0.0 → 6.0.0
- Updated Microsoft.Extensions.Hosting: 8.0.0 → 8.0.1
- Updated Microsoft.Extensions.DependencyInjection: 8.0.0 → 8.0.1

**Build Result**: ✅ Success - 0 Errors, 30 pre-existing CA2017 warnings (Serilog structured logging patterns)

### Exercise54 Changes ✅
**Files Modified**: Exercise54/Program.cs, Exercise54.csproj

**Program.cs Changes**:
- Added static properties to `Program` class:
  - `GrafanaUrl` property (lines 850-851): GRAFANA_URL env var, fallback `http://localhost:18010`
  - `PrometheusUrl` property (lines 853-854): PROMETHEUS_URL env var, fallback `http://localhost:18006`
- Updated console output lines 866-867, 897-898 to use properties with string interpolation
- **Fix Applied**: Moved properties from `GoogleSREAlertingService` class to `Program` class to fix scope issues

**Exercise54.csproj Changes**:
- Updated Serilog.Sinks.Console: 5.0.0 → 6.0.0
- Updated Serilog.Sinks.File: 5.0.0 → 6.0.0
- Updated Microsoft.Extensions.Hosting: 8.0.0 → 8.0.1
- Updated Microsoft.Extensions.DependencyInjection: 8.0.0 → 8.0.1

**Build Result**: ✅ Success - 0 Errors, 15 pre-existing CA2017 warnings (Serilog structured logging patterns)

## Phase 4: Testing & Validation

### Build Validation ✅
```bash
cd LearningCourse/Day05-Enterprise-Observability/Exercise-Solutions
dotnet build Exercise52/Exercise52.csproj --configuration Release  # ✅ 0 Errors, 0 Warnings
dotnet build Exercise53/Exercise53.csproj --configuration Release  # ✅ 0 Errors, 30 Warnings
dotnet build Exercise54/Exercise54.csproj --configuration Release  # ✅ 0 Errors, 15 Warnings
```

**All builds successful** - Pre-existing CA2017 warnings about Serilog structured logging parameter counts are not related to our changes.

### Changes Summary
- **Files Modified**: 6 files (3 .cs, 3 .csproj)
- **Environment Variables Added**: 5 configurable endpoints across 3 exercises
- **Package Updates**: 12 package references updated (4 per project)
- **No Functionality Changes**: All exercises remain simulation-based as designed
- **Aspire-Ready**: All infrastructure endpoints now configurable via environment variables

## Lessons Learned & Future Reference

### What Worked Well
- **Batch Processing**: Handling all three related exercises in one WI was efficient
- **Scope Analysis**: Checking where properties are referenced before placing them prevented errors
- **Build-First Approach**: Testing Exercise52 first revealed the static scope pattern needed for all three

### Key Insights for Similar Tasks
- **Static Property Placement**: Environment variable properties must be in the class where they're referenced (e.g., `Program.Main` static method needs properties in `Program` class, not service classes)
- **Console Output Updates**: String interpolation with properties requires proper scope access
- **Pre-existing Warnings**: CA2017 warnings about Serilog structured logging are common and acceptable - they use anonymous objects with property names that don't match logging template placeholders

### Specific Problems to Avoid in Future
- ❌ **Don't place static properties in wrong class scope** - always verify where properties are accessed
- ❌ **Don't assume instance properties** - Main() is static, needs static properties
- ✅ **Do verify builds after each exercise** - caught Exercise53/54 scope issues early
- ✅ **Do accept pre-existing analyzer warnings** - focus on errors, not inherited warnings

### Reference for Future WIs
This WI demonstrates the correct pattern for adding environment variable support to console applications with static Main methods. The static property pattern with null-coalescing operators provides both Aspire integration and manual testing fallbacks.