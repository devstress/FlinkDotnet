#nullable enable
using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Tests to achieve 100% branch coverage for DataStream partitioning and configuration methods.
    /// Covers Rebalance, Rescale, Forward, Shuffle, Broadcast, PartitionCustom, SetMaxParallelism, SlotSharingGroup.
    /// </summary>
    [TestFixture]
    public class DataStreamPartitioningCoverageTests
    {
        private StreamExecutionEnvironment? _env;

        [SetUp]
        public void Setup() => this._env = StreamExecutionEnvironment.GetExecutionEnvironment();

        #region Partitioning Operations

        [Test]
        public void Rebalance_ReturnsDataStream()
        {
            // Arrange
            var data = new List<int> { 1, 2, 3, 4, 5 };
            var stream = this._env.FromCollection(data);

            // Act
            var result = stream.Rebalance();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<DataStream<int>>());
            Assert.That(result, Is.SameAs(stream));
        }

        [Test]
        public void Rescale_ReturnsDataStream()
        {
            // Arrange
            var data = new List<int> { 1, 2, 3, 4, 5 };
            var stream = this._env.FromCollection(data);

            // Act
            var result = stream.Rescale();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<DataStream<int>>());
            Assert.That(result, Is.SameAs(stream));
        }

        [Test]
        public void Forward_ReturnsDataStream()
        {
            // Arrange
            var data = new List<int> { 1, 2, 3, 4, 5 };
            var stream = this._env.FromCollection(data);

            // Act
            var result = stream.Forward();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<DataStream<int>>());
            Assert.That(result, Is.SameAs(stream));
        }

        [Test]
        public void Shuffle_ReturnsDataStream()
        {
            // Arrange
            var data = new List<int> { 1, 2, 3, 4, 5 };
            var stream = this._env.FromCollection(data);

            // Act
            var result = stream.Shuffle();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<DataStream<int>>());
            Assert.That(result, Is.SameAs(stream));
        }

        [Test]
        public void Broadcast_ReturnsDataStream()
        {
            // Arrange
            var data = new List<int> { 1, 2, 3, 4, 5 };
            var stream = this._env.FromCollection(data);

            // Act
            var result = stream.Broadcast();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<DataStream<int>>());
            Assert.That(result, Is.SameAs(stream));
        }

        [Test]
        public void PartitionCustom_WithValidPartitioner_ReturnsDataStream()
        {
            // Arrange
            var data = new List<int> { 1, 2, 3, 4, 5 };
            var stream = this._env.FromCollection(data);
            Func<int, int, int> partitioner = (key, numPartitions) => key % numPartitions;
            Func<int, int> keySelector = x => x;

            // Act
            var result = stream.PartitionCustom(partitioner, keySelector);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<DataStream<int>>());
            Assert.That(result, Is.SameAs(stream));
        }

        [Test]
        public void PartitionCustom_WithStringKey_ReturnsDataStream()
        {
            // Arrange
            var data = new List<(string, int)> { ("a", 1), ("b", 2), ("c", 3) };
            var stream = this._env.FromCollection(data);
            Func<string, int, int> partitioner = (key, numPartitions) => key.Length % numPartitions;
            Func<(string, int), string> keySelector = x => x.Item1;

            // Act
            var result = stream.PartitionCustom(partitioner, keySelector);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<DataStream<(string, int)>>());
        }

        #endregion

        #region FlinkConfiguration Operations

        [Test]
        public void SetMaxParallelism_WithValidValue_ReturnsDataStream()
        {
            // Arrange
            var data = new List<int> { 1, 2, 3, 4, 5 };
            var stream = this._env.FromCollection(data);

            // Act
            var result = stream.SetMaxParallelism(128);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<DataStream<int>>());
            Assert.That(result, Is.SameAs(stream));
        }

        [Test]
        public void SetMaxParallelism_WithMinimumValue_ReturnsDataStream()
        {
            // Arrange
            var data = new List<int> { 1, 2, 3, 4, 5 };
            var stream = this._env.FromCollection(data);

            // Act
            var result = stream.SetMaxParallelism(1);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(stream));
        }

        [Test]
        public void SetMaxParallelism_WithMaximumValue_ReturnsDataStream()
        {
            // Arrange
            var data = new List<int> { 1, 2, 3, 4, 5 };
            var stream = this._env.FromCollection(data);

            // Act
            var result = stream.SetMaxParallelism(32768);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(stream));
        }

        [Test]
        public void SetMaxParallelism_WithZero_ThrowsArgumentException()
        {
            // Arrange
            var data = new List<int> { 1, 2, 3, 4, 5 };
            var stream = this._env.FromCollection(data);

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => stream.SetMaxParallelism(0));
            Assert.That(ex!.Message, Does.Contain("between 1 and 32768"));
        }

        [Test]
        public void SetMaxParallelism_WithNegativeValue_ThrowsArgumentException()
        {
            // Arrange
            var data = new List<int> { 1, 2, 3, 4, 5 };
            var stream = this._env.FromCollection(data);

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => stream.SetMaxParallelism(-1));
            Assert.That(ex!.Message, Does.Contain("between 1 and 32768"));
        }

        [Test]
        public void SetMaxParallelism_WithValueTooLarge_ThrowsArgumentException()
        {
            // Arrange
            var data = new List<int> { 1, 2, 3, 4, 5 };
            var stream = this._env.FromCollection(data);

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => stream.SetMaxParallelism(32769));
            Assert.That(ex!.Message, Does.Contain("between 1 and 32768"));
        }

        [Test]
        public void SlotSharingGroup_WithValidName_ReturnsDataStream()
        {
            // Arrange
            var data = new List<int> { 1, 2, 3, 4, 5 };
            var stream = this._env.FromCollection(data);

            // Act
            var result = stream.SlotSharingGroup("group1");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<DataStream<int>>());
            Assert.That(result, Is.SameAs(stream));
        }

        [Test]
        public void SlotSharingGroup_WithEmptyString_ReturnsDataStream()
        {
            // Arrange
            var data = new List<int> { 1, 2, 3, 4, 5 };
            var stream = this._env.FromCollection(data);

            // Act
            var result = stream.SlotSharingGroup("");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(stream));
        }

        #endregion

        #region Chaining Tests

        [Test]
        public void ChainedPartitioningOperations_WorksCorrectly()
        {
            // Arrange
            var data = new List<int> { 1, 2, 3, 4, 5 };
            var stream = this._env.FromCollection(data);

            // Act
            var result = stream
                .Rebalance()
                .Rescale()
                .Forward()
                .Shuffle()
                .Broadcast();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<DataStream<int>>());
        }

        [Test]
        public void ChainedConfigurationOperations_WorksCorrectly()
        {
            // Arrange
            var data = new List<int> { 1, 2, 3, 4, 5 };
            var stream = this._env.FromCollection(data);

            // Act
            var result = stream
                .SetMaxParallelism(128)
                .SlotSharingGroup("group1")
                .SetParallelism(4)
                .Name("test-stream");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<DataStream<int>>());
        }

        [Test]
        public void PartitioningWithTransformations_WorksCorrectly()
        {
            // Arrange
            var data = new List<int> { 1, 2, 3, 4, 5 };
            var stream = this._env.FromCollection(data);

            // Act
            var result = stream
                .Map(x => x * 2)
                .Rebalance()
                .Filter(x => x > 5)
                .Shuffle()
                .SetMaxParallelism(64);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        #endregion
    }
}
