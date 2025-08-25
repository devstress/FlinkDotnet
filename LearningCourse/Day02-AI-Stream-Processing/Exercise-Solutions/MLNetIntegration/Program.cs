using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.Extensions.Hosting;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Console.WriteLine("🤖 Day 2 Exercise 2.1: ML.NET Integration with Streaming");
Console.WriteLine("=======================================================");

// Create host for dependency injection
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<MLContext>();
        services.AddSingleton<FraudDetectionService>();
        services.AddSingleton<StreamingInferenceEngine>();
    })
    .UseSerilog()
    .Build();

var mlContext = host.Services.GetRequiredService<MLContext>();
var fraudDetectionService = host.Services.GetRequiredService<FraudDetectionService>();
var inferenceEngine = host.Services.GetRequiredService<StreamingInferenceEngine>();

try
{
    Console.WriteLine("🔧 Initializing ML.NET fraud detection model...");
    
    // Train or load the fraud detection model
    await fraudDetectionService.InitializeModelAsync();
    
    Console.WriteLine("✅ Model initialized successfully");
    Console.WriteLine("🌊 Starting streaming inference simulation...");
    
    // Simulate streaming data processing
    await inferenceEngine.StartStreamingInferenceAsync(fraudDetectionService);
    
    Console.WriteLine("🎉 Streaming inference completed successfully!");
}
catch (Exception ex)
{
    Log.Error(ex, "Error in ML.NET integration");
    Console.WriteLine($"❌ Error: {ex.Message}");
}
finally
{
    await host.StopAsync();
    Log.CloseAndFlush();
}

// Transaction data model
public class TransactionData
{
    public float Amount { get; set; }
    public float AccountAge { get; set; }
    public float TransactionCount { get; set; }
    public string Location { get; set; } = string.Empty;
    public float TimeOfDay { get; set; }
    public bool IsFraud { get; set; }
}

public class FraudPrediction
{
    [ColumnName("PredictedLabel")]
    public bool IsFraud { get; set; }
    
    [ColumnName("Probability")]
    public float Probability { get; set; }
    
    [ColumnName("Score")]
    public float Score { get; set; }
}

// Fraud detection service
public class FraudDetectionService
{
    private readonly MLContext _mlContext;
    private ITransformer? _model;
    private PredictionEngine<TransactionData, FraudPrediction>? _predictionEngine;
    
    public FraudDetectionService(MLContext mlContext)
    {
        _mlContext = mlContext;
    }
    
    public async Task InitializeModelAsync()
    {
        Log.Information("Creating synthetic training data...");
        
        // Create synthetic training data
        var trainingData = GenerateTrainingData();
        var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);
        
        Log.Information("Training fraud detection model...");
        
