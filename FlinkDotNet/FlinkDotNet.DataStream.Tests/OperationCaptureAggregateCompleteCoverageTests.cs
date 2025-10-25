#nullable enable
using System;
using System.Reflection;
using Flink.JobBuilder.Models;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Comprehensive tests for OperationCapture.TranslateAggregateOperation to achieve 100% branch coverage.
    /// Tests all code paths including function presence, window types (time-based vs count-based), and no-window scenarios.
    /// </summary>
    [TestFixture]
    public class OperationCaptureAggregateCompleteCoverageTests
    {
        private static OperationCapture CreateOperationCapture()
        {
            // Use reflection to create OperationCapture since it's internal
            var type = typeof(DataStream<>).Assembly.GetType("FlinkDotNet.DataStream.OperationCapture");
            if (type == null)
            {
                throw new InvalidOperationException("OperationCapture type not found");
            }

            return (OperationCapture) Activator.CreateInstance(type, true)!;
        }

        private static void SetWindowDefinition(OperationCapture capture, bool isCountBased, long size)
        {
            // Use reflection to set _windowDefinition private field
            var windowDefType = typeof(DataStream<>).Assembly.GetType("FlinkDotNet.DataStream.WindowDefinition");
            if (windowDefType == null)
            {
                throw new InvalidOperationException("WindowDefinition type not found");
            }

            var windowDef = Activator.CreateInstance(windowDefType, true);

            var isCountProp = windowDefType!.GetProperty("IsCountBased");
            var sizeProp = windowDefType.GetProperty("Size");

            isCountProp!.SetValue(windowDef, isCountBased);
            sizeProp!.SetValue(windowDef, size);

            var field = capture.GetType().GetField("_windowDefinition", BindingFlags.NonPublic | BindingFlags.Instance);
            field!.SetValue(capture, windowDef);
        }

        private static object CreateCapturedOperation(string operationType, object? function)
        {
            var capturedOpType = typeof(DataStream<>).Assembly.GetType("FlinkDotNet.DataStream.CapturedOperation");
            if (capturedOpType == null)
            {
                throw new InvalidOperationException("CapturedOperation type not found");
            }

            var capturedOp = Activator.CreateInstance(capturedOpType, true);

            var opTypeProp = capturedOpType!.GetProperty("OperationType");
            var functionProp = capturedOpType.GetProperty("Function");

            opTypeProp!.SetValue(capturedOp, operationType);
            if (function != null)
            {
                functionProp!.SetValue(capturedOp, function);
            }

            return capturedOp!;
        }

        private static void CallTranslateAggregateOperation(OperationCapture capture, JobDefinition jobDef, object operation)
        {
            var method = capture.GetType().GetMethod("TranslateAggregateOperation",
                BindingFlags.NonPublic | BindingFlags.Instance);
            _ = method!.Invoke(capture, new[] { jobDef, operation });
        }

        [Test]
        public void TranslateAggregateOperation_WithFunction_SetsMetadata()
        {
            var capture = CreateOperationCapture();
            var jobDef = new JobDefinition { Metadata = new JobMetadata() };
            var function = new TestAggregateFunction();
            var operation = CreateCapturedOperation("aggregate", function);

            CallTranslateAggregateOperation(capture, jobDef, operation);

            Assert.That(jobDef.Metadata.Properties, Contains.Key("aggregateFunction"));
            Assert.That(jobDef.Metadata.Properties["aggregateFunction"], Does.Contain("TestAggregateFunction"));
            Assert.That(jobDef.Operations.Count, Is.EqualTo(1));
        }

        [Test]
        public void TranslateAggregateOperation_WithoutFunction_DoesNotSetMetadata()
        {
            var capture = CreateOperationCapture();
            var jobDef = new JobDefinition { Metadata = new JobMetadata() };
            var operation = CreateCapturedOperation("aggregate", null);

            CallTranslateAggregateOperation(capture, jobDef, operation);

            Assert.That(jobDef.Metadata.Properties, Does.Not.ContainKey("aggregateFunction"));
            Assert.That(jobDef.Operations.Count, Is.EqualTo(1));
        }

        [Test]
        public void TranslateAggregateOperation_WithTimeBasedWindow_SetsWindowSeconds()
        {
            var capture = CreateOperationCapture();
            SetWindowDefinition(capture, isCountBased: false, size: 60000);
            var jobDef = new JobDefinition { Metadata = new JobMetadata() };
            var operation = CreateCapturedOperation("aggregate", null);

            CallTranslateAggregateOperation(capture, jobDef, operation);

            var aggOp = jobDef.Operations[0] as AggregateOperationDefinition;
            Assert.That(aggOp, Is.Not.Null);
            Assert.That(aggOp!.WindowSeconds, Is.EqualTo(60));
            Assert.That(aggOp.WindowCount, Is.Null);
        }

        [Test]
        public void TranslateAggregateOperation_WithCountBasedWindow_SetsWindowCount()
        {
            var capture = CreateOperationCapture();
            SetWindowDefinition(capture, isCountBased: true, size: 100);
            var jobDef = new JobDefinition { Metadata = new JobMetadata() };
            var operation = CreateCapturedOperation("aggregate", null);

            CallTranslateAggregateOperation(capture, jobDef, operation);

            var aggOp = jobDef.Operations[0] as AggregateOperationDefinition;
            Assert.That(aggOp, Is.Not.Null);
            Assert.That(aggOp!.WindowCount, Is.EqualTo(100));
            Assert.That(aggOp.WindowSeconds, Is.Null);
        }

        [Test]
        public void TranslateAggregateOperation_WithoutWindow_SetsNeitherWindowProperty()
        {
            var capture = CreateOperationCapture();
            var jobDef = new JobDefinition { Metadata = new JobMetadata() };
            var operation = CreateCapturedOperation("aggregate", null);

            CallTranslateAggregateOperation(capture, jobDef, operation);

            var aggOp = jobDef.Operations[0] as AggregateOperationDefinition;
            Assert.That(aggOp, Is.Not.Null);
            Assert.That(aggOp!.WindowSeconds, Is.Null);
            Assert.That(aggOp.WindowCount, Is.Null);
        }

        [Test]
        public void TranslateAggregateOperation_WithFunctionAndTimeWindow_SetsBoth()
        {
            var capture = CreateOperationCapture();
            SetWindowDefinition(capture, isCountBased: false, size: 30000);
            var jobDef = new JobDefinition { Metadata = new JobMetadata() };
            var function = new TestAggregateFunction();
            var operation = CreateCapturedOperation("aggregate", function);

            CallTranslateAggregateOperation(capture, jobDef, operation);

            Assert.That(jobDef.Metadata.Properties, Contains.Key("aggregateFunction"));
            var aggOp = jobDef.Operations[0] as AggregateOperationDefinition;
            Assert.That(aggOp!.WindowSeconds, Is.EqualTo(30));
            Assert.That(aggOp.WindowCount, Is.Null);
        }

        [Test]
        public void TranslateAggregateOperation_WithFunctionAndCountWindow_SetsBoth()
        {
            var capture = CreateOperationCapture();
            SetWindowDefinition(capture, isCountBased: true, size: 50);
            var jobDef = new JobDefinition { Metadata = new JobMetadata() };
            var function = new TestAggregateFunction();
            var operation = CreateCapturedOperation("aggregate", function);

            CallTranslateAggregateOperation(capture, jobDef, operation);

            Assert.That(jobDef.Metadata.Properties, Contains.Key("aggregateFunction"));
            var aggOp = jobDef.Operations[0] as AggregateOperationDefinition;
            Assert.That(aggOp!.WindowCount, Is.EqualTo(50));
            Assert.That(aggOp.WindowSeconds, Is.Null);
        }

        [Test]
        public void TranslateAggregateOperation_AlwaysAddsOperation()
        {
            var capture = CreateOperationCapture();
            var jobDef = new JobDefinition { Metadata = new JobMetadata() };
            var operation = CreateCapturedOperation("aggregate", null);

            CallTranslateAggregateOperation(capture, jobDef, operation);

            Assert.That(jobDef.Operations.Count, Is.EqualTo(1));
            Assert.That(jobDef.Operations[0], Is.InstanceOf<AggregateOperationDefinition>());
            var aggOp = jobDef.Operations[0] as AggregateOperationDefinition;
            Assert.That(aggOp!.AggregationType, Is.EqualTo("COLLECT"));
            Assert.That(aggOp.Field, Is.EqualTo("*"));
        }

        /// <summary>
        /// Test aggregate function class for testing purposes.
        /// Provides a simple placeholder implementation for aggregate testing.
        /// </summary>
        private class TestAggregateFunction : IAggregateFunction<int, int, int>
        {
            public int CreateAccumulator() => 0;
            public int Add(int value, int accumulator) => accumulator + value;
            public int GetResult(int accumulator) => accumulator;
            public int Merge(int acc1, int acc2) => acc1 + acc2;
        }
    }
}
