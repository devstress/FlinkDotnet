using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Tests that validate the FlinkDotNet state and runtime API contracts
    /// are correctly defined as pass-through interfaces for Flink Java translation.
    /// These interfaces are NOT implemented in .NET — they define the API surface
    /// that maps to org.apache.flink.api.common.state.* in Java Flink.
    /// </summary>
    [TestFixture]
    public class FlinkApiContractTests
    {
        #region IValueState<T> Contract Tests

        [Test]
        public void IValueState_IsInterface()
        {
            Assert.That(typeof(IValueState<>).IsInterface, Is.True);
        }

        [Test]
        public void IValueState_DefinesValueAsync()
        {
            var method = typeof(IValueState<string>).GetMethod("ValueAsync");
            Assert.That(method, Is.Not.Null);
            Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task<string>)));
            Assert.That(method.GetParameters(), Is.Empty);
        }

        [Test]
        public void IValueState_DefinesUpdateAsync()
        {
            var method = typeof(IValueState<string>).GetMethod("UpdateAsync");
            Assert.That(method, Is.Not.Null);
            Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task)));
            Assert.That(method.GetParameters(), Has.Length.EqualTo(1));
            Assert.That(method.GetParameters()[0].ParameterType, Is.EqualTo(typeof(string)));
        }

        [Test]
        public void IValueState_DefinesClearAsync()
        {
            var method = typeof(IValueState<string>).GetMethod("ClearAsync");
            Assert.That(method, Is.Not.Null);
            Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task)));
            Assert.That(method.GetParameters(), Is.Empty);
        }

        [Test]
        public void IValueState_CanBeMocked_ForUserDefinedFunctions()
        {
            // Validates the interface can be used in user-defined process functions
            // via dependency injection from the Flink Java runtime
            var mockState = new Mock<IValueState<int>>();
            mockState.Setup(s => s.ValueAsync()).ReturnsAsync(42);
            mockState.Setup(s => s.UpdateAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
            mockState.Setup(s => s.ClearAsync()).Returns(Task.CompletedTask);

            Assert.That(mockState.Object, Is.Not.Null);
            Assert.That(mockState.Object, Is.InstanceOf<IValueState<int>>());
        }

        [Test]
        public async Task IValueState_MockedInProcessFunction_WorksCorrectly()
        {
            // Simulates how user code interacts with state provided by Flink Java runtime
            var mockState = new Mock<IValueState<int>>();
            mockState.Setup(s => s.ValueAsync()).ReturnsAsync(100);
            mockState.Setup(s => s.UpdateAsync(It.IsAny<int>())).Returns(Task.CompletedTask);

            int value = await mockState.Object.ValueAsync();
            Assert.That(value, Is.EqualTo(100));

            await mockState.Object.UpdateAsync(200);
            mockState.Verify(s => s.UpdateAsync(200), Times.Once);
        }

        #endregion

        #region IListState<T> Contract Tests

        [Test]
        public void IListState_IsInterface()
        {
            Assert.That(typeof(IListState<>).IsInterface, Is.True);
        }

        [Test]
        public void IListState_DefinesGetAsync()
        {
            var method = typeof(IListState<string>).GetMethod("GetAsync");
            Assert.That(method, Is.Not.Null);
            Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task<IEnumerable<string>>)));
        }

        [Test]
        public void IListState_DefinesAddAsync()
        {
            var method = typeof(IListState<string>).GetMethod("AddAsync");
            Assert.That(method, Is.Not.Null);
            Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task)));
            Assert.That(method.GetParameters(), Has.Length.EqualTo(1));
        }

        [Test]
        public void IListState_DefinesAddAllAsync()
        {
            var method = typeof(IListState<string>).GetMethod("AddAllAsync");
            Assert.That(method, Is.Not.Null);
            Assert.That(method!.GetParameters()[0].ParameterType, Is.EqualTo(typeof(IEnumerable<string>)));
        }

        [Test]
        public void IListState_DefinesUpdateAsync()
        {
            var method = typeof(IListState<string>).GetMethod("UpdateAsync");
            Assert.That(method, Is.Not.Null);
            Assert.That(method!.GetParameters()[0].ParameterType, Is.EqualTo(typeof(IEnumerable<string>)));
        }

        [Test]
        public void IListState_DefinesClearAsync()
        {
            var method = typeof(IListState<string>).GetMethod("ClearAsync");
            Assert.That(method, Is.Not.Null);
        }

        [Test]
        public void IListState_CanBeMocked_ForUserDefinedFunctions()
        {
            var mockState = new Mock<IListState<string>>();
            mockState.Setup(s => s.GetAsync()).ReturnsAsync(new[] { "a", "b" });
            mockState.Setup(s => s.AddAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

            Assert.That(mockState.Object, Is.InstanceOf<IListState<string>>());
        }

        #endregion

        #region IMapState<TKey, TValue> Contract Tests

        [Test]
        public void IMapState_IsInterface()
        {
            Assert.That(typeof(IMapState<,>).IsInterface, Is.True);
        }

        [Test]
        public void IMapState_DefinesGetAsync()
        {
            var method = typeof(IMapState<string, int>).GetMethod("GetAsync");
            Assert.That(method, Is.Not.Null);
            Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task<int>)));
        }

        [Test]
        public void IMapState_DefinesPutAsync()
        {
            var method = typeof(IMapState<string, int>).GetMethod("PutAsync");
            Assert.That(method, Is.Not.Null);
            Assert.That(method!.GetParameters(), Has.Length.EqualTo(2));
        }

        [Test]
        public void IMapState_DefinesPutAllAsync()
        {
            var method = typeof(IMapState<string, int>).GetMethod("PutAllAsync");
            Assert.That(method, Is.Not.Null);
        }

        [Test]
        public void IMapState_DefinesRemoveAsync()
        {
            var method = typeof(IMapState<string, int>).GetMethod("RemoveAsync");
            Assert.That(method, Is.Not.Null);
        }

        [Test]
        public void IMapState_DefinesContainsAsync()
        {
            var method = typeof(IMapState<string, int>).GetMethod("ContainsAsync");
            Assert.That(method, Is.Not.Null);
            Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task<bool>)));
        }

        [Test]
        public void IMapState_DefinesEntriesAsync()
        {
            var method = typeof(IMapState<string, int>).GetMethod("EntriesAsync");
            Assert.That(method, Is.Not.Null);
        }

        [Test]
        public void IMapState_DefinesKeysAsync()
        {
            var method = typeof(IMapState<string, int>).GetMethod("KeysAsync");
            Assert.That(method, Is.Not.Null);
        }

        [Test]
        public void IMapState_DefinesValuesAsync()
        {
            var method = typeof(IMapState<string, int>).GetMethod("ValuesAsync");
            Assert.That(method, Is.Not.Null);
        }

        [Test]
        public void IMapState_DefinesIsEmptyAsync()
        {
            var method = typeof(IMapState<string, int>).GetMethod("IsEmptyAsync");
            Assert.That(method, Is.Not.Null);
            Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task<bool>)));
        }

        [Test]
        public void IMapState_CanBeMocked_ForUserDefinedFunctions()
        {
            var mockState = new Mock<IMapState<string, int>>();
            mockState.Setup(s => s.GetAsync("key")).ReturnsAsync(42);
            mockState.Setup(s => s.ContainsAsync("key")).ReturnsAsync(true);

            Assert.That(mockState.Object, Is.InstanceOf<IMapState<string, int>>());
        }

        #endregion

        #region IReducingState<T> Contract Tests

        [Test]
        public void IReducingState_IsInterface()
        {
            Assert.That(typeof(IReducingState<>).IsInterface, Is.True);
        }

        [Test]
        public void IReducingState_DefinesGetAsync()
        {
            var method = typeof(IReducingState<int>).GetMethod("GetAsync");
            Assert.That(method, Is.Not.Null);
            Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task<int>)));
        }

        [Test]
        public void IReducingState_DefinesAddAsync()
        {
            var method = typeof(IReducingState<int>).GetMethod("AddAsync");
            Assert.That(method, Is.Not.Null);
        }

        [Test]
        public void IReducingState_DefinesClearAsync()
        {
            var method = typeof(IReducingState<int>).GetMethod("ClearAsync");
            Assert.That(method, Is.Not.Null);
        }

        #endregion

        #region IAggregatingState<TIn, TOut> Contract Tests

        [Test]
        public void IAggregatingState_IsInterface()
        {
            Assert.That(typeof(IAggregatingState<,>).IsInterface, Is.True);
        }

        [Test]
        public void IAggregatingState_DefinesGetAsync()
        {
            var method = typeof(IAggregatingState<string, int>).GetMethod("GetAsync");
            Assert.That(method, Is.Not.Null);
            Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task<int>)));
        }

        [Test]
        public void IAggregatingState_DefinesAddAsync()
        {
            var method = typeof(IAggregatingState<string, int>).GetMethod("AddAsync");
            Assert.That(method, Is.Not.Null);
            Assert.That(method!.GetParameters()[0].ParameterType, Is.EqualTo(typeof(string)));
        }

        [Test]
        public void IAggregatingState_DefinesClearAsync()
        {
            var method = typeof(IAggregatingState<string, int>).GetMethod("ClearAsync");
            Assert.That(method, Is.Not.Null);
        }

        #endregion

        #region State Descriptor IR Translation Tests

        [Test]
        public void ValueStateDescriptor_CarriesTypeInfo_ForIRTranslation()
        {
            var descriptor = new ValueStateDescriptor<string>("myValueState");

            Assert.That(descriptor.Name, Is.EqualTo("myValueState"));
            Assert.That(descriptor.ValueType, Is.EqualTo(typeof(string)));
            Assert.That(descriptor, Is.InstanceOf<StateDescriptor>());
        }

        [Test]
        public void ListStateDescriptor_CarriesTypeInfo_ForIRTranslation()
        {
            var descriptor = new ListStateDescriptor<int>("myListState");

            Assert.That(descriptor.Name, Is.EqualTo("myListState"));
            Assert.That(descriptor.ElementType, Is.EqualTo(typeof(int)));
            Assert.That(descriptor, Is.InstanceOf<StateDescriptor>());
        }

        [Test]
        public void MapStateDescriptor_CarriesTypeInfo_ForIRTranslation()
        {
            var descriptor = new MapStateDescriptor<string, double>("myMapState");

            Assert.That(descriptor.Name, Is.EqualTo("myMapState"));
            Assert.That(descriptor.KeyType, Is.EqualTo(typeof(string)));
            Assert.That(descriptor.ValueType, Is.EqualTo(typeof(double)));
            Assert.That(descriptor, Is.InstanceOf<StateDescriptor>());
        }

        [Test]
        public void ReducingStateDescriptor_CarriesReduceFunction_ForIRTranslation()
        {
            var mockReduceFunc = new Mock<IReduceFunction<int>>();
            var descriptor = new ReducingStateDescriptor<int>("myReducingState", mockReduceFunc.Object);

            Assert.That(descriptor.Name, Is.EqualTo("myReducingState"));
            Assert.That(descriptor.ReduceFunction, Is.SameAs(mockReduceFunc.Object));
            Assert.That(descriptor, Is.InstanceOf<StateDescriptor>());
        }

        [Test]
        public void AggregatingStateDescriptor_CarriesAggregateFunction_ForIRTranslation()
        {
            var mockAggFunc = new Mock<IAggregateFunction<string, int, int>>();
            var descriptor = new AggregatingStateDescriptor<string, int, int>("myAggState", mockAggFunc.Object);

            Assert.That(descriptor.Name, Is.EqualTo("myAggState"));
            Assert.That(descriptor.AggregateFunction, Is.SameAs(mockAggFunc.Object));
            Assert.That(descriptor, Is.InstanceOf<StateDescriptor>());
        }

        [Test]
        public void StateDescriptor_NullName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ValueStateDescriptor<string>(null!));
            Assert.Throws<ArgumentNullException>(() => new ListStateDescriptor<int>(null!));
            Assert.Throws<ArgumentNullException>(() => new MapStateDescriptor<string, int>(null!));
        }

        [Test]
        public void ReducingStateDescriptor_NullFunction_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ReducingStateDescriptor<int>("state", null!));
        }

        [Test]
        public void AggregatingStateDescriptor_NullFunction_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new AggregatingStateDescriptor<string, int, int>("state", null!));
        }

        #endregion

        #region Context Interface Contract Tests

        [Test]
        public void IProcessContext_IsInterface()
        {
            Assert.That(typeof(IProcessContext).IsInterface, Is.True);
        }

        [Test]
        public void IProcessContext_DefinesTimestamp()
        {
            var prop = typeof(IProcessContext).GetProperty("Timestamp");
            Assert.That(prop, Is.Not.Null);
            Assert.That(prop!.PropertyType, Is.EqualTo(typeof(long)));
        }

        [Test]
        public void IProcessContext_DefinesCurrentProcessingTime()
        {
            var prop = typeof(IProcessContext).GetProperty("CurrentProcessingTime");
            Assert.That(prop, Is.Not.Null);
            Assert.That(prop!.PropertyType, Is.EqualTo(typeof(long)));
        }

        [Test]
        public void IProcessContext_DefinesCurrentWatermark()
        {
            var prop = typeof(IProcessContext).GetProperty("CurrentWatermark");
            Assert.That(prop, Is.Not.Null);
            Assert.That(prop!.PropertyType, Is.EqualTo(typeof(long)));
        }

        [Test]
        public void IProcessContext_DefinesTimerRegistration()
        {
            var regEvent = typeof(IProcessContext).GetMethod("RegisterEventTimeTimer");
            var regProc = typeof(IProcessContext).GetMethod("RegisterProcessingTimeTimer");
            var delEvent = typeof(IProcessContext).GetMethod("DeleteEventTimeTimer");
            var delProc = typeof(IProcessContext).GetMethod("DeleteProcessingTimeTimer");

            Assert.That(regEvent, Is.Not.Null);
            Assert.That(regProc, Is.Not.Null);
            Assert.That(delEvent, Is.Not.Null);
            Assert.That(delProc, Is.Not.Null);
        }

        [Test]
        public void IKeyedProcessContext_ExtendsIProcessContext()
        {
            Assert.That(typeof(IKeyedProcessContext<>).GetInterfaces(),
                Does.Contain(typeof(IProcessContext)));
        }

        [Test]
        public void IKeyedProcessContext_DefinesCurrentKey()
        {
            var prop = typeof(IKeyedProcessContext<string>).GetProperty("CurrentKey");
            Assert.That(prop, Is.Not.Null);
            Assert.That(prop!.PropertyType, Is.EqualTo(typeof(string)));
        }

        [Test]
        public void IOnTimerContext_ExtendsIProcessContext()
        {
            Assert.That(typeof(IOnTimerContext).GetInterfaces(),
                Does.Contain(typeof(IProcessContext)));
        }

        [Test]
        public void IOnTimerContext_DefinesTimeDomain()
        {
            var prop = typeof(IOnTimerContext).GetProperty("TimeDomain");
            Assert.That(prop, Is.Not.Null);
            Assert.That(prop!.PropertyType, Is.EqualTo(typeof(TimeDomain)));
        }

        [Test]
        public void IKeyedOnTimerContext_ExtendsIOnTimerContext()
        {
            Assert.That(typeof(IKeyedOnTimerContext<>).GetInterfaces(),
                Does.Contain(typeof(IOnTimerContext)));
        }

        [Test]
        public void IKeyedOnTimerContext_DefinesCurrentKey()
        {
            var prop = typeof(IKeyedOnTimerContext<string>).GetProperty("CurrentKey");
            Assert.That(prop, Is.Not.Null);
            Assert.That(prop!.PropertyType, Is.EqualTo(typeof(string)));
        }

        [Test]
        public void IWindowContext_IsInterface()
        {
            Assert.That(typeof(IWindowContext).IsInterface, Is.True);
        }

        [Test]
        public void IWindowContext_DefinesWindowStartAndEnd()
        {
            var start = typeof(IWindowContext).GetProperty("WindowStart");
            var end = typeof(IWindowContext).GetProperty("WindowEnd");
            Assert.That(start, Is.Not.Null);
            Assert.That(end, Is.Not.Null);
            Assert.That(start!.PropertyType, Is.EqualTo(typeof(long)));
            Assert.That(end!.PropertyType, Is.EqualTo(typeof(long)));
        }

        [Test]
        public void IWindowContext_DefinesProcessingTimeAndWatermark()
        {
            var procTime = typeof(IWindowContext).GetProperty("CurrentProcessingTime");
            var watermark = typeof(IWindowContext).GetProperty("CurrentWatermark");
            Assert.That(procTime, Is.Not.Null);
            Assert.That(watermark, Is.Not.Null);
        }

        #endregion

        #region ICollector<T> Contract Tests

        [Test]
        public void ICollector_IsInterface()
        {
            Assert.That(typeof(ICollector<>).IsInterface, Is.True);
        }

        [Test]
        public void ICollector_DefinesCollect()
        {
            var method = typeof(ICollector<string>).GetMethod("Collect");
            Assert.That(method, Is.Not.Null);
            Assert.That(method!.GetParameters(), Has.Length.EqualTo(1));
            Assert.That(method.GetParameters()[0].ParameterType, Is.EqualTo(typeof(string)));
        }

        [Test]
        public void ICollector_CanBeMocked_ForUserDefinedFunctions()
        {
            var collected = new List<string>();
            var mockCollector = new Mock<ICollector<string>>();
            mockCollector.Setup(c => c.Collect(It.IsAny<string>()))
                .Callback<string>(s => collected.Add(s));

            mockCollector.Object.Collect("hello");
            mockCollector.Object.Collect("world");

            Assert.That(collected, Has.Count.EqualTo(2));
            Assert.That(collected, Is.EqualTo(new[] { "hello", "world" }));
        }

        #endregion

        #region IResultFuture<T> Contract Tests

        [Test]
        public void IResultFuture_IsInterface()
        {
            Assert.That(typeof(IResultFuture<>).IsInterface, Is.True);
        }

        [Test]
        public void IResultFuture_DefinesComplete()
        {
            var method = typeof(IResultFuture<string>).GetMethod("Complete");
            Assert.That(method, Is.Not.Null);
        }

        [Test]
        public void IResultFuture_DefinesCompleteExceptionally()
        {
            var method = typeof(IResultFuture<string>).GetMethod("CompleteExceptionally");
            Assert.That(method, Is.Not.Null);
            Assert.That(method!.GetParameters()[0].ParameterType, Is.EqualTo(typeof(Exception)));
        }

        #endregion

        #region Function Interface Contract Tests

        [Test]
        public void IProcessFunction_DefinesProcessElementAsync()
        {
            var method = typeof(IProcessFunction<string, int>).GetMethod("ProcessElementAsync");
            Assert.That(method, Is.Not.Null);
            Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task)));

            var parameters = method.GetParameters();
            Assert.That(parameters, Has.Length.EqualTo(3));
            Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(string)));
            Assert.That(parameters[1].ParameterType, Is.EqualTo(typeof(IProcessContext)));
            Assert.That(parameters[2].ParameterType, Is.EqualTo(typeof(ICollector<int>)));
        }

        [Test]
        public void IProcessFunction_DefinesOnTimerAsync()
        {
            var method = typeof(IProcessFunction<string, int>).GetMethod("OnTimerAsync");
            Assert.That(method, Is.Not.Null);

            var parameters = method!.GetParameters();
            Assert.That(parameters, Has.Length.EqualTo(3));
            Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(long)));
            Assert.That(parameters[1].ParameterType, Is.EqualTo(typeof(IOnTimerContext)));
            Assert.That(parameters[2].ParameterType, Is.EqualTo(typeof(ICollector<int>)));
        }

        [Test]
        public void IKeyedProcessFunction_DefinesProcessElementAsync()
        {
            var method = typeof(IKeyedProcessFunction<string, int, string>).GetMethod("ProcessElementAsync");
            Assert.That(method, Is.Not.Null);

            var parameters = method!.GetParameters();
            Assert.That(parameters, Has.Length.EqualTo(3));
            Assert.That(parameters[1].ParameterType, Is.EqualTo(typeof(IKeyedProcessContext<string>)));
        }

        [Test]
        public void ICoProcessFunction_DefinesBothProcessElements()
        {
            var proc1 = typeof(ICoProcessFunction<string, int, bool>).GetMethod("ProcessElement1Async");
            var proc2 = typeof(ICoProcessFunction<string, int, bool>).GetMethod("ProcessElement2Async");
            Assert.That(proc1, Is.Not.Null);
            Assert.That(proc2, Is.Not.Null);
        }

        [Test]
        public void IProcessWindowFunction_DefinesProcessAsync()
        {
            var method = typeof(IProcessWindowFunction<string, int, string>).GetMethod("ProcessAsync");
            Assert.That(method, Is.Not.Null);

            var parameters = method!.GetParameters();
            Assert.That(parameters, Has.Length.EqualTo(4));
            Assert.That(parameters[2].ParameterType, Is.EqualTo(typeof(IWindowContext)));
            Assert.That(parameters[3].ParameterType, Is.EqualTo(typeof(ICollector<int>)));
        }

        [Test]
        public void IAsyncFunction_DefinesAsyncInvokeAsync()
        {
            var method = typeof(IAsyncFunction<string, int>).GetMethod("AsyncInvokeAsync");
            Assert.That(method, Is.Not.Null);

            var parameters = method!.GetParameters();
            Assert.That(parameters, Has.Length.EqualTo(2));
            Assert.That(parameters[1].ParameterType, Is.EqualTo(typeof(IResultFuture<int>)));
        }

        [Test]
        public void IJoinFunction_DefinesJoin()
        {
            var method = typeof(IJoinFunction<string, int, bool>).GetMethod("Join");
            Assert.That(method, Is.Not.Null);
            Assert.That(method!.ReturnType, Is.EqualTo(typeof(bool)));
        }

        [Test]
        public void IFlatJoinFunction_DefinesJoin()
        {
            var method = typeof(IFlatJoinFunction<string, int, bool>).GetMethod("Join");
            Assert.That(method, Is.Not.Null);
            Assert.That(method!.ReturnType, Is.EqualTo(typeof(IEnumerable<bool>)));
        }

        [Test]
        public void ICoGroupFunction_DefinesCoGroup()
        {
            var method = typeof(ICoGroupFunction<string, int, bool>).GetMethod("CoGroup");
            Assert.That(method, Is.Not.Null);
            Assert.That(method!.ReturnType, Is.EqualTo(typeof(IEnumerable<bool>)));
        }

        #endregion

        #region User-Defined Function Integration Pattern Tests

        [Test]
        public async Task UserProcessFunction_CanBeImplemented_WithMockedRuntime()
        {
            // Demonstrates the pass-through pattern: user implements the function,
            // Flink Java runtime provides the context and collector

            var userFunction = new SampleProcessFunction();
            var mockCtx = new Mock<IProcessContext>();
            mockCtx.Setup(c => c.Timestamp).Returns(1000L);
            mockCtx.Setup(c => c.CurrentProcessingTime).Returns(2000L);

            var collected = new List<string>();
            var mockCollector = new Mock<ICollector<string>>();
            mockCollector.Setup(c => c.Collect(It.IsAny<string>()))
                .Callback<string>(s => collected.Add(s));

            await userFunction.ProcessElementAsync(42, mockCtx.Object, mockCollector.Object);

            Assert.That(collected, Has.Count.EqualTo(1));
            Assert.That(collected[0], Does.Contain("42"));
        }

        [Test]
        public async Task UserKeyedProcessFunction_CanAccessKey_FromMockedRuntime()
        {
            var userFunction = new SampleKeyedProcessFunction();
            var mockCtx = new Mock<IKeyedProcessContext<string>>();
            mockCtx.Setup(c => c.CurrentKey).Returns("user-123");
            mockCtx.Setup(c => c.Timestamp).Returns(5000L);

            var collected = new List<string>();
            var mockCollector = new Mock<ICollector<string>>();
            mockCollector.Setup(c => c.Collect(It.IsAny<string>()))
                .Callback<string>(s => collected.Add(s));

            await userFunction.ProcessElementAsync(42, mockCtx.Object, mockCollector.Object);

            Assert.That(collected, Has.Count.EqualTo(1));
            Assert.That(collected[0], Does.Contain("user-123"));
        }

        [Test]
        public async Task UserAsyncFunction_CanCompleteResultFuture_FromMockedRuntime()
        {
            var userFunction = new SampleAsyncFunction();
            var completedResults = new List<IEnumerable<string>>();
            var mockFuture = new Mock<IResultFuture<string>>();
            mockFuture.Setup(f => f.Complete(It.IsAny<IEnumerable<string>>()))
                .Callback<IEnumerable<string>>(r => completedResults.Add(r));

            await userFunction.AsyncInvokeAsync(42, mockFuture.Object);

            Assert.That(completedResults, Has.Count.EqualTo(1));
            Assert.That(completedResults[0].First(), Is.EqualTo("result-42"));
        }

        [Test]
        public async Task UserProcessFunction_CanRegisterTimers_OnMockedContext()
        {
            var userFunction = new TimerRegisteringProcessFunction();
            var registeredTimers = new List<long>();
            var mockCtx = new Mock<IProcessContext>();
            mockCtx.Setup(c => c.RegisterEventTimeTimer(It.IsAny<long>()))
                .Callback<long>(t => registeredTimers.Add(t));
            mockCtx.Setup(c => c.Timestamp).Returns(1000L);

            var mockCollector = new Mock<ICollector<string>>();

            await userFunction.ProcessElementAsync("input", mockCtx.Object, mockCollector.Object);

            Assert.That(registeredTimers, Has.Count.EqualTo(1));
            Assert.That(registeredTimers[0], Is.EqualTo(6000L));
        }

        [Test]
        public async Task UserProcessWindowFunction_CanProcessWindow_WithMockedContext()
        {
            var userFunction = new SampleWindowFunction();
            var mockCtx = new Mock<IWindowContext>();
            mockCtx.Setup(c => c.WindowStart).Returns(0L);
            mockCtx.Setup(c => c.WindowEnd).Returns(60000L);

            var collected = new List<int>();
            var mockCollector = new Mock<ICollector<int>>();
            mockCollector.Setup(c => c.Collect(It.IsAny<int>()))
                .Callback<int>(v => collected.Add(v));

            var elements = new[] { 10, 20, 30 };
            await userFunction.ProcessAsync("key", elements, mockCtx.Object, mockCollector.Object);

            Assert.That(collected, Has.Count.EqualTo(1));
            Assert.That(collected[0], Is.EqualTo(60));
        }

        #endregion

        #region No In-Memory State Implementations Exist Tests

        [Test]
        public void NoInMemoryStateImplementations_ExistInAssembly()
        {
            // Verify that no concrete in-memory state implementations exist
            // State is managed by the Flink Java runtime, not by .NET
            var assembly = typeof(IValueState<>).Assembly;
            var types = assembly.GetTypes();

            var inMemoryTypes = types.Where(t =>
                t.Name.StartsWith("InMemory", StringComparison.Ordinal) &&
                t.Namespace?.Contains("State") == true).ToList();

            Assert.That(inMemoryTypes, Is.Empty,
                "No in-memory state implementations should exist. " +
                "State is managed by the Flink Java runtime.");
        }

        [Test]
        public void NoRuntimeContextImplementations_ExistInAssembly()
        {
            // Verify that no concrete runtime context implementations exist in Runtime namespace
            // Contexts are provided by the Flink Java runtime
            var assembly = typeof(IProcessContext).Assembly;
            var types = assembly.GetTypes();

            var runtimeTypes = types.Where(t =>
                t.Namespace == "FlinkDotNet.DataStream.Runtime" &&
                t.IsClass &&
                !t.IsAbstract).ToList();

            Assert.That(runtimeTypes, Is.Empty,
                "No concrete runtime implementations should exist. " +
                "Contexts, collectors, and result futures are provided by the Flink Java runtime.");
        }

        #endregion

        #region Sample User-Defined Functions (for testing the pass-through pattern)

        private sealed class SampleProcessFunction : IProcessFunction<int, string>
        {
            public Task ProcessElementAsync(int value, IProcessContext ctx, ICollector<string> @out)
            {
                @out.Collect($"Processed: {value} at {ctx.Timestamp}");
                return Task.CompletedTask;
            }

            public Task OnTimerAsync(long timestamp, IOnTimerContext ctx, ICollector<string> @out)
            {
                @out.Collect($"Timer fired at {timestamp}");
                return Task.CompletedTask;
            }
        }

        private sealed class SampleKeyedProcessFunction : IKeyedProcessFunction<string, int, string>
        {
            public Task ProcessElementAsync(int value, IKeyedProcessContext<string> ctx, ICollector<string> @out)
            {
                @out.Collect($"Key: {ctx.CurrentKey}, Value: {value}");
                return Task.CompletedTask;
            }

            public Task OnTimerAsync(long timestamp, IKeyedOnTimerContext<string> ctx, ICollector<string> @out)
            {
                @out.Collect($"Timer for key {ctx.CurrentKey} at {timestamp}");
                return Task.CompletedTask;
            }
        }

        private sealed class SampleAsyncFunction : IAsyncFunction<int, string>
        {
            public Task AsyncInvokeAsync(int input, IResultFuture<string> resultFuture)
            {
                resultFuture.Complete(new[] { $"result-{input}" });
                return Task.CompletedTask;
            }
        }

        private sealed class TimerRegisteringProcessFunction : IProcessFunction<string, string>
        {
            public Task ProcessElementAsync(string value, IProcessContext ctx, ICollector<string> @out)
            {
                ctx.RegisterEventTimeTimer(ctx.Timestamp + 5000L);
                return Task.CompletedTask;
            }

            public Task OnTimerAsync(long timestamp, IOnTimerContext ctx, ICollector<string> @out)
            {
                @out.Collect($"Timer at {timestamp}, domain: {ctx.TimeDomain}");
                return Task.CompletedTask;
            }
        }

        private sealed class SampleWindowFunction : IProcessWindowFunction<int, int, string>
        {
            public Task ProcessAsync(string key, IEnumerable<int> elements, IWindowContext ctx, ICollector<int> @out)
            {
                @out.Collect(elements.Sum());
                return Task.CompletedTask;
            }
        }

        #endregion
    }
}
