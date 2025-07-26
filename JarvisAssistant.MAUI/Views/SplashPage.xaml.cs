using JarvisAssistant.MAUI.ViewModels;

namespace JarvisAssistant.MAUI.Views;

/// <summary>
/// Splash page shown during application startup.
/// </summary>
public partial class SplashPage : ContentPage
{
    public SplashPage(SplashViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
