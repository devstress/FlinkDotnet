using FlinkDotNet.DataStream;
using System.Text.Json;
using Exercise31.Models;
using Microsoft.Extensions.Logging;

namespace Exercise31.FlinkJobs;

/// <summary>
/// Flink job that validates AI models from registration stream
/// Real streaming validation instead of Task.Delay simulation
/// </summary>
public class ModelValidationJob
{
    private readonly string _kafkaBootstrapServers;
    private readonly ILogger<ModelValidationJob> _logger;
    
    private const string RegistrationTopic = "ai-model-registrations";
    private const string ValidationResultsTopic = "ai-model-validations";
    private const string ConsumerGroup = "model-validation-job";
    
    public ModelValidationJob(string kafkaBootstrapServers, ILogger<ModelValidationJob> logger)
    {
        _kafkaBootstrapServers = kafkaBootstrapServers;
        _logger = logger;
    }
    
    /// <summary>
    /// Submit Flink validation job to process model registrations
    /// </summary>
    public async Task<FlinkDotNet.DataStream.IJobClient> SubmitAsync()
    {
        _logger.LogInformation("Submitting Model Validation Flink job...");
        
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        // Source: Model registration events from Kafka
        var registrationStream = env.FromKafka(
            topic: RegistrationTopic,
            bootstrapServers: _kafkaBootstrapServers,
            groupId: ConsumerGroup,
            startingOffsets: "earliest"
        );
        
        // Validate each model registration
        var validatedStream = registrationStream
            .Map(new ModelValidationFunction());
        
        // Sink: Validation results back to Kafka
        validatedStream.SinkToKafka(ValidationResultsTopic, _kafkaBootstrapServers);
        
        // Execute job
        var jobClient = await env.ExecuteAsync("ModelValidationJob");
        
        _logger.LogInformation("Model validation job submitted: {JobId}", jobClient.GetJobId());
        
        return jobClient;
    }
}

/// <summary>
/// Map function that performs actual model validation
/// </summary>
public class ModelValidationFunction : FlinkDotNet.DataStream.IMapFunction<string, string>
{
    public string Map(string eventJson)
    {
        try
        {
            var registration = JsonSerializer.Deserialize<ModelRegistrationEvent>(eventJson);
            
            if (registration == null)
            {
                return JsonSerializer.Serialize(new ModelValidationResult
                {
                    IsValid = false,
                    ValidationErrors = new[] { "Failed to deserialize registration event" },
                    Timestamp = DateTime.UtcNow
                });
            }
            
            // Perform real validation checks
            var (isValid, errors, warnings) = ValidateModel(registration);
            
            var validationResult = new ModelValidationResult
            {
                ModelId = registration.ModelId,
                IsValid = isValid,
                ValidationErrors = errors.ToArray(),
                ValidationWarnings = warnings.ToArray(),
                Timestamp = DateTime.UtcNow
            };
            
            return JsonSerializer.Serialize(validationResult);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new ModelValidationResult
            {
                IsValid = false,
                ValidationErrors = new[] { $"Validation exception: {ex.Message}" },
                Timestamp = DateTime.UtcNow
            });
        }
    }
    
    /// <summary>
    /// Real validation logic - checks schema, format, requirements
    /// </summary>
    private (bool isValid, List<string> errors, List<string> warnings) ValidateModel(ModelRegistrationEvent model)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        
        // Required fields validation
        if (string.IsNullOrEmpty(model.ModelName))
            errors.Add("Model name is required");
            
        if (string.IsNullOrEmpty(model.ModelVersion))
            errors.Add("Model version is required");
            
        if (string.IsNullOrEmpty(model.ModelType))
            errors.Add("Model type is required");
        
        // Schema validation
        if (model.InputSchema == null || model.InputSchema.Count == 0)
            errors.Add("Input schema must be defined");
        else
        {
            // Validate schema field types
            foreach (var field in model.InputSchema)
            {
                if (string.IsNullOrEmpty(field.Key))
                    errors.Add("Schema field names cannot be empty");
                    
                if (string.IsNullOrEmpty(field.Value))
                    errors.Add($"Schema field '{field.Key}' must have a type");
                else if (!IsValidSchemaType(field.Value))
                    warnings.Add($"Schema field '{field.Key}' has unusual type: {field.Value}");
            }
        }
        
        if (model.OutputSchema == null || model.OutputSchema.Count == 0)
            errors.Add("Output schema must be defined");
        
        // Quality metrics validation
        if (model.QualityMetrics != null)
        {
            if (model.QualityMetrics.Accuracy < 0 || model.QualityMetrics.Accuracy > 1)
                errors.Add("Accuracy must be between 0 and 1");
                
            if (model.QualityMetrics.Precision < 0 || model.QualityMetrics.Precision > 1)
                errors.Add("Precision must be between 0 and 1");
                
            if (model.QualityMetrics.Recall < 0 || model.QualityMetrics.Recall > 1)
                errors.Add("Recall must be between 0 and 1");
                
            if (model.QualityMetrics.F1Score < 0 || model.QualityMetrics.F1Score > 1)
                errors.Add("F1 Score must be between 0 and 1");
        }
        
        // Optimization settings validation
        if (model.OptimizationSettings != null)
        {
            if (model.OptimizationSettings.BatchSize <= 0)
                errors.Add("Batch size must be positive");
                
            if (model.OptimizationSettings.WarmupSamples < 0)
                errors.Add("Warmup samples cannot be negative");
        }
        
        return (errors.Count == 0, errors, warnings);
    }
    
    private bool IsValidSchemaType(string type)
    {
        var validTypes = new[] { "STRING", "INT", "LONG", "DOUBLE", "FLOAT", "BOOLEAN", "TIMESTAMP", "BINARY" };
        return validTypes.Contains(type.ToUpper());
    }
}