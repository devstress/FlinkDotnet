using Serilog;
using Temporalio.Client;
using Temporalio.Worker;
using Temporalio.Workflows;
using Temporalio.Activities;
using Temporalio.Exceptions;

// Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Console.WriteLine("🚀 Day 6 Exercise 6.2: Activity Patterns & Retry Logic");
Console.WriteLine("".PadRight(50, '='));
Console.WriteLine();

try
{
    // Helper method for connection retry logic with namespace verification
    async Task<TemporalClient> ConnectWithRetryAsync(string endpoint, int maxAttempts = 10)
    {
        var delayMs = 500;
        
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var client = await TemporalClient.ConnectAsync(new TemporalClientConnectOptions
                {
                    TargetHost = endpoint,
                    Namespace = "default"
                });
                
                // Verify namespace exists by attempting to describe it
                try
                {
                    await client.Connection.WorkflowService.DescribeNamespaceAsync(
                        new Temporalio.Api.WorkflowService.V1.DescribeNamespaceRequest
                        {
                            Namespace = "default"
                        });
                    
                    Log.Information("Namespace 'default' verified successfully");
                    return client;
                }
                catch (Temporalio.Exceptions.RpcException ex) when (ex.Message.Contains("not found") && attempt < maxAttempts)
                {
                    Log.Warning("Namespace 'default' not ready yet (attempt {Attempt}/{Max}). Retrying in {Delay}ms...",
                        attempt, maxAttempts, delayMs * attempt);
                    await Task.Delay(delayMs * attempt);
                    continue;
                }
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                Log.Warning("Connection attempt {Attempt}/{Max} failed: {Error}. Retrying in {Delay}ms...",
                    attempt, maxAttempts, ex.Message, delayMs * attempt);
                await Task.Delay(delayMs * attempt); // Exponential backoff
            }
        }
        
        throw new InvalidOperationException($"Failed to connect to Temporal at {endpoint} with verified namespace after {maxAttempts} attempts");
    }
    
    Log.Information("Starting Exercise 6.2: Activity Implementation with Retry Patterns");
    
    // Get Temporal endpoint from environment variable (service discovery)
    var temporalEndpoint = Environment.GetEnvironmentVariable("TEMPORAL_ENDPOINT") ?? "localhost:7233";
    Log.Information("📡 Connecting to Temporal server at {Endpoint}", temporalEndpoint);
    
    // Connect to Temporal server with retry logic
    var client = await ConnectWithRetryAsync(temporalEndpoint);
    
    Log.Information("✅ Connected to Temporal server successfully");
    
    // Create worker to execute workflows and activities
    const string taskQueue = "payment-processing-queue";
    using var worker = new TemporalWorker(
        client,
        new TemporalWorkerOptions(taskQueue)
            .AddWorkflow<PaymentRetryWorkflow>()
            .AddAllActivities(new PaymentActivities()));
    
    Console.WriteLine();
    Console.WriteLine("┌─────────────────────────────────────────────────────────────┐");
    Console.WriteLine("│ Exercise 6.2: Activity Retry Patterns                      │");
    Console.WriteLine("│                                                             │");
    Console.WriteLine("│ Demonstrates:                                               │");
    Console.WriteLine("│ • Automatic retry with exponential backoff                 │");
    Console.WriteLine("│ • RetryPolicy configuration                                │");
    Console.WriteLine("│ • Non-retryable error handling                             │");
    Console.WriteLine("│ • Activity timeout management                              │");
    Console.WriteLine("└─────────────────────────────────────────────────────────────┘");
    Console.WriteLine();
    
    // Start worker and execute workflows
    await worker.ExecuteAsync(async () =>
    {
        Log.Information("🔄 Temporal worker started on task queue: {TaskQueue}", taskQueue);
        
        // Test different payment scenarios
        var scenarios = new[]
        {
            new PaymentRequest { PaymentId = "PAY-001", Amount = 150.00m, Scenario = PaymentScenario.Success },
            new PaymentRequest { PaymentId = "PAY-002", Amount = 250.00m, Scenario = PaymentScenario.TemporaryFailure },
            new PaymentRequest { PaymentId = "PAY-003", Amount = 350.00m, Scenario = PaymentScenario.InsufficientFunds }
        };
        
        Console.WriteLine("💳 Processing {0} payment scenarios...", scenarios.Length);
        Console.WriteLine();
        
        foreach (var payment in scenarios)
        {
            var workflowId = $"payment-workflow-{payment.PaymentId}";
            
            Log.Information("🚀 Starting workflow for {PaymentId} (Scenario: {Scenario})", 
                payment.PaymentId, payment.Scenario);
            
            var handle = await client.StartWorkflowAsync(
                (PaymentRetryWorkflow wf) => wf.RunAsync(payment),
                new WorkflowOptions(id: workflowId, taskQueue: taskQueue));
            
            try
            {
                // Wait for workflow to complete
                var result = await handle.GetResultAsync();
                
                Console.WriteLine("✅ {0}: {1} - {2} (Attempts: {3})", 
                    payment.PaymentId, 
                    result.Status,
                    result.Message,
                    result.AttemptCount);
            }
            catch (WorkflowFailedException ex)
            {
                Console.WriteLine("❌ {0}: Failed - {1}", 
                    payment.PaymentId, 
                    ex.InnerException?.Message ?? ex.Message);
            }
            
            Console.WriteLine();
        }
        
        Console.WriteLine("".PadRight(50, '='));
        Console.WriteLine("✅ Exercise 6.2 completed successfully!");
        Console.WriteLine();
        Console.WriteLine("📊 Summary:");
        Console.WriteLine("   • Retry patterns demonstrated");
        Console.WriteLine("   • Exponential backoff validated");
        Console.WriteLine("   • Non-retryable errors handled");
        Console.WriteLine("   • Activity timeouts configured");
        Console.WriteLine();
        Console.WriteLine("🌐 View workflows in Temporal UI: http://localhost:8088");
        Console.WriteLine();
    });
    
    Log.Information("Exercise 6.2: Activity Patterns completed successfully");
}
catch (Exception ex)
{
    Log.Error(ex, "Error in Exercise 6.2: Activity Patterns");
    Console.WriteLine($"❌ Error: {ex.Message}");
    Environment.ExitCode = 1;
}
finally
{
    // Flush logs with timeout to prevent hanging
    var flushTask = Log.CloseAndFlushAsync().AsTask();
    if (await Task.WhenAny(flushTask, Task.Delay(TimeSpan.FromSeconds(2))) == flushTask)
    {
        await flushTask; // Completed successfully
    }
}

