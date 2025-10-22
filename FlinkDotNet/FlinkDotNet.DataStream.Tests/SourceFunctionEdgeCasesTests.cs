using NUnit.Framework;
using FlinkDotNet.DataStream;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FlinkDotNet.DataStream.Tests
{
    [TestFixture]
    public class SourceFunctionEdgeCasesTests
    {
        #region MappedSourceFunction Edge Cases

        [Test]
        public async Task MappedSourceFunction_WithNullInput_HandlesGracefully()
        {
            // Arrange
            var sourceData = new List<string?> { "a", null, "b", null, "c" };
            var sourceFunction = new TestSourceFunction<string?>(sourceData);
            var mappedFunction = new MappedSourceFunction<string?, int>(
                sourceFunction,
                x => x?.Length ?? 0
            );

            // Act
            var results = new List<int>();
            await foreach (var item in mappedFunction.RunAsync(CancellationToken.None))
            {
                results.Add(item);
            }

            // Assert
            Assert.That(results, Has.Count.EqualTo(5));
            Assert.That(results, Is.EqualTo(new[] { 1, 0, 1, 0, 1 }));
        }

        [Test]
        public void MappedSourceFunction_WithTransformationException_PropagatesException()
        {
            // Arrange
            var sourceData = new List<int> { 1, 2, 3 };
            var sourceFunction = new TestSourceFunction<int>(sourceData);
            var mappedFunction = new MappedSourceFunction<int, int>(
                sourceFunction,
                x => x == 2 ? throw new InvalidOperationException("Test exception") : x * 2
            );

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await foreach (var item in mappedFunction.RunAsync(CancellationToken.None))
                {
                    // Should throw on second item
                }
            });
        }

        [Test]
        public async Task MappedSourceFunction_WithComplexTransformation_WorksCorrectly()
        {
            // Arrange
            var sourceData = new List<(int, string)>
            {
                (1, "a"), (2, "b"), (3, "c")
            };
            var sourceFunction = new TestSourceFunction<(int, string)>(sourceData);
            var mappedFunction = new MappedSourceFunction<(int, string), string>(
                sourceFunction,
                x => $"{x.Item2}-{x.Item1}"
            );

            // Act
            var results = new List<string>();
            await foreach (var item in mappedFunction.RunAsync(CancellationToken.None))
            {
                results.Add(item);
            }

            // Assert
            Assert.That(results, Is.EqualTo(new[] { "a-1", "b-2", "c-3" }));
        }

        #endregion

        #region FlatMappedSourceFunction Edge Cases

        [Test]
        public async Task FlatMappedSourceFunction_WithEmptyCollections_HandlesGracefully()
        {
            // Arrange
            var sourceData = new List<int> { 1, 2, 3 };
            var sourceFunction = new TestSourceFunction<int>(sourceData);
            var flatMappedFunction = new FlatMappedSourceFunction<int, int>(
                sourceFunction,
                x => x == 2 ? Enumerable.Empty<int>() : new[] { x, x * 10 }
            );

            // Act
            var results = new List<int>();
            await foreach (var item in flatMappedFunction.RunAsync(CancellationToken.None))
            {
                results.Add(item);
            }

            // Assert
            Assert.That(results, Is.EqualTo(new[] { 1, 10, 3, 30 }));
        }

        [Test]
        public void FlatMappedSourceFunction_WithNullCollection_ThrowsException()
        {
            // Arrange
            var sourceData = new List<int> { 1, 2, 3 };
            var sourceFunction = new TestSourceFunction<int>(sourceData);
            var flatMappedFunction = new FlatMappedSourceFunction<int, int>(
                sourceFunction,
                x => x == 2 ? null! : new[] { x }
            );

            // Act & Assert
            Assert.ThrowsAsync<NullReferenceException>(async () =>
            {
                await foreach (var item in flatMappedFunction.RunAsync(CancellationToken.None))
                {
                    // Should throw on second item
                }
            });
        }

        [Test]
        public async Task FlatMappedSourceFunction_WithVariableLengthResults_WorksCorrectly()
        {
            // Arrange
            var sourceData = new List<int> { 1, 2, 3 };
            var sourceFunction = new TestSourceFunction<int>(sourceData);
            var flatMappedFunction = new FlatMappedSourceFunction<int, int>(
                sourceFunction,
                x => Enumerable.Range(1, x)
            );

            // Act
            var results = new List<int>();
            await foreach (var item in flatMappedFunction.RunAsync(CancellationToken.None))
            {
                results.Add(item);
            }

            // Assert
            Assert.That(results, Is.EqualTo(new[] { 1, 1, 2, 1, 2, 3 }));
        }

        #endregion

        #region FilteredSourceFunction Edge Cases

        [Test]
        public async Task FilteredSourceFunction_WithAllItemsFiltered_ReturnsEmpty()
        {
            // Arrange
            var sourceData = new List<int> { 1, 2, 3, 4, 5 };
            var sourceFunction = new TestSourceFunction<int>(sourceData);
            var filteredFunction = new FilteredSourceFunction<int>(
                sourceFunction,
                x => false // Filter out everything
            );

            // Act
            var results = new List<int>();
            await foreach (var item in filteredFunction.RunAsync(CancellationToken.None))
            {
                results.Add(item);
            }

            // Assert
            Assert.That(results, Is.Empty);
        }

        [Test]
        public async Task FilteredSourceFunction_WithNoneFiltered_ReturnsAll()
        {
            // Arrange
            var sourceData = new List<int> { 1, 2, 3, 4, 5 };
            var sourceFunction = new TestSourceFunction<int>(sourceData);
            var filteredFunction = new FilteredSourceFunction<int>(
                sourceFunction,
                x => true // Keep everything
            );

            // Act
            var results = new List<int>();
            await foreach (var item in filteredFunction.RunAsync(CancellationToken.None))
            {
                results.Add(item);
            }

            // Assert
            Assert.That(results, Is.EqualTo(sourceData));
        }

        [Test]
        public async Task FilteredSourceFunction_WithComplexPredicate_FiltersCorrectly()
        {
            // Arrange
            var sourceData = Enumerable.Range(1, 100).ToList();
            var sourceFunction = new TestSourceFunction<int>(sourceData);
            var filteredFunction = new FilteredSourceFunction<int>(
                sourceFunction,
                x => x % 2 == 0 && x % 3 == 0 // Divisible by both 2 and 3
            );

            // Act
            var results = new List<int>();
            await foreach (var item in filteredFunction.RunAsync(CancellationToken.None))
            {
                results.Add(item);
            }

            // Assert
            var expected = Enumerable.Range(1, 100).Where(x => x % 2 == 0 && x % 3 == 0).ToList();
            Assert.That(results, Is.EqualTo(expected));
        }

        #endregion

        #region AggregatedSourceFunction Edge Cases

        [Test]
        public async Task AggregatedSourceFunction_WithEmptySource_ReturnsInitialAccumulator()
        {
            // Arrange
            var sourceData = new List<int>();
            var sourceFunction = new TestSourceFunction<int>(sourceData);
            var aggregateFunction = new TestSumAggregateFunction();
            var aggregatedFunction = new AggregatedSourceFunction<int, int, int>(
                sourceFunction,
                aggregateFunction
            );

            // Act
            var results = new List<int>();
            await foreach (var item in aggregatedFunction.RunAsync(CancellationToken.None))
            {
                results.Add(item);
            }

            // Assert
            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0], Is.EqualTo(0)); // Initial accumulator
        }

        [Test]
        public async Task AggregatedSourceFunction_WithSingleItem_AggregatesCorrectly()
        {
            // Arrange
            var sourceData = new List<int> { 42 };
            var sourceFunction = new TestSourceFunction<int>(sourceData);
            var aggregateFunction = new TestSumAggregateFunction();
            var aggregatedFunction = new AggregatedSourceFunction<int, int, int>(
                sourceFunction,
                aggregateFunction
            );

            // Act
            var results = new List<int>();
            await foreach (var item in aggregatedFunction.RunAsync(CancellationToken.None))
            {
                results.Add(item);
            }

            // Assert
            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0], Is.EqualTo(42));
        }

        [Test]
        public async Task AggregatedSourceFunction_WithLargeDataset_AggregatesCorrectly()
        {
            // Arrange
            var sourceData = Enumerable.Range(1, 1000).ToList();
            var sourceFunction = new TestSourceFunction<int>(sourceData);
            var aggregateFunction = new TestSumAggregateFunction();
            var aggregatedFunction = new AggregatedSourceFunction<int, int, int>(
                sourceFunction,
                aggregateFunction
            );

            // Act
            var results = new List<int>();
            await foreach (var item in aggregatedFunction.RunAsync(CancellationToken.None))
            {
                results.Add(item);
            }

            // Assert
            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0], Is.EqualTo(500500)); // Sum of 1 to 1000
        }

        #endregion

        #region Cancellation Tests

        [Test]
        public async Task MappedSourceFunction_WithCancellation_StopsProcessing()
        {
            // Arrange
            var sourceData = Enumerable.Range(1, 1000).ToList();
            var sourceFunction = new TestSourceFunction<int>(sourceData);
            var mappedFunction = new MappedSourceFunction<int, int>(
                sourceFunction,
                x => x * 2
            );
            using var cts = new CancellationTokenSource();

            // Act
            var results = new List<int>();
            try
            {
                await foreach (var item in mappedFunction.RunAsync(cts.Token))
                {
                    results.Add(item);
                    if (results.Count >= 10)
                    {
                        cts.Cancel();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }

            // Assert
            Assert.That(results.Count, Is.LessThanOrEqualTo(10));
        }

        #endregion

        #region Helper Classes

        private class TestSourceFunction<T> : ISourceFunction<T>
        {
            private readonly IEnumerable<T> _data;

            public TestSourceFunction(IEnumerable<T> data)
            {
                _data = data;
            }

            public async IAsyncEnumerable<T> RunAsync(CancellationToken cancellationToken)
            {
                foreach (var item in _data)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Yield();
                    yield return item;
                }
            }
        }

        private class TestSumAggregateFunction : IAggregateFunction<int, int, int>
        {
            public int CreateAccumulator() => 0;
            public int Add(int value, int accumulator) => accumulator + value;
            public int GetResult(int accumulator) => accumulator;
            public int Merge(int a, int b) => a + b;
        }

        #endregion
    }
}