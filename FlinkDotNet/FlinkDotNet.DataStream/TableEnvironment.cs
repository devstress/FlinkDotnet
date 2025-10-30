using System;
using System.Collections.Generic;

namespace FlinkDotNet.DataStream;

/// <summary>
/// Table environment for managing tables and models in Apache Flink
/// Provides programmatic API for model management (complementing SQL DDL)
/// </summary>
public class TableEnvironment
{
    private readonly Dictionary<string, Model> _registeredModels = new();
    private readonly Dictionary<string, Table> _registeredTables = new();

    internal TableEnvironment(StreamExecutionEnvironment env) =>
        // Store reference for future use
        _ = env;

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
