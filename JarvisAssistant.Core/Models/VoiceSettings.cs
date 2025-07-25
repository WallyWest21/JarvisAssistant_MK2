using System.Text.Json.Serialization;

namespace JarvisAssistant.Core.Models
{
    /// <summary>
    /// Represents voice synthesis settings for ElevenLabs API.
    /// </summary>
    public class VoiceSettings
    {
        /// <summary>
        /// Voice stability setting (0.0 to 1.0). Higher values make the voice more stable and consistent.
        /// </summary>
        [JsonPropertyName("stability")]
        public float Stability { get; set; } = 0.75f;

        /// <summary>
        /// Voice similarity setting (0.0 to 1.0). Higher values make the voice more similar to the original speaker.
        /// </summary>
        [JsonPropertyName("similarity_boost")]
        public float SimilarityBoost { get; set; } = 0.85f;

        /// <summary>
        /// Voice style setting (0.0 to 1.0). Controls the style and expressiveness of the voice.
        /// </summary>
        [JsonPropertyName("style")]
        public float Style { get; set; } = 0.0f;

        /// <summary>
        /// Speaking rate setting (0.5 to 1.5). Controls the speed of speech.
        /// </summary>
        [JsonPropertyName("speaking_rate")]
        public float SpeakingRate { get; set; } = 0.9f;

        /// <summary>
        /// Creates voice settings optimized for Jarvis assistant.
        /// </summary>
        /// <returns>Optimized voice settings for Jarvis personality.</returns>
        public static VoiceSettings CreateJarvisSettings()
        {
            return new VoiceSettings
            {
                Stability = 0.75f,      // Natural variation while maintaining consistency
                SimilarityBoost = 0.85f, // High consistency for professional tone
                Style = 0.0f,           // Professional, measured tone
                SpeakingRate = 0.9f     // Slightly slower for measured pace
            };
        }

        /// <summary>
        /// Creates voice settings for emotional responses.
        /// </summary>
        /// <param name="emotion">The emotional context (excitement, concern, etc.)</param>
        /// <returns>Voice settings adapted for the specified emotion.</returns>
        public static VoiceSettings CreateEmotionalSettings(string emotion)
        {
            return emotion.ToLowerInvariant() switch
            {
                "excited" or "enthusiastic" => new VoiceSettings
                {
                    Stability = 0.6f,
                    SimilarityBoost = 0.8f,
                    Style = 0.3f,
                    SpeakingRate = 1.1f
                },
                "concerned" or "warning" => new VoiceSettings
                {
                    Stability = 0.8f,
                    SimilarityBoost = 0.9f,
                    Style = 0.1f,
                    SpeakingRate = 0.8f
                },
                "calm" or "reassuring" => new VoiceSettings
                {
                    Stability = 0.85f,
                    SimilarityBoost = 0.9f,
                    Style = 0.0f,
                    SpeakingRate = 0.85f
                },
                _ => CreateJarvisSettings()
            };
        }
    }
}
