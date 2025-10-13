using NUnit.Framework;

namespace LearningCourse.IntegrationTests;

/// <summary>
/// Integration tests for Day 7: Advanced Windows & Joins
///
/// These tests validate exercises for advanced windowing and join patterns:
/// - Exercise 1: Time Windows Configuration
/// - Exercise 2: Session Windows Implementation
/// - Exercise 3: Stream Joins Patterns
/// - Exercise 4: Complex Windowing Scenarios
///
/// Implementation: Uses FlinkDotNet with .NET Aspire for infrastructure
/// </summary>
[TestFixture]
[Category("day07-advanced-windows-joins")]
[Category("integration")]
public class Day07Tests : LearningCourseTestBase
{
    private const string Exercise1Path = "Day07-Advanced-Windows-Joins/Exercise-Solutions/Exercise61";
    private const string Exercise2Path = "Day07-Advanced-Windows-Joins/Exercise-Solutions/Exercise62";
    private const string Exercise3Path = "Day07-Advanced-Windows-Joins/Exercise-Solutions/Exercise63";
    private const string Exercise4Path = "Day07-Advanced-Windows-Joins/Exercise-Solutions/Exercise64";
    private static readonly TimeSpan ExerciseTimeout = TimeSpan.FromMinutes(3);

    [Test]
    [Description("Exercise 1: Time Windows Configuration")]
    public async Task Exercise1_TimeWindows_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 1: Time Windows Configuration");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise1Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 1 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 1 completed successfully");
    }

    [Test]
    [Description("Exercise 2: Session Windows Implementation")]
    public async Task Exercise2_SessionWindows_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 2: Session Windows Implementation");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise2Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 2 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 2 completed successfully");
    }

    [Test]
    [Description("Exercise 3: Stream Joins Patterns")]
    public async Task Exercise3_StreamJoins_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 3: Stream Joins Patterns");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise3Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 3 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 3 completed successfully");
    }

    [Test]
    [Description("Exercise 4: Complex Windowing Scenarios")]
    public async Task Exercise4_ComplexWindowing_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 4: Complex Windowing Scenarios");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise4Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 4 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 4 completed successfully");
    }
}