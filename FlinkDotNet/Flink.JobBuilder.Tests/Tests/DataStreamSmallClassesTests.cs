using NUnit.Framework;
using FlinkDotNet.DataStream;

namespace Flink.JobBuilder.Tests.Tests
{
    /// <summary>
    /// Tests for small DataStream classes to achieve 100% coverage
    /// Chunk 1: CapturedOperation, WindowDefinition, JobExecutionResult, SavepointResult, 
    /// StopWithSavepointResult, JobStatus, JobClient
    /// Chunk 1A: Core Infrastructure Classes - CapturedOperation, WindowDefinition, DataStreamExtensions, IAsyncFunction
    /// </summary>
    [TestFixture]
    public class DataStreamSmallClassesTests
    {
        #region CapturedOperation Tests (Chunk 1A)

        [Test]
        public void CapturedOperation_DefaultConstructor_InitializesProperties()
        {
            // Act
            var operation = new CapturedOperation();

            // Assert
            Assert.That(operation.Type, Is.EqualTo(string.Empty));
            Assert.That(operation.OperationType, Is.Null);
            Assert.That(operation.Function, Is.Null);
        }

        [Test]
        public void CapturedOperation_SetType_StoresValue()
        {
            // Arrange
            var operation = new CapturedOperation();

            // Act
            operation.Type = "Map";

            // Assert
            Assert.That(operation.Type, Is.EqualTo("Map"));
        }

        [Test]
        public void CapturedOperation_SetOperationType_StoresValue()
        {
            // Arrange
            var operation = new CapturedOperation();

            // Act
            operation.OperationType = "upper";

            // Assert
            Assert.That(operation.OperationType, Is.EqualTo("upper"));
        }

        [Test]
        public void CapturedOperation_SetOperationTypeNull_StoresNull()
        {
            // Arrange
            var operation = new CapturedOperation
            {
                OperationType = "upper"
            };

            // Act
            operation.OperationType = null;

            // Assert
            Assert.That(operation.OperationType, Is.Null);
        }

        [Test]
        public void CapturedOperation_SetFunction_StoresValue()
        {
            // Arrange
            var operation = new CapturedOperation();
            var function = new TestMapFunction();

            // Act
            operation.Function = function;

            // Assert
            Assert.That(operation.Function, Is.EqualTo(function));
        }

        [Test]
        public void CapturedOperation_SetFunctionNull_StoresNull()
        {
            // Arrange
            var operation = new CapturedOperation
            {
                Function = new TestMapFunction()
            };

            // Act
            operation.Function = null;

            // Assert
            Assert.That(operation.Function, Is.Null);
        }

        [Test]
        public void CapturedOperation_SetAllProperties_StoresAllValues()
        {
            // Arrange
            var operation = new CapturedOperation();
            var function = new TestMapFunction();

            // Act
            operation.Type = "Filter";
            operation.OperationType = "custom";
            operation.Function = function;

            // Assert
            Assert.That(operation.Type, Is.EqualTo("Filter"));
            Assert.That(operation.OperationType, Is.EqualTo("custom"));
            Assert.That(operation.Function, Is.EqualTo(function));
        }

        [Test]
        public void CapturedOperation_TypePropertyGetterAndSetter_WorkCorrectly()
        {
            // Arrange
            var operation = new CapturedOperation();

            // Act & Assert - Multiple assignments
            operation.Type = "Map";
            Assert.That(operation.Type, Is.EqualTo("Map"));

            operation.Type = "FlatMap";
            Assert.That(operation.Type, Is.EqualTo("FlatMap"));

            operation.Type = "Aggregate";
            Assert.That(operation.Type, Is.EqualTo("Aggregate"));
        }

