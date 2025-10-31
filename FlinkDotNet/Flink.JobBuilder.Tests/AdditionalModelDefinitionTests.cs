using Flink.JobBuilder.Models;
using NUnit.Framework;

namespace Flink.JobBuilder.Tests
{
    [TestFixture]
    public sealed class AdditionalModelDefinitionTests
    {
        #region CatalogDefinition Tests

        [Test]
        public void CatalogDefinition_Properties_CanBeSetAndRetrieved()
        {
            // Arrange & Act
            var catalog = new CatalogDefinition
            {
                CatalogName = "test_catalog",
                CatalogType = "hive",
                DefaultDatabase = "test_db"
            };

            // Assert
            Assert.That(catalog.CatalogName, Is.EqualTo("test_catalog"));
            Assert.That(catalog.CatalogType, Is.EqualTo("hive"));
            Assert.That(catalog.DefaultDatabase, Is.EqualTo("test_db"));
        }

        [Test]
        public void CatalogDefinition_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var catalog = new CatalogDefinition();

            // Assert
            Assert.That(catalog.CatalogName, Is.EqualTo(string.Empty));
            Assert.That(catalog.CatalogType, Is.EqualTo(string.Empty));
            Assert.That(catalog.DefaultDatabase, Is.Null);
        }

        [Test]
        public void CatalogDefinition_SupportsDifferentCatalogTypes()
        {
            // Test hive catalog
            var hiveCatalog = new CatalogDefinition
            {
                CatalogName = "hive_catalog",
                CatalogType = "hive"
            };
            Assert.That(hiveCatalog.CatalogType, Is.EqualTo("hive"));

            // Test JDBC catalog
            var jdbcCatalog = new CatalogDefinition
            {
                CatalogName = "jdbc_catalog",
                CatalogType = "jdbc"
            };
            Assert.That(jdbcCatalog.CatalogType, Is.EqualTo("jdbc"));

            // Test generic catalog
            var genericCatalog = new CatalogDefinition
            {
                CatalogName = "memory_catalog",
                CatalogType = "generic_in_memory"
            };
            Assert.That(genericCatalog.CatalogType, Is.EqualTo("generic_in_memory"));
        }

        #endregion

        #region DatabaseDefinition Tests

        [Test]
        public void DatabaseDefinition_Properties_CanBeSetAndRetrieved()
        {
            // Arrange & Act
            var database = new DatabaseDefinition
            {
                CatalogName = "catalog1",
                DatabaseName = "database1",
                IfNotExists = true
            };

            // Assert
            Assert.That(database.CatalogName, Is.EqualTo("catalog1"));
            Assert.That(database.DatabaseName, Is.EqualTo("database1"));
            Assert.That(database.IfNotExists, Is.True);
        }

        [Test]
        public void DatabaseDefinition_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var database = new DatabaseDefinition();

