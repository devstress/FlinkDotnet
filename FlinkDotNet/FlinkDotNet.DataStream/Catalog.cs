using System.Collections.Generic;
using System.Text;
using Flink.JobBuilder.Models;

namespace FlinkDotNet.DataStream;

/// <summary>
/// Represents a generic Apache Flink catalog for metadata management (Flink 1.10+)
/// Supports Hive, JDBC, and GenericInMemory catalog types
/// </summary>
public class Catalog
{
    private readonly CatalogDefinition _definition;

    internal Catalog(CatalogDefinition definition) => this._definition = definition;

    /// <summary>
    /// Gets the catalog name
    /// </summary>
    public string Name => this._definition.CatalogName;

    /// <summary>
    /// Gets the catalog type (hive, jdbc, generic_in_memory)
    /// </summary>
    public string CatalogType => this._definition.CatalogType;

    /// <summary>
    /// Gets the default database name
    /// </summary>
    public string? DefaultDatabase => this._definition.DefaultDatabase;

    /// <summary>
    /// Gets the underlying IR definition
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "RCS1085:Use auto-implemented property", Justification = "Property returns value from private readonly field")]
    public CatalogDefinition Definition => this._definition;

    /// <summary>
    /// Creates a builder for Hive catalog
    /// </summary>
    /// <param name="catalogName">Name for the catalog</param>
    /// <returns>A new CatalogBuilder instance configured for Hive</returns>
    public static CatalogBuilder Hive(string catalogName) => new(catalogName, "hive");

    /// <summary>
    /// Creates a builder for JDBC catalog
    /// </summary>
    /// <param name="catalogName">Name for the catalog</param>
    /// <returns>A new CatalogBuilder instance configured for JDBC</returns>
    public static CatalogBuilder Jdbc(string catalogName) => new(catalogName, "jdbc");

    /// <summary>
    /// Creates a builder for GenericInMemory catalog
    /// </summary>
    /// <param name="catalogName">Name for the catalog</param>
    /// <returns>A new CatalogBuilder instance configured for GenericInMemory</returns>
    public static CatalogBuilder GenericInMemory(string catalogName) => new(catalogName, "generic_in_memory");

    /// <summary>
    /// Generates the CREATE CATALOG SQL DDL statement
    /// </summary>
    /// <returns>SQL DDL string</returns>
    public string ToSql()
    {
        StringBuilder sb = new();
        sb.AppendLine($"CREATE CATALOG {this._definition.CatalogName} WITH (");

        // Add type
        sb.AppendLine($"  'type' = '{this._definition.CatalogType}'");

        // Add default database if specified
        if (!string.IsNullOrEmpty(this._definition.DefaultDatabase))
        {
            sb.AppendLine($"  ,'default-database' = '{this._definition.DefaultDatabase}'");
        }

        // Add custom properties
        foreach (KeyValuePair<string, string> prop in this._definition.Properties)
        {
            sb.AppendLine($"  ,'{prop.Key}' = '{prop.Value}'");
        }

        sb.Append(")");
        return sb.ToString();
    }

    /// <summary>
    /// Generates the USE CATALOG SQL statement
    /// </summary>
    /// <returns>SQL statement</returns>
    public string UseCatalogSql() => $"USE CATALOG {this._definition.CatalogName}";
}
