using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using JarvisAssistant.Services.LLM;
using JarvisAssistant.Core.Models;
using LLMErrorSeverity = JarvisAssistant.Core.Models.ErrorSeverity;

namespace JarvisAssistant.UnitTests.Services.LLM
{
    /// <summary>
    /// Tests for the LLM error handler to ensure all error scenarios are properly categorized and handled.
    /// </summary>
    public class LLMErrorHandlerTests
    {
        private readonly Mock<ILogger<LLMErrorHandler>> _mockLogger;
        private readonly LLMErrorHandler _errorHandler;

        public LLMErrorHandlerTests()
        {
            _mockLogger = new Mock<ILogger<LLMErrorHandler>>();
            _errorHandler = new LLMErrorHandler(_mockLogger.Object);
        }

        #region HTTP Error Tests

        [Theory]
        [InlineData("HTTP 404 Not Found", LLMErrorCodes.HTTP_404_NOT_FOUND, false)]
        [InlineData("The remote server returned an error: (404) Not Found", LLMErrorCodes.HTTP_404_NOT_FOUND, false)]
        [InlineData("404 endpoint not found", LLMErrorCodes.HTTP_404_NOT_FOUND, false)]
        public void ProcessException_HttpRequestException_404_ReturnsCorrectErrorCode(
            string exceptionMessage, string expectedErrorCode, bool expectedRetryable)
        {
            // Arrange
            var exception = new HttpRequestException(exceptionMessage);

            // Act
            var result = _errorHandler.ProcessException(exception, "test context");

            // Assert
            result.ErrorCode.Should().Be(expectedErrorCode);
            result.IsRetryable.Should().Be(expectedRetryable);
            result.Severity.Should().Be(LLMErrorSeverity.Critical);
            result.SuggestedAction.Should().Contain("Ollama");
            result.Context.Should().Be("test context");
        }

        [Theory]
        [InlineData("HTTP 401 Unauthorized", LLMErrorCodes.HTTP_401_UNAUTHORIZED)]
        [InlineData("401 authentication required", LLMErrorCodes.HTTP_401_UNAUTHORIZED)]
        [InlineData("Unauthorized access", LLMErrorCodes.HTTP_401_UNAUTHORIZED)]
        public void ProcessException_HttpRequestException_Authentication_ReturnsCorrectErrorCode(
            string exceptionMessage, string expectedErrorCode)
        {
            // Arrange
            var exception = new HttpRequestException(exceptionMessage);

            // Act
            var result = _errorHandler.ProcessException(exception);

            // Assert
            result.ErrorCode.Should().Be(expectedErrorCode);
            result.IsRetryable.Should().BeFalse();
            result.SuggestedAction.Should().Contain("authentication");
        }

        [Theory]
        [InlineData("HTTP 500 Internal Server Error", LLMErrorCodes.HTTP_500_INTERNAL_ERROR, true)]
        [InlineData("500 server error", LLMErrorCodes.HTTP_500_INTERNAL_ERROR, true)]
        [InlineData("Internal server error occurred", LLMErrorCodes.HTTP_500_INTERNAL_ERROR, true)]
        public void ProcessException_HttpRequestException_ServerError_ReturnsCorrectErrorCode(
            string exceptionMessage, string expectedErrorCode, bool expectedRetryable)
        {
            // Arrange
            var exception = new HttpRequestException(exceptionMessage);

            // Act
            var result = _errorHandler.ProcessException(exception);

            // Assert
            result.ErrorCode.Should().Be(expectedErrorCode);
            result.IsRetryable.Should().Be(expectedRetryable);
            result.Severity.Should().Be(LLMErrorSeverity.Error);
        }

        [Theory]
        [InlineData("HTTP 429 Too Many Requests", LLMErrorCodes.HTTP_429_RATE_LIMITED)]
        [InlineData("429 rate limit exceeded", LLMErrorCodes.HTTP_429_RATE_LIMITED)]
        [InlineData("Too many requests", LLMErrorCodes.HTTP_429_RATE_LIMITED)]
        public void ProcessException_HttpRequestException_RateLimit_ReturnsCorrectErrorCode(
            string exceptionMessage, string expectedErrorCode)
        {
            // Arrange
            var exception = new HttpRequestException(exceptionMessage);

            // Act
            var result = _errorHandler.ProcessException(exception);

            // Assert
            result.ErrorCode.Should().Be(expectedErrorCode);
            result.IsRetryable.Should().BeTrue();
            result.Severity.Should().Be(LLMErrorSeverity.Warning);
            result.SuggestedAction.Should().Contain("Wait before making");
        }

