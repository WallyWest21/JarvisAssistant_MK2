using Android.App;
using Android.Runtime;
using System;

namespace JarvisAssistant.MAUI;

[Application]
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
		// Set up global exception handling
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		AndroidEnvironment.UnhandledExceptionRaiser += OnAndroidUnhandledException;
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	public override void OnCreate()
	{
		try
		{
			base.OnCreate();
		}
		catch (Exception ex)
		{
			// Log the exception to prevent crashes during initialization
			System.Diagnostics.Debug.WriteLine($"MainApplication.OnCreate error: {ex}");
			throw; // Re-throw to maintain original behavior but with logging
		}
	}

	// Handle unhandled exceptions from the AppDomain
	private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		var exception = e.ExceptionObject as Exception;
		System.Diagnostics.Debug.WriteLine($"Unhandled AppDomain exception: {exception}");
		
		try
		{
			// Try to log the error if possible
			if (exception != null)
			{
				ErrorLogger.LogError(exception);
			}
		}
		catch
		{
			// Ignore any errors during error logging to prevent recursive crashes
		}
	}

	// Handle unhandled exceptions from Android environment
	private void OnAndroidUnhandledException(object sender, RaiseThrowableEventArgs e)
	{
		System.Diagnostics.Debug.WriteLine($"Unhandled Android exception: {e.Exception}");
		
		try
		{
			// Try to log the error if possible
			ErrorLogger.LogError(new Exception(e.Exception?.ToString() ?? "Unknown Android exception"));
		}
		catch
		{
			// Ignore any errors during error logging to prevent recursive crashes
		}
	}
}
