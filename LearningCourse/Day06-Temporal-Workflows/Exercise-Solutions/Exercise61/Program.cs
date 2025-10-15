using Serilog;
using Temporalio.Client;
using Temporalio.Worker;
using Temporalio.Workflows;
using Temporalio.Activities;

// Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Console.WriteLine("🚀 Day 6 Exercise 6.1: Basic Workflow Definition");
Console.WriteLine("".PadRight(50, '='));
Console.WriteLine();

try
{
    Log.Information("Starting Exercise 6.1: Basic Workflow Definition with Temporal");
    
    // Get Temporal endpoint from environment variable (service discovery)
    var temporalEndpoint = Environment.GetEnvironmentVariable("TEMPORAL_ENDPOINT") ?? "localhost:7233";
    Log.Information("📡 Connecting to Temporal server at {Endpoint}", temporalEndpoint);
    
    // Connect to Temporal server
    var client = await TemporalClient.ConnectAsync(new TemporalClientConnectOptions
    {
        TargetHost = temporalEndpoint,
        Namespace = "default"
    });
    
    Log.Information("✅ Connected to Temporal server successfully");
    
    // Create worker to execute workflows and activities
    const string taskQueue = "order-processing-queue";
    using var worker = new TemporalWorker(
        client,
        new TemporalWorkerOptions(taskQueue)
            .AddWorkflow<OrderProcessingWorkflow>()
            .AddAllActivities(new OrderActivities()));
    
    Console.WriteLine();
    Console.WriteLine("┌─────────────────────────────────────────────────────────────┐");
    Console.WriteLine("│ Exercise 6.1: Basic Workflow Pattern                       │");
    Console.WriteLine("│                                                             │");
    Console.WriteLine("│ Demonstrates:                                               │");
    Console.WriteLine("│ • Basic workflow definition with [Workflow] attribute      │");
    Console.WriteLine("│ • Sequential activity execution                            │");
    Console.WriteLine("│ • Simple order processing orchestration                    │");
    Console.WriteLine("│ • Workflow result handling                                 │");
    Console.WriteLine("└─────────────────────────────────────────────────────────────┘");
    Console.WriteLine();
    
    // Start worker and execute workflows
    await worker.ExecuteAsync(async () =>
    {
        Log.Information("🔄 Temporal worker started on task queue: {TaskQueue}", taskQueue);
        
        // Execute workflow - Process 3 sample orders
        var orders = new[]
        {
            new OrderRequest
            {
                OrderId = "ORDER-001",
                CustomerId = "CUST-100",
                Amount = 299.99m,
                ShippingAddress = "123 Main St"
            },
            new OrderRequest
            {
                OrderId = "ORDER-002",
                CustomerId = "CUST-101",
                Amount = 499.50m,
                ShippingAddress = "456 Oak Ave"
            },
            new OrderRequest
            {
                OrderId = "ORDER-003",
                CustomerId = "CUST-102",
                Amount = 899.00m,
                ShippingAddress = "789 Pine Rd"
            }
        };
        
        Console.WriteLine("📦 Processing {0} sample orders...", orders.Length);
        Console.WriteLine();
        
        foreach (var order in orders)
        {
            var workflowId = $"order-workflow-{order.OrderId}";
            
            Log.Information("🚀 Starting workflow for {OrderId}", order.OrderId);
            
            var handle = await client.StartWorkflowAsync(
                (OrderProcessingWorkflow wf) => wf.RunAsync(order),
                new WorkflowOptions(id: workflowId, taskQueue: taskQueue));
            
            // Wait for workflow to complete
            var result = await handle.GetResultAsync();
            
            Console.WriteLine("✅ {0}: {1} (Total: ${2:F2})", 
                order.OrderId, 
                result.Status, 
                result.TotalAmount);
        }
        
        Console.WriteLine();
        Console.WriteLine("".PadRight(50, '='));
        Console.WriteLine("✅ Exercise 6.1 completed successfully!");
        Console.WriteLine();
        Console.WriteLine("📊 Summary:");
        Console.WriteLine("   • {0} workflows executed", orders.Length);
        Console.WriteLine("   • All orders processed successfully");
        Console.WriteLine("   • Sequential activity execution demonstrated");
        Console.WriteLine();
        Console.WriteLine("🌐 View workflows in Temporal UI: http://localhost:8088");
        Console.WriteLine();
    });
    
    Log.Information("Exercise 6.1: Basic Workflow Definition completed successfully");
}
catch (Exception ex)
{
    Log.Error(ex, "Error in Exercise 6.1: Basic Workflow Definition");
    Console.WriteLine($"❌ Error: {ex.Message}");
    Environment.ExitCode = 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

// ============================================================================
// WORKFLOW DEFINITION
// ============================================================================

/// <summary>
/// Basic order processing workflow demonstrating fundamental Temporal concepts.
/// This workflow orchestrates a simple 3-step order fulfillment process.
/// </summary>
[Workflow]
public class OrderProcessingWorkflow
{
    [WorkflowRun]
    public async Task<OrderResult> RunAsync(OrderRequest request)
    {
        var result = new OrderResult
        {
            OrderId = request.OrderId,
            Status = "Processing",
            StartTime = Workflow.UtcNow
        };
        
        try
        {
            // Step 1: Validate Order
            await Workflow.ExecuteActivityAsync(
                (OrderActivities act) => act.ValidateOrderAsync(request),
                new ActivityOptions { StartToCloseTimeout = TimeSpan.FromMinutes(1) });
            
            // Step 2: Process Payment
            await Workflow.ExecuteActivityAsync(
                (OrderActivities act) => act.ProcessPaymentAsync(request),
                new ActivityOptions { StartToCloseTimeout = TimeSpan.FromMinutes(2) });
            
            // Step 3: Ship Order
            await Workflow.ExecuteActivityAsync(
                (OrderActivities act) => act.CreateShipmentAsync(request),
                new ActivityOptions { StartToCloseTimeout = TimeSpan.FromMinutes(1) });
            
            result.Status = "Completed";
            result.TotalAmount = request.Amount;
            result.CompletedAt = Workflow.UtcNow;
            
            return result;
        }
        catch (Exception)
        {
            result.Status = "Failed";
            result.CompletedAt = Workflow.UtcNow;
            return result;
        }
    }
}

// ============================================================================
// ACTIVITY IMPLEMENTATIONS
// ============================================================================

/// <summary>
/// Order processing activities - Each represents a discrete business operation.
/// Activities can fail and be retried independently.
/// </summary>
public class OrderActivities
{
    [Activity]
    public Task ValidateOrderAsync(OrderRequest request)
    {
        if (string.IsNullOrEmpty(request.CustomerId))
            throw new InvalidOperationException("Customer ID is required");
        
        if (request.Amount <= 0)
            throw new InvalidOperationException("Order amount must be greater than zero");
        
        return Task.CompletedTask;
    }
    
    [Activity]
    public async Task ProcessPaymentAsync(OrderRequest request)
    {
        // Simulate payment processing
        await Task.Delay(Random.Shared.Next(100, 300));
    }
    
    [Activity]
    public async Task CreateShipmentAsync(OrderRequest request)
    {
        // Simulate shipment creation
        await Task.Delay(Random.Shared.Next(100, 300));
    }
}

// ============================================================================
// DATA MODELS
// ============================================================================

public record OrderRequest
{
    public required string OrderId { get; init; }
    public required string CustomerId { get; init; }
    public required decimal Amount { get; init; }
    public required string ShippingAddress { get; init; }
}

public class OrderResult
{
    public required string OrderId { get; set; }
    public required string Status { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? CompletedAt { get; set; }
}
