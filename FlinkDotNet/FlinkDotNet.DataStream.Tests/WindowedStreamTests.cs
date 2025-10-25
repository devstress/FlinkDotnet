using System;
using System.Collections.Generic;
using NUnit.Framework;
using FlinkDotNet.DataStream.Window;
using FlinkDotNet.DataStream.Window.Assigners;
using FlinkDotNet.DataStream.Window.Functions;

namespace FlinkDotNet.DataStream.Tests
{
    [TestFixture]
    public class WindowedStreamTests
    {
        private StreamExecutionEnvironment _env = null!;

        [SetUp]
        public void Setup()
        {
            _env = StreamExecutionEnvironment.GetExecutionEnvironment();
        }

        // Test helper classes
        private class TestAggregateFunction : IAggregateFunction<string, List<string>, string>
        {
            public List<string> CreateAccumulator() => new List<string>();
            public List<string> Add(string value, List<string> accumulator)
            {
                accumulator.Add(value);
                return accumulator;
            }
            public string GetResult(List<string> accumulator) => string.Join(",", accumulator);
            public List<string> Merge(List<string> a, List<string> b)
            {
                a.AddRange(b);
                return a;
            }
        }

        private class TestReduceFunction : IReduceFunction<string>
        {
            public string Reduce(string value1, string value2) => value1 + value2;
        }

        private class TestProcessWindowFunction : IProcessWindowFunction<string, string, int, TimeWindow>
        {
            public IEnumerable<string> Process(int key, IProcessWindowFunction<string, string, int, TimeWindow>.IProcessWindowContext context, IEnumerable<string> elements)
            {
                return new List<string> { string.Join(",", elements) };
            }
        }

        [Test]
        public void Constructor_WithNullKeyedStream_ThrowsArgumentNullException()
        {
            // Arrange
            var windowAssigner = TumblingEventTimeWindows<string>.Of(Time.Seconds(5));

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => 
                new WindowedStream<string, int, TimeWindow>(null!, windowAssigner));
            Assert.That(ex!.ParamName, Is.EqualTo("keyedStream"));
        }

