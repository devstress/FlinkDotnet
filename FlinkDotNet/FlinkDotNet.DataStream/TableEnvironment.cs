using System;
using System.Collections.Generic;
using System.Linq;

namespace FlinkDotNet.DataStream;

/// <summary>
/// Table environment for managing tables and models in Apache Flink
/// Provides programmatic API for model management (complementing SQL DDL)
/// </summary>
public class TableEnvironment
{
    private readonly Dictionary<string, Model> _registeredModels = new();
    private readonly Dictionary<string, Table> _registeredTables = new();
    private readonly Dictionary<string, Catalog> _registeredCatalogs = new();
    private readonly Dictionary<string, Database> _registeredDatabases = new();
    private string? _currentCatalog;
    private string? _currentDatabase;

    internal TableEnvironment(StreamExecutionEnvironment env) =>
        // Store reference for future use
        _ = env;

    // ============================================
    // Catalog Management (Flink 1.10+)
    // ============================================

    /// <summary>
    /// Registers a catalog with this table environment
    /// </summary>
    /// <param name="catalog">Catalog to register</param>
    public void RegisterCatalog(Catalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        if (this._registeredCatalogs.ContainsKey(catalog.Name))
        {
            throw new InvalidOperationException($"Catalog '{catalog.Name}' already registered");
        }

        this._registeredCatalogs[catalog.Name] = catalog;
    }

    /// <summary>
    /// Sets the current catalog
    /// </summary>
    /// <param name="catalogName">Name of the catalog to use</param>
    public void UseCatalog(string catalogName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogName);

        if (!this._registeredCatalogs.ContainsKey(catalogName))
        {
            throw new InvalidOperationException($"Catalog '{catalogName}' not found");
        }

