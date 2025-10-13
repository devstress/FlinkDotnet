using NUnit.Framework;

namespace LearningCourse.IntegrationTests;

/// <summary>
/// Integration tests for Day 8: Complex Logic Stress Testing
///
/// Reference: LocalTesting Stress Test Framework
///
/// These tests validate exercises for stress testing patterns:
/// - Exercise 1: Volume Stress Testing - Million+ message throughput
/// - Exercise 2: Velocity Stress Testing - Burst traffic and latency
/// - Exercise 3: Variety Stress Testing - Complex data scenarios
/// - Exercise 4: Fault Injection Testing - Chaos engineering patterns
///
/// Implementation: Uses FlinkDotNet with .NET Aspire for infrastructure
/// </summary>
[TestFixture]
[Category("day08-stress-testing")]
[Category("integration")]
public class Day08Tests : LearningCourseTestBase
{
    private const string Exercise1Path = "Day08-Stress-Testing/Exercise-Solutions/Exercise71";
    private const string Exercise2Path = "Day08-Stress-Testing/Exercise-Solutions/Exercise72";
    private const string Exercise3Path = "Day08-Stress-Testing/Exercise-Solutions/Exercise73";
    private const string Exercise4Path = "Day08-Stress-Testing/Exercise-Solutions/Exercise74";
    private static readonly TimeSpan ExerciseTimeout = TimeSpan.FromMinutes(5); // Longer timeout for stress tests

    [Test]
    [Description("Exercise 1: Volume Stress Testing - Million+ messages")]
    public async Task Exercise1_VolumeStressTesting_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 1: Volume Stress Testing");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine();
        TestContext.WriteLine("Testing concepts:");
        TestContext.WriteLine("  - High-throughput data processing");
        TestContext.WriteLine("  - Million message stress test");
        TestContext.WriteLine("  - Large state operations");
        TestContext.WriteLine("  - Memory pressure testing");
        TestContext.WriteLine();

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise1Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 1 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 1 completed successfully");
    }

    [Test]
    [Description("Exercise 2: Velocity Stress Testing - Burst traffic")]
    public async Task Exercise2_VelocityStressTesting_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 2: Velocity Stress Testing");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine();
        TestContext.WriteLine("Testing concepts:");
        TestContext.WriteLine("  - Burst traffic simulation");
        TestContext.WriteLine("  - Variable rate testing");
        TestContext.WriteLine("  - Latency benchmark");
        TestContext.WriteLine("  - Real-time processing under load");
        TestContext.WriteLine();

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise2Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 2 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 2 completed successfully");
    }

    [Test]
    [Description("Exercise 3: Variety Stress Testing - Complex data scenarios")]
    public async Task Exercise3_VarietyStressTesting_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 3: Variety Stress Testing");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine();
        TestContext.WriteLine("Testing concepts:");
        TestContext.WriteLine("  - Schema evolution testing");
        TestContext.WriteLine("  - Data quality stress test");
        TestContext.WriteLine("  - Complex transformation load");
        TestContext.WriteLine("  - Diverse data type handling");
        TestContext.WriteLine();

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise3Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 3 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 3 completed successfully");
    }

    [Test]
    [Description("Exercise 4: Fault Injection Testing - Chaos engineering")]
    public async Task Exercise4_FaultInjectionTesting_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 4: Fault Injection Testing");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine();
        TestContext.WriteLine("Testing concepts:");
        TestContext.WriteLine("  - Network fault injection");
        TestContext.WriteLine("  - Service failure simulation");
        TestContext.WriteLine("  - Data corruption testing");
        TestContext.WriteLine("  - Chaos engineering patterns");
        TestContext.WriteLine();

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise4Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 4 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 4 completed successfully");
    }
}