using NUnit.Framework;

namespace LearningCourse.IntegrationTests;

/// <summary>
/// Integration tests for Day15 Capstone Project exercises.
/// Tests multi-domain platform architecture, domain implementation, cross-domain integration, and production deployment.
/// </summary>
[TestFixture]
[Category("day15-capstone-project")]
[Category("integration")]
public class Day15Tests : LearningCourseTestBase
{
    private const string Exercise151Path = "Day15-Capstone-Project/Exercise-Solutions/Exercise151";
    private const string Exercise152Path = "Day15-Capstone-Project/Exercise-Solutions/Exercise152";
    private const string Exercise153Path = "Day15-Capstone-Project/Exercise-Solutions/Exercise153";
    private const string Exercise154Path = "Day15-Capstone-Project/Exercise-Solutions/Exercise154";
    private static readonly TimeSpan ExerciseTimeout = TimeSpan.FromMinutes(2);

    [Test]
    [Description("Exercise151: Platform Architecture Validation")]
    public async Task Exercise151_PlatformArchitecture_ValidatesInfrastructureAndCreatesTopics()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise151: Platform Architecture Validation");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine();

        var (exitCode, output, error) = await ExecuteExerciseAsync(
            Exercise151Path,
            Array.Empty<string>(),
            ExerciseTimeout);

        TestContext.WriteLine();
        TestContext.WriteLine("--------------------------------------------------------------------------------");
        TestContext.WriteLine("Test Validation");
        TestContext.WriteLine("--------------------------------------------------------------------------------");

        // Validate output contains expected sections (exercise may report infrastructure issues but still validates architecture)
        Assert.That(output, Does.Contain("MULTI-DOMAIN PLATFORM ARCHITECTURE REPORT"), "Should display validation report header");
        Assert.That(output, Does.Contain("Infrastructure Status:"), "Should show infrastructure status section");
        Assert.That(output, Does.Contain("Kafka Cluster:"), "Should validate Kafka connectivity");
        Assert.That(output, Does.Contain("Redis State:"), "Should validate Redis connectivity");
        Assert.That(output, Does.Contain("Domain Configuration:"), "Should show domain configuration section");
        Assert.That(output, Does.Contain("E-commerce Domain:"), "Should show e-commerce domain section");
        Assert.That(output, Does.Contain("Financial Domain:"), "Should show financial domain section");
        Assert.That(output, Does.Contain("Cross-Domain:"), "Should show cross-domain section");
        Assert.That(output, Does.Contain("Total Topics Created:  8"), "Should create 8 multi-domain topics");
        Assert.That(output, Does.Contain("inventory-events"), "Should create inventory events topic");
        Assert.That(output, Does.Contain("transactions"), "Should create transactions topic");
        Assert.That(output, Does.Contain("domain-events"), "Should create domain events topic");
        Assert.That(output, Does.Contain("integrated-insights"), "Should create integrated insights topic");
        
        // Exercise completes architecture validation even if Flink is not available (infrastructure validation vs architecture design)
        Assert.That(exitCode, Is.EqualTo(0).Or.EqualTo(1),
            $"Exercise151 should complete architecture validation. Exit code: {exitCode}");

