using System.Text.Json.Serialization;

namespace Exercise31.Models;

/// <summary>
/// Result from Flink validation job
/// </summary>
public class ModelValidationResult
{
    [JsonPropertyName("modelId")]
    public string ModelId { get; set; } = string.Empty;
    
    [JsonPropertyName("isValid")]
    public bool IsValid { get; set; }
    
    [JsonPropertyName("validationErrors")]
    public string[] ValidationErrors { get; set; } = Array.Empty<string>();
    
    [JsonPropertyName("validationWarnings")]
    public string[] ValidationWarnings { get; set; } = Array.Empty<string>();
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
    
    [JsonPropertyName("validatedBy")]
    public string ValidatedBy { get; set; } = "ModelValidationJob";
}