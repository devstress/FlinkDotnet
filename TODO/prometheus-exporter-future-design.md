# Future: Comprehensive Prometheus Exporter Design

**Status**: Deferred - Not immediate priority
**Created**: 2025-10-17
**Priority**: Low (Future Enhancement)

## Context

This document contains a comprehensive design for building Prometheus exporters for FlinkDotnet.JobGateway, Kafka, and Apache Flink. This is a **future enhancement** and not required for current work.

## Current Priority: Fix Existing Tests First

Before implementing new exporters, we must **fix the existing observability UI tests** to properly verify metrics from Kafka and Apache Flink. Current tests allow empty results, which is incorrect.

## ReleasePackagesTesting: Pure Aspire Network Architecture

**Key Insight**: ReleasePackagesTesting uses **100% container-based deployment**, eliminating cross-bridge networking issues and enabling simplified Prometheus configuration.

### All Components Run as Containers

In `ReleasePackagesTesting/ReleasePackagesTesting.FlinkSqlAppHost/Program.cs`, all components are deployed using `.AddContainer()`:

| Component | Container Name | Port | Deployment Method |
|-----------|---------------|------|-------------------|
| **Flink JobManager** | `flink-jobmanager` | 8081 (HTTP), 9250 (metrics) | `.AddContainer("flink-jobmanager", "flink:2.1.0-java17")` |
| **Flink TaskManager** | `flink-taskmanager` | 9251 (metrics) | `.AddContainer("flink-taskmanager", "flink:2.1.0-java17")` |
| **Flink SQL Gateway** | `flink-sql-gateway` | 8083 (HTTP), 9252 (metrics) | `.AddContainer("flink-sql-gateway", "flink:2.1.0-java17")` |
| **JobGateway** | `flink-job-gateway` | 8080 (HTTP) | `.AddContainer("flink-job-gateway", "flinkdotnet/jobgateway", "latest")` |
| **Kafka** | `kafka` | 9092 (broker), 9101 (JMX) | `.AddKafka("kafka")` |
| **Kafka JMX Exporter** | `kafka-exporter` | 5556 (metrics) | `.AddContainer("kafka-exporter", "bitnami/jmx-exporter", "latest")` |
| **Prometheus** | `prometheus` | 9090 | `.AddContainer("prometheus", "prom/prometheus", "latest")` |
| **Grafana** | `grafana` | 3000 | `.AddContainer("grafana", "grafana/grafana", "latest")` |

### Pure Aspire Network Benefits

**No cross-bridge issues**: All containers run on the same Docker network managed by Aspire, enabling simple DNS resolution.

**Prometheus Configuration** (`ReleasePackagesTesting/prometheus.yml`):
```yaml
scrape_configs:
  # Flink JobManager - Simple container name resolution
  - job_name: 'flink-jobmanager'
    static_configs:
      - targets: ['flink-jobmanager:9250']

  # Flink TaskManager - Simple container name resolution  
  - job_name: 'flink-taskmanager'
    static_configs:
      - targets: ['flink-taskmanager:9251']

  # Kafka via JMX Exporter - Simple container name resolution
  - job_name: 'kafka'
    static_configs:
      - targets: ['kafka-exporter:5556']
```

**Future: JobGateway Metrics** (when implemented):
```yaml
  # JobGateway - Would work immediately with container networking
  - job_name: 'job-gateway'
    static_configs:
      - targets: ['flink-job-gateway:8080']  # No host networking needed!
```

### LocalTesting vs ReleasePackagesTesting Architecture

| Aspect | LocalTesting | ReleasePackagesTesting |
|--------|--------------|------------------------|
| **JobGateway Deployment** | `.AddProject()` - Host process | `.AddContainer()` - Docker container |
| **Container Network** | Mixed (containers + host) | Pure containers (Aspire network) |
| **Prometheus Scraping** | ❌ Cannot easily scrape JobGateway | ✅ Can scrape all components |
| **Cross-Bridge Issues** | ⚠️ Yes (requires 172.17.0.1 gateway) | ✅ No (all on same network) |
| **DNS Resolution** | Container-to-container only | All components use container names |
| **Use Case** | Local development, debugging | Production image validation |
| **JobGateway Metrics** | Complex (needs host networking) | Simple (container name:port) |

### Why ReleasePackagesTesting for Prometheus Work

**Advantages**:
1. **Production-like architecture**: Uses actual Docker images (`flinkdotnet/jobgateway:latest`)
2. **Simplified configuration**: All Prometheus targets use container names
3. **No workarounds needed**: No host networking, bridge IPs, or special DNS handling
4. **Validation environment**: Tests the exact deployment that customers would use
5. **Future-proof**: When JobGateway adds metrics endpoint, Prometheus can immediately use it

**When to Use Each Environment**:
- **LocalTesting**: Local development, fast iteration, debugging JobGateway code changes
- **ReleasePackagesTesting**: Prometheus metrics work, production architecture validation, release testing

## Detailed Design

**Note**: The detailed design document `WIs/WI74_prometheus-exporter-design.md` referenced below is planned but not yet created. See the Quick Reference and ReleasePackagesTesting sections above for current architecture.

Future design should include:

- FlinkDotNet.Metrics.Prometheus package design
- JobGateway instrumentation strategy (using prometheus-net)
- Apache Flink Prometheus reporter configuration (already working via FLINK_PROPERTIES)
- Kafka JMX exporter integration (already working in LEARNINGCOURSE mode)
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

## Implementation Recommendation

**✅ Use ReleasePackagesTesting** as the primary environment for Prometheus metrics implementation and validation:

1. **Development**: Implement metrics endpoints in JobGateway using prometheus-net
2. **Testing**: Validate metrics collection in ReleasePackagesTesting (pure container architecture)
3. **Configuration**: Update `ReleasePackagesTesting/prometheus.yml` with JobGateway scrape target
4. **Verification**: Ensure all metrics appear in Prometheus and Grafana dashboards
5. **Documentation**: Document metrics in observability guides

**LocalTesting Considerations**: 
- JobGateway metrics would require container-to-host networking (complex)
- Consider future migration to containerized JobGateway in LocalTesting for consistency
- Current LocalTesting design is optimized for development speed over production architecture

---

**Note**: This is intentionally deferred. Focus on fixing existing test verification first.

**Architecture Insight**: ReleasePackagesTesting's pure container deployment eliminates cross-bridge networking issues, making it the ideal environment for comprehensive Prometheus metrics implementation.