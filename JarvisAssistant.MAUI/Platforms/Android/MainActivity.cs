using Android.App;
using Android.Content.PM;
using Android.OS;
using System;

namespace JarvisAssistant.MAUI;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
	protected override void OnCreate(Bundle? savedInstanceState)
	{
		try
		{
			base.OnCreate(savedInstanceState);
			System.Diagnostics.Debug.WriteLine("MainActivity.OnCreate completed successfully");
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"MainActivity.OnCreate error: {ex}");
			// Log error but allow the exception to propagate
			// This ensures the crash is properly reported
			throw;
		}
	}

	protected override void OnStart()
	{
		try
		{
			base.OnStart();
			System.Diagnostics.Debug.WriteLine("MainActivity.OnStart completed successfully");
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"MainActivity.OnStart error: {ex}");
			throw;
		}
	}

	protected override void OnResume()
	{
		try
		{
			base.OnResume();
			System.Diagnostics.Debug.WriteLine("MainActivity.OnResume completed successfully");
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"MainActivity.OnResume error: {ex}");
			throw;
		}
	}
}
