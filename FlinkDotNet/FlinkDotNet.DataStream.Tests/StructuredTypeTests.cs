using System;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Tests for StructuredType, StructuredTypeField, and DataTypes utility classes
    /// to achieve coverage for complex type definitions.
    /// </summary>
    [TestFixture]
    public class StructuredTypeTests
    {
        [SetUp]
        public void Setup()
        {
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", "http://localhost:8080");
        }

        [TearDown]
        public void TearDown()
        {
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", null);
        }

        #region StructuredType Tests

        [Test]
        public void StructuredType_NewBuilder_WithValidName_ShouldReturnBuilder()
        {
            // Act
            var builder = StructuredType.NewBuilder("PersonType");

            // Assert
            Assert.That(builder, Is.Not.Null);
        }

        [Test]
        public void StructuredType_NewBuilder_WithNullName_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => StructuredType.NewBuilder(null!));
        }

        [Test]
        public void StructuredType_NewBuilder_WithEmptyName_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => StructuredType.NewBuilder(""));
        }

        [Test]
        public void StructuredType_NewBuilder_WithWhitespaceName_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => StructuredType.NewBuilder("   "));
        }

        [Test]
        public void StructuredType_ToSql_WithSimpleFields_ShouldGenerateSQL()
        {
            // Arrange
            var type = StructuredType.NewBuilder("PersonType")
                .Field("id", "BIGINT")
                .Field("name", "STRING")
                .Build();

            // Act
            var sql = type.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("CREATE TYPE PersonType AS ROW("));
            Assert.That(sql, Does.Contain("id BIGINT"));
            Assert.That(sql, Does.Contain("name STRING"));
        }

        [Test]
        public void StructuredType_Properties_ShouldReturnCorrectValues()
        {
            // Arrange
            var type = StructuredType.NewBuilder("TestType")
                .Field("field1", "STRING")
                .Build();

            // Assert
            Assert.That(type.TypeName, Is.EqualTo("TestType"));
            Assert.That(type.Fields, Is.Not.Null);
            Assert.That(type.Fields.Count, Is.EqualTo(1));
        }

        #endregion

        #region StructuredTypeBuilder Tests

        [Test]
        public void StructuredTypeBuilder_Field_WithStringDataType_ShouldAddField()
        {
            // Arrange
            var builder = StructuredType.NewBuilder("TestType");

            // Act
            var result = builder.Field("testField", "STRING");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(builder)); // Fluent API
        }

        [Test]
        public void StructuredTypeBuilder_Field_WithNullFieldName_ShouldThrow()
        {
            // Arrange
            var builder = StructuredType.NewBuilder("TestType");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.Field(null!, "STRING"));
        }

        [Test]
        public void StructuredTypeBuilder_Field_WithEmptyFieldName_ShouldThrow()
        {
            // Arrange
            var builder = StructuredType.NewBuilder("TestType");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.Field("", "STRING"));
        }

        [Test]
        public void StructuredTypeBuilder_Field_WithWhitespaceFieldName_ShouldThrow()
        {
            // Arrange
            var builder = StructuredType.NewBuilder("TestType");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.Field("   ", "STRING"));
        }

        [Test]
        public void StructuredTypeBuilder_Field_WithNullDataType_ShouldThrow()
        {
            // Arrange
            var builder = StructuredType.NewBuilder("TestType");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.Field("field", (string) null!));
        }

        [Test]
        public void StructuredTypeBuilder_Field_WithEmptyDataType_ShouldThrow()
        {
            // Arrange
            var builder = StructuredType.NewBuilder("TestType");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.Field("field", ""));
        }

        [Test]
        public void StructuredTypeBuilder_Field_WithNestedType_ShouldAddField()
        {
            // Arrange
            var nestedType = StructuredType.NewBuilder("AddressType")
                .Field("street", "STRING")
                .Field("city", "STRING")
                .Build();
            var builder = StructuredType.NewBuilder("PersonType");

            // Act
            var result = builder.Field("address", nestedType);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(builder));
        }

        [Test]
        public void StructuredTypeBuilder_Field_WithNullNestedType_ShouldThrow()
        {
            // Arrange
            var builder = StructuredType.NewBuilder("TestType");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => builder.Field("field", (StructuredType) null!));
        }

        [Test]
        public void StructuredTypeBuilder_Build_WithFields_ShouldSucceed()
        {
            // Arrange
            var builder = StructuredType.NewBuilder("TestType")
                .Field("field1", "STRING")
                .Field("field2", "BIGINT");

            // Act
            var type = builder.Build();

            // Assert
            Assert.That(type, Is.Not.Null);
            Assert.That(type.TypeName, Is.EqualTo("TestType"));
            Assert.That(type.Fields.Count, Is.EqualTo(2));
        }

        [Test]
        public void StructuredTypeBuilder_Build_WithoutFields_ShouldThrow()
        {
            // Arrange
            var builder = StructuredType.NewBuilder("TestType");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => builder.Build());
        }

        #endregion

        #region StructuredTypeField Tests

        [Test]
        public void StructuredTypeField_Properties_ShouldReturnCorrectValues()
        {
            // Arrange
            var type = StructuredType.NewBuilder("TestType")
                .Field("testField", "STRING")
                .Build();

            // Act
            var field = type.Fields[0];

            // Assert
            Assert.That(field.FieldName, Is.EqualTo("testField"));
            Assert.That(field.DataType, Is.EqualTo("STRING"));
        }

        #endregion

        #region DataTypes Tests

        [Test]
        public void DataTypes_String_ShouldReturnStringType()
        {
            // Act
            var type = DataTypes.String;

            // Assert
            Assert.That(type, Is.EqualTo("STRING"));
        }

        [Test]
        public void DataTypes_Boolean_ShouldReturnBooleanType()
        {
            // Act
            var type = DataTypes.Boolean;

            // Assert
            Assert.That(type, Is.EqualTo("BOOLEAN"));
        }

        [Test]
        public void DataTypes_Int_ShouldReturnIntType()
        {
            // Act
            var type = DataTypes.Int;

            // Assert
            Assert.That(type, Is.EqualTo("INT"));
        }

        [Test]
        public void DataTypes_BigInt_ShouldReturnBigIntType()
        {
            // Act
            var type = DataTypes.BigInt;

            // Assert
            Assert.That(type, Is.EqualTo("BIGINT"));
        }

        [Test]
        public void DataTypes_Double_ShouldReturnDoubleType()
        {
            // Act
            var type = DataTypes.Double;

            // Assert
            Assert.That(type, Is.EqualTo("DOUBLE"));
        }

        [Test]
        public void DataTypes_Timestamp_DefaultPrecision_ShouldReturnTimestampType()
        {
            // Act
            var type = DataTypes.Timestamp();

            // Assert
            Assert.That(type, Is.EqualTo("TIMESTAMP(3)"));
        }

        [Test]
        public void DataTypes_Timestamp_CustomPrecision_ShouldReturnTimestampType()
        {
            // Act
            var type = DataTypes.Timestamp(6);

            // Assert
            Assert.That(type, Is.EqualTo("TIMESTAMP(6)"));
        }

        [Test]
        public void DataTypes_Array_ShouldReturnArrayType()
        {
            // Act
            var type = DataTypes.Array("STRING");

            // Assert
            Assert.That(type, Is.EqualTo("ARRAY<STRING>"));
        }

        [Test]
        public void DataTypes_Map_ShouldReturnMapType()
        {
            // Act
            var type = DataTypes.Map("STRING", "BIGINT");

            // Assert
            Assert.That(type, Is.EqualTo("MAP<STRING, BIGINT>"));
        }

        [Test]
        public void DataTypes_Variant_ShouldReturnVariantType()
        {
            // Act
            var type = DataTypes.Variant;

            // Assert
            Assert.That(type, Is.EqualTo("VARIANT"));
        }

        #endregion

        #region Integration Tests

        [Test]
        public void StructuredType_CompleteWorkflow_WithNestedTypes_ShouldWork()
        {
            // Arrange
            var addressType = StructuredType.NewBuilder("AddressType")
                .Field("street", DataTypes.String)
                .Field("city", DataTypes.String)
                .Field("zipCode", DataTypes.String)
                .Build();

            var personType = StructuredType.NewBuilder("PersonType")
                .Field("id", DataTypes.BigInt)
                .Field("name", DataTypes.String)
                .Field("age", DataTypes.Int)
                .Field("isActive", DataTypes.Boolean)
                .Field("score", DataTypes.Double)
                .Field("address", addressType)
                .Build();

            // Act
            var sql = personType.ToSql();

            // Assert
            Assert.That(personType.TypeName, Is.EqualTo("PersonType"));
            Assert.That(personType.Fields.Count, Is.EqualTo(6));
            Assert.That(sql, Does.Contain("CREATE TYPE PersonType AS ROW("));
            Assert.That(sql, Does.Contain("id BIGINT"));
            Assert.That(sql, Does.Contain("name STRING"));
            Assert.That(sql, Does.Contain("address AddressType"));
        }

        [Test]
        public void StructuredType_WithComplexDataTypes_ShouldWork()
        {
            // Arrange
            var type = StructuredType.NewBuilder("ComplexType")
                .Field("tags", DataTypes.Array(DataTypes.String))
                .Field("metadata", DataTypes.Map(DataTypes.String, DataTypes.String))
                .Field("timestamp", DataTypes.Timestamp(6))
                .Field("data", DataTypes.Variant)
                .Build();

            // Act
            var sql = type.ToSql();

            // Assert
            Assert.That(sql, Does.Contain("tags ARRAY<STRING>"));
            Assert.That(sql, Does.Contain("metadata MAP<STRING, STRING>"));
            Assert.That(sql, Does.Contain("timestamp TIMESTAMP(6)"));
            Assert.That(sql, Does.Contain("data VARIANT"));
        }

        #endregion
    }
}
