using System;
using System.Collections.Generic;
using Flink.JobBuilder.Models;

namespace FlinkDotNet.DataStream;

/// <summary>
/// Builder for creating AI/ML model definitions with fluent API
/// </summary>
public class ModelBuilder
{
    private readonly string _modelName;
    private readonly Dictionary<string, string> _inputSchema = [];
    private readonly Dictionary<string, string> _outputSchema = [];
    private readonly Dictionary<string, string> _properties = [];
    private string _provider = string.Empty;
    private string _executionMode = "gateway";

    /// <summary>
    /// Creates a new ModelBuilder
    /// </summary>
    /// <param name="modelName">Name of the model to create</param>
    /// <exception cref="ArgumentException">Thrown when modelName is null or empty</exception>
    public ModelBuilder(string modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName))
        {
            throw new ArgumentException("Model name cannot be null or empty", nameof(modelName));
        }

        this._modelName = modelName;
    }

    /// <summary>
    /// Add an input column to the model schema
    /// </summary>
    /// <param name="columnName">Name of the input column</param>
    /// <param name="dataType">Flink data type (e.g., "STRING", "BIGINT", "DOUBLE")</param>
    /// <returns>This builder for method chaining</returns>
    public ModelBuilder InputColumn(string columnName, string dataType)
    {
        this._inputSchema[columnName] = dataType;
        return this;
    }

    /// <summary>
    /// Add multiple input columns to the model schema
    /// </summary>
    /// <param name="columns">Dictionary of column names to data types</param>
    /// <returns>This builder for method chaining</returns>
    public ModelBuilder InputColumns(Dictionary<string, string> columns)
    {
        foreach (KeyValuePair<string, string> column in columns)
        {
            this._inputSchema[column.Key] = column.Value;
        }
        return this;
    }

    /// <summary>
    /// Add an output column to the model schema
    /// </summary>
    /// <param name="columnName">Name of the output column</param>
    /// <param name="dataType">Flink data type (e.g., "STRING", "DOUBLE")</param>
    /// <returns>This builder for method chaining</returns>
    public ModelBuilder OutputColumn(string columnName, string dataType)
    {
        this._outputSchema[columnName] = dataType;
        return this;
    }

    /// <summary>
    /// Add multiple output columns to the model schema
    /// </summary>
    /// <param name="columns">Dictionary of column names to data types</param>
    /// <returns>This builder for method chaining</returns>
    public ModelBuilder OutputColumns(Dictionary<string, string> columns)
    {
        foreach (KeyValuePair<string, string> column in columns)
        {
            this._outputSchema[column.Key] = column.Value;
        }
        return this;
    }

    /// <summary>
    /// Set the AI provider for the model
    /// </summary>
    /// <param name="provider">Provider name (e.g., "openai", "azure_openai", "custom")</param>
    /// <returns>This builder for method chaining</returns>
    public ModelBuilder WithProvider(string provider)
    {
        this._provider = provider;
        return this;
    }

    /// <summary>
    /// Add a property to the model configuration
    /// </summary>
    /// <param name="key">Property key</param>
    /// <param name="value">Property value</param>
    /// <returns>This builder for method chaining</returns>
    public ModelBuilder WithProperty(string key, string value)
    {
        this._properties[key] = value;
        return this;
    }

    /// <summary>
    /// Add multiple properties to the model configuration
    /// </summary>
    /// <param name="properties">Dictionary of properties</param>
    /// <returns>This builder for method chaining</returns>
    public ModelBuilder WithProperties(Dictionary<string, string> properties)
    {
        foreach (KeyValuePair<string, string> prop in properties)
        {
            this._properties[prop.Key] = prop.Value;
        }
        return this;
    }

    /// <summary>
    /// Set the execution mode for the model
    /// </summary>
    /// <param name="executionMode">Execution mode (default: "gateway")</param>
    /// <returns>This builder for method chaining</returns>
    public ModelBuilder WithExecutionMode(string executionMode)
    {
        this._executionMode = executionMode;
        return this;
    }

    /// <summary>
    /// Build the Model object
    /// </summary>
    /// <returns>A new Model instance</returns>
    /// <exception cref="InvalidOperationException">Thrown when required fields are missing</exception>
    public Model Build()
    {
        // Validation
        if (this._inputSchema.Count == 0 && this._outputSchema.Count == 0)
        {
            throw new InvalidOperationException("Model must have at least input or output schema defined");
        }

        if (string.IsNullOrWhiteSpace(this._provider))
        {
            throw new InvalidOperationException("Provider must be specified");
        }

        ModelDefinition definition = new()
        {
            ModelName = this._modelName,
            InputSchema = new Dictionary<string, string>(this._inputSchema),
            OutputSchema = new Dictionary<string, string>(this._outputSchema),
            Provider = this._provider,
            Properties = new Dictionary<string, string>(this._properties),
            Operation = "CREATE",
            ExecutionMode = this._executionMode
        };

        return new Model(definition);
    }
}
