using JarvisAssistant.MAUI.Views;

namespace JarvisAssistant.MAUI;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		RegisterRoutes();
	}

	private static void RegisterRoutes()
	{
		try
		{
			System.Diagnostics.Debug.WriteLine("=== Starting Route Registration ===");
			
			// Register routes for navigation with explicit type registration
			Routing.RegisterRoute("ChatPage", typeof(ChatPage));
			Routing.RegisterRoute("VoiceDemoPage", typeof(VoiceDemoPage));
			
			// Alternative route names for compatibility - REMOVE the // prefix
			// These were causing issues with navigation
			System.Diagnostics.Debug.WriteLine("Routes registered successfully:");
			System.Diagnostics.Debug.WriteLine("- ChatPage -> " + typeof(ChatPage).FullName);
			System.Diagnostics.Debug.WriteLine("- VoiceDemoPage -> " + typeof(VoiceDemoPage).FullName);
			
			// Test route registration by checking if they exist
			var chatPageType = Routing.GetOrCreateContent("ChatPage");
			var voicePageType = Routing.GetOrCreateContent("VoiceDemoPage");
			
			System.Diagnostics.Debug.WriteLine($"ChatPage route test: {chatPageType?.GetType().Name ?? "NULL"}");
			System.Diagnostics.Debug.WriteLine($"VoiceDemoPage route test: {voicePageType?.GetType().Name ?? "NULL"}");
			
			System.Diagnostics.Debug.WriteLine("=== Route Registration Complete ===");
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"CRITICAL ERROR registering routes: {ex}");
			System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
		}
	}
}
