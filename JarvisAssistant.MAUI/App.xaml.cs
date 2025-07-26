using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.MAUI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using CoreAppTheme = JarvisAssistant.Core.Interfaces.AppTheme;

namespace JarvisAssistant.MAUI;

public partial class App : Application
{
	private IThemeManager? _themeManager;
	private ILogger<App>? _logger;
	private readonly object _pageLock = new object();

	public App()
	{
		try
		{
			InitializeComponent();
			
			// Set up global exception handling for better debugging
			SetupGlobalExceptionHandling();
			
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

	/// <summary>
	/// Sets up global exception handling for better debugging experience.
	/// </summary>
	private void SetupGlobalExceptionHandling()
	{
		try
		{
			// Handle unhandled exceptions on the main thread
			AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
			{
				var exception = e.ExceptionObject as Exception;
				System.Diagnostics.Debug.WriteLine($"Unhandled exception: {exception}");
				ErrorLogger.LogError(exception ?? new Exception("Unknown unhandled exception"));
				
				// Break into debugger if attached
				if (System.Diagnostics.Debugger.IsAttached)
				{
					System.Diagnostics.Debugger.Break();
				}
			};

			// Handle unhandled exceptions on background threads
			TaskScheduler.UnobservedTaskException += (sender, e) =>
			{
				System.Diagnostics.Debug.WriteLine($"Unobserved task exception: {e.Exception}");
				ErrorLogger.LogError(e.Exception);
				e.SetObserved(); // Prevent app termination
				
				// Break into debugger if attached
				if (System.Diagnostics.Debugger.IsAttached)
				{
					System.Diagnostics.Debugger.Break();
				}
			};

#if DEBUG
			// Additional debug-only exception handling
			System.Diagnostics.Debug.WriteLine("Global exception handling configured for debugging");
#endif
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error setting up global exception handling: {ex}");
		}
	}

	protected override async void OnStart()
	{
		try
		{
			base.OnStart();
			
			// Initialize theme management
			await InitializeThemeManagement();
			
			// Initialize voice mode as enabled by default for Windows and Android TV
			await InitializeVoiceModeAsync();
			
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
			_logger?.LogInformation("Application entering sleep mode");
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"App sleep error: {ex}");
			ErrorLogger.LogError(ex);
		}
	}

	protected override async void OnResume()
	{
		try
		{
			base.OnResume();
			_logger?.LogInformation("Application resuming from sleep mode");
			
			// Check if theme should be updated based on system changes
			if (_themeManager != null)
			{
				var currentTheme = _themeManager.CurrentTheme;
				if (currentTheme == CoreAppTheme.System)
				{
					// Re-apply system theme in case it changed while app was sleeping
					await _themeManager.SwitchThemeAsync(CoreAppTheme.System);
				}
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"App resume error: {ex}");
			ErrorLogger.LogError(ex);
		}
	}

	/// <summary>
	/// Initializes theme management services and applies the initial theme.
	/// </summary>
	private async Task InitializeThemeManagement()
	{
		try
		{
			// Get services from DI container
			var serviceProvider = Handler?.MauiContext?.Services;
			if (serviceProvider == null)
			{
				System.Diagnostics.Debug.WriteLine("Service provider not available for theme initialization");
				return;
			}

			_logger = serviceProvider.GetService<ILogger<App>>();
			_themeManager = serviceProvider.GetService<IThemeManager>();

			if (_themeManager == null)
			{
				_logger?.LogWarning("Theme manager service not available");
				return;
			}

			// Subscribe to theme change events
			_themeManager.ThemeChanged += OnThemeChanged;

			// Load and apply the stored theme preference
			var storedTheme = await _themeManager.LoadThemePreferenceAsync();
			
			// If no preference is stored or it's set to auto-select, get the optimal theme
			if (storedTheme == CoreAppTheme.System)
			{
				await _themeManager.AutoSelectThemeAsync();
			}
			else
			{
				await _themeManager.SwitchThemeAsync(storedTheme);
			}

			_logger?.LogInformation("Theme management initialized successfully");
		}
		catch (Exception ex)
		{
			_logger?.LogError(ex, "Failed to initialize theme management");
			System.Diagnostics.Debug.WriteLine($"Theme initialization error: {ex}");
		}
	}

	/// <summary>
	/// Handles theme change events and preserves the current page state.
	/// </summary>
	/// <param name="sender">The theme manager that raised the event.</param>
	/// <param name="newTheme">The new theme that was applied.</param>
	private void OnThemeChanged(object? sender, CoreAppTheme newTheme)
	{
		try
		{
			lock (_pageLock)
			{
				_logger?.LogInformation("Theme changed to: {Theme}", newTheme);
				
				// Store current page information to preserve state
				var currentPage = MainPage;
				var currentShell = MainPage as Shell;
				string? currentRoute = null;
				
				if (currentShell != null)
				{
					currentRoute = currentShell.CurrentState?.Location?.ToString();
				}

				// Force a UI refresh by triggering a layout update
				MainThread.BeginInvokeOnMainThread(() =>
				{
					try
					{
						// Update the visual tree to reflect theme changes
						if (currentPage != null)
						{
							// Trigger a layout refresh without losing page state
							RefreshPageForTheme(currentPage);
						}

						_logger?.LogDebug("UI refreshed for theme change to: {Theme}", newTheme);
					}
					catch (Exception ex)
					{
						_logger?.LogWarning(ex, "Error refreshing UI after theme change");
					}
				});
			}
		}
		catch (Exception ex)
		{
			_logger?.LogError(ex, "Error handling theme change event");
		}
	}

	/// <summary>
	/// Refreshes a page to apply theme changes.
	/// </summary>
	/// <param name="page">The page to refresh.</param>
	private void RefreshPageForTheme(Page page)
	{
		try
		{
			// Force a visual refresh by updating the page's resources
			if (page.Resources != null)
			{
				// Trigger a resource refresh
				var tempResource = page.Resources;
				page.Resources = new ResourceDictionary();
				page.Resources = tempResource;
			}

			// Refresh child elements if applicable
			RefreshPageHierarchy(page);
		}
		catch (Exception ex)
		{
			_logger?.LogWarning(ex, "Error refreshing page for theme change");
		}
	}

	/// <summary>
	/// Recursively refreshes the page hierarchy to apply theme changes.
	/// </summary>
	/// <param name="element">The visual element to refresh.</param>
	private void RefreshPageHierarchy(VisualElement element)
	{
		try
		{
			// Force a layout update by changing and reverting a property
			var currentOpacity = element.Opacity;
			element.Opacity = currentOpacity - 0.001;
			element.Opacity = currentOpacity;

			// Refresh child elements
			if (element is Layout layout)
			{
				foreach (var child in layout.Children.OfType<VisualElement>())
				{
					RefreshPageHierarchy(child);
				}
			}
			else if (element is ContentPage page && page.Content != null)
			{
				RefreshPageHierarchy(page.Content);
			}
			else if (element is Shell shell)
			{
				foreach (var item in shell.Items)
				{
					foreach (var section in item.Items)
					{
						foreach (var content in section.Items)
						{
							if (content.Content is VisualElement contentElement)
							{
								RefreshPageHierarchy(contentElement);
							}
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			_logger?.LogWarning(ex, "Error refreshing element hierarchy for theme change");
		}
	}

	/// <summary>
	/// Initializes voice mode as enabled by default for Windows and Android TV platforms.
	/// </summary>
	private async Task InitializeVoiceModeAsync()
	{
		try
		{
			// Get services from DI container
			var serviceProvider = Handler?.MauiContext?.Services;
			if (serviceProvider == null)
			{
				System.Diagnostics.Debug.WriteLine("Service provider not available for voice mode initialization");
				return;
			}

			var voiceModeManager = serviceProvider.GetService<IVoiceModeManager>();
			var logger = serviceProvider.GetService<ILogger<App>>();

			if (voiceModeManager == null)
			{
				logger?.LogWarning("Voice mode manager service not available");
				return;
			}

			// Enable voice mode by default on Windows and Android TV platforms
#if WINDOWS
			logger?.LogInformation("🎤 Enabling voice mode by default for Windows platform");
			await voiceModeManager.EnableVoiceModeAsync();
			logger?.LogInformation("✅ Voice mode enabled successfully on Windows");
#elif ANDROID
			// Check if running on Android TV
			var platformService = serviceProvider.GetService<IPlatformService>();
			if (platformService?.IsAndroidTV == true)
			{
				logger?.LogInformation("🎤 Enabling voice mode by default for Android TV platform");
				await voiceModeManager.EnableVoiceModeAsync();
				logger?.LogInformation("✅ Voice mode enabled successfully on Android TV");
			}
			else
			{
				logger?.LogInformation("📱 Android phone detected - voice mode available but not auto-enabled");
			}
#endif
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error initializing voice mode: {ex}");
			_logger?.LogError(ex, "Failed to initialize voice mode");
		}
	}

	/// <summary>
	/// Gets the theme manager service for external access.
	/// </summary>
	/// <returns>The theme manager instance or null if not available.</returns>
	public IThemeManager? GetThemeManager()
	{
		return _themeManager;
	}
}
