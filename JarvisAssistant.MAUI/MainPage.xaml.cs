using JarvisAssistant.MAUI.Views;
using JarvisAssistant.MAUI.ViewModels;
using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Services.Extensions;
using Microsoft.Extensions.Logging;

namespace JarvisAssistant.MAUI;

public partial class MainPage : ContentPage
{
    private readonly ILogger<MainPage>? _logger;
    private readonly IStatusMonitorService? _statusMonitorService;

    public MainPage()
    {
        InitializeComponent();
        
        // Add debug output for constructor
        System.Diagnostics.Debug.WriteLine("=== MainPage Constructor Started ===");
        
        // Get services from DI if available
        try
        {
            var services = Application.Current?.Handler?.MauiContext?.Services;
            _logger = services?.GetService(typeof(ILogger<MainPage>)) as ILogger<MainPage>;
            _statusMonitorService = services?.GetService(typeof(IStatusMonitorService)) as IStatusMonitorService;
            
            // Initialize status panel with ViewModel
            var statusPanelViewModel = services?.GetService(typeof(StatusPanelViewModel)) as StatusPanelViewModel;
            if (statusPanelViewModel != null && StatusPanel != null)
            {
                StatusPanel.BindingContext = statusPanelViewModel;
                System.Diagnostics.Debug.WriteLine("✅ StatusPanel BindingContext set to StatusPanelViewModel");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ StatusPanelViewModel or StatusPanel is null. ViewModel: {statusPanelViewModel != null}, Panel: {StatusPanel != null}");
                
                // Create a fallback ViewModel if DI isn't working
                if (StatusPanel != null && statusPanelViewModel == null)
                {
                    var fallbackLogger = services?.GetService(typeof(ILogger<StatusPanelViewModel>)) as ILogger<StatusPanelViewModel>;
                    var dialogService = services?.GetService(typeof(IDialogService)) as IDialogService;
                    
                    statusPanelViewModel = new StatusPanelViewModel(_statusMonitorService, dialogService, fallbackLogger);
                    StatusPanel.BindingContext = statusPanelViewModel;
                    System.Diagnostics.Debug.WriteLine("✅ Created fallback StatusPanelViewModel");
                }
            }
            
            System.Diagnostics.Debug.WriteLine("✅ Services initialized");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Error getting services: {ex}");
        }

        // Update status based on system state
        UpdateSystemStatus();
        
        System.Diagnostics.Debug.WriteLine("=== MainPage Constructor Completed ===");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await InitializeStatusMonitoringAsync();
        UpdateSystemStatus();
        _logger?.LogInformation("MainPage appeared");
        
        // Test that buttons are accessible
        TestButtonAvailability();
    }

