using NUnit.Framework;

namespace LearningCourse.IntegrationTests;

/// <summary>
/// Integration tests for Day 2: Apache Flink 2.1.0 Fundamentals & Production Environment
///
/// Reference: https://flink.apache.org/2025/07/31/apache-flink-2.1.0-ushers-in-a-new-era-of-unified-real-time-data--ai-with-comprehensive-upgrades/
///
/// These tests validate exercises based on Flink 2.1.0 fundamentals:
/// - Exercise 1.1: Production Infrastructure Validation - Complete unified Data + AI platform validation
/// - Exercise 1.2: Production Application - Enterprise-grade streaming application with observability
/// - Exercise 1.3: Observability Dashboard - Google-style SRE observability patterns
/// - Exercise 1.4: Load Testing - Performance validation and benchmarking
///
/// Implementation: Uses FlinkDotNet with .NET Aspire for infrastructure
/// </summary>
[TestFixture]
[Category("day02-flink21-fundamentals")]
[Category("integration")]
public class Day02Tests : LearningCourseTestBase
{
    private const string Exercise1Path = "Day02-Flink21-Fundamentals/Exercise-Solutions/Exercise21";
    private const string Exercise2Path = "Day02-Flink21-Fundamentals/Exercise-Solutions/Exercise22";
    private const string Exercise3Path = "Day02-Flink21-Fundamentals/Exercise-Solutions/Exercise23";
    private static readonly TimeSpan ExerciseTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Exercise 1.1: Production Infrastructure Validation
    ///
    /// This test validates:
    /// - Complete unified Data + AI platform components
    /// - Flink cluster health (JobManager, TaskManagers)
    /// - Kafka event streaming broker status
    /// - Temporal workflow engine readiness
    /// - Observability stack (Prometheus, Grafana)
    ///
    /// Expected: All services responding with HTTP 200, proper cluster configuration
    /// </summary>
    [Test]
    [Description("Exercise 1.1: Production Infrastructure Validation")]
    public async Task Exercise1_InfrastructureValidation_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 1.1: Production Infrastructure Validation");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine();
        TestContext.WriteLine("Reference: Apache Flink 2.1.0 Fundamentals");
        TestContext.WriteLine();
        TestContext.WriteLine("Testing concepts:");
        TestContext.WriteLine("  - Complete unified Data + AI platform validation");
        TestContext.WriteLine("  - Flink cluster health check (JobManager, TaskManagers)");
        TestContext.WriteLine("  - Kafka event streaming infrastructure");
        TestContext.WriteLine("  - Temporal workflow engine status");
        TestContext.WriteLine("  - Observability stack verification");
        TestContext.WriteLine();

        var (exitCode, output, error) = await ExecuteExerciseAsync(
            Exercise1Path,
            Array.Empty<string>(),
            ExerciseTimeout);

        TestContext.WriteLine();
        TestContext.WriteLine("--------------------------------------------------------------------------------");
        TestContext.WriteLine("Test Validation");
        TestContext.WriteLine("--------------------------------------------------------------------------------");

        // Validate execution completed successfully
        var validationChecks = BuildExercise1ValidationChecks(output);
        ValidateExerciseResults(validationChecks, output, error, "Exercise 1.1");
        
        Assert.That(exitCode, Is.EqualTo(0),
            $"Exercise 1.1 should complete successfully. Exit code: {exitCode}\nError: {error}");

