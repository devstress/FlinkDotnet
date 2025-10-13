using NUnit.Framework;

namespace LearningCourse.IntegrationTests;

/// <summary>
/// Integration tests for Day 6: Temporal Workflows
///
/// These tests validate exercises for Temporal workflow patterns:
/// - Exercise 1: Basic Workflow Setup
/// - Exercise 2: Activity Implementation
/// - Exercise 3: Workflow Orchestration
/// - Exercise 4: Advanced Workflow Patterns
///
/// Implementation: Uses FlinkDotNet with .NET Aspire for infrastructure
/// </summary>
[TestFixture]
[Category("day06-temporal-workflows")]
[Category("integration")]
public class Day06Tests : LearningCourseTestBase
{
    private const string Exercise1Path = "Day06-Temporal-Workflows/Exercise-Solutions/Exercise51";
    private const string Exercise2Path = "Day06-Temporal-Workflows/Exercise-Solutions/Exercise52";
    private const string Exercise3Path = "Day06-Temporal-Workflows/Exercise-Solutions/Exercise53";
    private const string Exercise4Path = "Day06-Temporal-Workflows/Exercise-Solutions/Exercise54";
    private static readonly TimeSpan ExerciseTimeout = TimeSpan.FromMinutes(3);

    [Test]
    [Description("Exercise 1: Basic Workflow Setup")]
    public async Task Exercise1_BasicWorkflowSetup_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 1: Basic Workflow Setup");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise1Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 1 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 1 completed successfully");
    }

    [Test]
    [Description("Exercise 2: Activity Implementation")]
    public async Task Exercise2_ActivityImplementation_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 2: Activity Implementation");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise2Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 2 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 2 completed successfully");
    }

    [Test]
    [Description("Exercise 3: Workflow Orchestration")]
    public async Task Exercise3_WorkflowOrchestration_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 3: Workflow Orchestration");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise3Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 3 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 3 completed successfully");
    }

    [Test]
    [Description("Exercise 4: Advanced Workflow Patterns")]
    public async Task Exercise4_AdvancedWorkflowPatterns_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 4: Advanced Workflow Patterns");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise4Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 4 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 4 completed successfully");
    }
}