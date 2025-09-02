# BDD Tests for Flink.NET Aspire Integration

This directory contains Behavior-Driven Development (BDD) tests using **Reqnroll** (successor to SpecFlow) integrated with **xUnit** for Flink.NET Aspire integration testing.

## Overview

The BDD tests cover the core requirements specified in the issue:

### 🚀 Stress Test Scenarios
1. **1 Million Message FIFO Processing**: Validates high-throughput message processing with exactly-once semantics
2. **Performance Metrics Validation**: Ensures throughput, latency, and resource utilization requirements
3. **Partition Distribution**: Verifies equal message distribution across 100 input/output partitions

### 🛡️ Reliability Test Scenarios  
1. **10% Failure Handling with DLQ**: Tests fault injection with Dead Letter Queue processing
2. **Backpressure and Rebalancing**: Validates consumer rebalancing under load
3. **Fault Recovery from Checkpoints**: Tests system recovery mechanisms
4. **System Health Monitoring**: Validates metrics and alerting capabilities

## Framework Stack

- **BDD Framework**: [Reqnroll 2.1.0](https://www.reqnroll.net/) - Modern .NET BDD framework
- **Test Runner**: xUnit integration with Reqnroll.xUnit
- **Target Platform**: .NET 8.0

## File Structure

```
FlinkDotNet.Aspire.IntegrationTests/
├── Features/
│   ├── StressTest.feature          # Gherkin scenarios for stress testing
│   ├── ReliabilityTest.feature     # Gherkin scenarios for reliability testing
│   ├── StressTest.feature.cs       # Generated test classes (auto-generated)
│   └── ReliabilityTest.feature.cs  # Generated test classes (auto-generated)
├── StepDefinitions/
│   ├── StressTestStepDefinitions.cs      # Step implementations for stress tests
│   └── ReliabilityTestStepDefinitions.cs # Step implementations for reliability tests
├── BDDTests.runsettings           # Test execution settings
└── README.md                      # This documentation
```

## Quick Start

### 1. Run All BDD Tests

```bash
# Run all BDD tests
dotnet test

# Run specific test categories
dotnet test --filter "Category=stress_test" --logger "console;verbosity=normal"
dotnet test --filter "Category=reliability_test" --logger "console;verbosity=normal"
```

## Test Scenarios Detail

### Stress Test Features (`StressTest.feature`)

#### Scenario: Process 1 Million Messages with FIFO and Exactly-Once Semantics
- **Given**: Flink cluster, Redis, and Kafka with 100 partitions are ready
- **When**: 1,000,000 messages are produced and processed through the pipeline
- **Then**: All messages are processed with FIFO order and exactly-once semantics

#### Scenario: Validate High-Throughput Performance Metrics  
- **Given**: Stress test pipeline is configured
- **When**: Messages are processed through the pipeline
- **Then**: Throughput ≥ 1,000 msg/sec, latency < 10s, CPU < 80%, Redis ops < 50ms

#### Scenario: Verify Equal Distribution Across Partitions
- **Given**: 100 input and output partitions configured
- **When**: Messages are evenly distributed and processed
- **Then**: Each partition receives ~10,000 messages (±5%) with balanced distribution

### Reliability Test Features (`ReliabilityTest.feature`)

#### Scenario: Handle 10% Message Failures with DLQ Processing
- **Given**: Fault tolerance enabled with 10% failure rate
- **When**: 1,000,000 messages are processed with fault injection  
- **Then**: ~90% reach output topic, ~10% go to DLQ, total count preserved

#### Scenario: Handle Backpressure with Consumer Rebalancing
- **Given**: Multi-partition setup with slow processing (backpressure)
- **When**: High-rate production with rebalancing events triggered
- **Then**: System handles backpressure gracefully without message loss

#### Scenario: Validate Fault Recovery from Checkpoints
- **Given**: Checkpointing enabled with various fault types
- **When**: System faults are introduced during processing
- **Then**: Automatic recovery from checkpoints with < 2 min recovery time

#### Scenario: Monitor System Health During Reliability Testing
- **Given**: Monitoring and metrics collection enabled
- **When**: Reliability tests run with 10% failures
- **Then**: All metrics are available, alerts trigger, dashboards show health

## Technical Implementation

### Step Definitions

The step definitions implement the behavior described in the Gherkin scenarios:

- **Given** steps: Set up test infrastructure (Kafka, Flink, Redis)
- **When** steps: Execute actions (produce messages, start jobs, inject faults)
- **Then** steps: Validate outcomes (message counts, performance metrics, system health)

### Simulation vs Real Implementation

The current implementation uses **simulation** for demonstration purposes:
- Infrastructure validation is mocked
- Message processing is simulated with appropriate delays
- Performance metrics return realistic simulated values

For **production testing**, replace the simulation methods with actual:
- Kafka producer/consumer implementations
- Flink job submission and monitoring
- Redis integration
- Performance metric collection

### Simulation vs Real Implementation

The current implementation uses **simulation** for demonstration purposes:
- Infrastructure validation is mocked
- Message processing is simulated with appropriate delays
- Performance metrics return realistic simulated values

For **production testing**, replace the simulation methods with actual:
- Kafka producer/consumer implementations
- Flink job submission and monitoring
- Redis integration
- Performance metric collection

## Configuration

### Environment Variables

```bash
# Optional: Override default message count for testing
export FLINKDOTNET_STANDARD_TEST_MESSAGES=100000
```

### Test Settings

Modify `BDDTests.runsettings` to customize:
- Test execution timeout
- Code coverage settings

## Integration with CI/CD

### GitHub Actions Example

```yaml
- name: Run BDD Tests
  run: |
    cd Sample/FlinkDotNet.Aspire.IntegrationTests
    dotnet test --filter "Category=stress_test|Category=reliability_test"
```

### Azure DevOps Example

```yaml
- task: DotNetCoreCLI@2
  displayName: 'Run BDD Tests'
  inputs:
    command: 'test'
    projects: 'Sample/FlinkDotNet.Aspire.IntegrationTests/*.csproj'
    arguments: '--filter "Category=stress_test|Category=reliability_test"'
```

## Troubleshooting

### Common Issues

1. **Missing Step Definitions**: If tests show "No matching step definition found"
   - Check step definition regex patterns
   - Ensure step definition classes are properly tagged with `[Binding]`
   - Verify namespace and assembly references

2. **Test Failures Due to Timeouts**:
   - Adjust timeout values in step definitions
   - Modify delay simulation values for faster execution
   - Check system resources during test execution

### Debug Mode

Run tests with verbose output for debugging:

```bash
dotnet test --filter "Category=stress_test" --logger "console;verbosity=diagnostic"
```

## Future Enhancements

1. **Real Infrastructure Integration**: Replace simulation with actual Kafka, Flink, and Redis
2. **Performance Benchmarking**: Add baseline performance comparisons
3. **Advanced Scenarios**: Additional fault types and recovery patterns
4. **Parallel Execution**: Run scenarios in parallel for faster execution
5. **Custom Reporters**: Additional reporting formats (JUnit, TeamCity)

## Contributing

When adding new BDD scenarios:

1. Write the Gherkin scenario in appropriate `.feature` file
2. Implement step definitions in corresponding `StepDefinitions` class
3. Add appropriate assertions and logging
4. Update this documentation
5. Test locally with `dotnet test`

## References

- [Reqnroll Documentation](https://www.reqnroll.net/)
- [Gherkin Syntax](https://cucumber.io/docs/gherkin/)
- [xUnit.net](https://xunit.net/)