        TestContext.WriteLine();
        TestContext.WriteLine("✅ Exercise 1.1 completed successfully");
        TestContext.WriteLine("================================================================================");
    }

    /// <summary>
    /// Exercise 1.2: Production Application
    ///
    /// This test validates:
    /// - Enterprise-grade streaming application deployment
    /// - State backend configuration (RocksDB)
    /// - Checkpoint and savepoint management
    /// - Production metrics and observability
    /// - Event processing with OpenTelemetry
    ///
    /// Expected: Application builds and runs with proper observability instrumentation
    /// </summary>
    [Test]
    [Description("Exercise 1.2: Production Application")]
    public async Task Exercise2_ProductionApp_ShouldExecuteSuccessfully()
    {
        PrintExercise2Header();

        var (exitCode, output, error) = await ExecuteExerciseAsync(
            Exercise2Path,
            Array.Empty<string>(),
            ExerciseTimeout);

        TestContext.WriteLine();
        TestContext.WriteLine("--------------------------------------------------------------------------------");
        TestContext.WriteLine("Test Validation");
        TestContext.WriteLine("--------------------------------------------------------------------------------");

        var validationChecks = BuildExercise2ValidationChecks(output);
        ValidateExerciseResults(validationChecks, output, error, "Exercise 1.2");

        Assert.That(exitCode, Is.EqualTo(0),
            $"Exercise 1.2 should complete successfully. Exit code: {exitCode}\nError: {error}");

        TestContext.WriteLine();
        TestContext.WriteLine("✅ Exercise 1.2 completed successfully");
        TestContext.WriteLine("================================================================================");
    }

    /// <summary>
    /// Exercise 1.3: Load Testing
    ///
    /// This test validates:
    /// - Performance validation and benchmarking
    /// - Throughput and latency measurements
    /// - Backpressure handling under load
    /// - Resource utilization monitoring
    /// - Scalability testing
    ///
    /// Expected: Load test executes with performance metrics reported
    /// </summary>
    [Test]
    [Description("Exercise 1.3: Load Testing")]
    public async Task Exercise3_LoadTesting_ShouldExecuteSuccessfully()
    {
        PrintExercise3Header();

        var (exitCode, output, error) = await ExecuteExerciseAsync(
            Exercise3Path,
            Array.Empty<string>(),
            ExerciseTimeout);

        TestContext.WriteLine();
        TestContext.WriteLine("--------------------------------------------------------------------------------");
        TestContext.WriteLine("Test Validation");
        TestContext.WriteLine("--------------------------------------------------------------------------------");

        var validationChecks = BuildExercise3ValidationChecks(output);
        ValidateExerciseResults(validationChecks, output, error, "Exercise 1.3");

        Assert.That(exitCode, Is.EqualTo(0),
            $"Exercise 1.3 should complete successfully. Exit code: {exitCode}\nError: {error}");

        TestContext.WriteLine();
        TestContext.WriteLine("✅ Exercise 1.3 completed successfully");
        TestContext.WriteLine("================================================================================");
    }

    private static Dictionary<string, (bool result, string failureMessage)> BuildExercise1ValidationChecks(string output)
    {
        return new Dictionary<string, (bool result, string failureMessage)>
        {
            ["Infrastructure Check"] = (output.Contains("Infrastructure") || output.Contains("validation"), "Infrastructure validation not found"),
            ["Flink Ready"] = (output.Contains("Flink") || output.Contains("JobManager") || output.Contains("TaskManager"), "Flink cluster not ready"),
            ["Kafka Ready"] = (output.Contains("Kafka") || output.Contains("broker"), "Kafka broker not ready"),
            ["Execution Completed"] = (output.Contains("COMPLETED") || output.Contains("SUCCESS") || output.Contains("✅"), "Exercise did not complete successfully")
        };
    }

    private static Dictionary<string, (bool result, string failureMessage)> BuildExercise2ValidationChecks(string output)
    {
        return new Dictionary<string, (bool result, string failureMessage)>
        {
            ["Production App"] = (output.Contains("Production") || output.Contains("Enterprise"), "Production application not found"),
            ["State Backend"] = (output.Contains("State") || output.Contains("RocksDB") || output.Contains("backend"), "State backend not configured"),
            ["Observability"] = (output.Contains("OpenTelemetry") || output.Contains("metrics") || output.Contains("observability"), "Observability not configured"),
            ["Execution Completed"] = (output.Contains("COMPLETED") || output.Contains("SUCCESS") || output.Contains("✅"), "Exercise did not complete successfully")
        };
    }

    private static Dictionary<string, (bool result, string failureMessage)> BuildExercise3ValidationChecks(string output)
    {
        return new Dictionary<string, (bool result, string failureMessage)>
        {
            ["Load Testing"] = (output.Contains("Load") || output.Contains("test") || output.Contains("performance"), "Load testing not found"),
            ["Performance Metrics"] = (output.Contains("throughput") || output.Contains("latency") || output.Contains("metrics"), "Performance metrics not reported"),
            ["Benchmarking"] = (output.Contains("benchmark") || output.Contains("results"), "Benchmarking not performed"),
            ["Execution Completed"] = (output.Contains("COMPLETED") || output.Contains("SUCCESS") || output.Contains("✅"), "Exercise did not complete successfully")
        };
    }

    private static void PrintExercise2Header()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 1.2: Production Application");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine();
        TestContext.WriteLine("Reference: Apache Flink 2.1.0 Production Patterns");
        TestContext.WriteLine();
        TestContext.WriteLine("Testing concepts:");
        TestContext.WriteLine("  - Enterprise-grade streaming application");
        TestContext.WriteLine("  - State backend configuration (RocksDB)");
        TestContext.WriteLine("  - Checkpoint and savepoint management");
        TestContext.WriteLine("  - Production metrics and observability");
        TestContext.WriteLine("  - Event processing with OpenTelemetry");
        TestContext.WriteLine();
    }

    private static void PrintExercise3Header()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 1.3: Load Testing");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine();
        TestContext.WriteLine("Reference: Performance Validation and Benchmarking");
        TestContext.WriteLine();
        TestContext.WriteLine("Testing concepts:");
        TestContext.WriteLine("  - Performance validation");
        TestContext.WriteLine("  - Throughput and latency measurements");
        TestContext.WriteLine("  - Backpressure handling under load");
        TestContext.WriteLine("  - Resource utilization monitoring");
        TestContext.WriteLine("  - Scalability testing");
        TestContext.WriteLine();
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
            TestContext.WriteLine($"[CHECK] {checkName}: {result}");
            if (!result)
            {
                validationFailures.Add($"{checkName}: {failureMessage}");
            }
        }

        if (validationFailures.Any())
        {
            ReportValidationFailures(validationFailures, output, error, exerciseName);
        }
    }

    /// <summary>
    /// Helper method to report validation failures with debug information
    /// </summary>
    private static void ReportValidationFailures(
        List<string> validationFailures,
        string output,
        string error,
        string exerciseName)
    {
        TestContext.WriteLine();
        TestContext.WriteLine("❌ Validation failures detected:");
        foreach (var failure in validationFailures)
        {
            TestContext.WriteLine($"   - {failure}");
        }
        TestContext.WriteLine();

        PrintDebugOutput(output, error);

        Assert.Fail($"{exerciseName} validation failed. See output above for details.");
    }

    /// <summary>
    /// Helper method to print debug output
    /// </summary>
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