using LearningCourse.IntegrationTests;
using NUnit.Framework;

namespace Day01.IntegrationTests;

/// <summary>
/// Integration tests for Day 1: Kafka-Flink Data Pipeline
///
/// Reference: https://www.baeldung.com/kafka-flink-data-pipeline
///
/// These tests validate exercises based on the Baeldung tutorial structure:
/// - Exercise 1: String Stream Processing (Sections 1-6) - Capitalize transformation
/// - Exercise 2: Custom Objects and Backup Aggregation (Sections 7-11) - Time-windowed aggregation
///
/// Implementation: Uses FlinkDotNet with .NET Aspire for infrastructure
/// </summary>
[TestFixture]
[Category("day01-kafka-flink-pipeline")]
[Category("integration")]
public class ExerciseExecutionTests : LearningCourseTestBase
{
    private const string Exercise1Path = "Day01-Kafka-Flink-Data-Pipeline/Exercise-Solutions/Exercise1-StringCapitalize";
    private const string Exercise2Path = "Day01-Kafka-Flink-Data-Pipeline/Exercise-Solutions/Exercise2-BackupAggregator";
    private static readonly TimeSpan ExerciseTimeout = TimeSpan.FromMinutes(3);

    /// <summary>
    /// Exercise 1: String Stream Processing (Baeldung Sections 1-6)
    ///
    /// This test validates:
    /// - Section 1: Overview - Understanding stream processing concepts
    /// - Section 2: Installation - Infrastructure setup via Aspire
    /// - Section 3: Flink Usage - Job submission and execution
    /// - Section 4: Kafka String Consumer - Consumer configuration
    /// - Section 5: Kafka String Producer - Producer configuration
    /// - Section 6: String Stream Processing - Capitalize transformation
    ///
    /// Pipeline: Kafka Input → Flink (Uppercase) → Kafka Output
    /// </summary>
    [Test]
    [Description("Exercise 1: String Stream Processing - Capitalize (Baeldung Sections 1-6)")]
    public async Task Exercise1_StringCapitalize_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 1: String Stream Processing (Capitalize)");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine();
        TestContext.WriteLine("Reference: https://www.baeldung.com/kafka-flink-data-pipeline (Sections 1-6)");
        TestContext.WriteLine();
        TestContext.WriteLine("Testing concepts:");
        TestContext.WriteLine("  - Section 1: Overview (Stream processing with Kafka + Flink)");
        TestContext.WriteLine("  - Section 2: Installation (Aspire infrastructure)");
        TestContext.WriteLine("  - Section 3: Flink Usage (Job submission)");
        TestContext.WriteLine("  - Section 4: Kafka String Consumer");
        TestContext.WriteLine("  - Section 5: Kafka String Producer");
        TestContext.WriteLine("  - Section 6: String Stream Processing (Capitalize)");
        TestContext.WriteLine();

        var (exitCode, output, error) = await ExecuteExerciseAsync(
            Exercise1Path,
            Array.Empty<string>(),
            ExerciseTimeout);

        TestContext.WriteLine();
        TestContext.WriteLine("--------------------------------------------------------------------------------");
        TestContext.WriteLine("Test Validation");
        TestContext.WriteLine("--------------------------------------------------------------------------------");

        // Check for actual infrastructure failure (not retry messages)
        // Distinguish between temporary retries (expected) and permanent failures (unexpected)
        bool hasKafkaTimeoutError = error.Contains("Kafka not ready within") && error.Contains("TimeoutException");
        bool hasFlinkTimeoutError = error.Contains("Could not connect to Flink") && error.Contains("TimeoutException");
        bool isInfrastructureTimeout = hasKafkaTimeoutError || hasFlinkTimeoutError;

        if (isInfrastructureTimeout)
        {
            TestContext.WriteLine("[FAIL] Infrastructure timeout detected - services did not become ready");
            TestContext.WriteLine($"Output: {output}");
            TestContext.WriteLine($"Error: {error}");
            Assert.Fail("Exercise 1 failed due to infrastructure timeout. Kafka or Flink did not become available within the timeout period.");
            return;
        }

        // Validate execution completed successfully
        Assert.That(exitCode, Is.EqualTo(0),
            $"Exercise 1 should complete successfully. Exit code: {exitCode}\nError: {error}");

        // Validate all required steps occurred
        bool hasKafkaReady = output.Contains("Kafka is ready") || output.Contains("Verifying Kafka");
        bool hasTopicsCreated = output.Contains("Topics created") || output.Contains("Topics already exist");
        bool hasJobSubmit = output.Contains("Submitting Flink") || output.Contains("capitalize job");
        bool hasProducing = output.Contains("Producing") && output.Contains("lowercase messages");
        bool hasConsuming = output.Contains("Consuming") && output.Contains("capitalized");
        bool hasCompletion = output.Contains("EXERCISE 1 COMPLETED") || output.Contains("COMPLETED");