        [Test]
        public void Constructor_WithNullWindowAssigner_ThrowsArgumentNullException()
        {
            // Arrange
            var dataStream = _env.FromCollection(new[] { "a", "b", "c" });
            var keyedStream = dataStream.KeyBy(x => x.GetHashCode());

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => 
                new WindowedStream<string, int, TimeWindow>(keyedStream, null!));
            Assert.That(ex!.ParamName, Is.EqualTo("windowAssigner"));
        }

        [Test]
        public void Aggregate_WithNullAggregateFunction_ThrowsArgumentNullException()
        {
            // Arrange
            var dataStream = _env.FromCollection(new[] { "a", "b", "c" });
            var keyedStream = dataStream.KeyBy(x => x.GetHashCode());
            var windowAssigner = TumblingEventTimeWindows<string>.Of(Time.Seconds(5));
            var windowedStream = new WindowedStream<string, int, TimeWindow>(keyedStream, windowAssigner);

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => 
                windowedStream.Aggregate<List<string>, string>(null!));
            Assert.That(ex!.ParamName, Is.EqualTo("aggregateFunction"));
        }

        [Test]
        public void Aggregate_WithValidAggregateFunction_ReturnsDataStream()
        {
            // Arrange
            var dataStream = _env.FromCollection(new[] { "a", "b", "c" });
            var keyedStream = dataStream.KeyBy(x => x.GetHashCode());
            var windowAssigner = TumblingEventTimeWindows<string>.Of(Time.Seconds(5));
            var windowedStream = new WindowedStream<string, int, TimeWindow>(keyedStream, windowAssigner);
            var aggregateFunction = new TestAggregateFunction();

            // Act
            var result = windowedStream.Aggregate(aggregateFunction);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<DataStream<string>>());
        }

        [Test]
        public void Reduce_WithNullReduceFunction_ThrowsArgumentNullException()
        {
            // Arrange
            var dataStream = _env.FromCollection(new[] { "a", "b", "c" });
            var keyedStream = dataStream.KeyBy(x => x.GetHashCode());
            var windowAssigner = TumblingEventTimeWindows<string>.Of(Time.Seconds(5));
            var windowedStream = new WindowedStream<string, int, TimeWindow>(keyedStream, windowAssigner);

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => 
                windowedStream.Reduce((IReduceFunction<string>)null!));
            Assert.That(ex!.ParamName, Is.EqualTo("reduceFunction"));
        }

        [Test]
        public void Reduce_WithValidReduceFunction_ReturnsDataStream()
        {
            // Arrange
            var dataStream = _env.FromCollection(new[] { "a", "b", "c" });
            var keyedStream = dataStream.KeyBy(x => x.GetHashCode());
            var windowAssigner = TumblingEventTimeWindows<string>.Of(Time.Seconds(5));
            var windowedStream = new WindowedStream<string, int, TimeWindow>(keyedStream, windowAssigner);
            var reduceFunction = new TestReduceFunction();

            // Act
            var result = windowedStream.Reduce(reduceFunction);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<DataStream<string>>());
        }

        [Test]
        public void Reduce_WithNullLambdaFunction_ThrowsArgumentNullException()
        {
            // Arrange
            var dataStream = _env.FromCollection(new[] { "a", "b", "c" });
            var keyedStream = dataStream.KeyBy(x => x.GetHashCode());
            var windowAssigner = TumblingEventTimeWindows<string>.Of(Time.Seconds(5));
            var windowedStream = new WindowedStream<string, int, TimeWindow>(keyedStream, windowAssigner);

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => 
                windowedStream.Reduce((Func<string, string, string>)null!));
            Assert.That(ex!.ParamName, Is.EqualTo("reduceFunction"));
        }

        [Test]
        public void Reduce_WithValidLambdaFunction_ReturnsDataStream()
        {
            // Arrange
            var dataStream = _env.FromCollection(new[] { "a", "b", "c" });
            var keyedStream = dataStream.KeyBy(x => x.GetHashCode());
            var windowAssigner = TumblingEventTimeWindows<string>.Of(Time.Seconds(5));
            var windowedStream = new WindowedStream<string, int, TimeWindow>(keyedStream, windowAssigner);

            // Act
            var result = windowedStream.Reduce((a, b) => a + b);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<DataStream<string>>());
        }

        [Test]
        public void Process_WithNullProcessFunction_ThrowsArgumentNullException()
        {
            // Arrange
            var dataStream = _env.FromCollection(new[] { "a", "b", "c" });
            var keyedStream = dataStream.KeyBy(x => x.GetHashCode());
            var windowAssigner = TumblingEventTimeWindows<string>.Of(Time.Seconds(5));
            var windowedStream = new WindowedStream<string, int, TimeWindow>(keyedStream, windowAssigner);

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => 
                windowedStream.Process<string>(null!));
            Assert.That(ex!.ParamName, Is.EqualTo("processFunction"));
        }

        [Test]
        public void Process_WithValidProcessFunction_ReturnsDataStream()
        {
            // Arrange
            var dataStream = _env.FromCollection(new[] { "a", "b", "c" });
            var keyedStream = dataStream.KeyBy(x => x.GetHashCode());
            var windowAssigner = TumblingEventTimeWindows<string>.Of(Time.Seconds(5));
            var windowedStream = new WindowedStream<string, int, TimeWindow>(keyedStream, windowAssigner);
            var processFunction = new TestProcessWindowFunction();

            // Act
            var result = windowedStream.Process(processFunction);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<DataStream<string>>());
        }

        [Test]
        public void GetWindowAssigner_ReturnsCorrectAssigner()
        {
            // Arrange
            var dataStream = _env.FromCollection(new[] { "a", "b", "c" });
            var keyedStream = dataStream.KeyBy(x => x.GetHashCode());
            var windowAssigner = TumblingEventTimeWindows<string>.Of(Time.Seconds(5));
            var windowedStream = new WindowedStream<string, int, TimeWindow>(keyedStream, windowAssigner);

            // Act
            var result = windowedStream.GetWindowAssigner();

            // Assert
            Assert.That(result, Is.SameAs(windowAssigner));
        }

        [Test]
        public void GetKeyedStream_ReturnsCorrectKeyedStream()
        {
            // Arrange
            var dataStream = _env.FromCollection(new[] { "a", "b", "c" });
            var keyedStream = dataStream.KeyBy(x => x.GetHashCode());
            var windowAssigner = TumblingEventTimeWindows<string>.Of(Time.Seconds(5));
            var windowedStream = new WindowedStream<string, int, TimeWindow>(keyedStream, windowAssigner);

            // Act
            var result = windowedStream.GetKeyedStream();

            // Assert
            Assert.That(result, Is.SameAs(keyedStream));
        }

        [Test]
        public void WindowedStream_WithSlidingWindows_WorksCorrectly()
        {
            // Arrange
            var dataStream = _env.FromCollection(new[] { "a", "b", "c" });
            var keyedStream = dataStream.KeyBy(x => x.GetHashCode());
            var windowAssigner = SlidingEventTimeWindows<string>.Of(Time.Seconds(10), Time.Seconds(5));
            var windowedStream = new WindowedStream<string, int, TimeWindow>(keyedStream, windowAssigner);
            var aggregateFunction = new TestAggregateFunction();

            // Act
            var result = windowedStream.Aggregate(aggregateFunction);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(windowedStream.GetWindowAssigner(), Is.SameAs(windowAssigner));
        }

        [Test]
        public void WindowedStream_WithSessionWindows_WorksCorrectly()
        {
            // Arrange
            var dataStream = _env.FromCollection(new[] { "a", "b", "c" });
            var keyedStream = dataStream.KeyBy(x => x.GetHashCode());
            var windowAssigner = SessionWindows<string>.WithGap(Time.Seconds(5));
            var windowedStream = new WindowedStream<string, int, TimeWindow>(keyedStream, windowAssigner);
            var reduceFunction = new TestReduceFunction();

            // Act
            var result = windowedStream.Reduce(reduceFunction);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(windowedStream.GetWindowAssigner(), Is.SameAs(windowAssigner));
        }
    }
}