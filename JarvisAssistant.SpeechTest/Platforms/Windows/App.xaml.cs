using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace JarvisAssistant.SpeechTest.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            try
            {
                this.InitializeComponent();
            }
            catch (System.Exception ex)
            {
                // Log the error for debugging
                System.Diagnostics.Debug.WriteLine($"App initialization failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Try to continue without crashing
                // The app might still work for basic functionality
            }
        }

        protected override MauiApp CreateMauiApp()
        {
            try
            {
                return MauiProgram.CreateMauiApp();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MauiApp creation failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw; // Re-throw since we can't continue without the MauiApp
            }
        }
    }
}
