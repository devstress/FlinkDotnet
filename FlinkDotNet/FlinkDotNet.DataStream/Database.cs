using System.Collections.Generic;
using System.Text;
using Flink.JobBuilder.Models;

namespace FlinkDotNet.DataStream;

/// <summary>
/// Represents a database in a Flink catalog
/// </summary>
public class Database
{
    private readonly DatabaseDefinition _definition;

    internal Database(DatabaseDefinition definition) => this._definition = definition;

    /// <summary>
    /// Gets the catalog name
    /// </summary>
    public string CatalogName => this._definition.CatalogName;

    /// <summary>
    /// Gets the database name
    /// </summary>
    public string DatabaseName => this._definition.DatabaseName;

    /// <summary>
    /// Gets the comment
    /// </summary>
    public string? Comment => this._definition.Comment;

    /// <summary>
    /// Gets the underlying IR definition
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "RCS1085:Use auto-implemented property", Justification = "Property returns value from private readonly field")]
    public DatabaseDefinition Definition => this._definition;

    /// <summary>
    /// Creates a new database builder
    /// </summary>
    /// <param name="catalogName">Name of the catalog</param>
    /// <param name="databaseName">Name of the database</param>
    /// <returns>A new DatabaseBuilder instance</returns>
    public static DatabaseBuilder Builder(string catalogName, string databaseName) => new(catalogName, databaseName);

    /// <summary>
    /// Generates the CREATE DATABASE SQL DDL statement
    /// </summary>
    /// <returns>SQL DDL string</returns>
    public string ToSql()
    {
        StringBuilder sb = new();

        // CREATE DATABASE [IF NOT EXISTS]
        sb.Append("CREATE DATABASE ");
        if (this._definition.IfNotExists)
        {
            sb.Append("IF NOT EXISTS ");
        }
        sb.Append(this._definition.CatalogName + "." + this._definition.DatabaseName);

        // Add comment if specified
        if (!string.IsNullOrEmpty(this._definition.Comment))
        {
            sb.Append($" COMMENT '{this._definition.Comment}'");
        }

        // Add properties if any
        if (this._definition.Properties.Count > 0)
        {
            sb.Append(" WITH (");
            bool first = true;
            foreach (KeyValuePair<string, string> prop in this._definition.Properties)
            {
                if (!first)
                {
                    sb.Append(", ");
                }
                sb.Append($"'{prop.Key}' = '{prop.Value}'");
                first = false;
            }
            sb.Append(')');
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates the USE DATABASE SQL statement
    /// </summary>
    /// <returns>SQL statement</returns>
    public string UseDatabaseSql() => $"USE {this._definition.CatalogName}.{this._definition.DatabaseName}";

    /// <summary>
    /// Generates the DROP DATABASE SQL statement
    /// </summary>
    /// <param name="ifExists">Whether to add IF EXISTS clause</param>
    /// <param name="cascade">Whether to add CASCADE clause</param>
    /// <returns>SQL statement</returns>
    public string DropDatabaseSql(bool ifExists = false, bool cascade = false)
    {
        StringBuilder sb = new();
        sb.Append("DROP DATABASE ");
        if (ifExists)
        {
            sb.Append("IF EXISTS ");
        }
        sb.Append(this._definition.CatalogName + "." + this._definition.DatabaseName);
        if (cascade)
        {
            sb.Append(" CASCADE");
        }
        return sb.ToString();
    }
}
