using System;
using System.Collections.Generic;
using Flink.JobBuilder.Models;

namespace FlinkDotNet.DataStream;

/// <summary>
/// Builder for creating Database instances with fluent API
/// </summary>
public class DatabaseBuilder
{
    private readonly string _catalogName;
    private readonly string _databaseName;
    private bool _ifNotExists;
    private string? _comment;
    private readonly Dictionary<string, string> _properties = [];

    internal DatabaseBuilder(string catalogName, string databaseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogName);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        this._catalogName = catalogName;
        this._databaseName = databaseName;
    }

    /// <summary>
    /// Sets the IF NOT EXISTS flag
    /// </summary>
    /// <returns>This builder instance for method chaining</returns>
    public DatabaseBuilder IfNotExists()
    {
        this._ifNotExists = true;
        return this;
    }

    /// <summary>
    /// Sets the database comment
    /// </summary>
    /// <param name="comment">Database comment</param>
    /// <returns>This builder instance for method chaining</returns>
    public DatabaseBuilder WithComment(string comment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(comment);
        this._comment = comment;
        return this;
    }

    /// <summary>
    /// Adds a database property
    /// </summary>
    /// <param name="key">Property key</param>
    /// <param name="value">Property value</param>
    /// <returns>This builder instance for method chaining</returns>
    public DatabaseBuilder WithProperty(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        this._properties[key] = value;
        return this;
    }

    /// <summary>
    /// Builds the Database instance
    /// </summary>
    /// <returns>A new Database instance</returns>
    public Database Build()
    {
        DatabaseDefinition definition = new()
        {
            CatalogName = this._catalogName,
            DatabaseName = this._databaseName,
            IfNotExists = this._ifNotExists,
            Comment = this._comment,
            Properties = new Dictionary<string, string>(this._properties)
        };

        return new Database(definition);
    }
}
