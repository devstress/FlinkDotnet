using Serilog;
using Temporalio.Client;
using Temporalio.Worker;
using Temporalio.Workflows;
using Temporalio.Activities;

// Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Console.WriteLine("🚀 Day 6 Exercise 6.4: Advanced Workflow Patterns");
Console.WriteLine("".PadRight(50, '='));
Console.WriteLine();

try
{
    Log.Information("Starting Exercise 6.4: Signals, Queries & Child Workflows");
    
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
    const string taskQueue = "support-ticket-queue";
    using var worker = new TemporalWorker(
        client,
        new TemporalWorkerOptions(taskQueue)
            .AddWorkflow<SupportTicketWorkflow>()
            .AddAllActivities(new SupportActivities()));
    
    Console.WriteLine();
    Console.WriteLine("┌─────────────────────────────────────────────────────────────┐");
    Console.WriteLine("│ Exercise 6.4: Advanced Temporal Patterns                   │");
    Console.WriteLine("│                                                             │");
    Console.WriteLine("│ Demonstrates:                                               │");
    Console.WriteLine("│ • Workflow signals (external events)                       │");
    Console.WriteLine("│ • Workflow queries (state inspection)                      │");
    Console.WriteLine("│ • WaitCondition (long-running waits)                       │");
    Console.WriteLine("│ • Dynamic workflow behavior                                │");
    Console.WriteLine("└─────────────────────────────────────────────────────────────┘");
    Console.WriteLine();
    
    // Start worker and execute workflows
    await worker.ExecuteAsync(async () =>
    {
        Log.Information("🔄 Temporal worker started on task queue: {TaskQueue}", taskQueue);
        
        var ticketRequest = new SupportTicketRequest
        {
            TicketId = "TICKET-001",
            CustomerName = "Alice Johnson",
            Issue = "Account access problem",
            Priority = TicketPriority.Normal
        };
        
        var workflowId = $"support-ticket-{ticketRequest.TicketId}";
        
        Console.WriteLine("🎫 Creating support ticket workflow...");
        Console.WriteLine("   Ticket ID: {0}", ticketRequest.TicketId);
        Console.WriteLine("   Customer: {0}", ticketRequest.CustomerName);
        Console.WriteLine("   Priority: {0}", ticketRequest.Priority);
        Console.WriteLine();
        
        var handle = await client.StartWorkflowAsync(
            (SupportTicketWorkflow wf) => wf.RunAsync(ticketRequest),
            new WorkflowOptions(id: workflowId, taskQueue: taskQueue));
        
        Console.WriteLine("✅ Workflow started: {0}", workflowId);
        Console.WriteLine();
        
        // Wait for initial processing
        await Task.Delay(1000);
        
        // Query: Get current status
        Console.WriteLine("📊 Querying workflow status...");
        var status = await handle.QueryAsync(wf => wf.GetStatus());
        Console.WriteLine("   Current Status: {0}", status);
        Console.WriteLine();
        
        // Signal: Add a comment
        Console.WriteLine("💬 Adding comment via signal...");
        await handle.SignalAsync(wf => wf.AddComment("Agent reviewed the issue"));
        await Task.Delay(500);
        
        // Signal: Escalate priority
        Console.WriteLine("⬆️  Escalating priority via signal...");
        await handle.SignalAsync(wf => wf.UpdatePriority(TicketPriority.High));
        await Task.Delay(500);
        
        // Query: Get updated status
        Console.WriteLine("📊 Querying updated status...");
        status = await handle.QueryAsync(wf => wf.GetStatus());
        Console.WriteLine("   Updated Status: {0}", status);
        Console.WriteLine();
        
        // Query: Get history
        Console.WriteLine("📜 Querying workflow history...");
        var history = await handle.QueryAsync(wf => wf.GetHistory());
        Console.WriteLine("   Event History:");
        foreach (var entry in history)
        {
            Console.WriteLine("      • {0}", entry);
        }
        Console.WriteLine();
        
        // Signal: Resolve the ticket
        Console.WriteLine("✅ Resolving ticket via signal...");
        await handle.SignalAsync(wf => wf.ResolveTicket("Issue resolved - password reset"));
        
        // Wait for workflow completion
        Console.WriteLine("⏳ Waiting for workflow completion...");
        var result = await handle.GetResultAsync();
        
        Console.WriteLine();
        Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║ Workflow Execution Result                                 ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
        Console.WriteLine("Ticket ID:       {0}", result.TicketId);
        Console.WriteLine("Final Status:    {0}", result.Status);
        Console.WriteLine("Final Priority:  {0}", result.Priority);
        Console.WriteLine("Resolution:      {0}", result.Resolution);
        Console.WriteLine("Event Count:     {0}", result.EventHistory.Count);
        Console.WriteLine();
        
        Console.WriteLine("".PadRight(50, '='));
        Console.WriteLine("✅ Exercise 6.4 completed successfully!");
        Console.WriteLine();
        Console.WriteLine("📊 Summary:");
        Console.WriteLine("   • Signals demonstrated (AddComment, UpdatePriority, Resolve)");
        Console.WriteLine("   • Queries demonstrated (GetStatus, GetHistory)");
        Console.WriteLine("   • WaitCondition used for resolution wait");
        Console.WriteLine("   • Dynamic workflow state management");
        Console.WriteLine();
        Console.WriteLine("🌐 View workflow in Temporal UI: http://localhost:8088");
        Console.WriteLine();
    });
    
    Log.Information("Exercise 6.4: Advanced Patterns completed successfully");
}
catch (Exception ex)
{
    Log.Error(ex, "Error in Exercise 6.4: Advanced Patterns");
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
/// Support ticket workflow demonstrating signals and queries.
/// Workflow can be modified externally via signals while running,
/// and its state can be inspected via queries at any time.
/// </summary>
[Workflow]
public class SupportTicketWorkflow
{
    private TicketPriority currentPriority = TicketPriority.Normal;
    private string currentStatus = "Open";
    private readonly List<string> eventHistory = new();
    private bool resolved = false;
    private string? resolution;
    
    [WorkflowRun]
    public async Task<TicketResult> RunAsync(SupportTicketRequest request)
    {
        currentPriority = request.Priority;
        eventHistory.Add($"Ticket created: {request.Issue}");
        
        // Step 1: Create ticket
        await Workflow.ExecuteActivityAsync(
            (SupportActivities act) => act.CreateTicketAsync(request.TicketId),
            new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(30) });
        
        currentStatus = "Assigned";
        eventHistory.Add("Ticket assigned to agent");
        
        // Step 2: Wait for resolution (or timeout after 30 seconds)
        var resolveTask = Workflow.WaitConditionAsync(() => resolved, TimeSpan.FromSeconds(30));
        await resolveTask;
        
        if (resolved && resolution != null)
        {
            currentStatus = "Resolved";
            eventHistory.Add($"Ticket resolved: {resolution}");
            
            // Step 3: Close ticket
            await Workflow.ExecuteActivityAsync(
                (SupportActivities act) => act.CloseTicketAsync(request.TicketId),
                new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(30) });
            
            currentStatus = "Closed";
            eventHistory.Add("Ticket closed");
        }
        else
        {
            currentStatus = "Timed Out";
            eventHistory.Add("Ticket timed out waiting for resolution");
        }
        
        return new TicketResult
        {
            TicketId = request.TicketId,
            Status = currentStatus,
            Priority = currentPriority,
            Resolution = resolution ?? "No resolution provided",
            EventHistory = new List<string>(eventHistory)
        };
    }
    
    // === SIGNALS: External events that modify workflow state ===
    
    [WorkflowSignal]
    public Task AddComment(string comment)
    {
        eventHistory.Add($"Comment added: {comment}");
        return Task.CompletedTask;
    }
    
    [WorkflowSignal]
    public Task UpdatePriority(TicketPriority newPriority)
    {
        var oldPriority = currentPriority;
        currentPriority = newPriority;
        eventHistory.Add($"Priority changed: {oldPriority} → {newPriority}");
        return Task.CompletedTask;
    }
    
    [WorkflowSignal]
    public Task ResolveTicket(string resolutionDetails)
    {
        resolution = resolutionDetails;
        resolved = true;
        return Task.CompletedTask;
    }
    
    // === QUERIES: Non-blocking state inspection ===
    
    [WorkflowQuery]
    public string GetStatus() => currentStatus;
    
    [WorkflowQuery]
    public List<string> GetHistory() => new(eventHistory);
    
    [WorkflowQuery]
    public TicketPriority GetPriority() => currentPriority;
}

// ============================================================================
// ACTIVITY IMPLEMENTATIONS
// ============================================================================

/// <summary>
/// Support ticket activities.
/// </summary>
public class SupportActivities
{
    [Activity]
    public Task CreateTicketAsync(string ticketId)
    {
        // Simulate ticket creation
        return Task.Delay(Random.Shared.Next(100, 300));
    }
    
    [Activity]
    public Task CloseTicketAsync(string ticketId)
    {
        // Simulate ticket closure
        return Task.Delay(Random.Shared.Next(100, 300));
    }
}

// ============================================================================
// DATA MODELS
// ============================================================================

public enum TicketPriority
{
    Low,
    Normal,
    High,
    Critical
}

public record SupportTicketRequest
{
    public required string TicketId { get; init; }
    public required string CustomerName { get; init; }
    public required string Issue { get; init; }
    public required TicketPriority Priority { get; init; }
}

public class TicketResult
{
    public required string TicketId { get; set; }
    public required string Status { get; set; }
    public required TicketPriority Priority { get; set; }
    public required string Resolution { get; set; }
    public required List<string> EventHistory { get; set; }
}
