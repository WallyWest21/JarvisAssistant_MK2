using JarvisAssistant.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace VoiceServiceTest;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Testing Windows Voice Services...");
        Console.WriteLine($"Operating System: {Environment.OSVersion}");
        Console.WriteLine($"Is Windows: {OperatingSystem.IsWindows()}");
        Console.WriteLine();

        // Test WindowsSapiVoiceService
        Console.WriteLine("Testing WindowsSapiVoiceService...");
        try
        {
            using var sapiService = new WindowsSapiVoiceService();
            var voices = sapiService.GetAvailableVoices();
            Console.WriteLine($"✅ WindowsSapiVoiceService created successfully");
            Console.WriteLine($"   Available voices: {voices.Length}");
            
            if (voices.Length > 0)
            {
                Console.WriteLine("   First few voices:");
                for (int i = 0; i < Math.Min(3, voices.Length); i++)
                {
                    Console.WriteLine($"   - {voices[i]}");
                }
            }

            // Test speech generation
            var audio = await sapiService.GenerateSpeechAsync("Hello, this is a test.");
            Console.WriteLine($"   Generated audio: {audio.Length} bytes");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ WindowsSapiVoiceService failed: {ex.Message}");
        }

        Console.WriteLine();

        // Test ModernWindowsTtsService
        Console.WriteLine("Testing ModernWindowsTtsService...");
        try
        {
            var logger = NullLogger<ModernWindowsTtsService>.Instance;
            using var modernService = new ModernWindowsTtsService(logger);
            var voices = modernService.GetAvailableVoices();
            Console.WriteLine($"✅ ModernWindowsTtsService created successfully");
            Console.WriteLine($"   Available voices: {voices.Length}");
            
            if (voices.Length > 0)
            {
                Console.WriteLine("   First few voices:");
                for (int i = 0; i < Math.Min(3, voices.Length); i++)
                {
                    Console.WriteLine($"   - {voices[i]}");
                }
            }

            // Test speech generation
            var audio = await modernService.GenerateSpeechAsync("Hello from Modern TTS service.");
            Console.WriteLine($"   Generated audio: {audio.Length} bytes");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ModernWindowsTtsService failed: {ex.Message}");
        }

        Console.WriteLine();
        Console.WriteLine("Voice service tests completed. Press any key to exit...");
        Console.ReadKey();
    }
}
