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
			
			// Register MAUI abstraction services using Core interfaces
			services.AddSingleton<JarvisAssistant.Core.Interfaces.IDialogService, MauiDialogService>();
			services.AddSingleton<JarvisAssistant.Core.Interfaces.INavigationService, MauiNavigationService>();
			
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
			
			// Register LLM Service with automatic endpoint detection
			try
			{
				System.Diagnostics.Debug.WriteLine("Configuring Ollama LLM Service with automatic endpoint detection...");
				
				// Add Ollama LLM Service with automatic fallback
				services.AddOllamaLLMService(); // Will auto-detect best endpoint
				
				// Configure with additional options
				services.ConfigureOllamaLLMService(options =>
				{
					// Override defaults if needed
					options.Timeout = TimeSpan.FromSeconds(30);
					options.MaxRetryAttempts = 3;
					
					// Platform-specific alternative endpoints
#if ANDROID
					options.AlternativeEndpoints = new List<string>
					{
						"http://10.0.2.2:11434",           // Android emulator host mapping
						"http://100.108.155.28:11434",     // Direct IP (might work on real device)
						"http://192.168.1.100:11434",      // Common local network
						"http://localhost:11434"           // Fallback
					};
#else
					options.AlternativeEndpoints = new List<string>
					{
						"http://localhost:11434",
						"http://127.0.0.1:11434",
						"http://100.108.155.28:11434", // Your original endpoint
						"http://host.docker.internal:11434"
					};
#endif
				});
				
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

			// Register status monitoring services with synchronized endpoints
			services.AddStatusMonitoring(options =>
			{
				options.MonitoringIntervalSeconds = 5;
				options.HealthCheckTimeoutSeconds = 10;
				options.AutoStartMonitoring = true;
				options.SignalRHubUrl = "http://localhost:5003/statusHub"; // Configure as needed
				
				// Configure service endpoints to match the detection logic
				var ollamaEndpoint = GetPlatformSpecificOllamaEndpoint();
				
				options.ServiceEndpoints["llm-engine"] = new ServiceEndpointConfig
				{
					Name = "llm-engine",
					DisplayName = "LLM Engine (Ollama)",
					HealthEndpoint = $"{ollamaEndpoint}/api/tags", // Use the platform-specific endpoint
					Enabled = true
				};
				
				options.ServiceEndpoints["vision-api"] = new ServiceEndpointConfig
				{
					Name = "vision-api",
					DisplayName = "Vision API",
					HealthEndpoint = "http://localhost:5000/health",
					Enabled = true
				};
				
				options.ServiceEndpoints["voice-service"] = new ServiceEndpointConfig
				{
					Name = "voice-service",
					DisplayName = "Voice Service",
					HealthEndpoint = "http://localhost:5001/health",
					Enabled = true
				};
			});

			// Register status panel view and view model
			services.AddTransient<StatusPanelView>();
			services.AddTransient<StatusPanelViewModel>();

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

	/// <summary>
	/// Gets the platform-specific Ollama endpoint URL.
	/// </summary>
	/// <returns>The appropriate Ollama endpoint for the current platform.</returns>
	private static string GetPlatformSpecificOllamaEndpoint()
	{
#if ANDROID
		// Android emulator maps host machine's localhost to 10.0.2.2
		return "http://10.0.2.2:11434";
#else
		// Use the working endpoint for other platforms
		return "http://100.108.155.28:11434";
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
