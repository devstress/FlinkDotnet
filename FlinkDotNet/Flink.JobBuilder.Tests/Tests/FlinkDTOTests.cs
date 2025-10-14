using NUnit.Framework;
using Flink.JobBuilder.Flink;

namespace Flink.JobBuilder.Tests.Tests
{
    /// <summary>
    /// Tests for Flink DTO classes to achieve 100% coverage
    /// Chunk 4: ConsumeResult, TopicPartition, RedisOperation, RedisOperationType, RedisTransactionResult
    /// </summary>
    [TestFixture]
    public class FlinkDTOTests
    {
        #region ConsumeResult Tests

        [Test]
        public void ConsumeResult_DefaultConstructor_InitializesProperties()
        {
            // Act
            var result = new ConsumeResult();

            // Assert
            Assert.That(result.Topic, Is.EqualTo(string.Empty));
            Assert.That(result.Partition, Is.EqualTo(0));
            Assert.That(result.Offset, Is.EqualTo(0));
            Assert.That(result.Key, Is.Null);
            Assert.That(result.Value, Is.Null);
        }

        [Test]
        public void ConsumeResult_SetProperties_StoresValues()
        {
            // Arrange
            var result = new ConsumeResult();
            var timestamp = System.DateTimeOffset.UtcNow;

            // Act
            result.Topic = "test-topic";
            result.Partition = 5;
            result.Offset = 12345L;
            result.Key = "test-key";
            result.Value = "test-value";
            result.Timestamp = timestamp;

            // Assert
            Assert.That(result.Topic, Is.EqualTo("test-topic"));
            Assert.That(result.Partition, Is.EqualTo(5));
            Assert.That(result.Offset, Is.EqualTo(12345L));
            Assert.That(result.Key, Is.EqualTo("test-key"));
            Assert.That(result.Value, Is.EqualTo("test-value"));
            Assert.That(result.Timestamp, Is.EqualTo(timestamp));
        }

        [Test]
        public void ConsumeResult_WithNullKeyAndValue_StoresNull()
        {
            // Arrange & Act
            var result = new ConsumeResult
            {
                Topic = "topic",
                Key = null,
                Value = null
            };

            // Assert
            Assert.That(result.Key, Is.Null);
            Assert.That(result.Value, Is.Null);
        }

        #endregion

        #region TopicPartition Tests

        [Test]
        public void TopicPartition_DefaultConstructor_InitializesProperties()
        {
            // Act
            var topicPartition = new TopicPartition();

            // Assert
            Assert.That(topicPartition.Topic, Is.EqualTo(string.Empty));
            Assert.That(topicPartition.Partition, Is.EqualTo(0));
        }

        [Test]
        public void TopicPartition_SetProperties_StoresValues()
        {
            // Arrange
            var topicPartition = new TopicPartition();

            // Act
            topicPartition.Topic = "my-topic";
            topicPartition.Partition = 3;

            // Assert
            Assert.That(topicPartition.Topic, Is.EqualTo("my-topic"));
            Assert.That(topicPartition.Partition, Is.EqualTo(3));
        }

        [Test]
        public void TopicPartition_ToString_ReturnsFormattedString()
        {
            // Arrange
            var topicPartition = new TopicPartition
            {
                Topic = "events",
                Partition = 7
            };

            // Act
            var result = topicPartition.ToString();

            // Assert
            Assert.That(result, Is.EqualTo("events[7]"));
        }

        #endregion

        #region RedisOperationType Tests

        [Test]
        public void RedisOperationType_Increment_HasCorrectValue()
        {
            // Assert
            Assert.That(RedisOperationType.Increment, Is.EqualTo(RedisOperationType.Increment));
            Assert.That(RedisOperationType.Increment.ToString(), Is.EqualTo("Increment"));
        }

        [Test]
        public void RedisOperationType_SetAdd_HasCorrectValue()
        {
            // Assert
            Assert.That(RedisOperationType.SetAdd, Is.EqualTo(RedisOperationType.SetAdd));
            Assert.That(RedisOperationType.SetAdd.ToString(), Is.EqualTo("SetAdd"));
        }

        [Test]
        public void RedisOperationType_Get_HasCorrectValue()
        {
            // Assert
            Assert.That(RedisOperationType.Get, Is.EqualTo(RedisOperationType.Get));
            Assert.That(RedisOperationType.Get.ToString(), Is.EqualTo("Get"));
        }

