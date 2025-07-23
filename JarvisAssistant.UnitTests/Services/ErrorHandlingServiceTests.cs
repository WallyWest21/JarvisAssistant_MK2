using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using JarvisAssistant.Core.Models;
using JarvisAssistant.Services;
using JarvisAssistant.Core.Interfaces;

namespace JarvisAssistant.UnitTests.Services
{
    /// <summary>
    /// Unit tests for the ErrorHandlingService class.
    /// </summary>
    public class ErrorHandlingServiceTests
    {
        private readonly Mock<ILogger<ErrorHandlingService>> _mockLogger;
        private readonly ErrorHandlingService _errorHandlingService;

        public ErrorHandlingServiceTests()
        {
            _mockLogger = new Mock<ILogger<ErrorHandlingService>>();
            _errorHandlingService = new ErrorHandlingService(_mockLogger.Object);
        }

        [Fact]
        public async Task HandleErrorAsync_WithValidErrorInfo_ShouldLogError()
        {
            // Arrange
            var errorInfo = new ErrorInfo
            {
                ErrorCode = "TEST_ERROR",
                UserMessage = "Test error message",
                TechnicalDetails = "Test technical details",
                Severity = ErrorSeverity.Error
            };

            // Act
            await _errorHandlingService.HandleErrorAsync(errorInfo);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TEST_ERROR")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleErrorAsync_WithException_ShouldCreateErrorInfoAndLog()
        {
            // Arrange
            var exception = new InvalidOperationException("Test exception");
            var context = "TestContext";
            var userMessage = "Custom user message";

            // Act
            await _errorHandlingService.HandleErrorAsync(exception, context, userMessage);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("InvalidOperationException")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetRecentErrorsAsync_AfterHandlingErrors_ShouldReturnErrors()
        {
            // Arrange
            var errorInfo1 = new ErrorInfo("ERROR_1", "First error");
            var errorInfo2 = new ErrorInfo("ERROR_2", "Second error");

            // Act
            await _errorHandlingService.HandleErrorAsync(errorInfo1);
            await _errorHandlingService.HandleErrorAsync(errorInfo2);
            var recentErrors = await _errorHandlingService.GetRecentErrorsAsync(10);

            // Assert
            Assert.Equal(2, recentErrors.Count());
            Assert.Contains(recentErrors, e => e.ErrorCode == "ERROR_1");
            Assert.Contains(recentErrors, e => e.ErrorCode == "ERROR_2");
        }

        [Fact]
        public async Task ClearErrorHistoryAsync_ShouldRemoveAllErrors()
        {
            // Arrange
            var errorInfo = new ErrorInfo("ERROR_1", "Test error");
            await _errorHandlingService.HandleErrorAsync(errorInfo);

            // Act
            await _errorHandlingService.ClearErrorHistoryAsync();
            var recentErrors = await _errorHandlingService.GetRecentErrorsAsync(10);

            // Assert
            Assert.Empty(recentErrors);
        }

        [Fact]
        public async Task SetErrorReportingAsync_ShouldUpdateProperty()
        {
            // Arrange
            var initialState = _errorHandlingService.IsErrorReportingEnabled;

            // Act
            await _errorHandlingService.SetErrorReportingAsync(!initialState);

            // Assert
            Assert.Equal(!initialState, _errorHandlingService.IsErrorReportingEnabled);
        }

        [Fact]
        public async Task HandleErrorAsync_WithNullErrorInfo_ShouldThrowArgumentNullException()
        {
            // Arrange
            ErrorInfo? errorInfo = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _errorHandlingService.HandleErrorAsync(errorInfo!));
        }

        [Fact]
        public async Task HandleErrorAsync_WithNullException_ShouldThrowArgumentNullException()
        {
            // Arrange
            Exception? exception = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _errorHandlingService.HandleErrorAsync(exception!));
        }

        [Fact]
        public async Task ErrorOccurred_Event_ShouldBeRaised()
        {
            // Arrange
            var errorInfo = new ErrorInfo("TEST_ERROR", "Test message");
            ErrorInfo? raisedErrorInfo = null;

            _errorHandlingService.ErrorOccurred += (sender, args) => raisedErrorInfo = args;

            // Act
            await _errorHandlingService.HandleErrorAsync(errorInfo);

            // Assert
            Assert.NotNull(raisedErrorInfo);
            Assert.Equal("TEST_ERROR", raisedErrorInfo.ErrorCode);
        }

        [Theory]
        [InlineData(ErrorSeverity.Info, LogLevel.Information)]
        [InlineData(ErrorSeverity.Warning, LogLevel.Warning)]
        [InlineData(ErrorSeverity.Error, LogLevel.Error)]
        [InlineData(ErrorSeverity.Critical, LogLevel.Critical)]
        [InlineData(ErrorSeverity.Fatal, LogLevel.Critical)]
        public async Task LogErrorAsync_WithDifferentSeverities_ShouldUseCorrectLogLevel(
            ErrorSeverity severity, LogLevel expectedLogLevel)
        {
            // Arrange
            var errorInfo = new ErrorInfo
            {
                ErrorCode = "TEST_ERROR",
                UserMessage = "Test message",
                Severity = severity
            };

            // Act
            await _errorHandlingService.LogErrorAsync(errorInfo);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    expectedLogLevel,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
