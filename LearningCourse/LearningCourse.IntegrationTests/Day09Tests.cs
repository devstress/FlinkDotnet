using NUnit.Framework;

namespace LearningCourse.IntegrationTests;

/// <summary>
/// Integration tests for Day 9: Exactly-Once Semantics
///
/// These tests validate exercises for exactly-once processing patterns:
/// - Exercise 1: Idempotent Processing Setup
/// - Exercise 2: Checkpoint Configuration
/// - Exercise 3: State Recovery Patterns
/// - Exercise 4: End-to-End Exactly-Once Guarantees
///
/// Implementation: Uses FlinkDotNet with .NET Aspire for infrastructure
/// </summary>
[TestFixture]
[Category("day09-exactly-once-semantics")]
[Category("integration")]
public class Day09Tests : LearningCourseTestBase
{
    private const string Exercise1Path = "Day09-Exactly-Once-Semantics/Exercise-Solutions/Exercise91";
    private const string Exercise2Path = "Day09-Exactly-Once-Semantics/Exercise-Solutions/Exercise92";
    private const string Exercise3Path = "Day09-Exactly-Once-Semantics/Exercise-Solutions/Exercise93";
    private const string Exercise4Path = "Day09-Exactly-Once-Semantics/Exercise-Solutions/Exercise94";
    private static readonly TimeSpan ExerciseTimeout = TimeSpan.FromMinutes(3);

    [Test]
    [Description("Exercise 1: Idempotent Processing Setup")]
    public async Task Exercise1_IdempotentProcessing_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 1: Idempotent Processing Setup");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise1Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 1 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 1 completed successfully");
    }

    [Test]
    [Description("Exercise 2: Checkpoint Configuration")]
    public async Task Exercise2_CheckpointConfiguration_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 2: Checkpoint Configuration");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise2Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 2 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 2 completed successfully");
    }

    [Test]
    [Description("Exercise 3: State Recovery Patterns")]
    public async Task Exercise3_StateRecovery_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 3: State Recovery Patterns");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise3Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 3 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 3 completed successfully");
    }

    [Test]
    [Description("Exercise 4: End-to-End Exactly-Once Guarantees")]
    public async Task Exercise4_EndToEndExactlyOnce_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 4: End-to-End Exactly-Once Guarantees");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise4Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 4 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 4 completed successfully");
    }
}