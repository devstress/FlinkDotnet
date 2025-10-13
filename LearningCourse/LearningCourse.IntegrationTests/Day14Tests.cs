using NUnit.Framework;

namespace LearningCourse.IntegrationTests;

/// <summary>
/// Integration tests for Day 14: Advanced Testing & Chaos Engineering
///
/// These tests validate exercises for advanced testing patterns:
/// - Exercise 1: Property-Based Testing
/// - Exercise 2: Mutation Testing
/// - Exercise 3: Fault Injection Testing
/// - Exercise 4: Chaos Engineering Experiments
///
/// Implementation: Uses FlinkDotNet with .NET Aspire for infrastructure
/// </summary>
[TestFixture]
[Category("day14-advanced-testing-chaos-engineering")]
[Category("integration")]
public class Day14Tests : LearningCourseTestBase
{
    private const string Exercise1Path = "Day14-Advanced-Testing-Chaos-Engineering/Exercise-Solutions/Exercise131";
    private const string Exercise2Path = "Day14-Advanced-Testing-Chaos-Engineering/Exercise-Solutions/Exercise132";
    private const string Exercise3Path = "Day14-Advanced-Testing-Chaos-Engineering/Exercise-Solutions/Exercise133";
    private const string Exercise4Path = "Day14-Advanced-Testing-Chaos-Engineering/Exercise-Solutions/Exercise134";
    private static readonly TimeSpan ExerciseTimeout = TimeSpan.FromMinutes(3);

    [Test]
    [Description("Exercise 1: Property-Based Testing")]
    public async Task Exercise1_PropertyBasedTesting_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 1: Property-Based Testing");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise1Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 1 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 1 completed successfully");
    }

    [Test]
    [Description("Exercise 2: Mutation Testing")]
    public async Task Exercise2_MutationTesting_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 2: Mutation Testing");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise2Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 2 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 2 completed successfully");
    }

    [Test]
    [Description("Exercise 3: Fault Injection Testing")]
    public async Task Exercise3_FaultInjectionTesting_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 3: Fault Injection Testing");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise3Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 3 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 3 completed successfully");
    }

    [Test]
    [Description("Exercise 4: Chaos Engineering Experiments")]
    public async Task Exercise4_ChaosEngineeringExperiments_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 4: Chaos Engineering Experiments");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise4Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 4 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 4 completed successfully");
    }
}