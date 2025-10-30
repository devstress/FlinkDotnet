using System;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Tests for PaimonCatalog, PaimonCatalogBuilder, PaimonTable, and PaimonTableBuilder
    /// to achieve coverage for Apache Paimon lakehouse integration features.
    /// </summary>
    [TestFixture]
    public class PaimonTests
    {
        [SetUp]
        public void Setup()
        {
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", "http://localhost:8080");
        }

        [TearDown]
        public void TearDown()
        {
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", null);
        }

        #region PaimonCatalogBuilder Tests

        [Test]
        public void PaimonCatalogBuilder_Constructor_ShouldSetCatalogName()
        {
            // Act
            var builder = new PaimonCatalogBuilder("my_catalog");

            // Assert
            Assert.That(builder, Is.Not.Null);
        }

        [Test]
        public void PaimonCatalogBuilder_WithWarehouse_ShouldSetWarehouse()
        {
            // Arrange
            var builder = new PaimonCatalogBuilder("test_catalog");

            // Act
            var result = builder.WithWarehouse("/tmp/paimon");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(builder)); // Fluent API
        }

        [Test]
        public void PaimonCatalogBuilder_WithHiveMetastore_ShouldSetHiveConfDir()
        {
            // Arrange
            var builder = new PaimonCatalogBuilder("test_catalog");

            // Act
            var result = builder.WithHiveMetastore("/etc/hive/conf");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(builder));
        }

        [Test]
        public void PaimonCatalogBuilder_WithHadoopConf_ShouldSetHadoopConfDir()
        {
            // Arrange
            var builder = new PaimonCatalogBuilder("test_catalog");

            // Act
            var result = builder.WithHadoopConf("/etc/hadoop/conf");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(builder));
        }

        [Test]
        public void PaimonCatalogBuilder_WithProperty_ShouldAddProperty()
        {
            // Arrange
            var builder = new PaimonCatalogBuilder("test_catalog");

            // Act
            var result = builder.WithProperty("custom.property", "value");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(builder));
        }

        [Test]
        public void PaimonCatalogBuilder_Build_WithRequiredFields_ShouldSucceed()
        {
            // Arrange
            var builder = new PaimonCatalogBuilder("my_catalog")
                .WithWarehouse("/tmp/paimon");

            // Act
            var catalog = builder.Build();

            // Assert
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Name, Is.EqualTo("my_catalog"));
            Assert.That(catalog.Warehouse, Is.EqualTo("/tmp/paimon"));
        }

        [Test]
        public void PaimonCatalogBuilder_Build_WithoutWarehouse_ShouldThrow()
        {
            // Arrange
            var builder = new PaimonCatalogBuilder("my_catalog");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => builder.Build());
        }

        [Test]
        public void PaimonCatalogBuilder_Build_WithEmptyCatalogName_ShouldThrow()
        {
            // Arrange
            var builder = new PaimonCatalogBuilder("")
                .WithWarehouse("/tmp/paimon");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => builder.Build());
        }

        #endregion

        #region PaimonCatalog Property Tests

        [Test]
        public void PaimonCatalog_Name_ShouldReturnCatalogName()
        {
            // Arrange
            var catalog = new PaimonCatalogBuilder("test_catalog")
                .WithWarehouse("/tmp/warehouse")
                .Build();

            // Assert
            Assert.That(catalog.Name, Is.EqualTo("test_catalog"));
        }

        [Test]
        public void PaimonCatalog_CatalogType_ShouldReturnType()
        {
            // Arrange - Default type is "paimon"
            var catalog = new PaimonCatalogBuilder("test_catalog")
                .WithWarehouse("/tmp/warehouse")
                .Build();

            // Assert
            Assert.That(catalog.CatalogType, Is.EqualTo("paimon"));
        }

        [Test]
        public void PaimonCatalog_CatalogType_WithHiveMetastore_ShouldReturnPaimonGeneric()
        {
            // Arrange
            var catalog = new PaimonCatalogBuilder("test_catalog")
                .WithWarehouse("/tmp/warehouse")
                .WithHiveMetastore("/etc/hive/conf")
                .Build();

            // Assert
            Assert.That(catalog.CatalogType, Is.EqualTo("paimon-generic"));
        }

        [Test]
        public void PaimonCatalog_Warehouse_ShouldReturnWarehousePath()
        {
            // Arrange
            var catalog = new PaimonCatalogBuilder("test_catalog")
                .WithWarehouse("s3://my-bucket/paimon")
                .Build();

            // Assert
            Assert.That(catalog.Warehouse, Is.EqualTo("s3://my-bucket/paimon"));
        }

        [Test]
        public void PaimonCatalog_Definition_ShouldReturnDefinition()
        {
            // Arrange
            var catalog = new PaimonCatalogBuilder("test_catalog")
                .WithWarehouse("/tmp/warehouse")
                .Build();

            // Assert
            Assert.That(catalog.Definition, Is.Not.Null);
            Assert.That(catalog.Definition.CatalogName, Is.EqualTo("test_catalog"));
        }

        [Test]
        public void PaimonCatalog_Builder_StaticMethod_ShouldReturnBuilder()
        {
            // Act
            var builder = PaimonCatalog.Builder("test_catalog");

            // Assert
            Assert.That(builder, Is.Not.Null);
            Assert.That(builder, Is.TypeOf<PaimonCatalogBuilder>());
        }

        #endregion

        #region PaimonCatalog ToSql Tests

        [Test]
        public void PaimonCatalog_ToSql_Basic_ShouldGenerateSQL()
        {
            // Arrange
            var catalog = new PaimonCatalogBuilder("my_catalog")
                .WithWarehouse("/tmp/paimon")
                .Build();

            // Act
            var sql = catalog.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("CREATE CATALOG my_catalog WITH ("));
            Assert.That(sql, Does.Contain("'type' = 'paimon'"));
            Assert.That(sql, Does.Contain("'warehouse' = '/tmp/paimon'"));
        }

        [Test]
        public void PaimonCatalog_ToSql_WithHiveMetastore_ShouldGenerateSQL()
        {
            // Arrange
            var catalog = new PaimonCatalogBuilder("hive_catalog")
                .WithWarehouse("/tmp/paimon")
                .WithHiveMetastore("/etc/hive/conf")
                .Build();

            // Act
            var sql = catalog.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("'type' = 'paimon-generic'"));
            Assert.That(sql, Does.Contain("'hive-conf-dir' = '/etc/hive/conf'"));
        }

        [Test]
        public void PaimonCatalog_ToSql_WithHadoopConf_ShouldGenerateSQL()
        {
            // Arrange
            var catalog = new PaimonCatalogBuilder("hadoop_catalog")
                .WithWarehouse("hdfs://namenode:9000/paimon")
                .WithHadoopConf("/etc/hadoop/conf")
                .Build();

            // Act
            var sql = catalog.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("'hadoop-conf-dir' = '/etc/hadoop/conf'"));
        }

        [Test]
        public void PaimonCatalog_ToSql_WithCustomProperties_ShouldGenerateSQL()
        {
            // Arrange
            var catalog = new PaimonCatalogBuilder("custom_catalog")
                .WithWarehouse("/tmp/paimon")
                .WithProperty("s3.endpoint", "minio:9000")
                .WithProperty("s3.access-key", "minioadmin")
                .Build();

            // Act
            var sql = catalog.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("'s3.endpoint' = 'minio:9000'"));
            Assert.That(sql, Does.Contain("'s3.access-key' = 'minioadmin'"));
        }

        #endregion

        #region PaimonTableBuilder Tests

        [Test]
        public void PaimonTableBuilder_Constructor_ShouldSetNames()
        {
            // Act
            var builder = new PaimonTableBuilder("my_catalog", "my_table");

            // Assert
            Assert.That(builder, Is.Not.Null);
        }

        [Test]
        public void PaimonTableBuilder_WithColumn_ShouldAddColumn()
        {
            // Arrange
            var builder = new PaimonTableBuilder("catalog", "table");

            // Act
            var result = builder.WithColumn("id", "BIGINT");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(builder));
        }

        [Test]
        public void PaimonTableBuilder_WithPrimaryKey_ShouldSetPrimaryKey()
        {
            // Arrange
            var builder = new PaimonTableBuilder("catalog", "table");

            // Act
            var result = builder.WithPrimaryKey("id", "timestamp");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(builder));
        }

        [Test]
        public void PaimonTableBuilder_WithPartition_ShouldSetPartition()
        {
            // Arrange
            var builder = new PaimonTableBuilder("catalog", "table");

            // Act
            var result = builder.WithPartition("date", "region");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(builder));
        }

        [Test]
        public void PaimonTableBuilder_WithBuckets_ShouldSetBuckets()
        {
            // Arrange
            var builder = new PaimonTableBuilder("catalog", "table");

            // Act
            var result = builder.WithBuckets(16);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(builder));
        }

        [Test]
        public void PaimonTableBuilder_WithChangelogMode_None_ShouldSetMode()
        {
            // Arrange
            var builder = new PaimonTableBuilder("catalog", "table");

            // Act
            var result = builder.WithChangelogMode(ChangelogProducerMode.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(builder));
        }

        [Test]
        public void PaimonTableBuilder_WithChangelogMode_Input_ShouldSetMode()
        {
            // Arrange
            var builder = new PaimonTableBuilder("catalog", "table");

            // Act
            var result = builder.WithChangelogMode(ChangelogProducerMode.Input);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void PaimonTableBuilder_WithChangelogMode_Lookup_ShouldSetMode()
        {
            // Arrange
            var builder = new PaimonTableBuilder("catalog", "table");

            // Act
            var result = builder.WithChangelogMode(ChangelogProducerMode.Lookup);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void PaimonTableBuilder_WithChangelogMode_FullCompaction_ShouldSetMode()
        {
            // Arrange
            var builder = new PaimonTableBuilder("catalog", "table");

            // Act
            var result = builder.WithChangelogMode(ChangelogProducerMode.FullCompaction);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void PaimonTableBuilder_WithProperty_ShouldAddProperty()
        {
            // Arrange
            var builder = new PaimonTableBuilder("catalog", "table");

            // Act
            var result = builder.WithProperty("compaction.max-file-num", "50");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(builder));
        }

        [Test]
        public void PaimonTableBuilder_Build_WithRequiredFields_ShouldSucceed()
        {
            // Arrange
            var builder = new PaimonTableBuilder("my_catalog", "my_table")
                .WithColumn("id", "BIGINT")
                .WithColumn("name", "STRING")
                .WithPrimaryKey("id");

            // Act
            var table = builder.Build();

            // Assert
            Assert.That(table, Is.Not.Null);
            Assert.That(table.CatalogName, Is.EqualTo("my_catalog"));
            Assert.That(table.TableName, Is.EqualTo("my_table"));
        }

        [Test]
        public void PaimonTableBuilder_Build_WithoutColumns_ShouldThrow()
        {
            // Arrange
            var builder = new PaimonTableBuilder("catalog", "table")
                .WithPrimaryKey("id");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => builder.Build());
        }

        [Test]
        public void PaimonTableBuilder_Build_WithoutPrimaryKey_ShouldThrow()
        {
            // Arrange
            var builder = new PaimonTableBuilder("catalog", "table")
                .WithColumn("id", "BIGINT");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => builder.Build());
        }

        [Test]
        public void PaimonTableBuilder_Build_WithEmptyCatalogName_ShouldThrow()
        {
            // Arrange
            var builder = new PaimonTableBuilder("", "table")
                .WithColumn("id", "BIGINT")
                .WithPrimaryKey("id");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => builder.Build());
        }

        [Test]
        public void PaimonTableBuilder_Build_WithEmptyTableName_ShouldThrow()
        {
            // Arrange
            var builder = new PaimonTableBuilder("catalog", "")
                .WithColumn("id", "BIGINT")
                .WithPrimaryKey("id");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => builder.Build());
        }

        #endregion

        #region PaimonTable Property Tests

        [Test]
        public void PaimonTable_CatalogName_ShouldReturnCatalogName()
        {
            // Arrange
            var table = new PaimonTableBuilder("test_catalog", "test_table")
                .WithColumn("id", "BIGINT")
                .WithPrimaryKey("id")
                .Build();

            // Assert
            Assert.That(table.CatalogName, Is.EqualTo("test_catalog"));
        }

        [Test]
        public void PaimonTable_TableName_ShouldReturnTableName()
        {
            // Arrange
            var table = new PaimonTableBuilder("catalog", "events")
                .WithColumn("id", "BIGINT")
                .WithPrimaryKey("id")
                .Build();

            // Assert
            Assert.That(table.TableName, Is.EqualTo("events"));
        }

        [Test]
        public void PaimonTable_Definition_ShouldReturnDefinition()
        {
            // Arrange
            var table = new PaimonTableBuilder("catalog", "table")
                .WithColumn("id", "BIGINT")
                .WithPrimaryKey("id")
                .Build();

            // Assert
            Assert.That(table.Definition, Is.Not.Null);
            Assert.That(table.Definition.TableName, Is.EqualTo("table"));
        }

        [Test]
        public void PaimonTable_Builder_StaticMethod_ShouldReturnBuilder()
        {
            // Act
            var builder = PaimonTable.Builder("catalog", "table");

            // Assert
            Assert.That(builder, Is.Not.Null);
            Assert.That(builder, Is.TypeOf<PaimonTableBuilder>());
        }

        #endregion

        #region PaimonTable ToSql Tests

        [Test]
        public void PaimonTable_ToSql_BasicTable_ShouldGenerateSQL()
        {
            // Arrange
            var table = new PaimonTableBuilder("my_catalog", "users")
                .WithColumn("id", "BIGINT")
                .WithColumn("name", "STRING")
                .WithPrimaryKey("id")
                .Build();

            // Act
            var sql = table.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("CREATE TABLE my_catalog.users"));
            Assert.That(sql, Does.Contain("id BIGINT"));
            Assert.That(sql, Does.Contain("name STRING"));
            Assert.That(sql, Does.Contain("PRIMARY KEY (id) NOT ENFORCED"));
        }

        [Test]
        public void PaimonTable_ToSql_WithMultipleColumns_ShouldGenerateSQL()
        {
            // Arrange
            var table = new PaimonTableBuilder("catalog", "events")
                .WithColumn("id", "BIGINT")
                .WithColumn("timestamp", "TIMESTAMP(3)")
                .WithColumn("user_id", "STRING")
                .WithColumn("event_type", "STRING")
                .WithPrimaryKey("id", "timestamp")
                .Build();

            // Act
            var sql = table.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("timestamp TIMESTAMP(3)"));
            Assert.That(sql, Does.Contain("PRIMARY KEY (id, timestamp) NOT ENFORCED"));
        }

        [Test]
        public void PaimonTable_ToSql_WithPartitioning_ShouldGenerateSQL()
        {
            // Arrange
            var table = new PaimonTableBuilder("catalog", "sales")
                .WithColumn("id", "BIGINT")
                .WithColumn("date", "STRING")
                .WithColumn("amount", "DECIMAL(10,2)")
                .WithPrimaryKey("id")
                .WithPartition("date")
                .Build();

            // Act
            var sql = table.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("PARTITIONED BY (date)"));
        }

        [Test]
        public void PaimonTable_ToSql_WithBuckets_ShouldGenerateSQL()
        {
            // Arrange
            var table = new PaimonTableBuilder("catalog", "table")
                .WithColumn("id", "BIGINT")
                .WithPrimaryKey("id")
                .WithBuckets(32)
                .Build();

            // Act
            var sql = table.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("'bucket' = '32'"));
        }

        [Test]
        public void PaimonTable_ToSql_WithChangelogModeInput_ShouldGenerateSQL()
        {
            // Arrange
            var table = new PaimonTableBuilder("catalog", "table")
                .WithColumn("id", "BIGINT")
                .WithPrimaryKey("id")
                .WithChangelogMode(ChangelogProducerMode.Input)
                .Build();

            // Act
            var sql = table.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("'changelog-producer' = 'input'"));
        }

        [Test]
        public void PaimonTable_ToSql_WithChangelogModeFullCompaction_ShouldGenerateSQL()
        {
            // Arrange
            var table = new PaimonTableBuilder("catalog", "table")
                .WithColumn("id", "BIGINT")
                .WithPrimaryKey("id")
                .WithChangelogMode(ChangelogProducerMode.FullCompaction)
                .Build();

            // Act
            var sql = table.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("'changelog-producer' = 'full-compaction'"));
        }

        [Test]
        public void PaimonTable_ToSql_WithCustomProperties_ShouldGenerateSQL()
        {
            // Arrange
            var table = new PaimonTableBuilder("catalog", "table")
                .WithColumn("id", "BIGINT")
                .WithPrimaryKey("id")
                .WithProperty("write-buffer-size", "256m")
                .WithProperty("compaction.max-file-num", "50")
                .Build();

            // Act
            var sql = table.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("'write-buffer-size' = '256m'"));
            Assert.That(sql, Does.Contain("'compaction.max-file-num' = '50'"));
        }

        [Test]
        public void PaimonTable_Drop_ShouldCreateDropOperation()
        {
            // Arrange
            var table = new PaimonTableBuilder("catalog", "table")
                .WithColumn("id", "BIGINT")
                .WithPrimaryKey("id")
                .Build();

            // Act
            var dropped = table.Drop();

            // Assert
            Assert.That(dropped, Is.Not.Null);
            Assert.That(dropped.CatalogName, Is.EqualTo("catalog"));
            Assert.That(dropped.TableName, Is.EqualTo("table"));
        }

        #endregion

        #region Integration Tests

        [Test]
        public void PaimonCatalog_CompleteWorkflow_ShouldBuildAndGenerateSQL()
        {
            // Arrange & Act
            var catalog = new PaimonCatalogBuilder("lakehouse_catalog")
                .WithWarehouse("s3://data-lake/warehouse")
                .WithHadoopConf("/etc/hadoop/conf")
                .WithProperty("s3.endpoint", "http://minio:9000")
                .WithProperty("s3.path-style-access", "true")
                .Build();

            var sql = catalog.ToSql();

            // Assert
            Assert.That(catalog.Name, Is.EqualTo("lakehouse_catalog"));
            Assert.That(sql, Does.Contain("CREATE CATALOG lakehouse_catalog"));
            Assert.That(sql, Does.Contain("'warehouse' = 's3://data-lake/warehouse'"));
            Assert.That(sql, Does.Contain("'s3.endpoint' = 'http://minio:9000'"));
        }

        [Test]
        public void PaimonTable_CompleteWorkflow_ShouldBuildAndGenerateSQL()
        {
            // Arrange & Act
            var table = new PaimonTableBuilder("lakehouse", "user_events")
                .WithColumn("user_id", "BIGINT")
                .WithColumn("event_time", "TIMESTAMP(3)")
                .WithColumn("event_type", "STRING")
                .WithColumn("date", "STRING")
                .WithColumn("region", "STRING")
                .WithPrimaryKey("user_id", "event_time")
                .WithPartition("date", "region")
                .WithBuckets(16)
                .WithChangelogMode(ChangelogProducerMode.Lookup)
                .WithProperty("snapshot.time-retained", "1h")
                .Build();

            var sql = table.ToSql();

            // Assert
            Assert.That(table.CatalogName, Is.EqualTo("lakehouse"));
            Assert.That(table.TableName, Is.EqualTo("user_events"));
            Assert.That(sql, Does.Contain("CREATE TABLE lakehouse.user_events"));
            Assert.That(sql, Does.Contain("PRIMARY KEY (user_id, event_time) NOT ENFORCED"));
            Assert.That(sql, Does.Contain("PARTITIONED BY (date, region)"));
            Assert.That(sql, Does.Contain("'bucket' = '16'"));
            Assert.That(sql, Does.Contain("'changelog-producer' = 'lookup'"));
        }

        #endregion
    }
}
