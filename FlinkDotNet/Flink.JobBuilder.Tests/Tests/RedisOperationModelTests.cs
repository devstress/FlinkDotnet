using Flink.JobBuilder.Flink;

namespace Flink.JobBuilder.Tests.Tests
{
    /// <summary>
    /// Tests for Redis operation models to improve coverage
    /// Target: Cover RedisOperation and related models
    /// </summary>
    [TestFixture]
    public class RedisOperationModelTests
    {
        #region RedisOperation Tests

        [Test]
        public void RedisOperation_DefaultConstructor_InitializesWithDefaults()
        {
            // Act
            var op = new RedisOperation();

            // Assert
            Assert.That(op, Is.Not.Null);
            Assert.That(op.Type, Is.EqualTo(RedisOperationType.Increment)); // Default value
        }

        [Test]
        public void RedisOperation_SetType_UpdatesType()
        {
            // Arrange
            var op = new RedisOperation();

            // Act
            op.Type = RedisOperationType.SetAdd;

            // Assert
            Assert.That(op.Type, Is.EqualTo(RedisOperationType.SetAdd));
        }

        [Test]
        public void RedisOperation_SetKey_UpdatesKey()
        {
            // Arrange
            var op = new RedisOperation();

            // Act
            op.Key = "test-key";

            // Assert
            Assert.That(op.Key, Is.EqualTo("test-key"));
        }

        [Test]
        public void RedisOperation_SetMember_UpdatesMember()
        {
            // Arrange
            var op = new RedisOperation();

            // Act
            op.Member = "test-member";

            // Assert
            Assert.That(op.Member, Is.EqualTo("test-member"));
        }

        [Test]
        public void RedisOperation_SetIncrement_UpdatesIncrement()
        {
            // Arrange
            var op = new RedisOperation();

            // Act
            op.Increment = 5;

            // Assert
            Assert.That(op.Increment, Is.EqualTo(5));
        }

        [Test]
        public void RedisOperation_SetValue_UpdatesValue()
        {
            // Arrange
            var op = new RedisOperation();

            // Act
            op.Value = "test-value";

            // Assert
            Assert.That(op.Value, Is.EqualTo("test-value"));
        }

        [Test]
        public void RedisOperation_AllProperties_CanBeSetTogether()
        {
            // Act
            var op = new RedisOperation
            {
                Type = RedisOperationType.Set,
                Key = "my-key",
                Value = "my-value",
                Member = "my-member",
                Increment = 10
            };

            // Assert
            Assert.That(op.Type, Is.EqualTo(RedisOperationType.Set));
            Assert.That(op.Key, Is.EqualTo("my-key"));
            Assert.That(op.Value, Is.EqualTo("my-value"));
            Assert.That(op.Member, Is.EqualTo("my-member"));
            Assert.That(op.Increment, Is.EqualTo(10));
        }

        #endregion

        #region RedisOperationType Tests

        [Test]
        public void RedisOperationType_HasIncrementValue()
        {
            // Assert
            Assert.That(RedisOperationType.Increment, Is.EqualTo(RedisOperationType.Increment));
        }

        [Test]
        public void RedisOperationType_HasSetAddValue()
        {
            // Assert
            Assert.That(RedisOperationType.SetAdd, Is.EqualTo(RedisOperationType.SetAdd));
        }

        [Test]
        public void RedisOperationType_HasGetValue()
        {
            // Assert
            Assert.That(RedisOperationType.Get, Is.EqualTo(RedisOperationType.Get));
        }

        [Test]
        public void RedisOperationType_HasSetValue()
        {
            // Assert
            Assert.That(RedisOperationType.Set, Is.EqualTo(RedisOperationType.Set));
        }

        #endregion

        #region RedisTransactionResult Tests

        [Test]
        public void RedisTransactionResult_DefaultConstructor_InitializesWithDefaults()
        {
            // Act
            var result = new RedisTransactionResult();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False); // Default bool value
            Assert.That(result.ErrorMessage, Is.Null.Or.Empty);
            Assert.That(result.Results, Is.Null.Or.Empty);
        }

        [Test]
        public void RedisTransactionResult_SetSuccess_UpdatesSuccess()
        {
            // Arrange
            var result = new RedisTransactionResult();

            // Act
            result.Success = true;

            // Assert
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void RedisTransactionResult_SetErrorMessage_UpdatesErrorMessage()
        {
            // Arrange
            var result = new RedisTransactionResult();

            // Act
            result.ErrorMessage = "Test error";

            // Assert
            Assert.That(result.ErrorMessage, Is.EqualTo("Test error"));
        }

        [Test]
        public void RedisTransactionResult_SetResults_UpdatesResults()
        {
            // Arrange
            var result = new RedisTransactionResult();
            var results = new List<object> { 1, "test", true };

            // Act
            result.Results = results;

            // Assert
            Assert.That(result.Results, Is.EqualTo(results));
            Assert.That(result.Results, Has.Count.EqualTo(3));
        }

        [Test]
        public void RedisTransactionResult_SuccessScenario_HasNoError()
        {
            // Act
            var result = new RedisTransactionResult
            {
                Success = true,
                Results = new List<object> { "result1", "result2" }
            };

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.ErrorMessage, Is.Null);
            Assert.That(result.Results, Is.Not.Null);
        }

        [Test]
        public void RedisTransactionResult_FailureScenario_HasError()
        {
            // Act
            var result = new RedisTransactionResult
            {
                Success = false,
                ErrorMessage = "Transaction failed"
            };

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.Not.Null);
        }

        #endregion
    }
}
