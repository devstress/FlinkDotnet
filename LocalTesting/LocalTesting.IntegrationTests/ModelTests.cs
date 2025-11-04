using System;
using System.Collections.Generic;
using System.Text.Json;
using Flink.JobBuilder.Models;
using FlinkDotNet.DataStream;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

/// <summary>
/// Consolidated integration tests for AI/ML Model DDL (Flink 2.1+).
/// Tests validate IR schema, C# API, SQL generation, and provider configurations.
/// Maximum 5 tests per Flink version as per project guidelines.
/// </summary>
[TestFixture]
[Category("ai-ml-model")]
public class ModelTests
{
    #region Test 1: IR Schema and Serialization

    /// <summary>
    /// Test 1: Validates complete IR schema serialization including:
    /// - CREATE operation with all configuration options
    /// - JSON round-trip serialization
    /// - Input/output schemas, provider, properties
    /// - Different operation types
    /// </summary>
    [Test]
    public void Test1_IRSchema_SerializesCompleteDefinition()
    {
        // Arrange - Create model definition with all features
        var jobDef = new JobDefinition
        {
            Source = new ModelDefinition
            {
                ModelName = "sentiment_analyzer",
                InputSchema = 
                {
                    { "text", "STRING" },
                    { "context", "STRING" }
                },
                OutputSchema = 
                {
                    { "sentiment", "STRING" },
                    { "confidence", "DOUBLE" },
                    { "score", "DOUBLE" }
                },
                Provider = "openai",
                Properties = 
                {
                    { "task", "classification" },
                    { "openai.model", "gpt-4" },
                    { "openai.endpoint", "https://api.openai.com/v1" },
                    { "temperature", "0.7" }
                },
                Operation = "CREATE",
                ExecutionMode = "gateway"
            },
            Metadata = new JobMetadata
            {
                                JobName = "Model Test",
                Version = "1.0"
            }
        };

        // Act - Serialize and deserialize
        var json = JsonSerializer.Serialize(jobDef, new JsonSerializerOptions { WriteIndented = true });
        var deserialized = JsonSerializer.Deserialize<JobDefinition>(json);

        // Assert - Structure
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.Source, Is.InstanceOf<ModelDefinition>());

        var modelDef = deserialized.Source as ModelDefinition;
        Assert.That(modelDef, Is.Not.Null);
        Assert.That(modelDef!.Type, Is.EqualTo("model"));
        Assert.That(modelDef.ModelName, Is.EqualTo("sentiment_analyzer"));
        Assert.That(modelDef.Provider, Is.EqualTo("openai"));
        Assert.That(modelDef.Operation, Is.EqualTo("CREATE"));
        Assert.That(modelDef.ExecutionMode, Is.EqualTo("gateway"));

        // Assert - Input Schema
        Assert.That(modelDef.InputSchema, Has.Count.EqualTo(2));
        Assert.That(modelDef.InputSchema["text"], Is.EqualTo("STRING"));
        Assert.That(modelDef.InputSchema["context"], Is.EqualTo("STRING"));

        // Assert - Output Schema
        Assert.That(modelDef.OutputSchema, Has.Count.EqualTo(3));
        Assert.That(modelDef.OutputSchema["sentiment"], Is.EqualTo("STRING"));
        Assert.That(modelDef.OutputSchema["confidence"], Is.EqualTo("DOUBLE"));
        Assert.That(modelDef.OutputSchema["score"], Is.EqualTo("DOUBLE"));

