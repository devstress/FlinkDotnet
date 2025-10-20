using NUnit.Framework;

namespace LearningCourse.IntegrationTests;

/// <summary>
/// Integration tests for Day 11: Security, Privacy & Compliance
///
/// These tests validate exercises for security and compliance patterns:
/// - Exercise 1: Authentication and Authorization Setup
/// - Exercise 2: Data Encryption Implementation
/// - Exercise 3: Privacy Compliance Patterns
/// - Exercise 4: Audit Logging and Monitoring
///
/// Implementation: Uses FlinkDotNet with .NET Aspire for infrastructure
/// </summary>
[TestFixture]
[Category("day11-security-privacy-compliance")]
[Category("integration")]
public class Day11Tests : LearningCourseTestBase
{
    private const string Exercise1Path = "Day11-Security-Privacy-Compliance/Exercise-Solutions/Exercise111";
    private const string Exercise2Path = "Day11-Security-Privacy-Compliance/Exercise-Solutions/Exercise112";
    private const string Exercise3Path = "Day11-Security-Privacy-Compliance/Exercise-Solutions/Exercise113";
    private const string Exercise4Path = "Day11-Security-Privacy-Compliance/Exercise-Solutions/Exercise114";
    private static readonly TimeSpan ExerciseTimeout = TimeSpan.FromSeconds(30);

    [Test]
    [Description("Exercise 1: Authentication and Authorization Setup")]
    public async Task Exercise1_AuthenticationAuthorization_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 1: Authentication and Authorization Setup");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise1Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 1 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 1 completed successfully");
    }

    [Test]
    [Description("Exercise 2: Data Encryption Implementation")]
    public async Task Exercise2_DataEncryption_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 2: Data Encryption Implementation");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise2Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 2 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 2 completed successfully");
    }

    [Test]
    [Description("Exercise 3: Privacy Compliance Patterns")]
    public async Task Exercise3_PrivacyCompliance_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 3: Privacy Compliance Patterns");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise3Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 3 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 3 completed successfully");
    }

    [Test]
    [Description("Exercise 4: Audit Logging and Monitoring")]
    public async Task Exercise4_AuditLoggingMonitoring_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 4: Audit Logging and Monitoring");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise4Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 4 should complete successfully. Exit code: {exitCode}\nError: {error}");
        TestContext.WriteLine("✅ Exercise 4 completed successfully");
    }
}