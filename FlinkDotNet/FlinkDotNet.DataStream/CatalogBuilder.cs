using System;
using System.Collections.Generic;
using Flink.JobBuilder.Models;

namespace FlinkDotNet.DataStream;

/// <summary>
/// Builder for creating Catalog instances with fluent API
/// </summary>
public class CatalogBuilder
{
    private readonly string _catalogName;
    private readonly string _catalogType;
    private string? _defaultDatabase;
    private readonly Dictionary<string, string> _properties = [];

    internal CatalogBuilder(string catalogName, string catalogType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogName);
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogType);

        this._catalogName = catalogName;
        this._catalogType = catalogType;
    }

    /// <summary>
    /// Sets the default database for this catalog
    /// </summary>
    /// <param name="defaultDatabase">Default database name</param>
    /// <returns>This builder instance for method chaining</returns>
    public CatalogBuilder WithDefaultDatabase(string defaultDatabase)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultDatabase);
        this._defaultDatabase = defaultDatabase;
        return this;
    }

    /// <summary>
    /// Adds a catalog property
    /// </summary>
    /// <param name="key">Property key</param>
    /// <param name="value">Property value</param>
    /// <returns>This builder instance for method chaining</returns>
    public CatalogBuilder WithProperty(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        this._properties[key] = value;
        return this;
    }

    /// <summary>
    /// Sets the Hive configuration directory (for Hive catalog)
    /// </summary>
    /// <param name="hiveConfDir">Path to Hive configuration directory</param>
    /// <returns>This builder instance for method chaining</returns>
    public CatalogBuilder WithHiveConfDir(string hiveConfDir)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hiveConfDir);
        return this.WithProperty("hive-conf-dir", hiveConfDir);
    }

    /// <summary>
    /// Sets the JDBC connection URL (for JDBC catalog)
    /// </summary>
    /// <param name="jdbcUrl">JDBC connection URL</param>
    /// <returns>This builder instance for method chaining</returns>
    public CatalogBuilder WithJdbcUrl(string jdbcUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jdbcUrl);
        
        // Set default database if not already set
        this._defaultDatabase ??= "default";
        
        this._properties["base-url"] = jdbcUrl;
        
        // Only set username/password if not already set
        if (!this._properties.ContainsKey("username"))
        {
            this._properties["username"] = "admin"; // Default JDBC username
        }
        if (!this._properties.ContainsKey("password"))
        {
            this._properties["password"] = "admin"; // Default JDBC password
        }
        
        return this;
    }

    /// <summary>
    /// Sets the JDBC username (for JDBC catalog)
    /// </summary>
    /// <param name="username">JDBC username</param>
    /// <returns>This builder instance for method chaining</returns>
    public CatalogBuilder WithJdbcUsername(string username)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        return this.WithProperty("username", username);
    }

    /// <summary>
    /// Sets the JDBC password (for JDBC catalog)
    /// </summary>
    /// <param name="password">JDBC password</param>
    /// <returns>This builder instance for method chaining</returns>
    public CatalogBuilder WithJdbcPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        return this.WithProperty("password", password);
    }

    /// <summary>
    /// Builds the Catalog instance
    /// </summary>
    /// <returns>A new Catalog instance</returns>
    public Catalog Build()
    {
        CatalogDefinition definition = new()
        {
            CatalogName = this._catalogName,
            CatalogType = this._catalogType,
            DefaultDatabase = this._defaultDatabase,
            Properties = new Dictionary<string, string>(this._properties)
        };

        return new Catalog(definition);
    }
}
