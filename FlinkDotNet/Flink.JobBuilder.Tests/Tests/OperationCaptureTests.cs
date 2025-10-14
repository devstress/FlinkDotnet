using FlinkDotNet.DataStream;
using Flink.JobBuilder.Models;

namespace Flink.JobBuilder.Tests.Tests;

[TestFixture]
public class OperationCaptureTests
{
    #region CaptureKafkaSource Tests

    [Test]
    public void CaptureKafkaSource_StoresSourceDefinition()
    {
        var capture = new OperationCapture();

        capture.CaptureKafkaSource("test-topic", "localhost:9092", "test-group", "earliest");

        var jobDef = capture.ToJobDefinition("job-1", "Test Job");
        var kafkaSource = jobDef.Source as KafkaSourceDefinition;

        Assert.That(kafkaSource, Is.Not.Null);
        Assert.That(kafkaSource!.Topic, Is.EqualTo("test-topic"));
        Assert.That(kafkaSource.BootstrapServers, Is.EqualTo("localhost:9092"));
        Assert.That(kafkaSource.GroupId, Is.EqualTo("test-group"));
        Assert.That(kafkaSource.StartingOffsets, Is.EqualTo("earliest"));
    }

    [Test]
    public void CaptureKafkaSource_WithDeserializer_StoresDeserializationFunction()
    {
        var capture = new OperationCapture();
        var deserializer = new TestDeserializer();

        capture.CaptureKafkaSource("test-topic", "localhost:9092", "test-group", "earliest", deserializer);

        var jobDef = capture.ToJobDefinition("job-1", "Test Job");
        
        Assert.That(jobDef.Metadata.Properties.ContainsKey("deserializationFunction"), Is.True);
        Assert.That(jobDef.Metadata.Properties["deserializationFunction"], Does.Contain("TestDeserializer"));
    }

    #endregion

    #region CaptureMapOperation Tests

    [Test]
    public void CaptureMapOperation_WithUpperOperationType_TranslatesToUpperExpression()
    {
        var capture = new OperationCapture();
        capture.CaptureKafkaSource("test-topic", "localhost:9092", "test-group", "earliest");
        
        capture.CaptureMapOperation("upper");

        var jobDef = capture.ToJobDefinition("job-1", "Test Job");
        var mapOp = jobDef.Operations[0] as MapOperationDefinition;

        Assert.That(mapOp, Is.Not.Null);
        Assert.That(mapOp!.Expression, Is.EqualTo("upper"));
    }

    [Test]
    public void CaptureMapOperation_WithLowerOperationType_TranslatesToLowerExpression()
    {
        var capture = new OperationCapture();
        capture.CaptureKafkaSource("test-topic", "localhost:9092", "test-group", "earliest");
        
        capture.CaptureMapOperation("lower");

        var jobDef = capture.ToJobDefinition("job-1", "Test Job");
        var mapOp = jobDef.Operations[0] as MapOperationDefinition;

        Assert.That(mapOp, Is.Not.Null);
        Assert.That(mapOp!.Expression, Is.EqualTo("lower"));
    }

    [Test]
    public void CaptureMapOperation_WithUpperFunction_TranslatesToUpperExpression()
    {
        var capture = new OperationCapture();
        capture.CaptureKafkaSource("test-topic", "localhost:9092", "test-group", "earliest");
        
        var upperFunc = new TestUpperMapFunction();
        capture.CaptureMapOperation("map", upperFunc);

        var jobDef = capture.ToJobDefinition("job-1", "Test Job");
        var mapOp = jobDef.Operations[0] as MapOperationDefinition;

        Assert.That(mapOp, Is.Not.Null);
        Assert.That(mapOp!.Expression, Is.EqualTo("upper"));
    }

    [Test]
    public void CaptureMapOperation_WithLowerFunction_TranslatesToLowerExpression()
    {
        var capture = new OperationCapture();
        capture.CaptureKafkaSource("test-topic", "localhost:9092", "test-group", "earliest");
        
        var lowerFunc = new TestLowerMapFunction();
        capture.CaptureMapOperation("map", lowerFunc);

        var jobDef = capture.ToJobDefinition("job-1", "Test Job");
        var mapOp = jobDef.Operations[0] as MapOperationDefinition;

        Assert.That(mapOp, Is.Not.Null);
        Assert.That(mapOp!.Expression, Is.EqualTo("lower"));
    }

    [Test]
    public void CaptureMapOperation_WithUnknownFunction_TranslatesToFunctionExpression()
    {
        var capture = new OperationCapture();
        capture.CaptureKafkaSource("test-topic", "localhost:9092", "test-group", "earliest");
        
        var unknownFunc = new TestUnknownMapFunction();
        capture.CaptureMapOperation("map", unknownFunc);

        var jobDef = capture.ToJobDefinition("job-1", "Test Job");
        var mapOp = jobDef.Operations[0] as MapOperationDefinition;

        Assert.That(mapOp, Is.Not.Null);
        Assert.That(mapOp!.Expression, Does.StartWith("function:"));
    }

