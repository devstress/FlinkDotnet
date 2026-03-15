using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlinkDotNet.DataStream;
using FlinkDotNet.DataStream.State;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    [TestFixture]
    public class InMemoryStateImplementationTests
    {
        #region InMemoryValueState Tests

        [Test]
        public async Task ValueState_InitialValue_ReturnsDefault()
        {
            var state = new InMemoryValueState<int>();

            int result = await state.ValueAsync();

            Assert.That(result, Is.EqualTo(default(int)));
        }

        [Test]
        public async Task ValueState_UpdateAndGet_ReturnsUpdatedValue()
        {
            var state = new InMemoryValueState<string>();

            await state.UpdateAsync("hello");
            string result = await state.ValueAsync();

            Assert.That(result, Is.EqualTo("hello"));
        }

        [Test]
        public async Task ValueState_MultipleUpdates_ReturnsLastValue()
        {
            var state = new InMemoryValueState<int>();

            await state.UpdateAsync(1);
            await state.UpdateAsync(2);
            await state.UpdateAsync(3);
            int result = await state.ValueAsync();

            Assert.That(result, Is.EqualTo(3));
        }

        [Test]
        public async Task ValueState_Clear_ResetsToDefault()
        {
            var state = new InMemoryValueState<string>();

            await state.UpdateAsync("value");
            await state.ClearAsync();
            string result = await state.ValueAsync();

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task ValueState_UpdateAfterClear_WorksCorrectly()
        {
            var state = new InMemoryValueState<int>();

            await state.UpdateAsync(42);
            await state.ClearAsync();
            await state.UpdateAsync(99);
            int result = await state.ValueAsync();

            Assert.That(result, Is.EqualTo(99));
        }

        #endregion

        #region InMemoryListState Tests

        [Test]
        public async Task ListState_InitialState_ReturnsEmptyList()
        {
            var state = new InMemoryListState<string>();

            IEnumerable<string> result = await state.GetAsync();

            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task ListState_AddSingleElement_ContainsElement()
        {
            var state = new InMemoryListState<int>();

            await state.AddAsync(42);
            IEnumerable<int> result = await state.GetAsync();

            Assert.That(result, Is.EqualTo(new[] { 42 }));
        }

        [Test]
        public async Task ListState_AddMultipleElements_ContainsAll()
        {
            var state = new InMemoryListState<string>();

            await state.AddAsync("a");
            await state.AddAsync("b");
            await state.AddAsync("c");
            IEnumerable<string> result = await state.GetAsync();

            Assert.That(result, Is.EqualTo(new[] { "a", "b", "c" }));
        }

        [Test]
        public async Task ListState_AddAll_ContainsAllElements()
        {
            var state = new InMemoryListState<int>();

            await state.AddAllAsync(new[] { 1, 2, 3 });
            IEnumerable<int> result = await state.GetAsync();

            Assert.That(result, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public async Task ListState_Update_ReplacesExistingContent()
        {
            var state = new InMemoryListState<int>();

            await state.AddAsync(1);
            await state.AddAsync(2);
            await state.UpdateAsync(new[] { 10, 20, 30 });
            IEnumerable<int> result = await state.GetAsync();

            Assert.That(result, Is.EqualTo(new[] { 10, 20, 30 }));
        }

        [Test]
        public async Task ListState_Clear_RemovesAllElements()
        {
            var state = new InMemoryListState<string>();

            await state.AddAllAsync(new[] { "x", "y", "z" });
            await state.ClearAsync();
            IEnumerable<string> result = await state.GetAsync();

            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task ListState_GetReturnsSnapshot_NotLiveReference()
        {
            var state = new InMemoryListState<int>();

            await state.AddAsync(1);
            IEnumerable<int> snapshot = await state.GetAsync();
            await state.AddAsync(2);

            Assert.That(snapshot.Count(), Is.EqualTo(1));
        }

        #endregion

        #region InMemoryMapState Tests

        [Test]
        public async Task MapState_InitialState_IsEmpty()
        {
            var state = new InMemoryMapState<string, int>();

            bool isEmpty = await state.IsEmptyAsync();

            Assert.That(isEmpty, Is.True);
        }

        [Test]
        public async Task MapState_PutAndGet_ReturnsValue()
        {
            var state = new InMemoryMapState<string, int>();

            await state.PutAsync("key1", 42);
            int result = await state.GetAsync("key1");

            Assert.That(result, Is.EqualTo(42));
        }

        [Test]
        public async Task MapState_PutMultipleEntries_ContainsAll()
        {
            var state = new InMemoryMapState<string, string>();

            await state.PutAsync("a", "alpha");
            await state.PutAsync("b", "beta");
            await state.PutAsync("c", "gamma");

            Assert.That(await state.ContainsAsync("a"), Is.True);
            Assert.That(await state.ContainsAsync("b"), Is.True);
            Assert.That(await state.ContainsAsync("c"), Is.True);
            Assert.That(await state.IsEmptyAsync(), Is.False);
        }

        [Test]
        public async Task MapState_PutAll_AddsAllEntries()
        {
            var state = new InMemoryMapState<int, string>();
            var map = new Dictionary<int, string>
            {
                { 1, "one" },
                { 2, "two" },
                { 3, "three" }
            };

            await state.PutAllAsync(map);
            IEnumerable<int> keys = await state.KeysAsync();

            Assert.That(keys.Count(), Is.EqualTo(3));
        }

        [Test]
        public async Task MapState_Remove_DeletesEntry()
        {
            var state = new InMemoryMapState<string, int>();

            await state.PutAsync("key", 100);
            await state.RemoveAsync("key");

            Assert.That(await state.ContainsAsync("key"), Is.False);
        }

        [Test]
        public async Task MapState_Entries_ReturnsAllKeyValuePairs()
        {
            var state = new InMemoryMapState<string, int>();

            await state.PutAsync("x", 1);
            await state.PutAsync("y", 2);
            IEnumerable<KeyValuePair<string, int>> entries = await state.EntriesAsync();

            Assert.That(entries.Count(), Is.EqualTo(2));
        }

        [Test]
        public async Task MapState_Keys_ReturnsAllKeys()
        {
            var state = new InMemoryMapState<string, int>();

            await state.PutAsync("a", 1);
            await state.PutAsync("b", 2);
            IEnumerable<string> keys = await state.KeysAsync();

            Assert.That(keys, Is.EquivalentTo(new[] { "a", "b" }));
        }

        [Test]
        public async Task MapState_Values_ReturnsAllValues()
        {
            var state = new InMemoryMapState<string, int>();

            await state.PutAsync("a", 10);
            await state.PutAsync("b", 20);
            IEnumerable<int> values = await state.ValuesAsync();

            Assert.That(values, Is.EquivalentTo(new[] { 10, 20 }));
        }

        [Test]
        public async Task MapState_Clear_RemovesAllEntries()
        {
            var state = new InMemoryMapState<string, int>();

            await state.PutAsync("x", 1);
            await state.PutAsync("y", 2);
            await state.ClearAsync();

            Assert.That(await state.IsEmptyAsync(), Is.True);
        }

        [Test]
        public async Task MapState_PutOverwrite_ReplacesValue()
        {
            var state = new InMemoryMapState<string, int>();

            await state.PutAsync("key", 1);
            await state.PutAsync("key", 2);
            int result = await state.GetAsync("key");

            Assert.That(result, Is.EqualTo(2));
        }

        #endregion

        #region InMemoryReducingState Tests

        [Test]
        public async Task ReducingState_InitialState_ReturnsDefault()
        {
            var reduceFunc = new SumReduceFunction();
            var state = new InMemoryReducingState<int>(reduceFunc);

            int result = await state.GetAsync();

            Assert.That(result, Is.EqualTo(default(int)));
        }

        [Test]
        public async Task ReducingState_AddSingleValue_ReturnsThatValue()
        {
            var reduceFunc = new SumReduceFunction();
            var state = new InMemoryReducingState<int>(reduceFunc);

            await state.AddAsync(5);
            int result = await state.GetAsync();

            Assert.That(result, Is.EqualTo(5));
        }

        [Test]
        public async Task ReducingState_AddMultipleValues_AppliesReduceFunction()
        {
            var reduceFunc = new SumReduceFunction();
            var state = new InMemoryReducingState<int>(reduceFunc);

            await state.AddAsync(1);
            await state.AddAsync(2);
            await state.AddAsync(3);
            int result = await state.GetAsync();

            Assert.That(result, Is.EqualTo(6));
        }

        [Test]
        public async Task ReducingState_Clear_ResetsState()
        {
            var reduceFunc = new SumReduceFunction();
            var state = new InMemoryReducingState<int>(reduceFunc);

            await state.AddAsync(10);
            await state.AddAsync(20);
            await state.ClearAsync();
            int result = await state.GetAsync();

            Assert.That(result, Is.EqualTo(default(int)));
        }

        [Test]
        public async Task ReducingState_AddAfterClear_StartsFromScratch()
        {
            var reduceFunc = new SumReduceFunction();
            var state = new InMemoryReducingState<int>(reduceFunc);

            await state.AddAsync(100);
            await state.ClearAsync();
            await state.AddAsync(5);
            int result = await state.GetAsync();

            Assert.That(result, Is.EqualTo(5));
        }

        [Test]
        public void ReducingState_NullReduceFunction_ThrowsArgumentNullException()
        {
            Assert.Throws<System.ArgumentNullException>(() => new InMemoryReducingState<int>(null!));
        }

        #endregion

        #region InMemoryAggregatingState Tests

        [Test]
        public async Task AggregatingState_InitialState_ReturnsInitialResult()
        {
            var aggFunc = new CountAggregateFunction();
            var state = new InMemoryAggregatingState<string, int, int>(aggFunc);

            int result = await state.GetAsync();

            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public async Task AggregatingState_AddElements_AppliesAggregation()
        {
            var aggFunc = new CountAggregateFunction();
            var state = new InMemoryAggregatingState<string, int, int>(aggFunc);

            await state.AddAsync("a");
            await state.AddAsync("b");
            await state.AddAsync("c");
            int result = await state.GetAsync();

            Assert.That(result, Is.EqualTo(3));
        }

        [Test]
        public async Task AggregatingState_Clear_ResetsState()
        {
            var aggFunc = new CountAggregateFunction();
            var state = new InMemoryAggregatingState<string, int, int>(aggFunc);

            await state.AddAsync("x");
            await state.AddAsync("y");
            await state.ClearAsync();
            int result = await state.GetAsync();

            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void AggregatingState_NullAggregateFunction_ThrowsArgumentNullException()
        {
            Assert.Throws<System.ArgumentNullException>(
                () => new InMemoryAggregatingState<string, int, int>(null!));
        }

        #endregion

        #region Test Helpers

        private sealed class SumReduceFunction : IReduceFunction<int>
        {
            public int Reduce(int value1, int value2) => value1 + value2;
        }

        private sealed class CountAggregateFunction : IAggregateFunction<string, int, int>
        {
            public int CreateAccumulator() => 0;

            public int Add(string value, int accumulator) => accumulator + 1;

            public int GetResult(int accumulator) => accumulator;

            public int Merge(int a, int b) => a + b;
        }

        #endregion
    }
}