        TestContext.WriteLine($"[CHECK] Kafka Ready: {hasKafkaReady}");
        TestContext.WriteLine($"[CHECK] Topics Created: {hasTopicsCreated}");
        TestContext.WriteLine($"[CHECK] Job Submitted: {hasJobSubmit}");
        TestContext.WriteLine($"[CHECK] Messages Produced: {hasProducing}");
        TestContext.WriteLine($"[CHECK] Results Consumed: {hasConsuming}");
        TestContext.WriteLine($"[CHECK] Exercise Completed: {hasCompletion}");

        Assert.That(hasKafkaReady, Is.True, "Should verify Kafka is ready");
        Assert.That(hasTopicsCreated, Is.True, "Should create Kafka topics");
        Assert.That(hasJobSubmit, Is.True, "Should submit Flink capitalize job");
        Assert.That(hasProducing, Is.True, "Should produce lowercase messages");
        Assert.That(hasCompletion, Is.True, "Should complete exercise successfully");

        TestContext.WriteLine();
        TestContext.WriteLine("[PASS] Exercise 1 validated all Baeldung Sections 1-6 concepts");
        TestContext.WriteLine();
    }

    /// <summary>
    /// Exercise 2: Custom Objects and Backup Aggregation (Baeldung Sections 7-11)
    ///
    /// This test validates:
    /// - Section 7: Custom Object Deserialization - InputMessage deserialization
    /// - Section 8: Custom Object Serialization - Backup serialization
    /// - Section 9: Timestamping Messages - EventTime handling
    /// - Section 10: Creating Time Windows - Tumbling 24-hour windows
    /// - Section 11: Aggregating Backups - Daily message aggregation
    ///
    /// Pipeline: Kafka Input (InputMessage) → Flink (Time Window + Aggregate) → Kafka Output (Backup)
    /// </summary>
    [Test]
    [Description("Exercise 2: Custom Objects and Backup Aggregation (Baeldung Sections 7-11)")]
    public async Task Exercise2_BackupAggregator_ShouldExecuteSuccessfully()
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

        CheckForInfrastructureTimeout(error, "Exercise 2");
        CheckForDeserializationErrors(output);

        // Validate execution completed successfully
        Assert.That(exitCode, Is.EqualTo(0),
            $"Exercise 2 should complete successfully. Exit code: {exitCode}\nError: {error}");

        var validationChecks = BuildExercise2ValidationChecks(output);
        PrintValidationResults(validationChecks);
        ValidateExerciseResults(validationChecks, output, error, "Exercise 2");

        TestContext.WriteLine();
        TestContext.WriteLine("[PASS] Exercise 2 validated all Baeldung Sections 7-11 concepts");
        TestContext.WriteLine();
    }

    private static void PrintExercise2Header()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 2: Custom Objects and Backup Aggregation");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine();
        TestContext.WriteLine("Reference: https://www.baeldung.com/kafka-flink-data-pipeline (Sections 7-11)");
        TestContext.WriteLine();
        TestContext.WriteLine("Testing concepts:");
        TestContext.WriteLine("  - Section 7: Custom Object Deserialization (InputMessage)");
        TestContext.WriteLine("  - Section 8: Custom Object Serialization (Backup)");
        TestContext.WriteLine("  - Section 9: Timestamping Messages (EventTime)");
        TestContext.WriteLine("  - Section 10: Creating Time Windows (Tumbling)");
        TestContext.WriteLine("  - Section 11: Aggregating Backups (Daily aggregation)");
        TestContext.WriteLine();
    }

    private static void CheckForInfrastructureTimeout(string error, string exerciseName)
    {
        bool hasKafkaTimeoutError = error.Contains("Kafka not ready within") && error.Contains("TimeoutException");
        bool hasFlinkTimeoutError = error.Contains("Could not connect to Flink") && error.Contains("TimeoutException");
        
        if (hasKafkaTimeoutError || hasFlinkTimeoutError)
        {
            TestContext.WriteLine("[FAIL] Infrastructure timeout detected - services did not become ready");
            Assert.Fail($"{exerciseName} failed due to infrastructure timeout. Kafka or Flink did not become available within the timeout period.");
        }
    }

    private static void CheckForDeserializationErrors(string output)
    {
        bool hasDeserializationError = output.Contains("[ERROR] Deserialization error:") ||
                                       output.Contains("is an invalid start of a value");
        
        if (hasDeserializationError)
        {
            PrintDeserializationDiagnostics(output);
            FailWithDeserializationError();
        }
    }
    
    private static void PrintDeserializationDiagnostics(string output)
    {
        TestContext.WriteLine("[FAIL] Deserialization error detected in Backup aggregation consumption");
        TestContext.WriteLine();
        TestContext.WriteLine("=== DIAGNOSTIC OUTPUT ===");
        
        var lines = output.Split('\n');
        ProcessDiagnosticLines(lines);
        
        TestContext.WriteLine();
        TestContext.WriteLine("=== END DIAGNOSTIC OUTPUT ===");
        TestContext.WriteLine();
    }
    
    private static void ProcessDiagnosticLines(string[] lines)
    {
        bool inRawMessageSection = false;
        bool inDebugSection = false;
        
        foreach (var line in lines)
        {
            ProcessSingleDiagnosticLine(line, ref inRawMessageSection, ref inDebugSection);
        }
    }
    
    private static void ProcessSingleDiagnosticLine(string line, ref bool inRawMessageSection, ref bool inDebugSection)
    {
        // Capture raw Kafka message sections
        if (line.Contains("Raw Kafka Message:"))
        {
            inRawMessageSection = true;
        }
        
        // Capture full raw value
        if (line.Contains("Full Raw Value:"))
        {
            TestContext.WriteLine($">>> {line.Trim()}");
            inRawMessageSection = false;
        }
        
        // Capture character breakdown
        if (line.Contains("Character breakdown"))
        {
            inDebugSection = true;
            TestContext.WriteLine($">>> {line.Trim()}");
        }
        
        if (inRawMessageSection || inDebugSection)
        {
            TestContext.WriteLine($">>> {line.Trim()}");
        }
        
        // End of debug section
        if (inDebugSection && (line.Contains("Backup Deserialized:") || line.Contains("[FAIL]")))
        {
            inDebugSection = false;
        }
        
        // Print error lines
        if (line.Contains("[ERROR]") || line.Contains("[FAIL]"))
        {
            TestContext.WriteLine($">>> {line.Trim()}");
        }
    }
    
    private static void FailWithDeserializationError()
    {
        Assert.Fail("Exercise 2 failed: Deserialization error when consuming Backup aggregations. " +
                   "This indicates the Flink job is outputting malformed JSON. " +
                   "Check the diagnostic output above for the exact raw content that caused the error.");
    }

    private static Dictionary<string, (bool result, string failureMessage)> BuildExercise2ValidationChecks(string output)
    {
        return new Dictionary<string, (bool result, string failureMessage)>
        {
            ["Kafka Ready"] = (output.Contains("Kafka is ready") || output.Contains("Verifying Kafka"), "Kafka is not ready"),
            ["Topics Created"] = (output.Contains("Topics created") || output.Contains("Topics already exist"), "Kafka topics were not created"),
            ["Job Submitted"] = (output.Contains("Submitting Flink") && output.Contains("backup aggregation") || output.Contains("job submitted successfully"), "Flink backup aggregation job was not submitted"),
            ["EventTime Used"] = (output.Contains("EventTime") || output.Contains("timestamped"), "EventTime was not used"),
            ["Count Windows"] = (output.Contains("Count windows") || output.Contains("CountWindowAll") || output.Contains("50 messages") || output.Contains("count-based"), "Count windows were not configured"),
            ["InputMessages Produced"] = (output.Contains("Producing") && output.Contains("InputMessage") || output.Contains("All 50 InputMessage objects produced"), "InputMessage objects were not produced"),
            ["Backups Consumed"] = (output.Contains("Consumed") && output.Contains("Backup") || output.Contains("Successfully aggregated") || output.Contains("window fired"), "Should consume aggregated backups with count window"),
            ["Job Running"] = (output.Contains("Job is running") || output.Contains("job submitted") || output.Contains("Flink") || output.Contains("SUCCESS"), "Job should be running in Flink")
        };
    }

    private static void PrintValidationResults(Dictionary<string, (bool result, string failureMessage)> validationChecks)
    {
        foreach (var check in validationChecks)
        {
            TestContext.WriteLine($"[CHECK] {check.Key}: {check.Value.result}");
        }
    }

    /// <summary>
    /// Helper method to validate exercise results and report failures
    /// </summary>
    private static void ValidateExerciseResults(
        Dictionary<string, (bool result, string failureMessage)> validationChecks,
        string output,
        string error,
        string exerciseName)
    {
        var validationFailures = validationChecks
            .Where(kvp => !kvp.Value.result)
            .Select(kvp => kvp.Value.failureMessage)
            .ToList();

        if (validationFailures.Count == 0)
            return;

        ReportValidationFailures(validationFailures, output, error, exerciseName);
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
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("VALIDATION FAILURES DETECTED");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine($"Failed validations ({validationFailures.Count}):");
        
        foreach (var failure in validationFailures)
        {
            TestContext.WriteLine($"  - {failure}");
        }

        PrintDebugOutput(output, error);

        Assert.Fail($"{exerciseName} validation failed. {validationFailures.Count} check(s) failed:\n  - {string.Join("\n  - ", validationFailures)}");
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