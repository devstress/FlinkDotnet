using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FlinkDotNet.DataStream;
using FlinkDotNet.DataStream.Runtime;
using NUnit.Framework;
using RuntimeOnTimerContext = FlinkDotNet.DataStream.Runtime.OnTimerContext;

namespace FlinkDotNet.DataStream.Tests
{
    [TestFixture]
    public class RuntimeImplementationTests
    {
        #region ProcessContext Tests

        [Test]
        public void ProcessContext_DefaultTimestamp_IsZero()
        {
            var ctx = new ProcessContext();

            Assert.That(ctx.Timestamp, Is.EqualTo(0));
        }

        [Test]
        public void ProcessContext_SetTimestamp_ReturnsValue()
        {
            var ctx = new ProcessContext { Timestamp = 12345L };

            Assert.That(ctx.Timestamp, Is.EqualTo(12345L));
        }

        [Test]
        public void ProcessContext_CurrentProcessingTime_IsPositive()
        {
            var ctx = new ProcessContext();

            Assert.That(ctx.CurrentProcessingTime, Is.GreaterThan(0));
        }

        [Test]
        public void ProcessContext_DefaultWatermark_IsMinValue()
        {
            var ctx = new ProcessContext();

            Assert.That(ctx.CurrentWatermark, Is.EqualTo(long.MinValue));
        }

        [Test]
        public void ProcessContext_RegisterEventTimeTimer_AddsTimer()
        {
            var ctx = new ProcessContext();

            ctx.RegisterEventTimeTimer(1000L);
            ctx.RegisterEventTimeTimer(2000L);

            Assert.That(ctx.EventTimeTimers, Has.Count.EqualTo(2));
            Assert.That(ctx.EventTimeTimers, Does.Contain(1000L));
            Assert.That(ctx.EventTimeTimers, Does.Contain(2000L));
        }

        [Test]
        public void ProcessContext_RegisterProcessingTimeTimer_AddsTimer()
        {
            var ctx = new ProcessContext();

            ctx.RegisterProcessingTimeTimer(500L);

            Assert.That(ctx.ProcessingTimeTimers, Has.Count.EqualTo(1));
            Assert.That(ctx.ProcessingTimeTimers, Does.Contain(500L));
        }

        [Test]
        public void ProcessContext_DeleteEventTimeTimer_RemovesTimer()
        {
            var ctx = new ProcessContext();

            ctx.RegisterEventTimeTimer(1000L);
            ctx.RegisterEventTimeTimer(2000L);
            ctx.DeleteEventTimeTimer(1000L);

            Assert.That(ctx.EventTimeTimers, Has.Count.EqualTo(1));
            Assert.That(ctx.EventTimeTimers, Does.Not.Contain(1000L));
        }

        [Test]
        public void ProcessContext_DeleteProcessingTimeTimer_RemovesTimer()
        {
            var ctx = new ProcessContext();

            ctx.RegisterProcessingTimeTimer(500L);
            ctx.DeleteProcessingTimeTimer(500L);

            Assert.That(ctx.ProcessingTimeTimers, Is.Empty);
        }

        #endregion

        #region KeyedProcessContext Tests

        [Test]
        public void KeyedProcessContext_HasCurrentKey()
        {
            var ctx = new KeyedProcessContext<string>("myKey");

            Assert.That(ctx.CurrentKey, Is.EqualTo("myKey"));
        }

        [Test]
        public void KeyedProcessContext_ImplementsIKeyedProcessContext()
        {
            var ctx = new KeyedProcessContext<int>(42);

            Assert.That(ctx, Is.InstanceOf<IKeyedProcessContext<int>>());
            Assert.That(ctx, Is.InstanceOf<IProcessContext>());
        }

        [Test]
        public void KeyedProcessContext_InheritsTimerFunctionality()
        {
            var ctx = new KeyedProcessContext<string>("key");

            ctx.RegisterEventTimeTimer(1000L);

            Assert.That(ctx.EventTimeTimers, Has.Count.EqualTo(1));
        }

