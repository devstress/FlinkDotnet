using System;
using System.Collections.Generic;
using System.Linq;

namespace FlinkDotNet.DataStream;

/// <summary>
/// Represents a structured (ROW) type definition in Flink's type system.
/// Structured types allow defining complex nested data structures with named fields.
/// </summary>
public class StructuredType
{
    /// <summary>
    /// Gets the name of the structured type
    /// </summary>
    public string TypeName { get; }

    /// <summary>
    /// Gets the fields of the structured type
    /// </summary>
    public IReadOnlyList<StructuredTypeField> Fields { get; }

    private StructuredType(string typeName, List<StructuredTypeField> fields)
    {
        this.TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
        this.Fields = fields.AsReadOnly();
    }

    /// <summary>
    /// Creates a new builder for constructing a structured type
    /// </summary>
    /// <param name="typeName">Name of the structured type</param>
    /// <returns>Builder instance</returns>
    public static StructuredTypeBuilder NewBuilder(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            throw new ArgumentException("Type name cannot be null or empty", nameof(typeName));
        }

        return new StructuredTypeBuilder(typeName);
    }

    /// <summary>
    /// Generates the SQL DDL for creating this structured type
    /// </summary>
    /// <returns>CREATE TYPE SQL statement</returns>
    public string ToSql()
    {
        var fieldDefinitions = this.Fields.Select(f => $"  {f.FieldName} {f.DataType}");
        return $"CREATE TYPE {this.TypeName} AS ROW(\n{string.Join(",\n", fieldDefinitions)}\n)";
    }

    /// <summary>
    /// Builder for constructing structured types
    /// </summary>
    public class StructuredTypeBuilder
    {
        private readonly string _typeName;
        private readonly List<StructuredTypeField> _fields = new();

        internal StructuredTypeBuilder(string typeName)
        {
            this._typeName = typeName;
        }

        /// <summary>
        /// Adds a field to the structured type
        /// </summary>
        /// <param name="fieldName">Name of the field</param>
        /// <param name="dataType">Data type of the field (e.g., "STRING", "BIGINT", or another StructuredType)</param>
        /// <returns>This builder for method chaining</returns>
        public StructuredTypeBuilder Field(string fieldName, string dataType)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                throw new ArgumentException("Field name cannot be null or empty", nameof(fieldName));
            }

            if (string.IsNullOrWhiteSpace(dataType))
            {
                throw new ArgumentException("Data type cannot be null or empty", nameof(dataType));
            }

            this._fields.Add(new StructuredTypeField(fieldName, dataType));
            return this;
        }

        /// <summary>
        /// Adds a field with a nested structured type
        /// </summary>
        /// <param name="fieldName">Name of the field</param>
        /// <param name="structuredType">Nested structured type</param>
        /// <returns>This builder for method chaining</returns>
        public StructuredTypeBuilder Field(string fieldName, StructuredType structuredType)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                throw new ArgumentException("Field name cannot be null or empty", nameof(fieldName));
            }

            if (structuredType == null)
            {
                throw new ArgumentNullException(nameof(structuredType));
            }

            this._fields.Add(new StructuredTypeField(fieldName, structuredType.TypeName));
            return this;
        }

        /// <summary>
        /// Builds the structured type
        /// </summary>
        /// <returns>The constructed structured type</returns>
        public StructuredType Build()
        {
            if (this._fields.Count == 0)
            {
                throw new InvalidOperationException("Structured type must have at least one field");
            }

            return new StructuredType(this._typeName, this._fields);
        }
    }
}

/// <summary>
/// Represents a field in a structured type
/// </summary>
public class StructuredTypeField
{
    /// <summary>
    /// Gets the field name
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    /// Gets the field data type
    /// </summary>
    public string DataType { get; }

    internal StructuredTypeField(string fieldName, string dataType)
    {
        this.FieldName = fieldName;
        this.DataType = dataType;
    }
}

/// <summary>
/// Common Flink data types for use with structured types
/// </summary>
public static class DataTypes
{
    /// <summary>
    /// STRING data type
    /// </summary>
    public static string String() => "STRING";

    /// <summary>
    /// BOOLEAN data type
    /// </summary>
    public static string Boolean() => "BOOLEAN";

    /// <summary>
    /// INT data type
    /// </summary>
    public static string Int() => "INT";

    /// <summary>
    /// BIGINT data type
    /// </summary>
    public static string BigInt() => "BIGINT";

    /// <summary>
    /// DOUBLE data type
    /// </summary>
    public static string Double() => "DOUBLE";

    /// <summary>
    /// TIMESTAMP data type with precision
    /// </summary>
    /// <param name="precision">Precision (0-9)</param>
    public static string Timestamp(int precision = 3) => $"TIMESTAMP({precision})";

    /// <summary>
    /// ARRAY data type
    /// </summary>
    /// <param name="elementType">Element type</param>
    public static string Array(string elementType) => $"ARRAY<{elementType}>";

    /// <summary>
    /// MAP data type
    /// </summary>
    /// <param name="keyType">Key type</param>
    /// <param name="valueType">Value type</param>
    public static string Map(string keyType, string valueType) => $"MAP<{keyType}, {valueType}>";

    /// <summary>
    /// VARIANT data type for semi-structured JSON data
    /// </summary>
    public static string Variant() => "VARIANT";
}
