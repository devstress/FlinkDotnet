using System;
using System.Collections.Generic;
using System.Text.Json;
using Flink.JobBuilder.Models;
using FlinkDotNet.DataStream;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

/// <summary>
/// Consolidated integration tests for Apache Paimon Table Store (Flink 1.15+).
/// Tests validate IR schema, C# API, SQL generation, catalog and table operations.
/// Maximum 5 tests per Flink version as per project guidelines.
/// </summary>
[TestFixture]
[Category("paimon")]
public class PaimonIntegrationTests
{
    #region Test 1: Catalog IR Schema and Serialization

    /// <summary>
    /// Test 1: Validates Paimon catalog IR schema including:
    /// - Filesystem catalog configuration
    /// - Hive metastore catalog configuration
    /// - JSON round-trip serialization
    /// - Catalog properties
    /// </summary>
    [Test]
    public void Test1_CatalogIRSchema_SerializesAllCatalogTypes()
    {
        // Part A: Filesystem catalog
        var filesystemCatalog = new PaimonCatalogDefinition
        {
            CatalogName = "paimon_filesystem",
            CatalogType = "paimon",
            Warehouse = "file:/tmp/paimon",
            Properties =
            {
                { "table-default.scan.parallelism", "4" }
            }
        };

        var json1 = JsonSerializer.Serialize(filesystemCatalog, new JsonSerializerOptions { WriteIndented = true });
        var deserialized1 = JsonSerializer.Deserialize<PaimonCatalogDefinition>(json1);

        Assert.That(deserialized1, Is.Not.Null);
        Assert.That(deserialized1.CatalogName, Is.EqualTo("paimon_filesystem"));
        Assert.That(deserialized1.CatalogType, Is.EqualTo("paimon"));
        Assert.That(deserialized1.Warehouse, Is.EqualTo("file:/tmp/paimon"));
        Assert.That(deserialized1.Properties.Count, Is.EqualTo(1));
        Assert.That(deserialized1.Properties["table-default.scan.parallelism"], Is.EqualTo("4"));

        // Part B: Hive metastore catalog
        var hiveCatalog = new PaimonCatalogDefinition
        {
            CatalogName = "paimon_hive",
            CatalogType = "paimon-generic",
            Warehouse = "hdfs://namenode:8020/warehouse",
            HiveConfDir = "/path/to/hive/conf",
            HadoopConfDir = "/path/to/hadoop/conf",
            Properties =
            {
                { "metastore", "hive" }
            }
        };

        var json2 = JsonSerializer.Serialize(hiveCatalog, new JsonSerializerOptions { WriteIndented = true });
        var deserialized2 = JsonSerializer.Deserialize<PaimonCatalogDefinition>(json2);

        Assert.That(deserialized2, Is.Not.Null);
        Assert.That(deserialized2.CatalogName, Is.EqualTo("paimon_hive"));
        Assert.That(deserialized2.CatalogType, Is.EqualTo("paimon-generic"));
        Assert.That(deserialized2.Warehouse, Is.EqualTo("hdfs://namenode:8020/warehouse"));
        Assert.That(deserialized2.HiveConfDir, Is.EqualTo("/path/to/hive/conf"));
        Assert.That(deserialized2.HadoopConfDir, Is.EqualTo("/path/to/hadoop/conf"));
        Assert.That(deserialized2.Properties["metastore"], Is.EqualTo("hive"));
    }

    #endregion

    #region Test 2: Table IR Schema and Serialization

