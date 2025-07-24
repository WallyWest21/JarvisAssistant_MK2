using JarvisAssistant.MAUI.ViewModels;

namespace JarvisAssistant.MAUI.Views
{
    /// <summary>
    /// Service status panel view for real-time monitoring display.
    /// </summary>
    public partial class StatusPanelView : ContentView
    {
        public StatusPanelView()
        {
            InitializeComponent();
        }

        public StatusPanelView(StatusPanelViewModel viewModel) : this()
        {
            BindingContext = viewModel;
        }
    }
}
