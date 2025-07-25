using JarvisAssistant.Services.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using JarvisAssistant.Core.Interfaces;

namespace JarvisAssistant.VoiceTest
{
    /// <summary>
    /// Quick test to verify ElevenLabs voice service configuration.
    /// </summary>
    public class VoiceServiceConfigTest
    {
        public static async Task TestVoiceServiceAsync()
        {
            // Set up service collection
            var services = new ServiceCollection();
            
            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Get API key from environment
            var apiKey = Environment.GetEnvironmentVariable("ELEVENLABS_API_KEY");
            
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Console.WriteLine("❌ ELEVENLABS_API_KEY environment variable not found!");
                Console.WriteLine("Setting from parameter...");
                apiKey = "sk_3f8d635adaf9cccefb6a9da0f368d4909a8d1348b9ac977e";
            }
            else
            {
                Console.WriteLine("✅ ELEVENLABS_API_KEY found in environment variables");
            }

            // Add ElevenLabs voice service
            try
            {
                services.AddJarvisVoiceService(apiKey);
                Console.WriteLine("✅ Voice service registered successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error registering voice service: {ex.Message}");
                return;
            }

            // Build service provider
            var serviceProvider = services.BuildServiceProvider();

            // Test service resolution
            try
            {
                var voiceService = serviceProvider.GetRequiredService<IVoiceService>();
                Console.WriteLine($"✅ Voice service resolved: {voiceService.GetType().Name}");
                
                // Test a simple operation
                await TestVoiceServiceHealthAsync(voiceService);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error resolving voice service: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private static async Task TestVoiceServiceHealthAsync(IVoiceService voiceService)
        {
            try
            {
                Console.WriteLine("🎯 Testing voice service with simple text...");
                
                var testText = "Hello, this is Jarvis voice service test.";
                var audioData = await voiceService.GenerateSpeechAsync(testText);
                
                if (audioData != null && audioData.Length > 0)
                {
                    Console.WriteLine($"✅ Voice service working! Generated {audioData.Length} bytes of audio");
                }
                else
                {
                    Console.WriteLine("⚠️ Voice service returned empty audio data");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Voice service test failed: {ex.Message}");
                
                // Check if it's a network issue
                if (ex.Message.Contains("timeout") || ex.Message.Contains("network"))
                {
                    Console.WriteLine("💡 This may be a network connectivity issue with ElevenLabs API");
                }
            }
        }
    }
}
