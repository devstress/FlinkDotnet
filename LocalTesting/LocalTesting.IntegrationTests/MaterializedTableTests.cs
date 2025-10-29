using System;
using System.Text.Json;
using Flink.JobBuilder.Models;
using FlinkDotNet.DataStream;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

/// <summary>
/// Consolidated integration tests for Materialized Tables (Flink 1.20+ FLIP-435).
/// Tests validate IR schema, C# API, SQL generation, and management operations.
/// Maximum 5 tests per Flink version as per project guidelines.
/// </summary>
[TestFixture]
[Category("materialized-tables")]
public class MaterializedTableTests
{
    #region Test 1: IR Schema and Serialization

    /// <summary>
    /// Test 1: Validates complete IR schema serialization including:
    /// - CREATE operation with all configuration options
    /// - JSON round-trip serialization
    /// - Schema, primary key, partitioning, freshness
    /// - Properties and execution mode
    /// </summary>
    [Test]
    public void Test1_IRSchema_SerializesCompleteDefinition()
    {
        // Arrange - Create materialized table definition with all features
        var jobDef = new JobDefinition
        {
            Source = new MaterializedTableDefinition
            {
                TableName = "dwd_orders",
                Query = @"SELECT 
                    DATE_FORMAT(order_time, 'yyyy-MM-dd') AS ds,
                    order_id,
                    user_id,
                    SUM(amount) AS total_amount
                FROM orders o
                JOIN users u ON o.user_id = u.id
                GROUP BY ds, order_id, user_id",
                RefreshMode = "CONTINUOUS",
                FreshnessInterval = "INTERVAL '3' MINUTE",
                PrimaryKey = { "ds", "order_id" },
                PartitionBy = { "ds" },
                Schema = 
                {
                    { "ds", "STRING" },
                    { "order_id", "BIGINT" },
                    { "user_id", "BIGINT" },
                    { "total_amount", "DECIMAL(10, 2)" }
                },
                Operation = "CREATE",
                ExecutionMode = "gateway",
                Properties = 
                {
                    { "compression", "gzip" },
                    { "format", "parquet" }
                }
            },
            Metadata = new JobMetadata
            {
                JobId = "materialized-table-job",
                JobName = "Materialized Table Test",
                Version = "1.0"
            }
        };

        // Act - Serialize and deserialize
        var json = JsonSerializer.Serialize(jobDef, new JsonSerializerOptions { WriteIndented = true });
        var deserialized = JsonSerializer.Deserialize<JobDefinition>(json);

        // Assert - Structure
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized.Source, Is.InstanceOf<MaterializedTableDefinition>());

        var mtDef = deserialized.Source as MaterializedTableDefinition;
        Assert.That(mtDef, Is.Not.Null);
        Assert.That(mtDef.Type, Is.EqualTo("materialized_table"));
        Assert.That(mtDef.TableName, Is.EqualTo("dwd_orders"));
        Assert.That(mtDef.RefreshMode, Is.EqualTo("CONTINUOUS"));
        Assert.That(mtDef.FreshnessInterval, Is.EqualTo("INTERVAL '3' MINUTE"));
        Assert.That(mtDef.Operation, Is.EqualTo("CREATE"));
        Assert.That(mtDef.ExecutionMode, Is.EqualTo("gateway"));

