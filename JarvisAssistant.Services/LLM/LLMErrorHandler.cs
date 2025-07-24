using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using JarvisAssistant.Core.Models;

namespace JarvisAssistant.Services.LLM
{
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
            [LLMErrorCodes.HTTP_400_BAD_REQUEST] = "The request to the LLM service was invalid. Please check your input parameters.",
            [LLMErrorCodes.HTTP_408_REQUEST_TIMEOUT] = "The request to the LLM service timed out. Please try again.",
            [LLMErrorCodes.HTTP_429_RATE_LIMITED] = "Too many requests to the LLM service. Please wait before trying again.",
            [LLMErrorCodes.HTTP_GENERIC] = "An HTTP error occurred while communicating with the LLM service.",
            
            [LLMErrorCodes.CONN_REFUSED] = "Connection to the LLM service was refused. Please ensure the service is running and accessible.",
            [LLMErrorCodes.CONN_HOST_NOT_FOUND] = "The LLM service host could not be found. Please check the server address.",
            [LLMErrorCodes.CONN_NETWORK_UNREACHABLE] = "The LLM service network is unreachable. Please check your network connection.",
            [LLMErrorCodes.CONN_TIMEOUT] = "Connection to the LLM service timed out. The service may be overloaded or unreachable.",
            [LLMErrorCodes.CONN_SSL_FAILURE] = "Secure connection to the LLM service failed. Please check SSL/TLS configuration.",
            
            [LLMErrorCodes.REQ_TIMEOUT] = "The request to the LLM service timed out. The model may be taking longer than expected to respond.",
            [LLMErrorCodes.REQ_CANCELLED] = "The request to the LLM service was cancelled.",
            [LLMErrorCodes.RESP_INVALID_JSON] = "The LLM service returned an invalid response format.",
            [LLMErrorCodes.RESP_EMPTY] = "The LLM service returned an empty response.",
            [LLMErrorCodes.RESP_TOO_LARGE] = "The LLM service response was too large to process.",
            
            [LLMErrorCodes.STREAM_CONNECTION_DROPPED] = "The streaming connection to the LLM service was dropped unexpectedly.",
            [LLMErrorCodes.STREAM_INVALID_FORMAT] = "The LLM service returned an invalid streaming format.",
            [LLMErrorCodes.STREAM_TIMEOUT] = "The streaming request to the LLM service timed out.",
            
            [LLMErrorCodes.MODEL_NOT_FOUND] = "The requested LLM model is not available. Please ensure the model is installed.",
            [LLMErrorCodes.MODEL_UNAVAILABLE] = "The LLM model is currently unavailable. It may be loading or updating.",
            [LLMErrorCodes.MODEL_LOADING] = "The LLM model is currently loading. Please wait and try again.",
            
            [LLMErrorCodes.RESOURCE_OUT_OF_MEMORY] = "Insufficient memory to process the LLM request.",
            [LLMErrorCodes.RESOURCE_DISK_FULL] = "Insufficient disk space for the LLM operation.",
            [LLMErrorCodes.RESOURCE_CPU_OVERLOAD] = "CPU overload detected during LLM operation.",
            
            [LLMErrorCodes.CONFIG_INVALID_URL] = "Invalid LLM service URL configuration. Please check the service endpoint.",
            [LLMErrorCodes.CONFIG_INVALID_TIMEOUT] = "Invalid timeout configuration for the LLM service.",
            [LLMErrorCodes.CONFIG_MISSING_PARAMS] = "Missing required configuration parameters for the LLM service.",
            
            [LLMErrorCodes.RETRY_MAX_ATTEMPTS] = "Maximum retry attempts exceeded when trying to connect to the LLM service.",
            [LLMErrorCodes.RETRY_BACKOFF_ACTIVE] = "Retry backoff is currently active. Please wait before retrying.",
            
