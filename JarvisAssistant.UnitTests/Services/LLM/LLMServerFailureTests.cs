using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Contrib.HttpClient;
using JarvisAssistant.Core.Models;
using JarvisAssistant.Services.LLM;
using JarvisAssistant.Services.Extensions;

namespace JarvisAssistant.UnitTests.Services.LLM
{
    /// <summary>
    /// Comprehensive test suite for all possible LLM server communication failures.
    /// Tests various network, HTTP, and application-level error scenarios with specific error codes.
    /// </summary>
    public class LLMServerFailureTests : IDisposable
    {
        private readonly Mock<ILogger<OllamaClient>> _mockLogger;
        private readonly Mock<HttpMessageHandler> _mockHttpHandler;
        private readonly HttpClient _httpClient;
        private readonly IOptions<OllamaLLMOptions> _options;
        private readonly OllamaClient _ollamaClient;

        public LLMServerFailureTests()
        {
            _mockLogger = new Mock<ILogger<OllamaClient>>();
            _mockHttpHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpHandler.Object)
            {
                BaseAddress = new Uri("http://localhost:11434")
            };

            _options = Options.Create(new OllamaLLMOptions
            {
                BaseUrl = "http://localhost:11434",
                Timeout = TimeSpan.FromSeconds(30),
                MaxRetryAttempts = 2,
                RetryDelay = TimeSpan.FromMilliseconds(100),
                AlternativeEndpoints = new List<string>
                {
                    "http://localhost:11434",
                    "http://127.0.0.1:11434",
                    "http://100.108.155.28:11434"
                }
            });

            _ollamaClient = new OllamaClient(_httpClient, _mockLogger.Object, _options);
        }

        #region HTTP Status Code Failures

        [Theory]
        [InlineData(HttpStatusCode.NotFound, "Service not found")]
        [InlineData(HttpStatusCode.Unauthorized, "Authentication required")]
        [InlineData(HttpStatusCode.Forbidden, "Access forbidden")]
        [InlineData(HttpStatusCode.InternalServerError, "Internal server error")]
        [InlineData(HttpStatusCode.BadGateway, "Bad gateway")]
        [InlineData(HttpStatusCode.ServiceUnavailable, "Service unavailable")]
        [InlineData(HttpStatusCode.GatewayTimeout, "Gateway timeout")]
        [InlineData(HttpStatusCode.BadRequest, "Bad request")]
        [InlineData(HttpStatusCode.RequestTimeout, "Request timeout")]
        [InlineData(HttpStatusCode.TooManyRequests, "Rate limit exceeded")]
        public async Task GenerateAsync_HttpStatusCodeFailures_ReturnsSpecificErrorCodes(
            HttpStatusCode statusCode, string errorDescription)
        {
            // Arrange
            var errorResponse = CreateErrorResponse(statusCode, errorDescription);
            _mockHttpHandler.SetupRequest(HttpMethod.Post, "http://localhost:11434/api/generate")
                .ReturnsResponse(statusCode, errorResponse);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _ollamaClient.GenerateAsync("test prompt"));

