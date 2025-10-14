using FlinkDotNet.Common;
using FlinkDotNet.DataStream;

namespace Flink.JobBuilder.Tests.Tests
{
    /// <summary>
    /// Comprehensive tests for FlinkDotNet main entry points and pipelines
    /// Target: Improve coverage for FlinkDotNet.Flink and FlinkDotNet.Pipelines.FlinkDotNet
    /// </summary>
    [TestFixture]
    public class FlinkDotNetMainEntryPointTests
    {
        #region Flink Entry Point Tests

        [Test]
        public void Flink_GetExecutionEnvironment_ReturnsEnvironment()
        {
            // Act
            var env = FlinkDotNet.Flink.GetExecutionEnvironment();

            // Assert
            Assert.That(env, Is.Not.Null);
            Assert.That(env, Is.TypeOf<StreamExecutionEnvironment>());
        }

        [Test]
        public void Flink_GetExecutionEnvironment_WithConfiguration_ReturnsEnvironment()
        {
            // Arrange
            var config = new Configuration();
            config.SetString("test.key", "test.value");

            // Act
            var env = FlinkDotNet.Flink.GetExecutionEnvironment(config);

            // Assert
            Assert.That(env, Is.Not.Null);
            Assert.That(env, Is.TypeOf<StreamExecutionEnvironment>());
        }

        [Test]
        public void Flink_GetExecutionEnvironment_WithNullConfiguration_ReturnsEnvironment()
        {
            // Act
            var env = FlinkDotNet.Flink.GetExecutionEnvironment(null);

            // Assert
            Assert.That(env, Is.Not.Null);
            Assert.That(env, Is.TypeOf<StreamExecutionEnvironment>());
        }

        [Test]
        public void Flink_CreateConfiguration_ReturnsNewConfiguration()
        {
            // Act
            var config = FlinkDotNet.Flink.CreateConfiguration();

            // Assert
            Assert.That(config, Is.Not.Null);
            Assert.That(config, Is.TypeOf<Configuration>());
        }

        [Test]
        public void Flink_CreateConfiguration_MultipleCallsReturnDifferentInstances()
        {
            // Act
            var config1 = FlinkDotNet.Flink.CreateConfiguration();
            var config2 = FlinkDotNet.Flink.CreateConfiguration();

            // Assert
            Assert.That(config1, Is.Not.Null);
            Assert.That(config2, Is.Not.Null);
            Assert.That(config1, Is.Not.SameAs(config2));
        }

        #endregion

        #region Flink.JobBuilder Entry Point Tests

        [Test]
        public void FlinkJobBuilder_FromKafka_CreatesBuilder()
        {
            // Act
            var builder = FlinkDotNet.Flink.JobBuilder.FromKafka("test-topic", "localhost:9092");

            // Assert
            Assert.That(builder, Is.Not.Null);
            Assert.That(builder, Is.TypeOf<global::Flink.JobBuilder.FlinkJobBuilder>());
        }

        [Test]
        public void FlinkJobBuilder_FromKafka_WithNullBootstrapServers_CreatesBuilder()
        {
            // Act
            var builder = FlinkDotNet.Flink.JobBuilder.FromKafka("test-topic", null);

            // Assert
            Assert.That(builder, Is.Not.Null);
        }

        [Test]
        public void FlinkJobBuilder_FromHttp_CreatesBuilder()
        {
            // Act
            var builder = FlinkDotNet.Flink.JobBuilder.FromHttp("http://example.com/api");

            // Assert
            Assert.That(builder, Is.Not.Null);
            Assert.That(builder, Is.TypeOf<global::Flink.JobBuilder.FlinkJobBuilder>());
        }

        [Test]
        public void FlinkJobBuilder_FromHttp_WithCustomMethod_CreatesBuilder()
        {
            // Act
            var builder = FlinkDotNet.Flink.JobBuilder.FromHttp("http://example.com/api", "POST");

            // Assert
            Assert.That(builder, Is.Not.Null);
        }

        [Test]
        public void FlinkJobBuilder_FromHttp_WithCustomInterval_CreatesBuilder()
        {
            // Act
            var builder = FlinkDotNet.Flink.JobBuilder.FromHttp("http://example.com/api", "GET", 120);

            // Assert
            Assert.That(builder, Is.Not.Null);
        }

        [Test]
        public void FlinkJobBuilder_FromDatabase_CreatesBuilder()
        {
            // Act
            var builder = FlinkDotNet.Flink.JobBuilder.FromDatabase(
                "Server=localhost;Database=test", 
                "SELECT * FROM table");

            // Assert
            Assert.That(builder, Is.Not.Null);
            Assert.That(builder, Is.TypeOf<global::Flink.JobBuilder.FlinkJobBuilder>());
        }

        [Test]
        public void FlinkJobBuilder_FromDatabase_WithCustomInterval_CreatesBuilder()
        {
            // Act
            var builder = FlinkDotNet.Flink.JobBuilder.FromDatabase(
                "Server=localhost;Database=test",
                "SELECT * FROM table",
                60);

            // Assert
            Assert.That(builder, Is.Not.Null);
        }

        #endregion

        #region Pipeline Helper Tests

