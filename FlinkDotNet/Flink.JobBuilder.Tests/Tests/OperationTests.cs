using Xunit;
using FlinkDotNet.Models;
using System.Collections.Generic;
using System;

namespace Flink.JobBuilder.Tests.Tests
{
    public class OperationTests
    {
        [Fact]
        public void MapOperationDefinition_Constructor_SetsProperties()
        {
            var mapOp = new MapOperationDefinition
            {
                OperationId = "map-1",
                MapFunction = "x => x.ToUpper()"
            };

            Assert.Equal("map-1", mapOp.OperationId);
            Assert.Equal("x => x.ToUpper()", mapOp.MapFunction);
        }

        [Fact]
        public void FilterOperationDefinition_Constructor_SetsProperties()
        {
            var filterOp = new FilterOperationDefinition
            {
                OperationId = "filter-1",
                FilterPredicate = "x => x > 0"
            };

            Assert.Equal("filter-1", filterOp.OperationId);
            Assert.Equal("x => x > 0", filterOp.FilterPredicate);
        }

        [Fact]
        public void KeyByOperationDefinition_Constructor_SetsProperties()
        {
            var keyByOp = new KeyByOperationDefinition
            {
                OperationId = "keyby-1",
                KeySelector = "x => x.Id"
            };

            Assert.Equal("keyby-1", keyByOp.OperationId);
            Assert.Equal("x => x.Id", keyByOp.KeySelector);
        }

        [Fact]
        public void WindowOperationDefinition_Constructor_SetsProperties()
        {
            var windowOp = new WindowOperationDefinition
            {
                OperationId = "window-1",
                WindowType = "Tumbling",
                WindowSize = TimeSpan.FromMinutes(5)
            };

            Assert.Equal("window-1", windowOp.OperationId);
            Assert.Equal("Tumbling", windowOp.WindowType);
            Assert.Equal(TimeSpan.FromMinutes(5), windowOp.WindowSize);
        }

        [Fact]
        public void AggregateOperationDefinition_Constructor_SetsProperties()
        {
            var aggOp = new AggregateOperationDefinition
            {
                OperationId = "agg-1",
                AggregationType = "Sum",
                AggregationField = "Amount"
            };

            Assert.Equal("agg-1", aggOp.OperationId);
            Assert.Equal("Sum", aggOp.AggregationType);
            Assert.Equal("Amount", aggOp.AggregationField);
        }

        [Fact]
        public void JoinOperationDefinition_Constructor_SetsProperties()
        {
            var joinOp = new JoinOperationDefinition
            {
                OperationId = "join-1",
                LeftKeySelector = "x => x.Id",
                RightKeySelector = "y => y.UserId",
                JoinType = "Inner"
            };

            Assert.Equal("join-1", joinOp.OperationId);
            Assert.Equal("x => x.Id", joinOp.LeftKeySelector);
            Assert.Equal("y => y.UserId", joinOp.RightKeySelector);
            Assert.Equal("Inner", joinOp.JoinType);
        }

        [Fact]
        public void FlatMapOperationDefinition_Constructor_SetsProperties()
        {
            var flatMapOp = new FlatMapOperationDefinition
            {
                OperationId = "flatmap-1",
                FlatMapFunction = "x => x.Split(',')"
            };

            Assert.Equal("flatmap-1", flatMapOp.OperationId);
            Assert.Equal("x => x.Split(',')", flatMapOp.FlatMapFunction);
        }

        [Fact]
        public void ReduceOperationDefinition_Constructor_SetsProperties()
        {
            var reduceOp = new ReduceOperationDefinition
            {
                OperationId = "reduce-1",
                ReduceFunction = "(a, b) => a + b"
            };

            Assert.Equal("reduce-1", reduceOp.OperationId);
            Assert.Equal("(a, b) => a + b", reduceOp.ReduceFunction);
        }

        [Fact]
        public void UnionOperationDefinition_Constructor_SetsProperties()
        {
            var unionOp = new UnionOperationDefinition
            {
                OperationId = "union-1",
                StreamIds = new List<string> { "stream1", "stream2" }
            };

            Assert.Equal("union-1", unionOp.OperationId);
            Assert.Contains("stream1", unionOp.StreamIds);
            Assert.Contains("stream2", unionOp.StreamIds);
        }

        [Fact]
        public void SideOutputDefinition_Constructor_SetsProperties()
        {
            var sideOutput = new SideOutputDefinition
            {
                OutputTag = "late-data",
                OutputStreamId = "late-stream"
            };

            Assert.Equal("late-data", sideOutput.OutputTag);
            Assert.Equal("late-stream", sideOutput.OutputStreamId);
        }

        [Fact]
        public void ProcessFunctionDefinition_Constructor_SetsProperties()
        {
            var processFunc = new ProcessFunctionDefinition
            {
                OperationId = "process-1",
                ProcessLogic = "custom logic",
                StateDescriptors = new List<string> { "state1", "state2" }
            };

            Assert.Equal("process-1", processFunc.OperationId);
            Assert.Equal("custom logic", processFunc.ProcessLogic);
            Assert.Equal(2, processFunc.StateDescriptors.Count);
        }

        [Fact]
        public void WatermarkStrategyDefinition_Constructor_SetsProperties()
        {
            var watermark = new WatermarkStrategyDefinition
            {
                TimestampAssigner = "x => x.Timestamp",
                MaxOutOfOrderness = TimeSpan.FromSeconds(5),
                IdlenessTimeout = TimeSpan.FromMinutes(1)
            };

            Assert.Equal("x => x.Timestamp", watermark.TimestampAssigner);
            Assert.Equal(TimeSpan.FromSeconds(5), watermark.MaxOutOfOrderness);
            Assert.Equal(TimeSpan.FromMinutes(1), watermark.IdlenessTimeout);
        }
    }
}
