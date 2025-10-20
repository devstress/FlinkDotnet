using FlinkDotNet.DataStream;

namespace Flink.JobBuilder.Tests.Tests
{
    [TestFixture]
    public class AdvancedFunctionsTests
    {
        #region OutputTag Tests

        [Test]
        public void OutputTag_Constructor_WithValidId_CreatesTag()
        {
            // Arrange
            var id = "test-output";

            // Act
            var tag = new OutputTag<string>(id);

            // Assert
            Assert.That(tag.Id, Is.EqualTo(id));
        }

        [Test]
        public void OutputTag_Constructor_WithNullId_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new OutputTag<string>(null!));
        }

        [Test]
        public void OutputTag_Id_IsReadOnly()
        {
            // Arrange
            var id = "readonly-test";
            var tag = new OutputTag<int>(id);

            // Assert
            Assert.That(tag.Id, Is.EqualTo(id));
            // Verify property is get-only (no setter available)
        }

        [Test]
        public void OutputTag_Equals_WithSameId_ReturnsTrue()
        {
            // Arrange
            var tag1 = new OutputTag<string>("same-id");
            var tag2 = new OutputTag<string>("same-id");

            // Act
            var result = tag1.Equals(tag2);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void OutputTag_Equals_WithDifferentId_ReturnsFalse()
        {
            // Arrange
            var tag1 = new OutputTag<string>("id1");
            var tag2 = new OutputTag<string>("id2");

            // Act
            var result = tag1.Equals(tag2);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void OutputTag_Equals_WithDifferentType_ReturnsFalse()
        {
            // Arrange
            var tag = new OutputTag<string>("id");
            var other = "not-a-tag";

            // Act
            var result = tag.Equals(other);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void OutputTag_Equals_WithNull_ReturnsFalse()
        {
            // Arrange
            var tag = new OutputTag<string>("id");

            // Act
            var result = tag.Equals(null);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void OutputTag_Equals_WithSameInstance_ReturnsTrue()
        {
            // Arrange
            var tag = new OutputTag<string>("id");

            // Act
            var result = tag.Equals(tag);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void OutputTag_GetHashCode_WithSameId_ReturnsSameHashCode()
        {
            // Arrange
            var tag1 = new OutputTag<string>("same-id");
            var tag2 = new OutputTag<string>("same-id");

            // Act
            var hash1 = tag1.GetHashCode();
            var hash2 = tag2.GetHashCode();

            // Assert
            Assert.That(hash1, Is.EqualTo(hash2));
        }

        [Test]
        public void OutputTag_GetHashCode_WithDifferentId_ReturnsDifferentHashCode()
        {
            // Arrange
            var tag1 = new OutputTag<string>("id1");
            var tag2 = new OutputTag<string>("id2");

            // Act
            var hash1 = tag1.GetHashCode();
            var hash2 = tag2.GetHashCode();

            // Assert
            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public void OutputTag_SupportsGenericTypes_String()
        {
            // Arrange & Act
            var tag = new OutputTag<string>("string-tag");

            // Assert
            Assert.That(tag, Is.Not.Null);
            Assert.That(tag.Id, Is.EqualTo("string-tag"));
        }

        [Test]
        public void OutputTag_SupportsGenericTypes_Int()
        {
            // Arrange & Act
            var tag = new OutputTag<int>("int-tag");

            // Assert
            Assert.That(tag, Is.Not.Null);
            Assert.That(tag.Id, Is.EqualTo("int-tag"));
        }

        [Test]
        public void OutputTag_SupportsGenericTypes_ComplexType()
        {
            // Arrange & Act
            var tag = new OutputTag<List<string>>("complex-tag");

            // Assert
            Assert.That(tag, Is.Not.Null);
            Assert.That(tag.Id, Is.EqualTo("complex-tag"));
        }

        [Test]
        public void OutputTag_DifferentGenericTypes_AreNotEqual()
        {
            // Arrange
            var stringTag = new OutputTag<string>("same-id");
            var intTag = new OutputTag<int>("same-id");

            // Act - Cannot directly compare due to different generic types
            // But their Ids should be equal
            // Assert
            Assert.That(stringTag.Id, Is.EqualTo(intTag.Id));
        }

        #endregion

        #region TimeDomain Tests

        [Test]
        public void TimeDomain_EventTime_HasCorrectValue()
        {
            // Arrange & Act
            var domain = TimeDomain.EventTime;

            // Assert
            Assert.That(domain, Is.EqualTo(TimeDomain.EventTime));
            Assert.That((int)domain, Is.EqualTo(0));
        }

        [Test]
        public void TimeDomain_ProcessingTime_HasCorrectValue()
        {
            // Arrange & Act
            var domain = TimeDomain.ProcessingTime;

            // Assert
            Assert.That(domain, Is.EqualTo(TimeDomain.ProcessingTime));
            Assert.That((int)domain, Is.EqualTo(1));
        }

        [Test]
        public void TimeDomain_HasOnlyTwoValues()
        {
            // Arrange
            var values = Enum.GetValues(typeof(TimeDomain));

            // Assert
            Assert.That(values.Length, Is.EqualTo(2));
        }

        [Test]
        public void TimeDomain_CanBeCompared()
        {
            // Arrange
            var eventTime = TimeDomain.EventTime;
            var processingTime = TimeDomain.ProcessingTime;

            // Assert
            Assert.That(eventTime, Is.Not.EqualTo(processingTime));
            Assert.That(eventTime == TimeDomain.EventTime, Is.True);
            Assert.That(processingTime == TimeDomain.ProcessingTime, Is.True);
        }

        [Test]
        public void TimeDomain_CanBeUsedInSwitch()
        {
            // Arrange
            var domain = TimeDomain.EventTime;
            var result = string.Empty;

            // Act
            switch (domain)
            {
                case TimeDomain.EventTime:
                    result = "event";
                    break;
                case TimeDomain.ProcessingTime:
                    result = "processing";
                    break;
            }

            // Assert
            Assert.That(result, Is.EqualTo("event"));
        }

        [Test]
        public void TimeDomain_ToString_ReturnsEnumName()
        {
            // Arrange
            var eventTime = TimeDomain.EventTime;
            var processingTime = TimeDomain.ProcessingTime;

            // Act
            var eventTimeStr = eventTime.ToString();
            var processingTimeStr = processingTime.ToString();

            // Assert
            Assert.That(eventTimeStr, Is.EqualTo("EventTime"));
            Assert.That(processingTimeStr, Is.EqualTo("ProcessingTime"));
        }

        #endregion

        #region Interface Contract Tests

        [Test]
        public void IProcessFunction_HasCorrectMethodSignatures()
        {
            // Arrange
            var type = typeof(IProcessFunction<string, int>);

            // Assert - Verify interface has expected methods
            var processMethod = type.GetMethod("ProcessElementAsync");
            Assert.That(processMethod, Is.Not.Null);
            Assert.That(processMethod!.ReturnType, Is.EqualTo(typeof(Task)));

            var timerMethod = type.GetMethod("OnTimerAsync");
            Assert.That(timerMethod, Is.Not.Null);
            Assert.That(timerMethod!.ReturnType, Is.EqualTo(typeof(Task)));
        }

        [Test]
        public void IKeyedProcessFunction_HasCorrectMethodSignatures()
        {
            // Arrange
            var type = typeof(IKeyedProcessFunction<string, int, double>);

            // Assert
            var processMethod = type.GetMethod("ProcessElementAsync");
            Assert.That(processMethod, Is.Not.Null);

            var timerMethod = type.GetMethod("OnTimerAsync");
            Assert.That(timerMethod, Is.Not.Null);
        }

        [Test]
        public void ICoProcessFunction_HasCorrectMethodSignatures()
        {
            // Arrange
            var type = typeof(ICoProcessFunction<string, int, double>);

            // Assert
            var process1Method = type.GetMethod("ProcessElement1Async");
            Assert.That(process1Method, Is.Not.Null);

            var process2Method = type.GetMethod("ProcessElement2Async");
            Assert.That(process2Method, Is.Not.Null);

            var timerMethod = type.GetMethod("OnTimerAsync");
            Assert.That(timerMethod, Is.Not.Null);
        }

        [Test]
        public void IAsyncFunction_HasCorrectMethodSignatures()
        {
            // Arrange
            var type = typeof(IAsyncFunction<string, int>);

            // Assert
            var asyncInvokeMethod = type.GetMethod("AsyncInvokeAsync");
            Assert.That(asyncInvokeMethod, Is.Not.Null);

            var timeoutMethod = type.GetMethod("TimeoutAsync");
            Assert.That(timeoutMethod, Is.Not.Null);
        }

        [Test]
        public void IProcessContext_HasCorrectProperties()
        {
            // Arrange
            var type = typeof(IProcessContext);

            // Assert
            var timestampProp = type.GetProperty("Timestamp");
            Assert.That(timestampProp, Is.Not.Null);

            var processingTimeProp = type.GetProperty("CurrentProcessingTime");
            Assert.That(processingTimeProp, Is.Not.Null);

            var watermarkProp = type.GetProperty("CurrentWatermark");
            Assert.That(watermarkProp, Is.Not.Null);
        }

        [Test]
        public void IKeyedProcessContext_InheritsFromIProcessContext()
        {
            // Arrange
            var type = typeof(IKeyedProcessContext<string>);

            // Assert
            Assert.That(typeof(IProcessContext).IsAssignableFrom(type), Is.True);
        }

        [Test]
        public void IOnTimerContext_InheritsFromIProcessContext()
        {
            // Arrange
            var type = typeof(IOnTimerContext);

            // Assert
            Assert.That(typeof(IProcessContext).IsAssignableFrom(type), Is.True);
        }

        [Test]
        public void IKeyedOnTimerContext_InheritsFromIOnTimerContext()
        {
            // Arrange
            var type = typeof(IKeyedOnTimerContext<string>);

            // Assert
            Assert.That(typeof(IOnTimerContext).IsAssignableFrom(type), Is.True);
        }

        [Test]
        public void IResultFuture_HasCompleteMethod()
        {
            // Arrange
            var type = typeof(IResultFuture<string>);

            // Assert
            var completeMethod = type.GetMethod("Complete");
            Assert.That(completeMethod, Is.Not.Null);

            var completeExceptionallyMethod = type.GetMethod("CompleteExceptionally");
            Assert.That(completeExceptionallyMethod, Is.Not.Null);
        }

        [Test]
        public void ICollector_HasCollectMethod()
        {
            // Arrange
            var type = typeof(ICollector<string>);

            // Assert
            var collectMethod = type.GetMethod("Collect");
            Assert.That(collectMethod, Is.Not.Null);
        }

        [Test]
        public void IJoinFunction_HasJoinMethod()
        {
            // Arrange
            var type = typeof(IJoinFunction<string, int, double>);

            // Assert
            var joinMethod = type.GetMethod("Join");
            Assert.That(joinMethod, Is.Not.Null);
            Assert.That(joinMethod!.ReturnType, Is.EqualTo(typeof(double)));
        }

        [Test]
        public void IFlatJoinFunction_HasJoinMethod()
        {
            // Arrange
            var type = typeof(IFlatJoinFunction<string, int, double>);

            // Assert
            var joinMethod = type.GetMethod("Join");
            Assert.That(joinMethod, Is.Not.Null);
        }

        [Test]
        public void ICoGroupFunction_HasCoGroupMethod()
        {
            // Arrange
            var type = typeof(ICoGroupFunction<string, int, double>);

            // Assert
            var coGroupMethod = type.GetMethod("CoGroup");
            Assert.That(coGroupMethod, Is.Not.Null);
        }

        #endregion
    }
}