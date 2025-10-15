using Confluent.Kafka;
using System.Text.Json;
using Exercise31.Models;
using Microsoft.Extensions.Logging;

namespace Exercise31.Services;

/// <summary>
/// Real Kafka-based model registration service
/// Replaces simulated AIModelDDLService with actual streaming
/// </summary>
public class ModelRegistrationService : IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<ModelRegistrationService> _logger;
    private const string RegistrationTopic = "ai-model-registrations";
    
    public ModelRegistrationService(string bootstrapServers, ILogger<ModelRegistrationService> logger)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            ClientId = "aimodel-ddl-producer",
            Acks = Acks.All,
            LingerMs = 5
        };
        
        _producer = new ProducerBuilder<string, string>(config).Build();
        _logger = logger;
    }
    
    /// <summary>
    /// Register AI model by publishing event to Kafka
    /// </summary>
    public async Task<string> RegisterModelAsync(AIModelDefinition model)
    {
        var modelId = Guid.NewGuid().ToString();
        
        var registrationEvent = new ModelRegistrationEvent
        {
            ModelId = modelId,
            ModelName = model.ModelName,
            ModelVersion = model.ModelVersion,
            ModelType = model.ModelType.ToString(),
            ModelFormat = model.ModelFormat.ToString(),
            ModelPath = model.ModelPath ?? string.Empty,
            InputSchema = model.InputSchema,
            OutputSchema = model.OutputSchema,
            OptimizationSettings = model.OptimizationSettings != null ? new OptimizationSettings
            {
                BatchSize = model.OptimizationSettings.BatchSize,
                CacheSize = model.OptimizationSettings.CacheSize,
                WarmupSamples = model.OptimizationSettings.WarmupSamples
            } : null,
            QualityMetrics = model.QualityMetrics != null ? new QualityMetrics
            {
                Accuracy = model.QualityMetrics.Accuracy,
                Precision = model.QualityMetrics.Precision,
                Recall = model.QualityMetrics.Recall,
                F1Score = model.QualityMetrics.F1Score
            } : null,
            Timestamp = DateTime.UtcNow
        };
        
        var json = JsonSerializer.Serialize(registrationEvent);
        
        try
        {
            var result = await _producer.ProduceAsync(RegistrationTopic, new Message<string, string>
            {
                Key = modelId,
                Value = json
            });
            
            _logger.LogInformation("Registered model {ModelName} v{Version} with ID {ModelId} at offset {Offset}", 
                model.ModelName, model.ModelVersion, modelId, result.Offset);
            
            return modelId;
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to register model {ModelName}", model.ModelName);
            throw;
        }
    }
    
    public void Dispose()
    {
        _logger.LogInformation("Flushing model registration producer...");
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
    }
}

// Keep original model definitions for compatibility
public enum ModelType
{
    Classification,
    Regression,
    NLPClassification,
    AnomalyDetection,
    Recommendation,
    Clustering
}

public enum ModelFormat
{
    ONNX,
    TensorFlow,
    PyTorch,
    ScikitLearn,
    XGBoost
}

public class AIModelDefinition
{
    public string ModelName { get; set; } = "";
    public ModelType ModelType { get; set; }
    public ModelFormat ModelFormat { get; set; }
    public string ModelVersion { get; set; } = "";
    public string? ModelPath { get; set; }
    
    public Dictionary<string, string> InputSchema { get; set; } = new();
    public Dictionary<string, string> OutputSchema { get; set; } = new();
    
    public ModelOptimizationSettings? OptimizationSettings { get; set; }
    public ModelQualityMetrics? QualityMetrics { get; set; }
}

public class ModelOptimizationSettings
{
    public int BatchSize { get; set; }
    public string CacheSize { get; set; } = "";
    public int WarmupSamples { get; set; }
}

public class ModelQualityMetrics
{
    public double Accuracy { get; set; }
    public double Precision { get; set; }
    public double Recall { get; set; }
    public double F1Score { get; set; }
}