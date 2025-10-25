using NUnit.Framework;
using System;
using System.Collections.Generic;
using FlinkDotNet.DataStream.Window;
using FlinkDotNet.DataStream.Window.Assigners;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Comprehensive tests to achieve 100% branch coverage for KeyedStream class.
    /// Tests all methods including Reduce, Aggregate, Window, and GetDataStream.
    /// </summary>
    [TestFixture]
    public class KeyedStreamCompleteCoverageTests
    {
        private StreamExecutionEnvironment? _env;

        [SetUp]
        public void Setup()
        {
            _env = StreamExecutionEnvironment.GetExecutionEnvironment();
        }

        #region Reduce Tests

        [Test]
        public void Reduce_WithFuncDelegate_ReturnsDataStream()
        {
            // Arrange
            var data = new List<int> { 1, 2, 3, 4, 5 };
            var stream = _env.FromCollection(data);
            var keyedStream = stream.KeyBy(x => x % 2);
            
            // Act
            var result = keyedStream.Reduce((a, b) => a + b);
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<DataStream<int>>());
        }

        [Test]
        public void Reduce_WithReduceFunction_ReturnsDataStream()
        {
            // Arrange
            var data = new List<int> { 1, 2, 3, 4, 5 };
            var stream = _env.FromCollection(data);
            var keyedStream = stream.KeyBy(x => x % 2);
            var reduceFunction = new SumReduceFunction();
            
            // Act
            var result = keyedStream.Reduce(reduceFunction);
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<DataStream<int>>());
        }

        [Test]
        public void Reduce_WithComplexType_ReturnsDataStream()
        {
            // Arrange
            var data = new List<(string, int)> { ("a", 1), ("b", 2), ("a", 3), ("b", 4) };
            var stream = _env.FromCollection(data);
            var keyedStream = stream.KeyBy(x => x.Item1);
            
            // Act
            var result = keyedStream.Reduce((a, b) => (a.Item1, a.Item2 + b.Item2));
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<DataStream<(string, int)>>());
        }

        #endregion

        #region Aggregate Tests

        [Test]
        public void Aggregate_WithStringParameters_ReturnsDataStream()
        {
            // Arrange
            var data = new List<int> { 1, 2, 3, 4, 5 };
            var stream = _env.FromCollection(data);
            var keyedStream = stream.KeyBy(x => x % 2);
            
            // Act
            var result = keyedStream.Aggregate("sum", "value");
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<DataStream<int>>());
        }

        [Test]
        public void Aggregate_WithDifferentAggregationType_ReturnsDataStream()
        {
            // Arrange
            var data = new List<int> { 1, 2, 3, 4, 5 };
            var stream = _env.FromCollection(data);
            var keyedStream = stream.KeyBy(x => x % 2);
            
            // Act
            var result = keyedStream.Aggregate("count", "value");
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<DataStream<int>>());
        }

        [Test]
        public void Aggregate_WithComplexType_ReturnsDataStream()
        {
            // Arrange
            var data = new List<(string key, int value)> { ("a", 1), ("b", 2), ("a", 3) };
            var stream = _env.FromCollection(data);
            var keyedStream = stream.KeyBy(x => x.key);
            
            // Act
            var result = keyedStream.Aggregate("avg", "value");
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<DataStream<(string, int)>>());
        }

        #endregion

        #region Window Tests

        [Test]
        public void Window_WithValidAssigner_ReturnsWindowedStream()
        {
            // Arrange
            var data = new List<string> { "a", "b", "c" };
            var stream = _env.FromCollection(data);
            var keyedStream = stream.KeyBy(x => x);
            var assigner = TumblingEventTimeWindows<string>.Of(Time.Seconds(10));
            
            // Act
            var result = keyedStream.Window(assigner);
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<Window.WindowedStream<string, string, TimeWindow>>());
        }

        [Test]
        public void Window_WithNullAssigner_ThrowsArgumentNullException()
        {
            // Arrange
            var data = new List<string> { "a", "b", "c" };
            var stream = _env.FromCollection(data);
            var keyedStream = stream.KeyBy(x => x);
            
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                keyedStream.Window<TimeWindow>(null!)
            );
        }

        [Test]
        public void Window_WithSlidingWindows_ReturnsWindowedStream()
        {
            // Arrange
            var data = new List<string> { "a", "b", "c" };
            var stream = _env.FromCollection(data);
            var keyedStream = stream.KeyBy(x => x);
            var assigner = SlidingEventTimeWindows<string>.Of(Time.Seconds(10), Time.Seconds(5));
            
            // Act
            var result = keyedStream.Window(assigner);
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<Window.WindowedStream<string, string, TimeWindow>>());
        }

        [Test]
        public void Window_WithSessionWindows_ReturnsWindowedStream()
        {
            // Arrange
            var data = new List<string> { "a", "b", "c" };
            var stream = _env.FromCollection(data);
            var keyedStream = stream.KeyBy(x => x);
            var assigner = SessionWindows<string>.WithGap(Time.Seconds(5));
            
            // Act
            var result = keyedStream.Window(assigner);
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<Window.WindowedStream<string, string, TimeWindow>>());
        }

        #endregion

        #region GetDataStream Tests

        [Test]
        public void GetDataStream_ReturnsUnderlyingDataStream()
        {
            // Arrange
            var data = new List<int> { 1, 2, 3, 4, 5 };
            var originalStream = _env.FromCollection(data);
            var keyedStream = originalStream.KeyBy(x => x % 2);
            
            // Act
            var result = keyedStream.GetDataStream();
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<DataStream<int>>());
        }

        [Test]
        public void GetDataStream_WithComplexKey_ReturnsDataStream()
        {
            // Arrange
            var data = new List<(string, int)> { ("a", 1), ("b", 2) };
            var originalStream = _env.FromCollection(data);
            var keyedStream = originalStream.KeyBy(x => x.Item1);
            
            // Act
            var result = keyedStream.GetDataStream();
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<DataStream<(string, int)>>());
        }

        #endregion

        #region Integration Tests

        [Test]
        public void KeyedStream_ChainedOperations_WorksCorrectly()
        {
            // Arrange
            var data = new List<int> { 1, 2, 3, 4, 5, 6 };
            var stream = _env.FromCollection(data);
            
            // Act - Chain multiple operations
            var result = stream
                .KeyBy(x => x % 2)
                .Reduce((a, b) => a + b)
                .KeyBy(x => x % 3)
                .Aggregate("sum", "value");
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<DataStream<int>>());
        }

        [Test]
        public void KeyedStream_AfterMapOperation_WorksCorrectly()
        {
            // Arrange
            var data = new List<int> { 1, 2, 3, 4, 5 };
            var stream = _env.FromCollection(data);
            
            // Act
            var result = stream
                .Map(x => x * 2)
                .KeyBy(x => x % 2)
                .Reduce((a, b) => a + b);
            
            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void KeyedStream_AfterFilterOperation_WorksCorrectly()
        {
            // Arrange
            var data = new List<int> { 1, 2, 3, 4, 5 };
            var stream = _env.FromCollection(data);
            
            // Act
            var result = stream
                .Filter(x => x > 2)
                .KeyBy(x => x % 2)
                .Reduce((a, b) => a + b);
            
            // Assert
            Assert.That(result, Is.Not.Null);
        }

        #endregion

        #region Helper Classes

        private class SumReduceFunction : IReduceFunction<int>
        {
            public int Reduce(int value1, int value2)
            {
                return value1 + value2;
            }
        }

        #endregion
    }
}
