using System.Text.Json.Serialization;

namespace Exercise31.Models;

/// <summary>
/// Event published to Kafka when a model is registered
/// </summary>
public class ModelRegistrationEvent
{
    [JsonPropertyName("modelId")]
    public string ModelId { get; set; } = string.Empty;
    
    [JsonPropertyName("modelName")]
    public string ModelName { get; set; } = string.Empty;
    
    [JsonPropertyName("modelVersion")]
    public string ModelVersion { get; set; } = string.Empty;
    
    [JsonPropertyName("modelType")]
    public string ModelType { get; set; } = string.Empty;
    
    [JsonPropertyName("modelFormat")]
    public string ModelFormat { get; set; } = string.Empty;
    
    [JsonPropertyName("modelPath")]
    public string ModelPath { get; set; } = string.Empty;
    
    [JsonPropertyName("inputSchema")]
    public Dictionary<string, string> InputSchema { get; set; } = new();
    
    [JsonPropertyName("outputSchema")]
    public Dictionary<string, string> OutputSchema { get; set; } = new();
    
    [JsonPropertyName("optimizationSettings")]
    public OptimizationSettings? OptimizationSettings { get; set; }
    
    [JsonPropertyName("qualityMetrics")]
    public QualityMetrics? QualityMetrics { get; set; }
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

public class OptimizationSettings
{
    [JsonPropertyName("batchSize")]
    public int BatchSize { get; set; }
    
    [JsonPropertyName("cacheSize")]
    public string CacheSize { get; set; } = string.Empty;
    
    [JsonPropertyName("warmupSamples")]
    public int WarmupSamples { get; set; }
}

public class QualityMetrics
{
    [JsonPropertyName("accuracy")]
    public double Accuracy { get; set; }
    
    [JsonPropertyName("precision")]
    public double Precision { get; set; }
    
    [JsonPropertyName("recall")]
    public double Recall { get; set; }
    
    [JsonPropertyName("f1Score")]
    public double F1Score { get; set; }
}