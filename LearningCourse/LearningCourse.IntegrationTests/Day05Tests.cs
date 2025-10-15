using NUnit.Framework;

namespace LearningCourse.IntegrationTests;

/// <summary>
/// Integration tests for Day 5: Enterprise Observability
///
/// These tests validate exercises for enterprise observability patterns:
/// - Exercise 1: Observability Infrastructure Setup
/// - Exercise 2: Metrics and Monitoring Implementation
/// - Exercise 3: Distributed Tracing Configuration
/// - Exercise 4: Alerting and Dashboards
///
/// Implementation: Uses FlinkDotNet with .NET Aspire for infrastructure
/// </summary>
[TestFixture]
[Category("day05-enterprise-observability")]
[Category("integration")]
public class Day05Tests : LearningCourseTestBase
{
    private const string Exercise1Path = "Day05-Enterprise-Observability/Exercise-Solutions/Exercise51";
    private const string Exercise2Path = "Day05-Enterprise-Observability/Exercise-Solutions/Exercise52";
    private const string Exercise3Path = "Day05-Enterprise-Observability/Exercise-Solutions/Exercise53";
    private const string Exercise4Path = "Day05-Enterprise-Observability/Exercise-Solutions/Exercise54";
    private static readonly TimeSpan ExerciseTimeout = TimeSpan.FromMinutes(3);

    [Test]
    [Description("Exercise 1: Observability Infrastructure Setup")]
    public async Task Exercise1_ObservabilityInfrastructure_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 1: Observability Infrastructure Setup");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise1Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 1 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 1 completed successfully");
    }

    [Test]
    [Description("Exercise 2: Metrics and Monitoring Implementation")]
    public async Task Exercise2_MetricsMonitoring_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 2: Metrics and Monitoring Implementation");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise2Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 2 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 2 completed successfully");
    }

    [Test]
    [Description("Exercise 3: Distributed Tracing Configuration")]
    public async Task Exercise3_DistributedTracing_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 3: Distributed Tracing Configuration");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise3Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 3 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 3 completed successfully");
    }

    [Test]
    [Description("Exercise 4: Alerting and Dashboards")]
    public async Task Exercise4_AlertingDashboards_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 4: Alerting and Dashboards");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise4Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 4 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 4 completed successfully");
    }
}