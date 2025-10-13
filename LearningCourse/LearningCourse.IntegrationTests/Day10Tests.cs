using NUnit.Framework;

namespace LearningCourse.IntegrationTests;

/// <summary>
/// Integration tests for Day 10: Performance Optimization & Scaling
///
/// These tests validate exercises for performance optimization patterns:
/// - Exercise 1: Performance Profiling Setup
/// - Exercise 2: Resource Optimization Techniques
/// - Exercise 3: Horizontal Scaling Configuration
/// - Exercise 4: Advanced Performance Tuning
///
/// Implementation: Uses FlinkDotNet with .NET Aspire for infrastructure
/// </summary>
[TestFixture]
[Category("day10-performance-optimization-scaling")]
[Category("integration")]
public class Day10Tests : LearningCourseTestBase
{
    private const string Exercise1Path = "Day10-Performance-Optimization-Scaling/Exercise-Solutions/Exercise91";
    private const string Exercise2Path = "Day10-Performance-Optimization-Scaling/Exercise-Solutions/Exercise92";
    private const string Exercise3Path = "Day10-Performance-Optimization-Scaling/Exercise-Solutions/Exercise93";
    private const string Exercise4Path = "Day10-Performance-Optimization-Scaling/Exercise-Solutions/Exercise94";
    private static readonly TimeSpan ExerciseTimeout = TimeSpan.FromMinutes(3);

    [Test]
    [Description("Exercise 1: Performance Profiling Setup")]
    public async Task Exercise1_PerformanceProfiling_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 1: Performance Profiling Setup");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise1Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 1 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 1 completed successfully");
    }

    [Test]
    [Description("Exercise 2: Resource Optimization Techniques")]
    public async Task Exercise2_ResourceOptimization_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 2: Resource Optimization Techniques");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise2Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 2 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 2 completed successfully");
    }

    [Test]
    [Description("Exercise 3: Horizontal Scaling Configuration")]
    public async Task Exercise3_HorizontalScaling_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 3: Horizontal Scaling Configuration");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise3Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 3 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 3 completed successfully");
    }

    [Test]
    [Description("Exercise 4: Advanced Performance Tuning")]
    public async Task Exercise4_AdvancedPerformanceTuning_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 4: Advanced Performance Tuning");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise4Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 4 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 4 completed successfully");
    }
}