        [Test]
        public void RedisOperationType_Set_HasCorrectValue()
        {
            // Assert
            Assert.That(RedisOperationType.Set, Is.EqualTo(RedisOperationType.Set));
            Assert.That(RedisOperationType.Set.ToString(), Is.EqualTo("Set"));
        }

        [Test]
        public void RedisOperationType_Delete_HasCorrectValue()
        {
            // Assert
            Assert.That(RedisOperationType.Delete, Is.EqualTo(RedisOperationType.Delete));
            Assert.That(RedisOperationType.Delete.ToString(), Is.EqualTo("Delete"));
        }

        #endregion

        #region RedisOperation Tests

        [Test]
        public void RedisOperation_DefaultConstructor_InitializesProperties()
        {
            // Act
            var operation = new RedisOperation();

            // Assert
            Assert.That(operation.Type, Is.EqualTo(default(RedisOperationType)));
            Assert.That(operation.Key, Is.Null);
            Assert.That(operation.Member, Is.Null);
            Assert.That(operation.Value, Is.Null);
            Assert.That(operation.Increment, Is.EqualTo(1));
        }

        [Test]
        public void RedisOperation_SetProperties_StoresValues()
        {
            // Arrange
            var operation = new RedisOperation();

            // Act
            operation.Type = RedisOperationType.Set;
            operation.Key = "user:123";
            operation.Member = "field1";
            operation.Value = "test-value";
            operation.Increment = 5;

            // Assert
            Assert.That(operation.Type, Is.EqualTo(RedisOperationType.Set));
            Assert.That(operation.Key, Is.EqualTo("user:123"));
            Assert.That(operation.Member, Is.EqualTo("field1"));
            Assert.That(operation.Value, Is.EqualTo("test-value"));
            Assert.That(operation.Increment, Is.EqualTo(5));
        }

        [Test]
        public void RedisOperation_IncrementOperation_HasCorrectDefaults()
        {
            // Arrange & Act
            var operation = new RedisOperation
            {
                Type = RedisOperationType.Increment,
                Key = "counter"
            };

            // Assert
            Assert.That(operation.Type, Is.EqualTo(RedisOperationType.Increment));
            Assert.That(operation.Increment, Is.EqualTo(1)); // Default increment
        }

        [Test]
        public void RedisOperation_WithNullValues_StoresNull()
        {
            // Arrange & Act
            var operation = new RedisOperation
            {
                Type = RedisOperationType.Get,
                Key = null,
                Member = null,
                Value = null
            };

            // Assert
            Assert.That(operation.Key, Is.Null);
            Assert.That(operation.Member, Is.Null);
            Assert.That(operation.Value, Is.Null);
        }

        #endregion

        #region RedisTransactionResult Tests

        [Test]
        public void RedisTransactionResult_DefaultConstructor_InitializesProperties()
        {
            // Act
            var result = new RedisTransactionResult();

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Results, Is.Not.Null);
            Assert.That(result.Results, Is.Empty);
            Assert.That(result.ErrorMessage, Is.Null);
        }

        [Test]
        public void RedisTransactionResult_SetProperties_StoresValues()
        {
            // Arrange
            var result = new RedisTransactionResult();
            var results = new System.Collections.Generic.List<object> { "result1", 42, true };

            // Act
            result.Success = true;
            result.Results = results;
            result.ErrorMessage = "Test error";

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Results, Is.EqualTo(results));
            Assert.That(result.Results.Count, Is.EqualTo(3));
            Assert.That(result.ErrorMessage, Is.EqualTo("Test error"));
        }

        [Test]
        public void RedisTransactionResult_SuccessfulTransaction_HasNoError()
        {
            // Arrange & Act
            var result = new RedisTransactionResult
            {
                Success = true,
                Results = new System.Collections.Generic.List<object> { "OK" }
            };

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.ErrorMessage, Is.Null);
        }

        [Test]
        public void RedisTransactionResult_FailedTransaction_HasError()
        {
            // Arrange & Act
            var result = new RedisTransactionResult
            {
                Success = false,
                ErrorMessage = "Connection timeout"
            };

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("Connection timeout"));
        }

        #endregion
    }
}
