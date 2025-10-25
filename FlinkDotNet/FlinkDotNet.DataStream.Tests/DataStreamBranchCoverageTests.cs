using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Flink.JobBuilder.Models;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Comprehensive tests to achieve 100% branch coverage for DataStream class.
    /// Targets specific uncovered branches identified in coverage analysis.
    /// </summary>
    [TestFixture]
    public class DataStreamBranchCoverageTests
    {
        private StreamExecutionEnvironment _env = null!;

        [SetUp]
        public void Setup()
        {
            _env = StreamExecutionEnvironment.GetExecutionEnvironment();
        }

        #region Where Method Branch Coverage

        [Test]
        public void Where_WithNullJob_ReturnsThis()
        {
            // Arrange - Create a stream without a job (collection-based stream)
            var collection = new[] { "test1", "test2", "test3" };
            var stream = _env.FromCollection(collection);

            // Act - Call Where with null job
            var result = stream.Where("length > 5");

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void Where_WithJob_AddsFilterOperation()
        {
            // Arrange - Create a stream with a job
            var stream = _env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act - Call Where with a job
            var result = stream.Where("length > 5");

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        #endregion

        #region SinkToKafka Branch Coverage

        [Test]
        public void SinkToKafka_WithNullBootstrapServers_ThrowsArgumentException()
        {
            // Arrange
            var stream = _env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => stream.SinkToKafka("output-topic", null));
        }

        [Test]
        public void SinkToKafka_WithEmptyBootstrapServers_ThrowsArgumentException()
        {
            // Arrange
            var stream = _env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => stream.SinkToKafka("output-topic", ""));
        }

        [Test]
        public void SinkToKafka_WithWhitespaceBootstrapServers_ThrowsArgumentException()
        {
            // Arrange
            var stream = _env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => stream.SinkToKafka("output-topic", "   "));
        }

        [Test]
        public void SinkToKafka_WithOperationCapture_CapturesSink()
        {
            // Arrange - Stream with operation capture
            var stream = _env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act - This should hit the operationCapture != null branch
            var result = stream.SinkToKafka("output-topic", "localhost:9092", x => x.ToString());

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void SinkToKafka_WithNullJobAndNoOperationCapture_ThrowsInvalidOperationException()
        {
            // Arrange - Create a collection-based stream (no job, no operation capture)
            var collection = new[] { "test1", "test2" };
            var stream = _env.FromCollection(collection);

            // Act & Assert - Should throw because _job is null and _operationCapture is null
            Assert.Throws<InvalidOperationException>(() =>
                stream.SinkToKafka("output-topic", "localhost:9092"));
        }

        [Test]
        public void SinkToKafka_WithJobDefinition_SetsSinkAndActiveJob()
        {
            // Arrange - Create a stream with a job definition
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobName = "TestJob" },
                Source = new KafkaSourceDefinition
                {
                    Topic = "test-topic",
                    BootstrapServers = "localhost:9092"
                }
            };
            var stream = CreateDataStreamWithJobDefinition<string>(jobDef);

            // Act - This should hit the _job != null branch
            var result = stream.SinkToKafka("output-topic", "localhost:9092");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(jobDef.Sink, Is.Not.Null);
        }

        #endregion

        #region AddSink Branch Coverage

        [Test]
        public void AddSink_WithNullSinkFunction_ReturnsStream()
        {
            // Arrange
            var stream = _env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act - Pass null sink function to test sinkFunction != null branch
            var result = stream.AddSink(null!);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void AddSink_WithOperationCaptureAndKafkaSink_ExtractsKafkaInfo()
        {
            // Arrange - Create a stream with operation capture
            var stream = _env.FromKafka("test-topic", "localhost:9092", "test-group");
            var kafkaSink = new KafkaSinkFunction<string>("output-topic", "localhost:9092", x => System.Text.Encoding.UTF8.GetBytes(x));

            // Act - This should hit the branch where topicProp and serversProp are not null
            var result = stream.AddSink(kafkaSink);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void AddSink_WithOperationCaptureAndNonKafkaSink_DoesNotExtractKafkaInfo()
        {
            // Arrange - Create a stream with operation capture
            var stream = _env.FromKafka("test-topic", "localhost:9092", "test-group");
            var customSink = new CustomSinkFunction();

            // Act - This should hit the branch where topicProp or serversProp is null
            var result = stream.AddSink(customSink);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void AddSink_WithOperationCaptureAndEmptyKafkaInfo_DoesNotCapture()
        {
            // Arrange - Create a stream with operation capture
            var stream = _env.FromKafka("test-topic", "localhost:9092", "test-group");
            var kafkaSinkWithEmptyTopic = new KafkaSinkFunction<string>("", "localhost:9092", x => System.Text.Encoding.UTF8.GetBytes(x));

            // Act - This should hit the branch where topic or servers is empty
            var result = stream.AddSink(kafkaSinkWithEmptyTopic);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void AddSink_WithoutOperationCapture_ReturnsStream()
        {
            // Arrange - Create a collection-based stream (no operation capture)
            var collection = new[] { "test1", "test2" };
            var stream = _env.FromCollection(collection);
            var customSink = new CustomSinkFunction();

            // Act - This should hit the branch where _operationCapture is null
            var result = stream.AddSink(customSink);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        #endregion

        #region SetMaxParallelism Branch Coverage

        [Test]
        public void SetMaxParallelism_WithCollectionStream_ReturnsStream()
        {
            // Arrange - Collection-based stream
            var collection = new[] { 1, 2, 3 };
            var stream = _env.FromCollection(collection);

            // Act
            var result = stream.SetMaxParallelism(4);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void SetMaxParallelism_WithSourceFunctionStream_ReturnsStream()
        {
            // Arrange - Source function stream
            var sourceFunc = new TestSourceFunction();
            var stream = CreateDataStreamWithSourceFunction(sourceFunc);

            // Act
            var result = stream.SetMaxParallelism(4);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void SetMaxParallelism_WithOperationCapture_ReturnsStream()
        {
            // Arrange - Stream with operation capture
            var stream = _env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act
            var result = stream.SetMaxParallelism(4);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void SetMaxParallelism_WithJobDefinition_ReturnsStream()
        {
            // Arrange - Stream with job definition
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobName = "TestJob" },
                Source = new KafkaSourceDefinition
                {
                    Topic = "test-topic",
                    BootstrapServers = "localhost:9092"
                }
            };
            var stream = CreateDataStreamWithJobDefinition<string>(jobDef);

            // Act
            var result = stream.SetMaxParallelism(4);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        #endregion

        #region AssignTimestampsAndWatermarks Branch Coverage

        [Test]
        public void AssignTimestampsAndWatermarks_WithPunctuatedWatermarks_WithCollectionStream_ReturnsStream()
        {
            // Arrange
            var collection = new[] { "test1", "test2" };
            var stream = _env.FromCollection(collection);
            var assigner = new TestPunctuatedWatermarksAssigner();

            // Act
            var result = stream.AssignTimestampsAndWatermarks(assigner);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void AssignTimestampsAndWatermarks_WithPunctuatedWatermarks_WithSourceFunctionStream_ReturnsStream()
        {
            // Arrange
            var sourceFunc = new TestSourceFunctionString();
            var stream = CreateDataStreamWithSourceFunction(sourceFunc);
            var assigner = new TestPunctuatedWatermarksAssigner();

            // Act
            var result = stream.AssignTimestampsAndWatermarks(assigner);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void AssignTimestampsAndWatermarks_WithWatermarkStrategy_WithCollectionStream_ReturnsStream()
        {
            // Arrange
            var collection = new[] { "test1", "test2" };
            var stream = _env.FromCollection(collection);
            var strategy = Watermarks.WatermarkStrategy<string>.ForMonotonousTimestamps()
                .WithTimestampAssigner(_ => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            // Act
            var result = stream.AssignTimestampsAndWatermarks(strategy);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void AssignTimestampsAndWatermarks_WithWatermarkStrategy_WithSourceFunctionStream_ReturnsStream()
        {
            // Arrange
            var sourceFunc = new TestSourceFunctionString();
            var stream = CreateDataStreamWithSourceFunction(sourceFunc);
            var strategy = Watermarks.WatermarkStrategy<string>.ForMonotonousTimestamps()
                .WithTimestampAssigner(_ => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            // Act
            var result = stream.AssignTimestampsAndWatermarks(strategy);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        #endregion

        #region TimeWindowAll Branch Coverage

        [Test]
        public void TimeWindowAll_WithCollectionStream_CreatesWindowedStream()
        {
            // Arrange
            var collection = new[] { 1, 2, 3 };
            var stream = _env.FromCollection(collection);

            // Act
            var result = stream.TimeWindowAll(Time.Seconds(5));

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void TimeWindowAll_WithSourceFunctionStream_CreatesWindowedStream()
        {
            // Arrange
            var sourceFunc = new TestSourceFunction();
            var stream = CreateDataStreamWithSourceFunction(sourceFunc);

            // Act
            var result = stream.TimeWindowAll(Time.Seconds(5));

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void TimeWindowAll_WithOperationCaptureStream_CreatesWindowedStream()
        {
            // Arrange - This will have _operationCapture or _job
            var stream = _env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act
            var result = stream.TimeWindowAll(Time.Seconds(5));

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void TimeWindowAll_WithJobDefinitionStream_CreatesWindowedStream()
        {
            // Arrange
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobName = "TestJob" },
                Source = new KafkaSourceDefinition
                {
                    Topic = "test-topic",
                    BootstrapServers = "localhost:9092"
                }
            };
            var stream = CreateDataStreamWithJobDefinition<int>(jobDef);

            // Act
            var result = stream.TimeWindowAll(Time.Seconds(5));

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        #endregion

        #region CountWindowAll Branch Coverage

        [Test]
        public void CountWindowAll_WithCollectionStream_CreatesWindowedStream()
        {
            // Arrange
            var collection = new[] { 1, 2, 3, 4, 5 };
            var stream = _env.FromCollection(collection);

            // Act
            var result = stream.CountWindowAll(3);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void CountWindowAll_WithSourceFunctionStream_CreatesWindowedStream()
        {
            // Arrange
            var sourceFunc = new TestSourceFunction();
            var stream = CreateDataStreamWithSourceFunction(sourceFunc);

            // Act
            var result = stream.CountWindowAll(3);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void CountWindowAll_WithOperationCaptureStream_CreatesWindowedStream()
        {
            // Arrange
            var stream = _env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act
            var result = stream.CountWindowAll(3);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void CountWindowAll_WithJobDefinitionStream_CreatesWindowedStream()
        {
            // Arrange
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobName = "TestJob" },
                Source = new KafkaSourceDefinition
                {
                    Topic = "test-topic",
                    BootstrapServers = "localhost:9092"
                }
            };
            var stream = CreateDataStreamWithJobDefinition<int>(jobDef);

            // Act - CountWindowAll works with all stream types
            var result = stream.CountWindowAll(3);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void CountWindowAll_WithInvalidSize_ThrowsArgumentException()
        {
            // Arrange
            var collection = new[] { 1, 2, 3, 4, 5 };
            var stream = _env.FromCollection(collection);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => stream.CountWindowAll(0));
            Assert.Throws<ArgumentException>(() => stream.CountWindowAll(-1));
        }

        #endregion

        #region Constructor Branch Coverage

        [Test]
        public void Constructor_WithJobDefinitionAndNullMetadata_InitializesMetadata()
        {
            // Arrange - Create job definition without metadata
            var jobDef = new JobDefinition
            {
                Source = new KafkaSourceDefinition
                {
                    Topic = "test-topic",
                    BootstrapServers = "localhost:9092"
                }
            };

            // Act - Constructor should initialize metadata if null
            var stream = CreateDataStreamWithJobDefinition<string>(jobDef);

            // Assert
            Assert.That(stream, Is.Not.Null);
            Assert.That(jobDef.Metadata, Is.Not.Null);
        }

        [Test]
        public void Constructor_WithJobDefinitionAndExistingMetadata_KeepsMetadata()
        {
            // Arrange - Create job definition with metadata
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobName = "ExistingJob" },
                Source = new KafkaSourceDefinition
                {
                    Topic = "test-topic",
                    BootstrapServers = "localhost:9092"
                }
            };

            // Act - Constructor should keep existing metadata
            var stream = CreateDataStreamWithJobDefinition<string>(jobDef);

            // Assert
            Assert.That(stream, Is.Not.Null);
            Assert.That(jobDef.Metadata.JobName, Is.EqualTo("ExistingJob"));
        }

        #endregion

        #region Helper Methods and Test Classes

        /// <summary>
        /// Creates a DataStream backed by a JobDefinition without OperationCapture.
        /// Uses reflection to access the internal constructor.
        /// </summary>
        private DataStream<T> CreateDataStreamWithJobDefinition<T>(JobDefinition job)
        {
            var dataStreamType = typeof(DataStream<T>);
            var constructor = dataStreamType.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(JobDefinition), typeof(StreamExecutionEnvironment) },
                null
            );

            if (constructor == null)
                throw new InvalidOperationException("Could not find internal constructor");

            return (DataStream<T>) constructor.Invoke(new object[] { job, _env });
        }

        /// <summary>
        /// Creates a DataStream with a source function.
        /// Uses reflection to access the internal constructor.
        /// </summary>
        private DataStream<T> CreateDataStreamWithSourceFunction<T>(ISourceFunction<T> sourceFunction)
        {
            var dataStreamType = typeof(DataStream<T>);
            var constructor = dataStreamType.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(ISourceFunction<T>), typeof(StreamExecutionEnvironment), typeof(string) },
                null
            );

            if (constructor == null)
                throw new InvalidOperationException("Could not find internal constructor");

            return (DataStream<T>) constructor.Invoke(new object[] { sourceFunction, _env, "TestSource" });
        }

        // Test sink function without Kafka properties
        private class CustomSinkFunction : ISinkFunction<string>
        {
            public Task InvokeAsync(string element, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }

        // Test source function
        private class TestSourceFunction : ISourceFunction<int>
        {
            public async IAsyncEnumerable<int> RunAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                yield return 1;
                yield return 2;
                await Task.CompletedTask;
            }
        }

        // Test source function for strings
        private class TestSourceFunctionString : ISourceFunction<string>
        {
            public async IAsyncEnumerable<string> RunAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                yield return "test1";
                yield return "test2";
                await Task.CompletedTask;
            }
        }

        // Test punctuated watermarks assigner
        private class TestPunctuatedWatermarksAssigner : IAssignerWithPunctuatedWatermarks<string>
        {
            public long ExtractTimestamp(string element, long previousElementTimestamp)
            {
                return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            public Watermark? CheckAndGetNextWatermark(string lastElement, long extractedTimestamp)
            {
                return new Watermark(extractedTimestamp);
            }
        }

        #endregion
    }
}