        this._currentCatalog = catalogName;
    }

    /// <summary>
    /// Gets the current catalog name
    /// </summary>
    /// <returns>Current catalog name or null if none set</returns>
    public string? GetCurrentCatalog() => this._currentCatalog;

    /// <summary>
    /// Lists all registered catalogs
    /// </summary>
    /// <returns>Collection of catalog names</returns>
    public IEnumerable<string> ListCatalogs() => this._registeredCatalogs.Keys;

    /// <summary>
    /// Gets a registered catalog by name
    /// </summary>
    /// <param name="catalogName">Name of the catalog</param>
    /// <returns>Catalog instance or null if not found</returns>
    public Catalog? GetCatalog(string catalogName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogName);
        return this._registeredCatalogs.GetValueOrDefault(catalogName);
    }

    // ============================================
    // Database Management (Flink 1.10+)
    // ============================================

    /// <summary>
    /// Creates and registers a database
    /// </summary>
    /// <param name="database">Database to create</param>
    public void CreateDatabase(Database database)
    {
        ArgumentNullException.ThrowIfNull(database);

        string key = $"{database.CatalogName}.{database.DatabaseName}";
        if (this._registeredDatabases.ContainsKey(key))
        {
            throw new InvalidOperationException($"Database '{key}' already exists");
        }

        this._registeredDatabases[key] = database;
    }

    /// <summary>
    /// Sets the current database
    /// </summary>
    /// <param name="catalogName">Catalog name</param>
    /// <param name="databaseName">Database name</param>
    public void UseDatabase(string catalogName, string databaseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogName);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        string key = $"{catalogName}.{databaseName}";
        if (!this._registeredDatabases.ContainsKey(key))
        {
            throw new InvalidOperationException($"Database '{key}' not found");
        }

        this._currentCatalog = catalogName;
        this._currentDatabase = databaseName;
    }

    /// <summary>
    /// Gets the current database name
    /// </summary>
    /// <returns>Current database name or null if none set</returns>
    public string? GetCurrentDatabase() => this._currentDatabase;

    /// <summary>
    /// Lists all registered databases in a catalog
    /// </summary>
    /// <param name="catalogName">Catalog name</param>
    /// <returns>Collection of database names</returns>
    public IEnumerable<string> ListDatabases(string catalogName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogName);

        return this._registeredDatabases.Keys
            .Where(k => k.StartsWith($"{catalogName}.", StringComparison.Ordinal))
            .Select(k => k[(catalogName.Length + 1)..]);
    }

    /// <summary>
    /// Gets a registered database
    /// </summary>
    /// <param name="catalogName">Catalog name</param>
    /// <param name="databaseName">Database name</param>
    /// <returns>Database instance or null if not found</returns>
    public Database? GetDatabase(string catalogName, string databaseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogName);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        string key = $"{catalogName}.{databaseName}";
        return this._registeredDatabases.GetValueOrDefault(key);
    }

    // ============================================
    // Model Management (Flink 2.1+)
    // ============================================

    /// <summary>
    /// Creates and registers a model programmatically
    /// </summary>
    /// <param name="modelName">Name of the model</param>
    /// <param name="model">Model instance</param>
    public void CreateModel(string modelName, Model model)
    {
        ArgumentNullException.ThrowIfNull(modelName);
        ArgumentNullException.ThrowIfNull(model);

        if (this._registeredModels.ContainsKey(modelName))
        {
            throw new InvalidOperationException($"Model '{modelName}' already exists");
        }

        this._registeredModels[modelName] = model;
    }

    /// <summary>
    /// Gets a registered model by name
    /// </summary>
    /// <param name="modelName">Name of the model</param>
    /// <returns>Model instance</returns>
    public Model? GetModel(string modelName)
    {
        ArgumentNullException.ThrowIfNull(modelName);
        return this._registeredModels.GetValueOrDefault(modelName);
    }

    /// <summary>
    /// Lists all registered models
    /// </summary>
    /// <returns>Collection of model names</returns>
    public IEnumerable<string> ListModels() => this._registeredModels.Keys;

    /// <summary>
    /// Drops a registered model
    /// </summary>
    /// <param name="modelName">Name of the model to drop</param>
    public void DropModel(string modelName)
    {
        ArgumentNullException.ThrowIfNull(modelName);

        if (!this._registeredModels.Remove(modelName))
        {
            throw new InvalidOperationException($"Model '{modelName}' does not exist");
        }
    }

    /// <summary>
    /// Describes a model (returns its schema and configuration)
    /// </summary>
    /// <param name="modelName">Name of the model</param>
    /// <returns>Model description</returns>
    public ModelDescription DescribeModel(string modelName)
    {
        ArgumentNullException.ThrowIfNull(modelName);

        Model? model = this.GetModel(modelName) ?? throw new InvalidOperationException($"Model '{modelName}' does not exist");

        return new ModelDescription
        {
            ModelName = model.ModelName,
            Provider = model.Provider,
            InputSchema = new Dictionary<string, string>(model.InputSchema),
            OutputSchema = new Dictionary<string, string>(model.OutputSchema),
            Properties = new Dictionary<string, string>(model.Definition.Properties)
        };
    }

    /// <summary>
    /// Registers a table for SQL operations
    /// </summary>
    /// <param name="tableName">Name of the table</param>
    /// <param name="table">Table instance</param>
    public void RegisterTable(string tableName, Table table)
    {
        ArgumentNullException.ThrowIfNull(tableName);
        ArgumentNullException.ThrowIfNull(table);

        this._registeredTables[tableName] = table;
    }

    /// <summary>
    /// Gets a registered table by name
    /// </summary>
    /// <param name="tableName">Name of the table</param>
    /// <returns>Table instance</returns>
    public Table? GetTable(string tableName)
    {
        ArgumentNullException.ThrowIfNull(tableName);
        return this._registeredTables.GetValueOrDefault(tableName);
    }

    /// <summary>
    /// Lists all registered tables
    /// </summary>
    /// <returns>Collection of table names</returns>
    public IEnumerable<string> ListTables() => this._registeredTables.Keys;
}

/// <summary>
/// Describes a model's schema and configuration
/// </summary>
public class ModelDescription
{
    /// <summary>
    /// Model name
    /// </summary>
    public string ModelName { get; init; } = string.Empty;

    /// <summary>
    /// AI provider name
    /// </summary>
    public string Provider { get; init; } = string.Empty;

    /// <summary>
    /// Input schema (column name -> data type)
    /// </summary>
    public Dictionary<string, string> InputSchema { get; init; } = new();

    /// <summary>
    /// Output schema (column name -> data type)
    /// </summary>
    public Dictionary<string, string> OutputSchema { get; init; } = new();

    /// <summary>
    /// Provider-specific properties
    /// </summary>
    public Dictionary<string, string> Properties { get; init; } = new();
}

/// <summary>
/// Extension methods for Table API operations
/// </summary>
public static class TableEnvironmentExtensions
{
    private static readonly Dictionary<StreamExecutionEnvironment, TableEnvironment> _environments = new();

    /// <summary>
    /// Gets or creates a TableEnvironment for the execution environment
    /// </summary>
    /// <param name="env">Stream execution environment</param>
    /// <returns>TableEnvironment instance</returns>
    public static TableEnvironment GetTableEnvironment(this StreamExecutionEnvironment env)
    {
        ArgumentNullException.ThrowIfNull(env);

        if (!_environments.TryGetValue(env, out TableEnvironment? tableEnv))
        {
            tableEnv = new TableEnvironment(env);
            _environments[env] = tableEnv;
        }

        return tableEnv;
    }
}