        [Test]
        public void CapturedOperation_FunctionPropertyWithDifferentTypes_StoresCorrectly()
        {
            // Arrange
            var operation = new CapturedOperation();

            // Act & Assert - Store a string
            operation.Function = "test string";
            Assert.That(operation.Function, Is.EqualTo("test string"));

            // Act & Assert - Store an integer
            operation.Function = 42;
            Assert.That(operation.Function, Is.EqualTo(42));

            // Act & Assert - Store a lambda-like object
            Func<int, int> lambda = x => x * 2;
            operation.Function = lambda;
            Assert.That(operation.Function, Is.EqualTo(lambda));
        }

        [Test]
        public void CapturedOperation_OperationTypePropertyNull_HandlesCorrectly()
        {
            // Arrange & Act
            var operation = new CapturedOperation
            {
                Type = "Map",
                OperationType = null,
                Function = "test"
            };

            // Assert
            Assert.That(operation.Type, Is.EqualTo("Map"));
            Assert.That(operation.OperationType, Is.Null);
            Assert.That(operation.Function, Is.EqualTo("test"));
        }

        // Helper class for testing - intentionally simple to test type handling
        private class TestMapFunction
        {
        }

        #endregion

        #region WindowDefinition Tests (Chunk 1A)

        [Test]
        public void WindowDefinition_DefaultConstructor_InitializesProperties()
        {
            // Act
            var window = new WindowDefinition();

            // Assert
            Assert.That(window.WindowType, Is.EqualTo(string.Empty));
            Assert.That(window.Size, Is.EqualTo(0));
            Assert.That(window.TimeUnit, Is.EqualTo(string.Empty));
            Assert.That(window.IsCountBased, Is.False);
        }

        [Test]
        public void WindowDefinition_SetWindowType_StoresValue()
        {
            // Arrange
            var window = new WindowDefinition();

            // Act
            window.WindowType = "TUMBLING";

            // Assert
            Assert.That(window.WindowType, Is.EqualTo("TUMBLING"));
        }

        [Test]
        public void WindowDefinition_SetSize_StoresValue()
        {
            // Arrange
            var window = new WindowDefinition();

            // Act
            window.Size = 5000;

            // Assert
            Assert.That(window.Size, Is.EqualTo(5000));
        }

        [Test]
        public void WindowDefinition_SetTimeUnit_StoresValue()
        {
            // Arrange
            var window = new WindowDefinition();

            // Act
            window.TimeUnit = "MILLISECONDS";

            // Assert
            Assert.That(window.TimeUnit, Is.EqualTo("MILLISECONDS"));
        }

        [Test]
        public void WindowDefinition_SetIsCountBased_StoresValue()
        {
            // Arrange
            var window = new WindowDefinition();

            // Act
            window.IsCountBased = true;

            // Assert
            Assert.That(window.IsCountBased, Is.True);
        }

        [Test]
        public void WindowDefinition_SetAllPropertiesForTimeWindow_StoresAllValues()
        {
            // Arrange
            var window = new WindowDefinition();

            // Act
            window.WindowType = "TUMBLING";
            window.Size = 30000;
            window.TimeUnit = "MILLISECONDS";
            window.IsCountBased = false;

            // Assert
            Assert.That(window.WindowType, Is.EqualTo("TUMBLING"));
            Assert.That(window.Size, Is.EqualTo(30000));
            Assert.That(window.TimeUnit, Is.EqualTo("MILLISECONDS"));
            Assert.That(window.IsCountBased, Is.False);
        }

        [Test]
        public void WindowDefinition_SetAllPropertiesForCountWindow_StoresAllValues()
        {
            // Arrange
            var window = new WindowDefinition();

            // Act
            window.WindowType = "TUMBLING";
            window.Size = 100;
            window.TimeUnit = "COUNT";
            window.IsCountBased = true;

            // Assert
            Assert.That(window.WindowType, Is.EqualTo("TUMBLING"));
            Assert.That(window.Size, Is.EqualTo(100));
            Assert.That(window.TimeUnit, Is.EqualTo("COUNT"));
            Assert.That(window.IsCountBased, Is.True);
        }

