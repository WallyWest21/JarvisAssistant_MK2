using JarvisAssistant.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JarvisAssistant.VoiceTest
{
    /// <summary>
    /// Test program to verify the intelligent fallback voice service functionality.
    /// This tests the scenario where ElevenLabs fails and fallback services are used.
    /// </summary>
    public class FallbackTestProgram
    {
        public static async Task RunAsync(string[] args)
        {
            Console.WriteLine("=== Jarvis Assistant - Voice Service Fallback Test ===");
            Console.WriteLine("Testing intelligent fallback voice services...\n");

            // Create logger
            var loggerFactory = LoggerFactory.Create(builder => 
                builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            var logger = loggerFactory.CreateLogger<IntelligentFallbackVoiceService>();

            // Create intelligent fallback service
            var fallbackService = new IntelligentFallbackVoiceService(logger);

            try
            {
                // Test 1: Get service status
                Console.WriteLine("1. Service Status:");
                var status = fallbackService.GetServiceStatus();
                foreach (var kvp in status)
                {
                    Console.WriteLine($"   {kvp.Key}: Available = {((Dictionary<string, object?>)kvp.Value!)["Available"]}");
                }
                Console.WriteLine();

                // Test 2: Generate speech with fallback
                Console.WriteLine("2. Testing speech generation with fallback services...");
                var testText = "Hello, this is a test of the Jarvis Assistant fallback voice system. If you can hear this, the fallback is working correctly.";
                
                var audioData = await fallbackService.GenerateSpeechAsync(testText);
                
                if (audioData.Length > 0)
                {
                    Console.WriteLine($"✓ Successfully generated {audioData.Length} bytes of audio");
                    
                    // Save to file for testing
                    var outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), 
                        "jarvis_fallback_test.wav");
                    
                    // Create WAV header and save
                    var wavData = CreateWavFile(audioData, 22050, 1, 16);
                    await File.WriteAllBytesAsync(outputPath, wavData);
                    Console.WriteLine($"✓ Audio saved to: {outputPath}");
                }
                else
                {
                    Console.WriteLine("✗ Failed to generate audio");
                }
                Console.WriteLine();

                // Test 3: Test streaming
                Console.WriteLine("3. Testing streaming speech...");
                var streamChunks = new List<byte[]>();
                
                await foreach (var chunk in fallbackService.StreamSpeechAsync("Testing streaming fallback functionality"))
                {
                    streamChunks.Add(chunk);
                }
                
                if (streamChunks.Count > 0)
                {
                    var totalStreamBytes = streamChunks.Sum(c => c.Length);
                    Console.WriteLine($"✓ Successfully streamed {streamChunks.Count} chunks, {totalStreamBytes} total bytes");
                }
                else
                {
                    Console.WriteLine("✗ Failed to stream audio");
                }
                Console.WriteLine();

                // Test 4: Show final status
                Console.WriteLine("4. Final Service Status:");
                status = fallbackService.GetServiceStatus();
                foreach (var kvp in status)
                {
                    var serviceStatus = (Dictionary<string, object?>)kvp.Value!;
                    Console.WriteLine($"   {kvp.Key}:");
                    Console.WriteLine($"      Available: {serviceStatus["Available"]}");
                    Console.WriteLine($"      Failure Count: {serviceStatus["FailureCount"]}");
                    Console.WriteLine($"      In Cooldown: {serviceStatus["InCooldown"]}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error during testing: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                fallbackService.Dispose();
                Console.WriteLine("\n=== Test Complete ===");
                
                if (args.Length == 0 || !args.Contains("--no-pause"))
                {
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                }
            }
        }

        /// <summary>
        /// Creates a WAV file from PCM audio data.
        /// </summary>
        /// <param name="pcmData">Raw PCM audio data</param>
        /// <param name="sampleRate">Sample rate (e.g., 22050)</param>
        /// <param name="channels">Number of channels (1 for mono, 2 for stereo)</param>
        /// <param name="bitsPerSample">Bits per sample (16 for 16-bit audio)</param>
        /// <returns>Complete WAV file data</returns>
        private static byte[] CreateWavFile(byte[] pcmData, int sampleRate, short channels, short bitsPerSample)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new BinaryWriter(memoryStream);

            // WAV header
            writer.Write("RIFF".ToCharArray());
            writer.Write(36 + pcmData.Length); // File size - 8
            writer.Write("WAVE".ToCharArray());

            // fmt chunk
            writer.Write("fmt ".ToCharArray());
            writer.Write(16); // Chunk size
            writer.Write((short)1); // PCM format
            writer.Write(channels);
            writer.Write(sampleRate);
            writer.Write(sampleRate * channels * bitsPerSample / 8); // Byte rate
            writer.Write((short)(channels * bitsPerSample / 8)); // Block align
            writer.Write(bitsPerSample);

            // data chunk
            writer.Write("data".ToCharArray());
            writer.Write(pcmData.Length);
            writer.Write(pcmData);

            return memoryStream.ToArray();
        }
    }
}
