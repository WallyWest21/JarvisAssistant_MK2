using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
			// Add any required services here in the future
			// For now, keep it minimal to avoid initialization issues
			
			// Example of how to add services safely:
			// services.AddSingleton<IMyService, MyService>();
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error configuring services: {ex}");
			throw;
		}
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