    /// <summary>
    /// Test 2: Validates Paimon table IR schema including:
    /// - Table schema with primary keys
    /// - Partitioning and bucketing
    /// - Changelog producer modes
    /// - JSON round-trip serialization
    /// - Integration with JobDefinition
    /// </summary>
    [Test]
    public void Test2_TableIRSchema_SerializesCompleteDefinition()
    {
        // Arrange - Create table definition with all features
        var jobDef = new JobDefinition
        {
            Source = new PaimonTableDefinition
            {
                CatalogName = "lakehouse",
                TableName = "orders",
                Schema =
                {
                    { "order_id", "BIGINT" },
                    { "user_id", "BIGINT" },
                    { "amount", "DECIMAL(10,2)" },
                    { "order_time", "TIMESTAMP(3)" },
                    { "dt", "STRING" }
                },
                PrimaryKey = { "dt", "order_id" },
                PartitionKeys = { "dt" },
                Buckets = 4,
                ChangelogProducerMode = "full-compaction",
                TableProperties =
                {
                    { "full-compaction.delta-commits", "2" },
                    { "snapshot.time-retained", "1h" },
                    { "compaction.optimization-interval", "10min" }
                },
                Operation = "CREATE"
            },
            Metadata = new JobMetadata
            {
                                JobName = "Paimon Table Test",
                Version = "1.0"
            }
        };

        // Act - Serialize and deserialize
        var json = JsonSerializer.Serialize(jobDef, new JsonSerializerOptions { WriteIndented = true });
        var deserialized = JsonSerializer.Deserialize<JobDefinition>(json);

        // Assert - Structure
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized.Source, Is.InstanceOf<PaimonTableDefinition>());

        var tableDef = deserialized.Source as PaimonTableDefinition;
        Assert.That(tableDef, Is.Not.Null);

        // Assert - Basic properties
        Assert.That(tableDef.CatalogName, Is.EqualTo("lakehouse"));
        Assert.That(tableDef.TableName, Is.EqualTo("orders"));
        Assert.That(tableDef.Type, Is.EqualTo("paimon_table"));

        // Assert - Schema
        Assert.That(tableDef.Schema.Count, Is.EqualTo(5));
        Assert.That(tableDef.Schema["order_id"], Is.EqualTo("BIGINT"));
        Assert.That(tableDef.Schema["amount"], Is.EqualTo("DECIMAL(10,2)"));

        // Assert - Primary key and partitions
        Assert.That(tableDef.PrimaryKey, Has.Count.EqualTo(2));
        Assert.That(tableDef.PrimaryKey[0], Is.EqualTo("dt"));
        Assert.That(tableDef.PrimaryKey[1], Is.EqualTo("order_id"));
        Assert.That(tableDef.PartitionKeys, Has.Count.EqualTo(1));
        Assert.That(tableDef.PartitionKeys[0], Is.EqualTo("dt"));

        // Assert - Buckets and changelog
        Assert.That(tableDef.Buckets, Is.EqualTo(4));
        Assert.That(tableDef.ChangelogProducerMode, Is.EqualTo("full-compaction"));

        // Assert - Table properties
        Assert.That(tableDef.TableProperties.Count, Is.EqualTo(3));
        Assert.That(tableDef.TableProperties["full-compaction.delta-commits"], Is.EqualTo("2"));

        // Part B: Test direct manipulation of definition to cover FULLCOMPACTION conversion
        // Create a table and modify its definition to test the ToSql conversion logic
        var testTable = PaimonTable.Builder("cat", "tbl")
            .WithColumn("id", "BIGINT")
            .WithPrimaryKey("id")
            .Build();
        