        [Test]
        public void Pipeline_KafkaToKafka_CreatesBuilder()
        {
            // Act
            var builder = FlinkDotNet.Pipelines.FlinkDotNet.KafkaToKafka(
                "input-topic",
                "output-topic",
                "localhost:9092");

            // Assert
            Assert.That(builder, Is.Not.Null);
            Assert.That(builder, Is.TypeOf<global::Flink.JobBuilder.FlinkJobBuilder>());
        }

        [Test]
        public void Pipeline_KafkaToKafka_WithNullBootstrap_CreatesBuilder()
        {
            // Act
            var builder = FlinkDotNet.Pipelines.FlinkDotNet.KafkaToKafka(
                "input-topic",
                "output-topic",
                null);

            // Assert
            Assert.That(builder, Is.Not.Null);
        }

        [Test]
        public void Pipeline_KafkaToKafka_WithCustomMapExpression_CreatesBuilder()
        {
            // Act
            var builder = FlinkDotNet.Pipelines.FlinkDotNet.KafkaToKafka(
                "input-topic",
                "output-topic",
                "localhost:9092",
                "upper");

            // Assert
            Assert.That(builder, Is.Not.Null);
        }

        [Test]
        public void Pipeline_KafkaToKafka_WithIdentityMap_CreatesBuilder()
        {
            // Act
            var builder = FlinkDotNet.Pipelines.FlinkDotNet.KafkaToKafka(
                "input-topic",
                "output-topic",
                "localhost:9092",
                "identity");

            // Assert
            Assert.That(builder, Is.Not.Null);
        }

        [Test]
        public void Pipeline_KafkaToConsole_CreatesBuilder()
        {
            // Act
            var builder = FlinkDotNet.Pipelines.FlinkDotNet.KafkaToConsole(
                "input-topic",
                "localhost:9092");

            // Assert
            Assert.That(builder, Is.Not.Null);
            Assert.That(builder, Is.TypeOf<global::Flink.JobBuilder.FlinkJobBuilder>());
        }

        [Test]
        public void Pipeline_KafkaToConsole_WithNullBootstrap_CreatesBuilder()
        {
            // Act
            var builder = FlinkDotNet.Pipelines.FlinkDotNet.KafkaToConsole(
                "input-topic",
                null);

            // Assert
            Assert.That(builder, Is.Not.Null);
        }

        [Test]
        public void Pipeline_KafkaToConsole_WithCustomMapExpression_CreatesBuilder()
        {
            // Act
            var builder = FlinkDotNet.Pipelines.FlinkDotNet.KafkaToConsole(
                "input-topic",
                "localhost:9092",
                "lower");

            // Assert
            Assert.That(builder, Is.Not.Null);
        }

        [Test]
        public void Pipeline_Sql_WithSingleStatement_CreatesBuilder()
        {
            // Act
            var builder = FlinkDotNet.Pipelines.FlinkDotNet.Sql(
                "CREATE TABLE test (id INT, name STRING)");

            // Assert
            Assert.That(builder, Is.Not.Null);
            Assert.That(builder, Is.TypeOf<global::Flink.JobBuilder.FlinkJobBuilder>());
        }

        [Test]
        public void Pipeline_Sql_WithMultipleStatements_CreatesBuilder()
        {
            // Act
            var builder = FlinkDotNet.Pipelines.FlinkDotNet.Sql(
                "CREATE TABLE test1 (id INT)",
                "CREATE TABLE test2 (name STRING)",
                "INSERT INTO test2 SELECT * FROM test1");

            // Assert
            Assert.That(builder, Is.Not.Null);
        }

        [Test]
        public void Pipeline_Sql_WithNullStatements_CreatesBuilder()
        {
            // Act
            var builder = FlinkDotNet.Pipelines.FlinkDotNet.Sql(null);

            // Assert
            Assert.That(builder, Is.Not.Null);
        }

        [Test]
        public void Pipeline_Sql_WithEmptyStatements_CreatesBuilder()
        {
            // Act
            var builder = FlinkDotNet.Pipelines.FlinkDotNet.Sql();

            // Assert
            Assert.That(builder, Is.Not.Null);
        }

        #endregion

        #region Integration Tests

        [Test]
        public void Integration_FlinkEntryPointAndPipeline()
        {
            // Arrange
            var env = FlinkDotNet.Flink.GetExecutionEnvironment();

            // Act - Create a pipeline
            var builder = FlinkDotNet.Pipelines.FlinkDotNet.KafkaToKafka(
                "in", "out", "localhost:9092");

            // Assert
            Assert.That(env, Is.Not.Null);
            Assert.That(builder, Is.Not.Null);
        }

        [Test]
        public void Integration_ConfigurationAndEnvironment()
        {
            // Arrange
            var config = FlinkDotNet.Flink.CreateConfiguration();
            config.SetInteger("parallelism", 4);

            // Act
            var env = FlinkDotNet.Flink.GetExecutionEnvironment(config);
            env.SetParallelism(4); // Set parallelism on environment
            var actualParallelism = env.GetParallelism();

            // Assert
            Assert.That(env, Is.Not.Null);
            Assert.That(actualParallelism, Is.EqualTo(4));
        }

        #endregion
    }
}
