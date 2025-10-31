# FlinkDotNet 1.0.1 Release

## New Features and Enhancements

### Apache Flink 2.0 Support
- **DisaggregatedStateBackend** - New state backend architecture for disaggregated state management
- Enhanced documentation for Flink 2.0 features with comprehensive feature coverage
- 5 new integration tests validating Apache Flink 2.0 functionality
- Memory calculator improvements for better resource management

### Observability and Testing Infrastructure
- **ObservabilityTesting** - New comprehensive observability testing framework
- Integration with Flink SQL Gateway for enhanced SQL capabilities
- SampleApp with FlinkDotNet JobGateway integration tests
- Aspire Dashboard configuration improvements for better monitoring
- Conditional Prometheus and Grafana setup based on LEARNINGCOURSE mode

### Code Quality and Coverage Improvements
- Achieved 95%+ code coverage across the codebase
- Comprehensive unit tests for state management, batching, and configuration
- 100% test coverage for Performance & Format features
- 100% test coverage for Unified Source API (Flink 1.12/FLIP-27)
- 100% test coverage for Catalog API (Flink 1.10)
- Additional tests for StructuredType, DataTypes, Table API, and model definitions
- Test classes made sealed for better code quality

### Infrastructure and Connectivity Fixes
- Fixed critical Flink cluster connectivity using config.yaml mounting
- Fixed Flink TaskManager connection with optimized 30s timeout
- Resolved TaskManager OOM issues by applying MemoryCalculator configurations
- Improved Kafka configuration - unified kafka:9092 endpoint for all Aspire connections
- Fixed Gateway Docker build to only run during dotnet build
- Enhanced Gateway metrics endpoint configuration
- Fixed JSON null handling in Gateway service

### Developer Experience
- Improved naming consistency - standardized to "FlinkDotNet JobGateway"
- Enhanced README with comprehensive architecture documentation
- Updated documentation with correct Docker image naming (devstress/flinkdotnet)
- Comprehensive Flink version coverage documentation (1.0-2.1) - 100% feature complete
- 15-day learning course updates with full Flink version tracking

### Code Analysis and Quality
- Fixed multiple code smells including formatting, sealed classes, collection initialization
- Resolved SonarCloud issues and code analysis warnings
- Fixed IDE0046 warnings by converting ternary to if statements
- Removed trailing whitespace, added accessibility modifiers and 'this' qualifiers
- Suppressed false positive RCS1085 warnings with pragma directives
- Enhanced validation error handling with ArgumentException

### Performance and Reliability
- Docker push retry logic added to all release workflows
- Improved release workflows to handle unmerged branches gracefully
- Gateway job submission improvements with GetJobDefinition method
- Prometheus metrics integration with retry logic
- Enhanced task timeout configurations for better reliability

## Baseline Features from v1.0.0

- Complete Apache Flink 2.1 DataStream API in C# with fluent interface
- Native .NET 9.0 SDK for building streaming jobs without Java code
- Full Kafka integration with sources and sinks support
- Event-time processing with watermarks and windowing (tumbling, sliding, session)
- Exactly-once semantics with checkpointing and savepoints
- JSON IR (Intermediate Representation) translator for job submission
- JobGateway service for submitting jobs to Flink clusters
- .NET Aspire integration for local development with one-command startup
- Complete observability stack (Prometheus, Grafana, Loki, OpenTelemetry)
- Temporal.io workflow orchestration integration
- Production-ready performance: 800K+ messages/sec throughput validated
- Dynamic scaling support with Flink 2.1 adaptive scheduler and reactive mode
- Advanced partitioning strategies (rebalance, rescale, forward, shuffle, broadcast, custom)
- Comprehensive documentation including 15-day learning course
- Three NuGet packages: FlinkDotNet.Common, FlinkDotNet.DataStream, Flink.JobBuilder
- Docker image for JobGateway service (devstress/flinkdotnet:latest)
- Multi-cluster orchestration support for enterprise deployments
- Complete Table API and SQL support via Flink SQL Gateway
- Stateful processing with timers and keyed state management

## Breaking Changes
None - v1.0.1 is fully backward compatible with v1.0.0

## Upgrade Instructions
Update your NuGet packages to v1.0.1:
```bash
dotnet add package FlinkDotNet.Common --version 1.0.1
dotnet add package FlinkDotNet.DataStream --version 1.0.1
dotnet add package Flink.JobBuilder --version 1.0.1
```

For Docker deployments:
```bash
docker pull devstress/flinkdotnet:1.0.1
# or use :latest for the most recent version
docker pull devstress/flinkdotnet:latest
```
