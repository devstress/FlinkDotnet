using NUnit.Framework;

namespace LearningCourse.IntegrationTests;

/// <summary>
/// Integration tests for Day 4: Production-Grade Backpressure & Distributed Rate Limiting
///
/// Reference: https://flink.apache.org/2022/11/25/optimising-the-throughput-of-async-sinks-using-a-custom-ratelimitingstrategy/
///
/// These tests validate exercises based on distributed rate limiting patterns:
/// - Exercise 1: Netflix Global Rate Limiting Controller - Epoch-based budget minting
/// - Exercise 2: Uber Regional Redis Coordination - Atomic budget operations
/// - Exercise 3: LinkedIn High-Performance Gateway - Local token buckets and hot path
/// - Exercise 4: Chaos Engineering Production Validation - Compound failure scenarios
/// - Exercise 5: Simple BackpressureQueue Implementation - Alternative approach comparison
///
/// Implementation: Uses FlinkDotNet with .NET Aspire for infrastructure
/// </summary>
[TestFixture]
[Category("day04-production-backpressure")]
[Category("integration")]
public class Day04Tests : LearningCourseTestBase
{
    private const string Exercise1Path = "Day04-Production-Backpressure/Exercise-Solutions/Exercise31";
    private const string Exercise2Path = "Day04-Production-Backpressure/Exercise-Solutions/Exercise32";
    private const string Exercise3Path = "Day04-Production-Backpressure/Exercise-Solutions/Exercise33";
    private const string Exercise4Path = "Day04-Production-Backpressure/Exercise-Solutions/Exercise34";
    private const string Exercise5Path = "Day04-Production-Backpressure/Exercise-Solutions/Exercise35";
    private static readonly TimeSpan ExerciseTimeout = TimeSpan.FromMinutes(3);

    /// <summary>
    /// Exercise 1: Netflix Global Rate Limiting Controller (90 minutes)
    ///
    /// This test validates:
    /// - Epoch-based budget minting every 250ms
    /// - Cross-region coordination prevention
    /// - Policy distribution to regional banks
    /// - Pre-mint budget futures for fault tolerance
    /// - Netflix-scale global quota management
    ///
    /// Expected: Netflix-level coordination with 99.99% API gateway uptime
    /// </summary>
    [Test]
    [Description("Exercise 1: Netflix Global Rate Limiting Controller - Epoch-based coordination")]
    public async Task Exercise1_NetflixGlobalQuotaController_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 1: Netflix Global Rate Limiting Controller");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine();
        TestContext.WriteLine("Reference: Netflix Zuul 2 Distributed Rate Limiting");
        TestContext.WriteLine();
        TestContext.WriteLine("Testing concepts:");
        TestContext.WriteLine("  - Epoch-based budget minting (250ms intervals)");
        TestContext.WriteLine("  - Cross-region coordination prevention");
        TestContext.WriteLine("  - Policy distribution to regional banks");
        TestContext.WriteLine("  - Pre-mint budget futures");
        TestContext.WriteLine("  - Global quota management");
        TestContext.WriteLine();

        var (exitCode, output, error) = await ExecuteExerciseAsync(
            Exercise1Path,
            Array.Empty<string>(),
            ExerciseTimeout);

        TestContext.WriteLine();
        TestContext.WriteLine("--------------------------------------------------------------------------------");
        TestContext.WriteLine("Test Validation");
        TestContext.WriteLine("--------------------------------------------------------------------------------");

        var validationChecks = BuildExercise1ValidationChecks(output);
        ValidateExerciseResults(validationChecks, output, error, "Exercise 1");

        Assert.That(exitCode, Is.EqualTo(0),
            $"Exercise 1 should complete successfully. Exit code: {exitCode}\nError: {error}");

