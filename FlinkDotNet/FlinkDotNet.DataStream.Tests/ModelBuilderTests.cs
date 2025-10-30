using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Tests for Model and ModelBuilder to achieve coverage.
    /// Tests the builder pattern, SQL generation, and AI/ML model operations.
    /// </summary>
    [TestFixture]
    public class ModelBuilderTests
    {
        private StreamExecutionEnvironment _env = null!;

        [SetUp]
        public void Setup()
        {
            _env = StreamExecutionEnvironment.GetExecutionEnvironment();
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", "http://localhost:8080");
        }

        [TearDown]
        public void TearDown()
        {
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", null);
        }

        #region ModelBuilder Constructor Tests

        [Test]
        public void ModelBuilder_Constructor_WithValidName_ShouldSucceed()
        {
            // Act
            var builder = new ModelBuilder("my_model");

            // Assert
            Assert.That(builder, Is.Not.Null);
        }

        [Test]
        public void ModelBuilder_Constructor_WithNullName_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new ModelBuilder(null!));
        }

        [Test]
        public void ModelBuilder_Constructor_WithEmptyName_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new ModelBuilder(""));
        }

        [Test]
        public void ModelBuilder_Constructor_WithWhitespaceName_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new ModelBuilder("   "));
        }

        #endregion

        #region ModelBuilder Fluent API Tests

        [Test]
        public void ModelBuilder_InputColumn_ShouldAddInputColumn()
        {
            // Arrange
            var builder = new ModelBuilder("test_model");

            // Act
            var result = builder.InputColumn("text", "STRING");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(builder)); // Fluent API check
        }

        [Test]
        public void ModelBuilder_InputColumns_ShouldAddMultipleInputColumns()
        {
            // Arrange
            var builder = new ModelBuilder("test_model");
            var columns = new Dictionary<string, string>
            {
                { "text", "STRING" },
                { "temperature", "DOUBLE" }
            };

            // Act
            var result = builder.InputColumns(columns);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(builder));
        }

        [Test]
        public void ModelBuilder_OutputColumn_ShouldAddOutputColumn()
        {
            // Arrange
            var builder = new ModelBuilder("test_model");

            // Act
            var result = builder.OutputColumn("response", "STRING");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(builder));
        }

        [Test]
        public void ModelBuilder_OutputColumns_ShouldAddMultipleOutputColumns()
        {
            // Arrange
            var builder = new ModelBuilder("test_model");
            var columns = new Dictionary<string, string>
            {
                { "response", "STRING" },
                { "confidence", "DOUBLE" }
            };

            // Act
            var result = builder.OutputColumns(columns);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(builder));
        }

        [Test]
        public void ModelBuilder_WithProvider_ShouldSetProvider()
        {
            // Arrange
            var builder = new ModelBuilder("test_model");

            // Act
            var result = builder.WithProvider("openai");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(builder));
        }

        [Test]
        public void ModelBuilder_WithProperty_ShouldAddProperty()
        {
            // Arrange
            var builder = new ModelBuilder("test_model");

            // Act
            var result = builder.WithProperty("api_key", "sk-test-key");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(builder));
        }

        [Test]
        public void ModelBuilder_WithProperties_ShouldAddMultipleProperties()
        {
            // Arrange
            var builder = new ModelBuilder("test_model");
            var properties = new Dictionary<string, string>
            {
                { "api_key", "sk-test-key" },
                { "model", "gpt-4" }
            };

            // Act
            var result = builder.WithProperties(properties);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(builder));
        }

        [Test]
        public void ModelBuilder_WithExecutionMode_ShouldSetExecutionMode()
        {
            // Arrange
            var builder = new ModelBuilder("test_model");

            // Act
            var result = builder.WithExecutionMode("tableenv");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(builder));
        }

        #endregion

        #region ModelBuilder Build Tests

        [Test]
        public void ModelBuilder_Build_WithInputAndOutputSchema_ShouldSucceed()
        {
            // Arrange
            var builder = new ModelBuilder("test_model")
                .InputColumn("text", "STRING")
                .OutputColumn("response", "STRING")
                .WithProvider("openai");

            // Act
            var model = builder.Build();

            // Assert
            Assert.That(model, Is.Not.Null);
            Assert.That(model.ModelName, Is.EqualTo("test_model"));
            Assert.That(model.Provider, Is.EqualTo("openai"));
        }

        [Test]
        public void ModelBuilder_Build_WithOnlyInputSchema_ShouldSucceed()
        {
            // Arrange
            var builder = new ModelBuilder("test_model")
                .InputColumn("text", "STRING")
                .WithProvider("openai");

            // Act
            var model = builder.Build();

            // Assert
            Assert.That(model, Is.Not.Null);
        }

        [Test]
        public void ModelBuilder_Build_WithOnlyOutputSchema_ShouldSucceed()
        {
            // Arrange
            var builder = new ModelBuilder("test_model")
                .OutputColumn("response", "STRING")
                .WithProvider("openai");

            // Act
            var model = builder.Build();

            // Assert
            Assert.That(model, Is.Not.Null);
        }

        [Test]
        public void ModelBuilder_Build_WithoutSchema_ShouldThrow()
        {
            // Arrange
            var builder = new ModelBuilder("test_model")
                .WithProvider("openai");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => builder.Build());
        }

        [Test]
        public void ModelBuilder_Build_WithoutProvider_ShouldThrow()
        {
            // Arrange
            var builder = new ModelBuilder("test_model")
                .InputColumn("text", "STRING");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => builder.Build());
        }

        #endregion

        #region Model Property Tests

        [Test]
        public void Model_ModelName_ShouldReturnModelName()
        {
            // Arrange
            var model = new ModelBuilder("my_ai_model")
                .InputColumn("text", "STRING")
                .OutputColumn("result", "STRING")
                .WithProvider("openai")
                .Build();

            // Assert
            Assert.That(model.ModelName, Is.EqualTo("my_ai_model"));
        }

        [Test]
        public void Model_Provider_ShouldReturnProvider()
        {
            // Arrange
            var model = new ModelBuilder("test_model")
                .InputColumn("text", "STRING")
                .WithProvider("azure_openai")
                .Build();

            // Assert
            Assert.That(model.Provider, Is.EqualTo("azure_openai"));
        }

        [Test]
        public void Model_InputSchema_ShouldReturnInputSchema()
        {
            // Arrange
            var model = new ModelBuilder("test_model")
                .InputColumn("text", "STRING")
                .InputColumn("temperature", "DOUBLE")
                .WithProvider("openai")
                .Build();

            // Assert
            Assert.That(model.InputSchema, Is.Not.Null);
            Assert.That(model.InputSchema.Count, Is.EqualTo(2));
            Assert.That(model.InputSchema["text"], Is.EqualTo("STRING"));
            Assert.That(model.InputSchema["temperature"], Is.EqualTo("DOUBLE"));
        }

        [Test]
        public void Model_OutputSchema_ShouldReturnOutputSchema()
        {
            // Arrange
            var model = new ModelBuilder("test_model")
                .OutputColumn("response", "STRING")
                .OutputColumn("confidence", "DOUBLE")
                .WithProvider("openai")
                .Build();

            // Assert
            Assert.That(model.OutputSchema, Is.Not.Null);
            Assert.That(model.OutputSchema.Count, Is.EqualTo(2));
            Assert.That(model.OutputSchema["response"], Is.EqualTo("STRING"));
            Assert.That(model.OutputSchema["confidence"], Is.EqualTo("DOUBLE"));
        }

        [Test]
        public void Model_Definition_ShouldReturnDefinition()
        {
            // Arrange
            var model = new ModelBuilder("test_model")
                .InputColumn("text", "STRING")
                .WithProvider("openai")
                .Build();

            // Assert
            Assert.That(model.Definition, Is.Not.Null);
            Assert.That(model.Definition.ModelName, Is.EqualTo("test_model"));
            Assert.That(model.Definition.Operation, Is.EqualTo("CREATE"));
        }

        #endregion

        #region Model ToSql Tests - CREATE

        [Test]
        public void Model_ToSql_CreateWithInputAndOutput_ShouldGenerateSQL()
        {
            // Arrange
            var model = new ModelBuilder("my_model")
                .InputColumn("text", "STRING")
                .OutputColumn("response", "STRING")
                .WithProvider("openai")
                .Build();

            // Act
            var sql = model.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("CREATE MODEL my_model"));
            Assert.That(sql, Does.Contain("INPUT (text STRING)"));
            Assert.That(sql, Does.Contain("OUTPUT (response STRING)"));
            Assert.That(sql, Does.Contain("'provider' = 'openai'"));
        }

        [Test]
        public void Model_ToSql_CreateWithMultipleInputs_ShouldGenerateSQL()
        {
            // Arrange
            var model = new ModelBuilder("my_model")
                .InputColumn("text", "STRING")
                .InputColumn("temperature", "DOUBLE")
                .InputColumn("max_tokens", "BIGINT")
                .WithProvider("openai")
                .Build();

            // Act
            var sql = model.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("text STRING"));
            Assert.That(sql, Does.Contain("temperature DOUBLE"));
            Assert.That(sql, Does.Contain("max_tokens BIGINT"));
        }

        [Test]
        public void Model_ToSql_CreateWithProperties_ShouldGenerateSQL()
        {
            // Arrange
            var model = new ModelBuilder("my_model")
                .InputColumn("text", "STRING")
                .WithProvider("openai")
                .WithProperty("api_key", "sk-test-key")
                .WithProperty("model", "gpt-4")
                .Build();

            // Act
            var sql = model.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("'api_key' = 'sk-test-key'"));
            Assert.That(sql, Does.Contain("'model' = 'gpt-4'"));
        }

        [Test]
        public void Model_ToSql_CreateWithOnlyInput_ShouldGenerateSQL()
        {
            // Arrange
            var model = new ModelBuilder("my_model")
                .InputColumn("text", "STRING")
                .WithProvider("openai")
                .Build();

            // Act
            var sql = model.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("CREATE MODEL my_model"));
            Assert.That(sql, Does.Contain("INPUT (text STRING)"));
            Assert.That(sql, Does.Not.Contain("OUTPUT"));
        }

        [Test]
        public void Model_ToSql_CreateWithOnlyOutput_ShouldGenerateSQL()
        {
            // Arrange
            var model = new ModelBuilder("my_model")
                .OutputColumn("result", "STRING")
                .WithProvider("custom")
                .Build();

            // Act
            var sql = model.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("CREATE MODEL my_model"));
            Assert.That(sql, Does.Contain("OUTPUT (result STRING)"));
            Assert.That(sql, Does.Not.Contain("INPUT"));
        }

        #endregion

        #region Model ToSql Tests - Other Operations

        [Test]
        public void Model_ToSql_Drop_ShouldGenerateSQL()
        {
            // Arrange
            var definition = new Flink.JobBuilder.Models.ModelDefinition
            {
                ModelName = "my_model",
                Operation = "DROP"
            };
            var model = new Model(definition);

            // Act
            var sql = model.ToSql();

            // Assert
            Assert.That(sql, Is.EqualTo("DROP MODEL my_model"));
        }

        [Test]
        public void Model_ToSql_Show_ShouldGenerateSQL()
        {
            // Arrange
            var definition = new Flink.JobBuilder.Models.ModelDefinition
            {
                ModelName = "my_model",
                Operation = "SHOW"
            };
            var model = new Model(definition);

            // Act
            var sql = model.ToSql();

            // Assert
            Assert.That(sql, Is.EqualTo("SHOW MODELS"));
        }

        [Test]
        public void Model_ToSql_Describe_ShouldGenerateSQL()
        {
            // Arrange
            var definition = new Flink.JobBuilder.Models.ModelDefinition
            {
                ModelName = "my_model",
                Operation = "DESCRIBE"
            };
            var model = new Model(definition);

            // Act
            var sql = model.ToSql();

            // Assert
            Assert.That(sql, Is.EqualTo("DESCRIBE MODEL my_model"));
        }

        [Test]
        public void Model_ToSql_UnsupportedOperation_ShouldThrow()
        {
            // Arrange
            var definition = new Flink.JobBuilder.Models.ModelDefinition
            {
                ModelName = "my_model",
                Operation = "INVALID_OPERATION"
            };
            var model = new Model(definition);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => model.ToSql());
        }

        #endregion

        #region ModelExtensions Tests

        [Test]
        public void ModelExtensions_CreateModel_ShouldReturnBuilder()
        {
            // Act
            var builder = _env.CreateModel("test_model");

            // Assert
            Assert.That(builder, Is.Not.Null);
            Assert.That(builder, Is.TypeOf<ModelBuilder>());
        }

        #endregion

        #region Integration Tests

        [Test]
        public void ModelBuilder_CompleteWorkflow_ShouldBuildAndGenerateSQL()
        {
            // Arrange & Act
            var model = new ModelBuilder("chat_model")
                .InputColumn("user_message", "STRING")
                .InputColumn("system_prompt", "STRING")
                .InputColumn("temperature", "DOUBLE")
                .OutputColumn("assistant_response", "STRING")
                .OutputColumn("tokens_used", "BIGINT")
                .WithProvider("openai")
                .WithProperty("api_key", "${API_KEY}")
                .WithProperty("model", "gpt-4")
                .WithProperty("max_tokens", "2048")
                .WithExecutionMode("gateway")
                .Build();

            var sql = model.ToSql();

            // Assert
            Assert.That(model.ModelName, Is.EqualTo("chat_model"));
            Assert.That(model.Provider, Is.EqualTo("openai"));
            Assert.That(model.InputSchema.Count, Is.EqualTo(3));
            Assert.That(model.OutputSchema.Count, Is.EqualTo(2));
            Assert.That(sql, Does.Contain("CREATE MODEL chat_model"));
            Assert.That(sql, Does.Contain("user_message STRING"));
            Assert.That(sql, Does.Contain("assistant_response STRING"));
            Assert.That(sql, Does.Contain("'provider' = 'openai'"));
        }

        #endregion
    }
}
