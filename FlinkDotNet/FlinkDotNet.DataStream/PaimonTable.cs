using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Flink.JobBuilder.Models;

namespace FlinkDotNet.DataStream;

/// <summary>
/// Changelog producer mode for Paimon tables
/// </summary>
public enum ChangelogProducerMode
{
    /// <summary>
    /// No extra changelog generation (fastest, lowest overhead)
    /// Best for snapshot queries
    /// </summary>
    None,

    /// <summary>
    /// Relies on input to produce changelog (for CDC scenarios)
    /// All records written to changelog files
    /// </summary>
    Input,

    /// <summary>
    /// Looks up previous values to build changelog (uses memory/disk cache)
    /// Best for incomplete input requiring correct streaming updates
    /// </summary>
    Lookup,

    /// <summary>
    /// Produces changelog via periodic full compaction (most complete, highest overhead)
    /// Best for critical correctness requirements
    /// </summary>
    FullCompaction
}

/// <summary>
/// Represents an Apache Paimon table with ACID semantics (Flink 1.15+)
/// Provides lakehouse table storage with support for upsert, delete, and update operations
/// </summary>
public class PaimonTable
{
    private readonly PaimonTableDefinition _definition;

    internal PaimonTable(PaimonTableDefinition definition) => this._definition = definition;

    /// <summary>
    /// Gets the catalog name
    /// </summary>
    public string CatalogName => this._definition.CatalogName;

    /// <summary>
    /// Gets the table name
    /// </summary>
    public string TableName => this._definition.TableName;

