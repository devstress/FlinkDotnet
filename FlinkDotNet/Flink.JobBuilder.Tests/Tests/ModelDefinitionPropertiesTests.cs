using System.Collections.Generic;
using NUnit.Framework;
using Flink.JobBuilder.Models;

namespace Flink.JobBuilder.Tests.Tests
{
    /// <summary>
    /// Tests for model definition properties to improve code coverage
    /// </summary>
    [TestFixture]
    public class ModelDefinitionPropertiesTests
    {
        #region MaterializedTableDefinition Tests

        [Test]
        public void MaterializedTableDefinition_Properties_ShouldGetAndSet()
        {
            // Arrange & Act
            var definition = new MaterializedTableDefinition
            {
                TableName = "materialized_view",
                Query = "SELECT * FROM source_table",
                RefreshMode = "FULL",
                FreshnessInterval = "INTERVAL '1' HOUR",
                Operation = "CREATE"
            };

            // Assert
            Assert.That(definition.TableName, Is.EqualTo("materialized_view"));
            Assert.That(definition.Query, Is.EqualTo("SELECT * FROM source_table"));
            Assert.That(definition.RefreshMode, Is.EqualTo("FULL"));
            Assert.That(definition.FreshnessInterval, Is.EqualTo("INTERVAL '1' HOUR"));
            Assert.That(definition.Operation, Is.EqualTo("CREATE"));
            Assert.That(definition.PartitionBy, Is.Not.Null);
            Assert.That(definition.Schema, Is.Not.Null);
            Assert.That(definition.PrimaryKey, Is.Not.Null);
        }

        [Test]
        public void MaterializedTableDefinition_DefaultValues_ShouldBeInitialized()
        {
            // Act
            var definition = new MaterializedTableDefinition();

            // Assert
            Assert.That(definition.TableName, Is.EqualTo(string.Empty));
            Assert.That(definition.Query, Is.EqualTo(string.Empty));
            Assert.That(definition.RefreshMode, Is.EqualTo("CONTINUOUS"));
            Assert.That(definition.Operation, Is.EqualTo("CREATE"));
            Assert.That(definition.PartitionBy, Is.Not.Null);
            Assert.That(definition.Schema, Is.Not.Null);
            Assert.That(definition.PrimaryKey, Is.Not.Null);
        }

        #endregion

        #region MLPredictDefinition Tests

        [Test]
        public void MLPredictDefinition_Properties_ShouldGetAndSet()
        {
            // Arrange & Act
            var definition = new MLPredictDefinition
            {
                ModelName = "sentiment_model",
                InputColumns = new List<string> { "text", "language" },
                OutputPrefix = "pred_"
            };

            // Assert
            Assert.That(definition.ModelName, Is.EqualTo("sentiment_model"));
            Assert.That(definition.InputColumns, Has.Count.EqualTo(2));
            Assert.That(definition.InputColumns[0], Is.EqualTo("text"));
            Assert.That(definition.InputColumns[1], Is.EqualTo("language"));
            Assert.That(definition.OutputPrefix, Is.EqualTo("pred_"));
        }

        [Test]
        public void MLPredictDefinition_DefaultValues_ShouldBeInitialized()
        {
            // Act
            var definition = new MLPredictDefinition();

            // Assert
            Assert.That(definition.ModelName, Is.EqualTo(string.Empty));
            Assert.That(definition.InputColumns, Is.Not.Null);
            Assert.That(definition.OutputPrefix, Is.Null.Or.EqualTo(string.Empty));
        }

        #endregion

        #region PaimonCatalogDefinition Tests

