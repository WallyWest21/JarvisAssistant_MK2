using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using Microsoft.Extensions.Logging;

namespace JarvisAssistant.Services
{
    /// <summary>
    /// Default implementation of the error handling service that manages application errors.
    /// </summary>
    public class ErrorHandlingService : IErrorHandlingService
    {
        private readonly ILogger<ErrorHandlingService> _logger;
        private readonly List<ErrorInfo> _errorHistory = new();
        private readonly object _lockObject = new();

        /// <summary>
        /// Occurs when a new error is handled.
        /// </summary>
        public event EventHandler<ErrorInfo>? ErrorOccurred;

        /// <summary>
        /// Gets a value indicating whether error reporting to external services is enabled.
        /// </summary>
        public bool IsErrorReportingEnabled { get; private set; } = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorHandlingService"/> class.
        /// </summary>
        /// <param name="logger">The logger for recording error information.</param>
        public ErrorHandlingService(ILogger<ErrorHandlingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles an error that occurred in the application.
        /// </summary>
        /// <param name="errorInfo">The error information to handle.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task HandleErrorAsync(ErrorInfo errorInfo)
        {
            if (errorInfo == null)
                throw new ArgumentNullException(nameof(errorInfo));

            // Log the error
            await LogErrorAsync(errorInfo);

            // Add to error history
            lock (_lockObject)
            {
                _errorHistory.Add(errorInfo);
                
                // Keep only the last 100 errors to prevent memory bloat
                if (_errorHistory.Count > 100)
                {
                    _errorHistory.RemoveAt(0);
                }
            }

            // Notify subscribers
            ErrorOccurred?.Invoke(this, errorInfo);

            // Report to external services if enabled
            if (IsErrorReportingEnabled && errorInfo.Severity >= ErrorSeverity.Error)
            {
                await ReportErrorToExternalServiceAsync(errorInfo);
            }
        }

        /// <summary>
        /// Handles an exception that occurred in the application.
        /// </summary>
        /// <param name="exception">The exception to handle.</param>
        /// <param name="context">Optional context information about where the error occurred.</param>
        /// <param name="userMessage">Optional custom user-friendly message.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task HandleErrorAsync(Exception exception, string? context = null, string? userMessage = null)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            var errorInfo = ErrorInfo.FromException(exception, userMessage: userMessage);
            errorInfo.Source = context;
            
            await HandleErrorAsync(errorInfo);
        }

        /// <summary>
        /// Logs an error without user notification.
        /// </summary>
        /// <param name="errorInfo">The error information to log.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task LogErrorAsync(ErrorInfo errorInfo)
        {
            if (errorInfo == null)
                throw new ArgumentNullException(nameof(errorInfo));

            var logLevel = errorInfo.Severity switch
            {
                ErrorSeverity.Info => LogLevel.Information,
                ErrorSeverity.Warning => LogLevel.Warning,
                ErrorSeverity.Error => LogLevel.Error,
                ErrorSeverity.Critical => LogLevel.Critical,
                ErrorSeverity.Fatal => LogLevel.Critical,
                _ => LogLevel.Error
            };

            _logger.Log(logLevel, 
                "Error {ErrorCode} in {Source}: {UserMessage}. Technical: {TechnicalDetails}", 
                errorInfo.ErrorCode,
                errorInfo.Source ?? "Unknown",
                errorInfo.UserMessage,
                errorInfo.TechnicalDetails);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets recent error history.
        /// </summary>
        /// <param name="count">The maximum number of recent errors to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the recent errors.</returns>
        public Task<IEnumerable<ErrorInfo>> GetRecentErrorsAsync(int count = 10)
        {
            lock (_lockObject)
            {
                var recentErrors = _errorHistory
                    .OrderByDescending(e => e.Timestamp)
                    .Take(count)
                    .ToList();

                return Task.FromResult<IEnumerable<ErrorInfo>>(recentErrors);
            }
        }

        /// <summary>
        /// Clears the error history.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task ClearErrorHistoryAsync()
        {
            lock (_lockObject)
            {
                _errorHistory.Clear();
            }

            _logger.LogInformation("Error history cleared");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Enables or disables error reporting to external services.
        /// </summary>
        /// <param name="enabled">True to enable error reporting, false to disable.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task SetErrorReportingAsync(bool enabled)
        {
            IsErrorReportingEnabled = enabled;
            _logger.LogInformation("Error reporting {Status}", enabled ? "enabled" : "disabled");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Reports an error to external monitoring services.
        /// </summary>
        /// <param name="errorInfo">The error information to report.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task ReportErrorToExternalServiceAsync(ErrorInfo errorInfo)
        {
            try
            {
                // TODO: Implement external error reporting (e.g., Application Insights, Sentry, etc.)
                // For now, just log that we would report it
                _logger.LogDebug("Would report error {ErrorCode} to external service", errorInfo.ErrorCode);
                
                await Task.Delay(1); // Placeholder for actual reporting logic
            }
            catch (Exception ex)
            {
                // Don't let error reporting errors crash the application
                _logger.LogWarning(ex, "Failed to report error to external service");
            }
        }
    }
}
