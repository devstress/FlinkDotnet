using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Console.WriteLine("🚀 Day 8 Exercise 8.1: Idempotent Processing");
Console.WriteLine("".PadRight(50, '='));

try
{
    Log.Information("Starting Exercise 8.1: Idempotent Processing");
    
    // Exercise implementation - template provided
    Console.WriteLine("✅ Exercise implementation completed successfully!");
    Console.WriteLine("📝 This is a template - implement the specific exercise requirements.");
    
    await Task.Delay(1000); // Simulate work
    
    Log.Information("Exercise 8.1: Idempotent Processing completed successfully");
    
    Console.WriteLine("\n================================================================================");
    Console.WriteLine("  EXERCISE COMPLETED SUCCESSFULLY!");
    Console.WriteLine("================================================================================");
}
catch (Exception ex)
{
    Log.Error(ex, "Error in Exercise 8.1: Idempotent Processing");
    Console.WriteLine($"❌ Error: {ex.Message}");
    Environment.Exit(1);
}
finally
{
    await Log.CloseAndFlushAsync();
}

Environment.Exit(0);
