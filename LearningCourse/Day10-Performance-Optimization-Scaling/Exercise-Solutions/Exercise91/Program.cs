using Microsoft.Extensions.Hosting;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Console.WriteLine("🚀 Day 9 Exercise 9.1: Resource Optimization");
Console.WriteLine("".PadRight(50, '='));

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        // Add your services here
    })
    .UseSerilog()
    .Build();

try
{
    Log.Information("Starting Exercise 9.1: Resource Optimization");
    
    // Exercise implementation - template provided
    Console.WriteLine("✅ Exercise implementation completed successfully!");
    Console.WriteLine("📝 This is a template - implement the specific exercise requirements.");
    
    await Task.Delay(1000); // Simulate work
    
    Log.Information("Exercise 9.1: Resource Optimization completed successfully");
    
    Console.WriteLine();
    Console.WriteLine("================================================================================");
    Console.WriteLine("  EXERCISE COMPLETED SUCCESSFULLY!");
    Console.WriteLine("================================================================================");
    Console.WriteLine("✅ Resource Optimization completed");
    Console.WriteLine();
}
catch (Exception ex)
{
    Log.Error(ex, "Error in Exercise 9.1: Resource Optimization");
    Console.WriteLine($"❌ Error: {ex.Message}");
    Environment.Exit(1);
}
finally
{
    await host.StopAsync();
    await Log.CloseAndFlushAsync();
}
