using System;
using System.Collections.Generic;
using System.Text;
using Flink.JobBuilder.Models;

namespace FlinkDotNet.DataStream;

/// <summary>
/// Represents a Table in Flink's Table API, providing fluent methods for declarative data transformations.
/// Tables can be created from DataStreams or from registered catalog tables.
/// </summary>
public class Table
{
    /// <summary>
    /// Gets the table source definition
    /// </summary>
    public TableSourceDefinition Definition { get; }

    /// <summary>
    /// Gets the list of operations applied to this table
    /// </summary>
    public List<IOperationDefinition> Operations { get; } = [];

    /// <summary>
    /// Gets the table name
    /// </summary>
    public string TableName => this.Definition.TableName;

    /// <summary>
    /// Initializes a new instance of the Table class with a table source
    /// </summary>
    /// <param name="definition">Table source definition</param>
    public Table(TableSourceDefinition definition)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    /// <summary>
    /// Initializes a new instance of the Table class with a table name
    /// </summary>
    /// <param name="tableName">Name of the table in the catalog</param>
    public Table(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));

        Definition = new TableSourceDefinition
        {
            TableName = tableName
        };
    }

    /// <summary>
    /// Selects specific columns from the table
    /// </summary>
    /// <param name="columns">Column names to select</param>
    /// <returns>A new Table with the select operation applied</returns>
    public Table Select(params string[] columns)
    {
        if (columns == null || columns.Length == 0)
            throw new ArgumentException("At least one column must be selected", nameof(columns));

        var newTable = Clone();
        newTable.Operations.Add(new TableOperationDefinition
        {
            OperationType = "select",
            Columns = [.. columns]
        });
        return newTable;
    }

    /// <summary>
    /// Filters rows based on a condition
    /// </summary>
    /// <param name="condition">Filter condition (SQL WHERE clause syntax)</param>
    /// <returns>A new Table with the filter operation applied</returns>
    public Table Where(string condition)
    {
        if (string.IsNullOrWhiteSpace(condition))
            throw new ArgumentException("Condition cannot be null or empty", nameof(condition));

        var newTable = Clone();
        newTable.Operations.Add(new TableOperationDefinition
        {
            OperationType = "where",
            Condition = condition
        });
        return newTable;
    }

    /// <summary>
    /// Groups rows by specified keys
    /// </summary>
    /// <param name="keys">Column names to group by</param>
    /// <returns>A GroupedTable that can be used for aggregations</returns>
    public GroupedTable GroupBy(params string[] keys)
    {
        if (keys == null || keys.Length == 0)
            throw new ArgumentException("At least one grouping key must be specified", nameof(keys));

        return new GroupedTable(this, keys);
    }

    /// <summary>
    /// Adds a computed column using PARSE_JSON to extract data from JSON strings
    /// </summary>
    /// <param name="sourceField">Source field containing JSON string</param>
    /// <param name="targetField">Target field name for parsed result</param>
    /// <param name="jsonPath">Optional JSON path for extracting nested values</param>
    /// <param name="strict">If true, uses PARSE_JSON (throws on error); if false, uses TRY_PARSE_JSON (returns NULL)</param>
    /// <returns>A new Table with the JSON parsing operation applied</returns>
    public Table AddJsonColumn(string sourceField, string targetField, string? jsonPath = null, bool strict = false)
    {
        if (string.IsNullOrWhiteSpace(sourceField))
            throw new ArgumentException("Source field cannot be null or empty", nameof(sourceField));
        if (string.IsNullOrWhiteSpace(targetField))
            throw new ArgumentException("Target field cannot be null or empty", nameof(targetField));

        var newTable = Clone();
        newTable.Operations.Add(new ParseJsonOperationDefinition
        {
            FunctionType = strict ? "PARSE_JSON" : "TRY_PARSE_JSON",
            SourceField = sourceField,
            TargetField = targetField,
            JsonPath = jsonPath
        });
        return newTable;
    }

    /// <summary>
    /// Applies a window table-valued function (TUMBLE, HOP, or CUMULATE)
    /// </summary>
    /// <param name="windowType">Type of window: "TUMBLE", "HOP", or "CUMULATE"</param>
    /// <param name="timeColumn">Time attribute column name</param>
    /// <param name="windowSize">Window size (e.g., "INTERVAL '1' HOUR")</param>
    /// <param name="slideInterval">Slide interval for HOP windows (optional)</param>
    /// <param name="maxWindowSize">Maximum window size for CUMULATE windows (optional)</param>
    /// <returns>A new Table with the window operation applied</returns>
    public Table Window(string windowType, string timeColumn, string windowSize, string? slideInterval = null, string? maxWindowSize = null)
    {
        if (string.IsNullOrWhiteSpace(windowType))
        {
            throw new ArgumentException("Window type cannot be null or empty", nameof(windowType));
        }

        if (string.IsNullOrWhiteSpace(timeColumn))
        {
            throw new ArgumentException("Time column cannot be null or empty", nameof(timeColumn));
        }

        if (string.IsNullOrWhiteSpace(windowSize))
        {
            throw new ArgumentException("Window size cannot be null or empty", nameof(windowSize));
        }

        var newTable = this.Clone();
        newTable.Operations.Add(new WindowTvfOperationDefinition
        {
            WindowType = windowType.ToUpper(),
            TimeColumn = timeColumn,
            WindowSize = windowSize,
            SlideInterval = slideInterval,
            MaxWindowSize = maxWindowSize
        });
        return newTable;
    }

    /// <summary>
    /// Applies a TUMBLE window (fixed, non-overlapping windows)
    /// </summary>
    /// <param name="timeColumn">Time attribute column name</param>
    /// <param name="windowSize">Window size (e.g., "INTERVAL '1' HOUR")</param>
    /// <returns>A new Table with TUMBLE window applied</returns>
    public Table TumbleWindow(string timeColumn, string windowSize)
    {
        return this.Window("TUMBLE", timeColumn, windowSize);
    }

    /// <summary>
    /// Applies a HOP window (sliding windows with overlap)
    /// </summary>
    /// <param name="timeColumn">Time attribute column name</param>
    /// <param name="slideInterval">How often windows are created</param>
    /// <param name="windowSize">Size of each window</param>
    /// <returns>A new Table with HOP window applied</returns>
    public Table HopWindow(string timeColumn, string slideInterval, string windowSize)
    {
        return this.Window("HOP", timeColumn, windowSize, slideInterval);
    }

    /// <summary>
    /// Applies a CUMULATE window (expanding windows within a maximum size)
    /// </summary>
    /// <param name="timeColumn">Time attribute column name</param>
    /// <param name="stepInterval">How often windows expand</param>
    /// <param name="maxWindowSize">Maximum window size</param>
    /// <returns>A new Table with CUMULATE window applied</returns>
    public Table CumulateWindow(string timeColumn, string stepInterval, string maxWindowSize)
    {
        return this.Window("CUMULATE", timeColumn, stepInterval, null, maxWindowSize);
    }

    /// <summary>
    /// Generates SQL query from table operations
    /// </summary>
    /// <returns>SQL query string representing the table transformation</returns>
    public string ToSql()
    {
        var sql = new StringBuilder($"SELECT * FROM {this.Definition.TableName}");

        foreach (var operation in this.Operations)
        {
            switch (operation)
            {
                case TableOperationDefinition tableOp when tableOp.OperationType == "select":
                    {
                        var selectColumns = string.Join(", ", tableOp.Columns);
                        sql.Replace("SELECT *", $"SELECT {selectColumns}");
                        break;
                    }

                case TableOperationDefinition tableOp when tableOp.OperationType == "where":
                    sql.Append($" WHERE {tableOp.Condition}");
                    break;

                case WindowTvfOperationDefinition windowOp:
                    {
                        // Generate window TVF SQL
                        var windowFunc = windowOp.WindowType switch
                        {
                            "TUMBLE" => $"TUMBLE(TABLE {this.Definition.TableName}, DESCRIPTOR({windowOp.TimeColumn}), {windowOp.WindowSize})",
                            "HOP" => $"HOP(TABLE {this.Definition.TableName}, DESCRIPTOR({windowOp.TimeColumn}), {windowOp.SlideInterval}, {windowOp.WindowSize})",
                            "CUMULATE" => $"CUMULATE(TABLE {this.Definition.TableName}, DESCRIPTOR({windowOp.TimeColumn}), {windowOp.WindowSize}, {windowOp.MaxWindowSize})",
                            _ => throw new InvalidOperationException($"Unknown window type: {windowOp.WindowType}")
                        };

                        sql = new StringBuilder($"SELECT * FROM TABLE({windowFunc})");

                        if (windowOp.GroupByColumns.Count > 0)
                        {
                            sql.Append($" GROUP BY {string.Join(", ", windowOp.GroupByColumns)}");
                        }

                        if (windowOp.Aggregations.Count > 0)
                        {
                            sql.Replace("SELECT *", $"SELECT window_start, window_end, {string.Join(", ", windowOp.Aggregations)}");
                        }

                        break;
                    }
                case ParseJsonOperationDefinition parseOp:
                    {
                        var jsonFunc = parseOp.FunctionType;
                        var jsonExpr = string.IsNullOrEmpty(parseOp.JsonPath)
                            ? $"{jsonFunc}({parseOp.SourceField})"
                            : $"{jsonFunc}({parseOp.SourceField})::VARIANT{parseOp.JsonPath}";

                        // Insert the new column into SELECT clause
                        var sqlStr = sql.ToString();
                        if (sqlStr.Contains("SELECT *"))
                        {
                            sql.Replace("SELECT *", $"SELECT *, {jsonExpr} AS {parseOp.TargetField}");
                        }
                        else
                        {
                            var selectIdx = sqlStr.IndexOf("FROM");
                            sql.Insert(selectIdx, $", {jsonExpr} AS {parseOp.TargetField} ");
                        }
                        break;
                    }

                default:
                    // Other operation types not yet implemented
                    break;
            }
        }

        return sql.ToString();
    }

    /// <summary>
    /// Applies ML_PREDICT operation to this table for AI/ML inference
    /// </summary>
    /// <param name="modelName">Name of the registered model</param>
    /// <param name="inputColumns">Input column names to pass to the model</param>
    /// <returns>New table with model predictions</returns>
    public Table Predict(string modelName, params string[] inputColumns)
    {
        if (string.IsNullOrWhiteSpace(modelName))
        {
            throw new ArgumentException("Model name cannot be null or empty", nameof(modelName));
        }

        if (inputColumns == null || inputColumns.Length == 0)
        {
            throw new ArgumentException("At least one input column must be specified", nameof(inputColumns));
        }

        var newTable = this.Clone();
        newTable.Operations.Add(new MLPredictDefinition
        {
            ModelName = modelName,
            InputColumns = new List<string>(inputColumns)
        });
        return newTable;
    }

    /// <summary>
    /// Applies ML_PREDICT operation with an output prefix/alias
    /// </summary>
    /// <param name="modelName">Name of the registered model</param>
    /// <param name="outputPrefix">Output prefix for prediction results</param>
    /// <param name="inputColumns">Input column names to pass to the model</param>
    /// <returns>New table with model predictions</returns>
    public Table PredictWithPrefix(string modelName, string outputPrefix, params string[] inputColumns)
    {
        if (string.IsNullOrWhiteSpace(modelName))
        {
            throw new ArgumentException("Model name cannot be null or empty", nameof(modelName));
        }

        if (string.IsNullOrWhiteSpace(outputPrefix))
        {
            throw new ArgumentException("Output prefix cannot be null or empty", nameof(outputPrefix));
        }

        if (inputColumns == null || inputColumns.Length == 0)
        {
            throw new ArgumentException("At least one input column must be specified", nameof(inputColumns));
        }

        var newTable = this.Clone();
        newTable.Operations.Add(new MLPredictDefinition
        {
            ModelName = modelName,
            InputColumns = new List<string>(inputColumns),
            OutputPrefix = outputPrefix
        });
        return newTable;
    }

    /// <summary>
    /// Clones this table with all operations
    /// </summary>
    internal Table Clone()
    {
        var newTable = new Table(this.Definition);
        newTable.Operations.AddRange(this.Operations);
        return newTable;
    }
}

