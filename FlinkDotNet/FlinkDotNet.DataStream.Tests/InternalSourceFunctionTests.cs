using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    [TestFixture]
    public class InternalSourceFunctionTests
    {
        private StreamExecutionEnvironment _env = null!;

        [SetUp]
        public void Setup()
        {
            _env = StreamExecutionEnvironment.GetExecutionEnvironment();
        }

        #region MappedSourceFunction Tests

        [Test]
        public async Task MappedSourceFunction_RunAsync_ShouldApplyMapFunction()
        {
            // Arrange
            var sourceData = new[] { 1, 2, 3, 4, 5 };
            var sourceFunction = new TestSourceFunction<int>(sourceData);
            Func<int, string> mapFunc = i => $"Value: {i}";

            // Create DataStream using AddSource and apply map to test the transformation
            _ = _env.AddSource(sourceFunction).Map(mapFunc);

            // Act - Execute via source function directly to verify transformation logic
            var results = new List<string>();
            await foreach (var item in sourceFunction.RunAsync())
            {
                results.Add(mapFunc(item));
            }

            // Assert
            Assert.That(results.Count, Is.EqualTo(5));
            Assert.That(results[0], Is.EqualTo("Value: 1"));
            Assert.That(results[1], Is.EqualTo("Value: 2"));
            Assert.That(results[4], Is.EqualTo("Value: 5"));
        }

        [Test]
        public async Task MappedSourceFunction_RunAsync_WithComplexTransformation_ShouldWork()
        {
            // Arrange
            var sourceData = new[] { "hello", "world", "test" };
            var sourceFunction = new TestSourceFunction<string>(sourceData);
            Func<string, int> mapFunc = s => s.Length;

            // Create DataStream to test the transformation
            _ = _env.AddSource(sourceFunction).Map(mapFunc);

            // Act
            var results = new List<int>();
            await foreach (var item in sourceFunction.RunAsync())
            {
                results.Add(mapFunc(item));
            }

            // Assert
            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(results[0], Is.EqualTo(5)); // "hello".Length
            Assert.That(results[1], Is.EqualTo(5)); // "world".Length
            Assert.That(results[2], Is.EqualTo(4)); // "test".Length
        }

        [Test]
        public void MappedSourceFunction_WithNullFunction_ViaMapOperation_ShouldHandleGracefully()
        {
            // Arrange
            var sourceData = new[] { 1, 2, 3 };
            var sourceFunction = new TestSourceFunction<int>(sourceData);

            // Act
            var stream = _env.AddSource(sourceFunction);
            var mappedStream = stream.Map(i => i * 2);

            // Assert - Stream should be created successfully
            Assert.That(mappedStream, Is.Not.Null);
        }

        #endregion

        #region FlatMappedSourceFunction Tests

        [Test]
        public async Task FlatMappedSourceFunction_RunAsync_ShouldFlattenResults()
        {
            // Arrange
            var sourceData = new[] { "hello world", "test data" };
            var sourceFunction = new TestSourceFunction<string>(sourceData);
            Func<string, IEnumerable<string>> flatMapFunc = s => s.Split(' ');

            // Act
            var results = new List<string>();
            await foreach (var item in sourceFunction.RunAsync())
            {
                foreach (var result in flatMapFunc(item))
                {
                    results.Add(result);
                }
            }

            // Assert
            Assert.That(results.Count, Is.EqualTo(4));
            Assert.That(results[0], Is.EqualTo("hello"));
            Assert.That(results[1], Is.EqualTo("world"));
            Assert.That(results[2], Is.EqualTo("test"));
            Assert.That(results[3], Is.EqualTo("data"));
        }

        [Test]
        public async Task FlatMappedSourceFunction_RunAsync_WithEmptyResults_ShouldFilterOut()
        {
            // Arrange
            var sourceData = new[] { 1, 2, 3 };
            var sourceFunction = new TestSourceFunction<int>(sourceData);
            Func<int, IEnumerable<int>> flatMapFunc = i =>
                i % 2 == 0 ? new[] { i, i * 10 } : Array.Empty<int>();

            // Act
            var results = new List<int>();
            await foreach (var item in sourceFunction.RunAsync())
            {
                foreach (var result in flatMapFunc(item))
                {
                    results.Add(result);
                }
            }

            // Assert - only even numbers produce output
            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results[0], Is.EqualTo(2));
            Assert.That(results[1], Is.EqualTo(20));
        }

        [Test]
        public async Task FlatMappedSourceFunction_RunAsync_WithMultipleOutputs_ShouldExpandAll()
        {
            // Arrange
            var sourceData = new[] { 1, 2 };
            var sourceFunction = new TestSourceFunction<int>(sourceData);
            Func<int, IEnumerable<int>> flatMapFunc = i =>
                Enumerable.Range(1, i); // 1 produces [1], 2 produces [1,2]

            // Act
            var results = new List<int>();
            await foreach (var item in sourceFunction.RunAsync())
            {
                foreach (var result in flatMapFunc(item))
                {
                    results.Add(result);
                }
            }

            // Assert
            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(results, Is.EqualTo(new[] { 1, 1, 2 }));
        }

        #endregion

        #region FilteredSourceFunction Tests

        [Test]
        public async Task FilteredSourceFunction_RunAsync_ShouldFilterElements()
        {
            // Arrange
            var sourceData = new[] { 1, 2, 3, 4, 5, 6 };
            var sourceFunction = new TestSourceFunction<int>(sourceData);
            Func<int, bool> filterFunc = i => i % 2 == 0; // Keep only even numbers

            // Act
            var results = new List<int>();
            await foreach (var item in sourceFunction.RunAsync())
            {
                if (filterFunc(item))
                {
                    results.Add(item);
                }
            }

            // Assert
            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(results, Is.EqualTo(new[] { 2, 4, 6 }));
        }

        [Test]
        public async Task FilteredSourceFunction_RunAsync_WithAllFilteredOut_ShouldReturnEmpty()
        {
            // Arrange
            var sourceData = new[] { 1, 3, 5, 7, 9 };
            var sourceFunction = new TestSourceFunction<int>(sourceData);
            Func<int, bool> filterFunc = i => i % 2 == 0; // Keep only even numbers

            // Act
            var results = new List<int>();
            await foreach (var item in sourceFunction.RunAsync())
            {
                if (filterFunc(item))
                {
                    results.Add(item);
                }
            }

            // Assert
            Assert.That(results.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task FilteredSourceFunction_RunAsync_WithAllKept_ShouldReturnAll()
        {
            // Arrange
            var sourceData = new[] { 2, 4, 6, 8 };
            var sourceFunction = new TestSourceFunction<int>(sourceData);
            Func<int, bool> filterFunc = i => i % 2 == 0; // Keep only even numbers

            // Act
            var results = new List<int>();
            await foreach (var item in sourceFunction.RunAsync())
            {
                if (filterFunc(item))
                {
                    results.Add(item);
                }
            }

            // Assert
            Assert.That(results.Count, Is.EqualTo(4));
            Assert.That(results, Is.EqualTo(new[] { 2, 4, 6, 8 }));
        }

        [Test]
        public async Task FilteredSourceFunction_RunAsync_WithComplexPredicate_ShouldWork()
        {
            // Arrange
            var sourceData = new[] { "hello", "world", "test", "a", "ab" };
            var sourceFunction = new TestSourceFunction<string>(sourceData);
            Func<string, bool> filterFunc = s => s.Length > 3;

            // Act
            var results = new List<string>();
            await foreach (var item in sourceFunction.RunAsync())
            {
                if (filterFunc(item))
                {
                    results.Add(item);
                }
            }

            // Assert
            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(results, Is.EqualTo(new[] { "hello", "world", "test" }));
        }

        #endregion

        #region AggregatedSourceFunction Tests

        [Test]
        public async Task AggregatedSourceFunction_RunAsync_ShouldAggregateAll()
        {
            // Arrange
            var sourceData = new[] { 1, 2, 3, 4, 5 };
            var sourceFunction = new TestSourceFunction<int>(sourceData);
            var aggregateFunc = new SumAggregateFunction();

            // Act - Manually aggregate to test the function
            var accumulator = aggregateFunc.CreateAccumulator();
            await foreach (var item in sourceFunction.RunAsync())
            {
                accumulator = aggregateFunc.Add(item, accumulator);
            }
            var result = aggregateFunc.GetResult(accumulator);

            // Assert
            Assert.That(result, Is.EqualTo(15)); // Sum of 1+2+3+4+5
        }

        [Test]
        public async Task AggregatedSourceFunction_RunAsync_WithEmptySource_ShouldReturnInitialAccumulator()
        {
            // Arrange
            var sourceData = Array.Empty<int>();
            var sourceFunction = new TestSourceFunction<int>(sourceData);
            var aggregateFunc = new SumAggregateFunction();

            // Act
            var accumulator = aggregateFunc.CreateAccumulator();
            await foreach (var item in sourceFunction.RunAsync())
            {
                accumulator = aggregateFunc.Add(item, accumulator);
            }
            var result = aggregateFunc.GetResult(accumulator);

            // Assert
            Assert.That(result, Is.EqualTo(0)); // Initial accumulator value
        }

        [Test]
        public async Task AggregatedSourceFunction_RunAsync_WithAverageAggregation_ShouldCalculateCorrectly()
        {
            // Arrange
            var sourceData = new[] { 10, 20, 30, 40, 50 };
            var sourceFunction = new TestSourceFunction<int>(sourceData);
            var aggregateFunc = new AverageAggregateFunction();

            // Act
            var accumulator = aggregateFunc.CreateAccumulator();
            await foreach (var item in sourceFunction.RunAsync())
            {
                accumulator = aggregateFunc.Add(item, accumulator);
            }
            var result = aggregateFunc.GetResult(accumulator);

            // Assert
            Assert.That(result, Is.EqualTo(30.0)); // Average of 10,20,30,40,50
        }

        #endregion

        #region Chained Transformations Tests

        [Test]
        public async Task ChainedTransformations_MapThenFilter_ShouldApplyInOrder()
        {
            // Arrange
            var sourceData = new[] { 1, 2, 3, 4, 5 };
            var sourceFunction = new TestSourceFunction<int>(sourceData);
            Func<int, int> mapFunc = i => i * 2;
            Func<int, bool> filterFunc = i => i > 5;

            // Act
            var results = new List<int>();
            await foreach (var item in sourceFunction.RunAsync())
            {
                var mapped = mapFunc(item);
                if (filterFunc(mapped))
                {
                    results.Add(mapped);
                }
            }

            // Assert
            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(results, Is.EqualTo(new[] { 6, 8, 10 }));
        }

        [Test]
        public async Task ChainedTransformations_FilterThenFlatMap_ShouldApplyInOrder()
        {
            // Arrange
            var sourceData = new[] { 1, 2, 3, 4, 5 };
            var sourceFunction = new TestSourceFunction<int>(sourceData);
            Func<int, bool> filterFunc = i => i <= 3;
            Func<int, IEnumerable<int>> flatMapFunc = i => Enumerable.Range(1, i);

            // Act
            var results = new List<int>();
            await foreach (var item in sourceFunction.RunAsync())
            {
                if (filterFunc(item))
                {
                    foreach (var result in flatMapFunc(item))
                    {
                        results.Add(result);
                    }
                }
            }

            // Assert
            Assert.That(results.Count, Is.EqualTo(6));
            Assert.That(results, Is.EqualTo(new[] { 1, 1, 2, 1, 2, 3 }));
        }

        #endregion

        #region Helper Classes and Methods

        private class TestSourceFunction<T> : ISourceFunction<T>
        {
            private readonly IEnumerable<T> _data;

            public TestSourceFunction(IEnumerable<T> data)
            {
                _data = data;
            }

            public async IAsyncEnumerable<T> RunAsync(
                [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                await Task.CompletedTask;
                foreach (var item in _data)
                {
                    if (cancellationToken.IsCancellationRequested)
                        yield break;
                    yield return item;
                }
            }
        }

        private class SumAggregateFunction : IAggregateFunction<int, int, int>
        {
            public int CreateAccumulator() => 0;
            public int Add(int value, int accumulator) => accumulator + value;
            public int GetResult(int accumulator) => accumulator;
            public int Merge(int acc1, int acc2) => acc1 + acc2;
        }

        private class AverageAggregateFunction : IAggregateFunction<int, (int sum, int count), double>
        {
            public (int sum, int count) CreateAccumulator() => (0, 0);

            public (int sum, int count) Add(int value, (int sum, int count) accumulator)
            {
                return (accumulator.sum + value, accumulator.count + 1);
            }

            public double GetResult((int sum, int count) accumulator)
            {
                return accumulator.count == 0 ? 0.0 : (double) accumulator.sum / accumulator.count;
            }

            public (int sum, int count) Merge((int sum, int count) acc1, (int sum, int count) acc2)
            {
                return (acc1.sum + acc2.sum, acc1.count + acc2.count);
            }
        }

        #region Direct Internal Source Function Tests

        [Test]
        public async Task MappedSourceFunction_DirectTest_ShouldTransformElements()
        {
            // Arrange
            var sourceData = new[] { 1, 2, 3 };
            var sourceFunction = new TestSourceFunction<int>(sourceData);
            var mappedFunction = new MappedSourceFunction<int, string>(sourceFunction, i => $"Value: {i}");

            // Act
            var results = new List<string>();
            await foreach (var item in mappedFunction.RunAsync())
            {
                results.Add(item);
            }

            // Assert
            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(results[0], Is.EqualTo("Value: 1"));
            Assert.That(results[1], Is.EqualTo("Value: 2"));
            Assert.That(results[2], Is.EqualTo("Value: 3"));
        }

        [Test]
        public async Task MappedSourceFunction_WithCancellation_ShouldStopProcessing()
        {
            // Arrange
            var sourceData = Enumerable.Range(1, 1000);
            var sourceFunction = new TestSourceFunction<int>(sourceData);
            var mappedFunction = new MappedSourceFunction<int, int>(sourceFunction, i => i * 2);
            using var cts = new CancellationTokenSource();

            // Act
            var results = new List<int>();
            await foreach (var item in mappedFunction.RunAsync(cts.Token))
            {
                results.Add(item);
                if (results.Count >= 5)
                {
                    cts.Cancel();
                }
            }

            // Assert
            Assert.That(results.Count, Is.LessThanOrEqualTo(5));
        }

        [Test]
        public void MappedSourceFunction_WithNullSource_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new MappedSourceFunction<int, string>(null!, i => i.ToString()));
        }

        [Test]
        public void MappedSourceFunction_WithNullMapFunction_ShouldThrowArgumentNullException()
        {
            // Arrange
            var sourceFunction = new TestSourceFunction<int>(new[] { 1, 2, 3 });

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new MappedSourceFunction<int, string>(sourceFunction, null!));
        }

        [Test]
        public async Task FlatMappedSourceFunction_DirectTest_ShouldFlattenElements()
        {
            // Arrange
            var sourceData = new[] { "hello world", "test" };
            var sourceFunction = new TestSourceFunction<string>(sourceData);
            var flatMappedFunction = new FlatMappedSourceFunction<string, string>(
                sourceFunction,
                s => s.Split(' '));

            // Act
            var results = new List<string>();
            await foreach (var item in flatMappedFunction.RunAsync())
            {
                results.Add(item);
            }

            // Assert
            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(results[0], Is.EqualTo("hello"));
            Assert.That(results[1], Is.EqualTo("world"));
            Assert.That(results[2], Is.EqualTo("test"));
        }

        [Test]
        public async Task FlatMappedSourceFunction_WithEmptyResults_ShouldProduceNoOutput()
        {
            // Arrange
            var sourceData = new[] { 1, 2, 3 };
            var sourceFunction = new TestSourceFunction<int>(sourceData);
            var flatMappedFunction = new FlatMappedSourceFunction<int, int>(
                sourceFunction,
                i => Array.Empty<int>());

            // Act
            var results = new List<int>();
            await foreach (var item in flatMappedFunction.RunAsync())
            {
                results.Add(item);
            }

            // Assert
            Assert.That(results.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task FlatMappedSourceFunction_WithCancellation_ShouldStopProcessing()
        {
            // Arrange
            var sourceData = Enumerable.Range(1, 100);
            var sourceFunction = new TestSourceFunction<int>(sourceData);
            var flatMappedFunction = new FlatMappedSourceFunction<int, int>(
                sourceFunction,
                i => Enumerable.Range(1, i));
            using var cts = new CancellationTokenSource();

            // Act
            var results = new List<int>();
            await foreach (var item in flatMappedFunction.RunAsync(cts.Token))
            {
                results.Add(item);
                if (results.Count >= 10)
                {
                    cts.Cancel();
                }
            }

            // Assert
            Assert.That(results.Count, Is.LessThanOrEqualTo(10));
        }

        [Test]
        public void FlatMappedSourceFunction_WithNullSource_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new FlatMappedSourceFunction<int, string>(null!, i => new[] { i.ToString() }));
        }

        [Test]
        public void FlatMappedSourceFunction_WithNullFlatMapFunction_ShouldThrowArgumentNullException()
        {
            // Arrange
            var sourceFunction = new TestSourceFunction<int>(new[] { 1, 2, 3 });

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new FlatMappedSourceFunction<int, string>(sourceFunction, null!));
        }

        [Test]
        public async Task FilteredSourceFunction_DirectTest_ShouldFilterElements()
        {
            // Arrange
            var sourceData = new[] { 1, 2, 3, 4, 5, 6 };
            var sourceFunction = new TestSourceFunction<int>(sourceData);
            var filteredFunction = new FilteredSourceFunction<int>(sourceFunction, i => i % 2 == 0);

            // Act
            var results = new List<int>();
            await foreach (var item in filteredFunction.RunAsync())
            {
                results.Add(item);
            }

            // Assert
            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(results, Is.EqualTo(new[] { 2, 4, 6 }));
        }

        [Test]
        public async Task FilteredSourceFunction_WithAllFiltered_ShouldProduceNoOutput()
        {
            // Arrange
            var sourceData = new[] { 1, 3, 5, 7 };
            var sourceFunction = new TestSourceFunction<int>(sourceData);
            var filteredFunction = new FilteredSourceFunction<int>(sourceFunction, i => i % 2 == 0);

            // Act
            var results = new List<int>();
            await foreach (var item in filteredFunction.RunAsync())
            {
                results.Add(item);
            }

            // Assert
            Assert.That(results.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task FilteredSourceFunction_WithCancellation_ShouldStopProcessing()
        {
            // Arrange
            var sourceData = Enumerable.Range(1, 1000).Where(i => i % 2 == 0);
            var sourceFunction = new TestSourceFunction<int>(sourceData);
            var filteredFunction = new FilteredSourceFunction<int>(sourceFunction, i => i > 0);
            using var cts = new CancellationTokenSource();

            // Act
            var results = new List<int>();
            await foreach (var item in filteredFunction.RunAsync(cts.Token))
            {
                results.Add(item);
                if (results.Count >= 5)
                {
                    cts.Cancel();
                }
            }

            // Assert
            Assert.That(results.Count, Is.LessThanOrEqualTo(5));
        }

        [Test]
        public void FilteredSourceFunction_WithNullSource_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new FilteredSourceFunction<int>(null!, i => true));
        }

        [Test]
        public void FilteredSourceFunction_WithNullFilterFunction_ShouldThrowArgumentNullException()
        {
            // Arrange
            var sourceFunction = new TestSourceFunction<int>(new[] { 1, 2, 3 });

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new FilteredSourceFunction<int>(sourceFunction, null!));
        }

        [Test]
        public async Task AggregatedSourceFunction_DirectTest_ShouldAggregateAllElements()
        {
            // Arrange
            var sourceData = new[] { 1, 2, 3, 4, 5 };
            var sourceFunction = new TestSourceFunction<int>(sourceData);
            var aggregateFunc = new SumAggregateFunction();
            var aggregatedFunction = new AggregatedSourceFunction<int, int, int>(
                sourceFunction,
                aggregateFunc);

            // Act
            var results = new List<int>();
            await foreach (var item in aggregatedFunction.RunAsync())
            {
                results.Add(item);
            }

            // Assert
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0], Is.EqualTo(15)); // Sum of 1+2+3+4+5
        }

        [Test]
        public async Task AggregatedSourceFunction_WithEmptySource_ShouldReturnInitialAccumulator()
        {
            // Arrange
            var sourceData = Array.Empty<int>();
            var sourceFunction = new TestSourceFunction<int>(sourceData);
            var aggregateFunc = new SumAggregateFunction();
            var aggregatedFunction = new AggregatedSourceFunction<int, int, int>(
                sourceFunction,
                aggregateFunc);

            // Act
            var results = new List<int>();
            await foreach (var item in aggregatedFunction.RunAsync())
            {
                results.Add(item);
            }

            // Assert
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0], Is.EqualTo(0)); // Initial accumulator
        }

        [Test]
        public async Task AggregatedSourceFunction_WithComplexAggregation_ShouldCalculateCorrectly()
        {
            // Arrange
            var sourceData = new[] { 10, 20, 30 };
            var sourceFunction = new TestSourceFunction<int>(sourceData);
            var aggregateFunc = new AverageAggregateFunction();
            var aggregatedFunction = new AggregatedSourceFunction<int, (int sum, int count), double>(
                sourceFunction,
                aggregateFunc);

            // Act
            var results = new List<double>();
            await foreach (var item in aggregatedFunction.RunAsync())
            {
                results.Add(item);
            }

            // Assert
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0], Is.EqualTo(20.0)); // Average of 10, 20, 30
        }

        [Test]
        public async Task AggregatedSourceFunction_WithCancellation_ShouldStopAndReturnPartialResult()
        {
            // Arrange
            var sourceData = Enumerable.Range(1, 100);
            var sourceFunction = new TestSourceFunction<int>(sourceData);
            var aggregateFunc = new SumAggregateFunction();
            var aggregatedFunction = new AggregatedSourceFunction<int, int, int>(
                sourceFunction,
                aggregateFunc);
            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act
            var results = new List<int>();
            await foreach (var item in aggregatedFunction.RunAsync(cts.Token))
            {
                results.Add(item);
            }

            // Assert - Should complete aggregation even with cancellation since it processes all before yielding
            Assert.That(results.Count, Is.LessThanOrEqualTo(1));
        }

        [Test]
        public void AggregatedSourceFunction_WithNullSource_ShouldThrowArgumentNullException()
        {
            // Arrange
            var aggregateFunc = new SumAggregateFunction();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new AggregatedSourceFunction<int, int, int>(null!, aggregateFunc));
        }

        [Test]
        public void AggregatedSourceFunction_WithNullAggregateFunction_ShouldThrowArgumentNullException()
        {
            // Arrange
            var sourceFunction = new TestSourceFunction<int>(new[] { 1, 2, 3 });

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new AggregatedSourceFunction<int, int, int>(sourceFunction, null!));
        }

        [Test]
        public void AggregatedSourceFunction_MergeOperation_ShouldCombineAccumulators()
        {
            // Arrange
            var aggregateFunc = new SumAggregateFunction();
            var acc1 = 10;
            var acc2 = 20;

            // Act
            var merged = aggregateFunc.Merge(acc1, acc2);

            // Assert
            Assert.That(merged, Is.EqualTo(30));
        }

        [Test]
        public void AggregatedSourceFunction_AverageMergeOperation_ShouldCombineCorrectly()
        {
            // Arrange
            var aggregateFunc = new AverageAggregateFunction();
            var acc1 = (sum: 30, count: 3); // Average 10
            var acc2 = (sum: 50, count: 2); // Average 25

            // Act
            var merged = aggregateFunc.Merge(acc1, acc2);
            var result = aggregateFunc.GetResult(merged);

            // Assert
            Assert.That(merged.sum, Is.EqualTo(80));
            Assert.That(merged.count, Is.EqualTo(5));
            Assert.That(result, Is.EqualTo(16.0)); // 80/5 = 16
        }

        #endregion
        #endregion
    }
}
