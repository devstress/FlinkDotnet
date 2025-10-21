using System;
using FlinkDotNet.DataStream;
using FlinkDotNet.DataStream.Window;
using FlinkDotNet.DataStream.Window.Assigners;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    [TestFixture]
    public class KeyedStreamTests
    {
        private StreamExecutionEnvironment _env = null!;

        [SetUp]
        public void Setup()
        {
            _env = StreamExecutionEnvironment.GetExecutionEnvironment();
        }

        [Test]
        public void Reduce_WithFunc_ReturnsDataStream()
        {
            var collection = new[] { 1, 2, 3, 4, 5 };
            var stream = _env.FromCollection(collection);
            var keyed = stream.KeyBy(x => x % 2);
            var reduced = keyed.Reduce((a, b) => a + b);
            Assert.That(reduced, Is.Not.Null);
        }

        [Test]
        public void Reduce_WithReduceFunction_ReturnsDataStream()
        {
            var collection = new[] { 1, 2, 3, 4, 5 };
            var stream = _env.FromCollection(collection);
            var keyed = stream.KeyBy(x => x % 2);
            var reduceFunction = new TestReduceFunction();
            var reduced = keyed.Reduce(reduceFunction);
            Assert.That(reduced, Is.Not.Null);
        }

        [Test]
        public void Aggregate_WithValidParameters_ReturnsDataStream()
        {
            var collection = new[] { 1, 2, 3, 4, 5 };
            var stream = _env.FromCollection(collection);
            var keyed = stream.KeyBy(x => x % 2);
            var aggregated = keyed.Aggregate("SUM", "value");
            Assert.That(aggregated, Is.Not.Null);
        }

        [Test]
        public void Window_WithWindowAssigner_ReturnsWindowedStream()
        {
            var collection = new[] { 1, 2, 3, 4, 5 };
            var stream = _env.FromCollection(collection);
            var keyed = stream.KeyBy(x => x % 2);
            var assigner = TumblingEventTimeWindows<int>.Of(Time.Seconds(5));
            var windowed = keyed.Window(assigner);
            Assert.That(windowed, Is.Not.Null);
            Assert.That(windowed, Is.InstanceOf<WindowedStream<int, int, TimeWindow>>());
        }

        [Test]
        public void Window_NullAssigner_ThrowsArgumentNullException()
        {
            var collection = new[] { 1, 2, 3, 4, 5 };
            var stream = _env.FromCollection(collection);
            var keyed = stream.KeyBy(x => x % 2);
            Assert.Throws<ArgumentNullException>(() => keyed.Window<TimeWindow>(null!));
        }

        [Test]
        public void GetDataStream_ReturnsUnderlyingDataStream()
        {
            var collection = new[] { 1, 2, 3, 4, 5 };
            var stream = _env.FromCollection(collection);
            var keyed = stream.KeyBy(x => x % 2);
            var dataStream = keyed.GetDataStream();
            Assert.That(dataStream, Is.SameAs(stream));
        }

        // Test helper class
        private class TestReduceFunction : IReduceFunction<int>
        {
            public int Reduce(int value1, int value2) => value1 + value2;
        }
    }

    [TestFixture]
    public class AllWindowedStreamTests
    {
        private StreamExecutionEnvironment _env = null!;

        [SetUp]
        public void Setup()
        {
            _env = StreamExecutionEnvironment.GetExecutionEnvironment();
        }

        [Test]
        public void Constructor_WithTimeWindow_CreatesAllWindowedStream()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = _env.FromCollection(collection);
            var windowed = stream.TimeWindowAll(Time.Seconds(5));
            Assert.That(windowed, Is.Not.Null);
        }

        [Test]
        public void Constructor_WithCountWindow_CreatesAllWindowedStream()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = _env.FromCollection(collection);
            var windowed = stream.CountWindowAll(100);
            Assert.That(windowed, Is.Not.Null);
        }

        [Test]
        public void GetWindowSize_TimeWindow_ReturnsWindowSize()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = _env.FromCollection(collection);
            var windowSize = Time.Seconds(10);
            var windowed = stream.TimeWindowAll(windowSize);
            Assert.That(windowed.GetWindowSize(), Is.EqualTo(windowSize));
        }

        [Test]
        public void GetWindowSize_CountWindow_ReturnsNull()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = _env.FromCollection(collection);
            var windowed = stream.CountWindowAll(100);
            Assert.That(windowed.GetWindowSize(), Is.Null);
        }

        [Test]
        public void GetWindowCount_CountWindow_ReturnsWindowCount()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = _env.FromCollection(collection);
            var windowed = stream.CountWindowAll(100);
            Assert.That(windowed.GetWindowCount(), Is.EqualTo(100));
        }

        [Test]
        public void GetWindowCount_TimeWindow_ReturnsNull()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = _env.FromCollection(collection);
            var windowed = stream.TimeWindowAll(Time.Seconds(5));
            Assert.That(windowed.GetWindowCount(), Is.Null);
        }

        [Test]
        public void Aggregate_WithValidAggregateFunction_ReturnsDataStream()
        {
            // Arrange
            var source = new TestSourceFunction();
            var stream = _env.AddSource(source, "test-source");
            var windowed = stream.TimeWindowAll(Time.Seconds(5));
            var aggregateFunction = new TestAggregateFunction();

            // Act
            var result = windowed.Aggregate(aggregateFunction);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<DataStream<int>>());
        }

        [Test]
        public void Aggregate_WithDifferentAccumulatorType_ReturnsDataStream()
        {
            // Arrange
            var source = new TestSourceFunction();
            var stream = _env.AddSource(source, "test-source");
            var windowed = stream.TimeWindowAll(Time.Seconds(5));
            var aggregateFunction = new SumAggregateFunction();

            // Act
            var result = windowed.Aggregate(aggregateFunction);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<DataStream<int>>());
        }

        [Test]
        public void Aggregate_WithCountWindow_ReturnsDataStream()
        {
            // Arrange
            var source = new TestSourceFunction();
            var stream = _env.AddSource(source, "test-source");
            var windowed = stream.CountWindowAll(10);
            var aggregateFunction = new TestAggregateFunction();

            // Act
            var result = windowed.Aggregate(aggregateFunction);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<DataStream<int>>());
        }


        // Test helper classes
        private class TestSourceFunction : ISourceFunction<int>
        {
            public async System.Collections.Generic.IAsyncEnumerable<int> RunAsync(
                [System.Runtime.CompilerServices.EnumeratorCancellation] System.Threading.CancellationToken cancellationToken = default)
            {
                await System.Threading.Tasks.Task.Delay(10, cancellationToken);
                yield return 1;
                yield return 2;
                yield return 3;
            }
        }

        private class TestAggregateFunction : IAggregateFunction<int, int, int>
        {
            public int CreateAccumulator() => 0;
            public int Add(int value, int accumulator) => accumulator + value;
            public int GetResult(int accumulator) => accumulator;
            public int Merge(int acc1, int acc2) => acc1 + acc2;
        }

        private class SumAggregateFunction : IAggregateFunction<int, long, int>
        {
            public long CreateAccumulator() => 0L;
            public long Add(int value, long accumulator) => accumulator + value;
            public int GetResult(long accumulator) => (int)accumulator;
            public long Merge(long acc1, long acc2) => acc1 + acc2;
        }
    }
}