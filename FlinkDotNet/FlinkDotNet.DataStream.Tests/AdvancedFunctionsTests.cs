using System;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
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
            Assert.That(tag, Is.Not.Null);
            Assert.That(tag.Id, Is.EqualTo(id));
        }

        [Test]
        public void OutputTag_Constructor_WithNullId_ThrowsArgumentNullException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new OutputTag<string>(null!));
            Assert.That(ex!.ParamName, Is.EqualTo("id"));
        }

        [Test]
        public void OutputTag_WithDifferentTypes_CreatesCorrectTags()
        {
            // Arrange & Act
            var stringTag = new OutputTag<string>("string-output");
            var intTag = new OutputTag<int>("int-output");
            var doubleTag = new OutputTag<double>("double-output");

            // Assert
            Assert.That(stringTag.Id, Is.EqualTo("string-output"));
            Assert.That(intTag.Id, Is.EqualTo("int-output"));
            Assert.That(doubleTag.Id, Is.EqualTo("double-output"));
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
            var tag1 = new OutputTag<string>("id-1");
            var tag2 = new OutputTag<string>("id-2");

            // Act
            var result = tag1.Equals(tag2);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void OutputTag_Equals_WithNull_ReturnsFalse()
        {
            // Arrange
            var tag = new OutputTag<string>("test-id");

            // Act
            var result = tag.Equals(null);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void OutputTag_Equals_WithDifferentType_ReturnsFalse()
        {
            // Arrange
            var tag = new OutputTag<string>("test-id");
            var other = "not-a-tag";

            // Act
            var result = tag.Equals(other);

            // Assert
            Assert.That(result, Is.False);
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
            var tag1 = new OutputTag<string>("id-1");
            var tag2 = new OutputTag<string>("id-2");

            // Act
            var hash1 = tag1.GetHashCode();
            var hash2 = tag2.GetHashCode();

            // Assert
            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public void OutputTag_Id_IsReadOnly()
        {
            // Arrange
            var tag = new OutputTag<string>("test-id");

            // Act
            var id = tag.Id;

            // Assert
            Assert.That(id, Is.EqualTo("test-id"));
            // Verify Id property is get-only
            var idProperty = typeof(OutputTag<string>).GetProperty("Id");
            Assert.That(idProperty!.CanWrite, Is.False);
        }

        #endregion

        #region TimeDomain Enum Tests

        [Test]
        public void TimeDomain_HasEventTimeValue()
        {
            // Arrange & Act
            var timeDomain = TimeDomain.EventTime;

            // Assert
            Assert.That(timeDomain, Is.EqualTo(TimeDomain.EventTime));
            Assert.That((int)timeDomain, Is.EqualTo(0));
        }

        [Test]
        public void TimeDomain_HasProcessingTimeValue()
        {
            // Arrange & Act
            var timeDomain = TimeDomain.ProcessingTime;

            // Assert
            Assert.That(timeDomain, Is.EqualTo(TimeDomain.ProcessingTime));
            Assert.That((int)timeDomain, Is.EqualTo(1));
        }

        [Test]
        public void TimeDomain_EventTimeAndProcessingTime_AreDifferent()
        {
            // Arrange
            var eventTime = TimeDomain.EventTime;
            var processingTime = TimeDomain.ProcessingTime;

            // Act & Assert
            Assert.That(eventTime, Is.Not.EqualTo(processingTime));
        }

        [Test]
        public void TimeDomain_CanBeSwitched()
        {
            // Arrange
            TimeDomain domain = TimeDomain.EventTime;
            Assert.That(domain, Is.EqualTo(TimeDomain.EventTime));

            // Act
            domain = TimeDomain.ProcessingTime;

            // Assert
            Assert.That(domain, Is.EqualTo(TimeDomain.ProcessingTime));
        }

        [Test]
        public void TimeDomain_SupportsComparison()
        {
            // Arrange
            var eventTime = TimeDomain.EventTime;
            var processingTime = TimeDomain.ProcessingTime;

            // Act
            var eventTimeInt = (int)eventTime;
            var processingTimeInt = (int)processingTime;

            // Assert
            Assert.That(eventTimeInt, Is.LessThan(processingTimeInt));
        }

        #endregion

        #region Interface Existence Tests

        [Test]
        public void IProcessFunction_InterfaceExists()
        {
            // Act
            var type = typeof(IProcessFunction<,>);

            // Assert
            Assert.That(type, Is.Not.Null);
            Assert.That(type.IsInterface, Is.True);
        }

        [Test]
        public void IKeyedProcessFunction_InterfaceExists()
        {
            // Act
            var type = typeof(IKeyedProcessFunction<,,>);

            // Assert
            Assert.That(type, Is.Not.Null);
            Assert.That(type.IsInterface, Is.True);
        }

        [Test]
        public void ICoProcessFunction_InterfaceExists()
        {
            // Act
            var type = typeof(ICoProcessFunction<,,>);

            // Assert
            Assert.That(type, Is.Not.Null);
            Assert.That(type.IsInterface, Is.True);
        }

        [Test]
        public void IProcessWindowFunction_InterfaceExists()
        {
            // Act
            var type = typeof(IProcessWindowFunction<,,>);

            // Assert
            Assert.That(type, Is.Not.Null);
            Assert.That(type.IsInterface, Is.True);
        }

        [Test]
        public void IAsyncFunction_InterfaceExists()
        {
            // Act
            var type = typeof(IAsyncFunction<,>);

            // Assert
            Assert.That(type, Is.Not.Null);
            Assert.That(type.IsInterface, Is.True);
        }

        [Test]
        public void IResultFuture_InterfaceExists()
        {
            // Act
            var type = typeof(IResultFuture<>);

            // Assert
            Assert.That(type, Is.Not.Null);
            Assert.That(type.IsInterface, Is.True);
        }

        [Test]
        public void IProcessContext_InterfaceExists()
        {
            // Act
            var type = typeof(IProcessContext);

            // Assert
            Assert.That(type, Is.Not.Null);
            Assert.That(type.IsInterface, Is.True);
        }

        [Test]
        public void IKeyedProcessContext_InterfaceExists()
        {
            // Act
            var type = typeof(IKeyedProcessContext<>);

            // Assert
            Assert.That(type, Is.Not.Null);
            Assert.That(type.IsInterface, Is.True);
        }

        [Test]
        public void IOnTimerContext_InterfaceExists()
        {
            // Act
            var type = typeof(IOnTimerContext);

            // Assert
            Assert.That(type, Is.Not.Null);
            Assert.That(type.IsInterface, Is.True);
        }

        [Test]
        public void IKeyedOnTimerContext_InterfaceExists()
        {
            // Act
            var type = typeof(IKeyedOnTimerContext<>);

            // Assert
            Assert.That(type, Is.Not.Null);
            Assert.That(type.IsInterface, Is.True);
        }

        [Test]
        public void IWindowContext_InterfaceExists()
        {
            // Act
            var type = typeof(IWindowContext);

            // Assert
            Assert.That(type, Is.Not.Null);
            Assert.That(type.IsInterface, Is.True);
        }

        [Test]
        public void ICollector_InterfaceExists()
        {
            // Act
            var type = typeof(ICollector<>);

            // Assert
            Assert.That(type, Is.Not.Null);
            Assert.That(type.IsInterface, Is.True);
        }

        [Test]
        public void IJoinFunction_InterfaceExists()
        {
            // Act
            var type = typeof(IJoinFunction<,,>);

            // Assert
            Assert.That(type, Is.Not.Null);
            Assert.That(type.IsInterface, Is.True);
        }

        [Test]
        public void IFlatJoinFunction_InterfaceExists()
        {
            // Act
            var type = typeof(IFlatJoinFunction<,,>);

            // Assert
            Assert.That(type, Is.Not.Null);
            Assert.That(type.IsInterface, Is.True);
        }

        [Test]
        public void ICoGroupFunction_InterfaceExists()
        {
            // Act
            var type = typeof(ICoGroupFunction<,,>);

            // Assert
            Assert.That(type, Is.Not.Null);
            Assert.That(type.IsInterface, Is.True);
        }

        #endregion
    }
}