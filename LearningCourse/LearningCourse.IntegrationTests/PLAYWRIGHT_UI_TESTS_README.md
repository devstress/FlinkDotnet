# Playwright UI Video Tests for Observability Stack

This document describes the Playwright UI testing infrastructure added to the LearningCourse integration tests for capturing video demonstrations of the Observability stack.

## Overview

The Playwright UI video tests provide automated browser-based testing with video recording capabilities for:
- **Comprehensive Message Tracking** (Day01): End-to-end demonstration of tracking messages through Flink Dashboard, Prometheus, and Grafana
- **Grafana Dashboard** (Day05): Visual demonstration of Grafana observability interface with anonymous access and Explore page
- **Prometheus End-to-End Message Tracking** (Day05): Comprehensive demonstration tracking messages through Prometheus metrics, Flink Dashboard, and complete pipeline monitoring

## Files Added

### 1. PlaywrightFixture.cs
**Location**: `LearningCourse/LearningCourse.IntegrationTests/PlaywrightFixture.cs`

Assembly-level fixture that manages Playwright browser lifecycle:
- Initializes Playwright and Chromium browser once for all tests
- Provides video recording infrastructure
- Handles browser cleanup after tests complete
- Creates isolated browser contexts for each test

**Key Features**:
- Headless browser mode for CI/CD compatibility
- Video recording at 1280x720 resolution
- Automatic video file naming with test name and timestamp
- Proper resource disposal

### 2. Day05Tests.cs (Enhanced)
**Location**: `LearningCourse/LearningCourse.IntegrationTests/Day05Tests.cs`

Added two new test methods:
- `UIVideoTest_GrafanaDashboard_ShouldNavigateSuccessfully()`: Tests Grafana UI navigation and captures video
- `UIVideoTest_PrometheusMetrics_ShouldNavigateSuccessfully()`: Tests Prometheus UI navigation and captures video

### 3. Package Reference
**Location**: `LearningCourse/LearningCourse.IntegrationTests/LearningCourse.IntegrationTests.csproj`

Added NuGet package:
```xml
<PackageReference Include="Microsoft.Playwright" Version="1.48.0" />
```

## Setup Instructions

### Prerequisites
1. .NET 9.0 SDK installed
2. Project built successfully
3. LocalTesting AppHost running with `LEARNINGCOURSE=true` environment variable

### Step 1: Build the Project
```bash
cd LearningCourse
dotnet build LearningCourse.IntegrationTests/LearningCourse.IntegrationTests.csproj --configuration Release
```

### Step 2: Install Playwright Browsers

**Option A: Using the provided script (Recommended)**
```powershell
cd LearningCourse/LearningCourse.IntegrationTests
.\install-playwright-browsers.ps1
```

**Option B: Manual installation**
```bash
# Install Playwright CLI globally
npm install -g playwright

# Or using npx
npx playwright install chromium

# Or using PowerShell Core
pwsh -c "playwright install chromium"
```

**Option C: Using Playwright CLI from NuGet package**
After building, the Playwright CLI tools are available in the bin directory. You can run:
```bash
# From the bin directory
playwright install chromium
```

### Step 3: Run the Tests

**Run only the UI video tests:**
```bash
cd LearningCourse
dotnet test LearningCourse.IntegrationTests/LearningCourse.IntegrationTests.csproj --filter "Category=ui-video"
```

**Run all Day05 tests (including UI video tests):**
```bash
dotnet test LearningCourse.IntegrationTests/LearningCourse.IntegrationTests.csproj --filter "Category=day05-enterprise-observability"
```

**Run a specific UI test:**
```bash
# Grafana test only
dotnet test --filter "FullyQualifiedName~UIVideoTest_GrafanaDashboard"

# Prometheus test only
dotnet test --filter "FullyQualifiedName~UIVideoTest_PrometheusMetrics"
```

## Video Output

### Location
Videos are saved to: `LocalTesting/test-logs/playwright-videos/`

### Naming Convention
- Format: `{TestName}_{Timestamp}.webm`
- Examples:
  - `GrafanaDashboard_20251017_100230.webm`
  - `PrometheusMetrics_20251017_100245.webm`
  - `ComprehensiveMessageTracking_20251017_100300.webm`

### Video Format
Videos are recorded in **WebM format** (native Playwright format):
- No conversion needed - faster video generation
- Smaller file sizes compared to MP4
- Excellent browser compatibility for playback
- Industry standard for web-based video recording

### Screenshots
Screenshots are also saved alongside videos:
- Format: `{Service}_{Timestamp}.png`
- Example: `Grafana_20251017_100230.png`

## Test Behavior

