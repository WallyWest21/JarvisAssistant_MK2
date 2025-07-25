using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using JarvisAssistant.Services;
using JarvisAssistant.Services.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Media;
using Xunit;
using Xunit.Abstractions;

namespace JarvisAssistant.UnitTests.Voice
{
    /// <summary>
    /// Audible tests for ElevenLabs voice service that actually generate and play sound.
    /// These tests require valid ElevenLabs API credentials and will make real API calls.
    /// </summary>
    public class ElevenLabsAudibleTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly ITestOutputHelper _output;
        private bool _disposed = false;

        public ElevenLabsAudibleTests(ITestOutputHelper output)
        {
            _output = output;
            
            var services = new ServiceCollection();
            
            // Add logging with debug output
            services.AddLogging(builder => 
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            
            // Configure ElevenLabs with real API key (you'll need to set this)
            services.AddElevenLabsVoiceService(config =>
            {
                // IMPORTANT: Replace with your actual ElevenLabs API key
                config.ApiKey = Environment.GetEnvironmentVariable("ELEVENLABS_API_KEY") ?? "your-actual-api-key-here";
                config.VoiceId = "21m00Tcm4TlvDq8ikWAM"; // Rachel voice ID
                config.EnableCaching = false; // Disable cache for real testing
                config.EnableFallback = true;
                config.TimeoutSeconds = 30;
                config.ModelId = "eleven_monolingual_v1";
                config.AudioFormat = "pcm_16000"; // Use PCM for better compatibility
            });

            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        [Trait("Category", "Audible")]
        [Trait("Category", "Manual")]
        public async Task ElevenLabs_GenerateAndPlayAudio_10Seconds()
        {
            // Skip if no API key is configured
            var config = _serviceProvider.GetRequiredService<ElevenLabsConfig>();
            if (string.IsNullOrWhiteSpace(config.ApiKey) || config.ApiKey == "your-actual-api-key-here")
            {
                _output.WriteLine("Skipping test - No valid ElevenLabs API key configured");
                _output.WriteLine("Set ELEVENLABS_API_KEY environment variable or update the test configuration");
                return;
            }

            // Arrange
            var voiceService = _serviceProvider.GetRequiredService<IVoiceService>();
            var logger = _serviceProvider.GetRequiredService<ILogger<ElevenLabsAudibleTests>>();
            
            var testText = @"Hello! This is Jarvis, your AI assistant. I am now generating speech using ElevenLabs text-to-speech technology. 
                           This test will run for approximately 10 seconds to demonstrate that the voice synthesis is working correctly. 
                           You should be able to hear this message clearly through your speakers or headphones. 
                           If you can hear this, then the ElevenLabs integration is functioning properly.";

            _output.WriteLine($"Testing with text: {testText}");
            _output.WriteLine($"Using API Key: {config.ApiKey[..8]}...");
            _output.WriteLine($"Using Voice ID: {config.VoiceId}");

            // Act
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                _output.WriteLine("Generating speech with ElevenLabs...");
                var audioData = await voiceService.GenerateSpeechAsync(testText);
                
                stopwatch.Stop();
                _output.WriteLine($"Speech generation completed in {stopwatch.ElapsedMilliseconds}ms");
                _output.WriteLine($"Generated {audioData.Length} bytes of audio data");

                // Assert audio was generated
                Assert.NotNull(audioData);
                Assert.True(audioData.Length > 0, "Audio data should not be empty");
                Assert.True(audioData.Length > 1000, "Audio data should be substantial for a 10-second message");

                // Save audio to temporary file for playback
                var tempAudioFile = Path.GetTempFileName() + ".wav";
                try
                {
                    // Create proper WAV file with headers for PCM data
                    var audioConfig = _serviceProvider.GetRequiredService<ElevenLabsConfig>();
                    byte[] fileData;
                    
                    if (audioConfig.AudioFormat.StartsWith("pcm"))
                    {
                        // PCM format - create WAV file with headers
                        fileData = CreateWavFile(audioData, 16000, 16, 1); // 16kHz, 16-bit, mono
                    }
                    else
                    {
                        // Other formats (MP3, etc.) - save as-is but with correct extension
                        var extension = audioConfig.AudioFormat.Split('_')[0];
                        tempAudioFile = Path.GetTempFileName() + $".{extension}";
                        fileData = audioData;
                    }
                    
                    await File.WriteAllBytesAsync(tempAudioFile, fileData);
                    _output.WriteLine($"Audio saved to: {tempAudioFile}");

                    // Try to play the audio using system default player
                    if (OperatingSystem.IsWindows())
                    {
                        _output.WriteLine("Playing audio on Windows...");
                        await PlayAudioWindows(tempAudioFile);
                    }
                    else
                    {
                        _output.WriteLine($"Audio file saved. Manual playback required on this platform: {tempAudioFile}");
                    }

                    _output.WriteLine("Audio playback test completed successfully!");
                }
                finally
                {
                    // Clean up temporary file
                    if (File.Exists(tempAudioFile))
                    {
                        File.Delete(tempAudioFile);
                    }
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Test failed with exception: {ex.Message}");
                logger.LogError(ex, "Audio generation test failed");
                throw;
            }
        }

        [Fact]
        [Trait("Category", "Audible")]
        [Trait("Category", "Manual")]
        public async Task ElevenLabs_StreamAndPlayAudio_10Seconds()
        {
            // Skip if no API key is configured
            var config = _serviceProvider.GetRequiredService<ElevenLabsConfig>();
            if (string.IsNullOrWhiteSpace(config.ApiKey) || config.ApiKey == "your-actual-api-key-here")
            {
                _output.WriteLine("Skipping streaming test - No valid ElevenLabs API key configured");
                return;
            }

            // Arrange
            var voiceService = _serviceProvider.GetRequiredService<IVoiceService>();
            var testText = @"This is a streaming audio test with Jarvis AI assistant. 
                           The audio is being generated and streamed in real-time using ElevenLabs technology. 
                           Each chunk of audio is processed as it arrives, allowing for lower latency playback. 
                           This streaming approach is ideal for interactive voice applications.";

            _output.WriteLine("Starting streaming audio test...");

            // Act
            var audioChunks = new List<byte[]>();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await foreach (var chunk in voiceService.StreamSpeechAsync(testText))
                {
                    audioChunks.Add(chunk);
                    _output.WriteLine($"Received audio chunk: {chunk.Length} bytes");
                    
                    // Break after reasonable amount of chunks for testing
                    if (audioChunks.Count > 50) break;
                }

                stopwatch.Stop();
                var totalBytes = audioChunks.Sum(c => c.Length);
                
                _output.WriteLine($"Streaming completed in {stopwatch.ElapsedMilliseconds}ms");
                _output.WriteLine($"Received {audioChunks.Count} chunks totaling {totalBytes} bytes");

                // Assert streaming worked
                Assert.NotEmpty(audioChunks);
                Assert.True(totalBytes > 0, "Should have received audio data");

                // Combine chunks for playback
                var combinedAudio = new byte[totalBytes];
                int offset = 0;
                foreach (var chunk in audioChunks)
                {
                    Array.Copy(chunk, 0, combinedAudio, offset, chunk.Length);
                    offset += chunk.Length;
                }

                // Save and play combined audio
                var tempAudioFile = Path.GetTempFileName() + ".wav";
                try
                {
                    // Create proper WAV file with headers for PCM data
                    var audioConfig = _serviceProvider.GetRequiredService<ElevenLabsConfig>();
                    byte[] fileData;
                    
                    if (audioConfig.AudioFormat.StartsWith("pcm"))
                    {
                        // PCM format - create WAV file with headers
                        fileData = CreateWavFile(combinedAudio, 16000, 16, 1); // 16kHz, 16-bit, mono
                    }
                    else
                    {
                        // Other formats (MP3, etc.) - save as-is but with correct extension
                        var extension = audioConfig.AudioFormat.Split('_')[0];
                        tempAudioFile = Path.GetTempFileName() + $".{extension}";
                        fileData = combinedAudio;
                    }
                    
                    await File.WriteAllBytesAsync(tempAudioFile, fileData);
                    _output.WriteLine($"Streamed audio saved to: {tempAudioFile}");

                    if (OperatingSystem.IsWindows())
                    {
                        await PlayAudioWindows(tempAudioFile);
                    }

                    _output.WriteLine("Streaming audio test completed successfully!");
                }
                finally
                {
                    if (File.Exists(tempAudioFile))
                    {
                        File.Delete(tempAudioFile);
                    }
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Streaming test failed: {ex.Message}");
                throw;
            }
        }

        private async Task PlayAudioWindows(string audioFile)
        {
            try
            {
                // Use Windows Media Player to play the audio
                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"Add-Type -AssemblyName presentationCore; $player = New-Object System.Windows.Media.MediaPlayer; $player.Open('{audioFile}'); $player.Play(); Start-Sleep -Seconds 12; $player.Stop()\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    _output.WriteLine("Audio playback completed");
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Audio playback failed: {ex.Message}");
                
                // Fallback: just indicate the file location
                _output.WriteLine($"Please manually play the audio file: {audioFile}");
            }
        }

        /// <summary>
        /// Creates a WAV file with proper headers from PCM audio data.
        /// </summary>
        private static byte[] CreateWavFile(byte[] pcmData, int sampleRate, short bitsPerSample, short channels)
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

        public void Dispose()
        {
            if (!_disposed)
            {
                _serviceProvider?.Dispose();
                _disposed = true;
            }
        }
    }
}
