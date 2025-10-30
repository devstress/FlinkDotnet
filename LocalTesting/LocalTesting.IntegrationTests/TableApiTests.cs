using System;
using System.Collections.Generic;
using System.Text.Json;
using Flink.JobBuilder.Models;
using FlinkDotNet.DataStream;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

/// <summary>
/// Consolidated integration tests for Table API & SQL features (Flink 2.1+).
/// Tests validate VARIANT type, JSON functions, and Native Table API.
/// Maximum 5 tests per Flink version as per project guidelines.
/// </summary>
[TestFixture]
[Category("table-api")]
public class TableApiTests
{
    #region Test 1: IR Schema for Table Operations and VARIANT Support

    /// <summary>
    /// Test 1: Validates IR schema for Table API operations including:
    /// - TableSourceDefinition with VARIANT schema
    /// - ParseJsonOperationDefinition for JSON parsing
    /// - TableOperationDefinition for transformations
    /// - JSON round-trip serialization
    /// </summary>
    [Test]
    public void Test1_IRSchema_TablesAndVariantSupport()
    {
        // Arrange - Create job with Table source containing VARIANT columns
        var jobDef = new JobDefinition
        {
            Source = new TableSourceDefinition
            {
                TableName = "events",
                Schema = new Dictionary<string, string>
                {
                    { "event_id", "STRING" },
                    { "event_data", "VARIANT" },  // Semi-structured JSON data
                    { "event_time", "TIMESTAMP(3)" }
                },
                Properties = new Dictionary<string, string>
                {
                    { "connector", "kafka" },
                    { "format", "json" }
                }
            },
            Operations = new List<IOperationDefinition>
            {
                // Parse JSON from string field
                new ParseJsonOperationDefinition
                {
                    FunctionType = "TRY_PARSE_JSON",
                    SourceField = "raw_json",
                    TargetField = "parsed_data",
                    JsonPath = "$.user.name"
                },
                // Table transformation: select columns
                new TableOperationDefinition
                {
                    OperationType = "select",
                    Columns = ["event_id", "parsed_data", "event_time"]
                },
                // Table transformation: where filter
                new TableOperationDefinition
                {
                    OperationType = "where",
                    Condition = "event_id IS NOT NULL"
                }
            },
            Metadata = new JobMetadata
            {
                JobId = "table-api-test-job",
                JobName = "Table API Test",
                Version = "1.0"
            }
        };

        // Act - Serialize and deserialize
        var json = JsonSerializer.Serialize(jobDef, new JsonSerializerOptions { WriteIndented = true });
        var deserialized = JsonSerializer.Deserialize<JobDefinition>(json);

        // Assert - Source structure
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized.Source, Is.InstanceOf<TableSourceDefinition>());

        var tableSource = deserialized.Source as TableSourceDefinition;
        Assert.That(tableSource, Is.Not.Null);
        Assert.That(tableSource.Type, Is.EqualTo("table"));
        Assert.That(tableSource.TableName, Is.EqualTo("events"));
        Assert.That(tableSource.Schema["event_data"], Is.EqualTo("VARIANT"));

        // Assert - Operations
        Assert.That(deserialized.Operations, Has.Count.EqualTo(3));

        var parseOp = deserialized.Operations[0] as ParseJsonOperationDefinition;
        Assert.That(parseOp, Is.Not.Null);
        Assert.That(parseOp.Type, Is.EqualTo("parseJson"));
        Assert.That(parseOp.FunctionType, Is.EqualTo("TRY_PARSE_JSON"));
        Assert.That(parseOp.JsonPath, Is.EqualTo("$.user.name"));

        var selectOp = deserialized.Operations[1] as TableOperationDefinition;
        Assert.That(selectOp, Is.Not.Null);
        Assert.That(selectOp.OperationType, Is.EqualTo("select"));
        Assert.That(selectOp.Columns, Has.Count.EqualTo(3));

