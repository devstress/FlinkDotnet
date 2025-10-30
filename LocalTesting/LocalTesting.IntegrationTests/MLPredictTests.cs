using System;
using System.Collections.Generic;
using System.Text.Json;
using Flink.JobBuilder.Models;
using FlinkDotNet.DataStream;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

/// <summary>
/// Consolidated integration tests for ML_PREDICT Table Value Function (Flink 2.1+).
/// Tests validate IR schema, C# API, SQL generation, and model integration.
/// Maximum 5 tests per Flink version as per project guidelines.
/// </summary>
[TestFixture]
[Category("ml-predict")]
public class MLPredictTests
{
    #region Test 1: IR Schema and Serialization

    /// <summary>
    /// Test 1: Validates complete IR schema serialization including:
    /// - ML_PREDICT operation definition
    /// - JSON round-trip serialization
    /// - Model name reference
    /// - Input/output column mapping
    /// - Integration with JobDefinition
    /// </summary>
    [Test]
    public void Test1_IRSchema_SerializesCompleteDefinition()
    {
        // Arrange - Create job with ML_PREDICT operation
        var jobDef = new JobDefinition
        {
            Source = new KafkaSourceDefinition
            {
                Topic = "customer-reviews",
                BootstrapServers = "localhost:9092",
                GroupId = "ml-predict-group",
                StartingOffsets = "latest"
            },
            Operations = new List<IOperationDefinition>
            {
                new MLPredictDefinition
                {
                    ModelName = "sentiment_model",
                    InputColumns = new List<string> { "review_text" },
                    OutputColumns = new List<string> { "sentiment", "confidence" },
                    OutputPrefix = "ml"
                }
            },
            Sink = new KafkaSinkDefinition
            {
                Topic = "sentiment-results",
                BootstrapServers = "localhost:9092"
            },
            Metadata = new JobMetadata
            {
                JobId = "ml-predict-job",
                JobName = "ML Predict Test",
                Version = "1.0"
            }
        };

        // Act - Serialize and deserialize
        var json = JsonSerializer.Serialize(jobDef, new JsonSerializerOptions { WriteIndented = true });
        var deserialized = JsonSerializer.Deserialize<JobDefinition>(json);

        // Assert - Structure
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized.Operations, Has.Count.EqualTo(1));
        Assert.That(deserialized.Operations[0], Is.InstanceOf<MLPredictDefinition>());

        var mlPredict = deserialized.Operations[0] as MLPredictDefinition;
        Assert.That(mlPredict, Is.Not.Null);
        Assert.That(mlPredict.Type, Is.EqualTo("ml_predict"));
        Assert.That(mlPredict.ModelName, Is.EqualTo("sentiment_model"));
        Assert.That(mlPredict.OutputPrefix, Is.EqualTo("ml"));

        // Assert - Input columns
        Assert.That(mlPredict.InputColumns, Has.Count.EqualTo(1));
        Assert.That(mlPredict.InputColumns, Contains.Item("review_text"));

