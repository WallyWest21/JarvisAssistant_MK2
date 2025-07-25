using System.Text.Json.Serialization;

namespace JarvisAssistant.Core.Models
{
    /// <summary>
    /// Request model for ElevenLabs text-to-speech API.
    /// </summary>
    public class ElevenLabsRequest
    {
        /// <summary>
        /// The text to convert to speech.
        /// </summary>
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Voice settings for the synthesis.
        /// </summary>
        [JsonPropertyName("voice_settings")]
        public VoiceSettings VoiceSettings { get; set; } = new();

        /// <summary>
        /// Model ID to use for synthesis.
        /// </summary>
        [JsonPropertyName("model_id")]
        public string ModelId { get; set; } = "eleven_multilingual_v2";

        /// <summary>
        /// Enable streaming response.
        /// </summary>
        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;

        /// <summary>
        /// Audio output format settings.
        /// </summary>
        [JsonPropertyName("output_format")]
        public string? OutputFormat { get; set; }
    }

    /// <summary>
    /// Response model from ElevenLabs API for quota information.
    /// </summary>
    public class ElevenLabsQuotaResponse
    {
        /// <summary>
        /// Characters remaining in the current quota.
        /// </summary>
        [JsonPropertyName("character_count")]
        public int CharacterCount { get; set; }

        /// <summary>
        /// Maximum characters allowed per month.
        /// </summary>
        [JsonPropertyName("character_limit")]
        public int CharacterLimit { get; set; }

        /// <summary>
        /// Remaining characters in current quota.
        /// </summary>
        [JsonPropertyName("characters_remaining")]
        public int CharactersRemaining { get; set; }

        /// <summary>
        /// When the quota resets.
        /// </summary>
        [JsonPropertyName("next_character_count_reset_unix")]
        public long NextResetUnix { get; set; }

        /// <summary>
        /// Gets the next reset time as a DateTime.
        /// </summary>
        public DateTime NextResetTime => DateTimeOffset.FromUnixTimeSeconds(NextResetUnix).DateTime;

        /// <summary>
        /// Gets the percentage of quota used.
        /// </summary>
        public double QuotaUsedPercentage => CharacterLimit > 0 ? (double)CharacterCount / CharacterLimit * 100 : 0;
    }

    /// <summary>
    /// Voice information from ElevenLabs API.
    /// </summary>
    public class ElevenLabsVoice
    {
        /// <summary>
        /// Voice ID.
        /// </summary>
        [JsonPropertyName("voice_id")]
        public string VoiceId { get; set; } = string.Empty;

        /// <summary>
        /// Voice name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Voice category (e.g., "generated", "cloned", "professional").
        /// </summary>
        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Voice description.
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Voice preview URL.
        /// </summary>
        [JsonPropertyName("preview_url")]
        public string? PreviewUrl { get; set; }

        /// <summary>
        /// Available for fine-tuning.
        /// </summary>
        [JsonPropertyName("fine_tuning")]
        public VoiceFineTuning? FineTuning { get; set; }

        /// <summary>
        /// Voice labels (accent, age, gender, etc.).
        /// </summary>
        [JsonPropertyName("labels")]
        public Dictionary<string, string>? Labels { get; set; }
    }

    /// <summary>
    /// Voice fine-tuning information.
    /// </summary>
    public class VoiceFineTuning
    {
        /// <summary>
        /// Whether fine-tuning is available for this voice.
        /// </summary>
        [JsonPropertyName("is_allowed_to_fine_tune")]
        public bool IsAllowedToFineTune { get; set; }

        /// <summary>
        /// Current fine-tuning state.
        /// </summary>
        [JsonPropertyName("state")]
        public string? State { get; set; }

        /// <summary>
        /// Verification attempts.
        /// </summary>
        [JsonPropertyName("verification_attempts")]
        public int VerificationAttempts { get; set; }

        /// <summary>
        /// Manual verification required.
        /// </summary>
        [JsonPropertyName("manual_verification_requested")]
        public bool ManualVerificationRequested { get; set; }
    }

    /// <summary>
    /// Response containing list of available voices.
    /// </summary>
    public class ElevenLabsVoicesResponse
    {
        /// <summary>
        /// List of available voices.
        /// </summary>
        [JsonPropertyName("voices")]
        public List<ElevenLabsVoice> Voices { get; set; } = new();
    }

    /// <summary>
    /// Error response from ElevenLabs API.
    /// </summary>
    public class ElevenLabsErrorResponse
    {
        /// <summary>
        /// Error detail information.
        /// </summary>
        [JsonPropertyName("detail")]
        public object? Detail { get; set; }

        /// <summary>
        /// Error message.
        /// </summary>
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        /// <summary>
        /// Error code.
        /// </summary>
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        /// <summary>
        /// Gets a formatted error message.
        /// </summary>
        public string GetFormattedMessage()
        {
            if (!string.IsNullOrWhiteSpace(Message))
                return Message;

            if (Detail != null)
                return Detail.ToString() ?? "Unknown error";

            return "An error occurred with the ElevenLabs API";
        }
    }
}
