using System;

namespace JarvisAssistant.MAUI;

public partial class App : Application
{
	public App()
	{
		try
		{
			InitializeComponent();
			MainPage = new AppShell();
		}
		catch (Exception ex)
		{
			// Log the initialization error
			System.Diagnostics.Debug.WriteLine($"App initialization error: {ex}");
			ErrorLogger.LogError(ex);
			
			// Try to create a basic page to prevent complete crash
			try
			{
				MainPage = new ContentPage 
				{ 
					Content = new Label 
					{ 
						Text = "App initialization failed. Please restart the application.",
						HorizontalOptions = LayoutOptions.Center,
						VerticalOptions = LayoutOptions.Center
					}
				};
			}
			catch
			{
				// If even this fails, re-throw the original exception
				throw ex;
			}
		}
	}

	protected override void OnStart()
	{
		try
		{
			base.OnStart();
			System.Diagnostics.Debug.WriteLine("App started successfully");
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"App start error: {ex}");
			ErrorLogger.LogError(ex);
		}
	}

	protected override void OnSleep()
	{
		try
		{
			base.OnSleep();
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"App sleep error: {ex}");
			ErrorLogger.LogError(ex);
		}
	}

	protected override void OnResume()
	{
		try
		{
			base.OnResume();
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"App resume error: {ex}");
			ErrorLogger.LogError(ex);
		}
	}
}