        // Assert - Collections
        Assert.That(mtDef.PrimaryKey, Has.Count.EqualTo(2));
        Assert.That(mtDef.PrimaryKey, Contains.Item("ds"));
        Assert.That(mtDef.PrimaryKey, Contains.Item("order_id"));
        Assert.That(mtDef.PartitionBy, Has.Count.EqualTo(1));
        Assert.That(mtDef.PartitionBy, Contains.Item("ds"));
        Assert.That(mtDef.Schema, Has.Count.EqualTo(4));
        Assert.That(mtDef.Schema["ds"], Is.EqualTo("STRING"));
        Assert.That(mtDef.Properties, Has.Count.EqualTo(2));
        Assert.That(mtDef.Properties["compression"], Is.EqualTo("gzip"));
    }

    #endregion

    #region Test 2: C# API Builder Pattern

    /// <summary>
    /// Test 2: Validates C# API builder pattern including:
    /// - Fluent API for creating materialized tables
    /// - TimeSpan to SQL interval conversion
    /// - Schema, primary key, partitioning configuration
    /// - Properties and execution mode
    /// </summary>
    [Test]
    public void Test2_CSharpAPI_BuilderCreatesCorrectDefinition()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        // Act - Build materialized table using fluent API
        var table = env.CreateMaterializedTable("fact_sales")
            .WithQuery("SELECT product_id, SUM(quantity) as total_qty FROM sales GROUP BY product_id")
            .WithRefreshMode("FULL")
            .WithFreshness(TimeSpan.FromMinutes(5))
            .WithPrimaryKey("product_id")
            .WithPartitioning("ds", "region")
            .AddColumn("product_id", "BIGINT")
            .AddColumn("total_qty", "BIGINT")
            .WithProperty("format", "orc")
            .WithExecutionMode("gateway")
            .Build();

        // Assert - MaterializedTable properties
        Assert.That(table, Is.Not.Null);
        Assert.That(table.TableName, Is.EqualTo("fact_sales"));
        Assert.That(table.RefreshMode, Is.EqualTo("FULL"));
        Assert.That(table.FreshnessInterval, Is.EqualTo("INTERVAL '5' MINUTE"));

        // Assert - IR definition
        var def = table.Definition;
        Assert.That(def.TableName, Is.EqualTo("fact_sales"));
        Assert.That(def.Query, Does.Contain("SUM(quantity)"));
        Assert.That(def.RefreshMode, Is.EqualTo("FULL"));
        Assert.That(def.FreshnessInterval, Is.EqualTo("INTERVAL '5' MINUTE"));
        Assert.That(def.PrimaryKey, Has.Count.EqualTo(1));
        Assert.That(def.PrimaryKey[0], Is.EqualTo("product_id"));
        Assert.That(def.PartitionBy, Has.Count.EqualTo(2));
        Assert.That(def.Schema, Has.Count.EqualTo(2));
        Assert.That(def.Properties["format"], Is.EqualTo("orc"));
        Assert.That(def.ExecutionMode, Is.EqualTo("gateway"));
    }

    #endregion

    #region Test 3: SQL Generation

    /// <summary>
    /// Test 3: Validates SQL generation including:
    /// - CREATE MATERIALIZED TABLE DDL
    /// - Schema definition with primary key
    /// - Partitioning and freshness interval
    /// - Query AS SELECT clause
    /// </summary>
    [Test]
    public void Test3_SQLGeneration_CreatesValidDDL()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        var table = env.CreateMaterializedTable("dim_products")
            .AddColumn("product_id", "BIGINT")
            .AddColumn("product_name", "STRING")
            .AddColumn("category", "STRING")
            .AddColumn("price", "DECIMAL(10, 2)")
            .WithPrimaryKey("product_id")
            .WithPartitioning("category")
            .WithFreshnessInterval("INTERVAL '1' HOUR")
            .WithQuery(@"SELECT product_id, product_name, category, price 
                        FROM raw_products 
                        WHERE is_active = true")
            .Build();

        // Act
        var sql = table.ToSql();

        // Assert - SQL structure
        Assert.That(sql, Does.Contain("CREATE MATERIALIZED TABLE dim_products"));
        Assert.That(sql, Does.Contain("product_id BIGINT"));
        Assert.That(sql, Does.Contain("product_name STRING"));
        Assert.That(sql, Does.Contain("category STRING"));
        Assert.That(sql, Does.Contain("price DECIMAL(10, 2)"));
        Assert.That(sql, Does.Contain("PRIMARY KEY(product_id) NOT ENFORCED"));
        Assert.That(sql, Does.Contain("PARTITIONED BY (category)"));
        Assert.That(sql, Does.Contain("FRESHNESS = INTERVAL '1' HOUR"));
        Assert.That(sql, Does.Contain("AS"));
        Assert.That(sql, Does.Contain("FROM raw_products"));
    }

    #endregion

    #region Test 4: Management Operations

    /// <summary>
    /// Test 4: Validates management operations including:
    /// - SUSPEND operation
    /// - RESUME operation
    /// - REFRESH PARTITION operation
    /// - DROP operation
    /// - SQL generation for each operation
    /// </summary>
    [Test]
    public void Test4_ManagementOperations_GenerateCorrectSQL()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        var table = env.CreateMaterializedTable("test_table")
            .WithQuery("SELECT * FROM source")
            .Build();

        // Act & Assert - Suspend
        var suspendTable = table.Suspend();
        var suspendSql = suspendTable.ToSql();
        Assert.That(suspendSql, Is.EqualTo("ALTER MATERIALIZED TABLE test_table SUSPEND"));

        // Act & Assert - Resume
        var resumeTable = table.Resume();
        var resumeSql = resumeTable.ToSql();
        Assert.That(resumeSql, Is.EqualTo("ALTER MATERIALIZED TABLE test_table RESUME"));

        // Act & Assert - Refresh Partition
        var refreshTable = table.RefreshPartition("ds='2024-10-27'");
        var refreshSql = refreshTable.ToSql();
        Assert.That(refreshSql, Is.EqualTo("ALTER MATERIALIZED TABLE test_table REFRESH PARTITION (ds='2024-10-27')"));

        // Act & Assert - Drop
        var dropTable = table.Drop();
        var dropSql = dropTable.ToSql();
        Assert.That(dropSql, Is.EqualTo("DROP MATERIALIZED TABLE test_table"));

        // Assert - Original table unchanged
        Assert.That(table.Definition.Operation, Is.EqualTo("CREATE"));
    }

    #endregion

    #region Test 5: Advanced Features and Edge Cases

    /// <summary>
    /// Test 5: Validates advanced features and edge cases including:
    /// - Different refresh modes (FULL vs CONTINUOUS)
    /// - TimeSpan conversions (seconds, minutes, hours, days)
    /// - Multiple partition columns
    /// - Composite primary keys
    /// - Builder validation errors
    /// </summary>
    [Test]
    public void Test5_AdvancedFeatures_HandlesDifferentConfigurations()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        // Part A: FULL refresh mode without freshness
        var fullRefreshTable = env.CreateMaterializedTable("batch_table")
            .WithQuery("SELECT * FROM batch_source")
            .WithRefreshMode("FULL")
            .Build();

        Assert.That(fullRefreshTable.RefreshMode, Is.EqualTo("FULL"));
        Assert.That(fullRefreshTable.Definition.FreshnessInterval, Is.Null);

        // Part B: TimeSpan conversions
        var secondsTable = env.CreateMaterializedTable("t1")
            .WithQuery("SELECT 1")
            .WithFreshness(TimeSpan.FromSeconds(30))
            .Build();
        Assert.That(secondsTable.FreshnessInterval, Is.EqualTo("INTERVAL '30' SECOND"));

        var minutesTable = env.CreateMaterializedTable("t2")
            .WithQuery("SELECT 1")
            .WithFreshness(TimeSpan.FromMinutes(10))
            .Build();
        Assert.That(minutesTable.FreshnessInterval, Is.EqualTo("INTERVAL '10' MINUTE"));

        var hoursTable = env.CreateMaterializedTable("t3")
            .WithQuery("SELECT 1")
            .WithFreshness(TimeSpan.FromHours(2))
            .Build();
        Assert.That(hoursTable.FreshnessInterval, Is.EqualTo("INTERVAL '2' HOUR"));

        var daysTable = env.CreateMaterializedTable("t4")
            .WithQuery("SELECT 1")
            .WithFreshness(TimeSpan.FromDays(1))
            .Build();
        Assert.That(daysTable.FreshnessInterval, Is.EqualTo("INTERVAL '1' DAY"));

        // Part C: Multiple partitions and composite keys
        var complexTable = env.CreateMaterializedTable("complex")
            .WithQuery("SELECT year, month, day, id, value FROM data")
            .AddColumn("year", "INT")
            .AddColumn("month", "INT")
            .AddColumn("day", "INT")
            .AddColumn("id", "BIGINT")
            .AddColumn("value", "DOUBLE")
            .WithPrimaryKey("year", "month", "day", "id")
            .WithPartitioning("year", "month", "day")
            .Build();

        var complexSql = complexTable.ToSql();
        Assert.That(complexSql, Does.Contain("PRIMARY KEY(year, month, day, id) NOT ENFORCED"));
        Assert.That(complexSql, Does.Contain("PARTITIONED BY (year, month, day)"));

        // Part D: Validation errors
        Assert.Throws<InvalidOperationException>(() =>
        {
            env.CreateMaterializedTable("invalid")
                .Build(); // No query or schema
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            new MaterializedTableBuilder("")
                .WithQuery("SELECT 1")
                .Build(); // Empty table name
        });
    }

    #endregion
}
