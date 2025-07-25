using JarvisAssistant.MAUI.Models;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace JarvisAssistant.MAUI.Controls
{
    /// <summary>
    /// Error notification control that displays error notifications with Jarvis styling.
    /// Supports multiple notification types with platform-appropriate rendering.
    /// </summary>
    public partial class ErrorNotificationControl : ContentView, INotifyPropertyChanged
    {
        private readonly ILogger<ErrorNotificationControl>? _logger;
        private ErrorNotification? _notification;

        #region Bindable Properties

        /// <summary>
        /// Bindable property for the notification to display.
        /// </summary>
        public static readonly BindableProperty NotificationProperty =
            BindableProperty.Create(
                nameof(Notification),
                typeof(ErrorNotification),
                typeof(ErrorNotificationControl),
                null,
                propertyChanged: OnNotificationChanged);

        /// <summary>
        /// Bindable property for the dismiss command.
        /// </summary>
        public static readonly BindableProperty DismissCommandProperty =
            BindableProperty.Create(
                nameof(DismissCommand),
                typeof(ICommand),
                typeof(ErrorNotificationControl));

        /// <summary>
        /// Bindable property for enabling animations.
        /// </summary>
        public static readonly BindableProperty EnableAnimationsProperty =
            BindableProperty.Create(
                nameof(EnableAnimations),
                typeof(bool),
                typeof(ErrorNotificationControl),
                true);

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the notification to display.
        /// </summary>
        public ErrorNotification? Notification
        {
            get => (ErrorNotification?)GetValue(NotificationProperty);
            set => SetValue(NotificationProperty, value);
        }

        /// <summary>
        /// Gets or sets the command to execute when dismissing the notification.
        /// </summary>
        public ICommand? DismissCommand
        {
            get => (ICommand?)GetValue(DismissCommandProperty);
            set => SetValue(DismissCommandProperty, value);
        }

        /// <summary>
        /// Gets or sets whether animations are enabled.
        /// </summary>
        public bool EnableAnimations
        {
            get => (bool)GetValue(EnableAnimationsProperty);
            set => SetValue(EnableAnimationsProperty, value);
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorNotificationControl"/> class.
        /// </summary>
        public ErrorNotificationControl()
        {
            InitializeComponent();
            BindingContext = this;

            // Try to get logger from service provider if available
            _logger = Handler?.MauiContext?.Services?.GetService<ILogger<ErrorNotificationControl>>();

            // Set up default dismiss command
            DismissCommand = new Command<string>(OnDismissRequested);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Called when the notification property changes.
        /// </summary>
        private static void OnNotificationChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ErrorNotificationControl control)
            {
                control.OnNotificationChanged((ErrorNotification?)oldValue, (ErrorNotification?)newValue);
            }
        }

        /// <summary>
        /// Handles notification property changes.
        /// </summary>
        private void OnNotificationChanged(ErrorNotification? oldNotification, ErrorNotification? newNotification)
        {
            _notification = newNotification;

            if (newNotification != null)
            {
                UpdateNotificationPresentation();
            }
            else
            {
                NotificationPresenter.Content = null;
            }
        }

        /// <summary>
        /// Updates the notification presentation based on the notification type.
        /// </summary>
        private void UpdateNotificationPresentation()
        {
            if (_notification == null)
            {
                NotificationPresenter.Content = null;
                return;
            }

            try
            {
                // Select the appropriate data template based on notification type
                DataTemplate? template = _notification.NotificationType switch
                {
                    ErrorNotificationType.Toast => (DataTemplate)Resources["ToastNotificationTemplate"],
                    ErrorNotificationType.Inline => (DataTemplate)Resources["InlineNotificationTemplate"],
                    ErrorNotificationType.StatusBar => (DataTemplate)Resources["StatusBarNotificationTemplate"],
                    ErrorNotificationType.Banner => (DataTemplate)Resources["BannerNotificationTemplate"],
                    ErrorNotificationType.Modal => (DataTemplate)Resources["ToastNotificationTemplate"], // Modals use toast template
                    _ => (DataTemplate)Resources["InlineNotificationTemplate"]
                };

                if (template != null)
                {
                    var content = template.CreateContent();
                    if (content is View view)
                    {
                        view.BindingContext = _notification;
                        NotificationPresenter.Content = view;

                        // Apply entrance animation if enabled
                        if (EnableAnimations)
                        {
                            _ = Task.Run(async () =>
                            {
                                await AnimateEntrance(view);
                            });
                        }

                        _logger?.LogDebug("Updated notification presentation for type {NotificationType}", 
                            _notification.NotificationType);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to update notification presentation");
                
                // Fallback to simple label
                NotificationPresenter.Content = new Label
                {
                    Text = _notification.Message,
                    TextColor = Colors.White,
                    BackgroundColor = Colors.Red,
                    Padding = 10
                };
            }
        }

        /// <summary>
        /// Handles dismiss requests.
        /// </summary>
        private void OnDismissRequested(string? notificationId)
        {
            if (string.IsNullOrEmpty(notificationId) || _notification?.Id != notificationId)
                return;

            try
            {
                if (EnableAnimations && NotificationPresenter.Content is View view)
                {
                    _ = Task.Run(async () =>
                    {
                        await AnimateExit(view);
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            RaiseDismissEvent(notificationId);
                        });
                    });
                }
                else
                {
                    RaiseDismissEvent(notificationId);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error dismissing notification {NotificationId}", notificationId);
                RaiseDismissEvent(notificationId);
            }
        }

        /// <summary>
        /// Raises the dismiss event.
        /// </summary>
        private void RaiseDismissEvent(string notificationId)
        {
            NotificationDismissed?.Invoke(this, notificationId);
            
            // Clear the content
            NotificationPresenter.Content = null;
            _notification = null;
        }

        /// <summary>
        /// Animates the notification entrance.
        /// </summary>
        private async Task AnimateEntrance(View view)
        {
            try
            {
                // Set initial state
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    view.Opacity = 0;
                    view.TranslationY = -20;
                    view.Scale = 0.9;
                });

                // Animate to final state
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var tasks = new[]
                    {
                        view.FadeTo(1, 300, Easing.CubicOut),
                        view.TranslateTo(0, 0, 300, Easing.CubicOut),
                        view.ScaleTo(1, 300, Easing.CubicOut)
                    };

                    await Task.WhenAll(tasks);
                });
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Animation failed during entrance");
                
                // Ensure view is visible even if animation fails
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    view.Opacity = 1;
                    view.TranslationY = 0;
                    view.Scale = 1;
                });
            }
        }

        /// <summary>
        /// Animates the notification exit.
        /// </summary>
        private async Task AnimateExit(View view)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var tasks = new[]
                    {
                        view.FadeTo(0, 200, Easing.CubicIn),
                        view.TranslateTo(0, -10, 200, Easing.CubicIn),
                        view.ScaleTo(0.95, 200, Easing.CubicIn)
                    };

                    await Task.WhenAll(tasks);
                });
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Animation failed during exit");
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when a notification is dismissed.
        /// </summary>
        public event EventHandler<string>? NotificationDismissed;

        #endregion

        #region INotifyPropertyChanged

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected override void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            base.OnPropertyChanged(propertyName);
        }

        #endregion
    }

    /// <summary>
    /// Container for displaying multiple error notifications with automatic management.
    /// </summary>
    public class ErrorNotificationContainer : StackLayout
    {
        private readonly ILogger<ErrorNotificationContainer>? _logger;
        private readonly Dictionary<string, ErrorNotificationControl> _activeControls = new();

        #region Bindable Properties

        /// <summary>
        /// Bindable property for the notifications collection.
        /// </summary>
        public static readonly BindableProperty NotificationsProperty =
            BindableProperty.Create(
                nameof(Notifications),
                typeof(ObservableCollection<ErrorNotification>),
                typeof(ErrorNotificationContainer),
                null,
                propertyChanged: OnNotificationsChanged);

        /// <summary>
        /// Bindable property for the maximum number of visible notifications.
        /// </summary>
        public static readonly BindableProperty MaxVisibleNotificationsProperty =
            BindableProperty.Create(
                nameof(MaxVisibleNotifications),
                typeof(int),
                typeof(ErrorNotificationContainer),
                5);

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the notifications collection.
        /// </summary>
        public ObservableCollection<ErrorNotification>? Notifications
        {
            get => (ObservableCollection<ErrorNotification>?)GetValue(NotificationsProperty);
            set => SetValue(NotificationsProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum number of visible notifications.
        /// </summary>
        public int MaxVisibleNotifications
        {
            get => (int)GetValue(MaxVisibleNotificationsProperty);
            set => SetValue(MaxVisibleNotificationsProperty, value);
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorNotificationContainer"/> class.
        /// </summary>
        public ErrorNotificationContainer()
        {
            Spacing = 8;
            VerticalOptions = LayoutOptions.Start;
            
            // Try to get logger from service provider if available
            _logger = Handler?.MauiContext?.Services?.GetService<ILogger<ErrorNotificationContainer>>();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Called when the notifications collection changes.
        /// </summary>
        private static void OnNotificationsChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ErrorNotificationContainer container)
            {
                container.OnNotificationsChanged(
                    (ObservableCollection<ErrorNotification>?)oldValue,
                    (ObservableCollection<ErrorNotification>?)newValue);
            }
        }

        /// <summary>
        /// Handles notifications collection changes.
        /// </summary>
        private void OnNotificationsChanged(
            ObservableCollection<ErrorNotification>? oldCollection,
            ObservableCollection<ErrorNotification>? newCollection)
        {
            // Unsubscribe from old collection
            if (oldCollection != null)
            {
                oldCollection.CollectionChanged -= OnNotificationCollectionChanged;
            }

            // Subscribe to new collection
            if (newCollection != null)
            {
                newCollection.CollectionChanged += OnNotificationCollectionChanged;
                RefreshNotifications();
            }
            else
            {
                ClearAllNotifications();
            }
        }

        /// <summary>
        /// Handles changes to the notification collection.
        /// </summary>
        private void OnNotificationCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(RefreshNotifications);
        }

        /// <summary>
        /// Refreshes the displayed notifications.
        /// </summary>
        private void RefreshNotifications()
        {
            try
            {
                if (Notifications == null)
                {
                    ClearAllNotifications();
                    return;
                }

                // Remove notifications that are no longer in the collection
                var currentIds = Notifications.Select(n => n.Id).ToHashSet();
                var controlsToRemove = _activeControls.Keys.Where(id => !currentIds.Contains(id)).ToList();

                foreach (var id in controlsToRemove)
                {
                    RemoveNotificationControl(id);
                }

                // Add or update notifications
                var visibleNotifications = Notifications
                    .OrderByDescending(n => n.Priority)
                    .ThenByDescending(n => n.CreatedAt)
                    .Take(MaxVisibleNotifications)
                    .ToList();

                for (int i = 0; i < visibleNotifications.Count; i++)
                {
                    var notification = visibleNotifications[i];
                    
                    if (_activeControls.TryGetValue(notification.Id, out var existingControl))
                    {
                        // Update existing control
                        existingControl.Notification = notification;
                        
                        // Ensure correct position
                        var currentIndex = Children.IndexOf(existingControl);
                        if (currentIndex != i && currentIndex >= 0)
                        {
                            Children.RemoveAt(currentIndex);
                            Children.Insert(Math.Min(i, Children.Count), existingControl);
                        }
                    }
                    else
                    {
                        // Create new control
                        CreateNotificationControl(notification, i);
                    }
                }

                _logger?.LogDebug("Refreshed notifications: {Count} visible, {Total} total", 
                    visibleNotifications.Count, Notifications.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error refreshing notifications");
            }
        }

        /// <summary>
        /// Creates a new notification control.
        /// </summary>
        private void CreateNotificationControl(ErrorNotification notification, int index)
        {
            try
            {
                var control = new ErrorNotificationControl
                {
                    Notification = notification,
                    EnableAnimations = true
                };

                control.NotificationDismissed += OnNotificationDismissed;
                
                _activeControls[notification.Id] = control;
                Children.Insert(Math.Min(index, Children.Count), control);

                _logger?.LogDebug("Created notification control for {NotificationId}", notification.Id);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to create notification control for {NotificationId}", notification.Id);
            }
        }

        /// <summary>
        /// Removes a notification control.
        /// </summary>
        private void RemoveNotificationControl(string notificationId)
        {
            try
            {
                if (_activeControls.TryGetValue(notificationId, out var control))
                {
                    control.NotificationDismissed -= OnNotificationDismissed;
                    Children.Remove(control);
                    _activeControls.Remove(notificationId);

                    _logger?.LogDebug("Removed notification control for {NotificationId}", notificationId);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to remove notification control for {NotificationId}", notificationId);
            }
        }

        /// <summary>
        /// Clears all notification controls.
        /// </summary>
        private void ClearAllNotifications()
        {
            try
            {
                foreach (var control in _activeControls.Values)
                {
                    control.NotificationDismissed -= OnNotificationDismissed;
                }

                Children.Clear();
                _activeControls.Clear();

                _logger?.LogDebug("Cleared all notification controls");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error clearing notifications");
            }
        }

        /// <summary>
        /// Handles notification dismissal.
        /// </summary>
        private void OnNotificationDismissed(object? sender, string notificationId)
        {
            try
            {
                // Remove from collection if present
                var notification = Notifications?.FirstOrDefault(n => n.Id == notificationId);
                if (notification != null && Notifications != null)
                {
                    Notifications.Remove(notification);
                }

                // Remove control
                RemoveNotificationControl(notificationId);

                // Raise event
                NotificationDismissed?.Invoke(this, notificationId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error handling notification dismissal for {NotificationId}", notificationId);
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when a notification is dismissed.
        /// </summary>
        public event EventHandler<string>? NotificationDismissed;

        #endregion
    }
}
