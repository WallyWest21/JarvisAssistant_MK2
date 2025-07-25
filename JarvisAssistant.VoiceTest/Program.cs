using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using JarvisAssistant.Services.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace JarvisAssistant.VoiceTest
{
    /// <summary>
    /// Simple console application to test ElevenLabs voice generation
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Jarvis ElevenLabs Voice Test ===");
            Console.WriteLine();

            // Get API key from environment or user input
            var apiKey = Environment.GetEnvironmentVariable("ELEVENLABS_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Console.Write("Enter your ElevenLabs API key: ");
                apiKey = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    Console.WriteLine("No API key provided. Exiting.");
                    return;
                }
            }

            // Setup services
            var services = new ServiceCollection();

            services.AddElevenLabsVoiceService(config =>
            {
                config.ApiKey = apiKey;
                config.VoiceId = "21m00Tcm4TlvDq8ikWAM"; // Rachel voice
                config.EnableCaching = false;
                config.TimeoutSeconds = 30;
                config.AudioFormat = "pcm_16000"; // Use PCM format for better compatibility
            });

            using var serviceProvider = services.BuildServiceProvider();
            var voiceService = serviceProvider.GetRequiredService<IVoiceService>();

            var testMessage = "Hello! This is Jarvis, your AI assistant. I am now speaking using ElevenLabs text-to-speech technology. This is a 10-second test to verify that voice synthesis is working correctly.";

            try
            {
                Console.WriteLine("Generating speech...");
                var stopwatch = Stopwatch.StartNew();
                
                var audioData = await voiceService.GenerateSpeechAsync(testMessage);
                
                stopwatch.Stop();
                Console.WriteLine($"Speech generated in {stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine($"Audio data size: {audioData.Length} bytes");

                // Save to file with proper WAV headers
                var audioFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "jarvis_test.wav");
                
                // If it's PCM data, create a proper WAV file with headers
                if (serviceProvider.GetRequiredService<JarvisAssistant.Core.Models.ElevenLabsConfig>().AudioFormat.StartsWith("pcm"))
                {
                    var wavData = CreateWavFile(audioData, 16000, 16, 1); // 16kHz, 16-bit, mono
                    await File.WriteAllBytesAsync(audioFile, wavData);
                }
                else
                {
                    // For MP3 and other formats, save with appropriate extension
                    var extension = serviceProvider.GetRequiredService<JarvisAssistant.Core.Models.ElevenLabsConfig>().AudioFormat.Split('_')[0];
                    audioFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"jarvis_test.{extension}");
                    await File.WriteAllBytesAsync(audioFile, audioData);
                }
                
                Console.WriteLine($"Audio saved to: {audioFile}");
                Console.WriteLine();

                // Try to play on Windows
                if (OperatingSystem.IsWindows())
                {
                    Console.WriteLine("Playing audio...");
                    try
                    {
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = audioFile,
                            UseShellExecute = true
                        };
                        Process.Start(startInfo);
                        
                        Console.WriteLine("Audio should be playing now!");
                        Console.WriteLine("Press any key to exit...");
                        Console.ReadKey();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Could not auto-play audio: {ex.Message}");
                        Console.WriteLine($"Please manually open: {audioFile}");
                    }
                }
                else
                {
                    Console.WriteLine($"Please manually play the audio file: {audioFile}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a WAV file with proper headers from PCM audio data.
        /// </summary>
        /// <param name="pcmData">Raw PCM audio data</param>
        /// <param name="sampleRate">Sample rate (e.g., 16000)</param>
        /// <param name="bitsPerSample">Bits per sample (e.g., 16)</param>
        /// <param name="channels">Number of channels (e.g., 1 for mono)</param>
        /// <returns>Complete WAV file data with headers</returns>
        static byte[] CreateWavFile(byte[] pcmData, int sampleRate, short bitsPerSample, short channels)
        {
            var blockAlign = (short)(channels * (bitsPerSample / 8));
            var byteRate = sampleRate * blockAlign;

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            // Write WAV header
            writer.Write("RIFF".ToCharArray());                    // ChunkID
            writer.Write(36 + pcmData.Length);                     // ChunkSize
            writer.Write("WAVE".ToCharArray());                    // Format
            writer.Write("fmt ".ToCharArray());                    // Subchunk1ID
            writer.Write(16);                                      // Subchunk1Size (PCM)
            writer.Write((short)1);                               // AudioFormat (PCM)
            writer.Write(channels);                               // NumChannels
            writer.Write(sampleRate);                             // SampleRate
            writer.Write(byteRate);                               // ByteRate
            writer.Write(blockAlign);                             // BlockAlign
            writer.Write(bitsPerSample);                          // BitsPerSample
            writer.Write("data".ToCharArray());                   // Subchunk2ID
            writer.Write(pcmData.Length);                         // Subchunk2Size
            writer.Write(pcmData);                                // Audio data

            return stream.ToArray();
        }
    }
}
