using JarvisAssistant.Core.Interfaces;

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
            
            // Return dummy audio data (in a real implementation, this would be actual audio)
            var dummyAudio = new byte[1024];
            new Random().NextBytes(dummyAudio);
            return dummyAudio;
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<byte[]> StreamSpeechAsync(string text, string? voiceId = null, CancellationToken cancellationToken = default)
        {
            // Simulate streaming speech generation
            var chunks = text.Length / 10 + 1;
            
            for (int i = 0; i < chunks; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;
                
                await Task.Delay(50, cancellationToken);
                
                var chunk = new byte[128];
                new Random().NextBytes(chunk);
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
    }
}