        #endregion

        #region OnTimerContext Tests

        [Test]
        public void OnTimerContext_EventTime_HasCorrectDomain()
        {
            var ctx = new RuntimeOnTimerContext(TimeDomain.EventTime);

            Assert.That(ctx.TimeDomain, Is.EqualTo(TimeDomain.EventTime));
        }

        [Test]
        public void OnTimerContext_ProcessingTime_HasCorrectDomain()
        {
            var ctx = new RuntimeOnTimerContext(TimeDomain.ProcessingTime);

            Assert.That(ctx.TimeDomain, Is.EqualTo(TimeDomain.ProcessingTime));
        }

        [Test]
        public void OnTimerContext_ImplementsIOnTimerContext()
        {
            var ctx = new RuntimeOnTimerContext(TimeDomain.EventTime);

            Assert.That(ctx, Is.InstanceOf<IOnTimerContext>());
            Assert.That(ctx, Is.InstanceOf<IProcessContext>());
        }

        #endregion

        #region KeyedOnTimerContext Tests

        [Test]
        public void KeyedOnTimerContext_HasKeyAndTimeDomain()
        {
            var ctx = new KeyedOnTimerContext<string>(TimeDomain.EventTime, "key1");

            Assert.That(ctx.CurrentKey, Is.EqualTo("key1"));
            Assert.That(ctx.TimeDomain, Is.EqualTo(TimeDomain.EventTime));
        }

        [Test]
        public void KeyedOnTimerContext_ImplementsIKeyedOnTimerContext()
        {
            var ctx = new KeyedOnTimerContext<int>(TimeDomain.ProcessingTime, 5);

            Assert.That(ctx, Is.InstanceOf<IKeyedOnTimerContext<int>>());
            Assert.That(ctx, Is.InstanceOf<IOnTimerContext>());
            Assert.That(ctx, Is.InstanceOf<IProcessContext>());
        }

        #endregion

        #region WindowContext Tests

        [Test]
        public void WindowContext_HasStartAndEnd()
        {
            var ctx = new WindowContext(1000L, 5000L);

            Assert.That(ctx.WindowStart, Is.EqualTo(1000L));
            Assert.That(ctx.WindowEnd, Is.EqualTo(5000L));
        }

        [Test]
        public void WindowContext_CurrentProcessingTime_IsPositive()
        {
            var ctx = new WindowContext(0, 1000);

            Assert.That(ctx.CurrentProcessingTime, Is.GreaterThan(0));
        }

        [Test]
        public void WindowContext_DefaultWatermark_IsMinValue()
        {
            var ctx = new WindowContext(0, 1000);

            Assert.That(ctx.CurrentWatermark, Is.EqualTo(long.MinValue));
        }

        [Test]
        public void WindowContext_ImplementsIWindowContext()
        {
            var ctx = new WindowContext(0, 1000);

            Assert.That(ctx, Is.InstanceOf<IWindowContext>());
        }

        #endregion

        #region ListCollector Tests

        [Test]
        public void ListCollector_InitialState_IsEmpty()
        {
            var collector = new ListCollector<string>();

            Assert.That(collector.Elements, Is.Empty);
        }

        [Test]
        public void ListCollector_Collect_AddsElement()
        {
            var collector = new ListCollector<int>();

            collector.Collect(42);

            Assert.That(collector.Elements, Has.Count.EqualTo(1));
            Assert.That(collector.Elements[0], Is.EqualTo(42));
        }

        [Test]
        public void ListCollector_CollectMultiple_AddsAll()
        {
            var collector = new ListCollector<string>();

            collector.Collect("a");
            collector.Collect("b");
            collector.Collect("c");

            Assert.That(collector.Elements, Is.EqualTo(new[] { "a", "b", "c" }));
        }

