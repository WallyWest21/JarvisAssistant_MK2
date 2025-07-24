using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.MAUI.Services;
using JarvisAssistant.MAUI.Views;
using JarvisAssistant.MAUI.ViewModels;
using JarvisAssistant.Services;
using JarvisAssistant.Services.Extensions;
using SkiaSharp.Views.Maui.Controls.Hosting;
using System;
using System.IO;

namespace JarvisAssistant.MAUI;

/// <summary>
/// Contains the configuration and setup for the MAUI application.
/// </summary>
public static class MauiProgram
{
	/// <summary>
	/// Creates and configures the MAUI application.
	/// </summary>
	/// <returns>The configured MAUI application.</returns>
	public static MauiApp CreateMauiApp()
	{
		try
		{
			var builder = MauiApp.CreateBuilder();
			builder
				.UseMauiApp<App>()
				.UseSkiaSharp()
				.ConfigureFonts(fonts =>
				{
					fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
					fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				});

#if DEBUG
			builder.Logging.AddDebug();
			
			// Set minimum log level for debugging
			builder.Logging.SetMinimumLevel(LogLevel.Debug);
#endif

			// Configure services safely
			ConfigureServices(builder.Services);

			var app = builder.Build();

			// Log successful initialization
			var logger = app.Services.GetService<ILogger<App>>();
			logger?.LogInformation("MAUI app created successfully");

			return app;
		}
		catch (Exception ex)
		{
			// Log to debug output if regular logging isn't available
			System.Diagnostics.Debug.WriteLine($"Error creating MAUI app: {ex}");
			
			// Try to log to file as well
			ErrorLogger.LogError(ex);
			
			// Re-throw to maintain original behavior
			throw;
		}
	}

	private static void ConfigureServices(IServiceCollection services)
	{
		try
		{
			// Register Core interfaces with their implementations
			services.AddSingleton<IPlatformService, PlatformService>();
			services.AddSingleton<IThemeManager, MauiThemeManager>();
			
			// Register existing services from JarvisAssistant.Services
			services.AddSingleton<JarvisAssistant.Core.Interfaces.IErrorHandlingService, JarvisAssistant.Services.ErrorHandlingService>();
			
			// Register voice mode services with error handling
			try
			{
				services.AddSingleton<IVoiceModeManager, VoiceModeManager>();
				services.AddSingleton<IVoiceCommandProcessor, VoiceCommandProcessor>();
				services.AddSingleton<IVoiceService, StubVoiceService>();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Warning: Voice services not available: {ex.Message}");
				// Voice services are optional
			}
			
			// Register LLM Service with Ollama integration
			try
			{
				System.Diagnostics.Debug.WriteLine("Configuring Ollama LLM Service...");
				
				// Add Ollama LLM Service (will fallback to local service if Ollama is not available)
				services.AddOllamaLLMService("http://localhost:11434");
				
				System.Diagnostics.Debug.WriteLine("Ollama LLM Service configured successfully");
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error configuring Ollama service, using fallback: {ex.Message}");
				
				// Fallback to basic LLM service if Ollama is not available
				services.AddSingleton<ILLMService>(serviceProvider =>
				{
					var fallbackLogger = serviceProvider.GetService<ILogger<FallbackLLMService>>();
					return new FallbackLLMService(fallbackLogger);
				});
			}
			
			// Register Pages and ViewModels (always required)
			services.AddTransient<ChatPage>();
			services.AddTransient<ChatViewModel>();
			services.AddTransient<VoiceDemoPage>();

			// Register platform-specific services
			RegisterPlatformServices(services);
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error configuring services: {ex}");
			// Don't re-throw - allow app to start with limited functionality
		}
	}

	/// <summary>
	/// Registers platform-specific services.
	/// </summary>
	/// <param name="services">The service collection to configure.</param>
	private static void RegisterPlatformServices(IServiceCollection services)
	{
		// Platform-specific service registrations will go here
		// For example, different implementations for Android vs Windows
		
#if ANDROID
		// Android-specific services
		System.Diagnostics.Debug.WriteLine("Registering Android-specific services");
#elif WINDOWS
		// Windows-specific services
		System.Diagnostics.Debug.WriteLine("Registering Windows-specific services");
#elif IOS
		// iOS-specific services
		System.Diagnostics.Debug.WriteLine("Registering iOS-specific services");
#elif MACCATALYST
		// macOS-specific services
		System.Diagnostics.Debug.WriteLine("Registering macOS-specific services");
#endif
	}
}

public static class ErrorLogger
{
    public static void LogError(Exception ex)
    {
        try
        {
			// Use a more robust path detection
			var logPath = GetLogPath();
            var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC: {ex}\n{new string('=', 80)}\n";
            File.AppendAllText(logPath, logEntry);
        }
        catch
        {
            // Swallow any exceptions to avoid recursive errors
            System.Diagnostics.Debug.WriteLine($"Failed to log error to file: {ex}");
        }
    }

	private static string GetLogPath()
	{
		try
		{
			return Path.Combine(FileSystem.AppDataDirectory, "error.log");
		}
		catch
		{
			// Fallback to temp directory if FileSystem.AppDataDirectory fails
			return Path.Combine(Path.GetTempPath(), "jarvis_error.log");
		}
	}
}
