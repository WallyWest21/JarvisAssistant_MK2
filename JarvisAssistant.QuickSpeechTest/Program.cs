using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Speech.Recognition;
using System.Speech.Synthesis;

namespace JarvisAssistant.QuickSpeechTest;

class Program
{
    private static readonly ServiceProvider _serviceProvider;
    private static readonly ILogger<Program> _logger;

    static Program()
    {
        var services = new ServiceCollection();
        
        // Configure logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Register services
        services.AddSingleton<IVoiceService, WindowsSapiVoiceService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<Program>>();
    }

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Jarvis Assistant Quick Speech Test ===");
        Console.WriteLine();

        try
        {
            await RunSpeechRecognitionTestAsync();
            await RunTextToSpeechTestAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during testing");
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            _serviceProvider.Dispose();
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    private static async Task RunSpeechRecognitionTestAsync()
    {
        Console.WriteLine("Testing Speech Recognition...");
        
        try
        {
            using var recognizer = new SpeechRecognitionEngine();
            
            // Test if speech recognition is available
            var recognizers = SpeechRecognitionEngine.InstalledRecognizers();
            Console.WriteLine($"Found {recognizers.Count} speech recognition engines");
            
            if (recognizers.Count == 0)
            {
                Console.WriteLine("? No speech recognition engines available");
                return;
            }

            // Use the default recognizer
            recognizer.LoadGrammar(new DictationGrammar());
            recognizer.SetInputToDefaultAudioDevice();

            Console.WriteLine("? Speech recognition initialized successfully");
            Console.WriteLine("?? Say something (listening for 5 seconds)...");

            bool speechDetected = false;
            recognizer.SpeechRecognized += (sender, e) =>
            {
                Console.WriteLine($"???  Recognized: \"{e.Result.Text}\" (Confidence: {e.Result.Confidence:P})");
                speechDetected = true;
            };

            recognizer.RecognizeAsync(RecognizeMode.Single);
            
            // Wait for speech or timeout
            var timeout = DateTime.Now.AddSeconds(5);
            while (DateTime.Now < timeout && !speechDetected)
            {
                await Task.Delay(100);
            }

            recognizer.RecognizeAsyncStop();

            if (!speechDetected)
            {
                Console.WriteLine("? No speech detected within timeout period");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Speech recognition test failed: {ex.Message}");
            _logger.LogError(ex, "Speech recognition test failed");
        }

        Console.WriteLine();
    }

    private static async Task RunTextToSpeechTestAsync()
    {
        Console.WriteLine("Testing Text-to-Speech...");

        try
        {
            var voiceService = _serviceProvider.GetRequiredService<IVoiceService>();
            
            Console.WriteLine("?? Testing TTS: \"Hello, this is a speech test.\"");
            
            var audioData = await voiceService.GenerateSpeechAsync("Hello, this is a speech test.");
            
            if (audioData?.Length > 0)
            {
                Console.WriteLine($"? TTS test successful - Generated {audioData.Length} bytes of audio data");
            }
            else
            {
                Console.WriteLine("??  TTS test completed but no audio data was returned");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? TTS test failed: {ex.Message}");
            _logger.LogError(ex, "TTS test failed");
        }

        Console.WriteLine();
    }
}