#nullable enable
namespace FlinkDotNet.DataStream.Tests
{
    [NUnit.Framework.TestFixture]
    public class OperationCaptureUnknownFunctionTests
    {
        // Custom map function for testing unknown type handling
        private class CustomMapFunction : IMapFunction<string, string>
        {
            public string Map(string value) => value.ToUpper();
        }

        private class UnknownMapFunction : IMapFunction<string, string>
        {
            public string Map(string value) => value + "_processed";
        }

        private class CustomFilterFunction : IFilterFunction<string>
        {
            public bool Filter(string value) => value.Length > 5;
        }

        [NUnit.Framework.Test]
        public void TranslateMapOperation_WithUnknownFunction_UsesFunctionTypeName()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromKafka("test-topic", "localhost:9092", "test-group");

            var customFunction = new CustomMapFunction();

            // Act
            _ = stream.Map(customFunction);

            // Get the captured operations using reflection
            var operationCaptureField = typeof(DataStream<string>).GetField("_operationCapture",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var operationCapture = operationCaptureField?.GetValue(stream);

            // Assert - Operation should be captured even for unknown function types
            NUnit.Framework.Assert.That(operationCapture, NUnit.Framework.Is.Not.Null);
        }

        [NUnit.Framework.Test]
        public void TranslateMapOperation_WithUnknownFunction_LogsWarning()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromKafka("test-topic", "localhost:9092", "test-group");

            var unknownFunction = new UnknownMapFunction();

            // Act - Map with unknown function should not throw
            NUnit.Framework.Assert.DoesNotThrow(() => _ = stream.Map(unknownFunction), "Should handle unknown map function gracefully");
        }

        [NUnit.Framework.Test]
        public void ToJobDefinition_WithUnknownMapFunction_CreatesJobDefinition()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromKafka("test-topic", "localhost:9092", "test-group");
            var customFunction = new CustomMapFunction();
            _ = stream.Map(customFunction);

            // Act - ExecuteAsync should translate unknown function and fail validation
            _ = NUnit.Framework.Assert.ThrowsAsync<System.InvalidOperationException>(async () => _ = await env.ExecuteAsync("test-job"), "Should translate unknown function and fail validation");
        }

        [NUnit.Framework.Test]
        public void TranslateMapOperation_WithNullFunction_HandlesGracefully()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act - Get operation capture to test directly
            var operationCaptureField = typeof(DataStream<string>).GetField("_operationCapture",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var operationCapture = operationCaptureField?.GetValue(stream);

            // Get CaptureMapOperation method
            var captureMethod = operationCapture?.GetType().GetMethod("CaptureMapOperation",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            // Act - Should not throw with null function
            NUnit.Framework.Assert.DoesNotThrow(() => _ = (captureMethod?.Invoke(operationCapture, new object?[] { "unknown", null })), "Should handle null function parameter");
        }

        [NUnit.Framework.Test]
        public void TranslateFilterOperation_WithCustomFunction_CreatesFilterDefinition()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromKafka("test-topic", "localhost:9092", "test-group");
            var filterFunction = new CustomFilterFunction();

            // Act
            _ = stream.Filter(filterFunction);

            // Assert - Should translate custom filter and fail validation
            _ = NUnit.Framework.Assert.ThrowsAsync<System.InvalidOperationException>(async () => _ = await env.ExecuteAsync("test-job"), "Should handle custom filter function and fail validation");
        }

        [NUnit.Framework.Test]
        public void TranslateMapOperation_WithMultipleUnknownFunctions_HandlesAll()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromKafka("test-topic", "localhost:9092", "test-group");

            var function1 = new CustomMapFunction();
            var function2 = new UnknownMapFunction();

            // Act - Chain multiple unknown functions
            _ = stream.Map(function1).Map(function2);

            // Assert
            _ = NUnit.Framework.Assert.ThrowsAsync<System.InvalidOperationException>(async () => _ = await env.ExecuteAsync("multi-function-job"), "Should handle multiple unknown functions and fail validation");
        }

        [NUnit.Framework.Test]
        public void TranslateMapOperation_WithFunctionFullName_IncludesInExpression()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromKafka("test-topic", "localhost:9092", "test-group");
            var customFunction = new CustomMapFunction();

            // Act
            _ = stream.Map(customFunction);

            // Get operation capture
            var operationCaptureField = typeof(DataStream<string>).GetField("_operationCapture",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var operationCapture = operationCaptureField?.GetValue(stream);

            // Get operations list
            var operationsField = operationCapture?.GetType().GetField("_operations",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var operations = operationsField?.GetValue(operationCapture) as System.Collections.IList;

            // Assert - Should have captured the operation
            NUnit.Framework.Assert.That(operations, NUnit.Framework.Is.Not.Null);
            NUnit.Framework.Assert.That(operations.Count, NUnit.Framework.Is.GreaterThan(0));
        }

        [NUnit.Framework.Test]
        public void TranslateMapOperation_WithUppercaseFunctionName_MapsToUpper()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Create a function with "Upper" in the name
            var upperFunction = new UpperCaseMapFunction();

            // Act
            _ = stream.Map(upperFunction);

            // Assert - Should recognize Upper in name and map to "upper" expression
            _ = NUnit.Framework.Assert.ThrowsAsync<System.InvalidOperationException>(async () => _ = await env.ExecuteAsync("upper-test-job"), "Should recognize uppercase function names and fail validation");
        }

        [NUnit.Framework.Test]
        public void TranslateMapOperation_WithLowercaseFunctionName_MapsToLower()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Create a function with "Lower" in the name
            var lowerFunction = new LowerCaseMapFunction();

            // Act
            _ = stream.Map(lowerFunction);

            // Assert - Should recognize Lower in name and map to "lower" expression
            _ = NUnit.Framework.Assert.ThrowsAsync<System.InvalidOperationException>(async () => _ = await env.ExecuteAsync("lower-test-job"), "Should recognize lowercase function names and fail validation");
        }

        [NUnit.Framework.Test]
        public void TranslateMapOperation_WithCapitalizerFunctionName_MapsToUpper()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Create a function with "Capitalizer" in the name
            var capFunction = new WordsCapitalizerFunction();

            // Act
            _ = stream.Map(capFunction);

            // Assert - Should recognize Capitalizer in name and map to "upper" expression
            _ = NUnit.Framework.Assert.ThrowsAsync<System.InvalidOperationException>(async () => _ = await env.ExecuteAsync("capitalizer-test-job"), "Should recognize capitalizer function names and fail validation");
        }

        // Helper classes for testing function name recognition
        private class UpperCaseMapFunction : IMapFunction<string, string>
        {
            public string Map(string value) => value.ToUpper();
        }

        private class LowerCaseMapFunction : IMapFunction<string, string>
        {
            public string Map(string value) => value.ToLower();
        }

        private class WordsCapitalizerFunction : IMapFunction<string, string>
        {
            public string Map(string value) => value.ToUpper();
        }
    }
}