        [Test]
        public void PaimonCatalogDefinition_Properties_ShouldGetAndSet()
        {
            // Arrange & Act
            var definition = new PaimonCatalogDefinition
            {
                CatalogName = "paimon_catalog",
                Warehouse = "/path/to/warehouse",
                CatalogType = "paimon-generic",
                HiveConfDir = "/etc/hive/conf"
            };

            // Assert
            Assert.That(definition.CatalogName, Is.EqualTo("paimon_catalog"));
            Assert.That(definition.Warehouse, Is.EqualTo("/path/to/warehouse"));
            Assert.That(definition.CatalogType, Is.EqualTo("paimon-generic"));
            Assert.That(definition.HiveConfDir, Is.EqualTo("/etc/hive/conf"));
        }

        [Test]
        public void PaimonCatalogDefinition_DefaultValues_ShouldBeInitialized()
        {
            // Act
            var definition = new PaimonCatalogDefinition();

            // Assert
            Assert.That(definition.CatalogName, Is.EqualTo(string.Empty));
            Assert.That(definition.Warehouse, Is.EqualTo(string.Empty));
            Assert.That(definition.CatalogType, Is.EqualTo("paimon"));
        }

        #endregion

        #region PaimonTableDefinition Tests

        [Test]
        public void PaimonTableDefinition_Properties_ShouldGetAndSet()
        {
            // Arrange & Act
            var definition = new PaimonTableDefinition
            {
                TableName = "paimon_table",
                CatalogName = "my_catalog",
                ChangelogProducerMode = "full-compaction",
                Buckets = 4,
                Operation = "CREATE"
            };

            // Assert
            Assert.That(definition.TableName, Is.EqualTo("paimon_table"));
            Assert.That(definition.CatalogName, Is.EqualTo("my_catalog"));
            Assert.That(definition.ChangelogProducerMode, Is.EqualTo("full-compaction"));
            Assert.That(definition.Buckets, Is.EqualTo(4));
            Assert.That(definition.Operation, Is.EqualTo("CREATE"));
            Assert.That(definition.PrimaryKey, Is.Not.Null);
            Assert.That(definition.PartitionKeys, Is.Not.Null);
            Assert.That(definition.Schema, Is.Not.Null);
            Assert.That(definition.TableProperties, Is.Not.Null);
        }

        [Test]
        public void PaimonTableDefinition_DefaultValues_ShouldBeInitialized()
        {
            // Act
            var definition = new PaimonTableDefinition();

            // Assert
            Assert.That(definition.TableName, Is.EqualTo(string.Empty));
            Assert.That(definition.CatalogName, Is.EqualTo(string.Empty));
            Assert.That(definition.ChangelogProducerMode, Is.EqualTo("none"));
            Assert.That(definition.Operation, Is.EqualTo("CREATE"));
            Assert.That(definition.PrimaryKey, Is.Not.Null);
            Assert.That(definition.PartitionKeys, Is.Not.Null);
            Assert.That(definition.Schema, Is.Not.Null);
            Assert.That(definition.TableProperties, Is.Not.Null);
        }

        #endregion

        #region ParseJsonOperationDefinition Tests

        [Test]
        public void ParseJsonOperationDefinition_Properties_ShouldGetAndSet()
        {
            // Arrange & Act
            var definition = new ParseJsonOperationDefinition
            {
                FunctionType = "PARSE_JSON",
                SourceField = "raw_data",
                TargetField = "parsed_data",
                JsonPath = "$.user.id"
            };

            // Assert
            Assert.That(definition.FunctionType, Is.EqualTo("PARSE_JSON"));
            Assert.That(definition.SourceField, Is.EqualTo("raw_data"));
            Assert.That(definition.TargetField, Is.EqualTo("parsed_data"));
            Assert.That(definition.JsonPath, Is.EqualTo("$.user.id"));
        }

        [Test]
        public void ParseJsonOperationDefinition_DefaultValues_ShouldBeInitialized()
        {
            // Act
            var definition = new ParseJsonOperationDefinition();

            // Assert
            Assert.That(definition.FunctionType, Is.EqualTo("TRY_PARSE_JSON"));
            Assert.That(definition.SourceField, Is.EqualTo(string.Empty));
            Assert.That(definition.TargetField, Is.EqualTo(string.Empty));
            Assert.That(definition.JsonPath, Is.Null.Or.EqualTo(string.Empty));
        }

