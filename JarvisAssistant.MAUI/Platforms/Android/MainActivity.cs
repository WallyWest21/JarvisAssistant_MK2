using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.MAUI.Platforms.Android.VoiceHandlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace JarvisAssistant.MAUI;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
	private GoogleTVVoiceHandler? _voiceHandler;
	private IPlatformService? _platformService;
	private ILogger<MainActivity>? _logger;

	protected override void OnCreate(Bundle? savedInstanceState)
	{
		try
		{
			base.OnCreate(savedInstanceState);
			
			// Initialize voice handler for Google TV
			InitializeVoiceHandler();
			
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

	protected override void OnPause()
	{
		try
		{
			base.OnPause();
			System.Diagnostics.Debug.WriteLine("MainActivity.OnPause completed successfully");
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"MainActivity.OnPause error: {ex}");
			throw;
		}
	}

	protected override void OnDestroy()
	{
		try
		{
			// Clean up voice handler
			_voiceHandler?.Dispose();
			_voiceHandler = null;

			base.OnDestroy();
			System.Diagnostics.Debug.WriteLine("MainActivity.OnDestroy completed successfully");
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"MainActivity.OnDestroy error: {ex}");
			throw;
		}
	}

	public override bool DispatchKeyEvent(KeyEvent? keyEvent)
	{
		try
		{
			// Handle voice-related key events first
			if (keyEvent != null && _voiceHandler != null && _platformService?.IsGoogleTV() == true)
			{
				if (_voiceHandler.HandleKeyEvent(keyEvent.KeyCode, keyEvent))
				{
					_logger?.LogDebug("Voice handler processed key event: {KeyCode}", keyEvent.KeyCode);
					return true; // Event was handled by voice handler
				}
			}

			// Fall back to default handling
			return base.DispatchKeyEvent(keyEvent);
		}
		catch (Exception ex)
		{
			_logger?.LogError(ex, "Error dispatching key event: {KeyCode}", keyEvent?.KeyCode);
			return base.DispatchKeyEvent(keyEvent);
		}
	}

	private void InitializeVoiceHandler()
	{
		try
		{
			// Get services from dependency injection
			var serviceProvider = IPlatformApplication.Current?.Services;
			if (serviceProvider == null)
			{
				System.Diagnostics.Debug.WriteLine("Service provider not available for voice handler initialization");
				return;
			}

			_platformService = serviceProvider.GetService<IPlatformService>();
			_logger = serviceProvider.GetService<ILogger<MainActivity>>();

			if (_platformService?.IsGoogleTV() == true)
			{
				var voiceModeManager = serviceProvider.GetService<IVoiceModeManager>();
				var commandProcessor = serviceProvider.GetService<IVoiceCommandProcessor>();
				var voiceHandlerLogger = serviceProvider.GetService<ILogger<GoogleTVVoiceHandler>>();

				if (voiceModeManager != null && commandProcessor != null && voiceHandlerLogger != null)
				{
					_voiceHandler = new GoogleTVVoiceHandler(
						voiceModeManager,
						commandProcessor,
						_platformService,
						voiceHandlerLogger);

					// Initialize asynchronously
					_ = Task.Run(async () =>
					{
						try
						{
							await _voiceHandler.InitializeAsync(this);
							_logger?.LogInformation("Google TV voice handler initialized successfully");
						}
						catch (Exception ex)
						{
							_logger?.LogError(ex, "Failed to initialize Google TV voice handler");
						}
					});
				}
				else
				{
					_logger?.LogWarning("Required services not available for Google TV voice handler");
				}
			}
			else
			{
				System.Diagnostics.Debug.WriteLine("Not running on Google TV, skipping voice handler initialization");
			}
		}
		catch (Exception ex)
		{
			_logger?.LogError(ex, "Error initializing voice handler");
			System.Diagnostics.Debug.WriteLine($"Error initializing voice handler: {ex}");
		}
	}
}
