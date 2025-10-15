using NUnit.Framework;

namespace LearningCourse.IntegrationTests;

/// <summary>
/// Integration tests for Day 4: Production-Grade Backpressure & Distributed Rate Limiting
///
/// Reference: https://flink.apache.org/2022/11/25/optimising-the-throughput-of-async-sinks-using-a-custom-ratelimitingstrategy/
///
/// These tests validate exercises based on distributed rate limiting patterns:
/// - Exercise 1: Netflix Global Rate Limiting Controller - Epoch-based budget minting
/// - Exercise 2: Uber Regional Redis Coordination - Atomic budget operations
/// - Exercise 3: LinkedIn High-Performance Gateway - Local token buckets and hot path
/// - Exercise 4: Production Deployment Strategies - Blue-Green, Canary, Rolling Update
/// - Exercise 5: Simple BackpressureQueue Implementation - Alternative approach comparison
///
/// Implementation: Uses FlinkDotNet with .NET Aspire for infrastructure
/// </summary>
[TestFixture]
[Category("day04-production-backpressure")]
[Category("integration")]
public class Day04Tests : LearningCourseTestBase
{
    private const string Exercise1Path = "Day04-Production-Backpressure/Exercise-Solutions/Exercise41";
    private const string Exercise2Path = "Day04-Production-Backpressure/Exercise-Solutions/Exercise42";
    private const string Exercise3Path = "Day04-Production-Backpressure/Exercise-Solutions/Exercise43";
    private const string Exercise4Path = "Day04-Production-Backpressure/Exercise-Solutions/Exercise44";
    private const string Exercise5Path = "Day04-Production-Backpressure/Exercise-Solutions/Exercise45";
    private static readonly TimeSpan ExerciseTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Exercise 1: Netflix Global Rate Limiting Controller (90 minutes)
    ///
    /// This test validates:
    /// - Epoch-based budget minting every 250ms
    /// - Cross-region coordination prevention
    /// - Policy distribution to regional banks
    /// - Pre-mint budget futures for fault tolerance
    /// - Netflix-scale global quota management
    ///
    /// Expected: Netflix-level coordination with 99.99% API gateway uptime
    /// </summary>
    [Test]
    [Description("Exercise 1: Netflix Global Rate Limiting Controller - Epoch-based coordination")]
    public async Task Exercise1_NetflixGlobalQuotaController_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 1: Netflix Global Rate Limiting Controller");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine();
        TestContext.WriteLine("Reference: Netflix Zuul 2 Distributed Rate Limiting");
        TestContext.WriteLine();
        TestContext.WriteLine("Testing concepts:");
        TestContext.WriteLine("  - Epoch-based budget minting (250ms intervals)");
        TestContext.WriteLine("  - Cross-region coordination prevention");
        TestContext.WriteLine("  - Policy distribution to regional banks");
        TestContext.WriteLine("  - Pre-mint budget futures");
        TestContext.WriteLine("  - Global quota management");
        TestContext.WriteLine();

        var (exitCode, output, error) = await ExecuteExerciseAsync(
            Exercise1Path,
            Array.Empty<string>(),
            ExerciseTimeout);

        TestContext.WriteLine();
        TestContext.WriteLine("--------------------------------------------------------------------------------");
        TestContext.WriteLine("Test Validation");
        TestContext.WriteLine("--------------------------------------------------------------------------------");

        var validationChecks = BuildExercise1ValidationChecks(output);
        ValidateExerciseResults(validationChecks, output, error, "Exercise 1");

        Assert.That(exitCode, Is.EqualTo(0),
            $"Exercise 1 should complete successfully. Exit code: {exitCode}\nError: {error}");

