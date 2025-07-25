using JarvisAssistant.Core.Models;

namespace JarvisAssistant.MAUI.Models
{
    /// <summary>
    /// Represents different types of error notifications that can be displayed in the UI.
    /// </summary>
    public enum ErrorNotificationType
    {
        /// <summary>
        /// Toast notification - brief, non-intrusive message that appears temporarily.
        /// </summary>
        Toast,

        /// <summary>
        /// Modal dialog - blocking dialog that requires user acknowledgment.
        /// </summary>
        Modal,

        /// <summary>
        /// Inline message - error message displayed within the content area.
        /// </summary>
        Inline,

        /// <summary>
        /// Status bar indicator - persistent status indicator in the status bar.
        /// </summary>
        StatusBar,

        /// <summary>
        /// Banner notification - prominent banner displayed at the top of the screen.
        /// </summary>
        Banner
    }

    /// <summary>
    /// Represents an error notification to be displayed in the UI with Jarvis-appropriate styling.
    /// </summary>
    public class ErrorNotification
    {
        /// <summary>
        /// Gets or sets the unique identifier for this notification.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the error information associated with this notification.
        /// </summary>
        public ErrorInfo ErrorInfo { get; set; } = new();

        /// <summary>
        /// Gets or sets the type of notification to display.
        /// </summary>
        public ErrorNotificationType NotificationType { get; set; } = ErrorNotificationType.Toast;

        /// <summary>
        /// Gets or sets the title of the notification.
        /// </summary>
        public string Title { get; set; } = "System Notice";

        /// <summary>
        /// Gets or sets the main message to display (uses Jarvis-style error message if not provided).
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets optional additional details or actions.
        /// </summary>
        public string? Details { get; set; }

