using System;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Tests for Catalog, CatalogBuilder, Database, and DatabaseBuilder
    /// to achieve 100% coverage for Apache Flink 1.10+ Catalog API features.
    /// </summary>
    [TestFixture]
    public class CatalogTests
    {
        #region CatalogBuilder Tests

        [Test]
        public void CatalogBuilder_Hive_ShouldCreateHiveCatalog()
        {
            // Act
            var builder = Catalog.Hive("my_hive");
            var catalog = builder.WithDefaultDatabase("default")
                .WithHiveConfDir("/etc/hive/conf")
                .WithProperty("hive.metastore.uris", "thrift://localhost:9083")
                .Build();

            // Assert
            Assert.That(catalog.Name, Is.EqualTo("my_hive"));
            Assert.That(catalog.CatalogType, Is.EqualTo("hive"));
            Assert.That(catalog.DefaultDatabase, Is.EqualTo("default"));
            Assert.That(catalog.Definition.Properties, Contains.Key("hive-conf-dir"));
            Assert.That(catalog.Definition.Properties, Contains.Key("hive.metastore.uris"));
        }

        [Test]
        public void CatalogBuilder_Jdbc_ShouldCreateJdbcCatalog()
        {
            // Act
            var builder = Catalog.Jdbc("my_jdbc");
            var catalog = builder.WithDefaultDatabase("mydb")
                .WithJdbcUrl("jdbc:postgresql://localhost:5432/mydb")
                .WithJdbcUsername("user")
                .WithJdbcPassword("pass")
                .Build();

            // Assert
            Assert.That(catalog.Name, Is.EqualTo("my_jdbc"));
            Assert.That(catalog.CatalogType, Is.EqualTo("jdbc"));
            Assert.That(catalog.DefaultDatabase, Is.EqualTo("mydb"));
            Assert.That(catalog.Definition.Properties, Contains.Key("base-url"));
            Assert.That(catalog.Definition.Properties, Contains.Key("username"));
            Assert.That(catalog.Definition.Properties, Contains.Key("password"));
        }

        [Test]
        public void CatalogBuilder_GenericInMemory_ShouldCreateInMemoryCatalog()
        {
            // Act
            var builder = Catalog.GenericInMemory("my_memory");
            var catalog = builder.Build();

            // Assert
            Assert.That(catalog.Name, Is.EqualTo("my_memory"));
            Assert.That(catalog.CatalogType, Is.EqualTo("generic_in_memory"));
        }

        [Test]
        public void CatalogBuilder_WithProperty_ShouldAddProperty()
        {
            // Arrange
            var builder = Catalog.Hive("test");

            // Act
            var result = builder.WithProperty("key1", "value1");
            var catalog = result.Build();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(catalog.Definition.Properties, Contains.Key("key1"));
            Assert.That(catalog.Definition.Properties["key1"], Is.EqualTo("value1"));
        }

        [Test]
        public void CatalogBuilder_WithDefaultDatabase_ShouldSetDefaultDatabase()
        {
            // Arrange
            var builder = Catalog.GenericInMemory("test");

            // Act
            var catalog = builder.WithDefaultDatabase("mydb").Build();

            // Assert
            Assert.That(catalog.DefaultDatabase, Is.EqualTo("mydb"));
        }

        [Test]
        public void CatalogBuilder_WithNullOrWhiteSpaceName_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CatalogBuilder(null!, "hive"));
            Assert.Throws<ArgumentException>(() => new CatalogBuilder("", "hive"));
            Assert.Throws<ArgumentException>(() => new CatalogBuilder(" ", "hive"));
        }

        [Test]
        public void CatalogBuilder_WithNullOrWhiteSpaceType_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CatalogBuilder("test", null!));
            Assert.Throws<ArgumentException>(() => new CatalogBuilder("test", ""));
            Assert.Throws<ArgumentException>(() => new CatalogBuilder("test", " "));
        }

        [Test]
        public void CatalogBuilder_WithDefaultDatabase_WithNullOrWhiteSpace_ShouldThrowArgumentException()
        {
            // Arrange
            var builder = Catalog.Hive("test");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => builder.WithDefaultDatabase(null!));
            Assert.Throws<ArgumentException>(() => builder.WithDefaultDatabase(""));
            Assert.Throws<ArgumentException>(() => builder.WithDefaultDatabase(" "));
        }

        [Test]
        public void CatalogBuilder_WithProperty_WithNullOrWhiteSpaceKey_ShouldThrowArgumentException()
        {
            // Arrange
            var builder = Catalog.Hive("test");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => builder.WithProperty(null!, "value"));
            Assert.Throws<ArgumentException>(() => builder.WithProperty("", "value"));
            Assert.Throws<ArgumentException>(() => builder.WithProperty(" ", "value"));
        }

        [Test]
        public void CatalogBuilder_WithProperty_WithNullOrWhiteSpaceValue_ShouldThrowArgumentException()
        {
            // Arrange
            var builder = Catalog.Hive("test");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => builder.WithProperty("key", null!));
            Assert.Throws<ArgumentException>(() => builder.WithProperty("key", ""));
            Assert.Throws<ArgumentException>(() => builder.WithProperty("key", " "));
        }

        #endregion

        #region Catalog SQL Generation Tests

        [Test]
        public void Catalog_ToSql_HiveCatalog_ShouldGenerateCorrectDDL()
        {
            // Arrange
            var catalog = Catalog.Hive("my_hive")
                .WithDefaultDatabase("default")
                .WithHiveConfDir("/etc/hive/conf")
                .Build();

            // Act
            var sql = catalog.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("CREATE CATALOG my_hive WITH ("));
            Assert.That(sql, Does.Contain("'type' = 'hive'"));
            Assert.That(sql, Does.Contain("'default-database' = 'default'"));
            Assert.That(sql, Does.Contain("'hive-conf-dir' = '/etc/hive/conf'"));
        }

        [Test]
        public void Catalog_ToSql_JdbcCatalog_ShouldGenerateCorrectDDL()
        {
            // Arrange
            var catalog = Catalog.Jdbc("my_jdbc")
                .WithJdbcUrl("jdbc:postgresql://localhost:5432/mydb")
                .WithJdbcUsername("user")
                .WithJdbcPassword("pass")
                .Build();

            // Act
            var sql = catalog.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("CREATE CATALOG my_jdbc WITH ("));
            Assert.That(sql, Does.Contain("'type' = 'jdbc'"));
            Assert.That(sql, Does.Contain("'base-url' = 'jdbc:postgresql://localhost:5432/mydb'"));
            Assert.That(sql, Does.Contain("'username' = 'user'"));
            Assert.That(sql, Does.Contain("'password' = 'pass'"));
        }

        [Test]
        public void Catalog_ToSql_WithoutDefaultDatabase_ShouldNotIncludeDefaultDatabase()
        {
            // Arrange
            var catalog = Catalog.GenericInMemory("test").Build();

            // Act
            var sql = catalog.ToSql();

            // Assert
            Assert.That(sql, Does.Not.Contain("default-database"));
        }

        [Test]
        public void Catalog_UseCatalogSql_ShouldGenerateCorrectStatement()
        {
            // Arrange
            var catalog = Catalog.Hive("my_hive").Build();

            // Act
            var sql = catalog.UseCatalogSql();

            // Assert
            Assert.That(sql, Is.EqualTo("USE CATALOG my_hive"));
        }

        #endregion

        #region DatabaseBuilder Tests

        [Test]
        public void DatabaseBuilder_WithAllOptions_ShouldBuildSuccessfully()
        {
            // Act
            var database = Database.Builder("my_catalog", "my_db")
                .IfNotExists()
                .WithComment("Test database")
                .WithProperty("owner", "admin")
                .Build();

            // Assert
            Assert.That(database.CatalogName, Is.EqualTo("my_catalog"));
            Assert.That(database.DatabaseName, Is.EqualTo("my_db"));
            Assert.That(database.Comment, Is.EqualTo("Test database"));
            Assert.That(database.Definition.IfNotExists, Is.True);
            Assert.That(database.Definition.Properties, Contains.Key("owner"));
        }

        [Test]
        public void DatabaseBuilder_WithNullOrWhiteSpaceCatalogName_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DatabaseBuilder(null!, "db"));
            Assert.Throws<ArgumentException>(() => new DatabaseBuilder("", "db"));
            Assert.Throws<ArgumentException>(() => new DatabaseBuilder(" ", "db"));
        }

        [Test]
        public void DatabaseBuilder_WithNullOrWhiteSpaceDatabaseName_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DatabaseBuilder("cat", null!));
            Assert.Throws<ArgumentException>(() => new DatabaseBuilder("cat", ""));
            Assert.Throws<ArgumentException>(() => new DatabaseBuilder("cat", " "));
        }

        [Test]
        public void DatabaseBuilder_WithComment_WithNullOrWhiteSpace_ShouldThrowArgumentException()
        {
            // Arrange
            var builder = Database.Builder("cat", "db");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => builder.WithComment(null!));
            Assert.Throws<ArgumentException>(() => builder.WithComment(""));
            Assert.Throws<ArgumentException>(() => builder.WithComment(" "));
        }

        [Test]
        public void DatabaseBuilder_WithProperty_WithNullOrWhiteSpaceKey_ShouldThrowArgumentException()
        {
            // Arrange
            var builder = Database.Builder("cat", "db");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => builder.WithProperty(null!, "value"));
            Assert.Throws<ArgumentException>(() => builder.WithProperty("", "value"));
            Assert.Throws<ArgumentException>(() => builder.WithProperty(" ", "value"));
        }

        [Test]
        public void DatabaseBuilder_WithProperty_WithNullOrWhiteSpaceValue_ShouldThrowArgumentException()
        {
            // Arrange
            var builder = Database.Builder("cat", "db");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => builder.WithProperty("key", null!));
            Assert.Throws<ArgumentException>(() => builder.WithProperty("key", ""));
            Assert.Throws<ArgumentException>(() => builder.WithProperty("key", " "));
        }

        #endregion

        #region Database SQL Generation Tests

        [Test]
        public void Database_ToSql_WithAllOptions_ShouldGenerateCorrectDDL()
        {
            // Arrange
            var database = Database.Builder("my_catalog", "my_db")
                .IfNotExists()
                .WithComment("Test database")
                .WithProperty("owner", "admin")
                .Build();

            // Act
            var sql = database.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("CREATE DATABASE IF NOT EXISTS my_catalog.my_db"));
            Assert.That(sql, Does.Contain("COMMENT 'Test database'"));
            Assert.That(sql, Does.Contain("WITH ("));
            Assert.That(sql, Does.Contain("'owner' = 'admin'"));
        }

        [Test]
        public void Database_ToSql_WithoutIfNotExists_ShouldNotIncludeIfNotExists()
        {
            // Arrange
            var database = Database.Builder("my_catalog", "my_db").Build();

            // Act
            var sql = database.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("CREATE DATABASE my_catalog.my_db"));
            Assert.That(sql, Does.Not.Contain("IF NOT EXISTS"));
        }

        [Test]
        public void Database_ToSql_WithoutComment_ShouldNotIncludeComment()
        {
            // Arrange
            var database = Database.Builder("my_catalog", "my_db").Build();

            // Act
            var sql = database.ToSql();

            // Assert
            Assert.That(sql, Does.Not.Contain("COMMENT"));
        }

        [Test]
        public void Database_ToSql_WithoutProperties_ShouldNotIncludeWithClause()
        {
            // Arrange
            var database = Database.Builder("my_catalog", "my_db").Build();

            // Act
            var sql = database.ToSql();

            // Assert
            Assert.That(sql, Does.Not.Contain("WITH ("));
        }

        [Test]
        public void Database_UseDatabaseSql_ShouldGenerateCorrectStatement()
        {
            // Arrange
            var database = Database.Builder("my_catalog", "my_db").Build();

            // Act
            var sql = database.UseDatabaseSql();

            // Assert
            Assert.That(sql, Is.EqualTo("USE my_catalog.my_db"));
        }

        [Test]
        public void Database_DropDatabaseSql_WithDefaults_ShouldGenerateBasicDrop()
        {
            // Arrange
            var database = Database.Builder("my_catalog", "my_db").Build();

            // Act
            var sql = database.DropDatabaseSql();

            // Assert
            Assert.That(sql, Is.EqualTo("DROP DATABASE my_catalog.my_db"));
        }

        [Test]
        public void Database_DropDatabaseSql_WithIfExists_ShouldIncludeIfExists()
        {
            // Arrange
            var database = Database.Builder("my_catalog", "my_db").Build();

            // Act
            var sql = database.DropDatabaseSql(ifExists: true);

            // Assert
            Assert.That(sql, Is.EqualTo("DROP DATABASE IF EXISTS my_catalog.my_db"));
        }

        [Test]
        public void Database_DropDatabaseSql_WithCascade_ShouldIncludeCascade()
        {
            // Arrange
            var database = Database.Builder("my_catalog", "my_db").Build();

            // Act
            var sql = database.DropDatabaseSql(cascade: true);

            // Assert
            Assert.That(sql, Is.EqualTo("DROP DATABASE my_catalog.my_db CASCADE"));
        }

        [Test]
        public void Database_DropDatabaseSql_WithBothOptions_ShouldIncludeBoth()
        {
            // Arrange
            var database = Database.Builder("my_catalog", "my_db").Build();

            // Act
            var sql = database.DropDatabaseSql(ifExists: true, cascade: true);

            // Assert
            Assert.That(sql, Is.EqualTo("DROP DATABASE IF EXISTS my_catalog.my_db CASCADE"));
        }

        #endregion

        #region Integration Tests

        [Test]
        public void Catalog_FullWorkflow_ShouldWorkEndToEnd()
        {
            // Create Hive catalog
            var catalog = Catalog.Hive("my_hive")
                .WithDefaultDatabase("default")
                .WithHiveConfDir("/etc/hive/conf")
                .WithProperty("hive.metastore.uris", "thrift://localhost:9083")
                .Build();

            // Generate CREATE CATALOG DDL
            var createSql = catalog.ToSql();
            Assert.That(createSql, Does.Contain("CREATE CATALOG my_hive"));

            // Generate USE CATALOG statement
            var useSql = catalog.UseCatalogSql();
            Assert.That(useSql, Is.EqualTo("USE CATALOG my_hive"));

            // Verify catalog properties
            Assert.That(catalog.Name, Is.EqualTo("my_hive"));
            Assert.That(catalog.CatalogType, Is.EqualTo("hive"));
            Assert.That(catalog.DefaultDatabase, Is.EqualTo("default"));
        }

        [Test]
        public void Database_FullWorkflow_ShouldWorkEndToEnd()
        {
            // Create database
            var database = Database.Builder("my_catalog", "my_db")
                .IfNotExists()
                .WithComment("Production database")
                .WithProperty("owner", "data_team")
                .Build();

            // Generate CREATE DATABASE DDL
            var createSql = database.ToSql();
            Assert.That(createSql, Does.Contain("CREATE DATABASE IF NOT EXISTS"));
            Assert.That(createSql, Does.Contain("COMMENT 'Production database'"));

            // Generate USE DATABASE statement
            var useSql = database.UseDatabaseSql();
            Assert.That(useSql, Is.EqualTo("USE my_catalog.my_db"));

            // Generate DROP DATABASE statement
            var dropSql = database.DropDatabaseSql(ifExists: true, cascade: true);
            Assert.That(dropSql, Is.EqualTo("DROP DATABASE IF EXISTS my_catalog.my_db CASCADE"));

            // Verify database properties
            Assert.That(database.CatalogName, Is.EqualTo("my_catalog"));
            Assert.That(database.DatabaseName, Is.EqualTo("my_db"));
            Assert.That(database.Comment, Is.EqualTo("Production database"));
        }

        #endregion
    }
}