### Comprehensive Message Tracking Test (Day01)
**Purpose**: Demonstrates practical end-to-end observability for tracking messages through the data pipeline

**Workflow**:
1. Starts Exercise1 (capitalize strings) in background to generate real message flow
2. Navigates to Flink Dashboard (http://localhost:8086)
   - Shows running jobs and task managers
   - Captures job execution state
3. Navigates to Prometheus for metrics queries
   - Queries `flink_taskmanager_job_task_numRecordsIn` (input messages)
   - Queries `flink_taskmanager_job_task_numRecordsOut` (output messages)
   - Demonstrates how to track message throughput
4. Navigates to Grafana (anonymous access enabled)
   - Shows visualization interface
   - Demonstrates Explore page for ad-hoc queries
5. Captures 6-7 screenshots showing complete workflow
6. Records 90-120 second video demonstration
7. Waits for Exercise1 to complete (verifies full pipeline)

**Key Features**:
- Real message flow (not just UI navigation)
- Demonstrates practical debugging workflow
- Shows how to correlate metrics across tools
- Educational value for learning observability

### Grafana Dashboard Test (Day05)
**Purpose**: Demonstrates Grafana UI and anonymous access configuration

**Workflow**:
1. Navigates to Grafana endpoint (discovered from `GrafanaHostEndpoint`)
2. Verifies anonymous access works (no login required)
3. Shows main dashboard interface
4. Navigates to Explore page for query interface
5. Explores side navigation menu
6. Captures 3-5 screenshots
7. Records 30-60 second video

**Configuration**:
- Anonymous authentication enabled via `GF_AUTH_ANONYMOUS_ENABLED=true`
- Admin role granted to anonymous users for full access
- No login required for learning environment simplicity

### Prometheus End-to-End Message Tracking Test (Day05)
**Purpose**: Demonstrates comprehensive message tracking through Prometheus as centralized observability platform

**Workflow** (Complete Pipeline Tracking):
1. Starts Exercise1 (capitalize) in background to generate real message flow
2. Navigates to Prometheus endpoint (discovered from `PrometheusHostEndpoint`)
3. **Message Input Tracking**: Queries `flink_taskmanager_job_task_operator_numRecordsIn` to track messages received by Flink operators
4. **Visualization**: Switches to Graph view to show time-series visualization of message flow
5. **Message Output Tracking**: Queries `flink_taskmanager_job_task_operator_numRecordsOut` to track messages sent by Flink operators
6. **Throughput Analysis**: Calculates message processing rate using `rate(flink_taskmanager_job_task_operator_numRecordsOut[1m])` to show messages/second
7. **Flink Dashboard Integration**: Navigates to Flink Dashboard (http://localhost:8086) to show:
   - Running jobs and task managers
   - Job execution state and parallelism
   - Visual correlation with Prometheus metrics
8. **Infrastructure Health**: Returns to Prometheus Targets page to verify scrape status
9. Captures 8-9 screenshots demonstrating complete tracking workflow
10. Records 120-150 second video showing end-to-end message journey

**Centralized Tracking Features**:
- **Kafka → Flink → Kafka Pipeline**: Tracks complete message flow from input-topic through Flink processing to output-topic
- **Records IN/OUT Metrics**: Demonstrates how Prometheus tracks both incoming and outgoing message counts
- **Throughput Monitoring**: Shows real-time message processing rate calculation
- **Cross-Tool Correlation**: Links Prometheus metrics with Flink Dashboard visualizations
- **Infrastructure Monitoring**: Validates that Prometheus is successfully scraping Flink metrics

**Educational Value**:
- Shows Prometheus as single source of truth for observability
- Demonstrates how to track messages through entire streaming pipeline
- Teaches metric queries for troubleshooting message flow issues
- Correlates metrics with job execution state in Flink Dashboard

## Architecture

### Browser Management
- **Browser Type**: Chromium (headless)
- **Lifecycle**: One browser instance shared across all tests
- **Contexts**: Each test creates its own isolated browser context
- **Video Recording**: Enabled per context, finalized on context close

### Integration with LearningCourseTestBase
The UI tests inherit from [`LearningCourseTestBase`](LearningCourseTestBase.cs:1) which provides:
- Infrastructure setup via GlobalSetUp
- Endpoint discovery for Grafana and Prometheus
- Proper test lifecycle management

### Video Recording Flow
```
Test Start
  ↓
Create Browser Context (with video recording)
  ↓
Navigate to URL
  ↓
Interact with UI
  ↓
Take Screenshot
  ↓
Close Context (finalizes video)
  ↓
Rename Video File
  ↓
Test Complete
```

## Troubleshooting

### Issue: "Playwright not initialized"
**Cause**: PlaywrightFixture.OneTimeSetUp not called
**Solution**: Ensure tests are run with NUnit test runner that respects [SetUpFixture]

### Issue: "Grafana/Prometheus endpoint not available"
**Cause**: LEARNINGCOURSE environment variable not set or infrastructure not running
**Solution**: 
```bash
# Set environment variable
export LEARNINGCOURSE=true  # Linux/Mac
set LEARNINGCOURSE=true     # Windows CMD
$env:LEARNINGCOURSE="true"  # PowerShell

# Start LocalTesting AppHost
cd LocalTesting/LocalTesting.FlinkSqlAppHost
dotnet run
```

### Issue: "Browser not found"
**Cause**: Playwright browsers not installed
**Solution**: Run installation script or manual installation (see Step 2 above)

### Issue: Videos not being created
**Cause**: Insufficient permissions or disk space
**Solution**: 
- Check LocalTesting/test-logs/ directory permissions
- Ensure sufficient disk space
- Check test output for error messages

### Issue: CS8604 Warning (Possible null reference)
**Cause**: Compiler warns about potential null endpoint
**Solution**: This is expected behavior - tests check for null and fail gracefully with clear error message

## CI/CD Integration

### GitHub Actions Example
```yaml
- name: Install Playwright Browsers
  run: |
    cd LearningCourse/LearningCourse.IntegrationTests
    pwsh ./install-playwright-browsers.ps1

- name: Run UI Video Tests
  env:
    LEARNINGCOURSE: true
  run: |
    dotnet test LearningCourse/LearningCourse.IntegrationTests/LearningCourse.IntegrationTests.csproj \
      --filter "Category=ui-video" \
      --configuration Release

- name: Upload Test Videos
  if: always()
  uses: actions/upload-artifact@v3
  with:
    name: playwright-videos
    path: LocalTesting/test-logs/playwright-videos/
```

## Configuration

### Video Recording Settings
Configured in [`PlaywrightFixture.CreateContextWithVideoAsync()`](PlaywrightFixture.cs:91):
```csharp
ViewportSize = 1280x720
RecordVideoSize = 1280x720
RecordVideoDir = LocalTesting/test-logs/playwright-videos/
```

### Browser Launch Options
Configured in [`PlaywrightFixture.OneTimeSetUp()`](PlaywrightFixture.cs:47):
```csharp
Headless = true                        // CI/CD compatible
Args = ["--disable-dev-shm-usage"]     // Prevent /dev/shm issues in containers
```

## Performance Considerations

- **Browser Initialization**: ~2-3 seconds (done once per test session)
- **Test Execution Times**:
  - Grafana Dashboard: 60-90 seconds (includes Exercise1 startup)
  - Prometheus End-to-End Tracking: 120-150 seconds (includes Exercise1 execution + comprehensive metric queries + Flink Dashboard)
- **Video File Sizes**:
  - Grafana: ~2-3MB (WebM format)
  - Prometheus End-to-End: ~4-6MB (WebM format, longer duration with multiple tools)
- **Resource Usage**: Headless Chromium uses ~200-400MB RAM per browser context

## Future Enhancements

Potential improvements for future iterations:
1. **Kafka UI Integration**: Add Kafka UI container to browse topics and messages directly
2. **Pre-configured Grafana Dashboards**: Create dashboards specifically for FlinkDotNet metrics with pre-built panels
3. **Prometheus Alerting**: Demonstrate alert rules for message processing failures and backpressure
4. **Jaeger Tracing**: Add distributed tracing to follow individual messages through the pipeline
5. **Visual Regression Testing**: Compare screenshots against baselines to detect UI changes
6. **Multi-Browser Support**: Test with Firefox and WebKit browsers for cross-browser compatibility
7. **Interactive Grafana Dashboards**: Click through panels and drill-down into specific time ranges
8. **Real-time Metrics Animation**: Capture video showing metrics updating live during high-volume message processing
9. **Multi-Job Scenarios**: Track multiple Flink jobs running simultaneously in Prometheus
10. **Custom Flink Metrics**: Demonstrate tracking custom application metrics alongside system metrics

## References

- [Microsoft Playwright Documentation](https://playwright.dev/dotnet/)
- [NUnit Documentation](https://docs.nunit.org/)
- [Grafana Documentation](https://grafana.com/docs/)
- [Prometheus Documentation](https://prometheus.io/docs/)

## Support

For issues or questions:
1. Check troubleshooting section above
2. Review test execution logs in `LocalTesting/test-logs/`
3. Inspect video recordings for visual debugging
4. Consult Work Item WI73 for implementation details