        [Test]
        public void WindowDefinition_SlidingWindow_StoresCorrectly()
        {
            // Arrange
            var window = new WindowDefinition();

            // Act
            window.WindowType = "SLIDING";
            window.Size = 60000;
            window.TimeUnit = "MILLISECONDS";
            window.IsCountBased = false;

            // Assert
            Assert.That(window.WindowType, Is.EqualTo("SLIDING"));
            Assert.That(window.Size, Is.EqualTo(60000));
        }

        [Test]
        public void WindowDefinition_SessionWindow_StoresCorrectly()
        {
            // Arrange
            var window = new WindowDefinition();

            // Act
            window.WindowType = "SESSION";
            window.Size = 10000;
            window.TimeUnit = "MILLISECONDS";
            window.IsCountBased = false;

            // Assert
            Assert.That(window.WindowType, Is.EqualTo("SESSION"));
            Assert.That(window.Size, Is.EqualTo(10000));
            Assert.That(window.IsCountBased, Is.False);
        }

        [Test]
        public void WindowDefinition_LargeSize_HandlesCorrectly()
        {
            // Arrange
            var window = new WindowDefinition();

            // Act
            window.Size = long.MaxValue;

            // Assert
            Assert.That(window.Size, Is.EqualTo(long.MaxValue));
        }

        #endregion

        #region JobExecutionResult Tests

        [Test]
        public void JobExecutionResult_DefaultConstructor_InitializesProperties()
        {
            // Act
            var result = new JobExecutionResult();

            // Assert
            Assert.That(result.JobId, Is.EqualTo(string.Empty));
            Assert.That(result.JobName, Is.EqualTo(string.Empty));
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Null);
        }

        [Test]
        public void JobExecutionResult_SetProperties_StoresValues()
        {
            // Arrange
            var result = new JobExecutionResult();
            var startTime = System.DateTime.UtcNow.AddMinutes(-5);
            var endTime = System.DateTime.UtcNow;

            // Act
            result.JobId = "job-123";
            result.JobName = "Test Job";
            result.Success = true;
            result.StartTime = startTime;
            result.EndTime = endTime;
            result.Error = "Test error";

            // Assert
            Assert.That(result.JobId, Is.EqualTo("job-123"));
            Assert.That(result.JobName, Is.EqualTo("Test Job"));
            Assert.That(result.Success, Is.True);
            Assert.That(result.StartTime, Is.EqualTo(startTime));
            Assert.That(result.EndTime, Is.EqualTo(endTime));
            Assert.That(result.Error, Is.EqualTo("Test error"));
        }

        #endregion

        #region SavepointResult Tests

        [Test]
        public void SavepointResult_DefaultConstructor_InitializesProperties()
        {
            // Act
            var result = new SavepointResult();

            // Assert
            Assert.That(result.SavepointPath, Is.EqualTo(string.Empty));
            Assert.That(result.Success, Is.False);
            Assert.That(result.TriggerId, Is.EqualTo(string.Empty));
            Assert.That(result.Error, Is.Null);
        }

        [Test]
        public void SavepointResult_SetProperties_StoresValues()
        {
            // Arrange
            var result = new SavepointResult();

            // Act
            result.SavepointPath = "/path/to/savepoint";
            result.Success = true;
            result.TriggerId = "trigger-456";
            result.Error = "Test error";

            // Assert
            Assert.That(result.SavepointPath, Is.EqualTo("/path/to/savepoint"));
            Assert.That(result.Success, Is.True);
            Assert.That(result.TriggerId, Is.EqualTo("trigger-456"));
            Assert.That(result.Error, Is.EqualTo("Test error"));
        }

        #endregion

        #region StopWithSavepointResult Tests

