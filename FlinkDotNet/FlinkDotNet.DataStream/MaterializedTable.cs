using System;
using System.Linq;
using Flink.JobBuilder.Models;

namespace FlinkDotNet.DataStream;

/// <summary>
/// Represents a materialized table in Apache Flink (FLIP-435, Flink 1.20+)
/// Provides a declarative SQL pattern for both batch and streaming ETL with automatic refresh management
/// </summary>
public class MaterializedTable
{
    private readonly MaterializedTableDefinition _definition;

    internal MaterializedTable(MaterializedTableDefinition definition) => this._definition = definition;

    /// <summary>
    /// Gets the table name
    /// </summary>
    public string TableName => this._definition.TableName;

    /// <summary>
    /// Gets the refresh mode (FULL or CONTINUOUS)
    /// </summary>
    public string RefreshMode => this._definition.RefreshMode;

    /// <summary>
    /// Gets the freshness interval
    /// </summary>
    public string? FreshnessInterval => this._definition.FreshnessInterval;

    /// <summary>
    /// Gets the underlying IR definition
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "RCS1085:Use auto-implemented property", Justification = "Property returns value from private readonly field")]
    public MaterializedTableDefinition Definition => this._definition;

    /// <summary>
    /// Suspend the materialized table (stops refresh jobs)
    /// </summary>
    /// <returns>A new MaterializedTable with suspend operation</returns>
    public MaterializedTable Suspend()
    {
        MaterializedTableDefinition suspendDef = new()
        {
            TableName = this._definition.TableName,
            Operation = "SUSPEND",
            ExecutionMode = this._definition.ExecutionMode
        };
        return new MaterializedTable(suspendDef);
    }

    /// <summary>
    /// Resume the materialized table (restarts refresh jobs)
    /// </summary>
    /// <returns>A new MaterializedTable with resume operation</returns>
    public MaterializedTable Resume()
    {
        MaterializedTableDefinition resumeDef = new()
        {
            TableName = this._definition.TableName,
            Operation = "RESUME",
            ExecutionMode = this._definition.ExecutionMode
        };
        return new MaterializedTable(resumeDef);
    }

    /// <summary>
    /// Refresh a specific partition of the materialized table
    /// </summary>
    /// <param name="partitionFilter">Partition filter (e.g., "ds='2024-10-27'")</param>
    /// <returns>A new MaterializedTable with refresh operation</returns>
    public MaterializedTable RefreshPartition(string partitionFilter)
    {
        MaterializedTableDefinition refreshDef = new()
        {
            TableName = this._definition.TableName,
            Operation = "REFRESH",
            PartitionFilter = partitionFilter,
            ExecutionMode = this._definition.ExecutionMode
        };
        return new MaterializedTable(refreshDef);
    }

    /// <summary>
    /// Drop the materialized table
    /// </summary>
    /// <returns>A new MaterializedTable with drop operation</returns>
    public MaterializedTable Drop()
    {
        MaterializedTableDefinition dropDef = new()
        {
            TableName = this._definition.TableName,
            Operation = "DROP",
            ExecutionMode = this._definition.ExecutionMode
        };
        return new MaterializedTable(dropDef);
    }

    /// <summary>
    /// Generate the SQL DDL statement for this materialized table
    /// </summary>
    /// <returns>SQL DDL statement</returns>
    public string ToSql() => this._definition.Operation switch
    {
        "CREATE" => this.GenerateCreateStatement(),
        "SUSPEND" => $"ALTER MATERIALIZED TABLE {this._definition.TableName} SUSPEND",
        "RESUME" => $"ALTER MATERIALIZED TABLE {this._definition.TableName} RESUME",
        "REFRESH" => $"ALTER MATERIALIZED TABLE {this._definition.TableName} REFRESH PARTITION ({this._definition.PartitionFilter})",
        "DROP" => $"DROP MATERIALIZED TABLE {this._definition.TableName}",
        _ => throw new InvalidOperationException($"Unknown operation: {this._definition.Operation}")
    };

    private string GenerateCreateStatement()
    {
        string sql = $"CREATE MATERIALIZED TABLE {this._definition.TableName}";

        // Add schema
        if (this._definition.Schema.Count > 0)
        {
            sql += " (\n";
            sql += string.Join(",\n", this._definition.Schema.Select(kv => $"  {kv.Key} {kv.Value}"));

            // Add primary key if specified
            if (this._definition.PrimaryKey.Count > 0)
            {
                sql += $",\n  PRIMARY KEY({string.Join(", ", this._definition.PrimaryKey)}) NOT ENFORCED";
            }

            sql += "\n)";
        }

        // Add partitioning
        if (this._definition.PartitionBy.Count > 0)
        {
            sql += $"\nPARTITIONED BY ({string.Join(", ", this._definition.PartitionBy)})";
        }

        // Add freshness interval
        if (!string.IsNullOrEmpty(this._definition.FreshnessInterval))
        {
            sql += $"\nFRESHNESS = {this._definition.FreshnessInterval}";
        }

        // Add query
        if (!string.IsNullOrEmpty(this._definition.Query))
        {
            sql += $"\nAS\n{this._definition.Query}";
        }

        return sql;
    }
}

/// <summary>
/// Builder for creating materialized tables with a fluent API
/// </summary>
public class MaterializedTableBuilder
{
    private readonly MaterializedTableDefinition _definition = new();

