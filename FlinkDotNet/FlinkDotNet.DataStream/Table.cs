using System;
using System.Collections.Generic;
using System.Text;
using Flink.JobBuilder.Models;

namespace FlinkDotNet.DataStream;

/// <summary>
/// Represents a table in Apache Flink Table API
/// Provides ML_PREDICT and other Table API operations
/// </summary>
public class Table
{
    private readonly string _tableName;
    private readonly Dictionary<string, string> _schema = [];
    private readonly List<MLPredictDefinition> _mlPredictOperations = [];

    /// <summary>
    /// Initializes a new instance of the Table class
    /// </summary>
    /// <param name="tableName">Name of the table</param>
    public Table(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));
        }

        this._tableName = tableName;
    }

    /// <summary>
    /// Gets the table name
    /// </summary>
    public string TableName => this._tableName;

    /// <summary>
    /// Gets the table schema
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "RCS1085:Use auto-implemented property", Justification = "Property returns value from private readonly field")]
    public IReadOnlyDictionary<string, string> Schema => this._schema;

    /// <summary>
    /// Apply ML_PREDICT operation to this table
    /// </summary>
    /// <param name="modelName">Name of the registered model</param>
    /// <param name="inputColumns">Input column names to pass to the model</param>
    /// <returns>New table with model predictions</returns>
    public Table Predict(string modelName, params string[] inputColumns)
    {
        ArgumentNullException.ThrowIfNull(modelName);

        if (string.IsNullOrWhiteSpace(modelName))
        {
            throw new ArgumentException("Model name cannot be null or empty", nameof(modelName));
        }

        ArgumentNullException.ThrowIfNull(inputColumns);

        if (inputColumns.Length == 0)
        {
            throw new ArgumentException("At least one input column must be specified", nameof(inputColumns));
        }

        // Validate input columns exist in schema
        string? invalidColumn = Array.Find(inputColumns, col => !this._schema.ContainsKey(col));
        if (invalidColumn != null)
        {
            throw new ArgumentException($"Column '{invalidColumn}' does not exist in table schema", nameof(inputColumns));
        }

        // Create new table with ML_PREDICT operation
        Table resultTable = new(this._tableName);

        // Copy schema
        foreach (KeyValuePair<string, string> col in this._schema)
        {
            resultTable._schema[col.Key] = col.Value;
        }

        // Copy existing ML_PREDICT operations
        resultTable._mlPredictOperations.AddRange(this._mlPredictOperations);

        // Add new ML_PREDICT operation
        MLPredictDefinition mlPredict = new()
        {
            ModelName = modelName,
            InputColumns = [.. inputColumns],
            OutputColumns = [], // Will be inferred from model schema
            OutputPrefix = null
        };

        resultTable._mlPredictOperations.Add(mlPredict);

        return resultTable;
    }

    /// <summary>
    /// Apply ML_PREDICT operation with custom output prefix
    /// </summary>
    /// <param name="modelName">Name of the registered model</param>
    /// <param name="outputPrefix">Prefix for output columns (e.g., "ml" for "AS ml")</param>
    /// <param name="inputColumns">Input column names to pass to the model</param>
    /// <returns>New table with model predictions</returns>
    public Table PredictWithPrefix(string modelName, string outputPrefix, params string[] inputColumns)
    {
        Table result = this.Predict(modelName, inputColumns);

        // Update the last ML_PREDICT operation with output prefix
        if (result._mlPredictOperations.Count > 0)
        {
            result._mlPredictOperations[^1].OutputPrefix = outputPrefix;
        }

        return result;
    }

    /// <summary>
    /// Get the ML_PREDICT definition for this table (for testing)
    /// </summary>
    /// <returns>The last ML_PREDICT definition or null</returns>
    public MLPredictDefinition? GetMLPredictDefinition() =>
        this._mlPredictOperations.Count > 0 ? this._mlPredictOperations[^1] : null;

    /// <summary>
    /// Generate SQL for this table including ML_PREDICT operations
    /// </summary>
    /// <returns>SQL query string</returns>
    public string ToSql()
    {
        if (this._mlPredictOperations.Count == 0)
        {
            return $"SELECT * FROM {this._tableName}";
        }

        StringBuilder sql = new();
        sql.Append("SELECT * FROM ML_PREDICT(\n");
        sql.Append($"  TABLE {this._tableName},\n");

        MLPredictDefinition mlPredict = this._mlPredictOperations[^1];
        sql.Append($"  MODEL {mlPredict.ModelName},\n");

        string columns = string.Join(", ", mlPredict.InputColumns);
        sql.Append($"  DESCRIPTOR({columns})\n");
        sql.Append(")");

        if (!string.IsNullOrWhiteSpace(mlPredict.OutputPrefix))
        {
            sql.Append($" AS {mlPredict.OutputPrefix}");
        }

        return sql.ToString();
    }

    /// <summary>
    /// Add a column to the table schema (for testing/builder purposes)
    /// </summary>
    /// <param name="columnName">Column name</param>
    /// <param name="dataType">Flink SQL data type</param>
    internal void AddColumn(string columnName, string dataType)
    {
        this._schema[columnName] = dataType;
    }
}

/// <summary>
/// Builder for creating tables with schema
/// </summary>
public class TableBuilder
{
    private readonly string _tableName;
    private readonly Dictionary<string, string> _columns = [];

    /// <summary>
    /// Initializes a new instance of the TableBuilder class
    /// </summary>
    /// <param name="tableName">Name of the table</param>
    public TableBuilder(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));
        }

        this._tableName = tableName;
    }

    /// <summary>
    /// Add a column to the table schema
    /// </summary>
    /// <param name="columnName">Column name</param>
    /// <param name="dataType">Flink SQL data type (e.g., "STRING", "BIGINT", "DOUBLE")</param>
    /// <returns>This builder for fluent API</returns>
    public TableBuilder AddColumn(string columnName, string dataType)
    {
        this._columns[columnName] = dataType;
        return this;
    }

    /// <summary>
    /// Build the table
    /// </summary>
    /// <returns>Configured table</returns>
    public Table Build()
    {
        Table table = new(this._tableName);

        foreach (KeyValuePair<string, string> col in this._columns)
        {
            table.AddColumn(col.Key, col.Value);
        }

        return table;
    }
}

/// <summary>
/// Extension methods for creating tables
/// </summary>
public static class TableExtensions
{
    /// <summary>
    /// Creates a new table builder
    /// </summary>
    /// <param name="env">Stream execution environment</param>
    /// <param name="tableName">Name of the table</param>
    /// <returns>A new TableBuilder</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Extension method pattern requires this parameter")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "RCS1175:Unused 'this' parameter", Justification = "Extension method pattern requires this parameter")]
    public static TableBuilder CreateTable(
        this StreamExecutionEnvironment env,
        string tableName) => new(tableName);
}
