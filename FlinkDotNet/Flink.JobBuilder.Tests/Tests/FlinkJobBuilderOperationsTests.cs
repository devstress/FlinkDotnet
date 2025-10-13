using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Flink.JobBuilder;
using Flink.JobBuilder.Models;

namespace Flink.JobBuilder.Tests.Tests
{
    public class FlinkJobBuilderOperationsTests
    {
        [Fact]
        public void Where_ShouldAddFilterOperation()
        {
            // Arrange
            var builder = FlinkJobBuilder.FromKafka("test-topic");
            var predicate = "value > 100";

            // Act
            var result = builder.Where(predicate);

            // Assert
            Assert.NotNull(result);
            Assert.Same(builder, result);
        }

        [Fact]
        public void Where_WithNullPredicate_ShouldHandleGracefully()
        {
            // Arrange
            var builder = FlinkJobBuilder.FromKafka("test-topic");

            // Act
            var result = builder.Where(null!);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void Map_ShouldAddMapOperation()
        {
            // Arrange
            var builder = FlinkJobBuilder.FromKafka("test-topic");
            var function = "value * 2";

            // Act
            var result = builder.Map(function);

            // Assert
            Assert.NotNull(result);
            Assert.Same(builder, result);
        }

        [Fact]
        public void Map_WithNullFunction_ShouldHandleGracefully()
        {
            // Arrange
            var builder = FlinkJobBuilder.FromKafka("test-topic");

            // Act
            var result = builder.Map(null!);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void FlatMap_ShouldAddFlatMapOperation()
        {
            // Arrange
            var builder = FlinkJobBuilder.FromKafka("test-topic");
            var function = "value.split(',')";

            // Act
            var result = builder.FlatMap(function);

            // Assert
            Assert.NotNull(result);
            Assert.Same(builder, result);
        }

        [Fact]
        public void GroupBy_ShouldAddGroupByOperation()
        {
            // Arrange
            var builder = FlinkJobBuilder.FromKafka("test-topic");
            var keySelector = "userId";

            // Act
            var result = builder.GroupBy(keySelector);

            // Assert
            Assert.NotNull(result);
            Assert.Same(builder, result);
        }

        [Fact]
        public void GroupBy_WithNullKeySelector_ShouldHandleGracefully()
        {
            // Arrange
            var builder = FlinkJobBuilder.FromKafka("test-topic");

            // Act
            var result = builder.GroupBy(null!);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void Aggregate_ShouldAddAggregateOperation()
        {
            // Arrange
            var builder = FlinkJobBuilder.FromKafka("test-topic").GroupBy("userId");
            var aggregateFunction = "SUM(amount)";

            // Act
            var result = builder.Aggregate(aggregateFunction);

            // Assert
            Assert.NotNull(result);
            Assert.Same(builder, result);
        }

        [Fact]
        public void Window_ShouldAddWindowOperation()
        {
            // Arrange
            var builder = FlinkJobBuilder.FromKafka("test-topic").GroupBy("userId");
            var windowType = "Tumbling";
            var windowSize = 60;

            // Act
            var result = builder.Window(windowType, windowSize);

            // Assert
            Assert.NotNull(result);
            Assert.Same(builder, result);
        }

        [Fact]
        public void Window_WithNullWindowType_ShouldHandleGracefully()
        {
            // Arrange
            var builder = FlinkJobBuilder.FromKafka("test-topic").GroupBy("userId");

            // Act
            var result = builder.Window(null!, 60);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void Join_ShouldAddJoinOperation()
        {
            // Arrange
            var builder = FlinkJobBuilder.FromKafka("test-topic");
            var otherStream = "other-stream";
            var joinKey = "userId";

            // Act
            var result = builder.Join(otherStream, joinKey);

            // Assert
            Assert.NotNull(result);
            Assert.Same(builder, result);
        }

        [Fact]
        public void Union_ShouldAddUnionOperation()
        {
            // Arrange
            var builder = FlinkJobBuilder.FromKafka("test-topic");
            var otherStream = "other-stream";

            // Act
            var result = builder.Union(otherStream);

            // Assert
            Assert.NotNull(result);
            Assert.Same(builder, result);
        }

        [Fact]
        public void KeyBy_ShouldAddKeyByOperation()
        {
            // Arrange
            var builder = FlinkJobBuilder.FromKafka("test-topic");
            var keySelector = "userId";

            // Act
            var result = builder.KeyBy(keySelector);

            // Assert
            Assert.NotNull(result);
            Assert.Same(builder, result);
        }

        [Fact]
        public void Reduce_ShouldAddReduceOperation()
        {
            // Arrange
            var builder = FlinkJobBuilder.FromKafka("test-topic").KeyBy("userId");
            var reduceFunction = "(a, b) => a + b";

            // Act
            var result = builder.Reduce(reduceFunction);

            // Assert
            Assert.NotNull(result);
            Assert.Same(builder, result);
        }

        [Fact]
        public void WithProcessFunction_ShouldAddProcessFunctionOperation()
        {
            // Arrange
            var builder = FlinkJobBuilder.FromKafka("test-topic");
            var processFunction = "processEvent";

            // Act
            var result = builder.WithProcessFunction(processFunction);

            // Assert
            Assert.NotNull(result);
            Assert.Same(builder, result);
        }

        [Fact]
        public void WithState_ShouldAddStatefulOperation()
        {
            // Arrange
            var builder = FlinkJobBuilder.FromKafka("test-topic").KeyBy("userId");
            var stateDescriptor = "userState";

            // Act
            var result = builder.WithState(stateDescriptor);

            // Assert
            Assert.NotNull(result);
            Assert.Same(builder, result);
        }

        [Fact]
        public void WithTimer_ShouldAddTimerOperation()
        {
            // Arrange
            var builder = FlinkJobBuilder.FromKafka("test-topic").KeyBy("userId");
            var timerType = "Processing";
            var delay = 5000;

            // Act
            var result = builder.WithTimer(timerType, delay);

            // Assert
            Assert.NotNull(result);
            Assert.Same(builder, result);
        }

        [Fact]
        public void WithRetry_ShouldAddRetryOperation()
        {
            // Arrange
            var builder = FlinkJobBuilder.FromKafka("test-topic");
            var maxRetries = 3;
            var backoffMs = 1000;

            // Act
            var result = builder.WithRetry(maxRetries, backoffMs);

            // Assert
            Assert.NotNull(result);
            Assert.Same(builder, result);
        }

        [Fact]
        public void WithRetry_WithNegativeRetries_ShouldHandleGracefully()
        {
            // Arrange
            var builder = FlinkJobBuilder.FromKafka("test-topic");

            // Act
            var result = builder.WithRetry(-1, 1000);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void AsyncHttp_ShouldAddAsyncHttpOperation()
        {
            // Arrange
            var builder = FlinkJobBuilder.FromKafka("test-topic");
            var url = "https://api.example.com/enrich";
            var method = "POST";

            // Act
            var result = builder.AsyncHttp(url, method);

            // Assert
            Assert.NotNull(result);
            Assert.Same(builder, result);
        }

        [Fact]
        public void AsyncHttp_WithNullUrl_ShouldHandleGracefully()
        {
            // Arrange
            var builder = FlinkJobBuilder.FromKafka("test-topic");

            // Act
            var result = builder.AsyncHttp(null!, "GET");

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void AsyncDatabase_ShouldAddAsyncDatabaseOperation()
        {
            // Arrange
            var builder = FlinkJobBuilder.FromKafka("test-topic");
            var connectionString = "Server=localhost;Database=test;";
            var query = "SELECT * FROM users WHERE id = ?";

            // Act
            var result = builder.AsyncDatabase(connectionString, query);

            // Assert
            Assert.NotNull(result);
            Assert.Same(builder, result);
        }

        [Fact]
        public void SideOutput_ShouldAddSideOutputOperation()
        {
            // Arrange
            var builder = FlinkJobBuilder.FromKafka("test-topic");
            var outputTag = "late-events";
            var condition = "timestamp < watermark";

            // Act
            var result = builder.SideOutput(outputTag, condition);

            // Assert
            Assert.NotNull(result);
            Assert.Same(builder, result);
        }

        [Fact]
        public void ChainedOperations_ShouldReturnSameBuilderInstance()
        {
            // Arrange
            var builder = FlinkJobBuilder.FromKafka("test-topic");

            // Act
            var result = builder
                .Where("value > 0")
                .Map("value * 2")
                .GroupBy("userId")
                .Aggregate("SUM(value)");

            // Assert
            Assert.NotNull(result);
            Assert.Same(builder, result);
        }

        [Fact]
        public void ComplexPipeline_ShouldSupportMultipleOperations()
        {
            // Arrange & Act
            var builder = FlinkJobBuilder.FromKafka("test-topic")
                .Where("value > 0")
                .Map("value * 2")
                .FlatMap("value.split(',')")
                .KeyBy("userId")
                .WithState("userState")
                .Reduce("(a, b) => a + b")
                .Window("Tumbling", 60)
                .WithRetry(3, 1000);

            // Assert
            Assert.NotNull(builder);
        }
    }
}