        /// <summary>
        /// Gets or sets the duration for which the notification should be displayed (for temporary notifications).
        /// </summary>
        public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets whether the notification can be dismissed by the user.
        /// </summary>
        public bool IsDismissible { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the notification should auto-dismiss after the duration.
        /// </summary>
        public bool AutoDismiss { get; set; } = true;

        /// <summary>
        /// Gets or sets the priority of the notification (higher priority notifications are shown first).
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Gets or sets whether the notification has been dismissed.
        /// </summary>
        public bool IsDismissed { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the notification was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets optional action buttons for the notification.
        /// </summary>
        public List<ErrorNotificationAction> Actions { get; set; } = new();

        /// <summary>
        /// Gets or sets platform-specific display options.
        /// </summary>
        public Dictionary<string, object> PlatformOptions { get; set; } = new();

        /// <summary>
        /// Creates an error notification from an ErrorInfo object with intelligent defaults.
        /// </summary>
        /// <param name="errorInfo">The error information to create a notification for.</param>
        /// <param name="notificationType">The type of notification to create.</param>
        /// <returns>A configured error notification.</returns>
        public static ErrorNotification FromErrorInfo(ErrorInfo errorInfo, ErrorNotificationType? notificationType = null)
        {
            var notification = new ErrorNotification
            {
                ErrorInfo = errorInfo,
                Message = errorInfo.UserMessage ?? "An unexpected situation has occurred."
            };

            // Determine notification type based on severity if not specified
            if (notificationType.HasValue)
            {
                notification.NotificationType = notificationType.Value;
            }
            else
            {
                notification.NotificationType = errorInfo.Severity switch
                {
                    ErrorSeverity.Info => ErrorNotificationType.Toast,
                    ErrorSeverity.Warning => ErrorNotificationType.Toast,
                    ErrorSeverity.Error => ErrorNotificationType.Modal,
                    ErrorSeverity.Critical => ErrorNotificationType.Modal,
                    ErrorSeverity.Fatal => ErrorNotificationType.Modal,
                    _ => ErrorNotificationType.Toast
                };
            }

            // Set title based on severity
            notification.Title = errorInfo.Severity switch
            {
                ErrorSeverity.Info => "Information",
                ErrorSeverity.Warning => "Advisory Notice",
                ErrorSeverity.Error => "System Notice",
                ErrorSeverity.Critical => "Critical System Notice",
                ErrorSeverity.Fatal => "Emergency System Notice",
                _ => "System Notice"
            };

            // Set duration based on severity
            notification.Duration = errorInfo.Severity switch
            {
                ErrorSeverity.Info => TimeSpan.FromSeconds(3),
                ErrorSeverity.Warning => TimeSpan.FromSeconds(5),
                ErrorSeverity.Error => TimeSpan.FromSeconds(8),
                ErrorSeverity.Critical => TimeSpan.FromSeconds(10),
                ErrorSeverity.Fatal => TimeSpan.FromSeconds(15),
                _ => TimeSpan.FromSeconds(5)
            };

            // Set priority based on severity
            notification.Priority = errorInfo.Severity switch
            {
                ErrorSeverity.Info => 1,
                ErrorSeverity.Warning => 2,
                ErrorSeverity.Error => 3,
                ErrorSeverity.Critical => 4,
                ErrorSeverity.Fatal => 5,
                _ => 2
            };

            // Critical and fatal errors shouldn't auto-dismiss
            if (errorInfo.Severity >= ErrorSeverity.Critical)
            {
                notification.AutoDismiss = false;
            }

            // Add technical details if available
            if (!string.IsNullOrEmpty(errorInfo.TechnicalDetails))
            {
                notification.Details = $"Technical Details: {errorInfo.TechnicalDetails}";
            }

            return notification;
        }

        /// <summary>
        /// Creates a toast notification for informational messages.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="title">Optional title for the notification.</param>
        /// <param name="duration">Optional duration for the notification.</param>
        /// <returns>A configured toast notification.</returns>
        public static ErrorNotification CreateToast(string message, string? title = null, TimeSpan? duration = null)
        {
            return new ErrorNotification
            {
                NotificationType = ErrorNotificationType.Toast,
                Title = title ?? "Notification",
                Message = message,
                Duration = duration ?? TimeSpan.FromSeconds(3),
                AutoDismiss = true,
                Priority = 1
            };
        }

        /// <summary>
        /// Creates a modal dialog notification for important messages.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="title">Optional title for the dialog.</param>
        /// <param name="actions">Optional actions for the dialog.</param>
        /// <returns>A configured modal notification.</returns>
        public static ErrorNotification CreateModal(string message, string? title = null, List<ErrorNotificationAction>? actions = null)
        {
            return new ErrorNotification
            {
                NotificationType = ErrorNotificationType.Modal,
                Title = title ?? "System Notice",
                Message = message,
                AutoDismiss = false,
                Priority = 3,
                Actions = actions ?? new List<ErrorNotificationAction>
                {
                    new ErrorNotificationAction { Text = "Understood", IsDefault = true }
                }
            };
        }

        /// <summary>
        /// Creates an inline error message for chat or content areas.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="context">Optional context information.</param>
        /// <returns>A configured inline notification.</returns>
        public static ErrorNotification CreateInline(string message, string? context = null)
        {
            return new ErrorNotification
            {
                NotificationType = ErrorNotificationType.Inline,
                Title = "Notice",
                Message = message,
                Details = context,
                AutoDismiss = false,
                IsDismissible = true,
                Priority = 2
            };
        }
    }

    /// <summary>
    /// Represents an action button that can be displayed on error notifications.
    /// </summary>
    public class ErrorNotificationAction
    {
        /// <summary>
        /// Gets or sets the text to display on the action button.
        /// </summary>
        public string Text { get; set; } = "OK";

        /// <summary>
        /// Gets or sets the action to perform when the button is clicked.
        /// </summary>
        public Func<Task>? Action { get; set; }

        /// <summary>
        /// Gets or sets whether this is the default action (highlighted button).
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// Gets or sets whether this is a destructive action (warning styling).
        /// </summary>
        public bool IsDestructive { get; set; }

        /// <summary>
        /// Gets or sets whether the action dismisses the notification.
        /// </summary>
        public bool DismissesNotification { get; set; } = true;

        /// <summary>
        /// Gets or sets additional styling options for the action.
        /// </summary>
        public Dictionary<string, object> StyleOptions { get; set; } = new();
    }
}