            [LLMErrorCodes.UNKNOWN_ERROR] = "An unexpected error occurred while communicating with the LLM service.",
            [LLMErrorCodes.SOCKET_GENERIC] = "A network socket error occurred while communicating with the LLM service.",
            [LLMErrorCodes.OPERATION_INVALID] = "An invalid operation was attempted with the LLM service."
        };

        public static string GetErrorMessage(string errorCode, string? additionalInfo = null)
        {
            var baseMessage = ErrorTemplates.TryGetValue(errorCode, out var template) 
                ? template 
                : "An unexpected error occurred while communicating with the LLM service.";

            return additionalInfo != null ? $"{baseMessage} Additional details: {additionalInfo}" : baseMessage;
        }
    }

    /// <summary>
    /// Service for handling and categorizing LLM communication errors with specific error codes and user-friendly messages.
    /// </summary>
    public class LLMErrorHandler
    {
        private readonly ILogger<LLMErrorHandler> _logger;

        public LLMErrorHandler(ILogger<LLMErrorHandler> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Processes an exception and returns a standardized error response.
        /// </summary>
        /// <param name="exception">The exception to process.</param>
        /// <param name="context">Additional context about the operation.</param>
        /// <returns>A standardized error response with error code and user-friendly message.</returns>
        public LLMErrorResponse ProcessException(Exception exception, string context = "")
        {
            var errorResponse = exception switch
            {
                HttpRequestException httpEx => ProcessHttpRequestException(httpEx),
                TaskCanceledException cancelEx => ProcessTaskCanceledException(cancelEx),
                TimeoutException timeoutEx => ProcessTimeoutException(timeoutEx),
                JsonException jsonEx => ProcessJsonException(jsonEx),
                SocketException socketEx => ProcessSocketException(socketEx),
                OutOfMemoryException memEx => ProcessOutOfMemoryException(memEx),
                OperationCanceledException opCancelEx => ProcessOperationCanceledException(opCancelEx),
                ArgumentException argEx => ProcessArgumentException(argEx),
                InvalidOperationException invOpEx => ProcessInvalidOperationException(invOpEx),
                _ => ProcessUnknownException(exception)
            };

            // Add context if provided
            if (!string.IsNullOrEmpty(context))
            {
                errorResponse.Context = context;
                errorResponse.TechnicalDetails += $" Context: {context}";
            }

            // Log the error with appropriate level
            LogError(errorResponse, exception);

            return errorResponse;
        }

        /// <summary>
        /// Creates an error response for HTTP-related exceptions.
        /// </summary>
        private LLMErrorResponse ProcessHttpRequestException(HttpRequestException exception)
        {
            var message = exception.Message.ToLowerInvariant();
            
            if (message.Contains("404") || message.Contains("not found"))
            {
                return new LLMErrorResponse
                {
                    ErrorCode = LLMErrorCodes.HTTP_404_NOT_FOUND,
                    UserMessage = LLMErrorMessages.GetErrorMessage(LLMErrorCodes.HTTP_404_NOT_FOUND),
                    TechnicalDetails = exception.Message,
                    Severity = ErrorSeverity.Critical,
                    IsRetryable = false,
                    SuggestedAction = "Ensure Ollama is installed and running on the configured port (default: 11434)"
                };
            }

            if (message.Contains("401") || message.Contains("unauthorized"))
            {
                return new LLMErrorResponse
                {
                    ErrorCode = LLMErrorCodes.HTTP_401_UNAUTHORIZED,
                    UserMessage = LLMErrorMessages.GetErrorMessage(LLMErrorCodes.HTTP_401_UNAUTHORIZED),
                    TechnicalDetails = exception.Message,
                    Severity = ErrorSeverity.Critical,
                    IsRetryable = false,
                    SuggestedAction = "Check authentication credentials and API access permissions"
                };
            }

            if (message.Contains("403") || message.Contains("forbidden"))
            {
                return new LLMErrorResponse
                {
                    ErrorCode = LLMErrorCodes.HTTP_403_FORBIDDEN,
                    UserMessage = LLMErrorMessages.GetErrorMessage(LLMErrorCodes.HTTP_403_FORBIDDEN),
                    TechnicalDetails = exception.Message,
                    Severity = ErrorSeverity.Critical,
                    IsRetryable = false,
                    SuggestedAction = "Verify access permissions and API key configuration"
                };
            }

            if (message.Contains("500") || message.Contains("internal server error"))
            {
                return new LLMErrorResponse
                {
                    ErrorCode = LLMErrorCodes.HTTP_500_INTERNAL_ERROR,
                    UserMessage = LLMErrorMessages.GetErrorMessage(LLMErrorCodes.HTTP_500_INTERNAL_ERROR),
                    TechnicalDetails = exception.Message,
                    Severity = ErrorSeverity.Error,
                    IsRetryable = true,
                    SuggestedAction = "Wait a moment and try again. If the problem persists, check server logs"
                };
            }

            if (message.Contains("502") || message.Contains("bad gateway"))
            {
                return new LLMErrorResponse
                {
                    ErrorCode = LLMErrorCodes.HTTP_502_BAD_GATEWAY,
                    UserMessage = LLMErrorMessages.GetErrorMessage(LLMErrorCodes.HTTP_502_BAD_GATEWAY),
                    TechnicalDetails = exception.Message,
                    Severity = ErrorSeverity.Error,
                    IsRetryable = true,
                    SuggestedAction = "Check proxy or load balancer configuration"
                };
            }

            if (message.Contains("503") || message.Contains("service unavailable"))
            {
                return new LLMErrorResponse
                {
                    ErrorCode = LLMErrorCodes.HTTP_503_SERVICE_UNAVAILABLE,
                    UserMessage = LLMErrorMessages.GetErrorMessage(LLMErrorCodes.HTTP_503_SERVICE_UNAVAILABLE),
                    TechnicalDetails = exception.Message,
                    Severity = ErrorSeverity.Error,
                    IsRetryable = true,
                    SuggestedAction = "Service is temporarily overloaded. Wait and retry in a few minutes"
                };
            }

            if (message.Contains("504") || message.Contains("gateway timeout"))
            {
                return new LLMErrorResponse
                {
                    ErrorCode = LLMErrorCodes.HTTP_504_GATEWAY_TIMEOUT,
                    UserMessage = LLMErrorMessages.GetErrorMessage(LLMErrorCodes.HTTP_504_GATEWAY_TIMEOUT),
                    TechnicalDetails = exception.Message,
                    Severity = ErrorSeverity.Error,
                    IsRetryable = true,
                    SuggestedAction = "Server is overloaded. Try a simpler query or wait before retrying"
                };
            }

            if (message.Contains("429") || message.Contains("too many requests"))
            {
                return new LLMErrorResponse
                {
                    ErrorCode = LLMErrorCodes.HTTP_429_RATE_LIMITED,
                    UserMessage = LLMErrorMessages.GetErrorMessage(LLMErrorCodes.HTTP_429_RATE_LIMITED),
                    TechnicalDetails = exception.Message,
                    Severity = ErrorSeverity.Warning,
                    IsRetryable = true,
                    SuggestedAction = "Wait before making additional requests to avoid rate limiting"
                };
            }

            if ((message.Contains("connection") && (message.Contains("refused") || message.Contains("failed"))) || 
                message.Contains("unable to connect"))
            {
                return new LLMErrorResponse
                {
                    ErrorCode = LLMErrorCodes.CONN_REFUSED,
                    UserMessage = LLMErrorMessages.GetErrorMessage(LLMErrorCodes.CONN_REFUSED),
                    TechnicalDetails = exception.Message,
                    Severity = ErrorSeverity.Critical,
                    IsRetryable = true,
                    SuggestedAction = "Verify Ollama is running: 'ollama serve' or check if port 11434 is accessible"
                };
            }

            if (message.Contains("ssl") || message.Contains("tls") || message.Contains("certificate"))
            {
                return new LLMErrorResponse
                {
                    ErrorCode = LLMErrorCodes.CONN_SSL_FAILURE,
                    UserMessage = LLMErrorMessages.GetErrorMessage(LLMErrorCodes.CONN_SSL_FAILURE),
                    TechnicalDetails = exception.Message,
                    Severity = ErrorSeverity.Critical,
                    IsRetryable = false,
                    SuggestedAction = "Check SSL/TLS configuration or use HTTP instead of HTTPS for local development"
                };
            }

            // Generic HTTP error
            return new LLMErrorResponse
            {
                ErrorCode = LLMErrorCodes.HTTP_GENERIC,
                UserMessage = "An HTTP error occurred while communicating with the LLM service",
                TechnicalDetails = exception.Message,
                Severity = ErrorSeverity.Error,
                IsRetryable = true,
                SuggestedAction = "Check network connectivity and service configuration"
            };
        }

        /// <summary>
        /// Creates an error response for task cancellation exceptions.
        /// </summary>
        private LLMErrorResponse ProcessTaskCanceledException(TaskCanceledException exception)
        {
            if (exception.InnerException is TimeoutException)
            {
                return new LLMErrorResponse
                {
                    ErrorCode = LLMErrorCodes.REQ_TIMEOUT,
                    UserMessage = LLMErrorMessages.GetErrorMessage(LLMErrorCodes.REQ_TIMEOUT),
                    TechnicalDetails = exception.Message,
                    Severity = ErrorSeverity.Error,
                    IsRetryable = true,
                    SuggestedAction = "Try a shorter prompt or increase timeout settings. The model may be processing a complex request"
                };
            }

            return new LLMErrorResponse
            {
                ErrorCode = LLMErrorCodes.REQ_CANCELLED,
                UserMessage = LLMErrorMessages.GetErrorMessage(LLMErrorCodes.REQ_CANCELLED),
                TechnicalDetails = exception.Message,
                Severity = ErrorSeverity.Warning,
                IsRetryable = true,
                SuggestedAction = "The request was cancelled. You can retry the operation"
            };
        }

        /// <summary>
        /// Creates an error response for timeout exceptions.
        /// </summary>
        private LLMErrorResponse ProcessTimeoutException(TimeoutException exception)
        {
            return new LLMErrorResponse
            {
                ErrorCode = LLMErrorCodes.REQ_TIMEOUT,
                UserMessage = LLMErrorMessages.GetErrorMessage(LLMErrorCodes.REQ_TIMEOUT),
                TechnicalDetails = exception.Message,
                Severity = ErrorSeverity.Error,
                IsRetryable = true,
                SuggestedAction = "The model is taking longer than expected. Try a simpler prompt or check server performance"
            };
        }

        /// <summary>
        /// Creates an error response for JSON parsing exceptions.
        /// </summary>
        private LLMErrorResponse ProcessJsonException(JsonException exception)
        {
            return new LLMErrorResponse
            {
                ErrorCode = LLMErrorCodes.RESP_INVALID_JSON,
                UserMessage = LLMErrorMessages.GetErrorMessage(LLMErrorCodes.RESP_INVALID_JSON),
                TechnicalDetails = exception.Message,
                Severity = ErrorSeverity.Error,
                IsRetryable = true,
                SuggestedAction = "The server returned malformed data. This may indicate a server-side issue"
            };
        }

        /// <summary>
        /// Creates an error response for socket exceptions.
        /// </summary>
        private LLMErrorResponse ProcessSocketException(SocketException exception)
        {
            return exception.SocketErrorCode switch
            {
                SocketError.ConnectionRefused => new LLMErrorResponse
                {
                    ErrorCode = LLMErrorCodes.CONN_REFUSED,
                    UserMessage = LLMErrorMessages.GetErrorMessage(LLMErrorCodes.CONN_REFUSED),
                    TechnicalDetails = $"{exception.SocketErrorCode}: {exception.Message}",
                    Severity = ErrorSeverity.Critical,
                    IsRetryable = true,
                    SuggestedAction = "Start Ollama service: 'ollama serve' or check firewall settings"
                },

                SocketError.HostNotFound => new LLMErrorResponse
                {
                    ErrorCode = LLMErrorCodes.CONN_HOST_NOT_FOUND,
                    UserMessage = LLMErrorMessages.GetErrorMessage(LLMErrorCodes.CONN_HOST_NOT_FOUND),
                    TechnicalDetails = $"{exception.SocketErrorCode}: {exception.Message}",
                    Severity = ErrorSeverity.Critical,
                    IsRetryable = false,
                    SuggestedAction = "Verify the server hostname or IP address in configuration"
                },

                SocketError.NetworkUnreachable => new LLMErrorResponse
                {
                    ErrorCode = LLMErrorCodes.CONN_NETWORK_UNREACHABLE,
                    UserMessage = LLMErrorMessages.GetErrorMessage(LLMErrorCodes.CONN_NETWORK_UNREACHABLE),
                    TechnicalDetails = $"{exception.SocketErrorCode}: {exception.Message}",
                    Severity = ErrorSeverity.Critical,
                    IsRetryable = true,
                    SuggestedAction = "Check network connectivity and routing configuration"
                },

                SocketError.TimedOut => new LLMErrorResponse
                {
                    ErrorCode = LLMErrorCodes.CONN_TIMEOUT,
                    UserMessage = LLMErrorMessages.GetErrorMessage(LLMErrorCodes.CONN_TIMEOUT),
                    TechnicalDetails = $"{exception.SocketErrorCode}: {exception.Message}",
                    Severity = ErrorSeverity.Error,
                    IsRetryable = true,
                    SuggestedAction = "Check network latency and server performance"
                },

                _ => new LLMErrorResponse
                {
                    ErrorCode = LLMErrorCodes.SOCKET_GENERIC,
                    UserMessage = "A network socket error occurred",
                    TechnicalDetails = exception.Message,
                    Severity = ErrorSeverity.Error,
                    IsRetryable = true,
                    SuggestedAction = "Check network connectivity and server availability"
                }
            };
        }

        /// <summary>
        /// Creates an error response for out of memory exceptions.
        /// </summary>
        private LLMErrorResponse ProcessOutOfMemoryException(OutOfMemoryException exception)
        {
            return new LLMErrorResponse
            {
                ErrorCode = LLMErrorCodes.RESOURCE_OUT_OF_MEMORY,
                UserMessage = "Insufficient memory to process the request",
                TechnicalDetails = exception.Message,
                Severity = ErrorSeverity.Critical,
                IsRetryable = false,
                SuggestedAction = "Try a shorter prompt or restart the application to free memory"
            };
        }

        /// <summary>
        /// Creates an error response for operation cancelled exceptions.
        /// </summary>
        private LLMErrorResponse ProcessOperationCanceledException(OperationCanceledException exception)
        {
            return new LLMErrorResponse
            {
                ErrorCode = LLMErrorCodes.REQ_CANCELLED,
                UserMessage = LLMErrorMessages.GetErrorMessage(LLMErrorCodes.REQ_CANCELLED),
                TechnicalDetails = exception.Message,
                Severity = ErrorSeverity.Warning,
                IsRetryable = true,
                SuggestedAction = "The operation was cancelled. You can retry if needed"
            };
        }

        /// <summary>
        /// Creates an error response for argument exceptions.
        /// </summary>
        private LLMErrorResponse ProcessArgumentException(ArgumentException exception)
        {
            return new LLMErrorResponse
            {
                ErrorCode = LLMErrorCodes.CONFIG_MISSING_PARAMS,
                UserMessage = "Invalid parameters provided to the LLM service",
                TechnicalDetails = exception.Message,
                Severity = ErrorSeverity.Error,
                IsRetryable = false,
                SuggestedAction = "Check the request parameters and configuration"
            };
        }

        /// <summary>
        /// Creates an error response for invalid operation exceptions.
        /// </summary>
        private LLMErrorResponse ProcessInvalidOperationException(InvalidOperationException exception)
        {
            var message = exception.Message.ToLowerInvariant();

            if (message.Contains("model") && message.Contains("not available"))
            {
                return new LLMErrorResponse
                {
                    ErrorCode = LLMErrorCodes.MODEL_NOT_FOUND,
                    UserMessage = LLMErrorMessages.GetErrorMessage(LLMErrorCodes.MODEL_NOT_FOUND),
                    TechnicalDetails = exception.Message,
                    Severity = ErrorSeverity.Critical,
                    IsRetryable = false,
                    SuggestedAction = "Ensure required models are installed: 'ollama pull llama3.2' and 'ollama pull deepseek-coder'"
                };
            }

            if (message.Contains("retry") && message.Contains("exceeded"))
            {
                return new LLMErrorResponse
                {
                    ErrorCode = LLMErrorCodes.RETRY_MAX_ATTEMPTS,
                    UserMessage = LLMErrorMessages.GetErrorMessage(LLMErrorCodes.RETRY_MAX_ATTEMPTS),
                    TechnicalDetails = exception.Message,
                    Severity = ErrorSeverity.Critical,
                    IsRetryable = false,
                    SuggestedAction = "Check service availability and try again later"
                };
            }

            return new LLMErrorResponse
            {
                ErrorCode = LLMErrorCodes.OPERATION_INVALID,
                UserMessage = "An invalid operation was attempted",
                TechnicalDetails = exception.Message,
                Severity = ErrorSeverity.Error,
                IsRetryable = false,
                SuggestedAction = "Check the operation parameters and try again"
            };
        }

        /// <summary>
        /// Creates an error response for unknown exceptions.
        /// </summary>
        private LLMErrorResponse ProcessUnknownException(Exception exception)
        {
            return new LLMErrorResponse
            {
                ErrorCode = LLMErrorCodes.UNKNOWN_ERROR,
                UserMessage = "An unexpected error occurred while communicating with the LLM service",
                TechnicalDetails = exception.Message,
                Severity = ErrorSeverity.Error,
                IsRetryable = true,
                SuggestedAction = "Try again. If the problem persists, check the application logs"
            };
        }

        /// <summary>
        /// Logs the error with appropriate level based on severity.
        /// </summary>
        private void LogError(LLMErrorResponse errorResponse, Exception exception)
        {
            var logLevel = errorResponse.Severity switch
            {
                ErrorSeverity.Warning => LogLevel.Warning,
                ErrorSeverity.Error => LogLevel.Error,
                ErrorSeverity.Critical => LogLevel.Critical,
                ErrorSeverity.Fatal => LogLevel.Critical,
                ErrorSeverity.Info => LogLevel.Information,
                _ => LogLevel.Error
            };

            _logger.Log(logLevel, exception, 
                "LLM Error - Code: {ErrorCode}, Message: {UserMessage}, Retryable: {IsRetryable}",
                errorResponse.ErrorCode, errorResponse.UserMessage, errorResponse.IsRetryable);
        }
    }

    /// <summary>
    /// Represents a standardized error response from the LLM error handler.
    /// </summary>
    public class LLMErrorResponse
    {
        /// <summary>
        /// Unique error code for programmatic handling.
        /// </summary>
        public string ErrorCode { get; set; } = string.Empty;

        /// <summary>
        /// User-friendly error message.
        /// </summary>
        public string UserMessage { get; set; } = string.Empty;

        /// <summary>
        /// Technical details for debugging.
        /// </summary>
        public string TechnicalDetails { get; set; } = string.Empty;

        /// <summary>
        /// Additional context about the operation that failed.
        /// </summary>
        public string? Context { get; set; }

        /// <summary>
        /// Severity level of the error.
        /// </summary>
        public ErrorSeverity Severity { get; set; } = ErrorSeverity.Error;

        /// <summary>
        /// Whether the operation can be retried.
        /// </summary>
        public bool IsRetryable { get; set; } = true;

        /// <summary>
        /// Suggested action for the user to resolve the issue.
        /// </summary>
        public string? SuggestedAction { get; set; }

        /// <summary>
        /// Timestamp when the error occurred.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Creates a ChatResponse representing this error.
        /// </summary>
        public ChatResponse ToChatResponse(string conversationId = "")
        {
            return new ChatResponse(UserMessage, "error")
            {
                Timestamp = Timestamp,
                IsComplete = true,
                Metadata = new Dictionary<string, object>
                {
                    ["errorCode"] = ErrorCode,
                    ["severity"] = Severity.ToString(),
                    ["isRetryable"] = IsRetryable,
                    ["technicalDetails"] = TechnicalDetails,
                    ["suggestedAction"] = SuggestedAction ?? "",
                    ["context"] = Context ?? "",
                    ["conversationId"] = conversationId
                }
            };
        }
    }
}