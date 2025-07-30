# Flink.NET Documentation Index

This page provides a complete index of all available documentation for Flink.NET. Use this as a reference to navigate to specific topics and understand the overall documentation structure.

### Table of Contents
- [1. Introduction](#1-introduction)
- [2. Core Concepts](#2-core-concepts)
- [3. Developing Applications](#3-developing-applications)
- [4. Connectors](#4-connectors)
- [5. API Reference](#5-api-reference)
- [6. Deployment](#6-deployment)
- [7. Advanced Topics](#7-advanced-topics)
- [8. Quality Assurance & Testing](#8-quality-assurance--testing)

## 1. Introduction
*   **[Welcome to Flink.NET](Home.md)** - Main entry point with overview and architecture
*   **[Getting Started](Getting-Started.md)** - Quick start guide for new users
*   **[System Design Overview](System-Design-Overview.md)** - Complete system architecture
*   **[Business Requirements](Business-Requirements.md)** - Business context and requirements
*   **[Project Status and Roadmap](Project-Status-And-Roadmap.md)** - Current status and future plans

## 2. Core Concepts
*   **Architecture**
    *   [JobManager](Core-Concepts-JobManager.md) - Central coordinator component
    *   [TaskManager](Core-Concepts-TaskManager.md) - Task execution component
*   **Stream Processing**
    *   [Core Processing Features](Core-Processing-Features.md) - Stream processing capabilities
    *   [Operator Chaining](Operator-Chaining.md) - Performance optimization
    *   [Credit-Based Flow Control](Credit-Based-Flow-Control.md) - Backpressure management
*   **State Management**
    *   [State Management Overview](Core-Concepts-State-Management-Overview.md) - State handling concepts
    *   [RocksDB State Backend](Core-Concepts-RocksDB-State-Backend.md) - Production state backend
*   **Checkpointing & Fault Tolerance**
    *   [Checkpointing Overview](Core-Concepts-Checkpointing-Overview.md) - Fault tolerance mechanism
    *   [Checkpointing Barriers](Core-Concepts-Checkpointing-Barriers.md) - Technical implementation
    *   [Exactly-Once Semantics](Core-Concepts-Exactly-Once-Semantics.md) - Consistency guarantees
*   **Memory Management**
    *   [Memory Overview](Core-Concepts-Memory-Overview.md) - Memory architecture
    *   [JobManager Memory](Core-Concepts-Memory-JobManager.md) - JobManager memory configuration
    *   [TaskManager Memory](Core-Concepts-Memory-TaskManager.md) - TaskManager memory configuration
    *   [Network Memory](Core-Concepts-Memory-Network.md) - Network buffer management
    *   [Memory Tuning](Core-Concepts-Memory-Tuning.md) - Performance optimization
    *   [Memory Troubleshooting](Core-Concepts-Memory-Troubleshooting.md) - Common issues and solutions
*   **Serialization**
    *   [Serialization Overview](Core-Concepts-Serialization.md) - Data serialization concepts
    *   [Serialization Strategy](Core-Concepts-Serialization-Strategy.md) - Implementation approach

## 3. Developing Applications
*   **[Defining Data Types](Developing-Data-Types.md)** - Working with POCOs and data structures
*   **[Working with Operators](Developing-Operators.md)** - Implementing custom operators
*   **[Using RuntimeContext](Developing-RuntimeContext.md)** - Accessing runtime information
*   **[Working with State](Developing-State.md)** - State management in applications
*   **[Windowing API](Developing-Windowing-Api.md)** - Time-based data processing

## 4. Connectors
*   **[Overview](Connectors-Overview.md)** - Available connectors and connector development

## 5. API Reference
*   **[JobManager REST API](JobManager-Rest-Api.md)** - HTTP API documentation
*   **[JobManager gRPC API](JobManager-Grpc-Api.md)** - Internal gRPC API

## 6. Deployment
*   **[Kubernetes Deployment](Deployment-Kubernetes.md)** - Production deployment guide
*   **[Local Development](Deployment-Local.md)** - Local setup and testing
*   **[Aspire Local Development Setup](Aspire-Local-Development-Setup.md)** - .NET Aspire integration

## 7. Advanced Topics
*   **Performance & Monitoring**
    *   [Advanced Performance Tuning](Advanced-Performance-Tuning.md) - Optimization techniques
    *   [Metrics and Monitoring](Advanced-Metrics-Monitoring.md) - Observability setup
*   **Security**
    *   [Advanced Security](Advanced-Security.md) - Security configuration
*   **Backpressure & Flow Control**
    *   **[Flink.NET Backpressure: Complete Reference Guide](Backpressure-Complete-Reference.md)** - ⭐ **Complete reference** for all backpressure questions (performance guidance, scalability, best practices)
*   **Best Practices**
    *   [Stream Processing Patterns](Flink.Net-Best-Practices-Stream-Processing-Patterns.md) - Recommended patterns
    *   [Pipeline Architecture Comparison](Pipeline-Architecture-Comparison-Custom-vs-Flink.Net-Standard.md) - Architecture decisions

## 8. Quality Assurance & Testing
*   **[Stress Tests Overview](Stress-Tests-Overview.md)** - High-performance load testing
*   **[Reliability Tests Overview](Reliability-Tests-Overview.md)** - Fault tolerance testing
*   **[Complex Logic Stress Tests](Complex-Logic-Stress-Tests.md)** - Advanced integration testing scenarios
*   **[LocalTesting Interactive Environment](LocalTesting-Interactive-Environment.md)** - ⭐ **Complete UI guide** with screenshots for interactive debugging and monitoring

---

For the most up-to-date overview and quick start information, see the main [README.md](../../README.md).
