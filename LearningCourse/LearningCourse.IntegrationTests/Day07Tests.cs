using NUnit.Framework;

namespace LearningCourse.IntegrationTests;

/// <summary>
/// Integration tests for Day 07: Advanced Windows & Joins
/// Tests all exercises using real infrastructure (Kafka + Flink)
/// 
/// All Day07 exercises already use real infrastructure - no conversion needed!
/// This test file validates the existing real infrastructure implementation.
/// </summary>
[TestFixture]
[Category("day07-advanced-windows-joins")]
[Category("integration")]
public class Day07Tests : LearningCourseTestBase
{
    private const string Exercise1Path = "Day07-Advanced-Windows-Joins/Exercise-Solutions/Exercise71";
    private const string Exercise2Path = "Day07-Advanced-Windows-Joins/Exercise-Solutions/Exercise72";
    private const string Exercise3Path = "Day07-Advanced-Windows-Joins/Exercise-Solutions/Exercise73";
    private const string Exercise4Path = "Day07-Advanced-Windows-Joins/Exercise-Solutions/Exercise74";
    private static readonly TimeSpan ExerciseTimeout = TimeSpan.FromSeconds(30);

    [Test]
    [Description("Exercise 71: E-commerce Order Enrichment - Multi-stream temporal joins")]
    public async Task Exercise71_EcommerceOrderEnrichment_ShouldEnrichOrdersSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 7.1: E-commerce Order Enrichment");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine();
        TestContext.WriteLine("Testing concepts:");
        TestContext.WriteLine("  - Multi-stream temporal joins");
        TestContext.WriteLine("  - Order enrichment pipeline");
        TestContext.WriteLine("  - Real-time data correlation");
        TestContext.WriteLine("  - Complex join patterns");
        TestContext.WriteLine();

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise1Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 7.1 should complete successfully. Exit code: {exitCode}\nError: {error}");
        Assert.That(output, Does.Contain("Multi-stream temporal joins"), "Should demonstrate multi-stream joins");
        Assert.That(output, Does.Contain("[SUCCESS]"), "Should show success markers");
        TestContext.WriteLine("✅ Exercise 7.1 completed successfully");
    }

    [Test]
    [Description("Exercise 72: Financial Fraud Detection Windows - Tumbling windows for velocity checks")]
    public async Task Exercise72_FraudDetectionWindows_ShouldDetectFraudPatternsSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 7.2: Financial Fraud Detection Windows");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine();
        TestContext.WriteLine("Testing concepts:");
        TestContext.WriteLine("  - Tumbling windows for velocity monitoring");
        TestContext.WriteLine("  - Fraud pattern detection");
        TestContext.WriteLine("  - Window-based transaction analysis");
        TestContext.WriteLine("  - Real-time alerting");
        TestContext.WriteLine();

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise2Path, Array.Empty<string>(), ExerciseTimeout);

        TestContext.WriteLine();
        TestContext.WriteLine("--------------------------------------------------------------------------------");
        TestContext.WriteLine("Test Validation");
        TestContext.WriteLine("--------------------------------------------------------------------------------");

        var validationChecks = BuildExercise72ValidationChecks(output);
        ValidateExerciseResults(validationChecks, output, error, "Exercise 7.2");

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 7.2 should complete successfully. Exit code: {exitCode}\nError: {error}");
        
        TestContext.WriteLine();
        TestContext.WriteLine("✅ Exercise 7.2 completed successfully");
        TestContext.WriteLine("================================================================================");
    }

    [Test]
    [Description("Exercise 73: IoT Sensor Data Correlation - Multi-sensor pattern detection")]
    public async Task Exercise73_IoTSensorCorrelation_ShouldCorrelateSensorsSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 7.3: IoT Sensor Data Correlation");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine();
        TestContext.WriteLine("Testing concepts:");
        TestContext.WriteLine("  - Multi-sensor data correlation");
        TestContext.WriteLine("  - Time-bounded joins");
        TestContext.WriteLine("  - Anomaly detection");
        TestContext.WriteLine("  - Sensor fusion patterns");
        TestContext.WriteLine();

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise3Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 7.3 should complete successfully. Exit code: {exitCode}\nError: {error}");
        Assert.That(output, Does.Contain("Multi-sensor").IgnoreCase, "Should demonstrate multi-sensor correlation");
        Assert.That(output, Does.Contain("[SUCCESS]"), "Should show success markers");
        TestContext.WriteLine("✅ Exercise 7.3 completed successfully");
    }

    [Test]
    [Description("Exercise 74: Advanced Windowing Optimization - High-throughput windowing")]
    public async Task Exercise74_WindowingOptimization_ShouldOptimizeWindowsSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 7.4: Advanced Windowing Optimization");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine();
        TestContext.WriteLine("Testing concepts:");
        TestContext.WriteLine("  - High-throughput windowing optimization");
        TestContext.WriteLine("  - Memory-efficient state management");
        TestContext.WriteLine("  - Performance tuning");
        TestContext.WriteLine("  - Batch processing optimization");
        TestContext.WriteLine();

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise4Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 7.4 should complete successfully. Exit code: {exitCode}\nError: {error}");
        Assert.That(output, Does.Contain("windowing").IgnoreCase, "Should demonstrate windowing optimization");
        Assert.That(output, Does.Contain("[SUCCESS]"), "Should show success markers");
        TestContext.WriteLine("✅ Exercise 7.4 completed successfully");
    }

    private static Dictionary<string, (bool result, string failureMessage)> BuildExercise72ValidationChecks(string output)
    {
        return new Dictionary<string, (bool result, string failureMessage)>
        {
            ["Infrastructure Ready"] = (
                output.Contains("Kafka is ready", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Flink cluster is healthy", StringComparison.OrdinalIgnoreCase),
                "Infrastructure validation not found"
            ),
            ["Kafka Topics Created"] = (
                output.Contains("Topics created", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Topics already exist", StringComparison.OrdinalIgnoreCase),
                "Kafka topic creation not found"
            ),
            ["Flink Job Submitted"] = (
                output.Contains("Flink", StringComparison.OrdinalIgnoreCase) &&
                output.Contains("job submitted", StringComparison.OrdinalIgnoreCase),
                "Flink job submission not found"
            ),
            ["Transactions Produced"] = (
                output.Contains("Producing", StringComparison.OrdinalIgnoreCase) &&
                output.Contains("transactions", StringComparison.OrdinalIgnoreCase),
                "Transaction production not found"
            ),
            ["Windowing Pattern"] = (
                output.Contains("tumbling", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("window", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("velocity", StringComparison.OrdinalIgnoreCase),
                "Windowing pattern not demonstrated"
            ),
            ["Fraud Alerts Generated"] = (
                output.Contains("fraud alerts", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("ALERT", StringComparison.OrdinalIgnoreCase),
                "Fraud alerts not found"
            ),
            ["Improved State Management"] = (
                !output.Contains("static dictionary", StringComparison.OrdinalIgnoreCase) &&
                !output.Contains("WARNING: Static state", StringComparison.OrdinalIgnoreCase),
                "NOTICE: Exercise uses simulation pending FlinkDotNet windowing API (acceptable for now)"
            ),
            ["Execution Completed"] = (
                output.Contains("COMPLETED successfully", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("SUCCESS", StringComparison.OrdinalIgnoreCase),
                "Exercise did not complete successfully"
            )
        };
    }

    private static void ValidateExerciseResults(
        Dictionary<string, (bool result, string failureMessage)> validationChecks,
        string output,
        string error,
        string exerciseName)
    {
        var validationFailures = new List<string>();

        foreach (var (checkName, (result, failureMessage)) in validationChecks)
        {
            TestContext.WriteLine($"[CHECK] {checkName}: {(result ? "✅ PASS" : "❌ FAIL")}");
            if (!result)
            {
                validationFailures.Add($"{checkName}: {failureMessage}");
            }
        }

        if (validationFailures.Any())
        {
            TestContext.WriteLine();
            TestContext.WriteLine("❌ Validation failures detected:");
            foreach (var failure in validationFailures)
            {
                TestContext.WriteLine($"   - {failure}");
            }
            
            PrintDebugOutput(output, error);
            Assert.Fail($"{exerciseName} validation failed. See output above for details.");
        }
    }

    private static void PrintDebugOutput(string output, string error)
    {
        TestContext.WriteLine();
        TestContext.WriteLine("Full Output:");
        TestContext.WriteLine("--------------------------------------------------------------------------------");
        TestContext.WriteLine(output);
        TestContext.WriteLine("--------------------------------------------------------------------------------");

        if (!string.IsNullOrEmpty(error))
        {
            TestContext.WriteLine();
            TestContext.WriteLine("Error Output:");
            TestContext.WriteLine("--------------------------------------------------------------------------------");
            TestContext.WriteLine(error);
            TestContext.WriteLine("--------------------------------------------------------------------------------");
        }
    }
}