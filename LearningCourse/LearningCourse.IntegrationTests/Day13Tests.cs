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
    private const string Exercise1Path = "Day13-Advanced-Streaming-Patterns/Exercise-Solutions/Exercise131";
    private const string Exercise2Path = "Day13-Advanced-Streaming-Patterns/Exercise-Solutions/Exercise132";
    private const string Exercise3Path = "Day13-Advanced-Streaming-Patterns/Exercise-Solutions/Exercise133";
    private const string Exercise4Path = "Day13-Advanced-Streaming-Patterns/Exercise-Solutions/Exercise134";
    private static readonly TimeSpan ExerciseTimeout = TimeSpan.FromSeconds(30);

    [Test]
    [Description("Exercise 1: Event Sourcing Pattern Implementation")]
    public async Task Exercise1_EventSourcing_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 1: Event Sourcing Pattern Implementation");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise1Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 1 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 1 (Event Sourcing) completed successfully");
    }

    [Test]
    [Description("Exercise 2: CQRS Pattern Implementation")]
    public async Task Exercise2_CQRS_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 2: CQRS Pattern Implementation");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise2Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 2 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 2 (CQRS) completed successfully");
    }

    [Test]
    [Description("Exercise 3: Saga Pattern Implementation")]
    public async Task Exercise3_SagaPattern_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 3: Saga Pattern Implementation");
        TestContext.WriteLine("================================================================================");

        // Note: This exercise submits 5 Flink jobs sequentially which takes ~60s
        // Using simple timeout instead of progress monitoring due to long job submission phase
        TestContext.WriteLine("Note: Exercise submits 5 Flink jobs sequentially (Saga workflow)");
        var (exitCode, output, error) = await ExecuteExerciseAsync(
            Exercise3Path,
            Array.Empty<string>(),
            TimeSpan.FromMinutes(3)); // 3 minutes to handle 5 sequential job submissions + execution

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 3 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 3 (Saga Pattern) completed successfully");
    }

    [Test]
    [Description("Exercise 4: Complex Event Processing (CEP) Pattern Implementation")]
    public async Task Exercise4_CEP_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 4: Complex Event Processing (CEP) Pattern Implementation");
        TestContext.WriteLine("================================================================================");

        // Note: This exercise submits 5+ Flink jobs sequentially which takes ~60s
        // Using simple timeout instead of progress monitoring due to long job submission phase
        TestContext.WriteLine("Note: Exercise submits 5+ Flink jobs sequentially (CEP patterns)");
        var (exitCode, output, error) = await ExecuteExerciseAsync(
            Exercise4Path,
            Array.Empty<string>(),
            TimeSpan.FromMinutes(3)); // 3 minutes to handle 5+ sequential job submissions + execution

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 4 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 4 (CEP) completed successfully");
    }
}