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
			
			// Register routes for navigation - ensure these pages can be instantiated
			Routing.RegisterRoute("ChatPage", typeof(ChatPage));
			Routing.RegisterRoute("VoiceDemoPage", typeof(VoiceDemoPage));
			Routing.RegisterRoute("KnowledgeBasePage", typeof(KnowledgeBasePage));
			
			System.Diagnostics.Debug.WriteLine("Routes registered successfully:");
			System.Diagnostics.Debug.WriteLine($"- ChatPage -> {typeof(ChatPage).FullName}");
			System.Diagnostics.Debug.WriteLine($"- VoiceDemoPage -> {typeof(VoiceDemoPage).FullName}");
			System.Diagnostics.Debug.WriteLine($"- KnowledgeBasePage -> {typeof(KnowledgeBasePage).FullName}");
			
			// Test that route registration worked by trying to create instances
			try
			{
				System.Diagnostics.Debug.WriteLine("Testing route registration...");
				
				// Test ChatPage creation
				var chatPageTest = Activator.CreateInstance(typeof(ChatPage));
				System.Diagnostics.Debug.WriteLine($"✓ ChatPage can be instantiated: {chatPageTest != null}");
				
				// Test VoiceDemoPage creation  
				var voicePageTest = Activator.CreateInstance(typeof(VoiceDemoPage));
				System.Diagnostics.Debug.WriteLine($"✓ VoiceDemoPage can be instantiated: {voicePageTest != null}");
				
				// Test KnowledgeBasePage creation
				var knowledgePageTest = Activator.CreateInstance(typeof(KnowledgeBasePage));
				System.Diagnostics.Debug.WriteLine($"✓ KnowledgeBasePage can be instantiated: {knowledgePageTest != null}");
			}
			catch (Exception testEx)
			{
				System.Diagnostics.Debug.WriteLine($"⚠️ Route test failed: {testEx.Message}");
				System.Diagnostics.Debug.WriteLine("This might indicate dependency injection issues but routes are still registered");
			}
			
			System.Diagnostics.Debug.WriteLine("=== Route Registration Complete ===");
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"❌ CRITICAL ERROR registering routes: {ex}");
			System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
			
			// This is critical - if routes can't be registered, navigation won't work
			throw new InvalidOperationException($"Failed to register navigation routes: {ex.Message}", ex);
		}
	}
}
