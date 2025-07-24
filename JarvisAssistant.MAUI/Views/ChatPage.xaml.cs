using JarvisAssistant.MAUI.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace JarvisAssistant.MAUI.Views;

public partial class ChatPage : ContentPage
{
    private readonly ChatViewModel _viewModel;

    public ChatPage() : this(null)
    {
        // Parameterless constructor for MAUI routing
        System.Diagnostics.Debug.WriteLine("=== ChatPage Parameterless Constructor Called ===");
    }

    public ChatPage(ChatViewModel? viewModel = null)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("=== ChatPage Constructor Started ===");
            
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("? InitializeComponent completed");
            
            // Get or create ViewModel
            _viewModel = GetOrCreateViewModel(viewModel);
            BindingContext = _viewModel;
            System.Diagnostics.Debug.WriteLine($"? BindingContext set to: {_viewModel.GetType().Name}");

            // Setup messaging and platform behaviors
            SetupMessaging();
            SetupPlatformBehaviors();
            
            System.Diagnostics.Debug.WriteLine("=== ChatPage Constructor Completed Successfully ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? CRITICAL ERROR in ChatPage constructor: {ex}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            
            // Try to create a minimal fallback to prevent total failure
            try
            {
                System.Diagnostics.Debug.WriteLine("Attempting to create fallback ViewModel...");
                _viewModel = CreateFallbackViewModel();
                BindingContext = _viewModel;
                System.Diagnostics.Debug.WriteLine("? Fallback ViewModel created successfully");
            }
            catch (Exception fallbackEx)
            {
                System.Diagnostics.Debug.WriteLine($"? Even fallback failed: {fallbackEx}");
                throw; // Re-throw if we can't even create a fallback
            }
        }
    }

    private ChatViewModel GetOrCreateViewModel(ChatViewModel? providedViewModel)
    {
        // If a viewmodel was provided directly, use it
        if (providedViewModel != null)
        {
            System.Diagnostics.Debug.WriteLine("? Using provided ChatViewModel");
            return providedViewModel;
        }

        // Try to get from DI container
        try
        {
            var services = Application.Current?.Handler?.MauiContext?.Services;
            if (services != null)
            {
                var diViewModel = services.GetService<ChatViewModel>();
                if (diViewModel != null)
                {
                    System.Diagnostics.Debug.WriteLine("? Got ChatViewModel from DI container");
                    return diViewModel;
                }
            }
            System.Diagnostics.Debug.WriteLine("?? Services container is null or ChatViewModel not registered");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"?? Error getting ChatViewModel from DI: {ex.Message}");
        }

        // Create fallback viewmodel
        System.Diagnostics.Debug.WriteLine("Creating fallback ChatViewModel");
        return CreateFallbackViewModel();
    }

    private ChatViewModel CreateFallbackViewModel()
    {
        System.Diagnostics.Debug.WriteLine("Creating fallback ChatViewModel with null services");
        // Create a viewmodel with null services - the viewmodel handles this gracefully
        return new ChatViewModel(null, null, null, null, null);
    }

    private void SetupMessaging()
    {
        try
        {
            // Subscribe to scroll messages using modern messaging
            WeakReferenceMessenger.Default.Register<string>(this, (recipient, message) =>
            {
                if (message == "ScrollToBottom")
                {
                    OnScrollToBottom(_viewModel);
                }
            });
            System.Diagnostics.Debug.WriteLine("? Messaging setup completed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"?? Error setting up messaging: {ex.Message}");
        }
    }

    private void SetupPlatformBehaviors()
    {
        try
        {
            if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
            {
                SetupDesktopBehaviors();
            }
            else if (DeviceInfo.Idiom == DeviceIdiom.Phone)
            {
                SetupMobileBehaviors();
            }
            else if (DeviceInfo.Idiom == DeviceIdiom.TV)
            {
                SetupTVBehaviors();
            }
            System.Diagnostics.Debug.WriteLine($"? Platform behaviors setup for {DeviceInfo.Idiom}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"?? Error setting up platform behaviors: {ex.Message}");
        }
    }

    private void SetupDesktopBehaviors()
    {
        // Desktop-specific setup
        this.Loaded += (s, e) =>
        {
            // Focus on the input field when loaded
            if (FindByName("ChatCollectionView") != null)
            {
                // Additional desktop-specific initialization
            }
        };
    }

    private void SetupMobileBehaviors()
    {
        try
        {
            // Add swipe gestures
            var swipeLeft = new SwipeGestureRecognizer
            {
                Direction = SwipeDirection.Left,
                Command = _viewModel.ClearConversationCommand
            };

            var swipeRight = new SwipeGestureRecognizer
            {
                Direction = SwipeDirection.Right,
                Command = _viewModel.RefreshConversationCommand
            };

            var swipeUp = new SwipeGestureRecognizer
            {
                Direction = SwipeDirection.Up,
                Command = _viewModel.ToggleVoiceModeCommand
            };

            var mobileLayout = this.FindByName("MobileLayout") as Grid;
            if (mobileLayout != null)
            {
                mobileLayout.GestureRecognizers.Add(swipeLeft);
                mobileLayout.GestureRecognizers.Add(swipeRight);
                mobileLayout.GestureRecognizers.Add(swipeUp);
            }

            // Haptic feedback for interactions
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) =>
            {
                try
                {
                    // Platform-specific haptic feedback implementation
#if ANDROID
                    // Android haptic feedback would go here
#elif IOS
                    // iOS haptic feedback would go here
#endif
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Haptic feedback error: {ex.Message}");
                }
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting up mobile behaviors: {ex}");
        }
    }

    private void SetupTVBehaviors()
    {
        try
        {
            // TV remote navigation
            this.Focused += (s, e) =>
            {
                // Auto-activate voice mode on focus
                if (!_viewModel.IsVoiceModeActive)
                {
                    _viewModel.ToggleVoiceModeCommand.Execute(null);
                }
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting up TV behaviors: {ex}");
        }
    }

    private void OnScrollToBottom(ChatViewModel viewModel)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                CollectionView? chatView = null;
                
                if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
                    chatView = this.FindByName("ChatCollectionView") as CollectionView;
                else if (DeviceInfo.Idiom == DeviceIdiom.Phone)
                    chatView = this.FindByName("MobileChatCollectionView") as CollectionView;
                else if (DeviceInfo.Idiom == DeviceIdiom.TV)
                    chatView = this.FindByName("TVChatCollectionView") as CollectionView;

                if (chatView?.ItemsSource is IEnumerable<object> items && items.Any())
                {
                    var lastItem = items.Last();
                    await Task.Delay(100); // Allow UI to update
                    chatView.ScrollTo(lastItem, animate: true);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Scroll error: {ex.Message}");
            }
        });
    }

    protected override void OnAppearing()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("=== ChatPage OnAppearing ===");
            base.OnAppearing();
            
            // Start any necessary background tasks
            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(500); // Allow UI to settle
                    
                    // Update voice activity level periodically when voice mode is active
                    while (_viewModel.IsVoiceModeActive)
                    {
                        // Simulate voice activity level (in real implementation, this would come from voice service)
                        var random = new Random();
                        var level = _viewModel.IsListening ? random.NextDouble() * 0.8 + 0.2 : 0.1;
                        
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            _viewModel.UpdateVoiceActivityLevel(level);
                        });

                        await Task.Delay(100);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in background task: {ex}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnAppearing: {ex}");
        }
    }

    protected override void OnDisappearing()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("=== ChatPage OnDisappearing ===");
            base.OnDisappearing();
            
            // Cleanup using modern messaging
            WeakReferenceMessenger.Default.Unregister<string>(this);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnDisappearing: {ex}");
        }
    }
}
