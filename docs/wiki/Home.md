# Welcome to Flink.NET

## Overview of Flink.NET

Flink.NET is a comprehensive Apache Flink integration solution that enables .NET developers to build and submit streaming jobs to Apache Flink clusters using a fluent C# DSL. It provides a bridge between the .NET ecosystem and Apache Flink's powerful distributed stream processing capabilities.

*(See main [Readme.md](../../Readme.md) for more details)*

## Key Features & Goals

*   **Fluent C# DSL:** Write streaming job definitions using an intuitive, type-safe C# API
*   **Apache Flink Integration:** Submit jobs to existing Apache Flink clusters without running JVM code locally
*   **JSON Intermediate Representation:** Clean separation between .NET job definition and Flink execution
*   **Kubernetes Ready:** Complete deployment manifests for production Apache Flink clusters
*   **REST API Gateway:** .NET ASP.NET Core Web API that translates .NET job definitions to Flink DataStream jobs

*(See main [Readme.md](../../Readme.md) for more details)*

## Architecture Overview

Flink.NET provides an integration architecture that connects .NET applications with Apache Flink:

*   **Integration Philosophy:** Flink.NET acts as a bridge to Apache Flink clusters rather than reimplementing stream processing in .NET
*   **Key Components:**
    *   **.NET SDK (Flink.JobBuilder):** Provides fluent C# API for job definition
    *   **Job Gateway (.NET ASP.NET Core):** Translates job definitions to Apache Flink DataStream API
    *   **Apache Flink Cluster:** Handles actual stream processing execution

**External References:**

*   [Apache Flink Documentation](https://flink.apache.org/) - Reference for stream processing concepts
*   [Stream Processing Overview](https://nightlies.apache.org/flink/flink-docs-stable/docs/concepts/overview/) - Foundational concepts

## Getting Started

Ready to dive in? Our [Getting Started](Getting-Started.md) guide will walk you through setting up FlinkDotnet, ensuring Java + Maven prerequisites, and deploying Apache Flink infrastructure.

## Use Cases

Flink.NET with Apache Flink integration can be used for a variety of stream processing applications, including:

*   Real-time data analytics with Apache Flink's high-throughput processing
*   Event-driven microservices integration with Kafka sources and sinks
*   Complex event processing using Flink's windowing and state management
*   Data ingestion and transformation pipelines with exactly-once semantics
*   Anomaly detection using Flink's CEP (Complex Event Processing) capabilities

## Core Documentation

For more details:

* **[Usage Examples](Usage-Examples.md)** - Step-by-step examples with real configuration
* **[Gateway API](Gateway-API.md)** - Communication architecture and API details

## Testing

FlinkDotnet includes comprehensive testing capabilities:

* Integration tests for application structure validation
* Stress tests for high-throughput performance validation  
* BDD testing framework with SpecFlow integration

Learn more in our [Getting Started](Getting-Started.md) guide.

## Community & Contribution

Flink.NET is an open-source project, and we welcome contributions from the community!

*   **Getting Involved:** Join our community channels (links to be added) to ask questions, share ideas, and connect with other users.
*   **Contribution Guidelines:** Please see the main [Readme.md](../../Readme.md#getting-involved--contribution) for details on how to contribute to the project.
