using Microsoft.Extensions.Logging;
using Moq;
using JarvisAssistant.Services;
using JarvisAssistant.Core.Models;
using JarvisAssistant.Core.ErrorCodes;
using Xunit;
using JarvisAssistant.Core.Interfaces;

namespace JarvisAssistant.UnitTests.Services
{
    /// <summary>
    /// Comprehensive unit tests for the EnhancedErrorHandlingService.
    /// Validates error handling, retry mechanisms, and frequency tracking.
    /// </summary>
    public class EnhancedErrorHandlingServiceTests
    {
        private readonly Mock<ILogger<EnhancedErrorHandlingService>> _mockLogger;
        private readonly EnhancedErrorHandlingService _service;

        public EnhancedErrorHandlingServiceTests()
        {
            _mockLogger = new Mock<ILogger<EnhancedErrorHandlingService>>();
            _service = new EnhancedErrorHandlingService(_mockLogger.Object);
        }

        [Fact]
        public async Task HandleErrorAsync_WithValidErrorInfo_LogsAndProcessesError()
        {
            // Arrange
            var errorInfo = new ErrorInfo
            {
                ErrorCode = ErrorCodeRegistry.LLM_CONN_001,
                UserMessage = "Test error",
                Severity = ErrorSeverity.Error,
                Timestamp = DateTimeOffset.UtcNow
            };

            // Act
            await _service.HandleErrorAsync(errorInfo);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(errorInfo.ErrorCode)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleErrorAsync_WithException_CreatesErrorInfoAndHandles()
        {
            // Arrange
            var exception = new InvalidOperationException("Test exception");
            var context = "TestContext";
            var userMessage = "Custom user message";

            // Act
            await _service.HandleErrorAsync(exception, context, userMessage);

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
        public async Task ExecuteWithRetryAsync_SuccessfulOperation_ReturnsResult()
        {
            // Arrange
            var expectedResult = "Success";
            var operation = new Func<Task<string>>(() => Task.FromResult(expectedResult));

            // Act
            var result = await _service.ExecuteWithRetryAsync(operation, maxRetries: 3);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_FailingOperation_RetriesAndThrows()
        {
            // Arrange
            var attempts = 0;
            var operation = new Func<Task<string>>(() =>
            {
                attempts++;
                throw new InvalidOperationException("Always fails");
            });

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ExecuteWithRetryAsync(operation, maxRetries: 2));
            
            Assert.Equal(3, attempts); // Initial attempt + 2 retries
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_SucceedsOnRetry_ReturnsResult()
        {
            // Arrange
            var attempts = 0;
            var expectedResult = "Success after retry";
            var operation = new Func<Task<string>>(() =>
            {
                attempts++;
                if (attempts < 3)
                    throw new InvalidOperationException("Fails first two times");
                return Task.FromResult(expectedResult);
            });

            // Act
            var result = await _service.ExecuteWithRetryAsync(operation, maxRetries: 3);

            // Assert
            Assert.Equal(expectedResult, result);
            Assert.Equal(3, attempts);
        }

        [Theory]
        [InlineData(typeof(ArgumentNullException), ErrorSeverity.Error)]
        [InlineData(typeof(InvalidOperationException), ErrorSeverity.Error)]
        [InlineData(typeof(TimeoutException), ErrorSeverity.Warning)]
        [InlineData(typeof(UnauthorizedAccessException), ErrorSeverity.Error)]
        public async Task HandleErrorAsync_DifferentExceptionTypes_MapsSeverityCorrectly(Type exceptionType, ErrorSeverity expectedSeverity)
        {
            // Arrange
            var exception = (Exception)Activator.CreateInstance(exceptionType, "Test exception")!;

            // Act
            await _service.HandleErrorAsync(exception, "TestContext");

            // Get the recent errors to verify severity mapping
            var recentErrors = await _service.GetRecentErrorsAsync(1);
            var errorInfo = recentErrors.First();

            // Assert
            Assert.Equal(expectedSeverity, errorInfo.Severity);
        }

        [Fact]
        public void IsTransientError_TransientExceptions_ReturnsTrue()
        {
            // Arrange
            var transientExceptions = new Exception[]
            {
                new TimeoutException(),
                new HttpRequestException(),
                new TaskCanceledException(),
                new OperationCanceledException()
            };

            // Act & Assert
            foreach (var exception in transientExceptions)
            {
                var result = _service.IsTransientError(exception);
                Assert.True(result, $"{exception.GetType().Name} should be considered transient");
            }
        }

        [Fact]
        public void IsTransientError_NonTransientExceptions_ReturnsFalse()
        {
            // Arrange
            var nonTransientExceptions = new Exception[]
            {
                new ArgumentNullException(),
                new InvalidOperationException(),
                new NotSupportedException(),
                new FormatException()
            };

            // Act & Assert
            foreach (var exception in nonTransientExceptions)
            {
                var result = _service.IsTransientError(exception);
                Assert.False(result, $"{exception.GetType().Name} should not be considered transient");
            }
        }

        [Fact]
        public async Task GetErrorFrequency_TracksErrorCounts()
        {
            // Arrange
            var errorCode = ErrorCodeRegistry.LLM_CONN_001;
            var errorInfo = new ErrorInfo
            {
                ErrorCode = errorCode,
                UserMessage = "Test error",
                Severity = ErrorSeverity.Error,
                Timestamp = DateTimeOffset.UtcNow
            };

            // Act
            await _service.HandleErrorAsync(errorInfo);
            await _service.HandleErrorAsync(errorInfo);
            await _service.HandleErrorAsync(errorInfo);

            var frequency = _service.GetErrorFrequency(errorCode, TimeSpan.FromMinutes(5));

            // Assert
            Assert.Equal(3, frequency);
        }

        [Fact]
        public async Task GetErrorFrequency_WithTimeWindow_ReturnsCorrectCount()
        {
            // Arrange
            var errorCode = ErrorCodeRegistry.NET_CONN_001;
            var oldErrorInfo = new ErrorInfo
            {
                ErrorCode = errorCode,
                UserMessage = "Old error",
                Severity = ErrorSeverity.Warning,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-10) // Outside 5-minute window
            };
            var recentErrorInfo = new ErrorInfo
            {
                ErrorCode = errorCode,
                UserMessage = "Recent error",
                Severity = ErrorSeverity.Warning,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-2) // Within 5-minute window
            };

            // Act
            await _service.HandleErrorAsync(oldErrorInfo);
            await _service.HandleErrorAsync(recentErrorInfo);

            var frequency = _service.GetErrorFrequency(errorCode, TimeSpan.FromMinutes(5));

            // Assert
            Assert.Equal(1, frequency); // Only the recent error should be counted
        }

        [Fact]
        public void Dispose_DoesNotThrow()
        {
            // Act & Assert
            var exception = Record.Exception(() => _service.Dispose());
            Assert.Null(exception);
        }

        [Fact]
        public async Task HandleErrorAsync_NullErrorInfo_DoesNotThrow()
        {
            // Act & Assert
            var exception = await Record.ExceptionAsync(() => _service.HandleErrorAsync((ErrorInfo)null!));
            Assert.Null(exception);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_ZeroRetries_ExecutesOnce()
        {
            // Arrange
            var attempts = 0;
            var operation = new Func<Task<string>>(() =>
            {
                attempts++;
                throw new InvalidOperationException("Always fails");
            });

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ExecuteWithRetryAsync(operation, maxRetries: 0));
            
            Assert.Equal(1, attempts); // Only initial attempt
        }

        [Fact]
        public async Task HandleErrorAsync_WithInnerException_CapturesDetails()
        {
            // Arrange
            var innerException = new ArgumentNullException("param", "Parameter was null");
            var outerException = new InvalidOperationException("Operation failed", innerException);

            // Act
            await _service.HandleErrorAsync(outerException, "TestContext");

            // Get the recent errors to verify details were captured
            var recentErrors = await _service.GetRecentErrorsAsync(1);
            var errorInfo = recentErrors.First();

            // Assert
            Assert.NotNull(errorInfo);
            Assert.Contains("Operation failed", errorInfo.UserMessage ?? errorInfo.TechnicalDetails ?? "");
            Assert.Contains("Parameter was null", errorInfo.TechnicalDetails ?? "");
        }

        [Fact]
        public async Task GetRecentErrorsAsync_AfterHandlingErrors_ShouldReturnErrors()
        {
            // Arrange
            var errorInfo1 = new ErrorInfo("ERROR_1", "First error");
            var errorInfo2 = new ErrorInfo("ERROR_2", "Second error");

            // Act
            await _service.HandleErrorAsync(errorInfo1);
            await _service.HandleErrorAsync(errorInfo2);
            var recentErrors = await _service.GetRecentErrorsAsync(10);

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
            await _service.HandleErrorAsync(errorInfo);

            // Act
            await _service.ClearErrorHistoryAsync();
            var recentErrors = await _service.GetRecentErrorsAsync(10);

            // Assert
            Assert.Empty(recentErrors);
        }

        [Fact]
        public async Task SetErrorReportingAsync_ShouldUpdateProperty()
        {
            // Arrange
            var initialState = _service.IsErrorReportingEnabled;

            // Act
            await _service.SetErrorReportingAsync(!initialState);

            // Assert
            Assert.Equal(!initialState, _service.IsErrorReportingEnabled);
        }

        [Fact]
        public async Task ErrorOccurred_Event_ShouldBeRaised()
        {
            // Arrange
            var errorInfo = new ErrorInfo("TEST_ERROR", "Test message");
            ErrorInfo? raisedErrorInfo = null;

            _service.ErrorOccurred += (sender, args) => raisedErrorInfo = args;

            // Act
            await _service.HandleErrorAsync(errorInfo);

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
            await _service.LogErrorAsync(errorInfo);

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
