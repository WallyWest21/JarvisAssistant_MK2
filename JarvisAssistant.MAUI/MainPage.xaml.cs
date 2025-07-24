using JarvisAssistant.MAUI.Views;
using Microsoft.Extensions.Logging;

namespace JarvisAssistant.MAUI;

public partial class MainPage : ContentPage
{
    private readonly ILogger<MainPage>? _logger;

    public MainPage()
    {
        InitializeComponent();
        
        // Get logger from DI if available
        try
        {
            var services = Application.Current?.Handler?.MauiContext?.Services;
            _logger = services?.GetService(typeof(ILogger<MainPage>)) as ILogger<MainPage>;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting logger: {ex}");
        }

        // Update status based on system state
        UpdateSystemStatus();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateSystemStatus();
        _logger?.LogInformation("MainPage appeared");
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
            _logger?.LogInformation("Navigating to chat page");
            await Shell.Current.GoToAsync("//ChatPage");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error navigating to chat page");
            await DisplayAlert("Error", "Failed to start chat. Please try again.", "OK");
        }
    }

    private async void OnVoiceDemoClicked(object sender, EventArgs e)
    {
        try
        {
            _logger?.LogInformation("Navigating to voice demo page");
            await Shell.Current.GoToAsync("//VoiceDemoPage");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error navigating to voice demo page");
            await DisplayAlert("Error", "Failed to open voice demo. Please try again.", "OK");
        }
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        try
        {
            _logger?.LogInformation("Settings clicked");
            // For now, show a placeholder alert - settings page can be implemented later
            await DisplayAlert("Settings", "Settings page coming soon!", "OK");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in settings");
            await DisplayAlert("Error", "Failed to open settings. Please try again.", "OK");
        }
    }
}