        [Test]
        public void ListCollector_Clear_RemovesAll()
        {
            var collector = new ListCollector<int>();

            collector.Collect(1);
            collector.Collect(2);
            collector.Clear();

            Assert.That(collector.Elements, Is.Empty);
        }

        [Test]
        public void ListCollector_ImplementsICollector()
        {
            var collector = new ListCollector<string>();

            Assert.That(collector, Is.InstanceOf<ICollector<string>>());
        }

        #endregion

        #region ResultFuture Tests

        [Test]
        public async Task ResultFuture_Complete_SetsResults()
        {
            var future = new ResultFuture<string>();

            future.Complete(new[] { "result1", "result2" });
            IEnumerable<string> results = await future.ResultTask;

            Assert.That(results, Is.EqualTo(new[] { "result1", "result2" }));
            Assert.That(future.IsCompleted, Is.True);
        }

        [Test]
        public void ResultFuture_CompleteExceptionally_SetsException()
        {
            var future = new ResultFuture<int>();
            var exception = new InvalidOperationException("test error");

            future.CompleteExceptionally(exception);

            Assert.That(future.IsCompleted, Is.True);
            Assert.ThrowsAsync<InvalidOperationException>(async () => await future.ResultTask);
        }

        [Test]
        public void ResultFuture_BeforeCompletion_IsNotCompleted()
        {
            var future = new ResultFuture<string>();

            Assert.That(future.IsCompleted, Is.False);
        }

        [Test]
        public async Task ResultFuture_CompleteWithNull_ReturnsEmptyCollection()
        {
            var future = new ResultFuture<string>();

            future.Complete(null!);
            IEnumerable<string> results = await future.ResultTask;

            Assert.That(results, Is.Empty);
        }

        [Test]
        public void ResultFuture_ImplementsIResultFuture()
        {
            var future = new ResultFuture<int>();

            Assert.That(future, Is.InstanceOf<IResultFuture<int>>());
        }

        #endregion

        #region SourceOutput Tests

        [Test]
        public void SourceOutput_InitialState_IsEmpty()
        {
            var output = new SourceOutput<string>();

            Assert.That(output.Elements, Is.Empty);
            Assert.That(output.CurrentWatermark, Is.EqualTo(long.MinValue));
        }

        [Test]
        public void SourceOutput_Collect_AddsElement()
        {
            var output = new SourceOutput<int>();

            output.Collect(42);

            Assert.That(output.Elements, Has.Count.EqualTo(1));
            Assert.That(output.Elements[0].Element, Is.EqualTo(42));
        }

        [Test]
        public void SourceOutput_CollectWithTimestamp_AddsElementWithTimestamp()
        {
            var output = new SourceOutput<string>();

            output.Collect("event", 12345L);

            Assert.That(output.Elements, Has.Count.EqualTo(1));
            Assert.That(output.Elements[0].Element, Is.EqualTo("event"));
            Assert.That(output.Elements[0].Timestamp, Is.EqualTo(12345L));
        }

        [Test]
        public void SourceOutput_EmitWatermark_UpdatesWatermark()
        {
            var output = new SourceOutput<int>();

            output.EmitWatermark(5000L);

            Assert.That(output.CurrentWatermark, Is.EqualTo(5000L));
        }

        [Test]
        public void SourceOutput_Clear_RemovesAllElements()
        {
            var output = new SourceOutput<string>();

            output.Collect("a");
            output.Collect("b");
            output.Clear();

            Assert.That(output.Elements, Is.Empty);
        }

        [Test]
        public void SourceOutput_ImplementsISourceOutput()
        {
            var output = new SourceOutput<int>();

            Assert.That(output, Is.InstanceOf<ISourceOutput<int>>());
        }

        #endregion

        #region JsonDeserializationSchema Tests