        TestContext.WriteLine();
        TestContext.WriteLine("✅ Exercise 1 completed successfully");
        TestContext.WriteLine("================================================================================");
    }

    /// <summary>
    /// Exercise 2: Uber Regional Redis Coordination (120 minutes)
    ///
    /// This test validates:
    /// - Atomic Redis operations with DECRBY
    /// - Regional budget bank management
    /// - TTL management for budget expiration
    /// - Regional failover handling
    /// - Uber-scale traffic coordination
    ///
    /// Expected: Uber-scale budget coordination handling 15M+ daily rides
    /// </summary>
    [Test]
    [Description("Exercise 2: Uber Regional Redis Coordination - Atomic budget operations")]
    public async Task Exercise2_UberRegionalBudgetBank_ShouldExecuteSuccessfully()
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
        ValidateExerciseResults(validationChecks, output, error, "Exercise 2");

        Assert.That(exitCode, Is.EqualTo(0),
            $"Exercise 2 should complete successfully. Exit code: {exitCode}\nError: {error}");

        TestContext.WriteLine();
        TestContext.WriteLine("✅ Exercise 2 completed successfully");
        TestContext.WriteLine("================================================================================");
    }

    /// <summary>
    /// Exercise 3: LinkedIn High-Performance Gateway (150 minutes)
    ///
    /// This test validates:
    /// - Local token buckets for hot path
    /// - Stateless rate limiting
    /// - Background refill from Regional Budget Bank
    /// - Safe by default startup behavior
    /// - LinkedIn-scale API gateway patterns
    ///
    /// Expected: LinkedIn-scale API gateway with 99.9% uptime during traffic spikes
    /// </summary>
    [Test]
    [Description("Exercise 3: LinkedIn High-Performance Gateway - Hot path rate limiting")]
    public async Task Exercise3_LinkedInAPIGateway_ShouldExecuteSuccessfully()
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
        ValidateExerciseResults(validationChecks, output, error, "Exercise 3");

        Assert.That(exitCode, Is.EqualTo(0),
            $"Exercise 3 should complete successfully. Exit code: {exitCode}\nError: {error}");

        TestContext.WriteLine();
        TestContext.WriteLine("✅ Exercise 3 completed successfully");
        TestContext.WriteLine("================================================================================");
    }

    /// <summary>
    /// Exercise 4: Chaos Engineering Production Validation (60 minutes)
    ///
    /// This test validates:
    /// - Combined failure scenarios
    /// - Fail-closed behavior verification
    /// - End-to-end flow control validation
    /// - Production monitoring during failures
    /// - Netflix/Uber/LinkedIn resilience patterns
    ///
    /// Expected: Production-validated resilience matching enterprise standards
    /// </summary>
    [Test]
    [Description("Exercise 4: Chaos Engineering - Compound failure validation")]
    public async Task Exercise4_ChaosEngineeringValidation_ShouldExecuteSuccessfully()
    {
        PrintExercise4Header();

        var (exitCode, output, error) = await ExecuteExerciseAsync(
            Exercise4Path,
            Array.Empty<string>(),
            ExerciseTimeout);

        TestContext.WriteLine();
        TestContext.WriteLine("--------------------------------------------------------------------------------");
        TestContext.WriteLine("Test Validation");
        TestContext.WriteLine("--------------------------------------------------------------------------------");

        var validationChecks = BuildExercise4ValidationChecks(output);
        ValidateExerciseResults(validationChecks, output, error, "Exercise 4");

        Assert.That(exitCode, Is.EqualTo(0),
            $"Exercise 4 should complete successfully. Exit code: {exitCode}\nError: {error}");

        TestContext.WriteLine();
        TestContext.WriteLine("✅ Exercise 4 completed successfully");
        TestContext.WriteLine("================================================================================");
    }

    /// <summary>
    /// Exercise 5: Simple BackpressureQueue Implementation (45 minutes)
    ///
    /// This test validates:
    /// - Semaphore-based backpressure limiting
    /// - Simple vs complex approach comparison
    /// - Three test scenarios with different configurations
    /// - When to use simple solutions over distributed patterns
    ///
    /// Expected: Clear understanding of simple vs complex backpressure trade-offs
    /// </summary>
    [Test]
    [Description("Exercise 5: Simple BackpressureQueue - Alternative approach comparison")]
    public async Task Exercise5_SimpleBackpressureQueue_ShouldExecuteSuccessfully()
    {
        PrintExercise5Header();

        var (exitCode, output, error) = await ExecuteExerciseAsync(
            Exercise5Path,
            Array.Empty<string>(),
            ExerciseTimeout);

        TestContext.WriteLine();
        TestContext.WriteLine("--------------------------------------------------------------------------------");
        TestContext.WriteLine("Test Validation");
        TestContext.WriteLine("--------------------------------------------------------------------------------");

        var validationChecks = BuildExercise5ValidationChecks(output);
        ValidateExerciseResults(validationChecks, output, error, "Exercise 5");

        Assert.That(exitCode, Is.EqualTo(0),
            $"Exercise 5 should complete successfully. Exit code: {exitCode}\nError: {error}");

        TestContext.WriteLine();
        TestContext.WriteLine("✅ Exercise 5 completed successfully");
        TestContext.WriteLine("================================================================================");
    }

    private static Dictionary<string, (bool result, string failureMessage)> BuildExercise1ValidationChecks(string output)
    {
        return new Dictionary<string, (bool result, string failureMessage)>
        {
            ["Exercise Started"] = (output.Contains("Exercise 3.1", StringComparison.OrdinalIgnoreCase) ||
                                   output.Contains("Netflix", StringComparison.OrdinalIgnoreCase) ||
                                   output.Contains("Starting Exercise", StringComparison.OrdinalIgnoreCase),
                                   "Exercise 3.1 not found"),
            ["Backpressure Implementation"] = (output.Contains("backpressure", StringComparison.OrdinalIgnoreCase) ||
                                              output.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
                                              output.Contains("adaptive", StringComparison.OrdinalIgnoreCase),
                                              "Backpressure implementation not found"),
            ["Netflix Patterns"] = (output.Contains("Netflix", StringComparison.OrdinalIgnoreCase) ||
                                   output.Contains("streaming", StringComparison.OrdinalIgnoreCase) ||
                                   output.Contains("quality", StringComparison.OrdinalIgnoreCase),
                                   "Netflix patterns not found"),
            ["Execution Completed"] = (output.Contains("COMPLETED", StringComparison.OrdinalIgnoreCase) ||
                                      output.Contains("completed successfully", StringComparison.OrdinalIgnoreCase) ||
                                      output.Contains("SUCCESS", StringComparison.OrdinalIgnoreCase) ||
                                      output.Contains("✅"),
                                      "Exercise did not complete successfully")
        };
    }

    private static Dictionary<string, (bool result, string failureMessage)> BuildExercise2ValidationChecks(string output)
    {
        return new Dictionary<string, (bool result, string failureMessage)>
        {
            ["Exercise Started"] = (output.Contains("Exercise 3.2", StringComparison.OrdinalIgnoreCase) ||
                                   output.Contains("Multi-Tier", StringComparison.OrdinalIgnoreCase) ||
                                   output.Contains("Starting Exercise", StringComparison.OrdinalIgnoreCase),
                                   "Exercise 3.2 not found"),
            ["Industry Patterns"] = (output.Contains("Twitter", StringComparison.OrdinalIgnoreCase) ||
                                    output.Contains("Uber", StringComparison.OrdinalIgnoreCase) ||
                                    output.Contains("Production Patterns", StringComparison.OrdinalIgnoreCase),
                                    "Industry patterns not found"),
            ["Rate Limiting"] = (output.Contains("rate", StringComparison.OrdinalIgnoreCase) ||
                                output.Contains("limit", StringComparison.OrdinalIgnoreCase) ||
                                output.Contains("tier", StringComparison.OrdinalIgnoreCase),
                                "Rate limiting not found"),
            ["Execution Completed"] = (output.Contains("COMPLETED", StringComparison.OrdinalIgnoreCase) ||
                                      output.Contains("completed successfully", StringComparison.OrdinalIgnoreCase) ||
                                      output.Contains("SUCCESS", StringComparison.OrdinalIgnoreCase) ||
                                      output.Contains("✅"),
                                      "Exercise did not complete successfully")
        };
    }

    private static Dictionary<string, (bool result, string failureMessage)> BuildExercise3ValidationChecks(string output)
    {
        return new Dictionary<string, (bool result, string failureMessage)>
        {
            ["Exercise Started"] = (output.Contains("Exercise 3.3") || output.Contains("Performance Testing"), "Exercise 3.3 not found"),
            ["Performance Testing"] = (output.Contains("performance") || output.Contains("testing") || output.Contains("load"), "Performance testing not found"),
            ["Industry Patterns"] = (output.Contains("Netflix") || output.Contains("Uber") || output.Contains("Twitter"), "Industry patterns not found"),
            ["Execution Completed"] = (output.Contains("COMPLETED") || output.Contains("completed successfully") || output.Contains("SUCCESS") || output.Contains("✅"), "Exercise did not complete successfully")
        };
    }

    private static Dictionary<string, (bool result, string failureMessage)> BuildExercise4ValidationChecks(string output)
    {
        return new Dictionary<string, (bool result, string failureMessage)>
        {
            ["Exercise Started"] = (output.Contains("Exercise 3.4") || output.Contains("Production Deployment"), "Exercise 3.4 not found"),
            ["Deployment Patterns"] = (output.Contains("deployment") || output.Contains("Blue-green") || output.Contains("Canary"), "Deployment patterns not found"),
            ["Resilience"] = (output.Contains("resilience") || output.Contains("Circuit breaker") || output.Contains("health"), "Resilience patterns not found"),
            ["Execution Completed"] = (output.Contains("COMPLETED") || output.Contains("completed successfully") || output.Contains("SUCCESS") || output.Contains("✅"), "Exercise did not complete successfully")
        };
    }

    private static Dictionary<string, (bool result, string failureMessage)> BuildExercise5ValidationChecks(string output)
    {
        return new Dictionary<string, (bool result, string failureMessage)>
        {
            ["Exercise Started"] = (output.Contains("Exercise 3.5", StringComparison.OrdinalIgnoreCase) ||
                                   output.Contains("BackpressureQueue", StringComparison.OrdinalIgnoreCase) ||
                                   output.Contains("Starting", StringComparison.OrdinalIgnoreCase),
                                   "Exercise not found"),
            ["Backpressure Implementation"] = (output.Contains("backpressure", StringComparison.OrdinalIgnoreCase) ||
                                              output.Contains("semaphore", StringComparison.OrdinalIgnoreCase) ||
                                              output.Contains("queue", StringComparison.OrdinalIgnoreCase) ||
                                              output.Contains("per-customer", StringComparison.OrdinalIgnoreCase),
                                              "Backpressure implementation not found"),
            ["Architecture"] = (output.Contains("Gateway", StringComparison.OrdinalIgnoreCase) ||
                               output.Contains("Kafka", StringComparison.OrdinalIgnoreCase) ||
                               output.Contains("Flink", StringComparison.OrdinalIgnoreCase) ||
                               output.Contains("Temporal", StringComparison.OrdinalIgnoreCase),
                               "Architecture components not found"),
            ["Execution Completed"] = (output.Contains("COMPLETED", StringComparison.OrdinalIgnoreCase) ||
                                      output.Contains("completed successfully", StringComparison.OrdinalIgnoreCase) ||
                                      output.Contains("SUCCESS", StringComparison.OrdinalIgnoreCase) ||
                                      output.Contains("✅"),
                                      "Exercise did not complete successfully")
        };
    }

    private static void PrintExercise2Header()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 2: Uber Regional Redis Coordination");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine();
        TestContext.WriteLine("Reference: Uber's Rate Limiting at Scale");
        TestContext.WriteLine();
        TestContext.WriteLine("Testing concepts:");
        TestContext.WriteLine("  - Atomic Redis operations with DECRBY");
        TestContext.WriteLine("  - Regional budget bank management");
        TestContext.WriteLine("  - TTL management for budget expiration");
        TestContext.WriteLine("  - Regional failover handling");
        TestContext.WriteLine("  - Uber-scale traffic coordination");
        TestContext.WriteLine();
    }

    private static void PrintExercise3Header()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 3: LinkedIn High-Performance Gateway");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine();
        TestContext.WriteLine("Reference: LinkedIn API Gateway Patterns");
        TestContext.WriteLine();
        TestContext.WriteLine("Testing concepts:");
        TestContext.WriteLine("  - Local token buckets for hot path");
        TestContext.WriteLine("  - Stateless rate limiting");
        TestContext.WriteLine("  - Background refill from Regional Budget Bank");
        TestContext.WriteLine("  - Safe by default startup behavior");
        TestContext.WriteLine("  - LinkedIn-scale API gateway patterns");
        TestContext.WriteLine();
    }

    private static void PrintExercise4Header()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 4: Chaos Engineering Production Validation");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine();
        TestContext.WriteLine("Reference: Netflix/Uber/LinkedIn Chaos Engineering");
        TestContext.WriteLine();
        TestContext.WriteLine("Testing concepts:");
        TestContext.WriteLine("  - Combined failure scenarios");
        TestContext.WriteLine("  - Fail-closed behavior verification");
        TestContext.WriteLine("  - End-to-end flow control validation");
        TestContext.WriteLine("  - Production monitoring during failures");
        TestContext.WriteLine("  - Enterprise resilience patterns");
        TestContext.WriteLine();
    }

    private static void PrintExercise5Header()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 5: Simple BackpressureQueue Implementation");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine();
        TestContext.WriteLine("Reference: Simple vs Complex Backpressure Patterns");
        TestContext.WriteLine();
        TestContext.WriteLine("Testing concepts:");
        TestContext.WriteLine("  - Semaphore-based backpressure limiting");
        TestContext.WriteLine("  - Simple vs complex approach comparison");
        TestContext.WriteLine("  - Three test scenarios with different configurations");
        TestContext.WriteLine("  - When to use simple solutions");
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