        [Test]
        public void StopWithSavepointResult_DefaultConstructor_InitializesProperties()
        {
            // Act
            var result = new StopWithSavepointResult();

            // Assert
            Assert.That(result.SavepointPath, Is.EqualTo(string.Empty));
            Assert.That(result.Success, Is.False);
            Assert.That(result.TriggerId, Is.EqualTo(string.Empty));
            Assert.That(result.Drained, Is.False);
            Assert.That(result.Error, Is.Null);
        }

        [Test]
        public void StopWithSavepointResult_SetProperties_StoresValues()
        {
            // Arrange
            var result = new StopWithSavepointResult();

            // Act
            result.SavepointPath = "/path/to/stop/savepoint";
            result.Success = true;
            result.TriggerId = "trigger-789";
            result.Drained = true;
            result.Error = "Stop error";

            // Assert
            Assert.That(result.SavepointPath, Is.EqualTo("/path/to/stop/savepoint"));
            Assert.That(result.Success, Is.True);
            Assert.That(result.TriggerId, Is.EqualTo("trigger-789"));
            Assert.That(result.Drained, Is.True);
            Assert.That(result.Error, Is.EqualTo("Stop error"));
        }

        #endregion

        #region JobStatus Tests

        [Test]
        public void JobStatus_DefaultConstructor_InitializesProperties()
        {
            // Act
            var status = new JobStatus();

            // Assert
            Assert.That(status.JobId, Is.EqualTo(string.Empty));
            Assert.That(status.JobName, Is.EqualTo(string.Empty));
            Assert.That(status.State, Is.EqualTo(string.Empty));
            Assert.That(status.Parallelism, Is.EqualTo(0));
            Assert.That(status.MaxParallelism, Is.EqualTo(0));
            Assert.That(status.EndTime, Is.Null);
            Assert.That(status.Error, Is.Null);
        }

        [Test]
        public void JobStatus_SetProperties_StoresValues()
        {
            // Arrange
            var status = new JobStatus();
            var startTime = System.DateTime.UtcNow.AddMinutes(-10);
            var endTime = System.DateTime.UtcNow;

            // Act
            status.JobId = "status-job-123";
            status.JobName = "Status Test Job";
            status.State = "RUNNING";
            status.Parallelism = 4;
            status.MaxParallelism = 8;
            status.StartTime = startTime;
            status.EndTime = endTime;
            status.Error = "Status error";

            // Assert
            Assert.That(status.JobId, Is.EqualTo("status-job-123"));
            Assert.That(status.JobName, Is.EqualTo("Status Test Job"));
            Assert.That(status.State, Is.EqualTo("RUNNING"));
            Assert.That(status.Parallelism, Is.EqualTo(4));
            Assert.That(status.MaxParallelism, Is.EqualTo(8));
            Assert.That(status.StartTime, Is.EqualTo(startTime));
            Assert.That(status.EndTime, Is.EqualTo(endTime));
            Assert.That(status.Error, Is.EqualTo("Status error"));
        }

        [Test]
        public void JobStatus_WithNullEndTime_StoresNull()
        {
            // Arrange
            var status = new JobStatus
            {
                JobId = "job-456",
                State = "RUNNING",
                EndTime = null
            };

            // Assert
            Assert.That(status.EndTime, Is.Null);
        }

        #endregion

        #region JobClient Tests

        [Test]
        public void JobClient_Constructor_InitializesJobName()
        {
            // Arrange
            var jobName = "Test Flink Job";

            // Act
            using var client = new JobClient(jobName);

            // Assert
            Assert.That(client.JobName, Is.EqualTo(jobName));
            Assert.That(client.JobId, Is.EqualTo(string.Empty));
        }

        [Test]
        public void JobClient_GetJobId_ReturnsJobId()
        {
            // Arrange
            using var client = new JobClient("Test Job");
            client.JobId = "test-job-id-123";

            // Act
            var jobId = client.GetJobId();

            // Assert
            Assert.That(jobId, Is.EqualTo("test-job-id-123"));
        }

