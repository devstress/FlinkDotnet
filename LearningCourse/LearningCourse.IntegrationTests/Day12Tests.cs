using NUnit.Framework;

namespace LearningCourse.IntegrationTests;

/// <summary>
/// Integration tests for Day 12: Disaster Recovery & Multi-Region
///
/// These tests validate exercises for disaster recovery patterns:
/// - Exercise 1: Multi-Region Setup Configuration
/// - Exercise 2: Failover Strategy Implementation
/// - Exercise 3: Data Replication Patterns
/// - Exercise 4: Recovery Testing and Validation
///
/// Implementation: Uses FlinkDotNet with .NET Aspire for infrastructure
/// </summary>
[TestFixture]
[Category("day12-disaster-recovery-multi-region")]
[Category("integration")]
public class Day12Tests : LearningCourseTestBase
{
    private const string Exercise1Path = "Day12-Disaster-Recovery-Multi-Region/Exercise-Solutions/Exercise111";
    private const string Exercise2Path = "Day12-Disaster-Recovery-Multi-Region/Exercise-Solutions/Exercise112";
    private const string Exercise3Path = "Day12-Disaster-Recovery-Multi-Region/Exercise-Solutions/Exercise113";
    private const string Exercise4Path = "Day12-Disaster-Recovery-Multi-Region/Exercise-Solutions/Exercise114";
    private static readonly TimeSpan ExerciseTimeout = TimeSpan.FromMinutes(3);

    [Test]
    [Description("Exercise 1: Multi-Region Setup Configuration")]
    public async Task Exercise1_MultiRegionSetup_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 1: Multi-Region Setup Configuration");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise1Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 1 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 1 completed successfully");
    }

    [Test]
    [Description("Exercise 2: Failover Strategy Implementation")]
    public async Task Exercise2_FailoverStrategy_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 2: Failover Strategy Implementation");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise2Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 2 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 2 completed successfully");
    }

    [Test]
    [Description("Exercise 3: Data Replication Patterns")]
    public async Task Exercise3_DataReplication_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 3: Data Replication Patterns");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise3Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 3 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 3 completed successfully");
    }

    [Test]
    [Description("Exercise 4: Recovery Testing and Validation")]
    public async Task Exercise4_RecoveryTesting_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 4: Recovery Testing and Validation");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise4Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 4 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 4 completed successfully");
    }
}