        [Theory]
        [InlineData("Connection refused", LLMErrorCodes.CONN_REFUSED)]
        [InlineData("Connection failed", LLMErrorCodes.CONN_REFUSED)]
        [InlineData("Unable to connect", LLMErrorCodes.CONN_REFUSED)]
        public void ProcessException_HttpRequestException_ConnectionRefused_ReturnsCorrectErrorCode(
            string exceptionMessage, string expectedErrorCode)
        {
            // Arrange
            var exception = new HttpRequestException(exceptionMessage);

            // Act
            var result = _errorHandler.ProcessException(exception);

            // Assert
            result.ErrorCode.Should().Be(expectedErrorCode);
            result.IsRetryable.Should().BeTrue();
            result.SuggestedAction.Should().Contain("ollama serve");
        }

        [Theory]
        [InlineData("SSL handshake failed", LLMErrorCodes.CONN_SSL_FAILURE)]
        [InlineData("TLS connection error", LLMErrorCodes.CONN_SSL_FAILURE)]
        [InlineData("Certificate validation failed", LLMErrorCodes.CONN_SSL_FAILURE)]
        public void ProcessException_HttpRequestException_SslFailure_ReturnsCorrectErrorCode(
            string exceptionMessage, string expectedErrorCode)
        {
            // Arrange
            var exception = new HttpRequestException(exceptionMessage);

            // Act
            var result = _errorHandler.ProcessException(exception);

            // Assert
            result.ErrorCode.Should().Be(expectedErrorCode);
            result.IsRetryable.Should().BeFalse();
            result.SuggestedAction.Should().Contain("SSL/TLS");
        }

        #endregion

        #region Socket Error Tests

        [Theory]
        [InlineData(SocketError.ConnectionRefused, LLMErrorCodes.CONN_REFUSED)]
        [InlineData(SocketError.HostNotFound, LLMErrorCodes.CONN_HOST_NOT_FOUND)]
        [InlineData(SocketError.NetworkUnreachable, LLMErrorCodes.CONN_NETWORK_UNREACHABLE)]
        [InlineData(SocketError.TimedOut, LLMErrorCodes.CONN_TIMEOUT)]
        public void ProcessException_SocketException_ReturnsCorrectErrorCode(
            SocketError socketError, string expectedErrorCode)
        {
            // Arrange
            var exception = new SocketException((int)socketError);

            // Act
            var result = _errorHandler.ProcessException(exception);

            // Assert
            result.ErrorCode.Should().Be(expectedErrorCode);
            result.TechnicalDetails.Should().Contain(socketError.ToString());
        }

        [Fact]
        public void ProcessException_SocketException_ConnectionRefused_HasCorrectSuggestion()
        {
            // Arrange
            var exception = new SocketException((int)SocketError.ConnectionRefused);

            // Act
            var result = _errorHandler.ProcessException(exception);

            // Assert
            result.ErrorCode.Should().Be(LLMErrorCodes.CONN_REFUSED);
            result.IsRetryable.Should().BeTrue();
            result.SuggestedAction.Should().Contain("ollama serve");
            result.Severity.Should().Be(LLMErrorSeverity.Critical);
        }

        [Fact]
        public void ProcessException_SocketException_HostNotFound_HasCorrectSuggestion()
        {
            // Arrange
            var exception = new SocketException((int)SocketError.HostNotFound);

            // Act
            var result = _errorHandler.ProcessException(exception);

            // Assert
            result.ErrorCode.Should().Be(LLMErrorCodes.CONN_HOST_NOT_FOUND);
            result.IsRetryable.Should().BeFalse();
            result.SuggestedAction.Should().Contain("hostname or IP address");
        }

        #endregion

        #region Timeout Error Tests

        [Fact]
        public void ProcessException_TaskCanceledException_WithTimeoutInner_ReturnsTimeoutError()
        {
            // Arrange
            var timeoutException = new TimeoutException("Operation timed out");
            var exception = new TaskCanceledException("Request was cancelled", timeoutException);

            // Act
            var result = _errorHandler.ProcessException(exception);

            // Assert
            result.ErrorCode.Should().Be(LLMErrorCodes.REQ_TIMEOUT);
            result.IsRetryable.Should().BeTrue();
            result.SuggestedAction.Should().Contain("shorter prompt");
        }

