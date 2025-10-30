using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Tests for ProcessTableFunction-related classes, GroupedTable, and various utility classes
    /// to achieve final coverage push to 90%.
    /// </summary>
    [TestFixture]
    public class AdditionalUtilityClassesTests
    {
        private StreamExecutionEnvironment _env = null!;

        [SetUp]
        public void Setup()
        {
            _env = StreamExecutionEnvironment.GetExecutionEnvironment();
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", "http://localhost:8080");
        }

        [TearDown]
        public void TearDown()
        {
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", null);
        }

        #region ProcessingContext Tests

        [Test]
        public void ProcessingContext_Timestamp_ShouldGetAndSet()
        {
            // Arrange
            var context = new ProcessingContext();

            // Act
            context.Timestamp = 1234567890;

            // Assert
            Assert.That(context.Timestamp, Is.EqualTo(1234567890));
        }

        [Test]
        public void ProcessingContext_CurrentWatermark_ShouldGetAndSet()
        {
            // Arrange
            var context = new ProcessingContext();

            // Act
            context.CurrentWatermark = 9876543210;

            // Assert
            Assert.That(context.CurrentWatermark, Is.EqualTo(9876543210));
        }

        [Test]
        public void ProcessingContext_Collect_WithValidOutput_ShouldAddToBuffer()
        {
            // Arrange
            var context = new ProcessingContext();
            var output = new { Value = "test" };

            // Act
            context.Collect(output);

            // Assert
            Assert.That(context.GetOutput().Count, Is.EqualTo(1));
            Assert.That(context.GetOutput()[0], Is.EqualTo(output));
        }

        [Test]
        public void ProcessingContext_Collect_WithNull_ShouldThrow()
        {
            // Arrange
            var context = new ProcessingContext();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => context.Collect(null!));
        }

        [Test]
        public void ProcessingContext_RegisterEventTimeTimer_ShouldAddTimer()
        {
            // Arrange
            var context = new ProcessingContext();

            // Act
            context.RegisterEventTimeTimer(1000);
            context.RegisterEventTimeTimer(2000);

            // Assert
            var timers = context.GetEventTimeTimers();
            Assert.That(timers.Count, Is.EqualTo(2));
            Assert.That(timers, Does.Contain(1000));
            Assert.That(timers, Does.Contain(2000));
        }

        [Test]
        public void ProcessingContext_RegisterProcessingTimeTimer_ShouldAddTimer()
        {
            // Arrange
            var context = new ProcessingContext();

            // Act
            context.RegisterProcessingTimeTimer(3000);

            // Assert
            var timers = context.GetProcessingTimeTimers();
            Assert.That(timers.Count, Is.EqualTo(1));
            Assert.That(timers[0], Is.EqualTo(3000));
        }

        [Test]
        public void ProcessingContext_DeleteEventTimeTimer_ShouldRemoveTimer()
        {
            // Arrange
            var context = new ProcessingContext();
            context.RegisterEventTimeTimer(1000);
            context.RegisterEventTimeTimer(2000);

            // Act
            context.DeleteEventTimeTimer(1000);

            // Assert
            var timers = context.GetEventTimeTimers();
            Assert.That(timers.Count, Is.EqualTo(1));
            Assert.That(timers, Does.Not.Contain(1000));
        }

        [Test]
        public void ProcessingContext_DeleteProcessingTimeTimer_ShouldRemoveTimer()
        {
            // Arrange
            var context = new ProcessingContext();
            context.RegisterProcessingTimeTimer(3000);

            // Act
            context.DeleteProcessingTimeTimer(3000);

            // Assert
            var timers = context.GetProcessingTimeTimers();
            Assert.That(timers, Is.Empty);
        }

        [Test]
        public void ProcessingContext_ClearOutput_ShouldRemoveAllOutputs()
        {
            // Arrange
            var context = new ProcessingContext();
            context.Collect("output1");
            context.Collect("output2");

            // Act
            context.ClearOutput();

            // Assert
            Assert.That(context.GetOutput(), Is.Empty);
        }

        #endregion

        #region FunctionContext Tests

        [Test]
        public void FunctionContext_GetState_WithValidDescriptor_ShouldReturnState()
        {
            // Arrange
            var context = new FunctionContext();
            var descriptor = new ValueStateDescriptor<string>("test-state");

            // Act
            var state = context.GetState(descriptor);

            // Assert
            Assert.That(state, Is.Not.Null);
        }

        [Test]
        public void FunctionContext_GetState_SameDescriptor_ShouldReturnSameState()
        {
            // Arrange
            var context = new FunctionContext();
            var descriptor = new ValueStateDescriptor<string>("test-state");

            // Act
            var state1 = context.GetState(descriptor);
            var state2 = context.GetState(descriptor);

            // Assert
            Assert.That(state2, Is.SameAs(state1));
        }

        [Test]
        public void FunctionContext_GetState_WithNullDescriptor_ShouldThrow()
        {
            // Arrange
            var context = new FunctionContext();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => context.GetState<string>(null!));
        }

        [Test]
        public void FunctionContext_GetListState_WithValidDescriptor_ShouldReturnState()
        {
            // Arrange
            var context = new FunctionContext();
            var descriptor = new ListStateDescriptor<int>("test-list-state");

            // Act
            var state = context.GetListState(descriptor);

            // Assert
            Assert.That(state, Is.Not.Null);
        }

        [Test]
        public void FunctionContext_GetListState_SameDescriptor_ShouldReturnSameState()
        {
            // Arrange
            var context = new FunctionContext();
            var descriptor = new ListStateDescriptor<int>("test-list-state");

            // Act
            var state1 = context.GetListState(descriptor);
            var state2 = context.GetListState(descriptor);

            // Assert
            Assert.That(state2, Is.SameAs(state1));
        }

        [Test]
        public void FunctionContext_GetListState_WithNullDescriptor_ShouldThrow()
        {
            // Arrange
            var context = new FunctionContext();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => context.GetListState<int>(null!));
        }

        #endregion

        #region SimpleValueState Tests

        [Test]
        public void SimpleValueState_Value_InitialState_ShouldReturnDefault()
        {
            // Arrange
            var context = new FunctionContext();
            var descriptor = new ValueStateDescriptor<string>("test");
            var state = context.GetState(descriptor);

            // Act
            var value = state.Value();

            // Assert
            Assert.That(value, Is.Null);
        }

        [Test]
        public void SimpleValueState_Update_ShouldSetValue()
        {
            // Arrange
            var context = new FunctionContext();
            var descriptor = new ValueStateDescriptor<string>("test");
            var state = context.GetState(descriptor);

            // Act
            state.Update("test-value");

            // Assert
            Assert.That(state.Value(), Is.EqualTo("test-value"));
        }

        [Test]
        public void SimpleValueState_Clear_ShouldResetValue()
        {
            // Arrange
            var context = new FunctionContext();
            var descriptor = new ValueStateDescriptor<string>("test");
            var state = context.GetState(descriptor);
            state.Update("test-value");

            // Act
            state.Clear();

            // Assert
            Assert.That(state.Value(), Is.Null);
        }

        #endregion

        #region SimpleListState Tests

        [Test]
        public void SimpleListState_Get_InitialState_ShouldReturnEmpty()
        {
            // Arrange
            var context = new FunctionContext();
            var descriptor = new ListStateDescriptor<int>("test");
            var state = context.GetListState(descriptor);

            // Act
            var values = state.Get().ToList();

            // Assert
            Assert.That(values, Is.Empty);
        }

        [Test]
        public void SimpleListState_Add_ShouldAddValue()
        {
            // Arrange
            var context = new FunctionContext();
            var descriptor = new ListStateDescriptor<int>("test");
            var state = context.GetListState(descriptor);

            // Act
            state.Add(1);
            state.Add(2);
            state.Add(3);

            // Assert
            var values = state.Get().ToList();
            Assert.That(values.Count, Is.EqualTo(3));
            Assert.That(values, Does.Contain(1));
            Assert.That(values, Does.Contain(2));
            Assert.That(values, Does.Contain(3));
        }

        [Test]
        public void SimpleListState_Clear_ShouldRemoveAllValues()
        {
            // Arrange
            var context = new FunctionContext();
            var descriptor = new ListStateDescriptor<int>("test");
            var state = context.GetListState(descriptor);
            state.Add(1);
            state.Add(2);

            // Act
            state.Clear();

            // Assert
            Assert.That(state.Get().ToList(), Is.Empty);
        }

        #endregion

        #region GroupedTable Tests

        [Test]
        public void GroupedTable_Aggregate_WithValidAggregations_ShouldReturnTable()
        {
            // Arrange
            var tableSource = new Flink.JobBuilder.Models.TableSourceDefinition
            {
                TableName = "test_table"
            };
            var table = new Table(tableSource);
            var grouped = table.GroupBy("category");

            // Act
            var result = grouped.Aggregate("COUNT(*) AS total", "SUM(amount) AS sum_amount");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.TypeOf<Table>());
        }

        [Test]
        public void GroupedTable_Aggregate_WithNullAggregations_ShouldThrow()
        {
            // Arrange
            var tableSource = new Flink.JobBuilder.Models.TableSourceDefinition
            {
                TableName = "test_table"
            };
            var table = new Table(tableSource);
            var grouped = table.GroupBy("category");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => grouped.Aggregate(null!));
        }

        [Test]
        public void GroupedTable_Aggregate_WithEmptyAggregations_ShouldThrow()
        {
            // Arrange
            var tableSource = new Flink.JobBuilder.Models.TableSourceDefinition
            {
                TableName = "test_table"
            };
            var table = new Table(tableSource);
            var grouped = table.GroupBy("category");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => grouped.Aggregate());
        }

        [Test]
        public void GroupedTable_Select_ShouldReturnTable()
        {
            // Arrange
            var tableSource = new Flink.JobBuilder.Models.TableSourceDefinition
            {
                TableName = "test_table"
            };
            var table = new Table(tableSource);
            var grouped = table.GroupBy("category");

            // Act
            var result = grouped.Select("category", "COUNT(*) AS cnt");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.TypeOf<Table>());
        }

        #endregion

        #region TableExtensions Tests

        [Test]
        public void TableExtensions_ToTable_WithValidName_ShouldReturnTable()
        {
            // Arrange
            var stream = _env.FromCollection(new[] { 1, 2, 3 });

            // Act
            var table = stream.ToTable("test_table");

            // Assert
            Assert.That(table, Is.Not.Null);
            Assert.That(table, Is.TypeOf<Table>());
        }

        [Test]
        public void TableExtensions_ToTable_WithSchema_ShouldReturnTable()
        {
            // Arrange
            var stream = _env.FromCollection(new[] { 1, 2, 3 });
            var schema = new Dictionary<string, string>
            {
                { "id", "BIGINT" },
                { "value", "STRING" }
            };

            // Act
            var table = stream.ToTable("test_table", schema);

            // Assert
            Assert.That(table, Is.Not.Null);
        }

        [Test]
        public void TableExtensions_ToTable_WithNullTableName_ShouldThrow()
        {
            // Arrange
            var stream = _env.FromCollection(new[] { 1, 2, 3 });

            // Act & Assert
            Assert.Throws<ArgumentException>(() => stream.ToTable(null!));
        }

        [Test]
        public void TableExtensions_ToTable_WithEmptyTableName_ShouldThrow()
        {
            // Arrange
            var stream = _env.FromCollection(new[] { 1, 2, 3 });

            // Act & Assert
            Assert.Throws<ArgumentException>(() => stream.ToTable(""));
        }

        [Test]
        public void TableExtensions_ToTable_WithWhitespaceTableName_ShouldThrow()
        {
            // Arrange
            var stream = _env.FromCollection(new[] { 1, 2, 3 });

            // Act & Assert
            Assert.Throws<ArgumentException>(() => stream.ToTable("   "));
        }

        #endregion

        #region OnTimerContext Tests

        [Test]
        public void OnTimerContext_Properties_ShouldGetAndSet()
        {
            // Arrange & Act
            var context = new OnTimerContext
            {
                TimerTimestamp = 12345,
                TimerType = TimerType.EventTime
            };

            // Assert
            Assert.That(context.TimerTimestamp, Is.EqualTo(12345));
            Assert.That(context.TimerType, Is.EqualTo(TimerType.EventTime));
        }

        [Test]
        public void OnTimerContext_TimerType_ProcessingTime_ShouldWork()
        {
            // Arrange & Act
            var context = new OnTimerContext
            {
                TimerType = TimerType.ProcessingTime
            };

            // Assert
            Assert.That(context.TimerType, Is.EqualTo(TimerType.ProcessingTime));
        }

        #endregion

        #region ProcessTableFunction Tests

        [Test]
        public void ProcessTableFunction_Open_DefaultImplementation_ShouldNotThrow()
        {
            // Arrange
            var function = new TestProcessTableFunction();
            var context = new FunctionContext();

            // Act & Assert
            Assert.DoesNotThrow(() => function.TestOpen(context));
        }

        [Test]
        public void ProcessTableFunction_OnTimer_DefaultImplementation_ShouldNotThrow()
        {
            // Arrange
            var function = new TestProcessTableFunction();
            var processingContext = new ProcessingContext();
            var timerContext = new OnTimerContext();

            // Act & Assert
            Assert.DoesNotThrow(() => function.TestOnTimer(processingContext, timerContext));
        }

        [Test]
        public void ProcessTableFunction_Close_DefaultImplementation_ShouldNotThrow()
        {
            // Arrange
            var function = new TestProcessTableFunction();

            // Act & Assert
            Assert.DoesNotThrow(() => function.TestClose());
        }

        private class TestProcessTableFunction : ProcessTableFunction<string, string>
        {
            public override void Eval(ProcessingContext context, string input)
            {
                context.Collect(input.ToUpper());
            }

            public void TestOpen(FunctionContext context) => Open(context);
            public void TestOnTimer(ProcessingContext context, OnTimerContext timerContext) => OnTimer(context, timerContext);
            public void TestClose() => Close();
        }

        #endregion

        #region State Descriptor Tests

        [Test]
        public void ValueStateDescriptor_Constructor_ShouldInitialize()
        {
            // Act
            var descriptor = new ValueStateDescriptor<string>("test-state");

            // Assert
            Assert.That(descriptor, Is.Not.Null);
            Assert.That(descriptor.Name, Is.EqualTo("test-state"));
            Assert.That(descriptor.ValueType, Is.EqualTo(typeof(string)));
        }

        [Test]
        public void ListStateDescriptor_Constructor_ShouldInitialize()
        {
            // Act
            var descriptor = new ListStateDescriptor<int>("test-list-state");

            // Assert
            Assert.That(descriptor, Is.Not.Null);
            Assert.That(descriptor.Name, Is.EqualTo("test-list-state"));
            Assert.That(descriptor.ElementType, Is.EqualTo(typeof(int)));
        }

        [Test]
        public void MapStateDescriptor_Constructor_ShouldInitialize()
        {
            // Act
            var descriptor = new MapStateDescriptor<string, int>("test-map-state");

            // Assert
            Assert.That(descriptor, Is.Not.Null);
            Assert.That(descriptor.Name, Is.EqualTo("test-map-state"));
            Assert.That(descriptor.KeyType, Is.EqualTo(typeof(string)));
            Assert.That(descriptor.ValueType, Is.EqualTo(typeof(int)));
        }

        [Test]
        public void OutputTag_Constructor_ShouldInitialize()
        {
            // Act
            var tag = new OutputTag<string>("side-output");

            // Assert
            Assert.That(tag, Is.Not.Null);
            Assert.That(tag.Id, Is.EqualTo("side-output"));
        }

        #endregion
    }
}
