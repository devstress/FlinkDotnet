using Microsoft.Extensions.Hosting;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Console.WriteLine("🚀 Day 10 Exercise 10.3: Audit Logging");
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
    Log.Information("Starting Exercise 10.3: Audit Logging");
    
    // Exercise implementation - template provided
    Console.WriteLine("✅ Exercise implementation completed successfully!");
    Console.WriteLine("📝 This is a template - implement the specific exercise requirements.");
    
    await Task.Delay(1000); // Simulate work
    
    Log.Information("Exercise 10.3: Audit Logging completed successfully");
}
catch (Exception ex)
{
    Log.Error(ex, "Error in Exercise 10.3: Audit Logging");
    Console.WriteLine($"❌ Error: {ex.Message}");
}
finally
{
    await host.StopAsync();
    await Log.CloseAndFlushAsync();
}