        TestContext.WriteLine();
        TestContext.WriteLine("✅ Exercise 1 completed successfully");
        TestContext.WriteLine("================================================================================");
    }

    /// <summary>
    /// Exercise 2: Uber Regional Redis Coordination (120 minutes)
    ///
    /// This test validates:
    /// - Atomic Redis operations with DECRBY
    /// - Regional budget bank management
    /// - TTL management for budget expiration
    /// - Regional failover handling
    /// - Uber-scale traffic coordination
    ///
    /// Expected: Uber-scale budget coordination handling 15M+ daily rides
    /// </summary>
    [Test]
    [Description("Exercise 2: Uber Regional Redis Coordination - Atomic budget operations")]
    public async Task Exercise2_UberRegionalBudgetBank_ShouldExecuteSuccessfully()
    {
        PrintExercise2Header();

        var (exitCode, output, error) = await ExecuteExerciseAsync(
            Exercise2Path,
            Array.Empty<string>(),
            ExerciseTimeout);

        TestContext.WriteLine();
        TestContext.WriteLine("--------------------------------------------------------------------------------");
        TestContext.WriteLine("Test Validation");
        TestContext.WriteLine("--------------------------------------------------------------------------------");

        var validationChecks = BuildExercise2ValidationChecks(output);
        ValidateExerciseResults(validationChecks, output, error, "Exercise 2");

        Assert.That(exitCode, Is.EqualTo(0),
            $"Exercise 2 should complete successfully. Exit code: {exitCode}\nError: {error}");

        TestContext.WriteLine();
        TestContext.WriteLine("✅ Exercise 2 completed successfully");
        TestContext.WriteLine("================================================================================");
    }

    /// <summary>
    /// Exercise 3: LinkedIn High-Performance Gateway (150 minutes)
    ///
    /// This test validates:
    /// - Local token buckets for hot path
    /// - Stateless rate limiting
    /// - Background refill from Regional Budget Bank
    /// - Safe by default startup behavior
    /// - LinkedIn-scale API gateway patterns
    ///
    /// Expected: LinkedIn-scale API gateway with 99.9% uptime during traffic spikes
    /// </summary>
    [Test]
    [Description("Exercise 3: LinkedIn High-Performance Gateway - Hot path rate limiting")]
    public async Task Exercise3_LinkedInAPIGateway_ShouldExecuteSuccessfully()
    {
        PrintExercise3Header();

        var (exitCode, output, error) = await ExecuteExerciseAsync(
            Exercise3Path,
            Array.Empty<string>(),
            ExerciseTimeout);

        TestContext.WriteLine();
        TestContext.WriteLine("--------------------------------------------------------------------------------");
        TestContext.WriteLine("Test Validation");
        TestContext.WriteLine("--------------------------------------------------------------------------------");

        var validationChecks = BuildExercise3ValidationChecks(output);
        ValidateExerciseResults(validationChecks, output, error, "Exercise 3");

        Assert.That(exitCode, Is.EqualTo(0),
            $"Exercise 3 should complete successfully. Exit code: {exitCode}\nError: {error}");

        TestContext.WriteLine();
        TestContext.WriteLine("✅ Exercise 3 completed successfully");
        TestContext.WriteLine("================================================================================");
    }

    /// <summary>
    /// Exercise 4: Production Deployment Strategies (90 minutes)
    ///
    /// This test validates:
    /// - Blue-Green deployment with instant traffic switching
    /// - Canary deployment with progressive rollout (1% → 5% → 25% → 100%)
    /// - Rolling update with batch-wise instance updates
    /// - Health check validation at deployment gates
    /// - Real Kafka/FlinkDotNet deployment orchestration
    ///
    /// Expected: Production-grade deployment strategies with real streaming infrastructure
    /// </summary>
    [Test]
    [Description("Exercise 4: Production Deployment - Blue-Green, Canary, Rolling Update")]
    public async Task Exercise4_ProductionDeployment_ShouldExecuteSuccessfully()
    {
        PrintExercise4Header();

        var (exitCode, output, error) = await ExecuteExerciseAsync(
            Exercise4Path,
            Array.Empty<string>(),
            ExerciseTimeout);

        TestContext.WriteLine();
        TestContext.WriteLine("--------------------------------------------------------------------------------");
        TestContext.WriteLine("Test Validation");
        TestContext.WriteLine("--------------------------------------------------------------------------------");

        var validationChecks = BuildExercise4ValidationChecks(output);
        ValidateExerciseResults(validationChecks, output, error, "Exercise 4");

        Assert.That(exitCode, Is.EqualTo(0),
            $"Exercise 4 should complete successfully. Exit code: {exitCode}\nError: {error}");

        TestContext.WriteLine();
        TestContext.WriteLine("✅ Exercise 4 completed successfully");
        TestContext.WriteLine("================================================================================");
    }

    /// <summary>
    /// Exercise 5: Simple BackpressureQueue Implementation (45 minutes)
    ///
    /// This test validates:
    /// - Semaphore-based backpressure limiting
    /// - Simple vs complex approach comparison
    /// - Three test scenarios with different configurations
    /// - When to use simple solutions over distributed patterns
    ///
    /// Expected: Clear understanding of simple vs complex backpressure trade-offs
    /// </summary>
    [Test]
    [Description("Exercise 5: Simple BackpressureQueue - Alternative approach comparison")]
    public async Task Exercise5_SimpleBackpressureQueue_ShouldExecuteSuccessfully()
    {
        PrintExercise5Header();

        var (exitCode, output, error) = await ExecuteExerciseAsync(
            Exercise5Path,
            Array.Empty<string>(),
            ExerciseTimeout);

        TestContext.WriteLine();
        TestContext.WriteLine("--------------------------------------------------------------------------------");
        TestContext.WriteLine("Test Validation");
        TestContext.WriteLine("--------------------------------------------------------------------------------");

        var validationChecks = BuildExercise5ValidationChecks(output);
        ValidateExerciseResults(validationChecks, output, error, "Exercise 5");

        Assert.That(exitCode, Is.EqualTo(0),
            $"Exercise 5 should complete successfully. Exit code: {exitCode}\nError: {error}");

        TestContext.WriteLine();
        TestContext.WriteLine("✅ Exercise 5 completed successfully");
        TestContext.WriteLine("================================================================================");
    }

    private static Dictionary<string, (bool result, string failureMessage)> BuildExercise1ValidationChecks(string output)
    {
        // STRICT validation for Exercise41: Must use real Kafka/FlinkDotNet infrastructure
        // Following WI39 requirements - Netflix-style adaptive backpressure with real streaming
        return new Dictionary<string, (bool result, string failureMessage)>
        {
            ["Infrastructure Ready"] = (
                output.Contains("Kafka is ready", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Flink cluster is healthy", StringComparison.OrdinalIgnoreCase),
                "Real infrastructure verification not found - Exercise41 must validate Kafka/Flink"
            ),
            ["Kafka Topics Created"] = (
                output.Contains("Topics created", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Topics already exist", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("streaming-requests", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("streaming-sessions", StringComparison.OrdinalIgnoreCase),
                "Kafka topic creation not found - Exercise41 must create real Kafka topics"
            ),
            ["FlinkDotNet Job Submission"] = (
                output.Contains("Flink job", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Submitting", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("JobId", StringComparison.OrdinalIgnoreCase),
                "FlinkDotNet job submission not found - Exercise41 must submit real Flink job"
            ),
            ["Messages Produced"] = (
                output.Contains("produced", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("requests generated", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("transactions produced", StringComparison.OrdinalIgnoreCase),
                "Real Kafka producer not found - Exercise41 must produce streaming requests"
            ),
            ["Quality Levels"] = (
                output.Contains("Ultra4K", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("HD1080p", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("HD720p", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("SD480p", StringComparison.OrdinalIgnoreCase),
                "Quality levels not demonstrated - Exercise41 must show Netflix quality adaptation"
            ),
            ["Backpressure Active"] = (
                output.Contains("Backpressure", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("quality adjust", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("degradat", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("adaptive", StringComparison.OrdinalIgnoreCase),
                "Adaptive backpressure not demonstrated - Exercise41 must show quality adjustments"
            ),
            ["Sessions Consumed"] = (
                output.Contains("sessions consumed", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("consumed", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("results", StringComparison.OrdinalIgnoreCase),
                "Streaming sessions not consumed - Exercise41 must consume from Kafka"
            ),
            ["Job Cleanup"] = (
                output.Contains("Cancelling", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("job cancelled", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Cleaning up", StringComparison.OrdinalIgnoreCase),
                "Flink job cleanup not performed - Exercise41 must cancel jobs properly"
            ),
            ["NO Simulation Patterns"] = (
                !output.Contains("ConcurrentQueue") &&
                !output.Contains("BackgroundService") &&
                !output.Contains("Task.Delay") &&
                !output.Contains("IAsyncEnumerable"),
                "CRITICAL: Simulation patterns detected - Exercise41 MUST use real Kafka/FlinkDotNet (no ConcurrentQueue, BackgroundService, Task.Delay)"
            ),
            ["Execution Completed"] = (
                output.Contains("COMPLETED SUCCESSFULLY", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("completed successfully", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("SUCCESS", StringComparison.OrdinalIgnoreCase),
                "Exercise did not complete successfully"
            )
        };
    }

    private static Dictionary<string, (bool result, string failureMessage)> BuildExercise2ValidationChecks(string output)
    {
        // STRICT validation for Exercise42: Must use real Kafka/FlinkDotNet infrastructure
        // Following WI40 requirements - Multi-tier rate limiting with real streaming
        return new Dictionary<string, (bool result, string failureMessage)>
        {
            ["Infrastructure Ready"] = (
                output.Contains("Kafka is ready", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Flink cluster is healthy", StringComparison.OrdinalIgnoreCase),
                "Real infrastructure verification not found - Exercise42 must validate Kafka/Flink"
            ),
            ["Kafka Topics Created"] = (
                output.Contains("Topics created", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Topics already exist", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("client-requests", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("gateway-filtered", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("application-filtered", StringComparison.OrdinalIgnoreCase),
                "Kafka topic creation not found - Exercise42 must create multi-tier topics"
            ),
            ["FlinkDotNet Jobs Submission"] = (
                output.Contains("Gateway Tier", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Application Tier", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Database Tier", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Flink job", StringComparison.OrdinalIgnoreCase),
                "FlinkDotNet job submission not found - Exercise42 must submit three-tier Flink jobs"
            ),
            ["Messages Produced"] = (
                output.Contains("produced", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("requests generated", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("client requests", StringComparison.OrdinalIgnoreCase),
                "Real Kafka producer not found - Exercise42 must produce client requests"
            ),
            ["User Tiers"] = (
                output.Contains("Free", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Premium", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Enterprise", StringComparison.OrdinalIgnoreCase),
                "User tiers not demonstrated - Exercise42 must show Free/Premium/Enterprise tier handling"
            ),
            ["Rate Limiting Tiers"] = (
                output.Contains("Gateway", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Application", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Database", StringComparison.OrdinalIgnoreCase),
                "Multi-tier rate limiting not demonstrated - Exercise42 must show all three tiers"
            ),
            ["Industry Patterns"] = (
                output.Contains("Twitter", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Uber", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Stripe", StringComparison.OrdinalIgnoreCase),
                "Industry patterns not referenced - Exercise42 must demonstrate Twitter/Uber/Stripe patterns"
            ),
            ["Results Consumed"] = (
                output.Contains("consumed", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("processed requests", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("results", StringComparison.OrdinalIgnoreCase),
                "Processed requests not consumed - Exercise42 must consume from final Kafka topic"
            ),
            ["Job Cleanup"] = (
                output.Contains("Cancelling", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("job cancelled", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Cleaning up", StringComparison.OrdinalIgnoreCase),
                "Flink job cleanup not performed - Exercise42 must cancel jobs properly"
            ),
            ["NO Simulation Patterns"] = (
                !output.Contains("ConcurrentQueue") &&
                !output.Contains("ConcurrentDictionary") &&
                !output.Contains("BackgroundService") &&
                !output.Contains("Task.Delay") &&
                !output.Contains("SemaphoreSlim"),
                "CRITICAL: Simulation patterns detected - Exercise42 MUST use real Kafka/FlinkDotNet (no ConcurrentQueue, ConcurrentDictionary, BackgroundService, Task.Delay, SemaphoreSlim)"
            ),
            ["Execution Completed"] = (
                output.Contains("COMPLETED SUCCESSFULLY", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("completed successfully", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("SUCCESS", StringComparison.OrdinalIgnoreCase),
                "Exercise did not complete successfully"
            )
        };
    }

    private static Dictionary<string, (bool result, string failureMessage)> BuildExercise3ValidationChecks(string output)
    {
        // STRICT validation for Exercise43: Must use real Kafka/FlinkDotNet infrastructure
        // Following WI41 requirements - Performance testing with real streaming metrics
        return new Dictionary<string, (bool result, string failureMessage)>
        {
            ["Infrastructure Ready"] = (
                output.Contains("Kafka is ready", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Flink cluster is healthy", StringComparison.OrdinalIgnoreCase),
                "Real infrastructure verification not found - Exercise43 must validate Kafka/Flink"
            ),
            ["Kafka Topics Created"] = (
                output.Contains("Topics created", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Topics already exist", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("performance-load-input", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("performance-latency-measurements", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("performance-throughput-metrics", StringComparison.OrdinalIgnoreCase),
                "Kafka topic creation not found - Exercise43 must create performance testing topics"
            ),
            ["FlinkDotNet Jobs Submission"] = (
                output.Contains("LoadGenerator", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("LatencyMeasurement", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("ThroughputBenchmark", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Flink job", StringComparison.OrdinalIgnoreCase),
                "FlinkDotNet job submission not found - Exercise43 must submit performance testing jobs"
            ),
            ["Load Patterns"] = (
                output.Contains("Constant", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Ramp", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Spike", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Stress", StringComparison.OrdinalIgnoreCase),
                "Load patterns not demonstrated - Exercise43 must show constant/ramp/spike/stress patterns"
            ),
            ["Latency Percentiles"] = (
                output.Contains("P50", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("P95", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("P99", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("percentile", StringComparison.OrdinalIgnoreCase),
                "Latency percentiles not found - Exercise43 must calculate P50/P95/P99 latencies"
            ),
            ["Throughput Metrics"] = (
                output.Contains("throughput", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("msg/sec", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("ops/sec", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("messages per second", StringComparison.OrdinalIgnoreCase),
                "Throughput metrics not found - Exercise43 must measure messages/sec"
            ),
            ["Industry Scenarios"] = (
                output.Contains("Netflix", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Uber", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Twitter", StringComparison.OrdinalIgnoreCase),
                "Industry scenarios not demonstrated - Exercise43 must show Netflix/Uber/Twitter patterns"
            ),
            ["Industry Benchmarks"] = (
                output.Contains("benchmark", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("100ms", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("23ms", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("50ms", StringComparison.OrdinalIgnoreCase),
                "Industry benchmarks not found - Exercise43 must reference real industry standards"
            ),
            ["Results Consumed"] = (
                output.Contains("consumed", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("results", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("metrics", StringComparison.OrdinalIgnoreCase),
                "Performance results not consumed - Exercise43 must consume from Kafka results topic"
            ),
            ["Job Cleanup"] = (
                output.Contains("Cancelling", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("job cancelled", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Cleaning up", StringComparison.OrdinalIgnoreCase),
                "Flink job cleanup not performed - Exercise43 must cancel jobs properly"
            ),
            ["NO Simulation Patterns"] = (
                !output.Contains("ConcurrentQueue") &&
                !output.Contains("ConcurrentDictionary") &&
                !output.Contains("BackgroundService") &&
                !output.Contains("Task.Delay", StringComparison.OrdinalIgnoreCase),
                "CRITICAL: Simulation patterns detected - Exercise43 MUST use real Kafka/FlinkDotNet (no ConcurrentQueue, ConcurrentDictionary, BackgroundService, Task.Delay for load generation)"
            ),
            ["Execution Completed"] = (
                output.Contains("COMPLETED SUCCESSFULLY", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("completed successfully", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("SUCCESS", StringComparison.OrdinalIgnoreCase),
                "Exercise did not complete successfully"
            )
        };
    }

    private static Dictionary<string, (bool result, string failureMessage)> BuildExercise4ValidationChecks(string output)
    {
        // STRICT validation for Exercise44: Must use real Kafka/FlinkDotNet infrastructure
        // Following WI42 requirements - Production deployment with real streaming orchestration
        return new Dictionary<string, (bool result, string failureMessage)>
        {
            ["Infrastructure Ready"] = (
                output.Contains("Kafka is ready", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Flink cluster is healthy", StringComparison.OrdinalIgnoreCase),
                "Real infrastructure verification not found - Exercise44 must validate Kafka/Flink"
            ),
            ["Kafka Topics Created"] = (
                output.Contains("Topics created", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Topics already exist", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("deployment-requests", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("blue-green-events", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("canary-events", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("rolling-update-events", StringComparison.OrdinalIgnoreCase),
                "Kafka topic creation not found - Exercise44 must create deployment orchestration topics"
            ),
            ["FlinkDotNet Jobs Submission"] = (
                output.Contains("BlueGreenDeployment", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("CanaryDeployment", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("RollingUpdate", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("HealthMonitor", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Flink job", StringComparison.OrdinalIgnoreCase),
                "FlinkDotNet job submission not found - Exercise44 must submit deployment orchestration jobs"
            ),
            ["Deployment Requests Produced"] = (
                output.Contains("produced", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("deployment request", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("triggering deployment", StringComparison.OrdinalIgnoreCase),
                "Real Kafka producer not found - Exercise44 must produce deployment requests"
            ),
            ["Blue-Green Strategy"] = (
                output.Contains("Blue-Green", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("BlueGreen", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("green environment", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("traffic switch", StringComparison.OrdinalIgnoreCase),
                "Blue-Green deployment not demonstrated - Exercise44 must show instant traffic switching"
            ),
            ["Canary Strategy"] = (
                output.Contains("Canary", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("1%", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("5%", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("25%", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("traffic", StringComparison.OrdinalIgnoreCase),
                "Canary deployment not demonstrated - Exercise44 must show progressive rollout"
            ),
            ["Rolling Update Strategy"] = (
                output.Contains("Rolling", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("batch", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("instances", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("instance-by-instance", StringComparison.OrdinalIgnoreCase),
                "Rolling update not demonstrated - Exercise44 must show batch-wise updates"
            ),
            ["Health Checks"] = (
                output.Contains("health check", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("health status", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("healthy", StringComparison.OrdinalIgnoreCase),
                "Health checks not demonstrated - Exercise44 must validate health during deployments"
            ),
            ["Industry Patterns"] = (
                output.Contains("Netflix", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("AWS", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Spotify", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Amazon", StringComparison.OrdinalIgnoreCase),
                "Industry patterns not referenced - Exercise44 must demonstrate Netflix/AWS patterns"
            ),
            ["Deployment Results Consumed"] = (
                output.Contains("consumed", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("deployment result", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("success", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("completed stages", StringComparison.OrdinalIgnoreCase),
                "Deployment results not consumed - Exercise44 must consume from results topic"
            ),
            ["Job Cleanup"] = (
                output.Contains("Cancelling", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("job cancelled", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Cleaning up", StringComparison.OrdinalIgnoreCase),
                "Flink job cleanup not performed - Exercise44 must cancel jobs properly"
            ),
            ["NO Simulation Patterns"] = (
                !output.Contains("ConcurrentQueue") &&
                !output.Contains("ConcurrentDictionary") &&
                !output.Contains("BackgroundService") &&
                !output.Contains("Task.Delay"),
                "CRITICAL: Simulation patterns detected - Exercise44 MUST use real Kafka/FlinkDotNet (no ConcurrentQueue, ConcurrentDictionary, BackgroundService, Task.Delay for deployment orchestration)"
            ),
            ["Execution Completed"] = (
                output.Contains("COMPLETED SUCCESSFULLY", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("completed successfully", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("SUCCESS", StringComparison.OrdinalIgnoreCase),
                "Exercise did not complete successfully"
            )
        };
    }

    private static Dictionary<string, (bool result, string failureMessage)> BuildExercise5ValidationChecks(string output)
    {
        return new Dictionary<string, (bool result, string failureMessage)>
        {
            ["Exercise Started"] = (output.Contains("Exercise 4.5", StringComparison.OrdinalIgnoreCase) ||
                                   output.Contains("BackpressureQueue", StringComparison.OrdinalIgnoreCase) ||
                                   output.Contains("Starting", StringComparison.OrdinalIgnoreCase),
                                   "Exercise not found"),
            ["Backpressure Implementation"] = (output.Contains("backpressure", StringComparison.OrdinalIgnoreCase) ||
                                              output.Contains("semaphore", StringComparison.OrdinalIgnoreCase) ||
                                              output.Contains("queue", StringComparison.OrdinalIgnoreCase) ||
                                              output.Contains("per-customer", StringComparison.OrdinalIgnoreCase),
                                              "Backpressure implementation not found"),
            ["Architecture"] = (output.Contains("Gateway", StringComparison.OrdinalIgnoreCase) ||
                               output.Contains("Kafka", StringComparison.OrdinalIgnoreCase) ||
                               output.Contains("Flink", StringComparison.OrdinalIgnoreCase) ||
                               output.Contains("Temporal", StringComparison.OrdinalIgnoreCase),
                               "Architecture components not found"),
            ["Execution Completed"] = (output.Contains("COMPLETED", StringComparison.OrdinalIgnoreCase) ||
                                      output.Contains("completed successfully", StringComparison.OrdinalIgnoreCase) ||
                                      output.Contains("SUCCESS", StringComparison.OrdinalIgnoreCase) ||
                                      output.Contains("✅"),
                                      "Exercise did not complete successfully")
        };
    }

    private static void PrintExercise2Header()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 2: Uber Regional Redis Coordination");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine();
        TestContext.WriteLine("Reference: Uber's Rate Limiting at Scale");
        TestContext.WriteLine();
        TestContext.WriteLine("Testing concepts:");
        TestContext.WriteLine("  - Atomic Redis operations with DECRBY");
        TestContext.WriteLine("  - Regional budget bank management");
        TestContext.WriteLine("  - TTL management for budget expiration");
        TestContext.WriteLine("  - Regional failover handling");
        TestContext.WriteLine("  - Uber-scale traffic coordination");
        TestContext.WriteLine();
    }

    private static void PrintExercise3Header()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 3: LinkedIn High-Performance Gateway");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine();
        TestContext.WriteLine("Reference: LinkedIn API Gateway Patterns");
        TestContext.WriteLine();
        TestContext.WriteLine("Testing concepts:");
        TestContext.WriteLine("  - Local token buckets for hot path");
        TestContext.WriteLine("  - Stateless rate limiting");
        TestContext.WriteLine("  - Background refill from Regional Budget Bank");
        TestContext.WriteLine("  - Safe by default startup behavior");
        TestContext.WriteLine("  - LinkedIn-scale API gateway patterns");
        TestContext.WriteLine();
    }

    private static void PrintExercise4Header()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 4: Production Deployment Strategies");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine();
        TestContext.WriteLine("Reference: Netflix Blue-Green, AWS Canary, Spotify Rolling Update");
        TestContext.WriteLine();
        TestContext.WriteLine("Testing concepts:");
        TestContext.WriteLine("  - Blue-Green deployment with instant traffic switching");
        TestContext.WriteLine("  - Canary deployment with progressive rollout (1% → 5% → 25% → 100%)");
        TestContext.WriteLine("  - Rolling update with batch-wise instance updates");
        TestContext.WriteLine("  - Health check validation at deployment gates");
        TestContext.WriteLine("  - Real Kafka/FlinkDotNet deployment orchestration");
        TestContext.WriteLine();
    }

    private static void PrintExercise5Header()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 5: Simple BackpressureQueue Implementation");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine();
        TestContext.WriteLine("Reference: Simple vs Complex Backpressure Patterns");
        TestContext.WriteLine();
        TestContext.WriteLine("Testing concepts:");
        TestContext.WriteLine("  - Semaphore-based backpressure limiting");
        TestContext.WriteLine("  - Simple vs complex approach comparison");
        TestContext.WriteLine("  - Three test scenarios with different configurations");
        TestContext.WriteLine("  - When to use simple solutions");
        TestContext.WriteLine();
    }

    private static void ValidateExerciseResults(
        Dictionary<string, (bool result, string failureMessage)> validationChecks,
        string output,
        string error,
        string exerciseName)
    {
        var validationFailures = new List<string>();

        foreach (var (checkName, (result, failureMessage)) in validationChecks)
        {
            TestContext.WriteLine($"[CHECK] {checkName}: {result}");
            if (!result)
            {
                validationFailures.Add($"{checkName}: {failureMessage}");
            }
        }

        if (validationFailures.Any())
        {
            ReportValidationFailures(validationFailures, output, error, exerciseName);
        }
    }

    private static void ReportValidationFailures(
        List<string> validationFailures,
        string output,
        string error,
        string exerciseName)
    {
        TestContext.WriteLine();
        TestContext.WriteLine("❌ Validation failures detected:");
        foreach (var failure in validationFailures)
        {
            TestContext.WriteLine($"   - {failure}");
        }
        TestContext.WriteLine();

        PrintDebugOutput(output, error);

        Assert.Fail($"{exerciseName} validation failed. See output above for details.");
    }

    private static void PrintDebugOutput(string output, string error)
    {
        TestContext.WriteLine();
        TestContext.WriteLine("Full Output:");
        TestContext.WriteLine("--------------------------------------------------------------------------------");
        TestContext.WriteLine(output);
        TestContext.WriteLine("--------------------------------------------------------------------------------");

        if (!string.IsNullOrEmpty(error))
        {
            TestContext.WriteLine();
            TestContext.WriteLine("Error Output:");
            TestContext.WriteLine("--------------------------------------------------------------------------------");
            TestContext.WriteLine(error);
            TestContext.WriteLine("--------------------------------------------------------------------------------");
        }
    }
}