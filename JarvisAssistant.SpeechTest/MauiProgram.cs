using JarvisAssistant.SpeechTest.Core;
using JarvisAssistant.SpeechTest.Services;
using JarvisAssistant.SpeechTest.ViewModels;
using Microsoft.Extensions.Logging;
using MauiPermissionStatus = Microsoft.Maui.ApplicationModel.PermissionStatus;

namespace JarvisAssistant.SpeechTest
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            // Add logging
            builder.Logging.AddDebug();
            builder.Logging.SetMinimumLevel(LogLevel.Debug);

            // Register services
#if WINDOWS
            builder.Services.AddSingleton<ISpeechRecognitionService, WindowsSpeechRecognitionService>();
#elif ANDROID
            builder.Services.AddSingleton<ISpeechRecognitionService, AndroidSpeechRecognitionService>();
#else
            builder.Services.AddSingleton<ISpeechRecognitionService, UnsupportedSpeechRecognitionService>();
#endif

            // Register ViewModels
            builder.Services.AddSingleton<SpeechTestViewModel>();

            // Register Pages
            builder.Services.AddSingleton<MainPage>();

            return builder.Build();
        }
    }

    /// <summary>
    /// Fallback service for unsupported platforms
    /// </summary>
    public class UnsupportedSpeechRecognitionService : ISpeechRecognitionService
    {
        public bool IsListening => false;
        public bool IsAvailable => false;

        public event EventHandler<SpeechRecognitionResult>? SpeechRecognized;
        public event EventHandler<string>? PartialResultsReceived;
        public event EventHandler<SpeechRecognitionState>? StateChanged;

        public Task<IEnumerable<string>> GetAvailableLanguagesAsync()
        {
            return Task.FromResult(Enumerable.Empty<string>());
        }

        public Task<Core.PermissionStatus> RequestPermissionsAsync()
        {
            return Task.FromResult(Core.PermissionStatus.Unknown);
        }

        public Task<SpeechRecognitionResult> RecognizeSpeechAsync(SpeechRecognitionOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new PlatformNotSupportedException("Speech recognition not supported on this platform");
        }

        public Task<DiagnosticResult> RunDiagnosticsAsync()
        {
            var result = new DiagnosticResult();
            result.Errors.Add("Speech recognition not supported on this platform");
            result.SystemInfo["Platform"] = "Unsupported";
            return Task.FromResult(result);
        }

        public Task<bool> StartListeningAsync(SpeechRecognitionOptions? options = null)
        {
            return Task.FromResult(false);
        }

        public Task StopListeningAsync()
        {
            return Task.CompletedTask;
        }
    }
}
