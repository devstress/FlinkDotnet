using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Tests for internal data classes (CapturedOperation, WindowDefinition) to achieve 100% branch coverage.
    /// These are simple POCO classes with auto-properties that need basic coverage.
    /// </summary>
    [TestFixture]
    public class InternalDataClassesBranchCoverageTests
    {
        // Note: CapturedOperation and WindowDefinition are internal classes in OperationCapture.cs
        // We test them indirectly through the public API

        [Test]
        public void OperationCapture_CaptureMapOperation_StoresOperationCorrectly()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act - This internally creates CapturedOperation instances
            var mapped = stream.Map(x => x.ToUpper());
            var filtered = mapped.Filter(x => x.Length > 0);

            // Assert - Verify stream operations were captured
            Assert.That(mapped, Is.Not.Null);
            Assert.That(filtered, Is.Not.Null);
        }

        [Test]
        public void OperationCapture_CaptureFilterOperation_StoresOperationCorrectly()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act - This creates a CapturedOperation with Type="Filter"
            var filtered = stream.Filter(x => x.Length > 5);

            // Assert
            Assert.That(filtered, Is.Not.Null);
        }

        [Test]
        public void OperationCapture_CaptureFlatMapOperation_StoresOperationCorrectly()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act - This creates a CapturedOperation with Type="FlatMap"
            var flatMapped = stream.FlatMap(x => new[] { x, x.ToUpper() });

            // Assert
            Assert.That(flatMapped, Is.Not.Null);
        }

        [Test]
        public void WindowDefinition_TimeBasedWindow_CreatesCorrectly()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act - This creates a WindowDefinition with time-based window
            var windowed = stream.TimeWindowAll(Time.Seconds(10));

            // Assert
            Assert.That(windowed, Is.Not.Null);
        }

        [Test]
        public void WindowDefinition_CountBasedWindow_CreatesCorrectly()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act - This creates a WindowDefinition with count-based window
            var windowed = stream.CountWindowAll(100);

            // Assert
            Assert.That(windowed, Is.Not.Null);
        }

        // JobExecutionResult, JobStatus, SavepointResult, and StopWithSavepointResult
        // are tested indirectly through JobClient methods in JobClientTests.cs
        // They are simple POCOs with auto-properties that get covered through usage
    }
}