            // Assert
            Assert.That(database.CatalogName, Is.EqualTo(string.Empty));
            Assert.That(database.DatabaseName, Is.EqualTo(string.Empty));
            Assert.That(database.IfNotExists, Is.False);
        }

        [Test]
        public void DatabaseDefinition_IfNotExists_CanBeSetToFalse()
        {
            // Arrange & Act
            var database = new DatabaseDefinition
            {
                CatalogName = "hive_catalog",
                DatabaseName = "production_db",
                IfNotExists = false
            };

            // Assert
            Assert.That(database.IfNotExists, Is.False);
        }

        #endregion

        #region UnifiedSourceDefinition Tests

        [Test]
        public void UnifiedSourceDefinition_Type_ReturnsUnifiedSource()
        {
            // Arrange & Act
            var source = new UnifiedSourceDefinition();

            // Assert
            Assert.That(source.Type, Is.EqualTo("unifiedSource"));
        }

        [Test]
        public void UnifiedSourceDefinition_Properties_CanBeSetAndRetrieved()
        {
            // Arrange & Act
            var source = new UnifiedSourceDefinition
            {
                SourceType = "kafka",
                Boundedness = "unbounded"
            };

            // Assert
            Assert.That(source.SourceType, Is.EqualTo("kafka"));
            Assert.That(source.Boundedness, Is.EqualTo("unbounded"));
        }

        [Test]
        public void UnifiedSourceDefinition_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var source = new UnifiedSourceDefinition();

            // Assert
            Assert.That(source.Type, Is.EqualTo("unifiedSource"));
            Assert.That(source.SourceType, Is.EqualTo(string.Empty));
            Assert.That(source.Boundedness, Is.EqualTo("unbounded"));
        }

        [Test]
        public void UnifiedSourceDefinition_SupportsDifferentSourceTypes()
        {
            // Test Kafka source
            var kafkaSource = new UnifiedSourceDefinition
            {
                SourceType = "kafka",
                Boundedness = "unbounded"
            };
            Assert.That(kafkaSource.SourceType, Is.EqualTo("kafka"));
            Assert.That(kafkaSource.Boundedness, Is.EqualTo("unbounded"));

            // Test File source
            var fileSource = new UnifiedSourceDefinition
            {
                SourceType = "file",
                Boundedness = "bounded"
            };
            Assert.That(fileSource.SourceType, Is.EqualTo("file"));
            Assert.That(fileSource.Boundedness, Is.EqualTo("bounded"));
        }

        [Test]
        public void UnifiedSourceDefinition_ImplementsISourceDefinition()
        {
            // Arrange & Act
            var source = new UnifiedSourceDefinition();

            // Assert
            Assert.That(source, Is.InstanceOf<ISourceDefinition>());
        }

        #endregion

        #region MaterializedTableDefinition Tests

        [Test]
        public void MaterializedTableDefinition_Properties_CanBeSet()
        {
            // Arrange & Act
            var tableDefinition = new MaterializedTableDefinition
            {
                TableName = "test_materialized_table",
                Query = "SELECT * FROM source_table"
            };

            // Assert
            Assert.That(tableDefinition.Type, Is.EqualTo("materialized_table"));
            Assert.That(tableDefinition.TableName, Is.EqualTo("test_materialized_table"));
            Assert.That(tableDefinition.Query, Is.EqualTo("SELECT * FROM source_table"));
        }

        #endregion

        #region MLPredictDefinition Tests

        [Test]
        public void MLPredictDefinition_Properties_CanBeSet()
        {
            // Arrange & Act
            var mlPredict = new MLPredictDefinition
            {
                ModelName = "sentiment_model",
                InputColumns = new System.Collections.Generic.List<string> { "text_column", "category" }
            };

            // Assert
            Assert.That(mlPredict.Type, Is.EqualTo("ml_predict"));
            Assert.That(mlPredict.ModelName, Is.EqualTo("sentiment_model"));
            Assert.That(mlPredict.InputColumns.Count, Is.EqualTo(2));
        }

        #endregion

        #region ParseJsonOperationDefinition Tests

        [Test]
        public void ParseJsonOperationDefinition_Properties_CanBeSet()
        {
            // Arrange & Act
            var parseJson = new ParseJsonOperationDefinition
            {
                FunctionType = "TRY_PARSE_JSON",
                SourceField = "json_data"
            };

            // Assert
            Assert.That(parseJson.Type, Is.EqualTo("parseJson"));
            Assert.That(parseJson.FunctionType, Is.EqualTo("TRY_PARSE_JSON"));
            Assert.That(parseJson.SourceField, Is.EqualTo("json_data"));
        }

        #endregion

        #region TableSourceDefinition Tests

        [Test]
        public void TableSourceDefinition_Properties_CanBeSet()
        {
            // Arrange & Act
            var tableSource = new TableSourceDefinition
            {
                TableName = "source_table",
                CatalogName = "default_catalog"
            };

            // Assert
            Assert.That(tableSource.Type, Is.EqualTo("table"));
            Assert.That(tableSource.TableName, Is.EqualTo("source_table"));
            Assert.That(tableSource.CatalogName, Is.EqualTo("default_catalog"));
        }

        #endregion
    }
}
