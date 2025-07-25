using JarvisAssistant.Core.Interfaces;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

#if WINDOWS
using System.Speech.Synthesis;
using System.Speech.AudioFormat;
#endif

namespace JarvisAssistant.Services
{
    /// <summary>
    /// Windows Speech API (SAPI) voice service implementation.
    /// Uses the built-in Windows text-to-speech engine as a free fallback option.
    /// Only available on Windows platforms.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class WindowsSapiVoiceService : IVoiceService, IDisposable
    {
#if WINDOWS
        private readonly SpeechSynthesizer? _synthesizer;
#endif
        private bool _disposed = false;

        public WindowsSapiVoiceService()
        {
#if WINDOWS
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    _synthesizer = new SpeechSynthesizer();
                    
                    // Configure for better quality
                    _synthesizer.Rate = 0; // Normal speed
                    _synthesizer.Volume = 80; // 80% volume
                }
            }
            catch (Exception)
            {
                // Initialization failed, _synthesizer will remain null
            }
#endif
        }

        /// <inheritdoc/>
        public async Task<byte[]> GenerateSpeechAsync(string text, string? voiceId = null, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(WindowsSapiVoiceService));

            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<byte>();

#if WINDOWS
            if (!OperatingSystem.IsWindows() || _synthesizer == null)
            {
                return Array.Empty<byte>();
            }

            try
            {
                // Set voice if specified
                if (!string.IsNullOrEmpty(voiceId))
                {
                    try
                    {
                        _synthesizer.SelectVoice(voiceId);
                    }
                    catch
                    {
                        // If voice selection fails, use default voice
                    }
                }

                using var memoryStream = new MemoryStream();
                
                // Configure audio format for 16kHz, 16-bit, mono (compatible with our WAV format)
                _synthesizer.SetOutputToAudioStream(memoryStream, 
                    new SpeechAudioFormatInfo(16000, AudioBitsPerSample.Sixteen, AudioChannel.Mono));

                var tcs = new TaskCompletionSource<bool>();
                
                _synthesizer.SpeakCompleted += (sender, e) =>
                {
                    if (e.Error != null)
                        tcs.SetException(e.Error);
                    else if (e.Cancelled)
                        tcs.SetCanceled();
                    else
                        tcs.SetResult(true);
                };

                // Start synthesis
                _synthesizer.SpeakAsync(text);

                // Wait for completion or cancellation
                using (cancellationToken.Register(() => 
                {
                    _synthesizer.SpeakAsyncCancelAll();
                    tcs.TrySetCanceled();
                }))
                {
                    await tcs.Task;
                }

                return memoryStream.ToArray();
            }
            catch (Exception)
            {
                return Array.Empty<byte>();
            }
#else
            await Task.Delay(100, cancellationToken); // Prevent unused parameter warning
            return Array.Empty<byte>();
#endif
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<byte[]> StreamSpeechAsync(string text, string? voiceId = null, 
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(WindowsSapiVoiceService));

            // For Windows SAPI, we'll generate the complete audio and chunk it
            // This is because SAPI doesn't support true streaming synthesis
            var audioData = await GenerateSpeechAsync(text, voiceId, cancellationToken);
            
            const int chunkSize = 4000; // 4KB chunks
            
            for (int i = 0; i < audioData.Length; i += chunkSize)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;
                
                var remainingBytes = Math.Min(chunkSize, audioData.Length - i);
                var chunk = new byte[remainingBytes];
                Array.Copy(audioData, i, chunk, 0, remainingBytes);
                
                yield return chunk;
                
                // Small delay to simulate streaming
                await Task.Delay(50, cancellationToken);
            }
        }

        /// <inheritdoc/>
        public async Task<string> RecognizeSpeechAsync(byte[] audioData, string? language = null, CancellationToken cancellationToken = default)
        {
            // Windows SAPI speech recognition would require additional setup
            // For now, return a placeholder similar to StubVoiceService
            await Task.Delay(300, cancellationToken);
            
            return "Speech recognition not implemented for Windows SAPI service";
        }

        /// <summary>
        /// Gets available voices from Windows Speech Platform.
        /// </summary>
        /// <returns>List of available voice names</returns>
        public string[] GetAvailableVoices()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(WindowsSapiVoiceService));

#if WINDOWS
            if (!OperatingSystem.IsWindows() || _synthesizer == null)
                return Array.Empty<string>();

            try
            {
                return _synthesizer.GetInstalledVoices()
                    .Where(v => v.Enabled)
                    .Select(v => v.VoiceInfo.Name)
                    .ToArray();
            }
            catch (Exception)
            {
                return Array.Empty<string>();
            }
#else
            return Array.Empty<string>();
#endif
        }

        /// <summary>
        /// Sets the speaking rate (speed) of the voice.
        /// </summary>
        /// <param name="rate">Rate from -10 (slowest) to 10 (fastest), 0 is normal</param>
        public void SetRate(int rate)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(WindowsSapiVoiceService));

#if WINDOWS
            if (OperatingSystem.IsWindows() && _synthesizer != null)
            {
                _synthesizer.Rate = Math.Clamp(rate, -10, 10);
            }
#endif
        }

        /// <summary>
        /// Sets the volume of the voice.
        /// </summary>
        /// <param name="volume">Volume from 0 to 100</param>
        public void SetVolume(int volume)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(WindowsSapiVoiceService));

#if WINDOWS
            if (OperatingSystem.IsWindows() && _synthesizer != null)
            {
                _synthesizer.Volume = Math.Clamp(volume, 0, 100);
            }
#endif
        }

        public void Dispose()
        {
            if (!_disposed)
            {
#if WINDOWS
                if (OperatingSystem.IsWindows())
                {
                    _synthesizer?.Dispose();
                }
#endif
                _disposed = true;
            }
        }
    }
}
