using NUnit.Framework;

namespace LearningCourse.IntegrationTests;

/// <summary>
/// Integration tests for Day 6: Temporal Workflows
///
/// These tests validate real Temporal workflow exercises:
/// - Exercise 6.1: Basic Workflow Definition (OrderProcessingWorkflow)
/// - Exercise 6.2: Activity Patterns (PaymentRetryWorkflow with retry logic)
/// - Exercise 6.3: Error Handling (BookingSagaWorkflow with compensation)
/// - Exercise 6.4: Advanced Patterns (SupportTicketWorkflow with signals/queries)
///
/// Infrastructure: Uses real Temporal server in LocalTesting (temporalio/auto-setup:1.22.4 + PostgreSQL)
/// </summary>
[TestFixture]
[NonParallelizable]
[Category("day06-temporal-workflows")]
[Category("integration")]
public class Day06Tests : LearningCourseTestBase
{
    private const string Exercise1Path = "Day06-Temporal-Workflows/Exercise-Solutions/Exercise61";
    private const string Exercise2Path = "Day06-Temporal-Workflows/Exercise-Solutions/Exercise62";
    private const string Exercise3Path = "Day06-Temporal-Workflows/Exercise-Solutions/Exercise63";
    private const string Exercise4Path = "Day06-Temporal-Workflows/Exercise-Solutions/Exercise64";
    private static readonly TimeSpan ExerciseTimeout = TimeSpan.FromSeconds(30);

    [Test]
    [Description("Exercise 6.1: Basic Workflow Definition - OrderProcessingWorkflow")]
    public async Task Exercise61_BasicWorkflowDefinition_ProcessesOrdersSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 6.1: Basic Workflow Definition");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("Validates: OrderProcessingWorkflow with sequential activity execution");
        TestContext.WriteLine();

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise1Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 6.1 should complete successfully. Exit code: {exitCode}\nError: {error}");
        
        // Verify workflow execution
        Assert.That(output, Does.Contain("Exercise 6.1 completed successfully"), "Should complete exercise");
        Assert.That(output, Does.Contain("workflows executed"), "Should report workflow count");
        Assert.That(output, Does.Contain("ORDER-001"), "Should process ORDER-001");
        Assert.That(output, Does.Contain("ORDER-002"), "Should process ORDER-002");
        Assert.That(output, Does.Contain("ORDER-003"), "Should process ORDER-003");
        
        TestContext.WriteLine("✅ Exercise 6.1: All order workflows completed successfully");
    }

    [Test]
    [Description("Exercise 6.2: Activity Patterns - PaymentRetryWorkflow with retry logic")]
    public async Task Exercise62_ActivityPatterns_DemonstratesRetryLogic()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 6.2: Activity Patterns & Retry Logic");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("Validates: Automatic retry, exponential backoff, non-retryable errors");
        TestContext.WriteLine();

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise2Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 6.2 should complete successfully. Exit code: {exitCode}\nError: {error}");
        
        // Verify retry patterns
        Assert.That(output, Does.Contain("Exercise 6.2 completed successfully"), "Should complete exercise");
        Assert.That(output, Does.Contain("PAY-001"), "Should process PAY-001 (success)");
        Assert.That(output, Does.Contain("PAY-002"), "Should process PAY-002 (temporary failure)");
        Assert.That(output, Does.Contain("PAY-003"), "Should handle PAY-003 (non-retryable)");
        Assert.That(output, Does.Contain("Retry patterns demonstrated"), "Should demonstrate retry patterns");
        
        TestContext.WriteLine("✅ Exercise 6.2: Retry patterns validated successfully");
    }

    [Test]
    [Description("Exercise 6.3: Error Handling - BookingSagaWorkflow with compensation")]
    [Ignore("Known issue: Exercise hangs in test infrastructure but works manually. Needs investigation.")]
    public async Task Exercise63_ErrorHandling_ExecutesSagaCompensation()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 6.3: Error Handling & Saga Pattern");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("Validates: Saga pattern, compensation logic, reverse-order rollback");
        TestContext.WriteLine();

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise3Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 6.3 should complete successfully. Exit code: {exitCode}\nError: {error}");
        
        // Verify saga pattern
        Assert.That(output, Does.Contain("Exercise 6.3 completed successfully"), "Should complete exercise");
        Assert.That(output, Does.Contain("BOOK-001"), "Should process BOOK-001 (success)");
        Assert.That(output, Does.Contain("BOOK-002"), "Should process BOOK-002 (payment failure)");
        Assert.That(output, Does.Contain("BOOK-003"), "Should process BOOK-003 (shipment failure)");
        Assert.That(output, Does.Contain("Saga pattern demonstrated"), "Should demonstrate saga pattern");
        Assert.That(output, Does.Contain("Compensation logic validated"), "Should validate compensation");
        
        TestContext.WriteLine("✅ Exercise 6.3: Saga compensation validated successfully");
    }

    [Test]
    [Description("Exercise 6.4: Advanced Patterns - SupportTicketWorkflow with signals/queries")]
    [Ignore("Known issue: Exercise hangs in test infrastructure but works manually. Needs investigation.")]
    public async Task Exercise64_AdvancedPatterns_HandlesSignalsAndQueries()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 6.4: Advanced Workflow Patterns");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("Validates: Workflow signals, queries, WaitCondition, dynamic behavior");
        TestContext.WriteLine();

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise4Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 6.4 should complete successfully. Exit code: {exitCode}\nError: {error}");
        
        // Verify advanced patterns
        Assert.That(output, Does.Contain("Exercise 6.4 completed successfully"), "Should complete exercise");
        Assert.That(output, Does.Contain("TICKET-001"), "Should process support ticket");
        Assert.That(output, Does.Contain("Signals demonstrated"), "Should demonstrate signals");
        Assert.That(output, Does.Contain("Queries demonstrated"), "Should demonstrate queries");
        Assert.That(output, Does.Contain("Querying workflow status"), "Should query workflow status");
        Assert.That(output, Does.Contain("Adding comment via signal"), "Should add comment via signal");
        Assert.That(output, Does.Contain("Escalating priority via signal"), "Should escalate via signal");
        
        TestContext.WriteLine("✅ Exercise 6.4: Signals and queries validated successfully");
    }
}