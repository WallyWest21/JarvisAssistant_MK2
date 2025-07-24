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
            System.Diagnostics.Debug.WriteLine("=== StatusPanelView constructor called ===");
        }

        public StatusPanelView(StatusPanelViewModel viewModel) : this()
        {
            BindingContext = viewModel;
            System.Diagnostics.Debug.WriteLine($"=== StatusPanelView constructor with ViewModel called. ViewModel: {viewModel != null} ===");
        }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();
            System.Diagnostics.Debug.WriteLine($"=== StatusPanelView BindingContext changed. New context type: {BindingContext?.GetType()?.Name ?? "null"} ===");
            
            if (BindingContext is StatusPanelViewModel vm)
            {
                System.Diagnostics.Debug.WriteLine($"=== StatusPanelView bound to StatusPanelViewModel. ServiceStatuses count: {vm.ServiceStatuses.Count} ===");
                System.Diagnostics.Debug.WriteLine($"=== ViewModel IsExpanded: {vm.IsExpanded} ===");
                
                // Test if commands are available
                System.Diagnostics.Debug.WriteLine($"=== ToggleExpandedCommand available: {vm.ToggleExpandedCommand != null} ===");
                System.Diagnostics.Debug.WriteLine($"=== TestCommandCommand available: {vm.TestCommandCommand != null} ===");
                
                // Test command execution manually
                try
                {
                    if (vm.ToggleExpandedCommand?.CanExecute(null) == true)
                    {
                        System.Diagnostics.Debug.WriteLine("=== ToggleExpandedCommand can execute ===");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("=== ToggleExpandedCommand cannot execute ===");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"=== Error testing ToggleExpandedCommand: {ex} ===");
                }
            }
        }

        private void OnTestButtonClicked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("?????? RED TEST BUTTON CLICKED EVENT FIRED! ??????");
            
            if (BindingContext is StatusPanelViewModel vm)
            {
                System.Diagnostics.Debug.WriteLine("=== Calling TestCommand directly from click event ===");
                vm.TestCommand();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("=== BindingContext is not StatusPanelViewModel ===");
            }
        }

        private void OnToggleButtonClicked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("?????? BLUE TOGGLE BUTTON CLICKED EVENT FIRED! ??????");
            
            if (BindingContext is StatusPanelViewModel vm)
            {
                System.Diagnostics.Debug.WriteLine("=== Calling ToggleExpanded directly from click event ===");
                vm.ToggleExpanded();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("=== BindingContext is not StatusPanelViewModel ===");
            }
        }
    }
}
