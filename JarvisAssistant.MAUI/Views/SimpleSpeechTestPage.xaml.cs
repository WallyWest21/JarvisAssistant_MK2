using JarvisAssistant.MAUI.ViewModels;

namespace JarvisAssistant.MAUI.Views;

public partial class SimpleSpeechTestPage : ContentPage
{
    public SimpleSpeechTestPage(SimpleSpeechTestViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
