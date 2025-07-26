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
    /// Direct Windows Speech API voice service that bypasses WAV file generation
    /// to avoid beeping issues. Uses direct speaker output instead.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class DirectWindowsVoiceService : IVoiceService, IDisposable
    {
#if WINDOWS
        private readonly SpeechSynthesizer? _synthesizer;
#endif
        private bool _disposed = false;

        public DirectWindowsVoiceService()
        {
#if WINDOWS
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    _synthesizer = new SpeechSynthesizer();
                    
                    // Configure for direct output (no WAV files)
                    _synthesizer.SetOutputToDefaultAudioDevice();
                    _synthesizer.Rate = 0; // Normal speed
                    _synthesizer.Volume = 80; // 80% volume
                    
                    System.Diagnostics.Debug.WriteLine("DirectWindowsVoiceService: Initialized for direct audio output");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DirectWindowsVoiceService: Initialization failed: {ex.Message}");
            }
#endif
        }

        /// <inheritdoc/>
        public async Task<byte[]> GenerateSpeechAsync(string text, string? voiceId = null, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(DirectWindowsVoiceService));

            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<byte>();

#if WINDOWS
            if (!OperatingSystem.IsWindows() || _synthesizer == null)
            {
                return Array.Empty<byte>();
            }

            try
            {
                // For this service, we'll speak directly instead of generating audio data
                // This avoids the WAV file beeping issue entirely
                await Task.Run(() =>
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"DirectWindowsVoiceService: Speaking directly: '{text.Substring(0, Math.Min(50, text.Length))}'");
                        
                        // Set voice if specified
                        if (!string.IsNullOrEmpty(voiceId))
                        {
                            try
                            {
                                _synthesizer.SelectVoice(voiceId);
                            }
                            catch
                            {
                                // Voice selection failed, use default
                            }
                        }
                        
                        // Speak directly to audio device (no WAV files, no beeping)
                        _synthesizer.Speak(text);
                        
                        System.Diagnostics.Debug.WriteLine("DirectWindowsVoiceService: Direct speech completed successfully");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"DirectWindowsVoiceService: Direct speech failed: {ex.Message}");
                    }
                }, cancellationToken);

                // Return empty array since we spoke directly (no audio data to return)
                return Array.Empty<byte>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DirectWindowsVoiceService: Exception: {ex.Message}");
                return Array.Empty<byte>();
            }
#else
            await Task.Delay(100, cancellationToken);
            return Array.Empty<byte>();
#endif
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<byte[]> StreamSpeechAsync(string text, string? voiceId = null, 
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(DirectWindowsVoiceService));

            // For direct speech, we don't stream - we speak the entire text at once
            await GenerateSpeechAsync(text, voiceId, cancellationToken);
            yield break;
        }

        /// <inheritdoc/>
        public async Task<string> RecognizeSpeechAsync(byte[] audioData, string? language = null, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(DirectWindowsVoiceService));

            if (audioData == null || audioData.Length == 0)
                return string.Empty;

#if WINDOWS
            if (!OperatingSystem.IsWindows())
            {
                return string.Empty;
            }

            try
            {
                // Note: DirectWindowsVoiceService focuses on TTS (text-to-speech).
                // For speech recognition (speech-to-text), Windows has limited built-in options.
                // In a production environment, you might want to integrate with:
                // - Windows Speech Recognition APIs
                // - Azure Cognitive Services Speech SDK
                // - Other cloud-based speech recognition services
                
                await Task.Delay(100, cancellationToken); // Simulate processing time
                
                System.Diagnostics.Debug.WriteLine("DirectWindowsVoiceService: Speech recognition not fully implemented - this is primarily a TTS service");
                
                // Return a placeholder result indicating that speech recognition isn't implemented
                return string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DirectWindowsVoiceService: Speech recognition error: {ex.Message}");
                return string.Empty;
            }
#else
            await Task.Delay(100, cancellationToken);
            return string.Empty;
#endif
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<VoiceInfo>> GetAvailableVoicesAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(DirectWindowsVoiceService));

#if WINDOWS
            if (!OperatingSystem.IsWindows() || _synthesizer == null)
            {
                return Enumerable.Empty<VoiceInfo>();
            }

            return await Task.Run(() =>
            {
                try
                {
                    var voices = _synthesizer.GetInstalledVoices()
                        .Where(v => v.VoiceInfo.Enabled)
                        .Select(v => new VoiceInfo
                        {
                            Id = v.VoiceInfo.Name,
                            Name = v.VoiceInfo.Name,
                            Language = v.VoiceInfo.Culture.Name,
                            Gender = v.VoiceInfo.Gender.ToString()
                        })
                        .ToList();

                    System.Diagnostics.Debug.WriteLine($"DirectWindowsVoiceService: Found {voices.Count} available voices");
                    return voices;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"DirectWindowsVoiceService: Error getting voices: {ex.Message}");
                    return Enumerable.Empty<VoiceInfo>();
                }
            }, cancellationToken);
#else
            await Task.Delay(100, cancellationToken);
            return Enumerable.Empty<VoiceInfo>();
#endif
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_disposed)
            {
#if WINDOWS
                _synthesizer?.Dispose();
#endif
                _disposed = true;
                System.Diagnostics.Debug.WriteLine("DirectWindowsVoiceService: Disposed");
            }
        }
    }

    /// <summary>
    /// Voice information for available TTS voices
    /// </summary>
    public class VoiceInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
    }
}