    private async Task InitializeStatusMonitoringAsync()
    {
        try
        {
            if (_statusMonitorService != null)
            {
                // Start monitoring all registered services
                await _statusMonitorService.StartMonitoringAllAsync();
                _logger?.LogInformation("Status monitoring started");
            }
            
            // Also setup status monitoring if available
            var services = Application.Current?.Handler?.MauiContext?.Services;
            var statusSetup = services?.GetService(typeof(IStatusMonitoringSetup)) as IStatusMonitoringSetup;
            if (statusSetup != null)
            {
                await statusSetup.SetupAsync();
                _logger?.LogInformation("Status monitoring setup completed");
            }
            else
            {
                _logger?.LogWarning("IStatusMonitoringSetup service not found");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize status monitoring");
        }
    }

    private void UpdateSystemStatus()
    {
        try
        {
            // Update the status indicator - could check actual system status here
            if (StatusLabel != null)
            {
                StatusLabel.Text = "System Online";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating system status: {ex}");
        }
    }

    private async void OnStartChatClicked(object sender, EventArgs e)
    {
        // Add immediate debug output
        System.Diagnostics.Debug.WriteLine("🔥🔥🔥 START CHAT BUTTON CLICKED - EVENT IS FIRING! 🔥🔥🔥");
        
        try
        {
            _logger?.LogInformation("=== Start Chat Button Clicked ===");
            System.Diagnostics.Debug.WriteLine("=== Start Chat Button Clicked ===");
            
            // Disable button temporarily to prevent double-clicks
            if (sender is Button button)
            {
                button.IsEnabled = false;
                System.Diagnostics.Debug.WriteLine("Button disabled temporarily");
            }

            // Ensure we're on the main thread
            if (!MainThread.IsMainThread)
            {
                System.Diagnostics.Debug.WriteLine("Not on main thread - dispatching to main thread");
                MainThread.BeginInvokeOnMainThread(async () => await NavigateToChatPageDirectly());
                return;
            }

            await NavigateToChatPageDirectly();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in OnStartChatClicked");
            System.Diagnostics.Debug.WriteLine($"ERROR in OnStartChatClicked: {ex}");
            await DisplayAlert("Navigation Error", $"Failed to navigate to chat: {ex.Message}", "OK");
        }
        finally
        {
            // Re-enable button
            if (sender is Button button)
            {
                button.IsEnabled = true;
                System.Diagnostics.Debug.WriteLine("Button re-enabled");
            }
        }
    }

    private async Task NavigateToChatPageDirectly()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("=== NavigateToChatPageDirectly Started ===");
            
            // Check if Shell.Current is available
            if (Shell.Current == null)
            {
                System.Diagnostics.Debug.WriteLine("ERROR: Shell.Current is null");
                await DisplayAlert("Navigation Error", "Navigation system not available. Please restart the app.", "OK");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Shell.Current available: {Shell.Current.GetType().Name}");
            
            // Direct navigation to ChatPage - this route is registered in AppShell.xaml.cs
            System.Diagnostics.Debug.WriteLine("Attempting direct navigation to ChatPage");
            await Shell.Current.GoToAsync("ChatPage");
            
            System.Diagnostics.Debug.WriteLine("SUCCESS: Navigation completed to ChatPage");
            _logger?.LogInformation("Successfully navigated to ChatPage");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR in NavigateToChatPageDirectly: {ex}");
            System.Diagnostics.Debug.WriteLine($"Exception type: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            
            _logger?.LogError(ex, "Failed to navigate to ChatPage");
            
            // Show detailed error message to help diagnose the issue
            await DisplayAlert("Navigation Failed", 
                $"Unable to navigate to the chat page.\n\n" +
                $"Error: {ex.Message}\n\n" +
                $"This might be due to:\n" +
                $"• Missing route registration\n" +
                $"• ChatPage constructor issues\n" +
                $"• Dependency injection problems\n\n" +
                $"Check the debug output for details.", "OK");
        }
    }

    private async void OnVoiceDemoClicked(object sender, EventArgs e)
    {
        // Add immediate debug output
        System.Diagnostics.Debug.WriteLine("🔥🔥🔥 VOICE DEMO BUTTON CLICKED - EVENT IS FIRING! 🔥🔥🔥");
        
        try
        {
            _logger?.LogInformation("Navigating to voice demo page");
            System.Diagnostics.Debug.WriteLine("=== Voice Demo Button Clicked ===");
            
            await Shell.Current.GoToAsync("VoiceDemoPage");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error navigating to voice demo page");
            System.Diagnostics.Debug.WriteLine($"ERROR navigating to voice demo: {ex}");
            await DisplayAlert("Error", "Failed to open voice demo. Please try again.", "OK");
        }
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        // Add immediate debug output
        System.Diagnostics.Debug.WriteLine("🔥🔥🔥 SETTINGS BUTTON CLICKED - EVENT IS FIRING! 🔥🔥🔥");
        
        try
        {
            _logger?.LogInformation("Settings clicked");
            System.Diagnostics.Debug.WriteLine("=== Settings Button Clicked ===");
            
            // Show the settings coming soon message
            await DisplayAlert("Settings", "Settings page coming soon!", "OK");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in settings");
            System.Diagnostics.Debug.WriteLine($"ERROR in settings: {ex}");
            await DisplayAlert("Error", "Failed to open settings. Please try again.", "OK");
        }
    }

    private void TestButtonAvailability()
    {
        System.Diagnostics.Debug.WriteLine("=== Testing Button Availability ===");
        
        if (StartChatBtn != null)
        {
            System.Diagnostics.Debug.WriteLine($"✅ StartChatBtn found - Enabled: {StartChatBtn.IsEnabled}, Visible: {StartChatBtn.IsVisible}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("❌ StartChatBtn is NULL!");
        }
        
        if (VoiceDemoBtn != null)
        {
            System.Diagnostics.Debug.WriteLine($"✅ VoiceDemoBtn found - Enabled: {VoiceDemoBtn.IsEnabled}, Visible: {VoiceDemoBtn.IsVisible}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("❌ VoiceDemoBtn is NULL!");
        }
        
        if (SettingsBtn != null)
        {
            System.Diagnostics.Debug.WriteLine($"✅ SettingsBtn found - Enabled: {SettingsBtn.IsEnabled}, Visible: {SettingsBtn.IsVisible}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("❌ SettingsBtn is NULL!");
        }
        
        System.Diagnostics.Debug.WriteLine("=== Button Availability Test Complete ===");
    }
}

