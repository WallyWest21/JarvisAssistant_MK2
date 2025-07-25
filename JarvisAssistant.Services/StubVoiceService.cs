using JarvisAssistant.Core.Interfaces;
using System.Runtime.CompilerServices;

namespace JarvisAssistant.Services
{
    /// <summary>
    /// Stub implementation of the voice service for testing and demonstration purposes.
    /// This should be replaced with a real implementation that integrates with actual TTS/STT services.
    /// </summary>
    public class StubVoiceService : IVoiceService
    {
        /// <inheritdoc/>
        public async Task<byte[]> GenerateSpeechAsync(string text, string? voiceId = null, CancellationToken cancellationToken = default)
        {
            // Simulate speech generation delay
            await Task.Delay(200, cancellationToken);
            
            // Generate a simple audible tone instead of random noise for testing
            return GenerateTestTone(text.Length * 100 + 2000); // Duration based on text length
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<byte[]> StreamSpeechAsync(string text, string? voiceId = null, 
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // Simulate streaming speech generation
            var chunks = text.Length / 10 + 1;
            
            for (int i = 0; i < chunks; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;
                
                await Task.Delay(50, cancellationToken);
                
                // Generate a short tone chunk instead of random noise
                var chunk = GenerateTestTone(200); // 200ms tone chunks
                yield return chunk;
            }
        }

        /// <inheritdoc/>
        public async Task<string> RecognizeSpeechAsync(byte[] audioData, string? language = null, CancellationToken cancellationToken = default)
        {
            // Simulate speech recognition delay
            await Task.Delay(300, cancellationToken);
            
            // Return a dummy recognized text (in a real implementation, this would process the audio)
            var sampleCommands = new[]
            {
                "what's my status",
                "generate code",
                "help me",
                "analyze this",
                "search for something",
                "open settings",
                "stop",
                "hey jarvis"
            };
            
            var random = new Random();
            return sampleCommands[random.Next(sampleCommands.Length)];
        }

        /// <summary>
        /// Generates a simple audible test tone instead of random noise.
        /// </summary>
        /// <param name="durationMs">Duration in milliseconds</param>
        /// <returns>PCM audio data for a simple tone</returns>
        private static byte[] GenerateTestTone(int durationMs = 2000)
        {
            const int sampleRate = 16000; // 16kHz
            const double frequency = 440.0; // A4 note
            const short amplitude = 8000; // Volume level
            
            int samples = (sampleRate * durationMs) / 1000;
            var audioData = new byte[samples * 2]; // 16-bit = 2 bytes per sample
            
            for (int i = 0; i < samples; i++)
            {
                // Generate sine wave
                double time = (double)i / sampleRate;
                double sineWave = Math.Sin(2 * Math.PI * frequency * time);
                short sample = (short)(sineWave * amplitude);
                
                // Convert to bytes (little-endian)
                audioData[i * 2] = (byte)(sample & 0xFF);
                audioData[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
            }
            
            return audioData;
        }
    }
}
