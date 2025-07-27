using JarvisAssistant.Core.Services;
using JarvisAssistant.Services.Speech;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace JarvisAssistant.Services.Tests
{
    /// <summary>
    /// Simple console application to test speech recognition functionality
    /// </summary>
    public class SpeechRecognitionConsoleTest
    {
        public static async Task RunTestAsync()
        {
            // Create a simple logger - simplified approach
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddProvider(new ConsoleLoggerProvider()).SetMinimumLevel(LogLevel.Debug);
            });
            
            var logger = loggerFactory.CreateLogger<SimpleSpeechRecognitionService>();
            
            // Create the speech recognition service
            var speechService = new SimpleSpeechRecognitionService(logger);
            
            Console.WriteLine("=== Speech Recognition Console Test ===");
            Console.WriteLine($"Available: {speechService.IsAvailable}");
            Console.WriteLine();
            
            try
            {
                // Test permissions
                Console.WriteLine("Testing permissions...");
                var permissionStatus = await speechService.RequestPermissionsAsync();
                Console.WriteLine($"Permission status: {permissionStatus}");
                
                if (permissionStatus != PermissionStatus.Granted)
                {
                    Console.WriteLine("Microphone permission not granted. Exiting.");
                    return;
                }
                
                // Test languages
                Console.WriteLine("\nTesting available languages...");
                var languages = await speechService.GetAvailableLanguagesAsync();
                Console.WriteLine($"Languages: {string.Join(", ", languages)}");
                
                // Test single recognition
                Console.WriteLine("\n=== Testing Single Recognition ===");
                Console.WriteLine("Press ENTER to start single recognition, then speak...");
                Console.ReadLine();
                
                Console.WriteLine("Listening... Speak now!");
                var result = await speechService.RecognizeSpeechAsync();
                
                Console.WriteLine($"Result: '{result.Text}' (Confidence: {result.Confidence:P})");
                
                // Test continuous recognition
                Console.WriteLine("\n=== Testing Continuous Recognition ===");
                Console.WriteLine("Press ENTER to start continuous recognition...");
                Console.ReadLine();
                
                // Subscribe to events
                speechService.SpeechRecognized += (sender, result) =>
                {
                    Console.WriteLine($"Recognized: '{result.Text}' (Confidence: {result.Confidence:P})");
                };
                
                speechService.StateChanged += (sender, state) =>
                {
                    Console.WriteLine($"State changed: {state}");
                };
                
                var options = new SpeechRecognitionOptions
                {
                    ContinuousRecognition = true,
                    Language = "en-US"
                };
                
                Console.WriteLine("Starting continuous recognition... Speak multiple times!");
                var started = await speechService.StartListeningAsync(options);
                
                if (started)
                {
                    Console.WriteLine("Continuous recognition started. Press ENTER to stop...");
                    Console.ReadLine();
                    
                    await speechService.StopListeningAsync();
                    Console.WriteLine("Continuous recognition stopped.");
                }
                else
                {
                    Console.WriteLine("Failed to start continuous recognition.");
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            
            Console.WriteLine("\nTest completed. Press ENTER to exit...");
            Console.ReadLine();
        }
    }

    // Simple console logger provider to avoid package dependency issues
    public class ConsoleLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new ConsoleLogger(categoryName);
        public void Dispose() { }
    }

    public class ConsoleLogger : ILogger
    {
        private readonly string _categoryName;

        public ConsoleLogger(string categoryName)
        {
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state) => null!;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                Console.WriteLine($"[{logLevel}] {_categoryName}: {formatter(state, exception)}");
            }
        }
    }
}
