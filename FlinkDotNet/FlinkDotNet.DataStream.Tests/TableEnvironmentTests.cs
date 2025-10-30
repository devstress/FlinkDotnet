using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Tests for TableEnvironment, TableEnvironmentExtensions, and ModelDescription
    /// to achieve coverage for table and model management features.
    /// </summary>
    [TestFixture]
    public class TableEnvironmentTests
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

        #region TableEnvironmentExtensions Tests

        [Test]
        public void TableEnvironmentExtensions_GetTableEnvironment_ShouldReturnTableEnvironment()
        {
            // Act
            var tableEnv = _env.GetTableEnvironment();

            // Assert
            Assert.That(tableEnv, Is.Not.Null);
        }

        [Test]
        public void TableEnvironmentExtensions_GetTableEnvironment_SamenvCalls_ShouldReturnSameInstance()
        {
            // Act
            var tableEnv1 = _env.GetTableEnvironment();
            var tableEnv2 = _env.GetTableEnvironment();

            // Assert
            Assert.That(tableEnv2, Is.SameAs(tableEnv1));
        }

        [Test]
        public void TableEnvironmentExtensions_GetTableEnvironment_WithNullEnv_ShouldThrow()
        {
            // Arrange
            StreamExecutionEnvironment nullEnv = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => nullEnv!.GetTableEnvironment());
        }

        #endregion

        #region TableEnvironment Model Management Tests

        [Test]
        public void TableEnvironment_CreateModel_ShouldRegisterModel()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();
            var model = new ModelBuilder("test_model")
                .InputColumn("text", "STRING")
                .WithProvider("openai")
                .Build();

            // Act
            tableEnv.CreateModel("test_model", model);
            var retrieved = tableEnv.GetModel("test_model");

            // Assert
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved, Is.SameAs(model));
        }

        [Test]
        public void TableEnvironment_CreateModel_WithNullModelName_ShouldThrow()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();
            var model = new ModelBuilder("test")
                .InputColumn("text", "STRING")
                .WithProvider("openai")
                .Build();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => tableEnv.CreateModel(null!, model));
        }

        [Test]
        public void TableEnvironment_CreateModel_WithNullModel_ShouldThrow()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => tableEnv.CreateModel("test", null!));
        }

        [Test]
        public void TableEnvironment_CreateModel_DuplicateName_ShouldThrow()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();
            var model = new ModelBuilder("test_model")
                .InputColumn("text", "STRING")
                .WithProvider("openai")
                .Build();
            tableEnv.CreateModel("test_model", model);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                tableEnv.CreateModel("test_model", model));
            Assert.That(ex.Message, Does.Contain("already exists"));
        }

        [Test]
        public void TableEnvironment_GetModel_NonExistent_ShouldReturnNull()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();

            // Act
            var model = tableEnv.GetModel("nonexistent");

            // Assert
            Assert.That(model, Is.Null);
        }

        [Test]
        public void TableEnvironment_GetModel_WithNullName_ShouldThrow()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => tableEnv.GetModel(null!));
        }

        [Test]
        public void TableEnvironment_ListModels_ShouldReturnModelNames()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();
            var model1 = new ModelBuilder("model1")
                .InputColumn("text", "STRING")
                .WithProvider("openai")
                .Build();
            var model2 = new ModelBuilder("model2")
                .OutputColumn("result", "STRING")
                .WithProvider("custom")
                .Build();

            tableEnv.CreateModel("model1", model1);
            tableEnv.CreateModel("model2", model2);

            // Act
            var models = tableEnv.ListModels().ToList();

            // Assert
            Assert.That(models, Has.Count.EqualTo(2));
            Assert.That(models, Does.Contain("model1"));
            Assert.That(models, Does.Contain("model2"));
        }

        [Test]
        public void TableEnvironment_ListModels_EmptyEnvironment_ShouldReturnEmpty()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();

            // Act
            var models = tableEnv.ListModels().ToList();

            // Assert
            Assert.That(models, Is.Empty);
        }

        [Test]
        public void TableEnvironment_DropModel_ShouldRemoveModel()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();
            var model = new ModelBuilder("test_model")
                .InputColumn("text", "STRING")
                .WithProvider("openai")
                .Build();
            tableEnv.CreateModel("test_model", model);

            // Act
            tableEnv.DropModel("test_model");
            var retrieved = tableEnv.GetModel("test_model");

            // Assert
            Assert.That(retrieved, Is.Null);
        }

        [Test]
        public void TableEnvironment_DropModel_WithNullName_ShouldThrow()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => tableEnv.DropModel(null!));
        }

        [Test]
        public void TableEnvironment_DropModel_NonExistent_ShouldThrow()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                tableEnv.DropModel("nonexistent"));
            Assert.That(ex.Message, Does.Contain("does not exist"));
        }

        [Test]
        public void TableEnvironment_DescribeModel_ShouldReturnDescription()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();
            var model = new ModelBuilder("test_model")
                .InputColumn("text", "STRING")
                .InputColumn("temperature", "DOUBLE")
                .OutputColumn("response", "STRING")
                .WithProvider("openai")
                .WithProperty("api_key", "sk-test")
                .Build();
            tableEnv.CreateModel("test_model", model);

            // Act
            var description = tableEnv.DescribeModel("test_model");

            // Assert
            Assert.That(description, Is.Not.Null);
            Assert.That(description.ModelName, Is.EqualTo("test_model"));
            Assert.That(description.Provider, Is.EqualTo("openai"));
            Assert.That(description.InputSchema.Count, Is.EqualTo(2));
            Assert.That(description.InputSchema["text"], Is.EqualTo("STRING"));
            Assert.That(description.OutputSchema.Count, Is.EqualTo(1));
            Assert.That(description.OutputSchema["response"], Is.EqualTo("STRING"));
            Assert.That(description.Properties["api_key"], Is.EqualTo("sk-test"));
        }

        [Test]
        public void TableEnvironment_DescribeModel_WithNullName_ShouldThrow()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => tableEnv.DescribeModel(null!));
        }

        [Test]
        public void TableEnvironment_DescribeModel_NonExistent_ShouldThrow()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                tableEnv.DescribeModel("nonexistent"));
            Assert.That(ex.Message, Does.Contain("does not exist"));
        }

        #endregion

        #region TableEnvironment Table Management Tests

        [Test]
        public void TableEnvironment_RegisterTable_ShouldStoreTable()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();
            var tableDefinition = new Flink.JobBuilder.Models.TableSourceDefinition
            {
                TableName = "test_table"
            };
            var table = new Table(tableDefinition);

            // Act
            tableEnv.RegisterTable("test_table", table);
            var retrieved = tableEnv.GetTable("test_table");

            // Assert
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved, Is.SameAs(table));
        }

        [Test]
        public void TableEnvironment_RegisterTable_WithNullTableName_ShouldThrow()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();
            var tableDefinition = new Flink.JobBuilder.Models.TableSourceDefinition
            {
                TableName = "test"
            };
            var table = new Table(tableDefinition);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => tableEnv.RegisterTable(null!, table));
        }

        [Test]
        public void TableEnvironment_RegisterTable_WithNullTable_ShouldThrow()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => tableEnv.RegisterTable("test", null!));
        }

        [Test]
        public void TableEnvironment_GetTable_NonExistent_ShouldReturnNull()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();

            // Act
            var table = tableEnv.GetTable("nonexistent");

            // Assert
            Assert.That(table, Is.Null);
        }

        [Test]
        public void TableEnvironment_GetTable_WithNullName_ShouldThrow()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => tableEnv.GetTable(null!));
        }

        [Test]
        public void TableEnvironment_ListTables_ShouldReturnTableNames()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();
            var tableDef1 = new Flink.JobBuilder.Models.TableSourceDefinition { TableName = "table1" };
            var tableDef2 = new Flink.JobBuilder.Models.TableSourceDefinition { TableName = "table2" };
            var table1 = new Table(tableDef1);
            var table2 = new Table(tableDef2);

            tableEnv.RegisterTable("table1", table1);
            tableEnv.RegisterTable("table2", table2);

            // Act
            var tables = tableEnv.ListTables().ToList();

            // Assert
            Assert.That(tables, Has.Count.EqualTo(2));
            Assert.That(tables, Does.Contain("table1"));
            Assert.That(tables, Does.Contain("table2"));
        }

        [Test]
        public void TableEnvironment_ListTables_EmptyEnvironment_ShouldReturnEmpty()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();

            // Act
            var tables = tableEnv.ListTables().ToList();

            // Assert
            Assert.That(tables, Is.Empty);
        }

        #endregion

        #region ModelDescription Property Tests

        [Test]
        public void ModelDescription_Properties_ShouldGetAndSet()
        {
            // Act
            var description = new ModelDescription
            {
                ModelName = "test_model",
                Provider = "openai",
                InputSchema = new Dictionary<string, string> { { "text", "STRING" } },
                OutputSchema = new Dictionary<string, string> { { "response", "STRING" } },
                Properties = new Dictionary<string, string> { { "key", "value" } }
            };

            // Assert
            Assert.That(description.ModelName, Is.EqualTo("test_model"));
            Assert.That(description.Provider, Is.EqualTo("openai"));
            Assert.That(description.InputSchema["text"], Is.EqualTo("STRING"));
            Assert.That(description.OutputSchema["response"], Is.EqualTo("STRING"));
            Assert.That(description.Properties["key"], Is.EqualTo("value"));
        }

        [Test]
        public void ModelDescription_DefaultValues_ShouldBeInitialized()
        {
            // Act
            var description = new ModelDescription();

            // Assert
            Assert.That(description.ModelName, Is.EqualTo(string.Empty));
            Assert.That(description.Provider, Is.EqualTo(string.Empty));
            Assert.That(description.InputSchema, Is.Not.Null);
            Assert.That(description.OutputSchema, Is.Not.Null);
            Assert.That(description.Properties, Is.Not.Null);
        }

        #endregion
    }
}
