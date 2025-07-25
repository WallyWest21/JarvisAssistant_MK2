using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using JarvisAssistant.Core.ErrorCodes;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace JarvisAssistant.Services
{
    /// <summary>
    /// Enhanced error handling service with retry mechanisms and user-friendly messaging.
    /// </summary>
    public class EnhancedErrorHandlingService : IErrorHandlingService
    {
        private readonly ILogger<EnhancedErrorHandlingService> _logger;
        private readonly List<ErrorInfo> _errorHistory = new();
        private readonly ConcurrentDictionary<string, ErrorFrequencyTracker> _errorFrequency = new();
        private readonly object _lockObject = new();

        /// <summary>
        /// Occurs when a new error is handled.
        /// </summary>
        public event EventHandler<ErrorInfo>? ErrorOccurred;

        /// <summary>
        /// Gets a value indicating whether error reporting to external services is enabled.
        /// </summary>
        public bool IsErrorReportingEnabled { get; private set; } = true;

        public EnhancedErrorHandlingService(ILogger<EnhancedErrorHandlingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task HandleErrorAsync(ErrorInfo errorInfo)
        {
            if (errorInfo == null)
                return;

            try
            {
                // Enhance with Jarvis-style message if needed
                if (string.IsNullOrEmpty(errorInfo.UserMessage) && !string.IsNullOrEmpty(errorInfo.ErrorCode))
                {
                    errorInfo.UserMessage = JarvisErrorMessages.GetErrorMessage(errorInfo.ErrorCode);
                }

                // Track error frequency
                TrackErrorFrequency(errorInfo);

                // Log the error
                await LogErrorAsync(errorInfo);

                // Add to history
                lock (_lockObject)
                {
                    _errorHistory.Add(errorInfo);
                    if (_errorHistory.Count > 100)
                        _errorHistory.RemoveAt(0);
                }

                // Notify subscribers
                ErrorOccurred?.Invoke(this, errorInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle error properly");
            }
        }

        public async Task HandleErrorAsync(Exception exception, string? context = null, string? userMessage = null)
        {
            if (exception == null) return;

            var errorCode = MapExceptionToErrorCode(exception);
            var errorInfo = new ErrorInfo
            {
                ErrorCode = errorCode,
                UserMessage = userMessage ?? JarvisErrorMessages.GetErrorMessage(errorCode),
                TechnicalDetails = exception.ToString(),
                Severity = MapExceptionToSeverity(exception),
                Timestamp = DateTime.UtcNow,
                Source = context ?? "Unknown"
            };

            await HandleErrorAsync(errorInfo);
        }

        public async Task LogErrorAsync(ErrorInfo errorInfo)
        {
            if (errorInfo == null) return;

            try
            {
                var logLevel = errorInfo.Severity switch
                {
                    ErrorSeverity.Critical => LogLevel.Critical,
                    ErrorSeverity.Error => LogLevel.Error,
                    ErrorSeverity.Warning => LogLevel.Warning,
                    ErrorSeverity.Info => LogLevel.Information,
                    ErrorSeverity.Fatal => LogLevel.Critical,
                    _ => LogLevel.Error
                };

                _logger.Log(logLevel, 
                    "Error {ErrorCode}: {UserMessage} | Technical: {TechnicalDetails}",
                    errorInfo.ErrorCode ?? "UNKNOWN",
                    errorInfo.UserMessage ?? "No message",
                    errorInfo.TechnicalDetails ?? "No details");

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logging failed: {ex.Message}");
            }
        }

        public Task<IEnumerable<ErrorInfo>> GetRecentErrorsAsync(int count = 10)
        {
            lock (_lockObject)
            {
                var recentErrors = _errorHistory
                    .OrderByDescending(e => e.Timestamp)
                    .Take(Math.Max(1, count))
                    .ToList();

                return Task.FromResult<IEnumerable<ErrorInfo>>(recentErrors);
            }
        }

        public Task ClearErrorHistoryAsync()
        {
            lock (_lockObject)
            {
                _errorHistory.Clear();
                _logger.LogInformation("Error history cleared");
            }
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

        public async Task<T> ExecuteWithRetryAsync<T>(
            Func<Task<T>> operation, 
            int maxRetries = 3, 
            TimeSpan? retryDelay = null)
        {
            var delay = retryDelay ?? TimeSpan.FromSeconds(1);
            Exception? lastException = null;

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex) when (attempt < maxRetries && IsTransientError(ex))
                {
                    lastException = ex;
                    var currentDelay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * Math.Pow(2, attempt));
                    await Task.Delay(currentDelay);
                }
            }

            if (lastException != null)
                throw lastException;

            throw new InvalidOperationException("Operation failed");
        }

        public bool IsTransientError(Exception exception)
        {
            return exception switch
            {
                TimeoutException => true,
                HttpRequestException => true,
                TaskCanceledException => true,
                OperationCanceledException => true,
                SocketException => true,
                _ => false
            };
        }

        public int GetErrorFrequency(string errorCode, TimeSpan timeWindow)
        {
            if (!_errorFrequency.TryGetValue(errorCode, out var tracker))
                return 0;

            var cutoff = DateTime.UtcNow - timeWindow;
            lock (tracker.Occurrences)
            {
                return tracker.Occurrences.Count(occurrence => occurrence > cutoff);
            }
        }

        private string MapExceptionToErrorCode(Exception exception)
        {
            return exception switch
            {
                TimeoutException => ErrorCodeRegistry.NET_CONN_002,
                HttpRequestException => ErrorCodeRegistry.NET_CONN_001,
                UnauthorizedAccessException => ErrorCodeRegistry.NET_AUTH_001,
                ArgumentNullException => ErrorCodeRegistry.LLM_PROC_001,
                InvalidOperationException => ErrorCodeRegistry.LLM_PROC_002,
                OutOfMemoryException => ErrorCodeRegistry.LLM_MEM_001,
                _ => ErrorCodeRegistry.LLM_PROC_005
            };
        }

        private ErrorSeverity MapExceptionToSeverity(Exception exception)
        {
            return exception switch
            {
                OutOfMemoryException => ErrorSeverity.Critical,
                ArgumentNullException => ErrorSeverity.Error,
                InvalidOperationException => ErrorSeverity.Error,
                TimeoutException => ErrorSeverity.Warning,
                _ => ErrorSeverity.Error
            };
        }

        private void TrackErrorFrequency(ErrorInfo errorInfo)
        {
            if (string.IsNullOrEmpty(errorInfo.ErrorCode)) return;

            var tracker = _errorFrequency.GetOrAdd(errorInfo.ErrorCode, _ => new ErrorFrequencyTracker());
            
            lock (tracker.Occurrences)
            {
                tracker.Occurrences.Add(DateTime.UtcNow);
                
                // Clean up old occurrences
                var cutoff = DateTime.UtcNow.AddHours(-1);
                tracker.Occurrences.RemoveAll(occurrence => occurrence < cutoff);
            }
        }

        public void Dispose()
        {
            _errorFrequency.Clear();
            lock (_lockObject)
            {
                _errorHistory.Clear();
            }
        }
    }

    internal class ErrorFrequencyTracker
    {
        public List<DateTime> Occurrences { get; } = new();
    }
}