        // Define data preparation and training pipeline
        var pipeline = _mlContext.Transforms.Text.FeaturizeText("LocationFeatures", nameof(TransactionData.Location))
            .Append(_mlContext.Transforms.Concatenate("Features", 
                nameof(TransactionData.Amount),
                nameof(TransactionData.AccountAge), 
                nameof(TransactionData.TransactionCount),
                nameof(TransactionData.TimeOfDay),
                "LocationFeatures"))
            .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(
                labelColumnName: nameof(TransactionData.IsFraud),
                featureColumnName: "Features"));
        
        // Train the model
        _model = pipeline.Fit(dataView);
        
        // Create prediction engine
        _predictionEngine = _mlContext.Model.CreatePredictionEngine<TransactionData, FraudPrediction>(_model);
        
        Log.Information("Model training completed successfully");
        
        // Simulate async operation
        await Task.Delay(100);
    }
    
    public async Task<FraudPrediction> PredictFraudAsync(TransactionData transaction)
    {
        if (_predictionEngine == null)
            throw new InvalidOperationException("Model not initialized");
            
        // Simulate inference latency
        await Task.Delay(Random.Shared.Next(10, 50));
        
        var prediction = _predictionEngine.Predict(transaction);
        
        Log.Debug("Fraud prediction: Amount={Amount}, Probability={Probability}, IsFraud={IsFraud}",
            transaction.Amount, prediction.Probability, prediction.IsFraud);
            
        return prediction;
    }
    
    private List<TransactionData> GenerateTrainingData()
    {
        var data = new List<TransactionData>();
        var random = new Random(42); // Fixed seed for reproducibility
        
        // Generate 1000 training samples
        for (int i = 0; i < 1000; i++)
        {
            var isFraud = random.NextDouble() < 0.1; // 10% fraud rate
            
            data.Add(new TransactionData
            {
                Amount = isFraud ? random.Next(1000, 10000) : random.Next(1, 500),
                AccountAge = isFraud ? random.Next(1, 30) : random.Next(30, 365),
                TransactionCount = isFraud ? random.Next(1, 5) : random.Next(5, 50),
                Location = isFraud ? "Unknown" : GetRandomLocation(random),
                TimeOfDay = isFraud ? random.Next(0, 6) : random.Next(6, 24), // Fraud more likely at night
                IsFraud = isFraud
            });
        }
        
        return data;
    }
    
    private string GetRandomLocation(Random random)
    {
        var locations = new[] { "New York", "London", "Tokyo", "Sydney", "San Francisco", "Toronto" };
        return locations[random.Next(locations.Length)];
    }
}

// Streaming inference engine
public class StreamingInferenceEngine
{
    public async Task StartStreamingInferenceAsync(FraudDetectionService fraudDetectionService)
    {
        var random = new Random();
        var processedCount = 0;
        var fraudCount = 0;
        var startTime = DateTime.UtcNow;
        
        Console.WriteLine("🔄 Processing streaming transactions...");
        Console.WriteLine("Press Ctrl+C to stop");
        
        // Simulate streaming transactions
        for (int i = 0; i < 100; i++)
        {
            var transaction = GenerateRandomTransaction(random);
            
            var inferenceStart = DateTime.UtcNow;
            var prediction = await fraudDetectionService.PredictFraudAsync(transaction);
            var inferenceTime = DateTime.UtcNow - inferenceStart;
            
            processedCount++;
            if (prediction.IsFraud) fraudCount++;
            
            // Log every 10th transaction
            if (i % 10 == 0)
            {
                Console.WriteLine($"Transaction {i + 1}: Amount=${transaction.Amount:F2}, " +
                    $"Fraud={prediction.IsFraud} (P={prediction.Probability:F3}), " +
                    $"Inference={inferenceTime.TotalMilliseconds:F1}ms");
            }
            
            // Simulate streaming delay
            await Task.Delay(Random.Shared.Next(50, 200));
        }
        
        var totalTime = DateTime.UtcNow - startTime;
        var throughput = processedCount / totalTime.TotalSeconds;
        
        Console.WriteLine();
        Console.WriteLine("📊 Streaming Inference Results:");
        Console.WriteLine($"   Processed: {processedCount} transactions");
        Console.WriteLine($"   Fraud Detected: {fraudCount} ({(double)fraudCount / processedCount * 100:F1}%)");
        Console.WriteLine($"   Total Time: {totalTime.TotalSeconds:F1}s");
        Console.WriteLine($"   Throughput: {throughput:F1} transactions/sec");
        Console.WriteLine($"   Average Latency: {totalTime.TotalMilliseconds / processedCount:F1}ms");
    }
    
    private TransactionData GenerateRandomTransaction(Random random)
    {
        var locations = new[] { "New York", "London", "Tokyo", "Sydney", "San Francisco", "Toronto", "Unknown" };
        
        return new TransactionData
        {
            Amount = random.Next(1, 2000),
            AccountAge = random.Next(1, 365),
            TransactionCount = random.Next(1, 100),
            Location = locations[random.Next(locations.Length)],
            TimeOfDay = random.Next(0, 24)
        };
    }
}