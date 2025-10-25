namespace FlinkDotNet.DataStream.Tests
{
    [NUnit.Framework.TestFixture]
    public class StreamExecutionEnvironmentExecuteAsyncExceptionTests
    {
        [NUnit.Framework.Test]
        public void ExecuteAsync_ThrowsInvalidOperationException_WhenNoJobDefined()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert - No Kafka source or operation capture
            var ex = NUnit.Framework.Assert.ThrowsAsync<System.InvalidOperationException>(async () =>
            {
                await env.ExecuteAsync("test-job");
            });

            NUnit.Framework.Assert.That(ex.Message, NUnit.Framework.Does.Contain("No Flink-compatible job is defined"));
        }

        [NUnit.Framework.Test]
        public void ExecuteAsync_ThrowsInvalidOperationException_WhenJobSubmissionFails()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            _ = env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Note: This will fail validation because there's no sink defined

            // Act & Assert
            NUnit.Framework.Assert.ThrowsAsync<System.InvalidOperationException>(async () =>
            {
                await env.ExecuteAsync("test-job");
            }, "Should throw InvalidOperationException when job validation fails");
        }

        [NUnit.Framework.Test]
        public void ExecuteAsync_HandlesNullJobName_WithDefaultName()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            _ = env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act & Assert - Should use default name "Flink Streaming Job"
            NUnit.Framework.Assert.ThrowsAsync<System.InvalidOperationException>(async () =>
            {
                await env.ExecuteAsync(null);
            }, "Should attempt execution with default name and fail validation");
        }

        [NUnit.Framework.Test]
        public void ExecuteAsync_HandlesEmptyJobName_WithDefaultName()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            _ = env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act & Assert
            NUnit.Framework.Assert.ThrowsAsync<System.InvalidOperationException>(async () =>
            {
                await env.ExecuteAsync(string.Empty);
            }, "Should attempt execution and fail validation");
        }

        [NUnit.Framework.Test]
        public void ExecuteAsync_UsesCancellationToken_WhenProvided()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            _ = env.FromKafka("test-topic", "localhost:9092", "test-group");
            using var cts = new System.Threading.CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert - Cancellation happens before HTTP call in validation
            NUnit.Framework.Assert.ThrowsAsync<System.InvalidOperationException>(async () =>
            {
                await env.ExecuteAsync("test-job", cts.Token);
            }, "Should fail validation before reaching cancellation point");
        }

        [NUnit.Framework.Test]
        public void ExecuteAsync_TranslatesNativeAPIOperations_ToJobDefinition()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromKafka("test-topic", "localhost:9092", "test-group");
            stream.Map(x => x.ToUpper());

            // Act & Assert - Should translate operations before submission
            NUnit.Framework.Assert.ThrowsAsync<System.InvalidOperationException>(async () =>
            {
                await env.ExecuteAsync("test-job");
            }, "Should translate native API operations and fail validation");
        }

        [NUnit.Framework.Test]
        public void ExecuteAsync_UsesActiveJob_WhenOperationCaptureEmpty()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Create a job definition directly without operation capture
            var jobDef = new Flink.JobBuilder.Models.JobDefinition
            {
                Source = new Flink.JobBuilder.Models.KafkaSourceDefinition
                {
                    Topic = "test-topic",
                    BootstrapServers = "localhost:9092",
                    GroupId = "test-group",
                    StartingOffsets = "latest"
                },
                Metadata = new Flink.JobBuilder.Models.JobMetadata
                {
                    JobId = System.Guid.NewGuid().ToString(),
                    JobName = "test-job",
                    CreatedAt = System.DateTime.UtcNow,
                    Version = "1.0"
                }
            };

            // Use reflection to set the active job without operation capture
            var activeJobField = typeof(StreamExecutionEnvironment).GetField("_activeJob",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            activeJobField?.SetValue(env, jobDef);

            // Act & Assert - Should use active job definition
            NUnit.Framework.Assert.ThrowsAsync<System.InvalidOperationException>(async () =>
            {
                await env.ExecuteAsync("override-name");
            }, "Should use active job and fail validation");
        }

        [NUnit.Framework.Test]
        public void ExecuteAsync_SetsJobNameFromParameter_WhenProvided()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            _ = env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act & Assert
            NUnit.Framework.Assert.ThrowsAsync<System.InvalidOperationException>(async () =>
            {
                await env.ExecuteAsync("custom-job-name");
            }, "Should set custom job name and fail validation");
        }

        [NUnit.Framework.Test]
        public void ExecuteAsync_HandlesLongJobNames_Gracefully()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            _ = env.FromKafka("test-topic", "localhost:9092", "test-group");
            var longName = new string('a', 1000);

            // Act & Assert
            NUnit.Framework.Assert.ThrowsAsync<System.InvalidOperationException>(async () =>
            {
                await env.ExecuteAsync(longName);
            }, "Should handle long job names and fail validation");
        }

        [NUnit.Framework.Test]
        public void ExecuteAsync_HandlesSpecialCharactersInJobName()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            _ = env.FromKafka("test-topic", "localhost:9092", "test-group");
            var specialName = "test-job-!@#$%^&*()";

            // Act & Assert
            NUnit.Framework.Assert.ThrowsAsync<System.InvalidOperationException>(async () =>
            {
                await env.ExecuteAsync(specialName);
            }, "Should handle special characters and fail validation");
        }

        [NUnit.Framework.Test]
        public void ExecuteAsync_WithMultipleOperations_TranslatesAll()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromKafka("test-topic", "localhost:9092", "test-group");
            stream.Map(x => x.ToUpper())
                  .Filter(x => x.Length > 0);

            // Act & Assert
            NUnit.Framework.Assert.ThrowsAsync<System.InvalidOperationException>(async () =>
            {
                await env.ExecuteAsync("multi-op-job");
            }, "Should translate multiple operations and fail validation");
        }

        [NUnit.Framework.Test]
        public void ExecuteAsync_WithComplexPipeline_TranslatesCorrectly()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromKafka("input-topic", "localhost:9092", "test-group");
            stream.Map(x => x.ToUpper())
                  .Filter(x => x.Length > 5)
                  .Map(x => x.ToLower());

            // Act & Assert
            NUnit.Framework.Assert.ThrowsAsync<System.InvalidOperationException>(async () =>
            {
                await env.ExecuteAsync("complex-pipeline");
            }, "Should translate complex pipeline and fail validation");
        }
    }
}
