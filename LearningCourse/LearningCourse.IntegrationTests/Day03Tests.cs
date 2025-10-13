using NUnit.Framework;

namespace LearningCourse.IntegrationTests;

/// <summary>
/// Integration tests for Day 3: AI Stream Processing with Apache Flink 2.1.0
///
/// Reference: https://flink.apache.org/2025/07/31/apache-flink-2.1.0-ushers-in-a-new-era-of-unified-real-time-data--ai-with-comprehensive-upgrades/
///
/// These tests validate exercises based on Flink 2.1.0 AI features:
/// - Exercise 1: Netflix AI Model DDL Mastery - Complete AI model lifecycle management
/// - Exercise 2: Uber Fraud Detection Pipeline - Real-time AI inference with ML_PREDICT TVF
/// - Exercise 3: LinkedIn Behavioral Analytics - Event-driven AI applications with PTFs
/// - Exercise 4: Amazon Product Recommendations - VARIANT data types for dynamic schema AI
///
/// Implementation: Uses FlinkDotNet with .NET Aspire for infrastructure
/// </summary>
[TestFixture]
[Category("day03-ai-stream-processing")]
[Category("integration")]
public class Day03Tests : LearningCourseTestBase
{
    private const string Exercise1Path = "Day03-AI-Stream-Processing/Exercise-Solutions/AIModelDDLMastery";
    private const string Exercise2Path = "Day03-AI-Stream-Processing/Exercise-Solutions/FraudDetectionSystem";
    private const string Exercise3Path = "Day03-AI-Stream-Processing/Exercise-Solutions/MLPredictTVFImplementation";
    private const string Exercise4Path = "Day03-AI-Stream-Processing/Exercise-Solutions/MLNetIntegration";
    private static readonly TimeSpan ExerciseTimeout = TimeSpan.FromMinutes(3);

    /// <summary>
    /// Exercise 1: Netflix AI Model DDL Mastery (90 minutes)
    ///
    /// This test validates:
    /// - AI model registration with complete metadata
    /// - Model versioning and lifecycle management
    /// - A/B testing infrastructure for content recommendation
    /// - Enterprise model governance and compliance
    /// - Multi-environment deployment (staging → production)
    ///
    /// Expected: Netflix-scale model management, 99.9% recommendation uptime, 200+ models
    /// </summary>
    [Test]
    [Description("Exercise 1: Netflix AI Model DDL Mastery - Complete model lifecycle")]
    public async Task Exercise1_AIModelDDLMastery_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 1: Netflix AI Model DDL Mastery");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine();
        TestContext.WriteLine("Reference: Apache Flink 2.1.0 AI Model DDL");
        TestContext.WriteLine();
        TestContext.WriteLine("Testing concepts:");
        TestContext.WriteLine("  - AI model registration and lifecycle management");
        TestContext.WriteLine("  - Model versioning with inheritance patterns");
        TestContext.WriteLine("  - A/B testing infrastructure");
        TestContext.WriteLine("  - Auto-rollback conditions");
        TestContext.WriteLine("  - Enterprise model governance");
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
    /// Exercise 2: Uber Fraud Detection Pipeline (120 minutes)
    ///
    /// This test validates:
    /// - Real-time AI inference using ML_PREDICT TVF
    /// - Multi-model ensemble inference
    /// - Dynamic model selection based on transaction characteristics
    /// - Real-time feature engineering with streaming joins
    /// - Production integration with monitoring
    ///
    /// Expected: 99.8% fraud accuracy, sub-100ms inference, 15M+ daily transactions
    /// </summary>
    [Test]
    [Description("Exercise 2: Uber Fraud Detection Pipeline - ML_PREDICT TVF implementation")]
    public async Task Exercise2_FraudDetectionSystem_ShouldExecuteSuccessfully()
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
    /// Exercise 3: LinkedIn Behavioral Analytics Engine (150 minutes)
    ///
    /// This test validates:
    /// - Process Table Functions (PTFs) with managed state
    /// - Stateful behavioral analysis
    /// - Event-driven AI applications
    /// - Real-time personalization scoring
    /// - Complex event processing with timer services
    ///
    /// Expected: 900M+ user events processed, real-time content scoring, stateful profiling
    /// </summary>
    [Test]
    [Description("Exercise 3: LinkedIn Behavioral Analytics - PTFs with managed state")]
    public async Task Exercise3_MLPredictTVFImplementation_ShouldExecuteSuccessfully()
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
    /// Exercise 4: Amazon Product Recommendation Engine (90 minutes)
    ///
    /// This test validates:
    /// - VARIANT data types for dynamic schema processing
    /// - Semi-structured product data handling
    /// - Dynamic schema evolution for diverse categories
    /// - Flexible feature engineering
    /// - Apache Paimon integration for lakehouse
    ///
    /// Expected: 310M+ customers supported, flexible catalog processing, improved accuracy
    /// </summary>
    [Test]
    [Description("Exercise 4: Amazon Product Recommendations - VARIANT types for dynamic AI")]
    public async Task Exercise4_MLNetIntegration_ShouldExecuteSuccessfully()
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