        // Access and modify the definition to test edge case
        var testDef = testTable.Definition;
        testDef.ChangelogProducerMode = "FULLCOMPACTION"; // Set uppercase to test conversion
        var modifiedTable = new PaimonTable(testDef);
        var sqlModified = modifiedTable.ToSql();
        Assert.That(sqlModified, Does.Contain("'changelog-producer' = 'full-compaction'"));
    }

    #endregion

    #region Test 3: Catalog C# API and SQL Generation

    /// <summary>
    /// Test 3: Validates Paimon catalog C# API including:
    /// - Builder pattern for filesystem catalog
    /// - Builder pattern for Hive metastore catalog
    /// - SQL DDL generation for both catalog types
    /// - Property configuration
    /// </summary>
    [Test]
    public void Test3_CatalogAPI_GeneratesCorrectDDL()
    {
        // Part A: Filesystem catalog
        var filesystemCatalog = PaimonCatalog.Builder("my_catalog")
            .WithWarehouse("file:/tmp/paimon")
            .WithProperty("table-default.scan.parallelism", "4")
            .Build();

        Assert.That(filesystemCatalog.Name, Is.EqualTo("my_catalog"));
        Assert.That(filesystemCatalog.CatalogType, Is.EqualTo("paimon"));
        Assert.That(filesystemCatalog.Warehouse, Is.EqualTo("file:/tmp/paimon"));

        var sql1 = filesystemCatalog.ToSql();
        Assert.That(sql1, Does.Contain("CREATE CATALOG my_catalog WITH ("));
        Assert.That(sql1, Does.Contain("'type' = 'paimon'"));
        Assert.That(sql1, Does.Contain("'warehouse' = 'file:/tmp/paimon'"));
        Assert.That(sql1, Does.Contain("'table-default.scan.parallelism' = '4'"));

        // Part B: Hive metastore catalog
        var hiveCatalog = PaimonCatalog.Builder("hive_catalog")
            .WithWarehouse("hdfs://namenode:8020/warehouse")
            .WithHiveMetastore("/path/to/hive/conf")
            .WithHadoopConf("/path/to/hadoop/conf")
            .Build();

        Assert.That(hiveCatalog.Name, Is.EqualTo("hive_catalog"));
        Assert.That(hiveCatalog.CatalogType, Is.EqualTo("paimon-generic"));

        var sql2 = hiveCatalog.ToSql();
        Assert.That(sql2, Does.Contain("CREATE CATALOG hive_catalog WITH ("));
        Assert.That(sql2, Does.Contain("'type' = 'paimon-generic'"));
        Assert.That(sql2, Does.Contain("'warehouse' = 'hdfs://namenode:8020/warehouse'"));
        Assert.That(sql2, Does.Contain("'hive-conf-dir' = '/path/to/hive/conf'"));
        Assert.That(sql2, Does.Contain("'hadoop-conf-dir' = '/path/to/hadoop/conf'"));

        // Part C: Validation - missing warehouse
        Assert.Throws<ArgumentException>(() =>
        {
            PaimonCatalog.Builder("test_catalog").Build();
        });

        // Part D: Validation - missing catalog name
        Assert.Throws<ArgumentException>(() =>
        {
            new PaimonCatalogBuilder("").WithWarehouse("file:/tmp").Build();
        });
    }

    #endregion

    #region Test 4: Table C# API and SQL Generation

    /// <summary>
    /// Test 4: Validates Paimon table C# API including:
    /// - Builder pattern for table creation
    /// - Primary key enforcement
    /// - Partitioning and bucketing
    /// - All 4 changelog producer modes
    /// - SQL DDL generation with all features
    /// </summary>
    [Test]
    public void Test4_TableAPI_GeneratesCorrectDDLForAllConfigurations()
    {
        // Part A: Basic table with primary key
        var basicTable = PaimonTable.Builder("catalog1", "users")
            .WithColumn("user_id", "BIGINT")
            .WithColumn("name", "STRING")
            .WithColumn("email", "STRING")
            .WithPrimaryKey("user_id")
            .Build();

        Assert.That(basicTable.CatalogName, Is.EqualTo("catalog1"));
        Assert.That(basicTable.TableName, Is.EqualTo("users"));

        var sql1 = basicTable.ToSql();
        Assert.That(sql1, Does.Contain("CREATE TABLE catalog1.users ("));
        Assert.That(sql1, Does.Contain("user_id BIGINT"));
        Assert.That(sql1, Does.Contain("name STRING"));
        Assert.That(sql1, Does.Contain("email STRING"));
        Assert.That(sql1, Does.Contain("PRIMARY KEY (user_id) NOT ENFORCED"));

        // Part B: Partitioned table with buckets
        var partitionedTable = PaimonTable.Builder("catalog2", "orders")
            .WithColumn("order_id", "BIGINT")
            .WithColumn("user_id", "BIGINT")
            .WithColumn("amount", "DECIMAL(10,2)")
            .WithColumn("dt", "STRING")
            .WithPrimaryKey("dt", "order_id")
            .WithPartition("dt")
            .WithBuckets(4)
            .Build();

        var sql2 = partitionedTable.ToSql();
        Assert.That(sql2, Does.Contain("PRIMARY KEY (dt, order_id) NOT ENFORCED"));
        Assert.That(sql2, Does.Contain("PARTITIONED BY (dt)"));
        Assert.That(sql2, Does.Contain("'bucket' = '4'"));

        // Part C: Test all 4 changelog modes
        var changelogModes = new[]
        {
            (ChangelogProducerMode.None, "none"),
            (ChangelogProducerMode.Input, "input"),
            (ChangelogProducerMode.Lookup, "lookup"),
            (ChangelogProducerMode.FullCompaction, "full-compaction")
        };

        foreach (var (mode, expectedSql) in changelogModes)
        {
            var table = PaimonTable.Builder("cat", "tbl")
                .WithColumn("id", "BIGINT")
                .WithPrimaryKey("id")
                .WithChangelogMode(mode)
                .Build();

            var sql = table.ToSql();
            
            if (mode == ChangelogProducerMode.None)
            {
                // None mode should not appear in WITH clause
                Assert.That(sql, Does.Not.Contain("'changelog-producer'"));
            }
            else
            {
                Assert.That(sql, Does.Contain($"'changelog-producer' = '{expectedSql}'"));
            }
        }

        // Part D: Table with custom properties
        var tableWithProps = PaimonTable.Builder("catalog3", "events")
            .WithColumn("event_id", "BIGINT")
            .WithColumn("event_time", "TIMESTAMP(3)")
            .WithPrimaryKey("event_id")
            .WithChangelogMode(ChangelogProducerMode.FullCompaction)
            .WithProperty("full-compaction.delta-commits", "2")
            .WithProperty("snapshot.time-retained", "1h")
            .Build();

        var sql4 = tableWithProps.ToSql();
        Assert.That(sql4, Does.Contain("'changelog-producer' = 'full-compaction'"));
        Assert.That(sql4, Does.Contain("'full-compaction.delta-commits' = '2'"));
        Assert.That(sql4, Does.Contain("'snapshot.time-retained' = '1h'"));

        // Part E: Validation tests
        Assert.Throws<ArgumentException>(() =>
        {
            PaimonTable.Builder("cat", "tbl").Build(); // No columns
        });

        Assert.Throws<ArgumentException>(() =>
        {
            PaimonTable.Builder("cat", "tbl")
                .WithColumn("id", "BIGINT")
                .Build(); // No primary key
        });

        Assert.Throws<ArgumentException>(() =>
        {
            PaimonTable.Builder("", "tbl")
                .WithColumn("id", "BIGINT")
                .WithPrimaryKey("id")
                .Build(); // Empty catalog name
        });

        Assert.Throws<ArgumentException>(() =>
        {
            PaimonTable.Builder("cat", "")
                .WithColumn("id", "BIGINT")
                .WithPrimaryKey("id")
                .Build(); // Empty table name
        });
    }

    #endregion

    #region Test 5: Complete Workflow and Drop Operations

    /// <summary>
    /// Test 5: Validates complete Paimon workflow including:
    /// - Catalog and table definition access
    /// - Drop table operation
    /// - Complex table configurations
    /// - Multi-column primary keys and partitions
    /// </summary>
    [Test]
    public void Test5_CompleteWorkflow_CatalogTableAndOperations()
    {
        // Part A: Create catalog and access definition
        var catalog = PaimonCatalog.Builder("production")
            .WithWarehouse("s3://my-bucket/warehouse")
            .WithProperty("fs.s3a.access.key", "AKIAIOSFODNN7EXAMPLE")
            .WithProperty("fs.s3a.secret.key", "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY")
            .Build();

        Assert.That(catalog.Definition, Is.Not.Null);
        Assert.That(catalog.Definition.CatalogName, Is.EqualTo("production"));
        Assert.That(catalog.Definition.Warehouse, Is.EqualTo("s3://my-bucket/warehouse"));
        Assert.That(catalog.Definition.Properties.Count, Is.EqualTo(2));

        // Part B: Create complex table
        var table = PaimonTable.Builder("production", "fact_sales")
            .WithColumn("sale_id", "BIGINT")
            .WithColumn("product_id", "BIGINT")
            .WithColumn("customer_id", "BIGINT")
            .WithColumn("quantity", "INT")
            .WithColumn("amount", "DECIMAL(10,2)")
            .WithColumn("sale_time", "TIMESTAMP(3)")
            .WithColumn("region", "STRING")
            .WithColumn("dt", "STRING")
            .WithPrimaryKey("dt", "region", "sale_id")
            .WithPartition("dt", "region")
            .WithBuckets(8)
            .WithChangelogMode(ChangelogProducerMode.Lookup)
            .WithProperty("lookup.cache-file-retention", "2h")
            .WithProperty("lookup.cache-max-memory-size", "512mb")
            .Build();

        Assert.That(table.Definition, Is.Not.Null);
        Assert.That(table.Definition.Schema.Count, Is.EqualTo(8));
        Assert.That(table.Definition.PrimaryKey.Count, Is.EqualTo(3));
        Assert.That(table.Definition.PartitionKeys.Count, Is.EqualTo(2));

        var createSql = table.ToSql();
        Assert.That(createSql, Does.Contain("PRIMARY KEY (dt, region, sale_id) NOT ENFORCED"));
        Assert.That(createSql, Does.Contain("PARTITIONED BY (dt, region)"));
        Assert.That(createSql, Does.Contain("'bucket' = '8'"));
        Assert.That(createSql, Does.Contain("'changelog-producer' = 'lookup'"));
        Assert.That(createSql, Does.Contain("'lookup.cache-file-retention' = '2h'"));

        // Part C: Drop table operation
        var dropTable = table.Drop();
        Assert.That(dropTable, Is.Not.Null);
        Assert.That(dropTable.Definition.Operation, Is.EqualTo("DROP"));
        Assert.That(dropTable.Definition.TableName, Is.EqualTo("fact_sales"));
        Assert.That(dropTable.Definition.CatalogName, Is.EqualTo("production"));

        // Part D: Verify builder can be reused
        var anotherTable = PaimonTable.Builder("production", "dim_products")
            .WithColumn("product_id", "BIGINT")
            .WithColumn("product_name", "STRING")
            .WithColumn("category", "STRING")
            .WithPrimaryKey("product_id")
            .WithChangelogMode(ChangelogProducerMode.Input)
            .Build();

        Assert.That(anotherTable.TableName, Is.EqualTo("dim_products"));
        Assert.That(anotherTable.Definition.ChangelogProducerMode, Is.EqualTo("input"));

        // Part E: Test edge cases for branch coverage
        // Test table with only buckets (no changelog mode, no properties)
        var tableBucketsOnly = PaimonTable.Builder("cat", "buckets_only")
            .WithColumn("id", "BIGINT")
            .WithPrimaryKey("id")
            .WithBuckets(2)
            .Build();
        var sqlBucketsOnly = tableBucketsOnly.ToSql();
        Assert.That(sqlBucketsOnly, Does.Contain("'bucket' = '2'"));
        Assert.That(sqlBucketsOnly, Does.Not.Contain("'changelog-producer'"));

        // Test table with only properties (no buckets, no changelog mode)
        var tablePropsOnly = PaimonTable.Builder("cat", "props_only")
            .WithColumn("id", "BIGINT")
            .WithPrimaryKey("id")
            .WithProperty("compaction.optimization-interval", "5min")
            .Build();
        var sqlPropsOnly = tablePropsOnly.ToSql();
        Assert.That(sqlPropsOnly, Does.Contain("'compaction.optimization-interval' = '5min'"));
        Assert.That(sqlPropsOnly, Does.Not.Contain("'bucket'"));
        Assert.That(sqlPropsOnly, Does.Not.Contain("'changelog-producer'"));

        // Test table with changelog mode = "none" (should not appear in SQL)
        var tableNoneMode = PaimonTable.Builder("cat", "none_mode")
            .WithColumn("id", "BIGINT")
            .WithPrimaryKey("id")
            .WithChangelogMode(ChangelogProducerMode.None)
            .Build();
        var sqlNoneMode = tableNoneMode.ToSql();
        Assert.That(sqlNoneMode, Does.Not.Contain("WITH ("));
        Assert.That(sqlNoneMode, Does.Not.Contain("'changelog-producer'"));
    }

    #endregion
}
