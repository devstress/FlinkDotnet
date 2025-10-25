using System;
using System.Linq;
using System.Reflection;
using Flink.JobBuilder.Models;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Tests to cover specific branch scenarios for DataStream methods when working with JobDefinition-backed streams.
    /// These tests target uncovered branches in Map, Filter, and FlatMap methods.
    /// </summary>
    [TestFixture]
    public class DataStreamJobDefinitionBranchCoverageTests
    {
        private StreamExecutionEnvironment _env = null!;

        [SetUp]
        public void Setup() => this._env = StreamExecutionEnvironment.GetExecutionEnvironment();

        #region Map Branch Coverage Tests

        [Test]
        public void Map_WithJobDefinitionBackedStream_NoOperationCapture_CreatesNewStream()
        {
            // Arrange - Create a DataStream backed by JobDefinition without OperationCapture
            // This tests the scenario where _job != null but _operationCapture == null
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobName = "TestJob" },
                Source = new KafkaSourceDefinition
                {
                    Topic = "test-topic",
                    BootstrapServers = "localhost:9092"
                }
            };

            var stream = this.CreateDataStreamWithJobDefinition<string>(jobDef);

            // Act - This should hit the branch where _job != null but _operationCapture == null
            var mapped = stream.Map(s => s.ToUpper());

            // Assert
            Assert.That(mapped, Is.Not.Null);
        }

        [Test]
        public void Map_WithNullJobAndOperationCapture_CreatesNewJobDefinition()
        {
            // Arrange - Create a scenario where _job is null but _operationCapture is not null
            // This is achieved through FromKafka which creates operation capture but no initial job
            var stream = this._env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act - This should hit the branch where _job is null so new JobDefinition() is created
            var mapped = stream.Map(s => s.ToUpper());

            // Assert
            Assert.That(mapped, Is.Not.Null);
        }

        #endregion

        #region Filter Branch Coverage Tests

        [Test]
        public void Filter_WithJobDefinitionBackedStream_NoOperationCapture_CreatesNewStream()
        {
            // Arrange - Create a DataStream backed by JobDefinition without OperationCapture
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobName = "TestJob" },
                Source = new KafkaSourceDefinition
                {
                    Topic = "test-topic",
                    BootstrapServers = "localhost:9092"
                }
            };

            var stream = this.CreateDataStreamWithJobDefinition<string>(jobDef);

            // Act - This should hit the branch where _job != null but _operationCapture == null
            var filtered = stream.Filter(s => s.Length > 0);

            // Assert
            Assert.That(filtered, Is.Not.Null);
        }

        [Test]
        public void Filter_WithNullJobAndOperationCapture_CreatesNewJobDefinition()
        {
            // Arrange - Create a scenario where _job is null but _operationCapture is not null
            var stream = this._env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act - This should hit the branch where _job is null so new JobDefinition() is created
            var filtered = stream.Filter(s => !string.IsNullOrEmpty(s));

            // Assert
            Assert.That(filtered, Is.Not.Null);
        }

        #endregion

        #region FlatMap Branch Coverage Tests

        [Test]
        public void FlatMap_WithJobDefinitionBackedStream_NoOperationCapture_CreatesNewStream()
        {
            // Arrange - Create a DataStream backed by JobDefinition without OperationCapture
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobName = "TestJob" },
                Source = new KafkaSourceDefinition
                {
                    Topic = "test-topic",
                    BootstrapServers = "localhost:9092"
                }
            };

            var stream = this.CreateDataStreamWithJobDefinition<string>(jobDef);

            // Act - This should hit the branch where _job != null but _operationCapture == null
            var flatMapped = stream.FlatMap(s => s.Split(' '));

            // Assert
            Assert.That(flatMapped, Is.Not.Null);
        }

        [Test]
        public void FlatMap_WithNullJobAndOperationCapture_CreatesNewJobDefinition()
        {
            // Arrange - Create a scenario where _job is null but _operationCapture is not null
            var stream = this._env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act - This should hit the branch where _job is null so new JobDefinition() is created
            var flatMapped = stream.FlatMap(s => s.ToCharArray().Select(c => c.ToString()));

            // Assert
            Assert.That(flatMapped, Is.Not.Null);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a DataStream backed by a JobDefinition without OperationCapture.
        /// This uses reflection to access the internal constructor.
        /// </summary>
        private DataStream<T> CreateDataStreamWithJobDefinition<T>(JobDefinition job)
        {
            // Use reflection to access the internal constructor: DataStream(JobDefinition, StreamExecutionEnvironment)
            var dataStreamType = typeof(DataStream<T>);
            var constructor = dataStreamType.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(JobDefinition), typeof(StreamExecutionEnvironment) },
                null
            );

            if (constructor == null)
            {
                throw new InvalidOperationException("Could not find internal constructor for DataStream");
            }

            var stream = (DataStream<T>) constructor.Invoke(new object[] { job, this._env });
            return stream;
        }

        #endregion
    }
}
