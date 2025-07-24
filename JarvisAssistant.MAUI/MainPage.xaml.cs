using JarvisAssistant.MAUI.Views;
using JarvisAssistant.MAUI.ViewModels;
using JarvisAssistant.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace JarvisAssistant.MAUI;

public partial class MainPage : ContentPage
{
    private readonly ILogger<MainPage>? _logger;
    private readonly IStatusMonitorService? _statusMonitorService;

    public MainPage()
    {
        InitializeComponent();
        
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
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting services: {ex}");
        }

        // Update status based on system state
        UpdateSystemStatus();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await InitializeStatusMonitoringAsync();
        UpdateSystemStatus();
        _logger?.LogInformation("MainPage appeared");
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
        try
        {
            _logger?.LogInformation("Start Chat button clicked - beginning navigation");
            System.Diagnostics.Debug.WriteLine("=== Start Chat Button Clicked ===");
            
            // Disable button temporarily to prevent double-clicks
            if (sender is Button button)
            {
                button.IsEnabled = false;
                System.Diagnostics.Debug.WriteLine("Button disabled temporarily");
            }

            // Check if Shell.Current is available
            if (Shell.Current == null)
            {
                var errorMsg = "Shell.Current is null - navigation system not available";
                _logger?.LogError(errorMsg);
                System.Diagnostics.Debug.WriteLine($"ERROR: {errorMsg}");
                await DisplayAlert("Navigation Error", "The navigation system is not available. Please restart the application.", "OK");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Shell.Current available: {Shell.Current.GetType().Name}");
            
            // Check if we're on the UI thread
            if (!MainThread.IsMainThread)
            {
                System.Diagnostics.Debug.WriteLine("Not on main thread - dispatching to main thread");
                MainThread.BeginInvokeOnMainThread(async () => await NavigateToChatPage());
                return;
            }

            await NavigateToChatPage();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in OnStartChatClicked");
            System.Diagnostics.Debug.WriteLine($"ERROR in OnStartChatClicked: {ex}");
            await DisplayAlert("Error", $"Failed to navigate to chat: {ex.Message}", "OK");
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

    private async Task NavigateToChatPage()
    {
        System.Diagnostics.Debug.WriteLine("=== NavigateToChatPage Method Started ===");
        
        var navigationAttempts = new[]
        {
            "ChatPage"
            // Removed the problematic // routes that were causing issues
        };

        foreach (var route in navigationAttempts)
        {
            try
            {
                _logger?.LogInformation($"Attempting navigation to: {route}");
                System.Diagnostics.Debug.WriteLine($"Attempting navigation to: {route}");
                
                // Check if Shell.Current is still available
                if (Shell.Current == null)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: Shell.Current became null during navigation");
                    throw new InvalidOperationException("Shell.Current is null");
                }
                
                System.Diagnostics.Debug.WriteLine($"Shell.Current type: {Shell.Current.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Current page: {Shell.Current.CurrentPage?.GetType().Name ?? "NULL"}");
                
                // Try the navigation
                await Shell.Current.GoToAsync(route);
                
                _logger?.LogInformation($"Navigation successful to: {route}");
                System.Diagnostics.Debug.WriteLine($"SUCCESS: Navigation completed to {route}");
                
                // Verify we actually navigated
                await Task.Delay(100); // Give time for navigation to complete
                System.Diagnostics.Debug.WriteLine($"After navigation, current page: {Shell.Current.CurrentPage?.GetType().Name ?? "NULL"}");
                
                return; // Success - exit the loop
            }
            catch (Exception navEx)
            {
                _logger?.LogWarning(navEx, $"Navigation attempt failed for route: {route}");
                System.Diagnostics.Debug.WriteLine($"Navigation attempt failed for {route}: {navEx.Message}");
                System.Diagnostics.Debug.WriteLine($"Exception type: {navEx.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {navEx.StackTrace}");
                
                // Continue to next route attempt
                continue;
            }
        }

        // If we get here, all navigation attempts failed
        var finalError = "All navigation attempts failed. Please check if the ChatPage is properly registered in AppShell and can be instantiated.";
        _logger?.LogError(finalError);
        System.Diagnostics.Debug.WriteLine($"FINAL ERROR: {finalError}");
        
        // Show user-friendly error dialog
        await DisplayAlert("Navigation Failed", 
            "Unable to open the chat page. This might be due to:\n" +
            "1. Missing dependencies\n" +
            "2. Route registration issues\n" +
            "3. Page construction problems\n\n" +
            "Please check the debug output for details.", "OK");
    }

    private async void OnVoiceDemoClicked(object sender, EventArgs e)
    {
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
        try
        {
            _logger?.LogInformation("Settings clicked");
            System.Diagnostics.Debug.WriteLine("=== Settings Button Clicked ===");
            // For now, show a placeholder alert - settings page can be implemented later
            await DisplayAlert("Settings", "Settings page coming soon!", "OK");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in settings");
            System.Diagnostics.Debug.WriteLine($"ERROR in settings: {ex}");
            await DisplayAlert("Error", "Failed to open settings. Please try again.", "OK");
        }
    }
}

