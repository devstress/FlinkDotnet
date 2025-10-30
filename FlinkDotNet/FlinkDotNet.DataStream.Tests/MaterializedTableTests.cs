using System;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Tests for MaterializedTable and MaterializedTableBuilder to achieve coverage.
    /// Tests the builder pattern, SQL generation, and operations.
    /// </summary>
    [TestFixture]
    public class MaterializedTableTests
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

        #region MaterializedTableBuilder Tests

        [Test]
        public void MaterializedTableBuilder_WithQuery_ShouldSetQuery()
        {
            // Arrange
            var builder = new MaterializedTableBuilder("test_table");
            var query = "SELECT * FROM source_table";

            // Act
            var result = builder.WithQuery(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(builder)); // Fluent API check
        }

        [Test]
        public void MaterializedTableBuilder_WithRefreshMode_ShouldSetMode()
        {
            // Arrange
            var builder = new MaterializedTableBuilder("test_table");

            // Act
            var result = builder.WithRefreshMode("CONTINUOUS");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(builder));
        }

        [Test]
        public void MaterializedTableBuilder_WithFreshness_Days_ShouldSetFreshnessInterval()
        {
            // Arrange
            var builder = new MaterializedTableBuilder("test_table");
            var interval = TimeSpan.FromDays(2);

            // Act
            var result = builder.WithFreshness(interval);
            var table = builder.WithQuery("SELECT 1").Build();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(table.FreshnessInterval, Is.EqualTo("INTERVAL '2' DAY"));
        }

        [Test]
        public void MaterializedTableBuilder_WithFreshness_Hours_ShouldSetFreshnessInterval()
        {
            // Arrange
            var builder = new MaterializedTableBuilder("test_table");
            var interval = TimeSpan.FromHours(3);

            // Act
            var result = builder.WithFreshness(interval);
            var table = builder.WithQuery("SELECT 1").Build();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(table.FreshnessInterval, Is.EqualTo("INTERVAL '3' HOUR"));
        }

        [Test]
        public void MaterializedTableBuilder_WithFreshness_Minutes_ShouldSetFreshnessInterval()
        {
            // Arrange
            var builder = new MaterializedTableBuilder("test_table");
            var interval = TimeSpan.FromMinutes(30);

            // Act
            var result = builder.WithFreshness(interval);
            var table = builder.WithQuery("SELECT 1").Build();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(table.FreshnessInterval, Is.EqualTo("INTERVAL '30' MINUTE"));
        }

        [Test]
        public void MaterializedTableBuilder_WithFreshness_Seconds_ShouldSetFreshnessInterval()
        {
            // Arrange
            var builder = new MaterializedTableBuilder("test_table");
            var interval = TimeSpan.FromSeconds(45);

            // Act
            var result = builder.WithFreshness(interval);
            var table = builder.WithQuery("SELECT 1").Build();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(table.FreshnessInterval, Is.EqualTo("INTERVAL '45' SECOND"));
        }

        [Test]
        public void MaterializedTableBuilder_WithFreshnessInterval_ShouldSetInterval()
        {
            // Arrange
            var builder = new MaterializedTableBuilder("test_table");

            // Act
            var result = builder.WithFreshnessInterval("INTERVAL '10' MINUTE");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(builder));
        }

        [Test]
        public void MaterializedTableBuilder_WithPrimaryKey_ShouldSetPrimaryKey()
        {
            // Arrange
            var builder = new MaterializedTableBuilder("test_table");

            // Act
            var result = builder.WithPrimaryKey("id", "timestamp");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(builder));
        }

        [Test]
        public void MaterializedTableBuilder_WithPartitioning_ShouldSetPartitioning()
        {
            // Arrange
            var builder = new MaterializedTableBuilder("test_table");

            // Act
            var result = builder.WithPartitioning("date", "region");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(builder));
        }

        [Test]
        public void MaterializedTableBuilder_AddColumn_ShouldAddColumn()
        {
            // Arrange
            var builder = new MaterializedTableBuilder("test_table");

            // Act
            var result = builder.AddColumn("id", "BIGINT")
                                .AddColumn("name", "STRING");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(builder));
        }

        [Test]
        public void MaterializedTableBuilder_WithProperty_ShouldSetProperty()
        {
            // Arrange
            var builder = new MaterializedTableBuilder("test_table");

            // Act
            var result = builder.WithProperty("connector", "kafka");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(builder));
        }

        [Test]
        public void MaterializedTableBuilder_WithExecutionMode_ShouldSetExecutionMode()
        {
            // Arrange
            var builder = new MaterializedTableBuilder("test_table");

            // Act
            var result = builder.WithExecutionMode("gateway");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(builder));
        }

        [Test]
        public void MaterializedTableBuilder_Build_WithQuery_ShouldCreateTable()
        {
            // Arrange
            var builder = new MaterializedTableBuilder("test_table")
                .WithQuery("SELECT * FROM source");

            // Act
            var table = builder.Build();

            // Assert
            Assert.That(table, Is.Not.Null);
            Assert.That(table.TableName, Is.EqualTo("test_table"));
        }

        [Test]
        public void MaterializedTableBuilder_Build_WithSchema_ShouldCreateTable()
        {
            // Arrange
            var builder = new MaterializedTableBuilder("test_table")
                .AddColumn("id", "BIGINT")
                .AddColumn("name", "STRING");

            // Act
            var table = builder.Build();

            // Assert
            Assert.That(table, Is.Not.Null);
            Assert.That(table.TableName, Is.EqualTo("test_table"));
        }

        [Test]
        public void MaterializedTableBuilder_Build_WithoutQueryOrSchema_ShouldThrow()
        {
            // Arrange
            var builder = new MaterializedTableBuilder("test_table");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => builder.Build());
        }

        [Test]
        public void MaterializedTableBuilder_Build_WithEmptyTableName_ShouldThrow()
        {
            // Arrange
            var builder = new MaterializedTableBuilder("")
                .WithQuery("SELECT 1");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => builder.Build());
        }

        #endregion

        #region MaterializedTable Property Tests

        [Test]
        public void MaterializedTable_TableName_ShouldReturnTableName()
        {
            // Arrange
            var table = new MaterializedTableBuilder("my_table")
                .WithQuery("SELECT 1")
                .Build();

            // Assert
            Assert.That(table.TableName, Is.EqualTo("my_table"));
        }

        [Test]
        public void MaterializedTable_RefreshMode_ShouldReturnRefreshMode()
        {
            // Arrange
            var table = new MaterializedTableBuilder("my_table")
                .WithQuery("SELECT 1")
                .WithRefreshMode("FULL")
                .Build();

            // Assert
            Assert.That(table.RefreshMode, Is.EqualTo("FULL"));
        }

        [Test]
        public void MaterializedTable_FreshnessInterval_ShouldReturnInterval()
        {
            // Arrange
            var table = new MaterializedTableBuilder("my_table")
                .WithQuery("SELECT 1")
                .WithFreshnessInterval("INTERVAL '5' MINUTE")
                .Build();

            // Assert
            Assert.That(table.FreshnessInterval, Is.EqualTo("INTERVAL '5' MINUTE"));
        }

        [Test]
        public void MaterializedTable_Definition_ShouldReturnDefinition()
        {
            // Arrange
            var table = new MaterializedTableBuilder("my_table")
                .WithQuery("SELECT 1")
                .Build();

            // Assert
            Assert.That(table.Definition, Is.Not.Null);
            Assert.That(table.Definition.TableName, Is.EqualTo("my_table"));
        }

        #endregion

        #region MaterializedTable Operations Tests

        [Test]
        public void MaterializedTable_Suspend_ShouldCreateSuspendOperation()
        {
            // Arrange
            var table = new MaterializedTableBuilder("my_table")
                .WithQuery("SELECT 1")
                .Build();

            // Act
            var suspended = table.Suspend();

            // Assert
            Assert.That(suspended, Is.Not.Null);
            Assert.That(suspended.TableName, Is.EqualTo("my_table"));
        }

        [Test]
        public void MaterializedTable_Resume_ShouldCreateResumeOperation()
        {
            // Arrange
            var table = new MaterializedTableBuilder("my_table")
                .WithQuery("SELECT 1")
                .Build();

            // Act
            var resumed = table.Resume();

            // Assert
            Assert.That(resumed, Is.Not.Null);
            Assert.That(resumed.TableName, Is.EqualTo("my_table"));
        }

        [Test]
        public void MaterializedTable_RefreshPartition_ShouldCreateRefreshOperation()
        {
            // Arrange
            var table = new MaterializedTableBuilder("my_table")
                .WithQuery("SELECT 1")
                .Build();

            // Act
            var refreshed = table.RefreshPartition("date='2024-10-30'");

            // Assert
            Assert.That(refreshed, Is.Not.Null);
            Assert.That(refreshed.TableName, Is.EqualTo("my_table"));
        }

        [Test]
        public void MaterializedTable_Drop_ShouldCreateDropOperation()
        {
            // Arrange
            var table = new MaterializedTableBuilder("my_table")
                .WithQuery("SELECT 1")
                .Build();

            // Act
            var dropped = table.Drop();

            // Assert
            Assert.That(dropped, Is.Not.Null);
            Assert.That(dropped.TableName, Is.EqualTo("my_table"));
        }

        #endregion

        #region MaterializedTable ToSql Tests

        [Test]
        public void MaterializedTable_ToSql_CreateWithQuery_ShouldGenerateSQL()
        {
            // Arrange
            var table = new MaterializedTableBuilder("my_table")
                .WithQuery("SELECT id, name FROM source_table")
                .Build();

            // Act
            var sql = table.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("CREATE MATERIALIZED TABLE my_table"));
            Assert.That(sql, Does.Contain("SELECT id, name FROM source_table"));
        }

        [Test]
        public void MaterializedTable_ToSql_CreateWithSchema_ShouldGenerateSQL()
        {
            // Arrange
            var table = new MaterializedTableBuilder("my_table")
                .AddColumn("id", "BIGINT")
                .AddColumn("name", "STRING")
                .Build();

            // Act
            var sql = table.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("CREATE MATERIALIZED TABLE my_table"));
            Assert.That(sql, Does.Contain("id BIGINT"));
            Assert.That(sql, Does.Contain("name STRING"));
        }

        [Test]
        public void MaterializedTable_ToSql_CreateWithPrimaryKey_ShouldGenerateSQL()
        {
            // Arrange
            var table = new MaterializedTableBuilder("my_table")
                .AddColumn("id", "BIGINT")
                .AddColumn("name", "STRING")
                .WithPrimaryKey("id")
                .Build();

            // Act
            var sql = table.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("PRIMARY KEY(id) NOT ENFORCED"));
        }

        [Test]
        public void MaterializedTable_ToSql_CreateWithPartitioning_ShouldGenerateSQL()
        {
            // Arrange
            var table = new MaterializedTableBuilder("my_table")
                .AddColumn("id", "BIGINT")
                .AddColumn("date", "STRING")
                .WithPartitioning("date")
                .Build();

            // Act
            var sql = table.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("PARTITIONED BY (date)"));
        }

        [Test]
        public void MaterializedTable_ToSql_CreateWithFreshness_ShouldGenerateSQL()
        {
            // Arrange
            var table = new MaterializedTableBuilder("my_table")
                .WithQuery("SELECT 1")
                .WithFreshnessInterval("INTERVAL '10' MINUTE")
                .Build();

            // Act
            var sql = table.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("FRESHNESS = INTERVAL '10' MINUTE"));
        }

        [Test]
        public void MaterializedTable_ToSql_Suspend_ShouldGenerateSQL()
        {
            // Arrange
            var table = new MaterializedTableBuilder("my_table")
                .WithQuery("SELECT 1")
                .Build()
                .Suspend();

            // Act
            var sql = table.ToSql();

            // Assert
            Assert.That(sql, Is.EqualTo("ALTER MATERIALIZED TABLE my_table SUSPEND"));
        }

        [Test]
        public void MaterializedTable_ToSql_Resume_ShouldGenerateSQL()
        {
            // Arrange
            var table = new MaterializedTableBuilder("my_table")
                .WithQuery("SELECT 1")
                .Build()
                .Resume();

            // Act
            var sql = table.ToSql();

            // Assert
            Assert.That(sql, Is.EqualTo("ALTER MATERIALIZED TABLE my_table RESUME"));
        }

        [Test]
        public void MaterializedTable_ToSql_RefreshPartition_ShouldGenerateSQL()
        {
            // Arrange
            var table = new MaterializedTableBuilder("my_table")
                .WithQuery("SELECT 1")
                .Build()
                .RefreshPartition("date='2024-10-30'");

            // Act
            var sql = table.ToSql();

            // Assert
            Assert.That(sql, Is.EqualTo("ALTER MATERIALIZED TABLE my_table REFRESH PARTITION (date='2024-10-30')"));
        }

        [Test]
        public void MaterializedTable_ToSql_Drop_ShouldGenerateSQL()
        {
            // Arrange
            var table = new MaterializedTableBuilder("my_table")
                .WithQuery("SELECT 1")
                .Build()
                .Drop();

            // Act
            var sql = table.ToSql();

            // Assert
            Assert.That(sql, Is.EqualTo("DROP MATERIALIZED TABLE my_table"));
        }

        #endregion

        #region MaterializedTableExtensions Tests

        [Test]
        public void MaterializedTableExtensions_CreateMaterializedTable_ShouldReturnBuilder()
        {
            // Act
            var builder = _env.CreateMaterializedTable("test_table");

            // Assert
            Assert.That(builder, Is.Not.Null);
            Assert.That(builder, Is.TypeOf<MaterializedTableBuilder>());
        }

        #endregion
    }
}
