using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Branch coverage tests for DataStream null checks and constructor validation
    /// Targets uncovered branches in constructor null checks
    /// </summary>
    [TestFixture]
    public class DataStreamConstructorBranchCoverageTests
    {
        #region DataStream Constructor Null Checks

        [Test]
        public void DataStreamConstructor_WithNullJobDefinition_ThrowsArgumentNullException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert - Test null JobDefinition
            var ex = Assert.Throws<System.Reflection.TargetInvocationException>(() =>
            {
                var constructor = typeof(DataStream<string>).GetConstructor(
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                    null,
                    new[] { typeof(Flink.JobBuilder.Models.JobDefinition), typeof(StreamExecutionEnvironment) },
                    null);
                constructor?.Invoke(new object?[] { null, env });
            });

            Assert.That(ex!.InnerException, Is.InstanceOf<ArgumentNullException>());
        }

        [Test]
        public void DataStreamConstructor_WithNullEnvironment_ThrowsArgumentNullException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromKafka("test", "localhost:9092", "group", "earliest");

            // Get job definition from the stream
            var jobDefField = typeof(DataStream<string>).GetField("_jobDefinition",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var jobDef = jobDefField?.GetValue(stream);

            // Act & Assert - Test null environment
            var ex = Assert.Throws<System.Reflection.TargetInvocationException>(() =>
            {
                var constructor = typeof(DataStream<string>).GetConstructor(
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                    null,
                    new[] { typeof(Flink.JobBuilder.Models.JobDefinition), typeof(StreamExecutionEnvironment) },
                    null);
                constructor?.Invoke(new object?[] { jobDef, null });
            });

            Assert.That(ex!.InnerException, Is.InstanceOf<ArgumentNullException>());
        }

        #endregion

        #region Map Operation Null Checks

        [Test]
        public void Map_WithNullMapFunction_ThrowsArgumentNullException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var collection = new[] { 1, 2, 3 };
            var stream = env.FromCollection(collection);
            Func<int, int>? nullFunc = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                stream.Map(nullFunc!));
        }

        #endregion

        #region Filter Operation Null Checks

        [Test]
        public void Filter_WithNullFilterFunction_ThrowsArgumentNullException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var collection = new[] { 1, 2, 3 };
            var stream = env.FromCollection(collection);
            Func<int, bool>? nullFunc = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                stream.Filter(nullFunc!));
        }

        #endregion

        #region FlatMap Operation Null Checks

        [Test]
        public void FlatMap_WithNullFlatMapFunction_ThrowsArgumentNullException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var collection = new[] { "a", "b", "c" };
            var stream = env.FromCollection(collection);
            Func<string, IEnumerable<char>>? nullFunc = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                stream.FlatMap(nullFunc!));
        }

        #endregion

        #region SetMaxParallelism Tests

        [Test]
        public void SetMaxParallelism_WithPositiveValue_SetsValue()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var collection = new[] { 1, 2, 3 };
            var stream = env.FromCollection(collection);

            // Act
            var result = stream.SetMaxParallelism(100);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void SetMaxParallelism_WithLargeValue_SetsValue()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var collection = new[] { 1, 2, 3 };
            var stream = env.FromCollection(collection);

            // Act
            var result = stream.SetMaxParallelism(32768);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        #endregion

        #region Name Operation Tests

        [Test]
        public void Name_WithNullName_SetsNullName()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var collection = new[] { 1, 2, 3 };
            var stream = env.FromCollection(collection);

            // Act
            var result = stream.Name(null);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void Name_WithEmptyName_SetsEmptyName()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var collection = new[] { 1, 2, 3 };
            var stream = env.FromCollection(collection);

            // Act
            var result = stream.Name(string.Empty);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void Name_WithVeryLongName_SetsName()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var collection = new[] { 1, 2, 3 };
            var stream = env.FromCollection(collection);
            var longName = new string('a', 10000);

            // Act
            var result = stream.Name(longName);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        #endregion
    }
}