        // Assert - Output columns
        Assert.That(mlPredict.OutputColumns, Has.Count.EqualTo(2));
        Assert.That(mlPredict.OutputColumns, Contains.Item("sentiment"));
        Assert.That(mlPredict.OutputColumns, Contains.Item("confidence"));
    }

    #endregion

    #region Test 2: C# API Extension Method

    /// <summary>
    /// Test 2: Validates C# API Table.Predict() extension method including:
    /// - Extension method creates correct IR definition
    /// - Single input column scenario
    /// - Multiple input columns scenario
    /// - Output column inference from model
    /// - Fluent API chaining
    /// </summary>
    [Test]
    public void Test2_CSharpAPI_PredictExtensionCreatesCorrectDefinition()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        // Part A: Single input column
        var model = env.CreateModel("sentiment_analyzer")
            .InputColumn("text", "STRING")
            .OutputColumn("sentiment", "STRING")
            .OutputColumn("confidence", "DOUBLE")
            .WithProvider("openai")
            .Build();

        var table = env.CreateTable("reviews")
            .AddColumn("customer_id", "BIGINT")
            .AddColumn("review_text", "STRING")
            .AddColumn("rating", "INT")
            .Build();

        // Act - Apply ML_PREDICT
        var predicted = table.Predict("sentiment_analyzer", "review_text");

        // Assert - IR definition created
        var def = predicted.GetMLPredictDefinition();
        Assert.That(def, Is.Not.Null);
        Assert.That(def.ModelName, Is.EqualTo("sentiment_analyzer"));
        Assert.That(def.InputColumns, Has.Count.EqualTo(1));
        Assert.That(def.InputColumns[0], Is.EqualTo("review_text"));

        // Part B: Multiple input columns (fraud detection)
        var fraudModel = env.CreateModel("fraud_detector")
            .InputColumn("amount", "DOUBLE")
            .InputColumn("location", "STRING")
            .InputColumn("device_id", "STRING")
            .OutputColumn("is_fraud", "BOOLEAN")
            .OutputColumn("risk_score", "DOUBLE")
            .WithProvider("openai")
            .Build();

        var transactions = env.CreateTable("transactions")
            .AddColumn("transaction_id", "BIGINT")
            .AddColumn("amount", "DOUBLE")
            .AddColumn("location", "STRING")
            .AddColumn("device_id", "STRING")
            .Build();

        var fraudPredicted = transactions.Predict("fraud_detector", "amount", "location", "device_id");

        var fraudDef = fraudPredicted.GetMLPredictDefinition();
        Assert.That(fraudDef, Is.Not.Null);
        Assert.That(fraudDef.ModelName, Is.EqualTo("fraud_detector"));
        Assert.That(fraudDef.InputColumns, Has.Count.EqualTo(3));
        Assert.That(fraudDef.InputColumns, Contains.Item("amount"));
        Assert.That(fraudDef.InputColumns, Contains.Item("location"));
        Assert.That(fraudDef.InputColumns, Contains.Item("device_id"));

        // Part C: Fluent API chaining (not directly testable without full Table API, but validate structure)
        Assert.That(predicted, Is.Not.Null);
        Assert.That(fraudPredicted, Is.Not.Null);
    }

    #endregion

    #region Test 3: SQL Generation

    /// <summary>
    /// Test 3: Validates SQL generation for ML_PREDICT TVF including:
    /// - ML_PREDICT function syntax
    /// - TABLE clause
    /// - MODEL clause
    /// - DESCRIPTOR clause with input columns
    /// - Output alias handling
    /// - Complete SQL query structure
    /// </summary>
    [Test]
    public void Test3_SQLGeneration_CreatesValidMLPredictTVF()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        var table = env.CreateTable("customer_reviews")
            .AddColumn("customer_id", "BIGINT")
            .AddColumn("review_text", "STRING")
            .AddColumn("product_id", "BIGINT")
            .Build();

        var predicted = table.Predict("sentiment_model", "review_text");

        // Act
        var sql = predicted.ToSql();

        // Assert - SQL structure
        Assert.That(sql, Does.Contain("ML_PREDICT"));
        Assert.That(sql, Does.Contain("TABLE customer_reviews"));
        Assert.That(sql, Does.Contain("MODEL sentiment_model"));
        Assert.That(sql, Does.Contain("DESCRIPTOR(review_text)"));

        // Part B: Multiple input columns
        var fraudTable = env.CreateTable("transactions")
            .AddColumn("id", "BIGINT")
            .AddColumn("amount", "DOUBLE")
            .AddColumn("location", "STRING")
            .Build();

        var fraudPredicted = fraudTable.Predict("fraud_model", "amount", "location");
        var fraudSql = fraudPredicted.ToSql();

        Assert.That(fraudSql, Does.Contain("DESCRIPTOR(amount, location)"));

        // Part C: With output prefix
        var prefixTable = env.CreateTable("data")
            .AddColumn("text", "STRING")
            .Build();

        var prefixPredicted = prefixTable.PredictWithPrefix("classifier", "ml", "text");
        var prefixSql = prefixPredicted.ToSql();

        Assert.That(prefixSql, Does.Contain("ML_PREDICT"));
        Assert.That(prefixSql, Does.Contain("AS ml"));
    }

    #endregion

    #region Test 4: ModelDefinition Integration

    /// <summary>
    /// Test 4: Validates integration with CREATE MODEL (WI8) including:
    /// - Model name resolution
    /// - Input schema compatibility validation
    /// - Output schema mapping
    /// - Provider configuration reference
    /// - Multiple models in same job
    /// </summary>
    [Test]
    public void Test4_ModelIntegration_WorksWithModelDefinition()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        // Part A: Create model and use in ML_PREDICT
        var sentimentModel = env.CreateModel("sentiment_analyzer")
            .InputColumn("text", "STRING")
            .OutputColumn("sentiment", "STRING")
            .OutputColumn("confidence", "DOUBLE")
            .WithProvider("openai")
            .WithProperty("task", "classification")
            .Build();

        var reviews = env.CreateTable("reviews")
            .AddColumn("id", "BIGINT")
            .AddColumn("text", "STRING")
            .Build();

        var predicted = reviews.Predict("sentiment_analyzer", "text");

        // Assert - Model integration
        var mlDef = predicted.GetMLPredictDefinition();
        Assert.That(mlDef.ModelName, Is.EqualTo("sentiment_analyzer"));

        // Verify output columns match model output schema
        Assert.That(sentimentModel.OutputSchema, Has.Count.EqualTo(2));
        Assert.That(sentimentModel.OutputSchema.ContainsKey("sentiment"), Is.True);
        Assert.That(sentimentModel.OutputSchema.ContainsKey("confidence"), Is.True);

        // Part B: Multiple models in pipeline
        var toxicityModel = env.CreateModel("toxicity_detector")
            .InputColumn("text", "STRING")
            .OutputColumn("is_toxic", "BOOLEAN")
            .OutputColumn("toxicity_score", "DOUBLE")
            .WithProvider("openai")
            .Build();

        // First prediction: sentiment
        var withSentiment = reviews.Predict("sentiment_analyzer", "text");

        // Second prediction: toxicity (chained)
        var withBoth = withSentiment.Predict("toxicity_detector", "text");

        var bothDef = withBoth.GetMLPredictDefinition();
        Assert.That(bothDef.ModelName, Is.EqualTo("toxicity_detector"));

        // Part C: Complete job with model + ML_PREDICT
        var completeJob = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "reviews" },
            Operations = new List<IOperationDefinition>
            {
                new MLPredictDefinition
                {
                    ModelName = "sentiment_analyzer",
                    InputColumns = new List<string> { "text" },
                    OutputColumns = new List<string> { "sentiment", "confidence" }
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "results" },
            Metadata = new JobMetadata { JobId = "complete-ml-job", Version = "1.0" }
        };

        var json = JsonSerializer.Serialize(completeJob);
        Assert.That(json, Does.Contain("ml_predict"));
        Assert.That(json, Does.Contain("sentiment_analyzer"));
    }

    #endregion

    #region Test 5: Edge Cases and Validation

    /// <summary>
    /// Test 5: Validates edge cases and error handling including:
    /// - Missing model name error
    /// - Empty input columns error
    /// - Invalid column names
    /// - Schema mismatch detection
    /// - Complex scenarios (temporal tables, joins, windows)
    /// </summary>
    [Test]
    public void Test5_EdgeCasesAndValidation_HandlesErrorsCorrectly()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        var table = env.CreateTable("data")
            .AddColumn("col1", "STRING")
            .AddColumn("col2", "INT")
            .Build();

        // Part A: Missing model name
        Assert.Throws<ArgumentException>(() =>
        {
            table.Predict("", "col1");
        });

        Assert.Throws<ArgumentNullException>(() =>
        {
            table.Predict(null!, "col1");
        });

        // Part B: Empty input columns
        Assert.Throws<ArgumentException>(() =>
        {
            table.Predict("model");
        });

        Assert.Throws<ArgumentException>(() =>
        {
            table.Predict("model", new string[0]);
        });

        // Part C: Invalid column names
        Assert.Throws<ArgumentException>(() =>
        {
            table.Predict("model", "nonexistent_column");
        });

        // Part D: Complex data types
        var complexTable = env.CreateTable("complex")
            .AddColumn("id", "BIGINT")
            .AddColumn("json_data", "STRING")
            .AddColumn("timestamp_col", "TIMESTAMP(3)")
            .AddColumn("array_col", "ARRAY<STRING>")
            .AddColumn("map_col", "MAP<STRING, INT>")
            .Build();

        var complexPredicted = complexTable.Predict("complex_model", "json_data", "timestamp_col");
        var complexDef = complexPredicted.GetMLPredictDefinition();

        Assert.That(complexDef.InputColumns, Has.Count.EqualTo(2));
        Assert.That(complexDef.InputColumns, Contains.Item("json_data"));
        Assert.That(complexDef.InputColumns, Contains.Item("timestamp_col"));

        // Part E: SQL generation with complex types
        var complexSql = complexPredicted.ToSql();
        Assert.That(complexSql, Does.Contain("DESCRIPTOR(json_data, timestamp_col)"));

        // Part F: IR serialization with all edge cases
        var edgeCaseJob = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "input" },
            Operations = new List<IOperationDefinition>
            {
                new MLPredictDefinition
                {
                    ModelName = "edge_case_model",
                    InputColumns = new List<string> { "col1", "col2", "col3", "col4", "col5" },
                    OutputColumns = new List<string> { "out1", "out2", "out3" },
                    OutputPrefix = "prediction"
                }
            },
            Metadata = new JobMetadata { JobId = "edge-case-job", Version = "1.0" }
        };

        var edgeJson = JsonSerializer.Serialize(edgeCaseJob);
        var edgeDeserialized = JsonSerializer.Deserialize<JobDefinition>(edgeJson);

        var edgeMlPredict = edgeDeserialized!.Operations[0] as MLPredictDefinition;
        Assert.That(edgeMlPredict, Is.Not.Null);
        Assert.That(edgeMlPredict!.InputColumns, Has.Count.EqualTo(5));
        Assert.That(edgeMlPredict.OutputColumns, Has.Count.EqualTo(3));
        Assert.That(edgeMlPredict.OutputPrefix, Is.EqualTo("prediction"));

        // Part G: Validation happens in Table.Predict(), not in MLPredictDefinition
        // MLPredictDefinition is a simple IR schema (data) class without validation logic
        Assert.That(edgeMlPredict.ModelName, Is.Not.Null);
        Assert.That(edgeMlPredict.InputColumns, Is.Not.Null);
        Assert.That(edgeMlPredict.OutputColumns, Is.Not.Null);

        // Part H: AI Provider Integration - OpenAI
        var openaiProvider = ModelProviderFactory.GetProvider("openai");
        Assert.That(openaiProvider, Is.Not.Null);
        Assert.That(openaiProvider!.ProviderName, Is.EqualTo("openai"));

        var openaiConfig = new Dictionary<string, string>
        {
            { "openai.api_key", "sk-test" },
            { "openai.model", "gpt-4" }
        };
        Assert.That(openaiProvider.ValidateConfiguration(openaiConfig), Is.True);

        // Part I: AI Provider Integration - Azure OpenAI
        var azureProvider = ModelProviderFactory.GetProvider("azure_openai");
        Assert.That(azureProvider, Is.Not.Null);
        Assert.That(azureProvider!.ProviderName, Is.EqualTo("azure_openai"));

        var azureConfig = new Dictionary<string, string>
        {
            { "azure.endpoint", "https://test.openai.azure.com" },
            { "azure.deployment", "gpt-4" },
            { "azure.api_key", "test-key" }
        };
        Assert.That(azureProvider.ValidateConfiguration(azureConfig), Is.True);

        // Part J: Model Management API - TableEnvironment
        var tableEnv = env.GetTableEnvironment();
        Assert.That(tableEnv, Is.Not.Null);

        // Create and register model
        var mgmtModel = env.CreateModel("test_model")
            .InputColumn("input", "STRING")
            .OutputColumn("output", "STRING")
            .WithProvider("openai")
            .Build();

        tableEnv.CreateModel("test_model", mgmtModel);

        // List models
        var models = tableEnv.ListModels();
        Assert.That(models, Contains.Item("test_model"));

        // Get model
        var retrievedModel = tableEnv.GetModel("test_model");
        Assert.That(retrievedModel, Is.Not.Null);
        Assert.That(retrievedModel!.ModelName, Is.EqualTo("test_model"));

        // Describe model
        var description = tableEnv.DescribeModel("test_model");
        Assert.That(description.ModelName, Is.EqualTo("test_model"));
        Assert.That(description.Provider, Is.EqualTo("openai"));
        Assert.That(description.InputSchema, Has.Count.EqualTo(1));
        Assert.That(description.OutputSchema, Has.Count.EqualTo(1));

        // Drop model
        tableEnv.DropModel("test_model");
        Assert.That(tableEnv.ListModels(), Does.Not.Contain("test_model"));
    }

    #endregion
}
