using System;
using System.IO;
using System.Media;
using System.Threading.Tasks;
using JarvisAssistant.Services;
using Microsoft.Extensions.Logging;

class TTSServiceTest
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Testing Jarvis TTS Service Integration...");
        
        try
        {
            // Create logger
            using var loggerFactory = LoggerFactory.Create(builder =>
                builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
            var logger = loggerFactory.CreateLogger<WindowsSapiVoiceService>();
            
            // Create Windows SAPI voice service
            using var voiceService = new WindowsSapiVoiceService(logger);
            
            Console.WriteLine("✅ WindowsSapiVoiceService created");
            
            // Generate speech
            Console.WriteLine("🔊 Generating speech...");
            var audioData = await voiceService.GenerateSpeechAsync("Hello from Jarvis, this is a test message.");
            
            Console.WriteLine($"✅ Generated {audioData.Length} bytes of audio data");
            
            if (audioData.Length > 0)
            {
                // Save to file and play
                var audioFile = Path.Combine(Path.GetTempPath(), "jarvis_test.wav");
                
                // Create WAV file with proper headers
                var wavData = CreateWavFile(audioData, 16000, 1, 16);
                await File.WriteAllBytesAsync(audioFile, wavData);
                
                Console.WriteLine($"📁 Audio saved to: {audioFile} ({wavData.Length} bytes total)");
                
                // Play the audio
                Console.WriteLine("🔊 Playing audio...");
                try
                {
                    using var player = new SoundPlayer(audioFile);
                    player.LoadAsync();
                    player.PlaySync();
                    Console.WriteLine("✅ Audio played successfully!");
                }
                catch (Exception playEx)
                {
                    Console.WriteLine($"❌ Failed to play audio: {playEx.Message}");
                }
                
                // Cleanup
                try
                {
                    File.Delete(audioFile);
                }
                catch { /* ignore cleanup errors */ }
            }
            else
            {
                Console.WriteLine("❌ No audio data generated");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test failed: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
    
    private static byte[] CreateWavFile(byte[] audioData, int sampleRate, int channels, int bitsPerSample)
    {
        var bytesPerSample = bitsPerSample / 8;
        var blockAlign = channels * bytesPerSample;
        var byteRate = sampleRate * blockAlign;

        using var wav = new MemoryStream();
        using var writer = new BinaryWriter(wav);

        // RIFF header
        writer.Write("RIFF".ToCharArray());
        writer.Write(36 + audioData.Length);
        writer.Write("WAVE".ToCharArray());

        // Format chunk
        writer.Write("fmt ".ToCharArray());
        writer.Write(16); // PCM format chunk size
        writer.Write((short)1); // PCM format
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)blockAlign);
        writer.Write((short)bitsPerSample);

        // Data chunk
        writer.Write("data".ToCharArray());
        writer.Write(audioData.Length);
        writer.Write(audioData);

        return wav.ToArray();
    }
}
