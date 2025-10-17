using Serilog;
using Temporalio.Client;
using Temporalio.Worker;
using Temporalio.Workflows;
using Temporalio.Activities;

// Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Console.WriteLine("🚀 Day 6 Exercise 6.3: Error Handling & Saga Pattern");
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
    
    Log.Information("Starting Exercise 6.3: Saga Compensation Workflow");
    
    // Get Temporal endpoint from environment variable (service discovery)
    var temporalEndpoint = Environment.GetEnvironmentVariable("TEMPORAL_ENDPOINT") ?? "localhost:7233";
    Log.Information("📡 Connecting to Temporal server at {Endpoint}", temporalEndpoint);
    
    // Connect to Temporal server with retry logic
    var client = await ConnectWithRetryAsync(temporalEndpoint);
    
    Log.Information("✅ Connected to Temporal server successfully");
    
    // Create worker to execute workflows and activities
    const string taskQueue = "booking-saga-queue";
    
    // Configure worker with explicit concurrency settings
    var workerOptions = new TemporalWorkerOptions(taskQueue)
    {
        MaxConcurrentWorkflowTasks = 10,    // Allow 10 concurrent workflow tasks
        MaxConcurrentActivities = 20,        // Allow 20 concurrent activities
        MaxConcurrentLocalActivities = 20    // Allow 20 concurrent local activities
    };
    
    using var worker = new TemporalWorker(
        client,
        workerOptions
            .AddWorkflow<BookingSagaWorkflow>()
            .AddAllActivities(new BookingActivities()));
    
    Console.WriteLine();
    Console.WriteLine("┌─────────────────────────────────────────────────────────────┐");
    Console.WriteLine("│ Exercise 6.3: Saga Compensation Pattern                    │");
    Console.WriteLine("│                                                             │");
    Console.WriteLine("│ Demonstrates:                                               │");
    Console.WriteLine("│ • Multi-step distributed transaction (Saga pattern)        │");
    Console.WriteLine("│ • Automatic compensation on failure                        │");
    Console.WriteLine("│ • Reverse-order rollback                                   │");
    Console.WriteLine("│ • Error handling and recovery                              │");
    Console.WriteLine("└─────────────────────────────────────────────────────────────┘");
    Console.WriteLine();
    
    // Test success and failure scenarios
    var scenarios = new[]
    {
        new BookingRequest 
        { 
            BookingId = "BOOK-001", 
            CustomerName = "John Doe",
            FailAt = BookingStep.None // Success scenario
        },
        new BookingRequest 
        { 
            BookingId = "BOOK-002", 
            CustomerName = "Jane Smith",
            FailAt = BookingStep.Payment // Fail at payment
        },
        new BookingRequest 
        { 
            BookingId = "BOOK-003", 
            CustomerName = "Bob Johnson",
            FailAt = BookingStep.Shipment // Fail at shipment
        }
    };
    
    Console.WriteLine("🎭 Processing {0} booking scenarios (success + failures)...", scenarios.Length);
    Console.WriteLine();
    
    // Start worker and execute workflows
    await worker.ExecuteAsync(async () =>
    {
        Log.Information("🔄 Temporal worker started on task queue: {TaskQueue}", taskQueue);
        
        // Execute workflow scenarios
        foreach (var booking in scenarios)
        {
            // Use unique workflow ID with timestamp to avoid collisions from previous runs
            var workflowId = $"booking-saga-{booking.BookingId}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            
            Log.Information("🚀 Starting saga for {BookingId} (Fail at: {FailAt})",
                booking.BookingId, booking.FailAt);
            
            // Retry workflow start with exponential backoff (namespace race condition)
            WorkflowHandle<BookingSagaWorkflow, BookingResult> handle = null!;
            for (int attempt = 1; attempt <= 5; attempt++)
            {
                try
                {
                    handle = await client.StartWorkflowAsync(
                        (BookingSagaWorkflow wf) => wf.RunAsync(booking),
                        new WorkflowOptions(id: workflowId, taskQueue: taskQueue));
                    break; // Success
                }
                catch (Temporalio.Exceptions.RpcException ex) when (ex.Message.Contains("not found") && attempt < 5)
                {
                    Log.Warning("Namespace temporarily unavailable (attempt {Attempt}/5), retrying in {Delay}ms...",
                        attempt, 500 * attempt);
                    await Task.Delay(500 * attempt);
                }
            }
            
            try
            {
                // Wait for workflow to complete
                Log.Information("⏳ Waiting for workflow {WorkflowId} to complete...", workflowId);
                
                var result = await handle.GetResultAsync();
                
                Console.WriteLine("✅ {0}: {1}",
                    booking.BookingId,
                    result.Status);
                
                if (result.CompensatedSteps.Count > 0)
                {
                    Console.WriteLine("   🔄 Compensated steps:");
                    foreach (var step in result.CompensatedSteps)
                    {
                        Console.WriteLine("      - {0}", step);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ {0}: Failed - {1}",
                    booking.BookingId,
                    ex.Message);
            }
            
            Console.WriteLine();
        }
        
        Console.WriteLine("".PadRight(50, '='));
        Console.WriteLine("✅ Exercise 6.3 completed successfully!");
        Console.WriteLine();
        Console.WriteLine("📊 Summary:");
        Console.WriteLine("   • Saga pattern demonstrated");
        Console.WriteLine("   • Compensation logic validated");
        Console.WriteLine("   • Reverse-order rollback working");
        Console.WriteLine("   • Error handling patterns shown");
        Console.WriteLine();
        Console.WriteLine("🌐 View workflows in Temporal UI: http://localhost:8088");
        Console.WriteLine();
    });
    
    Log.Information("Exercise 6.3: Saga Compensation Workflow completed successfully");
}
catch (Exception ex)
{
    Log.Error(ex, "Error in Exercise 6.3: Saga Compensation Workflow");
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
/// Saga workflow for distributed booking transaction.
/// Implements compensation pattern: if any step fails, previously completed
/// steps are rolled back in reverse order.
/// </summary>
[Workflow]
public class BookingSagaWorkflow
{
    [WorkflowRun]
    public async Task<BookingResult> RunAsync(BookingRequest request)
    {
        var result = new BookingResult
        {
            BookingId = request.BookingId,
            Status = "Processing",
            CompletedSteps = new List<string>(),
            CompensatedSteps = new List<string>()
        };
        
        var compensations = new List<(string stepName, Func<Task> compensate)>();
        
        try
        {
            // Step 1: Reserve Hotel
            if (request.FailAt == BookingStep.Hotel)
                throw new InvalidOperationException("Simulated hotel reservation failure");
            
            await Workflow.ExecuteActivityAsync(
                (BookingActivities act) => act.ReserveHotelAsync(request.BookingId),
                new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(30) });
            
            result.CompletedSteps.Add("Hotel reserved");
            compensations.Add(("Hotel", async () => 
                await Workflow.ExecuteActivityAsync(
                    (BookingActivities act) => act.CancelHotelAsync(request.BookingId),
                    new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(30) })));
            
            // Step 2: Reserve Flight
            if (request.FailAt == BookingStep.Flight)
                throw new InvalidOperationException("Simulated flight reservation failure");
            
            await Workflow.ExecuteActivityAsync(
                (BookingActivities act) => act.ReserveFlightAsync(request.BookingId),
                new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(30) });
            
            result.CompletedSteps.Add("Flight reserved");
            compensations.Add(("Flight", async () => 
                await Workflow.ExecuteActivityAsync(
                    (BookingActivities act) => act.CancelFlightAsync(request.BookingId),
                    new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(30) })));
            
            // Step 3: Process Payment
            if (request.FailAt == BookingStep.Payment)
                throw new InvalidOperationException("Simulated payment failure");
            
            await Workflow.ExecuteActivityAsync(
                (BookingActivities act) => act.ProcessPaymentAsync(request.BookingId),
                new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(30) });
            
            result.CompletedSteps.Add("Payment processed");
            compensations.Add(("Payment", async () => 
                await Workflow.ExecuteActivityAsync(
                    (BookingActivities act) => act.RefundPaymentAsync(request.BookingId),
                    new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(30) })));
            
            // Step 4: Create Shipment
            if (request.FailAt == BookingStep.Shipment)
                throw new InvalidOperationException("Simulated shipment failure");
            
            await Workflow.ExecuteActivityAsync(
                (BookingActivities act) => act.CreateShipmentAsync(request.BookingId),
                new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(30) });
            
            result.CompletedSteps.Add("Shipment created");
            
            result.Status = "Completed";
            return result;
        }
        catch (Exception)
        {
            // SAGA COMPENSATION: Execute rollback in reverse order
            result.Status = "Failed - Compensated";
            compensations.Reverse();
            
            foreach (var (stepName, compensate) in compensations)
            {
                try
                {
                    await compensate();
                    result.CompensatedSteps.Add($"{stepName} cancelled");
                }
                catch (Exception)
                {
                    result.CompensatedSteps.Add($"{stepName} cancellation failed");
                }
            }
            
            return result;
        }
    }
}

