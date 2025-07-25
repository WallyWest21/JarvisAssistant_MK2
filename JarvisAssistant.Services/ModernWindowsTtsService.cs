using JarvisAssistant.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

#if WINDOWS
using System.Speech.Synthesis;
using System.Speech.AudioFormat;
#endif

namespace JarvisAssistant.Services
{
    /// <summary>
    /// Enhanced Windows Text-to-Speech service using System.Speech with improved features.
    /// Provides higher quality configuration and better error handling compared to basic SAPI service.
    /// Only available on Windows platforms.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class ModernWindowsTtsService : IVoiceService, IDisposable
    {
        private readonly ILogger<ModernWindowsTtsService> _logger;
#if WINDOWS
        private readonly SpeechSynthesizer? _synthesizer;
#endif
        private bool _disposed = false;

        public ModernWindowsTtsService(ILogger<ModernWindowsTtsService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

#if WINDOWS
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    _synthesizer = new SpeechSynthesizer();
                    
                    // Configure for optimal quality
                    _synthesizer.Rate = -1; // Slightly slower for better clarity
                    _synthesizer.Volume = 85; // High volume but not max to avoid distortion
                    
                    _logger.LogInformation("Initialized Enhanced Windows TTS service using System.Speech");
                }
                else
                {
                    _logger.LogWarning("Enhanced Windows TTS service is only available on Windows platforms");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Enhanced Windows TTS service");
            }
#else
            _logger.LogWarning("Enhanced Windows TTS service is not available on this platform");
#endif
        }

        /// <inheritdoc/>
        public async Task<byte[]> GenerateSpeechAsync(string text, string? voiceId = null, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ModernWindowsTtsService));

            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<byte>();

#if WINDOWS
            if (!OperatingSystem.IsWindows() || _synthesizer == null)
            {
                _logger.LogWarning("Windows TTS not available on this platform");
                return Array.Empty<byte>();
            }

            try
            {
                // Set voice if specified
                if (!string.IsNullOrEmpty(voiceId))
                {
                    try
                    {
                        // Try to select by exact name first, then by partial match
                        var availableVoices = _synthesizer.GetInstalledVoices()
                            .Where(v => v.Enabled)
                            .ToList();

                        var exactMatch = availableVoices.FirstOrDefault(v => 
                            v.VoiceInfo.Name.Equals(voiceId, StringComparison.OrdinalIgnoreCase));

                        if (exactMatch != null)
                        {
                            _synthesizer.SelectVoice(exactMatch.VoiceInfo.Name);
                            _logger.LogDebug("Selected voice: {VoiceName}", exactMatch.VoiceInfo.Name);
                        }
                        else
                        {
                            var partialMatch = availableVoices.FirstOrDefault(v => 
                                v.VoiceInfo.Name.Contains(voiceId, StringComparison.OrdinalIgnoreCase));

                            if (partialMatch != null)
                            {
                                _synthesizer.SelectVoice(partialMatch.VoiceInfo.Name);
                                _logger.LogDebug("Selected voice by partial match: {VoiceName}", partialMatch.VoiceInfo.Name);
                            }
                            else
                            {
                                _logger.LogWarning("Voice '{VoiceId}' not found, using default", voiceId);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to select voice '{VoiceId}', using default", voiceId);
                    }
                }

                using var memoryStream = new MemoryStream();
                
                // Configure audio format for high quality 22kHz, 16-bit, mono (better than 16kHz)
                _synthesizer.SetOutputToAudioStream(memoryStream, 
                    new SpeechAudioFormatInfo(22050, AudioBitsPerSample.Sixteen, AudioChannel.Mono));

                // Use TaskCompletionSource to make the synchronous call async
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

                // Register cancellation
                using var registration = cancellationToken.Register(() =>
                {
                    _synthesizer.SpeakAsyncCancelAll();
                    tcs.TrySetCanceled();
                });

                // Start synthesis
                _synthesizer.SpeakAsync(text);
                
                // Wait for completion
                await tcs.Task;

                var audioData = memoryStream.ToArray();
                _logger.LogDebug("Generated {AudioLength} bytes of audio using Enhanced Windows TTS", audioData.Length);
                
                return audioData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating speech with Enhanced Windows TTS");
                return Array.Empty<byte>();
            }
#else
            await Task.Delay(100, cancellationToken); // Prevent unused parameter warning
            _logger.LogWarning("Windows TTS not available on this platform");
            return Array.Empty<byte>();
#endif
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<byte[]> StreamSpeechAsync(string text, string? voiceId = null, 
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ModernWindowsTtsService));

            if (string.IsNullOrWhiteSpace(text))
                yield break;

            // For streaming, generate the full audio and chunk it
            var audioData = await GenerateSpeechAsync(text, voiceId, cancellationToken);
            
            if (audioData.Length == 0)
                yield break;

            // Stream in 8KB chunks for better performance
            const int chunkSize = 8192;
            for (int i = 0; i < audioData.Length; i += chunkSize)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;

                var remainingBytes = Math.Min(chunkSize, audioData.Length - i);
                var chunk = new byte[remainingBytes];
                Array.Copy(audioData, i, chunk, 0, remainingBytes);
                
                yield return chunk;
                
                // Minimal delay for streaming feel
                await Task.Delay(25, cancellationToken);
            }
        }

        /// <inheritdoc/>
        public async Task<string> RecognizeSpeechAsync(byte[] audioData, string? language = null, CancellationToken cancellationToken = default)
        {
            // Speech recognition would require additional setup and is not implemented
            // This is a TTS-focused service
            await Task.Delay(100, cancellationToken);
            return "Speech recognition not implemented in Enhanced Windows TTS service";
        }

        /// <summary>
        /// Gets available voices from Windows Speech Platform.
        /// </summary>
        /// <returns>List of available voice information</returns>
        public string[] GetAvailableVoices()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ModernWindowsTtsService));

