using System.Collections.Generic;
using System.Linq;
using FlinkDotNet.DataStream.Window;
using FlinkDotNet.DataStream.Window.Assigners;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Comprehensive tests for SessionWindows.MergeWindows to achieve 100% branch coverage.
    /// Covers all conditional paths including sorting, null checks, overlapping, and non-overlapping scenarios.
    /// </summary>
    [TestFixture]
    public class SessionWindowsMergeCompleteCoverageTests
    {
        [Test]
        public void MergeWindows_WithEmptyCollection_ReturnsEmptyList()
        {
            // Arrange
            var windows = new List<TimeWindow>();

            // Act
            var result = SessionWindows<string>.MergeWindows(windows).ToList();

            // Assert
            Assert.That(result, Is.Empty);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void MergeWindows_WithSingleWindow_ReturnsSingleWindow()
        {
            // Arrange - Single window tests line 88 (currentWindow == null path) and line 105 (currentWindow != null path)
            var windows = new List<TimeWindow>
            {
                new TimeWindow(1000, 5000)
            };

            // Act
            var result = SessionWindows<string>.MergeWindows(windows).ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Start, Is.EqualTo(1000));
            Assert.That(result[0].End, Is.EqualTo(5000));
        }

        [Test]
        public void MergeWindows_WithTwoOverlappingWindows_MergesIntoOne()
        {
            // Arrange - Tests line 92 (window.Start <= currentWindow.End is true)
            var windows = new List<TimeWindow>
            {
                new TimeWindow(1000, 5000),
                new TimeWindow(4000, 8000)  // Overlaps with first window
            };

            // Act
            var result = SessionWindows<string>.MergeWindows(windows).ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Start, Is.EqualTo(1000));
            Assert.That(result[0].End, Is.EqualTo(8000));
        }

        [Test]
        public void MergeWindows_WithTwoNonOverlappingWindows_KeepsBoth()
        {
            // Arrange - Tests line 92 (window.Start <= currentWindow.End is false, else branch at line 98)
            var windows = new List<TimeWindow>
            {
                new TimeWindow(1000, 3000),
                new TimeWindow(5000, 8000)  // Does not overlap with first window
            };

            // Act
            var result = SessionWindows<string>.MergeWindows(windows).ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].Start, Is.EqualTo(1000));
            Assert.That(result[0].End, Is.EqualTo(3000));
            Assert.That(result[1].Start, Is.EqualTo(5000));
            Assert.That(result[1].End, Is.EqualTo(8000));
        }

        [Test]
        public void MergeWindows_WithUnsortedWindows_SortsAndMerges()
        {
            // Arrange - Tests line 81 (sorting logic)
            var windows = new List<TimeWindow>
            {
                new TimeWindow(5000, 8000),
                new TimeWindow(1000, 3000),
                new TimeWindow(2500, 6000)
            };

            // Act
            var result = SessionWindows<string>.MergeWindows(windows).ToList();

            // Assert - Should be sorted and overlapping ones merged
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Start, Is.EqualTo(1000));
            Assert.That(result[0].End, Is.EqualTo(8000));
        }

        [Test]
        public void MergeWindows_WithMixedOverlappingAndNonOverlapping_MergesCorrectly()
        {
            // Arrange - Complex scenario testing all branches
            var windows = new List<TimeWindow>
            {
                new TimeWindow(1000, 3000),   // Window 1
                new TimeWindow(2000, 5000),   // Overlaps with Window 1 -> merged
                new TimeWindow(10000, 12000), // Window 2 (separate)
                new TimeWindow(11000, 15000), // Overlaps with Window 2 -> merged
                new TimeWindow(20000, 22000)  // Window 3 (separate)
            };

            // Act
            var result = SessionWindows<string>.MergeWindows(windows).ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result[0].Start, Is.EqualTo(1000));
            Assert.That(result[0].End, Is.EqualTo(5000));
            Assert.That(result[1].Start, Is.EqualTo(10000));
            Assert.That(result[1].End, Is.EqualTo(15000));
            Assert.That(result[2].Start, Is.EqualTo(20000));
            Assert.That(result[2].End, Is.EqualTo(22000));
        }

        [Test]
        public void MergeWindows_WithThreeConsecutiveOverlappingWindows_MergesAll()
        {
            // Arrange - Tests multiple iterations through the merge loop
            var windows = new List<TimeWindow>
            {
                new TimeWindow(0, 5000),
                new TimeWindow(4000, 9000),
                new TimeWindow(8000, 13000)
            };

            // Act
            var result = SessionWindows<string>.MergeWindows(windows).ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Start, Is.EqualTo(0));
            Assert.That(result[0].End, Is.EqualTo(13000));
        }

        [Test]
        public void MergeWindows_WithAdjacentWindows_TouchingAtBoundary_MergesThem()
        {
            // Arrange - Tests exact boundary condition (window.Start == currentWindow.End)
            var windows = new List<TimeWindow>
            {
                new TimeWindow(1000, 5000),
                new TimeWindow(5000, 9000)  // Starts exactly where previous ends
            };

            // Act
            var result = SessionWindows<string>.MergeWindows(windows).ToList();

            // Assert - Should merge since Start <= End (not < End)
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Start, Is.EqualTo(1000));
            Assert.That(result[0].End, Is.EqualTo(9000));
        }

        [Test]
        public void MergeWindows_WithWindowsJustAfterBoundary_KeepsSeparate()
        {
            // Arrange - Tests boundary condition (window.Start > currentWindow.End by 1)
            var windows = new List<TimeWindow>
            {
                new TimeWindow(1000, 5000),
                new TimeWindow(5001, 9000)  // Starts 1ms after previous ends
            };

            // Act
            var result = SessionWindows<string>.MergeWindows(windows).ToList();

            // Assert - Should NOT merge since Start > End
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].Start, Is.EqualTo(1000));
            Assert.That(result[0].End, Is.EqualTo(5000));
            Assert.That(result[1].Start, Is.EqualTo(5001));
            Assert.That(result[1].End, Is.EqualTo(9000));
        }

        [Test]
        public void MergeWindows_WithReverseOrderedWindows_SortsAndProcesses()
        {
            // Arrange - Completely reverse order to test sorting
            var windows = new List<TimeWindow>
            {
                new TimeWindow(15000, 20000),
                new TimeWindow(10000, 14000),
                new TimeWindow(5000, 9000),
                new TimeWindow(0, 4000)
            };

            // Act
            var result = SessionWindows<string>.MergeWindows(windows).ToList();

            // Assert - All non-overlapping, should have 4 windows in sorted order
            Assert.That(result.Count, Is.EqualTo(4));
            Assert.That(result[0].Start, Is.EqualTo(0));
            Assert.That(result[1].Start, Is.EqualTo(5000));
            Assert.That(result[2].Start, Is.EqualTo(10000));
            Assert.That(result[3].Start, Is.EqualTo(15000));
        }

        [Test]
        public void MergeWindows_WithDuplicateWindows_HandlesCorrectly()
        {
            // Arrange - Same window repeated
            var windows = new List<TimeWindow>
            {
                new TimeWindow(1000, 5000),
                new TimeWindow(1000, 5000),
                new TimeWindow(1000, 5000)
            };

            // Act
            var result = SessionWindows<string>.MergeWindows(windows).ToList();

            // Assert - Should merge into one
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Start, Is.EqualTo(1000));
            Assert.That(result[0].End, Is.EqualTo(5000));
        }

        [Test]
        public void MergeWindows_WithOneWindowInsideAnother_MergesCorrectly()
        {
            // Arrange - One window completely contained in another
            var windows = new List<TimeWindow>
            {
                new TimeWindow(1000, 10000),
                new TimeWindow(3000, 7000)  // Completely inside first window
            };

            // Act
            var result = SessionWindows<string>.MergeWindows(windows).ToList();

            // Assert - Should keep the larger window
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Start, Is.EqualTo(1000));
            Assert.That(result[0].End, Is.EqualTo(10000));
        }
    }
}