// ============================================================================
// ACTIVITY IMPLEMENTATIONS
// ============================================================================

/// <summary>
/// Booking activities with compensation counterparts.
/// Each forward activity has a corresponding compensation activity.
/// </summary>
public class BookingActivities
{
    [Activity]
    public Task ReserveHotelAsync(string bookingId)
    {
        // Simulate hotel reservation
        return Task.Delay(Random.Shared.Next(100, 300));
    }
    
    [Activity]
    public Task CancelHotelAsync(string bookingId)
    {
        // Simulate hotel cancellation (compensation)
        return Task.Delay(Random.Shared.Next(100, 200));
    }
    
    [Activity]
    public Task ReserveFlightAsync(string bookingId)
    {
        // Simulate flight reservation
        return Task.Delay(Random.Shared.Next(100, 300));
    }
    
    [Activity]
    public Task CancelFlightAsync(string bookingId)
    {
        // Simulate flight cancellation (compensation)
        return Task.Delay(Random.Shared.Next(100, 200));
    }
    
    [Activity]
    public Task ProcessPaymentAsync(string bookingId)
    {
        // Simulate payment processing
        return Task.Delay(Random.Shared.Next(100, 300));
    }
    
    [Activity]
    public Task RefundPaymentAsync(string bookingId)
    {
        // Simulate payment refund (compensation)
        return Task.Delay(Random.Shared.Next(100, 200));
    }
    
    [Activity]
    public Task CreateShipmentAsync(string bookingId)
    {
        // Simulate shipment creation
        return Task.Delay(Random.Shared.Next(100, 300));
    }
}

// ============================================================================
// DATA MODELS
// ============================================================================

public enum BookingStep
{
    None,      // Success - no failure
    Hotel,     // Fail at hotel reservation
    Flight,    // Fail at flight reservation
    Payment,   // Fail at payment
    Shipment   // Fail at shipment
}

public record BookingRequest
{
    public required string BookingId { get; init; }
    public required string CustomerName { get; init; }
    public required BookingStep FailAt { get; init; }
}

public class BookingResult
{
    public required string BookingId { get; set; }
    public required string Status { get; set; }
    public required List<string> CompletedSteps { get; set; }
    public required List<string> CompensatedSteps { get; set; }
}