        #endregion

        #region ProcessTableFunctionDefinition Tests

        [Test]
        public void ProcessTableFunctionDefinition_Properties_ShouldGetAndSet()
        {
            // Arrange & Act
            var definition = new ProcessTableFunctionDefinition
            {
                FunctionName = "my_ptf",
                ClassName = "com.example.MyPTF",
                UsesEventTimeTimers = true,
                UsesProcessingTimeTimers = false
            };

            // Assert
            Assert.That(definition.FunctionName, Is.EqualTo("my_ptf"));
            Assert.That(definition.ClassName, Is.EqualTo("com.example.MyPTF"));
            Assert.That(definition.UsesEventTimeTimers, Is.True);
            Assert.That(definition.UsesProcessingTimeTimers, Is.False);
            Assert.That(definition.InputColumns, Is.Not.Null);
            Assert.That(definition.OutputColumns, Is.Not.Null);
            Assert.That(definition.StateDescriptors, Is.Not.Null);
            Assert.That(definition.Properties, Is.Not.Null);
        }

        [Test]
        public void ProcessTableFunctionDefinition_DefaultValues_ShouldBeInitialized()
        {
            // Act
            var definition = new ProcessTableFunctionDefinition();

            // Assert
            Assert.That(definition.FunctionName, Is.EqualTo(string.Empty));
            Assert.That(definition.ClassName, Is.EqualTo(string.Empty));
            Assert.That(definition.InputColumns, Is.Not.Null);
            Assert.That(definition.OutputColumns, Is.Not.Null);
            Assert.That(definition.StateDescriptors, Is.Not.Null);
            Assert.That(definition.Properties, Is.Not.Null);
            Assert.That(definition.UsesEventTimeTimers, Is.False);
            Assert.That(definition.UsesProcessingTimeTimers, Is.False);
        }

        #endregion

        #region TableOperationDefinition Tests

        [Test]
        public void TableOperationDefinition_Properties_ShouldGetAndSet()
        {
            // Arrange & Act
            var definition = new TableOperationDefinition
            {
                OperationType = "select",
                Columns = new List<string> { "id", "name", "email" },
                Condition = "age > 18",
                GroupByKeys = new List<string> { "category" },
                Aggregations = new List<string> { "COUNT(*) AS total", "AVG(price) AS avg_price" }
            };

            // Assert
            Assert.That(definition.OperationType, Is.EqualTo("select"));
            Assert.That(definition.Columns, Has.Count.EqualTo(3));
            Assert.That(definition.Columns[0], Is.EqualTo("id"));
            Assert.That(definition.Condition, Is.EqualTo("age > 18"));
            Assert.That(definition.GroupByKeys, Has.Count.EqualTo(1));
            Assert.That(definition.GroupByKeys[0], Is.EqualTo("category"));
            Assert.That(definition.Aggregations, Has.Count.EqualTo(2));
            Assert.That(definition.Aggregations[0], Is.EqualTo("COUNT(*) AS total"));
        }

        [Test]
        public void TableOperationDefinition_DefaultValues_ShouldBeInitialized()
        {
            // Act
            var definition = new TableOperationDefinition();

            // Assert
            Assert.That(definition.OperationType, Is.EqualTo(string.Empty));
            Assert.That(definition.Columns, Is.Not.Null);
            Assert.That(definition.Condition, Is.Null.Or.EqualTo(string.Empty));
            Assert.That(definition.GroupByKeys, Is.Not.Null);
            Assert.That(definition.Aggregations, Is.Not.Null);
        }

        #endregion

        #region TableSourceDefinition Tests

