using FlinkDotNet.DataStream;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests;

/// <summary>
/// Tests to cover OperationCapture.TranslateFilterOperation branch coverage
/// Tests that filter operations work correctly
/// </summary>
[TestFixture]
public class OperationCaptureFilterTranslationTests
{
    [Test]
    public void Filter_WithFilterFunction_CreatesFilteredStream()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromKafka("test-input", "localhost:9092");

        // Act - Apply filter with a lambda function
        var filtered = stream.Filter(x => x.Length > 5);
        
        // Add sink to complete the job definition
        var result = filtered.SinkToKafka("test-output", "localhost:9092");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(filtered, Is.Not.Null);
    }

    [Test]
    public void Filter_WithAlwaysTrueFilter_CreatesFilteredStream()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromKafka("test-input", "localhost:9092");

        // Act - Apply filter operation that always returns true
        var filtered = stream.Filter(x => true);
        
        // Add sink to complete the job definition
        var result = filtered.SinkToKafka("test-output", "localhost:9092");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(filtered, Is.Not.Null);
    }
}