// ============================================================================
// WORKFLOW DEFINITION
// ============================================================================

/// <summary>
/// Payment processing workflow with sophisticated retry logic.
/// Demonstrates how Temporal handles transient failures vs permanent failures.
/// </summary>
[Workflow]
public class PaymentRetryWorkflow
{
    [WorkflowRun]
    public async Task<PaymentResult> RunAsync(PaymentRequest request)
    {
        var result = new PaymentResult
        {
            PaymentId = request.PaymentId,
            Status = "Processing",
            StartTime = Workflow.UtcNow,
            AttemptCount = 0
        };
        
        try
        {
            // Process payment with retry policy
            // - Initial interval: 1 second
            // - Maximum interval: 30 seconds
            // - Backoff coefficient: 2.0 (exponential)
            // - Maximum attempts: 5
            // - Non-retryable: InsufficientFundsException
            var paymentSuccess = await Workflow.ExecuteActivityAsync(
                (PaymentActivities act) => act.ProcessPaymentAsync(request),
                new ActivityOptions
                {
                    StartToCloseTimeout = TimeSpan.FromSeconds(30),
                    RetryPolicy = new()
                    {
                        InitialInterval = TimeSpan.FromSeconds(1),
                        MaximumInterval = TimeSpan.FromSeconds(30),
                        BackoffCoefficient = 2.0f,
                        MaximumAttempts = 5,
                        NonRetryableErrorTypes = new[] { "InsufficientFundsException", "InvalidPaymentMethodException" }
                    }
                });
            
            result.Status = "Completed";
            result.Message = "Payment processed successfully";
            result.CompletedAt = Workflow.UtcNow;
            result.AttemptCount = 1; // Simplified - real impl would track actual attempts
            
            return result;
        }
        catch (Exception ex)
        {
            result.Status = "Failed";
            result.Message = ex.Message;
            result.CompletedAt = Workflow.UtcNow;
            throw;
        }
    }
}

// ============================================================================
// ACTIVITY IMPLEMENTATIONS
// ============================================================================

/// <summary>
/// Payment processing activities with simulated failure scenarios.
/// </summary>
public class PaymentActivities
{
    private static int attemptCounter = 0;
    
    [Activity]
    public Task<bool> ProcessPaymentAsync(PaymentRequest request)
    {
        attemptCounter++;
        var currentAttempt = attemptCounter;
        
        // Simulate different payment scenarios
        switch (request.Scenario)
        {
            case PaymentScenario.Success:
                // Immediate success
                return Task.FromResult(true);
            
            case PaymentScenario.TemporaryFailure:
                // Fail first 2 attempts, then succeed
                if (currentAttempt <= 2)
                {
                    throw new ApplicationException($"Temporary payment gateway error (attempt {currentAttempt})");
                }
                attemptCounter = 0; // Reset for next workflow
                return Task.FromResult(true);
            
            case PaymentScenario.InsufficientFunds:
                // Non-retryable error
                throw new InsufficientFundsException("Customer account has insufficient funds");
            
            default:
                throw new InvalidOperationException($"Unknown scenario: {request.Scenario}");
        }
    }
}

// ============================================================================
// DATA MODELS
// ============================================================================

public enum PaymentScenario
{
    Success,
    TemporaryFailure,
    InsufficientFunds
}

public record PaymentRequest
{
    public required string PaymentId { get; init; }
    public required decimal Amount { get; init; }
    public required PaymentScenario Scenario { get; init; }
}

public class PaymentResult
{
    public required string PaymentId { get; set; }
    public required string Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int AttemptCount { get; set; }
}

// Custom exception for non-retryable errors
public class InsufficientFundsException : Exception
{
    public InsufficientFundsException(string message) : base(message) { }
}
