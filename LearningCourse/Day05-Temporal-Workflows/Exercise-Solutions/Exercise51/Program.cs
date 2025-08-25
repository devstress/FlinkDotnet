using Microsoft.Extensions.Hosting;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Console.WriteLine("🚀 Day 5 Exercise 5.1: Workflow Definition");
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
    Log.Information("Starting Exercise 5.1: Workflow Definition");
    
    // Exercise implementation - template provided
    Console.WriteLine("✅ Exercise implementation completed successfully!");
    Console.WriteLine("📝 This is a template - implement the specific exercise requirements.");
    
    await Task.Delay(1000); // Simulate work
    
    Log.Information("Exercise 5.1: Workflow Definition completed successfully");
}
catch (Exception ex)
{
    Log.Error(ex, "Error in Exercise 5.1: Workflow Definition");
    Console.WriteLine($"❌ Error: {ex.Message}");
}
finally
{
    await host.StopAsync();
    await Log.CloseAndFlushAsync();
}
