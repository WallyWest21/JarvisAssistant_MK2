using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.MAUI.Models;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;

namespace JarvisAssistant.MAUI.Services
{
    /// <summary>
    /// Service for managing error notifications across different UI contexts with platform-specific optimizations.
    /// Provides sophisticated notification management with Jarvis-appropriate styling and behavior.
    /// </summary>
    public class ErrorNotificationService : IErrorNotificationService
    {
        private readonly ILogger<ErrorNotificationService> _logger;
        private readonly IDialogService? _dialogService;
        private readonly ConcurrentQueue<ErrorNotification> _notificationQueue = new();
        private readonly ConcurrentDictionary<string, ErrorNotification> _activeNotifications = new();
        private readonly ObservableCollection<ErrorNotification> _statusBarNotifications = new();
        private readonly Timer _cleanupTimer;
        private bool _isProcessingQueue = false;

        /// <summary>
        /// Occurs when a new notification is available for display.
        /// </summary>
        public event EventHandler<ErrorNotification>? NotificationAvailable;

        /// <summary>
        /// Occurs when a notification is dismissed or expires.
        /// </summary>
        public event EventHandler<string>? NotificationDismissed;

        /// <summary>
        /// Gets the collection of status bar notifications for binding to UI.
        /// </summary>
        public ObservableCollection<ErrorNotification> StatusBarNotifications => _statusBarNotifications;