        [Test]
        public void JsonDeserializationSchema_Deserialize_ReturnsObject()
        {
            var schema = new JsonDeserializationSchema<TestRecord>();
            byte[] bytes = Encoding.UTF8.GetBytes("{\"Name\":\"test\",\"Value\":42}");

            TestRecord result = schema.Deserialize(bytes);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("test"));
            Assert.That(result.Value, Is.EqualTo(42));
        }

        [Test]
        public void JsonDeserializationSchema_IsEndOfStream_ReturnsFalse()
        {
            var schema = new JsonDeserializationSchema<string>();

            Assert.That(schema.IsEndOfStream("test"), Is.False);
        }

        [Test]
        public void JsonDeserializationSchema_GetProducedType_ReturnsTypeInformation()
        {
            var schema = new JsonDeserializationSchema<string>();

            TypeInformation<string> typeInfo = schema.GetProducedType();

            Assert.That(typeInfo, Is.Not.Null);
        }

        [Test]
        public void JsonDeserializationSchema_ImplementsIDeserializationSchema()
        {
            var schema = new JsonDeserializationSchema<string>();

            Assert.That(schema, Is.InstanceOf<IDeserializationSchema<string>>());
        }

        #endregion

        #region JsonSerializationSchema Tests

        [Test]
        public void JsonSerializationSchema_Serialize_ReturnsBytes()
        {
            var schema = new JsonSerializationSchema<TestRecord>();
            var record = new TestRecord { Name = "test", Value = 42 };

            byte[] bytes = schema.Serialize(record);

            Assert.That(bytes, Is.Not.Null);
            Assert.That(bytes.Length, Is.GreaterThan(0));
        }

        [Test]
        public void JsonSerializationSchema_RoundTrip_PreservesData()
        {
            var serSchema = new JsonSerializationSchema<TestRecord>();
            var deSchema = new JsonDeserializationSchema<TestRecord>();
            var original = new TestRecord { Name = "hello", Value = 99 };

            byte[] bytes = serSchema.Serialize(original);
            TestRecord result = deSchema.Deserialize(bytes);

            Assert.That(result.Name, Is.EqualTo("hello"));
            Assert.That(result.Value, Is.EqualTo(99));
        }

        [Test]
        public void JsonSerializationSchema_ImplementsISerializationSchema()
        {
            var schema = new JsonSerializationSchema<string>();

            Assert.That(schema, Is.InstanceOf<ISerializationSchema<string>>());
        }

        #endregion

        #region JsonSimpleVersionedSerializer Tests

        [Test]
        public void JsonSimpleVersionedSerializer_DefaultVersion_IsOne()
        {
            var serializer = new JsonSimpleVersionedSerializer<string>();

            Assert.That(serializer.Version, Is.EqualTo(1));
        }

        [Test]
        public void JsonSimpleVersionedSerializer_CustomVersion_IsSet()
        {
            var serializer = new JsonSimpleVersionedSerializer<string>(version: 3);

            Assert.That(serializer.Version, Is.EqualTo(3));
        }

        [Test]
        public void JsonSimpleVersionedSerializer_RoundTrip_PreservesData()
        {
            var serializer = new JsonSimpleVersionedSerializer<TestRecord>();
            var original = new TestRecord { Name = "data", Value = 7 };

            byte[] bytes = serializer.Serialize(original);
            TestRecord result = serializer.Deserialize(serializer.Version, bytes);

            Assert.That(result.Name, Is.EqualTo("data"));
            Assert.That(result.Value, Is.EqualTo(7));
        }

        [Test]
        public void JsonSimpleVersionedSerializer_ImplementsISimpleVersionedSerializer()
        {
            var serializer = new JsonSimpleVersionedSerializer<string>();

            Assert.That(serializer, Is.InstanceOf<ISimpleVersionedSerializer<string>>());
        }

        #endregion

        #region Test Helpers

        public sealed class TestRecord
        {
            public string Name { get; set; } = string.Empty;
            public int Value { get; set; }
        }

        #endregion
    }
}
