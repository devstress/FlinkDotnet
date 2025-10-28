using System;
using System.Linq;
using FlinkDotNet.DataStream.Window.Assigners;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Additional tests for SessionWindows to achieve 100% coverage.
    /// Covers ToString and other methods not fully covered by existing tests.
    /// </summary>
    [TestFixture]
    public class SessionWindowsAdditionalCoverageTests
    {
        [Test]
        public void SessionWindows_ToString_ReturnsCorrectFormat()
        {
            // Arrange
            var sessionGap = Time.Seconds(30);
            var windows = SessionWindows<string>.WithGap(sessionGap);

            // Act
            var result = windows.ToString();

            // Assert
            Assert.That(result, Is.EqualTo("SessionWindows(30000ms gap)"));
        }

        [Test]
        public void SessionWindows_ToString_WithDifferentGap_ReturnsCorrectFormat()
        {
            // Arrange
            var sessionGap = Time.Minutes(5);
            var windows = SessionWindows<int>.WithGap(sessionGap);

            // Act
            var result = windows.ToString();

            // Assert
            Assert.That(result, Is.EqualTo("SessionWindows(300000ms gap)"));
        }

        [Test]
        public void SessionWindows_TimeCharacteristic_ReturnsEventTime()
        {
            // Arrange
            var windows = SessionWindows<string>.WithGap(Time.Seconds(10));

            // Act
            var timeChar = windows.TimeCharacteristic;

            // Assert - Use Assigners.TimeCharacteristic
            Assert.That(timeChar, Is.EqualTo(Window.Assigners.TimeCharacteristic.EventTime));
        }

        [Test]
        public void SessionWindows_IsEventTime_ReturnsTrue()
        {
            // Arrange
            var windows = SessionWindows<string>.WithGap(Time.Seconds(10));

            // Act
            var isEventTime = windows.IsEventTime;

            // Assert
            Assert.That(isEventTime, Is.True);
        }

        [Test]
        public void SessionWindows_CanMerge_ReturnsTrue()
        {
            // Arrange
            var windows = SessionWindows<string>.WithGap(Time.Seconds(10));

            // Act
            var canMerge = windows.CanMerge;

            // Assert
            Assert.That(canMerge, Is.True);
        }

        [Test]
        public void SessionWindows_AssignWindows_CreatesWindowWithCorrectDuration()
        {
            // Arrange
            var sessionGap = Time.Seconds(60);
            var windows = SessionWindows<string>.WithGap(sessionGap);
            var element = "test-element";
            var timestamp = 1000L;

            // Act
            var assignedWindows = windows.AssignWindows(element, timestamp).ToList();

            // Assert
            Assert.That(assignedWindows, Has.Count.EqualTo(1));
            Assert.That(assignedWindows[0].Start, Is.EqualTo(timestamp));
            Assert.That(assignedWindows[0].End, Is.EqualTo(timestamp + 60000));
        }

        [Test]
        public void SessionWindows_AssignWindows_WithZeroTimestamp_CreatesCorrectWindow()
        {
            // Arrange
            var sessionGap = Time.Seconds(30);
            var windows = SessionWindows<int>.WithGap(sessionGap);
            var element = 42;
            var timestamp = 0L;

            // Act
            var assignedWindows = windows.AssignWindows(element, timestamp).ToList();

            // Assert
            Assert.That(assignedWindows, Has.Count.EqualTo(1));
            Assert.That(assignedWindows[0].Start, Is.EqualTo(0));
            Assert.That(assignedWindows[0].End, Is.EqualTo(30000));
        }

        [Test]
        public void SessionWindows_AssignWindows_WithLargeTimestamp_CreatesCorrectWindow()
        {
            // Arrange
            var sessionGap = Time.Milliseconds(500);
            var windows = SessionWindows<double>.WithGap(sessionGap);
            var element = 3.14;
            var timestamp = long.MaxValue / 2; // Large but safe timestamp

            // Act
            var assignedWindows = windows.AssignWindows(element, timestamp).ToList();

            // Assert
            Assert.That(assignedWindows, Has.Count.EqualTo(1));
            Assert.That(assignedWindows[0].Start, Is.EqualTo(timestamp));
            Assert.That(assignedWindows[0].End, Is.EqualTo(timestamp + 500));
        }

        [Test]
        public void SessionWindows_WithGap_CreatesNewInstance()
        {
            // Arrange & Act
            var windows1 = SessionWindows<string>.WithGap(Time.Seconds(10));
            var windows2 = SessionWindows<string>.WithGap(Time.Seconds(10));

            // Assert - Should create different instances
            Assert.That(windows1, Is.Not.SameAs(windows2));
        }

        [Test]
        public void SessionWindows_Properties_ConsistentAcrossMultipleCalls()
        {
            // Arrange
            var windows = SessionWindows<string>.WithGap(Time.Seconds(45));

            // Act & Assert - Properties should return consistent values (using Assigners namespace)
            Assert.That(windows.TimeCharacteristic, Is.EqualTo(Window.Assigners.TimeCharacteristic.EventTime));
            Assert.That(windows.IsEventTime, Is.True);
            Assert.That(windows.CanMerge, Is.True);
            
            // Call again to ensure consistency
            Assert.That(windows.TimeCharacteristic, Is.EqualTo(Window.Assigners.TimeCharacteristic.EventTime));
            Assert.That(windows.IsEventTime, Is.True);
            Assert.That(windows.CanMerge, Is.True);
        }
    }
}
