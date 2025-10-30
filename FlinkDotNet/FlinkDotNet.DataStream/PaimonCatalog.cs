using System;
using System.Collections.Generic;
using System.Text;
using Flink.JobBuilder.Models;

namespace FlinkDotNet.DataStream;

/// <summary>
/// Represents an Apache Paimon catalog for lakehouse table storage (Flink 1.15+)
/// Provides ACID-compliant table storage with support for both batch and streaming workloads
/// </summary>
public class PaimonCatalog
{
    private readonly PaimonCatalogDefinition _definition;

    internal PaimonCatalog(PaimonCatalogDefinition definition) => this._definition = definition;

    /// <summary>
    /// Gets the catalog name
    /// </summary>
    public string Name => this._definition.CatalogName;

    /// <summary>
    /// Gets the catalog type (paimon or paimon-generic)
    /// </summary>
    public string CatalogType => this._definition.CatalogType;

    /// <summary>
    /// Gets the warehouse path
    /// </summary>
    public string Warehouse => this._definition.Warehouse;

    /// <summary>
    /// Gets the underlying IR definition
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "RCS1085:Use auto-implemented property", Justification = "Property returns value from private readonly field")]
    public PaimonCatalogDefinition Definition => this._definition;

    /// <summary>
    /// Creates a new catalog builder
    /// </summary>
    /// <param name="catalogName">Name for the catalog</param>
    /// <returns>A new PaimonCatalogBuilder instance</returns>
    public static PaimonCatalogBuilder Builder(string catalogName) => new(catalogName);

    /// <summary>
    /// Generates the CREATE CATALOG SQL DDL statement
    /// </summary>
    /// <returns>SQL DDL string</returns>
    public string ToSql()
    {
        StringBuilder sb = new();
        sb.AppendLine($"CREATE CATALOG {this._definition.CatalogName} WITH (");

        // Add type
        sb.AppendLine($"  'type' = '{this._definition.CatalogType}',");

        // Add warehouse
        sb.AppendLine($"  'warehouse' = '{this._definition.Warehouse}'");

        // Add Hive conf dir if specified
        if (!string.IsNullOrEmpty(this._definition.HiveConfDir))
        {
            sb.AppendLine($"  ,'hive-conf-dir' = '{this._definition.HiveConfDir}'");
        }

        // Add Hadoop conf dir if specified
        if (!string.IsNullOrEmpty(this._definition.HadoopConfDir))
        {
            sb.AppendLine($"  ,'hadoop-conf-dir' = '{this._definition.HadoopConfDir}'");
        }

        // Add custom properties
        foreach (KeyValuePair<string, string> prop in this._definition.Properties)
        {
            sb.AppendLine($"  ,'{prop.Key}' = '{prop.Value}'");
        }

        sb.Append(")");
        return sb.ToString();
    }
}

/// <summary>
/// Builder for creating PaimonCatalog instances with fluent API
/// </summary>
public class PaimonCatalogBuilder
{
    private readonly PaimonCatalogDefinition _definition = new();

    /// <summary>
    /// Initializes a new catalog builder
    /// </summary>
    /// <param name="catalogName">Name for the catalog</param>
    public PaimonCatalogBuilder(string catalogName) => this._definition.CatalogName = catalogName;

    /// <summary>
    /// Sets the warehouse path for the catalog
    /// </summary>
    /// <param name="warehouse">Warehouse path (file://, hdfs://, s3://, oss://)</param>
    /// <returns>This builder instance</returns>
    public PaimonCatalogBuilder WithWarehouse(string warehouse)
    {
        this._definition.Warehouse = warehouse;
        return this;
    }

    /// <summary>
    /// Configures Hive metastore integration
    /// </summary>
    /// <param name="hiveConfDir">Path to Hive configuration directory</param>
    /// <returns>This builder instance</returns>
    public PaimonCatalogBuilder WithHiveMetastore(string hiveConfDir)
    {
        this._definition.CatalogType = "paimon-generic";
        this._definition.HiveConfDir = hiveConfDir;
        return this;
    }

    /// <summary>
    /// Sets Hadoop configuration directory
    /// </summary>
    /// <param name="hadoopConfDir">Path to Hadoop configuration directory</param>
    /// <returns>This builder instance</returns>
    public PaimonCatalogBuilder WithHadoopConf(string hadoopConfDir)
    {
        this._definition.HadoopConfDir = hadoopConfDir;
        return this;
    }

    /// <summary>
    /// Adds a custom catalog property
    /// </summary>
    /// <param name="key">Property key</param>
    /// <param name="value">Property value</param>
    /// <returns>This builder instance</returns>
    public PaimonCatalogBuilder WithProperty(string key, string value)
    {
        this._definition.Properties[key] = value;
        return this;
    }

    /// <summary>
    /// Builds the PaimonCatalog instance
    /// </summary>
    /// <returns>A new PaimonCatalog instance</returns>
    /// <exception cref="ArgumentException">Thrown when required configuration is missing</exception>
    public PaimonCatalog Build()
    {
        // Validate required fields
        ArgumentException.ThrowIfNullOrWhiteSpace(this._definition.CatalogName);
        ArgumentException.ThrowIfNullOrWhiteSpace(this._definition.Warehouse);

        return new PaimonCatalog(this._definition);
    }
}
