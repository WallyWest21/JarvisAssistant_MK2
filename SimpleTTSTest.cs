using JarvisAssistant.Services;
using JarvisAssistant.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

/// <summary>
/// Simple test to verify TTS is working end-to-end.
/// </summary>
class SimpleTTSTest
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Testing Jarvis TTS System ===");
        Console.WriteLine();

        // Setup services
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Add Windows SAPI voice service
        services.AddSingleton<IVoiceService, WindowsSapiVoiceService>();

        using var serviceProvider = services.BuildServiceProvider();
        var voiceService = serviceProvider.GetRequiredService<IVoiceService>();
        var logger = serviceProvider.GetRequiredService<ILogger<SimpleTTSTest>>();

        Console.WriteLine("🎤 Testing TTS service...");
        
        try
        {
            var testText = "Hello Sir, this is Jarvis. I am pleased to inform you that the text to speech system is functioning correctly.";
            logger.LogInformation("Generating speech for: '{Text}'", testText);
            
            var audioData = await voiceService.GenerateSpeechAsync(testText);
            
            if (audioData.Length > 0)
            {
                Console.WriteLine($"✅ TTS generated {audioData.Length} bytes of audio data");
                
                // Try to play the audio
                var tempFile = Path.GetTempFileName();
                var audioFile = Path.ChangeExtension(tempFile, ".wav");
                
                try
                {
                    // Create WAV file
                    var wavData = CreateWavFile(audioData, 16000, 1, 16);
                    await File.WriteAllBytesAsync(audioFile, wavData);
                    
                    Console.WriteLine($"💾 Audio saved to: {audioFile}");
                    Console.WriteLine("🔊 Attempting to play audio...");
                    
                    // Try to play using Windows Media Player
                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = audioFile,
                        UseShellExecute = true,
                        CreateNoWindow = true
                    };
                    
                    using var process = System.Diagnostics.Process.Start(startInfo);
                    
                    Console.WriteLine("If you can hear Jarvis speaking, TTS is working correctly!");
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                }
                finally
                {
                    try
                    {
                        if (File.Exists(tempFile)) File.Delete(tempFile);
                        if (File.Exists(audioFile)) File.Delete(audioFile);
                    }
                    catch { /* Ignore cleanup errors */ }
                }
            }
            else
            {
                Console.WriteLine("❌ TTS service returned empty audio data");
                logger.LogError("Voice service returned no audio data");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ TTS test failed: {ex.Message}");
            logger.LogError(ex, "TTS test failed");
        }
    }
    
    static byte[] CreateWavFile(byte[] audioData, int sampleRate, int channels, int bitsPerSample)
    {
        var byteRate = sampleRate * channels * bitsPerSample / 8;
        var blockAlign = channels * bitsPerSample / 8;
        var dataSize = audioData.Length;
        var chunkSize = 36 + dataSize;

        var wavFile = new byte[44 + dataSize];
        var index = 0;

        // RIFF header
        Buffer.BlockCopy(System.Text.Encoding.ASCII.GetBytes("RIFF"), 0, wavFile, index, 4);
        index += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(chunkSize), 0, wavFile, index, 4);
        index += 4;
        Buffer.BlockCopy(System.Text.Encoding.ASCII.GetBytes("WAVE"), 0, wavFile, index, 4);
        index += 4;

        // fmt sub-chunk
        Buffer.BlockCopy(System.Text.Encoding.ASCII.GetBytes("fmt "), 0, wavFile, index, 4);
        index += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(16), 0, wavFile, index, 4);
        index += 4;
        Buffer.BlockCopy(BitConverter.GetBytes((short)1), 0, wavFile, index, 2);
        index += 2;
        Buffer.BlockCopy(BitConverter.GetBytes((short)channels), 0, wavFile, index, 2);
        index += 2;
        Buffer.BlockCopy(BitConverter.GetBytes(sampleRate), 0, wavFile, index, 4);
        index += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(byteRate), 0, wavFile, index, 4);
        index += 4;
        Buffer.BlockCopy(BitConverter.GetBytes((short)blockAlign), 0, wavFile, index, 2);
        index += 2;
        Buffer.BlockCopy(BitConverter.GetBytes((short)bitsPerSample), 0, wavFile, index, 2);
        index += 2;

        // data sub-chunk
        Buffer.BlockCopy(System.Text.Encoding.ASCII.GetBytes("data"), 0, wavFile, index, 4);
        index += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(dataSize), 0, wavFile, index, 4);
        index += 4;
        Buffer.BlockCopy(audioData, 0, wavFile, index, dataSize);

        return wavFile;
    }
}
