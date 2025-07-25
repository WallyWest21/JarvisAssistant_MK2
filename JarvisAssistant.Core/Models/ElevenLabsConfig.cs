using System.Text.Json.Serialization;

namespace JarvisAssistant.Core.Models
{
    /// <summary>
    /// Configuration options for ElevenLabs voice service.
    /// </summary>
    public class ElevenLabsConfig
    {
        /// <summary>
        /// ElevenLabs API key.
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// Voice ID to use for Jarvis. If not specified, uses a default British accent voice.
        /// </summary>
        public string? VoiceId { get; set; } = "EXAVITQu4vr4xnSDxMaL"; // Default to British accent voice

        /// <summary>
        /// Base URL for ElevenLabs API.
        /// </summary>
        public string BaseUrl { get; set; } = "https://api.elevenlabs.io";

        /// <summary>
        /// API version to use.
        /// </summary>
        public string ApiVersion { get; set; } = "v1";

        /// <summary>
        /// Default model ID for speech synthesis.
        /// </summary>
        public string ModelId { get; set; } = "eleven_multilingual_v2";

        /// <summary>
        /// Request timeout in seconds.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Maximum number of retry attempts for failed requests.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Enable response caching for repeated phrases.
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// Maximum cache size in MB.
        /// </summary>
        public int MaxCacheSizeMB { get; set; } = 100;

        /// <summary>
        /// Cache expiry time in hours.
        /// </summary>
        public int CacheExpiryHours { get; set; } = 24;

        /// <summary>
        /// Enable streaming for real-time audio.
        /// </summary>
        public bool EnableStreaming { get; set; } = true;

        /// <summary>
        /// Chunk size for streaming audio (in bytes).
        /// </summary>
        public int StreamingChunkSize { get; set; } = 4096;

        /// <summary>
        /// Enable rate limiting protection.
        /// </summary>
        public bool EnableRateLimiting { get; set; } = true;

        /// <summary>
        /// Maximum requests per minute.
        /// </summary>
        public int MaxRequestsPerMinute { get; set; } = 100;

        /// <summary>
        /// Default voice settings for Jarvis.
        /// </summary>
        public VoiceSettings DefaultVoiceSettings { get; set; } = VoiceSettings.CreateJarvisSettings();

        /// <summary>
        /// Enable fallback to local TTS if ElevenLabs is unavailable.
        /// </summary>
        public bool EnableFallback { get; set; } = true;

        /// <summary>
        /// Quality setting for audio output (0-10, higher is better quality but larger files).
        /// </summary>
        public int AudioQuality { get; set; } = 7;

        /// <summary>
        /// Audio format for output.
        /// </summary>
        public string AudioFormat { get; set; } = "mp3_44100_128";

        /// <summary>
        /// Validates the configuration.
        /// </summary>
        /// <returns>True if configuration is valid, false otherwise.</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(ApiKey) &&
                   !string.IsNullOrWhiteSpace(VoiceId) &&
                   !string.IsNullOrWhiteSpace(BaseUrl) &&
                   TimeoutSeconds > 0 &&
                   MaxRetryAttempts >= 0;
        }

        /// <summary>
        /// Gets the complete API URL for text-to-speech endpoint.
        /// </summary>
        /// <returns>Complete API URL.</returns>
        public string GetTextToSpeechUrl()
        {
            return $"{BaseUrl.TrimEnd('/')}/{ApiVersion}/text-to-speech/{VoiceId}";
        }

        /// <summary>
        /// Gets the complete API URL for streaming text-to-speech endpoint.
        /// </summary>
        /// <returns>Complete streaming API URL.</returns>
        public string GetStreamingUrl()
        {
            return $"{BaseUrl.TrimEnd('/')}/{ApiVersion}/text-to-speech/{VoiceId}/stream";
        }

        /// <summary>
        /// Gets the complete API URL for voices endpoint.
        /// </summary>
        /// <returns>Complete voices API URL.</returns>
        public string GetVoicesUrl()
        {
            return $"{BaseUrl.TrimEnd('/')}/{ApiVersion}/voices";
        }
    }
}
