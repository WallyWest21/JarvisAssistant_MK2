using JarvisAssistant.SpeechTest.ViewModels;

namespace JarvisAssistant.SpeechTest
{
    public partial class MainPage : ContentPage
    {
        public MainPage(SpeechTestViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