    /// <summary>
    /// Creates a new MaterializedTableBuilder with the specified table name
    /// </summary>
    /// <param name="tableName">Name of the materialized table</param>
    public MaterializedTableBuilder(string tableName) => this._definition.TableName = tableName;

    /// <summary>
    /// Sets the SQL query for the materialized table
    /// </summary>
    /// <param name="query">SQL SELECT query</param>
    /// <returns>This builder for fluent chaining</returns>
    public MaterializedTableBuilder WithQuery(string query)
    {
        this._definition.Query = query;
        return this;
    }

    /// <summary>
    /// Sets the refresh mode (FULL or CONTINUOUS)
    /// </summary>
    /// <param name="mode">Refresh mode</param>
    /// <returns>This builder for fluent chaining</returns>
    public MaterializedTableBuilder WithRefreshMode(string mode)
    {
        this._definition.RefreshMode = mode;
        return this;
    }

    /// <summary>
    /// Sets the freshness interval
    /// </summary>
    /// <param name="interval">Freshness interval (e.g., TimeSpan.FromMinutes(3))</param>
    /// <returns>This builder for fluent chaining</returns>
    public MaterializedTableBuilder WithFreshness(TimeSpan interval)
    {
        // Convert TimeSpan to SQL INTERVAL format
        if (interval.TotalDays >= 1)
        {
            this._definition.FreshnessInterval = $"INTERVAL '{(int)interval.TotalDays}' DAY";
            return this;
        }

        if (interval.TotalHours >= 1)
        {
            this._definition.FreshnessInterval = $"INTERVAL '{(int)interval.TotalHours}' HOUR";
            return this;
        }

        if (interval.TotalMinutes >= 1)
        {
            this._definition.FreshnessInterval = $"INTERVAL '{(int)interval.TotalMinutes}' MINUTE";
            return this;
        }

        this._definition.FreshnessInterval = $"INTERVAL '{(int)interval.TotalSeconds}' SECOND";
        return this;
    }

    /// <summary>
    /// Sets the freshness interval using SQL syntax
    /// </summary>
    /// <param name="intervalSql">SQL INTERVAL syntax (e.g., "INTERVAL '3' MINUTE")</param>
    /// <returns>This builder for fluent chaining</returns>
    public MaterializedTableBuilder WithFreshnessInterval(string intervalSql)
    {
        this._definition.FreshnessInterval = intervalSql;
        return this;
    }

    /// <summary>
    /// Sets the primary key columns
    /// </summary>
    /// <param name="columns">Primary key column names</param>
    /// <returns>This builder for fluent chaining</returns>
    public MaterializedTableBuilder WithPrimaryKey(params string[] columns)
    {
        this._definition.PrimaryKey.Clear();
        this._definition.PrimaryKey.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Sets the partition columns
    /// </summary>
    /// <param name="columns">Partition column names</param>
    /// <returns>This builder for fluent chaining</returns>
    public MaterializedTableBuilder WithPartitioning(params string[] columns)
    {
        this._definition.PartitionBy.Clear();
        this._definition.PartitionBy.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Adds a column to the schema
    /// </summary>
    /// <param name="columnName">Column name</param>
    /// <param name="dataType">SQL data type</param>
    /// <returns>This builder for fluent chaining</returns>
    public MaterializedTableBuilder AddColumn(string columnName, string dataType)
    {
        this._definition.Schema[columnName] = dataType;
        return this;
    }

    /// <summary>
    /// Sets a custom property
    /// </summary>
    /// <param name="key">Property key</param>
    /// <param name="value">Property value</param>
    /// <returns>This builder for fluent chaining</returns>
    public MaterializedTableBuilder WithProperty(string key, string value)
    {
        this._definition.Properties[key] = value;
        return this;
    }

    /// <summary>
    /// Sets the execution mode (tableenv or gateway)
    /// </summary>
    /// <param name="mode">Execution mode</param>
    /// <returns>This builder for fluent chaining</returns>
    public MaterializedTableBuilder WithExecutionMode(string mode)
    {
        this._definition.ExecutionMode = mode;
        return this;
    }

    /// <summary>
    /// Builds the MaterializedTable
    /// </summary>
    /// <returns>A new MaterializedTable instance</returns>
    public MaterializedTable Build()
    {
        // Validation
        if (string.IsNullOrEmpty(this._definition.TableName))
        {
            throw new InvalidOperationException("Table name is required");
        }

        return string.IsNullOrEmpty(this._definition.Query) && this._definition.Schema.Count == 0
            ? throw new InvalidOperationException("Either query or schema must be specified")
            : new MaterializedTable(this._definition);
    }
}

/// <summary>
/// Extension methods for creating materialized tables
/// </summary>
public static class MaterializedTableExtensions
{
    /// <summary>
    /// Creates a new materialized table builder
    /// </summary>
    /// <param name="env">Stream execution environment</param>
    /// <param name="tableName">Name of the materialized table</param>
    /// <returns>A new MaterializedTableBuilder</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Extension method pattern requires this parameter")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "RCS1175:Unused 'this' parameter", Justification = "Extension method pattern requires this parameter")]
    public static MaterializedTableBuilder CreateMaterializedTable(
        this StreamExecutionEnvironment env,
        string tableName) => new(tableName);
}
