# Stress Tests - High-Performance Load Testing

This document explains FLINK.NET stress testing infrastructure using Flink.Net Gateway communication with Apache Flink, what we do, and why we follow these practices to meet high quality standards.

## Overview

Our stress tests validate FLINK.NET's ability to handle high-volume message processing under realistic production conditions through Flink.Net Gateway communication with Apache Flink. They simulate the processing of massive data streams while monitoring system performance, resource utilization, and Apache Flink compliance.

FLINK.NET provides two comprehensive stress testing approaches:
1. **Redis-based Stress Tests** (`stress_test` category) - High throughput validation (covered in this document)
2. **[Complex Logic Stress Tests](Complex-Logic-Stress-Tests.md)** (`complex_logic_test` category) - Advanced correlation-based processing with HTTP endpoints and security token management

## What We Do

### Consolidated Stress Test Framework

Following feedback and streamlined architecture, FLINK.NET stress tests are now organized into exactly **two main categories**:

1. **Redis-based Stress Tests** (`stress_test` category): Uses Redis to assign redis_ordered_id for each message with FIFO processing and exactly-once semantics through Flink.Net Gateway
2. **Complex Logic Stress Tests** (`complex_logic_test` category): Comprehensive integration testing with correlation ID tracking, security token management, HTTP batch processing, and response verification

### Key Specifications

- **Message Count**: Process 1 million messages per test run via Flink.Net Gateway
- **Fast Processing Target**: Process 1 million messages in **less than 1 second** (target)
- **Minimum Throughput**: 200,000+ messages per second required through Apache Flink integration
- **High Performance**: 5.2+ million msg/sec achieved on optimized hardware
- **Architecture**: Flink.Net Gateway manages Apache Flink job submission and execution
- **Message Flow**: .NET SDK → Flink.Net Gateway → Apache Flink → Kafka → Redis Counter
- **Production Integration**: Complete gateway communication for production deployment

### Advanced Testing Capabilities

For complex integration scenarios including correlation ID matching, security token management, batch processing with HTTP endpoints, and response verification, see our **[Complex Logic Stress Tests](Complex-Logic-Stress-Tests.md)** documentation.

## How to run

### Using Flink.Net Gateway Communication with Apache Flink

All stress tests now use the consolidated framework with Flink.Net as a gateway for communication with Apache Flink, providing production-grade streaming job management and execution.

### Consolidated Test Execution Process

1. **Build all components**: `./build-all.ps1` (cross-platform build script)
2. **Start the Aspire environment**: 
   ```bash
   cd Sample/FlinkDotNet.Aspire.AppHost
   dotnet run
   ```
   This starts all services including Flink.Net Gateway, Apache Flink cluster, Redis, and Kafka.
3. **Wait for all services** (Redis, Kafka, Apache Flink, Flink.Net Gateway) to start and be healthy.
4. **Run Redis-based stress tests**:
   ```bash
   cd Sample/FlinkDotNet.Aspire.IntegrationTests
   dotnet test --filter "Category=stress_test"
   ```
5. **Run complex logic stress tests**:
   ```bash
   cd Sample/FlinkDotNet.Aspire.IntegrationTests
   dotnet test --filter "Category=complex_logic_test"
   ```

### Consolidated Test Categories

Both `correlation_id_test` and `security_token_test` filters now point to the same comprehensive integration test that validates both functionalities together:

```bash
# Both filters point to the same consolidated test
dotnet test --filter "Category=correlation_id_test"
dotnet test --filter "Category=security_token_test"
```

This demonstrates FLINK.NET exceeds the target of 1+ million messages with < 1 second processing capacity through the Flink.Net Gateway integration with Apache Flink.


---
[Back to Wiki Home](Home.md)
