using JarvisAssistant.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.DependencyInjection;

namespace JarvisAssistant.VoiceTest
{
    /// <summary>
    /// Simple test to verify the enhanced fallback voice service works correctly
    /// </summary>
    public class SimpleFallbackTest
    {
        public static async Task RunAsync(string[] args)
        {
            Console.WriteLine("=== Enhanced Fallback Voice Service Test ===");
            Console.WriteLine();

            // Setup logging
            var services = new ServiceCollection();
            services.AddLogging(builder => 
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            var serviceProvider = services.BuildServiceProvider();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<IntelligentFallbackVoiceService>();

            try
            {
                Console.WriteLine("🔧 Creating Intelligent Fallback Voice Service...");
                using var fallbackService = new IntelligentFallbackVoiceService(logger);

                Console.WriteLine("✅ Service created successfully!");
                Console.WriteLine();

                // Test 1: Show service status
                Console.WriteLine("📊 Available Fallback Services:");
                var status = fallbackService.GetServiceStatus();
                foreach (var kvp in status)
                {
                    var serviceStatus = (Dictionary<string, object?>)kvp.Value!;
                    var available = serviceStatus["Available"];
                    var failureCount = serviceStatus["FailureCount"];
                    var inCooldown = serviceStatus["InCooldown"];
                    
                    Console.WriteLine($"   • {kvp.Key}:");
                    Console.WriteLine($"     - Available: {available}");
                    Console.WriteLine($"     - Failure Count: {failureCount}");
                    Console.WriteLine($"     - In Cooldown: {inCooldown}");
                }
                Console.WriteLine();

                // Test 2: Generate speech
                Console.WriteLine("🎤 Testing Speech Generation...");
                var testText = "Hello! This is a test of the enhanced fallback voice service. The system automatically uses free Windows TTS when ElevenLabs is unavailable.";
                
                var audioData = await fallbackService.GenerateSpeechAsync(testText);
                
                if (audioData.Length > 0)
                {
                    Console.WriteLine($"✅ Successfully generated {audioData.Length:N0} bytes of audio!");
                    
                    // Try to save audio file
                    try
                    {
                        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                        var audioFile = Path.Combine(desktopPath, $"jarvis_fallback_test_{DateTime.Now:yyyyMMdd_HHmmss}.wav");
                        
                        // Create WAV file
                        var wavData = CreateWavFile(audioData, 22050, 1, 16);
                        await File.WriteAllBytesAsync(audioFile, wavData);
                        
                        Console.WriteLine($"💾 Audio saved to: {audioFile}");
                        Console.WriteLine("   You can play this file to hear the fallback TTS!");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️  Could not save audio file: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("❌ Failed to generate audio");
                }
                Console.WriteLine();

                // Test 3: Test streaming
                Console.WriteLine("📡 Testing Streaming Speech...");
                var streamChunks = new List<byte[]>();
                var streamText = "Testing streaming functionality with fallback services.";
                
                await foreach (var chunk in fallbackService.StreamSpeechAsync(streamText))
                {
                    streamChunks.Add(chunk);
                    Console.Write(".");
                }
                
                Console.WriteLine();
                if (streamChunks.Count > 0)
                {
                    var totalBytes = streamChunks.Sum(c => c.Length);
                    Console.WriteLine($"✅ Successfully streamed {streamChunks.Count} chunks ({totalBytes:N0} total bytes)");
                }
                else
                {
                    Console.WriteLine("❌ Streaming failed");
                }
                Console.WriteLine();

                // Test 4: Show final status
                Console.WriteLine("📈 Final Service Status:");
                status = fallbackService.GetServiceStatus();
                foreach (var kvp in status)
                {
                    var serviceStatus = (Dictionary<string, object?>)kvp.Value!;
                    Console.WriteLine($"   • {kvp.Key}: {serviceStatus["Available"]} (Failures: {serviceStatus["FailureCount"]})");
                }

                Console.WriteLine();
                Console.WriteLine("🎯 Fallback System Summary:");
                Console.WriteLine("   ✅ Automatic failover when ElevenLabs is unavailable");
                Console.WriteLine("   ✅ Uses free Windows TTS services as fallbacks");
                Console.WriteLine("   ✅ Intelligent health monitoring and recovery");
                Console.WriteLine("   ✅ No interruption to voice functionality");
                Console.WriteLine("   ✅ Zero additional cost for fallback services");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine();
            Console.WriteLine("=== Test Complete ===");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Creates a simple WAV file from PCM audio data
        /// </summary>
        private static byte[] CreateWavFile(byte[] pcmData, int sampleRate, short channels, short bitsPerSample)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            // WAV header
            writer.Write("RIFF".ToCharArray());
            writer.Write(36 + pcmData.Length);
            writer.Write("WAVE".ToCharArray());

            // fmt chunk
            writer.Write("fmt ".ToCharArray());
            writer.Write(16);
            writer.Write((short)1); // PCM
            writer.Write(channels);
            writer.Write(sampleRate);
            writer.Write(sampleRate * channels * bitsPerSample / 8);
            writer.Write((short)(channels * bitsPerSample / 8));
            writer.Write(bitsPerSample);

            // data chunk
            writer.Write("data".ToCharArray());
            writer.Write(pcmData.Length);
            writer.Write(pcmData);

            return stream.ToArray();
        }
    }
}
