using System;
using FlinkDotNet.DataStream;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests;

/// <summary>
/// Tests to cover constructor null check branches in StateDescriptor and OutputTag classes
/// These are simple 2-branch tests (null vs non-null) for each constructor
/// </summary>
[TestFixture]
public class StateDescriptorAndOutputTagConstructorTests
{
    #region StateDescriptor Constructor Tests

    [Test]
    public void StateDescriptor_WithValidName_CreatesInstance()
    {
        // Arrange & Act
        var descriptor = new ValueStateDescriptor<string>("test-state");

        // Assert
        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor.Name, Is.EqualTo("test-state"));
    }

    [Test]
    public void StateDescriptor_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ValueStateDescriptor<string>(null!));
    }

    #endregion

    #region ValueStateDescriptor Constructor Tests

    [Test]
    public void ValueStateDescriptor_WithValidName_CreatesInstance()
    {
        // Arrange & Act
        var descriptor = new ValueStateDescriptor<int>("value-state");

        // Assert
        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor.Name, Is.EqualTo("value-state"));
        Assert.That(descriptor.ValueType, Is.EqualTo(typeof(int)));
    }

    [Test]
    public void ValueStateDescriptor_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ValueStateDescriptor<int>(null!));
    }

    #endregion

    #region ListStateDescriptor Constructor Tests

    [Test]
    public void ListStateDescriptor_WithValidName_CreatesInstance()
    {
        // Arrange & Act
        var descriptor = new ListStateDescriptor<string>("list-state");

        // Assert
        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor.Name, Is.EqualTo("list-state"));
        Assert.That(descriptor.ElementType, Is.EqualTo(typeof(string)));
    }

    [Test]
    public void ListStateDescriptor_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ListStateDescriptor<string>(null!));
    }

    #endregion

    #region MapStateDescriptor Constructor Tests

    [Test]
    public void MapStateDescriptor_WithValidName_CreatesInstance()
    {
        // Arrange & Act
        var descriptor = new MapStateDescriptor<string, int>("map-state");

        // Assert
        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor.Name, Is.EqualTo("map-state"));
        Assert.That(descriptor.KeyType, Is.EqualTo(typeof(string)));
        Assert.That(descriptor.ValueType, Is.EqualTo(typeof(int)));
    }

    [Test]
    public void MapStateDescriptor_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MapStateDescriptor<string, int>(null!));
    }

    #endregion

    #region ReducingStateDescriptor Constructor Tests

    [Test]
    public void ReducingStateDescriptor_WithValidName_CreatesInstance()
    {
        // Arrange
        var reduceFunc = new TestReduceFunction();

        // Act
        var descriptor = new ReducingStateDescriptor<int>("reducing-state", reduceFunc);

        // Assert
        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor.Name, Is.EqualTo("reducing-state"));
        Assert.That(descriptor.ReduceFunction, Is.EqualTo(reduceFunc));
    }

    [Test]
    public void ReducingStateDescriptor_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange
        var reduceFunc = new TestReduceFunction();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ReducingStateDescriptor<int>(null!, reduceFunc));
    }

    [Test]
    public void ReducingStateDescriptor_WithNullReduceFunction_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ReducingStateDescriptor<int>("reducing-state", null!));
    }

    #endregion

    #region AggregatingStateDescriptor Constructor Tests

    [Test]
    public void AggregatingStateDescriptor_WithValidName_CreatesInstance()
    {
        // Arrange
        var aggregateFunc = new TestAggregateFunction();

        // Act
        var descriptor = new AggregatingStateDescriptor<int, int, int>("aggregating-state", aggregateFunc);

        // Assert
        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor.Name, Is.EqualTo("aggregating-state"));
        Assert.That(descriptor.AggregateFunction, Is.EqualTo(aggregateFunc));
    }

    [Test]
    public void AggregatingStateDescriptor_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange
        var aggregateFunc = new TestAggregateFunction();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AggregatingStateDescriptor<int, int, int>(null!, aggregateFunc));
    }

    [Test]
    public void AggregatingStateDescriptor_WithNullAggregateFunction_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AggregatingStateDescriptor<int, int, int>("aggregating-state", null!));
    }

    #endregion

    #region OutputTag Constructor Tests

    [Test]
    public void OutputTag_WithValidId_CreatesInstance()
    {
        // Arrange & Act
        var outputTag = new OutputTag<string>("side-output");

        // Assert
        Assert.That(outputTag, Is.Not.Null);
        Assert.That(outputTag.Id, Is.EqualTo("side-output"));
    }

    [Test]
    public void OutputTag_WithNullId_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OutputTag<string>(null!));
    }

    [Test]
    public void OutputTag_Equals_WithSameId_ReturnsTrue()
    {
        // Arrange
        var tag1 = new OutputTag<string>("test-id");
        var tag2 = new OutputTag<string>("test-id");

        // Act & Assert
        Assert.That(tag1.Equals(tag2), Is.True);
        Assert.That(tag1.GetHashCode(), Is.EqualTo(tag2.GetHashCode()));
    }

    [Test]
    public void OutputTag_Equals_WithDifferentId_ReturnsFalse()
    {
        // Arrange
        var tag1 = new OutputTag<string>("test-id-1");
        var tag2 = new OutputTag<string>("test-id-2");

        // Act & Assert
        Assert.That(tag1.Equals(tag2), Is.False);
    }

    #endregion

    #region Helper Classes

    private class TestReduceFunction : IReduceFunction<int>
    {
        public int Reduce(int value1, int value2) => value1 + value2;
    }

    private class TestAggregateFunction : IAggregateFunction<int, int, int>
    {
        public int CreateAccumulator() => 0;
        public int Add(int value, int accumulator) => accumulator + value;
        public int GetResult(int accumulator) => accumulator;
        public int Merge(int a, int b) => a + b;
    }

    #endregion
}