#if WINDOWS
            if (!OperatingSystem.IsWindows() || _synthesizer == null)
                return Array.Empty<string>();

            try
            {
                return _synthesizer.GetInstalledVoices()
                    .Where(v => v.Enabled)
                    .Select(v => $"{v.VoiceInfo.Name} ({v.VoiceInfo.Culture.Name}) - {v.VoiceInfo.Gender}")
                    .OrderBy(name => name)
                    .ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available voices");
                return Array.Empty<string>();
            }
#else
            return Array.Empty<string>();
#endif
        }

        /// <summary>
        /// Gets information about the active TTS engine.
        /// </summary>
        /// <returns>TTS engine information</returns>
        public string GetEngineInfo()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ModernWindowsTtsService));

#if WINDOWS
            return OperatingSystem.IsWindows() && _synthesizer != null 
                ? "Enhanced Windows System.Speech TTS Engine (High Quality)" 
                : "Windows TTS not available";
#else
            return "Windows TTS not available on this platform";
#endif
        }

        /// <summary>
        /// Sets the speaking rate (speed) of the voice.
        /// </summary>
        /// <param name="rate">Rate from -10 (slowest) to 10 (fastest), 0 is normal</param>
        public void SetRate(int rate)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ModernWindowsTtsService));

#if WINDOWS
            if (OperatingSystem.IsWindows() && _synthesizer != null)
            {
                _synthesizer.Rate = Math.Clamp(rate, -10, 10);
                _logger.LogDebug("Set TTS rate to {Rate}", rate);
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
                throw new ObjectDisposedException(nameof(ModernWindowsTtsService));

#if WINDOWS
            if (OperatingSystem.IsWindows() && _synthesizer != null)
            {
                _synthesizer.Volume = Math.Clamp(volume, 0, 100);
                _logger.LogDebug("Set TTS volume to {Volume}", volume);
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
                _logger.LogDebug("Enhanced Windows TTS service disposed");
            }
        }
    }
}