        var whereOp = deserialized.Operations[2] as TableOperationDefinition;
        Assert.That(whereOp, Is.Not.Null);
        Assert.That(whereOp.OperationType, Is.EqualTo("where"));
    }

    #endregion

    #region Test 2: Table API Fluent Interface

    /// <summary>
    /// Test 2: Validates Native Table API fluent interface including:
    /// - Table creation from DataStream
    /// - Select, Where, GroupBy operations
    /// - Operation chaining
    /// - SQL generation from API calls
    /// </summary>
    [Test]
    public void Test2_TableAPI_FluentInterface()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection([1, 2, 3, 4, 5]);

        // Act - Create table and apply transformations
        var table = stream.ToTable("my_table", new Dictionary<string, string>
        {
            { "id", "INT" },
            { "value", "STRING" },
            { "timestamp", "TIMESTAMP(3)" }
        });

        // Assert - Table creation
        Assert.That(table, Is.Not.Null);
        Assert.That(table.TableName, Is.EqualTo("my_table"));
        Assert.That(table.Definition.Schema, Has.Count.EqualTo(3));

        // Act - Apply select operation
        var selectedTable = table.Select("id", "value");
        Assert.That(selectedTable.Operations, Has.Count.EqualTo(1));
        var selectOp = selectedTable.Operations[0] as TableOperationDefinition;
        Assert.That(selectOp?.OperationType, Is.EqualTo("select"));
        Assert.That(selectOp?.Columns, Has.Count.EqualTo(2));

        // Act - Apply where filter
        var filteredTable = selectedTable.Where("value IS NOT NULL");
        Assert.That(filteredTable.Operations, Has.Count.EqualTo(2));
        var whereOp = filteredTable.Operations[1] as TableOperationDefinition;
        Assert.That(whereOp?.OperationType, Is.EqualTo("where"));

        // Act - Generate SQL
        var sql = filteredTable.ToSql();
        Assert.That(sql, Does.Contain("SELECT"));
        Assert.That(sql, Does.Contain("id, value"));
        Assert.That(sql, Does.Contain("FROM my_table"));
        Assert.That(sql, Does.Contain("WHERE value IS NOT NULL"));
    }

    #endregion

    #region Test 3: VARIANT Type and JSON Functions

    /// <summary>
    /// Test 3: Validates VARIANT data type and JSON parsing functions including:
    /// - PARSE_JSON vs TRY_PARSE_JSON behavior
    /// - JSON path navigation
    /// - Multiple JSON column additions
    /// - SQL generation with VARIANT operations
    /// </summary>
    [Test]
    public void Test3_VariantType_JsonFunctions()
    {
        // Arrange
        var table = new Table(new TableSourceDefinition
        {
            TableName = "raw_events",
            Schema = new Dictionary<string, string>
            {
                { "event_id", "STRING" },
                { "raw_json", "STRING" }
            }
        });

        // Act - Add JSON column with TRY_PARSE_JSON (lenient)
        var table1 = table.AddJsonColumn("raw_json", "user_name", "$.user.name", strict: false);

        // Assert - TRY_PARSE_JSON operation
        Assert.That(table1.Operations, Has.Count.EqualTo(1));
        var parseOp1 = table1.Operations[0] as ParseJsonOperationDefinition;
        Assert.That(parseOp1?.FunctionType, Is.EqualTo("TRY_PARSE_JSON"));
        Assert.That(parseOp1?.SourceField, Is.EqualTo("raw_json"));
        Assert.That(parseOp1?.TargetField, Is.EqualTo("user_name"));
        Assert.That(parseOp1?.JsonPath, Is.EqualTo("$.user.name"));

        // Act - Add another JSON column with PARSE_JSON (strict)
        var table2 = table1.AddJsonColumn("raw_json", "user_id", "$.user.id", strict: true);

        // Assert - PARSE_JSON operation
        Assert.That(table2.Operations, Has.Count.EqualTo(2));
        var parseOp2 = table2.Operations[1] as ParseJsonOperationDefinition;
        Assert.That(parseOp2?.FunctionType, Is.EqualTo("PARSE_JSON"));

        // Act - Generate SQL
        var sql = table2.ToSql();

        // Assert - SQL contains JSON functions
        Assert.That(sql, Does.Contain("TRY_PARSE_JSON"));
        Assert.That(sql, Does.Contain("PARSE_JSON"));
        Assert.That(sql, Does.Contain("::VARIANT"));
        Assert.That(sql, Does.Contain("$.user.name"));
        Assert.That(sql, Does.Contain("$.user.id"));
    }

    #endregion

    #region Test 4: Table GroupBy and Aggregations

    /// <summary>
    /// Test 4: Validates Table API grouping and aggregations including:
    /// - GroupBy operation
    /// - Multiple aggregation functions
    /// - Grouped table operations
    /// - Aggregation SQL generation
    /// </summary>
    [Test]
    public void Test4_TableAPI_GroupByAndAggregations()
    {
        // Arrange
        var table = new Table("orders");

        // Act - Group by customer_id and aggregate
        var groupedTable = table.GroupBy("customer_id");
        var aggregatedTable = groupedTable.Aggregate(
            "COUNT(*) AS order_count",
            "SUM(amount) AS total_amount",
            "AVG(amount) AS avg_amount"
        );

        // Assert - Operations structure
        Assert.That(aggregatedTable.Operations, Has.Count.EqualTo(1));
        var aggOp = aggregatedTable.Operations[0] as TableOperationDefinition;
        Assert.That(aggOp, Is.Not.Null);
        Assert.That(aggOp.OperationType, Is.EqualTo("aggregate"));
        Assert.That(aggOp.GroupByKeys, Has.Count.EqualTo(1));
        Assert.That(aggOp.GroupByKeys[0], Is.EqualTo("customer_id"));
        Assert.That(aggOp.Aggregations, Has.Count.EqualTo(3));
        Assert.That(aggOp.Aggregations[0], Is.EqualTo("COUNT(*) AS order_count"));
        Assert.That(aggOp.Aggregations[1], Is.EqualTo("SUM(amount) AS total_amount"));
        Assert.That(aggOp.Aggregations[2], Is.EqualTo("AVG(amount) AS avg_amount"));

        // Act - Use GroupedTable.Select shorthand
        var groupedTable2 = table.GroupBy("product_id", "category");
        var selectedTable = groupedTable2.Select(
            "COUNT(*) AS count",
            "MAX(price) AS max_price"
        );

        // Assert - Multiple group keys
        Assert.That(selectedTable.Operations, Has.Count.EqualTo(1));
        var selectOp = selectedTable.Operations[0] as TableOperationDefinition;
        Assert.That(selectOp?.GroupByKeys, Has.Count.EqualTo(2));
        Assert.That(selectOp?.Aggregations, Has.Count.EqualTo(2));
    }

    #endregion

    #region Test 5: Complex Table API Workflow with VARIANT

    /// <summary>
    /// Test 5: Validates complex Table API workflow combining all features:
    /// - VARIANT columns in schema
    /// - JSON parsing and path navigation
    /// - Select, Where, GroupBy operations
    /// - Complete SQL generation
    /// - End-to-end transformation pipeline
    /// </summary>
    [Test]
    public void Test5_ComplexWorkflow_VariantAndTableAPI()
    {
        // Arrange - Table with VARIANT column
        var table = new Table(new TableSourceDefinition
        {
            TableName = "user_events",
            Schema = new Dictionary<string, string>
            {
                { "user_id", "STRING" },
                { "event_data", "VARIANT" },
                { "event_timestamp", "TIMESTAMP(3)" }
            },
            Properties = new Dictionary<string, string>
            {
                { "connector", "kafka" },
                { "topic", "user-events" },
                { "format", "json" }
            }
        });

        // Act - Complex transformation pipeline
        var result = table
            .AddJsonColumn("event_data", "event_type", "$.type", strict: false)
            .AddJsonColumn("event_data", "event_value", "$.value", strict: false)
            .Select("user_id", "event_type", "event_value", "event_timestamp")
            .Where("event_type IS NOT NULL");

        // Assert - Operations chain
        Assert.That(result.Operations, Has.Count.EqualTo(4));

        // Verify operation types in order
        Assert.That((result.Operations[0] as ParseJsonOperationDefinition)?.TargetField, Is.EqualTo("event_type"));
        Assert.That((result.Operations[1] as ParseJsonOperationDefinition)?.TargetField, Is.EqualTo("event_value"));
        Assert.That((result.Operations[2] as TableOperationDefinition)?.OperationType, Is.EqualTo("select"));
        Assert.That((result.Operations[3] as TableOperationDefinition)?.OperationType, Is.EqualTo("where"));

        // Act - Generate final SQL
        var sql = result.ToSql();

        // Assert - SQL contains all operations
        Assert.That(sql, Does.Contain("SELECT"));
        Assert.That(sql, Does.Contain("user_id"));
        Assert.That(sql, Does.Contain("event_type"));
        Assert.That(sql, Does.Contain("event_value"));
        Assert.That(sql, Does.Contain("TRY_PARSE_JSON"));
        Assert.That(sql, Does.Contain("::VARIANT"));
        Assert.That(sql, Does.Contain("WHERE event_type IS NOT NULL"));

        // Assert - Original table schema preserved
        Assert.That(table.Definition.Schema["event_data"], Is.EqualTo("VARIANT"));
        Assert.That(table.Definition.Properties["connector"], Is.EqualTo("kafka"));

        // Validate edge cases
        // Test: Empty table name should throw
        Assert.Throws<ArgumentException>(() => new Table(""));

        // Test: Invalid JSON path should still create operation
        var invalidPathTable = table.AddJsonColumn("event_data", "invalid", "$.invalid.path[999]");
        Assert.That(invalidPathTable.Operations, Has.Count.EqualTo(1));

        // Test: Multiple GroupBy keys
        var groupedResult = result.GroupBy("user_id", "event_type");
        var aggregated = groupedResult.Aggregate("COUNT(*) AS event_count");
        Assert.That((aggregated.Operations[4] as TableOperationDefinition)?.GroupByKeys, Has.Count.EqualTo(2));
    }

    #endregion
}