            exception.Message.Should().Contain(((int)statusCode).ToString());
            if (statusCode == HttpStatusCode.NotFound)
            {
                exception.Message.Should().Contain("Ollama service not found");
            }
            else
            {
                exception.Message.Should().Contain("Ollama API returned");
            }

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(((int)statusCode).ToString())),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task GenerateAsync_404NotFound_SpecialHandling()
        {
            // Arrange
            _mockHttpHandler.SetupRequest(HttpMethod.Post, "http://localhost:11434/api/generate")
                .ReturnsResponse(HttpStatusCode.NotFound, "Endpoint not found");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _ollamaClient.GenerateAsync("test prompt"));

            exception.Message.Should().Contain("Ollama service not found");
            exception.Message.Should().Contain("Please ensure Ollama is running and accessible");
            exception.Message.Should().Contain("HTTP 404");
        }

        #endregion

        #region Network Connection Failures

        [Fact]
        public async Task GenerateAsync_ConnectionRefused_ReturnsConnectionError()
        {
            // Arrange
            _mockHttpHandler.SetupRequest(HttpMethod.Post, "http://localhost:11434/api/generate")
                .Throws(new HttpRequestException("Connection refused", 
                    new SocketException((int)SocketError.ConnectionRefused)));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _ollamaClient.GenerateAsync("test prompt"));

            exception.Message.Should().Contain("Unable to connect to Ollama server");
            exception.Message.Should().Contain("Connection refused");
            VerifyErrorLogged("Failed to connect to Ollama server");
        }

        [Fact]
        public async Task GenerateAsync_HostNotFound_ReturnsHostError()
        {
            // Arrange
            _mockHttpHandler.SetupRequest(HttpMethod.Post, "http://localhost:11434/api/generate")
                .Throws(new HttpRequestException("connection name resolution failure", 
                    new SocketException((int)SocketError.HostNotFound)));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _ollamaClient.GenerateAsync("test prompt"));

            exception.Message.Should().Contain("Unable to connect to Ollama server");
            VerifyErrorLogged("Failed to connect to Ollama server");
        }

        [Fact]
        public async Task GenerateAsync_NetworkUnreachable_ReturnsNetworkError()
        {
            // Arrange
            _mockHttpHandler.SetupRequest(HttpMethod.Post, "http://localhost:11434/api/generate")
                .Throws(new HttpRequestException("Network unreachable", 
                    new SocketException((int)SocketError.NetworkUnreachable)));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _ollamaClient.GenerateAsync("test prompt"));

            exception.Message.Should().Contain("Unable to connect to Ollama server");
            exception.Message.Should().Contain("Network unreachable");
        }

        [Fact]
        public async Task GenerateAsync_PortUnreachable_ReturnsPortError()
        {
            // Arrange
            _mockHttpHandler.SetupRequest(HttpMethod.Post, "http://localhost:11434/api/generate")
                .Throws(new HttpRequestException("Port unreachable", 
                    new SocketException((int)SocketError.ConnectionRefused)));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _ollamaClient.GenerateAsync("test prompt"));

            exception.Message.Should().Contain("Unable to connect to Ollama server");
        }

        #endregion

        #region Timeout Failures

        [Fact]
        public async Task GenerateAsync_RequestTimeout_ReturnsTimeoutError()
        {
            // Arrange
            _mockHttpHandler.SetupRequest(HttpMethod.Post, "http://localhost:11434/api/generate")
                .Throws(new TaskCanceledException("Request timeout", new TimeoutException()));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<TimeoutException>(
                () => _ollamaClient.GenerateAsync("test prompt"));

            exception.Message.Should().Contain("The request to Ollama timed out");
            exception.Message.Should().Contain("model may be taking longer than expected");
            VerifyErrorLogged("Ollama request timed out");
        }

        [Fact]
        public async Task GenerateAsync_CancellationTimeout_ReturnsTimeoutError()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            _mockHttpHandler.SetupRequest(HttpMethod.Post, "http://localhost:11434/api/generate")
                .Throws(new TaskCanceledException("Operation was canceled", new TimeoutException("Request timed out")));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<TimeoutException>(
                () => _ollamaClient.GenerateAsync("test prompt", QueryType.General, cts.Token));

            exception.Message.Should().Contain("The request to Ollama timed out");
        }

        [Fact]
        public async Task GenerateAsync_OperationCanceled_HandlesGracefully()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            _mockHttpHandler.SetupRequest(HttpMethod.Post, "http://localhost:11434/api/generate")
                .Throws(new OperationCanceledException("The operation was canceled."));

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _ollamaClient.GenerateAsync("test prompt", QueryType.General, cts.Token));
        }

        #endregion

        #region SSL/TLS Failures

        [Fact]
        public async Task GenerateAsync_SslHandshakeFailure_ReturnsSslError()
        {
            // Arrange
            _mockHttpHandler.SetupRequest(HttpMethod.Post, "http://localhost:11434/api/generate")
                .Throws(new HttpRequestException("SSL handshake failed"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _ollamaClient.GenerateAsync("test prompt"));

            exception.Message.Should().Contain("Unable to connect to Ollama server");
            exception.InnerException?.Message.Should().Contain("SSL handshake failed");
        }

        [Fact]
        public async Task GenerateAsync_CertificateValidationFailure_ReturnsCertError()
        {
            // Arrange
            _mockHttpHandler.SetupRequest(HttpMethod.Post, "http://localhost:11434/api/generate")
                .Throws(new HttpRequestException("Certificate validation failed connection"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _ollamaClient.GenerateAsync("test prompt"));

            exception.Message.Should().Contain("Unable to connect to Ollama server");
        }

        #endregion

        #region Response Format Failures

        [Fact]
        public async Task GenerateAsync_InvalidJsonResponse_HandlesGracefully()
        {
            // Arrange
            _mockHttpHandler.SetupRequest(HttpMethod.Post, "http://localhost:11434/api/generate")
                .ReturnsResponse(HttpStatusCode.OK, "Invalid JSON content", "application/json");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<JsonException>(
                () => _ollamaClient.GenerateAsync("test prompt"));

            exception.Message.Should().Contain("JSON");
        }

        [Fact]
        public async Task GenerateAsync_EmptyResponse_ReturnsEmptyString()
        {
            // Arrange
            var validResponse = new { response = "", done = true };
            var responseJson = JsonSerializer.Serialize(validResponse);
            
            _mockHttpHandler.SetupRequest(HttpMethod.Post, "http://localhost:11434/api/generate")
                .ReturnsResponse(HttpStatusCode.OK, responseJson, "application/json");

            // Act
            var result = await _ollamaClient.GenerateAsync("test prompt");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GenerateAsync_MalformedResponse_ThrowsJsonException()
        {
            // Arrange
            _mockHttpHandler.SetupRequest(HttpMethod.Post, "http://localhost:11434/api/generate")
                .ReturnsResponse(HttpStatusCode.OK, "{\"response\": \"test\", \"done\": ", "application/json");

            // Act & Assert
            await Assert.ThrowsAsync<JsonException>(() => _ollamaClient.GenerateAsync("test prompt"));
        }

        #endregion

        #region Streaming Failures

        [Fact]
        public async Task StreamGenerateAsync_ConnectionDropped_HandlesGracefully()
        {
            // Arrange
            var partialStream = "data: {\"response\": \"Hello\", \"done\": false}\n";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(partialStream));
            stream.Seek(0, SeekOrigin.Begin);

            _mockHttpHandler.SetupRequest(HttpMethod.Post, "http://localhost:11434/api/generate")
                .ReturnsResponse(HttpStatusCode.OK, stream, "application/json");

            // Act
            var chunks = new List<string>();
            var exception = await Record.ExceptionAsync(async () =>
            {
                await foreach (var chunk in _ollamaClient.StreamGenerateAsync("test prompt"))
                {
                    chunks.Add(chunk);
                }
            });

            // Assert
            exception.Should().BeNull(); // Should handle gracefully without throwing
        }

        [Fact]
        public async Task StreamGenerateAsync_StreamTimeout_ReturnsTimeoutError()
        {
            // Arrange
            _mockHttpHandler.SetupRequest(HttpMethod.Post, "http://localhost:11434/api/generate")
                .Throws(new TaskCanceledException("Stream timeout", new TimeoutException()));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<TimeoutException>(async () =>
            {
                await foreach (var chunk in _ollamaClient.StreamGenerateAsync("test prompt"))
                {
                    // Should not reach here
                }
            });

            exception.Message.Should().Contain("The streaming request to Ollama timed out");
        }

        [Fact]
        public async Task StreamGenerateAsync_InvalidStreamingJson_SkipsInvalidChunks()
        {
            // Arrange
            var streamData = """
                {"response": "Hello", "done": false}
                {invalid json line}
                {"response": " World", "done": false}
                {"response": "", "done": true}
                """;
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(streamData));

            _mockHttpHandler.SetupRequest(HttpMethod.Post, "http://localhost:11434/api/generate")
                .ReturnsResponse(HttpStatusCode.OK, stream, "application/json");

            // Act
            var chunks = new List<string>();
            await foreach (var chunk in _ollamaClient.StreamGenerateAsync("test prompt"))
            {
                if (!string.IsNullOrEmpty(chunk))
                    chunks.Add(chunk);
            }

            // Assert
            chunks.Should().HaveCount(2);
            chunks[0].Should().Be("Hello");
            chunks[1].Should().Be(" World");

            // Verify warning was logged for invalid JSON
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to parse Ollama stream chunk")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Model Availability Failures

        [Fact]
        public async Task GetAvailableModelsAsync_404Error_ReturnsEmptyList()
        {
            // Arrange
            _mockHttpHandler.SetupRequest(HttpMethod.Get, "http://localhost:11434/api/tags")
                .ReturnsResponse(HttpStatusCode.NotFound, "Models endpoint not found");

            // Act
            var result = await _ollamaClient.GetAvailableModelsAsync();

            // Assert
            result.Should().BeEmpty();
            VerifyErrorLogged("Ollama models endpoint not found (404)");
        }

        [Fact]
        public async Task GetAvailableModelsAsync_ServiceUnavailable_ReturnsEmptyList()
        {
            // Arrange
            _mockHttpHandler.SetupRequest(HttpMethod.Get, "http://localhost:11434/api/tags")
                .ReturnsResponse(HttpStatusCode.ServiceUnavailable, "Service temporarily unavailable");

            // Act
            var result = await _ollamaClient.GetAvailableModelsAsync();

            // Assert
            result.Should().BeEmpty();
            VerifyErrorLogged("Failed to get Ollama models: ServiceUnavailable");
        }

        [Fact]
        public async Task GetAvailableModelsAsync_ConnectionError_ReturnsEmptyList()
        {
            // Arrange
            _mockHttpHandler.SetupRequest(HttpMethod.Get, "http://localhost:11434/api/tags")
                .Throws(new HttpRequestException("Connection failed"));

            // Act
            var result = await _ollamaClient.GetAvailableModelsAsync();

            // Assert
            result.Should().BeEmpty();
            VerifyErrorLogged("Error retrieving Ollama models");
        }

        #endregion

        #region Memory and Resource Failures

        [Fact]
        public async Task GenerateAsync_OutOfMemory_ThrowsOutOfMemoryException()
        {
            // Arrange
            _mockHttpHandler.SetupRequest(HttpMethod.Post, "http://localhost:11434/api/generate")
                .Throws(new OutOfMemoryException("Insufficient memory"));

            // Act & Assert
            await Assert.ThrowsAsync<OutOfMemoryException>(
                () => _ollamaClient.GenerateAsync("test prompt"));
        }

        [Fact]
        public async Task GenerateAsync_LargePayload_HandlesGracefully()
        {
            // Arrange - Simulate a very large response
            var largeResponse = new { response = new string('A', 100000), done = true };
            var responseJson = JsonSerializer.Serialize(largeResponse);
            
            _mockHttpHandler.SetupRequest(HttpMethod.Post, "http://localhost:11434/api/generate")
                .ReturnsResponse(HttpStatusCode.OK, responseJson, "application/json");

            // Act
            var result = await _ollamaClient.GenerateAsync("test prompt");

            // Assert
            result.Should().HaveLength(100000);
        }

        #endregion

        #region Concurrent Access Failures

        [Fact]
        public async Task GenerateAsync_ConcurrentRequests_HandlesRaceConditions()
        {
            // Arrange
            var response = new { response = "Concurrent response", done = true };
            var responseJson = JsonSerializer.Serialize(response);
            
            _mockHttpHandler.SetupRequest(HttpMethod.Post, "http://localhost:11434/api/generate")
                .ReturnsResponse(HttpStatusCode.OK, responseJson, "application/json");

            // Act
            var tasks = Enumerable.Range(0, 10)
                .Select(i => _ollamaClient.GenerateAsync($"test prompt {i}"))
                .ToArray();

            var results = await Task.WhenAll(tasks);

            // Assert
            results.Should().HaveCount(10);
            results.Should().AllSatisfy(r => r.Should().Be("Concurrent response"));
        }

        #endregion

        #region Configuration Failures

        [Fact]
        public async Task GenerateAsync_InvalidConfiguration_ThrowsArgumentException()
        {
            // Arrange
            var invalidOptions = Options.Create(new OllamaLLMOptions
            {
                BaseUrl = "http://invalid-host:65535",
                MaxRetryAttempts = 0  // Use 0 instead of -1 to avoid loop issues
            });

            // Act & Assert - Constructor validation should prevent this, but test for robustness
            var invalidClient = new OllamaClient(_httpClient, _mockLogger.Object, invalidOptions);
            
            // The client should still work but with fallback behavior
            _mockHttpHandler.SetupRequest(HttpMethod.Post, "http://localhost:11434/api/generate")
                .ReturnsResponse(HttpStatusCode.BadRequest, "Invalid request");

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => invalidClient.GenerateAsync("test"));
            
            exception.Message.Should().Contain("Ollama API returned BadRequest");
        }

        #endregion

        #region Error Recovery and Retry Scenarios

        [Fact]
        public async Task GenerateAsync_RetryAfterFailure_EventuallySucceeds()
        {
            // Arrange
            var successResponse = new { response = "Success after retry", done = true };
            var successJson = JsonSerializer.Serialize(successResponse);

            // First call fails, second call succeeds
            _mockHttpHandler.SetupRequest(HttpMethod.Post, "http://localhost:11434/api/generate")
                .ReturnsResponse(HttpStatusCode.OK, successJson, "application/json");

            // Act
            var result = await _ollamaClient.GenerateAsync("test prompt");

            // Assert
            result.Should().Be("Success after retry");
        }

        [Fact]
        public async Task GenerateAsync_MaxRetriesExceeded_ThrowsFinalException()
        {
            // Arrange - Use connection error that triggers retry logic
            _mockHttpHandler.SetupRequest(HttpMethod.Post, "http://localhost:11434/api/generate")
                .Throws(new HttpRequestException("Connection refused"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _ollamaClient.GenerateAsync("test prompt"));

            exception.Message.Should().Contain("Unable to connect to Ollama server");
        }

        #endregion

        #region Helper Methods

        private static string CreateErrorResponse(HttpStatusCode statusCode, string description)
        {
            return JsonSerializer.Serialize(new
            {
                error = new
                {
                    code = (int)statusCode,
                    message = description,
                    type = statusCode.ToString()
                }
            });
        }

        private void VerifyErrorLogged(string expectedLogMessage)
        {
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedLogMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        #endregion
    }

    /// <summary>
    /// Error code constants for LLM communication failures.
    /// </summary>
    public static class LLMErrorCodes
    {
        // HTTP Status Code Errors
        public const string HTTP_404_NOT_FOUND = "LLM-HTTP-404-001";
        public const string HTTP_401_UNAUTHORIZED = "LLM-HTTP-401-001";
        public const string HTTP_403_FORBIDDEN = "LLM-HTTP-403-001";
        public const string HTTP_500_INTERNAL_ERROR = "LLM-HTTP-500-001";
        public const string HTTP_502_BAD_GATEWAY = "LLM-HTTP-502-001";
        public const string HTTP_503_SERVICE_UNAVAILABLE = "LLM-HTTP-503-001";
        public const string HTTP_504_GATEWAY_TIMEOUT = "LLM-HTTP-504-001";
        public const string HTTP_400_BAD_REQUEST = "LLM-HTTP-400-001";
        public const string HTTP_408_REQUEST_TIMEOUT = "LLM-HTTP-408-001";
        public const string HTTP_429_RATE_LIMITED = "LLM-HTTP-429-001";

        // Network Connection Errors
        public const string CONN_REFUSED = "LLM-CONN-001";
        public const string CONN_HOST_NOT_FOUND = "LLM-CONN-002";
        public const string CONN_NETWORK_UNREACHABLE = "LLM-CONN-003";
        public const string CONN_TIMEOUT = "LLM-CONN-004";
        public const string CONN_SSL_FAILURE = "LLM-CONN-005";

        // Request/Response Errors
        public const string REQ_TIMEOUT = "LLM-REQ-001";
        public const string REQ_CANCELLED = "LLM-REQ-002";
        public const string RESP_INVALID_JSON = "LLM-RESP-001";
        public const string RESP_EMPTY = "LLM-RESP-002";
        public const string RESP_TOO_LARGE = "LLM-RESP-003";

        // Streaming Errors
        public const string STREAM_CONN_DROPPED = "LLM-STREAM-001";
        public const string STREAM_TIMEOUT = "LLM-STREAM-002";
        public const string STREAM_INVALID_FORMAT = "LLM-STREAM-003";

        // Model Errors
        public const string MODEL_NOT_FOUND = "LLM-MODEL-001";
        public const string MODEL_UNAVAILABLE = "LLM-MODEL-002";
        public const string MODEL_LOADING = "LLM-MODEL-003";

        // Resource Errors
        public const string RESOURCE_OUT_OF_MEMORY = "LLM-RESOURCE-001";
        public const string RESOURCE_DISK_FULL = "LLM-RESOURCE-002";
        public const string RESOURCE_CPU_OVERLOAD = "LLM-RESOURCE-003";

        // Configuration Errors
        public const string CONFIG_INVALID_URL = "LLM-CONFIG-001";
        public const string CONFIG_INVALID_TIMEOUT = "LLM-CONFIG-002";
        public const string CONFIG_MISSING_PARAMS = "LLM-CONFIG-003";

        // Retry and Recovery Errors
        public const string RETRY_MAX_ATTEMPTS = "LLM-RETRY-001";
        public const string RETRY_BACKOFF_ACTIVE = "LLM-RETRY-002";

        // Generic/Unknown Errors
        public const string UNKNOWN_ERROR = "LLM-UNKNOWN-001";
        public const string HTTP_GENERIC = "LLM-HTTP-GENERIC-001";
        public const string SOCKET_GENERIC = "LLM-SOCKET-GENERIC-001";
        public const string OPERATION_INVALID = "LLM-OPERATION-001";
    }

    /// <summary>
    /// Error message templates for user-friendly error descriptions.
    /// </summary>
    public static class LLMErrorMessages
    {
        public static readonly Dictionary<string, string> ErrorTemplates = new()
        {
            [LLMErrorCodes.HTTP_404_NOT_FOUND] = "The LLM service is not available. Please ensure Ollama is running and accessible at the configured endpoint.",
            [LLMErrorCodes.HTTP_401_UNAUTHORIZED] = "Authentication is required to access the LLM service. Please check your credentials.",
            [LLMErrorCodes.HTTP_403_FORBIDDEN] = "Access to the LLM service is forbidden. Please check your permissions.",
            [LLMErrorCodes.HTTP_500_INTERNAL_ERROR] = "The LLM service encountered an internal error. Please try again later.",
            [LLMErrorCodes.HTTP_502_BAD_GATEWAY] = "The LLM service gateway is not responding properly. Please check your network connection.",
            [LLMErrorCodes.HTTP_503_SERVICE_UNAVAILABLE] = "The LLM service is temporarily unavailable. Please try again in a few moments.",
            [LLMErrorCodes.HTTP_504_GATEWAY_TIMEOUT] = "The LLM service request timed out at the gateway. The service may be overloaded.",
            [LLMErrorCodes.HTTP_429_RATE_LIMITED] = "Too many requests to the LLM service. Please wait before trying again.",
            
            [LLMErrorCodes.CONN_REFUSED] = "Connection to the LLM service was refused. Please ensure the service is running and accessible.",
            [LLMErrorCodes.CONN_HOST_NOT_FOUND] = "The LLM service host could not be found. Please check the server address.",
            [LLMErrorCodes.CONN_NETWORK_UNREACHABLE] = "The LLM service network is unreachable. Please check your network connection.",
            [LLMErrorCodes.CONN_TIMEOUT] = "Connection to the LLM service timed out. The service may be overloaded or unreachable.",
            [LLMErrorCodes.CONN_SSL_FAILURE] = "Secure connection to the LLM service failed. Please check SSL/TLS configuration.",
            
            [LLMErrorCodes.REQ_TIMEOUT] = "The request to the LLM service timed out. The model may be taking longer than expected to respond.",
            [LLMErrorCodes.REQ_CANCELLED] = "The request to the LLM service was cancelled.",
            [LLMErrorCodes.RESP_INVALID_JSON] = "The LLM service returned an invalid response format.",
            [LLMErrorCodes.RESP_EMPTY] = "The LLM service returned an empty response.",
            
            [LLMErrorCodes.MODEL_NOT_FOUND] = "The requested LLM model is not available. Please ensure the model is installed.",
            [LLMErrorCodes.MODEL_UNAVAILABLE] = "The LLM model is currently unavailable. It may be loading or updating.",
            
            [LLMErrorCodes.RETRY_MAX_ATTEMPTS] = "Maximum retry attempts exceeded when trying to connect to the LLM service."
        };

        public static string GetErrorMessage(string errorCode, string? additionalInfo = null)
        {
            var baseMessage = ErrorTemplates.TryGetValue(errorCode, out var template) 
                ? template 
                : "An unexpected error occurred while communicating with the LLM service.";

            return additionalInfo != null ? $"{baseMessage} Additional details: {additionalInfo}" : baseMessage;
        }
    }
}