    /// <summary>
    /// Gets the underlying IR definition
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "RCS1085:Use auto-implemented property", Justification = "Property returns value from private readonly field")]
    public PaimonTableDefinition Definition => this._definition;

    /// <summary>
    /// Creates a new table builder
    /// </summary>
    /// <param name="catalogName">Name of the catalog</param>
    /// <param name="tableName">Name for the table</param>
    /// <returns>A new PaimonTableBuilder instance</returns>
    public static PaimonTableBuilder Builder(string catalogName, string tableName) => new(catalogName, tableName);

    /// <summary>
    /// Generates the CREATE TABLE SQL DDL statement
    /// </summary>
    /// <returns>SQL DDL string</returns>
    public string ToSql()
    {
        StringBuilder sb = new();

        // CREATE TABLE statement
        sb.AppendLine($"CREATE TABLE {this._definition.CatalogName}.{this._definition.TableName} (");

        // Add columns
        string[] columns = [.. this._definition.Schema.Select(kvp => $"  {kvp.Key} {kvp.Value}")];
        sb.AppendJoin("," + Environment.NewLine, columns).AppendLine();

        // Add primary key if specified
        if (this._definition.PrimaryKey.Count > 0)
        {
            string primaryKeyColumns = string.Join(", ", this._definition.PrimaryKey);
            sb.AppendLine($"  ,PRIMARY KEY ({primaryKeyColumns}) NOT ENFORCED");
        }

        sb.Append(")");

        // Add partitioning if specified
        if (this._definition.PartitionKeys.Count > 0)
        {
            string partitionColumns = string.Join(", ", this._definition.PartitionKeys);
            sb.AppendLine().Append($" PARTITIONED BY ({partitionColumns})");
        }

        // Add table properties
        bool hasProperties = this._definition.Buckets.HasValue ||
                             (!string.IsNullOrEmpty(this._definition.ChangelogProducerMode) && this._definition.ChangelogProducerMode != "none") ||
                             this._definition.TableProperties.Count > 0;

        if (hasProperties)
        {
            sb.AppendLine(" WITH (");
            List<string> properties = [];

            // Add bucket configuration
            if (this._definition.Buckets.HasValue)
            {
                properties.Add($"  'bucket' = '{this._definition.Buckets.Value}'");
            }

            // Add changelog producer mode
            if (!string.IsNullOrEmpty(this._definition.ChangelogProducerMode) && this._definition.ChangelogProducerMode != "none")
            {
                // Paimon configuration requires lowercase values, with special handling for FULLCOMPACTION
                string normalizedMode = this._definition.ChangelogProducerMode.ToUpperInvariant() switch
                {
                    "FULLCOMPACTION" => "full-compaction",
                    string s => s.ToLowerInvariant()
                };
                properties.Add($"  'changelog-producer' = '{normalizedMode}'");
            }

            // Add custom properties
            foreach (KeyValuePair<string, string> prop in this._definition.TableProperties)
            {
                properties.Add($"  '{prop.Key}' = '{prop.Value}'");
            }

            sb.AppendJoin("," + Environment.NewLine, properties).AppendLine()
                .AppendLine().Append(")");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Drops the table
    /// </summary>
    /// <returns>A new PaimonTable with drop operation</returns>
    public PaimonTable Drop()
    {
        PaimonTableDefinition dropDef = new()
        {
            CatalogName = this._definition.CatalogName,
            TableName = this._definition.TableName,
            Operation = "DROP"
        };
        return new PaimonTable(dropDef);
    }
}

/// <summary>
/// Builder for creating PaimonTable instances with fluent API
/// </summary>
public class PaimonTableBuilder
{
    private readonly PaimonTableDefinition _definition = new();

    /// <summary>
    /// Initializes a new table builder
    /// </summary>
    /// <param name="catalogName">Name of the catalog</param>
    /// <param name="tableName">Name for the table</param>
    public PaimonTableBuilder(string catalogName, string tableName)
    {
        this._definition.CatalogName = catalogName;
        this._definition.TableName = tableName;
    }

    /// <summary>
    /// Adds a column to the table schema
    /// </summary>
    /// <param name="name">Column name</param>
    /// <param name="type">Flink SQL data type (e.g., "BIGINT", "STRING", "DECIMAL(10,2)")</param>
    /// <returns>This builder instance</returns>
    public PaimonTableBuilder WithColumn(string name, string type)
    {
        this._definition.Schema[name] = type;
        return this;
    }

    /// <summary>
    /// Sets the primary key columns (required for ACID semantics)
    /// </summary>
    /// <param name="columns">Primary key column names</param>
    /// <returns>This builder instance</returns>
    public PaimonTableBuilder WithPrimaryKey(params string[] columns)
    {
        this._definition.PrimaryKey.Clear();
        this._definition.PrimaryKey.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Sets the partition columns
    /// </summary>
    /// <param name="columns">Partition column names</param>
    /// <returns>This builder instance</returns>
    public PaimonTableBuilder WithPartition(params string[] columns)
    {
        this._definition.PartitionKeys.Clear();
        this._definition.PartitionKeys.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Sets the number of buckets for parallelism
    /// </summary>
    /// <param name="buckets">Number of buckets</param>
    /// <returns>This builder instance</returns>
    public PaimonTableBuilder WithBuckets(int buckets)
    {
        this._definition.Buckets = buckets;
        return this;
    }

    /// <summary>
    /// Sets the changelog producer mode
    /// </summary>
    /// <param name="mode">Changelog producer mode</param>
    /// <returns>This builder instance</returns>
    public PaimonTableBuilder WithChangelogMode(ChangelogProducerMode mode)
    {
        // Paimon configuration requires lowercase values, with special handling for FULLCOMPACTION
        this._definition.ChangelogProducerMode = mode.ToString().ToUpperInvariant() switch
        {
            "FULLCOMPACTION" => "full-compaction",
            string s => s.ToLowerInvariant()
        };
        return this;
    }

    /// <summary>
    /// Adds a custom table property
    /// </summary>
    /// <param name="key">Property key</param>
    /// <param name="value">Property value</param>
    /// <returns>This builder instance</returns>
    public PaimonTableBuilder WithProperty(string key, string value)
    {
        this._definition.TableProperties[key] = value;
        return this;
    }

    /// <summary>
    /// Builds the PaimonTable instance
    /// </summary>
    /// <returns>A new PaimonTable instance</returns>
    /// <exception cref="InvalidOperationException">Thrown when required configuration is missing</exception>
    public PaimonTable Build()
    {
        // Validate required fields
        if (string.IsNullOrEmpty(this._definition.CatalogName))
        {
            throw new InvalidOperationException("Catalog name is required");
        }

        if (string.IsNullOrEmpty(this._definition.TableName))
        {
            throw new InvalidOperationException("Table name is required");
        }

        if (this._definition.Schema.Count == 0)
        {
            throw new InvalidOperationException("At least one column is required");
        }

        if (this._definition.PrimaryKey.Count == 0)
        {
            throw new InvalidOperationException("Primary key is required for Paimon ACID tables");
        }

        return new PaimonTable(this._definition);
    }
}