        [Test]
        public void TableSourceDefinition_Properties_ShouldGetAndSet()
        {
            // Arrange & Act
            var definition = new TableSourceDefinition
            {
                TableName = "my_table",
                Schema = new Dictionary<string, string> { { "id", "BIGINT" }, { "name", "STRING" } },
                CatalogName = "my_catalog",
                DatabaseName = "my_database"
            };

            // Assert
            Assert.That(definition.TableName, Is.EqualTo("my_table"));
            Assert.That(definition.Schema, Has.Count.EqualTo(2));
            Assert.That(definition.Schema["id"], Is.EqualTo("BIGINT"));
            Assert.That(definition.Schema["name"], Is.EqualTo("STRING"));
            Assert.That(definition.CatalogName, Is.Null.Or.EqualTo("my_catalog"));
            Assert.That(definition.DatabaseName, Is.Null.Or.EqualTo("my_database"));
        }

        [Test]
        public void TableSourceDefinition_DefaultValues_ShouldBeInitialized()
        {
            // Act
            var definition = new TableSourceDefinition();

            // Assert
            Assert.That(definition.TableName, Is.EqualTo(string.Empty));
            Assert.That(definition.Schema, Is.Not.Null);
        }

        #endregion

        #region WindowTvfOperationDefinition Tests

        [Test]
        public void WindowTvfOperationDefinition_Properties_ShouldGetAndSet()
        {
            // Arrange & Act
            var definition = new WindowTvfOperationDefinition
            {
                WindowType = "TUMBLE",
                TimeColumn = "event_time",
                WindowSize = "INTERVAL '1' HOUR",
                SlideInterval = "INTERVAL '30' MINUTE",
                MaxWindowSize = "INTERVAL '2' HOUR",
                GroupByColumns = new List<string> { "user_id", "category" },
                Aggregations = new List<string> { "COUNT(*) AS count", "SUM(amount) AS total" }
            };

            // Assert
            Assert.That(definition.WindowType, Is.EqualTo("TUMBLE"));
            Assert.That(definition.TimeColumn, Is.EqualTo("event_time"));
            Assert.That(definition.WindowSize, Is.EqualTo("INTERVAL '1' HOUR"));
            Assert.That(definition.SlideInterval, Is.EqualTo("INTERVAL '30' MINUTE"));
            Assert.That(definition.MaxWindowSize, Is.EqualTo("INTERVAL '2' HOUR"));
            Assert.That(definition.GroupByColumns, Has.Count.EqualTo(2));
            Assert.That(definition.GroupByColumns[0], Is.EqualTo("user_id"));
            Assert.That(definition.Aggregations, Has.Count.EqualTo(2));
            Assert.That(definition.Aggregations[0], Is.EqualTo("COUNT(*) AS count"));
        }

        [Test]
        public void WindowTvfOperationDefinition_DefaultValues_ShouldBeInitialized()
        {
            // Act
            var definition = new WindowTvfOperationDefinition();

            // Assert
            Assert.That(definition.WindowType, Is.EqualTo(string.Empty));
            Assert.That(definition.TimeColumn, Is.EqualTo(string.Empty));
            Assert.That(definition.WindowSize, Is.EqualTo(string.Empty));
            Assert.That(definition.SlideInterval, Is.Null.Or.EqualTo(string.Empty));
            Assert.That(definition.MaxWindowSize, Is.Null.Or.EqualTo(string.Empty));
            Assert.That(definition.GroupByColumns, Is.Not.Null);
            Assert.That(definition.Aggregations, Is.Not.Null);
        }

        #endregion

        #region ModelDefinition Additional Properties Tests

        [Test]
        public void ModelDefinition_AllProperties_ShouldGetAndSet()
        {
            // Arrange & Act
            var definition = new ModelDefinition
            {
                ModelName = "my_model",
                Provider = "openai"
            };

            // Assert
            Assert.That(definition.ModelName, Is.EqualTo("my_model"));
            Assert.That(definition.Provider, Is.EqualTo("openai"));
            Assert.That(definition.InputSchema, Is.Not.Null);
            Assert.That(definition.OutputSchema, Is.Not.Null);
            Assert.That(definition.Properties, Is.Not.Null);
        }

        #endregion
    }
}