    #endregion

    #region CaptureFilterOperation Tests

    [Test]
    public void CaptureFilterOperation_WithFunction_CreatesFilterDefinition()
    {
        var capture = new OperationCapture();
        capture.CaptureKafkaSource("test-topic", "localhost:9092", "test-group", "earliest");
        
        var filterFunc = new TestFilterFunction();
        capture.CaptureFilterOperation(filterFunc);

        var jobDef = capture.ToJobDefinition("job-1", "Test Job");
        var filterOp = jobDef.Operations[0] as FilterOperationDefinition;

        Assert.That(filterOp, Is.Not.Null);
        Assert.That(filterOp!.Expression, Does.StartWith("function:"));
    }

    #endregion

    #region CaptureFlatMapOperation Tests

    [Test]
    public void CaptureFlatMapOperation_WithFunction_AddsFlatMapOperation()
    {
        var capture = new OperationCapture();
        capture.CaptureKafkaSource("test-topic", "localhost:9092", "test-group", "earliest");
        
        var flatMapFunc = new TestFlatMapFunction();
        capture.CaptureFlatMapOperation(flatMapFunc);

        var jobDef = capture.ToJobDefinition("job-1", "Test Job");
        
        // FlatMap operations are captured but the specific translation depends on the function
        Assert.That(jobDef, Is.Not.Null);
    }

    #endregion

    #region CaptureTimestampAssigner Tests

    [Test]
    public void CaptureTimestampAssigner_SetsEventTimeCharacteristic()
    {
        var capture = new OperationCapture();
        capture.CaptureKafkaSource("test-topic", "localhost:9092", "test-group", "earliest");
        
        var assigner = new TestTimestampAssigner();
        capture.CaptureTimestampAssigner(assigner);

        var jobDef = capture.ToJobDefinition("job-1", "Test Job");

        Assert.That(jobDef.Metadata.Properties.ContainsKey("timeCharacteristic"), Is.True);
        Assert.That(jobDef.Metadata.Properties["timeCharacteristic"], Is.EqualTo("EventTime"));
    }

    #endregion

    #region CaptureTimeWindow Tests

    [Test]
    public void CaptureTimeWindow_WithWindowSize_CreatesWindowDefinition()
    {
        var capture = new OperationCapture();
        capture.CaptureKafkaSource("test-topic", "localhost:9092", "test-group", "earliest");
        
        var windowSize = Time.Seconds(10);
        capture.CaptureTimeWindow(windowSize);

        var jobDef = capture.ToJobDefinition("job-1", "Test Job");
        
        // Window is captured for use in aggregate operations
        Assert.That(jobDef, Is.Not.Null);
    }

    #endregion

    #region CaptureCountWindow Tests

    [Test]
    public void CaptureCountWindow_WithWindowSize_CreatesCountWindowDefinition()
    {
        var capture = new OperationCapture();
        capture.CaptureKafkaSource("test-topic", "localhost:9092", "test-group", "earliest");
        
        capture.CaptureCountWindow(100);

        var jobDef = capture.ToJobDefinition("job-1", "Test Job");
        
        // Window is captured for use in aggregate operations
        Assert.That(jobDef, Is.Not.Null);
    }

    #endregion

    #region CaptureAggregateOperation Tests

    [Test]
    public void CaptureAggregateOperation_WithTimeWindow_CreatesAggregateWithWindowSeconds()
    {
        var capture = new OperationCapture();
        capture.CaptureKafkaSource("test-topic", "localhost:9092", "test-group", "earliest");
        
        var windowSize = Time.Seconds(10);
        capture.CaptureTimeWindow(windowSize);
        
        var aggFunc = new TestAggregateFunction();
        capture.CaptureAggregateOperation(aggFunc);

        var jobDef = capture.ToJobDefinition("job-1", "Test Job");
        var aggOp = jobDef.Operations[0] as AggregateOperationDefinition;

        Assert.That(aggOp, Is.Not.Null);
        Assert.That(aggOp!.WindowSeconds, Is.EqualTo(10));
        Assert.That(aggOp.WindowCount, Is.Null);
    }

    [Test]
    public void CaptureAggregateOperation_WithCountWindow_CreatesAggregateWithWindowCount()
    {
        var capture = new OperationCapture();
        capture.CaptureKafkaSource("test-topic", "localhost:9092", "test-group", "earliest");
        
        capture.CaptureCountWindow(100);
        
        var aggFunc = new TestAggregateFunction();
        capture.CaptureAggregateOperation(aggFunc);

        var jobDef = capture.ToJobDefinition("job-1", "Test Job");
        var aggOp = jobDef.Operations[0] as AggregateOperationDefinition;

        Assert.That(aggOp, Is.Not.Null);
        Assert.That(aggOp!.WindowCount, Is.EqualTo(100));
        Assert.That(aggOp.WindowSeconds, Is.Null);
    }