        /// <summary>
        /// Gets or sets whether notifications are currently enabled.
        /// </summary>
        public bool NotificationsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of concurrent notifications.
        /// </summary>
        public int MaxConcurrentNotifications { get; set; } = 5;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorNotificationService"/> class.
        /// </summary>
        /// <param name="logger">The logger for recording notification events.</param>
        /// <param name="dialogService">Optional dialog service for modal notifications.</param>
        public ErrorNotificationService(ILogger<ErrorNotificationService> logger, IDialogService? dialogService = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dialogService = dialogService;

            // Set up cleanup timer to remove expired notifications
            _cleanupTimer = new Timer(CleanupExpiredNotifications, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

            _logger.LogInformation("Error notification service initialized");
        }

        /// <summary>
        /// Shows an error notification with platform-appropriate display method.
        /// </summary>
        /// <param name="notification">The notification to display.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ShowNotificationAsync(ErrorNotification notification)
        {
            if (!NotificationsEnabled || notification == null)
                return;

            try
            {
                _logger.LogDebug("Showing notification {NotificationId} of type {NotificationType}", 
                    notification.Id, notification.NotificationType);

                // Add to active notifications
                _activeNotifications.TryAdd(notification.Id, notification);

                switch (notification.NotificationType)
                {
                    case ErrorNotificationType.Toast:
                        await ShowToastNotificationAsync(notification);
                        break;

                    case ErrorNotificationType.Modal:
                        await ShowModalNotificationAsync(notification);
                        break;

                    case ErrorNotificationType.Inline:
                        ShowInlineNotification(notification);
                        break;

                    case ErrorNotificationType.StatusBar:
                        ShowStatusBarNotification(notification);
                        break;

                    case ErrorNotificationType.Banner:
                        ShowBannerNotification(notification);
                        break;

                    default:
                        await ShowToastNotificationAsync(notification);
                        break;
                }

                // Auto-dismiss if configured
                if (notification.AutoDismiss && notification.Duration > TimeSpan.Zero)
                {
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(notification.Duration);
                        await DismissNotificationAsync(notification.Id);
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show notification {NotificationId}", notification.Id);
            }
        }

        /// <summary>
        /// Queues multiple notifications for display with intelligent batching.
        /// </summary>
        /// <param name="notifications">The notifications to queue.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task QueueNotificationsAsync(IEnumerable<ErrorNotification> notifications)
        {
            if (!NotificationsEnabled || notifications == null)
                return;

            var notificationList = notifications.ToList();
            _logger.LogDebug("Queueing {Count} notifications", notificationList.Count);

            // Sort by priority (highest first)
            var sortedNotifications = notificationList.OrderByDescending(n => n.Priority).ToList();

            foreach (var notification in sortedNotifications)
            {
                _notificationQueue.Enqueue(notification);
            }

            // Process queue if not already processing
            if (!_isProcessingQueue)
            {
                await ProcessNotificationQueueAsync();
            }
        }

        /// <summary>
        /// Dismisses a specific notification by ID.
        /// </summary>
        /// <param name="notificationId">The ID of the notification to dismiss.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task DismissNotificationAsync(string notificationId)
        {
            if (string.IsNullOrEmpty(notificationId))
                return Task.CompletedTask;

            try
            {
                if (_activeNotifications.TryRemove(notificationId, out var notification))
                {
                    notification.IsDismissed = true;

                    // Remove from status bar if it's there
                    var statusBarNotification = _statusBarNotifications.FirstOrDefault(n => n.Id == notificationId);
                    if (statusBarNotification != null)
                    {
                        if (Application.Current?.Dispatcher != null)
                        {
                            Application.Current.Dispatcher.Dispatch(() =>
                                _statusBarNotifications.Remove(statusBarNotification));
                        }
                        else
                        {
                            _statusBarNotifications.Remove(statusBarNotification);
                        }
                    }

                    NotificationDismissed?.Invoke(this, notificationId);
                    _logger.LogDebug("Dismissed notification {NotificationId}", notificationId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dismiss notification {NotificationId}", notificationId);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Dismisses all active notifications.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DismissAllNotificationsAsync()
        {
            _logger.LogDebug("Dismissing all active notifications");

            var notificationIds = _activeNotifications.Keys.ToList();
            foreach (var id in notificationIds)
            {
                await DismissNotificationAsync(id);
            }

            // Clear the queue as well
            while (_notificationQueue.TryDequeue(out _)) { }
        }

        /// <summary>
        /// Gets all currently active notifications.
        /// </summary>
        /// <returns>A collection of active notifications.</returns>
        public IEnumerable<ErrorNotification> GetActiveNotifications()
        {
            return _activeNotifications.Values.ToList();
        }

        /// <summary>
        /// Shows a quick toast message with Jarvis styling.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="duration">Optional duration (default: 3 seconds).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ShowToastAsync(string message, TimeSpan? duration = null)
        {
            var notification = ErrorNotification.CreateToast(message, "Jarvis Assistant", duration);
            await ShowNotificationAsync(notification);
        }

        /// <summary>
        /// Shows a modal dialog with Jarvis styling.
        /// </summary>
        /// <param name="title">The dialog title.</param>
        /// <param name="message">The dialog message.</param>
        /// <param name="actions">Optional custom actions.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ShowModalAsync(string title, string message, List<ErrorNotificationAction>? actions = null)
        {
            var notification = ErrorNotification.CreateModal(message, title, actions);
            await ShowNotificationAsync(notification);
        }

        #region Private Methods

        /// <summary>
        /// Processes the notification queue with rate limiting and batching.
        /// </summary>
        private async Task ProcessNotificationQueueAsync()
        {
            if (_isProcessingQueue) return;

            _isProcessingQueue = true;
            try
            {
                while (_notificationQueue.TryDequeue(out var notification))
                {
                    // Respect maximum concurrent notifications
                    while (_activeNotifications.Count >= MaxConcurrentNotifications)
                    {
                        await Task.Delay(500); // Wait before checking again
                    }

                    await ShowNotificationAsync(notification);

                    // Small delay between notifications to prevent UI overwhelming
                    await Task.Delay(100);
                }
            }
            finally
            {
                _isProcessingQueue = false;
            }
        }

        /// <summary>
        /// Shows a toast notification with platform-specific implementation.
        /// </summary>
        private async Task ShowToastNotificationAsync(ErrorNotification notification)
        {
            try
            {
                // Platform-specific toast implementation would go here
                // For now, we'll use the dialog service if available
                if (_dialogService != null)
                {
                    // For desktop, show as a brief alert
                    if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
                    {
                        // Don't await - let it show and auto-dismiss
                        _ = Task.Run(async () =>
                        {
                            await _dialogService.DisplayAlertAsync(notification.Title, notification.Message, "OK");
                        });
                    }
                }

                // Also raise event for custom toast implementations
                NotificationAvailable?.Invoke(this, notification);

                _logger.LogDebug("Toast notification displayed: {Message}", notification.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show toast notification");
            }
        }

        /// <summary>
        /// Shows a modal notification dialog.
        /// </summary>
        private async Task ShowModalNotificationAsync(ErrorNotification notification)
        {
            try
            {
                if (_dialogService != null)
                {
                    if (notification.Actions.Count > 1)
                    {
                        // Show action sheet for multiple actions
                        var actionTexts = notification.Actions.Select(a => a.Text).ToArray();
                        var result = await _dialogService.DisplayActionSheetAsync(
                            notification.Title,
                            "Cancel",
                            null,
                            actionTexts);

                        // Execute the selected action
                        var selectedAction = notification.Actions.FirstOrDefault(a => a.Text == result);
                        if (selectedAction?.Action != null)
                        {
                            await selectedAction.Action();
                        }
                    }
                    else
                    {
                        // Simple alert
                        await _dialogService.DisplayAlertAsync(
                            notification.Title,
                            notification.Message,
                            notification.Actions.FirstOrDefault()?.Text ?? "OK");

                        // Execute the action if available
                        var action = notification.Actions.FirstOrDefault()?.Action;
                        if (action != null)
                        {
                            await action();
                        }
                    }
                }
                else
                {
                    // Fallback to event-based notification
                    NotificationAvailable?.Invoke(this, notification);
                }

                _logger.LogDebug("Modal notification displayed: {Title}", notification.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show modal notification");
            }
        }

        /// <summary>
        /// Shows an inline notification within content areas.
        /// </summary>
        private void ShowInlineNotification(ErrorNotification notification)
        {
            try
            {
                // Inline notifications are handled by UI components via events
                NotificationAvailable?.Invoke(this, notification);
                _logger.LogDebug("Inline notification raised: {Message}", notification.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show inline notification");
            }
        }

        /// <summary>
        /// Shows a notification in the status bar.
        /// </summary>
        private void ShowStatusBarNotification(ErrorNotification notification)
        {
            try
            {
                if (Application.Current?.Dispatcher != null)
                {
                    Application.Current.Dispatcher.Dispatch(() =>
                    {
                        _statusBarNotifications.Add(notification);
                        
                        // Keep only the most recent status bar notifications (max 3)
                        while (_statusBarNotifications.Count > 3)
                        {
                            _statusBarNotifications.RemoveAt(0);
                        }
                    });
                }
                else
                {
                    _statusBarNotifications.Add(notification);
                }

                _logger.LogDebug("Status bar notification added: {Message}", notification.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show status bar notification");
            }
        }

        /// <summary>
        /// Shows a banner notification at the top of the screen.
        /// </summary>
        private void ShowBannerNotification(ErrorNotification notification)
        {
            try
            {
                // Banner notifications are handled by UI components via events
                NotificationAvailable?.Invoke(this, notification);
                _logger.LogDebug("Banner notification raised: {Message}", notification.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show banner notification");
            }
        }

        /// <summary>
        /// Cleans up expired notifications.
        /// </summary>
        private void CleanupExpiredNotifications(object? state)
        {
            try
            {
                var expiredIds = new List<string>();
                var cutoffTime = DateTime.UtcNow - TimeSpan.FromMinutes(10); // Clean up notifications older than 10 minutes

                foreach (var kvp in _activeNotifications)
                {
                    if (kvp.Value.CreatedAt < cutoffTime || kvp.Value.IsDismissed)
                    {
                        expiredIds.Add(kvp.Key);
                    }
                }

                foreach (var id in expiredIds)
                {
                    _ = DismissNotificationAsync(id);
                }

                if (expiredIds.Count > 0)
                {
                    _logger.LogDebug("Cleaned up {Count} expired notifications", expiredIds.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during notification cleanup");
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
            _activeNotifications.Clear();
            _statusBarNotifications.Clear();
        }

        #endregion
    }

    /// <summary>
    /// Interface for the error notification service.
    /// </summary>
    public interface IErrorNotificationService : IDisposable
    {
        event EventHandler<ErrorNotification>? NotificationAvailable;
        event EventHandler<string>? NotificationDismissed;
        ObservableCollection<ErrorNotification> StatusBarNotifications { get; }
        bool NotificationsEnabled { get; set; }
        int MaxConcurrentNotifications { get; set; }

        Task ShowNotificationAsync(ErrorNotification notification);
        Task QueueNotificationsAsync(IEnumerable<ErrorNotification> notifications);
        Task DismissNotificationAsync(string notificationId);
        Task DismissAllNotificationsAsync();
        IEnumerable<ErrorNotification> GetActiveNotifications();
        Task ShowToastAsync(string message, TimeSpan? duration = null);
        Task ShowModalAsync(string title, string message, List<ErrorNotificationAction>? actions = null);
    }
}
