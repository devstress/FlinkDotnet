# Future: Comprehensive Prometheus Exporter Design

**Status**: Deferred - Not immediate priority
**Created**: 2025-10-17
**Priority**: Low (Future Enhancement)

## Context

This document contains a comprehensive design for building Prometheus exporters for FlinkDotnet.JobGateway, Kafka, and Apache Flink. This is a **future enhancement** and not required for current work.

## Current Priority: Fix Existing Tests First

Before implementing new exporters, we must **fix the existing observability UI tests** to properly verify metrics from Kafka and Apache Flink. Current tests allow empty results, which is incorrect.

## Detailed Design

See [`WIs/WI74_prometheus-exporter-design.md`](../WIs/WI74_prometheus-exporter-design.md) for the complete architectural design including:

- FlinkDotNet.Metrics.Prometheus package design
- JobGateway instrumentation strategy
- Apache Flink Prometheus reporter configuration
- Kafka JMX exporter integration
- Complete metrics taxonomy
- System architecture diagrams
- Implementation phases (8-10 days of work)

## When to Revisit

This design should be implemented **after**:
1. ✅ Current observability UI tests are fixed
2. ✅ Tests properly verify Kafka and Flink metrics (no empty results)
3. ✅ Baseline metric collection is validated
4. Decision is made that custom JobGateway metrics are needed

## Quick Reference: Key Technologies

- **prometheus-net**: .NET Prometheus client library
- **flink-metrics-prometheus-2.1.0.jar**: Flink's built-in Prometheus reporter
- **bitnami/jmx-exporter**: For Kafka JMX metrics
- **Prometheus naming convention**: Follow Apache Flink 2.1.0 patterns

## Estimated Effort

**Total**: 8-10 working days for full implementation across all components

---

**Note**: This is intentionally deferred. Focus on fixing existing test verification first.