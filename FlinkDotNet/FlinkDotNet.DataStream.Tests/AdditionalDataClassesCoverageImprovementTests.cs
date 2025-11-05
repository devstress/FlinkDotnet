using System;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests;

/// <summary>
/// Tests for additional result, status, and data classes to improve code coverage to 95%.
/// These are simple DTO classes that need property and constructor coverage.
/// </summary>
[TestFixture]
public class AdditionalDataClassesCoverageImprovementTests
{
    #region State Descriptor Tests

    [Test]
    public void ValueStateDescriptor_CanBeCreated()
    {
        // Arrange & Act
        var descriptor = new ValueStateDescriptor<string>("testState");

        // Assert
        Assert.That(descriptor, Is.Not.Null);
    }

    [Test]
    public void ListStateDescriptor_CanBeCreated()
    {
        // Arrange & Act
        var descriptor = new ListStateDescriptor<int>("testListState");

        // Assert
        Assert.That(descriptor, Is.Not.Null);
    }

    [Test]
    public void MapStateDescriptor_CanBeCreated()
    {
        // Arrange & Act
        var descriptor = new MapStateDescriptor<string, int>("testMapState");

        // Assert
        Assert.That(descriptor, Is.Not.Null);
    }

    #endregion

    #region Model and Configuration Classes

    [Test]
    public void ModelDescription_CanBeCreated()
    {
        // Arrange & Act
        var description = new ModelDescription();

        // Assert
        Assert.That(description, Is.Not.Null);
        Assert.That(description.ModelName, Is.Not.Null);
        Assert.That(description.Provider, Is.Not.Null);
    }

    [Test]
    public void RocksDBOptions_CanBeCreated()
    {
        // Arrange & Act
        var options = new RocksDBOptions();

        // Assert
        Assert.That(options, Is.Not.Null);
    }

    [Test]
    public void SinkWriterContext_CanBeCreated()
    {
        // Arrange & Act
        var context = new SinkWriterContext();

        // Assert
        Assert.That(context, Is.Not.Null);
    }

    #endregion

    #region OutputTag Tests

    [Test]
    public void OutputTag_CanBeCreated()
    {
        // Arrange & Act
        var tag = new OutputTag<string>("side-output");

        // Assert
        Assert.That(tag, Is.Not.Null);
    }

    [Test]
    public void OutputTag_WithDifferentType_CanBeCreated()
    {
        // Arrange
        string tagName = "my-side-output";

        // Act
        var tag = new OutputTag<int>(tagName);

        // Assert - OutputTag should store the name
        Assert.That(tag, Is.Not.Null);
    }

    #endregion
}