        [Fact]
        public void ProcessException_TaskCanceledException_WithoutTimeoutInner_ReturnsCancelledError()
        {
            // Arrange
            var exception = new TaskCanceledException("Request was cancelled");

            // Act
            var result = _errorHandler.ProcessException(exception);

            // Assert
            result.ErrorCode.Should().Be(LLMErrorCodes.REQ_CANCELLED);
            result.IsRetryable.Should().BeTrue();
            result.Severity.Should().Be(ErrorSeverity.Warning);
        }

        [Fact]
        public void ProcessException_TimeoutException_ReturnsTimeoutError()
        {
            // Arrange
            var exception = new TimeoutException("The operation timed out");

            // Act
            var result = _errorHandler.ProcessException(exception);

            // Assert
            result.ErrorCode.Should().Be(LLMErrorCodes.REQ_TIMEOUT);
            result.IsRetryable.Should().BeTrue();
            result.SuggestedAction.Should().Contain("simpler prompt");
        }

        #endregion

        #region JSON Error Tests

        [Fact]
        public void ProcessException_JsonException_ReturnsInvalidJsonError()
        {
            // Arrange
            var exception = new JsonException("Invalid JSON format");

            // Act
            var result = _errorHandler.ProcessException(exception);

            // Assert
            result.ErrorCode.Should().Be(LLMErrorCodes.RESP_INVALID_JSON);
            result.IsRetryable.Should().BeTrue();
            result.SuggestedAction.Should().Contain("server-side issue");
        }

        #endregion

        #region Memory Error Tests

        [Fact]
        public void ProcessException_OutOfMemoryException_ReturnsMemoryError()
        {
            // Arrange
            var exception = new OutOfMemoryException("Insufficient memory");

            // Act
            var result = _errorHandler.ProcessException(exception);

            // Assert
            result.ErrorCode.Should().Be(LLMErrorCodes.RESOURCE_OUT_OF_MEMORY);
            result.IsRetryable.Should().BeFalse();
            result.Severity.Should().Be(ErrorSeverity.Critical);
            result.SuggestedAction.Should().Contain("shorter prompt");
        }

        #endregion

        #region Operation Error Tests

        [Fact]
        public void ProcessException_InvalidOperationException_ModelNotAvailable_ReturnsModelError()
        {
            // Arrange
            var exception = new InvalidOperationException("Model not available");

            // Act
            var result = _errorHandler.ProcessException(exception);

            // Assert
            result.ErrorCode.Should().Be(LLMErrorCodes.MODEL_NOT_FOUND);
            result.IsRetryable.Should().BeFalse();
            result.SuggestedAction.Should().Contain("ollama pull");
        }

        [Fact]
        public void ProcessException_InvalidOperationException_RetryExceeded_ReturnsRetryError()
        {
            // Arrange
            var exception = new InvalidOperationException("Maximum retry attempts exceeded");

            // Act
            var result = _errorHandler.ProcessException(exception);

            // Assert
            result.ErrorCode.Should().Be(LLMErrorCodes.RETRY_MAX_ATTEMPTS);
            result.IsRetryable.Should().BeFalse();
            result.SuggestedAction.Should().Contain("service availability");
        }

        [Fact]
        public void ProcessException_ArgumentException_ReturnsConfigError()
        {
            // Arrange
            var exception = new ArgumentException("Invalid argument", "paramName");

            // Act
            var result = _errorHandler.ProcessException(exception);

            // Assert
            result.ErrorCode.Should().Be(LLMErrorCodes.CONFIG_MISSING_PARAMS);
            result.IsRetryable.Should().BeFalse();
            result.SuggestedAction.Should().Contain("parameters and configuration");
        }

        #endregion

        #region Unknown Error Tests

        [Fact]
        public void ProcessException_UnknownException_ReturnsGenericError()
        {
            // Arrange
            var exception = new NotImplementedException("This feature is not implemented");

            // Act
            var result = _errorHandler.ProcessException(exception);

            // Assert
            result.ErrorCode.Should().Be("LLM-UNKNOWN-001");
            result.IsRetryable.Should().BeTrue();
            result.UserMessage.Should().Contain("unexpected error");
        }

        #endregion

        #region Context and Logging Tests

        [Fact]
        public void ProcessException_WithContext_IncludesContextInResponse()
        {
            // Arrange
            var exception = new HttpRequestException("Connection failed");
            var context = "Testing streaming endpoint";

            // Act
            var result = _errorHandler.ProcessException(exception, context);

            // Assert
            result.Context.Should().Be(context);
            result.TechnicalDetails.Should().Contain(context);
        }