        [Test]
        public void JobClient_SetJobId_UpdatesJobId()
        {
            // Arrange
            using var client = new JobClient("Test Job");

            // Act
            client.JobId = "new-job-id-456";

            // Assert
            Assert.That(client.JobId, Is.EqualTo("new-job-id-456"));
            Assert.That(client.GetJobId(), Is.EqualTo("new-job-id-456"));
        }

        [Test]
        public void JobClient_Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            var client = new JobClient("Test Job");

            // Act & Assert - should not throw
            Assert.DoesNotThrow(() => client.Dispose());
            Assert.DoesNotThrow(() => client.Dispose());
        }

        [Test]
        public void JobClient_ImplementsIJobClient()
        {
            // Arrange & Act
            using var client = new JobClient("Test Job");

            // Assert
            Assert.That(client, Is.InstanceOf<IJobClient>());
        }

        [Test]
        public void JobClient_ImplementsIDisposable()
        {
            // Arrange & Act
            using var client = new JobClient("Test Job");

            // Assert
            Assert.That(client, Is.InstanceOf<IDisposable>());
        }

        #endregion

        #region DataStreamExtensions Tests (Chunk 1A)

        [Test]
        public void DataStreamExtensions_AddSink_WithKafkaSinkFunction_CallsStreamAddSink()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromKafka(
                "test-topic",
                "localhost:9092",
                "test-group",
                "earliest"
            );
            var sinkFunction = new KafkaSinkFunction<string>(
                "output-topic",
                "localhost:9092",
                s => System.Text.Encoding.UTF8.GetBytes(s)
            );

            // Act - This tests the DataStreamExtensions.AddSink method
            var result = DataStreamExtensions.AddSink(stream, sinkFunction);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<DataStream<string>>());
        }

        [Test]
        public void DataStreamExtensions_AddSink_ReturnsDataStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromKafka(
                "input-topic",
                "localhost:9092",
                "test-group",
                "earliest"
            );
            var sinkFunction = new KafkaSinkFunction<string>(
                "output-topic",
                "localhost:9092",
                s => System.Text.Encoding.UTF8.GetBytes(s)
            );

            // Act
            var result = stream.AddSink(sinkFunction);

            // Assert
            Assert.That(result, Is.SameAs(stream));
        }

        #endregion

        #region IAsyncFunction Tests (Chunk 1A)

        [Test]
        public void IAsyncFunction_Implementation_CanBeCreated()
        {
            // Arrange & Act
            var asyncFunc = new TestAsyncFunction();

            // Assert
            Assert.That(asyncFunc, Is.Not.Null);
            Assert.That(asyncFunc, Is.InstanceOf<IAsyncFunction<string, string>>());
        }

        [Test]
        public async Task IAsyncFunction_AsyncInvokeAsync_CanBeCalled()
        {
            // Arrange
            var asyncFunc = new TestAsyncFunction();
            var resultFuture = new TestResultFuture();
            var input = "test";

            // Act
            await asyncFunc.AsyncInvokeAsync(input, resultFuture);

            // Assert
            Assert.That(resultFuture.IsCompleted, Is.True);
            Assert.That(resultFuture.Results, Is.Not.Null);
            Assert.That(resultFuture.Results, Contains.Item("TEST"));
        }

        [Test]
        public async Task IAsyncFunction_TimeoutAsync_CustomImplementation_CanBeOverridden()
        {
            // Arrange
            var asyncFunc = new TestAsyncFunctionWithCustomTimeout();
            var resultFuture = new TestResultFuture();
            var input = "timeout-test";

            // Act
            await asyncFunc.TimeoutAsync(input, resultFuture);

            // Assert
            Assert.That(resultFuture.IsCompleted, Is.True);
            Assert.That(resultFuture.Results, Contains.Item("TIMEOUT"));
        }

        [Test]
        public void IAsyncFunction_MultipleImplementations_CanCoexist()
        {
            // Arrange & Act
            var func1 = new TestAsyncFunction();
            var func2 = new TestAsyncFunctionWithCustomTimeout();

            // Assert
            Assert.That(func1, Is.Not.Null);
            Assert.That(func2, Is.Not.Null);
            Assert.That(func1, Is.InstanceOf<IAsyncFunction<string, string>>());
            Assert.That(func2, Is.InstanceOf<IAsyncFunction<string, string>>());
            Assert.That(func1, Is.Not.SameAs(func2));
        }

        [Test]
        public void IAsyncFunction_ResultFuture_CompleteExceptionally_HandlesExceptions()
        {
            // Arrange
            var resultFuture = new TestResultFuture();
            var exception = new InvalidOperationException("Test error");

            // Act
            resultFuture.CompleteExceptionally(exception);

            // Assert
            Assert.That(resultFuture.Exception, Is.EqualTo(exception));
            Assert.That(resultFuture.IsCompletedExceptionally, Is.True);
        }

        [Test]
        public void IAsyncFunction_ResultFuture_Complete_StoresResults()
        {
            // Arrange
            var resultFuture = new TestResultFuture();
            var results = new[] { "result1", "result2", "result3" };

            // Act
            resultFuture.Complete(results);

            // Assert
            Assert.That(resultFuture.IsCompleted, Is.True);
            Assert.That(resultFuture.Results, Has.Count.EqualTo(3));
            Assert.That(resultFuture.Results, Is.EqualTo(results));
        }

        [Test]
        public async Task IAsyncFunction_AsyncOperation_WithMultipleResults()
        {
            // Arrange
            var asyncFunc = new TestAsyncFunctionMultipleResults();
            var resultFuture = new TestResultFuture();
            var input = "test";

            // Act
            await asyncFunc.AsyncInvokeAsync(input, resultFuture);

            // Assert
            Assert.That(resultFuture.IsCompleted, Is.True);
            Assert.That(resultFuture.Results, Has.Count.EqualTo(2));
            Assert.That(resultFuture.Results, Contains.Item("TEST"));
            Assert.That(resultFuture.Results, Contains.Item("test"));
        }

        // Helper classes for testing IAsyncFunction
        private class TestAsyncFunction : IAsyncFunction<string, string>
        {
            public Task AsyncInvokeAsync(string input, IResultFuture<string> resultFuture)
            {
                resultFuture.Complete(new[] { input.ToUpper() });
                return Task.CompletedTask;
            }
        }

        private class TestAsyncFunctionWithCustomTimeout : IAsyncFunction<string, string>
        {
            public Task AsyncInvokeAsync(string input, IResultFuture<string> resultFuture)
            {
                resultFuture.Complete(new[] { input.ToUpper() });
                return Task.CompletedTask;
            }

            public Task TimeoutAsync(string input, IResultFuture<string> resultFuture)
            {
                resultFuture.Complete(new[] { "TIMEOUT" });
                return Task.CompletedTask;
            }
        }

        private class TestAsyncFunctionMultipleResults : IAsyncFunction<string, string>
        {
            public Task AsyncInvokeAsync(string input, IResultFuture<string> resultFuture)
            {
                resultFuture.Complete(new[] { input.ToUpper(), input.ToLower() });
                return Task.CompletedTask;
            }
        }

        private class TestResultFuture : IResultFuture<string>
        {
            public bool IsCompleted { get; private set; }
            public bool IsCompletedExceptionally { get; private set; }
            public List<string> Results { get; } = new List<string>();
            public Exception? Exception { get; private set; }

            public void Complete(IEnumerable<string> results)
            {
                Results.AddRange(results);
                IsCompleted = true;
            }

            public void CompleteExceptionally(Exception exception)
            {
                Exception = exception;
                IsCompletedExceptionally = true;
            }
        }

        #endregion
    }
}
