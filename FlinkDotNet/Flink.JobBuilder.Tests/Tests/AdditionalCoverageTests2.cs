using FlinkDotNet.DataStream;
using FlinkDotNet.DataStream.Window;
using FlinkDotNet.DataStream.Window.Assigners;
using Flink.JobBuilder.Flink;

namespace Flink.JobBuilder.Tests.Tests;

/// <summary>
/// Additional tests to increase code coverage to 90%
/// Focuses on covering methods and branches that are not yet tested
/// </summary>
[TestFixture]
public class AdditionalCoverageTests2
{
    #region KeyedStream Window Method Tests

    [Test]
    public void KeyedStream_Window_WithTumblingWindow_CreatesWindowedStream()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var dataStream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });
        var keyedStream = dataStream.KeyBy(x => x % 2);
        var assigner = TumblingEventTimeWindows<int>.Of(Time.Seconds(10));

        // Act
        var windowedStream = keyedStream.Window(assigner);

        // Assert
        Assert.That(windowedStream, Is.Not.Null);
        Assert.That(windowedStream.GetKeyedStream(), Is.EqualTo(keyedStream));
        Assert.That(windowedStream.GetWindowAssigner(), Is.EqualTo(assigner));
    }

    [Test]
    public void KeyedStream_Window_WithSlidingWindow_CreatesWindowedStream()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var dataStream = env.FromCollection(new[] { "a", "b", "c" });
        var keyedStream = dataStream.KeyBy(s => s[0]);
        var assigner = SlidingEventTimeWindows<string>.Of(Time.Minutes(5), Time.Minutes(1));

        // Act
        var windowedStream = keyedStream.Window(assigner);

        // Assert
        Assert.That(windowedStream, Is.Not.Null);
    }

    [Test]
    public void KeyedStream_Window_WithSessionWindow_CreatesWindowedStream()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var dataStream = env.FromCollection(new[] { 1, 2, 3 });
        var keyedStream = dataStream.KeyBy(x => x);
        var assigner = SessionWindows<int>.WithGap(Time.Minutes(10));

        // Act
        var windowedStream = keyedStream.Window(assigner);

        // Assert
        Assert.That(windowedStream, Is.Not.Null);
    }

    [Test]
    public void KeyedStream_Window_WithNullAssigner_ThrowsArgumentNullException()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var dataStream = env.FromCollection(new[] { 1, 2, 3 });
        var keyedStream = dataStream.KeyBy(x => x);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => keyedStream.Window<TimeWindow>(null!));
    }

    #endregion

    #region KeyedStream Aggregate Method Tests

    [Test]
    public void KeyedStream_Aggregate_WithStringAggregation_ReturnsDataStream()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var dataStream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });
        var keyedStream = dataStream.KeyBy(x => x % 2);

        // Act
        var result = keyedStream.Aggregate("sum", "value");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<DataStream<int>>());
    }

    [Test]
    public void KeyedStream_Aggregate_WithAverage_ReturnsDataStream()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var dataStream = env.FromCollection(new[] { 10, 20, 30, 40, 50 });
        var keyedStream = dataStream.KeyBy(x => x % 10);

        // Act
        var result = keyedStream.Aggregate("avg", "value");

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void KeyedStream_Aggregate_WithMin_ReturnsDataStream()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var dataStream = env.FromCollection(new[] { 5, 3, 8, 1, 9 });
        var keyedStream = dataStream.KeyBy(x => x % 2);

        // Act
        var result = keyedStream.Aggregate("min", "value");

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void KeyedStream_Aggregate_WithMax_ReturnsDataStream()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var dataStream = env.FromCollection(new[] { 5, 3, 8, 1, 9 });
        var keyedStream = dataStream.KeyBy(x => x % 2);

        // Act
        var result = keyedStream.Aggregate("max", "value");

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void KeyedStream_Aggregate_WithCount_ReturnsDataStream()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var dataStream = env.FromCollection(new[] { "a", "b", "c" });
        var keyedStream = dataStream.KeyBy(s => s);

        // Act
        var result = keyedStream.Aggregate("count", "value");

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    #endregion
}
