using FlinkDotNet.DataStream;
using FlinkDotNet.DataStream.Window;
using FlinkDotNet.DataStream.Window.Assigners;
using FlinkDotNet.DataStream.Window.Functions;

namespace Flink.JobBuilder.Tests.Tests;

/// <summary>
/// Comprehensive tests for WindowedStream class to achieve 100% code coverage
/// </summary>
[TestFixture]
public class WindowedStreamTests
{
    #region Constructor Tests

    [Test]
    public void WindowedStream_Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var dataStream = env.FromCollection(new[] { 1, 2, 3 });
        var keyedStream = dataStream.KeyBy(x => x % 2);
        var assigner = TumblingEventTimeWindows<int>.Of(Time.Seconds(5));

        // Act
        var windowedStream = keyedStream.Window(assigner);

        // Assert
        Assert.That(windowedStream, Is.Not.Null);
    }

    [Test]
    public void WindowedStream_Constructor_WithNullAssigner_ThrowsArgumentNullException()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var dataStream = env.FromCollection(new[] { 1, 2, 3 });
        var keyedStream = dataStream.KeyBy(x => x % 2);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            keyedStream.Window<TimeWindow>(null!));
    }

    #endregion

    #region Aggregate Tests

    [Test]
    public void WindowedStream_Aggregate_WithAggregateFunction_ReturnsDataStream()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var dataStream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });
        var keyedStream = dataStream.KeyBy(x => x % 2);
        var assigner = TumblingEventTimeWindows<int>.Of(Time.Seconds(5));
        var windowedStream = keyedStream.Window(assigner);
        var aggregateFunction = new SumAggregateFunction();

        // Act
        var result = windowedStream.Aggregate(aggregateFunction);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<DataStream<int>>());
    }

    [Test]
    public void WindowedStream_Aggregate_WithNullFunction_ThrowsArgumentNullException()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var dataStream = env.FromCollection(new[] { 1, 2, 3 });
        var keyedStream = dataStream.KeyBy(x => x % 2);
        var assigner = TumblingEventTimeWindows<int>.Of(Time.Seconds(5));
        var windowedStream = keyedStream.Window(assigner);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            windowedStream.Aggregate<int, int>(null!));
    }

    #endregion

    #region Reduce Tests

    [Test]
    public void WindowedStream_Reduce_WithReduceFunction_ReturnsDataStream()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var dataStream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });
        var keyedStream = dataStream.KeyBy(x => x % 2);
        var assigner = TumblingEventTimeWindows<int>.Of(Time.Seconds(5));
        var windowedStream = keyedStream.Window(assigner);
        var reduceFunction = new SumReduceFunction();

        // Act
        var result = windowedStream.Reduce(reduceFunction);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<DataStream<int>>());
    }

    [Test]
    public void WindowedStream_Reduce_WithNullFunction_ThrowsArgumentNullException()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var dataStream = env.FromCollection(new[] { 1, 2, 3 });
        var keyedStream = dataStream.KeyBy(x => x % 2);
        var assigner = TumblingEventTimeWindows<int>.Of(Time.Seconds(5));
        var windowedStream = keyedStream.Window(assigner);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            windowedStream.Reduce((IReduceFunction<int>)null!));
    }

    [Test]
    public void WindowedStream_Reduce_WithLambda_ReturnsDataStream()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var dataStream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });
        var keyedStream = dataStream.KeyBy(x => x % 2);
        var assigner = TumblingEventTimeWindows<int>.Of(Time.Seconds(5));
        var windowedStream = keyedStream.Window(assigner);

        // Act
        var result = windowedStream.Reduce((a, b) => a + b);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<DataStream<int>>());
    }

    [Test]
    public void WindowedStream_Reduce_WithNullLambda_ThrowsArgumentNullException()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var dataStream = env.FromCollection(new[] { 1, 2, 3 });
        var keyedStream = dataStream.KeyBy(x => x % 2);
        var assigner = TumblingEventTimeWindows<int>.Of(Time.Seconds(5));
        var windowedStream = keyedStream.Window(assigner);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            windowedStream.Reduce((Func<int, int, int>)null!));
    }

    #endregion

    #region Process Tests

    [Test]
    public void WindowedStream_Process_WithProcessFunction_ReturnsDataStream()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var dataStream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });
        var keyedStream = dataStream.KeyBy(x => x % 2);
        var assigner = TumblingEventTimeWindows<int>.Of(Time.Seconds(5));
        var windowedStream = keyedStream.Window(assigner);
        var processFunction = new TestProcessWindowFunction();

        // Act
        var result = windowedStream.Process(processFunction);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<DataStream<string>>());
    }

    [Test]
    public void WindowedStream_Process_WithNullFunction_ThrowsArgumentNullException()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var dataStream = env.FromCollection(new[] { 1, 2, 3 });
        var keyedStream = dataStream.KeyBy(x => x % 2);
        var assigner = TumblingEventTimeWindows<int>.Of(Time.Seconds(5));
        var windowedStream = keyedStream.Window(assigner);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            windowedStream.Process<string>(null!));
    }

    #endregion

    #region GetWindowAssigner Tests

    [Test]
    public void WindowedStream_GetWindowAssigner_ReturnsCorrectAssigner()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var dataStream = env.FromCollection(new[] { 1, 2, 3 });
        var keyedStream = dataStream.KeyBy(x => x % 2);
        var assigner = TumblingEventTimeWindows<int>.Of(Time.Seconds(5));
        var windowedStream = keyedStream.Window(assigner);

        // Act
        var retrievedAssigner = windowedStream.GetWindowAssigner();

        // Assert
        Assert.That(retrievedAssigner, Is.Not.Null);
        Assert.That(retrievedAssigner, Is.EqualTo(assigner));
    }

    #endregion

    #region GetKeyedStream Tests

    [Test]
    public void WindowedStream_GetKeyedStream_ReturnsCorrectKeyedStream()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var dataStream = env.FromCollection(new[] { 1, 2, 3 });
        var keyedStream = dataStream.KeyBy(x => x % 2);
        var assigner = TumblingEventTimeWindows<int>.Of(Time.Seconds(5));
        var windowedStream = keyedStream.Window(assigner);

        // Act
        var retrievedKeyedStream = windowedStream.GetKeyedStream();

        // Assert
        Assert.That(retrievedKeyedStream, Is.Not.Null);
        Assert.That(retrievedKeyedStream, Is.EqualTo(keyedStream));
    }

    #endregion

    #region Integration Tests with Different Window Assigners

    [Test]
    public void WindowedStream_WithSlidingWindow_WorksCorrectly()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var dataStream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });
        var keyedStream = dataStream.KeyBy(x => x % 3);
        var assigner = SlidingEventTimeWindows<int>.Of(Time.Seconds(10), Time.Seconds(5));
        
        // Act
        var windowedStream = keyedStream.Window(assigner);
        var result = windowedStream.Reduce((a, b) => a + b);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<DataStream<int>>());
    }

    [Test]
    public void WindowedStream_WithSessionWindow_WorksCorrectly()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var dataStream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });
        var keyedStream = dataStream.KeyBy(x => x % 3);
        var assigner = SessionWindows<int>.WithGap(Time.Minutes(5));
        
        // Act
        var windowedStream = keyedStream.Window(assigner);
        var result = windowedStream.Reduce((a, b) => a + b);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<DataStream<int>>());
    }

    #endregion

    #region Test Helper Classes

    private class SumAggregateFunction : IAggregateFunction<int, int, int>
    {
        public int CreateAccumulator() => 0;
        public int Add(int value, int accumulator) => accumulator + value;
        public int GetResult(int accumulator) => accumulator;
        public int Merge(int a, int b) => a + b;
    }

    private class SumReduceFunction : IReduceFunction<int>
    {
        public int Reduce(int value1, int value2) => value1 + value2;
    }

    private class TestProcessWindowFunction : IProcessWindowFunction<int, string, int, TimeWindow>
    {
        public IEnumerable<string> Process(
            int key,
            IProcessWindowFunction<int, string, int, TimeWindow>.IProcessWindowContext context,
            IEnumerable<int> elements)
        {
            yield return $"Key: {key}, Count: {elements.Count()}";
        }
    }

    #endregion
}
