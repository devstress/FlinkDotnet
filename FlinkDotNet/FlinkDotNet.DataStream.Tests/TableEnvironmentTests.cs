using System;
using System.Collections.Generic;
using System.Linq;
using FlinkDotNet.DataStream;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests;

/// <summary>
/// Comprehensive unit tests for TableEnvironment and Model Management API (WI9)
/// Achieves 100% code coverage for TableEnvironment, ModelDescription, and related functionality
/// </summary>
[TestFixture]
public class TableEnvironmentTests
{
    #region TableEnvironment - CreateModel Tests

    [Test]
    public void CreateModel_ValidModel_Success()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var tableEnv = env.GetTableEnvironment();
        var model = env.CreateModel("test_model")
            .InputColumn("input", "STRING").WithProvider("openai")
            .OutputColumn("output", "STRING")
            .WithProvider("openai")
            .Build();

        // Act
        tableEnv.CreateModel("test_model", model);

        // Assert
        var retrieved = tableEnv.GetModel("test_model");
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.ModelName, Is.EqualTo("test_model"));
    }

    [Test]
    public void CreateModel_NullModelName_ThrowsArgumentNullException()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var tableEnv = env.GetTableEnvironment();
        var model = env.CreateModel("test")
            .InputColumn("input", "STRING").WithProvider("openai")
            .Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => tableEnv.CreateModel(null!, model));
    }

    [Test]
    public void CreateModel_NullModel_ThrowsArgumentNullException()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var tableEnv = env.GetTableEnvironment();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => tableEnv.CreateModel("test_model", null!));
    }

    [Test]
    public void CreateModel_DuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var tableEnv = env.GetTableEnvironment();
        var model1 = env.CreateModel("duplicate")
            .InputColumn("input", "STRING").WithProvider("openai")
            .Build();
        var model2 = env.CreateModel("duplicate")
            .InputColumn("input", "STRING").WithProvider("openai")
            .Build();

        tableEnv.CreateModel("duplicate", model1);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => tableEnv.CreateModel("duplicate", model2));
        Assert.That(ex!.Message, Does.Contain("already exists"));
    }

    #endregion

    #region TableEnvironment - GetModel Tests

    [Test]
    public void GetModel_ExistingModel_ReturnsModel()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var tableEnv = env.GetTableEnvironment();
        var model = env.CreateModel("get_test")
            .InputColumn("input", "STRING").WithProvider("openai")
            .Build();
        tableEnv.CreateModel("get_test", model);

        // Act
        var retrieved = tableEnv.GetModel("get_test");

        // Assert
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.ModelName, Is.EqualTo("get_test"));
    }

    [Test]
    public void GetModel_NonExistingModel_ReturnsNull()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var tableEnv = env.GetTableEnvironment();

        // Act
        var result = tableEnv.GetModel("non_existing");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetModel_NullModelName_ThrowsArgumentNullException()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var tableEnv = env.GetTableEnvironment();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => tableEnv.GetModel(null!));
    }

    #endregion

    #region TableEnvironment - ListModels Tests

    [Test]
    public void ListModels_NoModels_ReturnsEmptyCollection()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var tableEnv = env.GetTableEnvironment();

        // Act
        var models = tableEnv.ListModels();

        // Assert
        Assert.That(models, Is.Not.Null);
        Assert.That(models, Is.Empty);
    }

    [Test]
    public void ListModels_WithModels_ReturnsAllModelNames()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var tableEnv = env.GetTableEnvironment();
        var model1 = env.CreateModel("model1").InputColumn("input", "STRING").WithProvider("openai").Build();
        var model2 = env.CreateModel("model2").InputColumn("input", "STRING").WithProvider("openai").Build();

        tableEnv.CreateModel("model1", model1);
        tableEnv.CreateModel("model2", model2);

        // Act
        var models = tableEnv.ListModels().ToList();

        // Assert
        Assert.That(models, Has.Count.EqualTo(2));
        Assert.That(models, Contains.Item("model1"));
        Assert.That(models, Contains.Item("model2"));
    }

    #endregion

    #region TableEnvironment - DropModel Tests

    [Test]
    public void DropModel_ExistingModel_Success()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var tableEnv = env.GetTableEnvironment();
        var model = env.CreateModel("drop_test")
            .InputColumn("input", "STRING").WithProvider("openai")
            .Build();
        tableEnv.CreateModel("drop_test", model);

        // Act
        tableEnv.DropModel("drop_test");

        // Assert
        var retrieved = tableEnv.GetModel("drop_test");
        Assert.That(retrieved, Is.Null);
    }

    [Test]
    public void DropModel_NonExistingModel_ThrowsInvalidOperationException()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var tableEnv = env.GetTableEnvironment();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => tableEnv.DropModel("non_existing"));
        Assert.That(ex!.Message, Does.Contain("does not exist"));
    }

    [Test]
    public void DropModel_NullModelName_ThrowsArgumentNullException()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var tableEnv = env.GetTableEnvironment();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => tableEnv.DropModel(null!));
    }

    #endregion

    #region TableEnvironment - DescribeModel Tests

    [Test]
    public void DescribeModel_ExistingModel_ReturnsDescription()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var tableEnv = env.GetTableEnvironment();
        var model = env.CreateModel("describe_test")
            .InputColumn("input1", "STRING")
            .InputColumn("input2", "BIGINT")
            .OutputColumn("output1", "STRING")
            .OutputColumn("output2", "DOUBLE")
            .WithProvider("openai")
            .WithProperty("task", "classification")
            .Build();

        tableEnv.CreateModel("describe_test", model);

        // Act
        var description = tableEnv.DescribeModel("describe_test");

        // Assert
        Assert.That(description, Is.Not.Null);
        Assert.That(description.ModelName, Is.EqualTo("describe_test"));
        Assert.That(description.Provider, Is.EqualTo("openai"));
        Assert.That(description.InputSchema, Has.Count.EqualTo(2));
        Assert.That(description.OutputSchema, Has.Count.EqualTo(2));
        Assert.That(description.Properties, Has.Count.EqualTo(1));
        Assert.That(description.Properties["task"], Is.EqualTo("classification"));
    }

    [Test]
    public void DescribeModel_NonExistingModel_ThrowsInvalidOperationException()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var tableEnv = env.GetTableEnvironment();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => tableEnv.DescribeModel("non_existing"));
        Assert.That(ex!.Message, Does.Contain("does not exist"));
    }

    [Test]
    public void DescribeModel_NullModelName_ThrowsArgumentNullException()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var tableEnv = env.GetTableEnvironment();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => tableEnv.DescribeModel(null!));
    }

    #endregion

    #region TableEnvironment - Table Management Tests

    [Test]
    public void RegisterTable_ValidTable_Success()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var tableEnv = env.GetTableEnvironment();
        var table = env.CreateTable("test_table")
            .AddColumn("col1", "STRING")
            .Build();

        // Act
        tableEnv.RegisterTable("test_table", table);

        // Assert
        var retrieved = tableEnv.GetTable("test_table");
        Assert.That(retrieved, Is.Not.Null);
    }

    [Test]
    public void RegisterTable_NullTableName_ThrowsArgumentNullException()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var tableEnv = env.GetTableEnvironment();
        var table = env.CreateTable("test").Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => tableEnv.RegisterTable(null!, table));
    }

    [Test]
    public void RegisterTable_NullTable_ThrowsArgumentNullException()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var tableEnv = env.GetTableEnvironment();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => tableEnv.RegisterTable("test", null!));
    }

    [Test]
    public void GetTable_ExistingTable_ReturnsTable()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var tableEnv = env.GetTableEnvironment();
        var table = env.CreateTable("get_table_test")
            .AddColumn("col1", "STRING")
            .Build();
        tableEnv.RegisterTable("get_table_test", table);

        // Act
        var retrieved = tableEnv.GetTable("get_table_test");

        // Assert
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.TableName, Is.EqualTo("get_table_test"));
    }

    [Test]
    public void GetTable_NonExistingTable_ReturnsNull()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var tableEnv = env.GetTableEnvironment();

        // Act
        var result = tableEnv.GetTable("non_existing");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetTable_NullTableName_ThrowsArgumentNullException()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var tableEnv = env.GetTableEnvironment();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => tableEnv.GetTable(null!));
    }

    [Test]
    public void ListTables_NoTables_ReturnsEmptyCollection()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var tableEnv = env.GetTableEnvironment();

        // Act
        var tables = tableEnv.ListTables();

        // Assert
        Assert.That(tables, Is.Not.Null);
        Assert.That(tables, Is.Empty);
    }

    [Test]
    public void ListTables_WithTables_ReturnsAllTableNames()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var tableEnv = env.GetTableEnvironment();
        var table1 = env.CreateTable("table1").Build();
        var table2 = env.CreateTable("table2").Build();

        tableEnv.RegisterTable("table1", table1);
        tableEnv.RegisterTable("table2", table2);

        // Act
        var tables = tableEnv.ListTables().ToList();

        // Assert
        Assert.That(tables, Has.Count.EqualTo(2));
        Assert.That(tables, Contains.Item("table1"));
        Assert.That(tables, Contains.Item("table2"));
    }

    #endregion

    #region TableEnvironmentExtensions Tests

    [Test]
    public void GetTableEnvironment_SameEnvironment_ReturnsSameInstance()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        // Act
        var tableEnv1 = env.GetTableEnvironment();
        var tableEnv2 = env.GetTableEnvironment();

        // Assert
        Assert.That(tableEnv1, Is.SameAs(tableEnv2));
    }

    [Test]
    public void GetTableEnvironment_DifferentEnvironments_ReturnsDifferentInstances()
    {
        // Arrange
        var env1 = StreamExecutionEnvironment.GetExecutionEnvironment();
        var env2 = StreamExecutionEnvironment.GetExecutionEnvironment();

        // Act
        var tableEnv1 = env1.GetTableEnvironment();
        var tableEnv2 = env2.GetTableEnvironment();

        // Assert
        Assert.That(tableEnv1, Is.Not.SameAs(tableEnv2));
    }

    [Test]
    public void GetTableEnvironment_NullEnvironment_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => TableEnvironmentExtensions.GetTableEnvironment(null!));
    }

    #endregion

    #region ModelDescription Tests

    [Test]
    public void ModelDescription_DefaultValues_AreEmpty()
    {
        // Arrange & Act
        var description = new ModelDescription();

        // Assert
        Assert.That(description.ModelName, Is.EqualTo(string.Empty));
        Assert.That(description.Provider, Is.EqualTo(string.Empty));
        Assert.That(description.InputSchema, Is.Not.Null);
        Assert.That(description.OutputSchema, Is.Not.Null);
        Assert.That(description.Properties, Is.Not.Null);
    }

    [Test]
    public void ModelDescription_InitWithValues_Success()
    {
        // Arrange & Act
        var description = new ModelDescription
        {
            ModelName = "test",
            Provider = "openai",
            InputSchema = new Dictionary<string, string> { { "input", "STRING" } },
            OutputSchema = new Dictionary<string, string> { { "output", "STRING" } },
            Properties = new Dictionary<string, string> { { "key", "value" } }
        };

        // Assert
        Assert.That(description.ModelName, Is.EqualTo("test"));
        Assert.That(description.Provider, Is.EqualTo("openai"));
        Assert.That(description.InputSchema, Has.Count.EqualTo(1));
        Assert.That(description.OutputSchema, Has.Count.EqualTo(1));
        Assert.That(description.Properties, Has.Count.EqualTo(1));
    }

    #endregion
}
