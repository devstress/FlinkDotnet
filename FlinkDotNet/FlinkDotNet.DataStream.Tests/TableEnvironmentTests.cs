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

        #region Catalog Management Tests (Flink 1.10+)

        [Test]
        public void TableEnvironment_RegisterCatalog_ShouldRegisterCatalog()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();
            var catalog = Catalog.Hive("my_hive").Build();

            // Act
            tableEnv.RegisterCatalog(catalog);

            // Assert
            var catalogs = tableEnv.ListCatalogs().ToList();
            Assert.That(catalogs, Does.Contain("my_hive"));
        }

        [Test]
        public void TableEnvironment_RegisterCatalog_WithNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => tableEnv.RegisterCatalog(null!));
        }

        [Test]
        public void TableEnvironment_RegisterCatalog_DuplicateName_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();
            var catalog1 = Catalog.Hive("my_catalog").Build();
            var catalog2 = Catalog.Jdbc("my_catalog").Build();
            tableEnv.RegisterCatalog(catalog1);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => tableEnv.RegisterCatalog(catalog2));
        }

        [Test]
        public void TableEnvironment_UseCatalog_ShouldSetCurrentCatalog()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();
            var catalog = Catalog.GenericInMemory("test_catalog").Build();
            tableEnv.RegisterCatalog(catalog);

            // Act
            tableEnv.UseCatalog("test_catalog");

            // Assert
            Assert.That(tableEnv.GetCurrentCatalog(), Is.EqualTo("test_catalog"));
        }

        [Test]
        public void TableEnvironment_UseCatalog_WithNullOrWhiteSpace_ShouldThrowArgumentException()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => tableEnv.UseCatalog(null!));
            Assert.Throws<ArgumentException>(() => tableEnv.UseCatalog(""));
            Assert.Throws<ArgumentException>(() => tableEnv.UseCatalog(" "));
        }

        [Test]
        public void TableEnvironment_UseCatalog_NotRegistered_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => tableEnv.UseCatalog("non_existent"));
        }

        [Test]
        public void TableEnvironment_GetCatalog_ShouldReturnCatalog()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();
            var catalog = Catalog.Hive("my_hive").Build();
            tableEnv.RegisterCatalog(catalog);

            // Act
            var retrieved = tableEnv.GetCatalog("my_hive");

            // Assert
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved!.Name, Is.EqualTo("my_hive"));
        }

        [Test]
        public void TableEnvironment_GetCatalog_NotFound_ShouldReturnNull()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();

            // Act
            var result = tableEnv.GetCatalog("non_existent");

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void TableEnvironment_GetCatalog_WithNullOrWhiteSpace_ShouldThrowArgumentException()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => tableEnv.GetCatalog(null!));
            Assert.Throws<ArgumentException>(() => tableEnv.GetCatalog(""));
            Assert.Throws<ArgumentException>(() => tableEnv.GetCatalog(" "));
        }

        [Test]
        public void TableEnvironment_ListCatalogs_ShouldReturnAllCatalogs()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();
            var catalog1 = Catalog.Hive("cat1").Build();
            var catalog2 = Catalog.Jdbc("cat2").Build();
            tableEnv.RegisterCatalog(catalog1);
            tableEnv.RegisterCatalog(catalog2);

            // Act
            var catalogs = tableEnv.ListCatalogs().ToList();

            // Assert
            Assert.That(catalogs, Has.Count.EqualTo(2));
            Assert.That(catalogs, Does.Contain("cat1"));
            Assert.That(catalogs, Does.Contain("cat2"));
        }

        [Test]
        public void TableEnvironment_GetCurrentCatalog_Initially_ShouldReturnNull()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();

            // Act
            var current = tableEnv.GetCurrentCatalog();

            // Assert
            Assert.That(current, Is.Null);
        }

        #endregion

        #region Database Management Tests (Flink 1.10+)

        [Test]
        public void TableEnvironment_CreateDatabase_ShouldRegisterDatabase()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();
            var database = Database.Builder("my_catalog", "my_db").Build();

            // Act
            tableEnv.CreateDatabase(database);

            // Assert
            var databases = tableEnv.ListDatabases("my_catalog").ToList();
            Assert.That(databases, Does.Contain("my_db"));
        }

        [Test]
        public void TableEnvironment_CreateDatabase_WithNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => tableEnv.CreateDatabase(null!));
        }

        [Test]
        public void TableEnvironment_CreateDatabase_Duplicate_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();
            var database1 = Database.Builder("cat", "db").Build();
            var database2 = Database.Builder("cat", "db").Build();
            tableEnv.CreateDatabase(database1);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => tableEnv.CreateDatabase(database2));
        }

        [Test]
        public void TableEnvironment_UseDatabase_ShouldSetCurrentDatabase()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();
            var database = Database.Builder("my_catalog", "my_db").Build();
            tableEnv.CreateDatabase(database);

            // Act
            tableEnv.UseDatabase("my_catalog", "my_db");

            // Assert
            Assert.That(tableEnv.GetCurrentDatabase(), Is.EqualTo("my_db"));
            Assert.That(tableEnv.GetCurrentCatalog(), Is.EqualTo("my_catalog"));
        }

        [Test]
        public void TableEnvironment_UseDatabase_WithNullOrWhiteSpace_ShouldThrowArgumentException()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => tableEnv.UseDatabase(null!, "db"));
            Assert.Throws<ArgumentException>(() => tableEnv.UseDatabase("", "db"));
            Assert.Throws<ArgumentException>(() => tableEnv.UseDatabase(" ", "db"));
            Assert.Throws<ArgumentNullException>(() => tableEnv.UseDatabase("cat", null!));
            Assert.Throws<ArgumentException>(() => tableEnv.UseDatabase("cat", ""));
            Assert.Throws<ArgumentException>(() => tableEnv.UseDatabase("cat", " "));
        }

        [Test]
        public void TableEnvironment_UseDatabase_NotFound_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => tableEnv.UseDatabase("cat", "db"));
        }

        [Test]
        public void TableEnvironment_GetDatabase_ShouldReturnDatabase()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();
            var database = Database.Builder("my_catalog", "my_db").Build();
            tableEnv.CreateDatabase(database);

            // Act
            var retrieved = tableEnv.GetDatabase("my_catalog", "my_db");

            // Assert
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved!.DatabaseName, Is.EqualTo("my_db"));
        }

        [Test]
        public void TableEnvironment_GetDatabase_NotFound_ShouldReturnNull()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();

            // Act
            var result = tableEnv.GetDatabase("cat", "db");

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void TableEnvironment_GetDatabase_WithNullOrWhiteSpace_ShouldThrowArgumentException()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => tableEnv.GetDatabase(null!, "db"));
            Assert.Throws<ArgumentException>(() => tableEnv.GetDatabase("", "db"));
            Assert.Throws<ArgumentException>(() => tableEnv.GetDatabase(" ", "db"));
            Assert.Throws<ArgumentNullException>(() => tableEnv.GetDatabase("cat", null!));
            Assert.Throws<ArgumentException>(() => tableEnv.GetDatabase("cat", ""));
            Assert.Throws<ArgumentException>(() => tableEnv.GetDatabase("cat", " "));
        }

        [Test]
        public void TableEnvironment_ListDatabases_ShouldReturnAllDatabasesInCatalog()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();
            var db1 = Database.Builder("cat1", "db1").Build();
            var db2 = Database.Builder("cat1", "db2").Build();
            var db3 = Database.Builder("cat2", "db3").Build();
            tableEnv.CreateDatabase(db1);
            tableEnv.CreateDatabase(db2);
            tableEnv.CreateDatabase(db3);

            // Act
            var databases = tableEnv.ListDatabases("cat1").ToList();

            // Assert
            Assert.That(databases, Has.Count.EqualTo(2));
            Assert.That(databases, Does.Contain("db1"));
            Assert.That(databases, Does.Contain("db2"));
            Assert.That(databases, Does.Not.Contain("db3"));
        }

        [Test]
        public void TableEnvironment_ListDatabases_WithNullOrWhiteSpace_ShouldThrowArgumentException()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => tableEnv.ListDatabases(null!));
            Assert.Throws<ArgumentException>(() => tableEnv.ListDatabases(""));
            Assert.Throws<ArgumentException>(() => tableEnv.ListDatabases(" "));
        }

        [Test]
        public void TableEnvironment_GetCurrentDatabase_Initially_ShouldReturnNull()
        {
            // Arrange
            var tableEnv = _env.GetTableEnvironment();

            // Act
            var current = tableEnv.GetCurrentDatabase();

            // Assert
            Assert.That(current, Is.Null);
        }

        #endregion
    }
}
