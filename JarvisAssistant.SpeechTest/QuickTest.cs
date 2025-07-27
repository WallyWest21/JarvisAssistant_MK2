using JarvisAssistant.SpeechTest.Core;
using JarvisAssistant.SpeechTest.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace JarvisAssistant.SpeechTest.QuickConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            System.Console.WriteLine("?? Quick Speech Test - Starting...");
            
            // Simple console-based test without MAUI overhead
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            
#if WINDOWS
            services.AddSingleton<ISpeechRecognitionService, WindowsSpeechRecognitionService>();
            System.Console.WriteLine("? Windows Speech Recognition Service loaded");
#else
            System.Console.WriteLine("? Only Windows is supported in this quick test");
            return;
#endif

            var serviceProvider = services.BuildServiceProvider();
            var speechService = serviceProvider.GetRequiredService<ISpeechRecognitionService>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                // Quick availability check
                System.Console.WriteLine($"Service Available: {speechService.IsAvailable}");
                
                if (!speechService.IsAvailable)
                {
                    System.Console.WriteLine("? Speech recognition not available");
                    return;
                }

                // Test permissions
                var permission = await speechService.RequestPermissionsAsync();
                System.Console.WriteLine($"Permission Status: {permission}");

                // Quick recognition test
                System.Console.WriteLine("\n?? Say something in the next 5 seconds...");
                
                var options = new SpeechRecognitionOptions
                {
                    Language = "en-US",
                    MaxListeningTime = TimeSpan.FromSeconds(5)
                };

                var result = await speechService.RecognizeSpeechAsync(options);
                
                System.Console.WriteLine($"\n? Result: '{result.Text}'");
                System.Console.WriteLine($"Confidence: {result.Confidence:P}");
                
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"? Error: {ex.Message}");
                logger.LogError(ex, "Speech test failed");
            }
            
            System.Console.WriteLine("\nPress any key to exit...");
            System.Console.ReadKey();
        }
    }
}