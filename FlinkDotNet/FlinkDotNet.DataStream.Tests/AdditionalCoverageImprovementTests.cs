using System;
using FlinkDotNet.DataStream;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Additional tests to improve code coverage to 100%.
    /// Focuses on missing branches and edge cases not covered by existing tests.
    /// </summary>
    [TestFixture]
    public class AdditionalCoverageImprovementTests
    {
        #region OutputTag Additional Coverage

        [Test]
        public void OutputTag_Equals_WithNull_ReturnsFalse()
        {
            // Arrange
            var tag = new OutputTag<string>("test-id");

            // Act & Assert
            Assert.That(tag.Equals(null), Is.False);
        }

        [Test]
        public void OutputTag_Equals_WithWrongType_ReturnsFalse()
        {
            // Arrange
            var tag = new OutputTag<string>("test-id");
            var wrongType = new object();

            // Act & Assert
            Assert.That(tag.Equals(wrongType), Is.False);
        }

        [Test]
        public void OutputTag_Equals_WithDifferentGenericType_ReturnsFalse()
        {
            // Arrange
            var tag1 = new OutputTag<string>("test-id");
            var tag2 = new OutputTag<int>("test-id");

            // Act & Assert
            Assert.That(tag1.Equals((object)tag2), Is.False);
        }

        [Test]
        public void OutputTag_GetHashCode_ConsistentForSameId()
        {
            // Arrange
            var tag = new OutputTag<string>("consistent-id");

            // Act
            var hash1 = tag.GetHashCode();
            var hash2 = tag.GetHashCode();

            // Assert
            Assert.That(hash1, Is.EqualTo(hash2));
        }

        #endregion

        #region State Descriptor Property Access Coverage

        [Test]
        public void ValueStateDescriptor_ValueType_ReturnsCorrectType()
        {
            // Arrange & Act
            var descriptor = new ValueStateDescriptor<double>("double-state");

            // Assert - Explicitly access property to ensure coverage
            Assert.That(descriptor.ValueType, Is.EqualTo(typeof(double)));
            Assert.That(descriptor.ValueType, Is.Not.Null);
        }

        [Test]
        public void ListStateDescriptor_ElementType_ReturnsCorrectType()
        {
            // Arrange & Act
            var descriptor = new ListStateDescriptor<bool>("bool-list-state");

            // Assert - Explicitly access property to ensure coverage
            Assert.That(descriptor.ElementType, Is.EqualTo(typeof(bool)));
            Assert.That(descriptor.ElementType, Is.Not.Null);
        }

        [Test]
        public void MapStateDescriptor_KeyType_ReturnsCorrectType()
        {
            // Arrange & Act
            var descriptor = new MapStateDescriptor<Guid, string>("guid-map-state");

            // Assert - Explicitly access property to ensure coverage
            Assert.That(descriptor.KeyType, Is.EqualTo(typeof(Guid)));
            Assert.That(descriptor.KeyType, Is.Not.Null);
        }

        [Test]
        public void MapStateDescriptor_ValueType_ReturnsCorrectType()
        {
            // Arrange & Act
            var descriptor = new MapStateDescriptor<string, DateTime>("datetime-map-state");

            // Assert - Explicitly access property to ensure coverage
            Assert.That(descriptor.ValueType, Is.EqualTo(typeof(DateTime)));
            Assert.That(descriptor.ValueType, Is.Not.Null);
        }

        [Test]
        public void ReducingStateDescriptor_ReduceFunction_NotNull()
        {
            // Arrange
            var reduceFunc = new TestReduceFunction();

            // Act
            var descriptor = new ReducingStateDescriptor<int>("reduce-state", reduceFunc);

            // Assert - Explicitly access property to ensure coverage
            Assert.That(descriptor.ReduceFunction, Is.Not.Null);
            Assert.That(descriptor.ReduceFunction, Is.EqualTo(reduceFunc));
        }

        [Test]
        public void ReducingStateDescriptor_WithNullFunction_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ReducingStateDescriptor<int>("state", null!));
        }

        [Test]
        public void AggregatingStateDescriptor_AggregateFunction_NotNull()
        {
            // Arrange
            var aggFunc = new TestAggregateFunction();

            // Act
            var descriptor = new AggregatingStateDescriptor<int, int, int>("agg-state", aggFunc);

            // Assert - Explicitly access property to ensure coverage
            Assert.That(descriptor.AggregateFunction, Is.Not.Null);
            Assert.That(descriptor.AggregateFunction, Is.EqualTo(aggFunc));
        }

        [Test]
        public void AggregatingStateDescriptor_WithNullFunction_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new AggregatingStateDescriptor<int, int, int>("state", null!));
        }

        [Test]
        public void StateDescriptor_Name_IsAccessible()
        {
            // Arrange & Act
            var descriptor = new ValueStateDescriptor<string>("name-test");

            // Assert - Explicitly access Name property from base class
            Assert.That(descriptor.Name, Is.EqualTo("name-test"));
            Assert.That(descriptor.Name, Is.Not.Null);
            Assert.That(descriptor.Name.Length, Is.GreaterThan(0));
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
}