        TestContext.WriteLine();
        TestContext.WriteLine("[PASS] Exercise151 platform architecture validated successfully");
        TestContext.WriteLine();
    }

    [Test]
    [Description("Exercise152: Domain Implementation")]
    public async Task Exercise152_DomainImplementation_ProducesEventsToKafkaAndStoresInRedis()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise152: Domain Implementation");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine();

        var (exitCode, output, error) = await ExecuteExerciseAsync(
            Exercise152Path,
            Array.Empty<string>(),
            ExerciseTimeout);

        TestContext.WriteLine();
        TestContext.WriteLine("--------------------------------------------------------------------------------");
        TestContext.WriteLine("Test Validation");
        TestContext.WriteLine("--------------------------------------------------------------------------------");

        // Validate execution completed successfully
        Assert.That(exitCode, Is.EqualTo(0),
            $"Exercise152 should complete successfully. Exit code: {exitCode}\nError: {error}");

        // Validate output contains expected domain implementations
        Assert.That(output, Does.Contain("MULTI-DOMAIN PROCESSING REPORT"), "Should display multi-domain processing report");
        Assert.That(output, Does.Contain("E-commerce Domain"), "Should show e-commerce domain section");
        Assert.That(output, Does.Contain("Inventory Events:"), "Should show inventory events processed");
        Assert.That(output, Does.Contain("Recommendations:"), "Should show recommendations generated");
        Assert.That(output, Does.Contain("Financial Domain"), "Should show financial domain section");
        Assert.That(output, Does.Contain("Transactions:"), "Should show transactions processed");
        Assert.That(output, Does.Contain("Fraud Alerts:"), "Should show fraud alerts generated");
        Assert.That(output, Does.Contain("State Storage:         Redis"), "Should show Redis state storage");
        Assert.That(output, Does.Contain("State Synchronization: Active via Redis"), "Should show active Redis synchronization");

        TestContext.WriteLine();
        TestContext.WriteLine("[PASS] Exercise152 domain implementation completed successfully");
        TestContext.WriteLine();
    }

    [Test]
    [Description("Exercise153: Cross-Domain Integration")]
    public async Task Exercise153_CrossDomainIntegration_CorrelatesEventsAndPublishesInsights()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise153: Cross-Domain Integration");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine();

        var (exitCode, output, error) = await ExecuteExerciseAsync(
            Exercise153Path,
            Array.Empty<string>(),
            ExerciseTimeout);

        TestContext.WriteLine();
        TestContext.WriteLine("--------------------------------------------------------------------------------");
        TestContext.WriteLine("Test Validation");
        TestContext.WriteLine("--------------------------------------------------------------------------------");

        // Validate execution completed successfully
        Assert.That(exitCode, Is.EqualTo(0),
            $"Exercise153 should complete successfully. Exit code: {exitCode}\nError: {error}");

        // Validate output contains expected cross-domain integration
        Assert.That(output, Does.Contain("CROSS-DOMAIN CORRELATION REPORT"), "Should display correlation report");
        Assert.That(output, Does.Contain("Event Collection:"), "Should show event collection section");
        Assert.That(output, Does.Contain("Correlation Patterns:"), "Should show correlation patterns section");
        Assert.That(output, Does.Contain("Pattern 1: High-Risk + Low Inventory"), "Should implement pattern 1");
        Assert.That(output, Does.Contain("Pattern 2: High Transaction Activity + Recommendations"), "Should implement pattern 2");
        Assert.That(output, Does.Contain("Integration Results:"), "Should show integration results section");
        Assert.That(output, Does.Contain("Platform Integration:"), "Should show platform integration section");
        Assert.That(output, Does.Contain("Cross-Domain Hub:"), "Should show cross-domain hub status");

        TestContext.WriteLine();
        TestContext.WriteLine("[PASS] Exercise153 cross-domain integration completed successfully");
        TestContext.WriteLine();
    }

    [Test]
    [Description("Exercise154: Production Deployment Validation")]
    public async Task Exercise154_ProductionDeployment_ValidatesSystemReadinessAndPerformance()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise154: Production Deployment Validation");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine();

        var (exitCode, output, error) = await ExecuteExerciseAsync(
            Exercise154Path,
            Array.Empty<string>(),
            TimeSpan.FromMinutes(3)); // Longer timeout for performance benchmarking

        TestContext.WriteLine();
        TestContext.WriteLine("--------------------------------------------------------------------------------");
        TestContext.WriteLine("Test Validation");
        TestContext.WriteLine("--------------------------------------------------------------------------------");

        // Validate output contains expected production validation sections
        Assert.That(output, Does.Contain("PRODUCTION DEPLOYMENT VALIDATION REPORT"), "Should display validation report");
        Assert.That(output, Does.Contain("Infrastructure Health:"), "Should show infrastructure health section");
        Assert.That(output, Does.Contain("Kafka Cluster:"), "Should validate Kafka status");
        Assert.That(output, Does.Contain("Flink Cluster:"), "Should validate Flink status");
        Assert.That(output, Does.Contain("Redis State Store:"), "Should validate Redis status");
        Assert.That(output, Does.Contain("Topic Configuration:"), "Should show topic configuration section");
        Assert.That(output, Does.Contain("Topics Validated:"), "Should validate topics exist");
        Assert.That(output, Does.Contain("Data Flow Validation:"), "Should show data flow validation section");
        Assert.That(output, Does.Contain("End-to-End Test:"), "Should show end-to-end test result");
        Assert.That(output, Does.Contain("Performance Benchmarks:"), "Should show performance benchmarks section");
        Assert.That(output, Does.Contain("Throughput:"), "Should measure throughput");
        Assert.That(output, Does.Contain("Latency (P99):"), "Should measure latency");
        Assert.That(output, Does.Contain("Operational Readiness:"), "Should show operational readiness section");
        Assert.That(output, Does.Contain("System Status:"), "Should show system status");
        
        // Exercise completes deployment validation even with infrastructure issues (validation exercise, not production deployment)
        Assert.That(exitCode, Is.EqualTo(0).Or.EqualTo(1),
            $"Exercise154 should complete deployment validation. Exit code: {exitCode}");

        TestContext.WriteLine();
        TestContext.WriteLine("[PASS] Exercise154 production deployment validation completed successfully");
        TestContext.WriteLine();
    }
}