        // Assert - Properties
        Assert.That(modelDef.Properties, Has.Count.EqualTo(4));
        Assert.That(modelDef.Properties["task"], Is.EqualTo("classification"));
        Assert.That(modelDef.Properties["openai.model"], Is.EqualTo("gpt-4"));
        Assert.That(modelDef.Properties["openai.endpoint"], Is.EqualTo("https://api.openai.com/v1"));
        Assert.That(modelDef.Properties["temperature"], Is.EqualTo("0.7"));
    }

    #endregion

    #region Test 2: C# API Builder Pattern

    /// <summary>
    /// Test 2: Validates C# API builder pattern including:
    /// - Fluent API for creating models
    /// - Input/output schema configuration
    /// - Provider and properties setup
    /// - Builder validation
    /// </summary>
    [Test]
    public void Test2_CSharpAPI_BuilderCreatesCorrectDefinition()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        // Act - Build model using fluent API
        var model = env.CreateModel("fraud_detector")
            .InputColumn("amount", "DOUBLE")
            .InputColumn("location", "STRING")
            .InputColumn("device_id", "STRING")
            .OutputColumn("is_fraud", "BOOLEAN")
            .OutputColumn("risk_score", "DOUBLE")
            .WithProvider("openai")
            .WithProperty("task", "classification")
            .WithProperty("openai.model", "gpt-4-turbo")
            .Build();

        // Assert - Model properties
        Assert.That(model, Is.Not.Null);
        Assert.That(model.ModelName, Is.EqualTo("fraud_detector"));
        Assert.That(model.Provider, Is.EqualTo("openai"));

        // Assert - Input schema
        Assert.That(model.InputSchema, Has.Count.EqualTo(3));
        Assert.That(model.InputSchema["amount"], Is.EqualTo("DOUBLE"));
        Assert.That(model.InputSchema["location"], Is.EqualTo("STRING"));
        Assert.That(model.InputSchema["device_id"], Is.EqualTo("STRING"));

        // Assert - Output schema
        Assert.That(model.OutputSchema, Has.Count.EqualTo(2));
        Assert.That(model.OutputSchema["is_fraud"], Is.EqualTo("BOOLEAN"));
        Assert.That(model.OutputSchema["risk_score"], Is.EqualTo("DOUBLE"));

        // Assert - IR definition
        var def = model.Definition;
        Assert.That(def.ModelName, Is.EqualTo("fraud_detector"));
        Assert.That(def.Provider, Is.EqualTo("openai"));
        Assert.That(def.Properties["task"], Is.EqualTo("classification"));
        Assert.That(def.Properties["openai.model"], Is.EqualTo("gpt-4-turbo"));
        Assert.That(def.Operation, Is.EqualTo("CREATE"));
        Assert.That(def.ExecutionMode, Is.EqualTo("gateway"));
    }

    #endregion

    #region Test 3: SQL DDL Generation

    /// <summary>
    /// Test 3: Validates SQL DDL generation including:
    /// - CREATE MODEL statement structure
    /// - Input/output schema in DDL
    /// - WITH clause with provider and properties
    /// - Proper SQL formatting
    /// </summary>
    [Test]
    public void Test3_SQLGeneration_CreatesValidDDL()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        var model = env.CreateModel("content_moderator")
            .InputColumn("text", "STRING")
            .InputColumn("image_url", "STRING")
            .OutputColumn("is_appropriate", "BOOLEAN")
            .OutputColumn("category", "STRING")
            .WithProvider("azure_openai")
            .WithProperty("task", "moderation")
            .WithProperty("azure.endpoint", "https://my-instance.openai.azure.com")
            .WithProperty("azure.deployment", "gpt-4")
            .Build();

        // Act
        var sql = model.ToSql();

        // Assert - SQL structure
        Assert.That(sql, Does.Contain("CREATE MODEL content_moderator"));
        Assert.That(sql, Does.Contain("INPUT (text STRING, image_url STRING)"));
        Assert.That(sql, Does.Contain("OUTPUT (is_appropriate BOOLEAN, category STRING)"));
        Assert.That(sql, Does.Contain("WITH ("));
        Assert.That(sql, Does.Contain("'provider' = 'azure_openai'"));
        Assert.That(sql, Does.Contain("'task' = 'moderation'"));
        Assert.That(sql, Does.Contain("'azure.endpoint' = 'https://my-instance.openai.azure.com'"));
        Assert.That(sql, Does.Contain("'azure.deployment' = 'gpt-4'"));
    }

    #endregion

    #region Test 4: Provider Configurations

    /// <summary>
    /// Test 4: Validates different AI provider configurations including:
    /// - OpenAI provider setup
    /// - Azure OpenAI provider setup
    /// - Custom REST API provider setup
    /// - Provider-specific properties
    /// </summary>
    [Test]
    public void Test4_ProviderConfigurations_SupportMultipleProviders()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        // Part A: OpenAI Provider
        var openaiModel = env.CreateModel("openai_model")
            .InputColumn("input", "STRING")
            .OutputColumn("output", "STRING")
            .WithProvider("openai")
            .WithProperty("openai.api_key", "sk-xxx")
            .WithProperty("openai.model", "gpt-4")
            .WithProperty("openai.endpoint", "https://api.openai.com/v1")
            .Build();

        Assert.That(openaiModel.Provider, Is.EqualTo("openai"));
        Assert.That(openaiModel.Definition.Properties["openai.model"], Is.EqualTo("gpt-4"));
        var openaiSql = openaiModel.ToSql();
        Assert.That(openaiSql, Does.Contain("'provider' = 'openai'"));
        Assert.That(openaiSql, Does.Contain("'openai.model' = 'gpt-4'"));

        // Part B: Azure OpenAI Provider
        var azureModel = env.CreateModel("azure_model")
            .InputColumn("query", "STRING")
            .OutputColumn("response", "STRING")
            .WithProvider("azure_openai")
            .WithProperty("azure.endpoint", "https://my-resource.openai.azure.com")
            .WithProperty("azure.deployment", "gpt-4")
            .WithProperty("azure.api_key", "xxx")
            .Build();

        Assert.That(azureModel.Provider, Is.EqualTo("azure_openai"));
        Assert.That(azureModel.Definition.Properties["azure.deployment"], Is.EqualTo("gpt-4"));
        var azureSql = azureModel.ToSql();
        Assert.That(azureSql, Does.Contain("'provider' = 'azure_openai'"));
        Assert.That(azureSql, Does.Contain("'azure.endpoint' = 'https://my-resource.openai.azure.com'"));

        // Part C: Custom REST API Provider
        var customModel = env.CreateModel("custom_model")
            .InputColumn("data", "STRING")
            .OutputColumn("result", "STRING")
            .WithProvider("custom")
            .WithProperty("endpoint", "https://my-api.example.com/predict")
            .WithProperty("method", "POST")
            .WithProperty("auth.type", "bearer")
            .WithProperty("auth.token", "xxx")
            .Build();

        Assert.That(customModel.Provider, Is.EqualTo("custom"));
        Assert.That(customModel.Definition.Properties["endpoint"], Is.EqualTo("https://my-api.example.com/predict"));
        var customSql = customModel.ToSql();
        Assert.That(customSql, Does.Contain("'provider' = 'custom'"));
        Assert.That(customSql, Does.Contain("'endpoint' = 'https://my-api.example.com/predict'"));

        // Assert - All models are independent
        Assert.That(openaiModel.ModelName, Is.Not.EqualTo(azureModel.ModelName));
        Assert.That(azureModel.ModelName, Is.Not.EqualTo(customModel.ModelName));
    }

    #endregion

    #region Test 5: Schema Validation and Edge Cases

    /// <summary>
    /// Test 5: Validates schema validation and edge cases including:
    /// - Different Flink data types
    /// - Complex schema configurations
    /// - Validation errors (missing schema, missing provider)
    /// - DROP and DESCRIBE operations
    /// </summary>
    [Test]
    public void Test5_ValidationAndEdgeCases_HandlesVariousScenarios()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        // Part A: Different data types
        var typesModel = env.CreateModel("types_test")
            .InputColumn("string_col", "STRING")
            .InputColumn("int_col", "INT")
            .InputColumn("bigint_col", "BIGINT")
            .InputColumn("double_col", "DOUBLE")
            .InputColumn("boolean_col", "BOOLEAN")
            .InputColumn("timestamp_col", "TIMESTAMP(3)")
            .OutputColumn("result", "STRING")
            .WithProvider("openai")
            .Build();

        Assert.That(typesModel.InputSchema, Has.Count.EqualTo(6));
        Assert.That(typesModel.InputSchema["timestamp_col"], Is.EqualTo("TIMESTAMP(3)"));
        var typesSql = typesModel.ToSql();
        Assert.That(typesSql, Does.Contain("STRING"));
        Assert.That(typesSql, Does.Contain("INT"));
        Assert.That(typesSql, Does.Contain("BIGINT"));
        Assert.That(typesSql, Does.Contain("DOUBLE"));
        Assert.That(typesSql, Does.Contain("BOOLEAN"));
        Assert.That(typesSql, Does.Contain("TIMESTAMP(3)"));

        // Part B: Bulk schema addition
        var bulkModel = env.CreateModel("bulk_test")
            .InputColumns(new Dictionary<string, string>
            {
                { "field1", "STRING" },
                { "field2", "INT" },
                { "field3", "DOUBLE" }
            })
            .OutputColumns(new Dictionary<string, string>
            {
                { "result1", "STRING" },
                { "result2", "DOUBLE" }
            })
            .WithProvider("openai")
            .WithProperties(new Dictionary<string, string>
            {
                { "prop1", "value1" },
                { "prop2", "value2" }
            })
            .Build();

        Assert.That(bulkModel.InputSchema, Has.Count.EqualTo(3));
        Assert.That(bulkModel.OutputSchema, Has.Count.EqualTo(2));
        Assert.That(bulkModel.Definition.Properties, Has.Count.EqualTo(2));

        // Part C: Validation errors
        Assert.Throws<InvalidOperationException>(() =>
        {
            // Missing schema
            env.CreateModel("invalid1")
                .WithProvider("openai")
                .Build();
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            // Missing provider
            env.CreateModel("invalid2")
                .InputColumn("test", "STRING")
                .Build();
        });

        Assert.Throws<ArgumentException>(() =>
        {
            // Empty model name
            new ModelBuilder("")
                .InputColumn("test", "STRING")
                .WithProvider("openai")
                .Build();
        });

        // Part D: DROP operation (simple SQL generation)
        var dropModel = new Model(new ModelDefinition
        {
            ModelName = "old_model",
            Operation = "DROP"
        });
        var dropSql = dropModel.ToSql();
        Assert.That(dropSql, Is.EqualTo("DROP MODEL old_model"));

        // Part E: DESCRIBE operation
        var describeModel = new Model(new ModelDefinition
        {
            ModelName = "my_model",
            Operation = "DESCRIBE"
        });
        var describeSql = describeModel.ToSql();
        Assert.That(describeSql, Is.EqualTo("DESCRIBE MODEL my_model"));

        // Part F: SHOW operation
        var showModel = new Model(new ModelDefinition
        {
            Operation = "SHOW"
        });
        var showSql = showModel.ToSql();
        Assert.That(showSql, Is.EqualTo("SHOW MODELS"));
    }

    #endregion
}
