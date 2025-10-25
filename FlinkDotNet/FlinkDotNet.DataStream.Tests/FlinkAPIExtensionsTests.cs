using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    [TestFixture]
    public class FlinkAPIExtensionsTests
    {
        private StreamExecutionEnvironment _env = null!;

        [SetUp]
        public void Setup() => this._env = StreamExecutionEnvironment.GetExecutionEnvironment();

        #region TimeCharacteristic Tests

        [Test]
        public void TimeCharacteristic_EnumValues_ShouldBeDefined()
        {
            // Verify all time characteristic values exist
            Assert.That((int) TimeCharacteristic.ProcessingTime, Is.EqualTo(0));
            Assert.That((int) TimeCharacteristic.EventTime, Is.EqualTo(1));
            Assert.That((int) TimeCharacteristic.IngestionTime, Is.EqualTo(2));
        }

        #endregion

        #region TypeInformation Tests

        [Test]
        public void TypeInformation_Of_ShouldCreateTypeInformation()
        {
            // Act
            var typeInfo = TypeInformation<string>.Of();

            // Assert
            Assert.That(typeInfo, Is.Not.Null);
            Assert.That(typeInfo.GetType(), Is.EqualTo(typeof(string)));
        }

        [Test]
        public void TypeInformation_OfGeneric_ShouldCreateTypeInformation()
        {
            // Act
            var typeInfo = TypeInformation<int>.Of<int>();

            // Assert
            Assert.That(typeInfo, Is.Not.Null);
            Assert.That(typeInfo.GetType(), Is.EqualTo(typeof(int)));
        }

        [Test]
        public void TypeInformation_OfDifferentType_ShouldCreateCorrectTypeInformation()
        {
            // Act
            var stringTypeInfo = TypeInformation<string>.Of<string>();
            var intTypeInfo = TypeInformation<int>.Of<int>();

            // Assert
            Assert.That(stringTypeInfo.GetType(), Is.EqualTo(typeof(string)));
            Assert.That(intTypeInfo.GetType(), Is.EqualTo(typeof(int)));
        }

        #endregion

        #region KafkaSinkFunction Tests

        [Test]
        public void KafkaSinkFunction_Constructor_ShouldSetProperties()
        {
            // Arrange
            var topic = "test-topic";
            var bootstrapServers = "localhost:9092";
            Func<string, byte[]> serializer = s => System.Text.Encoding.UTF8.GetBytes(s);

            // Act
            var sink = new KafkaSinkFunction<string>(topic, bootstrapServers, serializer);

            // Assert
            Assert.That(sink.Topic, Is.EqualTo(topic));
            Assert.That(sink.BootstrapServers, Is.EqualTo(bootstrapServers));
        }

        [Test]
        public async Task KafkaSinkFunction_InvokeAsync_ShouldCompleteSuccessfully()
        {
            // Arrange
            var topic = "test-topic";
            var bootstrapServers = "localhost:9092";
            Func<string, byte[]> serializer = s => System.Text.Encoding.UTF8.GetBytes(s);
            var sink = new KafkaSinkFunction<string>(topic, bootstrapServers, serializer);

            // Act
            await sink.InvokeAsync("test-message");

            // Assert - Method completed without throwing
            Assert.Pass("InvokeAsync completed successfully");
        }

        #endregion

        #region StartingOffsets Tests

        [Test]
        public void StartingOffsets_Constants_ShouldHaveCorrectValues()
        {
            // Assert
            Assert.That(StartingOffsets.Earliest, Is.EqualTo("earliest"));
            Assert.That(StartingOffsets.Latest, Is.EqualTo("latest"));
        }

        #endregion

        #region StreamExecutionEnvironmentExtensions Tests

        [Test]
        public void SetStreamTimeCharacteristic_ProcessingTime_ShouldSetCorrectly()
        {
            // Act
            var result = this._env.SetStreamTimeCharacteristic(TimeCharacteristic.ProcessingTime);

            // Assert
            Assert.That(result, Is.SameAs(this._env));
            var config = this._env.GetConfig().GetConfiguration();
            Assert.That(config.GetString("stream.time-characteristic", null), Is.EqualTo("ProcessingTime"));
        }

        [Test]
        public void SetStreamTimeCharacteristic_EventTime_ShouldSetCorrectly()
        {
            // Act
            var result = this._env.SetStreamTimeCharacteristic(TimeCharacteristic.EventTime);

            // Assert
            Assert.That(result, Is.SameAs(this._env));
            var config = this._env.GetConfig().GetConfiguration();
            Assert.That(config.GetString("stream.time-characteristic", null), Is.EqualTo("EventTime"));
        }

        [Test]
        public void SetStreamTimeCharacteristic_IngestionTime_ShouldSetCorrectly()
        {
            // Act
            var result = this._env.SetStreamTimeCharacteristic(TimeCharacteristic.IngestionTime);

            // Assert
            Assert.That(result, Is.SameAs(this._env));
            var config = this._env.GetConfig().GetConfiguration();
            Assert.That(config.GetString("stream.time-characteristic", null), Is.EqualTo("IngestionTime"));
        }

        [Test]
        public void AddSource_WithSourceFunction_ShouldCreateDataStream()
        {
            // Arrange
            var sourceFunction = new TestSourceFunction<string>();

            // Act
            var stream = this._env.AddSource(sourceFunction);

            // Assert
            Assert.That(stream, Is.Not.Null);
            Assert.That(stream, Is.InstanceOf<DataStream<string>>());
        }

        #endregion

        #region DataStreamExtensions Tests

        [Test]
        public void DataStreamExtensions_AddSink_WithKafkaSinkFunction_ShouldReturnStream()
        {
            // Arrange
            var data = new[] { "test1", "test2", "test3" };
            var stream = this._env.FromCollection(data);
            var kafkaSink = new KafkaSinkFunction<string>(
                "test-topic",
                "localhost:9092",
                s => System.Text.Encoding.UTF8.GetBytes(s)
            );

            // Act
            var result = stream.AddSink(kafkaSink);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(stream));
        }

        #endregion

        #region KafkaSourceFunctionExtensions Tests

        [Test]
        public void KafkaSourceFunction_SetStartFromEarliest_ShouldReturnSameSource()
        {
            // Arrange
            var source = new KafkaSourceFunction<string>(
                "test-topic",
                "localhost:9092",
                "test-group",
                s => s,
                StartingOffsets.Latest
            );

            // Act
            var result = source.SetStartFromEarliest();

            // Assert
            Assert.That(result, Is.SameAs(source));
        }

        [Test]
        public void KafkaSourceFunction_AssignTimestampsAndWatermarks_WithPunctuated_ShouldReturnSameSource()
        {
            // Arrange
            var source = new KafkaSourceFunction<string>(
                "test-topic",
                "localhost:9092",
                "test-group",
                s => s,
                StartingOffsets.Earliest
            );
            var assigner = new TestPunctuatedWatermarkAssigner();

            // Act
            var result = source.AssignTimestampsAndWatermarks(assigner);

            // Assert
            Assert.That(result, Is.SameAs(source));
        }

        [Test]
        public void KafkaSourceFunction_AssignTimestampsAndWatermarks_WithPeriodic_ShouldReturnSameSource()
        {
            // Arrange
            var source = new KafkaSourceFunction<string>(
                "test-topic",
                "localhost:9092",
                "test-group",
                s => s,
                StartingOffsets.Earliest
            );
            var assigner = new TestPeriodicWatermarkAssigner();

            // Act
            var result = source.AssignTimestampsAndWatermarks(assigner);

            // Assert
            Assert.That(result, Is.SameAs(source));
        }

        #endregion

        #region Helper Classes

        private class TestSourceFunction<T> : ISourceFunction<T>
        {
            public async System.Collections.Generic.IAsyncEnumerable<T> RunAsync(
                [System.Runtime.CompilerServices.EnumeratorCancellation] System.Threading.CancellationToken cancellationToken = default)
            {
                await Task.CompletedTask;
                yield break;
            }
        }

        private class TestPunctuatedWatermarkAssigner : IAssignerWithPunctuatedWatermarks<string>
        {
            public long ExtractTimestamp(string element, long previousElementTimestamp) => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            public Watermark? CheckAndGetNextWatermark(string lastElement, long extractedTimestamp) => new Watermark(extractedTimestamp);
        }

        private class TestPeriodicWatermarkAssigner : IAssignerWithPeriodicWatermarks<string>
        {
            public long ExtractTimestamp(string element, long previousElementTimestamp) => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            public Watermark? GetCurrentWatermark() => new Watermark(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        }
        private class TestAsyncFunction : IAsyncFunction<string, int>
        {
            public async Task AsyncInvokeAsync(string input, IResultFuture<int> resultFuture)
            {
                await Task.Delay(10);
                resultFuture.Complete(new[] { input.Length });
            }
        }

        private class TestAsyncFunctionWithTimeout : IAsyncFunction<string, int>
        {
            public async Task AsyncInvokeAsync(string input, IResultFuture<int> resultFuture)
            {
                await Task.Delay(10);
                resultFuture.Complete(new[] { input.Length });
            }

            public Task TimeoutAsync(string input, IResultFuture<int> resultFuture)
            {
                resultFuture.Complete(new[] { -1 });
                return Task.CompletedTask;
            }
        }

        private class TestResultFuture<T> : IResultFuture<T>
        {
            public System.Collections.Generic.List<T> Results { get; } = new();
            public Exception? Exception
            {
                get; private set;
            }

            public void Complete(System.Collections.Generic.IEnumerable<T> results) => this.Results.AddRange(results);

            public void CompleteExceptionally(Exception exception) => this.Exception = exception;
        }

        #endregion

        #region IAsyncFunction Tests

        [Test]
        public async Task IAsyncFunction_AsyncInvokeAsync_ShouldCompleteWithResults()
        {
            // Arrange
            IAsyncFunction<string, int> asyncFunc = new TestAsyncFunction();
            var resultFuture = new TestResultFuture<int>();

            // Act
            await asyncFunc.AsyncInvokeAsync("test", resultFuture);

            // Assert
            Assert.That(resultFuture.Results.Count, Is.EqualTo(1));
            Assert.That(resultFuture.Results[0], Is.EqualTo(4));
        }

        [Test]
        public async Task IAsyncFunction_TimeoutAsync_DefaultImplementation_ShouldReturnEmptyCollection()
        {
            // Arrange
            IAsyncFunction<string, int> asyncFunc = new TestAsyncFunction();
            var resultFuture = new TestResultFuture<int>();

            // Act
            await asyncFunc.TimeoutAsync("test", resultFuture);

            // Assert
            Assert.That(resultFuture.Results.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task IAsyncFunction_TimeoutAsync_CustomImplementation_ShouldHandleTimeout()
        {
            // Arrange
            var asyncFunc = new TestAsyncFunctionWithTimeout();
            var resultFuture = new TestResultFuture<int>();

            // Act
            await asyncFunc.TimeoutAsync("test", resultFuture);

            // Assert
            Assert.That(resultFuture.Results.Count, Is.EqualTo(1));
            Assert.That(resultFuture.Results[0], Is.EqualTo(-1));
        }

        [Test]
        public void IResultFuture_Complete_ShouldStoreResults()
        {
            // Arrange
            var resultFuture = new TestResultFuture<int>();
            var results = new[] { 1, 2, 3 };

            // Act
            resultFuture.Complete(results);

            // Assert
            Assert.That(resultFuture.Results.Count, Is.EqualTo(3));
            Assert.That(resultFuture.Results, Is.EqualTo(results));
        }

        [Test]
        public void IResultFuture_CompleteExceptionally_ShouldStoreException()
        {
            // Arrange
            var resultFuture = new TestResultFuture<int>();
            var exception = new InvalidOperationException("Test exception");

            // Act
            resultFuture.CompleteExceptionally(exception);

            // Assert
            Assert.That(resultFuture.Exception, Is.Not.Null);
            Assert.That(resultFuture.Exception, Is.SameAs(exception));
            Assert.That(resultFuture.Exception!.Message, Is.EqualTo("Test exception"));
        }

        [Test]
        public async Task IAsyncFunction_MultipleInvocations_ShouldProcessAllInputs()
        {
            // Arrange
            IAsyncFunction<string, int> asyncFunc = new TestAsyncFunction();
            var inputs = new[] { "hello", "world", "test" };
            var allResults = new System.Collections.Generic.List<int>();

            // Act
            foreach (var input in inputs)
            {
                var resultFuture = new TestResultFuture<int>();
                await asyncFunc.AsyncInvokeAsync(input, resultFuture);
                allResults.AddRange(resultFuture.Results);
            }

            // Assert
            Assert.That(allResults.Count, Is.EqualTo(3));
            Assert.That(allResults[0], Is.EqualTo(5)); // "hello".Length
            Assert.That(allResults[1], Is.EqualTo(5)); // "world".Length
            Assert.That(allResults[2], Is.EqualTo(4)); // "test".Length
        }

        [Test]
        public void IResultFuture_MultipleCompletes_ShouldAccumulateResults()
        {
            // Arrange
            var resultFuture = new TestResultFuture<int>();

            // Act
            resultFuture.Complete(new[] { 1, 2 });
            resultFuture.Complete(new[] { 3, 4 });

            // Assert
            Assert.That(resultFuture.Results.Count, Is.EqualTo(4));
            Assert.That(resultFuture.Results, Is.EqualTo(new[] { 1, 2, 3, 4 }));
        }


        #endregion
    }
}
