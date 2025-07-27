using JarvisAssistant.MAUI.ViewModels;

namespace JarvisAssistant.MAUI.Views;

public partial class VoicePage : ContentPage
{
    public VoicePage(VoiceViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
