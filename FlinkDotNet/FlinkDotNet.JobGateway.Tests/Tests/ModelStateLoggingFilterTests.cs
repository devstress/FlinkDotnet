using FlinkDotNet.JobGateway.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;

namespace FlinkDotNet.JobGateway.Tests
{
    [TestFixture]
    public class ModelStateLoggingFilterTests
    {
        private Mock<ILogger<ModelStateLoggingFilter>> _mockLogger = null!;
        private ModelStateLoggingFilter _filter = null!;

        [SetUp]
        public void SetUp()
        {
            _mockLogger = new Mock<ILogger<ModelStateLoggingFilter>>();
            this._filter = new ModelStateLoggingFilter(_mockLogger.Object);
        }

        [Test]
        public void OnActionExecuting_WithValidModelState_DoesNotLog()
        {
            // Arrange
            var actionContext = CreateActionContext();
            var context = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object?>(),
                new object());

            // Act
            this._filter.OnActionExecuting(context);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        [Test]
        public void OnActionExecuting_WithInvalidModelState_LogsWarning()
        {
            // Arrange
            var actionContext = CreateActionContext();
            actionContext.ModelState.AddModelError("TestProperty", "Test error message");

            var context = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object?>(),
                new object());

            // Act
            this._filter.OnActionExecuting(context);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ModelState invalid")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public void OnActionExecuting_WithMultipleValidationErrors_LogsAllErrors()
        {
            // Arrange
            var actionContext = CreateActionContext();
            actionContext.ModelState.AddModelError("Property1", "Error 1");
            actionContext.ModelState.AddModelError("Property2", "Error 2");
            actionContext.ModelState.AddModelError("Property2", "Error 3"); // Multiple errors on same property

            var context = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object?>(),
                new object());

            // Act
            this._filter.OnActionExecuting(context);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString()!.Contains("Property1:Error 1") &&
                        v.ToString()!.Contains("Property2:Error 2|Error 3")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public void OnActionExecuting_LogsRequestPath()
        {
            // Arrange
            var actionContext = CreateActionContext("/api/test/endpoint");
            actionContext.ModelState.AddModelError("Field", "Validation failed");

            var context = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object?>(),
                new object());

            // Act
            this._filter.OnActionExecuting(context);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("/api/test/endpoint")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public void OnActionExecuted_DoesNothing()
        {
            // Arrange
            var actionContext = CreateActionContext();
            var context = new ActionExecutedContext(
                actionContext,
                new List<IFilterMetadata>(),
                new object());

            // Act - Should not throw any exceptions
            this._filter.OnActionExecuted(context);

            // Assert - No logging should occur
            _mockLogger.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        private static ActionContext CreateActionContext(string path = "/test")
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = path;

            return new ActionContext(
                httpContext,
                new RouteData(),
                new ActionDescriptor(),
                new ModelStateDictionary());
        }
    }
}
