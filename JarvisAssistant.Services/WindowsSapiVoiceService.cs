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
                    // Initialize Windows audio system first
                    InitializeWindowsAudio();
                    
                    _synthesizer = new SpeechSynthesizer();
                    
                    // Configure for better quality and compatibility
                    _synthesizer.Rate = 0; // Normal speed
                    _synthesizer.Volume = 80; // 80% volume
                    
                    // Test if TTS is working by getting installed voices
                    var voices = _synthesizer.GetInstalledVoices();
                    System.Diagnostics.Debug.WriteLine($"WindowsSapiVoiceService: Found {voices.Count} installed voices");
                    
                    // Try to select a good quality voice if available
                    var englishVoice = voices.FirstOrDefault(v => v.VoiceInfo.Culture.TwoLetterISOLanguageName == "en");
                    if (englishVoice != null)
                    {
                        _synthesizer.SelectVoice(englishVoice.VoiceInfo.Name);
                        System.Diagnostics.Debug.WriteLine($"WindowsSapiVoiceService: Selected voice: {englishVoice.VoiceInfo.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WindowsSapiVoiceService: Initialization failed: {ex.Message}");
                // Initialization failed, _synthesizer will remain null
            }
#endif
        }

        /// <summary>
        /// Initialize Windows audio system to ensure TTS works properly
        /// </summary>
        private static void InitializeWindowsAudio()
        {
#if WINDOWS
            try
            {
                // Force Windows to initialize the audio subsystem
                // This helps prevent the "beeping" issue
                using var testSynth = new SpeechSynthesizer();
                testSynth.SetOutputToDefaultAudioDevice();
                
                // Attempt a very short test to initialize audio
                testSynth.SpeakAsync(" "); // Single space - minimal audio
                System.Threading.Thread.Sleep(100); // Give it a moment
                testSynth.SpeakAsyncCancelAll();
                
                System.Diagnostics.Debug.WriteLine("WindowsSapiVoiceService: Windows audio system initialized");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WindowsSapiVoiceService: Audio initialization warning: {ex.Message}");
                // Non-fatal - continue anyway
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

            System.Diagnostics.Debug.WriteLine($"WindowsSapiVoiceService: Generating speech for text: '{text.Substring(0, Math.Min(50, text.Length))}'");

#if WINDOWS
            if (!OperatingSystem.IsWindows() || _synthesizer == null)
            {
                System.Diagnostics.Debug.WriteLine("WindowsSapiVoiceService: Not on Windows or synthesizer is null");
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
                        System.Diagnostics.Debug.WriteLine($"WindowsSapiVoiceService: Selected voice: {voiceId}");
                    }
                    catch
                    {
                        // If voice selection fails, use default voice
                        System.Diagnostics.Debug.WriteLine($"WindowsSapiVoiceService: Failed to select voice '{voiceId}', using default");
                    }
                }

                using var memoryStream = new MemoryStream();
                
                // Configure audio format for 22kHz, 16-bit, mono (better compatibility than 16kHz)
                _synthesizer.SetOutputToAudioStream(memoryStream, 
                    new SpeechAudioFormatInfo(22050, AudioBitsPerSample.Sixteen, AudioChannel.Mono));

                var tcs = new TaskCompletionSource<bool>();
                
                _synthesizer.SpeakCompleted += (sender, e) =>
                {
                    if (e.Error != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"WindowsSapiVoiceService: Synthesis error: {e.Error.Message}");
                        tcs.SetException(e.Error);
                    }
                    else if (e.Cancelled)
                    {
                        System.Diagnostics.Debug.WriteLine("WindowsSapiVoiceService: Synthesis cancelled");
                        tcs.SetCanceled();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"WindowsSapiVoiceService: Synthesis completed, generated {memoryStream.Length} bytes");
                        tcs.SetResult(true);
                    }
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

                var audioData = memoryStream.ToArray();
                System.Diagnostics.Debug.WriteLine($"WindowsSapiVoiceService: Returning {audioData.Length} bytes of audio data");
                return audioData;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WindowsSapiVoiceService: Exception during synthesis: {ex.Message}");
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
