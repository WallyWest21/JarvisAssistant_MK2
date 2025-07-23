namespace JarvisAssistant.Core.Models
{
    /// <summary>
    /// Represents information about an error that occurred in the application.
    /// </summary>
    public class ErrorInfo
    {
        /// <summary>
        /// Gets or sets the unique error code identifying the type of error.
        /// </summary>
        public string ErrorCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user-friendly error message suitable for display to end users.
        /// </summary>
        public string UserMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the technical details about the error for debugging purposes.
        /// </summary>
        public string? TechnicalDetails { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the error occurred.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets the severity level of the error.
        /// </summary>
        public ErrorSeverity Severity { get; set; } = ErrorSeverity.Error;

        /// <summary>
        /// Gets or sets the source or component where the error originated.
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// Gets or sets additional context or metadata related to the error.
        /// </summary>
        public Dictionary<string, object>? Context { get; set; }

        /// <summary>
        /// Gets or sets the inner exception details if applicable.
        /// </summary>
        public string? InnerException { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorInfo"/> class.
        /// </summary>
        public ErrorInfo()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorInfo"/> class with the specified error details.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="userMessage">The user-friendly error message.</param>
        /// <param name="technicalDetails">Optional technical details.</param>
        public ErrorInfo(string errorCode, string userMessage, string? technicalDetails = null)
        {
            ErrorCode = errorCode;
            UserMessage = userMessage;
            TechnicalDetails = technicalDetails;
        }

        /// <summary>
        /// Creates an ErrorInfo instance from an exception.
        /// </summary>
        /// <param name="exception">The exception to create error info from.</param>
        /// <param name="errorCode">Optional custom error code.</param>
        /// <param name="userMessage">Optional custom user message.</param>
        /// <returns>A new ErrorInfo instance.</returns>
        public static ErrorInfo FromException(Exception exception, string? errorCode = null, string? userMessage = null)
        {
            return new ErrorInfo
            {
                ErrorCode = errorCode ?? exception.GetType().Name,
                UserMessage = userMessage ?? "An unexpected error occurred. Please try again.",
                TechnicalDetails = exception.Message,
                InnerException = exception.InnerException?.ToString(),
                Source = exception.Source
            };
        }
    }

    /// <summary>
    /// Represents the severity level of an error.
    /// </summary>
    public enum ErrorSeverity
    {
        /// <summary>
        /// Informational message, not an error.
        /// </summary>
        Info,

        /// <summary>
        /// Warning message indicating a potential issue.
        /// </summary>
        Warning,

        /// <summary>
        /// Error that affects functionality but doesn't crash the application.
        /// </summary>
        Error,

        /// <summary>
        /// Critical error that may cause application instability.
        /// </summary>
        Critical,

        /// <summary>
        /// Fatal error that causes application termination.
        /// </summary>
        Fatal
    }
}
