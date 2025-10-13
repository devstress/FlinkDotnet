using NUnit.Framework;

namespace LearningCourse.IntegrationTests;

/// <summary>
/// Integration tests for Day 13: Advanced Streaming Patterns
///
/// These tests validate exercises for advanced streaming patterns:
/// - Exercise 1: Pattern Matching Implementation
/// - Exercise 2: Event Correlation Strategies
/// - Exercise 3: Complex Event Processing (CEP)
/// - Exercise 4: Stream Pattern Libraries
///
/// Implementation: Uses FlinkDotNet with .NET Aspire for infrastructure
/// </summary>
[TestFixture]
[Category("day13-advanced-streaming-patterns")]
[Category("integration")]
public class Day13Tests : LearningCourseTestBase
{
    private const string Exercise1Path = "Day13-Advanced-Streaming-Patterns/Exercise-Solutions/Exercise121";
    private const string Exercise2Path = "Day13-Advanced-Streaming-Patterns/Exercise-Solutions/Exercise122";
    private const string Exercise3Path = "Day13-Advanced-Streaming-Patterns/Exercise-Solutions/Exercise123";
    private const string Exercise4Path = "Day13-Advanced-Streaming-Patterns/Exercise-Solutions/Exercise124";
    private static readonly TimeSpan ExerciseTimeout = TimeSpan.FromMinutes(3);

    [Test]
    [Description("Exercise 1: Pattern Matching Implementation")]
    public async Task Exercise1_PatternMatching_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 1: Pattern Matching Implementation");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise1Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 1 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 1 completed successfully");
    }

    [Test]
    [Description("Exercise 2: Event Correlation Strategies")]
    public async Task Exercise2_EventCorrelation_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 2: Event Correlation Strategies");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise2Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 2 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 2 completed successfully");
    }

    [Test]
    [Description("Exercise 3: Complex Event Processing (CEP)")]
    public async Task Exercise3_ComplexEventProcessing_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 3: Complex Event Processing (CEP)");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise3Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 3 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 3 completed successfully");
    }

    [Test]
    [Description("Exercise 4: Stream Pattern Libraries")]
    public async Task Exercise4_StreamPatternLibraries_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 4: Stream Pattern Libraries");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise4Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 4 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 4 completed successfully");
    }
}