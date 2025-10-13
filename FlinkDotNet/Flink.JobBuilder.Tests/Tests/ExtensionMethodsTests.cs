using Xunit;
using FlinkDotNet.DataStream;
using System;

namespace Flink.JobBuilder.Tests.Tests
{
    public class ExtensionMethodsTests
    {
        [Fact]
        public void TypeInformation_Of_ReturnsCorrectType()
        {
            var typeInfo = TypeInformation<string>.Of(typeof(string));
            Assert.NotNull(typeInfo);
        }

        [Fact]
        public void TypeInformation_GetTypeClass_ReturnsType()
        {
            var typeInfo = TypeInformation<int>.Of(typeof(int));
            var type = typeInfo.GetTypeClass();
            Assert.Equal(typeof(int), type);
        }

        [Fact]
        public void TypeInformation_IsBasicType_ReturnsTrueForPrimitives()
        {
            var intTypeInfo = TypeInformation<int>.Of(typeof(int));
            Assert.True(intTypeInfo.IsBasicType());
        }

        [Fact]
        public void TypeInformation_IsTupleType_ReturnsFalseForBasicTypes()
        {
            var stringTypeInfo = TypeInformation<string>.Of(typeof(string));
            Assert.False(stringTypeInfo.IsTupleType());
        }

        [Fact]
        public void TimeCharacteristic_ProcessingTime_HasCorrectValue()
        {
            var timeChar = TimeCharacteristic.ProcessingTime;
            Assert.Equal(TimeCharacteristic.ProcessingTime, timeChar);
        }

        [Fact]
        public void TimeCharacteristic_EventTime_HasCorrectValue()
        {
            var timeChar = TimeCharacteristic.EventTime;
            Assert.Equal(TimeCharacteristic.EventTime, timeChar);
        }

        [Fact]
        public void TimeCharacteristic_IngestionTime_HasCorrectValue()
        {
            var timeChar = TimeCharacteristic.IngestionTime;
            Assert.Equal(TimeCharacteristic.IngestionTime, timeChar);
        }

        [Fact]
        public void Time_Seconds_CreatesCorrectTimeSpan()
        {
            var time = Time.Seconds(5);
            Assert.Equal(TimeSpan.FromSeconds(5), time.ToTimeSpan());
        }

        [Fact]
        public void Time_Minutes_CreatesCorrectTimeSpan()
        {
            var time = Time.Minutes(2);
            Assert.Equal(TimeSpan.FromMinutes(2), time.ToTimeSpan());
        }

        [Fact]
        public void Time_Hours_CreatesCorrectTimeSpan()
        {
            var time = Time.Hours(1);
            Assert.Equal(TimeSpan.FromHours(1), time.ToTimeSpan());
        }

        [Fact]
        public void Time_Days_CreatesCorrectTimeSpan()
        {
            var time = Time.Days(3);
            Assert.Equal(TimeSpan.FromDays(3), time.ToTimeSpan());
        }

        [Fact]
        public void Time_Milliseconds_CreatesCorrectTimeSpan()
        {
            var time = Time.Milliseconds(500);
            Assert.Equal(TimeSpan.FromMilliseconds(500), time.ToTimeSpan());
        }

        [Fact]
        public void Time_GetTotalMilliseconds_ReturnsCorrectValue()
        {
            var time = Time.Seconds(2);
            Assert.Equal(2000, time.GetTotalMilliseconds());
        }
    }
}
