using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using JarvisAssistant.Services.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace JarvisAssistant.VoiceTest
{
    /// <summary>
    /// Simple console application to test ElevenLabs voice generation and fallback services
    /// </summary>
    class Program
    {
        private static CancellationTokenSource? _cancellationTokenSource;
        private static ServiceProvider? _serviceProvider;

        static async Task Main(string[] args)
        {
            // Set up cancellation handling for graceful shutdown
            _cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true; // Prevent immediate termination
                Console.WriteLine("\nShutdown requested. Cleaning up...");
                _cancellationTokenSource.Cancel();
            };

            try
            {
                await RunApplicationAsync(args);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Application was cancelled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unhandled error: {ex.Message}");
            }
            finally
            {
                // Clean up resources
                _serviceProvider?.Dispose();
                _cancellationTokenSource?.Dispose();
                Console.WriteLine("Cleanup completed. Press any key to exit...");
                Console.ReadKey();
            }
        }

        static async Task RunApplicationAsync(string[] args)
        {
            Console.WriteLine("=== Jarvis Voice Test Suite ===");
            Console.WriteLine();
            
            // First test environment variable reading
            VoiceServiceStatusTest.TestEnvironmentVariableReading();
            
            Console.WriteLine();
            Console.WriteLine("🎤 Testing Voice Service Configuration...");
            Console.WriteLine("=========================================");
            
            // Test the service configuration
            await VoiceServiceConfigTest.TestVoiceServiceAsync();
            
            Console.WriteLine();
            Console.WriteLine("Select test mode:");
            Console.WriteLine("1. ElevenLabs API Test (requires API key)");
            Console.WriteLine("2. Fallback Services Test (free, no API key required)");
            Console.WriteLine("3. Simple Fallback Test (basic functionality test)");
            Console.WriteLine("4. Windows SAPI TTS Test (direct TTS test)");
            Console.WriteLine("5. Exit");
            Console.WriteLine();
            Console.Write("Enter choice (1-5): ");

            var choice = Console.ReadLine();
            
            switch (choice)
            {
                case "1":
                    await RunElevenLabsTest(args);
                    break;
                case "2":
                    await FallbackTestProgram.RunAsync(args);
                    break;
                case "3":
                    await SimpleFallbackTest.RunAsync(args);
                    break;
                case "4":
                    await TTSTestProgram.RunAsync(args);
                    break;
                case "5":
                    Console.WriteLine("Exiting...");
                    break;
                default:
                    Console.WriteLine("Invalid choice. Running simple fallback test by default...");
                    await SimpleFallbackTest.RunAsync(args);
                    break;
            }
        }

        static async Task RunElevenLabsTest(string[] args)
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

            // Store service provider for proper disposal
            _serviceProvider = services.BuildServiceProvider();
            var voiceService = _serviceProvider.GetRequiredService<IVoiceService>();

            var testMessage = "Hello! This is Jarvis, your AI assistant. I am now speaking using ElevenLabs text-to-speech technology. This is a 10-second test to verify that voice synthesis is working correctly.";

            try
            {
                // Check for cancellation
                _cancellationTokenSource?.Token.ThrowIfCancellationRequested();

                Console.WriteLine("Generating speech...");
                var stopwatch = Stopwatch.StartNew();
                
                var audioData = await voiceService.GenerateSpeechAsync(testMessage);
                
                stopwatch.Stop();
                Console.WriteLine($"Speech generated in {stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine($"Audio data size: {audioData.Length} bytes");

                // Save to file with proper WAV headers
                var audioFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "jarvis_test.wav");
                
                // If it's PCM data, create a proper WAV file with headers
                if (_serviceProvider.GetRequiredService<JarvisAssistant.Core.Models.ElevenLabsConfig>().AudioFormat.StartsWith("pcm"))
                {
                    var wavData = CreateWavFile(audioData, 16000, 16, 1); // 16kHz, 16-bit, mono
                    await File.WriteAllBytesAsync(audioFile, wavData, _cancellationTokenSource?.Token ?? CancellationToken.None);
                }
                else
                {
                    // For MP3 and other formats, save with appropriate extension
                    var extension = _serviceProvider.GetRequiredService<JarvisAssistant.Core.Models.ElevenLabsConfig>().AudioFormat.Split('_')[0];
                    audioFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"jarvis_test.{extension}");
                    await File.WriteAllBytesAsync(audioFile, audioData, _cancellationTokenSource?.Token ?? CancellationToken.None);
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
                        
                        using var process = Process.Start(startInfo);
                        
                        Console.WriteLine("Audio should be playing now!");
                        Console.WriteLine("Press any key to exit...");
                        
                        // Wait for key press or cancellation
                        await WaitForKeyOrCancellationAsync();
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
            catch (OperationCanceledException)
            {
                Console.WriteLine("Operation was cancelled.");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Waits for a key press or cancellation token
        /// </summary>
        private static async Task WaitForKeyOrCancellationAsync()
        {
            var tcs = new TaskCompletionSource<bool>();
            
            // Set up key reading on a background thread
            var keyTask = Task.Run(() =>
            {
                try
                {
                    Console.ReadKey();
                    tcs.TrySetResult(true);
                }
                catch
                {
                    tcs.TrySetResult(false);
                }
            });

            // Wait for either key press or cancellation
            try
            {
                await Task.WhenAny(
                    tcs.Task,
                    Task.Delay(-1, _cancellationTokenSource?.Token ?? CancellationToken.None)
                );
            }
            catch (OperationCanceledException)
            {
                // Cancellation was requested
                throw;
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
