using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlinkDotNet.DataStream;
using FlinkDotNet.DataStream.Window;
using FlinkDotNet.DataStream.Window.Assigners;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Comprehensive tests targeting specific uncovered areas to reach 100% coverage.
    /// Focuses on edge cases, error paths, and less-common code branches.
    /// </summary>
    [TestFixture]
    public class ComprehensiveCoverageTargetingTests
    {
        #region SessionWindows Coverage

        [Test]
        public void SessionWindows_AssignWindows_WithNegativeTimestamp_CreatesWindow()
        {
            // Arrange
            var windows = SessionWindows<string>.WithGap(Time.Seconds(10));

            // Act
            var result = windows.AssignWindows("test", -1000L);

            // Assert
            var windowList = new List<TimeWindow>(result);
            Assert.That(windowList, Has.Count.EqualTo(1));
        }

        [Test]
        public void SessionWindows_WithVerySmallGap_CreatesWindow()
        {
            // Arrange & Act
            var windows = SessionWindows<int>.WithGap(Time.Milliseconds(1));
            var result = windows.AssignWindows(42, 1000L);

            // Assert
            var windowList = new List<TimeWindow>(result);
            Assert.That(windowList, Has.Count.EqualTo(1));
            Assert.That(windowList[0].End - windowList[0].Start, Is.EqualTo(1));
        }

        [Test]
        public void SessionWindows_ToString_WithVeryLargeGap_ReturnsCorrectFormat()
        {
            // Arrange
            var windows = SessionWindows<string>.WithGap(Time.Hours(24));

            // Act
            var result = windows.ToString();

            // Assert
            Assert.That(result, Does.Contain("86400000ms gap"));
        }

        #endregion

        #region StreamExecutionEnvironment Coverage

        [Test]
        public void StreamExecutionEnvironment_GetExecutionEnvironment_WithNullConfig_CreatesEnvironment()
        {
            // Arrange & Act
            var env = StreamExecutionEnvironment.GetExecutionEnvironment(null);

            // Assert
            Assert.That(env, Is.Not.Null);
        }

        [Test]
        public void StreamExecutionEnvironment_FromCollection_WithLargeCollection_CreatesStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var data = new List<int>(Enumerable.Range(1, 1000));

            // Act
            var stream = env.FromCollection(data);

            // Assert
            Assert.That(stream, Is.Not.Null);
        }

        [Test]
        public void StreamExecutionEnvironment_SetParallelism_ReturnsEnvironment()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.SetParallelism(4);

            // Assert
            Assert.That(result, Is.SameAs(env));
            Assert.That(env.GetParallelism(), Is.EqualTo(4));
        }

        [Test]
        public void StreamExecutionEnvironment_SetMaxParallelism_ValidValue_ReturnsEnvironment()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.SetMaxParallelism(128);

            // Assert
            Assert.That(result, Is.SameAs(env));
            Assert.That(env.GetMaxParallelism(), Is.EqualTo(128));
        }

        [Test]
        public void StreamExecutionEnvironment_SetMaxParallelism_MaxValue_Succeeds()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            Assert.DoesNotThrow(() => env.SetMaxParallelism(32768));
        }

        [Test]
        public void StreamExecutionEnvironment_SetMaxParallelism_ZeroValue_ThrowsException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => env.SetMaxParallelism(0));
        }

        [Test]
        public void StreamExecutionEnvironment_SetMaxParallelism_NegativeValue_ThrowsException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => env.SetMaxParallelism(-1));
        }

        [Test]
        public void StreamExecutionEnvironment_SetMaxParallelism_TooLargeValue_ThrowsException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => env.SetMaxParallelism(32769));
        }

        [Test]
        public void StreamExecutionEnvironment_SetBufferTimeout_ReturnsEnvironment()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.SetBufferTimeout(200);

            // Assert
            Assert.That(result, Is.SameAs(env));
            Assert.That(env.GetBufferTimeout(), Is.EqualTo(200));
        }

        [Test]
        public void StreamExecutionEnvironment_DisableOperatorChaining_ReturnsEnvironment()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.DisableOperatorChaining();

            // Assert
            Assert.That(result, Is.SameAs(env));
            Assert.That(env.IsChainingEnabled(), Is.False);
        }

        [Test]
        public void StreamExecutionEnvironment_EnableCheckpointing_ReturnsEnvironment()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.EnableCheckpointing(5000);

            // Assert
            Assert.That(result, Is.SameAs(env));
            Assert.That(env.GetCheckpointInterval(), Is.EqualTo(5000));
        }

        [Test]
        public void StreamExecutionEnvironment_EnableAdaptiveScheduler_ReturnsEnvironment()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.EnableAdaptiveScheduler(true);

            // Assert
            Assert.That(result, Is.SameAs(env));
            Assert.That(env.IsAdaptiveSchedulerEnabled(), Is.True);
        }

        [Test]
        public void StreamExecutionEnvironment_EnableReactiveMode_ReturnsEnvironment()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.EnableReactiveMode(true);

            // Assert
            Assert.That(result, Is.SameAs(env));
            Assert.That(env.IsReactiveModeEnabled(), Is.True);
        }

        [Test]
        public void StreamExecutionEnvironment_FromSavepoint_ReturnsEnvironment()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var path = "/tmp/savepoints/sp-1";

            // Act
            var result = env.FromSavepoint(path);

            // Assert
            Assert.That(result, Is.SameAs(env));
            Assert.That(env.GetSavepointPath(), Is.EqualTo(path));
        }

        [Test]
        public void StreamExecutionEnvironment_SetStateBackend_WithHashMap_ReturnsEnvironment()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stateBackend = new FlinkDotNet.DataStream.State.HashMapStateBackend();

            // Act
            var result = env.SetStateBackend(stateBackend);

            // Assert
            Assert.That(result, Is.SameAs(env));
            Assert.That(env.GetStateBackend(), Is.SameAs(stateBackend));
        }

        [Test]
        public void StreamExecutionEnvironment_SetStateBackend_WithRocksDB_ReturnsEnvironment()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stateBackend = new FlinkDotNet.DataStream.State.EmbeddedRocksDBStateBackend();

            // Act
            var result = env.SetStateBackend(stateBackend);

            // Assert
            Assert.That(result, Is.SameAs(env));
            Assert.That(env.GetStateBackend(), Is.SameAs(stateBackend));
        }

        [Test]
        public void StreamExecutionEnvironment_SetStateBackend_WithNull_ThrowsException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => env.SetStateBackend(null!));
        }

        [Test]
        public void StreamExecutionEnvironment_GetCheckpointConfig_ReturnsConfig()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var config = env.GetCheckpointConfig();

            // Assert
            Assert.That(config, Is.Not.Null);
        }

        #endregion

        #region DataStream Additional Coverage

        [Test]
        public void DataStream_FlatMap_WithEmptyResult_CreatesStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var data = new List<string> { "a", "b", "c" };
            var stream = env.FromCollection(data);

            // Act
            var flatMapped = stream.FlatMap(x => Array.Empty<string>());

            // Assert
            Assert.That(flatMapped, Is.Not.Null);
        }

        [Test]
        public void DataStream_Map_WithExceptionInFunction_DoesNotThrowDuringCreation()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var data = new List<int> { 1, 2, 3 };
            var stream = env.FromCollection(data);

            // Act & Assert - Map creation should not throw, only execution would
            Assert.DoesNotThrow(() => stream.Map<int>(x => throw new InvalidOperationException()));
        }

        #endregion

        #region Time Coverage

        [Test]
        public void Time_Milliseconds_WithZero_CreatesTime()
        {
            // Arrange & Act
            var time = Time.Milliseconds(0);

            // Assert
            Assert.That(time.ToMilliseconds(), Is.EqualTo(0));
        }

        [Test]
        public void Time_Seconds_WithLargeValue_CreatesTime()
        {
            // Arrange & Act
            var time = Time.Seconds(3600);

            // Assert
            Assert.That(time.ToMilliseconds(), Is.EqualTo(3600000));
        }

        [Test]
        public void Time_Minutes_WithOne_CreatesTime()
        {
            // Arrange & Act
            var time = Time.Minutes(1);

            // Assert
            Assert.That(time.ToMilliseconds(), Is.EqualTo(60000));
        }

        [Test]
        public void Time_Hours_WithOne_CreatesTime()
        {
            // Arrange & Act
            var time = Time.Hours(1);

            // Assert
            Assert.That(time.ToMilliseconds(), Is.EqualTo(3600000));
        }

        [Test]
        public void Time_Days_WithOne_CreatesTime()
        {
            // Arrange & Act
            var time = Time.Days(1);

            // Assert
            Assert.That(time.ToMilliseconds(), Is.EqualTo(86400000));
        }

        #endregion
    }
}
