# WI: Fix Observability Video Test - Empty Data and Grafana Issues

**File**: `WIs/WI_observability-video-empty-data-fix.md`
**Title**: [Day05] Fix empty Prometheus queries and Grafana data source configuration  
**Description**: Video test passes but shows empty Prometheus queries and Grafana with no data source
**Priority**: High
**Component**: LearningCourse Day 05 Observability Tests
**Type**: Bug Fix
**Assignee**: AI Agent
**Created**: 2025-10-18
**Status**: Investigation

## User Feedback
"Have you ever verify the output? They are all empty or error shown in the page. Fix the all! Our grafana doesn't show anything useful, please also fix."

## Problem Statement
The UIVideoTest_EndToEndObservability test passes validation but the video recording shows:
- Empty Prometheus query results (no data returned)
- Grafana dashboards with no configured data source
- No actual metrics visualization despite test passing

## Root Cause Analysis (Investigation Phase)

### Issue 1: Grafana Data Source Not Configured
- Grafana starts with empty configuration (no data sources)
- Test navigates to Grafana but doesn't configure Prometheus as data source
- Grafana Explore view shows "No data source" error

### Issue 2: Prometheus Targets May Not Be UP
- prometheus.yml references container names that might not match actual Docker container names
- Kafka exporter target: `kafka-exporter:5556` but actual container name might be different
- Need to verify Prometheus targets are UP before querying metrics

### Issue 3: Metric Queries Timing Issues
- Prometheus scrape interval: 15 seconds
- Current wait time: 30 seconds after job start
- May not be enough time for multiple scrapes to populate rate() queries
- rate() queries need at least 2 data points

### Issue 4: Wrong Metric Names or Labels
- Queries might be using incorrect metric names
- Kafka JMX exporter metrics might have different naming
- Need to verify actual metrics available in Prometheus

## Phase 1: Investigation - Debug Information

### Current Test Flow
1. Start Exercise51 (10,000 messages)
2. Wait for job to start (~12s)
3. Wait for Prometheus ready (~0s - immediate)
4. Wait 30s for metrics to populate
5. Query Prometheus metrics (returning empty)
6. Navigate to Grafana (no data source configured)

### Prometheus Configuration Analysis
```yaml
scrape_interval: 15s  # Scrapes every 15 seconds

Targets:
- flink-jobmanager:9250  # Flink JobManager metrics
- flink-taskmanager:9251  # Flink TaskManager metrics  
- kafka-exporter:5556     # Kafka JMX metrics (POTENTIAL ISSUE)
- localhost:9090          # Prometheus self-metrics
```

### Required Fixes
1. **Configure Grafana Data Source via API**
   - POST to `/api/datasources` with Prometheus configuration
   - URL should point to Prometheus container endpoint

2. **Verify Prometheus Targets Before Querying**
   - Query `/api/v1/targets` to ensure all targets are UP
   - Wait for targets to become healthy
   - Log which targets are down

3. **Use Queries That Will Have Data**
   - Query Prometheus self-metrics first (always available)
   - Use simpler queries that don't require rate() initially
   - Verify metrics exist before using rate()

4. **Increase Wait Times**
   - Wait at least 45-60 seconds after job start
   - Allow 3-4 scrape intervals (45-60s) for rate() queries
   - Add progress logging during wait

## Phase 2: Design

### Solution Architecture

#### 1. Prometheus Target Verification
```csharp
private async Task WaitForPrometheusTargetsHealthyAsync(string prometheusEndpoint)
{
    // Query /api/v1/targets
    // Wait for flink-jobmanager, flink-taskmanager to be UP
    // Log status of kafka-exporter (may be down - that's OK for test)
    // Timeout after 60s
}
```

#### 2. Grafana Data Source Configuration
```csharp
private async Task ConfigureGrafanaDataSourceAsync(string grafanaEndpoint, string prometheusEndpoint)
{
    // POST /api/datasources
    // Body: { "name": "Prometheus", "type": "prometheus", "url": "http://prometheus:9090", "access": "proxy", "isDefault": true }
    // Handle 409 Conflict if already exists
}
```

#### 3. Enhanced Metric Querying
```csharp
private async Task<bool> VerifyMetricHasDataAsync(string prometheusEndpoint, string metric)
{
    // Query metric
    // Return true if result array has data
    // Return false if empty (don't fail - just skip visualization)
}
```

#### 4. Progressive Metric Discovery
- Start with Prometheus self-metrics (always available)
- Then Flink metrics (should be available after job starts)
- Finally Kafka metrics (may not be available if exporter is down)

### Updated Test Flow
1. Start Exercise51
2. Wait for job to start
3. **Verify Prometheus targets are healthy** (NEW)
4. Wait 60s for metrics to populate (INCREASED)
5. **Configure Grafana data source** (NEW)
6. Query Prometheus with fallback handling (ENHANCED)
7. Navigate to Grafana with working data source (FIXED)
8. Show Flink Dashboard

## Phase 3: Implementation Plan

### Step 1: Add Prometheus Target Verification
- [ ] Add `WaitForPrometheusTargetsHealthyAsync()` method
- [ ] Query `/api/v1/targets` endpoint
- [ ] Parse JSON response to check target health
- [ ] Log which targets are UP/DOWN

### Step 2: Add Grafana Data Source Configuration  
- [ ] Add `ConfigureGrafanaDataSourceAsync()` method
- [ ] Use Grafana API to create Prometheus data source
- [ ] Handle authentication (Grafana default: no auth required)
- [ ] Verify data source is working

### Step 3: Enhance Metric Querying
- [ ] Add `VerifyMetricHasDataAsync()` helper
- [ ] Update `QueryAndDisplayMetric()` to skip if no data
- [ ] Use fallback metrics that are guaranteed to exist
- [ ] Add more informative logging

### Step 4: Update Test Timing
- [ ] Increase wait time from 30s to 60s
- [ ] Add progress logging during wait
- [ ] Show scrape count estimate

### Step 5: Fix Docker Container Naming
- [ ] Investigate actual kafka-exporter container name
- [ ] Update prometheus.yml if needed
- [ ] OR: Make prometheus.yml use service discovery

## Phase 4: Testing Strategy

### Test Cases
1. **Prometheus Targets Verification**
   - All critical targets (Flink JM/TM) should be UP
   - Kafka exporter may be DOWN (acceptable)
   - Test should continue with available metrics

2. **Grafana Data Source**
   - Data source creation should succeed or already exist
   - Explore view should show Prometheus as available
   - Queries in Explore should return data

3. **Metric Availability**
   - At least Flink metrics should have data
   - Prometheus self-metrics should always work
   - Test should gracefully handle missing Kafka metrics

4. **Video Content**
   - Should show actual metric values (not empty queries)
   - Should show Grafana with working data source
   - Should show graphs rendering in Grafana Explore

## Phase 5: Lessons Learned (To Be Filled After Implementation)

### What Worked Well
- TBD

### What Could Be Improved
- TBD

### Key Insights for Similar Tasks
- TBD

### Problems to Avoid in Future
- TBD