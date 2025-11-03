using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Tests for comprehensive .NET string method support in Map operations.
    /// Validates ToUpperInvariant, ToLowerInvariant, Trim, TrimStart, TrimEnd support.
    /// </summary>
    [TestFixture]
    public class StringMapFunctionsTests
    {
        private StreamExecutionEnvironment _env = null!;

        [SetUp]
        public void Setup() => this._env = StreamExecutionEnvironment.GetExecutionEnvironment();

        #region IMapFunction Tests

        [Test]
        public void Map_WithToUpperInvariantMapFunction_CapturesUpperExpression()
        {
            // Arrange
            var stream = this._env.FromKafka("input-topic", "localhost:9092", "test-group", "earliest");
            
            // Act
            var mapped = stream.Map(new ToUpperInvariantMapFunction());
            
            // Assert
            Assert.That(mapped, Is.Not.Null);
        }

        [Test]
        public void Map_WithToLowerInvariantMapFunction_CapturesLowerExpression()
        {
            // Arrange
            var stream = this._env.FromKafka("input-topic", "localhost:9092", "test-group", "earliest");
            
            // Act
            var mapped = stream.Map(new ToLowerInvariantMapFunction());
            
            // Assert
            Assert.That(mapped, Is.Not.Null);
        }

        [Test]
        public void Map_WithTrimMapFunction_CapturesTrimExpression()
        {
            // Arrange
            var stream = this._env.FromKafka("input-topic", "localhost:9092", "test-group", "earliest");
            
            // Act
            var mapped = stream.Map(new TrimMapFunction());
            
            // Assert
            Assert.That(mapped, Is.Not.Null);
        }

        [Test]
        public void Map_WithTrimStartMapFunction_CapturesLtrimExpression()
        {
            // Arrange
            var stream = this._env.FromKafka("input-topic", "localhost:9092", "test-group", "earliest");
            
            // Act
            var mapped = stream.Map(new TrimStartMapFunction());
            
            // Assert
            Assert.That(mapped, Is.Not.Null);
        }

        [Test]
        public void Map_WithTrimEndMapFunction_CapturesRtrimExpression()
        {
            // Arrange
            var stream = this._env.FromKafka("input-topic", "localhost:9092", "test-group", "earliest");
            
            // Act
            var mapped = stream.Map(new TrimEndMapFunction());
            
            // Assert
            Assert.That(mapped, Is.Not.Null);
        }

        #endregion

        #region String Expression Tests

        [Test]
        public void Map_WithTrimExpression_CreatesDataStream()
        {
            // Arrange
            var stream = this._env.FromKafka("input-topic", "localhost:9092", "test-group", "earliest");
            
            // Act
            var mapped = stream.Map("trim");
            
            // Assert
            Assert.That(mapped, Is.Not.Null);
        }

        [Test]
        public void Map_WithLtrimExpression_CreatesDataStream()
        {
            // Arrange
            var stream = this._env.FromKafka("input-topic", "localhost:9092", "test-group", "earliest");
            
            // Act
            var mapped = stream.Map("ltrim");
            
            // Assert
            Assert.That(mapped, Is.Not.Null);
        }

        [Test]
        public void Map_WithRtrimExpression_CreatesDataStream()
        {
            // Arrange
            var stream = this._env.FromKafka("input-topic", "localhost:9092", "test-group", "earliest");
            
            // Act
            var mapped = stream.Map("rtrim");
            
            // Assert
            Assert.That(mapped, Is.Not.Null);
        }

        [Test]
        public void Map_WithCompositeExpression_TrimAndUpper_CreatesDataStream()
        {
            // Arrange
            var stream = this._env.FromKafka("input-topic", "localhost:9092", "test-group", "earliest");
            
            // Act
            var mapped = stream.Map("trim,upper");
            
            // Assert
            Assert.That(mapped, Is.Not.Null);
        }

        [Test]
        public void Map_WithCompositeExpression_LowerAndTrim_CreatesDataStream()
        {
            // Arrange
            var stream = this._env.FromKafka("input-topic", "localhost:9092", "test-group", "earliest");
            
            // Act
            var mapped = stream.Map("lower,trim");
            
            // Assert
            Assert.That(mapped, Is.Not.Null);
        }

        [Test]
        public void Map_WithCompositeExpression_LtrimAndUpper_CreatesDataStream()
        {
            // Arrange
            var stream = this._env.FromKafka("input-topic", "localhost:9092", "test-group", "earliest");
            
            // Act
            var mapped = stream.Map("ltrim,upper");
            
            // Assert
            Assert.That(mapped, Is.Not.Null);
        }

        #endregion

        #region Chaining Tests

        [Test]
        public void Map_ChainedStringFunctions_CreatesDataStream()
        {
            // Arrange
            var stream = this._env.FromKafka("input-topic", "localhost:9092", "test-group", "earliest");
            
            // Act
            var result = stream
                .Map(new TrimMapFunction())
                .Map(new ToUpperInvariantMapFunction());
            
            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void Map_MultipleChainedOperations_CreatesDataStream()
        {
            // Arrange
            var stream = this._env.FromKafka("input-topic", "localhost:9092", "test-group", "earliest");
            
            // Act
            var result = stream
                .Map("ltrim")
                .Map("upper")
                .Map("rtrim");
            
            // Assert
            Assert.That(result, Is.Not.Null);
        }

        #endregion

        #region Integration Tests

        [Test]
        public void FullPipeline_WithStringMapFunctionAndSink_CreatesCompleteChain()
        {
            // Arrange
            var stream = this._env.FromKafka("input-topic", "localhost:9092", "test-group", "earliest");
            
            // Act
            var mapped = stream.Map(new ToUpperInvariantMapFunction());
            var result = mapped.SinkToKafka("output-topic", "localhost:9092");
            
            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void FullPipeline_WithMultipleStringFunctions_CreatesCompleteChain()
        {
            // Arrange
            var stream = this._env.FromKafka("input-topic", "localhost:9092", "test-group", "earliest");
            
            // Act
            var result = stream
                .Map(new TrimMapFunction())
                .Map(new ToLowerInvariantMapFunction())
                .SinkToKafka("output-topic", "localhost:9092");
            
            // Assert
            Assert.That(result, Is.Not.Null);
        }

        #endregion

        #region Local Execution Tests

        [Test]
        public void ToUpperInvariantMapFunction_WithLocalExecution_ConvertsToUpper()
        {
            // Arrange
            var mapFunc = new ToUpperInvariantMapFunction();
            
            // Act
            var result = mapFunc.Map("hello world");
            
            // Assert
            Assert.That(result, Is.EqualTo("HELLO WORLD"));
        }

        [Test]
        public void ToLowerInvariantMapFunction_WithLocalExecution_ConvertsToLower()
        {
            // Arrange
            var mapFunc = new ToLowerInvariantMapFunction();
            
            // Act
            var result = mapFunc.Map("HELLO WORLD");
            
            // Assert
            Assert.That(result, Is.EqualTo("hello world"));
        }

        [Test]
        public void TrimMapFunction_WithLocalExecution_TrimsWhitespace()
        {
            // Arrange
            var mapFunc = new TrimMapFunction();
            
            // Act
            var result = mapFunc.Map("  hello world  ");
            
            // Assert
            Assert.That(result, Is.EqualTo("hello world"));
        }

        [Test]
        public void TrimStartMapFunction_WithLocalExecution_TrimsLeadingWhitespace()
        {
            // Arrange
            var mapFunc = new TrimStartMapFunction();
            
            // Act
            var result = mapFunc.Map("  hello world  ");
            
            // Assert
            Assert.That(result, Is.EqualTo("hello world  "));
        }

        [Test]
        public void TrimEndMapFunction_WithLocalExecution_TrimsTrailingWhitespace()
        {
            // Arrange
            var mapFunc = new TrimEndMapFunction();
            
            // Act
            var result = mapFunc.Map("  hello world  ");
            
            // Assert
            Assert.That(result, Is.EqualTo("  hello world"));
        }

        #endregion

        #region Null Handling Tests

        [Test]
        public void ToUpperInvariantMapFunction_WithNull_ReturnsEmptyString()
        {
            // Arrange
            var mapFunc = new ToUpperInvariantMapFunction();
            
            // Act
            var result = mapFunc.Map(null!);
            
            // Assert
            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void TrimMapFunction_WithNull_ReturnsEmptyString()
        {
            // Arrange
            var mapFunc = new TrimMapFunction();
            
            // Act
            var result = mapFunc.Map(null!);
            
            // Assert
            Assert.That(result, Is.EqualTo(string.Empty));
        }

        #endregion
    }
}
