# ObservabilityTesting Test Failures - Container Debugging Report

## Test Results

**Date:** 2025-11-01  
**Status:** 3 of 6 tests failed

### Test Summary
- ✅ Test3_GrafanaIntegration_ConfiguresDataSourceAndQueries: **PASSED** (64ms)
- ❌ Test1_GatewayMetrics_AggregatesAccurately: **FAILED** (timeout after 3 minutes)
- ❌ Test2_PrometheusIntegration_ScrapesMetricsSuccessfully: **FAILED** (timeout after 25 seconds)
- ❌ Test4_BackpressureAndCheckpoints_DetectsAccurately: **FAILED** (timeout after 3 minutes)  
- ❓ Test5 & Test6: Not reached (timed out)

## Root Cause Analysis

### Container Status
All 7 containers started and are running correctly:
```
kafka-b77f4790                    Up 7 minutes   127.0.0.1:32773->9092/tcp, 127.0.0.1:32774->9093/tcp
flink-jobmanager-b77f4790         Up 7 minutes   127.0.0.1:32770->8081/tcp, 127.0.0.1:32771->9250/tcp
flink-taskmanager-b77f4790        Up 7 minutes   127.0.0.1:32772->9251/tcp
flink-sql-gateway-b77f4790        Up 7 minutes   127.0.0.1:32775->8083/tcp
flinkdotnet-jobgateway-b77f4790   Up 6 minutes   127.0.0.1:32776->8086/tcp, 127.0.0.1:32777->9253/tcp
prometheus-b77f4790               Up 7 minutes   127.0.0.1:32769->9090/tcp
grafana-b77f4790                  Up 6 minutes   127.0.0.1:32778->3000/tcp
```

### Issue: Kafka Advertised Listeners

**Problem:** Kafka producers connecting from the host machine fail with:
```
Failed to resolve 'kafka:9093': Temporary failure in name resolution
```

**Cause:** After initial connection to `localhost:32774`, Kafka returns broker metadata with the internal hostname `kafka:9093`. The producer then tries to reconnect using this internal address, which is not resolvable from the host.

**Evidence:**
- Initial connection to `localhost:32774` succeeds
- Kafka returns `kafka:9093` in broker metadata
- Producer attempts to connect to `kafka:9093` and fails with DNS resolution error
- This is a standard Docker networking issue with Kafka advertised listeners

### Why Test3 Passed

Test3 (Grafana Integration) doesn't produce messages to Kafka, it only queries Grafana/Prometheus APIs. That's why it passed while tests that produce messages to Kafka failed.

## Technical Details

### Kafka Configuration Issue

Aspire.Hosting.Kafka configures two listeners:
- `PLAINTEXT://kafka:9092` - Internal (container-to-container)
- `PLAINTEXT_EXTERNAL://0.0.0.0:9093` - External (host access)

However, the `KAFKA_ADVERTISED_LISTENERS` environment variable needs to advertise:
- Internal: `PLAINTEXT://kafka:9092` (for Flink jobs)
- External: `PLAINTEXT_EXTERNAL://localhost:<dynamic-port>` (for host producers)

Currently, Kafka advertises its hostname (`kafka`) for the external listener, causing DNS resolution failures from the host.

### Container Logs Analysis

**Kafka logs show:**
- Kafka started successfully
- Topics created on demand (observability-test4-input, etc.)
- No errors in Kafka itself

**Test logs show:**
- Producers fail with "Failed to resolve 'kafka:9093'"
- Connection attempts to both `localhost:32774` and `kafka:9093`
- Timeouts after 3 minutes

## Solution

### Option 1: Configure Kafka Advertised Listeners (Recommended)

Need to configure Aspire.Hosting.Kafka to properly advertise localhost for external access. This requires:
1. Getting the dynamically assigned external port
2. Setting `KAFKA_ADVERTISED_LISTENERS` to include `localhost:<port>`

### Option 2: Use Kafka IP Address

Instead of using localhost, use the container's IP address directly. This avoids DNS issues but requires discovering the Kafka container IP.

### Option 3: Custom Kafka Resource

Create a custom Kafka resource that properly configures advertised listeners for testing scenarios.

## Immediate Workaround

The tests can work if we ensure producers use port 9092 (internal listener) instead of 9093 (external listener), but this requires all test code (including SampleApp) to run inside Docker containers, which defeats the purpose of integration testing from the host.

## Recommendation

Implement Option 1 by modifying the Kafka configuration in `ObservabilityTesting.FlinkSqlAppHost/Program.cs` to properly set advertised listeners with the dynamic port.