        [Fact]
        public void ProcessException_LogsErrorWithCorrectLevel()
        {
            // Arrange
            var exception = new HttpRequestException("404 Not Found");

            // Act
            _errorHandler.ProcessException(exception);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Critical, // High severity = Critical
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("LLM Error")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void ProcessException_LowSeverityError_LogsAsWarning()
        {
            // Arrange
            var exception = new HttpRequestException("429 Too Many Requests");

            // Act
            _errorHandler.ProcessException(exception);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning, // Low severity = Warning
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("LLM Error")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region ChatResponse Conversion Tests

        [Fact]
        public void LLMErrorResponse_ToChatResponse_ReturnsCorrectChatResponse()
        {
            // Arrange
            var errorResponse = new LLMErrorResponse
            {
                ErrorCode = LLMErrorCodes.HTTP_404_NOT_FOUND,
                UserMessage = "Service not found",
                TechnicalDetails = "HTTP 404 error",
                Severity = LLMErrorSeverity.Critical,
                IsRetryable = false,
                SuggestedAction = "Start Ollama service"
            };

            var conversationId = "test-conv-123";

            // Act
            var chatResponse = errorResponse.ToChatResponse(conversationId);

            // Assert
            chatResponse.Type.Should().Be("error");
            chatResponse.Message.Should().Be("Service not found");
            chatResponse.IsComplete.Should().BeTrue();
            chatResponse.Metadata.Should().ContainKey("errorCode");
            chatResponse.Metadata.Should().ContainKey("severity");
            chatResponse.Metadata.Should().ContainKey("isRetryable");
            chatResponse.Metadata.Should().ContainKey("conversationId");
            chatResponse.Metadata["conversationId"].Should().Be(conversationId);
        }

        #endregion

        #region Error Message Template Tests

        [Theory]
        [InlineData(LLMErrorCodes.HTTP_404_NOT_FOUND)]
        [InlineData(LLMErrorCodes.HTTP_500_INTERNAL_ERROR)]
        [InlineData(LLMErrorCodes.CONN_REFUSED)]
        [InlineData(LLMErrorCodes.REQ_TIMEOUT)]
        [InlineData(LLMErrorCodes.MODEL_NOT_FOUND)]
        public void LLMErrorMessages_GetErrorMessage_ReturnsNonEmptyMessage(string errorCode)
        {
            // Act
            var message = LLMErrorMessages.GetErrorMessage(errorCode);

            // Assert
            message.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void LLMErrorMessages_GetErrorMessage_WithAdditionalInfo_IncludesInfo()
        {
            // Arrange
            var errorCode = LLMErrorCodes.HTTP_404_NOT_FOUND;
            var additionalInfo = "Port 11434 is not accessible";

            // Act
            var message = LLMErrorMessages.GetErrorMessage(errorCode, additionalInfo);

            // Assert
            message.Should().Contain(additionalInfo);
            message.Should().Contain("Additional details:");
        }

        [Fact]
        public void LLMErrorMessages_GetErrorMessage_UnknownErrorCode_ReturnsDefaultMessage()
        {
            // Arrange
            var unknownErrorCode = "UNKNOWN-ERROR-999";

            // Act
            var message = LLMErrorMessages.GetErrorMessage(unknownErrorCode);

            // Assert
            message.Should().Contain("unexpected error");
        }

        #endregion

        #region Comprehensive Error Scenario Tests

        [Fact]
        public void ProcessException_CompleteErrorScenario_ReturnsDetailedResponse()
        {
            // Arrange
            var exception = new HttpRequestException("HTTP 503 Service Unavailable: Server is temporarily overloaded");
            var context = "Attempting to generate response for user query";

            // Act
            var result = _errorHandler.ProcessException(exception, context);

            // Assert
            result.ErrorCode.Should().Be(LLMErrorCodes.HTTP_503_SERVICE_UNAVAILABLE);
            result.UserMessage.Should().NotBeNullOrEmpty();
            result.TechnicalDetails.Should().Contain("503");
            result.Context.Should().Be(context);
            result.Severity.Should().Be(LLMErrorSeverity.Error);
            result.IsRetryable.Should().BeTrue();
            result.SuggestedAction.Should().NotBeNullOrEmpty();
            result.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        }

        #endregion
    }
}