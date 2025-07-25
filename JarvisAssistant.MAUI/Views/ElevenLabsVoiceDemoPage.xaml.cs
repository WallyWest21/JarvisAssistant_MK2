using JarvisAssistant.MAUI.ViewModels;

namespace JarvisAssistant.MAUI.Views;

/// <summary>
/// Page for demonstrating ElevenLabs voice synthesis capabilities.
/// </summary>
public partial class ElevenLabsVoiceDemoPage : ContentPage
{
    public ElevenLabsVoiceDemoPage(ElevenLabsVoiceDemoViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
