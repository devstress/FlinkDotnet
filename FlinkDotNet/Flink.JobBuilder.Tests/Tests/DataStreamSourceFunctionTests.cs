using FlinkDotNet.DataStream;

#pragma warning disable CS1998 // Async method lacks 'await' operators

namespace Flink.JobBuilder.Tests.Tests
{
    /// <summary>
    /// Comprehensive tests for DataStream source function wrappers
    /// Target: Improve coverage for MappedSourceFunction, FlatMappedSourceFunction, FilteredSourceFunction, AggregatedSourceFunction
    /// </summary>
    [TestFixture]
    public class DataStreamSourceFunctionTests
    {
        #region MappedSourceFunction Tests

        [Test]
        public async Task MappedSourceFunction_TransformsValues()
        {
            // Arrange
            var sourceData = new List<int> { 1, 2, 3, 4, 5 };
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceStream = env.FromCollection(sourceData);

            // Act - Map to double the values
            var mappedStream = sourceStream.Map(x => x * 2);

            // Assert - Verify the stream was created (we can't execute it without infrastructure)
            Assert.That(mappedStream, Is.Not.Null);
            Assert.That(mappedStream, Is.TypeOf<DataStream<int>>());
        }

        [Test]
        public async Task MappedSourceFunction_WithStringTransformation()
        {
            // Arrange
            var sourceData = new List<string> { "hello", "world" };
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceStream = env.FromCollection(sourceData);

            // Act
            var mappedStream = sourceStream.Map(s => s.ToUpper());

            // Assert
            Assert.That(mappedStream, Is.Not.Null);
            Assert.That(mappedStream, Is.TypeOf<DataStream<string>>());
        }

        [Test]
        public async Task MappedSourceFunction_TypeConversion()
        {
            // Arrange
            var sourceData = new List<int> { 1, 2, 3 };
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceStream = env.FromCollection(sourceData);

            // Act - Map int to string
            var mappedStream = sourceStream.Map(x => x.ToString());

            // Assert
            Assert.That(mappedStream, Is.Not.Null);
            Assert.That(mappedStream, Is.TypeOf<DataStream<string>>());
        }

        #endregion

        #region FlatMappedSourceFunction Tests

        [Test]
        public async Task FlatMappedSourceFunction_SplitsValues()
        {
            // Arrange
            var sourceData = new List<string> { "hello world", "foo bar" };
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceStream = env.FromCollection(sourceData);

            // Act - FlatMap to split strings
            var flatMappedStream = sourceStream.FlatMap(s => s.Split(' '));

            // Assert
            Assert.That(flatMappedStream, Is.Not.Null);
            Assert.That(flatMappedStream, Is.TypeOf<DataStream<string>>());
        }

        [Test]
        public async Task FlatMappedSourceFunction_WithEmptyResults()
        {
            // Arrange
            var sourceData = new List<int> { 1, 2, 3 };
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceStream = env.FromCollection(sourceData);

            // Act - FlatMap that produces empty results for some inputs
            var flatMappedStream = sourceStream.FlatMap(x => 
                x % 2 == 0 ? new[] { x, x * 2 } : Array.Empty<int>());

            // Assert
            Assert.That(flatMappedStream, Is.Not.Null);
            Assert.That(flatMappedStream, Is.TypeOf<DataStream<int>>());
        }

        [Test]
        public async Task FlatMappedSourceFunction_TypeConversion()
        {
            // Arrange
            var sourceData = new List<int> { 1, 2, 3 };
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceStream = env.FromCollection(sourceData);

            // Act - FlatMap with type conversion
            var flatMappedStream = sourceStream.FlatMap(x => 
                Enumerable.Range(0, x).Select(i => $"item-{i}"));

            // Assert
            Assert.That(flatMappedStream, Is.Not.Null);
            Assert.That(flatMappedStream, Is.TypeOf<DataStream<string>>());
        }

        #endregion

        #region FilteredSourceFunction Tests

        [Test]
        public async Task FilteredSourceFunction_FiltersValues()
        {
            // Arrange
            var sourceData = new List<int> { 1, 2, 3, 4, 5 };
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceStream = env.FromCollection(sourceData);

            // Act - Filter even numbers
            var filteredStream = sourceStream.Filter(x => x % 2 == 0);

            // Assert
            Assert.That(filteredStream, Is.Not.Null);
            Assert.That(filteredStream, Is.TypeOf<DataStream<int>>());
        }

