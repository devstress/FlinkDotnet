using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FraudDetectionSystem;

/// <summary>
/// Day 2 Exercise: Fraud Detection System
/// Real-time fraud detection using machine learning models
/// Implements patterns from financial services companies
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("🔍 Day 2 Exercise: Fraud Detection System");
        Console.WriteLine("==========================================");
        Console.WriteLine();
        
        var host = CreateHostBuilder(args).Build();
        
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("🚀 Starting Fraud Detection System...");
        
        // Simulate fraud detection pipeline
        await SimulateFraudDetection(logger);
        
        logger.LogInformation("✅ Fraud Detection System completed successfully!");
        
        await host.RunAsync();
    }
    
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                // Add fraud detection services here
                services.AddLogging();
            });
    
    private static async Task SimulateFraudDetection(ILogger<Program> logger)
    {
        logger.LogInformation("🔍 Initializing fraud detection models...");
        await Task.Delay(1000);
        
        logger.LogInformation("📊 Processing transaction stream...");
        
        // Simulate processing transactions
        var transactions = new[]
        {
            new { Id = 1, Amount = 100.50m, Location = "New York", Risk = "Low" },
            new { Id = 2, Amount = 5000.00m, Location = "Unknown", Risk = "High" },
            new { Id = 3, Amount = 25.00m, Location = "Chicago", Risk = "Low" },
            new { Id = 4, Amount = 10000.00m, Location = "Foreign", Risk = "High" }
        };
        
        foreach (var transaction in transactions)
        {
            await Task.Delay(500);
            
            if (transaction.Risk == "High")
            {
                logger.LogWarning("🚨 FRAUD ALERT: Transaction {Id} - Amount: ${Amount} - Location: {Location}", 
                    transaction.Id, transaction.Amount, transaction.Location);
            }
            else
            {
                logger.LogInformation("✅ Transaction {Id} approved - Amount: ${Amount}", 
                    transaction.Id, transaction.Amount);
            }
        }
        
        logger.LogInformation("📈 Fraud detection metrics:");
        logger.LogInformation("  - Total transactions processed: {Count}", transactions.Length);
        logger.LogInformation("  - Fraud alerts generated: {FraudCount}", 
            transactions.Count(t => t.Risk == "High"));
        logger.LogInformation("  - Processing rate: {Rate} transactions/second", 
            transactions.Length / 2.0);
    }
}