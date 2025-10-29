using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Flink.JobBuilder.Models;

namespace FlinkDotNet.DataStream;

/// <summary>
/// Represents an AI/ML model in Apache Flink 2.1+
/// Provides CREATE MODEL DDL support for AI/ML integration
/// </summary>
public class Model
{
    private readonly ModelDefinition _definition;

    /// <summary>
    /// Creates a new Model instance with the given definition
    /// </summary>
    /// <param name="definition">The model definition</param>
    public Model(ModelDefinition definition) => this._definition = definition;

    /// <summary>
    /// Gets the model name
    /// </summary>
    public string ModelName => this._definition.ModelName;

    /// <summary>
    /// Gets the AI provider (e.g., "openai", "azure_openai", "custom")
    /// </summary>
    public string Provider => this._definition.Provider;

    /// <summary>
    /// Gets the input schema
    /// </summary>
    public IReadOnlyDictionary<string, string> InputSchema => this._definition.InputSchema;

    /// <summary>
    /// Gets the output schema
    /// </summary>
    public IReadOnlyDictionary<string, string> OutputSchema => this._definition.OutputSchema;

    /// <summary>
    /// Gets the underlying IR definition
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "RCS1085:Use auto-implemented property", Justification = "Property returns value from private readonly field")]
    public ModelDefinition Definition => this._definition;

    /// <summary>
    /// Generate CREATE MODEL DDL statement
    /// </summary>
    /// <returns>SQL DDL string</returns>
    public string ToSql()
    {
        if (this._definition.Operation != "CREATE")
        {
            // Handle ALTER, DROP, etc. (simple operations)
            return this._definition.Operation switch
            {
                "DROP" => $"DROP MODEL {this.ModelName}",
                "SHOW" => "SHOW MODELS",
                "DESCRIBE" => $"DESCRIBE MODEL {this.ModelName}",
                _ => throw new InvalidOperationException($"Unsupported operation: {this._definition.Operation}")
            };
        }

        // Build CREATE MODEL statement
        StringBuilder sql = new();
        sql.AppendLine($"CREATE MODEL {this.ModelName} (");

        // Input schema
        if (this._definition.InputSchema.Count > 0)
        {
            sql.Append("  INPUT (")
               .Append(string.Join(", ", this._definition.InputSchema.Select(kvp => $"{kvp.Key} {kvp.Value}")))
               .AppendLine(")");
        }

        // Output schema
        if (this._definition.OutputSchema.Count > 0)
        {
            sql.Append("  OUTPUT (")
               .Append(string.Join(", ", this._definition.OutputSchema.Select(kvp => $"{kvp.Key} {kvp.Value}")))
               .AppendLine(")");
        }

        sql.AppendLine(")");

        // WITH clause for properties
        if (this._definition.Properties.Count > 0 || !string.IsNullOrEmpty(this._definition.Provider))
        {
            sql.AppendLine("WITH (");

            List<string> properties = [];

            // Add provider property first
            if (!string.IsNullOrEmpty(this._definition.Provider))
            {
                properties.Add($"  'provider' = '{this._definition.Provider}'");
            }

            // Add other properties
            foreach (KeyValuePair<string, string> prop in this._definition.Properties)
            {
                properties.Add($"  '{prop.Key}' = '{prop.Value}'");
            }

            sql.Append(string.Join(",\n", properties))
               .AppendLine()
               .Append(")");
        }

        return sql.ToString();
    }
}

/// <summary>
/// Extension methods for creating AI/ML models
/// </summary>
public static class ModelExtensions
{
    /// <summary>
    /// Creates a new AI/ML model builder
    /// </summary>
    /// <param name="env">Stream execution environment</param>
    /// <param name="modelName">Name of the model</param>
    /// <returns>A new ModelBuilder</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Extension method pattern requires this parameter")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "RCS1175:Unused 'this' parameter", Justification = "Extension method pattern requires this parameter")]
    public static ModelBuilder CreateModel(
        this StreamExecutionEnvironment env,
        string modelName) => new(modelName);
}