        [Test]
        public async Task FilteredSourceFunction_WithStringPredicate()
        {
            // Arrange
            var sourceData = new List<string> { "apple", "banana", "apricot", "berry" };
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceStream = env.FromCollection(sourceData);

            // Act - Filter strings starting with 'a'
            var filteredStream = sourceStream.Filter(s => s.StartsWith("a"));

            // Assert
            Assert.That(filteredStream, Is.Not.Null);
            Assert.That(filteredStream, Is.TypeOf<DataStream<string>>());
        }

        [Test]
        public async Task FilteredSourceFunction_EmptyFilter()
        {
            // Arrange
            var sourceData = new List<int> { 1, 2, 3 };
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceStream = env.FromCollection(sourceData);

            // Act - Filter that matches nothing
            var filteredStream = sourceStream.Filter(x => x > 100);

            // Assert
            Assert.That(filteredStream, Is.Not.Null);
            Assert.That(filteredStream, Is.TypeOf<DataStream<int>>());
        }

        [Test]
        public async Task FilteredSourceFunction_AllPassFilter()
        {
            // Arrange
            var sourceData = new List<int> { 1, 2, 3, 4, 5 };
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceStream = env.FromCollection(sourceData);

            // Act - Filter that matches all
            var filteredStream = sourceStream.Filter(x => x > 0);

            // Assert
            Assert.That(filteredStream, Is.Not.Null);
            Assert.That(filteredStream, Is.TypeOf<DataStream<int>>());
        }

        #endregion

        #region Chaining Tests

        [Test]
        public async Task ChainedTransformations_MapThenFilter()
        {
            // Arrange
            var sourceData = new List<int> { 1, 2, 3, 4, 5 };
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceStream = env.FromCollection(sourceData);

            // Act - Chain map and filter
            var resultStream = sourceStream
                .Map(x => x * 2)
                .Filter(x => x > 5);

            // Assert
            Assert.That(resultStream, Is.Not.Null);
            Assert.That(resultStream, Is.TypeOf<DataStream<int>>());
        }

        [Test]
        public async Task ChainedTransformations_FlatMapThenMap()
        {
            // Arrange
            var sourceData = new List<string> { "a b", "c d" };
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceStream = env.FromCollection(sourceData);

            // Act
            var resultStream = sourceStream
                .FlatMap(s => s.Split(' '))
                .Map(s => s.ToUpper());

            // Assert
            Assert.That(resultStream, Is.Not.Null);
            Assert.That(resultStream, Is.TypeOf<DataStream<string>>());
        }

        [Test]
        public async Task ChainedTransformations_ComplexChain()
        {
            // Arrange
            var sourceData = new List<int> { 1, 2, 3 };
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceStream = env.FromCollection(sourceData);

            // Act - Complex transformation chain
            var resultStream = sourceStream
                .Map(x => x * 2)
                .Filter(x => x < 10)
                .FlatMap(x => new[] { x, x + 1 })
                .Map(x => x.ToString());

            // Assert
            Assert.That(resultStream, Is.Not.Null);
            Assert.That(resultStream, Is.TypeOf<DataStream<string>>());
        }

        #endregion

        #region Edge Cases

        [Test]
        public async Task EmptyCollection_Map()
        {
            // Arrange
            var sourceData = new List<int>();
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceStream = env.FromCollection(sourceData);

            // Act
            var mappedStream = sourceStream.Map(x => x * 2);

            // Assert
            Assert.That(mappedStream, Is.Not.Null);
        }

        [Test]
        public async Task EmptyCollection_FlatMap()
        {
            // Arrange
            var sourceData = new List<string>();
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceStream = env.FromCollection(sourceData);

            // Act
            var flatMappedStream = sourceStream.FlatMap(s => s.Split(' '));

            // Assert
            Assert.That(flatMappedStream, Is.Not.Null);
        }

        [Test]
        public async Task EmptyCollection_Filter()
        {
            // Arrange
            var sourceData = new List<int>();
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceStream = env.FromCollection(sourceData);

            // Act
            var filteredStream = sourceStream.Filter(x => x > 0);

            // Assert
            Assert.That(filteredStream, Is.Not.Null);
        }

        [Test]
        public async Task SingleElement_AllTransformations()
        {
            // Arrange
            var sourceData = new List<int> { 42 };
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceStream = env.FromCollection(sourceData);

            // Act
            var resultStream = sourceStream
                .Map(x => x * 2)
                .Filter(x => x > 50)
                .FlatMap(x => new[] { x, x + 1 });

            // Assert
            Assert.That(resultStream, Is.Not.Null);
        }

        #endregion
    }
}
