using Xunit;
using JarvisAssistant.Services;
using System.Threading.Tasks;

namespace JarvisAssistant.UnitTests.Voice
{
    public class WindowsSapiVoiceServiceTests
    {
        [Fact(Skip = "Skipping SAPI tests in this environment")]
        public async Task WindowsSapiVoiceService_Should_GenerateAudio()
        {
            // Arrange
            using var voiceService = new WindowsSapiVoiceService();
            var testText = "Hello, this is a test of the Windows Speech API voice service.";

            // Act
            var audioData = await voiceService.GenerateSpeechAsync(testText);

            // Assert
            Assert.NotNull(audioData);
            Assert.True(audioData.Length > 0);
            
            // Audio should be larger for longer text (rough check)
            Assert.True(audioData.Length > 1000);
        }

        [Fact(Skip = "Skipping SAPI tests in this environment")]
        public async Task WindowsSapiVoiceService_Should_StreamAudio()
        {
            // Arrange
            using var voiceService = new WindowsSapiVoiceService();
            var testText = "This is a streaming test for the Windows SAPI voice service.";
            var chunks = new List<byte[]>();

            // Act
            await foreach (var chunk in voiceService.StreamSpeechAsync(testText))
            {
                chunks.Add(chunk);
            }

            // Assert
            Assert.True(chunks.Count > 0);
            
            var totalBytes = chunks.Sum(c => c.Length);
            Assert.True(totalBytes > 1000);
        }

        [Fact(Skip = "Skipping SAPI tests in this environment")]
        public void WindowsSapiVoiceService_Should_ListAvailableVoices()
        {
            // Arrange
            using var voiceService = new WindowsSapiVoiceService();

            // Act
            var voices = voiceService.GetAvailableVoices();

            // Assert
            Assert.NotNull(voices);
            Assert.True(voices.Length > 0);
            
            foreach (var voice in voices)
            {
                Assert.False(string.IsNullOrWhiteSpace(voice));
            }
        }

        [Fact]
        public async Task WindowsSapiVoiceService_Should_HandleEmptyText()
        {
            // Arrange
            using var voiceService = new WindowsSapiVoiceService();

            // Act
            var audioData = await voiceService.GenerateSpeechAsync("");

            // Assert
            Assert.NotNull(audioData);
        }

        [Fact(Skip = "This test requires audio hardware and is intended for manual validation.")]
        [Trait("Category", "Audible")]
        public async Task WindowsSapiVoiceService_Should_ProduceAudibleSpeech()
        {
            // Arrange
            using var voiceService = new WindowsSapiVoiceService();
            var testText = "Testing Windows Speech API. This should produce clear, audible speech.";

            // Act
            var audioData = await voiceService.GenerateSpeechAsync(testText);

            // Create a WAV file for testing
            var wavData = CreateWavFile(audioData, 16000, 1, 16);
            var testFile = Path.Combine(Path.GetTempPath(), $"windows_sapi_test_{DateTime.Now:yyyyMMdd_HHmmss}.wav");
            
            await File.WriteAllBytesAsync(testFile, wavData);

            // Assert
            Assert.True(File.Exists(testFile), "WAV file should be created");
            Assert.True(new FileInfo(testFile).Length > 1000, "WAV file should contain substantial audio data");

            // Play the audio (Windows only)
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    System.Media.SoundPlayer player = new System.Media.SoundPlayer(testFile);
                    player.PlaySync();
                }
                catch (Exception ex)
                {
                    // Log but don't fail the test if audio playback fails
                    Console.WriteLine($"Audio playback failed: {ex.Message}");
                }
            }

            Console.WriteLine($"Audio file saved to: {testFile}");
        }

        private static byte[] CreateWavFile(byte[] audioData, int sampleRate, int channels, int bitsPerSample)
        {
            using var memStream = new MemoryStream();
            using var writer = new BinaryWriter(memStream);

            // WAV header
            writer.Write("RIFF".ToCharArray());
            writer.Write((int)(36 + audioData.Length));
            writer.Write("WAVE".ToCharArray());
            writer.Write("fmt ".ToCharArray());
            writer.Write(16); // PCM format chunk size
            writer.Write((short)1); // PCM format
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(sampleRate * channels * bitsPerSample / 8); // Byte rate
            writer.Write((short)(channels * bitsPerSample / 8)); // Block align
            writer.Write((short)bitsPerSample);
            writer.Write("data".ToCharArray());
            writer.Write(audioData.Length);
            writer.Write(audioData);

            return memStream.ToArray();
        }
    }
}
