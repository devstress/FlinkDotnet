using System;
using System.Collections.Generic;
using Flink.JobBuilder.Models;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Comprehensive tests for Table class to improve code coverage
    /// </summary>
    [TestFixture]
    public class TableTests
    {
        #region Constructor Tests

        [Test]
        public void Table_ConstructorWithDefinition_ShouldInitialize()
        {
            // Arrange
            var definition = new TableSourceDefinition { TableName = "test_table" };

            // Act
            var table = new Table(definition);

            // Assert
            Assert.That(table, Is.Not.Null);
            Assert.That(table.Definition, Is.SameAs(definition));
            Assert.That(table.TableName, Is.EqualTo("test_table"));
            Assert.That(table.Operations, Is.Empty);
        }

        [Test]
        public void Table_ConstructorWithDefinition_NullDefinition_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new Table((TableSourceDefinition) null!));
        }

        [Test]
        public void Table_ConstructorWithTableName_ShouldInitialize()
        {
            // Act
            var table = new Table("my_table");

            // Assert
            Assert.That(table, Is.Not.Null);
            Assert.That(table.TableName, Is.EqualTo("my_table"));
            Assert.That(table.Definition, Is.Not.Null);
            Assert.That(table.Definition.TableName, Is.EqualTo("my_table"));
            Assert.That(table.Operations, Is.Empty);
        }

        [Test]
        public void Table_ConstructorWithTableName_NullName_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new Table((string) null!));
        }

        [Test]
        public void Table_ConstructorWithTableName_EmptyName_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new Table(""));
        }

        [Test]
        public void Table_ConstructorWithTableName_WhitespaceName_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new Table("   "));
        }

        #endregion

        #region Select Method Tests

        [Test]
        public void Table_Select_ShouldAddSelectOperation()
        {
            // Arrange
            var table = new Table("test_table");

            // Act
            var result = table.Select("col1", "col2", "col3");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Operations, Has.Count.EqualTo(1));
            var operation = result.Operations[0] as TableOperationDefinition;
            Assert.That(operation, Is.Not.Null);
            Assert.That(operation!.OperationType, Is.EqualTo("select"));
            Assert.That(operation.Columns, Has.Count.EqualTo(3));
            Assert.That(operation.Columns, Does.Contain("col1"));
            Assert.That(operation.Columns, Does.Contain("col2"));
            Assert.That(operation.Columns, Does.Contain("col3"));
        }

        [Test]
        public void Table_Select_NullColumns_ShouldThrow()
        {
            // Arrange
            var table = new Table("test_table");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => table.Select(null!));
        }

        [Test]
        public void Table_Select_EmptyColumns_ShouldThrow()
        {
            // Arrange
            var table = new Table("test_table");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => table.Select());
        }

        [Test]
        public void Table_Select_SingleColumn_ShouldWork()
        {
            // Arrange
            var table = new Table("test_table");

            // Act
            var result = table.Select("col1");

            // Assert
            Assert.That(result.Operations, Has.Count.EqualTo(1));
            var operation = result.Operations[0] as TableOperationDefinition;
            Assert.That(operation!.Columns, Has.Count.EqualTo(1));
            Assert.That(operation.Columns[0], Is.EqualTo("col1"));
        }

        #endregion

        #region Where Method Tests

        [Test]
        public void Table_Where_ShouldAddWhereOperation()
        {
            // Arrange
            var table = new Table("test_table");

            // Act
            var result = table.Where("age > 18");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Operations, Has.Count.EqualTo(1));
            var operation = result.Operations[0] as TableOperationDefinition;
            Assert.That(operation, Is.Not.Null);
            Assert.That(operation!.OperationType, Is.EqualTo("where"));
            Assert.That(operation.Condition, Is.EqualTo("age > 18"));
        }

        [Test]
        public void Table_Where_NullCondition_ShouldThrow()
        {
            // Arrange
            var table = new Table("test_table");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => table.Where(null!));
        }

        [Test]
        public void Table_Where_EmptyCondition_ShouldThrow()
        {
            // Arrange
            var table = new Table("test_table");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => table.Where(""));
        }

        [Test]
        public void Table_Where_WhitespaceCondition_ShouldThrow()
        {
            // Arrange
            var table = new Table("test_table");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => table.Where("   "));
        }

        [Test]
        public void Table_Where_ComplexCondition_ShouldWork()
        {
            // Arrange
            var table = new Table("test_table");

            // Act
            var result = table.Where("age > 18 AND status = 'active' AND balance > 1000");

            // Assert
            var operation = result.Operations[0] as TableOperationDefinition;
            Assert.That(operation!.Condition, Is.EqualTo("age > 18 AND status = 'active' AND balance > 1000"));
        }

        #endregion

        #region GroupBy and Aggregate Tests

        [Test]
        public void Table_GroupBy_ShouldReturnGroupedTable()
        {
            // Arrange
            var table = new Table("test_table");

            // Act
            var result = table.GroupBy("category", "region");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<GroupedTable>());
        }

        [Test]
        public void Table_GroupBy_NullKeys_ShouldThrow()
        {
            // Arrange
            var table = new Table("test_table");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => table.GroupBy(null!));
        }

        [Test]
        public void Table_GroupBy_EmptyKeys_ShouldThrow()
        {
            // Arrange
            var table = new Table("test_table");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => table.GroupBy());
        }

        [Test]
        public void GroupedTable_Aggregate_ShouldAddAggregateOperation()
        {
            // Arrange
            var table = new Table("test_table");
            var grouped = table.GroupBy("category");

            // Act
            var result = grouped.Aggregate("COUNT(*) AS total", "SUM(amount) AS sum_amount");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Operations, Has.Count.EqualTo(1));
            var operation = result.Operations[0] as TableOperationDefinition;
            Assert.That(operation, Is.Not.Null);
            Assert.That(operation!.OperationType, Is.EqualTo("aggregate"));
            Assert.That(operation.GroupByKeys, Has.Count.EqualTo(1));
            Assert.That(operation.GroupByKeys[0], Is.EqualTo("category"));
            Assert.That(operation.Aggregations, Has.Count.EqualTo(2));
            Assert.That(operation.Aggregations[0], Is.EqualTo("COUNT(*) AS total"));
            Assert.That(operation.Aggregations[1], Is.EqualTo("SUM(amount) AS sum_amount"));
        }

        [Test]
        public void GroupedTable_Aggregate_NullAggregations_ShouldThrow()
        {
            // Arrange
            var table = new Table("test_table");
            var grouped = table.GroupBy("category");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => grouped.Aggregate(null!));
        }

        [Test]
        public void GroupedTable_Aggregate_EmptyAggregations_ShouldThrow()
        {
            // Arrange
            var table = new Table("test_table");
            var grouped = table.GroupBy("category");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => grouped.Aggregate());
        }

        [Test]
        public void GroupedTable_Select_ShouldCallAggregate()
        {
            // Arrange
            var table = new Table("test_table");
            var grouped = table.GroupBy("category");

            // Act
            var result = grouped.Select("AVG(price) AS avg_price");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Operations, Has.Count.EqualTo(1));
            var operation = result.Operations[0] as TableOperationDefinition;
            Assert.That(operation!.OperationType, Is.EqualTo("aggregate"));
            Assert.That(operation.Aggregations[0], Is.EqualTo("AVG(price) AS avg_price"));
        }

        #endregion

        #region Window Method Tests

        [Test]
        public void Table_Window_TumbleWindow_ShouldAddWindowOperation()
        {
            // Arrange
            var table = new Table("test_table");

            // Act
            var result = table.Window("TUMBLE", "event_time", "INTERVAL '1' HOUR");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Operations, Has.Count.EqualTo(1));
            var operation = result.Operations[0] as WindowTvfOperationDefinition;
            Assert.That(operation, Is.Not.Null);
            Assert.That(operation!.WindowType, Is.EqualTo("TUMBLE"));
            Assert.That(operation.TimeColumn, Is.EqualTo("event_time"));
            Assert.That(operation.WindowSize, Is.EqualTo("INTERVAL '1' HOUR"));
        }

        [Test]
        public void Table_Window_HopWindow_ShouldAddWindowOperation()
        {
            // Arrange
            var table = new Table("test_table");

            // Act
            var result = table.Window("HOP", "event_time", "INTERVAL '1' HOUR", "INTERVAL '30' MINUTE");

            // Assert
            var operation = result.Operations[0] as WindowTvfOperationDefinition;
            Assert.That(operation!.WindowType, Is.EqualTo("HOP"));
            Assert.That(operation.SlideInterval, Is.EqualTo("INTERVAL '30' MINUTE"));
        }

        [Test]
        public void Table_Window_CumulateWindow_ShouldAddWindowOperation()
        {
            // Arrange
            var table = new Table("test_table");

            // Act
            var result = table.Window("CUMULATE", "event_time", "INTERVAL '5' MINUTE", null, "INTERVAL '1' HOUR");

            // Assert
            var operation = result.Operations[0] as WindowTvfOperationDefinition;
            Assert.That(operation!.WindowType, Is.EqualTo("CUMULATE"));
            Assert.That(operation.MaxWindowSize, Is.EqualTo("INTERVAL '1' HOUR"));
        }

        [Test]
        public void Table_Window_NullWindowType_ShouldThrow()
        {
            // Arrange
            var table = new Table("test_table");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => table.Window(null!, "event_time", "INTERVAL '1' HOUR"));
        }

        [Test]
        public void Table_Window_EmptyWindowType_ShouldThrow()
        {
            // Arrange
            var table = new Table("test_table");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => table.Window("", "event_time", "INTERVAL '1' HOUR"));
        }

        [Test]
        public void Table_Window_NullTimeColumn_ShouldThrow()
        {
            // Arrange
            var table = new Table("test_table");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => table.Window("TUMBLE", null!, "INTERVAL '1' HOUR"));
        }

        [Test]
        public void Table_Window_EmptyTimeColumn_ShouldThrow()
        {
            // Arrange
            var table = new Table("test_table");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => table.Window("TUMBLE", "", "INTERVAL '1' HOUR"));
        }

        [Test]
        public void Table_Window_NullWindowSize_ShouldThrow()
        {
            // Arrange
            var table = new Table("test_table");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => table.Window("TUMBLE", "event_time", null!));
        }

        [Test]
        public void Table_Window_EmptyWindowSize_ShouldThrow()
        {
            // Arrange
            var table = new Table("test_table");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => table.Window("TUMBLE", "event_time", ""));
        }

        [Test]
        public void Table_TumbleWindow_ShouldCallWindow()
        {
            // Arrange
            var table = new Table("test_table");

            // Act
            var result = table.TumbleWindow("event_time", "INTERVAL '1' HOUR");

            // Assert
            var operation = result.Operations[0] as WindowTvfOperationDefinition;
            Assert.That(operation!.WindowType, Is.EqualTo("TUMBLE"));
            Assert.That(operation.TimeColumn, Is.EqualTo("event_time"));
            Assert.That(operation.WindowSize, Is.EqualTo("INTERVAL '1' HOUR"));
        }

        [Test]
        public void Table_HopWindow_ShouldCallWindow()
        {
            // Arrange
            var table = new Table("test_table");

            // Act
            var result = table.HopWindow("event_time", "INTERVAL '30' MINUTE", "INTERVAL '1' HOUR");

            // Assert
            var operation = result.Operations[0] as WindowTvfOperationDefinition;
            Assert.That(operation!.WindowType, Is.EqualTo("HOP"));
            Assert.That(operation.SlideInterval, Is.EqualTo("INTERVAL '30' MINUTE"));
            Assert.That(operation.WindowSize, Is.EqualTo("INTERVAL '1' HOUR"));
        }

        [Test]
        public void Table_CumulateWindow_ShouldCallWindow()
        {
            // Arrange
            var table = new Table("test_table");

            // Act
            var result = table.CumulateWindow("event_time", "INTERVAL '5' MINUTE", "INTERVAL '1' HOUR");

            // Assert
            var operation = result.Operations[0] as WindowTvfOperationDefinition;
            Assert.That(operation!.WindowType, Is.EqualTo("CUMULATE"));
            Assert.That(operation.WindowSize, Is.EqualTo("INTERVAL '5' MINUTE"));
            Assert.That(operation.MaxWindowSize, Is.EqualTo("INTERVAL '1' HOUR"));
        }

        #endregion

        #region AddJsonColumn Method Tests

        [Test]
        public void Table_AddJsonColumn_WithoutJsonPath_ShouldAddParseJsonOperation()
        {
            // Arrange
            var table = new Table("test_table");

            // Act
            var result = table.AddJsonColumn("raw_json", "parsed_data");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Operations, Has.Count.EqualTo(1));
            var operation = result.Operations[0] as ParseJsonOperationDefinition;
            Assert.That(operation, Is.Not.Null);
            Assert.That(operation!.FunctionType, Is.EqualTo("TRY_PARSE_JSON"));
            Assert.That(operation.SourceField, Is.EqualTo("raw_json"));
            Assert.That(operation.TargetField, Is.EqualTo("parsed_data"));
            Assert.That(operation.JsonPath, Is.Null);
        }

        [Test]
        public void Table_AddJsonColumn_WithJsonPath_ShouldAddParseJsonOperation()
        {
            // Arrange
            var table = new Table("test_table");

            // Act
            var result = table.AddJsonColumn("raw_json", "user_name", "$.user.name");

            // Assert
            var operation = result.Operations[0] as ParseJsonOperationDefinition;
            Assert.That(operation!.JsonPath, Is.EqualTo("$.user.name"));
        }

        [Test]
        public void Table_AddJsonColumn_WithStrict_ShouldUseParseJson()
        {
            // Arrange
            var table = new Table("test_table");

            // Act
            var result = table.AddJsonColumn("raw_json", "parsed_data", null, strict: true);

            // Assert
            var operation = result.Operations[0] as ParseJsonOperationDefinition;
            Assert.That(operation!.FunctionType, Is.EqualTo("PARSE_JSON"));
        }

        [Test]
        public void Table_AddJsonColumn_WithoutStrict_ShouldUseTryParseJson()
        {
            // Arrange
            var table = new Table("test_table");

            // Act
            var result = table.AddJsonColumn("raw_json", "parsed_data", null, strict: false);

            // Assert
            var operation = result.Operations[0] as ParseJsonOperationDefinition;
            Assert.That(operation!.FunctionType, Is.EqualTo("TRY_PARSE_JSON"));
        }

        [Test]
        public void Table_AddJsonColumn_NullSourceField_ShouldThrow()
        {
            // Arrange
            var table = new Table("test_table");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => table.AddJsonColumn(null!, "target"));
        }

        [Test]
        public void Table_AddJsonColumn_EmptySourceField_ShouldThrow()
        {
            // Arrange
            var table = new Table("test_table");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => table.AddJsonColumn("", "target"));
        }

        [Test]
        public void Table_AddJsonColumn_NullTargetField_ShouldThrow()
        {
            // Arrange
            var table = new Table("test_table");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => table.AddJsonColumn("source", null!));
        }

        [Test]
        public void Table_AddJsonColumn_EmptyTargetField_ShouldThrow()
        {
            // Arrange
            var table = new Table("test_table");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => table.AddJsonColumn("source", ""));
        }

        #endregion

        #region Predict Method Tests

        [Test]
        public void Table_Predict_ShouldAddMLPredictOperation()
        {
            // Arrange
            var table = new Table("test_table");

            // Act
            var result = table.Predict("my_model", "feature1", "feature2");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Operations, Has.Count.EqualTo(1));
            var operation = result.Operations[0] as MLPredictDefinition;
            Assert.That(operation, Is.Not.Null);
            Assert.That(operation!.ModelName, Is.EqualTo("my_model"));
            Assert.That(operation.InputColumns, Has.Count.EqualTo(2));
            Assert.That(operation.InputColumns[0], Is.EqualTo("feature1"));
            Assert.That(operation.InputColumns[1], Is.EqualTo("feature2"));
        }

        [Test]
        public void Table_Predict_NullModelName_ShouldThrow()
        {
            // Arrange
            var table = new Table("test_table");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => table.Predict(null!, "col1"));
        }

        [Test]
        public void Table_Predict_EmptyModelName_ShouldThrow()
        {
            // Arrange
            var table = new Table("test_table");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => table.Predict("", "col1"));
        }

        [Test]
        public void Table_Predict_NullInputColumns_ShouldThrow()
        {
            // Arrange
            var table = new Table("test_table");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => table.Predict("model", null!));
        }

        [Test]
        public void Table_Predict_EmptyInputColumns_ShouldThrow()
        {
            // Arrange
            var table = new Table("test_table");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => table.Predict("model"));
        }

        [Test]
        public void Table_PredictWithPrefix_ShouldAddMLPredictOperation()
        {
            // Arrange
            var table = new Table("test_table");

            // Act
            var result = table.PredictWithPrefix("my_model", "pred_", "feature1", "feature2");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Operations, Has.Count.EqualTo(1));
            var operation = result.Operations[0] as MLPredictDefinition;
            Assert.That(operation, Is.Not.Null);
            Assert.That(operation!.ModelName, Is.EqualTo("my_model"));
            Assert.That(operation.OutputPrefix, Is.EqualTo("pred_"));
            Assert.That(operation.InputColumns, Has.Count.EqualTo(2));
        }

        [Test]
        public void Table_PredictWithPrefix_NullModelName_ShouldThrow()
        {
            // Arrange
            var table = new Table("test_table");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => table.PredictWithPrefix(null!, "prefix", "col1"));
        }

        [Test]
        public void Table_PredictWithPrefix_EmptyModelName_ShouldThrow()
        {
            // Arrange
            var table = new Table("test_table");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => table.PredictWithPrefix("", "prefix", "col1"));
        }

        [Test]
        public void Table_PredictWithPrefix_NullOutputPrefix_ShouldThrow()
        {
            // Arrange
            var table = new Table("test_table");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => table.PredictWithPrefix("model", null!, "col1"));
        }

        [Test]
        public void Table_PredictWithPrefix_EmptyOutputPrefix_ShouldThrow()
        {
            // Arrange
            var table = new Table("test_table");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => table.PredictWithPrefix("model", "", "col1"));
        }

        [Test]
        public void Table_PredictWithPrefix_NullInputColumns_ShouldThrow()
        {
            // Arrange
            var table = new Table("test_table");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => table.PredictWithPrefix("model", "prefix", null!));
        }

        [Test]
        public void Table_PredictWithPrefix_EmptyInputColumns_ShouldThrow()
        {
            // Arrange
            var table = new Table("test_table");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => table.PredictWithPrefix("model", "prefix"));
        }

        #endregion

        #region ToSql Method Tests

        [Test]
        public void Table_ToSql_NoOperations_ShouldReturnSelectAll()
        {
            // Arrange
            var table = new Table("users");

            // Act
            var sql = table.ToSql();

            // Assert
            Assert.That(sql, Is.EqualTo("SELECT * FROM users"));
        }

        [Test]
        public void Table_ToSql_WithSelect_ShouldReplaceColumns()
        {
            // Arrange
            var table = new Table("users").Select("id", "name", "email");

            // Act
            var sql = table.ToSql();

            // Assert
            Assert.That(sql, Is.EqualTo("SELECT id, name, email FROM users"));
        }

        [Test]
        public void Table_ToSql_WithWhere_ShouldAppendWhereClause()
        {
            // Arrange
            var table = new Table("users").Where("age > 18");

            // Act
            var sql = table.ToSql();

            // Assert
            Assert.That(sql, Is.EqualTo("SELECT * FROM users WHERE age > 18"));
        }

        [Test]
        public void Table_ToSql_WithSelectAndWhere_ShouldCombine()
        {
            // Arrange
            var table = new Table("users")
                .Select("id", "name")
                .Where("active = true");

            // Act
            var sql = table.ToSql();

            // Assert
            Assert.That(sql, Is.EqualTo("SELECT id, name FROM users WHERE active = true"));
        }

        [Test]
        public void Table_ToSql_WithTumbleWindow_ShouldGenerateWindowTVF()
        {
            // Arrange
            var table = new Table("events")
                .TumbleWindow("event_time", "INTERVAL '1' HOUR");

            // Act
            var sql = table.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("TUMBLE(TABLE events"));
            Assert.That(sql, Does.Contain("DESCRIPTOR(event_time)"));
            Assert.That(sql, Does.Contain("INTERVAL '1' HOUR"));
        }

        [Test]
        public void Table_ToSql_WithHopWindow_ShouldGenerateWindowTVF()
        {
            // Arrange
            var table = new Table("events")
                .HopWindow("event_time", "INTERVAL '30' MINUTE", "INTERVAL '1' HOUR");

            // Act
            var sql = table.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("HOP(TABLE events"));
            Assert.That(sql, Does.Contain("INTERVAL '30' MINUTE"));
            Assert.That(sql, Does.Contain("INTERVAL '1' HOUR"));
        }

        [Test]
        public void Table_ToSql_WithCumulateWindow_ShouldGenerateWindowTVF()
        {
            // Arrange
            var table = new Table("events")
                .CumulateWindow("event_time", "INTERVAL '5' MINUTE", "INTERVAL '1' HOUR");

            // Act
            var sql = table.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("CUMULATE(TABLE events"));
            Assert.That(sql, Does.Contain("INTERVAL '5' MINUTE"));
            Assert.That(sql, Does.Contain("INTERVAL '1' HOUR"));
        }

        [Test]
        public void Table_ToSql_WithAddJsonColumn_ShouldAddJsonParsing()
        {
            // Arrange
            var table = new Table("logs")
                .AddJsonColumn("payload", "parsed_data");

            // Act
            var sql = table.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("TRY_PARSE_JSON(payload)"));
            Assert.That(sql, Does.Contain("AS parsed_data"));
        }

        [Test]
        public void Table_ToSql_WithAddJsonColumnAndPath_ShouldAddJsonPathExtraction()
        {
            // Arrange
            var table = new Table("logs")
                .AddJsonColumn("payload", "user_id", "$.user.id", strict: true);

            // Act
            var sql = table.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("PARSE_JSON(payload)"));
            Assert.That(sql, Does.Contain("::VARIANT$.user.id"));
            Assert.That(sql, Does.Contain("AS user_id"));
        }

        [Test]
        public void Table_ToSql_WithMultipleJsonColumns_ShouldAddAll()
        {
            // Arrange
            var table = new Table("logs")
                .AddJsonColumn("payload", "data1")
                .AddJsonColumn("metadata", "data2");

            // Act
            var sql = table.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("TRY_PARSE_JSON(payload) AS data1"));
            Assert.That(sql, Does.Contain("TRY_PARSE_JSON(metadata) AS data2"));
        }

        [Test]
        public void Table_ToSql_ComplexQuery_ShouldCombineAllOperations()
        {
            // Arrange
            var table = new Table("users")
                .Select("id", "name", "age")
                .Where("age > 18 AND active = true");

            // Act
            var sql = table.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("SELECT id, name, age"));
            Assert.That(sql, Does.Contain("FROM users"));
            Assert.That(sql, Does.Contain("WHERE age > 18 AND active = true"));
        }

        #endregion

        #region TableExtensions Tests

        [Test]
        public void TableExtensions_ToTable_ShouldCreateTable()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromCollection(new[] { 1, 2, 3 });

            // Act
            var table = stream.ToTable("numbers");

            // Assert
            Assert.That(table, Is.Not.Null);
            Assert.That(table.TableName, Is.EqualTo("numbers"));
            Assert.That(table.Definition, Is.Not.Null);
            Assert.That(table.Definition.TableName, Is.EqualTo("numbers"));
        }

        [Test]
        public void TableExtensions_ToTable_WithSchema_ShouldCreateTableWithSchema()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromCollection(new[] { "test" });
            var schema = new Dictionary<string, string>
            {
                { "id", "BIGINT" },
                { "name", "STRING" }
            };

            // Act
            var table = stream.ToTable("users", schema);

            // Assert
            Assert.That(table, Is.Not.Null);
            Assert.That(table.Definition.Schema, Is.Not.Null);
            Assert.That(table.Definition.Schema, Has.Count.EqualTo(2));
            Assert.That(table.Definition.Schema["id"], Is.EqualTo("BIGINT"));
            Assert.That(table.Definition.Schema["name"], Is.EqualTo("STRING"));
        }

        [Test]
        public void TableExtensions_ToTable_NullTableName_ShouldThrow()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromCollection(new[] { 1, 2, 3 });

            // Act & Assert
            Assert.Throws<ArgumentException>(() => stream.ToTable(null!));
        }

        [Test]
        public void TableExtensions_ToTable_EmptyTableName_ShouldThrow()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromCollection(new[] { 1, 2, 3 });

            // Act & Assert
            Assert.Throws<ArgumentException>(() => stream.ToTable(""));
        }

        #endregion

        #region Clone Method Tests

        [Test]
        public void Table_Clone_ShouldCreateNewInstanceWithSameData()
        {
            // Arrange
            var original = new Table("test_table")
                .Select("col1", "col2")
                .Where("col1 > 10");

            // Act
            var cloned = original.Clone();

            // Assert
            Assert.That(cloned, Is.Not.SameAs(original));
            Assert.That(cloned.Definition, Is.SameAs(original.Definition));
            Assert.That(cloned.Operations, Has.Count.EqualTo(original.Operations.Count));
            Assert.That(cloned.Operations, Is.Not.SameAs(original.Operations));
        }

        [Test]
        public void Table_Clone_ModifyingClone_ShouldNotAffectOriginal()
        {
            // Arrange
            var original = new Table("test_table").Select("col1");
            var cloned = original.Clone();

            // Act
            var modified = cloned.Where("col1 > 10");

            // Assert
            Assert.That(original.Operations, Has.Count.EqualTo(1));
            Assert.That(cloned.Operations, Has.Count.EqualTo(1));
            Assert.That(modified.Operations, Has.Count.EqualTo(2));
        }

        #endregion

        #region Chaining Tests

        [Test]
        public void Table_ChainMultipleOperations_ShouldBuildCorrectly()
        {
            // Arrange & Act
            var table = new Table("events")
                .Select("user_id", "event_type", "event_time")
                .Where("event_type = 'click'")
                .AddJsonColumn("metadata", "parsed_metadata");

            // Assert
            Assert.That(table.Operations, Has.Count.EqualTo(3));
            Assert.That((table.Operations[0] as TableOperationDefinition)!.OperationType, Is.EqualTo("select"));
            Assert.That((table.Operations[1] as TableOperationDefinition)!.OperationType, Is.EqualTo("where"));
            Assert.That(table.Operations[2], Is.InstanceOf<ParseJsonOperationDefinition>());
        }

        [Test]
        public void Table_ChainWithGroupByAggregate_ShouldWork()
        {
            // Arrange & Act
            var table = new Table("sales")
                .Select("region", "product", "amount")
                .Where("amount > 100")
                .GroupBy("region", "product")
                .Aggregate("SUM(amount) AS total_amount", "COUNT(*) AS count");

            // Assert
            Assert.That(table.Operations, Has.Count.EqualTo(3));
            var aggregateOp = table.Operations[2] as TableOperationDefinition;
            Assert.That(aggregateOp!.OperationType, Is.EqualTo("aggregate"));
            Assert.That(aggregateOp.GroupByKeys, Has.Count.EqualTo(2));
            Assert.That(aggregateOp.Aggregations, Has.Count.EqualTo(2));
        }

        #endregion
    }
}