/// <summary>
/// Represents a grouped table for aggregation operations
/// </summary>
public class GroupedTable
{
    private readonly Table _table;
    private readonly string[] _groupByKeys;

    internal GroupedTable(Table table, string[] groupByKeys)
    {
        this._table = table;
        this._groupByKeys = groupByKeys;
    }

    /// <summary>
    /// Applies aggregation functions to the grouped table
    /// </summary>
    /// <param name="aggregations">Aggregation expressions (e.g., "COUNT(*) AS total", "SUM(amount) AS sum_amount")</param>
    /// <returns>A new Table with grouping and aggregation applied</returns>
    public Table Aggregate(params string[] aggregations)
    {
        if (aggregations == null || aggregations.Length == 0)
            throw new ArgumentException("At least one aggregation must be specified", nameof(aggregations));

        var newTable = this._table.Clone();
        newTable.Operations.Add(new TableOperationDefinition
        {
            OperationType = "aggregate",
            GroupByKeys = [.. this._groupByKeys],
            Aggregations = [.. aggregations]
        });
        return newTable;
    }

    /// <summary>
    /// Selects specific columns and applies aggregations
    /// </summary>
    /// <param name="selections">Column selections and aggregations</param>
    /// <returns>A new Table with the selection applied</returns>
    public Table Select(params string[] selections)
    {
        return Aggregate(selections);
    }
}

/// <summary>
/// Extension methods for creating tables from DataStreams
/// </summary>
public static class TableExtensions
{
    /// <summary>
    /// Converts a DataStream to a Table for Table API operations
    /// </summary>
    /// <typeparam name="T">Type of elements in the stream</typeparam>
    /// <param name="stream">Source DataStream</param>
    /// <param name="tableName">Name to register the table with</param>
    /// <param name="schema">Optional schema definition (column_name: data_type)</param>
    /// <returns>A Table representing the stream</returns>
    public static Table ToTable<T>(this DataStream<T> stream, string tableName, Dictionary<string, string>? schema = null)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));

        var tableSource = new TableSourceDefinition
        {
            TableName = tableName,
            Schema = schema != null ? new Dictionary<string, string>(schema) : []
        };

        return new Table(tableSource);
    }
}
