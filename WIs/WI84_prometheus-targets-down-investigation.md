# WI84: Prometheus Targets DOWN Investigation

**File**: `WIs/WI84_prometheus-targets-down-investigation.md`
**Title**: [Observability] Investigate why 3 out of 5 Prometheus targets are DOWN
**Description**: Day05PrometheusMetricsTest shows 3 targets DOWN but doesn't capture error details
**Priority**: High
**Component**: Observability
**Type**: Bug Investigation
**Assignee**: AI Agent
**Created**: 2025-10-17
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI81: Fixed Kafka JMX Exporter Docker image
- WI83: Implemented custom Prometheus exporter for Gateway

### Lessons Applied
- Always debug infrastructure issues before implementing fixes
- Check Prometheus targets status to understand scraping failures
- Use host.docker.internal for Gateway (runs on host, not in Docker)

### Problems Prevented
- Not assuming metrics will work without verification
- Not skipping target health checks before querying metrics

## Phase 1: Investigation
### Requirements
Debug why 3 out of 5 Prometheus targets show as DOWN when test runs

### Debug Information (MANDATORY)
**Error Messages**: 
- Test output: "Targets UP: 2, Targets DOWN: 3"
- No specific error messages captured by regex patterns

**Observations**:
1. Prometheus container is running and accessible
2. Test waits 30 seconds for scraping but metrics still unavailable
3. Updated prometheus.yml to use `host.docker.internal:8080` for Gateway
4. Regex patterns in test don't match actual JSON structure

**System State**:
- Infrastructure: 10 containers running (confirmed in Aspire logs)
- Prometheus: http://127.0.0.1:39929 (dynamic port)
- Test: Waiting 30 seconds for initial scrape
- Result: 2 UP, 3 DOWN - but which targets?

**Reproduction Steps**:
1. Set LEARNINGCOURSE=true
2. Run Day05PrometheusMetricsTest
3. Test reports targets DOWN but regex fails to extract details

**Evidence**:
- Test successfully queries Prometheus `/api/v1/targets` endpoint
- Count of UP/DOWN targets is correct (2 UP, 3 DOWN)
- Job names and error messages not extracted by regex

**Next Investigation Steps**:
1. Query Prometheus targets API directly while infrastructure is running
2. Examine actual JSON response format to fix regex patterns
3. Identify which 3 targets are DOWN and root causes
4. Verify prometheus.yml configuration is loaded correctly
5. Check if containers are exposing metrics on expected ports

**Hypothesis**:
- Kafka JMX Exporter might not be exposing metrics correctly
- Flink containers might not have metrics JARs loaded properly
- Gateway `host.docker.internal:8080` might not resolve from Prometheus container
- Prometheus configuration might not be mounted/loaded correctly

## Current Status
**Phase**: Investigation - Need to query Prometheus targets API directly

**Blocker**: Cannot proceed without understanding which targets are DOWN and why

**Action Required**: Query http://127.0.0.1:XXXXX/api/v1/targets while infrastructure is running