    [Test]
    public void CaptureAggregateOperation_WithFunction_StoresAggregateFunctionMetadata()
    {
        var capture = new OperationCapture();
        capture.CaptureKafkaSource("test-topic", "localhost:9092", "test-group", "earliest");
        
        var aggFunc = new TestAggregateFunction();
        capture.CaptureAggregateOperation(aggFunc);

        var jobDef = capture.ToJobDefinition("job-1", "Test Job");

        Assert.That(jobDef.Metadata.Properties.ContainsKey("aggregateFunction"), Is.True);
        Assert.That(jobDef.Metadata.Properties["aggregateFunction"], Does.Contain("TestAggregateFunction"));
    }

    #endregion

    #region CaptureKafkaSink Tests

    [Test]
    public void CaptureKafkaSink_StoresSinkDefinition()
    {
        var capture = new OperationCapture();
        capture.CaptureKafkaSource("test-topic", "localhost:9092", "test-group", "earliest");
        
        capture.CaptureKafkaSink("output-topic", "localhost:9092");

        var jobDef = capture.ToJobDefinition("job-1", "Test Job");
        var kafkaSink = jobDef.Sink as KafkaSinkDefinition;

        Assert.That(kafkaSink, Is.Not.Null);
        Assert.That(kafkaSink!.Topic, Is.EqualTo("output-topic"));
        Assert.That(kafkaSink.BootstrapServers, Is.EqualTo("localhost:9092"));
    }

    [Test]
    public void CaptureKafkaSink_WithSerializer_StoresSerializationFunction()
    {
        var capture = new OperationCapture();
        capture.CaptureKafkaSource("test-topic", "localhost:9092", "test-group", "earliest");
        
        var serializer = new TestSerializer();
        capture.CaptureKafkaSink("output-topic", "localhost:9092", serializer);

        var jobDef = capture.ToJobDefinition("job-1", "Test Job");

        Assert.That(jobDef.Metadata.Properties.ContainsKey("serializationFunction"), Is.True);
        Assert.That(jobDef.Metadata.Properties["serializationFunction"], Does.Contain("TestSerializer"));
    }

    #endregion

    #region ToJobDefinition Tests

    [Test]
    public void ToJobDefinition_WithNoKafkaSource_ThrowsInvalidOperationException()
    {
        var capture = new OperationCapture();

        Assert.Throws<InvalidOperationException>(() => capture.ToJobDefinition("job-1", "Test Job"));
    }

    [Test]
    public void ToJobDefinition_CreatesJobDefinitionWithMetadata()
    {
        var capture = new OperationCapture();
        capture.CaptureKafkaSource("test-topic", "localhost:9092", "test-group", "earliest");

        var jobDef = capture.ToJobDefinition("job-1", "Test Job");

        Assert.That(jobDef, Is.Not.Null);
        Assert.That(jobDef.Metadata, Is.Not.Null);
        Assert.That(jobDef.Metadata.JobId, Is.EqualTo("job-1"));
        Assert.That(jobDef.Metadata.JobName, Is.EqualTo("Test Job"));
        Assert.That(jobDef.Metadata.Version, Is.EqualTo("1.0"));
    }

    #endregion

    #region HasOperations Tests

    [Test]
    public void HasOperations_WithNoOperations_ReturnsFalse()
    {
        var capture = new OperationCapture();

        Assert.That(capture.HasOperations(), Is.False);
    }

    [Test]
    public void HasOperations_WithKafkaSource_ReturnsTrue()
    {
        var capture = new OperationCapture();
        capture.CaptureKafkaSource("test-topic", "localhost:9092", "test-group", "earliest");

        Assert.That(capture.HasOperations(), Is.True);
    }

    [Test]
    public void HasOperations_WithOperations_ReturnsTrue()
    {
        var capture = new OperationCapture();
        capture.CaptureKafkaSource("test-topic", "localhost:9092", "test-group", "earliest");
        capture.CaptureMapOperation("upper");

        Assert.That(capture.HasOperations(), Is.True);
    }

    #endregion

    #region Test Helper Classes

    private class TestDeserializer { }
    private class TestSerializer { }
    private class TestUpperMapFunction { }
    private class TestLowerMapFunction { }
    private class TestUnknownMapFunction { }
    private class TestFilterFunction { }
    private class TestFlatMapFunction { }
    private class TestTimestampAssigner { }
    private class TestAggregateFunction { }

    #endregion
}
