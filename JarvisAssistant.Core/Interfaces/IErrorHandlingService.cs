using JarvisAssistant.Core.Models;

namespace JarvisAssistant.Core.Interfaces
{
    /// <summary>
    /// Provides methods for handling and managing application errors.
    /// </summary>
    public interface IErrorHandlingService
    {
        /// <summary>
        /// Handles an error that occurred in the application.
        /// </summary>
        /// <param name="errorInfo">The error information to handle.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task HandleErrorAsync(ErrorInfo errorInfo);

        /// <summary>
        /// Handles an exception that occurred in the application.
        /// </summary>
        /// <param name="exception">The exception to handle.</param>
        /// <param name="context">Optional context information about where the error occurred.</param>
        /// <param name="userMessage">Optional custom user-friendly message.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task HandleErrorAsync(Exception exception, string? context = null, string? userMessage = null);

        /// <summary>
        /// Logs an error without user notification.
        /// </summary>
        /// <param name="errorInfo">The error information to log.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task LogErrorAsync(ErrorInfo errorInfo);

        /// <summary>
        /// Gets recent error history.
        /// </summary>
        /// <param name="count">The maximum number of recent errors to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the recent errors.</returns>
        Task<IEnumerable<ErrorInfo>> GetRecentErrorsAsync(int count = 10);

        /// <summary>
        /// Clears the error history.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task ClearErrorHistoryAsync();

        /// <summary>
        /// Occurs when a new error is handled.
        /// </summary>
        event EventHandler<ErrorInfo>? ErrorOccurred;

        /// <summary>
        /// Gets a value indicating whether error reporting to external services is enabled.
        /// </summary>
        bool IsErrorReportingEnabled { get; }

        /// <summary>
        /// Enables or disables error reporting to external services.
        /// </summary>
        /// <param name="enabled">True to enable error reporting, false to disable.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task SetErrorReportingAsync(bool enabled);
    }
}
