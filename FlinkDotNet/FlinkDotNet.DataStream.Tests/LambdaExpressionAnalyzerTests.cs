using System;
using System.Linq.Expressions;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Tests for LambdaExpressionAnalyzer utility class.
    /// Validates lambda expression parsing and translation to Flink IR.
    /// </summary>
    [TestFixture]
    public class LambdaExpressionAnalyzerTests
    {
        #region String Method Analysis Tests

        [Test]
        public void AnalyzeLambda_WithToUpper_ReturnsUpperExpression()
        {
            // Arrange
            Expression<Func<string, string>> lambda = s => s.ToUpper();
            
            // Act
            var result = LambdaExpressionAnalyzer.AnalyzeLambda(lambda);
            
            // Assert
            Assert.That(result, Is.EqualTo("upper"));
        }

        [Test]
        public void AnalyzeLambda_WithToUpperInvariant_ReturnsUpperExpression()
        {
            // Arrange
            Expression<Func<string, string>> lambda = s => s.ToUpperInvariant();
            
            // Act
            var result = LambdaExpressionAnalyzer.AnalyzeLambda(lambda);
            
            // Assert
            Assert.That(result, Is.EqualTo("upper"));
        }

        [Test]
        public void AnalyzeLambda_WithToLower_ReturnsLowerExpression()
        {
            // Arrange
            Expression<Func<string, string>> lambda = s => s.ToLower();
            
            // Act
            var result = LambdaExpressionAnalyzer.AnalyzeLambda(lambda);
            
            // Assert
            Assert.That(result, Is.EqualTo("lower"));
        }

        [Test]
        public void AnalyzeLambda_WithToLowerInvariant_ReturnsLowerExpression()
        {
            // Arrange
            Expression<Func<string, string>> lambda = s => s.ToLowerInvariant();
            
            // Act
            var result = LambdaExpressionAnalyzer.AnalyzeLambda(lambda);
            
            // Assert
            Assert.That(result, Is.EqualTo("lower"));
        }

        [Test]
        public void AnalyzeLambda_WithTrim_ReturnsTrimExpression()
        {
            // Arrange
            Expression<Func<string, string>> lambda = s => s.Trim();
            
            // Act
            var result = LambdaExpressionAnalyzer.AnalyzeLambda(lambda);
            
            // Assert
            Assert.That(result, Is.EqualTo("trim"));
        }

        [Test]
        public void AnalyzeLambda_WithTrimStart_ReturnsLtrimExpression()
        {
            // Arrange
            Expression<Func<string, string>> lambda = s => s.TrimStart();
            
            // Act
            var result = LambdaExpressionAnalyzer.AnalyzeLambda(lambda);
            
            // Assert
            Assert.That(result, Is.EqualTo("ltrim"));
        }

        [Test]
        public void AnalyzeLambda_WithTrimEnd_ReturnsRtrimExpression()
        {
            // Arrange
            Expression<Func<string, string>> lambda = s => s.TrimEnd();
            
            // Act
            var result = LambdaExpressionAnalyzer.AnalyzeLambda(lambda);
            
            // Assert
            Assert.That(result, Is.EqualTo("rtrim"));
        }

        #endregion

        #region Method Chaining Tests

        [Test]
        public void AnalyzeLambda_WithTrimAndToUpper_ReturnsCompositeExpression()
        {
            // Arrange
            Expression<Func<string, string>> lambda = s => s.Trim().ToUpper();
            
            // Act
            var result = LambdaExpressionAnalyzer.AnalyzeLambda(lambda);
            
            // Assert
            Assert.That(result, Is.EqualTo("trim,upper"));
        }

        [Test]
        public void AnalyzeLambda_WithToLowerAndTrim_ReturnsCompositeExpression()
        {
            // Arrange
            Expression<Func<string, string>> lambda = s => s.ToLower().Trim();
            
            // Act
            var result = LambdaExpressionAnalyzer.AnalyzeLambda(lambda);
            
            // Assert
            Assert.That(result, Is.EqualTo("lower,trim"));
        }

        [Test]
        public void AnalyzeLambda_WithTripleChain_ReturnsCompositeExpression()
        {
            // Arrange
            Expression<Func<string, string>> lambda = s => s.TrimStart().ToUpperInvariant().TrimEnd();
            
            // Act
            var result = LambdaExpressionAnalyzer.AnalyzeLambda(lambda);
            
            // Assert
            Assert.That(result, Is.EqualTo("ltrim,upper,rtrim"));
        }

        #endregion

        #region Numeric Operation Tests

        [Test]
        public void AnalyzeLambda_WithMultiplication_ReturnsMultiplyExpression()
        {
            // Arrange
            Expression<Func<int, int>> lambda = i => i * 2;
            
            // Act
            var result = LambdaExpressionAnalyzer.AnalyzeLambda(lambda);
            
            // Assert
            Assert.That(result, Is.EqualTo("multiply:$input:2"));
        }

        [Test]
        public void AnalyzeLambda_WithSquare_ReturnsMultiplyExpression()
        {
            // Arrange
            Expression<Func<int, int>> lambda = i => i * i;
            
            // Act
            var result = LambdaExpressionAnalyzer.AnalyzeLambda(lambda);
            
            // Assert
            Assert.That(result, Is.EqualTo("multiply:$input:$input"));
        }

        [Test]
        public void AnalyzeLambda_WithAddition_ReturnsAddExpression()
        {
            // Arrange
            Expression<Func<int, int>> lambda = i => i + 10;
            
            // Act
            var result = LambdaExpressionAnalyzer.AnalyzeLambda(lambda);
            
            // Assert
            Assert.That(result, Is.EqualTo("add:$input:10"));
        }

        [Test]
        public void AnalyzeLambda_WithSubtraction_ReturnsSubtractExpression()
        {
            // Arrange
            Expression<Func<int, int>> lambda = i => i - 5;
            
            // Act
            var result = LambdaExpressionAnalyzer.AnalyzeLambda(lambda);
            
            // Assert
            Assert.That(result, Is.EqualTo("subtract:$input:5"));
        }

        [Test]
        public void AnalyzeLambda_WithDivision_ReturnsDivideExpression()
        {
            // Arrange
            Expression<Func<int, int>> lambda = i => i / 2;
            
            // Act
            var result = LambdaExpressionAnalyzer.AnalyzeLambda(lambda);
            
            // Assert
            Assert.That(result, Is.EqualTo("divide:$input:2"));
        }

        [Test]
        public void AnalyzeLambda_WithModulo_ReturnsModuloExpression()
        {
            // Arrange
            Expression<Func<int, int>> lambda = i => i % 3;
            
            // Act
            var result = LambdaExpressionAnalyzer.AnalyzeLambda(lambda);
            
            // Assert
            Assert.That(result, Is.EqualTo("modulo:$input:3"));
        }

        #endregion

        #region Identity and Unsupported Tests

        [Test]
        public void AnalyzeLambda_WithIdentity_ReturnsIdentityExpression()
        {
            // Arrange
            Expression<Func<string, string>> lambda = s => s;
            
            // Act
            var result = LambdaExpressionAnalyzer.AnalyzeLambda(lambda);
            
            // Assert
            Assert.That(result, Is.EqualTo("identity"));
        }

        [Test]
        public void AnalyzeLambda_WithUnsupportedMethod_ReturnsNull()
        {
            // Arrange
            Expression<Func<string, int>> lambda = s => s.Length;
            
            // Act
            var result = LambdaExpressionAnalyzer.AnalyzeLambda(lambda);
            
            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void AnalyzeLambda_WithNullExpression_ReturnsNull()
        {
            // Act
            var result = LambdaExpressionAnalyzer.AnalyzeLambda<string, string>(null!);
            
            // Assert
            Assert.That(result, Is.Null);
        }

        #endregion
    }
}