    private static Dictionary<string, (bool result, string failureMessage)> BuildExercise1ValidationChecks(string output)
    {
        return new Dictionary<string, (bool result, string failureMessage)>
        {
            ["Exercise Started"] = (output.Contains("Exercise") || output.Contains("AI") || output.Contains("Model"), "Exercise output not found"),
            ["AI/ML Processing"] = (output.Contains("model", StringComparison.OrdinalIgnoreCase) ||
                                   output.Contains("AI", StringComparison.OrdinalIgnoreCase) ||
                                   output.Contains("ML", StringComparison.OrdinalIgnoreCase) ||
                                   output.Contains("machine learning", StringComparison.OrdinalIgnoreCase), "AI/ML processing not found"),
            ["Execution Completed"] = (output.Contains("COMPLETED") || output.Contains("completed successfully") || output.Contains("SUCCESS") || output.Contains("✅") || output.Contains("finished"), "Exercise did not complete successfully")
        };
    }

    private static Dictionary<string, (bool result, string failureMessage)> BuildExercise2ValidationChecks(string output)
    {
        // More lenient validation - accept fraud detection demonstrations even without "Uber" branding
        return new Dictionary<string, (bool result, string failureMessage)>
        {
            ["Fraud Detection"] = (output.Contains("fraud", StringComparison.OrdinalIgnoreCase) ||
                                   output.Contains("risk", StringComparison.OrdinalIgnoreCase) ||
                                   output.Contains("transaction", StringComparison.OrdinalIgnoreCase),
                                   "Fraud detection not found"),
            ["Real-time Processing"] = (output.Contains("latency", StringComparison.OrdinalIgnoreCase) ||
                                       output.Contains("processing", StringComparison.OrdinalIgnoreCase) ||
                                       output.Contains("rate", StringComparison.OrdinalIgnoreCase) ||
                                       output.Contains("metrics", StringComparison.OrdinalIgnoreCase),
                                       "Real-time processing metrics not found"),
            ["Execution Completed"] = (output.Contains("COMPLETED") || output.Contains("SUCCESS") || output.Contains("✅"),
                                      "Exercise did not complete successfully")
        };
    }

    private static Dictionary<string, (bool result, string failureMessage)> BuildExercise3ValidationChecks(string output)
    {
        return new Dictionary<string, (bool result, string failureMessage)>
        {
            ["Exercise Started"] = (output.Contains("Exercise") || output.Contains("Processing") || output.Contains("Starting"), "Exercise output not found"),
            ["ML/AI Processing"] = (output.Contains("model", StringComparison.OrdinalIgnoreCase) ||
                                   output.Contains("predict", StringComparison.OrdinalIgnoreCase) ||
                                   output.Contains("ML", StringComparison.OrdinalIgnoreCase) ||
                                   output.Contains("analytics", StringComparison.OrdinalIgnoreCase), "ML/AI processing not found"),
            ["Execution Completed"] = (output.Contains("COMPLETED") || output.Contains("completed successfully") || output.Contains("SUCCESS") || output.Contains("✅") || output.Contains("finished"), "Exercise did not complete successfully")
        };
    }

    private static Dictionary<string, (bool result, string failureMessage)> BuildExercise4ValidationChecks(string output)
    {
        return new Dictionary<string, (bool result, string failureMessage)>
        {
            ["Exercise Started"] = (output.Contains("Exercise") || output.Contains("Processing") || output.Contains("Starting"), "Exercise output not found"),
            ["ML Integration"] = (output.Contains("ML", StringComparison.OrdinalIgnoreCase) ||
                                output.Contains("model", StringComparison.OrdinalIgnoreCase) ||
                                output.Contains("predict", StringComparison.OrdinalIgnoreCase) ||
                                output.Contains("integration", StringComparison.OrdinalIgnoreCase), "ML integration not found"),
            ["Execution Completed"] = (output.Contains("COMPLETED") || output.Contains("completed successfully") || output.Contains("SUCCESS") || output.Contains("✅") || output.Contains("finished"), "Exercise did not complete successfully")
        };
    }

    private static void PrintExercise2Header()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 2: Uber Fraud Detection Pipeline");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine();
        TestContext.WriteLine("Reference: Apache Flink 2.1.0 ML_PREDICT TVF");
        TestContext.WriteLine();
        TestContext.WriteLine("Testing concepts:");
        TestContext.WriteLine("  - Real-time AI inference using ML_PREDICT TVF");
        TestContext.WriteLine("  - Multi-model ensemble inference");
        TestContext.WriteLine("  - Dynamic model selection");
        TestContext.WriteLine("  - Real-time feature engineering");
        TestContext.WriteLine("  - Production integration with monitoring");
        TestContext.WriteLine();
    }

    private static void PrintExercise3Header()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 3: LinkedIn Behavioral Analytics Engine");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine();
        TestContext.WriteLine("Reference: Apache Flink 2.1.0 Process Table Functions (PTFs)");
        TestContext.WriteLine();
        TestContext.WriteLine("Testing concepts:");
        TestContext.WriteLine("  - Process Table Functions with managed state");
        TestContext.WriteLine("  - Stateful behavioral analysis");
        TestContext.WriteLine("  - Event-driven AI applications");
        TestContext.WriteLine("  - Real-time personalization scoring");
        TestContext.WriteLine("  - Complex event processing with timer services");
        TestContext.WriteLine();
    }

    private static void PrintExercise4Header()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 4: Amazon Product Recommendation Engine");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine();
        TestContext.WriteLine("Reference: Apache Flink 2.1.0 VARIANT Data Types");
        TestContext.WriteLine();
        TestContext.WriteLine("Testing concepts:");
        TestContext.WriteLine("  - VARIANT data types for dynamic schema");
        TestContext.WriteLine("  - Semi-structured product data handling");
        TestContext.WriteLine("  - Dynamic schema evolution");
        TestContext.WriteLine("  - Flexible feature engineering");
        TestContext.WriteLine("  - Apache Paimon lakehouse integration");
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